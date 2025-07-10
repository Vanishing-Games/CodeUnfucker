using System;
using System.Threading.Tasks;
using CodeUnfucker.Services;

namespace CodeUnfucker
{
    /// <summary>
    /// 程序主入口点
    /// </summary>
    public class Program
    {
        /// <summary>
        /// 应用程序主入口点
        /// </summary>
        /// <param name="args">命令行参数</param>
        /// <returns>程序退出代码</returns>
        static async Task<int> Main(string[] args)
        {
            try
            {
                var serviceContainer = ServiceContainer.Instance;
                var applicationService = serviceContainer.ApplicationService;
                
                bool success = await applicationService.RunAsync(args);
                return success ? 0 : 1;
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[FATAL] 程序发生致命错误: {ex.Message}");
                Console.WriteLine($"[FATAL] 堆栈跟踪: {ex.StackTrace}");
                return 1;
            }
        }
    }
}