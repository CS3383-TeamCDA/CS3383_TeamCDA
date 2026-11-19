using System.Reflection;
using NUnit.Framework;

/// <summary>
/// Helpers shared by the TL1 tests.
/// </summary>
public static class TL1TestUtil
{
    // Sets a private [SerializeField] the way the Inspector would, so tests can wire up components.
    public static void SetField(object target, string fieldName, object value)
    {
        FieldInfo field = null;
        for (System.Type type = target.GetType(); type != null && field == null; type = type.BaseType)
        {
            field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        Assert.IsNotNull(field, $"{target.GetType().Name} has no field '{fieldName}'.");
        field.SetValue(target, value);
    }

    // Calls a private method by name, e.g. to finish a step a test faked instead of running for real.
    public static void Invoke(object target, string methodName, params object[] arguments)
    {
        MethodInfo method = null;
        for (System.Type type = target.GetType(); type != null && method == null; type = type.BaseType)
        {
            method = type.GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);
        }

        Assert.IsNotNull(method, $"{target.GetType().Name} has no method '{methodName}'.");
        method.Invoke(target, arguments);
    }
}
