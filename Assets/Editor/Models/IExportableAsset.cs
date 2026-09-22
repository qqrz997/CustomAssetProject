using UnityEngine;

namespace Editor.Models
{
    internal interface IExportableAsset
    {
        public string TargetBeatSaberDir { get; }
        public string FileFormat { get; }
        public string PrefabName { get; }
        
        public GameObject GameObject { get; }
        
        public string Name { get; }
        public string Author { get; }
        public Texture2D AssetIcon { get; }
        
        public bool IsReadyForExport { get; }
    }
}
