using System.Threading;
using AssetComponents.Editor;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(OptionalPropertyAttribute))]
public class OptionalPropertyDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        label.text += " (Optional)";
        EditorGUI.PropertyField(position, property, label, true);
    }

    public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
    {
        return EditorGUI.GetPropertyHeight(property, label, true);
    }
}