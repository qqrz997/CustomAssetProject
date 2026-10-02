using System.Collections.Generic;
using AssetComponents.Components.Sabers;
using AssetComponents.Models;
using Editor.Common;
using Editor.Extensions;
using UnityEngine;

namespace Editor.Models
{
    internal sealed class ExportableSaber : IExportableAsset
    {
        private readonly SaberDescriptor saberDescriptor;
        
        private readonly bool hasTrail;
        private readonly bool hasSaberTransforms;
        
        public ExportableSaber(SaberDescriptor descriptor)
        {
            saberDescriptor = descriptor;
            hasTrail = GameObject.GetComponentInChildren<CustomTrail>();
            var (leftSaber, rightSaber) = (GameObject.transform.Find("LeftSaber"), GameObject.transform.Find("RightSaber"));
            hasSaberTransforms = leftSaber && !IsTransformClear(leftSaber) 
                                 || rightSaber && !IsTransformClear(rightSaber);
        }

        public string TargetBeatSaberDir => "CustomSabers";
        public string FileFormat => ".saber2";
        public string PrefabName => AssetBundleDefinition.SaberAssetName;

        public GameObject GameObject => saberDescriptor.gameObject;
        
        public string Name => saberDescriptor.saberName;
        public string Author => saberDescriptor.authorName;
        public Texture2D AssetIcon => saberDescriptor.coverImage;

        public bool IsReadyForExport => saberDescriptor.leftSaber;
        
        public IEnumerable<ValidationMessage> Validate()
        {
            if (!IsReadyForExport)
                yield return ValidationMessage.Error(" - Left Saber is not assigned");

            var saberBounds = GameObject.GetObjectBounds().extents * 2;
            if (saberBounds.z > SaberTools.SaberLength + 0.1f)
                yield return ValidationMessage.Warning(" - The saber might be too long");
            if (saberBounds.z < SaberTools.SaberLength - 0.1f)
                yield return ValidationMessage.Warning(" - The saber might be too short");
            if (saberBounds.x > 1.0)
                yield return ValidationMessage.Warning(" - The saber might be too large");
            if (saberBounds.x > saberBounds.z || saberBounds.y > saberBounds.z)
                yield return ValidationMessage.Warning(" - Your saber might be rotated incorrectly");
            if (!hasTrail)
                yield return ValidationMessage.Warning(" - Your saber doesn't have any trails");
            if (hasSaberTransforms)
                yield return ValidationMessage.Warning(" - Your Left/Right-Saber gameobject has transforms applied");
        }

        private static bool IsTransformClear(Transform t) => 
            t.localScale == Vector3.one && t.eulerAngles == Vector3.zero;
    }
}
