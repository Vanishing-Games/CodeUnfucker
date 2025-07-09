using System;
using System.Diagnostics.Contracts;
using UnityEngine;

namespace TestData
{
    public class PureAnalyzerTestData
    {
        private int _field = 0;
        
        // ✅ 应该建议添加 [Pure] - 简单计算
        public int CalculateHealth(int baseHp, int armor)
        {
            return baseHp + armor * 5;
        }
        
        // ✅ 应该建议添加 [Pure] - 数学运算
        public double CalculateDistance(double x1, double y1, double x2, double y2)
        {
            return Math.Sqrt((x2 - x1) * (x2 - x1) + (y2 - y1) * (y2 - y1));
        }
        
        // ✅ 应该建议添加 [Pure] - 字符串操作
        public string FormatPlayerName(string firstName, string lastName)
        {
            return $"{firstName} {lastName}".Trim();
        }
        
        // ✅ 应该建议添加 [Pure] - 条件表达式
        public int GetMaxValue(int a, int b, int c)
        {
            return a > b ? (a > c ? a : c) : (b > c ? b : c);
        }
        
        // ❌ 不应该标记为 [Pure] - void 返回类型
        public void UpdateHealth(int newHealth)
        {
            _field = newHealth;
        }
        
        // ❌ 不应该标记为 [Pure] - 修改字段
        public int IncrementAndReturn()
        {
            _field++;
            return _field;
        }
        
        // ❌ 不应该标记为 [Pure] - Unity API 调用
        public Vector3 GetRandomPosition()
        {
            return new Vector3(
                UnityEngine.Random.Range(0f, 10f),
                UnityEngine.Random.Range(0f, 10f),
                UnityEngine.Random.Range(0f, 10f)
            );
        }
        
        // ❌ 不应该标记为 [Pure] - Debug.Log 调用
        public int CalculateWithLogging(int a, int b)
        {
            Debug.Log($"Calculating {a} + {b}");
            return a + b;
        }
        
        // ❌ 不应该标记为 [Pure] - 调用有副作用的方法
        public int CalculateWithSideEffect(int value)
        {
            UpdateHealth(value); // 有副作用的调用
            return value * 2;
        }
        
        // ⚠️ 错误的 [Pure] 标记 - 应该建议移除
        [Pure]
        public int WronglyMarkedAsPure()
        {
            Debug.Log("This has side effects!");
            return 42;
        }
        
        // ⚠️ 错误的 [Pure] 标记 - 应该建议移除
        [Pure]
        public int AnotherWrongPure(int value)
        {
            _field = value; // 修改状态
            return value;
        }
        
        // ✅ 正确的 [Pure] 标记 - 不应该建议移除
        [Pure]
        public int CorrectlyMarkedAsPure(int a, int b)
        {
            return a + b;
        }
        
        // ✅ 应该建议添加 [Pure] - 只读属性
        public string FullName => $"Player_{_field}";
        
        // ✅ 应该建议添加 [Pure] - 带复杂逻辑的只读属性
        public bool IsValid 
        { 
            get 
            { 
                return _field > 0 && _field < 100; 
            } 
        }
        
        // ❌ 不应该标记为 [Pure] - 有setter的属性
        public int Health 
        { 
            get => _field; 
            set => _field = value; 
        }
        
        // ❌ 不应该标记为 [Pure] - getter有副作用
        public int BadProperty
        {
            get
            {
                Debug.Log("Getting property value");
                return _field;
            }
        }
        
        // ✅ 应该建议添加 [Pure] - LINQ 查询
        public int[] FilterEvenNumbers(int[] numbers)
        {
            return numbers.Where(x => x % 2 == 0).ToArray();
        }
        
        // ✅ 应该建议添加 [Pure] - 递归函数
        public int Factorial(int n)
        {
            if (n <= 1) return 1;
            return n * Factorial(n - 1);
        }
        
        // ❌ 不应该标记为 [Pure] - 使用 DateTime.Now (有副作用)
        public string GetTimestamp()
        {
            return DateTime.Now.ToString();
        }
        
        // ✅ 应该建议添加 [Pure] - 使用传入的时间参数
        public string FormatTimestamp(DateTime dateTime)
        {
            return dateTime.ToString("yyyy-MM-dd HH:mm:ss");
        }
        
        // Internal 方法测试
        internal int InternalPureMethod(int x, int y)
        {
            return x * y;
        }
        
        // Protected 方法测试
        protected virtual double ProtectedPureMethod(double value)
        {
            return Math.Abs(value);
        }
        
        // Private 方法测试
        private string PrivatePureMethod(string input)
        {
            return input?.ToUpper() ?? "";
        }
    }
    
    // 测试 partial 类
    public partial class PartialTestClass
    {
        partial void PartialMethod(int value);
        
        public int RegularMethod(int a, int b)
        {
            return a + b;
        }
    }
}