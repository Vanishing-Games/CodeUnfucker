<!--
 * // -----------------------------------------------------------------------------
 * //  Copyright (c) 2025 Vanishing Games. All Rights Reserved.
 * @Author: VanishXiao
 * @Date: 2025-07-08 22:38:59
 * @LastEditTime: 2025-07-09 20:23:28
 * // -----------------------------------------------------------------------------
-->
# CodeUnfucker

## 简介

CodeUnfucker 是一个用于批量格式化、重构和清理 C# 代码的工具，支持命令行操作，适用于自动化脚本和持续集成环境。

## 支持的功能

- **代码格式化**：使用 CSharpier 或自定义格式化规则批量格式化 C# 源码。
- **Roslynator 重构**：批量应用 Roslynator 支持的常用重构。
- **移除 using**：批量移除未使用或指定的 using 指令。

## 安装与使用

### 依赖
- .NET 6.0 及以上

### 编译
```bash
# 在项目根目录下
 dotnet build
```

### 命令行用法

#### 1. 代码格式化
```bash
CodeUnfucker format <目录或文件路径>
```
- 使用自定义格式化规则批量格式化 C# 文件。

#### 2. 使用 CSharpier 格式化
```bash
CodeUnfucker csharpier <目录或文件路径>
```
- 使用 CSharpier 格式化 C# 文件。

#### 3. Roslynator 重构
```bash
CodeUnfucker roslynator <目录或文件路径>
```
- 批量应用 Roslynator 支持的重构。

#### 4. 移除 using 指令
```bash
CodeUnfucker rmusing <目录或文件路径>
```
- 批量移除未使用或指定的 using 指令。

## 配置

- `FormatterConfig.json`：格式化相关配置。
- `UsingRemoverConfig.json`：using 移除相关配置。

## 贡献

欢迎提交 issue 和 PR 以改进本项目。

## License

MIT 