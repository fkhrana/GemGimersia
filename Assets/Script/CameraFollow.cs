using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// CameraFollow: follows only on X with optional side bias and left/right clamp.
/// - If preferPlayerOnLeft = true, camera center = player.x + sideOffset (player appears left).
/// - If preferPlayerOnLeft = false, camera center = player.x - sideOffset (player appears right).
/// - Camera center X is clamped between minBounds.x and maxBounds.x.
/// - Keep SetTarget and SetPreferPlayerOnLeft APIs for GameManager compatibility.
/// </summary>
[RequireComponent(typeof(Camera))]
public class CameraFollow : MonoBehaviour
{
    [Header("Target")]
    [Tooltip("Assign player transform (or give player tag 'Player' for auto-find).")]
    public Transform target;

    [Header("Follow")]
    [Tooltip("Smooth time for X movement. Set to 0 for instant follow.")]
    public float smoothTime = 0.08f;

    [Header("Side bias")]
    [Tooltip("When preferPlayerOnLeft=true, player is placed this many units from center to the left.\nWhen preferPlayerOnLeft=false, player is placed this many units from center to the right.")]
    public float sideOffset = 2f;
    [Tooltip("If true, place player on left side. If false, place player on right side.")]
    public bool preferPlayerOnLeft = false;

    [Header("Camera center bounds (world-space). X components are used for left/right clamp.")]
    public Vector2 minBounds = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    public Vector2 maxBounds = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

    [Header("Gizmos")]
    [Tooltip("Draw vertical edge lines at minBounds.x and maxBounds.x")]
    public bool showEdgeLines = true;
    public Color edgeLineColor = new Color(1f, 0.25f, 0.25f, 1f);

    Camera cam;
    float velX = 0f;
    bool triedAutoFind = false;

    void Awake()
    {
        cam = GetComponent<Camera>();
    }

    void LateUpdate()
    {
        // Auto-find player by tag if target is null (only once)
        if (target == null && !triedAutoFind)
        {
            triedAutoFind = true;
            var go = GameObject.FindWithTag("Player");
            if (go != null)
            {
                target = go.transform;
                Debug.Log($"[CameraFollow] Auto-assigned target to '{go.name}' via tag 'Player'.");
            }
        }

        if (target == null) return;

        // compute bias: player appears left if preferPlayerOnLeft == true
        float bias = preferPlayerOnLeft ? sideOffset : -sideOffset;

        // Desired X is player's X plus bias
        float desiredX = target.position.x + bias;

        // Clamp to bounds (minBounds.x / maxBounds.x represent camera center X limits)
        if (!float.IsInfinity(minBounds.x) && !float.IsInfinity(maxBounds.x))
            desiredX = Mathf.Clamp(desiredX, minBounds.x, maxBounds.x);

        // Preserve current Y/Z
        Vector3 current = transform.position;
        if (smoothTime > 0f)
        {
            float newX = Mathf.SmoothDamp(current.x, desiredX, ref velX, smoothTime);
            transform.position = new Vector3(newX, current.y, current.z);
        }
        else
        {
            transform.position = new Vector3(desiredX, current.y, current.z);
        }
    }

    /// <summary>
    /// Backwards-compatible API: allow GameManager to set the follow target.
    /// </summary>
    public void SetTarget(Transform t)
    {
        target = t;
        triedAutoFind = true; // stop auto-find
    }

    /// <summary>
    /// Set whether to keep player on left side (true) or right side (false).
    /// </summary>
    public void SetPreferPlayerOnLeft(bool preferLeft)
    {
        preferPlayerOnLeft = preferLeft;
        Debug.Log($"[CameraFollow] SetPreferPlayerOnLeft: {preferLeft}, sideOffset={sideOffset}");
    }

    void OnDrawGizmos()
    {
        if (!showEdgeLines) return;
        if (cam == null) cam = GetComponent<Camera>();

        Gizmos.color = edgeLineColor;

        // determine vertical span for lines: a few screens tall so lines are visible
        float halfHeight = cam != null && cam.orthographic ? cam.orthographicSize : 5f;
        float top = transform.position.y + halfHeight * 2f;
        float bottom = transform.position.y - halfHeight * 2f;

        if (!float.IsInfinity(minBounds.x))
        {
            Vector3 p1 = new Vector3(minBounds.x, bottom, transform.position.z);
            Vector3 p2 = new Vector3(minBounds.x, top, transform.position.z);
            Gizmos.DrawLine(p1, p2);
#if UNITY_EDITOR
            Handles.color = edgeLineColor;
            Handles.DrawLine(p1, p2);
            Handles.Label(new Vector3(minBounds.x, top, transform.position.z), "CameraLeftEdge");
#endif
        }

        if (!float.IsInfinity(maxBounds.x))
        {
            Vector3 p1 = new Vector3(maxBounds.x, bottom, transform.position.z);
            Vector3 p2 = new Vector3(maxBounds.x, top, transform.position.z);
            Gizmos.DrawLine(p1, p2);
#if UNITY_EDITOR
            Handles.color = edgeLineColor;
            Handles.DrawLine(p1, p2);
            Handles.Label(new Vector3(maxBounds.x, top, transform.position.z), "CameraRightEdge");
#endif
        }
    }
}