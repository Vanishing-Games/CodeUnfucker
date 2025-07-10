using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using FluentAssertions;
using CodeUnfucker.Analyzers;

namespace CodeUnfucker.Tests
{
    public class PureMethodAnalyzerTests : TestBase
    {
        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectMethodEligibleForPure_WhenMethodHasNoSideEffects()
        {
            // Arrange
            var sourceCode = @"
using System;

public class TestClass
{
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0009");
            diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
            diagnostic.GetMessage().Should().Contain("CalculateSum");
            diagnostic.GetMessage().Should().Contain("建议添加 [System.Diagnostics.Contracts.Pure] 属性");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectIncorrectlyMarkedPureMethod_WhenMethodHasSideEffects()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int ProcessData(int value)
    {
        Console.WriteLine(""Processing: "" + value);
        return value * 2;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0010");
            diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
            diagnostic.GetMessage().Should().Contain("ProcessData");
            diagnostic.GetMessage().Should().Contain("包含副作用，应移除 [Pure] 属性");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotSuggestPure_WhenMethodReturnsVoid()
        {
            // Arrange
            var sourceCode = @"
using System;

public class TestClass
{
    public void ProcessData(int value)
    {
        var result = value * 2;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotSuggestPure_WhenMethodIsPrivate()
        {
            // Arrange
            var sourceCode = @"
using System;

public class TestClass
{
    private int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectSideEffectsFromAssignment()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;

public class TestClass
{
    private int _counter = 0;

    [Pure]
    public int ProcessAndIncrement(int value)
    {
        _counter++;  // Side effect: assignment
        return value * 2;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0010");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectSideEffectsFromVoidMethodCall()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int ProcessWithLogging(int value)
    {
        Console.WriteLine(""Processing"");  // Side effect: void method call
        return value * 2;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0010");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectSideEffectsFromIncrementOperator()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int ProcessWithIncrement(int value)
    {
        value++;  // Side effect: increment
        return value;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0010");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectSideEffectsFromUnityApiCall()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;
using UnityEngine;

public class TestClass
{
    [Pure]
    public int ProcessWithDebugLog(int value)
    {
        Debug.Log(""Processing: "" + value);  // Side effect: Unity API call
        return value * 2;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0010");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldSuggestPureForInternalMethod()
        {
            // Arrange
            var sourceCode = @"
using System;

public class TestClass
{
    internal int CalculateProduct(int a, int b)
    {
        return a * b;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0009");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotSuggestPureForPartialMethod()
        {
            // Arrange
            var sourceCode = @"
using System;

public partial class TestClass
{
    public partial int CalculateSum(int a, int b);
}

public partial class TestClass
{
    public partial int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotReportIssuesForCorrectlyMarkedPureMethod()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int CalculateSum(int a, int b)
    {
        return a + b;  // No side effects
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldHandleMultipleMethodsCorrectly()
        {
            // Arrange
            var sourceCode = @"
using System;
using System.Diagnostics.Contracts;

public class TestClass
{
    // Should suggest Pure
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }

    // Should warn about incorrect Pure
    [Pure]
    public int ProcessWithSideEffect(int value)
    {
        Console.WriteLine(""Processing"");
        return value * 2;
    }

    // Should not suggest Pure (void return)
    public void DoSomething()
    {
        var x = 5;
    }

    // Correctly marked Pure method (no issues)
    [Pure]
    public int Multiply(int a, int b)
    {
        return a * b;
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics.OrderBy(d => d.Id).ToList();

            // Assert
            diagnostics.Should().HaveCount(2);
            
            // First diagnostic should be UNITY0009 (suggest Pure)
            diagnostics[0].Id.Should().Be("UNITY0009");
            diagnostics[0].GetMessage().Should().Contain("CalculateSum");

            // Second diagnostic should be UNITY0010 (incorrect Pure)
            diagnostics[1].Id.Should().Be("UNITY0010");
            diagnostics[1].GetMessage().Should().Contain("ProcessWithSideEffect");
        }

        private PureMethodAnalyzer CreateAnalyzer(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(Console).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var analyzer = new PureMethodAnalyzer();
            analyzer.AnalyzeSyntaxTree(syntaxTree, semanticModel);

            return analyzer;
        }
    }
}