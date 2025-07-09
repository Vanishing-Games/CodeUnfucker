using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

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
                default:
                    LogError($"未知命令: {command}");
                    LogError("支持的命令: analyze, format, csharpier");
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

            if ((command == "format" || command == "csharpier") && !File.Exists(path) && !Directory.Exists(path))
            {
                LogError($"格式化模式下，路径必须是存在的文件或目录: {path}");
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
            LogInfo("  analyze    - 分析代码");
            LogInfo("  format     - 使用内置格式化器格式化代码");
            LogInfo("  csharpier  - 使用CSharpier格式化代码");
            LogInfo("");
            LogInfo("选项:");
            LogInfo("  --config, -c  - 指定配置文件目录路径");
            LogInfo("");
            LogInfo("示例:");
            LogInfo("  CodeUnfucker analyze ./Scripts");
            LogInfo("  CodeUnfucker format ./Scripts --config ./MyConfig");
            LogInfo("  CodeUnfucker csharpier MyFile.cs");
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
                }
            }
            else
            {
                LogInfo("语法分析已禁用，跳过分析步骤");
            }
        }

        private void FormatCode(string path)
        {
            LogInfo($"开始格式化代码，扫描路径: {path}");
            FormatCodeInternal(path, false);
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