using Editor.Extensions;
using SaberComponents.Components;
using UnityEngine;

namespace Editor.Models
{
    public class SaberInfo
    {
        private readonly CustomTrail[] customTrails;
        
        public SaberInfo(SaberDescriptor descriptor)
        {
            SaberDescriptor = descriptor;
            customTrails = descriptor.gameObject.GetComponentsInChildren<CustomTrail>();
            LeftSaber = SaberDescriptor.transform.Find("LeftSaber");
            RightSaber = SaberDescriptor.transform.Find("RightSaber");

            HasSaberTransforms = LeftSaber && !IsTransformClear(LeftSaber) 
                                 || RightSaber && !IsTransformClear(RightSaber);
        }
        
        private Vector3? size;
        private Transform transform;

        public SaberDescriptor SaberDescriptor { get; }
        public Transform LeftSaber { get; }
        public Transform RightSaber { get; }
        public bool HasSaberTransforms { get; }
        
        public GameObject GameObject => SaberDescriptor.gameObject;
        public bool HasTrail => customTrails is { Length: > 0 };
        public bool IsValid => SaberDescriptor != null;

        public Vector3 Size => size ??= GameObject.GetObjectBounds().extents * 2;
        
        static bool IsTransformClear(Transform t)
        {
            return t.localScale == Vector3.one && t.eulerAngles == Vector3.zero;
        }
    }
}