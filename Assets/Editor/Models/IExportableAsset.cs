using System.Collections.Generic;
using Editor.Common;
using UnityEngine;

namespace Editor.Models
{
    internal interface IExportableAsset
    {
        string TargetBeatSaberDir { get; }
        string FileFormat { get; }
        string PrefabName { get; }
        
        GameObject GameObject { get; }
        
        string Name { get; }
        string Author { get; }
        Texture2D AssetIcon { get; }
        
        bool IsReadyForExport { get; }

        IEnumerable<ValidationMessage> Validate();
    }
}
