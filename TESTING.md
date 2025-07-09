# CodeUnfucker 测试指南

本文档介绍如何运行 CodeUnfucker 项目的单元测试。

## 目录

- [快速开始](#快速开始)
- [测试结构](#测试结构)
- [运行测试](#运行测试)
- [代码覆盖率](#代码覆盖率)
- [CI/CD](#cicd)
- [测试最佳实践](#测试最佳实践)

## 快速开始

### 前提条件

- [.NET 9.0 SDK](https://dotnet.microsoft.com/download)
- 支持的操作系统：Windows、macOS、Linux

### 克隆项目并运行测试

```bash
# 克隆项目
git clone <repository-url>
cd CodeUnfucker

# 恢复依赖
dotnet restore

# 运行所有测试
dotnet test
```

## 测试结构

项目包含以下测试组件：

```
CodeUnfucker.Tests/
├── Tests/
│   ├── TestBase.cs              # 测试基类，提供通用工具方法
│   ├── ConfigManagerTests.cs    # ConfigManager 单元测试
│   ├── CodeFormatterTests.cs    # CodeFormatter 单元测试
│   ├── CSharpierFormatterTests.cs # CSharpierFormatter 单元测试
│   └── ProgramTests.cs          # Program 类单元测试
├── TestData/
│   ├── SampleClass.cs           # 示例C#代码
│   ├── TestFormatterConfig.json # 测试格式化配置
│   └── TestAnalyzerConfig.json  # 测试分析器配置
└── CodeUnfucker.Tests.csproj    # 测试项目文件
```

### 测试覆盖范围

- ✅ **ConfigManager**: 配置文件加载、默认配置、错误处理
- ✅ **CodeFormatter**: 代码格式化、成员重组、Region生成
- ✅ **CSharpierFormatter**: CSharpier集成、错误处理
- ✅ **Program**: 命令行参数解析、主要业务逻辑

## 运行测试

### 使用脚本运行（推荐）

#### Linux/macOS

```bash
# 基本测试
./run-tests.sh

# 包含代码覆盖率
./run-tests.sh --coverage

# 详细输出
./run-tests.sh --verbose

# 清理并运行
./run-tests.sh --clean --coverage
```

#### Windows

```cmd
REM 基本测试
run-tests.bat

REM 包含代码覆盖率
run-tests.bat --coverage

REM 详细输出
run-tests.bat --verbose

REM 清理并运行
run-tests.bat --clean --coverage
```

### 使用 dotnet CLI

```bash
# 运行所有测试
dotnet test

# 指定配置
dotnet test --configuration Release

# 详细输出
dotnet test --verbosity normal

# 收集代码覆盖率
dotnet test --collect:"XPlat Code Coverage"

# 运行特定测试类
dotnet test --filter "ClassName=ConfigManagerTests"

# 运行特定测试方法
dotnet test --filter "TestMethodName=GetFormatterConfig_ShouldReturnDefaultConfig_WhenNoConfigFileExists"
```

### 在 IDE 中运行

#### Visual Studio
1. 打开 `CodeUnfucker.sln`
2. 在 "测试资源管理器" 中查看所有测试
3. 右键点击测试运行或调试

#### Visual Studio Code
1. 安装 C# 扩展
2. 在命令面板中输入 "Test: Run All Tests"
3. 使用 `.NET Core Test Explorer` 扩展查看测试

#### JetBrains Rider
1. 打开项目
2. 在 "单元测试" 窗口中查看所有测试
3. 右键点击测试运行或调试

## 代码覆盖率

### 生成覆盖率报告

```bash
# 使用脚本生成（推荐）
./run-tests.sh --coverage

# 或手动生成
dotnet test --collect:"XPlat Code Coverage" --results-directory ./TestResults
dotnet tool install -g dotnet-reportgenerator-globaltool
reportgenerator -reports:"./TestResults/**/coverage.cobertura.xml" -targetdir:"./coverage" -reporttypes:"Html;TextSummary"
```

### 查看覆盖率

生成的报告位于 `./coverage/` 目录：

- **HTML 报告**: `./coverage/index.html` - 在浏览器中查看详细报告
- **摘要**: `./coverage/Summary.txt` - 控制台友好的摘要

### 覆盖率目标

| 组件 | 目标覆盖率 | 当前状态 |
|------|-----------|----------|
| ConfigManager | 90%+ | ✅ |
| CodeFormatter | 85%+ | ✅ |
| CSharpierFormatter | 80%+ | ✅ |
| Program (核心逻辑) | 80%+ | ✅ |

## CI/CD

项目配置了 GitHub Actions 工作流，在每次推送和 Pull Request 时自动运行测试。

### 工作流包括：

1. **多平台构建测试** (Ubuntu, Windows, macOS)
2. **单元测试执行**
3. **代码覆盖率收集**
4. **覆盖率报告上传** (Codecov)
5. **构建产物发布** (仅主分支)

### 查看 CI 结果

- 在 GitHub 仓库的 "Actions" 标签页查看工作流状态
- Pull Request 中会显示测试状态检查
- 覆盖率变化会在 PR 中显示

## 测试最佳实践

### 编写新测试

1. **继承 TestBase**: 获取通用测试工具
```csharp
public class NewFeatureTests : TestBase
{
    // 测试代码
}
```

2. **使用 AAA 模式**: Arrange, Act, Assert
```csharp
[Fact]
public void Method_Should_ExpectedBehavior_When_Condition()
{
    // Arrange
    var input = "test";
    
    // Act
    var result = MethodUnderTest(input);
    
    // Assert
    result.Should().Be("expected");
}
```

3. **使用 FluentAssertions**: 更可读的断言
```csharp
result.Should().NotBeNull();
result.Should().Be("expected");
result.Should().Contain("text");
```

4. **测试边界条件**: null、空字符串、异常情况
```csharp
[Theory]
[InlineData(null)]
[InlineData("")]
[InlineData("   ")]
public void Method_Should_HandleEdgeCases(string input)
{
    // 测试逻辑
}
```

### 测试隔离

- 每个测试使用独立的临时目录
- 测试完成后自动清理资源
- 不依赖测试执行顺序

### 性能考虑

- 使用 `CreateTempFile()` 创建测试文件
- 避免创建大量文件或长时间运行的测试
- 使用 Mock 对象替代重量级依赖

## 故障排除

### 常见问题

1. **测试失败**: 检查错误消息和堆栈跟踪
2. **文件权限错误**: 确保测试目录有写权限
3. **依赖项缺失**: 运行 `dotnet restore`
4. **覆盖率工具缺失**: 运行脚本会自动安装

### 调试测试

```bash
# 运行特定失败的测试
dotnet test --filter "TestMethodName" --verbosity normal

# 在调试模式下运行
dotnet test --configuration Debug
```

### 清理

```bash
# 清理构建输出和测试结果
./run-tests.sh --clean

# 手动清理
dotnet clean
rm -rf TestResults coverage
```

## 贡献

在添加新功能时，请：

1. 为新代码编写对应的单元测试
2. 确保所有测试通过
3. 维护代码覆盖率目标
4. 更新相关文档

有关更多信息，请参阅项目的贡献指南。 