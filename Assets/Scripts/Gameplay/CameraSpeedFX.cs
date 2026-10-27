using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// Put on your existing camera (it can stay a child of the player, as now).
    /// Doesn't move the camera around; only adds: FOV widening with speed, extra FOV + shake on nitro,
    /// a crash shake, and optional extra camera views for the camera button.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class CameraSpeedFX : MonoBehaviour
    {
        public PlayerController player;
        public RaceSession session;                // optional: shake on crash

        [Header("FOV")]
        public float baseFov = 60f;
        public float maxSpeedFov = 72f;
        public float nitroExtraFov = 8f;

        [Header("Shake")]
        public float highSpeedShake = 0.02f;
        public float nitroShake = 0.06f;
        public float crashShake = 0.5f;

        [Tooltip("OFF = only change FOV (use if another script already moves this camera)")]
        public bool moveCamera = true;

        [Header("Extra views (optional, local to the camera's parent)")]
        [Tooltip("Leave empty to keep only your current camera position")]
        public Vector3[] extraViewPositions;
        public Vector3[] extraViewRotations;

        Camera cam;
        Vector3 basePos;
        Quaternion baseRot;
        int view = -1;           // -1 = your original camera position
        float crashTimer;

        void Awake()
        {
            cam = GetComponent<Camera>();
            basePos = transform.localPosition;
            baseRot = transform.localRotation;
            int saved = PlayerPrefs.GetInt("camView", -1);
            view = extraViewPositions != null && saved < extraViewPositions.Length ? saved : -1;
        }

        void OnEnable() { if (session != null) session.OnCrashed += CrashShake; }
        void OnDisable() { if (session != null) session.OnCrashed -= CrashShake; }

        /// <summary>Camera button OnClick.</summary>
        public void CycleView()
        {
            int count = extraViewPositions != null ? extraViewPositions.Length : 0;
            view = view + 1 >= count ? -1 : view + 1;
            PlayerPrefs.SetInt("camView", view);
        }

        public void CrashShake() => crashTimer = 0.4f;

        void LateUpdate()
        {
            if (player == null) return;
            float dt = Time.deltaTime;

            float top = player.maxSpeed * player.nitroSpeedMultiplier;
            float t = Mathf.InverseLerp(player.minSpeed, top, player.currentSpeed);

            float fov = Mathf.Lerp(baseFov, maxSpeedFov, t) + (player.NitroActive ? nitroExtraFov : 0f);
            cam.fieldOfView = Mathf.Lerp(cam.fieldOfView, fov, 1f - Mathf.Exp(-5f * dt));

            Vector3 pos = basePos;
            Quaternion rot = baseRot;
            if (view >= 0)
            {
                pos = extraViewPositions[view];
                if (extraViewRotations != null && view < extraViewRotations.Length) rot = Quaternion.Euler(extraViewRotations[view]);
            }

            if (!moveCamera) return;

            float shake = highSpeedShake * t + (player.NitroActive ? nitroShake : 0f);
            if (crashTimer > 0f) { crashTimer -= dt; shake += crashShake * crashTimer; }
            if (shake > 0f)
            {
                float n = Time.time * 25f;
                pos += new Vector3((Mathf.PerlinNoise(n, 0f) - 0.5f) * shake, (Mathf.PerlinNoise(0f, n) - 0.5f) * shake, 0f);
            }

            transform.localPosition = pos;
            transform.localRotation = rot;
        }
    }
}
