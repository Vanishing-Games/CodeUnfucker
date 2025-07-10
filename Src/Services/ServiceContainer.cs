using CodeUnfucker.Commands;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 简单的服务容器，用于依赖注入管理
    /// </summary>
    public class ServiceContainer
    {
        private static ServiceContainer? _instance;
        private readonly ILogger _logger;
        private readonly IFileService _fileService;
        private readonly CommandLineParser _commandLineParser;
        private readonly CommandRegistry _commandRegistry;
        private readonly ApplicationService _applicationService;

        private ServiceContainer()
        {
            // 创建服务实例
            _logger = new ConsoleLogger();
            _fileService = new FileService(_logger);
            _commandLineParser = new CommandLineParser(_logger);
            _commandRegistry = new CommandRegistry(_logger, _fileService);
            _applicationService = new ApplicationService(_logger, _fileService, _commandLineParser, _commandRegistry);
        }

        /// <summary>
        /// 获取服务容器实例（单例模式）
        /// </summary>
        public static ServiceContainer Instance
        {
            get
            {
                _instance ??= new ServiceContainer();
                return _instance;
            }
        }

        /// <summary>
        /// 获取日志服务
        /// </summary>
        public ILogger Logger => _logger;

        /// <summary>
        /// 获取文件服务
        /// </summary>
        public IFileService FileService => _fileService;

        /// <summary>
        /// 获取命令行解析器
        /// </summary>
        public CommandLineParser CommandLineParser => _commandLineParser;

        /// <summary>
        /// 获取命令注册表
        /// </summary>
        public CommandRegistry CommandRegistry => _commandRegistry;

        /// <summary>
        /// 获取应用程序服务
        /// </summary>
        public ApplicationService ApplicationService => _applicationService;

        /// <summary>
        /// 重置服务容器（主要用于测试）
        /// </summary>
        public static void Reset()
        {
            _instance = null;
        }
    }
}