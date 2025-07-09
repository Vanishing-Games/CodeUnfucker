using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using CodeUnfucker.Analyzers;

namespace CodeUnfucker
{
    public class Program
    {
        static void Main(string[] args)
        {
            LogInfo("CodeUnfucker 启动");
            var program = new Program();
            program.Run(args);
            LogInfo("CodeUnfucker 运行结束");
        }

        public void Run(string[] args)
        {
            if (!ValidateArgs(args, out string command, out string path, out string? configPath))
                return;
            // 如果指定了配置路径，设置它
            if (!string.IsNullOrEmpty(configPath))
            {
                SetupConfig(configPath);
            }

            switch (command.ToLower())
            {
                case "analyze":
                    AnalyzeCode(path);
                    break;
                case "format":
                    FormatCode(path);
                    break;
                case "csharpier":
                    FormatCodeWithCSharpier(path);
                    break;
                case "rmusing":
                    RemoveUnusedUsings(path);
                    break;
                case "roslynator":
                    RunRoslynatorRefactoring(path);
                    break;
                default:
                    LogError($"未知命令: {command}");
                    LogError("支持的命令: analyze, format, csharpier, rmusing, roslynator");
                    ShowUsage();
                    break;
            }
        }

        public static bool ValidateArgs(string[] args, out string command, out string path, out string? configPath)
        {
            command = string.Empty;
            path = string.Empty;
            configPath = null;
            // 检查帮助选项
            if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help"))
            {
                ShowUsage();
                return false;
            }

            if (args.Length == 1)
            {
                // 向后兼容：如果只有一个参数，默认为analyze命令
                command = "analyze";
                path = args[0];
            }
            else if (args.Length == 2)
            {
                command = args[0];
                path = args[1];
            }
            else if (args.Length == 4 && (args[2] == "--config" || args[2] == "-c"))
            {
                command = args[0];
                path = args[1];
                configPath = args[3];
            }
            else
            {
                ShowUsage();
                return false;
            }

            if (command == "analyze" && !Directory.Exists(path))
            {
                LogError($"分析模式下，路径必须是存在的目录: {path}");
                return false;
            }

            if ((command == "format" || command == "csharpier" || command == "rmusing" || command == "roslynator") && !File.Exists(path) && !Directory.Exists(path))
            {
                LogError($"格式化/处理模式下，路径必须是存在的文件或目录: {path}");
                return false;
            }

            return true;
        }

        private static void ShowUsage()
        {
            LogInfo("用法:");
            LogInfo("  CodeUnfucker <command> <path> [--config <config-path>]");
            LogInfo("");
            LogInfo("命令:");
            LogInfo("  analyze   - 分析代码");
            LogInfo("  format    - 使用内置格式化器格式化代码");
            LogInfo("  csharpier - 使用CSharpier格式化代码");
            LogInfo("  rmusing   - 移除未使用的using语句");
            LogInfo("  roslynator - 使用Roslynator重构代码");
            LogInfo("");
            LogInfo("选项:");
            LogInfo("  --config, -c  - 指定配置文件目录路径");
            LogInfo("");
            LogInfo("示例:");
            LogInfo("  CodeUnfucker analyze ./Scripts");
            LogInfo("  CodeUnfucker format ./Scripts --config ./MyConfig");
            LogInfo("  CodeUnfucker csharpier MyFile.cs");
            LogInfo("  CodeUnfucker rmusing ./Scripts");
            LogInfo("  CodeUnfucker roslynator ./Scripts");
        }

        private void SetupConfig(string? configPath)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                if (Directory.Exists(configPath))
                {
                    ConfigManager.SetConfigPath(configPath);
                }
                else
                {
                    LogError($"指定的配置路径不存在: {configPath}");
                    LogInfo("使用默认配置");
                }
            }
        }

        private void FormatCodeWithCSharpier(string path)
        {
            LogInfo($"开始使用CSharpier格式化代码，扫描路径: {path}");
            // 直接调用格式化逻辑，强制使用CSharpier
            FormatCodeInternal(path, true);
        }

        private void FormatCodeInternal(string path, bool forceCSharpier = false)
        {
            if (File.Exists(path) && path.EndsWith(".cs"))
            {
                // 格式化单个文件
                FormatSingleFile(path, forceCSharpier);
            }
            else if (Directory.Exists(path))
            {
                // 格式化目录中的所有文件
                FormatDirectory(path, forceCSharpier);
            }
            else
            {
                LogError($"无效的路径: {path}");
            }
        }

        private void AnalyzeCode(string scriptPath)
        {
            var config = ConfigManager.GetAnalyzerConfig();
            LogInfo($"开始分析代码，扫描路径: {scriptPath}");
            var csFiles = GetCsFiles(scriptPath);
            if (csFiles.Length == 0)
            {
                LogWarn("未找到任何 .cs 文件");
                return;
            }

            if (config.OutputSettings.ShowFileCount)
            {
                LogInfo($"找到 {csFiles.Length} 个 .cs 文件");
            }

            if (config.AnalyzerSettings.EnableSyntaxAnalysis)
            {
                var syntaxTrees = ParseSyntaxTrees(csFiles);
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
                        RunStaticAnalyzers(syntaxTrees, compilation, config);
                    }
                }
            }
            else
            {
                LogInfo("语法分析已禁用，跳过分析步骤");
            }
        }

        /// <summary>
        /// 运行所有静态分析器
        /// </summary>
        private void RunStaticAnalyzers(List<SyntaxTree> syntaxTrees, CSharpCompilation compilation, AnalyzerConfig config)
        {
            LogInfo("开始运行静态分析器...");
            var allDiagnostics = new List<Diagnostic>();
            int analyzedFileCount = 0;

            foreach (var syntaxTree in syntaxTrees)
            {
                var semanticModel = compilation.GetSemanticModel(syntaxTree);
                var fileName = Path.GetFileName(syntaxTree.FilePath);
                
                if (config.AnalyzerSettings.VerboseLogging)
                {
                    LogDebug($"分析文件: {fileName}");
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
            LogInfo($"静态分析完成！分析了 {analyzedFileCount} 个文件，发现 {allDiagnostics.Count} 个问题");
        }

        /// <summary>
        /// 输出诊断结果
        /// </summary>
        private void OutputDiagnostics(List<Diagnostic> diagnostics, AnalyzerConfig config)
        {
            if (diagnostics.Count == 0)
            {
                LogInfo("✅ 未发现任何问题");
                return;
            }

            // 按严重程度分组
            var groupedDiagnostics = diagnostics.GroupBy(d => d.Severity).OrderByDescending(g => g.Key);

            foreach (var group in groupedDiagnostics)
            {
                LogInfo($"\n=== {GetSeverityDisplayName(group.Key)} ({group.Count()}) ===");
                
                foreach (var diagnostic in group.OrderBy(d => d.Location.SourceTree?.FilePath).ThenBy(d => d.Location.GetLineSpan().StartLinePosition.Line))
                {
                    OutputDiagnostic(diagnostic, config);
                }
            }
        }

        /// <summary>
        /// 输出单个诊断信息
        /// </summary>
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
                    LogError(message);
                    break;
                case DiagnosticSeverity.Warning:
                    LogWarn(message);
                    break;
                case DiagnosticSeverity.Info:
                    LogInfo(message);
                    break;
                default:
                    LogDebug(message);
                    break;
            }

            if (config.OutputSettings.ShowDetailedErrors && !string.IsNullOrEmpty(diagnostic.Descriptor.Description?.ToString()))
            {
                LogInfo($"    描述: {diagnostic.Descriptor.Description}");
            }
        }

        /// <summary>
        /// 获取严重程度显示名称
        /// </summary>
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

        /// <summary>
        /// 获取严重程度图标
        /// </summary>
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

        private void FormatCode(string path)
        {
            LogInfo($"开始格式化代码，扫描路径: {path}");
            FormatCodeInternal(path, false);
        }

        private void RemoveUnusedUsings(string path)
        {
            LogInfo($"开始移除未使用的using语句，扫描路径: {path}");
            RemoveUnusedUsingsInternal(path);
        }

        private void RunRoslynatorRefactoring(string path)
        {
            LogInfo($"开始Roslynator重构，扫描路径: {path}");
            var refactorer = new RoslynatorRefactorer();
            refactorer.RefactorCode(path);
        }

        private void RemoveUnusedUsingsInternal(string path)
        {
            if (File.Exists(path) && path.EndsWith(".cs"))
            {
                // 处理单个文件
                RemoveUnusedUsingsFromSingleFile(path);
            }
            else if (Directory.Exists(path))
            {
                // 处理目录中的所有文件
                RemoveUnusedUsingsFromDirectory(path);
            }
            else
            {
                LogError($"无效的路径: {path}");
            }
        }

        private void RemoveUnusedUsingsFromSingleFile(string filePath)
        {
            try
            {
                LogInfo($"移除未使用using: {filePath}");
                string originalCode = File.ReadAllText(filePath);
                
                var remover = new UsingStatementRemover();
                string processedCode = remover.RemoveUnusedUsings(originalCode, filePath);

                var config = ConfigManager.GetUsingRemoverConfig();
                
                // 根据配置决定是否创建备份
                if (config.Settings.CreateBackupFiles)
                {
                    string backupPath = filePath + config.Settings.BackupFileExtension;
                    File.Copy(filePath, backupPath, true);
                    LogInfo($"已创建备份: {backupPath}");
                }

                // 写入处理后的代码
                File.WriteAllText(filePath, processedCode);
                LogInfo($"✅ 移除未使用using完成: {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"移除未使用using失败 {filePath}: {ex.Message}");
            }
        }

        private void RemoveUnusedUsingsFromDirectory(string directoryPath)
        {
            var csFiles = GetCsFiles(directoryPath);
            if (csFiles.Length == 0)
            {
                LogWarn("未找到任何 .cs 文件");
                return;
            }

            LogInfo($"找到 {csFiles.Length} 个 .cs 文件，开始批量移除未使用using...");
            int successCount = 0;
            int failureCount = 0;
            
            foreach (var file in csFiles)
            {
                try
                {
                    RemoveUnusedUsingsFromSingleFile(file);
                    successCount++;
                }
                catch (Exception ex)
                {
                    LogError($"处理失败 {file}: {ex.Message}");
                    failureCount++;
                }
            }

            LogInfo($"移除未使用using完成！成功: {successCount}, 失败: {failureCount}");
        }

        private void FormatSingleFile(string filePath, bool forceCSharpier = false)
        {
            try
            {
                LogInfo($"格式化文件: {filePath}");
                string originalCode = File.ReadAllText(filePath);
                string formattedCode;
                if (forceCSharpier)
                {
                    var csharpierFormatter = new CSharpierFormatter();
                    formattedCode = csharpierFormatter.FormatCode(originalCode, filePath);
                }
                else
                {
                    var formatter = new CodeFormatter();
                    formattedCode = formatter.FormatCode(originalCode, filePath);
                }

                var config = ConfigManager.GetFormatterConfig();
                // 根据配置决定是否创建备份
                if (config.FormatterSettings.CreateBackupFiles)
                {
                    string backupPath = filePath + config.FormatterSettings.BackupFileExtension;
                    File.Copy(filePath, backupPath, true);
                    LogInfo($"已创建备份: {backupPath}");
                }

                // 写入格式化后的代码
                File.WriteAllText(filePath, formattedCode);
                LogInfo($"✅ 格式化完成: {filePath}");
            }
            catch (Exception ex)
            {
                LogError($"格式化文件失败 {filePath}: {ex.Message}");
            }
        }

        private void FormatDirectory(string directoryPath, bool forceCSharpier = false)
        {
            var csFiles = GetCsFiles(directoryPath);
            if (csFiles.Length == 0)
            {
                LogWarn("未找到任何 .cs 文件");
                return;
            }

            LogInfo($"找到 {csFiles.Length} 个 .cs 文件，开始批量格式化...");
            int successCount = 0;
            int failureCount = 0;
            foreach (var file in csFiles)
            {
                try
                {
                    FormatSingleFile(file, forceCSharpier);
                    successCount++;
                }
                catch (Exception ex)
                {
                    LogError($"格式化失败 {file}: {ex.Message}");
                    failureCount++;
                }
            }

            LogInfo($"格式化完成！成功: {successCount}, 失败: {failureCount}");
        }

        private static string[] GetCsFiles(string path)
        {
            return Directory.GetFiles(path, "*.cs", SearchOption.AllDirectories);
        }

        private static List<SyntaxTree> ParseSyntaxTrees(string[] files)
        {
            LogInfo("开始解析 .cs 文件为语法树");
            var trees = new List<SyntaxTree>();
            foreach (var file in files)
            {
                LogDebug($"解析文件: {file}");
                var code = File.ReadAllText(file);
                var tree = CSharpSyntaxTree.ParseText(code, path: file);
                trees.Add(tree);
            }

            return trees;
        }

        private static List<MetadataReference> GetMetadataReferences()
        {
            LogInfo("加载程序集引用");
            var refs = new List<MetadataReference>
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(Assembly.Load("System.Runtime").Location),
            };
            return refs;
        }

        private static CSharpCompilation CreateCompilation(List<SyntaxTree> syntaxTrees, List<MetadataReference> references)
        {
            LogInfo("创建 Roslyn 编译对象");
            return CSharpCompilation.Create("AnalyzerTempAssembly", syntaxTrees, references, new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
        }

        private static void LogReferencedAssemblies(CSharpCompilation compilation)
        {
            LogInfo("以下是引用的程序集:");
            foreach (var reference in compilation.ReferencedAssemblyNames)
            {
                LogInfo($"  - {reference.Name}");
            }
        }

#region LoggingHelpers
        static private void LogInfo(string message) => Console.WriteLine($"[INFO] {message}");
        private static void LogWarn(string message) => Console.WriteLine($"[WARN] {message}");
        private static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
        private static void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
#endregion
    }
}