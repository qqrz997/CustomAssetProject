using System;
using AssetComponents.Components.Notes;
using AssetComponents.Models;
using UnityEngine;

namespace Editor.Models
{
    internal sealed class ExportableNote : IExportableAsset
    {
        private readonly NoteDescriptor noteDescriptor;
        
        public ExportableNote(NoteDescriptor descriptor)
        {
            noteDescriptor = descriptor;
        }
        
        public string TargetBeatSaberDir => "CustomNotes";
        public string FileFormat => ".bloq2";
        public string PrefabName => AssetBundleDefinition.NoteAssetName;

        public GameObject GameObject => noteDescriptor.gameObject;
        
        public string Name => noteDescriptor.noteName;
        public string Author => noteDescriptor.authorName;
        public Texture2D AssetIcon => noteDescriptor.icon;

        public bool IsReadyForExport => throw new NotImplementedException();
    }
}
