using System;
using System.Collections.Generic;
using System.Collections.Immutable;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Diagnostics;
using Microsoft.CodeAnalysis.Text;

namespace CodeUnfucker
{
    public class RoslynatorRefactorer
    {
        private readonly RoslynatorConfig _config;

        public RoslynatorRefactorer()
        {
            _config = ConfigManager.GetRoslynatorConfig();
        }

        public async Task<string> RefactorCodeAsync(string code, string filePath)
        {
            try
            {
                LogInfo($"开始使用 Roslynator 重构代码: {filePath}");
                
                // 创建语法树
                var syntaxTree = CSharpSyntaxTree.ParseText(code, path: filePath);
                
                // 应用简单的代码重构规则
                var refactoredCode = ApplySimpleRefactorings(code, syntaxTree);
                
                if (refactoredCode != code)
                {
                    LogInfo("✅ Roslynator 重构完成");
                    return refactoredCode;
                }
                else
                {
                    LogInfo("未发现需要重构的问题");
                    return code;
                }
            }
            catch (Exception ex)
            {
                LogError($"Roslynator 重构失败: {ex.Message}");
                return code; // 返回原始代码
            }
        }

        private string ApplySimpleRefactorings(string originalCode, SyntaxTree syntaxTree)
        {
            var code = originalCode;
            
            LogInfo("应用简单重构规则...");
            
            // RCS1036: Remove redundant empty line
            code = RemoveRedundantEmptyLines(code);
            
            // RCS1037: Remove trailing white-space
            code = RemoveTrailingWhitespace(code);
            
            // RCS1173: Use coalesce expression instead of if
            code = UseCoalesceExpression(code);
            
            // RCS1073: Convert if to return statement
            code = ConvertIfToReturnStatement(code);
            
            // RCS1156: Use string.Length instead of comparison with empty string
            code = UseStringLengthInsteadOfEmpty(code);
            
            // RCS1049: Simplify boolean comparison
            code = SimplifyBooleanComparison(code);
            
            LogInfo("✅ 简单重构规则应用完成");
            return code;
        }
        
        private string RemoveRedundantEmptyLines(string code)
        {
            var lines = code.Split('\n');
            var result = new List<string>();
            bool lastLineWasEmpty = false;
            
            foreach (var line in lines)
            {
                var trimmedLine = line.Trim();
                if (string.IsNullOrEmpty(trimmedLine))
                {
                    if (!lastLineWasEmpty)
                    {
                        result.Add(line);
                        lastLineWasEmpty = true;
                    }
                }
                else
                {
                    result.Add(line);
                    lastLineWasEmpty = false;
                }
            }
            
            return string.Join('\n', result);
        }
        
        private string RemoveTrailingWhitespace(string code)
        {
            var lines = code.Split('\n');
            var result = lines.Select(line => line.TrimEnd()).ToArray();
            return string.Join('\n', result);
        }
        
        private string UseCoalesceExpression(string code)
        {
            // if (value != null) { return value; } else { return defaultValue; }
            // => return value ?? defaultValue;
            
            // 简单的正则表达式替换，实际应用中应该使用语法树分析
            var patterns = new[]
            {
                (@"\s*if\s*\(\s*(\w+)\s*!=\s*null\s*\)\s*\{\s*return\s+\1\s*;\s*\}\s*else\s*\{\s*return\s+([^;]+);\s*\}", "return $1 ?? $2;"),
                (@"\s*if\s*\(\s*(\w+)\s*==\s*null\s*\)\s*\{\s*return\s+([^;]+);\s*\}\s*else\s*\{\s*return\s+\1\s*;\s*\}", "return $1 ?? $2;")
            };
            
            foreach (var (pattern, replacement) in patterns)
            {
                code = System.Text.RegularExpressions.Regex.Replace(code, pattern, replacement, 
                    System.Text.RegularExpressions.RegexOptions.Multiline);
            }
            
            return code;
        }
        
        private string ConvertIfToReturnStatement(string code)
        {
            // if (condition) { return true; } else { return false; }
            // => return condition;
            
            var patterns = new[]
            {
                (@"\s*if\s*\(([^)]+)\)\s*\{\s*return\s+true\s*;\s*\}\s*else\s*\{\s*return\s+false\s*;\s*\}", "return $1;"),
                (@"\s*if\s*\(([^)]+)\)\s*\{\s*return\s+false\s*;\s*\}\s*else\s*\{\s*return\s+true\s*;\s*\}", "return !($1);")
            };
            
            foreach (var (pattern, replacement) in patterns)
            {
                code = System.Text.RegularExpressions.Regex.Replace(code, pattern, replacement, 
                    System.Text.RegularExpressions.RegexOptions.Multiline);
            }
            
            return code;
        }
        
        private string UseStringLengthInsteadOfEmpty(string code)
        {
            // str == string.Empty => str.Length == 0
            // str != string.Empty => str.Length != 0
            
            var patterns = new[]
            {
                (@"(\w+)\s*==\s*string\.Empty", "$1.Length == 0"),
                (@"(\w+)\s*!=\s*string\.Empty", "$1.Length != 0"),
                (@"string\.Empty\s*==\s*(\w+)", "$1.Length == 0"),
                (@"string\.Empty\s*!=\s*(\w+)", "$1.Length != 0")
            };
            
            foreach (var (pattern, replacement) in patterns)
            {
                code = System.Text.RegularExpressions.Regex.Replace(code, pattern, replacement);
            }
            
            return code;
        }
        
        private string SimplifyBooleanComparison(string code)
        {
            // condition == true => condition
            // condition == false => !condition
            // condition != true => !condition
            // condition != false => condition
            
            var patterns = new[]
            {
                (@"([^=!<>]+)\s*==\s*true\b", "$1"),
                (@"([^=!<>]+)\s*!=\s*false\b", "$1"),
                (@"([^=!<>]+)\s*==\s*false\b", "!($1)"),
                (@"([^=!<>]+)\s*!=\s*true\b", "!($1)"),
                (@"\btrue\s*==\s*([^=!<>]+)", "$1"),
                (@"\bfalse\s*!=\s*([^=!<>]+)", "$1"),
                (@"\bfalse\s*==\s*([^=!<>]+)", "!($1)"),
                (@"\btrue\s*!=\s*([^=!<>]+)", "!($1)")
            };
            
            foreach (var (pattern, replacement) in patterns)
            {
                code = System.Text.RegularExpressions.Regex.Replace(code, pattern, replacement);
            }
            
            return code;
        }

        #region Logging
        private static void LogInfo(string message) => Console.WriteLine($"[INFO] {message}");
        private static void LogWarn(string message) => Console.WriteLine($"[WARN] {message}");
        private static void LogError(string message) => Console.WriteLine($"[ERROR] {message}");
        private static void LogDebug(string message) => Console.WriteLine($"[DEBUG] {message}");
        #endregion
    }
}