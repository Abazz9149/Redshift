using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>After a crash: CONTINUE (free with Premium, otherwise rewarded ad) with a countdown, or NO THANKS.</summary>
    public class ContinuePanel : MonoBehaviour
    {
        public RaceSession session;
        public GameObject panel;
        public Button continueButton;
        public TMP_Text continueLabel;          // "FREE CONTINUE" / "WATCH AD TO CONTINUE"
        public Button noThanksButton;
        public Image countdownFill;
        public TMP_Text countdownText;
        public float countdownSeconds = 5f;
        public float delayBeforeShow = 0.8f;    // let the crash read before the popup

        float timer;
        float showDelay;
        bool waitingForAd;

        void Awake()
        {
            panel.SetActive(false);
            continueButton.onClick.AddListener(OnContinue);
            noThanksButton.onClick.AddListener(Decline);
        }

        void OnEnable() { session.OnCrashed += HandleCrash; }
        void OnDisable() { session.OnCrashed -= HandleCrash; }

        void HandleCrash()
        {
            if (!session.CanContinue) { session.Finish(); return; }
            showDelay = delayBeforeShow;
            timer = countdownSeconds;
            waitingForAd = false;
            if (continueLabel) continueLabel.text = session.ContinueIsFree ? "FREE CONTINUE" : "WATCH AD TO CONTINUE";
        }

        void Update()
        {
            if (!session.Crashed || session.Finished || waitingForAd) return;
            if (showDelay > 0f)
            {
                showDelay -= Time.unscaledDeltaTime;
                if (showDelay <= 0f) panel.SetActive(true);
                return;
            }
            timer -= Time.unscaledDeltaTime;
            if (countdownFill) countdownFill.fillAmount = timer / countdownSeconds;
            if (countdownText) countdownText.text = Mathf.CeilToInt(Mathf.Max(0f, timer)).ToString();
            if (timer <= 0f) Decline();
        }

        void OnContinue()
        {
            if (session.ContinueIsFree)
            {
                panel.SetActive(false);
                session.Continue();
                return;
            }
            waitingForAd = true;
            Ads.ShowRewarded("continue", ok =>
            {
                waitingForAd = false;
                panel.SetActive(false);
                if (ok) session.Continue();
                else session.Finish();
            });
        }

        void Decline()
        {
            panel.SetActive(false);
            session.Finish();
        }
    }
}
