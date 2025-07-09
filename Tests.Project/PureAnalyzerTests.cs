using System;
using System.IO;
using System.Linq;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using CodeUnfucker;
using Xunit;
using System.Collections.Generic;

namespace CodeUnfucker.Tests
{
    public class PureAnalyzerTests : TestBase
    {
        private readonly PureAnalyzerConfig _defaultConfig;

        public PureAnalyzerTests()
        {
            _defaultConfig = PureAnalyzerConfig.GetDefault();
        }

        [Fact]
        public void AnalyzePureMethod_ShouldSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    public int CalculateSum(int a, int b)
    {
        return a + b;
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestAddPureRule.Id);
        }

        [Fact]
        public void AnalyzeMethodWithSideEffects_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    private int _field;
    
    public int IncrementAndReturn()
    {
        _field++;
        return _field;
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeVoidMethod_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    public void DoSomething()
    {
        var x = 1 + 1;
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeWronglyMarkedPureMethod_ShouldSuggestRemovingPure()
        {
            // Arrange
            var code = @"
using System.Diagnostics.Contracts;

public class TestClass
{
    private int _field;
    
    [Pure]
    public int WronglyMarkedMethod()
    {
        _field = 42; // Side effect
        return _field;
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestRemovePureRule.Id);
        }

        [Fact]
        public void AnalyzeCorrectlyMarkedPureMethod_ShouldNotSuggestRemoving()
        {
            // Arrange
            var code = @"
using System.Diagnostics.Contracts;

public class TestClass
{
    [Pure]
    public int CorrectlyMarkedMethod(int a, int b)
    {
        return a + b;
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzePureProperty_ShouldSuggestAddingPure()
        {
            // Arrange
            var code = @"
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

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestAddPureRule.Id);
        }

        [Fact]
        public void AnalyzePropertyWithSideEffects_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    private int _field;
    
    public int BadProperty
    {
        get
        {
            System.Console.WriteLine(""Getting value""); // Side effect
            return _field;
        }
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeWriteableProperty_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    public int Value { get; set; }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeMethodWithUnityApi_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
using UnityEngine;

public class TestClass
{
    public Vector3 GetRandomPosition()
    {
        return new Vector3(Random.Range(0f, 10f), 0f, 0f);
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeMethodWithDebugLog_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
using UnityEngine;

public class TestClass
{
    public int CalculateWithLogging(int a, int b)
    {
        Debug.Log($""Calculating {a} + {b}"");
        return a + b;
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzePrivateMethod_WithPrivateNotInConfig_ShouldNotSuggest()
        {
            // Arrange
            var code = @"
public class TestClass
{
    private int PrivateMethod(int a, int b)
    {
        return a + b;
    }
}";

            var config = PureAnalyzerConfig.GetDefault();
            config.Accessibility = new List<string> { "public", "internal" }; // Exclude private

            // Act
            var diagnostics = AnalyzeCode(code, config);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzePartialMethod_WithExcludePartialEnabled_ShouldNotSuggest()
        {
            // Arrange
            var code = @"
public partial class TestClass
{
    public partial int PartialMethod(int a, int b);
}";

            var config = PureAnalyzerConfig.GetDefault();
            config.ExcludePartial = true;

            // Act
            var diagnostics = AnalyzeCode(code, config);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void AnalyzeLinqMethod_ShouldSuggestAddingPure()
        {
            // Arrange
            var code = @"
using System.Linq;

public class TestClass
{
    public int[] FilterEvenNumbers(int[] numbers)
    {
        return numbers.Where(x => x % 2 == 0).ToArray();
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestAddPureRule.Id);
        }

        [Fact]
        public void AnalyzeRecursiveMethod_ShouldSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    public int Factorial(int n)
    {
        if (n <= 1) return 1;
        return n * Factorial(n - 1);
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestAddPureRule.Id);
        }

        [Fact]
        public void AnalyzeMathMethod_ShouldSuggestAddingPure()
        {
            // Arrange
            var code = @"
using System;

public class TestClass
{
    public double CalculateDistance(double x1, double y1, double x2, double y2)
    {
        return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestAddPureRule.Id);
        }

        [Fact]
        public void AnalyzeStringMethod_ShouldSuggestAddingPure()
        {
            // Arrange
            var code = @"
public class TestClass
{
    public string FormatName(string firstName, string lastName)
    {
        return $""{firstName} {lastName}"".Trim();
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().ContainSingle();
            diagnostics.First().Id.Should().Be(PureAnalyzer.SuggestAddPureRule.Id);
        }

        [Fact]
        public void AnalyzeMethodWithDateTimeNow_ShouldNotSuggestAddingPure()
        {
            // Arrange
            var code = @"
using System;

public class TestClass
{
    public string GetTimestamp()
    {
        return DateTime.Now.ToString();
    }
}";

            // Act
            var diagnostics = AnalyzeCode(code);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void ConfigurationTests_DisabledSuggestAdd_ShouldNotSuggest()
        {
            // Arrange
            var code = @"
public class TestClass
{
    public int PureMethod(int a, int b)
    {
        return a + b;
    }
}";

            var config = PureAnalyzerConfig.GetDefault();
            config.EnableSuggestAdd = false;

            // Act
            var diagnostics = AnalyzeCode(code, config);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        [Fact]
        public void ConfigurationTests_DisabledSuggestRemove_ShouldNotSuggest()
        {
            // Arrange
            var code = @"
using System.Diagnostics.Contracts;

public class TestClass
{
    private int _field;
    
    [Pure]
    public int ImpureMethod()
    {
        _field = 42;
        return _field;
    }
}";

            var config = PureAnalyzerConfig.GetDefault();
            config.EnableSuggestRemove = false;

            // Act
            var diagnostics = AnalyzeCode(code, config);

            // Assert
            diagnostics.Should().BeEmpty();
        }

        private Diagnostic[] AnalyzeCode(string code, PureAnalyzerConfig? config = null)
        {
            config ??= _defaultConfig;
            
            var syntaxTree = CSharpSyntaxTree.ParseText(code);
            var references = new[]
            {
                MetadataReference.CreateFromFile(typeof(object).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Linq.Enumerable).Assembly.Location),
                MetadataReference.CreateFromFile(typeof(System.Console).Assembly.Location)
            };

            var compilation = CSharpCompilation.Create(
                "TestAssembly",
                new[] { syntaxTree },
                references,
                new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));

            var semanticModel = compilation.GetSemanticModel(syntaxTree);
            var diagnostics = new List<Diagnostic>();
            var root = syntaxTree.GetRoot();

            // Analyze methods
            var methods = root.DescendantNodes().OfType<MethodDeclarationSyntax>();
            foreach (var method in methods)
            {
                var methodDiagnostics = AnalyzeMethod(method, semanticModel, config);
                diagnostics.AddRange(methodDiagnostics);
            }

            // Analyze properties
            var properties = root.DescendantNodes().OfType<PropertyDeclarationSyntax>();
            foreach (var property in properties)
            {
                var propertyDiagnostics = AnalyzeProperty(property, semanticModel, config);
                diagnostics.AddRange(propertyDiagnostics);
            }

            return diagnostics.ToArray();
        }

        private List<Diagnostic> AnalyzeMethod(MethodDeclarationSyntax method, SemanticModel semanticModel, PureAnalyzerConfig config)
        {
            var diagnostics = new List<Diagnostic>();
            
            if (!config.EnableSuggestAdd && !config.EnableSuggestRemove)
                return diagnostics;

            // Check accessibility
            if (!IsTargetAccessibility(method, config))
                return diagnostics;

            // Check if partial methods should be excluded
            if (config.ExcludePartial && method.Modifiers.Any(SyntaxKind.PartialKeyword))
                return diagnostics;

            var methodSymbol = semanticModel.GetDeclaredSymbol(method);
            if (methodSymbol == null)
                return diagnostics;

            bool hasPureAttribute = HasPureAttribute(method);
            bool shouldBePure = ShouldMethodBePure(method, methodSymbol, semanticModel, config);

            if (config.EnableSuggestAdd && !hasPureAttribute && shouldBePure)
            {
                var diagnostic = Diagnostic.Create(
                    PureAnalyzer.SuggestAddPureRule,
                    method.Identifier.GetLocation(),
                    methodSymbol.Name);
                diagnostics.Add(diagnostic);
            }
            else if (config.EnableSuggestRemove && hasPureAttribute && !shouldBePure)
            {
                var diagnostic = Diagnostic.Create(
                    PureAnalyzer.SuggestRemovePureRule,
                    method.Identifier.GetLocation(),
                    methodSymbol.Name);
                diagnostics.Add(diagnostic);
            }

            return diagnostics;
        }

        private List<Diagnostic> AnalyzeProperty(PropertyDeclarationSyntax property, SemanticModel semanticModel, PureAnalyzerConfig config)
        {
            var diagnostics = new List<Diagnostic>();
            
            if (!config.AllowGetters || !config.EnableSuggestAdd)
                return diagnostics;

            // Check accessibility
            if (!IsTargetAccessibility(property, config))
                return diagnostics;

            var propertySymbol = semanticModel.GetDeclaredSymbol(property);
            if (propertySymbol == null || propertySymbol.IsWriteOnly)
                return diagnostics;

            // Check if readonly property should be marked as Pure
            var getter = property.AccessorList?.Accessors
                .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));

            if (getter?.Body == null && getter?.ExpressionBody == null)
                return diagnostics; // Auto property, no need to check

            bool hasPureAttribute = HasPureAttribute(property);
            bool shouldBePure = ShouldPropertyBePure(property, propertySymbol, semanticModel, config);

            if (!hasPureAttribute && shouldBePure)
            {
                var diagnostic = Diagnostic.Create(
                    PureAnalyzer.SuggestAddPureRule,
                    property.Identifier.GetLocation(),
                    propertySymbol.Name);
                diagnostics.Add(diagnostic);
            }

            return diagnostics;
        }

        private bool IsTargetAccessibility(MemberDeclarationSyntax member, PureAnalyzerConfig config)
        {
            var modifiers = member.Modifiers;
            
            if (modifiers.Any(SyntaxKind.PublicKeyword) && config.Accessibility.Contains("public"))
                return true;
            
            if (modifiers.Any(SyntaxKind.InternalKeyword) && config.Accessibility.Contains("internal"))
                return true;
            
            if (modifiers.Any(SyntaxKind.ProtectedKeyword) && config.Accessibility.Contains("protected"))
                return true;
            
            if (modifiers.Any(SyntaxKind.PrivateKeyword) && config.Accessibility.Contains("private"))
                return true;

            // Default accessibility check
            if (!modifiers.Any(m => m.IsKind(SyntaxKind.PublicKeyword) || 
                                    m.IsKind(SyntaxKind.InternalKeyword) || 
                                    m.IsKind(SyntaxKind.ProtectedKeyword) || 
                                    m.IsKind(SyntaxKind.PrivateKeyword)))
            {
                return config.Accessibility.Contains("internal") || config.Accessibility.Contains("private");
            }

            return false;
        }

        private bool HasPureAttribute(MemberDeclarationSyntax member)
        {
            return member.AttributeLists
                .SelectMany(al => al.Attributes)
                .Any(attr => 
                {
                    var name = attr.Name.ToString();
                    return name == "Pure" || 
                           name == "System.Diagnostics.Contracts.Pure" ||
                           name == "PureAttribute" ||
                           name == "System.Diagnostics.Contracts.PureAttribute";
                });
        }

        private bool ShouldMethodBePure(MethodDeclarationSyntax method, IMethodSymbol methodSymbol, 
            SemanticModel semanticModel, PureAnalyzerConfig config)
        {
            // Check return type
            if (methodSymbol.ReturnsVoid)
                return false;

            // Check method body
            if (method.Body == null && method.ExpressionBody == null)
                return false; // Abstract or interface method

            // Analyze if method body has side effects
            var hasSideEffects = HasSideEffects(method, semanticModel, config);
            return !hasSideEffects;
        }

        private bool ShouldPropertyBePure(PropertyDeclarationSyntax property, IPropertySymbol propertySymbol, 
            SemanticModel semanticModel, PureAnalyzerConfig config)
        {
            if (propertySymbol.IsWriteOnly)
                return false;

            var getter = property.AccessorList?.Accessors
                .FirstOrDefault(a => a.IsKind(SyntaxKind.GetAccessorDeclaration));

            if (getter == null)
                return false;

            // Analyze if getter has side effects
            var hasSideEffects = HasSideEffectsInAccessor(getter, semanticModel, config);
            return !hasSideEffects;
        }

        private bool HasSideEffects(MethodDeclarationSyntax method, SemanticModel semanticModel, PureAnalyzerConfig config)
        {
            var walker = new SideEffectWalker(semanticModel, config);
            
            if (method.Body != null)
                walker.Visit(method.Body);
            
            if (method.ExpressionBody != null)
                walker.Visit(method.ExpressionBody);
            
            return walker.HasSideEffects;
        }

        private bool HasSideEffectsInAccessor(AccessorDeclarationSyntax accessor, SemanticModel semanticModel, PureAnalyzerConfig config)
        {
            var walker = new SideEffectWalker(semanticModel, config);
            
            if (accessor.Body != null)
                walker.Visit(accessor.Body);
            
            if (accessor.ExpressionBody != null)
                walker.Visit(accessor.ExpressionBody);
            
            return walker.HasSideEffects;
        }
    }
}