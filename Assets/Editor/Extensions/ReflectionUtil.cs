using System.Reflection;

namespace Editor.Extensions
{
    internal static class ReflectionUtil
    {
        public static void SetField<T>(this T obj, string fieldName, object value)
        {
            var field = typeof(T).GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                        ?? throw new($"Field {fieldName} not found");
            field.SetValue(obj, value);
        }
    }
}