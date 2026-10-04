using System.Collections.Generic;
using Editor.Extensions;
using Editor.Models;
using AssetComponents.Components.Sabers;
using Editor.Common;
using UnityEditor;
using UnityEngine;

public class ModelExporterWindow : EditorWindow
{
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();
    
    private readonly List<ExportableSaber> sabers = new();
    private Vector2 scrollPosition = Vector2.zero;
    private bool isExporting;

    [MenuItem("Window/BS Asset Project/Exporter")]
    public static void ShowWindow()
    {
        GetWindow<ModelExporterWindow>(false, "Exporter");
    }
    
    private void OnFocus()
    {
        if (isExporting) return;
        
        sabers.Clear();
        foreach (var saber in FindObjectsByType<SaberDescriptor>(FindObjectsSortMode.None))
        {
            if (saber) sabers.Add(new(saber));
        }
    }
    
    private void OnGUI()
    {
        var isPathValid = Settings.BeatSaberDirValid();
        if (!isPathValid)
        {
            EditorGUILayout.HelpBox("Beat Saber Path is invalid\nPlease adjust the settings", MessageType.Warning);
            GUILayout.Space(10);
            if (UITools.Button("Settings", 25, Color.HSVToRGB(0.1f, 0.15f, 1)))
                SettingsService.OpenProjectSettings("Project/BS Model Toolkit");
        }

        EditorGUI.BeginDisabledGroup(!isPathValid);
        GUILayout.Space(10);

        if (sabers is not { Count: > 0 })
        {
            UITools.CenterHeader("There aren't any sabers in the scene", Color.yellow);
            if (GUILayout.Button("Create saber using Saber Tools", GUILayout.Height(25)))
                SaberTools.OpenSaberTools();
            return;
        }

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);
        foreach (var exportableSaber in sabers)
        {
            DrawSaberExportBox(exportableSaber);
        }
        EditorGUILayout.EndScrollView();
        EditorGUI.EndDisabledGroup();
    }

    private void DrawSaberExportBox(ExportableSaber exportableSaber)
    {
        UITools.CenterHeader(exportableSaber.Name, Color.white);
        GUILayout.BeginVertical("box");
        
        if (!exportableSaber.GameObject)
        {
            UITools.BoldLabel($"Couldn't find saber object. Exporter needs to refresh.", ValidationMessage.ErrorColor);
            GUILayout.EndVertical();
            return;
        }

        var isWarning = false;
        foreach (var (message, color) in exportableSaber.Validate())
        {
            isWarning = true;
            UITools.BoldLabel(message, color);
        }
            
        GUILayout.Space(5);
        GUILayout.Label($"Export \"{exportableSaber.Name}\" by \"{exportableSaber.Author}\"");
            
        GUILayout.Space(5);
        if (GUILayout.Button("Select"))
            Selection.activeGameObject = exportableSaber.GameObject;

        EditorGUI.BeginDisabledGroup(!exportableSaber.IsReadyForExport);
        GUI.color = !exportableSaber.IsReadyForExport ? ValidationMessage.ErrorColor
            : isWarning ? ValidationMessage.WarningColor
            : ValidationMessage.SuccessColor;
        if (GUILayout.Button("Export", GUILayout.Height(25)))
        {
            GUI.color = Color.white;
            isExporting = true;
            ModelExporter.ExportAsset(exportableSaber, onComplete: () => isExporting = false);
        }
        GUI.color = Color.white;

        EditorGUI.EndDisabledGroup();
        GUILayout.EndVertical();
    }
}