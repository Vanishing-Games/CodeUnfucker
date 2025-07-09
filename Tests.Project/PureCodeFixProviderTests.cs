using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CodeActions;
using Microsoft.CodeAnalysis.CodeFixes;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Text;
using CodeUnfucker;
using Xunit;

namespace CodeUnfucker.Tests
{
    public class PureCodeFixProviderTests : TestBase
    {
        private readonly PureCodeFixProvider _codeFixProvider;

        public PureCodeFixProviderTests()
        {
            _codeFixProvider = new PureCodeFixProvider();
        }

        [Fact]
        public void FixableDiagnosticIds_ShouldIncludeBothRules()
        {
            // Act
            var fixableIds = _codeFixProvider.FixableDiagnosticIds;

            // Assert
            fixableIds.Should().Contain(PureAnalyzer.SuggestAddPureRule.Id);
            fixableIds.Should().Contain(PureAnalyzer.SuggestRemovePureRule.Id);
        }

        [Fact]
        public async Task AddPureAttribute_ShouldAddAttributeAndUsingStatement()
        {
            // Arrange
            var originalCode = @"
public class TestClass
{
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            var expectedCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            // Act
            var fixedCode = await ApplyAddPureFixAsync(originalCode);

            // Assert
            fixedCode.Should().Be(expectedCode);
        }

        [Fact]
        public async Task AddPureAttribute_WithExistingUsing_ShouldOnlyAddAttribute()
        {
            // Arrange
            var originalCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            var expectedCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            // Act
            var fixedCode = await ApplyAddPureFixAsync(originalCode);

            // Assert
            fixedCode.Should().Be(expectedCode);
        }

        [Fact]
        public async Task RemovePureAttribute_ShouldRemoveOnlyPureAttribute()
        {
            // Arrange
            var originalCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    [Obsolete]
    public int CalculateSum(int a, int b)
    {
        _field = a; // Side effect
        return a + b;
    }
    
    private int _field;
}";

            var expectedCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    [Obsolete]
    public int CalculateSum(int a, int b)
    {
        _field = a; // Side effect
        return a + b;
    }
    
    private int _field;
}";

            // Act
            var fixedCode = await ApplyRemovePureFixAsync(originalCode);

            // Assert
            fixedCode.Should().Be(expectedCode);
        }

        [Fact]
        public async Task RemovePureAttribute_WithOnlyPureAttribute_ShouldRemoveEntireAttributeList()
        {
            // Arrange
            var originalCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    private int _field;
    
    [Pure]
    public int CalculateSum(int a, int b)
    {
        _field = a; // Side effect
        return a + b;
    }
}";

            var expectedCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    private int _field;
    
    public int CalculateSum(int a, int b)
    {
        _field = a; // Side effect
        return a + b;
    }
}";

            // Act
            var fixedCode = await ApplyRemovePureFixAsync(originalCode);

            // Assert
            fixedCode.Should().Be(expectedCode);
        }

        [Fact]
        public async Task AddPureToProperty_ShouldAddAttributeCorrectly()
        {
            // Arrange
            var originalCode = @"
public class TestClass
{
    private readonly string _name = ""Test"";
    
    public string FormattedName 
    { 
        get 
        { 
            return $""Name: {_name}""; 
        } 
    }
}";

            var expectedCode = @"using System.Diagnostics.Contracts;

public class TestClass
{
    private readonly string _name = ""Test"";
    
    [Pure]
    public string FormattedName 
    { 
        get 
        { 
            return $""Name: {_name}""; 
        } 
    }
}";

            // Act
            var fixedCode = await ApplyAddPureFixAsync(originalCode);

            // Assert
            fixedCode.Should().Be(expectedCode);
        }

        private async Task<string> ApplyAddPureFixAsync(string originalCode)
        {
            // Create diagnostic for "suggest add pure"
            var diagnostic = Diagnostic.Create(
                PureAnalyzer.SuggestAddPureRule,
                Location.Create(
                    SyntaxFactory.ParseSyntaxTree(originalCode),
                    new TextSpan(0, 1)),
                "TestMethod");

            return await ApplyCodeFixAsync(originalCode, diagnostic);
        }

        private async Task<string> ApplyRemovePureFixAsync(string originalCode)
        {
            // Create diagnostic for "suggest remove pure"  
            var diagnostic = Diagnostic.Create(
                PureAnalyzer.SuggestRemovePureRule,
                Location.Create(
                    SyntaxFactory.ParseSyntaxTree(originalCode),
                    new TextSpan(0, 1)),
                "TestMethod");

            return await ApplyCodeFixAsync(originalCode, diagnostic);
        }



        private async Task<string> ApplyCodeFixAsync(string originalCode, Diagnostic diagnostic)
        {
            // Create workspace and document
            using var workspace = new AdhocWorkspace();
            var projectId = ProjectId.CreateNewId();
            var documentId = DocumentId.CreateNewId(projectId);

            var solution = workspace
                .CurrentSolution
                .AddProject(projectId, "TestProject", "TestProject", LanguageNames.CSharp)
                .AddDocument(documentId, "Test.cs", originalCode);

            var document = solution.GetDocument(documentId);
            var root = await document.GetSyntaxRootAsync();

            // Create a mock code action to capture the result
            CodeAction? capturedAction = null;

            // Create code fix context
            var context = new CodeFixContext(
                document,
                diagnostic,
                (action, diagnostics) => { capturedAction = action; },
                CancellationToken.None);

            // Register code fixes
            await _codeFixProvider.RegisterCodeFixesAsync(context);

            if (capturedAction == null)
                throw new InvalidOperationException("No code fix was registered");

            // Apply the code fix
            var operations = await capturedAction.GetOperationsAsync(CancellationToken.None);
            var applyChangesOperation = operations.OfType<ApplyChangesOperation>().FirstOrDefault();

            if (applyChangesOperation == null)
                throw new InvalidOperationException("No apply changes operation found");

            var newSolution = applyChangesOperation.ChangedSolution;
            var newDocument = newSolution.GetDocument(documentId);
            var newText = await newDocument.GetTextAsync();

            return newText.ToString();
        }
    }
}