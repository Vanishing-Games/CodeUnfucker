using System;

namespace CodeUnfucker.Services
{
    /// <summary>
    /// 日志记录接口，提供统一的日志输出功能
    /// </summary>
    public interface ILogger
    {
        /// <summary>
        /// 记录信息级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogInfo(string message);

        /// <summary>
        /// 记录警告级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogWarn(string message);

        /// <summary>
        /// 记录错误级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogError(string message);

        /// <summary>
        /// 记录调试级别日志
        /// </summary>
        /// <param name="message">日志消息</param>
        void LogDebug(string message);

        /// <summary>
        /// 记录带异常信息的错误日志
        /// </summary>
        /// <param name="message">日志消息</param>
        /// <param name="exception">异常对象</param>
        void LogError(string message, Exception exception);
    }
}