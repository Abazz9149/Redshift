using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// Works with your RoadManagerRecycle + PlayerController (player stays at Z≈0, traffic moves past).
    /// - Overtake: a traffic car goes from in front of the player to behind
    /// - Near-miss: it passes with a side gap smaller than nearMissGap, above minNearMissSpeed
    /// - Crash: real mesh-bounds overlap (same idea as AICarController.CheckCrash)
    /// Runs after the road has moved traffic this frame.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public class TrafficInteraction : MonoBehaviour
    {
        public PlayerController player;
        public RoadManagerRecycle road;
        public RaceSession session;

        [Header("Near-miss")]
        [Tooltip("Side gap (metres) that still counts as a near-miss")]
        public float nearMissGap = 1.0f;
        [Tooltip("Same units as PlayerController.currentSpeed")]
        public float minNearMissSpeed = 50f;

        [Header("Crash")]
        [Tooltip("OFF if your TrafficAI / collision code already handles player crashes — then call RaceSession.Crash() from there")]
        public bool crashOnContact = true;
        [Range(0.5f, 1f)] public float crashForgiveness = 0.85f;

        class PassState { public bool wasAhead; public bool passed; }

        readonly Dictionary<TrafficAI, PassState> states = new Dictionary<TrafficAI, PassState>();
        readonly Dictionary<Transform, CarBounds> sizes = new Dictionary<Transform, CarBounds>();
        CarBounds me;

        void OnEnable() { if (road) road.OnTrafficSpawned += ResetState; }
        void OnDisable() { if (road) road.OnTrafficSpawned -= ResetState; }

        void Start()
        {
            // PlayerController (-100) has already created carBody in its Start
            Transform mesh = player.carBody != null ? player.carBody : player.transform;
            me = CarBounds.Measure(mesh, player.transform);
        }

        void ResetState(TrafficAI ai)
        {
            if (!states.TryGetValue(ai, out var st)) { st = new PassState(); states[ai] = st; }
            st.wasAhead = true;
            st.passed = false;
        }

        CarBounds SizeOf(Transform t)
        {
            if (!sizes.TryGetValue(t, out var b))
            {
                b = CarBounds.Measure(t, t);
                sizes[t] = b;
            }
            return b;
        }

        void Update()
        {
            if (session == null || !session.Running) return;

            Vector3 pp = player.transform.position;
            float px = pp.x + me.offX;
            float pz = pp.z + me.offZ;
            var traffic = road.ActiveTraffic;

            for (int i = 0; i < traffic.Count; i++)
            {
                TrafficAI ai = traffic[i];
                if (ai == null) continue;
                Transform t = ai.transform;
                CarBounds s = SizeOf(t);

                float cx = t.position.x + s.offX;
                float cz = t.position.z + s.offZ;
                float dx = cx - px;
                float dz = cz - pz;

                if (crashOnContact &&
                    Mathf.Abs(dx) < (me.halfX + s.halfX) * crashForgiveness &&
                    Mathf.Abs(dz) < (me.halfZ + s.halfZ) * crashForgiveness)
                {
                    session.Crash();
                    return;
                }

                if (!states.TryGetValue(ai, out var st))
                {
                    st = new PassState { wasAhead = dz > 0f };
                    states[ai] = st;
                }

                bool ahead = dz > 0f;
                if (st.wasAhead && !ahead && !st.passed)
                {
                    st.passed = true;
                    float sideGap = Mathf.Abs(dx) - (me.halfX + s.halfX);
                    bool near = sideGap < nearMissGap && player.currentSpeed >= minNearMissSpeed;
                    session.RegisterPass(road.IsOncoming(ai), near);
                }
                st.wasAhead = ahead;
            }
        }
    }
}
