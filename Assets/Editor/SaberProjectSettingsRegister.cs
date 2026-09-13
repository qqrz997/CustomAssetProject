using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

internal static class SaberProjectSettingsRegister
{
    [SettingsProvider]
    public static SettingsProvider CreateSaberProjectSettingsProvider()
    {
        var provider = new SettingsProvider("Project/BS Model Toolkit", SettingsScope.Project)
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
        var settings = SaberProjectSettings.GetSerializedSettings();
        
        EditorGUIUtility.labelWidth = 190;
        
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Required", EditorStyles.boldLabel);
        EditorGUILayout.BeginHorizontal();
        var beatSaberPath = settings.FindProperty(nameof(SaberProjectSettings.beatSaberPath));
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
        EditorGUILayout.PropertyField(settings.FindProperty(nameof(SaberProjectSettings.author)), new GUIContent("Author Name"));
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical("box");
        EditorGUILayout.PropertyField(settings.FindProperty(nameof(SaberProjectSettings.exportFilename)), new GUIContent("Export Filename"));
        var labelStyle = new GUIStyle(EditorStyles.label);
        labelStyle.normal.textColor = EditorStyles.label.normal.textColor * 0.9f;
        labelStyle.fontSize = EditorStyles.label.fontSize - 2;
        GUILayout.Label("Available templates: {ModelName}, {AuthorName}", labelStyle);
        GUILayout.Label("Examples: \"{AuthorName}_{ModelName}\", \"Bob_{ModelName}\"", labelStyle);
        EditorGUILayout.EndVertical();
        
        EditorGUILayout.BeginVertical("box");
        GUILayout.Label("Options", EditorStyles.boldLabel);
        EditorGUILayout.PropertyField(settings.FindProperty(nameof(SaberProjectSettings.showOverlay)), new GUIContent("Show Overlay"));
        EditorGUILayout.EndVertical();
        
        settings.ApplyModifiedProperties();
    }
}