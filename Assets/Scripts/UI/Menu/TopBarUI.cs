using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>Top bar on every menu screen: PLAYER card + LV, CREDITS, DIAMONDS, settings cog, red-dot badges.</summary>
    public class TopBarUI : MonoBehaviour
    {
        public TMP_Text playerNameText;
        public TMP_Text levelText;
        public Image xpFill;
        public TMP_Text creditsText;
        public TMP_Text diamondsText;

        [Header("Notification dots (optional)")]
        public GameObject rewardsDot;
        public GameObject missionsDot;

        void OnEnable()
        {
            Economy.OnChanged += Refresh;
            MissionManager.OnChanged += Refresh;
            DailyRewardManager.OnChanged += Refresh;
            Refresh();
        }

        void OnDisable()
        {
            Economy.OnChanged -= Refresh;
            MissionManager.OnChanged -= Refresh;
            DailyRewardManager.OnChanged -= Refresh;
        }

        public void Refresh()
        {
            var d = SaveSystem.Data;
            if (playerNameText) playerNameText.text = d.playerName;
            if (GameManager.I != null)
            {
                if (levelText) levelText.text = $"LV.{GameManager.I.PlayerLevel}";
                if (xpFill) xpFill.fillAmount = GameManager.I.LevelProgress01;
            }
            if (creditsText) creditsText.text = d.credits.ToString("N0");
            if (diamondsText) diamondsText.text = d.diamonds.ToString("N0");
            if (rewardsDot) rewardsDot.SetActive(DailyRewardManager.CanClaimToday);
            if (missionsDot) missionsDot.SetActive(MissionManager.ClaimableCount() > 0);
        }
    }
}
