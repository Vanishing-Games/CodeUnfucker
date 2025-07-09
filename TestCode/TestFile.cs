using System;

namespace TestNamespace
{
    public class testclass  // 命名不规范
    {
        private int unusedVariable;  // 未使用的变量
        
        public void Method_with_underscores()  // 命名不规范
        {
            // 缺少文档注释
            for(int i=0;i<100;i++)  // 高复杂度代码
            {
                for(int j=0;j<100;j++)
                {
                    if(i>50&&j>50)
                    {
                        Console.WriteLine("Complex logic");
                    }
                }
            }
        }
    }
}
