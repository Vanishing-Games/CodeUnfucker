using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Diagnostics;

namespace CodeUnfucker;

/// <summary>
/// Pure 属性分析器
/// 检测应该添加或移除 [Pure] 属性的方法
/// </summary>
[DiagnosticAnalyzer(LanguageNames.CSharp)]
public class PureAnalyzer : DiagnosticAnalyzer
{
    // 诊断规则：建议添加 [Pure]
    public static readonly DiagnosticDescriptor SuggestAddPureRule = new DiagnosticDescriptor(
        "UNITY0009",
        "此方法可以标记为 [Pure]",
        "方法 '{0}' 无副作用且有返回值，建议添加 [Pure] 属性",
        "Purity",
        DiagnosticSeverity.Info,
        isEnabledByDefault: true,
        description: "此方法不会改变程序状态，可以安全地标记为 [Pure]。");

    // 诊断规则：建议移除 [Pure]
    public static readonly DiagnosticDescriptor SuggestRemovePureRule = new DiagnosticDescriptor(
        "UNITY0010",
        "此方法包含副作用，不应标记为 [Pure]",
        "方法 '{0}' 包含副作用，不应标记为 [Pure] 属性",
        "Purity",
        DiagnosticSeverity.Warning,
        isEnabledByDefault: true,
        description: "此方法包含副作用操作，标记为 [Pure] 是不正确的。");

    public override ImmutableArray<DiagnosticDescriptor> SupportedDiagnostics =>
        ImmutableArray.Create(SuggestAddPureRule, SuggestRemovePureRule);

    private PureAnalyzerConfig? _config;

    public override void Initialize(AnalysisContext context)
    {
        context.EnableConcurrentExecution();
        context.ConfigureGeneratedCodeAnalysis(GeneratedCodeAnalysisFlags.None);
        context.RegisterSyntaxNodeAction(AnalyzeMethod, SyntaxKind.MethodDeclaration);
        context.RegisterSyntaxNodeAction(AnalyzeProperty, SyntaxKind.PropertyDeclaration);
    }

    private PureAnalyzerConfig GetConfig()
    {
        if (_config == null)
        {
            var configPath = Path.Combine("Config", "PureAnalyzerConfig.json");
            _config = PureAnalyzerConfig.LoadFromFile(configPath);
        }
        return _config;
    }

    private void AnalyzeMethod(SyntaxNodeAnalysisContext context)
    {
        var methodDeclaration = (MethodDeclarationSyntax)context.Node;
        var config = GetConfig();

        if (!config.EnableSuggestAdd && !config.EnableSuggestRemove)
            return;

        // 检查可见性
        if (!IsTargetAccessibility(methodDeclaration, config))
            return;

        // 检查是否排除 partial 方法
        if (config.ExcludePartial && methodDeclaration.Modifiers.Any(SyntaxKind.PartialKeyword))
            return;

        var semanticModel = context.SemanticModel;
        var methodSymbol = semanticModel.GetDeclaredSymbol(methodDeclaration);
        
        if (methodSymbol == null)
            return;

        bool hasPureAttribute = HasPureAttribute(methodDeclaration);
        bool shouldBePure = ShouldBePure(methodDeclaration, methodSymbol, semanticModel, config);

        if (config.EnableSuggestAdd && !hasPureAttribute && shouldBePure)
        {
            // 建议添加 [Pure]
            var diagnostic = Diagnostic.Create(
                SuggestAddPureRule,
                methodDeclaration.Identifier.GetLocation(),
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
        else if (config.EnableSuggestRemove && hasPureAttribute && !shouldBePure)
        {
            // 建议移除 [Pure]
            var diagnostic = Diagnostic.Create(
                SuggestRemovePureRule,
                methodDeclaration.Identifier.GetLocation(),
                methodSymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private void AnalyzeProperty(SyntaxNodeAnalysisContext context)
    {
        var propertyDeclaration = (PropertyDeclarationSyntax)context.Node;
        var config = GetConfig();

        if (!config.AllowGetters || !config.EnableSuggestAdd)
            return;

        // 检查可见性
        if (!IsTargetAccessibility(propertyDeclaration, config))
            return;

        var semanticModel = context.SemanticModel;
        var propertySymbol = semanticModel.GetDeclaredSymbol(propertyDeclaration);

        if (propertySymbol == null || propertySymbol.IsWriteOnly)
            return;

        // 检查只读属性是否应该标记为 Pure
        var getter = propertyDeclaration.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));

        if (getter?.Body == null && getter?.ExpressionBody == null)
            return; // 自动属性，不需要检查

        bool hasPureAttribute = HasPureAttribute(propertyDeclaration);
        bool shouldBePure = ShouldPropertyBePure(propertyDeclaration, propertySymbol, semanticModel, config);

        if (!hasPureAttribute && shouldBePure)
        {
            var diagnostic = Diagnostic.Create(
                SuggestAddPureRule,
                propertyDeclaration.Identifier.GetLocation(),
                propertySymbol.Name);
            context.ReportDiagnostic(diagnostic);
        }
    }

    private bool IsTargetAccessibility(MemberDeclarationSyntax member, PureAnalyzerConfig config)
    {
        var modifiers = member.Modifiers;
        
        if (modifiers.Any(SyntaxKind.PublicKeyword) && config.Accessibility.Contains("public"))
            return true;
        
        if (modifiers.Any(SyntaxKind.InternalKeyword) && config.Accessibility.Contains("internal"))
            return true;
        
        if (modifiers.Any(SyntaxKind.ProtectedKeyword) && config.Accessibility.Contains("protected"))
            return true;
        
        if (modifiers.Any(SyntaxKind.PrivateKeyword) && config.Accessibility.Contains("private"))
            return true;

        // 默认访问级别检查
        if (!modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || 
                                m.IsKind(SyntaxKind.InternalKeyword) || 
                                m.IsKind(SyntaxKind.ProtectedKeyword) || 
                                m.IsKind(SyntaxKind.PrivateKeyword)))
        {
            // 默认为 internal (对于顶级成员) 或 private (对于嵌套成员)
            return config.Accessibility.Contains("internal") || config.Accessibility.Contains("private");
        }

        return false;
    }

    private bool HasPureAttribute(MemberDeclarationSyntax member)
    {
        return member.AttributeLists
            .SelectMany(al => al.Attributes)
            .Any(attr => 
            {
                var name = attr.Name.ToString();
                return name == "Pure" || 
                       name == "System.Diagnostics.Contracts.Pure" ||
                       name == "PureAttribute" ||
                       name == "System.Diagnostics.Contracts.PureAttribute";
            });
    }

    private bool ShouldBePure(MethodDeclarationSyntax method, IMethodSymbol methodSymbol, 
        SemanticModel semanticModel, PureAnalyzerConfig config)
    {
        // 检查返回类型
        if (methodSymbol.ReturnsVoid)
            return false;

        // 检查方法体
        if (method.Body == null && method.ExpressionBody == null)
            return false; // 抽象方法或接口方法

        // 分析方法体是否有副作用
        var hasSideEffects = HasSideEffects(method, semanticModel, config);
        return !hasSideEffects;
    }

    private bool ShouldPropertyBePure(PropertyDeclarationSyntax property, IPropertySymbol propertySymbol, 
        SemanticModel semanticModel, PureAnalyzerConfig config)
    {
        if (propertySymbol.IsWriteOnly)
            return false;

        var getter = property.AccessorList?.Accessors
            .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));

        if (getter == null)
            return false;

        // 分析 getter 是否有副作用
        var hasSideEffects = HasSideEffectsInAccessor(getter, semanticModel, config);
        return !hasSideEffects;
    }

    private bool HasSideEffects(MethodDeclarationSyntax method, SemanticModel semanticModel, PureAnalyzerConfig config)
    {
        var walker = new SideEffectWalker(semanticModel, config);
        
        if (method.Body != null)
            walker.Visit(method.Body);
        
        if (method.ExpressionBody != null)
            walker.Visit(method.ExpressionBody);
        
        return walker.HasSideEffects;
    }

    private bool HasSideEffectsInAccessor(AccessorDeclarationSyntax accessor, SemanticModel semanticModel, PureAnalyzerConfig config)
    {
        var walker = new SideEffectWalker(semanticModel, config);
        
        if (accessor.Body != null)
            walker.Visit(accessor.Body);
        
        if (accessor.ExpressionBody != null)
            walker.Visit(accessor.ExpressionBody);
        
        return walker.HasSideEffects;
    }
}

/// <summary>
/// 语法树遍历器，用于检测副作用
/// </summary>
public class SideEffectWalker : CSharpSyntaxWalker
{
    private readonly SemanticModel _semanticModel;
    private readonly PureAnalyzerConfig _config;
    private readonly List<Regex> _unityApiRegexes;

    public bool HasSideEffects { get; private set; }

    public SideEffectWalker(SemanticModel semanticModel, PureAnalyzerConfig config)
    {
        _semanticModel = semanticModel;
        _config = config;
        _unityApiRegexes = config.UnityApiPatterns
            .Select(pattern => new Regex(pattern, RegexOptions.Compiled))
            .ToList();
    }

    public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
    {
        // 赋值操作通常有副作用
        HasSideEffects = true;
        base.VisitAssignmentExpression(node);
    }

    public override void VisitInvocationExpression(InvocationExpressionSyntax node)
    {
        var symbolInfo = _semanticModel.GetSymbolInfo(node);
        
        if (symbolInfo.Symbol is IMethodSymbol methodSymbol)
        {
            // 检查是否是排除的方法
            var fullName = $"{methodSymbol.ContainingType?.Name}.{methodSymbol.Name}";
            if (_config.ExcludedMethods.Any(excluded => fullName.Contains(excluded)))
            {
                HasSideEffects = true;
                return;
            }

            // 检查是否是 Unity API
            if (IsUnityApi(node.ToString()) || IsUnityNamespace(methodSymbol))
            {
                HasSideEffects = true;
                return;
            }

            // 检查返回类型，void 方法通常有副作用
            if (methodSymbol.ReturnsVoid)
            {
                HasSideEffects = true;
                return;
            }

            // 检查是否已标记为 Pure
            if (!IsPureMethod(methodSymbol))
            {
                // 对于未知的非 void 方法，保守地认为有副作用
                // 除非能确定是纯方法（如数学函数等）
                if (!IsKnownPureMethod(methodSymbol))
                {
                    HasSideEffects = true;
                    return;
                }
            }
        }

        base.VisitInvocationExpression(node);
    }

    public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
    {
        // ++ 和 -- 操作符有副作用
        if (node.OperatorToken.IsKind(SyntaxKind.PlusPlusToken) || 
            node.OperatorToken.IsKind(SyntaxKind.MinusMinusToken))
        {
            HasSideEffects = true;
        }
        
        base.VisitPostfixUnaryExpression(node);
    }

    public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
    {
        // ++ 和 -- 操作符有副作用
        if (node.OperatorToken.IsKind(SyntaxKind.PlusPlusToken) || 
            node.OperatorToken.IsKind(SyntaxKind.MinusMinusToken))
        {
            HasSideEffects = true;
        }
        
        base.VisitPrefixUnaryExpression(node);
    }

    private bool IsUnityApi(string invocationText)
    {
        return _unityApiRegexes.Any(regex => regex.IsMatch(invocationText));
    }

    private bool IsUnityNamespace(IMethodSymbol methodSymbol)
    {
        var namespaceName = methodSymbol.ContainingNamespace?.ToDisplayString();
        return _config.ExcludedNamespaces.Any(excluded => 
            namespaceName?.StartsWith(excluded, StringComparison.OrdinalIgnoreCase) == true);
    }

    private bool IsPureMethod(IMethodSymbol methodSymbol)
    {
        return methodSymbol.GetAttributes()
            .Any(attr => attr.AttributeClass?.Name == "PureAttribute" ||
                        attr.AttributeClass?.ToDisplayString() == "System.Diagnostics.Contracts.PureAttribute");
    }

    private bool IsKnownPureMethod(IMethodSymbol methodSymbol)
    {
        var typeName = methodSymbol.ContainingType?.ToDisplayString();
        var methodName = methodSymbol.Name;

        // 已知的纯方法类型
        var knownPureTypes = new[]
        {
            "System.Math",
            "System.MathF",
            "System.String",
            "System.Convert",
            "System.Enum",
            "System.Type",
            "System.Reflection",
            "System.Linq.Enumerable"
        };

        return knownPureTypes.Any(knownType => typeName?.StartsWith(knownType) == true);
    }
}