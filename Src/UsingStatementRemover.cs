using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker
{
    public class UsingStatementRemover
    {
        private readonly UsingRemoverConfig _config;

        public UsingStatementRemover()
        {
            _config = ConfigManager.GetUsingRemoverConfig();
        }

        public string RemoveUnusedUsings(string sourceCode, string filePath)
        {
            try
            {
                var tree = CSharpSyntaxTree.ParseText(sourceCode);
                var root = tree.GetCompilationUnitRoot();

                // 获取所有using指令
                var usingDirectives = root.Usings.ToList();
                if (usingDirectives.Count == 0)
                {
                    return sourceCode; // 没有using语句，直接返回
                }

                // 创建编译以进行语义分析
                var compilation = CreateCompilation(tree);
                var semanticModel = compilation.GetSemanticModel(tree);

                // 分析哪些using是必需的
                var usedNamespaces = AnalyzeUsedNamespaces(root, semanticModel);
                var requiredUsings = GetRequiredUsings(usingDirectives, usedNamespaces);

                // 过滤掉配置中要保留的using
                requiredUsings = FilterPreservedUsings(requiredUsings);

                // 移除未使用的using
                var newRoot = root.WithUsings(SyntaxFactory.List(requiredUsings));
                
                return newRoot.ToFullString();
            }
            catch (Exception ex)
            {
                LogError($"移除未使用using失败 {filePath}: {ex.Message}");
                return sourceCode; // 出错时返回原始代码
            }
        }

        private CSharpCompilation CreateCompilation(SyntaxTree tree)
        {
            var references = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Runtime").Location)
            };

            // 添加更多常用的引用
            try
            {
                references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Collections").Location));
                references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.Text.RegularExpressions").Location));
                references.Add(MetadataReference.CreateFromFile(System.Reflection.Assembly.Load("System.ComponentModel.Primitives").Location));
            }
            catch
            {
                // 如果某些程序集加载失败，忽略它们
            }

            return CSharpCompilation.Create(
                "TempAssembly",
                new[] { tree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private HashSet<string> AnalyzeUsedNamespaces(CompilationUnitSyntax root, SemanticModel semanticModel)
        {
            var usedNamespaces = new HashSet<string>();
            var walker = new NamespaceUsageWalker(semanticModel, usedNamespaces);
            walker.Visit(root);
            return usedNamespaces;
        }

        private List<UsingDirectiveSyntax> GetRequiredUsings(List<UsingDirectiveSyntax> allUsings, HashSet<string> usedNamespaces)
        {
            var requiredUsings = new List<UsingDirectiveSyntax>();

            foreach (var usingDirective in allUsings)
            {
                var namespaceName = usingDirective.Name?.ToString();
                if (string.IsNullOrEmpty(namespaceName))
                    continue;

                // 检查是否被使用
                if (usedNamespaces.Contains(namespaceName) || 
                    usedNamespaces.Any(used => used.StartsWith(namespaceName + ".")))
                {
                    requiredUsings.Add(usingDirective);
                }
            }

            return requiredUsings;
        }

        private List<UsingDirectiveSyntax> FilterPreservedUsings(List<UsingDirectiveSyntax> usings)
        {
            if (_config.PreservedUsings == null || _config.PreservedUsings.Count == 0)
                return usings;

            var allUsings = new List<UsingDirectiveSyntax>(usings);

            // 添加配置中要保留的using
            foreach (var preservedUsing in _config.PreservedUsings)
            {
                if (!allUsings.Any(u => u.Name?.ToString() == preservedUsing))
                {
                    // 这个保留的using不在当前文件中，不需要添加
                    continue;
                }
            }

            return allUsings;
        }

        private static void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }

        private class NamespaceUsageWalker : CSharpSyntaxWalker
        {
            private readonly SemanticModel _semanticModel;
            private readonly HashSet<string> _usedNamespaces;

            public NamespaceUsageWalker(SemanticModel semanticModel, HashSet<string> usedNamespaces)
            {
                _semanticModel = semanticModel;
                _usedNamespaces = usedNamespaces;
            }

            public override void VisitIdentifierName(IdentifierNameSyntax node)
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(node);
                if (symbolInfo.Symbol != null)
                {
                    var namespaceName = GetNamespace(symbolInfo.Symbol);
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        _usedNamespaces.Add(namespaceName);
                    }
                }
                base.VisitIdentifierName(node);
            }

            public override void VisitQualifiedName(QualifiedNameSyntax node)
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(node);
                if (symbolInfo.Symbol != null)
                {
                    var namespaceName = GetNamespace(symbolInfo.Symbol);
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        _usedNamespaces.Add(namespaceName);
                    }
                }
                base.VisitQualifiedName(node);
            }

            public override void VisitGenericName(GenericNameSyntax node)
            {
                var symbolInfo = _semanticModel.GetSymbolInfo(node);
                if (symbolInfo.Symbol != null)
                {
                    var namespaceName = GetNamespace(symbolInfo.Symbol);
                    if (!string.IsNullOrEmpty(namespaceName))
                    {
                        _usedNamespaces.Add(namespaceName);
                    }
                }
                base.VisitGenericName(node);
            }

            private string? GetNamespace(ISymbol symbol)
            {
                var containingNamespace = symbol.ContainingNamespace;
                if (containingNamespace == null || containingNamespace.IsGlobalNamespace)
                    return null;

                return containingNamespace.ToDisplayString();
            }
        }
    }

    // 配置类
    public class UsingRemoverConfig
    {
        public string Description { get; set; } = "移除未使用using语句功能配置";
        public string Version { get; set; } = "1.0.0";
        public UsingRemoverSettings Settings { get; set; } = new();
        public List<string> PreservedUsings { get; set; } = new();
    }

    public class UsingRemoverSettings
    {
        public bool CreateBackupFiles { get; set; } = true;
        public string BackupFileExtension { get; set; } = ".backup";
        public bool VerboseLogging { get; set; } = false;
        public bool SortUsings { get; set; } = true;
        public bool RemoveEmptyLines { get; set; } = true;
    }
}