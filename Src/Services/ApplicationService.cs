using System;
using System.IO;
using System.Threading.Tasks;
using CodeUnfucker.Commands;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 应用程序主服务，协调所有操作
    /// </summary>
    public class ApplicationService
    {
        private readonly ILogger _logger;
        private readonly IFileService _fileService;
        private readonly CommandLineParser _commandLineParser;
        private readonly CommandRegistry _commandRegistry;

        public ApplicationService(
            ILogger logger, 
            IFileService fileService, 
            CommandLineParser commandLineParser, 
            CommandRegistry commandRegistry)
        {
            _logger = logger;
            _fileService = fileService;
            _commandLineParser = commandLineParser;
            _commandRegistry = commandRegistry;
        }

        /// <summary>
        /// 运行应用程序
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>是否成功执行</returns>
        public async Task<bool> RunAsync(string[] args)
        {
            _logger.LogInfo("CodeUnfucker 启动");

            try
            {
                var parsedArgs = _commandLineParser.Parse(args);

                if (parsedArgs.ShowHelp)
                {
                    _commandLineParser.ShowUsage();
                    return true;
                }

                if (!parsedArgs.IsValid)
                {
                    _logger.LogError("无效的命令行参数");
                    _commandLineParser.ShowUsage();
                    return false;
                }

                // 如果指定了配置路径，设置它
                if (!string.IsNullOrEmpty(parsedArgs.ConfigPath))
                {
                    SetupConfig(parsedArgs.ConfigPath);
                }

                // 获取并执行命令
                var command = _commandRegistry.GetCommand(parsedArgs.Command);
                if (command == null)
                {
                    _logger.LogError($"未知命令: {parsedArgs.Command}");
                    _logger.LogError($"支持的命令: {string.Join(", ", _commandRegistry.GetSupportedCommands())}");
                    _commandLineParser.ShowUsage();
                    return false;
                }

                // 验证命令参数
                if (!command.ValidateParameters(parsedArgs.Path))
                {
                    return false;
                }

                // 执行命令
                _logger.LogInfo($"执行命令: {command.Name} - {command.Description}");
                bool success = await command.ExecuteAsync(parsedArgs.Path);

                if (success)
                {
                    _logger.LogInfo("命令执行成功");
                }
                else
                {
                    _logger.LogError("命令执行失败");
                }

                return success;
            }
            catch (Exception ex)
            {
                _logger.LogError("应用程序运行时发生未预期的错误", ex);
                return false;
            }
            finally
            {
                _logger.LogInfo("CodeUnfucker 运行结束");
            }
        }

        /// <summary>
        /// 设置配置路径
        /// </summary>
        /// <param name="configPath">配置文件路径</param>
        private void SetupConfig(string configPath)
        {
            if (!string.IsNullOrEmpty(configPath))
            {
                if (_fileService.DirectoryExists(configPath))
                {
                    ConfigManager.SetConfigPath(configPath);
                    _logger.LogInfo($"使用配置路径: {configPath}");
                }
                else
                {
                    _logger.LogError($"指定的配置路径不存在: {configPath}");
                    _logger.LogInfo("使用默认配置");
                }
            }
        }
    }
}