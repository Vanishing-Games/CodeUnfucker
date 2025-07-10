using System;
using System.Threading.Tasks;
using CodeUnfucker.Services;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// 移除未使用using语句命令
    /// </summary>
    public class RemoveUsingCommand : BaseCommand
    {
        public override string Name => "rmusing";
        public override string Description => "移除未使用的using语句";

        public RemoveUsingCommand(ILogger logger, IFileService fileService) 
            : base(logger, fileService)
        {
        }

        public override bool ValidateParameters(string path)
        {
            if (!FileService.FileExists(path) && !FileService.DirectoryExists(path))
            {
                Logger.LogError($"移除using模式下，路径必须是存在的文件或目录: {path}");
                return false;
            }
            return true;
        }

        public override async Task<bool> ExecuteAsync(string path)
        {
            Logger.LogInfo($"开始移除未使用的using语句，扫描路径: {path}");
            
            try
            {
                if (FileService.FileExists(path) && IsCSharpFile(path))
                {
                    // 处理单个文件
                    bool success = await ProcessSingleFileAsync(path);
                    if (success)
                    {
                        Logger.LogInfo("移除未使用using完成");
                    }
                    return success;
                }
                else if (FileService.DirectoryExists(path))
                {
                    // 处理目录中的所有文件
                    var (successCount, failureCount) = await ProcessDirectoryAsync(path);
                    Logger.LogInfo($"移除未使用using完成！成功: {successCount}, 失败: {failureCount}");
                    return failureCount == 0;
                }
                else
                {
                    Logger.LogError($"无效的路径: {path}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError("移除未使用using失败", ex);
                return false;
            }
        }

        protected override async Task<bool> ProcessSingleFileAsync(string filePath)
        {
            try
            {
                Logger.LogInfo($"移除未使用using: {filePath}");
                
                var originalCode = FileService.ReadAllText(filePath);
                if (originalCode == null)
                {
                    return false;
                }
                
                var remover = new UsingStatementRemover();
                string processedCode = remover.RemoveUnusedUsings(originalCode, filePath);

                var config = ConfigManager.GetUsingRemoverConfig();
                
                // 根据配置决定是否创建备份
                if (config.Settings.CreateBackupFiles)
                {
                    FileService.CreateBackup(filePath, config.Settings.BackupFileExtension);
                }

                // 写入处理后的代码
                bool writeSuccess = FileService.WriteAllText(filePath, processedCode);
                if (writeSuccess)
                {
                    Logger.LogInfo($"✅ 移除未使用using完成: {filePath}");
                    await Task.CompletedTask; // 保持异步签名一致性，为未来异步操作预留
                    return true;
                }
                else
                {
                    Logger.LogError($"写入处理后的代码失败: {filePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"移除未使用using失败 {filePath}", ex);
                return false;
            }
        }
    }
}