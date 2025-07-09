using System.Collections.Immutable;
using System.Composition;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace CodeUnfucker;

/// <summary>
/// Pure 属性代码修复器
/// 自动添加或移除 [Pure] 属性
/// </summary>
[ExportCodeFixProvider(LanguageNames.CSharp, Name = nameof(PureCodeFixProvider)), Shared]
public class PureCodeFixProvider : CodeFixProvider
{
    public sealed override ImmutableArray<string> FixableDiagnosticIds =>
        ImmutableArray.Create(PureAnalyzer.SuggestAddPureRule.Id, PureAnalyzer.SuggestRemovePureRule.Id);

    public sealed override FixAllProvider GetFixAllProvider() => WellKnownFixAllProviders.BatchFixer;

    public sealed override async Task RegisterCodeFixesAsync(CodeFixContext context)
    {
        var root = await context.Document.GetSyntaxRootAsync(context.CancellationToken).ConfigureAwait(false);

        if (root == null)
            return;

        foreach (var diagnostic in context.Diagnostics)
        {
            var diagnosticSpan = diagnostic.Location.SourceSpan;
            var node = root.FindNode(diagnosticSpan);

            // 查找方法或属性声明
            var memberDeclaration = node.FirstAncestorOrSelf<MemberDeclarationSyntax>();
            
            if (memberDeclaration == null)
                continue;

            if (diagnostic.Id == PureAnalyzer.SuggestAddPureRule.Id)
            {
                RegisterAddPureCodeFix(context, root, memberDeclaration, diagnostic);
            }
            else if (diagnostic.Id == PureAnalyzer.SuggestRemovePureRule.Id)
            {
                RegisterRemovePureCodeFix(context, root, memberDeclaration, diagnostic);
            }
        }
    }

    private void RegisterAddPureCodeFix(CodeFixContext context, SyntaxNode root, 
        MemberDeclarationSyntax memberDeclaration, Diagnostic diagnostic)
    {
        var action = CodeAction.Create(
            title: "添加 [Pure] 属性",
            createChangedDocument: cancellationToken => AddPureAttributeAsync(context.Document, root, memberDeclaration, cancellationToken),
            equivalenceKey: "AddPure");

        context.RegisterCodeFix(action, diagnostic);
    }

    private void RegisterRemovePureCodeFix(CodeFixContext context, SyntaxNode root, 
        MemberDeclarationSyntax memberDeclaration, Diagnostic diagnostic)
    {
        var action = CodeAction.Create(
            title: "移除 [Pure] 属性",
            createChangedDocument: cancellationToken => RemovePureAttributeAsync(context.Document, root, memberDeclaration, cancellationToken),
            equivalenceKey: "RemovePure");

        context.RegisterCodeFix(action, diagnostic);
    }

    private async Task<Document> AddPureAttributeAsync(Document document, SyntaxNode root, 
        MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
    {
        // 创建 [Pure] 属性
        var pureAttribute = SyntaxFactory.Attribute(
            SyntaxFactory.IdentifierName("Pure"))
            .WithLeadingTrivia(SyntaxFactory.Whitespace("    ")); // 添加缩进

        var attributeList = SyntaxFactory.AttributeList(
            SyntaxFactory.SingletonSeparatedList(pureAttribute))
            .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

        // 添加属性到成员声明
        var newMemberDeclaration = memberDeclaration.AddAttributeLists(attributeList);

        // 替换旧的成员声明
        var newRoot = root.ReplaceNode(memberDeclaration, newMemberDeclaration);

        // 检查是否需要添加 using 语句
        var newDocument = document.WithSyntaxRoot(newRoot);
        newDocument = await EnsureUsingDirectiveAsync(newDocument, cancellationToken);

        return newDocument;
    }

    private async Task<Document> RemovePureAttributeAsync(Document document, SyntaxNode root, 
        MemberDeclarationSyntax memberDeclaration, CancellationToken cancellationToken)
    {
        var newMemberDeclaration = memberDeclaration;

        // 查找并移除所有 Pure 属性
        foreach (var attributeList in memberDeclaration.AttributeLists.ToList())
        {
            var attributesToRemove = attributeList.Attributes
                .Where(attr => IsPureAttribute(attr))
                .ToList();

            if (attributesToRemove.Any())
            {
                var remainingAttributes = attributeList.Attributes
                    .Where(attr => !attributesToRemove.Contains(attr))
                    .ToList();

                if (remainingAttributes.Any())
                {
                    // 如果还有其他属性，只移除 Pure 属性
                    var newAttributeList = attributeList.WithAttributes(
                        SyntaxFactory.SeparatedList(remainingAttributes));
                    newMemberDeclaration = newMemberDeclaration.ReplaceNode(attributeList, newAttributeList);
                }
                else
                {
                    // 如果没有其他属性，移除整个属性列表
                    var removedNode = newMemberDeclaration.RemoveNode(attributeList, SyntaxRemoveOptions.KeepNoTrivia);
                    if (removedNode != null)
                        newMemberDeclaration = removedNode;
                }
            }
        }

        var newRoot = root.ReplaceNode(memberDeclaration, newMemberDeclaration);
        return document.WithSyntaxRoot(newRoot);
    }

    private async Task<Document> EnsureUsingDirectiveAsync(Document document, CancellationToken cancellationToken)
    {
        var root = await document.GetSyntaxRootAsync(cancellationToken).ConfigureAwait(false);
        
        if (root is not CompilationUnitSyntax compilationUnit)
            return document;

        // 检查是否已经有 System.Diagnostics.Contracts using 语句
        var hasContractsUsing = compilationUnit.Usings.Any(u =>
            u.Name?.ToString() == "System.Diagnostics.Contracts");

        if (!hasContractsUsing)
        {
            // 添加 using System.Diagnostics.Contracts;
            var contractsUsing = SyntaxFactory.UsingDirective(
                SyntaxFactory.QualifiedName(
                    SyntaxFactory.QualifiedName(
                        SyntaxFactory.IdentifierName("System"),
                        SyntaxFactory.IdentifierName("Diagnostics")),
                    SyntaxFactory.IdentifierName("Contracts")))
                .WithTrailingTrivia(SyntaxFactory.EndOfLine("\n"));

            var newCompilationUnit = compilationUnit.AddUsings(contractsUsing);
            return document.WithSyntaxRoot(newCompilationUnit);
        }

        return document;
    }

    private bool IsPureAttribute(AttributeSyntax attribute)
    {
        var name = attribute.Name.ToString();
        return name == "Pure" || 
               name == "System.Diagnostics.Contracts.Pure" ||
               name == "PureAttribute" ||
               name == "System.Diagnostics.Contracts.PureAttribute";
    }
}