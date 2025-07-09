using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker
{
    /// <summary>
    /// Roslynator 重构工具 - 提供常用的代码重构功能
    /// </summary>
    public class RoslynatorRefactorer
    {
        public void RefactorCode(string path)
        {
            Console.WriteLine($"[INFO] 开始 Roslynator 重构，扫描路径: {path}");
            
            if (File.Exists(path) && path.EndsWith(".cs"))
            {
                RefactorSingleFile(path);
            }
            else if (Directory.Exists(path))
            {
                RefactorDirectory(path);
            }
            else
            {
                Console.WriteLine($"[ERROR] 无效的路径: {path}");
            }
        }

        private void RefactorSingleFile(string filePath)
        {
            try
            {
                Console.WriteLine($"[INFO] 重构文件: {filePath}");
                string originalCode = File.ReadAllText(filePath);
                
                var syntaxTree = CSharpSyntaxTree.ParseText(originalCode);
                var root = syntaxTree.GetRoot();
                
                // 应用重构规则
                var refactoredRoot = ApplyRefactoringRules(root);
                
                if (refactoredRoot != root)
                {
                    var refactoredCode = refactoredRoot.ToFullString();
                    
                    // 创建备份
                    string backupPath = filePath + ".roslynator.backup";
                    File.Copy(filePath, backupPath, true);
                    Console.WriteLine($"[INFO] 已创建备份: {backupPath}");
                    
                    // 写入重构后的代码
                    File.WriteAllText(filePath, refactoredCode);
                    Console.WriteLine($"[INFO] ✅ 重构完成: {filePath}");
                }
                else
                {
                    Console.WriteLine($"[INFO] 无需重构: {filePath}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[ERROR] 重构文件失败 {filePath}: {ex.Message}");
            }
        }

        private void RefactorDirectory(string directoryPath)
        {
            var csFiles = Directory.GetFiles(directoryPath, "*.cs", SearchOption.AllDirectories);
            if (csFiles.Length == 0)
            {
                Console.WriteLine("[WARN] 未找到任何 .cs 文件");
                return;
            }

            Console.WriteLine($"[INFO] 找到 {csFiles.Length} 个 .cs 文件，开始批量重构...");
            int successCount = 0;
            int failureCount = 0;
            int unchangedCount = 0;
            
            foreach (var file in csFiles)
            {
                try
                {
                    string originalCode = File.ReadAllText(file);
                    var syntaxTree = CSharpSyntaxTree.ParseText(originalCode);
                    var root = syntaxTree.GetRoot();
                    var refactoredRoot = ApplyRefactoringRules(root);
                    
                    if (refactoredRoot != root)
                    {
                        var refactoredCode = refactoredRoot.ToFullString();
                        string backupPath = file + ".roslynator.backup";
                        File.Copy(file, backupPath, true);
                        File.WriteAllText(file, refactoredCode);
                        successCount++;
                        Console.WriteLine($"[INFO] ✅ 重构: {Path.GetFileName(file)}");
                    }
                    else
                    {
                        unchangedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[ERROR] 处理失败 {file}: {ex.Message}");
                    failureCount++;
                }
            }

            Console.WriteLine($"[INFO] 重构完成！成功: {successCount}, 失败: {failureCount}, 无需更改: {unchangedCount}");
        }

        private SyntaxNode ApplyRefactoringRules(SyntaxNode root)
        {
            var rewriter = new RoslynatorRewriter();
            return rewriter.Visit(root);
        }
    }

    /// <summary>
    /// Roslynator 重构语法重写器
    /// </summary>
    internal class RoslynatorRewriter : CSharpSyntaxRewriter
    {
        public override SyntaxNode VisitVariableDeclaration(VariableDeclarationSyntax node)
        {
            // RCS1014: 使用显式类型而不是 var（如果类型明显）
            if (node.Type.IsVar)
            {
                var variable = node.Variables.FirstOrDefault();
                if (variable?.Initializer?.Value != null)
                {
                    var initValue = variable.Initializer.Value;
                    
                    // 如果初始化器是明显的类型（如 new int()），建议使用显式类型
                    if (initValue is ObjectCreationExpressionSyntax objCreation)
                    {
                        var explicitType = objCreation.Type;
                        return node.WithType(explicitType);
                    }
                }
            }

            return base.VisitVariableDeclaration(node);
        }

        public override SyntaxNode VisitIfStatement(IfStatementSyntax node)
        {
            // RCS1003: 添加大括号到 if-else
            if (node.Statement != null && !(node.Statement is BlockSyntax))
            {
                var block = SyntaxFactory.Block(node.Statement);
                node = node.WithStatement(block);
            }

            if (node.Else?.Statement != null && !(node.Else.Statement is BlockSyntax) && !(node.Else.Statement is IfStatementSyntax))
            {
                var elseBlock = SyntaxFactory.Block(node.Else.Statement);
                node = node.WithElse(node.Else.WithStatement(elseBlock));
            }

            return base.VisitIfStatement(node);
        }

        public override SyntaxNode VisitWhileStatement(WhileStatementSyntax node)
        {
            // 为 while 语句添加大括号
            if (node.Statement != null && !(node.Statement is BlockSyntax))
            {
                var block = SyntaxFactory.Block(node.Statement);
                node = node.WithStatement(block);
            }

            return base.VisitWhileStatement(node);
        }

        public override SyntaxNode VisitForStatement(ForStatementSyntax node)
        {
            // 为 for 语句添加大括号
            if (node.Statement != null && !(node.Statement is BlockSyntax))
            {
                var block = SyntaxFactory.Block(node.Statement);
                node = node.WithStatement(block);
            }

            return base.VisitForStatement(node);
        }

        public override SyntaxNode VisitForEachStatement(ForEachStatementSyntax node)
        {
            // 为 foreach 语句添加大括号
            if (node.Statement != null && !(node.Statement is BlockSyntax))
            {
                var block = SyntaxFactory.Block(node.Statement);
                node = node.WithStatement(block);
            }

            return base.VisitForEachStatement(node);
        }

        public override SyntaxNode VisitUsingStatement(UsingStatementSyntax node)
        {
            // 为 using 语句添加大括号
            if (node.Statement != null && !(node.Statement is BlockSyntax))
            {
                var block = SyntaxFactory.Block(node.Statement);
                node = node.WithStatement(block);
            }

            return base.VisitUsingStatement(node);
        }

        public override SyntaxNode VisitBinaryExpression(BinaryExpressionSyntax node)
        {
            // RCS1104: 简化条件表达式
            if (node.IsKind(SyntaxKind.EqualsExpression))
            {
                // true == expr -> expr
                if (node.Left.IsKind(SyntaxKind.TrueLiteralExpression))
                {
                    return node.Right;
                }
                // expr == true -> expr  
                if (node.Right.IsKind(SyntaxKind.TrueLiteralExpression))
                {
                    return node.Left;
                }
                // false == expr -> !expr
                if (node.Left.IsKind(SyntaxKind.FalseLiteralExpression))
                {
                    return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, node.Right);
                }
                // expr == false -> !expr
                if (node.Right.IsKind(SyntaxKind.FalseLiteralExpression))
                {
                    return SyntaxFactory.PrefixUnaryExpression(SyntaxKind.LogicalNotExpression, node.Left);
                }
            }

            return base.VisitBinaryExpression(node);
        }
    }
}