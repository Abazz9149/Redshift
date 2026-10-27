using System.Collections;
using TMPro;
using UnityEngine;

namespace Redshift
{
    /// <summary>"NEAR MISS! +250" punch-scale popup above the car.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public class NearMissPopup : MonoBehaviour
    {
        public TMP_Text label;
        public TMP_Text pointsText;
        public float duration = 0.9f;
        public float rise = 60f;
        public Color normalColor = new Color(1f, 0.77f, 0f);
        public Color hotColor = new Color(1f, 0.28f, 0.28f);
        public AudioSource sfx;

        CanvasGroup group;
        RectTransform rect;
        Vector2 basePos;
        Coroutine routine;

        void Awake()
        {
            group = GetComponent<CanvasGroup>();
            rect = (RectTransform)transform;
            basePos = rect.anchoredPosition;
            group.alpha = 0f;
        }

        public void Show(int points, int combo, bool oncoming)
        {
            string text = oncoming ? "ONCOMING!" : combo >= 6 ? "INSANE!" : combo >= 3 ? "CLOSE CALL!" : "NEAR MISS!";
            if (label)
            {
                label.text = text;
                label.color = combo >= 6 || oncoming ? hotColor : normalColor;
            }
            if (pointsText) pointsText.text = $"+{points:N0}";
            if (sfx) { sfx.pitch = 1f + 0.05f * Mathf.Min(combo, 10); sfx.Play(); }

            if (routine != null) StopCoroutine(routine);
            routine = StartCoroutine(Animate());
        }

        IEnumerator Animate()
        {
            float t = 0f;
            while (t < duration)
            {
                t += Time.unscaledDeltaTime;
                float k = t / duration;
                float punch = k < 0.15f ? Mathf.Lerp(0.6f, 1.25f, k / 0.15f) : Mathf.Lerp(1.25f, 1f, (k - 0.15f) / 0.25f);
                rect.localScale = Vector3.one * punch;
                rect.anchoredPosition = basePos + Vector2.up * rise * k;
                group.alpha = k < 0.7f ? 1f : 1f - (k - 0.7f) / 0.3f;
                yield return null;
            }
            group.alpha = 0f;
            rect.anchoredPosition = basePos;
        }
    }
}
