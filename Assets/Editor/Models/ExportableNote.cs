using System.Collections.Generic;
using AssetComponents.Components.Notes;
using AssetComponents.Models;
using Editor.Common;
using UnityEngine;

namespace Editor.Models
{
    internal sealed class ExportableNote : IExportableAsset
    {
        private readonly NoteSet leftNotes;
        
        public ExportableNote(NoteDescriptor descriptor)
        {
            leftNotes = descriptor.leftNotes;
            GameObject = descriptor.gameObject;
            Name = descriptor.name;
            Author = descriptor.authorName;
            AssetIcon = descriptor.icon;
        }
        
        public string TargetBeatSaberDir => "CustomNotes";
        public string FileFormat => ".bloq2";
        public string PrefabName => AssetBundleDefinition.NoteAssetName;

        public GameObject GameObject { get; }
        
        public string Name { get; }
        public string Author { get; }
        public Texture2D AssetIcon { get; }

        public bool IsReadyForExport => leftNotes && leftNotes.noteArrow;
        public IEnumerable<ValidationMessage> Validate()
        {
            yield break;
        }
    }
}
