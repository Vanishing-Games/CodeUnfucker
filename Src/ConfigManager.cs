using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeUnfucker
{
    public static class ConfigManager
    {
        private static string? _customConfigPath;
        private static string ConfigPath
        {
            get
            {
                // 如果显式设置了自定义配置路径，就使用它，即使路径不存在
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
        private static UsingRemoverConfig? _usingRemoverConfig;
        
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

        public static UsingRemoverConfig GetUsingRemoverConfig()
        {
            if (_usingRemoverConfig == null)
            {
                _usingRemoverConfig = LoadConfig<UsingRemoverConfig>("UsingRemoverConfig.json");
            }

            return _usingRemoverConfig;
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

                string jsonContent;
                try
                {
                    jsonContent = File.ReadAllText(configFile);
                }
                catch (IOException ex)
                {
                    Console.WriteLine($"[ERROR] 无法读取配置文件 {configFile}: {ex.Message}");
                    Console.WriteLine($"[INFO] 使用默认配置");
                    return new T();
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine($"[ERROR] 没有访问配置文件的权限 {configFile}: {ex.Message}");
                    Console.WriteLine($"[INFO] 使用默认配置");
                    return new T();
                }

                var options = new JsonSerializerOptions
                {
                    PropertyNameCaseInsensitive = true,
                    PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                    ReadCommentHandling = JsonCommentHandling.Skip,
                    AllowTrailingCommas = true,
                    Converters = { new JsonStringEnumConverter() }
                };
                
                T? config;
                try
                {
                    config = JsonSerializer.Deserialize<T>(jsonContent, options);
                }
                catch (JsonException ex)
                {
                    Console.WriteLine($"[ERROR] 配置文件格式错误 {fileName}: {ex.Message}");
                    Console.WriteLine($"[INFO] 使用默认配置");
                    return new T();
                }
                
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
            _usingRemoverConfig = null;
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
        public bool EnablePureMethodAnalysis { get; set; } = true;
        public bool EnableUnityHeapAllocationAnalysis { get; set; } = true;
    }
}