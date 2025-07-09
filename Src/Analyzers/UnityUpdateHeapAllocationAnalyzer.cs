using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker.Analyzers
{
    /// <summary>
    /// Unity Update 方法堆内存分配检测器 - 检测每帧调用方法中的堆分配操作
    /// </summary>
    public class UnityUpdateHeapAllocationAnalyzer
    {
        private readonly List<Diagnostic> _diagnostics = new();
        private readonly HashSet<string> _unityUpdateMethods = new()
        {
            "Update", "LateUpdate", "FixedUpdate", "OnGUI"
        };

        private readonly HashSet<string> _linqMethods = new()
        {
            "Where", "Select", "SelectMany", "OrderBy", "OrderByDescending",
            "GroupBy", "Join", "First", "FirstOrDefault", "Last", "LastOrDefault",
            "Single", "SingleOrDefault", "Take", "Skip", "Distinct", "Union",
            "Intersect", "Except", "Reverse", "ToArray", "ToList", "ToDictionary"
        };

        private readonly HashSet<string> _valueTypes = new()
        {
            "int", "float", "double", "bool", "char", "byte", "sbyte",
            "short", "ushort", "uint", "long", "ulong", "decimal",
            "Vector2", "Vector3", "Vector4", "Quaternion", "Color", "Color32"
        };

        public IReadOnlyList<Diagnostic> Diagnostics => _diagnostics;

        /// <summary>
        /// 分析语法树中的 Unity MonoBehaviour 类
        /// </summary>
        public void AnalyzeSyntaxTree(SyntaxTree syntaxTree, SemanticModel semanticModel)
        {
            var root = syntaxTree.GetRoot();
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();

            foreach (var @class in classes)
            {
                if (IsMonoBehaviourClass(@class, semanticModel))
                {
                    AnalyzeMonoBehaviourClass(@class, semanticModel);
                }
            }
        }

        /// <summary>
        /// 检查类是否继承自 MonoBehaviour
        /// </summary>
        private bool IsMonoBehaviourClass(ClassDeclarationSyntax @class, SemanticModel semanticModel)
        {
            if (@class.BaseList == null) return false;

            foreach (var baseType in @class.BaseList.Types)
            {
                // 首先尝试语义分析
                var baseTypeInfo = semanticModel.GetTypeInfo(baseType.Type);
                if (baseTypeInfo.Type != null)
                {
                    var typeName = baseTypeInfo.Type.ToDisplayString();
                    if (typeName.Contains("MonoBehaviour") || typeName.Contains("UnityEngine.MonoBehaviour"))
                    {
                        return true;
                    }
                }

                // 如果语义分析失败，使用语法分析作为后备
                var baseTypeName = baseType.Type.ToString();
                if (baseTypeName.Contains("MonoBehaviour"))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// 分析 MonoBehaviour 类中的 Update 方法
        /// </summary>
        private void AnalyzeMonoBehaviourClass(ClassDeclarationSyntax @class, SemanticModel semanticModel)
        {
            var methods = @class.Members.OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                if (_unityUpdateMethods.Contains(method.Identifier.ValueText))
                {
                    AnalyzeUpdateMethod(method, semanticModel);
                }
            }
        }

        /// <summary>
        /// 分析 Update 方法中的堆分配
        /// </summary>
        private void AnalyzeUpdateMethod(MethodDeclarationSyntax method, SemanticModel semanticModel)
        {
            if (method.Body == null) return;

            var walker = new HeapAllocationWalker(semanticModel, _linqMethods, _valueTypes);
            walker.Visit(method.Body);

            foreach (var allocation in walker.HeapAllocations)
            {
                var diagnostic = CreateDiagnostic(
                    "UNITY0001",
                    "Unity Update 方法中检测到堆内存分配",
                    $"在 {method.Identifier.ValueText}() 方法中发现堆内存分配：{allocation.Description}，这可能导致 GC 压力",
                    DiagnosticSeverity.Warning,
                    allocation.Location
                );
                _diagnostics.Add(diagnostic);
            }
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
    /// 堆分配信息
    /// </summary>
    internal class HeapAllocation
    {
        public string Description { get; set; } = string.Empty;
        public Location Location { get; set; } = Location.None;
    }

    /// <summary>
    /// 堆分配检查器 - 遍历语法树检查堆内存分配
    /// </summary>
    internal class HeapAllocationWalker : CSharpSyntaxWalker
    {
        private readonly SemanticModel _semanticModel;
        private readonly HashSet<string> _linqMethods;
        private readonly HashSet<string> _valueTypes;
        public List<HeapAllocation> HeapAllocations { get; } = new();

        public HeapAllocationWalker(SemanticModel semanticModel, HashSet<string> linqMethods, HashSet<string> valueTypes)
        {
            _semanticModel = semanticModel;
            _linqMethods = linqMethods;
            _valueTypes = valueTypes;
        }

        public override void VisitObjectCreationExpression(ObjectCreationExpressionSyntax node)
        {
            var typeInfo = _semanticModel.GetTypeInfo(node.Type);
            if (typeInfo.Type != null)
            {
                var typeName = typeInfo.Type.Name;
                
                // 排除值类型的创建
                if (!_valueTypes.Contains(typeName) && !typeInfo.Type.IsValueType)
                {
                    HeapAllocations.Add(new HeapAllocation
                    {
                        Description = $"new {typeName}()",
                        Location = node.GetLocation()
                    });
                }
            }
            else
            {
                // 语义分析失败时的语法分析后备
                var syntaxTypeName = node.Type.ToString();
                
                // 检查是否是明显的值类型 - 更精确的检查，避免误匹配泛型参数
                var typeName = syntaxTypeName.Split('<')[0].Split('.').Last(); // 获取主类型名，去掉泛型和命名空间
                var isKnownValueType = _valueTypes.Contains(typeName);
                if (!isKnownValueType)
                {
                    HeapAllocations.Add(new HeapAllocation
                    {
                        Description = $"new {syntaxTypeName}",
                        Location = node.GetLocation()
                    });
                }
            }

            base.VisitObjectCreationExpression(node);
        }

        public override void VisitInvocationExpression(InvocationExpressionSyntax node)
        {
            // 检查 LINQ 方法调用
            if (node.Expression is MemberAccessExpressionSyntax memberAccess)
            {
                var methodName = memberAccess.Name.Identifier.ValueText;
                if (_linqMethods.Contains(methodName))
                {
                    HeapAllocations.Add(new HeapAllocation
                    {
                        Description = $"LINQ 方法 .{methodName}()",
                        Location = node.GetLocation()
                    });
                }
            }

            base.VisitInvocationExpression(node);
        }

        public override void VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // 检查字符串拼接
            if (node.IsKind(SyntaxKind.AddExpression))
            {
                var leftType = _semanticModel.GetTypeInfo(node.Left).Type;
                var rightType = _semanticModel.GetTypeInfo(node.Right).Type;

                if (IsStringType(leftType) || IsStringType(rightType))
                {
                    HeapAllocations.Add(new HeapAllocation
                    {
                        Description = "字符串拼接操作 (+)",
                        Location = node.GetLocation()
                    });
                }
            }

            base.VisitBinaryExpression(node);
        }

        public override void VisitInterpolatedStringExpression(InterpolatedStringExpressionSyntax node)
        {
            // 字符串插值
            HeapAllocations.Add(new HeapAllocation
            {
                Description = "字符串插值 ($\"...\")",
                Location = node.GetLocation()
            });

            base.VisitInterpolatedStringExpression(node);
        }

        public override void VisitImplicitObjectCreationExpression(ImplicitObjectCreationExpressionSyntax node)
        {
            // new() 表达式
            HeapAllocations.Add(new HeapAllocation
            {
                Description = "隐式对象创建 (new())",
                Location = node.GetLocation()
            });

            base.VisitImplicitObjectCreationExpression(node);
        }

        public override void VisitArrayCreationExpression(ArrayCreationExpressionSyntax node)
        {
            // 数组创建
            var typeInfo = _semanticModel.GetTypeInfo(node.Type);
            if (typeInfo.Type != null)
            {
                HeapAllocations.Add(new HeapAllocation
                {
                    Description = $"数组创建 ({typeInfo.Type.Name}[])",
                    Location = node.GetLocation()
                });
            }

            base.VisitArrayCreationExpression(node);
        }

        public override void VisitImplicitArrayCreationExpression(ImplicitArrayCreationExpressionSyntax node)
        {
            // 隐式数组创建 new[] { ... }
            HeapAllocations.Add(new HeapAllocation
            {
                Description = "隐式数组创建 (new[] { ... })",
                Location = node.GetLocation()
            });

            base.VisitImplicitArrayCreationExpression(node);
        }

        public override void VisitInitializerExpression(InitializerExpressionSyntax node)
        {
            // 集合初始化器可能分配堆内存
            if (node.IsKind(SyntaxKind.CollectionInitializerExpression) || 
                node.IsKind(SyntaxKind.ObjectInitializerExpression))
            {
                HeapAllocations.Add(new HeapAllocation
                {
                    Description = "集合/对象初始化器",
                    Location = node.GetLocation()
                });
            }

            base.VisitInitializerExpression(node);
        }

        public override void VisitAnonymousObjectCreationExpression(AnonymousObjectCreationExpressionSyntax node)
        {
            // 匿名对象创建
            HeapAllocations.Add(new HeapAllocation
            {
                Description = "匿名对象创建 (new { ... })",
                Location = node.GetLocation()
            });

            base.VisitAnonymousObjectCreationExpression(node);
        }

        public override void VisitParenthesizedLambdaExpression(ParenthesizedLambdaExpressionSyntax node)
        {
            // Lambda 表达式可能产生闭包
            CheckForClosure(node.Body, node.GetLocation());
            base.VisitParenthesizedLambdaExpression(node);
        }

        public override void VisitSimpleLambdaExpression(SimpleLambdaExpressionSyntax node)
        {
            // Lambda 表达式可能产生闭包
            CheckForClosure(node.Body, node.GetLocation());
            base.VisitSimpleLambdaExpression(node);
        }

        private void CheckForClosure(SyntaxNode lambdaBody, Location location)
        {
            // 简单检查：如果 lambda 中引用了外部变量，可能产生闭包
            // 这是一个简化的检查，实际情况更复杂
            var identifiers = lambdaBody.DescendantNodes().OfType<IdentifierNameSyntax>();
            if (identifiers.Any())
            {
                HeapAllocations.Add(new HeapAllocation
                {
                    Description = "Lambda 表达式可能产生闭包",
                    Location = location
                });
            }
        }

        private bool IsStringType(ITypeSymbol? type)
        {
            return type?.SpecialType == SpecialType.System_String;
        }
    }
}