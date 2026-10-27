using UnityEngine;

// Ab ye script khud Update/Raycast nahi chalati.
// RoadManagerRecycle har frame ek hi loop me sabko Tick() karta hai (20 raycast/frame ka kharcha khatam).
public class TrafficAI : MonoBehaviour
{
    public enum TrafficState { Driving, SlowingDown }

    [HideInInspector] public float currentSpeed;
    [HideInInspector] public float baseSpeed;
    [HideInInspector] public int laneIndex;
    [HideInInspector] public TrafficState state;

    // gameObject.activeInHierarchy native call hai (slow), ye bool free hai
    public bool IsActive { get; private set; }

    public float safeDistance = 15f;

    // Purane prefabs ka serialized data na toote isliye field rakhi hai, ab use nahi hoti
    [HideInInspector] public LayerMask trafficLayer;

    const float Response = 2f;

    // frontGap = same lane me aage wali car tak z distance, frontSpeed = uski speed
    public void Tick(float dt, float frontGap, float frontSpeed)
    {
        float target = baseSpeed;

        if (frontGap < safeDistance)
        {
            state = TrafficState.SlowingDown;
            // Aage wali car ki speed match karo (pehle 0 tak ruk jaati thi = jhatke wali braking)
            target = Mathf.Min(baseSpeed, frontSpeed);
            if (frontGap < safeDistance * 0.4f) target *= 0.8f;   // bahut paas ho to thoda aur dheere
        }
        else
        {
            state = TrafficState.Driving;
        }

        // Frame-rate independent smoothing
        currentSpeed = Mathf.Lerp(currentSpeed, target, 1f - Mathf.Exp(-Response * dt));
    }

    public void Initialize(float speed, int lane)
    {
        baseSpeed = speed;
        currentSpeed = speed;
        laneIndex = lane;
        state = TrafficState.Driving;
        IsActive = true;
        gameObject.SetActive(true);
    }

    public void Deactivate()
    {
        IsActive = false;
        gameObject.SetActive(false);
    }
}