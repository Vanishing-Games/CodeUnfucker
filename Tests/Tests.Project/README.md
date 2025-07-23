# 测试项目说明

## 测试环境

- 需要 .NET 8.0 及以上
- 支持 Windows、Linux、macOS

## 测试内容

- 代码格式化（format 命令）
- CSharpier 格式化（csharpier 命令）
- Roslynator 重构（roslynator 命令）
- 移除未使用 using（rmusing 命令）
- 配置文件隔离与边界用例

## 如何运行测试

```bash
dotnet test
```

## 贡献测试用例

欢迎补充更多边界用例和实际项目样例。

## 测试隔离与架构

- 测试项目采用独立服务实例，避免单例污染，确保每个测试互不影响。
- 详细说明见 `../Docs/TEST_FAILURE_FIX.md`

## 其他文档

- [测试说明 | TESTING.md](../Docs/TESTING.md)
- [失败修复报告 | TEST_FAILURE_FIX.md](../Docs/TEST_FAILURE_FIX.md)
- [验证状态 | VERIFICATION_STATUS.md](../Docs/VERIFICATION_STATUS.md)

