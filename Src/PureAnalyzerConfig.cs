using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace CodeUnfucker;

/// <summary>
/// Pure 分析器配置类
/// </summary>
public class PureAnalyzerConfig
{
    /// <summary>
    /// 需要检查的可见性级别
    /// </summary>
    [JsonPropertyName("Accessibility")]
    public List<string> Accessibility { get; set; } = new() { "public", "internal" };

    /// <summary>
    /// 是否排除 partial 方法
    /// </summary>
    [JsonPropertyName("ExcludePartial")]
    public bool ExcludePartial { get; set; } = true;

    /// <summary>
    /// 是否自动标记无副作用的只读属性为 pure
    /// </summary>
    [JsonPropertyName("AllowGetters")]
    public bool AllowGetters { get; set; } = true;

    /// <summary>
    /// 是否启用建议添加 [Pure] 功能
    /// </summary>
    [JsonPropertyName("EnableSuggestAdd")]
    public bool EnableSuggestAdd { get; set; } = true;

    /// <summary>
    /// 是否启用建议移除 [Pure] 功能
    /// </summary>
    [JsonPropertyName("EnableSuggestRemove")]
    public bool EnableSuggestRemove { get; set; } = true;

    /// <summary>
    /// 排除的命名空间
    /// </summary>
    [JsonPropertyName("ExcludedNamespaces")]
    public List<string> ExcludedNamespaces { get; set; } = new()
    {
        "UnityEngine",
        "Unity",
        "UnityEditor"
    };

    /// <summary>
    /// 排除的方法名称
    /// </summary>
    [JsonPropertyName("ExcludedMethods")]
    public List<string> ExcludedMethods { get; set; } = new()
    {
        "Debug.Log",
        "Debug.LogWarning",
        "Debug.LogError",
        "Console.WriteLine",
        "Console.Write"
    };

    /// <summary>
    /// Unity API 正则表达式模式
    /// </summary>
    [JsonPropertyName("UnityApiPatterns")]
    public List<string> UnityApiPatterns { get; set; } = new()
    {
        "transform\\.",
        "gameObject\\.",
        "Time\\.",
        "Input\\.",
        "Physics\\.",
        "Rigidbody\\.",
        "Collider\\.",
        "Renderer\\.",
        "Camera\\.",
        "Light\\.",
        "Audio\\.",
        "Animation\\.",
        "Animator\\.",
        "Canvas\\.",
        "UI\\.",
        "SceneManager\\.",
        "AssetDatabase\\.",
        "EditorUtility\\.",
        "Selection\\."
    };

    /// <summary>
    /// 从 JSON 文件加载配置
    /// </summary>
    public static PureAnalyzerConfig LoadFromFile(string configPath)
    {
        try
        {
            if (!File.Exists(configPath))
            {
                Console.WriteLine($"配置文件不存在: {configPath}，使用默认配置");
                return new PureAnalyzerConfig();
            }

            var jsonContent = File.ReadAllText(configPath);
            var configWrapper = JsonSerializer.Deserialize<PureAnalyzerConfigWrapper>(jsonContent);
            
            return configWrapper?.PureAnalyzerSettings ?? new PureAnalyzerConfig();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"加载配置文件失败: {ex.Message}，使用默认配置");
            return new PureAnalyzerConfig();
        }
    }

    /// <summary>
    /// 获取默认配置
    /// </summary>
    public static PureAnalyzerConfig GetDefault()
    {
        return new PureAnalyzerConfig();
    }
}

/// <summary>
/// 配置文件包装类
/// </summary>
internal class PureAnalyzerConfigWrapper
{
    [JsonPropertyName("PureAnalyzerSettings")]
    public PureAnalyzerConfig? PureAnalyzerSettings { get; set; }
}