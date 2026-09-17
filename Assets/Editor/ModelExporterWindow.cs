using System.Linq;
using Editor.Extensions;
using Editor.Models;
using SaberComponents.Components;
using UnityEditor;
using UnityEngine;

public class ModelExporterWindow : EditorWindow
{
    private SaberInfo[] sabers;
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
            .Select(x=>new SaberInfo(x)).ToArray();
    }
    
    private void OnGUI()
    {
        if (!InitialValidation()) return;

        scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, false);
        foreach (var saber in sabers)
        {
            var gameObject = saber.GameObject;
            if (!gameObject) continue;

            UITools.CenterHeader(gameObject.name, Color.white);
            GUILayout.BeginVertical("box");

            var (cantExport, isWarning) = RunSaberValidation(saber);

            GUILayout.Space(5);

            saber.SaberDescriptor.AuthorName = EditorGUILayout.TextField("Author name", saber.SaberDescriptor.AuthorName);
            saber.SaberDescriptor.SaberName = EditorGUILayout.TextField("Saber name", saber.SaberDescriptor.SaberName);

            EditorGUI.BeginDisabledGroup(!(saber.LeftSaber && saber.RightSaber));

            GUILayout.Space(5);
            if (GUILayout.Button("Select"))
            {
                Selection.activeGameObject = saber.GameObject;
            }

            GUI.color = (cantExport, isWarning) switch
            {
                (true, _) => new(0.7f, 0f, 0f),
                (_, true) => new(0.7f, 0.46f, 0f),
                _ => new(0.25f, 0.65f, 0.25f)
            };
            if (GUILayout.Button("Export", GUILayout.Height(25)))
            {
                GUI.color = Color.white;
                ModelExporter.ExportModel(saber);
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

    private (bool cantExport, bool isWarning) RunSaberValidation(SaberInfo saber)
    {
        var cantExport = false;
        var isWarning = false;
        var saberBounds = saber.Size;

        if (!saber.LeftSaber)
        {
            cantExport = true;
            GUI.color = Color.red;
            GUILayout.Label(" - LeftSaber gameObject is missing", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (saberBounds.z > SaberTools.SaberLength + 0.1f)
        {
            isWarning = true;
            GUI.color = Color.yellow;
            GUILayout.Label(" - The saber might be too long", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (saberBounds.z < SaberTools.SaberLength - 0.1f)
        {
            isWarning = true;
            GUI.color = Color.yellow;
            GUILayout.Label(" - The saber might be too short", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (saberBounds.x > 1.0)
        {
            isWarning = true;
            GUI.color = Color.yellow;
            GUILayout.Label(" - The saber might be too large", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (saberBounds.x > saberBounds.z || saberBounds.y > saberBounds.z)
        {
            isWarning = true;
            GUI.color = Color.yellow;
            GUILayout.Label(" - Your saber might be rotated incorrectly", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (!saber.HasTrail)
        {
            isWarning = true;
            GUI.color = Color.yellow;
            GUILayout.Label(" - Your saber doesn't have any trails", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }

        if (saber.HasSaberTransforms)
        {
            isWarning = true;
            GUI.color = Color.yellow;
            GUILayout.Label(" - Your Left/Right-Saber gameobject has transforms applied", EditorStyles.boldLabel);
            GUI.color = Color.white;
        }
        return (cantExport, isWarning);
    }
}