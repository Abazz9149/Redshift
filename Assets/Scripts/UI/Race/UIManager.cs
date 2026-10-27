using UnityEngine;
using TMPro;
using Redshift;

/// <summary>
/// Your original UIManager, kept so existing scene references don't break.
/// If a RaceSession is assigned it shows the kit's score (includes near-miss + combo points)
/// and real road distance. For the full HUD (combo, nitro, mission, rivals) use HUDController.
/// </summary>
public class UIManager : MonoBehaviour
{
    [Header("UI References")]
    public TextMeshProUGUI speedText;
    public TextMeshProUGUI distanceText;
    public TextMeshProUGUI scoreText;

    [Header("Player Reference")]
    public PlayerController playerController;
    public RaceSession session;     // optional

    void Update()
    {
        if (playerController == null) return;

        if (speedText != null)
            speedText.text = $"{Mathf.RoundToInt(playerController.currentSpeed)} KM/H";

        float distanceM = session != null ? session.Stats.distanceM : playerController.distanceTraveled;
        if (distanceText != null)
            distanceText.text = $"{distanceM / 1000f:F2} KM";

        float score = session != null ? session.Stats.score : playerController.currentScore;
        if (scoreText != null)
            scoreText.text = $"{Mathf.RoundToInt(score):N0}";
    }
}
