using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>One card on DAILY MISSIONS: tag, title, description, progress bar, reward, CLAIM.</summary>
    public class MissionRow : MonoBehaviour
    {
        public TMP_Text tagText;          // "MISSION 01 · SPRINT OBJECTIVE"
        public TMP_Text titleText;        // "OVERTAKE 15 CARS"
        public TMP_Text descriptionText;
        public TMP_Text progressText;     // "0 / 15"
        public Image progressFill;
        public TMP_Text rewardText;       // "500 COINS"
        public Button claimButton;
        public TMP_Text claimLabel;       // CLAIM / CLAIMED
        public GameObject completeHighlight;

        public void Bind(int index, MissionManager.Entry e, System.Action<string> onClaim)
        {
            var d = e.def;
            if (tagText) tagText.text = $"MISSION {index + 1:00} · {d.tag}";
            if (titleText) titleText.text = d.title;
            if (descriptionText) descriptionText.text = d.description;
            if (progressText) progressText.text = d.FormatProgress(e.progress.progress);
            if (progressFill) progressFill.fillAmount = e.progress.progress / (float)Mathf.Max(1, d.target);

            int amount = d.rewardAmount * (SaveSystem.Data.premium ? 2 : 1);
            if (rewardText) rewardText.text = $"{amount:N0} {(d.rewardCurrency == Currency.Credits ? "COINS" : "DIAMONDS")}";

            claimButton.interactable = e.Claimable;
            if (claimLabel) claimLabel.text = e.progress.claimed ? "CLAIMED" : "CLAIM";
            if (completeHighlight) completeHighlight.SetActive(e.Claimable);

            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => onClaim(d.id));
        }
    }
}
