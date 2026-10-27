using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>One DAY card on the DAILY REWARDS screen.</summary>
    public class DailyRewardSlot : MonoBehaviour
    {
        public TMP_Text dayText;          // "DAY 01"
        public TMP_Text amountText;       // "100"
        public TMP_Text currencyText;     // "CREDITS" / "DIAMONDS" / car name
        public Image icon;
        public Sprite creditSprite;
        public Sprite diamondSprite;
        public Button claimButton;

        [Header("State visuals")]
        public GameObject readyState;     // red border + READY tag + red CLAIM
        public GameObject upcomingState;  // UPCOMING tag + grey CLAIM
        public GameObject claimedState;   // check mark / dimmed

        public void Bind(int day, DailyReward reward, RewardSlotState state, System.Action onClaim)
        {
            if (dayText) dayText.text = $"DAY {day + 1:00}";
            if (amountText) amountText.text = reward.amount.ToString("N0");
            if (currencyText)
                currencyText.text = reward.car != null ? reward.car.displayName
                    : reward.currency == Currency.Credits ? "CREDITS" : "DIAMONDS";
            if (icon) icon.sprite = reward.car != null && reward.car.thumbnail != null ? reward.car.thumbnail
                : reward.currency == Currency.Credits ? creditSprite : diamondSprite;

            if (readyState) readyState.SetActive(state == RewardSlotState.Ready);
            if (upcomingState) upcomingState.SetActive(state == RewardSlotState.Upcoming);
            if (claimedState) claimedState.SetActive(state == RewardSlotState.Claimed);

            claimButton.interactable = state == RewardSlotState.Ready;
            claimButton.onClick.RemoveAllListeners();
            claimButton.onClick.AddListener(() => onClaim());
        }
    }
}
