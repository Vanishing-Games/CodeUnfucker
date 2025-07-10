using System;
using System.IO;
using System.Linq;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 文件服务实现类
    /// </summary>
    public class FileService : IFileService
    {
        private readonly ILogger _logger;

        public FileService(ILogger logger)
        {
            _logger = logger;
        }

        /// <summary>
        /// 读取文件的所有文本内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容，如果读取失败返回null</returns>
        public string? ReadAllText(string filePath)
        {
            try
            {
                return File.ReadAllText(filePath);
            }
            catch (IOException ex)
            {
                _logger.LogError($"无法读取文件 {filePath}", ex);
                return null;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"没有访问文件的权限 {filePath}", ex);
                return null;
            }
            catch (Exception ex)
            {
                _logger.LogError($"读取文件时发生未预期的错误 {filePath}", ex);
                return null;
            }
        }

        /// <summary>
        /// 将文本内容写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">要写入的内容</param>
        /// <returns>是否写入成功</returns>
        public bool WriteAllText(string filePath, string content)
        {
            try
            {
                File.WriteAllText(filePath, content);
                return true;
            }
            catch (IOException ex)
            {
                _logger.LogError($"无法写入文件 {filePath}", ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"没有写入文件的权限 {filePath}", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"写入文件时发生未预期的错误 {filePath}", ex);
                return false;
            }
        }

        /// <summary>
        /// 创建文件备份
        /// </summary>
        /// <param name="filePath">原始文件路径</param>
        /// <param name="backupExtension">备份文件扩展名</param>
        /// <returns>是否创建成功</returns>
        public bool CreateBackup(string filePath, string backupExtension)
        {
            try
            {
                string backupPath = filePath + backupExtension;
                File.Copy(filePath, backupPath, true);
                _logger.LogInfo($"已创建备份: {backupPath}");
                return true;
            }
            catch (IOException ex)
            {
                _logger.LogWarn($"无法创建备份文件: {ex.Message}");
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"创建备份文件时发生未预期的错误", ex);
                return false;
            }
        }

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件是否存在</returns>
        public bool FileExists(string filePath)
        {
            return File.Exists(filePath);
        }

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>目录是否存在</returns>
        public bool DirectoryExists(string directoryPath)
        {
            return Directory.Exists(directoryPath);
        }

        /// <summary>
        /// 获取目录中的所有C#文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="recursive">是否递归搜索子目录</param>
        /// <returns>C#文件路径数组</returns>
        public string[] GetCsFiles(string directoryPath, bool recursive = true)
        {
            try
            {
                var searchOption = recursive ? SearchOption.AllDirectories : SearchOption.TopDirectoryOnly;
                return Directory.GetFiles(directoryPath, "*.cs", searchOption);
            }
            catch (DirectoryNotFoundException ex)
            {
                _logger.LogError($"目录不存在: {directoryPath}", ex);
                return Array.Empty<string>();
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"没有访问目录的权限: {directoryPath}", ex);
                return Array.Empty<string>();
            }
            catch (Exception ex)
            {
                _logger.LogError($"搜索C#文件时发生未预期的错误: {directoryPath}", ex);
                return Array.Empty<string>();
            }
        }

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="destinationFile">目标文件路径</param>
        /// <param name="overwrite">是否覆盖现有文件</param>
        /// <returns>是否复制成功</returns>
        public bool CopyFile(string sourceFile, string destinationFile, bool overwrite = true)
        {
            try
            {
                File.Copy(sourceFile, destinationFile, overwrite);
                return true;
            }
            catch (IOException ex)
            {
                _logger.LogError($"无法复制文件从 {sourceFile} 到 {destinationFile}", ex);
                return false;
            }
            catch (UnauthorizedAccessException ex)
            {
                _logger.LogError($"没有权限复制文件从 {sourceFile} 到 {destinationFile}", ex);
                return false;
            }
            catch (Exception ex)
            {
                _logger.LogError($"复制文件时发生未预期的错误", ex);
                return false;
            }
        }
    }
}