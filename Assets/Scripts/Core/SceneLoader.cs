using UnityEngine.SceneManagement;

namespace Redshift
{
    /// <summary>Goes through the "Loading" scene (Frame: LOADING... 82%) to reach the target scene.</summary>
    public static class SceneLoader
    {
        public const string LoadingScene = "Loading";
        public static string Target { get; private set; }

        public static void Load(string sceneName)
        {
            UnityEngine.Time.timeScale = 1f;
            Target = sceneName;
            SceneManager.LoadScene(LoadingScene);
        }

        public static void ToMenu(string openScreen = null)
        {
            if (GameManager.I != null)
            {
                GameManager.I.PendingMenuScreen = openScreen;
                Load(GameManager.I.menuScene);
            }
            else Load("Menu");
        }

        public static void ToRace()
        {
            Load(GameManager.I != null ? GameManager.I.raceScene : "Race");
        }
    }
}
