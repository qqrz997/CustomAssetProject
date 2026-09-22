using AssetComponents.Editor;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(PropertyLabelAttribute))]
public class PropertyLabelDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attr = (PropertyLabelAttribute)attribute;
        EditorGUI.PropertyField(position, property, new(attr.Label), true);
    }
}
