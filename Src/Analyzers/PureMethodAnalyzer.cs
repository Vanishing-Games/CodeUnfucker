using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker.Analyzers
{
    /// <summary>
    /// Pure 方法分析器 - 检测可以标记为 [Pure] 的方法和错误标记的方法
    /// </summary>
    public class PureMethodAnalyzer
    {
        private readonly List<Diagnostic> _diagnostics = new();
        private readonly HashSet<string> _pureMethodNames = new();
        private readonly HashSet<string> _unityApiMethods = new()
        {
            "Debug.Log", "Debug.LogWarning", "Debug.LogError", "Debug.LogException",
            "Console.WriteLine", "Console.Write",
            "UnityEngine.Object.Instantiate", "UnityEngine.Object.Destroy",
            "UnityEngine.Object.DestroyImmediate"
        };

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// 分析语法树中的方法
        /// </summary>
        public void AnalyzeSyntaxTree(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            var root = syntaxTree.GetRoot();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                AnalyzeMethod(method, semanticModel);
            }
        }

        /// <summary>
        /// 分析单个方法
        /// </summary>
        private void AnalyzeMethod(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null) return;

            bool hasPureAttribute = HasPureAttribute(method);
            bool shouldBePure = ShouldMethodBePure(method, semanticModel);
            bool hasSideEffects = HasSideEffects(method, semanticModel);

            // UNITY0009: 建议添加 [Pure] 属性
            if (!hasPureAttribute && shouldBePure && !hasSideEffects)
            {
                var diagnostic = CreateDiagnostic(
                    "UNITY0009",
                    "此方法可以标记为 [Pure]",
                    $"方法 '{methodSymbol.Name}' 无副作用且有返回值，建议添加 [System.Diagnostics.Contracts.Pure] 属性",
                    DiagnosticSeverity.Warning,
                    method.Identifier.GetLocation()
                );
                _diagnostics.Add(diagnostic);
            }

            // UNITY0010: 建议移除 [Pure] 属性
            if (hasPureAttribute && hasSideEffects)
            {
                var diagnostic = CreateDiagnostic(
                    "UNITY0010",
                    "此方法包含副作用，不应标记为 [Pure]",
                    $"方法 '{methodSymbol.Name}' 包含副作用，应移除 [Pure] 属性",
                    DiagnosticSeverity.Warning,
                    method.Identifier.GetLocation()
                );
                _diagnostics.Add(diagnostic);
            }
        }

        /// <summary>
        /// 检查方法是否已标记为 Pure
        /// </summary>
        private bool HasPureAttribute(MethodDeclarationSyntax method)
        {
            return method.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => attr.Name.ToString().Contains("Pure"));
        }

        /// <summary>
        /// 判断方法是否应该被标记为 Pure
        /// </summary>
        private bool ShouldMethodBePure(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            // 必须有返回值且不是 void
            if (method.ReturnType.ToString() == "void")
                return false;

            // 必须是 public 或 internal
            var accessibility = GetAccessibility(method);
            if (accessibility != "public" && accessibility != "internal")
                return false;

            // 不应该是 partial 方法
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.PartialKeyword)))
                return false;

            return true;
        }

        /// <summary>
        /// 检查方法是否有副作用
        /// </summary>
        private bool HasSideEffects(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            if (method.Body == null) return false;

            var walker = new SideEffectWalker(semanticModel, _unityApiMethods);
            walker.Visit(method.Body);
            return walker.HasSideEffects;
        }

        /// <summary>
        /// 获取方法的可访问性
        /// </summary>
        private string GetAccessibility(MethodDeclarationSyntax method)
        {
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)))
                return "public";
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.InternalKeyword)))
                return "internal";
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.ProtectedKeyword)))
                return "protected";
            if (method.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)))
                return "private";
            return "private"; // 默认为 private
        }

        /// <summary>
        /// 创建诊断信息
        /// </summary>
        private Diagnostic CreateDiagnostic(string id, string title, string message, 
            DiagnosticSeverity severity, Location location)
        {
            var descriptor = new DiagnosticDescriptor(
                id: id,
                title: title,
                messageFormat: message,
                category: "Performance",
                defaultSeverity: severity,
                isEnabledByDefault: true,
                description: message
            );

            return Diagnostic.Create(descriptor, location);
        }
    }

    /// <summary>
    /// 副作用检查器 - 遍历语法树检查是否有副作用
    /// </summary>
    internal class SideEffectWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _unityApiMethods;
        public bool HasSideEffects { get; private set; }

        public SideEffectWalker(SemanticModel semanticModel, HashSet<string> unityApiMethods)
        {
            _semanticModel = semanticModel;
            _unityApiMethods = unityApiMethods;
        }

        public override void VisitAssignmentExpression(AssignmentExpressionSyntax node)
        {
            // 任何赋值操作都可能是副作用
            HasSideEffects = true;
            base.VisitAssignmentExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            var memberAccess = node.Expression as MemberAccessExpressionSyntax;
            if (memberAccess != null)
            {
                var fullName = memberAccess.ToString();
                
                // 检查是否调用了 Unity API 或其他有副作用的方法
                if (_unityApiMethods.Any(api => fullName.Contains(api)))
                {
                    HasSideEffects = true;
                    return;
                }

                // 检查是否调用了 void 方法（可能有副作用）
                var symbolInfo = _semanticModel.GetSymbolInfo(node);
                if (symbolInfo.Symbol is IMethodSymbol method && method.ReturnsVoid)
                {
                    HasSideEffects = true;
                    return;
                }
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitPostfixUnaryExpression(PostfixUnaryExpressionSyntax node)
        {
            // ++ 和 -- 操作符
            if (node.IsKind(SyntaxKind.PostIncrementExpression) || 
                node.IsKind(SyntaxKind.PostDecrementExpression))
            {
                HasSideEffects = true;
            }
            base.VisitPostfixUnaryExpression(node);
        }

        public override void VisitPrefixUnaryExpression(PrefixUnaryExpressionSyntax node)
        {
            // ++ 和 -- 操作符
            if (node.IsKind(SyntaxKind.PreIncrementExpression) || 
                node.IsKind(SyntaxKind.PreDecrementExpression))
            {
                HasSideEffects = true;
            }
            base.VisitPrefixUnaryExpression(node);
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            // 创建对象可能有副作用（虽然不总是）
            // 这里可以根据具体需求调整
            base.VisitObjectCreationExpression(node);
        }
    }
}