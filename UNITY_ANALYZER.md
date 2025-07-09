# Unity 性能分析器 (UNITY0001)

## 📝 概述

Unity 性能分析器是一个专门为 Unity 项目设计的 Roslyn 分析器，用于检测 `Update()`、`LateUpdate()`、`FixedUpdate()` 等频繁调用的方法中的**堆内存分配操作**，帮助开发者避免因频繁 GC 导致的性能抖动。

## 🎯 功能特性

### 检测范围

分析器会检测继承自 `UnityEngine.MonoBehaviour` 的类中以下方法：

**默认检测方法：**
- `Update()`
- `LateUpdate()`
- `FixedUpdate()`
- `OnGUI()`

**可配置的其他方法：**
- `OnPreCull()`
- `OnPreRender()`
- `OnPostRender()`
- `OnRenderObject()`
- `OnApplicationPause()`
- `OnApplicationFocus()`

### 检测的堆内存分配类型

| 类型 | 示例 | 说明 |
|------|------|------|
| **new 关键字** | `new List<GameObject>()` | 创建引用类型对象（排除 Unity 值类型） |
| **LINQ 方法** | `.Where()`, `.Select()`, `.ToList()` | LINQ 扩展方法会产生迭代器和临时集合 |
| **字符串拼接** | `"Frame: " + frameCount` | 使用 `+` 操作符拼接字符串 |
| **字符串插值** | `$"Current frame: {frameCount}"` | 字符串插值表达式 |
| **集合初始化** | `new List<int> { 1, 2, 3 }` | 集合和数组的初始化语法 |
| **Lambda 闭包** | `enemies.Where(e => e.transform.position.x > transform.position.x)` | 可能产生隐式闭包的 Lambda 表达式 |

## ⚙️ 配置选项

在 `Config/AnalyzerConfig.json` 中的 `UnityAnalyzer` 部分：

```json
{
  "UnityAnalyzer": {
    "EnableUnityAnalysis": true,
    "CheckNewKeyword": true,
    "CheckLinqMethods": true,
    "CheckStringConcatenation": true,
    "CheckStringInterpolation": true,
    "CheckClosures": true,
    "CheckCollectionInitialization": true,
    "CustomUpdateMethods": [
      "OnPreCull",
      "OnPreRender",
      "OnPostRender"
    ],
    "ExcludedValueTypes": [],
    "DefaultSeverity": "Warning"
  }
}
```

### 配置说明

- `EnableUnityAnalysis`: 是否启用 Unity 分析器
- `CheckNewKeyword`: 检测 new 关键字分配
- `CheckLinqMethods`: 检测 LINQ 方法调用
- `CheckStringConcatenation`: 检测字符串拼接
- `CheckStringInterpolation`: 检测字符串插值
- `CheckClosures`: 检测可能的闭包
- `CheckCollectionInitialization`: 检测集合初始化
- `CustomUpdateMethods`: 自定义要检测的方法名
- `ExcludedValueTypes`: 要排除检测的值类型
- `DefaultSeverity`: 默认诊断严重程度

## 🚀 使用方法

### 基本用法

```bash
# 分析当前目录下的所有 .cs 文件
CodeUnfucker analyze ./Scripts

# 使用自定义配置
CodeUnfucker analyze ./Scripts --config ./MyConfig
```

### 示例输出

```
[INFO] 开始 Unity 性能分析 (包含语义分析)
[WARN] 🔍 Unity 性能分析完成：发现 8 个潜在的堆内存分配问题

📁 文件: BadPerformanceExample.cs
  ⚠️ [UNITY0001] BadPerformanceExample.cs(18,28): warning: 在 Update() 中使用 'new List<GameObject>' 会产生堆内存分配
  ⚠️ [UNITY0001] BadPerformanceExample.cs(21,36): warning: 在 Update() 中使用 LINQ 方法 'Where' 会产生堆内存分配
  ⚠️ [UNITY0001] BadPerformanceExample.cs(21,73): warning: 在 Update() 中使用 LINQ 方法 'ToList' 会产生堆内存分配
  ⚠️ [UNITY0001] BadPerformanceExample.cs(25,26): warning: 在 Update() 中使用字符串拼接 '+' 会产生堆内存分配

📊 问题类型统计:
  • LINQ 方法调用: 3 个
  • new 关键字分配: 2 个
  • 字符串拼接: 2 个
  • 字符串插值: 1 个

💡 建议:
  - 考虑使用对象池来避免频繁的 new 操作
  - 使用 StringBuilder 替代字符串拼接
  - 缓存 LINQ 查询结果，避免每帧重复计算
  - 将复杂计算移到 Start() 或 Awake() 中
```

## 📋 优化建议

### 1. 避免 new 关键字
```csharp
// ❌ 坏的做法
void Update()
{
    var enemies = new List<GameObject>();
}

// ✅ 好的做法
private List<GameObject> enemies = new List<GameObject>();
void Update()
{
    enemies.Clear(); // 重用现有列表
}
```

### 2. 避免 LINQ
```csharp
// ❌ 坏的做法
void Update()
{
    var activeEnemies = enemies.Where(e => e.activeInHierarchy).ToList();
}

// ✅ 好的做法
void Update()
{
    activeEnemies.Clear();
    for (int i = 0; i < enemies.Count; i++)
    {
        if (enemies[i].activeInHierarchy)
            activeEnemies.Add(enemies[i]);
    }
}
```

### 3. 避免字符串操作
```csharp
// ❌ 坏的做法
void Update()
{
    string text = "Score: " + score;
    string debug = $"Player: {playerName}";
}

// ✅ 好的做法
private StringBuilder sb = new StringBuilder();
void Update()
{
    sb.Clear();
    sb.Append("Score: ");
    sb.Append(score);
    string text = sb.ToString();
}
```

### 4. 缓存和对象池
```csharp
// ✅ 使用对象池
private Queue<Bullet> bulletPool = new Queue<Bullet>();

void Update()
{
    if (shouldShoot)
    {
        var bullet = bulletPool.Count > 0 ? bulletPool.Dequeue() : CreateBullet();
        // 使用 bullet
    }
}
```

## 🔧 已知限制

1. **语义分析依赖**: 某些检测需要语义模型，在没有 Unity 引用的环境下可能无法完全识别 MonoBehaviour 继承关系
2. **闭包检测**: 当前的闭包检测比较简单，可能产生误报
3. **字符串检测**: 字符串变量识别基于命名模式，可能不够精确

## 📈 性能影响

- 分析器本身对构建性能影响很小
- 建议在 CI/CD 流程中集成，定期检查代码质量
- 可以通过配置选择性地禁用某些检测来提高性能

## 🤝 贡献

欢迎提交 Issue 和 Pull Request 来改进分析器功能！