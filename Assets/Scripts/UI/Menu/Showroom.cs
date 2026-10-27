using System.Collections.Generic;
using UnityEngine;

namespace Redshift
{
    /// <summary>
    /// The 3D garage turntable behind HOME and GARAGE. Shows the real in-game car model,
    /// so the menu always matches the gameplay. Drag to spin, auto-rotates when idle.
    /// </summary>
    public class Showroom : MonoBehaviour
    {
        public Transform turntable;
        public float autoRotateSpeed = 18f;
        public float dragSensitivity = 0.3f;
        public float idleResumeDelay = 2f;

        readonly Dictionary<string, GameObject> cache = new Dictionary<string, GameObject>();
        GameObject current;
        float idleTimer;
        float lastPointerX;
        bool dragging;

        void Start()
        {
            if (GameManager.I != null && GameManager.I.SelectedCar != null) Display(GameManager.I.SelectedCar);
        }

        public void Display(CarData car)
        {
            if (car == null || car.prefab == null) return;
            if (current) current.SetActive(false);
            if (!cache.TryGetValue(car.id, out var go))
            {
                go = Instantiate(car.prefab, turntable, false);
                cache[car.id] = go;
            }
            go.SetActive(true);
            current = go;
        }

        void Update()
        {
#if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetMouseButtonDown(0) && !IsPointerOverUI()) { dragging = true; lastPointerX = Input.mousePosition.x; }
            if (Input.GetMouseButtonUp(0)) dragging = false;
            if (dragging)
            {
                float dx = Input.mousePosition.x - lastPointerX;
                lastPointerX = Input.mousePosition.x;
                turntable.Rotate(0f, -dx * dragSensitivity, 0f, Space.World);
                idleTimer = idleResumeDelay;
                return;
            }
#endif
            if (idleTimer > 0f) { idleTimer -= Time.deltaTime; return; }
            turntable.Rotate(0f, autoRotateSpeed * Time.deltaTime, 0f, Space.World);
        }

#if ENABLE_LEGACY_INPUT_MANAGER
        static bool IsPointerOverUI()
        {
            var es = UnityEngine.EventSystems.EventSystem.current;
            return es != null && es.IsPointerOverGameObject() ||
                   es != null && Input.touchCount > 0 && es.IsPointerOverGameObject(Input.GetTouch(0).fingerId);
        }
#endif
    }
}
