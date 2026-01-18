using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;

/// <summary>
/// Simple 1-finger touch orbit controller for mobile (with mouse fallback in editor).
/// </summary>
[DisallowMultipleComponent]
public class TouchOrbitCamera : MonoBehaviour
{
    [Header("Target")]
    [SerializeField] private Transform target;

    [Header("Orbit")]
    [SerializeField] private float distance = 1000f;
    [SerializeField] private float yawSpeed = 0.15f;   // degrees per pixel
    [SerializeField] private float pitchSpeed = 0.15f; // degrees per pixel
    [SerializeField] private float minPitch = -80f;
    [SerializeField] private float maxPitch = 80f;
    [SerializeField] private float touchDeadzonePixels = 1.5f;

    [Header("UI")]
    [SerializeField] private bool ignoreWhenPointerOverUI = true;

    private float _yaw;
    private float _pitch;

    private bool _hasInitial;
    private Vector3 _initialPosition;
    private Quaternion _initialRotation;
    private float _initialYaw;
    private float _initialPitch;
    private float _initialDistance;

    private bool _hasTouchLastPos;
    private Vector2 _touchLastPos;

    private void OnEnable()
    {
        // Required for EnhancedTouch API (works with Input System package).
        if (!EnhancedTouchSupport.enabled)
            EnhancedTouchSupport.Enable();
    }

    private void Start()
    {
        // Cache initial camera transform.
        _initialPosition = transform.position;
        _initialRotation = transform.rotation;
        _hasInitial = true;

        if (target == null)
            return;

        // Initialize yaw/pitch/distance from current camera offset.
        var offset = transform.position - target.position;
        distance = offset.magnitude > 0.001f ? offset.magnitude : distance;

        var dir = offset.normalized;
        _yaw = Mathf.Atan2(dir.x, dir.z) * Mathf.Rad2Deg;
        _pitch = Mathf.Asin(Mathf.Clamp(dir.y, -1f, 1f)) * Mathf.Rad2Deg;
        _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

        _initialYaw = _yaw;
        _initialPitch = _pitch;
        _initialDistance = distance;
        _hasInitial = true;

        ApplyTransform();
    }

    private void Update()
    {
        if (target == null)
            return;

        if (TryGetDragDelta(out var delta))
        {
            _yaw += delta.x * yawSpeed;
            _pitch -= delta.y * pitchSpeed;
            _pitch = Mathf.Clamp(_pitch, minPitch, maxPitch);

            ApplyTransform();
        }
    }

    private bool TryGetDragDelta(out Vector2 delta)
    {
        delta = default;

        // Touch
        if (Touch.activeFingers.Count == 1)
        {
            var finger = Touch.activeFingers[0];
            var t = finger.currentTouch;

            if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject(t.touchId))
                return false;

            var pos = t.screenPosition;
            if (t.phase == UnityEngine.InputSystem.TouchPhase.Began || !_hasTouchLastPos)
            {
                _touchLastPos = pos;
                _hasTouchLastPos = true;
                return false;
            }

            if (t.phase == UnityEngine.InputSystem.TouchPhase.Ended || t.phase == UnityEngine.InputSystem.TouchPhase.Canceled)
            {
                _hasTouchLastPos = false;
                return false;
            }

            // Compute delta manually to reduce device-specific jitter from Touch.delta.
            delta = pos - _touchLastPos;
            _touchLastPos = pos;

            if (delta.sqrMagnitude < touchDeadzonePixels * touchDeadzonePixels)
                return false;

            return true;
        }

        _hasTouchLastPos = false;

        // Mouse fallback (Editor/Desktop)
        if (Mouse.current != null && Mouse.current.leftButton.isPressed)
        {
            if (ignoreWhenPointerOverUI && EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return false;

            delta = Mouse.current.delta.ReadValue();
            return delta.sqrMagnitude > 0.0001f;
        }

        return false;
    }

    private void ApplyTransform()
    {
        var rot = Quaternion.Euler(_pitch, _yaw, 0f);
        var pos = target.position + rot * new Vector3(0f, 0f, -distance);
        transform.SetPositionAndRotation(pos, rot);
    }

    public void ResetToInitial()
    {
        if (!_hasInitial)
            return;

        // Restore camera transform and internal orbit parameters.
        transform.SetPositionAndRotation(_initialPosition, _initialRotation);

        if (target == null)
            return;

        _yaw = _initialYaw;
        _pitch = _initialPitch;
        distance = _initialDistance;
        ApplyTransform();
    }
}


