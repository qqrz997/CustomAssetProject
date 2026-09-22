using System.Collections.Generic;
using System.Linq;
using Editor;
using Editor.Extensions;
using Editor.Models;
using AssetComponents.Components.Sabers;
using Editor.Common;
using UnityEditor;
using UnityEngine;

public class ModelExporterWindow : EditorWindow
{
    private ExportableSaber[] sabers;
    private Vector2 scrollPosition = Vector2.zero;
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();

    [MenuItem("Window/BS Asset Project/Exporter")]
    public static void ShowWindow()
    {
        GetWindow<ModelExporterWindow>(false, "Exporter");
    }
    
    private void OnFocus()
    {
        sabers = FindObjectsByType<SaberDescriptor>(FindObjectsSortMode.None)
            .Select(x=>new ExportableSaber(x)).ToArray();
    }
    
    private void OnGUI()
    {
        if (!InitialValidation()) return;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);
        foreach (var exportableSaber in sabers)
        {
            if (!exportableSaber.GameObject)
            {
                OnFocus();
                break;
            }

            UITools.CenterHeader(exportableSaber.GameObject.name, Color.white);
            GUILayout.BeginVertical("box");

            var isWarning = false;
            foreach (var (msg, clr) in RunSaberValidation(exportableSaber))
            {
                isWarning = true;
                UITools.BoldLabel(msg, clr);
            }
            
            GUILayout.Space(5);
            GUILayout.Label($"Export \"{exportableSaber.Name}\" by {exportableSaber.Author}\"");
            
            EditorGUI.BeginDisabledGroup(!exportableSaber.IsReadyForExport);
            GUILayout.Space(5);
            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = exportableSaber.GameObject;
            }

            GUI.color = (exportableSaber.IsReadyForExport, isWarning) switch
            {
                (false, _) => new(0.7f, 0f, 0f),
                (_, true) => new(0.7f, 0.46f, 0f),
                _ => new(0.25f, 0.65f, 0.25f)
            };
            if (GUILayout.Button("Export", GUILayout.Height(25)))
            {
                GUI.color = Color.white;
                ModelExporter.ExportAsset(exportableSaber);
            }
            GUI.color = Color.white;

            EditorGUI.EndDisabledGroup();
            GUILayout.EndVertical();
        }
        EditorGUILayout.EndScrollView();
        EditorGUI.EndDisabledGroup();
    }

    private bool InitialValidation()
    {
        var isPathValid = Settings.BeatSaberDirValid();
        if (!isPathValid)
        {
            EditorGUILayout.HelpBox("Beat Saber Path is invalid\nPlease adjust the settings", MessageType.Warning);
            GUILayout.Space(10);
            if (UITools.Button("Settings", 25, Color.HSVToRGB(0.1f, 0.15f, 1)))
            {
                SettingsService.OpenProjectSettings("Project/BS Model Toolkit");
            }
        }

        EditorGUI.BeginDisabledGroup(!isPathValid);
        GUILayout.Space(10);
        
        if (sabers == null || sabers.Length < 1)
        {
            UITools.CenterHeader("There aren't any sabers in the scene", Color.yellow);
            if (GUILayout.Button("Create saber using Saber Tools", GUILayout.Height(25)))
            {
                SaberTools.OpenSaberTools();
            }
            return false;
        }
        return true;
    }

    private static IEnumerable<(string, Color)> RunSaberValidation(ExportableSaber saber)
    {
        if (!saber.IsReadyForExport)
            yield return (" - LeftSaber gameObject is missing", Color.red);

        var saberBounds = saber.GameObject.GetObjectBounds().extents * 2;
        if (saberBounds.z > SaberTools.SaberLength + 0.1f)
            yield return (" - The saber might be too long", Color.yellow);
        if (saberBounds.z < SaberTools.SaberLength - 0.1f)
            yield return (" - The saber might be too short", Color.yellow);
        if (saberBounds.x > 1.0)
            yield return (" - The saber might be too large", Color.yellow);
        if (saberBounds.x > saberBounds.z || saberBounds.y > saberBounds.z)
            yield return (" - Your saber might be rotated incorrectly", Color.yellow);
        if (!saber.HasTrail)
            yield return (" - Your saber doesn't have any trails", Color.yellow);
        if (saber.HasSaberTransforms)
            yield return (" - Your Left/Right-Saber gameobject has transforms applied", Color.yellow);
    }
}