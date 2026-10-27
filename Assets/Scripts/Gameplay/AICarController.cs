using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// PRO AI rival car.
/// - Traffic ko "Traffic" tag se dhundti hai, size real renderer bounds se leti hai
/// - Lane ko speed-potential se score karti hai aur early overtake karti hai (follow nahi karti)
/// - Late aur smooth braking (physics formula), emergency me dodge
/// - Contact hote hi real crash (bounds overlap)
///
/// RoadManagerRecycle me ye line honi chahiye:
///     public float CurrentRoadSpeed => smoothedSpeed;
///
/// REDSHIFT KIT: RivalTracker in cars ko manage karta hai (race position, "DEFEAT AI CARS" mission,
/// player ki car ke hisaab se maxSpeed). Two-Way mode me rivals band rehte hain.
/// </summary>
[DefaultExecutionOrder(0)]   // Road (-50) ke baad chalti hai
public class AICarController : MonoBehaviour
{
    public enum AIDifficulty { Rookie, Normal, Pro, Insane, Custom }

    [Header("References")]
    public PlayerController player;
    public RoadManagerRecycle road;
    public Transform carBody;               // AI car ka visual mesh (yaw/roll/pitch isi pe)
    public GameObject visualRoot;           // (ab use nahi hota, hide renderers se hota hai, khali chhod sakte ho)

    [Header("Start")]
    public int startLane = 1;
    public float startGap = 0f;             // + = player se aage, - = peeche

    [Header("Difficulty")]
    public AIDifficulty difficulty = AIDifficulty.Pro;   // Custom chuno to neeche ki values use hongi
    public float topSpeedFactor = 1.02f;    // maxSpeed ka kitna hissa (Pro player se thoda tez)
    public float reactionTime = 0.1f;       // lane decision kitni jaldi (kam = pro)
    [Range(0f, 0.2f)] public float mistakeChance = 0.008f;   // evasive moment pe galti ka chance
    public float mistakeDuration = 0.8f;    // galti me itni der obstacle ignore
    public float rubberStrength = 0.06f;    // 0 = band. Peeche ho to thodi tez, aage ho to thodi dheemi
    public float rubberRange = 60f;
    public float switchThreshold = 3f;      // lane badalne ke liye kitna fayda chahiye (kam = aggressive overtake)
    public float laneChangeCost = 3f;       // door ki lane ka extra cost

    [Header("Speed (player jaisa)")]
    public float minSpeed = 10f;
    public float maxSpeed = 60f;
    public float acceleration = 15f;
    public float brakeForce = 40f;
    public float naturalDrag = 8f;
    [Range(0.1f, 1f)] public float brakeThreshold = 0.35f;   // brakeForce ka kitna hissa chahiye tab brake lage (kam = jaldi brake)

    [Header("Traffic Detection")]
    public string trafficTag = "Traffic";
    public bool avoidPlayer = true;
    public float planDistance = 50f;        // itni door tak dekhti hai
    public float safeGap = 5f;              // follow karte time gap
    public float emergencyGap = 12f;        // isse kam gap = emergency dodge
    public float laneMargin = 0.4f;         // side me extra margin
    public float sideBuffer = 2.5f;         // lane change ke time aage/peeche extra jagah
    public float refreshInterval = 0.5f;

    [Header("Steering")]
    public float steerSpeed = 12f;
    public float steerAcceleration = 30f;
    public float steerRelease = 45f;

    [Header("Crash")]
    public bool crashEnabled = true;
    [Range(0.5f, 1f)] public float crashForgiveness = 0.85f;  // bounds overlap ka kitna hissa = takkar
    public float crashDuration = 1.2f;
    public float crashDecel = 60f;
    public float crashSpeedFactor = 0.2f;
    public float crashShove = 5f;           // takkar pe side me dhakka
    public float crashShake = 25f;
    public float invulnerableTime = 1.5f;
    public AudioSource crashSound;
    public UnityEngine.Events.UnityEvent onCrash;

    [Header("Race Window (AI hamesha traffic zone ke andar dikhegi)")]
    public float maxAhead = 40f;
    public float maxBehind = 15f;

    [Header("Car Feel")]
    public float maxYawAngle = 28f;
    public float maxRollAngle = 6f;
    public float maxPitchAngle = 3f;
    public float yawSmoothness = 8f;
    public float rollSmoothness = 6f;
    public float pitchSmoothness = 5f;

    [Header("Debug (read only)")]
    public float currentSpeed;
    public float raceGap;                   // + = AI aage, - = tum aage (meters)
    public bool isAheadOfPlayer;
    public float pathGap;                   // AI ke seedhe aage kitni jagah khali

    class Obstacle
    {
        public Transform tr;
        public TrafficAI ai;                // null = player
        public bool isPlayer;
        public float halfX, halfZ;          // real size (renderer bounds)
        public float offX, offZ;            // pivot se center ka farak
    }

    readonly List<Obstacle> obstacles = new List<Obstacle>();
    readonly Dictionary<Transform, Obstacle> cache = new Dictionary<Transform, Obstacle>();
    static readonly float[] sampleTimes = { 0f, 0.3f, 0.6f, 0.9f };

    float dt;
    float laneLo, laneHi;                   // lane limits, ek baar calculate (pehle har frame loop chalta tha)
    float refreshTimer, decisionTimer;
    int targetLane;
    float lateralVelocity;
    float currentYaw, currentRoll, currentPitch;
    bool isAccelerating, isBraking;

    float pathObsSpeed;
    float crashTimer, invulnTimer, ignoreTimer;

    // AI ki apni size
    float myHalfX = 1f, myHalfZ = 2.2f, myOffX, myOffZ;

    Renderer[] bodyRenderers;
    bool renderersShown = true;

    void Start()
    {
        // Prefab me scene references save nahi hote, isliye null ho to khud dhundh lo
        if (player == null) player = FindPlayer();
        if (road == null) road = FindRoad();

        if (player == null || road == null)
        {
            Debug.LogError("AICarController: player ya road nahi mila. Scene me PlayerController aur RoadManagerRecycle hone chahiye.", this);
            return;   // script ko khud disable nahi karte
        }

        // Ye cheezein AI car pe hui to traffic recycle ya tag logic isse band kar sakta hai
        if (GetComponent<TrafficAI>() != null || CompareTag(trafficTag))
            Debug.LogWarning("AICarController: AI car pe TrafficAI component ya '" + trafficTag + "' tag mat rakho, traffic recycle isse disable kar dega.", this);

        ApplyPreset();

        float minC = float.MaxValue, maxC = float.MinValue;
        for (int i = 0; i < road.lanePositions.Length; i++)
        {
            minC = Mathf.Min(minC, road.lanePositions[i]);
            maxC = Mathf.Max(maxC, road.lanePositions[i]);
        }
        laneLo = minC - 0.5f;
        laneHi = maxC + 0.5f;

        // Apni size measure karo (bounds se)
        Transform src = carBody != null ? carBody : transform;

        // Sirf mesh renderers cache karo aur pehle se OFF ho to bhi ON kar do (bounds sahi aayein)
        Renderer[] allRenderers = src.GetComponentsInChildren<Renderer>(true);
        List<Renderer> meshRenderers = new List<Renderer>();
        for (int i = 0; i < allRenderers.Length; i++)
        {
            if (allRenderers[i] is MeshRenderer || allRenderers[i] is SkinnedMeshRenderer)
            {
                allRenderers[i].enabled = true;
                meshRenderers.Add(allRenderers[i]);
            }
        }
        bodyRenderers = meshRenderers.ToArray();

        MeasureSize(src, transform, out myHalfX, out myHalfZ, out myOffX, out myOffZ);

        currentSpeed = minSpeed;
        raceGap = startGap;

        startLane = Mathf.Clamp(startLane, 0, road.lanePositions.Length - 1);
        targetLane = startLane;

        Vector3 pos = transform.position;
        pos.x = road.lanePositions[startLane] - myOffX;
        pos.z = player.transform.position.z + Mathf.Clamp(raceGap, -maxBehind, maxAhead);
        transform.position = pos;

        pathObsSpeed = float.MaxValue;
        pathGap = planDistance;
        RefreshObstacles();
    }

    static PlayerController FindPlayer()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<PlayerController>();
#else
        return FindObjectOfType<PlayerController>();
#endif
    }

    static RoadManagerRecycle FindRoad()
    {
#if UNITY_2023_1_OR_NEWER
        return FindFirstObjectByType<RoadManagerRecycle>();
#else
        return FindObjectOfType<RoadManagerRecycle>();
#endif
    }

    // Debug: koi bhi AI ko disable kare to console me stack trace aayega (kisne kiya)
    bool quitting;
    void OnApplicationQuit() { quitting = true; }

    /// <summary>RivalTracker jaan-boojh kar disable kare to warning mat do.</summary>
    [HideInInspector] public bool disabledByRace;

    void OnDisable()
    {
#if UNITY_EDITOR
        if (quitting || disabledByRace || !gameObject.scene.isLoaded) return;
        Debug.LogWarning("AICarController disable hua! Kisne kiya wo neeche stack trace me dekho:\n" + System.Environment.StackTrace, this);
#endif
    }

    void ApplyPreset()
    {
        switch (difficulty)
        {
            case AIDifficulty.Rookie:
                topSpeedFactor = 0.80f; reactionTime = 0.35f; mistakeChance = 0.10f;
                rubberStrength = 0.20f; switchThreshold = 8f; break;
            case AIDifficulty.Normal:
                topSpeedFactor = 0.92f; reactionTime = 0.22f; mistakeChance = 0.04f;
                rubberStrength = 0.15f; switchThreshold = 5f; break;
            case AIDifficulty.Pro:
                topSpeedFactor = 1.02f; reactionTime = 0.10f; mistakeChance = 0.008f;
                rubberStrength = 0.06f; switchThreshold = 3f; break;
            case AIDifficulty.Insane:
                topSpeedFactor = 1.10f; reactionTime = 0.06f; mistakeChance = 0f;
                rubberStrength = 0f; switchThreshold = 1.5f; break;
        }
    }

    void Update()
    {
        if (player == null || road == null) return;

        dt = Mathf.Min(Time.deltaTime, 0.05f);

        refreshTimer -= dt;
        if (refreshTimer <= 0f)
        {
            refreshTimer = refreshInterval;
            RefreshObstacles();
        }

        crashTimer -= dt;
        invulnTimer -= dt;
        ignoreTimer -= dt;

        if (crashEnabled) CheckCrash();

        pathGap = PathGap(MyX(), out pathObsSpeed);

        decisionTimer -= dt;
        if (decisionTimer <= 0f && crashTimer <= 0f)
        {
            decisionTimer = reactionTime;
            DecideLane();
        }

        HandleSteering();
        HandleSpeed();
        UpdateRacePosition();
        UpdateVisuals();
    }

    // =========================================================
    // POSITION HELPERS (pivot nahi, real center use hota hai)
    // =========================================================

    float MyX() { return transform.position.x + myOffX; }
    float MyZ() { return transform.position.z + myOffZ; }
    float ObsX(Obstacle o) { return o.tr.position.x + o.offX; }
    float ObsZ(Obstacle o) { return o.tr.position.z + o.offZ; }

    static void MeasureSize(Transform src, Transform pivot, out float hx, out float hz, out float offX, out float offZ)
    {
        Renderer[] rs = src.GetComponentsInChildren<Renderer>();
        Bounds b = new Bounds();
        bool has = false;

        for (int i = 0; i < rs.Length; i++)
        {
            if (!(rs[i] is MeshRenderer) && !(rs[i] is SkinnedMeshRenderer)) continue;
            if (!has) { b = rs[i].bounds; has = true; }
            else b.Encapsulate(rs[i].bounds);
        }

        if (!has || b.size.sqrMagnitude < 0.01f)
        {
            hx = 1f; hz = 2.2f; offX = 0f; offZ = 0f;
            return;
        }

        hx = Mathf.Max(0.6f, b.extents.x);
        hz = Mathf.Max(1.5f, b.extents.z);
        offX = b.center.x - pivot.position.x;
        offZ = b.center.z - pivot.position.z;
    }

    // =========================================================
    // OBSTACLES (Traffic tag + player)
    // =========================================================

    void RefreshObstacles()
    {
        obstacles.Clear();

        // FindGameObjectsWithTag har 0.5s pe array alloc karta tha = GC hitch. Ab road ki list use hoti hai.
        IReadOnlyList<TrafficAI> traffic = road.ActiveTraffic;
        for (int i = 0; i < traffic.Count; i++)
        {
            TrafficAI t = traffic[i];
            if (t == null || t.transform == transform) continue;
            obstacles.Add(GetObstacle(t.transform, t, false));
        }

        if (avoidPlayer)
            obstacles.Add(GetObstacle(player.transform, null, true));
    }

    Obstacle GetObstacle(Transform root, TrafficAI t, bool isPlayer)
    {
        if (cache.TryGetValue(root, out Obstacle o)) return o;

        o = new Obstacle { tr = root, ai = t, isPlayer = isPlayer };
        MeasureSize(root, root, out o.halfX, out o.halfZ, out o.offX, out o.offZ);
        cache[root] = o;
        return o;
    }

    bool Valid(Obstacle o)
    {
        if (o.tr == null) return false;
        return o.ai != null ? o.ai.IsActive : true;   // activeInHierarchy native call se sasta
    }

    float ObstacleSpeed(Obstacle o)
    {
        if (o.isPlayer) return player.currentSpeed;
        return o.ai != null ? o.ai.currentSpeed : 0f;
    }

    // Kisi x pe (seedhe aage) sabse paas ki car ka bumper-to-bumper gap
    float PathGap(float x, out float obsSpeed)
    {
        float myZ = MyZ();
        float best = planDistance;
        obsSpeed = float.MaxValue;

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle o = obstacles[i];
            if (!Valid(o)) continue;

            if (Mathf.Abs(ObsX(o) - x) > myHalfX + o.halfX + laneMargin) continue;

            float dz = ObsZ(o) - myZ;
            if (dz < -(o.halfZ + myHalfZ)) continue;     // ye car poori tarah peeche reh gayi

            float gap = Mathf.Max(0f, dz - o.halfZ - myHalfZ);
            if (gap < best)
            {
                best = gap;
                obsSpeed = ObstacleSpeed(o);
            }
        }

        return best;
    }

    // =========================================================
    // LANE PLANNING (pro overtaking)
    // =========================================================

    float TargetSpeed()
    {
        float target = maxSpeed * topSpeedFactor;

        if (rubberStrength > 0f)
        {
            float t = Mathf.Clamp(raceGap / rubberRange, -1f, 1f);
            target *= 1f - t * rubberStrength;
        }

        return Mathf.Max(target, minSpeed);
    }

    // Lane ka score = wahan kitni speed mil sakti hai + kitna khula rasta hai
    float LaneScore(int lane, out float gap, out float obsSpeed)
    {
        float x = road.lanePositions[lane];
        gap = PathGap(x, out obsSpeed);

        float target = TargetSpeed();
        float potential = target;

        if (gap < planDistance)
        {
            float k = Mathf.InverseLerp(safeGap, planDistance, gap);
            potential = Mathf.Lerp(Mathf.Min(obsSpeed, target), target, k);
        }

        return potential + Mathf.Min(gap, planDistance) * 0.15f;
    }

    void DecideLane()
    {
        // Lane change beech me ho to pehle poora hone do
        if (Mathf.Abs(MyX() - road.lanePositions[targetLane]) > 1.2f) return;

        int cur = targetLane;
        float curScore = LaneScore(cur, out float curGap, out _);

        if (curGap >= planDistance) return;   // rasta khula hai

        // Galti (sirf jab evasive action chahiye)
        if (Random.value < mistakeChance)
        {
            ignoreTimer = mistakeDuration;
            return;
        }

        bool emergency = curGap < emergencyGap;

        int best = cur;
        float bestScore = curScore + (emergency ? 0f : switchThreshold);

        for (int i = 0; i < road.lanePositions.Length; i++)
        {
            if (i == cur) continue;

            float score = LaneScore(i, out _, out _) - Mathf.Abs(i - cur) * laneChangeCost;
            if (score <= bestScore) continue;
            if (!LaneChangeSafe(cur, i, emergency)) continue;

            best = i;
            bestScore = score;
        }

        targetLane = best;
    }

    bool LaneChangeSafe(int from, int to, bool emergency)
    {
        int step = to > from ? 1 : -1;
        for (int l = from + step; ; l += step)
        {
            if (!LaneClear(l, emergency)) return false;
            if (l == to) break;
        }
        return true;
    }

    // Lane me side me koi car abhi ya agle ~1 sec me aane wali to nahi (predicted position)
    bool LaneClear(int lane, bool emergency)
    {
        float laneX = road.lanePositions[lane];
        float myZ = MyZ();
        float buf = emergency ? 0.5f : sideBuffer;

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle o = obstacles[i];
            if (!Valid(o)) continue;

            if (Mathf.Abs(ObsX(o) - laneX) > myHalfX + o.halfX + laneMargin) continue;

            float dz0 = ObsZ(o) - myZ;
            float relV = (ObstacleSpeed(o) - currentSpeed) * road.speedMultiplier;
            float reach = o.halfZ + myHalfZ + buf;

            for (int s = 0; s < sampleTimes.Length; s++)
            {
                if (Mathf.Abs(dz0 + relV * sampleTimes[s]) < reach)
                    return false;
            }
        }

        return true;
    }

    // =========================================================
    // STEERING (overshoot ke bina, sahi jagah ruk ke)
    // =========================================================

    void HandleSteering()
    {
        float dx = crashTimer > 0f ? 0f : road.lanePositions[targetLane] - MyX();

        float desired = 0f;
        if (Mathf.Abs(dx) > 0.03f)
        {
            // Braking distance formula: utni hi speed jitne me target pe ruk sake
            float maxApproach = Mathf.Sqrt(2f * steerRelease * 0.85f * Mathf.Abs(dx));
            desired = Mathf.Sign(dx) * Mathf.Min(steerSpeed, maxApproach);
        }

        bool speedingUp = Mathf.Abs(desired) > Mathf.Abs(lateralVelocity) && desired * lateralVelocity >= 0f;
        float rate = speedingUp ? steerAcceleration : steerRelease;

        lateralVelocity = Mathf.MoveTowards(lateralVelocity, desired, rate * dt);

        Vector3 pos = transform.position;
        pos.x += lateralVelocity * dt;
        // Limit lanes ke hisaab se (player.minX/maxX se nahi), warna AI edge pe atak kar hamesha ek taraf ghumti rehti hai
        float lo = laneLo - myOffX;
        float hi = laneHi - myOffX;

        if (pos.x < lo)
        {
            pos.x = lo;
            if (lateralVelocity < 0f) lateralVelocity = 0f;
        }
        else if (pos.x > hi)
        {
            pos.x = hi;
            if (lateralVelocity > 0f) lateralVelocity = 0f;
        }

        transform.position = pos;
    }

    // =========================================================
    // SPEED (late braking, follow sirf tab jab lane na mile)
    // =========================================================

    void HandleSpeed()
    {
        // Crash ke baad car ruk jayegi
        if (crashTimer > 0f)
        {
            isAccelerating = false;
            isBraking = true;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, crashDecel * dt);
            return;
        }

        float target = TargetSpeed();
        float brakeCmd = 0f;
        bool hasObs = pathGap < planDistance && ignoreTimer <= 0f;

        if (hasObs)
        {
            float closing = currentSpeed - pathObsSpeed;

            if (closing > 0f)
            {
                // Kitni deceleration chahiye taaki safeGap pe aage wali car ki speed match ho
                float d = Mathf.Max(0.5f, pathGap - safeGap);
                float aNeeded = closing * closing * road.speedMultiplier / (2f * d);

                if (aNeeded > brakeForce * brakeThreshold)
                    brakeCmd = Mathf.Min(aNeeded * 1.3f, brakeForce);
            }

            // Bahut paas aur lane nahi mili -> follow
            if (pathGap < safeGap * 1.4f && pathObsSpeed < target)
                target = pathObsSpeed;
        }

        isAccelerating = false;
        isBraking = false;

        if (brakeCmd > 0f)
        {
            isBraking = true;
            float floor = hasObs ? Mathf.Max(minSpeed * 0.5f, pathObsSpeed) : minSpeed;
            if (currentSpeed > floor)
                currentSpeed = Mathf.MoveTowards(currentSpeed, floor, brakeCmd * dt);
        }
        else if (currentSpeed < target)
        {
            isAccelerating = true;
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, acceleration * dt);
        }
        else
        {
            // Target se upar hai (follow ya rubber band) -> thoda zyada decel
            float over = currentSpeed - target;
            float rate = naturalDrag + (over > 3f ? brakeForce * 0.3f : 0f);
            currentSpeed = Mathf.MoveTowards(currentSpeed, target, rate * dt);
        }

        float cap = maxSpeed * Mathf.Max(topSpeedFactor, 1f) * (1f + rubberStrength);
        currentSpeed = Mathf.Clamp(currentSpeed, 0f, cap);
    }

    // =========================================================
    // CRASH (real bounds overlap)
    // =========================================================

    void CheckCrash()
    {
        if (invulnTimer > 0f || crashTimer > 0f) return;

        float myX = MyX();
        float myZ = MyZ();

        for (int i = 0; i < obstacles.Count; i++)
        {
            Obstacle o = obstacles[i];
            if (o.isPlayer || !Valid(o)) continue;

            float dx = ObsX(o) - myX;
            float dz = ObsZ(o) - myZ;

            if (Mathf.Abs(dx) < (myHalfX + o.halfX) * crashForgiveness &&
                Mathf.Abs(dz) < (myHalfZ + o.halfZ) * crashForgiveness)
            {
                Crash(dx);
                return;
            }
        }
    }

    void Crash(float obstacleDx)
    {
        crashTimer = crashDuration;
        invulnTimer = crashDuration + invulnerableTime;
        ignoreTimer = 0f;

        // Car ko obstacle se door dhakka
        float dir = Mathf.Abs(obstacleDx) > 0.05f ? -Mathf.Sign(obstacleDx) : (Random.value < 0.5f ? -1f : 1f);
        lateralVelocity = dir * crashShove;

        currentSpeed *= crashSpeedFactor;

        // Crash ke baad nearest lane pe wapas aane ka plan
        targetLane = NearestLane(MyX());

        if (crashSound != null) crashSound.Play();
        if (onCrash != null) onCrash.Invoke();
    }

    int NearestLane(float x)
    {
        int best = 0;
        float bestDist = float.MaxValue;
        for (int i = 0; i < road.lanePositions.Length; i++)
        {
            float d = Mathf.Abs(road.lanePositions[i] - x);
            if (d < bestDist) { bestDist = d; best = i; }
        }
        return best;
    }

    // =========================================================
    // RACE POSITION (road ke relative, bilkul traffic ki tarah)
    // =========================================================

    void UpdateRacePosition()
    {
        float relative = currentSpeed * road.speedMultiplier - road.CurrentRoadSpeed;
        raceGap = Mathf.Clamp(raceGap + relative * dt, -1000f, 1000f);
        isAheadOfPlayer = raceGap > 0f;

        Vector3 pos = transform.position;
        pos.z = player.transform.position.z + Mathf.Clamp(raceGap, -maxBehind, maxAhead) - myOffZ;
        transform.position = pos;
    }

    // =========================================================
    // VISUALS
    // =========================================================

    float Smooth(float current, float target, float speed)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * dt));
    }

    void UpdateVisuals()
    {
        if (carBody == null) return;

        float lateralNorm = steerSpeed > 0.01f
            ? Mathf.Clamp(lateralVelocity / steerSpeed, -1f, 1f)
            : 0f;

        float speed01 = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);

        currentYaw = Smooth(currentYaw, lateralNorm * maxYawAngle * Mathf.Lerp(1f, 0.65f, speed01), yawSmoothness);
        currentRoll = Smooth(currentRoll, lateralNorm * maxRollAngle * (0.6f + 0.4f * speed01), rollSmoothness);

        float targetPitch = isAccelerating ? -maxPitchAngle : (isBraking ? maxPitchAngle : 0f);
        currentPitch = Smooth(currentPitch, targetPitch, pitchSmoothness);

        float wobble = 0f;
        if (crashTimer > 0f)
            wobble = Mathf.Sin(Time.time * 25f) * crashShake * Mathf.Clamp01(crashTimer / crashDuration);

        carBody.localRotation = Quaternion.Euler(currentPitch, currentYaw + wobble, currentRoll);

        // Renderer sirf crash ke baad blink karega, aur kisi haalat me hide nahi hoga
        bool show = !(invulnTimer > 0f && Mathf.Repeat(Time.time, 0.2f) < 0.1f);
        if (show != renderersShown && bodyRenderers != null)
        {
            renderersShown = show;
            for (int i = 0; i < bodyRenderers.Length; i++)
                if (bodyRenderers[i] != null) bodyRenderers[i].enabled = show;
        }
    }

    // UI ke liye: + = AI aage, - = tum aage
    public float GapToPlayer() { return raceGap; }
}