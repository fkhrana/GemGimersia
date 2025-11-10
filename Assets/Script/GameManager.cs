using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// GameManager per-scene. Besides game flow, it helps setup CameraFollow:
/// - assign camera target
/// - set left/right bias per scene
/// - apply camera bounds (manual or auto from level collider)
/// </summary>
public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

    [Header("References")]
    public PlayerController player;
    public Transform startPoint; // posisi start di GameplayFuture (kanan)
    public Camera mainCamera;

    [Header("Settings")]
    public float returnArrivalThreshold = 0.5f;

    [Header("Camera bounds override (manual)")]
    public bool overrideCameraBounds = false;
    public Vector2 camMinBounds = new Vector2(float.NegativeInfinity, float.NegativeInfinity);
    public Vector2 camMaxBounds = new Vector2(float.PositiveInfinity, float.PositiveInfinity);

    [Header("Auto camera bounds (optional)")]
    [Tooltip("If assigned, bounds will be computed from this Collider2D (e.g. BoxCollider2D) that encloses the level.")]
    public Collider2D levelBoundsCollider;
    [Tooltip("Padding (world units) added from level edges so camera doesn't stick to edge.")]
    public float paddingX = 2f;
    public float paddingY = 0.5f;
    [Tooltip("If true, compute camera bounds from levelBoundsCollider. Otherwise use manual override or none.")]
    public bool useAutoCameraBounds = true;

    bool gameOver = false;

    void Awake()
    {
        Instance = this;
        Debug.Log("[GameManager] Awake - Instance assigned for scene: " + SceneManager.GetActiveScene().name);
    }

    void Start()
    {
        string scene = SceneManager.GetActiveScene().name;
        Debug.Log($"[GameManager] Start - Scene '{scene}' initializing.");

        if (player == null)
        {
            Debug.LogError("[GameManager] Player reference not assigned in inspector!");
            return;
        }

        // Scene-specific init
        if (scene == SceneNames.GameplayFuture)
        {
            if (startPoint != null)
            {
                player.transform.position = startPoint.position;
                Debug.Log($"[GameManager] Player positioned at StartPoint {startPoint.position}.");
            }

            player.SetDirection(-1);
            player.started = false;

            if (UIManager.Instance != null)
                UIManager.Instance.ShowStartText("Press Space or Left Click to start");

            Debug.Log("[GameManager] GameplayFuture initialized: player waiting for start.");

            // Camera: player on RIGHT side in Future
            SetupCameraForScene(preferPlayerOnLeft: false);
        }
        else if (scene == SceneNames.GameplayPast)
        {
            if (GameSession.hasKey)
            {
                Vector3 spawnPos = GameSession.lastKeyPosition + Vector3.up * 0.1f;
                player.transform.position = spawnPos;
                Debug.Log($"[GameManager] GameplayPast: spawning player at lastKeyPosition {spawnPos}.");
            }
            else
            {
                Debug.LogWarning("[GameManager] GameplayPast loaded but GameSession.hasKey == false. Player will remain at scene default position.");
            }

            player.SetDirection(+1);
            player.started = true;
            player.rbWakeUp();

            if (player.rb != null)
            {
                player.rb.linearVelocity = new Vector2(player.moveSpeed * player.direction, player.rb.linearVelocity.y);
                Debug.Log($"[GameManager] GameplayPast: player started moving with linearVelocity.x = {player.rb.linearVelocity.x}");
            }

            if (UIManager.Instance != null)
                UIManager.Instance.HideStartText();

            Debug.Log("[GameManager] GameplayPast initialized: player moving to the right.");

            // Camera: player on LEFT side in Past
            SetupCameraForScene(preferPlayerOnLeft: true);
        }
        else
        {
            Debug.Log($"[GameManager] Start - Scene '{scene}' has no special initialization in GameManager.");
            SetupCameraForScene(preferPlayerOnLeft: false);
        }
    }

    /// <summary>
    /// Setup camera: assign target, set left/right bias, and apply bounds either manual or auto.
    /// </summary>
    void SetupCameraForScene(bool preferPlayerOnLeft)
    {
        if (mainCamera == null)
        {
            Debug.LogWarning("[GameManager] mainCamera not assigned in inspector. Camera setup skipped.");
            return;
        }

        var camFollow = mainCamera.GetComponent<CameraFollow>();
        if (camFollow == null)
        {
            Debug.LogWarning("[GameManager] CameraFollow component missing on mainCamera.");
            return;
        }

        camFollow.SetTarget(player.transform);
        camFollow.SetPreferPlayerOnLeft(preferPlayerOnLeft);
        Debug.Log($"[GameManager] CameraSetup: preferPlayerOnLeft={preferPlayerOnLeft}, target={player.name}");

        // Decide bounds: auto (from level collider), or manual override
        if (useAutoCameraBounds && levelBoundsCollider != null)
        {
            ApplyAutoCameraBounds(camFollow, levelBoundsCollider, paddingX, paddingY);
        }
        else if (overrideCameraBounds)
        {
            camFollow.minBounds = camMinBounds;
            camFollow.maxBounds = camMaxBounds;
            Debug.Log($"[GameManager] Applied manual camera bounds: min={camMinBounds}, max={camMaxBounds}");
        }
        else
        {
            Debug.Log("[GameManager] No camera bounds applied (infinite movement).");
        }
    }

    /// <summary>
    /// Compute camera center bounds from a Collider2D that encloses the playable level area.
    /// Works for orthographic camera (and approximates for perspective).
    /// </summary>
    void ApplyAutoCameraBounds(CameraFollow camFollow, Collider2D levelCollider, float padX, float padY)
    {
        if (mainCamera == null || camFollow == null)
        {
            Debug.LogWarning("[GameManager] Cannot apply auto camera bounds: mainCamera or camFollow null.");
            return;
        }

        Bounds levelBounds = levelCollider.bounds;
        float levelLeft = levelBounds.min.x;
        float levelRight = levelBounds.max.x;
        float levelBottom = levelBounds.min.y;
        float levelTop = levelBounds.max.y;

        Camera cam = mainCamera;
        Vector2 half = CameraHalfSizeWorld(cam, player != null ? player.transform : null);

        // compute camera center min/max so that camera viewport doesn't go past level edges,
        // but leave padding so player still has space when camera is clamped.
        float minCamX = levelLeft + half.x + padX;
        float maxCamX = levelRight - half.x - padX;
        float minCamY = levelBottom + half.y + padY;
        float maxCamY = levelTop - half.y - padY;

        // If computed values are inverted (level smaller than viewport), fall back to level center
        if (minCamX > maxCamX)
        {
            float centerX = (levelLeft + levelRight) * 0.5f;
            minCamX = maxCamX = centerX;
            Debug.LogWarning("[GameManager] Level width smaller than camera viewport; X bounds collapsed to center.");
        }
        if (minCamY > maxCamY)
        {
            float centerY = (levelBottom + levelTop) * 0.5f;
            minCamY = maxCamY = centerY;
            Debug.LogWarning("[GameManager] Level height smaller than camera viewport; Y bounds collapsed to center.");
        }

        camFollow.minBounds = new Vector2(minCamX, minCamY);
        camFollow.maxBounds = new Vector2(maxCamX, maxCamY);
        Debug.Log($"[GameManager] Auto camera bounds applied. levelBounds={levelBounds}, half={half}, camMin={camFollow.minBounds}, camMax={camFollow.maxBounds}");
    }

    Vector2 CameraHalfSizeWorld(Camera cam, Transform reference = null)
    {
        if (cam == null) return Vector2.zero;

        if (cam.orthographic)
        {
            float halfHeight = cam.orthographicSize;
            float halfWidth = halfHeight * cam.aspect;
            return new Vector2(halfWidth, halfHeight);
        }
        else
        {
            // approximate for perspective using distance to reference (or default to 10)
            float distance = 10f;
            if (reference != null)
                distance = Mathf.Abs(cam.transform.position.z - reference.position.z);
            float halfHeight = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
            float halfWidth = halfHeight * cam.aspect;
            return new Vector2(halfWidth, halfHeight);
        }
    }

    // Called by PlayerController on collision with obstacle/ground/ceiling
    public void OnPlayerDied()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("[GameManager] OnPlayerDied - player died. Restarting to GameplayFuture and resetting session.");
        GameSession.Reset();
        StartCoroutine(RestartToFutureNextFrame());
    }

    IEnumerator RestartToFutureNextFrame()
    {
        // brief frame wait to allow any collision sounds/effects
        yield return null;
        Debug.Log("[GameManager] Restarting: Loading GameplayFuture scene now.");
        SceneManager.LoadScene(SceneNames.GameplayFuture);
    }

    // Called by MachineTrigger when player reaches the time machine in Past
    public void OnPlayerReachedMachine()
    {
        if (gameOver) return;
        gameOver = true;
        Debug.Log("[GameManager] OnPlayerReachedMachine - player reached the time machine. Showing win popup.");
        if (player != null) player.enabled = false;
        if (UIManager.Instance != null) UIManager.Instance.ShowWinPopup();
    }

    // UI button: back to menu
    public void BackToMenu()
    {
        Debug.Log("[GameManager] BackToMenu called. Resetting session and loading MainMenu.");
        GameSession.Reset();
        SceneManager.LoadScene(SceneNames.MainMenu);
    }

    // Optionally restart current scene
    public void RestartCurrent()
    {
        Debug.Log("[GameManager] RestartCurrent called. Reloading current scene.");
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}