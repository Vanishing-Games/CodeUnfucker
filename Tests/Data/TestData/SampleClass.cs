using System;

public class SampleClass
{
    private int privateField;
    public string PublicProperty { get; set; } = string.Empty;
    protected bool protectedField;

    public void PublicMethod()
    {
        Console.WriteLine("Public method");
    }

    private void Start()
    {
        Console.WriteLine("Start");
    }

    private void Update()
    {
        // Update logic
    }

    protected virtual void ProtectedMethod()
    {
        // Protected method
    }

    private void PrivateMethod()
    {
        // Private method
    }

    public class NestedClass
    {
        public void NestedMethod() { }
    }

    private void Awake()
    {
        Console.WriteLine("Awake");
    }

    public SampleClass()
    {
        privateField = 0;
    }
} 