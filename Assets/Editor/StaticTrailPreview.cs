using SaberComponents.Components;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(CustomTrail))]
[CanEditMultipleObjects]
public class StaticTrailPreview : UnityEditor.Editor
{
    private static readonly Color[] Colors = new Color[4];
    private static readonly int[] Triangles =
    {
        0, 3, 1,
        0, 2, 3
    };
    private static readonly Vector2[] Uvs =
    {
        new(1, 0),
        new(1, 1),
        new(0, 0),
        new(0, 1)
    };

    private static Mesh mesh;
    private static Vector3[] vertices = new Vector3[4];

    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();
    
    public override void OnInspectorGUI()
    {
        serializedObject.Update();
        DrawPropertiesExcluding(serializedObject, "m_Script");
        serializedObject.ApplyModifiedProperties();
    }

    private void OnEnable()
    {
        if (!mesh) mesh = new() { name = "TrailPreviewMesh" };
        UpdateMesh();
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawGizmo(CustomTrail trail, GizmoType gizmoType)
    {
        if (!Settings.showTrailPreview 
            || !mesh || !trail.material || !trail.bottom || !trail.top)
        {
            return;
        }

        var bot = trail.bottom.localPosition;
        var top = trail.top.localPosition;

        var offset = new Vector3(Settings.trailPreviewLength, 0, 0);
        vertices[0] = bot;
        vertices[1] = bot + offset;
        vertices[2] = top;
        vertices[3] = top + offset;

        var color = (trail.useTrailColor ? trail.trailColor : Settings.colorScheme.ColorForType(trail.colorSchemeType)) 
                    * trail.multiplierColor;
        for (int i = 0; i < Colors.Length; i++) Colors[i] = color;

        UpdateMesh();

        trail.material.SetPass(0);
        Graphics.DrawMeshNow(mesh, trail.bottom.parent.localToWorldMatrix);
    }

    private static void UpdateMesh()
    {
        mesh.vertices = vertices;
        mesh.uv = Uvs;
        mesh.triangles = Triangles;
        mesh.colors = Colors;
        mesh.RecalculateNormals();
    }
}