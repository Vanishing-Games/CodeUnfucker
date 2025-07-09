using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Reflection;
using UnityEngine;
using UnityEditor;

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