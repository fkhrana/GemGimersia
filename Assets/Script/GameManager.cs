using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Simplified GameManager camera setup for this project:
/// - Keeps existing game flow (start, death, machine win).
/// - Simplifies camera setup: assigns CameraFollow target and writes only left/right (X) bounds
///   into CameraFollow.minBounds.x / CameraFollow.maxBounds.x.
/// - Supports manual camMinX/camMaxX override or automatic calculation from a level Collider2D.
/// - Keeps API compatibility with existing scripts (SetTarget / SetPreferPlayerOnLeft calls still OK).
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

    [Header("Camera bounds (simple X-only)")]
    [Tooltip("If true, use manual camMinX/camMaxX values (camera center X).")]
    public bool overrideCameraBounds = false;
    [Tooltip("Manual camera center min X")]
    public float camMinX = float.NegativeInfinity;
    [Tooltip("Manual camera center max X")]
    public float camMaxX = float.PositiveInfinity;

    [Header("Auto camera bounds (optional)")]
    [Tooltip("If assigned and useAutoCameraBounds true, min/max X will be computed from this Collider2D.")]
    public Collider2D levelBoundsCollider;
    [Tooltip("Padding (world units) to leave between camera edge and level edge.")]
    public float paddingX = 2f;
    [Tooltip("If true and levelBoundsCollider assigned, compute camera bounds automatically.")]
    public bool useAutoCameraBounds = true;

    [Header("Settings")]
    public float restartDelay = 2.5f; // extra wait after animation/sound


    bool gameOver = false;
    public Animator animator;
    public AudioSource hit;

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

            // Camera: no side-bias in simplified manager but keep call for compatibility
            SetupCameraForScene(preferPlayerOnLeft: false);
        }
        else if (scene == SceneNames.GameplayPast)
        {
            if (GameSession.hasKey)
            {
                // Compute a safe spawn position slightly above the recorded key position to avoid
                // spawning inside the ground/platform and causing immediate physics overlap/jitter.
                Vector3 spawnPos = GameSession.lastKeyPosition + Vector3.up * 0.1f;

                // If we have access to a SpriteRenderer or Collider2D on the player, use its extents
                // to guarantee we are fully clear of geometry.
                var sr = player != null ? player.GetComponent<SpriteRenderer>() : null;
                var col = player != null ? player.GetComponent<Collider2D>() : null;
                float halfHeight = 0.5f;
                if (sr != null)
                    halfHeight = sr.bounds.extents.y;
                else if (col != null)
                    halfHeight = col.bounds.extents.y;

                // Add a small safety margin above the recorded position
                spawnPos = GameSession.lastKeyPosition + Vector3.up * (halfHeight + 0.08f);

                player.transform.position = spawnPos;
                Debug.Log($"[GameManager] GameplayPast: spawning player at safe lastKeyPosition {spawnPos}.");
            }
            else
            {
                Debug.LogWarning("[GameManager] GameplayPast loaded but GameSession.hasKey == false. Player will remain at scene default position.");
            }

            player.SetDirection(+1);
            player.started = true;

            // Ensure rigidbody is awake and positioned properly before setting velocity
            player.rbWakeUp();

            if (player.rb != null)
            {
                // set horizontal movement immediately
                player.rb.linearVelocity = new Vector2(player.moveSpeed * player.direction, player.rb.linearVelocity.y);
                Debug.Log($"[GameManager] GameplayPast: player started moving with linearVelocity.x = {player.rb.linearVelocity.x}");
            }

            if (UIManager.Instance != null)
                UIManager.Instance.HideStartText();

            Debug.Log("[GameManager] GameplayPast initialized: player moving to the right.");

            SetupCameraForScene(preferPlayerOnLeft: true);
        }
        else
        {
            Debug.Log($"[GameManager] Start - Scene '{scene}' has no special initialization in GameManager.");
            SetupCameraForScene(preferPlayerOnLeft: false);
        }
    }

    /// <summary>
    /// Setup camera: assign target, optionally compute/apply X-only bounds.
    /// Writes X bounds into CameraFollow.minBounds.x / maxBounds.x to match simplified CameraFollow.
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

        // Assign player as camera target
        camFollow.SetTarget(player.transform);

        // Keep compatibility: allow GameManager to still signal preferPlayerOnLeft
        camFollow.SetPreferPlayerOnLeft(preferPlayerOnLeft);

        // Determine bounds X
        float minX = float.NegativeInfinity;
        float maxX = float.PositiveInfinity;

        if (useAutoCameraBounds && levelBoundsCollider != null)
        {
            // Calculate based on level collider and camera half width (orthographic expected)
            Bounds levelB = levelBoundsCollider.bounds;
            float levelLeft = levelB.min.x;
            float levelRight = levelB.max.x;

            float halfWidth = CameraHalfWidthWorld(mainCamera, player != null ? player.transform : null);

            minX = levelLeft + halfWidth + paddingX;
            maxX = levelRight - halfWidth - paddingX;

            if (minX > maxX)
            {
                float center = (levelLeft + levelRight) * 0.5f;
                minX = maxX = center;
                Debug.LogWarning("[GameManager] Level narrower than camera viewport; X bounds collapsed to center.");
            }

            Debug.Log($"[GameManager] Auto camera X bounds computed: minX={minX:F2}, maxX={maxX:F2}");
        }
        else if (overrideCameraBounds)
        {
            minX = camMinX;
            maxX = camMaxX;
            Debug.Log($"[GameManager] Manual camera X bounds used: minX={minX}, maxX={maxX}");
        }
        else
        {
            Debug.Log("[GameManager] No camera X bounds applied (infinite movement).");
        }

        // Apply to CameraFollow's Vector2 min/max (preserve Y components)
        Vector2 newMin = camFollow.minBounds;
        Vector2 newMax = camFollow.maxBounds;
        newMin.x = minX;
        newMax.x = maxX;
        camFollow.minBounds = newMin;
        camFollow.maxBounds = newMax;
    }

    float CameraHalfWidthWorld(Camera cam, Transform reference = null)
    {
        if (cam == null) return 0f;
        if (cam.orthographic)
        {
            float halfH = cam.orthographicSize;
            return halfH * cam.aspect;
        }
        else
        {
            float distance = 10f;
            if (reference != null)
                distance = Mathf.Abs(cam.transform.position.z - reference.position.z);
            float halfH = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
            return halfH * cam.aspect;
        }
    }

    // Called by PlayerController on collision with obstacle/ground/ceiling

    // ✅ Called by PlayerController when dying
    public void OnPlayerDied()
    {
        if (gameOver) return;
        gameOver = true;

        Debug.Log("[GameManager] OnPlayerDied - Playing animation and sound.");

        if (animator != null)
            animator.Play("Die");

        if (hit != null)
            hit.Play();

        GameSession.Reset();
        StartCoroutine(RestartAfterDelay());
    }

    IEnumerator RestartAfterDelay()
    {
        // Wait for animation or sound duration + manual delay
        float waitTime = restartDelay;

        // if audio clip is longer, use that time instead
        if (hit != null && hit.clip != null)
            waitTime = Mathf.Max(waitTime, hit.clip.length + 0.5f);

        yield return new WaitForSeconds(waitTime);

        Debug.Log("[GameManager] Restarting GameplayFuture...");
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

    public void StartGameplay()
    {
        Debug.Log("[GameManager] Cutscene done. Enabling gameplay.");
        if (player != null)
        {
            player.started = true;
            // allow input and movement player.rbWakeUp(); 
        }
        if (UIManager.Instance != null)
            UIManager.Instance.ShowStartText("Press Space or Left Click to start");
    }

    public void SetGameplayActive(bool active)
    {
        if (player != null)
            player.enabled = active;

        // If you have jetpack effect scripts
        var jetpack = FindObjectOfType<JetpackRingController>();
        if (jetpack != null)
            jetpack.enabled = active;

        Debug.Log($"[GameManager] Gameplay {(active ? "enabled" : "disabled")}.");
    }

}