using System;
using System.Globalization;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 命令行参数解析结果
    /// </summary>
    public class CommandLineArgs
    {
        public string Command { get; set; } = string.Empty;
        public string Path { get; set; } = string.Empty;
        public string? ConfigPath { get; set; }
        public bool ShowHelp { get; set; }
        public bool IsValid { get; set; }
    }

    /// <summary>
    /// 命令行解析器
    /// </summary>
    public class CommandLineParser
    {
        private readonly ILogger _logger;

        public CommandLineParser(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 解析命令行参数
        /// </summary>
        /// <param name="args">命令行参数数组</param>
        /// <returns>解析结果</returns>
        public CommandLineArgs Parse(string[] args)
        {
            var result = new CommandLineArgs();

            // 检查帮助选项
            if (args.Length == 1 && (args[0] == "--help" || args[0] == "-h" || args[0] == "help"))
            {
                result.ShowHelp = true;
                result.IsValid = true;
                return result;
            }

            if (args.Length == 1)
            {
                // 向后兼容：如果只有一个参数，默认为analyze命令
                result.Command = "analyze";
                result.Path = args[0];
                result.IsValid = true;
            }
            else if (args.Length == 2)
            {
                result.Command = args[0];
                result.Path = args[1];
                result.IsValid = true;
            }
            else if (args.Length == 4 && (args[2] == "--config" || args[2] == "-c"))
            {
                result.Command = args[0];
                result.Path = args[1];
                result.ConfigPath = args[3];
                result.IsValid = true;
            }
            else
            {
                result.IsValid = false;
            }

            return result;
        }

        /// <summary>
        /// 显示使用说明
        /// </summary>
        public void ShowUsage()
        {
            _logger.LogInfo("用法:");
            _logger.LogInfo("  CodeUnfucker <command> <path> [--config <config-path>]");
            _logger.LogInfo("");
            _logger.LogInfo("命令:");
            _logger.LogInfo("  analyze   - 分析代码");
            _logger.LogInfo("  format    - 使用内置格式化器格式化代码");
            _logger.LogInfo("  csharpier - 使用CSharpier格式化代码");
            _logger.LogInfo("  rmusing   - 移除未使用的using语句");
            _logger.LogInfo("  roslynator - 使用Roslynator重构代码");
            _logger.LogInfo("");
            _logger.LogInfo("选项:");
            _logger.LogInfo("  --config, -c  - 指定配置文件目录路径");
            _logger.LogInfo("");
            _logger.LogInfo("示例:");
            _logger.LogInfo("  CodeUnfucker analyze ./Scripts");
            _logger.LogInfo("  CodeUnfucker format ./Scripts --config ./MyConfig");
            _logger.LogInfo("  CodeUnfucker csharpier MyFile.cs");
            _logger.LogInfo("  CodeUnfucker rmusing ./Scripts");
            _logger.LogInfo("  CodeUnfucker roslynator ./Scripts");
        }
    }
}