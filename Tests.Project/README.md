# CodeUnfucker 测试项目

## 简介

本目录包含 CodeUnfucker 的所有自动化测试用例，覆盖命令行解析、服务层、命令层、配置管理、格式化、重构等核心功能，确保主项目的正确性和可维护性。

## 测试环境
- .NET 8.0 及以上
- 推荐平台：Windows / Linux / macOS

## 依赖
- [xUnit](https://xunit.net/) 2.4.2
- [FluentAssertions](https://fluentassertions.com/) 6.12.0
- [Moq](https://github.com/moq/moq4) 4.20.69
- [Microsoft.CodeAnalysis.CSharp](https://www.nuget.org/packages/Microsoft.CodeAnalysis.CSharp) 4.11.0
- [coverlet.collector](https://github.com/coverlet-coverage/coverlet) 6.0.0

## 运行测试

```bash
# 在项目根目录下
 dotnet test
```

或带覆盖率：

```bash
./run-tests.sh --coverage
```

## 测试内容

- **命令行解析**：验证各种命令、参数、边界情况的解析正确性。
- **服务层**：测试日志、文件服务、依赖注入等基础服务的行为。
- **命令层**：测试各命令（格式化、重构、移除using等）的执行逻辑和参数校验。
- **配置管理**：测试配置文件的加载、热重载、异常处理。
- **格式化与重构**：验证格式化、重构、using移除等核心功能的正确性。
- **集成用例**：模拟真实命令行调用，验证端到端流程。
- **边界与异常**：覆盖无效输入、异常路径、极端场景。

## 贡献测试用例

欢迎补充更多边界用例和实际项目样例。请确保：
- 新增测试用例能复现实际问题或覆盖未测分支
- 测试命名清晰，Arrange/Act/Assert 分明
- 保持测试隔离，避免状态污染

## 目录结构

- `CodeFormatterTests.cs`      代码格式化核心逻辑测试
- `CSharpierFormatterTests.cs` CSharpier格式化相关测试
- `ConfigManagerTests.cs`      配置管理相关测试
- `ProgramTests.cs`            命令行与集成流程测试
- `TestBase.cs`                测试基类与通用工具
- `SimpleTest.cs`              最基础的样例测试
- `TestData/`                  测试用数据文件

## License

MIT