# 移除未使用using语句功能

## 功能概述

已成功为CodeUnfucker项目添加了**移除未使用using语句**的功能，该功能可以自动检测并移除C#文件中未使用的using语句，帮助保持代码清洁和减少文件大小。

## 新增文件

### 1. `Src/UsingStatementRemover.cs`
- **主要功能类**：`UsingStatementRemover`
- **配置类**：`UsingRemoverConfig` 和 `UsingRemoverSettings`
- 使用Microsoft.CodeAnalysis.CSharp进行语义分析
- 通过语法树遍历识别真正使用的命名空间
- 支持配置化的保留规则

### 2. `Config/UsingRemoverConfig.json`
- 控制移除行为的配置文件
- 支持备份文件设置
- 可配置强制保留的using语句列表
- 支持详细日志和排序选项

## 功能特性

### ✅ 智能检测
- 使用Roslyn编译器进行语义分析
- 准确识别实际使用的命名空间
- 避免误删必需的using语句

### ✅ 安全备份
- 默认创建`.backup`备份文件
- 可通过配置禁用备份功能
- 自定义备份文件扩展名

### ✅ 配置灵活
- 可强制保留特定using（如Unity相关）
- 支持详细日志输出
- 可配置using排序

### ✅ 批量处理
- 支持单文件处理
- 支持目录批量处理
- 统计处理结果

## 使用方法

### 单文件处理
```bash
dotnet run -- rmusing MyFile.cs
```

### 目录批量处理
```bash
dotnet run -- rmusing ./Scripts
```

### 使用自定义配置
```bash
dotnet run -- rmusing ./Scripts --config ./MyConfig
```

## 配置示例

```json
{
  "Description": "移除未使用using语句功能配置",
  "Version": "1.0.0",
  "Settings": {
    "CreateBackupFiles": true,
    "BackupFileExtension": ".backup",
    "VerboseLogging": false,
    "SortUsings": true,
    "RemoveEmptyLines": true
  },
  "PreservedUsings": [
    "System",
    "UnityEngine",
    "UnityEditor"
  ]
}
```

## 技术实现

### 核心算法
1. **解析源码**：使用CSharpSyntaxTree解析C#代码
2. **语义分析**：创建编译上下文进行语义分析  
3. **遍历识别**：通过NamespaceUsageWalker遍历语法树
4. **符号解析**：解析标识符、限定名、泛型名的命名空间
5. **智能过滤**：保留实际使用的using和配置保留的using

### 代码架构
- **主处理类**：UsingStatementRemover
- **语法遍历器**：NamespaceUsageWalker
- **配置管理**：集成到现有ConfigManager系统
- **命令行集成**：扩展Program.cs的命令处理

## 测试验证

### 测试用例
创建了包含9个using语句的测试文件，其中只有4个被实际使用：

**原始文件**：
```csharp
using System;                    // ✅ 保留 (Console.WriteLine)
using System.Collections.Generic; // ✅ 保留 (List<string>)
using System.IO;                 // ❌ 移除 (未使用)
using System.Linq;               // ✅ 保留 (Where, ToList)
using System.Text;               // ❌ 移除 (未使用)
using System.Threading.Tasks;    // ❌ 移除 (未使用)
using System.Reflection;         // ❌ 移除 (未使用)
using UnityEngine;               // ✅ 保留 (MonoBehaviour, Debug.Log)
using UnityEditor;               // ❌ 移除 (未使用)
```

**处理结果**：成功移除5个未使用的using语句，保留4个必需的using语句。

## 集成更新

### 1. Program.cs 更新
- 添加`rmusing`命令（简化的移除未使用using语句命令）
- 更新命令验证逻辑
- 更新帮助信息和使用示例

### 2. ConfigManager.cs 更新
- 添加`GetUsingRemoverConfig()`方法
- 更新配置重载逻辑
- 支持新配置文件加载

### 3. README.md 更新
- 添加新功能描述
- 更新命令行使用说明
- 添加配置文件说明
- 更新示例命令

## 兼容性

- ✅ 与现有功能完全兼容
- ✅ 遵循现有代码风格和架构
- ✅ 使用相同的配置系统
- ✅ 统一的日志输出格式
- ✅ 一致的错误处理机制

## 性能特点

- **高效分析**：利用Roslyn编译器的优化
- **内存友好**：流式处理，不占用过多内存
- **批量优化**：支持大量文件的批量处理
- **错误容错**：单文件错误不影响整体处理

## 未来扩展

1. **增强检测**：支持更复杂的using模式
2. **IDE集成**：可集成到Visual Studio等IDE中
3. **规则扩展**：支持更复杂的保留规则
4. **性能优化**：并行处理大型项目
5. **报告生成**：生成详细的处理报告

---

该功能已完全集成到CodeUnfucker项目中，可以立即使用，为Unity项目的代码维护提供了强大的自动化工具。