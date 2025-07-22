# 测试项目说明 | Test Project Guide

## 测试环境 | Test Environment

- 需要 .NET 8.0 及以上
- 支持 Windows、Linux、macOS

- .NET 8.0 or above required
- Supports Windows, Linux, macOS

## 测试内容 | Test Coverage

- 代码格式化（format 命令）
- CSharpier 格式化（csharpier 命令）
- Roslynator 重构（roslynator 命令）
- 移除未使用 using（rmusing 命令）
- 配置文件隔离与边界用例

- Code formatting (format command)
- CSharpier formatting (csharpier command)
- Roslynator refactoring (roslynator command)
- Remove unused using (rmusing command)
- Config isolation and edge cases

## 如何运行测试 | How to Run Tests

```bash
dotnet test
```

## 贡献测试用例 | Contribute Test Cases

欢迎补充更多边界用例和实际项目样例。

Feel free to add more edge cases and real-world samples.

## 测试隔离与架构 | Test Isolation & Architecture

- 测试项目采用独立服务实例，避免单例污染，确保每个测试互不影响。
- 详细说明见 `../Docs/TEST_FAILURE_FIX.md`。

- The test project uses independent service instances to avoid singleton pollution and ensure test isolation.
- See `../Docs/TEST_FAILURE_FIX.md` for details.

## 其他文档 | More Docs

- [测试说明 | TESTING.md](../Docs/TESTING.md)
- [失败修复报告 | TEST_FAILURE_FIX.md](../Docs/TEST_FAILURE_FIX.md)
- [验证状态 | VERIFICATION_STATUS.md](../Docs/VERIFICATION_STATUS.md)

