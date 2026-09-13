using UnityEngine;

namespace Editor.Extensions
{
    internal static class GameObjectExtensions
    {
        public static Bounds GetObjectBounds(this GameObject g)
        {
            var b = new Bounds(g.transform.position, Vector3.zero);
            foreach (var r in g.GetComponentsInChildren<Renderer>()) b.Encapsulate(r.bounds);
            return b;
        }
    }
}