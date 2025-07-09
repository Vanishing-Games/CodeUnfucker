<!--
 * // -----------------------------------------------------------------------------
 * //  Copyright (c) 2025 Vanishing Games. All Rights Reserved.
 * @Author: VanishXiao
 * @Date: 2025-07-08 22:38:59
 * @LastEditTime: 2025-07-09 20:23:28
 * // -----------------------------------------------------------------------------
-->
# CodeUnfucker

一个Unity项目的代码辅助工具，提供代码分析和格式化功能。

此仓库,使用Cursor完成.

## 功能

1. **代码分析** - 静态分析C#代码并输出结果到Unity控制台
2. **代码格式化** - 按规范重新排列类成员并添加Region宏
3. **代码重构** - 使用Roslynator进行智能代码重构和优化
4. **配置系统** - 通过JSON配置文件灵活控制所有功能

## 使用方法

### 构建

```bash
cd CodeUnfucker
dotnet build
```

### 命令行使用

```bash
# 分析代码
dotnet run -- analyze <目录路径> [--config <配置文件目录>]

# 格式化代码（使用内置格式化器）
dotnet run -- format <文件路径或目录路径> [--config <配置文件目录>]

# 格式化代码（使用CSharpier）
dotnet run -- csharpier <文件路径或目录路径> [--config <配置文件目录>]

# 重构代码（使用Roslynator）
dotnet run -- roslynator <文件路径或目录路径> [--config <配置文件目录>]

# 查看帮助
dotnet run -- help
```

#### 配置文件路径选项

程序会按以下优先级查找配置文件：
1. `--config` 参数指定的路径
2. 当前工作目录的 `Config` 文件夹
3. 程序所在目录的 `Config` 文件夹
4. 如果都不存在，使用默认配置

#### 示例

```bash
# 分析当前目录的Scripts文件夹
dotnet run -- analyze ./Scripts

# 使用自定义配置格式化代码
dotnet run -- format ./Scripts --config ./MyConfig

# 使用CSharpier格式化单个文件
dotnet run -- csharpier MyFile.cs

# 使用Roslynator重构代码
dotnet run -- roslynator ./Scripts --config ./MyConfig
```

### Unity Editor集成

1. **自动分析**: 每次编译完成后自动分析Assets/Scripts目录
2. **手动格式化**: 在Project窗口中选择文件/文件夹，然后选择菜单 `CodeUnfucker > Format Code`

## 配置系统

CodeUnfucker使用JSON配置文件来控制所有功能。配置文件位于 `Config/` 目录下。

### 配置文件

#### FormatterConfig.json - 格式化配置
控制代码格式化的所有行为：
- `MinLinesForRegion`: 当区域代码行数超过此值时添加Region宏 (默认: 15)
- `EnableRegionGeneration`: 是否启用Region生成 (默认: true)
- `CreateBackupFiles`: 是否创建备份文件 (默认: true)
- `BackupFileExtension`: 备份文件扩展名 (默认: ".backup")
- `FormatterType`: 选择格式化器类型 (可选值: "Built_in", "CSharpier", 默认: "Built_in")
- `UnityLifeCycleMethods`: Unity生命周期方法列表
- `RegionSettings`: Region名称和缩进设置

#### AnalyzerConfig.json - 分析配置
控制代码分析的所有行为：
- `EnableSyntaxAnalysis`: 是否启用语法分析 (默认: true)
- `EnableSemanticAnalysis`: 是否启用语义分析 (默认: true)
- `ShowReferencedAssemblies`: 是否显示引用的程序集 (默认: true)
- `ShowFileCount`: 是否显示文件数量 (默认: true)
- `FileFilters`: 文件过滤规则

#### RoslynatorConfig.json - 重构配置
控制 Roslynator 代码重构的所有行为：
- `EnableCodeRefactoring`: 是否启用代码重构 (默认: true)
- `CreateBackupFiles`: 是否创建备份文件 (默认: true)
- `BackupFileExtension`: 备份文件扩展名 (默认: ".roslynator.backup")
- `MinimumSeverity`: 最小重构严重级别 (默认: "Info")
- `EnabledRules`: 启用的重构规则列表
- `DisabledRules`: 禁用的重构规则列表
- `SeverityOverrides`: 特定规则的严重级别覆盖
- `ExcludedFiles`: 排除重构的文件模式

### 配置示例

```json
{
  "FormatterSettings": {
    "MinLinesForRegion": 10,
    "EnableRegionGeneration": true,
    "CreateBackupFiles": false
  },
  "RegionSettings": {
    "PublicRegionName": "公有成员",
    "PrivateRegionName": "私有成员"
  }
}
```

## 代码格式化功能

### 内置格式化器
格式化后的类成员顺序（可通过配置文件修改）：
1. **Public** - 公有成员
2. **Unity LifeCycle** - Unity生命周期方法 (Awake, Start, Update等)
3. **Protected** - 保护成员
4. **Private** - 私有成员
5. **Nested Classes** - 嵌套类
6. **Member Variables** - 成员变量

### CSharpier格式化器
- 使用业界标准的CSharpier进行代码格式化
- 提供一致的代码风格和更好的可读性
- 支持最新的C#语法特性
- 可通过配置文件选择或使用专用命令 `csharpier` 强制使用

### Unity生命周期方法

支持的Unity生命周期方法（可通过配置文件扩展）：
- `Awake`, `Start`, `Update`, `FixedUpdate`, `LateUpdate`
- `OnEnable`, `OnDisable`, `OnDestroy`
- `OnApplicationPause`, `OnApplicationFocus`, `OnApplicationQuit`
- `OnGUI`, `OnValidate`, `Reset`
- 所有碰撞和触发器事件方法
- `OnDrawGizmos`, `OnDrawGizmosSelected`

## 安全性

- 格式化时可选择性创建备份文件（通过配置控制）
- 支持单文件和批量格式化
- 格式化完成后会自动刷新Unity Asset Database
- 配置文件错误时自动使用默认配置
- 详细的错误日志和状态信息

## 测试

项目包含完整的单元测试套件，确保代码质量和功能稳定性。

### 快速运行测试

```bash
# 运行所有测试
dotnet test

# 使用测试脚本（推荐）
./run-tests.sh                # Linux/macOS
run-tests.bat                 # Windows

# 包含代码覆盖率
./run-tests.sh --coverage
```

### 测试覆盖

- ✅ **ConfigManager**: 配置文件加载、默认配置、错误处理
- ✅ **CodeFormatter**: 代码格式化、成员重组、Region生成  
- ✅ **CSharpierFormatter**: CSharpier集成、错误处理
- ✅ **Program**: 命令行参数解析、主要业务逻辑

### 测试工具

- **xUnit**: 测试框架
- **FluentAssertions**: 可读性更强的断言
- **Coverlet**: 代码覆盖率收集
- **ReportGenerator**: 覆盖率报告生成

详细的测试指南请查看 [TESTING.md](TESTING.md)。

## Roslynator 代码重构功能

### 智能代码重构
- 使用业界标准的 Roslynator 分析器进行代码重构
- 支持 100+ 种代码改进建议
- 可配置的重构规则和严重级别
- 自动应用代码优化和最佳实践

### 重构规则类别

#### 代码简化
- 内联不必要的变量
- 简化布尔表达式
- 移除冗余的类型转换
- 优化 LINQ 查询

#### 性能优化
- 优化字符串操作
- 减少装箱/拆箱操作
- 改进集合使用
- 异步编程最佳实践

#### 代码质量
- 添加文档注释
- 改进命名约定
- 强化异常处理
- 提升代码可读性

#### 现代 C# 特性
- 使用模式匹配
- 应用空值合并操作符
- 利用表达式语法
- 采用 var 关键字

### 配置示例

```json
{
  "RefactorSettings": {
    "MinimumSeverity": "Warning",
    "EnabledRules": [
      "RCS1036",  // Remove redundant empty line
      "RCS1090",  // Call 'ConfigureAwait(false)'
      "RCS1169"   // Make field read-only
    ],
    "SeverityOverrides": {
      "RCS1090": "Error"  // 异步规则设为错误级别
    }
  }
}
```

### 使用场景
- **代码审查前**: 自动应用编码标准
- **重构遗留代码**: 升级到现代 C# 语法
- **性能优化**: 发现并修复性能问题
- **团队协作**: 保持一致的代码风格

## 高级功能

- **灵活的配置路径**: 支持通过 `--config` 参数指定配置文件位置，解决部署后配置文件路径问题
- **多种格式化器**: 支持内置格式化器和CSharpier，满足不同需求
- **智能代码重构**: 集成 Roslynator 提供专业级代码优化建议
- **配置热重载**: 修改配置文件后无需重启
- **灵活的Region命名**: 可自定义所有Region的名称
- **可配置的Unity方法检测**: 支持添加新的Unity生命周期方法
- **详细的日志控制**: 可控制输出的详细程度
- **完整的测试套件**: 高覆盖率的单元测试确保代码质量 