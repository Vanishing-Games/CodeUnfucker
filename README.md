# CodeUnfucker

## 项目简介 | Project Introduction

CodeUnfucker 是一个面向 C# 项目的自动化代码格式化、重构和清理工具，支持多种格式化器和命令行操作，适用于提升代码质量和一致性。

CodeUnfucker is an automated code formatter, refactoring, and cleanup tool for C# projects. It supports multiple formatters and command-line operations to improve code quality and consistency.

## 主要功能 | Main Features

- 内置格式化器自动格式化 C# 代码
- 集成 CSharpier 格式化
- 支持 Roslynator 自动重构
- 一键移除未使用的 using 语句
- 命令行批量处理文件或目录

- Built-in formatter for C# code
- Integrated CSharpier formatting
- Roslynator-based code refactoring
- Remove unused using statements
- Batch process files or directories via CLI

## 安装与依赖 | Installation & Dependencies

- .NET 8.0 及以上
- 依赖包见 `CodeUnfucker.csproj`

- .NET 8.0 or above
- See `CodeUnfucker.csproj` for dependencies

## 快速开始 | Quick Start

```bash
dotnet build
# 格式化代码
dotnet run -- format ./src
# 使用CSharpier格式化
dotnet run -- csharpier ./src
# 移除未使用using
dotnet run -- rmusing ./src
# Roslynator重构
dotnet run -- roslynator ./src
# 查看帮助
dotnet run -- --help
```

## 命令行用法 | CLI Usage

- `format <path>`: 使用内置格式化器格式化指定文件或目录
- `csharpier <path>`: 使用CSharpier格式化代码
- `rmusing <path>`: 移除未使用的using语句
- `roslynator <path>`: 使用Roslynator进行代码重构

- `format <path>`: Format code with built-in formatter
- `csharpier <path>`: Format code with CSharpier
- `rmusing <path>`: Remove unused using statements
- `roslynator <path>`: Refactor code with Roslynator

## 项目结构 | Project Structure

- `Src/` 主程序源码
- `Config/` 配置文件
- `Tests/` 测试项目及数据
- `README.md` 项目说明

- `Src/` Main source code
- `Config/` Configuration files
- `Tests/` Test project and data
- `README.md` Project documentation

## 贡献与测试 | Contribution & Testing

欢迎提交PR和Issue，完善功能和文档。

- 运行所有测试：

```bash
dotnet test
```

详细测试说明见 `Tests/Docs/TESTING.md`。

---

Welcome to contribute via PR or Issue.

- Run all tests:

```bash
dotnet test
```

See `Tests/Docs/TESTING.md` for detailed test instructions.

