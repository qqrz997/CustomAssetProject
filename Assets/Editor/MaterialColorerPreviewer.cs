using AssetComponents.Components;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public class MaterialColorerPreviewer
{
    private static ProjectSettings Settings => ProjectSettings.GetOrCreateSettings();
    
    static MaterialColorerPreviewer()
    {
        EditorSceneManager.sceneOpened -= OnSceneOpened;
        EditorSceneManager.sceneOpened += OnSceneOpened;
        EditorApplication.delayCall += RefreshAll;
        Undo.postprocessModifications -= OnPostprocessModifications;
        Undo.postprocessModifications += OnPostprocessModifications;
    }

    private static void OnSceneOpened(Scene scene, OpenSceneMode mode) => RefreshAll();

    public static void RefreshAll()
    {
        EditorApplication.delayCall -= RefreshAll;
        foreach (var colorer in Resources.FindObjectsOfTypeAll<MaterialColorer>())
        {
            if (colorer.gameObject.scene.IsValid() && colorer.gameObject.scene.isLoaded) ApplyPreview(colorer);
        }
    }

    private static UndoPropertyModification[] OnPostprocessModifications(UndoPropertyModification[] modifications)
    {
        foreach (var modification in modifications)
        {
            if (modification.currentValue.target is MaterialColorer materialColorer) ApplyPreview(materialColorer);
        }

        return modifications;
    }
    
    private static void ApplyPreview(MaterialColorer colorer)
    {
        if (colorer == null) return;
        var color = Settings.colorScheme.ColorForType(colorer.ColorSchemeType);
        colorer.SetColor(color);
    }
}
