using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

namespace TestData
{
    // 示例 1: 包含多种堆内存分配问题的 MonoBehaviour
    public class BadPerformanceExample : MonoBehaviour
    {
        private List<GameObject> enemies;
        private string playerName = "Player";
        private int frameCount = 0;

        void Update()
        {
            // 问题 1: new 关键字分配
            var newList = new List<GameObject>();
            var newVector = new Vector3(1, 2, 3); // 这个应该被忽略，因为是值类型

            // 问题 2: LINQ 方法调用
            var activeEnemies = enemies.Where(e => e.activeInHierarchy).ToList();
            var enemyCount = enemies.Count(e => e.activeInHierarchy);

            // 问题 3: 字符串拼接
            string message = "Frame: " + frameCount + ", Enemies: " + activeEnemies.Count;

            // 问题 4: 字符串插值
            string interpolatedMessage = $"Current frame: {frameCount}, Active enemies: {enemyCount}";

            // 问题 5: 集合初始化
            var testArray = new int[] { 1, 2, 3, 4, 5 };
            var testList = new List<string> { "a", "b", "c" };

            // 问题 6: Lambda 表达式（可能产生闭包）
            var filteredEnemies = enemies.Where(e => e.transform.position.x > transform.position.x);

            frameCount++;
        }

        void LateUpdate()
        {
            // 在 LateUpdate 中也有堆内存分配
            var debugInfo = new Dictionary<string, object>();
            debugInfo["frame"] = Time.frameCount;
            debugInfo["time"] = Time.time;
        }

        void FixedUpdate()
        {
            // FixedUpdate 中的字符串操作
            string physicsDebug = "Physics step: " + Time.fixedTime;
        }

        void OnGUI()
        {
            // OnGUI 中的堆内存分配
            var buttonRect = new Rect(10, 10, 100, 30); // 值类型，应该被忽略
            var style = new GUIStyle(); // 引用类型，应该检测到
        }

        // 自定义的更新方法（需要在配置中添加）
        void OnPreRender()
        {
            var renderSettings = new List<object> { "setting1", "setting2" };
        }
    }

    // 示例 2: 性能良好的 MonoBehaviour（应该没有警告）
    public class GoodPerformanceExample : MonoBehaviour
    {
        private List<GameObject> enemies;
        private StringBuilder messageBuilder = new StringBuilder();
        private Vector3 cachedPosition;
        private int enemyCount;

        void Start()
        {
            // 在 Start 中进行初始化，避免在 Update 中分配
            enemies = new List<GameObject>();
            messageBuilder = new StringBuilder(256);
        }

        void Update()
        {
            // 使用缓存值
            cachedPosition = transform.position;
            
            // 避免 LINQ，使用简单循环
            enemyCount = 0;
            for (int i = 0; i < enemies.Count; i++)
            {
                if (enemies[i].activeInHierarchy)
                {
                    enemyCount++;
                }
            }

            // 使用 StringBuilder 避免字符串拼接
            messageBuilder.Clear();
            messageBuilder.Append("Enemies: ");
            messageBuilder.Append(enemyCount);
            string message = messageBuilder.ToString();

            // 使用值类型
            Vector3 movement = new Vector3(1, 0, 0);
            Quaternion rotation = Quaternion.identity;
        }
    }

    // 示例 3: 非 MonoBehaviour 类（应该被忽略）
    public class RegularClass
    {
        public void SomeMethod()
        {
            // 这些分配不应该被检测，因为不是 MonoBehaviour
            var list = new List<string>();
            string text = "Hello " + "World";
        }

        public void Update()
        {
            // 即使方法名是 Update，也不应该检测，因为不是 MonoBehaviour
            var data = new Dictionary<string, object>();
        }
    }

    // 示例 4: 继承自其他类的 MonoBehaviour
    public class DerivedMonoBehaviour : BadPerformanceExample
    {
        void Update()
        {
            // 应该检测到这里的问题
            var temporaryList = new List<int>();
            base.Update(); // 调用父类方法
        }
    }
}