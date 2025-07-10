using System;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 控制台日志记录器实现
    /// </summary>
    public class ConsoleLogger : ILogger
    {
        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogInfo(string message)
        {
            Console.WriteLine($"[INFO] {message}");
        }

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogWarn(string message)
        {
            Console.WriteLine($"[WARN] {message}");
        }

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogError(string message)
        {
            Console.WriteLine($"[ERROR] {message}");
        }

        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        public void LogDebug(string message)
        {
            Console.WriteLine($"[DEBUG] {message}");
        }

        /// <summary>
        /// 记录带异常信息的错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常对象</param>
        public void LogError(string message, Exception exception)
        {
            Console.WriteLine($"[ERROR] {message}: {exception.Message}");
            if (exception.StackTrace != null)
            {
                Console.WriteLine($"[ERROR] StackTrace: {exception.StackTrace}");
            }
        }
    }
}