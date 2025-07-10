using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeUnfucker.Services;
using CodeUnfucker.Analyzers;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// 代码分析命令
    /// </summary>
    public class AnalyzeCommand : BaseCommand
    {
        public override string Name => "analyze";
        public override string Description => "分析代码";

        public AnalyzeCommand(ILogger logger, IFileService fileService) 
            : base(logger, fileService)
        {
        }

        public override bool ValidateParameters(string path)
        {
            if (!FileService.DirectoryExists(path))
            {
                Logger.LogError($"分析模式下，路径必须是存在的目录: {path}");
                return false;
            }
            return true;
        }

        public override async Task<bool> ExecuteAsync(string path)
        {
            Logger.LogInfo($"开始分析代码，扫描路径: {path}");
            
            try
            {
                await AnalyzeCodeAsync(path);
                Logger.LogInfo("代码分析完成");
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("代码分析失败", ex);
                return false;
            }
        }

        protected override Task<bool> ProcessSingleFileAsync(string filePath)
        {
            // Analyze command doesn't process single files, it processes directories
            throw new NotSupportedException("分析命令只支持目录分析");
        }

        private async Task AnalyzeCodeAsync(string scriptPath)
        {
            var config = ConfigManager.GetAnalyzerConfig();
            var csFiles = FileService.GetCsFiles(scriptPath);
            
            if (csFiles.Length == 0)
            {
                Logger.LogWarn("未找到任何 .cs 文件");
                return;
            }

            if (config.OutputSettings.ShowFileCount)
            {
                Logger.LogInfo($"找到 {csFiles.Length} 个 .cs 文件");
            }

            if (config.AnalyzerSettings.EnableSyntaxAnalysis)
            {
                var syntaxTrees = await ParseSyntaxTreesAsync(csFiles);
                if (config.AnalyzerSettings.EnableSemanticAnalysis)
                {
                    var references = GetMetadataReferences();
                    var compilation = CreateCompilation(syntaxTrees, references);
                    
                    if (config.AnalyzerSettings.ShowReferencedAssemblies)
                    {
                        LogReferencedAssemblies(compilation);
                    }

                    // 运行静态分析器
                    if (config.AnalyzerSettings.EnableDiagnostics)
                    {
                        await RunStaticAnalyzersAsync(syntaxTrees, compilation, config);
                    }
                }
            }
            else
            {
                Logger.LogInfo("语法分析已禁用，跳过分析步骤");
            }
        }

        private async Task<List<SyntaxTree>> ParseSyntaxTreesAsync(string[] files)
        {
            var syntaxTrees = new List<SyntaxTree>();
            
            foreach (var file in files)
            {
                try
                {
                    var content = FileService.ReadAllText(file);
                    if (content != null)
                    {
                        var syntaxTree = CSharpSyntaxTree.ParseText(content, path: file);
                        syntaxTrees.Add(syntaxTree);
                    }
                }
                catch (Exception ex)
                {
                    Logger.LogError($"解析文件失败: {file}", ex);
                }
            }
            
            await Task.CompletedTask; // 保持异步签名一致性，为未来异步操作预留
            return syntaxTrees;
        }

        private List<MetadataReference> GetMetadataReferences()
        {
            var references = new List<MetadataReference>();
            
            try
            {
                // 添加基本的.NET引用
                var netAssemblies = new[]
                {
                    typeof(object).Assembly.Location, // System.Private.CoreLib
                    typeof(Console).Assembly.Location, // System.Console
                    typeof(System.Collections.Generic.List<>).Assembly.Location, // System.Collections
                    typeof(System.Linq.Enumerable).Assembly.Location, // System.Linq
                    typeof(System.IO.File).Assembly.Location, // System.IO.FileSystem
                };

                foreach (var assemblyPath in netAssemblies)
                {
                    if (!string.IsNullOrEmpty(assemblyPath) && File.Exists(assemblyPath))
                    {
                        references.Add(MetadataReference.CreateFromFile(assemblyPath));
                    }
                }
            }
            catch (Exception ex)
            {
                Logger.LogWarn($"获取元数据引用时发生错误: {ex.Message}");
            }
            
            return references;
        }

        private CSharpCompilation CreateCompilation(List<SyntaxTree> syntaxTrees, List<MetadataReference> references)
        {
            return CSharpCompilation.Create(
                "CodeAnalysis",
                syntaxTrees,
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private void LogReferencedAssemblies(CSharpCompilation compilation)
        {
            Logger.LogInfo("引用的程序集:");
            foreach (var reference in compilation.References)
            {
                if (reference is PortableExecutableReference peRef && !string.IsNullOrEmpty(peRef.FilePath))
                {
                    Logger.LogInfo($"  - {Path.GetFileName(peRef.FilePath)}");
                }
            }
        }

        private async Task RunStaticAnalyzersAsync(List<SyntaxTree> syntaxTrees, CSharpCompilation compilation, AnalyzerConfig config)
        {
            Logger.LogInfo("开始运行静态分析器...");
            var allDiagnostics = new List<Diagnostic>();
            int analyzedFileCount = 0;

            foreach (var syntaxTree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var fileName = Path.GetFileName(syntaxTree.FilePath);
                
                if (config.AnalyzerSettings.VerboseLogging)
                {
                    Logger.LogDebug($"分析文件: {fileName}");
                }

                // Pure Method Analyzer (UNITY0009/UNITY0010)
                if (config.StaticAnalysisRules.EnablePureMethodAnalysis)
                {
                    var pureAnalyzer = new PureMethodAnalyzer();
                    pureAnalyzer.AnalyzeSyntaxTree(syntaxTree, semanticModel);
                    allDiagnostics.AddRange(pureAnalyzer.Diagnostics);
                }

                // Unity Update Heap Allocation Analyzer (UNITY0001)
                if (config.StaticAnalysisRules.EnableUnityHeapAllocationAnalysis)
                {
                    var heapAnalyzer = new UnityUpdateHeapAllocationAnalyzer();
                    heapAnalyzer.AnalyzeSyntaxTree(syntaxTree, semanticModel);
                    allDiagnostics.AddRange(heapAnalyzer.Diagnostics);
                }

                analyzedFileCount++;
            }

            // 输出诊断结果
            OutputDiagnostics(allDiagnostics, config);
            Logger.LogInfo($"静态分析完成！分析了 {analyzedFileCount} 个文件，发现 {allDiagnostics.Count} 个问题");
            await Task.CompletedTask; // 保持异步签名一致性，为未来异步操作预留
        }

        private void OutputDiagnostics(List<Diagnostic> diagnostics, AnalyzerConfig config)
        {
            if (diagnostics.Count == 0)
            {
                Logger.LogInfo("✅ 未发现任何问题");
                return;
            }

            // 按严重程度分组
            var groupedDiagnostics = diagnostics.GroupBy(d => d.Severity).OrderByDescending(g => g.Key);

            foreach (var group in groupedDiagnostics)
            {
                Logger.LogInfo($"\n=== {GetSeverityDisplayName(group.Key)} ({group.Count()}) ===");
                
                foreach (var diagnostic in group.OrderBy(d => d.Location.SourceTree?.FilePath).ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Line))
                {
                    OutputDiagnostic(diagnostic, config);
                }
            }
        }

        private void OutputDiagnostic(Diagnostic diagnostic, AnalyzerConfig config)
        {
            var location = diagnostic.Location;
            var fileName = Path.GetFileName(location.SourceTree?.FilePath ?? "Unknown");
            var lineSpan = location.GetLineSpan();
            var line = lineSpan.StartLinePosition.Line + 1;
            var column = lineSpan.StartLinePosition.Character + 1;

            var severityIcon = GetSeverityIcon(diagnostic.Severity);
            var message = $"{severityIcon} [{diagnostic.Id}] {fileName}({line},{column}): {diagnostic.GetMessage()}";

            switch (diagnostic.Severity)
            {
                case DiagnosticSeverity.Error:
                    Logger.LogError(message);
                    break;
                case DiagnosticSeverity.Warning:
                    Logger.LogWarn(message);
                    break;
                case DiagnosticSeverity.Info:
                    Logger.LogInfo(message);
                    break;
                default:
                    Logger.LogDebug(message);
                    break;
            }

            if (config.OutputSettings.ShowDetailedErrors && !string.IsNullOrEmpty(diagnostic.Descriptor.Description?.ToString()))
            {
                Logger.LogInfo($"    描述: {diagnostic.Descriptor.Description}");
            }
        }

        private string GetSeverityDisplayName(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => "错误",
                DiagnosticSeverity.Warning => "警告", 
                DiagnosticSeverity.Info => "信息",
                DiagnosticSeverity.Hidden => "隐藏",
                _ => "未知"
            };
        }

        private string GetSeverityIcon(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => "❌",
                DiagnosticSeverity.Warning => "⚠️",
                DiagnosticSeverity.Info => "ℹ️",
                DiagnosticSeverity.Hidden => "👁️",
                _ => "❓"
            };
        }
    }
}