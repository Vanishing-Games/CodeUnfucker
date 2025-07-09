using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker
{
    /// <summary>
    /// Unity 性能分析器 - 检测 Update 方法中的堆内存分配操作
    /// </summary>
    public class UnityPerformanceAnalyzer
    {
        private readonly UnityAnalyzerConfig _config;
        private readonly List<UnityDiagnostic> _diagnostics;

        private static readonly HashSet<string> UnityUpdateMethods = new()
        {
            "Update",
            "LateUpdate", 
            "FixedUpdate",
            "OnGUI"
        };

        private static readonly HashSet<string> LinqMethods = new()
        {
            "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending",
            "GroupBy", "Join", "Concat", "Union", "Intersect", "Except",
            "Take", "Skip", "First", "FirstOrDefault", "Single", "SingleOrDefault",
            "Last", "LastOrDefault", "Any", "All", "Count", "Sum", "Average",
            "Min", "Max", "Aggregate", "ToList", "ToArray", "ToDictionary", "ToLookup"
        };

        private static readonly HashSet<string> ValueTypes = new()
        {
            "Vector2", "Vector3", "Vector4", "Quaternion", "Color", "Color32",
            "Matrix4x4", "Bounds", "Ray", "RaycastHit", "Rect", "RectInt",
            "Vector2Int", "Vector3Int", "LayerMask", "AnimationCurve",
            "int", "float", "double", "bool", "byte", "char", "decimal",
            "long", "short", "uint", "ulong", "ushort", "sbyte"
        };

        public UnityPerformanceAnalyzer(UnityAnalyzerConfig config)
        {
            _config = config;
            _diagnostics = new List<UnityDiagnostic>();
        }

        public List<UnityDiagnostic> AnalyzeSyntaxTree(SyntaxTree syntaxTree, SemanticModel? semanticModel = null)
        {
            _diagnostics.Clear();
            
            var root = syntaxTree.GetRoot();
            var classNodes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var classNode in classNodes)
            {
                if (IsMonoBehaviourClass(classNode, semanticModel))
                {
                    AnalyzeMonoBehaviourClass(classNode, syntaxTree.FilePath);
                }
            }

            return _diagnostics.ToList();
        }

        private bool IsMonoBehaviourClass(ClassDeclarationSyntax classNode, SemanticModel? semanticModel)
        {
            if (semanticModel == null)
            {
                // 回退到简单的字符串匹配
                var baseList = classNode.BaseList?.Types;
                if (baseList == null) return false;

                return baseList.Any(type => 
                    type.Type.ToString().Contains("MonoBehaviour") ||
                    type.Type.ToString().Contains("MonoBehavior"));
            }

            // 使用语义模型进行更精确的检查
            var classSymbol = semanticModel.GetDeclaredSymbol(classNode);
            if (classSymbol == null) return false;

            return InheritsFromMonoBehaviour(classSymbol);
        }

        private bool InheritsFromMonoBehaviour(INamedTypeSymbol classSymbol)
        {
            var baseType = classSymbol.BaseType;
            while (baseType != null)
            {
                if (baseType.Name == "MonoBehaviour" && 
                    baseType.ContainingNamespace?.ToDisplayString() == "UnityEngine")
                {
                    return true;
                }
                baseType = baseType.BaseType;
            }
            return false;
        }

        private void AnalyzeMonoBehaviourClass(ClassDeclarationSyntax classNode, string filePath)
        {
            var methods = classNode.Members.OfType<MethodDeclarationSyntax>();
            
            foreach (var method in methods)
            {
                if (IsUnityUpdateMethod(method))
                {
                    AnalyzeUpdateMethod(method, classNode.Identifier.ValueText, filePath);
                }
            }
        }

        private bool IsUnityUpdateMethod(MethodDeclarationSyntax method)
        {
            var methodName = method.Identifier.ValueText;
            
            // 检查是否是预定义的 Unity 更新方法
            if (UnityUpdateMethods.Contains(methodName))
                return true;

            // 检查是否是配置中的自定义方法
            return _config.CustomUpdateMethods.Contains(methodName);
        }

        private void AnalyzeUpdateMethod(MethodDeclarationSyntax method, string className, string filePath)
        {
            if (method.Body == null) return;

            var walker = new HeapAllocationWalker(_config, _diagnostics, className, method.Identifier.ValueText, filePath);
            walker.Visit(method.Body);
        }
    }

    /// <summary>
    /// 堆内存分配检测遍历器
    /// </summary>
    public class HeapAllocationWalker : CSharpSyntaxWalker
    {
        private readonly UnityAnalyzerConfig _config;
        private readonly List<UnityDiagnostic> _diagnostics;
        private readonly string _className;
        private readonly string _methodName;
        private readonly string _filePath;

        public HeapAllocationWalker(UnityAnalyzerConfig config, List<UnityDiagnostic> diagnostics, 
            string className, string methodName, string filePath)
        {
            _config = config;
            _diagnostics = diagnostics;
            _className = className;
            _methodName = methodName;
            _filePath = filePath;
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            if (_config.CheckNewKeyword)
            {
                var typeName = node.Type.ToString();
                if (!IsValueType(typeName))
                {
                    AddDiagnostic(UnityDiagnosticType.NewKeyword, 
                        $"在 {_methodName}() 中使用 'new {typeName}' 会产生堆内存分配",
                        node.GetLocation());
                }
            }
            base.VisitObjectCreationExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            if (_config.CheckLinqMethods)
            {
                CheckLinqMethodCall(node);
            }
            base.VisitInvocationExpression(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            if (_config.CheckStringConcatenation && node.IsKind(SyntaxKind.AddExpression))
            {
                CheckStringConcatenation(node);
            }
            base.VisitBinaryExpression(node);
        }

        public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            if (_config.CheckStringInterpolation)
            {
                AddDiagnostic(UnityDiagnosticType.StringInterpolation,
                    $"在 {_methodName}() 中使用字符串插值会产生堆内存分配",
                    node.GetLocation());
            }
            base.VisitInterpolatedStringExpression(node);
        }

        public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            if (_config.CheckCollectionInitialization)
            {
                AddDiagnostic(UnityDiagnosticType.CollectionInitialization,
                    $"在 {_methodName}() 中使用集合初始化会产生堆内存分配",
                    node.GetLocation());
            }
            base.VisitImplicitArrayCreationExpression(node);
        }

        public override void VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            if (_config.CheckCollectionInitialization && 
                (node.IsKind(SyntaxKind.CollectionInitializerExpression) ||
                 node.IsKind(SyntaxKind.ArrayInitializerExpression)))
            {
                AddDiagnostic(UnityDiagnosticType.CollectionInitialization,
                    $"在 {_methodName}() 中使用集合初始化会产生堆内存分配",
                    node.GetLocation());
            }
            base.VisitInitializerExpression(node);
        }

        public override void VisitLambdaExpression(LambdaExpressionSyntax node)
        {
            if (_config.CheckClosures)
            {
                // 简单检测：如果 lambda 表达式在方法内部，可能产生闭包
                AddDiagnostic(UnityDiagnosticType.ImplicitClosure,
                    $"在 {_methodName}() 中使用 Lambda 表达式可能产生隐式闭包和堆内存分配",
                    node.GetLocation());
            }
            base.VisitLambdaExpression(node);
        }

        private void CheckLinqMethodCall(InvocationExpressionSyntax node)
        {
            var memberAccess = node.Expression as MemberAccessExpressionSyntax;
            if (memberAccess == null) return;

            var methodName = memberAccess.Name.Identifier.ValueText;
            if (UnityPerformanceAnalyzer.LinqMethods.Contains(methodName))
            {
                AddDiagnostic(UnityDiagnosticType.LinqMethod,
                    $"在 {_methodName}() 中使用 LINQ 方法 '{methodName}' 会产生堆内存分配",
                    node.GetLocation());
            }
        }

        private void CheckStringConcatenation(BinaryExpressionSyntax node)
        {
            // 检查是否是字符串拼接
            if (IsStringExpression(node.Left) || IsStringExpression(node.Right))
            {
                AddDiagnostic(UnityDiagnosticType.StringConcatenation,
                    $"在 {_methodName}() 中使用字符串拼接 '+' 会产生堆内存分配",
                    node.GetLocation());
            }
        }

        private bool IsStringExpression(ExpressionSyntax expression)
        {
            // 简单检测字符串字面量
            if (expression is LiteralExpressionSyntax literal &&
                literal.IsKind(SyntaxKind.StringLiteralExpression))
            {
                return true;
            }

            // 检测可能的字符串变量（基于常见命名）
            if (expression is IdentifierNameSyntax identifier)
            {
                var name = identifier.Identifier.ValueText.ToLower();
                return name.Contains("str") || name.Contains("text") || name.Contains("message");
            }

            return false;
        }

        private bool IsValueType(string typeName)
        {
            return UnityPerformanceAnalyzer.ValueTypes.Contains(typeName) ||
                   UnityPerformanceAnalyzer.ValueTypes.Any(vt => typeName.StartsWith(vt));
        }

        private void AddDiagnostic(UnityDiagnosticType type, string message, Location location)
        {
            _diagnostics.Add(new UnityDiagnostic
            {
                Id = "UNITY0001",
                Type = type,
                Severity = DiagnosticSeverity.Warning,
                Message = message,
                FilePath = _filePath,
                ClassName = _className,
                MethodName = _methodName,
                Location = location,
                LineNumber = location.GetLineSpan().StartLinePosition.Line + 1,
                ColumnNumber = location.GetLineSpan().StartLinePosition.Character + 1
            });
        }
    }

    /// <summary>
    /// Unity 诊断信息
    /// </summary>
    public class UnityDiagnostic
    {
        public string Id { get; set; } = string.Empty;
        public UnityDiagnosticType Type { get; set; }
        public DiagnosticSeverity Severity { get; set; }
        public string Message { get; set; } = string.Empty;
        public string FilePath { get; set; } = string.Empty;
        public string ClassName { get; set; } = string.Empty;
        public string MethodName { get; set; } = string.Empty;
        public Location Location { get; set; } = Location.None;
        public int LineNumber { get; set; }
        public int ColumnNumber { get; set; }

        public override string ToString()
        {
            var fileName = System.IO.Path.GetFileName(FilePath);
            return $"[{Id}] {fileName}({LineNumber},{ColumnNumber}): {Severity.ToString().ToLower()}: {Message}";
        }
    }

    /// <summary>
    /// Unity 诊断类型
    /// </summary>
    public enum UnityDiagnosticType
    {
        NewKeyword,
        LinqMethod,
        StringConcatenation,
        StringInterpolation,
        ImplicitClosure,
        CollectionInitialization
    }

    /// <summary>
    /// Unity 分析器配置
    /// </summary>
    public class UnityAnalyzerConfig
    {
        public bool EnableUnityAnalysis { get; set; } = true;
        public bool CheckNewKeyword { get; set; } = true;
        public bool CheckLinqMethods { get; set; } = true;
        public bool CheckStringConcatenation { get; set; } = true;
        public bool CheckStringInterpolation { get; set; } = true;
        public bool CheckClosures { get; set; } = true;
        public bool CheckCollectionInitialization { get; set; } = true;
        public List<string> CustomUpdateMethods { get; set; } = new();
        public List<string> ExcludedValueTypes { get; set; } = new();
        public DiagnosticSeverity DefaultSeverity { get; set; } = DiagnosticSeverity.Warning;
    }
}