using System;
using System.Collections.Generic;
using System.Linq;

namespace TestNamespace
{
    public class TestFile : MonoBehaviour
    {
        private List<string> items = new List<string>();
        
        void Start()
        {
            Console.WriteLine("Hello World");
            Debug.Log("Unity Debug");
        }
        
        private void ProcessItems()
        {
            var result = items.Where(x => !string.IsNullOrEmpty(x)).ToList();
        }
    }
}