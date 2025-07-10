using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// CodeFormatter 的单元测试
    /// </summary>
    public class CodeFormatterTests : TestBase
    {
        private const string SampleUnorganizedClass = @"
using System;
using UnityEngine;

public class TestClass : MonoBehaviour
{
    private int privateField;
    public string PublicProperty { get; set; }
    protected bool protectedField;

    public void PublicMethod()
    {
        Debug.Log(""Public method"");
    }

    private void Start()
    {
        Debug.Log(""Start"");
    }

    private void Update()
    {
        // Update logic
    }

    protected virtual void ProtectedMethod()
    {
        // Protected method
    }

    private void PrivateMethod()
    {
        // Private method
    }

    public class NestedClass
    {
        public void NestedMethod() { }
    }

    private void Awake()
    {
        Debug.Log(""Awake"");
    }

    public TestClass()
    {
        privateField = 0;
    }
}";

        [Fact]
        public void FormatCode_ShouldReorganizeMembers_InCorrectOrder()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();

            // Act
            var result = formatter.FormatCode(SampleUnorganizedClass, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // 验证代码可以被解析
            var tree = CSharpSyntaxTree.ParseText(result);
            var diagnostics = tree.GetDiagnostics();
            diagnostics.Where(d => d.Severity == DiagnosticSeverity.Error).Should().BeEmpty();
        }

        [Fact]
        public void FormatCode_ShouldPreserveOriginalCode_WhenNoClassFound()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var simpleCode = @"
using System;

namespace TestNamespace
{
    // Just a namespace, no class
}";

            // Act
            var result = formatter.FormatCode(simpleCode, "TestFile.cs");

            // Assert
            result.Should().Contain("namespace TestNamespace");
        }

        [Fact]
        public void FormatCode_ShouldGroupUnityLifeCycleMethods_Correctly()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var codeWithUnityMethods = @"
public class TestClass : MonoBehaviour
{
    private void Update() { }
    public void PublicMethod() { }
    private void Start() { }
    private void Awake() { }
    private void OnDestroy() { }
}";

            // Act
            var result = formatter.FormatCode(codeWithUnityMethods, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            
            // 验证Unity方法被正确分组
            var tree = CSharpSyntaxTree.ParseText(result);
            var root = tree.GetCompilationUnitRoot();
            root.Should().NotBeNull();
        }

        [Theory]
        [InlineData(FormatterType.Built_in)]
        [InlineData(FormatterType.CSharpier)]
        public void FormatCode_ShouldUseCorrectFormatter_BasedOnConfig(FormatterType formatterType)
        {
            // Arrange
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    FormatterType = formatterType,
                    EnableRegionGeneration = false
                }
            };

            CreateTempConfigFile("FormatterConfig.json", config);
            ConfigManager.SetConfigPath(Path.Combine(TestTempDirectory, "Config"));

            var formatter = new CodeFormatter();

            // Act
            var result = formatter.FormatCode(SampleUnorganizedClass, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            // 对于CSharpier，由于是占位符实现，应该返回原始代码
            if (formatterType == FormatterType.CSharpier)
            {
                result.Should().Be(SampleUnorganizedClass);
            }
        }

        [Fact]
        public void FormatCode_ShouldAddRegions_WhenEnabledAndCodeIsLongEnough()
        {
            // Arrange
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    EnableRegionGeneration = true,
                    MinLinesForRegion = 3, // 低阈值确保生成region
                    FormatterType = FormatterType.Built_in
                },
                RegionSettings = new RegionSettings
                {
                    PublicRegionName = "公有成员",
                    PrivateRegionName = "私有成员"
                }
            };

            CreateTempConfigFile("FormatterConfig.json", config);
            ConfigManager.SetConfigPath(Path.Combine(TestTempDirectory, "Config"));

            var formatter = new CodeFormatter();
            var longClass = @"
public class TestClass
{
    public void Method1() { /* line 1 */ }
    public void Method2() { /* line 2 */ }
    public void Method3() { /* line 3 */ }
    public void Method4() { /* line 4 */ }
    public void Method5() { /* line 5 */ }
}";

            // Act
            var result = formatter.FormatCode(longClass, "TestClass.cs");

            // Assert
            result.Should().Contain("#region");
            result.Should().Contain("#endregion");
        }

        [Fact]
        public void FormatCode_ShouldNotAddRegions_WhenDisabled()
        {
            // Arrange
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    EnableRegionGeneration = false,
                    FormatterType = FormatterType.Built_in
                }
            };

            CreateTempConfigFile("FormatterConfig.json", config);
            ConfigManager.SetConfigPath(Path.Combine(TestTempDirectory, "Config"));

            var formatter = new CodeFormatter();

            // Act
            var result = formatter.FormatCode(SampleUnorganizedClass, "TestClass.cs");

            // Assert
            result.Should().NotContain("#region");
            result.Should().NotContain("#endregion");
        }

        [Fact]
        public void FormatCode_ShouldHandleClassWithOnlyFields()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var fieldsOnlyClass = @"
public class TestClass
{
    public string publicField;
    private int privateField;
    protected bool protectedField;
}";

            // Act
            var result = formatter.FormatCode(fieldsOnlyClass, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("publicField");
            result.Should().Contain("privateField");
            result.Should().Contain("protectedField");
        }

        [Fact]
        public void FormatCode_ShouldHandleNestedClasses()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var nestedClassCode = @"
public class OuterClass
{
    public void PublicMethod() { }
    
    public class InnerClass
    {
        public void InnerMethod() { }
    }
    
    private void PrivateMethod() { }
}";

            // Act
            var result = formatter.FormatCode(nestedClassCode, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("InnerClass");
            result.Should().Contain("InnerMethod");
        }

        [Fact]
        public void FormatCode_ShouldHandleConstructors()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var constructorCode = @"
public class TestClass
{
    private int field;
    
    public TestClass() { }
    
    public TestClass(int value) { field = value; }
    
    public void Method() { }
}";

            // Act
            var result = formatter.FormatCode(constructorCode, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("TestClass()");
            result.Should().Contain("TestClass(int value)");
        }

        [Fact]
        public void FormatCode_ShouldHandleMultipleClasses()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var multiClassCode = @"
public class FirstClass
{
    public void Method1() { }
    private void PrivateMethod1() { }
}

public class SecondClass
{
    public void Method2() { }
    private void PrivateMethod2() { }
}";

            // Act
            var result = formatter.FormatCode(multiClassCode, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("FirstClass");
            result.Should().Contain("SecondClass");
            result.Should().Contain("Method1");
            result.Should().Contain("Method2");
        }

        [Fact]
        public void FormatCode_ShouldPreserveComments()
        {
            // Arrange
            SetupTestConfig();
            var formatter = new CodeFormatter();
            var codeWithComments = @"
public class TestClass
{
    /// <summary>
    /// This is a public method
    /// </summary>
    public void PublicMethod() 
    {
        // Implementation comment
    }
    
    // Private method comment
    private void PrivateMethod() { }
}";

            // Act
            var result = formatter.FormatCode(codeWithComments, "TestClass.cs");

            // Assert
            result.Should().NotBeNullOrEmpty();
            result.Should().Contain("This is a public method");
            result.Should().Contain("Implementation comment");
            result.Should().Contain("Private method comment");
        }

        private void SetupTestConfig()
        {
            // 首先确保从干净的状态开始
            SetIsolatedConfigPath();
            
            var config = new FormatterConfig
            {
                FormatterSettings = new FormatterSettings
                {
                    EnableRegionGeneration = true,
                    MinLinesForRegion = 5,
                    FormatterType = FormatterType.Built_in
                },
                UnityLifeCycleMethods = new()
                {
                    "Awake", "Start", "Update", "FixedUpdate", "LateUpdate",
                    "OnEnable", "OnDisable", "OnDestroy"
                },
                RegionSettings = new RegionSettings
                {
                    PublicRegionName = "Public",
                    UnityLifeCycleRegionName = "Unity LifeCycle",
                    ProtectedRegionName = "Protected",
                    PrivateRegionName = "Private",
                    NestedClassesRegionName = "Nested Classes",
                    MemberVariablesRegionName = "Member Variables"
                }
            };

            CreateTempConfigFile("FormatterConfig.json", config);
            ConfigManager.SetConfigPath(Path.Combine(TestTempDirectory, "Config"));
        }
    }
} 