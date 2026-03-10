using System;
using System.IO;
using System.Reflection;
using System.Xml.Serialization;

// ---- Test types for ShouldSerialize scenarios ----

// Scenario 1: ShouldSerialize on same class (existing test coverage - works fine)
public class TypeWithShouldSerializeSameClass
{
    public int Foo { get; set; } = 42;
    public bool ShouldSerializeFoo() => Foo != 0;
}

// Scenario 2: ShouldSerialize defined on BASE class (triggers DeclaredOnly bug)
public class BaseWithShouldSerialize
{
    public int Value { get; set; } = 99;
    public bool ShouldSerializeValue() => Value != 0;
}

public class DerivedFromBaseWithShouldSerialize : BaseWithShouldSerialize
{
    public string? Label { get; set; } = "test";
}

// Scenario 3: Simple serialization (baseline trace)
public class SimpleType
{
    public string? Name { get; set; }
    public int Age { get; set; }
}

class Program
{
    static void Main()
    {
        Console.WriteLine("=== Runtime Trace Test ===");
        Console.WriteLine($"TRACE_LOG_JSON_PATH = {Environment.GetEnvironmentVariable("TRACE_LOG_JSON_PATH") ?? "(not set)"}");

        // Force reflection-based serialization so ReflectionXmlSerializationWriter is used
        // SerializationMode.ReflectionOnly = 1 (internal enum)
        var modeField = typeof(XmlSerializer).GetField("s_mode", BindingFlags.NonPublic | BindingFlags.Static);
        if (modeField != null)
        {
            modeField.SetValue(null, 1); // ReflectionOnly
            Console.WriteLine("Forced ReflectionOnly serialization mode");
        }
        else
        {
            Console.WriteLine("WARNING: Could not find s_mode field");
        }
        // Test 1: Simple serialization
        Console.WriteLine("\n--- Test 1: Simple Serialization ---");
        try
        {
            var simple = new SimpleType { Name = "Alice", Age = 30 };
            var serializer1 = new XmlSerializer(typeof(SimpleType));
            using var sw1 = new StringWriter();
            serializer1.Serialize(sw1, simple);
            Console.WriteLine($"OK: {sw1.ToString().Substring(0, Math.Min(80, sw1.ToString().Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
        }

        // Test 2: ShouldSerialize on same class (should work)
        Console.WriteLine("\n--- Test 2: ShouldSerialize Same Class ---");
        try
        {
            var obj2 = new TypeWithShouldSerializeSameClass { Foo = 42 };
            var serializer2 = new XmlSerializer(typeof(TypeWithShouldSerializeSameClass));
            using var sw2 = new StringWriter();
            serializer2.Serialize(sw2, obj2);
            Console.WriteLine($"OK (Foo=42, should serialize): {sw2.ToString().Contains("<Foo>")}");

            var obj2b = new TypeWithShouldSerializeSameClass { Foo = 0 };
            using var sw2b = new StringWriter();
            serializer2.Serialize(sw2b, obj2b);
            Console.WriteLine($"OK (Foo=0, should NOT serialize): {!sw2b.ToString().Contains("<Foo>")}");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
        }

        // Test 3: ShouldSerialize on BASE class (DeclaredOnly bug trigger)
        Console.WriteLine("\n--- Test 3: ShouldSerialize Inherited (DeclaredOnly Bug) ---");
        try
        {
            var obj3 = new DerivedFromBaseWithShouldSerialize { Value = 99, Label = "hello" };
            var serializer3 = new XmlSerializer(typeof(DerivedFromBaseWithShouldSerialize));
            using var sw3 = new StringWriter();
            serializer3.Serialize(sw3, obj3);
            Console.WriteLine($"OK: {sw3.ToString().Substring(0, Math.Min(100, sw3.ToString().Length))}...");
        }
        catch (Exception ex)
        {
            Console.WriteLine($"FAIL: {ex.GetType().Name}: {ex.Message}");
            if (ex.InnerException != null)
                Console.WriteLine($"  Inner: {ex.InnerException.GetType().Name}: {ex.InnerException.Message}");
        }

        Console.WriteLine("\n=== Done ===");
    }
}
