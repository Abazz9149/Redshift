using System.Collections.Generic;
using System;
using UnityEngine;
using Redshift;
using Random = UnityEngine.Random;

// Execution order: Player (-100) -> Road (-50) -> AI (0) -> Camera (LateUpdate)
// Isse sab ek hi frame ki speed use karte hain, 1-frame lag wala micro-jitter nahi aata.
//
// REDSHIFT KIT ADDITIONS:
//  - Begin(mode): Two-Way me left lanes (oncomingLanes) ki traffic saamne se aati hai
//  - Difficulty ramp: distance badhne par spawn jaldi hota hai
//  - ClearAround(): CONTINUE ke baad player ke paas ki traffic hata deta hai
//  - StopSpawning(): race khatam
//  - OnTrafficSpawned event + IsOncoming(): near-miss detection (TrafficInteraction) ke liye
//  - Recycle hone wale tile pe RoadSegmentVariants ho to naya roadside set dikhata hai
[DefaultExecutionOrder(-50)]
public class RoadManagerRecycle : MonoBehaviour
{
    [Header("References")]
    public PlayerController player;

    [Header("Performance")]
    [Tooltip("60 rakho. 0 = Unity ka default (mobile pe 30 aa sakta hai, high speed pe strobing dikhti hai)")]
    public int targetFrameRate = 60;
    [Tooltip("Lag spike pe tiles/cars teleport na karein, isliye dt yahan tak clamp hota hai")]
    public float maxDeltaTime = 0.05f;

    [Header("Road Tiles (scene mein pehle se rakhe hue)")]
    public Transform[] roadTiles;
    public float segmentLength = 30f;
    public float recycleZ = -30f;

    // [0] = sabse peeche, [last] = sabse aage
    private readonly List<Transform> tileOrder = new List<Transform>();

    [Header("Smooth Movement")]
    public float speedMultiplier = 1f;
    [Tooltip("Zyada value = zyada smooth (0.2 - 0.5)")]
    public float speedSmoothTime = 0.3f;
    public float CurrentRoadSpeed => smoothedSpeed;

    private float smoothedSpeed;
    private float speedVelocity;

    [Header("Traffic Settings")]
    public GameObject[] trafficPrefabs;
    public int trafficPoolSize = 20;
    public float spawnDistance = 80f;
    public float despawnDistance = -20f;

    [Header("Spawn Dynamics")]
    public float minSpawnInterval = 1f;
    public float maxSpawnInterval = 3f;
    public float minDistanceBetweenCars = 15f;
    [Tooltip("Same lane maanne ke liye max X farak")]
    public float sameLaneTolerance = 1f;

    private float spawnTimer = 0f;

    [Header("Traffic Mode")]
    public bool twoWayMode = false;
    [Tooltip("Two-Way me kitni left lanes (index 0 se) saamne se aayengi")]
    public int oncomingLanes = 2;

    [Header("Difficulty Ramp")]
    [Tooltip("Itne meter ke baad traffic sabse ghani")]
    public float rampDistance = 6000f;
    [Tooltip("Ramp ke end pe spawn interval ka multiplier (0.45 = 2x se zyada traffic)")]
    [Range(0.2f, 1f)] public float hardestIntervalScale = 0.45f;

    [Header("Lane Settings")]
    public float[] lanePositions = { -7.5f, -2.5f, 2.5f, 7.5f };
    public float[] laneSpeeds = { 40f, 35f, 30f, 20f };

    // AICarController isi list se obstacles leta hai (FindGameObjectsWithTag ki zarurat nahi)
    private readonly List<TrafficAI> active = new List<TrafficAI>(32);
    private readonly Stack<TrafficAI> free = new Stack<TrafficAI>(32);
    public IReadOnlyList<TrafficAI> ActiveTraffic => active;

    private readonly HashSet<TrafficAI> oncoming = new HashSet<TrafficAI>();
    public bool IsOncoming(TrafficAI ai) => oncoming.Contains(ai);
    public event Action<TrafficAI> OnTrafficSpawned;
    public bool SpawningEnabled { get; private set; } = true;

    /// <summary>RaceSession race shuru hone se pehle call karta hai.</summary>
    public void Begin(GameMode mode)
    {
        twoWayMode = mode == GameMode.TwoWay;
        SpawningEnabled = true;
    }

    public void StopSpawning() => SpawningEnabled = false;

    bool IsOncomingLane(int lane) => twoWayMode && lane < oncomingLanes;

    float dt;

    void Awake()
    {
        if (targetFrameRate > 0)
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = targetFrameRate;
        }
    }

    void Start()
    {
        InitializeRoadTiles();
        InitializeTrafficPool();
        smoothedSpeed = player.currentSpeed * speedMultiplier;
    }

    void Update()
    {
        dt = Mathf.Min(Time.deltaTime, maxDeltaTime);

        UpdateSmoothedSpeed();
        MoveRoads();
        RecycleRoads();
        UpdateTraffic();       // front check + move + despawn, ek hi loop me
        HandleTrafficSpawning();
    }

    // ---------------- SPEED SMOOTHING ----------------

    void UpdateSmoothedSpeed()
    {
        float target = player.currentSpeed * speedMultiplier;
        smoothedSpeed = Mathf.SmoothDamp(smoothedSpeed, target, ref speedVelocity, speedSmoothTime, Mathf.Infinity, dt);
    }

    // ---------------- ROAD ----------------

    void InitializeRoadTiles()
    {
        if (roadTiles == null || roadTiles.Length == 0)
        {
            Debug.LogWarning("RoadManagerRecycle: roadTiles assign nahi kiye gaye!");
            return;
        }

        tileOrder.AddRange(roadTiles);
        tileOrder.Sort((a, b) => a.position.z.CompareTo(b.position.z));
    }

    void MoveRoads()
    {
        int n = tileOrder.Count;
        if (n == 0) return;

        float delta = smoothedSpeed * dt;

        for (int i = 0; i < n; i++)
        {
            Transform t = tileOrder[i];
            Vector3 p = t.position;
            p.z -= delta;
            t.position = p;
        }
    }

    void RecycleRoads()
    {
        if (tileOrder.Count == 0) return;

        while (tileOrder[0].position.z <= recycleZ)
        {
            Transform backMost = tileOrder[0];
            Transform frontMost = tileOrder[tileOrder.Count - 1];

            Vector3 pos = backMost.position;
            pos.z = frontMost.position.z + segmentLength;
            backMost.position = pos;

            tileOrder.RemoveAt(0);
            tileOrder.Add(backMost);

            var variants = backMost.GetComponent<RoadSegmentVariants>();
            if (variants != null) variants.PickRandom();
        }
    }

    // ---------------- TRAFFIC ----------------

    void InitializeTrafficPool()
    {
        if (trafficPrefabs == null || trafficPrefabs.Length == 0)
        {
            Debug.LogWarning("RoadManagerRecycle: trafficPrefabs assign nahi hain!");
            return;
        }

        for (int i = 0; i < trafficPoolSize; i++)
        {
            GameObject prefab = trafficPrefabs[Random.Range(0, trafficPrefabs.Length)];
            GameObject car = Instantiate(prefab, transform);
            car.SetActive(false);

            TrafficAI ai = car.GetComponent<TrafficAI>();
            if (ai == null)
            {
                Debug.LogError("Traffic prefab pe TrafficAI nahi hai: " + prefab.name, prefab);
                Destroy(car);
                continue;
            }

            free.Push(ai);
        }
    }

    void UpdateTraffic()
    {
        // Peeche se aage loop: swap-remove safe rehta hai
        for (int i = active.Count - 1; i >= 0; i--)
        {
            TrafficAI ai = active[i];
            Transform tr = ai.transform;
            Vector3 p = tr.position;
            bool isOncoming = oncoming.Contains(ai);
            float dir = isOncoming ? -1f : 1f;   // car kis taraf chal rahi hai

            // Same lane me sabse paas ki aage wali car (raycast ki jagah simple math)
            // Oncoming car ke liye "aage" = kam Z
            float gap = float.MaxValue;
            float frontSpeed = 0f;
            for (int j = 0; j < active.Count; j++)
            {
                if (j == i) continue;
                if (oncoming.Contains(active[j]) != isOncoming) continue;
                Vector3 op = active[j].transform.position;
                if (Mathf.Abs(op.x - p.x) > sameLaneTolerance) continue;

                float dz = (op.z - p.z) * dir;
                if (dz > 0f && dz < gap)
                {
                    gap = dz;
                    frontSpeed = active[j].currentSpeed;
                }
            }

            ai.Tick(dt, gap, frontSpeed);

            // Road ke saath same multiplier, taaki cars road pe slide na karein
            float relativeSpeed = smoothedSpeed - dir * ai.currentSpeed * speedMultiplier;
            p.z -= relativeSpeed * dt;
            tr.position = p;

            // Bahut aage nikal gayi (player ruk gaya ho / crash) to bhi hatao
            if (p.z < despawnDistance || p.z > player.transform.position.z + spawnDistance + 60f)
                RemoveAt(i);
        }
    }

    void RemoveAt(int i)
    {
        TrafficAI ai = active[i];
        ai.Deactivate();
        oncoming.Remove(ai);
        free.Push(ai);

        int last = active.Count - 1;
        active[i] = active[last];
        active.RemoveAt(last);
    }

    /// <summary>CONTINUE: player ke aage-peeche ki traffic hata do.</summary>
    public void ClearAround(float z, float radius)
    {
        for (int i = active.Count - 1; i >= 0; i--)
        {
            if (Mathf.Abs(active[i].transform.position.z - z) < radius)
                RemoveAt(i);
        }
    }

    void HandleTrafficSpawning()
    {
        if (!SpawningEnabled) return;

        spawnTimer -= dt * (1f + (smoothedSpeed / 30f));

        if (spawnTimer <= 0f)
        {
            SpawnTraffic();
            float ramp = rampDistance > 0f ? Mathf.Clamp01(player.distanceTraveled / rampDistance) : 0f;
            spawnTimer = Random.Range(minSpawnInterval, maxSpawnInterval) * Mathf.Lerp(1f, hardestIntervalScale, ramp);
        }
    }

    void SpawnTraffic()
    {
        if (free.Count == 0 || lanePositions.Length == 0) return;

        float spawnZ = player.transform.position.z + spawnDistance;
        int start = Random.Range(0, lanePositions.Length);

        // Ek lane crowded ho to baaki lanes try karo (pehle spawn skip ho jaata tha)
        for (int k = 0; k < lanePositions.Length; k++)
        {
            int lane = (start + k) % lanePositions.Length;
            float laneX = lanePositions[lane];

            if (!IsSpawnSafe(laneX, spawnZ)) continue;

            TrafficAI ai = free.Pop();
            ai.transform.position = new Vector3(laneX, 0f, spawnZ);
            ai.Initialize(GetLaneSpeed(lane), lane);

            bool isOncoming = IsOncomingLane(lane);
            if (isOncoming) oncoming.Add(ai);
            ai.transform.rotation = Quaternion.Euler(0f, isOncoming ? 180f : 0f, 0f);

            active.Add(ai);
            OnTrafficSpawned?.Invoke(ai);
            return;
        }
    }

    bool IsSpawnSafe(float laneX, float spawnZ)
    {
        for (int i = 0; i < active.Count; i++)
        {
            Vector3 p = active[i].transform.position;
            if (Mathf.Abs(p.x - laneX) < sameLaneTolerance &&
                Mathf.Abs(p.z - spawnZ) < minDistanceBetweenCars)
                return false;
        }
        return true;
    }

    float GetLaneSpeed(int lane)
    {
        if (laneSpeeds != null && lane >= 0 && lane < laneSpeeds.Length)
            return laneSpeeds[lane];
        return 20f;
    }
}