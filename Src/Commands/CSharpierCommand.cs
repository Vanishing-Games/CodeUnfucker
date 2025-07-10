using System;
using System.Threading.Tasks;
using CodeUnfucker.Services;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// CSharpier格式化命令
    /// </summary>
    public class CSharpierCommand : BaseCommand
    {
        public override string Name => "csharpier";
        public override string Description => "使用CSharpier格式化代码";

        public CSharpierCommand(ILogger logger, IFileService fileService) 
            : base(logger, fileService)
        {
        }

        public override bool ValidateParameters(string path)
        {
            if (!FileService.FileExists(path) && !FileService.DirectoryExists(path))
            {
                Logger.LogError($"格式化模式下，路径必须是存在的文件或目录: {path}");
                return false;
            }
            return true;
        }

        public override async Task<bool> ExecuteAsync(string path)
        {
            Logger.LogInfo($"开始使用CSharpier格式化代码，扫描路径: {path}");
            
            try
            {
                if (FileService.FileExists(path) && IsCSharpFile(path))
                {
                    // 格式化单个文件
                    bool success = await ProcessSingleFileAsync(path);
                    if (success)
                    {
                        Logger.LogInfo("CSharpier格式化完成");
                    }
                    return success;
                }
                else if (FileService.DirectoryExists(path))
                {
                    // 格式化目录中的所有文件
                    var (successCount, failureCount) = await ProcessDirectoryAsync(path);
                    Logger.LogInfo($"CSharpier格式化完成！成功: {successCount}, 失败: {failureCount}");
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
                Logger.LogError("CSharpier格式化失败", ex);
                return false;
            }
        }

        protected override async Task<bool> ProcessSingleFileAsync(string filePath)
        {
            try
            {
                Logger.LogInfo($"使用CSharpier格式化文件: {filePath}");
                
                var originalCode = FileService.ReadAllText(filePath);
                if (originalCode == null)
                {
                    return false;
                }
                
                var formatter = new CSharpierFormatter();
                string formattedCode = formatter.FormatCode(originalCode, filePath);

                var config = ConfigManager.GetFormatterConfig();
                
                // 根据配置决定是否创建备份
                if (config.Settings.CreateBackupFiles)
                {
                    FileService.CreateBackup(filePath, config.Settings.BackupFileExtension);
                }

                // 写入格式化后的代码
                bool writeSuccess = FileService.WriteAllText(filePath, formattedCode);
                if (writeSuccess)
                {
                    Logger.LogInfo($"✅ CSharpier格式化完成: {filePath}");
                    return true;
                }
                else
                {
                    Logger.LogError($"写入CSharpier格式化后的代码失败: {filePath}");
                    return false;
                }
            }
            catch (Exception ex)
            {
                Logger.LogError($"CSharpier格式化文件失败 {filePath}", ex);
                return false;
            }
        }
    }
}