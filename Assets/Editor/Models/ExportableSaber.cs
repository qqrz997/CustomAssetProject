using AssetComponents.Components.Sabers;
using UnityEngine;

namespace Editor.Models
{
    internal sealed class ExportableSaber : IExportableAsset
    {
        private readonly SaberDescriptor saberDescriptor;
        
        public ExportableSaber(SaberDescriptor descriptor)
        {
            saberDescriptor = descriptor;
            HasTrail = GameObject.GetComponentInChildren<CustomTrail>();
            var (leftSaber, rightSaber) = (GameObject.transform.Find("LeftSaber"), GameObject.transform.Find("RightSaber"));
            HasSaberTransforms = leftSaber && !IsTransformClear(leftSaber) 
                                 || rightSaber && !IsTransformClear(rightSaber);
        }

        public string TargetBeatSaberDir => "CustomSabers";
        public string FileFormat => ".saber2";
        public string PrefabName => "CustomSaber";

        public GameObject GameObject => saberDescriptor.gameObject;
        
        public string Name => saberDescriptor.saberName;
        public string Author => saberDescriptor.authorName;
        public Texture2D AssetIcon => saberDescriptor.coverImage;

        public bool IsReadyForExport => saberDescriptor.transform.Find("LeftSaber"); // todo - add leftSaber reference
        public bool HasTrail { get; }
        public bool HasSaberTransforms { get; }

        private static bool IsTransformClear(Transform t) => 
            t.localScale == Vector3.one && t.eulerAngles == Vector3.zero;
    }
}
