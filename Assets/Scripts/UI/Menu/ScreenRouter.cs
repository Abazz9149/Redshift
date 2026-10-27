using System;
using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// Shows one menu screen at a time. Button OnClick → ScreenRouter.Open("garage") / Back().
    /// Ids used by the kit: home, modes, garage, rewards, missions, store, premium, settings.
    /// </summary>
    public class ScreenRouter : MonoBehaviour
    {
        [Serializable]
        public class ScreenEntry
        {
            public string id;
            public GameObject root;
            [Tooltip("Popups keep the previous screen visible underneath (e.g. premium, settings)")]
            public bool isPopup;
        }

        public List<ScreenEntry> screens = new List<ScreenEntry>();
        public string startScreen = "home";
        [Tooltip("Shown on top of every non-popup screen except those listed in hideTopBarOn")]
        public GameObject topBar;
        public List<string> hideTopBarOn = new List<string> { "settings" };

        public event Action<string> OnScreenChanged;
        public string Current { get; private set; }

        readonly Stack<string> history = new Stack<string>();

        void Start()
        {
            string open = startScreen;
            if (GameManager.I != null && !string.IsNullOrEmpty(GameManager.I.PendingMenuScreen))
            {
                open = GameManager.I.PendingMenuScreen;
                GameManager.I.PendingMenuScreen = null;
            }
            foreach (var s in screens) if (s.root) s.root.SetActive(false);
            Show(open, false);

            // First open of the day: show daily rewards automatically
            if (open == startScreen && DailyRewardManager.CanClaimToday && Find("rewards") != null)
                Open("rewards");
        }

        public void Open(string id) => Show(id, true);

        public void Back()
        {
            if (history.Count == 0) { Show(startScreen, false); return; }
            Show(history.Pop(), false);
        }

        void Show(string id, bool pushHistory)
        {
            var target = Find(id);
            if (target == null) { Debug.LogWarning($"[Router] No screen '{id}'"); return; }

            var current = Find(Current);
            if (pushHistory && Current != null && Current != id) history.Push(Current);

            if (!target.isPopup)
            {
                foreach (var s in screens) if (s.root && s != target) s.root.SetActive(false);
            }
            else if (current != null && current.isPopup && current != target)
            {
                current.root.SetActive(false);
            }

            target.root.SetActive(true);
            Current = id;
            if (topBar) topBar.SetActive(!hideTopBarOn.Contains(id));
            OnScreenChanged?.Invoke(id);
        }

        ScreenEntry Find(string id)
        {
            if (string.IsNullOrEmpty(id)) return null;
            foreach (var s in screens) if (s.id == id) return s;
            return null;
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) && Current != startScreen) Back();   // Android back
#endif
        }
    }
}
