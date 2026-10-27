using UnityEngine;

namespace Redshift
{
    /// <summary>Car size from its mesh renderers (same method as AICarController.MeasureSize).</summary>
    public struct CarBounds
    {
        public float halfX, halfZ;   // half width / half length
        public float offX, offZ;     // mesh centre minus pivot

        public static CarBounds Measure(Transform meshRoot, Transform pivot)
        {
            var rs = meshRoot.GetComponentsInChildren<Renderer>();
            var b = new Bounds();
            bool has = false;
            foreach (var r in rs)
            {
                if (!(r is MeshRenderer) && !(r is SkinnedMeshRenderer)) continue;
                if (!has) { b = r.bounds; has = true; }
                else b.Encapsulate(r.bounds);
            }

            if (!has || b.size.sqrMagnitude < 0.01f)
                return new CarBounds { halfX = 1f, halfZ = 2.2f };

            return new CarBounds
            {
                halfX = Mathf.Max(0.6f, b.extents.x),
                halfZ = Mathf.Max(1.5f, b.extents.z),
                offX = b.center.x - pivot.position.x,
                offZ = b.center.z - pivot.position.z,
            };
        }
    }
}
