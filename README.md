# CodeUnfucker

## 项目简介

CodeUnfucker 是一个面向 C# 项目的自动化代码格式化、重构和清理工具，支持多种格式化器和命令行操作，适用于提升代码质量和一致性。

## 主要功能

- 内置格式化器自动格式化 C# 代码
- 集成 CSharpier 格式化
- 支持 Roslynator 自动重构
- 一键移除未使用的 using 语句
- 命令行批量处理文件或目录

## 安装与依赖

- .NET 8.0 及以上
- 依赖包见 `CodeUnfucker.csproj`

## 快速开始

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

## 命令行用法

- `format <path>`: 使用内置格式化器格式化指定文件或目录
- `csharpier <path>`: 使用CSharpier格式化代码
- `rmusing <path>`: 移除未使用的using语句
- `roslynator <path>`: 使用Roslynator进行代码重构

## 功能示例

### 1. 代码格式化
**处理前：**
```csharp
public   class  Demo{public void Test( ) {Console.WriteLine("Hello" );}}
```
**处理后：**
```csharp
public class Demo
{
    public void Test()
    {
        Console.WriteLine("Hello");
    }
}
```

### 2. 移除未使用的using
**处理前：**
```csharp
using System;
using System.Text;

namespace DemoApp {
    class Program {
        static void Main() {
            Console.WriteLine("Hello");
        }
    }
}
```
**处理后：**
```csharp
using System;

namespace DemoApp {
    class Program {
        static void Main() {
            Console.WriteLine("Hello");
        }
    }
}
```

### 3. Roslynator重构
**处理前：**
```csharp
if (flag == true) {
    DoSomething();
}
```
**处理后：**
```csharp
if (flag) {
    DoSomething();
}
```

## 项目结构

- `Src/` 主程序源码
- `Config/` 配置文件
- `Tests/` 测试项目及数据
- `README.md` 项目说明

## 贡献与测试

欢迎提交PR和Issue，完善功能和文档。

- 运行所有测试：

```bash
dotnet test
```

详细测试说明见 `Tests/Docs/TESTING.md`。

