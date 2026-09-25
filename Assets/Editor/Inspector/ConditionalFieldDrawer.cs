using System;
using AssetComponents.Components;
using AssetComponents.Editor;
using UnityEditor;
using UnityEngine;

[CustomPropertyDrawer(typeof(ConditionalFieldAttribute))]
public class ConditionalFieldDrawer : PropertyDrawer
{
    public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
    {
        var attribute = (ConditionalFieldAttribute)this.attribute;
        var sourceProperty = property.serializedObject.FindProperty(attribute.FieldName);

        var isEnabled = sourceProperty != null && ShouldShow(sourceProperty, attribute);
        using (new EditorGUI.DisabledScope(!isEnabled))
            EditorGUI.PropertyField(position, property, true);
    }

    private static bool ShouldShow(SerializedProperty property, ConditionalFieldAttribute attribute)
    {
        var source = property.serializedObject.FindProperty(attribute.FieldName);
        if (source == null) return false;

        object value = source.propertyType is SerializedPropertyType.ObjectReference ? source.objectReferenceValue : null;

        var condition = (ConditionalFieldAttribute.ICondition)Activator.CreateInstance(attribute.ConditionType);
        return condition.GetState(value);
    }
}