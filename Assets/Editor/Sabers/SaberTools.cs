using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AssetComponents.Components;
using AssetComponents.Components.Sabers;
using AssetComponents.Models;
using Editor.Extensions;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SaberTools : EditorWindow
{
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();

    public const float SaberLength = 1.179f;
    private const float SaberOffset = 0.1745f;

    private string templateName = "NewSaber";
    private GameObject templatePrefab;
    private Material trailMaterial;
    private float trailLength = 0.4f;
    private float trailWidth = 0.5f;
    
    private Vector2 scrollPos = Vector2.zero;

    [MenuItem("Window/BS Asset Project/Saber Tools")]
    public static void OpenSaberTools()
    {
        GetWindow<SaberTools>(false, "Saber Tools");
    }

    public void OnGUI()
    {
        GUILayout.Space(5);

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        UITools.Header("Visuals");
        UITools.ChangedToggle(ref Settings.showGuides, "Show Guides", _ => SceneView.RepaintAll());
        if (Settings.showGuides) 
            UITools.ChangedToggle(ref Settings.showTrailGuides, "Trail Guides", _ => SceneView.RepaintAll());
        
        GUILayout.Space(10);
        UITools.Header("Trail Preview");
        UITools.ChangedToggle(ref Settings.showTrailPreview, "Enabled", _ => SceneView.RepaintAll());
        if (Settings.showTrailPreview)
        {
            var newTrailPreviewLength = EditorGUILayout.Slider("Preview length", Settings.trailPreviewLength, 0, 1);
            if (!Mathf.Approximately(newTrailPreviewLength, Settings.trailPreviewLength)) SceneView.RepaintAll();
            Settings.trailPreviewLength = newTrailPreviewLength;
        }

        GUILayout.Space(15);
        UITools.Header("Create Saber");
        templateName = EditorGUILayout.TextField("Name", templateName);
        templatePrefab =
            (GameObject) EditorGUILayout.ObjectField("Template Prefab", templatePrefab, typeof(GameObject), false);
        GUILayout.Space(10);
        trailMaterial = (Material) EditorGUILayout.ObjectField("Trail Material", trailMaterial, typeof(Material), false);
        trailLength = EditorGUILayout.Slider("Trail Length", trailLength, 0f, 1f);
        trailWidth = EditorGUILayout.Slider("Trail Width", trailWidth, 0f, SaberLength);
        GUILayout.Space(10);
        if (GUILayout.Button("Create Template", GUILayout.Height(20)))
        {
            CreateTemplate();
        }

        GUILayout.Space(15);
        UITools.Header("Other tools");
        if (UITools.Button("Select all renderers"))
        {
            UITools.SelectAllRenderers();
        }

        GUILayout.EndScrollView();
    }
    
    private void CreateTemplate()
    {
        var rootGo = new GameObject(templateName);
        var saberDescriptor = rootGo.AddComponent<SaberDescriptor>();
        saberDescriptor.saberName = templateName;
        saberDescriptor.authorName = Settings.author;
        
        CreateSaber(rootGo.transform, ColorSchemeType.LeftSaber, -0.3f);
        CreateSaber(rootGo.transform, ColorSchemeType.RightSaber, 0.3f);

        MaterialColorerPreviewer.RefreshAll();
        Selection.activeGameObject = rootGo;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawGizmos(SaberDescriptor descriptor, GizmoType gizmoType)
    {
        if (!Settings.showGuides)
        {
            return;
        }

        foreach (Transform t in descriptor.transform)
        {
            if (t.name == "LeftSaber") DrawSaberGizmo(t, Settings.colorScheme.saberAColor);
            else if (t.name == "RightSaber") DrawSaberGizmo(t, Settings.colorScheme.saberBColor);
        }
    }

    private static void DrawSaberGizmo(Transform t, Color color)
    {
        color.a = Settings.guidesTransparency;
        Gizmos.color = color;
        Gizmos.DrawWireCube(t.position+new Vector3(0, 0, SaberLength/2-SaberOffset), new(0.05f, 0.05f, SaberLength));

        if (Settings.showTrailGuides)
        {
            foreach (var trail in t.GetComponentsInChildren<CustomTrail>())
                if (trail && trail.bottom && trail.top) DrawTrailGizmo(trail);
        }
        Gizmos.color = Color.white;
    }

    private static void DrawTrailGizmo(CustomTrail trail)
    {
        var trailWidth = trail.top.position.z - trail.bottom.position.z;
        var gizmoWidth = Settings.trailPreviewLength;
        Gizmos.DrawWireCube(
            trail.bottom.position + new Vector3(0.025f + gizmoWidth / 2, 0, trailWidth/ 2),
            new(gizmoWidth, 0.05f, trailWidth));
    }
        
    private void CreateSaber(Transform parent, ColorSchemeType colorType, float spacing)
    {
        var go = new GameObject(colorType.ToString());
        go.transform.SetParent(parent, false);
        go.transform.position = new(spacing, 0, 0);
        CreateTrail(go, trailMaterial, trailLength, trailWidth, colorType);
        if (!templatePrefab) return;
        var instance = Instantiate(templatePrefab, go.transform, false);
        if (colorType == ColorSchemeType.RightSaber)
            foreach (var colorer in instance.GetComponentsInChildren<MaterialColorer>()) colorer.MirrorColorType();
    }

    private static void CreateTrail(GameObject parent, Material mat, float length, float width, ColorSchemeType type)
    {
        var trail = parent.AddComponent<CustomTrail>();
        trail.material = mat;
        trail.length = length;
        
        var colorer = parent.AddComponent<TrailColorer>();
        var field = colorer.GetType().GetField("colorSchemeType", BindingFlags.Instance | BindingFlags.NonPublic)
            ?? throw new NullReferenceException();
        field.SetValue(colorer, type);

        var trailGuides = new GameObject("Trail Guides").transform;
        trailGuides.parent = parent.transform;
        trailGuides.localPosition = Vector3.zero;

        var trailGuideTop = new GameObject("Trail Top").transform;
        trailGuideTop.parent = trailGuides;
        trailGuideTop.localPosition = new(0, 0, SaberLength - SaberOffset);

        var trailGuideBottom = new GameObject("Trail Bottom").transform;
        trailGuideBottom.parent = trailGuides;
        trailGuideBottom.localPosition = new(0, 0, SaberLength - SaberOffset - width);

        trail.top = trailGuideTop;
        trail.bottom = trailGuideBottom;
    }
}