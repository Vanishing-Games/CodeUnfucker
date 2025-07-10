using System;
using System.Collections.Generic;
using System.Linq;
using CodeUnfucker.Services;

namespace CodeUnfucker.Commands
{
    /// <summary>
    /// 命令注册表，管理所有可用的命令
    /// </summary>
    public class CommandRegistry
    {
        private readonly Dictionary<string, ICommand> _commands;

        public CommandRegistry(ILogger logger, IFileService fileService)
        {
            _commands = new Dictionary<string, ICommand>(StringComparer.OrdinalIgnoreCase)
            {
                { "analyze", new AnalyzeCommand(logger, fileService) },
                { "format", new FormatCommand(logger, fileService) },
                { "csharpier", new CSharpierCommand(logger, fileService) },
                { "rmusing", new RemoveUsingCommand(logger, fileService) },
                { "roslynator", new RoslynatorCommand(logger, fileService) }
            };
        }

        /// <summary>
        /// 获取指定名称的命令
        /// </summary>
        /// <param name="commandName">命令名称</param>
        /// <returns>命令实例，如果不存在返回null</returns>
        public ICommand? GetCommand(string commandName)
        {
            _commands.TryGetValue(commandName, out var command);
            return command;
        }

        /// <summary>
        /// 获取所有可用的命令
        /// </summary>
        /// <returns>所有命令的集合</returns>
        public IEnumerable<ICommand> GetAllCommands()
        {
            return _commands.Values;
        }

        /// <summary>
        /// 检查命令是否存在
        /// </summary>
        /// <param name="commandName">命令名称</param>
        /// <returns>命令是否存在</returns>
        public bool CommandExists(string commandName)
        {
            return _commands.ContainsKey(commandName);
        }

        /// <summary>
        /// 获取所有支持的命令名称
        /// </summary>
        /// <returns>命令名称列表</returns>
        public string[] GetSupportedCommands()
        {
            return _commands.Keys.ToArray();
        }
    }
}