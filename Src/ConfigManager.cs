using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.CodeAnalysis;

namespace CodeUnfucker
{
    public static class ConfigManager
    {
        private static string? _customConfigPath;
        private static string ConfigPath
        {
            get
            {
                if (!string.IsNullOrEmpty(_customConfigPath))
                    return _customConfigPath;
                // 按优先级查找配置文件
                var searchPaths = new[]
                {
                    Path.Combine(Directory.GetCurrentDirectory(), "Config"),
                    Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Config")
                };
                foreach (var path in searchPaths)
                {
                    if (Directory.Exists(path))
                        return path;
                }

                // 如果都不存在，返回当前目录下的Config
                return Path.Combine(Directory.GetCurrentDirectory(), "Config");
            }
        }

        private static FormatterConfig? _formatterConfig;
        private static AnalyzerConfig? _analyzerConfig;
        private static RoslynatorConfig? _roslynatorConfig;
        public static FormatterConfig GetFormatterConfig()
        {
            if (_formatterConfig == null)
            {
                _formatterConfig = LoadConfig<FormatterConfig>("FormatterConfig.json");
            }

            return _formatterConfig;
        }

        public static AnalyzerConfig GetAnalyzerConfig()
        {
            if (_analyzerConfig == null)
            {
                _analyzerConfig = LoadConfig<AnalyzerConfig>("AnalyzerConfig.json");
            }

            return _analyzerConfig;
        }

        public static RoslynatorConfig GetRoslynatorConfig()
        {
            if (_roslynatorConfig == null)
            {
                _roslynatorConfig = LoadConfig<RoslynatorConfig>("RoslynatorConfig.json");
            }

            return _roslynatorConfig;
        }

        private static T LoadConfig<T>(string fileName)
            where T : new()
        {
            try
            {
                string configFile = Path.Combine(ConfigPath, fileName);
                if (!File.Exists(configFile))
                {
                    Console.WriteLine($"[WARN] 配置文件不存在: {configFile}，使用默认配置");
                    return new T();
                }

                string jsonContent = File.ReadAllText(configFile);
                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                var config = JsonSerializer.Deserialize<T>(jsonContent, options);
                Console.WriteLine($"[INFO] 成功加载配置文件: {fileName}");
                return config ?? new T();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 加载配置文件失败 {fileName}: {ex.Message}");
                Console.WriteLine($"[INFO] 使用默认配置");
                return new T();
            }
        }

        public static void SetConfigPath(string configPath)
        {
            _customConfigPath = configPath;
            ReloadConfigs();
            Console.WriteLine($"[INFO] 配置路径已设置为: {configPath}");
        }

        public static void ReloadConfigs()
        {
            _formatterConfig = null;
            _analyzerConfig = null;
            _roslynatorConfig = null;
            Console.WriteLine("[INFO] 配置已重新加载");
        }
    }

    // 配置类定义
    public class FormatterConfig
    {
        public string Description { get; set; } = "CodeUnfucker 代码格式化功能配置";
        public string Version { get; set; } = "1.0.0";
        public FormatterSettings FormatterSettings { get; set; } = new();
        public MemberOrdering MemberOrdering { get; set; } = new();
        public List<string> UnityLifeCycleMethods { get; set; } = new();
        public RegionSettings RegionSettings { get; set; } = new();
    }

    public class FormatterSettings
    {
        public int MinLinesForRegion { get; set; } = 15;
        public bool EnableRegionGeneration { get; set; } = true;
        public bool CreateBackupFiles { get; set; } = true;
        public string BackupFileExtension { get; set; } = ".backup";
        public FormatterType FormatterType { get; set; } = FormatterType.Built_in;
    }

    public enum FormatterType
    {
        Built_in,
        CSharpier
    }

    public class MemberOrdering
    {
        public List<string> Order { get; set; } = new()
        {
            "Public",
            "UnityLifeCycle",
            "Protected",
            "Private",
            "NestedClasses",
            "MemberVariables",
        };
        public string Description { get; set; } = "类成员的排序规则，按此顺序重新排列";
    }

    public class RegionSettings
    {
        public string PublicRegionName { get; set; } = "Public";
        public string UnityLifeCycleRegionName { get; set; } = "Unity LifeCycle";
        public string ProtectedRegionName { get; set; } = "Protected";
        public string PrivateRegionName { get; set; } = "Private";
        public string NestedClassesRegionName { get; set; } = "Nested Classes";
        public string MemberVariablesRegionName { get; set; } = "Member Variables";
        public int IndentationSpaces { get; set; } = 8;
    }

    public class AnalyzerConfig
    {
        public string Description { get; set; } = "CodeUnfucker 代码分析功能配置";
        public string Version { get; set; } = "1.0.0";
        public AnalyzerSettings AnalyzerSettings { get; set; } = new();
        public FileFilters FileFilters { get; set; } = new();
        public OutputSettings OutputSettings { get; set; } = new();
        public StaticAnalysisRules StaticAnalysisRules { get; set; } = new();
    }

    public class AnalyzerSettings
    {
        public bool EnableSyntaxAnalysis { get; set; } = true;
        public bool EnableSemanticAnalysis { get; set; } = true;
        public bool EnableDiagnostics { get; set; } = true;
        public bool ShowReferencedAssemblies { get; set; } = true;
        public bool VerboseLogging { get; set; } = false;
    }

    public class FileFilters
    {
        public List<string> IncludePatterns { get; set; } = new()
        {
            "*.cs"
        };
        public List<string> ExcludePatterns { get; set; } = new()
        {
            "*.Designer.cs",
            "*.generated.cs",
            "**/bin/**",
            "**/obj/**",
            "**/Temp/**"
        };
        public bool SearchSubdirectories { get; set; } = true;
    }

    public class OutputSettings
    {
        public string LogLevel { get; set; } = "Info";
        public bool ShowFileCount { get; set; } = true;
        public bool ShowProcessingTime { get; set; } = true;
        public bool ShowDetailedErrors { get; set; } = true;
    }

    public class StaticAnalysisRules
    {
        public bool CheckNamingConventions { get; set; } = true;
        public bool CheckCodeComplexity { get; set; } = false;
        public bool CheckUnusedVariables { get; set; } = false;
        public bool CheckDocumentationComments { get; set; } = false;
        public int MaxComplexityThreshold { get; set; } = 10;
    }

    public class RoslynatorConfig
    {
        public string Description { get; set; } = "CodeUnfucker Roslynator 重构功能配置";
        public string Version { get; set; } = "1.0.0";
        public RefactorSettings RefactorSettings { get; set; } = new();
        public Dictionary<string, DiagnosticSeverity> SeverityOverrides { get; set; } = new();
        public List<string> ExcludedFiles { get; set; } = new();
        public RoslynatorOutputSettings OutputSettings { get; set; } = new();
    }

    public class RefactorSettings
    {
        public bool EnableCodeRefactoring { get; set; } = true;
        public bool CreateBackupFiles { get; set; } = true;
        public string BackupFileExtension { get; set; } = ".roslynator.backup";
        public DiagnosticSeverity MinimumSeverity { get; set; } = DiagnosticSeverity.Info;
        public bool ApplyAllSuggestions { get; set; } = true;
        public int MaxIterations { get; set; } = 3;
        public List<string> EnabledRules { get; set; } = new()
        {
            // 常用的 Roslynator 规则
            "RCS1036", // Remove redundant empty line
            "RCS1037", // Remove trailing white-space
            "RCS1090", // Call 'ConfigureAwait(false)'
            "RCS1124", // Inline local variable
            "RCS1129", // Remove redundant field initalization
            "RCS1138", // Add summary to documentation comment
            "RCS1157", // Composite enum value contains undefined flag
            "RCS1163", // Unused parameter
            "RCS1164", // Unused type parameter
            "RCS1169", // Make field read-only
            "RCS1170", // Use read-only auto-implemented property
            "RCS1173", // Use coalesce expression instead of if
            "RCS1179", // Unnecessary assignment
            "RCS1197", // Optimize StringBuilder.Append/AppendLine call
            "RCS1210", // Return completed task instead of returning null
        };
        public List<string> DisabledRules { get; set; } = new()
        {
            // 可能过于激进的规则
            "RCS1003", // Add braces to if-else
            "RCS1007", // Add braces
        };
    }

    public class RoslynatorOutputSettings
    {
        public bool ShowDetailedLog { get; set; } = true;
        public bool ShowAppliedRules { get; set; } = true;
        public bool ShowSkippedRules { get; set; } = false;
        public bool ShowPerformanceMetrics { get; set; } = false;
        public string LogLevel { get; set; } = "Info";
    }
}