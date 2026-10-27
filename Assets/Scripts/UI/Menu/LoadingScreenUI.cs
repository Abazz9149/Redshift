using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>LOADING (Frame 4): progress bar, percentage, rotating tips. Lives in the "Loading" scene.</summary>
    public class LoadingScreenUI : MonoBehaviour
    {
        public Image progressFill;
        public TMP_Text percentText;
        public TMP_Text tipText;
        public float minimumShowSeconds = 0.8f;   // avoid a flash
        [TextArea]
        public string[] tips =
        {
            "Pass cars closely for NEAR MISS points.",
            "Chain near-misses within 3 seconds to grow your combo.",
            "Near-misses refill your nitro.",
            "Upgrade your engine in the Garage to reach higher speeds.",
            "Oncoming near-misses in Two-Way score double.",
            "Come back every day: Day 7 has the grand prize.",
        };

        IEnumerator Start()
        {
            if (tipText && tips.Length > 0) tipText.text = "TIP: " + tips[Random.Range(0, tips.Length)];

            string target = string.IsNullOrEmpty(SceneLoader.Target) ? "Menu" : SceneLoader.Target;
            var op = SceneManager.LoadSceneAsync(target);
            op.allowSceneActivation = false;

            float shown = 0f;
            float displayed = 0f;
            while (true)
            {
                shown += Time.unscaledDeltaTime;
                float real = Mathf.Clamp01(op.progress / 0.9f);
                displayed = Mathf.MoveTowards(displayed, real, Time.unscaledDeltaTime * 1.5f);
                if (progressFill) progressFill.fillAmount = displayed;
                if (percentText) percentText.text = $"{Mathf.RoundToInt(displayed * 100)}%";

                if (real >= 1f && displayed >= 1f && shown >= minimumShowSeconds) break;
                yield return null;
            }
            op.allowSceneActivation = true;
        }
    }
}
