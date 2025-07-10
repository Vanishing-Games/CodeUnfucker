using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Xunit;
using FluentAssertions;
using CodeUnfucker.Analyzers;

namespace CodeUnfucker.Tests
{
    public class UnityUpdateHeapAllocationAnalyzerTests : TestBase
    {
        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectObjectCreation_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        var list = new System.Collections.Generic.List<int>();
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0001");
            diagnostic.Severity.Should().Be(DiagnosticSeverity.Warning);
            diagnostic.GetMessage().Should().Contain("Update()");
            diagnostic.GetMessage().Should().Contain("堆内存分配");
            diagnostic.GetMessage().Should().Contain("System.Collections.Generic.List");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectLinqUsage_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class TestBehaviour : MonoBehaviour
{
    private List<int> numbers = new List<int> { 1, 2, 3, 4, 5 };

    void Update()
    {
        var evenNumbers = numbers.Where(x => x % 2 == 0).ToList();
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Line).ToList();

            // Assert
            diagnostics.Should().HaveCount(3);  // Where, ToList, and Lambda closure
            
            // Check that we have LINQ methods
            var linqDiagnostics = diagnostics.Where(d => d.GetMessage().Contains("LINQ 方法")).ToList();
            linqDiagnostics.Should().HaveCount(2);
            
            // Check LINQ methods are detected
            linqDiagnostics.Any(d => d.GetMessage().Contains(".Where()")).Should().BeTrue();
            linqDiagnostics.Any(d => d.GetMessage().Contains(".ToList()")).Should().BeTrue();
            
            // Check Lambda closure is detected
            diagnostics.Any(d => d.GetMessage().Contains("Lambda 表达式可能产生闭包")).Should().BeTrue();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectStringConcatenation_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        string message = ""Player score: "" + GetScore();
    }

    private int GetScore() => 100;
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0001");
            diagnostic.GetMessage().Should().Contain("字符串拼接操作");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectStringInterpolation_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        string message = $""Player score: {GetScore()}"";
    }

    private int GetScore() => 100;
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0001");
            diagnostic.GetMessage().Should().Contain("字符串插值");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectArrayCreation_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        int[] numbers = new int[10];
        var moreNumbers = new[] { 1, 2, 3 };
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Line).ToList();

            // Assert
            diagnostics.Should().HaveCount(2);
            
            // First array creation
            diagnostics[0].GetMessage().Should().Contain("数组创建");
            
            // Second array creation (implicit)
            diagnostics[1].GetMessage().Should().Contain("隐式数组创建");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectAnonymousObjects_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        var data = new { Name = ""Player"", Score = 100 };
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0001");
            diagnostic.GetMessage().Should().Contain("匿名对象创建");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectLambdaClosures_InUpdateMethod()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;
using System;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        Action callback = () => Debug.Log(""Test"");
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.Id.Should().Be("UNITY0001");
            diagnostic.GetMessage().Should().Contain("Lambda 表达式可能产生闭包");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldAnalyzeAllUnityUpdateMethods()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        var list1 = new System.Collections.Generic.List<int>();
    }

    void LateUpdate()
    {
        var list2 = new System.Collections.Generic.List<string>();
    }

    void FixedUpdate()
    {
        var list3 = new System.Collections.Generic.List<float>();
    }

    void OnGUI()
    {
        var list4 = new System.Collections.Generic.List<bool>();
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Line).ToList();

            // Assert
            diagnostics.Should().HaveCount(4);
            diagnostics[0].GetMessage().Should().Contain("Update()");
            diagnostics[1].GetMessage().Should().Contain("LateUpdate()");
            diagnostics[2].GetMessage().Should().Contain("FixedUpdate()");
            diagnostics[3].GetMessage().Should().Contain("OnGUI()");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotAnalyzeNonMonoBehaviourClasses()
        {
            // Arrange
            var sourceCode = @"
using System.Collections.Generic;

public class TestClass
{
    void Update()
    {
        var list = new List<int>();  // Should NOT be detected
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotAnalyzeNonUpdateMethods()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Start()
    {
        var list = new System.Collections.Generic.List<int>();  // Should NOT be detected
    }

    void OnEnable()
    {
        var dict = new System.Collections.Generic.Dictionary<string, int>();  // Should NOT be detected
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldNotDetectValueTypeCreation()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        Vector3 position = new Vector3(1, 2, 3);  // Value type, should NOT be detected
        int number = new int();  // Value type, should NOT be detected
        Color color = new Color(1, 0, 0, 1);  // Value type, should NOT be detected
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectCollectionInitializers()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;
using System.Collections.Generic;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        var list = new List<int> { 1, 2, 3 };
        var dict = new Dictionary<string, int> { [""key""] = 42 };
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics.OrderBy(d => d.Location.GetLineSpan().StartLinePosition.Line).ToList();

            // Assert - We should have initializer diagnostics for collection/object initializers
            diagnostics.Should().HaveCountGreaterThan(0);
            
            // Check that we have initializer diagnostics
            var initializerDiagnostics = diagnostics.Where(d => d.GetMessage().Contains("初始化器")).ToList();
            initializerDiagnostics.Should().HaveCount(2);
            
            // All diagnostics should be UNITY0001
            diagnostics.All(d => d.Id == "UNITY0001").Should().BeTrue();
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldDetectImplicitObjectCreation()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;
using System.Collections.Generic;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        List<int> list = new();  // C# 9.0 target-typed new
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            diagnostic.GetMessage().Should().Contain("隐式对象创建");
        }

        [Fact]
        public void AnalyzeSyntaxTree_ShouldProduceCorrectLocationInformation()
        {
            // Arrange
            var sourceCode = @"
using UnityEngine;

public class TestBehaviour : MonoBehaviour
{
    void Update()
    {
        var list = new System.Collections.Generic.List<int>();
    }
}";

            var analyzer = CreateAnalyzer(sourceCode);

            // Act
            var diagnostics = analyzer.Diagnostics;

            // Assert
            diagnostics.Should().HaveCount(1);
            var diagnostic = diagnostics.First();
            
            // Check that location information is valid
            var location = diagnostic.Location;
            location.Should().NotBe(Location.None);
            
            var lineSpan = location.GetLineSpan();
            lineSpan.StartLinePosition.Line.Should().BeGreaterOrEqualTo(0);
            lineSpan.StartLinePosition.Character.Should().BeGreaterOrEqualTo(0);
        }

        private UnityUpdateHeapAllocationAnalyzer CreateAnalyzer(string sourceCode)
        {
            var syntaxTree = CSharpSyntaxTree.ParseText(sourceCode);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.List<>).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Collections.Generic.Dictionary<,>).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var analyzer = new UnityUpdateHeapAllocationAnalyzer();
            analyzer.AnalyzeSyntaxTree(syntaxTree, semanticModel);

            return analyzer;
        }
    }
}