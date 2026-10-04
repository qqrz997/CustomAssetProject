using AssetComponents.Components;
using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(MaterialColorer))]
[CanEditMultipleObjects]
public class MaterialColorerPreviewer : UnityEditor.Editor
{
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();
    
    public static void RefreshAll()
    {
        EditorApplication.delayCall -= RefreshAll;
        foreach (var colorer in Resources.FindObjectsOfTypeAll<MaterialColorer>())
        {
            if (colorer.gameObject.scene.IsValid() && colorer.gameObject.scene.isLoaded) ApplyPreview(colorer);
        }
    }

    [DrawGizmo(GizmoType.Selected | GizmoType.NonSelected)]
    private static void DrawGizmo(MaterialColorer colorer, GizmoType gizmoType)
    {
        ApplyPreview(colorer);
    }
    
    private static void ApplyPreview(MaterialColorer colorer)
    {
        if (colorer == null) return;
        var color = Settings.colorScheme.ColorForType(colorer.ColorSchemeType);
        colorer.SetColor(color);
    }
}
