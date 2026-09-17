using System.Collections.Generic;
using Editor.Models;
using UnityEditor;
using UnityEngine;

internal static class ProjectSettingsRegister
{
    [SettingsProvider]
    public static SettingsProvider CreateSaberProjectSettingsProvider()
    {
        var provider = new SettingsProvider("Project/BS Asset Project", SettingsScope.Project)
        {
            label = "BS Model Toolkit",
            guiHandler = HandleGui,
            keywords = new HashSet<string>(new[]
            {
                "Beat Saber Path", "Author Name", "Create RightSaber on Export", "Export Filename", "Show Overlay"
            })
        };

        return provider;
    }

    private static void HandleGui(string searchContext)
    {
        var settings = ProjectSettings.GetSerializedSettings();
        
        EditorGUIUtility.labelWidth = 190;
        
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Required", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        var beatSaberPath = settings.FindProperty(nameof(ProjectSettings.beatSaberPath));
        EditorGUILayout.PropertyField(beatSaberPath, new GUIContent("Beat Saber Path"));
        if (GUILayout.Button("Browse", EditorStyles.miniButtonLeft, GUILayout.Width(67)))
        {
            var path = EditorUtility.OpenFolderPanel("Select Beat Saber Install", beatSaberPath.stringValue, string.Empty);
            if (!string.IsNullOrEmpty(path))
            {
                beatSaberPath.stringValue = path;
            }
        }
        EditorGUILayout.EndHorizontal();
        EditorGUILayout.PropertyField(settings.FindProperty(nameof(ProjectSettings.author)), new GUIContent("Author Name"));
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(settings.FindProperty(nameof(ProjectSettings.exportFilename)), new GUIContent("Export Filename"));
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = EditorStyles.label.normal.textColor * 0.9f;
        labelStyle.fontSize = EditorStyles.label.fontSize - 2;
        GUILayout.Label("Available templates: {ModelName}, {AuthorName}", labelStyle);
        GUILayout.Label("Examples: \"{AuthorName}_{ModelName}\", \"Bob_{ModelName}\"", labelStyle);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(settings.FindProperty(nameof(ProjectSettings.showOverlay)), new GUIContent("Show Overlay"));
        EditorGUILayout.EndVertical();

        EditorGUILayout.BeginVertical("box");
        var colorScheme = settings.FindProperty(nameof(ProjectSettings.colorScheme));
        EditorGUILayout.LabelField("Color Scheme", EditorStyles.boldLabel);
        DrawColorProperty(settings, colorScheme, "saberAColor", MockColorScheme.Default.saberAColor);
        DrawColorProperty(settings, colorScheme, "saberBColor", MockColorScheme.Default.saberBColor);
        DrawColorProperty(settings, colorScheme, "obstaclesColor", MockColorScheme.Default.obstaclesColor);
        DrawColorProperty(settings, colorScheme, "environmentColor0", MockColorScheme.Default.environmentColor0);
        DrawColorProperty(settings, colorScheme, "environmentColor1", MockColorScheme.Default.environmentColor1);
        DrawColorProperty(settings, colorScheme, "environmentColorW", MockColorScheme.Default.environmentColorW);
        DrawColorProperty(settings, colorScheme, "environmentColor0Boost", MockColorScheme.Default.environmentColor0Boost);
        DrawColorProperty(settings, colorScheme, "environmentColor1Boost", MockColorScheme.Default.environmentColor0Boost);
        DrawColorProperty(settings, colorScheme, "environmentColorWBoost", MockColorScheme.Default.environmentColor1Boost);
        EditorGUILayout.EndVertical();

        if (settings.ApplyModifiedProperties())
        {
            RefreshOnPropertyChanged();
        }
    }

    private static void DrawColorProperty(
        SerializedObject settings, SerializedProperty colorScheme, string propertyName, Color defaultColor)
    {
        var property = colorScheme.FindPropertyRelative(propertyName);
        var rect = EditorGUILayout.GetControlRect(true, EditorGUI.GetPropertyHeight(property));

        if (Event.current.type != EventType.ContextClick || !rect.Contains(Event.current.mousePosition))
        {
            EditorGUI.PropertyField(rect, property);
            return;
        }

        var menu = new GenericMenu();
        menu.AddItem(new("Reset"), false, () =>
        {
            Undo.RecordObject(settings.targetObject, $"Reset {property.displayName}");
            property.colorValue = defaultColor;
            if (settings.ApplyModifiedProperties()) RefreshOnPropertyChanged();
        });
        menu.ShowAsContext();
        Event.current.Use();
    }

    private static void RefreshOnPropertyChanged()
    {
        MaterialColorerPreviewer.RefreshAll();
    }
}