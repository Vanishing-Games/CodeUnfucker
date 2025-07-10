using System.Collections.Generic;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 文件服务接口，提供统一的文件操作功能
    /// </summary>
    public interface IFileService
    {
        /// <summary>
        /// 读取文件的所有文本内容
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件内容，如果读取失败返回null</returns>
        string? ReadAllText(string filePath);

        /// <summary>
        /// 将文本内容写入文件
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <param name="content">要写入的内容</param>
        /// <returns>是否写入成功</returns>
        bool WriteAllText(string filePath, string content);

        /// <summary>
        /// 创建文件备份
        /// </summary>
        /// <param name="filePath">原始文件路径</param>
        /// <param name="backupExtension">备份文件扩展名</param>
        /// <returns>是否创建成功</returns>
        bool CreateBackup(string filePath, string backupExtension);

        /// <summary>
        /// 检查文件是否存在
        /// </summary>
        /// <param name="filePath">文件路径</param>
        /// <returns>文件是否存在</returns>
        bool FileExists(string filePath);

        /// <summary>
        /// 检查目录是否存在
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <returns>目录是否存在</returns>
        bool DirectoryExists(string directoryPath);

        /// <summary>
        /// 获取目录中的所有C#文件
        /// </summary>
        /// <param name="directoryPath">目录路径</param>
        /// <param name="recursive">是否递归搜索子目录</param>
        /// <returns>C#文件路径数组</returns>
        string[] GetCsFiles(string directoryPath, bool recursive = true);

        /// <summary>
        /// 复制文件
        /// </summary>
        /// <param name="sourceFile">源文件路径</param>
        /// <param name="destinationFile">目标文件路径</param>
        /// <param name="overwrite">是否覆盖现有文件</param>
        /// <returns>是否复制成功</returns>
        bool CopyFile(string sourceFile, string destinationFile, bool overwrite = true);
    }
}