using System.Threading.Tasks;
using CodeUnfucker.Services;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// 命令基类，提供通用功能
    /// </summary>
    public abstract class BaseCommand : ICommand
    {
        protected readonly ILogger Logger;
        protected readonly IFileService FileService;

        protected BaseCommand(ILogger logger, IFileService fileService)
        {
            Logger = logger;
            FileService = fileService;
        }

        /// <summary>
        /// 命令名称
        /// </summary>
        public abstract string Name { get; }

        /// <summary>
        /// 命令描述
        /// </summary>
        public abstract string Description { get; }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <returns>执行是否成功</returns>
        public abstract Task<bool> ExecuteAsync(string path);

        /// <summary>
        /// 验证命令参数
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <returns>验证是否通过</returns>
        public abstract bool ValidateParameters(string path);

        /// <summary>
        /// 检查路径是否为C#文件
        /// </summary>
        /// <param name="path">文件路径</param>
        /// <returns>是否为C#文件</returns>
        protected bool IsCSharpFile(string path)
        {
            return path.EndsWith(".cs", System.StringComparison.OrdinalIgnoreCase);
        }

        /// <summary>
        /// 处理单个文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>处理是否成功</returns>
        protected abstract Task<bool> ProcessSingleFileAsync(string filePath);

        /// <summary>
        /// 处理目录中的所有C#文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>处理统计信息</returns>
        protected async Task<(int successCount, int failureCount)> ProcessDirectoryAsync(string directoryPath)
        {
            var csFiles = FileService.GetCsFiles(directoryPath);
            if (csFiles.Length == 0)
            {
                Logger.LogWarn("未找到任何 .cs 文件");
                return (0, 0);
            }

            Logger.LogInfo($"找到 {csFiles.Length} 个 .cs 文件，开始批量处理...");
            int successCount = 0;
            int failureCount = 0;

            foreach (var file in csFiles)
            {
                try
                {
                    bool success = await ProcessSingleFileAsync(file);
                    if (success)
                    {
                        successCount++;
                    }
                    else
                    {
                        failureCount++;
                    }
                }
                catch (System.Exception ex)
                {
                    Logger.LogError($"处理失败 {file}", ex);
                    failureCount++;
                }
            }

            return (successCount, failureCount);
        }
    }
}