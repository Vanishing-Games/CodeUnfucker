using System;
using System.IO;
using System.Reflection;
using System.Text;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

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
            
            var startTime = DateTime.Now;
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

            int totalIssues = 0;
            var diagnosticCount = new Dictionary<string, int>();

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

                    // 获取编译诊断信息
                    if (config.AnalyzerSettings.EnableDiagnostics)
                    {
                        var diagnostics = compilation.GetDiagnostics();
                        totalIssues += ProcessDiagnostics(diagnostics, diagnosticCount, config);
                    }

                    // 执行静态分析规则
                    if (config.StaticAnalysisRules.CheckNamingConventions ||
                        config.StaticAnalysisRules.CheckCodeComplexity ||
                        config.StaticAnalysisRules.CheckUnusedVariables ||
                        config.StaticAnalysisRules.CheckDocumentationComments)
                    {
                        totalIssues += PerformStaticAnalysis(compilation, config);
                    }
                }
                else
                {
                    // 只进行语法分析
                    totalIssues += PerformSyntaxAnalysis(syntaxTrees, config);
                }
            }
            else
            {
                LogInfo("语法分析已禁用，跳过分析步骤");
            }

            // 输出分析结果摘要
            LogInfo("");
            LogInfo("=== 分析结果摘要 ===");
            LogInfo($"分析文件数: {csFiles.Length}");
            LogInfo($"发现问题数: {totalIssues}");
            
            if (diagnosticCount.Any())
            {
                LogInfo("问题分类:");
                foreach (var kvp in diagnosticCount.OrderByDescending(x => x.Value))
                {
                    LogInfo($"  {kvp.Key}: {kvp.Value}");
                }
            }

            if (config.OutputSettings.ShowProcessingTime)
            {
                var elapsed = DateTime.Now - startTime;
                LogInfo($"分析耗时: {elapsed.TotalMilliseconds:F0} ms");
            }

            LogInfo("===================");
        }

        private int ProcessDiagnostics(IEnumerable<Diagnostic> diagnostics, Dictionary<string, int> diagnosticCount, AnalyzerConfig config)
        {
            int count = 0;
            var diagnosticsByFile = diagnostics.GroupBy(d => d.Location.SourceTree?.FilePath ?? "Unknown");

            foreach (var fileGroup in diagnosticsByFile)
            {
                var fileName = Path.GetFileName(fileGroup.Key);
                var fileDiagnostics = fileGroup.ToList();
                
                if (fileDiagnostics.Any())
                {
                    LogInfo($"\n文件: {fileName}");
                }

                foreach (var diagnostic in fileDiagnostics)
                {
                    count++;
                    string category = GetDiagnosticCategory(diagnostic.Severity);
                    diagnosticCount[category] = diagnosticCount.GetValueOrDefault(category, 0) + 1;

                    var location = diagnostic.Location.GetLineSpan();
                    var message = $"  [{diagnostic.Severity}] {diagnostic.Id}: {diagnostic.GetMessage()}";
                    
                    if (location.IsValid)
                    {
                        message += $" (行 {location.StartLinePosition.Line + 1})";
                    }

                    if (diagnostic.Severity == DiagnosticSeverity.Error)
                    {
                        LogError(message);
                    }
                    else if (diagnostic.Severity == DiagnosticSeverity.Warning)
                    {
                        LogWarn(message);
                    }
                    else
                    {
                        LogInfo(message);
                    }

                    if (config.OutputSettings.ShowDetailedErrors && !string.IsNullOrEmpty(diagnostic.Descriptor.HelpLinkUri))
                    {
                        LogInfo($"    详细信息: {diagnostic.Descriptor.HelpLinkUri}");
                    }
                }
            }

            return count;
        }

        private string GetDiagnosticCategory(DiagnosticSeverity severity)
        {
            return severity switch
            {
                DiagnosticSeverity.Error => "错误",
                DiagnosticSeverity.Warning => "警告",
                DiagnosticSeverity.Info => "信息",
                DiagnosticSeverity.Hidden => "隐藏",
                _ => "其他"
            };
        }

        private int PerformSyntaxAnalysis(List<SyntaxTree> syntaxTrees, AnalyzerConfig config)
        {
            int issueCount = 0;
            LogInfo("\n=== 语法分析 ===");

            foreach (var tree in syntaxTrees)
            {
                var root = tree.GetRoot();
                var fileName = Path.GetFileName(tree.FilePath);

                if (config.StaticAnalysisRules.CheckNamingConventions)
                {
                    issueCount += CheckNamingConventions(root, fileName);
                }

                if (config.StaticAnalysisRules.CheckCodeComplexity)
                {
                    issueCount += CheckCodeComplexity(root, fileName, config.StaticAnalysisRules.MaxComplexityThreshold);
                }
            }

            return issueCount;
        }

        private int PerformStaticAnalysis(CSharpCompilation compilation, AnalyzerConfig config)
        {
            int issueCount = 0;
            LogInfo("\n=== 静态分析 ===");

            foreach (var tree in compilation.SyntaxTrees)
            {
                var root = tree.GetRoot();
                var semanticModel = compilation.GetSemanticModel(tree);
                var fileName = Path.GetFileName(tree.FilePath);

                if (config.StaticAnalysisRules.CheckNamingConventions)
                {
                    issueCount += CheckNamingConventions(root, fileName);
                }

                if (config.StaticAnalysisRules.CheckCodeComplexity)
                {
                    issueCount += CheckCodeComplexity(root, fileName, config.StaticAnalysisRules.MaxComplexityThreshold);
                }

                if (config.StaticAnalysisRules.CheckUnusedVariables)
                {
                    issueCount += CheckUnusedVariables(root, semanticModel, fileName);
                }

                if (config.StaticAnalysisRules.CheckDocumentationComments)
                {
                    issueCount += CheckDocumentationComments(root, fileName);
                }
            }

            return issueCount;
        }

        private int CheckNamingConventions(SyntaxNode root, string fileName)
        {
            int issueCount = 0;
            var classes = root.DescendantNodes().OfType<ClassDeclarationSyntax>();
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            var fields = root.DescendantNodes().OfType<FieldDeclarationSyntax>();
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();

            // 检查类名命名约定（应该使用PascalCase）
            foreach (var classDecl in classes)
            {
                var className = classDecl.Identifier.ValueText;
                if (!IsPascalCase(className))
                {
                    LogWarn($"  [命名约定] 类名 '{className}' 应使用 PascalCase 命名 ({fileName})");
                    issueCount++;
                }
            }

            // 检查方法命名约定（应该使用PascalCase）
            foreach (var method in methods)
            {
                var methodName = method.Identifier.ValueText;
                if (!IsPascalCase(methodName) && !IsUnityLifecycleMethod(methodName))
                {
                    LogWarn($"  [命名约定] 方法名 '{methodName}' 应使用 PascalCase 命名 ({fileName})");
                    issueCount++;
                }
            }

            // 检查字段命名约定（私有字段应使用camelCase或_camelCase）
            foreach (var field in fields)
            {
                foreach (var variable in field.Declaration.Variables)
                {
                    var fieldName = variable.Identifier.ValueText;
                    var isPrivate = field.Modifiers.Any(m => m.IsKind(SyntaxKind.PrivateKeyword)) || 
                                   !field.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || 
                                                             m.IsKind(SyntaxKind.ProtectedKeyword) || 
                                                             m.IsKind(SyntaxKind.InternalKeyword));
                    
                    if (isPrivate && !IsCamelCase(fieldName) && !fieldName.StartsWith("_"))
                    {
                        LogWarn($"  [命名约定] 私有字段 '{fieldName}' 应使用 camelCase 或 _camelCase 命名 ({fileName})");
                        issueCount++;
                    }
                    else if (!isPrivate && !IsPascalCase(fieldName))
                    {
                        LogWarn($"  [命名约定] 公有字段 '{fieldName}' 应使用 PascalCase 命名 ({fileName})");
                        issueCount++;
                    }
                }
            }

            // 检查属性命名约定（应该使用PascalCase）
            foreach (var property in properties)
            {
                var propertyName = property.Identifier.ValueText;
                if (!IsPascalCase(propertyName))
                {
                    LogWarn($"  [命名约定] 属性名 '{propertyName}' 应使用 PascalCase 命名 ({fileName})");
                    issueCount++;
                }
            }

            return issueCount;
        }

        private int CheckCodeComplexity(SyntaxNode root, string fileName, int threshold)
        {
            int issueCount = 0;
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();

            foreach (var method in methods)
            {
                var complexity = CalculateCyclomaticComplexity(method);
                if (complexity > threshold)
                {
                    var methodName = method.Identifier.ValueText;
                    LogWarn($"  [复杂度] 方法 '{methodName}' 的圈复杂度为 {complexity}，超过阈值 {threshold} ({fileName})");
                    issueCount++;
                }
            }

            return issueCount;
        }

        private int CheckUnusedVariables(SyntaxNode root, SemanticModel semanticModel, string fileName)
        {
            int issueCount = 0;
            var variables = root.DescendantNodes().OfType<VariableDeclaratorSyntax>();

            foreach (var variable in variables)
            {
                var symbol = semanticModel.GetDeclaredSymbol(variable);
                if (symbol is IFieldSymbol fieldSymbol)
                {
                    var references = root.DescendantNodes()
                        .OfType<IdentifierNameSyntax>()
                        .Where(id => id.Identifier.ValueText == fieldSymbol.Name)
                        .Count();

                    // 如果只有一个引用（即声明本身），则认为未使用
                    if (references <= 1)
                    {
                        LogWarn($"  [未使用变量] 字段 '{fieldSymbol.Name}' 声明后从未使用 ({fileName})");
                        issueCount++;
                    }
                }
            }

            return issueCount;
        }

        private int CheckDocumentationComments(SyntaxNode root, string fileName)
        {
            int issueCount = 0;
            var publicMembers = root.DescendantNodes()
                .Where(n => n is ClassDeclarationSyntax || n is MethodDeclarationSyntax || n is PropertyDeclarationSyntax)
                .Where(n => HasPublicModifier(n));

            foreach (var member in publicMembers)
            {
                if (!HasDocumentationComment(member))
                {
                    var memberName = GetMemberName(member);
                    var memberType = GetMemberType(member);
                    LogWarn($"  [文档注释] {memberType} '{memberName}' 缺少 XML 文档注释 ({fileName})");
                    issueCount++;
                }
            }

            return issueCount;
        }

        // 辅助方法
        private bool IsPascalCase(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsUpper(name[0]) && !name.Contains('_');
        }

        private bool IsCamelCase(string name)
        {
            return !string.IsNullOrEmpty(name) && char.IsLower(name[0]) && !name.Contains('_');
        }

        private bool IsUnityLifecycleMethod(string methodName)
        {
            var unityMethods = new[] { "Awake", "Start", "Update", "FixedUpdate", "LateUpdate", 
                                     "OnEnable", "OnDisable", "OnDestroy", "OnGUI", "OnValidate", "Reset" };
            return unityMethods.Contains(methodName);
        }

        private int CalculateCyclomaticComplexity(MethodDeclarationSyntax method)
        {
            int complexity = 1; // 基础复杂度
            var controlFlowNodes = method.DescendantNodes().Where(n =>
                n.IsKind(SyntaxKind.IfStatement) ||
                n.IsKind(SyntaxKind.ElseClause) ||
                n.IsKind(SyntaxKind.WhileStatement) ||
                n.IsKind(SyntaxKind.ForStatement) ||
                n.IsKind(SyntaxKind.ForEachStatement) ||
                n.IsKind(SyntaxKind.DoStatement) ||
                n.IsKind(SyntaxKind.SwitchStatement) ||
                n.IsKind(SyntaxKind.CaseSwitchLabel) ||
                n.IsKind(SyntaxKind.CatchClause) ||
                n.IsKind(SyntaxKind.ConditionalExpression));

            complexity += controlFlowNodes.Count();
            return complexity;
        }

        private bool HasPublicModifier(SyntaxNode node)
        {
            return node switch
            {
                ClassDeclarationSyntax classDecl => classDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)),
                MethodDeclarationSyntax methodDecl => methodDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)),
                PropertyDeclarationSyntax propDecl => propDecl.Modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword)),
                _ => false
            };
        }

        private bool HasDocumentationComment(SyntaxNode node)
        {
            return node.GetLeadingTrivia().Any(t => t.IsKind(SyntaxKind.SingleLineDocumentationCommentTrivia) ||
                                                   t.IsKind(SyntaxKind.MultiLineDocumentationCommentTrivia));
        }

        private string GetMemberName(SyntaxNode node)
        {
            return node switch
            {
                ClassDeclarationSyntax classDecl => classDecl.Identifier.ValueText,
                MethodDeclarationSyntax methodDecl => methodDecl.Identifier.ValueText,
                PropertyDeclarationSyntax propDecl => propDecl.Identifier.ValueText,
                _ => "Unknown"
            };
        }

        private string GetMemberType(SyntaxNode node)
        {
            return node switch
            {
                ClassDeclarationSyntax => "类",
                MethodDeclarationSyntax => "方法",
                PropertyDeclarationSyntax => "属性",
                _ => "成员"
            };
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