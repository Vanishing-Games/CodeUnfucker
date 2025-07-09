# Pure 属性分析器

## 📝 概述

Pure 属性分析器是 CodeUnfucker 的一个新功能，用于自动检测应该标记为 `[Pure]` 的方法和属性，同时识别错误标记的情况。

`[Pure]` 是来自 `System.Diagnostics.Contracts` 命名空间的属性，表示方法无副作用，不会改变程序状态。

## 🎯 功能特性

### 1. 建议添加 [Pure] 属性

自动识别以下类型的方法和属性，建议添加 `[Pure]` 属性：

- ✅ **纯计算方法**：只进行数学运算、字符串处理等无副作用操作
- ✅ **LINQ 查询方法**：使用 LINQ 进行数据转换的方法
- ✅ **递归函数**：纯函数式递归计算
- ✅ **只读属性**：getter 中无副作用的属性
- ✅ **条件表达式方法**：使用三元运算符等的条件计算

### 2. 建议移除 [Pure] 属性

检测已标记为 `[Pure]` 但包含副作用的方法：

- ⚠️ **字段/属性修改**：改变对象状态的操作
- ⚠️ **I/O 操作**：文件读写、网络请求等
- ⚠️ **Unity API 调用**：Debug.Log、Random、Time 等
- ⚠️ **void 方法调用**：可能有副作用的方法调用
- ⚠️ **事件触发**：事件发布、委托调用等

## 🚀 使用方法

### 命令行使用

```bash
# 分析指定目录的 Pure 属性建议
dotnet run -- pure ./Scripts

# 使用自定义配置
dotnet run -- pure ./Scripts --config ./MyConfig
```

### 输出示例

```
[INFO] 开始分析 [Pure] 属性建议，扫描路径: ./Scripts
[INFO] 找到 15 个 .cs 文件
✅ PlayerController.cs(25,17): 方法 'CalculateHealth' 无副作用且有返回值，建议添加 [Pure] 属性
✅ MathUtils.cs(10,24): 方法 'GetDistance' 无副作用且有返回值，建议添加 [Pure] 属性
⚠️ GameManager.cs(45,16): 方法 'WronglyMarkedMethod' 包含副作用，不应标记为 [Pure] 属性

[INFO] 分析完成！
[INFO] 建议添加 [Pure]: 8 个方法
[INFO] 建议移除 [Pure]: 2 个方法
```

## ⚙️ 配置选项

配置文件：`Config/PureAnalyzerConfig.json`

```json
{
  "PureAnalyzerSettings": {
    "Accessibility": ["public", "internal"],
    "ExcludePartial": true,
    "AllowGetters": true,
    "EnableSuggestAdd": true,
    "EnableSuggestRemove": true,
    "ExcludedNamespaces": [
      "UnityEngine",
      "Unity",
      "UnityEditor"
    ],
    "ExcludedMethods": [
      "Debug.Log",
      "Debug.LogWarning", 
      "Debug.LogError",
      "Console.WriteLine"
    ],
    "UnityApiPatterns": [
      "transform\\.",
      "gameObject\\.",
      "Time\\.",
      "Input\\.",
      "Random\\."
    ]
  }
}
```

### 配置参数说明

| 参数 | 默认值 | 说明 |
|------|--------|------|
| `Accessibility` | `["public", "internal"]` | 需要检查的可见性级别 |
| `ExcludePartial` | `true` | 是否排除 partial 方法 |
| `AllowGetters` | `true` | 是否检查属性的 getter |
| `EnableSuggestAdd` | `true` | 是否启用添加建议 |
| `EnableSuggestRemove` | `true` | 是否启用移除建议 |
| `ExcludedNamespaces` | Unity 相关 | 排除的命名空间 |
| `ExcludedMethods` | 日志方法等 | 排除的具体方法 |
| `UnityApiPatterns` | Unity API 模式 | Unity API 正则表达式 |

## 💡 示例代码

### ✅ 建议添加 [Pure] 的情况

```csharp
// 数学计算
public int CalculateHealth(int baseHp, int armor)
{
    return baseHp + armor * 5;
}

// 字符串处理
public string FormatPlayerName(string firstName, string lastName)
{
    return $"{firstName} {lastName}".Trim();
}

// LINQ 查询
public int[] FilterEvenNumbers(int[] numbers)
{
    return numbers.Where(x => x % 2 == 0).ToArray();
}

// 递归函数
public int Factorial(int n)
{
    if (n <= 1) return 1;
    return n * Factorial(n - 1);
}

// 只读属性
public string FullName 
{ 
    get { return $"{FirstName} {LastName}"; }
}
```

### ❌ 不应该标记为 [Pure] 的情况

```csharp
// 修改字段
public int IncrementCounter()
{
    _counter++;  // 副作用
    return _counter;
}

// Unity API 调用
public Vector3 GetRandomPosition()
{
    return new Vector3(Random.Range(0f, 10f), 0f, 0f);  // 副作用
}

// 日志输出
public int CalculateWithLogging(int a, int b)
{
    Debug.Log($"Calculating {a} + {b}");  // 副作用
    return a + b;
}

// void 返回类型
public void UpdateHealth(int newHealth)
{
    _health = newHealth;  // void 方法不能标记为 Pure
}
```

### ⚠️ 错误标记需要移除的情况

```csharp
[Pure]  // ❌ 错误的标记
public int WronglyMarkedMethod()
{
    Debug.Log("This has side effects!");  // 包含副作用
    return 42;
}

[Pure]  // ❌ 错误的标记
public int AnotherWrongPure(int value)
{
    _field = value;  // 修改状态
    return value;
}
```

## 🔧 代码修复器

分析器配合代码修复器提供自动修复功能：

### 自动添加 [Pure] 属性

- 自动添加 `[Pure]` 属性到方法或属性上方
- 自动添加 `using System.Diagnostics.Contracts;` 引用（如果不存在）

### 自动移除 [Pure] 属性

- 移除错误的 `[Pure]` 属性
- 保留其他属性（如 `[Obsolete]` 等）
- 如果是唯一属性，移除整个属性列表

## 📋 诊断规则

| 规则 ID | 严重性 | 说明 |
|---------|--------|------|
| `UNITY0009` | Info | 建议添加 [Pure] 属性 |
| `UNITY0010` | Warning | 建议移除 [Pure] 属性 |

## 🧪 测试覆盖

项目包含完整的单元测试覆盖：

- ✅ **PureAnalyzer** 核心分析逻辑测试
- ✅ **PureCodeFixProvider** 代码修复器测试
- ✅ **配置系统** 配置加载和验证测试
- ✅ **边界情况** 各种复杂场景测试

运行测试：

```bash
dotnet test --filter "PureAnalyzer"
```

## 🎯 最佳实践

1. **定期运行分析**：在代码审查前运行 Pure 分析器
2. **配置调优**：根据项目需求调整配置参数
3. **渐进式应用**：从 public 方法开始，逐步扩展到其他可见性
4. **团队规范**：建立团队内的 [Pure] 属性使用规范
5. **CI 集成**：将分析器集成到持续集成流程中

## 🔗 相关资源

- [.NET Code Contracts 文档](https://docs.microsoft.com/en-us/dotnet/framework/debug-trace-profile/code-contracts)
- [Pure 属性 MSDN 文档](https://docs.microsoft.com/en-us/dotnet/api/system.diagnostics.contracts.pureattribute)
- [Roslyn 分析器开发指南](https://docs.microsoft.com/en-us/dotnet/csharp/roslyn-sdk/)

## 🐛 问题反馈

如果发现误报或漏报，请：

1. 检查配置文件设置
2. 查看测试数据文件 `TestData/PureAnalyzerTestData.cs`
3. 提交 Issue 并附上代码示例
4. 考虑贡献测试用例

---

通过 Pure 属性分析器，您可以：
- 🎯 **提升代码质量**：明确标识无副作用的方法
- 🔍 **增强静态分析**：配合 ReSharper 等工具发现问题
- 📚 **改善代码文档**：让代码意图更加清晰
- 🚀 **促进函数式编程**：鼓励编写纯函数