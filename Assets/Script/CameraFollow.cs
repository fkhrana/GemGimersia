using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CameraFollow with side-bias, clamped center bounds and improved gizmos.
/// This version draws visible vertical edge lines at minBounds.x and maxBounds.x (if finite),
/// so you can see the left/right camera limits in the Scene view.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Tooltip("The transform the camera will follow (assign Player).")]
    public Transform target;

    [Tooltip("Base offset from target (z is preserved for camera).")]
    public Vector3 offset = new Vector3(0f, 0f, -10f);

    [Tooltip("How far (world units) the camera should bias away from the player center.")]
    public float sideOffset = 2f;

    [Tooltip("If true, camera will place player on the left side of the view; otherwise on the right.")]
    public bool preferPlayerOnLeft = false;

    [Tooltip("Smooth time for the camera movement. Set small for snappier follow.")]
    public float smoothTime = 0.12f;

    [Tooltip("Follow on X axis?")]
    public bool followX = true;

    [Tooltip("Follow on Y axis? If false camera Y remains fixed at current Y.")]
    public bool followY = false;

    [Header("Camera center bounds (world-space). The camera center will be clamped inside this rectangle.")]
    public Vector2 minBounds = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    public Vector2 maxBounds = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

    [Header("Gizmo / Edge Lines")]
    [Tooltip("If true, draw gizmos for bounds and viewport in Scene view.")]
    public bool showGizmosAlways = true;
    [Tooltip("Draw the camera viewport rectangle at the clamped center.")]
    public bool drawViewportGizmo = true;
    [Tooltip("Draw vertical edge lines at minBounds.x and maxBounds.x (when finite).")]
    public bool showEdgeLines = true;
    [Tooltip("Color used for gizmo bounds and edge lines.")]
    public Color gizmoColor = new Color(0.2f, 0.8f, 0.9f, 0.25f);
    [Tooltip("Color for the vertical edge lines.")]
    public Color edgeLineColor = new Color(1f, 0.25f, 0.25f, 1f);

    Camera cam;
    Vector3 velocity = Vector3.zero;
    bool triedAutoFind = false;
    bool loggedNoTargetWarning = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        // Auto-find player by tag if target is null (only try once)
        if (target == null && !triedAutoFind)
        {
            triedAutoFind = true;
            var go = GameObject.FindWithTag("Player");
            if (go != null)
            {
                target = go.transform;
                Debug.Log($"[CameraFollow] Auto-assigned target to '{go.name}' via tag 'Player'.");
            }
            else
            {
                if (!loggedNoTargetWarning)
                {
                    Debug.LogWarning("[CameraFollow] No target assigned and no GameObject with tag 'Player' found. Assign target in Inspector or call SetTarget().");
                    loggedNoTargetWarning = true;
                }
            }
        }

        if (target == null) return;

        Vector3 desired = target.position;

        // Side bias so player appears left or right
        float bias = preferPlayerOnLeft ? sideOffset : -sideOffset;
        desired.x += bias;

        // Vertical handling
        if (followY)
            desired.y += offset.y;
        else
            desired.y = transform.position.y; // lock vertical

        // Preserve camera z
        desired.z = transform.position.z;

        if (!followX) desired.x = transform.position.x;
        if (!followY) desired.y = transform.position.y;

        // Clamp camera center inside bounds (if finite)
        if (!float.IsInfinity(minBounds.x) && !float.IsInfinity(maxBounds.x))
            desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
        if (!float.IsInfinity(minBounds.y) && !float.IsInfinity(maxBounds.y))
            desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);

        transform.position = Vector3.SmoothDamp(transform.position, desired, ref velocity, smoothTime);
    }

    public void SetTarget(Transform newTarget)
    {
        target = newTarget;
        triedAutoFind = true; // don't auto-find anymore
        loggedNoTargetWarning = false;
        Debug.Log($"[CameraFollow] SetTarget called. New target = {(newTarget != null ? newTarget.name : "null")}");
    }

    public void SetPreferPlayerOnLeft(bool preferLeft)
    {
        preferPlayerOnLeft = preferLeft;
        Debug.Log($"[CameraFollow] SetPreferPlayerOnLeft: {preferLeft} (sideOffset = {sideOffset})");
    }

    // Helper: compute camera half extents in world units (works for orthographic)
    Vector2 CameraHalfSizeWorld()
    {
        if (cam == null) cam = GetComponent<Camera>();
        if (cam == null) return Vector2.one * 5f;

        if (cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            return new Vector2(halfWidth, halfHeight);
        }
        else
        {
            float distance = 10f;
            if (target != null)
                distance = Mathf.Abs(cam.transform.position.z - target.position.z);
            float halfHeight = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
            float halfWidth = halfHeight * cam.aspect;
            return new Vector2(halfWidth, halfHeight);
        }
    }

    void OnDrawGizmos()
    {
        if (!showGizmosAlways && !SelectionContainsThis()) return;

        if (cam == null) cam = GetComponent<Camera>();
        Vector2 half = CameraHalfSizeWorld();

        // Draw bounds rect (centered)
        if (minBounds.x <= maxBounds.x && minBounds.y <= maxBounds.y)
        {
            Gizmos.color = gizmoColor;
            Vector3 center = new Vector3((minBounds.x + maxBounds.x) * 0.5f, (minBounds.y + maxBounds.y) * 0.5f, transform.position.z);
            Vector3 size = new Vector3(Mathf.Max(0.01f, maxBounds.x - minBounds.x), Mathf.Max(0.01f, maxBounds.y - minBounds.y), 0.1f);
            Gizmos.DrawCube(center, size);

            Gizmos.color = new Color(gizmoColor.r, gizmoColor.g, gizmoColor.b, 1f);
            Gizmos.DrawWireCube(center, size);
        }

        // Draw viewport rectangle (where camera would be centered given target + bias and clamped)
        if (drawViewportGizmo && cam != null)
        {
            Vector3 desired = (target != null) ? target.position : transform.position;
            float bias = preferPlayerOnLeft ? sideOffset : -sideOffset;
            desired.x += bias;
            desired.y = followY ? desired.y + offset.y : transform.position.y;

            if (!float.IsInfinity(minBounds.x) && !float.IsInfinity(maxBounds.x))
                desired.x = Mathf.Clamp(desired.x, minBounds.x, maxBounds.x);
            if (!float.IsInfinity(minBounds.y) && !float.IsInfinity(maxBounds.y))
                desired.y = Mathf.Clamp(desired.y, minBounds.y, maxBounds.y);

            Vector3 vCenter = new Vector3(desired.x, desired.y, transform.position.z);
            Vector3 vSize = new Vector3(half.x * 2f, half.y * 2f, 0.1f);

            Gizmos.color = new Color(1f, 0.65f, 0f, 0.15f);
            Gizmos.DrawCube(vCenter, vSize);
            Gizmos.color = new Color(1f, 0.65f, 0f, 1f);
            Gizmos.DrawWireCube(vCenter, vSize);
        }

        // Draw vertical edge lines at minBounds.x and maxBounds.x
        if (showEdgeLines && cam != null)
        {
            float topY, bottomY;
            // determine vertical span for the lines:
            if (!float.IsInfinity(minBounds.y) && !float.IsInfinity(maxBounds.y))
            {
                bottomY = minBounds.y;
                topY = maxBounds.y;
            }
            else
            {
                // fallback: use camera center +/- a couple screen heights to ensure lines visible
                Vector2 halfSize = half;
                bottomY = transform.position.y - halfSize.y * 2f;
                topY = transform.position.y + halfSize.y * 2f;
            }

            Gizmos.color = edgeLineColor;

            if (!float.IsInfinity(minBounds.x))
            {
                Vector3 p1 = new Vector3(minBounds.x, bottomY, transform.position.z);
                Vector3 p2 = new Vector3(minBounds.x, topY, transform.position.z);
                Gizmos.DrawLine(p1, p2);
#if UNITY_EDITOR
                Handles.color = edgeLineColor;
                Handles.DrawLine(p1, p2);
                Handles.Label(new Vector3(minBounds.x, topY, transform.position.z), "LeftCameraEdge");
#endif
            }

            if (!float.IsInfinity(maxBounds.x))
            {
                Vector3 p1 = new Vector3(maxBounds.x, bottomY, transform.position.z);
                Vector3 p2 = new Vector3(maxBounds.x, topY, transform.position.z);
                Gizmos.DrawLine(p1, p2);
#if UNITY_EDITOR
                Handles.color = edgeLineColor;
                Handles.DrawLine(p1, p2);
                Handles.Label(new Vector3(maxBounds.x, topY, transform.position.z), "RightCameraEdge");
#endif
            }
        }
    }

    // helper to avoid using UnityEditor.Selection in runtime builds; uses conditional compilation
    bool SelectionContainsThis()
    {
#if UNITY_EDITOR
        try { return UnityEditor.Selection.Contains(gameObject); } catch { return false; }
#else
        return false;
#endif
    }
}