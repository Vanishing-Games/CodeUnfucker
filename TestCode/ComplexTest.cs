using System;

namespace TestNamespace
{
    /// <summary>
    /// 这是一个有文档注释的类
    /// </summary>
    public class GoodClass
    {
        private readonly int _privateField;
        public int PublicProperty { get; set; }
        
        /// <summary>
        /// 有文档注释的公共方法
        /// </summary>
        public void GoodMethod()
        {
            Console.WriteLine("Good method");
        }
        
        private void PrivateMethod()
        {
            // 私有方法不需要文档注释
        }
    }
    
    // 没有文档注释的公共类
    public class badClassName  // 命名不规范
    {
        public int bad_field_name;  // 公共字段命名不规范
        private int unusedField;    // 未使用的字段
        
        // 没有文档注释的公共方法
        public void method_with_bad_name()  // 命名不规范
        {
            // 高复杂度方法
            for(int i = 0; i < 100; i++)
            {
                for(int j = 0; j < 100; j++)
                {
                    for(int k = 0; k < 100; k++)
                    {
                        if(i > 50)
                        {
                            if(j > 50)
                            {
                                if(k > 50)
                                {
                                    Console.WriteLine($"Complex: {i}, {j}, {k}");
                                }
                                else
                                {
                                    Console.WriteLine("Else branch");
                                }
                            }
                        }
                    }
                }
            }
        }
        
        public int PublicProperty_Bad_Name { get; set; }  // 属性命名不规范
    }
}
