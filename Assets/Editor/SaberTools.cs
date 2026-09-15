using System.Collections.Generic;
using System.Linq;
using Editor.Extensions;
using SaberComponents.Components;
using SaberComponents.Models;
using UnityEditor;
using UnityEngine;
using Object = UnityEngine.Object;

public class SaberTools : EditorWindow
{
    private static SaberProjectSettings Settings => SaberProjectSettings.GetOrCreateSettings();

    public const float SaberLength = 1.179f;
    private const float SaberOffset = 0.1745f;

    public bool beatSaberLookActive;

    private string templateText = "NewSaber";
    private GameObject templatePrefab;
    private Material trailMaterial;
    private float trailLength = 0.4f;
    private float trailWidth = 0.5f;

    private bool isCreateSaberOpen;
    private bool isGuidesOpen;
    private bool isFixingOpen;
    private bool isOtherToolsOpen;

    private Vector2 scrollPos = Vector2.zero;

    private SaberDescriptor selectedDescriptor;

    [MenuItem("Window/Saber Project/Saber Tools")]
    public static void OpenSaberTools()
    {
        GetWindow<SaberTools>(false, "Saber Tools");
    }

    public void OnGUI()
    {
        GUILayout.Space(5);

        scrollPos = GUILayout.BeginScrollView(scrollPos);

        UITools.Header("Visuals");
        UITools.Foldout(ref isGuidesOpen);
        if (isGuidesOpen)
        {
            UITools.Header("Custom Colors");
            EditorGUILayout.BeginHorizontal();
            Settings.customColorLeft = EditorGUILayout.ColorField("Left", Settings.customColorLeft);
            GUILayout.Space(5);
            Settings.customColorRight = EditorGUILayout.ColorField("Right", Settings.customColorRight);
            EditorGUILayout.EndHorizontal();
            
            GUILayout.Space(10);
            UITools.Header("Guides");
            UITools.ChangedToggle(ref Settings.showSaberGuides, "Sabers Enabled", val =>
            {
                SceneView.RepaintAll();
            });

            if (Settings.showSaberGuides) UITools.ChangedToggle(ref Settings.showTrailGuides, "Trails Enabled", val =>
            {
                SceneView.RepaintAll();
            });
            
            GUILayout.Space(10);
            UITools.Header("Trail Preview");
            UITools.ChangedToggle(ref Settings.showTrailPreview, "Enabled", val =>
            {
                SceneView.RepaintAll();
            });

            if (Settings.showTrailPreview)
            {
                var newTrailPreviewLength = EditorGUILayout.Slider("Preview length", Settings.trailPreviewLength, 0, 1);
                if (!Mathf.Approximately(newTrailPreviewLength, Settings.trailPreviewLength))
                {
                    SceneView.RepaintAll();
                }
                Settings.trailPreviewLength = newTrailPreviewLength;
            }
        }

        GUILayout.Space(15);
        UITools.Header("Create Saber");
        UITools.Foldout(ref isCreateSaberOpen);
        if (isCreateSaberOpen)
        {
            UITools.Header("General");
            templateText = EditorGUILayout.TextField("Name", templateText);
            GUILayout.Space(2);
            templatePrefab =
                (GameObject) EditorGUILayout.ObjectField("Template Prefab", templatePrefab, typeof(GameObject), false);
            GUILayout.Space(5);
            UITools.Header("Trails");
            trailMaterial = (Material) EditorGUILayout.ObjectField("Trail Material", trailMaterial, typeof(Material), false);
            trailLength = EditorGUILayout.Slider("Trail Length", trailLength, 0f, 1f);
            trailWidth = EditorGUILayout.Slider("Trail Width", trailWidth, 0f, SaberLength);
            GUILayout.Space(10);
            if (GUILayout.Button("Create Template", GUILayout.Height(20)))
            {
                CreateTemplate();
            }
        }

        GUILayout.Space(15);
        UITools.Header("Fixing");
        UITools.Foldout(ref isFixingOpen);
        if (isFixingOpen)
        {
            if (UITools.Button("Fix Length"))
            {
                FixLength();
            }
        }

        GUILayout.Space(15);
        UITools.Header("Other tools");
        UITools.Foldout(ref isOtherToolsOpen);
        if (isOtherToolsOpen)
        {
            if (UITools.Button("Select all renderers"))
            {
                var go = Selection.activeGameObject;
                if (go)
                {
                    var gos = new List<GameObject>();
                    foreach (var meshRenderer in go.GetComponentsInChildren<MeshRenderer>())
                    {
                        gos.Add(meshRenderer.gameObject);
                    }
                    Selection.objects = gos.Cast<Object>().ToArray();
                }
            }

            GUILayout.Space(5);
            GUILayout.Label("Select trail transform");

            GUILayout.BeginHorizontal();
            if (UITools.Button("Bottom"))
            {
                SelectTrailTransform(Selection.activeGameObject, false);
            }

            if (UITools.Button("Top"))
            {
                SelectTrailTransform(Selection.activeGameObject, true);
            }
            GUILayout.EndHorizontal();

            GUILayout.Space(10);

            if (UITools.Button("Create spinning anim", 23))
            {
                if (Selection.activeGameObject)
                {
                    var animCreator = AnimCreatorWindow.Open();
                    animCreator.Setup(new()
                    {
                        GameObject = Selection.activeGameObject
                    });
                }
                else
                {
                    SaberProjectOverlay.ShowNotification("Select a gameobject first");
                }
            }
        }

        GUILayout.EndScrollView();
    }

    public void OnFocus()
    {
        selectedDescriptor = Selection.activeGameObject ? Selection.activeGameObject.GetComponent<SaberDescriptor>() : null;
    }

    private static void FixLength()
    {
        foreach (var gameObject in Selection.gameObjects)
        {
            var t = gameObject.transform;
            var localToWorld = t.localToWorldMatrix;
            var worldToLocal = t.worldToLocalMatrix;

            var ogScale = Abs(localToWorld.rotation*t.localScale);
            ogScale.z = 1f;
            t.localScale = Abs(worldToLocal.rotation * ogScale);

            var bounds = gameObject.GetObjectBounds().extents * 2;
            var targetZ = SaberLength / bounds.z;
            ogScale.z = targetZ;
            t.localScale = Abs(worldToLocal.rotation * ogScale);
        }
        return;
        static Vector3 Abs(Vector3 vec) => new(Mathf.Abs(vec.x), Mathf.Abs(vec.y), Mathf.Abs(vec.z));
    }

    private static void SelectTrailTransform(GameObject root, bool top)
    {
        var trails = new List<GameObject>();

        foreach (var trail in root.GetComponentsInChildren<CustomTrail>())
        {
            var go = top ? trail.top : trail.bottom;
            if (go) trails.Add(go.gameObject);
        }

        Selection.objects = trails.ToArray();
    }

    public void CreateTemplate()
    {
        var rootGo = new GameObject(templateText);
        var saberDescriptor = rootGo.AddComponent<SaberDescriptor>();
        saberDescriptor.SaberName = templateText;
        saberDescriptor.AuthorName = Settings.author;
        
        CreateSaber(ColorSchemeType.RightSaber, 0.3f);
        CreateSaber(ColorSchemeType.LeftSaber, -0.3f);
        
        void CreateSaber(ColorSchemeType colorType, float spacing)
        {
            var go = new GameObject(colorType.ToString());
            go.transform.parent = rootGo.transform;
            go.transform.position = new(spacing, 0, 0);
            CreateTrail(go, colorType);
            // todo - may want to flip assigned ColorSchemeTypes when using a template
            if (templatePrefab) Instantiate(templatePrefab, go.transform, false);
        }

        Selection.activeGameObject = rootGo;
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawGizmos(SaberDescriptor descriptor, GizmoType gizmoType)
    {
        if (Settings == null || !Settings.showSaberGuides)
        {
            return;
        }

        foreach (Transform t in descriptor.transform)
        {
            switch (t.name)
            {
                case "LeftSaber":
                    DrawSaberGizmo(t, Settings.customColorLeft);
                    break;
                case "RightSaber":
                    DrawSaberGizmo(t, Settings.customColorRight);
                    break;
            }
        }
    }

    private static void DrawSaberGizmo(Transform t, Color color)
    {
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

    private void CreateTrail(GameObject saberGo, ColorSchemeType colorSchemeType)
    {
        var trail = saberGo.AddComponent<CustomTrail>();
        trail.material = trailMaterial;
        trail.length = trailLength;
        trail.colorSchemeType = colorSchemeType;

        var trailGuides = new GameObject("Trail Guides").transform;
        trailGuides.parent = saberGo.transform;
        trailGuides.localPosition = Vector3.zero;

        var trailGuideTop = new GameObject("Trail Top").transform;
        trailGuideTop.parent = trailGuides;
        trailGuideTop.localPosition = new(0, 0, SaberLength - SaberOffset);

        var trailGuideBottom = new GameObject("Trail Bottom").transform;
        trailGuideBottom.parent = trailGuides;
        trailGuideBottom.localPosition = new(0, 0, SaberLength - SaberOffset - trailWidth);

        trail.top = trailGuideTop;
        trail.bottom = trailGuideBottom;
    }
}