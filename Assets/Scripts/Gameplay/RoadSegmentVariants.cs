using UnityEngine;

namespace Redshift
{
    /// <summary>Put on a road segment with child roadside sets (trees, city, bridge rails...). One is shown at a time.</summary>
    public class RoadSegmentVariants : MonoBehaviour
    {
        public GameObject[] variants;

        void Start() => PickRandom();

        public void PickRandom()
        {
            if (variants == null || variants.Length == 0) return;
            int pick = Random.Range(0, variants.Length);
            for (int i = 0; i < variants.Length; i++)
                if (variants[i]) variants[i].SetActive(i == pick);
        }
    }
}
