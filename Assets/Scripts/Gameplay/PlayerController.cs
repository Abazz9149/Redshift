using System;
using UnityEngine;
using Redshift;

// Sabse pehle chalta hai (-100), taaki Road/AI/Camera isi frame ki speed padhein
//
// REDSHIFT KIT ADDITIONS (aapka original logic same hai):
//  - Nitro: ActivateNitro(), NitroActive, Nitro01, AddNitro()  (near-miss se refill hota hai)
//  - Garage stats: ApplyStats() -> maxSpeed / acceleration / steerSpeed / nitro tank
//  - Crash / Continue: Stop(), Resume()  (RaceSession call karta hai)
//  - Control mode SETTINGS se save/load hota hai (0 Buttons, 1 Wheel, 2 Gyro)
//  - Editor me keyboard: A/D ya arrows = steer, W = gas, S = brake, Space = nitro
[DefaultExecutionOrder(-100)]
[RequireComponent(typeof(Rigidbody))]
public class PlayerController : MonoBehaviour
{
    [Header("Lane Movement")]
    public float steerSpeed = 12f;          // max side speed (units/sec)
    public float minX = -9.5f;
    public float maxX = 9.5f;
    public float steerAcceleration = 30f;   // steer dabane par side velocity kitni jaldi badhe
    public float steerRelease = 45f;        // steer chhodne par kitni jaldi ruke

    [Header("Realistic Car Feel")]
    public Transform carBody;               // Car ka visual mesh (child). Yaw/Roll/Pitch SIRF isi pe lagega
    public bool autoCreateVisualPivot = true; // carBody khali ho to script khud pivot bana degi
    public Transform[] frontWheels;         // Optional: sirf front wheels ke pivots
    public float maxYawAngle = 28f;         // turn me car ki nose kitni ghumegi
    public float maxRollAngle = 6f;         // turn me body kitni jhukegi
    public float maxPitchAngle = 3f;        // accelerate/brake me nose up/down
    public float wheelTurnAngle = 30f;      // front wheels ka steer angle
    public float yawSmoothness = 8f;
    public float rollSmoothness = 6f;
    public float pitchSmoothness = 5f;
    public bool invertRoll = false;         // agar body ulti taraf jhuk rahi ho to tick karo
    [Range(0f, 1f)] public float highSpeedSteerReduction = 0.35f; // tez speed pe yaw thoda kam

    [Header("Audio")]
    public AudioSource AccelerateSound;
    public AudioSource EngineSound;

    [Header("Speed Settings")]
    public float minSpeed = 10f;
    public float maxSpeed = 60f;
    public float acceleration = 15f;
    public float brakeForce = 40f;
    public float naturalDrag = 8f;

    [Header("Mobile Buttons")]
    public GameObject turnLeftButton;
    public GameObject turnRightButton;
    public GameObject accelerateButton;
    public GameObject brakeButton;

    [Header("Steering Wheel UI")]
    public MobileSteeringWheel steeringWheelUI;
    public Transform steeringWheelVisual;
    public float steeringWheelRotation = 300f;
    public float wheelSmoothSpeed = 10f;

    [Header("Gyroscope")]
    public float gyroSensitivity = 2f;

    [Header("Nitro")]
    public float nitroSeconds = 3f;               // tank size (garage upgrade badhata hai)
    public float nitroSpeedMultiplier = 1.3f;     // nitro me maxSpeed x 1.3
    public float nitroAccelMultiplier = 2f;
    public float minNitroToActivate = 0.6f;
    public AudioSource NitroSound;

    [Header("Score Settings")]
    public float scorePerMeter = 1f;
    public float speedBonusMultiplier = 0.05f;

    private PrometeoTouchInput leftPTI;
    private PrometeoTouchInput rightPTI;
    private PrometeoTouchInput accelPTI;
    private PrometeoTouchInput brakePTI;

    private int controlMode = 0; // 0 = Buttons, 1 = Wheel, 2 = Gyro

    private float currentSteerInput;
    private float lateralVelocity;   // side me kitni speed se ja rahi hai
    private float currentYaw;
    private float currentRoll;
    private float currentPitch;
    private float currentWheelAngle;
    private Quaternion[] wheelStartRot;

    private bool isAccelSoundPlaying = false;
    private bool isAccelerating;
    private bool isBraking;

    public float maxDeltaTime = 0.05f;   // lag spike pe car/speed ek saath na kood jaye
    private float dt;

    public float currentSpeed;
    public float distanceTraveled;
    public float currentScore;

    // ---- Kit API ----
    public bool NitroActive { get; private set; }
    public float Nitro { get; private set; }
    public float Nitro01 => nitroSeconds > 0f ? Nitro / nitroSeconds : 0f;
    public bool Controllable { get; private set; } = true;
    public float EffectiveMaxSpeed => maxSpeed * (NitroActive ? nitroSpeedMultiplier : 1f);
    public event Action OnNitroStarted;
    bool nitroRequested;

    void Awake()
    {
        Nitro = nitroSeconds;
    }

    void OnEnable() { GameSettings.OnControlModeChanged += SetControlMode; }
    void OnDisable() { GameSettings.OnControlModeChanged -= SetControlMode; }

    void Start()
    {
        currentSpeed = minSpeed;
        Nitro = nitroSeconds;
        controlMode = Mathf.Clamp(SaveSystem.Data.controlMode, 0, 2);

        // Get mobile button inputs
        if (turnLeftButton)
            leftPTI = turnLeftButton.GetComponent<PrometeoTouchInput>();

        if (turnRightButton)
            rightPTI = turnRightButton.GetComponent<PrometeoTouchInput>();

        if (accelerateButton)
            accelPTI = accelerateButton.GetComponent<PrometeoTouchInput>();

        if (brakeButton)
            brakePTI = brakeButton.GetComponent<PrometeoTouchInput>();

        // Visual pivot ready karo (rotation root pe nahi, sirf mesh pe jayegi)
        SetupVisualPivot();

        // Front wheels ki original rotation save karo
        if (frontWheels != null)
        {
            wheelStartRot = new Quaternion[frontWheels.Length];
            for (int i = 0; i < frontWheels.Length; i++)
                if (frontWheels[i] != null)
                    wheelStartRot[i] = frontWheels[i].localRotation;
        }

        // Setup audio
        SetupAudio();

        // Enable gyroscope
        if (SystemInfo.supportsGyroscope)
            Input.gyro.enabled = true;

        ApplyControlMode();
    }

    void Update()
    {
        dt = Mathf.Min(Time.deltaTime, maxDeltaTime);

        HandleSteering();
        HandleSpeed();
        UpdateCarVisuals();
        UpdateSteeringWheel();

        float delta = currentSpeed * dt;

        distanceTraveled += delta;

        HandleScore(delta);
    }

    // =========================================================
    // AUDIO SETUP
    // =========================================================

    void SetupAudio()
    {
        if (AccelerateSound != null)
        {
            AccelerateSound.loop = true;
            AccelerateSound.playOnAwake = false;
            AccelerateSound.Stop();
        }

        if (EngineSound != null)
        {
            EngineSound.loop = true;
            EngineSound.playOnAwake = false;
            EngineSound.Play();
        }

        isAccelSoundPlaying = false;
    }

    public void StopAllAudio()
    {
        if (AccelerateSound != null)
            AccelerateSound.Stop();

        if (EngineSound != null)
            EngineSound.Stop();

        isAccelSoundPlaying = false;
    }

    // =========================================================
    // STEERING (velocity based - ab jerk nahi hoga)
    // =========================================================

    void HandleSteering()
    {
        float horizontal = 0f;

        if (controlMode == 0)
        {
            if (leftPTI != null && leftPTI.buttonPressed)
                horizontal = -1f;

            if (rightPTI != null && rightPTI.buttonPressed)
                horizontal = 1f;
        }
        else if (controlMode == 1 && steeringWheelUI != null)
        {
            horizontal = steeringWheelUI.steerValue;
        }
        else if (controlMode == 2)
        {
            horizontal = Input.acceleration.x * gyroSensitivity * SaveSystem.Data.steerSensitivity;
        }
        else
        {
            horizontal = Input.GetAxis("Horizontal");
        }

#if UNITY_EDITOR
        if (Mathf.Abs(horizontal) < 0.01f) horizontal = Input.GetAxisRaw("Horizontal");
#endif
        if (!Controllable) horizontal = 0f;

        horizontal = Mathf.Clamp(horizontal, -1f, 1f);
        currentSteerInput = horizontal;

        // Side velocity ko dheere dheere target tak le jao (real car ki tarah inertia)
        float targetLateral = horizontal * steerSpeed;
        float rate = Mathf.Abs(horizontal) > 0.01f ? steerAcceleration : steerRelease;

        lateralVelocity = Mathf.MoveTowards(
            lateralVelocity,
            targetLateral,
            rate * dt
        );

        Vector3 pos = transform.position;
        pos.x += lateralVelocity * dt;

        // Road ke edge pe pahunche to side velocity zero (car edge pe seedhi ho jayegi)
        if (pos.x <= minX)
        {
            pos.x = minX;
            if (lateralVelocity < 0f) lateralVelocity = 0f;
        }
        else if (pos.x >= maxX)
        {
            pos.x = maxX;
            if (lateralVelocity > 0f) lateralVelocity = 0f;
        }

        transform.position = pos;
    }

    // =========================================================
    // VISUAL PIVOT: car ka mesh alag pivot me, camera root pe hi rahe
    // =========================================================

    void SetupVisualPivot()
    {
        if (carBody != null || !autoCreateVisualPivot)
            return;

        GameObject pivot = new GameObject("CarVisualPivot");
        pivot.transform.SetParent(transform, false);

        // Root ke saare children pivot me daalo, sirf camera wale chhod ke
        for (int i = transform.childCount - 1; i >= 0; i--)
        {
            Transform child = transform.GetChild(i);

            if (child == pivot.transform) continue;
            if (child.GetComponentInChildren<Camera>(true) != null) continue;

            child.SetParent(pivot.transform, false);
        }

        carBody = pivot.transform;

        if (pivot.transform.childCount == 0)
            Debug.LogWarning("PlayerController: car ka mesh root pe hi hai. Mesh ko child object banao aur carBody me assign karo.");
    }

    // =========================================================
    // CAR VISUALS: yaw + roll + pitch + front wheels
    // =========================================================

    // Frame-rate independent smoothing
    float Smooth(float current, float target, float speed)
    {
        return Mathf.Lerp(current, target, 1f - Mathf.Exp(-speed * dt));
    }

    void UpdateCarVisuals()
    {
        // -1 se +1 tak: abhi kitna side me ja rahi hai
        float lateralNorm = steerSpeed > 0.01f
            ? Mathf.Clamp(lateralVelocity / steerSpeed, -1f, 1f)
            : 0f;

        // Tez speed pe yaw thoda kam (stable feel)
        float speed01 = Mathf.InverseLerp(minSpeed, maxSpeed, currentSpeed);
        float yawScale = Mathf.Lerp(1f, 1f - highSpeedSteerReduction, speed01);

        // YAW: nose steer input ke saath turant ghumti hai, movement thodi baad me aati hai
        float yawSource = Mathf.Lerp(lateralNorm, currentSteerInput, 0.5f);
        float targetYaw = yawSource * maxYawAngle * yawScale;
        currentYaw = Smooth(currentYaw, targetYaw, yawSmoothness);

        // ROLL: turn me body bahar ki taraf jhukti hai (weight transfer)
        float rollDir = invertRoll ? -1f : 1f;
        float targetRoll = lateralNorm * maxRollAngle * rollDir * (0.6f + 0.4f * speed01);
        currentRoll = Smooth(currentRoll, targetRoll, rollSmoothness);

        // PITCH: accelerate = nose upar (squat), brake = nose neeche (dive)
        float targetPitch = 0f;
        if (isAccelerating) targetPitch = -maxPitchAngle;
        else if (isBraking) targetPitch = maxPitchAngle;
        currentPitch = Smooth(currentPitch, targetPitch, pitchSmoothness);

        // Root ki rotation kabhi nahi chhedni (camera isi ke saath hai)
        // Sirf visual mesh rotate hoga
        if (carBody != null)
            carBody.localRotation = Quaternion.Euler(currentPitch, currentYaw, currentRoll);

        // FRONT WHEELS: steer input se thoda pehle ghumte hain
        if (frontWheels != null && wheelStartRot != null)
        {
            currentWheelAngle = Smooth(currentWheelAngle, currentSteerInput * wheelTurnAngle, 12f);

            for (int i = 0; i < frontWheels.Length; i++)
            {
                if (frontWheels[i] == null) continue;
                frontWheels[i].localRotation =
                    wheelStartRot[i] * Quaternion.Euler(0f, currentWheelAngle, 0f);
            }
        }
    }

    // =========================================================
    // SPEED SYSTEM
    // =========================================================

    void HandleSpeed()
    {
        bool accelerating = accelPTI != null && accelPTI.buttonPressed;
        bool braking = brakePTI != null && brakePTI.buttonPressed;

#if UNITY_EDITOR
        accelerating |= Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow);
        braking |= Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow);
        if (Input.GetKeyDown(KeyCode.Space)) ActivateNitro();
#endif

        // Crash ke baad: car ruk jaati hai, road bhi (road player.currentSpeed follow karta hai)
        if (!Controllable)
        {
            nitroRequested = false;
            isAccelerating = false;
            isBraking = true;
            currentSpeed = Mathf.MoveTowards(currentSpeed, 0f, brakeForce * 2f * dt);
            HandleEngineAudio(false);
            return;
        }

        // ---- Nitro ----
        if (nitroRequested)
        {
            nitroRequested = false;
            if (!NitroActive && Nitro >= minNitroToActivate)
            {
                NitroActive = true;
                if (NitroSound != null) NitroSound.Play();
                OnNitroStarted?.Invoke();
            }
        }
        if (NitroActive)
        {
            Nitro -= dt;
            if (Nitro <= 0f || braking)
            {
                NitroActive = false;
                Nitro = Mathf.Max(0f, Nitro);
                if (NitroSound != null) NitroSound.Stop();
            }
        }

        isAccelerating = accelerating || NitroActive;
        isBraking = !isAccelerating && braking;

        if (NitroActive)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, EffectiveMaxSpeed, acceleration * nitroAccelMultiplier * dt);

            HandleEngineAudio(true);
        }
        else if (currentSpeed > maxSpeed)
        {
            // Nitro khatam: dheere dheere normal top speed pe wapas
            currentSpeed = Mathf.MoveTowards(currentSpeed, maxSpeed, naturalDrag * 1.5f * dt);
            HandleEngineAudio(accelerating);
        }
        else if (accelerating)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, maxSpeed, acceleration * dt);

            HandleEngineAudio(true);
        }
        else if (braking)
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, 0f, brakeForce * dt);

            HandleEngineAudio(false);
        }
        else
        {
            currentSpeed = Mathf.MoveTowards(
                currentSpeed, minSpeed, naturalDrag * dt);

            HandleEngineAudio(false);
        }

        currentSpeed = Mathf.Clamp(currentSpeed, minSpeed, Mathf.Max(maxSpeed, EffectiveMaxSpeed));
    }

    // =========================================================
    // ENGINE / ACCELERATION AUDIO
    // =========================================================

    void HandleEngineAudio(bool accelerating)
    {
        // Har frame Play/Stop nahi karna
        if (accelerating != isAccelSoundPlaying)
        {
            isAccelSoundPlaying = accelerating;

            if (accelerating)
            {
                if (EngineSound != null)
                    EngineSound.Stop();

                if (AccelerateSound != null && !AccelerateSound.isPlaying)
                    AccelerateSound.Play();
            }
            else
            {
                if (AccelerateSound != null)
                    AccelerateSound.Stop();

                if (EngineSound != null && !EngineSound.isPlaying)
                    EngineSound.Play();
            }
        }
    }

    // =========================================================
    // STEERING WHEEL VISUAL (dashboard wala)
    // =========================================================

    void UpdateSteeringWheel()
    {
        if (steeringWheelVisual == null)
            return;

        float targetRotation = -currentSteerInput * steeringWheelRotation;

        steeringWheelVisual.localRotation =
            Quaternion.Lerp(
                steeringWheelVisual.localRotation,
                Quaternion.Euler(0f, 0f, targetRotation),
                dt * wheelSmoothSpeed
            );
    }

    // =========================================================
    // CONTROL MODE
    // =========================================================

    public void ToggleSteeringMode()
    {
        controlMode++;

        if (controlMode > 2)
            controlMode = 0;

        GameSettings.SetControlMode(controlMode);   // save + baaki listeners ko batao
        ApplyControlMode();
    }

    /// <summary>SETTINGS screen (menu ya pause) se call hota hai.</summary>
    public void SetControlMode(int mode)
    {
        controlMode = Mathf.Clamp(mode, 0, 2);
        ApplyControlMode();
    }

    void ApplyControlMode()
    {
        if (turnLeftButton)
            turnLeftButton.SetActive(controlMode == 0);

        if (turnRightButton)
            turnRightButton.SetActive(controlMode == 0);

        if (steeringWheelUI != null)
            steeringWheelUI.gameObject.SetActive(controlMode == 1);
    }

    // =========================================================
    // KIT API: nitro, garage stats, crash / continue
    // =========================================================

    /// <summary>NITRO UI button ka OnClick yahan lagao.</summary>
    public void ActivateNitro() => nitroRequested = true;

    /// <summary>Near-miss pe nitro refill (risk = zyada speed).</summary>
    public void AddNitro(float seconds) => Nitro = Mathf.Min(nitroSeconds, Nitro + seconds);

    /// <summary>Garage ki car + upgrades apply karo. RaceSession.Awake me call hota hai.</summary>
    public void ApplyStats(CarStats s)
    {
        maxSpeed = s.topSpeedKmh;
        acceleration = s.acceleration;
        steerSpeed = s.handling;
        nitroSeconds = s.nitroSeconds;
        Nitro = nitroSeconds;
    }

    public void Stop()
    {
        Controllable = false;
        NitroActive = false;
        if (NitroSound != null) NitroSound.Stop();
    }

    public void Resume()
    {
        Controllable = true;
        lateralVelocity = 0f;
        currentSpeed = minSpeed;
    }

    // =========================================================
    // SCORE SYSTEM
    // =========================================================

    void HandleScore(float distanceDelta)
    {
        float speedFactor =
            1f +
            (currentSpeed - minSpeed) *
            speedBonusMultiplier /
            (maxSpeed - minSpeed);

        currentScore += distanceDelta * scorePerMeter * speedFactor;
    }
}