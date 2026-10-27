using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace Redshift
{
    /// <summary>DAILY MISSIONS (Frame 13): 3 rows + "RESET IN 14H 28M".</summary>
    public class MissionsUI : MonoBehaviour
    {
        public MissionRow[] rows;
        public TMP_Text resetText;
        public Button backButton;
        public ScreenRouter router;

        float clock;

        void Awake()
        {
            if (backButton) backButton.onClick.AddListener(() => router.Back());
        }

        void OnEnable()
        {
            MissionManager.OnChanged += Refresh;
            Refresh();
        }

        void OnDisable() => MissionManager.OnChanged -= Refresh;

        void Refresh()
        {
            var list = MissionManager.Active();
            for (int i = 0; i < rows.Length; i++)
            {
                bool has = i < list.Count;
                rows[i].gameObject.SetActive(has);
                if (has) rows[i].Bind(i, list[i], id => MissionManager.Claim(id));
            }
            UpdateClock();
        }

        void Update()
        {
            clock -= Time.unscaledDeltaTime;
            if (clock <= 0f) UpdateClock();
        }

        void UpdateClock()
        {
            clock = 30f;
            MissionManager.EnsureToday();   // rolls over at midnight while the screen is open
            if (resetText) resetText.text = "RESET IN " + TimeUtil.FormatReset(TimeUtil.UntilMidnight);
        }
    }
}
