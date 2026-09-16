using SaberComponents.Components;
using SaberComponents.Models;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public class MaterialColorerPreviewer
{
    private static SaberProjectSettings Settings => SaberProjectSettings.GetOrCreateSettings();
    
    static MaterialColorerPreviewer()
    {
        Undo.postprocessModifications -= OnPostprocessModifications;
        Undo.postprocessModifications += OnPostprocessModifications;
    }

    public static void RefreshAll()
    {
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
        colorer.materialPropertyBlock ??= new();
        colorer.materialPropertyBlock.SetColor(colorer.propertyName,
            Settings.colorScheme.ColorForType(colorer.colorSchemeType));
        colorer.meshRenderer.SetPropertyBlock(colorer.materialPropertyBlock);
    }
}
