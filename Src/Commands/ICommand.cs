using System.Threading.Tasks;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// 命令接口，定义所有命令的统一行为
    /// </summary>
    public interface ICommand
    {
        /// <summary>
        /// 命令名称
        /// </summary>
        string Name { get; }

        /// <summary>
        /// 命令描述
        /// </summary>
        string Description { get; }

        /// <summary>
        /// 执行命令
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <returns>执行是否成功</returns>
        Task<bool> ExecuteAsync(string path);

        /// <summary>
        /// 验证命令参数
        /// </summary>
        /// <param name="path">目标路径</param>
        /// <returns>验证是否通过</returns>
        bool ValidateParameters(string path);
    }
}