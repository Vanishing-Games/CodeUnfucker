# CodeUnfucker 项目分析报告

## 📊 当前状态分析

### ✅ 已实现功能
1. **代码格式化** - 内置格式化器和 CSharpier 集成
2. **Using语句清理** - 自动移除未使用的 using 语句  
3. **配置系统** - 基于 JSON 的灵活配置管理
4. **基础代码分析** - Roslyn 语法树解析和编译对象创建
5. **单元测试** - 基本的测试覆盖

### ❌ 发现的问题

#### 1. 测试失败
- `GetFormatterConfig_ShouldParseFormatterType_Correctly` - 枚举类型解析失败
- `ReloadConfigs_ShouldClearCachedConfigs` - 配置缓存清理问题

#### 2. 静态分析功能不足
- 当前的 `analyze` 命令只做语法树解析，没有实际的分析规则
- 缺少具体的诊断和建议功能
- 没有实现用户要求的专项分析器

#### 3. 缺失的核心功能
- 无 Pure 属性自动添加/移除功能
- 无 Unity Update 方法堆内存分配检测
- 无 Roslynator 重构功能

## 🎯 新功能需求实现计划

### 1. Pure 属性分析器 (UNITY0009/UNITY0010)

**功能设计：**
- **UNITY0009**: 检测可以标记为 `[Pure]` 的方法
  - 公有/内部方法，有返回值，无副作用
  - 仅包含算法逻辑、return 语句或 LINQ 表达式
  - 不调用非 pure 方法，不进行状态修改

- **UNITY0010**: 检测错误标记 `[Pure]` 的方法
  - 包含 `Debug.Log()`、赋值操作、事件触发等副作用
  - 调用 Unity 引擎 API

**实现方案：**
- 创建 `PureMethodAnalyzer` 类
- 实现语法树访问器分析方法体
- 提供 CodeFixProvider 自动修复

### 2. Unity Update 堆内存分配检测器 (UNITY0001)

**功能设计：**
- 检测 `Update()`, `LateUpdate()`, `FixedUpdate()`, `OnGUI()` 方法
- 识别堆内存分配操作：
  - `new` 关键字（排除值类型）
  - LINQ 扩展方法
  - 字符串拼接和插值
  - 隐式闭包
  - 集合初始化

**实现方案：**
- 创建 `UnityUpdateHeapAllocationAnalyzer` 类
- 检测继承自 `UnityEngine.MonoBehaviour` 的类
- 分析特定方法中的堆分配模式

### 3. Roslynator 重构功能

**功能设计：**
- 新增 `roslynator` 命令
- 支持单文件和目录批量处理
- 集成现有命令行界面

**实现方案：**
- 扩展 Program.cs 添加 roslynator 命令处理
- 创建 RoslynatorRefactorer 类
- 配置文件支持重构规则设置

## 🔧 修复计划

### 1. 修复现有测试
- 修复枚举解析问题
- 修复配置缓存清理逻辑

### 2. 增强测试覆盖
- 为新分析器添加完整测试用例
- 测试 Pure 属性检测的各种场景
- 测试 Unity Update 堆内存分配检测
- 测试边界条件和错误处理

### 3. 完善配置系统
- 添加新分析器的配置选项
- 支持分析器启用/禁用
- 支持自定义排除规则

## 📋 实施优先级

### 高优先级
1. 修复现有测试失败
2. 实现 Pure 属性分析器
3. 实现 Unity Update 堆内存分配检测器

### 中优先级  
1. 添加 Roslynator 重构功能
2. 完善测试覆盖
3. 优化配置系统

### 低优先级
1. 性能优化
2. 文档完善
3. 用户体验改进

## 🎁 预期收益

1. **提升代码质量** - 自动检测性能和纯度问题
2. **减少手动工作** - 自动添加/移除属性
3. **避免Unity性能陷阱** - 及时发现堆分配问题
4. **标准化代码** - 统一的重构和格式化