using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>RACE COMPLETE (Frame 16): score count-up, high score delta, coins, distance, double-coins ad.</summary>
    public class ResultsUI : MonoBehaviour
    {
        public RaceSession session;
        public GameObject panel;

        [Header("Texts")]
        public TMP_Text scoreText;
        public TMP_Text highScoreText;
        public TMP_Text recordDeltaText;        // "-7,000 TO ALL-TIME RECORD" or "NEW RECORD!"
        public GameObject newRecordBadge;
        public TMP_Text coinsText;
        public TMP_Text coinsBonusText;         // "+2X PREMIUM" etc.
        public TMP_Text distanceText;
        public TMP_Text statsLineText;          // "TOP SPEED 212 KM/H · NEAR MISSES 14 · BEST COMBO x6"
        public Image recordProgressFill;        // % of high score

        [Header("Buttons")]
        public Button doubleCoinsButton;
        public Button restartButton;
        public Button changeCarButton;
        public Button menuButton;

        public float countUpSeconds = 1.2f;

        RaceResult result;

        void Awake()
        {
            panel.SetActive(false);
            restartButton.onClick.AddListener(() => { Ads.MaybeShowInterstitial(); SceneLoader.ToRace(); });
            changeCarButton.onClick.AddListener(() => { Ads.MaybeShowInterstitial(); SceneLoader.ToMenu("garage"); });
            menuButton.onClick.AddListener(() => { Ads.MaybeShowInterstitial(); SceneLoader.ToMenu(); });
            if (doubleCoinsButton) doubleCoinsButton.onClick.AddListener(DoubleCoins);
        }

        void OnEnable() => session.OnFinished += Show;
        void OnDisable() => session.OnFinished -= Show;

        void Show(RaceResult r)
        {
            result = r;
            panel.SetActive(true);
            var s = r.stats;

            int best = Mathf.Max(r.previousHighScore, s.score);
            if (highScoreText) highScoreText.text = best.ToString("N0");
            if (newRecordBadge) newRecordBadge.SetActive(r.newRecord);
            if (recordDeltaText)
                recordDeltaText.text = r.newRecord
                    ? "NEW RECORD!"
                    : $"-{r.previousHighScore - s.score:N0} TO ALL-TIME RECORD ({r.previousHighScore:N0} PTS)";
            if (recordProgressFill) recordProgressFill.fillAmount = best > 0 ? s.score / (float)best : 1f;

            if (distanceText) distanceText.text = $"{s.distanceM / 1000f:0.00} KM";
            if (statsLineText)
                statsLineText.text = $"TOP SPEED {Mathf.RoundToInt(s.topSpeedKmh)} KM/H  ·  NEAR MISSES {s.nearMisses}  ·  BEST COMBO x{s.bestCombo}"
                                     + (s.rivalsBeaten > 0 ? $"  ·  RIVALS BEATEN {s.rivalsBeaten}" : "");
            if (coinsBonusText) coinsBonusText.text = SaveSystem.Data.premium ? "2X PREMIUM BONUS" : "";
            if (doubleCoinsButton) doubleCoinsButton.gameObject.SetActive(r.coins > 0);

            StartCoroutine(CountUp(s.score, r.coins));
        }

        IEnumerator CountUp(int score, int coins)
        {
            float t = 0f;
            while (t < countUpSeconds)
            {
                t += Time.unscaledDeltaTime;
                float k = 1f - Mathf.Pow(1f - Mathf.Clamp01(t / countUpSeconds), 3f);
                if (scoreText) scoreText.text = Mathf.RoundToInt(score * k).ToString("N0");
                if (coinsText) coinsText.text = Mathf.RoundToInt(coins * k).ToString("N0");
                yield return null;
            }
            if (scoreText) scoreText.text = score.ToString("N0");
            if (coinsText) coinsText.text = coins.ToString("N0");
        }

        void DoubleCoins()
        {
            if (result == null || result.coinsDoubled) return;
            doubleCoinsButton.interactable = false;
            Ads.ShowRewarded("double_coins", ok =>
            {
                if (!ok) { doubleCoinsButton.interactable = true; return; }
                result.coinsDoubled = true;
                Economy.Add(Currency.Credits, result.coins);
                if (coinsText) coinsText.text = (result.coins * 2).ToString("N0");
                doubleCoinsButton.gameObject.SetActive(false);
            });
        }
    }
}
