using System;
using System.Threading.Tasks;
using CodeUnfucker.Services;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// Roslynator重构命令
    /// </summary>
    public class RoslynatorCommand : BaseCommand
    {
        public override string Name => "roslynator";
        public override string Description => "使用Roslynator重构代码";

        public RoslynatorCommand(ILogger logger, IFileService fileService) 
            : base(logger, fileService)
        {
        }

        public override bool ValidateParameters(string path)
        {
            if (!FileService.FileExists(path) && !FileService.DirectoryExists(path))
            {
                Logger.LogError($"Roslynator重构模式下，路径必须是存在的文件或目录: {path}");
                return false;
            }
            return true;
        }

        public override async Task<bool> ExecuteAsync(string path)
        {
            Logger.LogInfo($"开始Roslynator重构，扫描路径: {path}");
            
            try
            {
                var refactorer = new RoslynatorRefactorer();
                refactorer.RefactorCode(path);
                
                Logger.LogInfo("Roslynator重构完成");
                await Task.CompletedTask; // 保持异步签名一致性，为未来异步操作预留
                return true;
            }
            catch (Exception ex)
            {
                Logger.LogError("Roslynator重构失败", ex);
                return false;
            }
        }

        protected override Task<bool> ProcessSingleFileAsync(string filePath)
        {
            // Roslynator refactoring is handled by the RoslynatorRefactorer class
            // This method is not used for this command type
            throw new NotSupportedException("Roslynator重构由RoslynatorRefactorer类处理");
        }
    }
}