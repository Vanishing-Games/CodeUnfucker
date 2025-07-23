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

## format 与 roslynator 的区别

- `format`（内置格式化器）：
  - 主要用于统一代码风格、结构和分区。
  - 支持自动分组成员（public/protected/private/Unity生命周期/变量/嵌套类），可按配置插入#region分区。
  - 可根据配置自动备份原文件。
  - 格式化时会保留注释、XML文档等原始信息。
  - 支持切换为CSharpier格式化（通过配置）。
  - 适合团队风格规范和结构统一。
- `roslynator`：
  - 主要用于自动化常见的代码重构和规范化（如加大括号、var转显式类型、条件表达式简化等）。
  - 不会调整成员顺序或插入#region，仅做语法和风格层面的重构。
  - 会为每个文件生成`.roslynator.backup`备份。
  - 适合批量消除低级错误和提升代码规范性。

## format 功能详细说明

- 递归扫描指定C#文件或目录，使用内置格式化器（CodeFormatter）对代码进行格式化。
- 支持自动分组成员（如public/protected/private/Unity生命周期/变量/嵌套类），可按配置自动插入#region分区。
- 可根据配置自动备份原文件。
- 格式化时会保留注释、XML文档等原始信息。
- 支持切换为CSharpier格式化（通过配置）。
- 适合统一代码风格、结构、分区，提升可读性和一致性。

## 功能示例

### 1. 代码格式化（真实效果）
**处理前：**
```csharp
using System;

public class SampleClass
{
    private int privateField;
    public string PublicProperty { get; set; } = string.Empty;
    protected bool protectedField;

    public void PublicMethod()
    {
        Console.WriteLine("Public method");
    }

    private void Start()
    {
        Console.WriteLine("Start");
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
        Console.WriteLine("Awake");
    }

    public SampleClass()
    {
        privateField = 0;
    }
} 
```
**处理后：**
```csharp
using System;

public class SampleClass
{
    public void PublicMethod()
    {
        Console.WriteLine("Public method");
    }

    public SampleClass()
    {
        privateField = 0;
    }

    #region Unity LifeCycle

    private void Start()
    {
        Console.WriteLine("Start");
    }

    private void Update()
    {
        // Update logic
    }

    private void Awake()
    {
        Console.WriteLine("Awake");
    }

    #endregion

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
    private int privateField;
    public string PublicProperty { get; set; } = string.Empty;
    protected bool protectedField;
} 
```

### 2. Roslynator重构（真实效果）
**处理前/处理后：**
（本例未发生变化，因代码已规范）
```csharp
using System;

public class SampleClass
{
    public void PublicMethod()
    {
        Console.WriteLine("Public method");
    }

    public SampleClass()
    {
        privateField = 0;
    }

    #region Unity LifeCycle

    private void Start()
    {
        Console.WriteLine("Start");
    }

    private void Update()
    {
        // Update logic
    }

    private void Awake()
    {
        Console.WriteLine("Awake");
    }

    #endregion

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
    private int privateField;
    public string PublicProperty { get; set; } = string.Empty;
    protected bool protectedField;
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

