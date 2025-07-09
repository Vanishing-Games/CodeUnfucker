using System;
using System.IO;
using System.Linq;
using Xunit;
using FluentAssertions;
using System.Text.RegularExpressions;

namespace CodeUnfucker.Tests
{
    /// <summary>
    /// 静态分析功能的单元测试
    /// </summary>
    public class StaticAnalysisTests : TestBase
    {
        [Fact]
        public void AnalyzeCode_ShouldDetectNamingConventionIssues()
        {
            // Arrange
            var testCode = @"
namespace TestNamespace
{
    public class badClassName  // 命名不规范
    {
        public int bad_field_name;  // 命名不规范
        public int BadProperty_Name { get; set; }  // 命名不规范
        
        public void method_name()  // 命名不规范
        {
        }
    }
}";
            var testFile = CreateTempFile("BadNaming.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);
            
            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().Contain("[命名约定]");
            output.Should().Contain("类名 'badClassName' 应使用 PascalCase 命名");
            output.Should().Contain("公有字段 'bad_field_name' 应使用 PascalCase 命名");
            output.Should().Contain("属性名 'BadProperty_Name' 应使用 PascalCase 命名");
            output.Should().Contain("方法名 'method_name' 应使用 PascalCase 命名");
        }

        [Fact]
        public void AnalyzeCode_ShouldDetectCodeComplexityIssues()
        {
            // Arrange
            var testCode = @"
using System;
namespace TestNamespace
{
    public class ComplexClass
    {
        public void VeryComplexMethod()
        {
            // 创建高复杂度方法
            for(int i = 0; i < 10; i++)
            {
                if(i > 5)
                {
                    for(int j = 0; j < 10; j++)
                    {
                        if(j > 5)
                        {
                            switch(i)
                            {
                                case 6:
                                    if(j == 6) 
                                    {
                                        Console.WriteLine(""Complex"");
                                    }
                                    else if(j == 7)
                                    {
                                        Console.WriteLine(""More complex"");
                                    }
                                    break;
                                case 7:
                                    try
                                    {
                                        Console.WriteLine(""Try block"");
                                    }
                                    catch(Exception ex)
                                    {
                                        Console.WriteLine(ex.Message);
                                    }
                                    break;
                            }
                        }
                    }
                }
            }
        }
    }
}";
            var testFile = CreateTempFile("ComplexCode.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().Contain("[复杂度]");
            output.Should().Contain("VeryComplexMethod");
            output.Should().Contain("超过阈值");
        }

        [Fact]
        public void AnalyzeCode_ShouldDetectUnusedVariables()
        {
            // Arrange
            var testCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        private int unusedField1;
        private string unusedField2;
        private readonly bool usedField = true;
        
        public void TestMethod()
        {
            if(usedField)
            {
                // 使用 usedField
            }
        }
    }
}";
            var testFile = CreateTempFile("UnusedVariables.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().Contain("[未使用变量]");
            output.Should().Contain("unusedField1");
            output.Should().Contain("unusedField2");
            output.Should().NotContain("usedField' 声明后从未使用");
        }

        [Fact]
        public void AnalyzeCode_ShouldDetectMissingDocumentationComments()
        {
            // Arrange
            var testCode = @"
namespace TestNamespace
{
    /// <summary>
    /// 有文档注释的类
    /// </summary>
    public class GoodClass
    {
        /// <summary>
        /// 有文档注释的方法
        /// </summary>
        public void GoodMethod()
        {
        }
    }
    
    // 没有文档注释的类
    public class BadClass
    {
        public int BadProperty { get; set; }
        
        public void BadMethod()
        {
        }
    }
}";
            var testFile = CreateTempFile("Documentation.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().Contain("[文档注释]");
            output.Should().Contain("类 'BadClass' 缺少 XML 文档注释");
            output.Should().Contain("属性 'BadProperty' 缺少 XML 文档注释");
            output.Should().Contain("方法 'BadMethod' 缺少 XML 文档注释");
            output.Should().NotContain("GoodClass' 缺少 XML 文档注释");
            output.Should().NotContain("GoodMethod' 缺少 XML 文档注释");
        }

        [Fact]
        public void AnalyzeCode_ShouldShowDiagnostics()
        {
            // Arrange
            var testCode = @"
using System;
using System.Linq;  // 未使用的 using
namespace TestNamespace
{
    public class TestClass
    {
        public void TestMethod()
        {
            // 编译错误：未定义的变量
            undefinedVariable = 123;
            
            // 编译警告：未使用的变量
            int unusedLocal = 456;
        }
    }
}";
            var testFile = CreateTempFile("Diagnostics.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().Contain("[Error]");
            output.Should().Contain("[Warning]");
            output.Should().Contain("[Hidden]");
            output.Should().Contain("CS8019");  // 未使用的 using
        }

        [Fact]
        public void AnalyzeCode_ShouldProvideAnalysisSummary()
        {
            // Arrange
            var testCode = @"
namespace TestNamespace
{
    public class testclass  // 命名问题
    {
        private int unusedVar;  // 未使用变量
        
        public void badMethod() // 缺少文档注释和命名问题
        {
        }
    }
}";
            var testFile = CreateTempFile("Summary.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().Contain("=== 分析结果摘要 ===");
            output.Should().Contain("分析文件数:");
            output.Should().Contain("发现问题数:");
            output.Should().Contain("问题分类:");
            output.Should().Contain("分析耗时:");
        }

        [Fact]
        public void AnalyzeCode_ShouldRespectConfigurationSettings()
        {
            // Arrange - 创建禁用某些规则的配置
            var customConfig = new AnalyzerConfig
            {
                AnalyzerSettings = new AnalyzerSettings
                {
                    EnableSyntaxAnalysis = true,
                    EnableSemanticAnalysis = true,
                    EnableDiagnostics = true
                },
                StaticAnalysisRules = new StaticAnalysisRules
                {
                    CheckNamingConventions = false,  // 禁用命名检查
                    CheckCodeComplexity = false,
                    CheckUnusedVariables = true,
                    CheckDocumentationComments = false
                }
            };

            CreateTempConfigFile("AnalyzerConfig.json", customConfig);

            var testCode = @"
namespace TestNamespace
{
    public class badClassName  // 应该不检查命名
    {
        private int unusedField;  // 应该检查未使用
        
        public void bad_method_name() // 应该不检查命名和文档
        {
        }
    }
}";
            var testFile = CreateTempFile("ConfigTest.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().NotContain("[命名约定]");
            output.Should().NotContain("[文档注释]");
            output.Should().Contain("[未使用变量]");
            output.Should().Contain("unusedField");
        }

        [Fact]
        public void AnalyzeCode_ShouldHandleUnityLifeCycleMethods()
        {
            // Arrange
            var testCode = @"
using UnityEngine;
namespace TestNamespace
{
    public class UnityClass : MonoBehaviour
    {
        void Awake()  // Unity 生命周期方法，不应报命名错误
        {
        }
        
        void Start()  // Unity 生命周期方法
        {
        }
        
        void Update()  // Unity 生命周期方法
        {
        }
        
        void custom_method()  // 自定义方法，应报命名错误
        {
        }
    }
}";
            var testFile = CreateTempFile("UnityMethods.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().NotContain("方法名 'Awake' 应使用 PascalCase 命名");
            output.Should().NotContain("方法名 'Start' 应使用 PascalCase 命名");
            output.Should().NotContain("方法名 'Update' 应使用 PascalCase 命名");
            output.Should().Contain("方法名 'custom_method' 应使用 PascalCase 命名");
        }

        [Fact]
        public void AnalyzeCode_ShouldHandleEmptyDirectory()
        {
            // Arrange
            var emptyDir = TestTempDirectory;

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", emptyDir }));

            // Assert
            output.Should().Contain("未找到任何 .cs 文件");
        }

        [Fact]
        public void AnalyzeCode_ShouldHandlePrivateFieldNamingCorrectly()
        {
            // Arrange
            var testCode = @"
namespace TestNamespace
{
    public class TestClass
    {
        private int _goodPrivateField;      // 正确的私有字段命名
        private int goodCamelCase;          // 正确的私有字段命名
        private int BadPrivateField;        // 错误的私有字段命名
        public int GoodPublicField;         // 正确的公有字段命名
        public int bad_public_field;        // 错误的公有字段命名
    }
}";
            var testFile = CreateTempFile("FieldNaming.cs", testCode);
            var testDir = Path.GetDirectoryName(testFile);

            var program = new Program();
            var output = CaptureConsoleOutput(() => program.Run(new[] { "analyze", testDir! }));

            // Assert
            output.Should().NotContain("'_goodPrivateField' 应使用");
            output.Should().NotContain("'goodCamelCase' 应使用");
            output.Should().NotContain("'GoodPublicField' 应使用");
            output.Should().Contain("私有字段 'BadPrivateField' 应使用 camelCase 或 _camelCase 命名");
            output.Should().Contain("公有字段 'bad_public_field' 应使用 PascalCase 命名");
        }

        private string CaptureConsoleOutput(Action action)
        {
            var originalOut = Console.Out;
            var originalError = Console.Error;
            try
            {
                using var stringWriter = new StringWriter();
                Console.SetOut(stringWriter);
                Console.SetError(stringWriter);
                
                action();
                
                return stringWriter.ToString();
            }
            finally
            {
                Console.SetOut(originalOut);
                Console.SetError(originalError);
            }
        }
    }
}