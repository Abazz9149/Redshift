using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>DAILY REWARDS (Frame 6). Assign 7 slots in order.</summary>
    public class DailyRewardsUI : MonoBehaviour
    {
        public DailyRewardSlot[] slots;
        public Button backButton;
        public ScreenRouter router;
        public AudioSource claimSfx;

        void Awake()
        {
            if (backButton) backButton.onClick.AddListener(() => router.Back());
        }

        void OnEnable()
        {
            DailyRewardManager.OnChanged += Refresh;
            Refresh();
        }

        void OnDisable() => DailyRewardManager.OnChanged -= Refresh;

        void Refresh()
        {
            int count = Mathf.Min(slots.Length, DailyRewardManager.DayCount);
            for (int i = 0; i < count; i++)
                slots[i].Bind(i, DailyRewardManager.Get(i), DailyRewardManager.StateOf(i), Claim);
        }

        void Claim()
        {
            if (DailyRewardManager.Claim() && claimSfx) claimSfx.Play();
        }
    }
}
