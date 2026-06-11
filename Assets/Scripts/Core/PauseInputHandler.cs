using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

// Abre/cierra el menu de pausa con Esc, el boton Start del gamepad,
// o un toque simultaneo de dos dedos (tap corto, sin arrastre).
public class PauseInputHandler : MonoBehaviour
{
    [Header("Gesto de dos dedos")]
    [SerializeField] bool enableTwoFingerTap = true;
    [SerializeField] float maxTapDuration = 0.3f;
    [SerializeField] float maxTapMovement = 60f;
    [SerializeField] float maxStartGap = 0.15f;

    class TouchInfo
    {
        public Vector2 startPos;
        public float startTime;
        public float endTime = -1f;
        public bool moved;
    }

    readonly Dictionary<int, TouchInfo> touches = new Dictionary<int, TouchInfo>();
    bool gestureInvalid;

    void Update()
    {
        if (GameManager.Instance == null) return;
        if (GameManager.Instance.CurrentState == GameManager.State.GameOver) return;

        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
            GameManager.Instance.TogglePause();

        if (Gamepad.current != null && Gamepad.current.startButton.wasPressedThisFrame)
            GameManager.Instance.TogglePause();

        if (enableTwoFingerTap)
            UpdateTwoFingerTap();
    }

    void UpdateTwoFingerTap()
    {
        var touchscreen = Touchscreen.current;
        if (touchscreen == null) return;

        foreach (var touch in touchscreen.touches)
        {
            int id = touch.touchId.ReadValue();
            var phase = touch.phase.ReadValue();
            Vector2 pos = touch.position.ReadValue();

            switch (phase)
            {
                case TouchPhase.Began:
                    if (touches.Count >= 2) gestureInvalid = true;
                    touches[id] = new TouchInfo { startPos = pos, startTime = Time.unscaledTime };
                    break;

                case TouchPhase.Moved:
                    if (touches.TryGetValue(id, out var moving) &&
                        Vector2.Distance(moving.startPos, pos) > maxTapMovement)
                        moving.moved = true;
                    break;

                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    if (touches.TryGetValue(id, out var ending))
                        ending.endTime = Time.unscaledTime;
                    break;
            }
        }

        if (touches.Count == 0) return;

        foreach (var info in touches.Values)
        {
            if (info.endTime < 0f) return; // todavia hay dedos sobre la pantalla
        }

        if (!gestureInvalid && touches.Count == 2)
        {
            float minStart = float.MaxValue, maxStart = float.MinValue, maxDuration = 0f;
            bool anyMoved = false;

            foreach (var info in touches.Values)
            {
                anyMoved   |= info.moved;
                minStart    = Mathf.Min(minStart, info.startTime);
                maxStart    = Mathf.Max(maxStart, info.startTime);
                maxDuration = Mathf.Max(maxDuration, info.endTime - info.startTime);
            }

            if (!anyMoved && maxDuration <= maxTapDuration && (maxStart - minStart) <= maxStartGap)
                GameManager.Instance.TogglePause();
        }

        touches.Clear();
        gestureInvalid = false;
    }
}
