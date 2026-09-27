using AssetComponents.Components.Sabers;
using AssetComponents.Models;
using UnityEditor;
using UnityEditor.Rendering;

[CustomEditor(typeof(TrailColorer))]
public class TrailColorerEditor : UnityEditor.Editor
{
    private SerializedProperty customTrail;
    private SerializedProperty materialIndex;
    private SerializedProperty propertyName;
    private SerializedProperty colorSchemeType;
    private SerializedProperty useColorBoostEvents;
    private SerializedProperty applyToVertexColor;
    private SerializedProperty multiplierColor;

    private void OnEnable()
    {
        if (!serializedObject.targetObject) return;
        customTrail = serializedObject.FindProperty("customTrail");
        materialIndex = serializedObject.FindProperty("materialIndex");
        propertyName = serializedObject.FindProperty("propertyName");
        colorSchemeType = serializedObject.FindProperty("colorSchemeType");
        useColorBoostEvents = serializedObject.FindProperty("useColorBoostEvents");
        applyToVertexColor = serializedObject.FindProperty("applyToVertexColor");
        multiplierColor = serializedObject.FindProperty("multiplierColor");
    }

    public override void OnInspectorGUI()
    {
        serializedObject.Update();

        EditorGUILayout.PropertyField(customTrail);

        using (new EditorGUI.DisabledScope(customTrail.objectReferenceValue == null))
        {
            DrawMaterialIndex();
            EditorGUILayout.PropertyField(propertyName);
            EditorGUILayout.PropertyField(colorSchemeType);

            using (new EditorGUI.DisabledScope(colorSchemeType.GetEnumValue<ColorSchemeType>() 
                is not (ColorSchemeType.EnvironmentColor0 or ColorSchemeType.EnvironmentColor0Boost
                or ColorSchemeType.EnvironmentColor1 or ColorSchemeType.EnvironmentColor1Boost
                or ColorSchemeType.EnvironmentColorW or ColorSchemeType.EnvironmentColorWBoost)))
            {
                EditorGUILayout.PropertyField(useColorBoostEvents);
            }

            EditorGUILayout.PropertyField(applyToVertexColor);
            EditorGUILayout.PropertyField(multiplierColor);
        }

        serializedObject.ApplyModifiedProperties();
    }

    private void DrawMaterialIndex()
    {
        var trail = customTrail.objectReferenceValue as CustomTrail;
        if (!trail || trail.materials.Length > 1)
        {
            EditorGUILayout.PropertyField(materialIndex);
            return;
        }

        materialIndex.intValue = 0;
        using (new EditorGUI.DisabledScope(true))
            EditorGUILayout.PropertyField(materialIndex);
    }
}
