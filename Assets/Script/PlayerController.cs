using UnityEngine;
using UnityEngine.EventSystems;

// PlayerController: input = Space or Left Mouse Button
// Uses Rigidbody2D.linearVelocity and physics in FixedUpdate.
[RequireComponent(typeof(Rigidbody2D))]
public class PlayerController : MonoBehaviour
{
    [Header("Movement")]
    public float moveSpeed = 3f;       // horizontal speed
    public float jumpForce = 6f;       // vertical impulse (set via linearVelocity)

    [Header("Vertical clamp (manual default)")]
    public float maxY = 4.5f;          // default ceiling clamp (overridden if clampToCameraTop = true)
    public float minY = -4.5f;         // floor clamp

    [Header("State")]
    public bool started = false;
    public int direction = -1;         // -1 = left, +1 = right

    [Header("Auto-clamp to Camera Top (optional)")]
    [Tooltip("If assigned, uses this camera; otherwise uses Camera.main.")]
    public Camera followCamera;
    [Tooltip("When true, clamp player's top so it never goes above the camera top.")]
    public bool clampToCameraTop = false;
    [Tooltip("Padding (world units) between player's top and camera top.")]
    public float topPadding = 0.05f;

    [Header("Ground check (optional)")]
    [Tooltip("Layers considered ground for grounded detection. Set to your ground/platform layer for best results.")]
    public LayerMask groundLayerMask = ~0; // default: everything
    [Tooltip("Extra distance for ground check raycast.")]
    public float groundCheckExtra = 0.05f;

    [HideInInspector]
    public Rigidbody2D rb;
    SpriteRenderer sr;
    GameManager gm;

    // ignore input after resume (unscaled time)
    float ignoreInputUntil = 0f;
    // require release of held inputs before accepting new input
    bool blockUntilRelease = false;

    // cached half-height of the player (world units) used for top clamp
    float playerHalfHeight = 0.5f;

    // input flags (captured in Update, executed in FixedUpdate)
    bool jumpRequested = false;
    bool startRequested = false;

    // soft-fall
    float originalGravityScale = 1f;
    Coroutine softFallCoroutine = null;

    void Awake()
    {
        rb = GetComponent<Rigidbody2D>();
        sr = GetComponent<SpriteRenderer>();
        // cache gravity scale early
        if (rb != null) originalGravityScale = rb.gravityScale;
        Debug.Log("[PlayerController] Awake - components cached.");
    }

    void Start()
    {
        // Freeze horizontal movement until started
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // cache GameManager.Instance
        gm = GameManager.Instance;
        if (gm == null)
            Debug.LogWarning("[PlayerController] Start - GameManager.Instance is null.");
        else
            Debug.Log("[PlayerController] Start - GameManager instance cached.");

        // determine player's half-height (prefer SpriteRenderer, fallback to Collider2D)
        if (sr != null)
            playerHalfHeight = sr.bounds.extents.y;
        else
        {
            var col = GetComponent<Collider2D>();
            if (col != null) playerHalfHeight = col.bounds.extents.y;
            else playerHalfHeight = 0.5f;
        }
    }

    void Update()
    {
        // detect held state
        bool isMouseHeld = Input.GetMouseButton(0) || Input.GetMouseButton(1) || Input.GetMouseButton(2);
        bool isSpaceHeld = Input.GetKey(KeyCode.Space);

        // Determine whether to skip input (ignore-window / hold-block)
        bool timeBlock = Time.unscaledTime < ignoreInputUntil;
        bool holdBlock = blockUntilRelease && (isMouseHeld || isSpaceHeld);
        bool skipInput = timeBlock || holdBlock;

        // If blockUntilRelease was true and no buttons are held and timeout passed, clear the block.
        if (blockUntilRelease && !isMouseHeld && !isSpaceHeld && !timeBlock)
        {
            blockUntilRelease = false;
            Debug.Log("[PlayerController] blockUntilRelease cleared - inputs released and timeout passed.");
        }

        // UI checks
        bool uiPointerOver = false;
        bool uiHasSelected = false;
        if (EventSystem.current != null)
        {
            uiPointerOver = EventSystem.current.IsPointerOverGameObject();
            uiHasSelected = EventSystem.current.currentSelectedGameObject != null;
        }
        bool skipBecauseUI = uiPointerOver || uiHasSelected;

        // Check UIManager pause flag (do not accept gameplay input while paused)
        bool uiPaused = (UIManager.Instance != null) ? UIManager.Instance.isPaused : false;

        bool allowInput = !skipInput && !skipBecauseUI && !uiPaused;

        // Capture input as flags (do not perform physics here)
        if (!started)
        {
            if (allowInput && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                startRequested = true;
            }
        }
        else
        {
            if (allowInput && (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0)))
            {
                jumpRequested = true;
            }
        }
    }

    void FixedUpdate()
    {
        if (rb == null) return;

        // Handle start request
        if (startRequested)
        {
            started = true;
            Vector2 lv = rb.linearVelocity;
            lv.x = direction * moveSpeed;
            rb.linearVelocity = lv;
            startRequested = false;
            Debug.Log($"[PlayerController] Game started by input (FixedUpdate). Direction={direction}, linearVelocity.x set to {rb.linearVelocity.x}.");
            if (UIManager.Instance != null) UIManager.Instance.HideStartText();
        }

        // Horizontal motion: enforce constant horizontal speed while started
        if (started)
        {
            Vector2 lv = rb.linearVelocity;
            lv.x = direction * moveSpeed;
            rb.linearVelocity = lv;
        }

        // Handle jump request in physics step (use linearVelocity)
        if (jumpRequested)
        {
            Vector2 lv = rb.linearVelocity;
            lv.y = jumpForce; // directly set vertical linear velocity for jump
            rb.linearVelocity = lv;
            Debug.Log("[PlayerController] Jump applied in FixedUpdate via linearVelocity.");
            jumpRequested = false;
        }

        // Clamp vertical position in physics-friendly way
        Vector2 rbPos = rb.position;
        float effectiveMaxY = maxY;

        if (clampToCameraTop)
        {
            Camera cam = followCamera != null ? followCamera : Camera.main;
            if (cam != null)
            {
                float halfCamHeight;
                if (cam.orthographic)
                    halfCamHeight = cam.orthographicSize;
                else
                {
                    float distance = Mathf.Abs(cam.transform.position.z - transform.position.z);
                    halfCamHeight = Mathf.Tan(cam.fieldOfView * Mathf.Deg2Rad * 0.5f) * distance;
                }

                float camTop = cam.transform.position.y + halfCamHeight;
                float allowedMax = camTop - playerHalfHeight - topPadding;
                effectiveMaxY = Mathf.Max(minY, allowedMax);
            }
        }

        // If player is above effectiveMaxY (overlapping top), correct using physics API and remove upward momentum
        float playerTop = rbPos.y + playerHalfHeight;
        if (playerTop > effectiveMaxY)
        {
            float correctedY = effectiveMaxY - playerHalfHeight;
            // move player down to corrected position via rb.position + MovePosition
            rb.position = new Vector2(rbPos.x, correctedY);
            rb.MovePosition(rb.position);
            // remove upward momentum so it won't stick or bounce
            Vector2 newLv = rb.linearVelocity;
            if (newLv.y > 0f) newLv.y = 0f;
            // tiny downward bias to separate from boundary
            newLv.y = Mathf.Min(newLv.y, -0.01f);
            rb.linearVelocity = newLv;
        }

        // Floor clamp in physics-friendly way
        if (rb.position.y - playerHalfHeight < minY)
        {
            float correctedY = minY + playerHalfHeight;
            rb.position = new Vector2(rb.position.x, correctedY);
            rb.MovePosition(rb.position);
            Vector2 newLv = rb.linearVelocity;
            if (newLv.y < 0f) newLv.y = 0f;
            rb.linearVelocity = newLv;
        }
    }

    public void SetDirection(int newDir)
    {
        direction = Mathf.Clamp(newDir, -1, 1);
        if (sr != null) sr.flipX = direction > 0 ? true : false;
        if (started)
        {
            Vector2 lv = rb.linearVelocity;
            lv.x = direction * moveSpeed;
            rb.linearVelocity = lv;
            Debug.Log($"[PlayerController] SetDirection called. New direction={direction}, linearVelocity.x={rb.linearVelocity.x}");
        }
        else
        {
            Debug.Log($"[PlayerController] SetDirection called. New direction={direction}, player not started yet.");
        }
    }

    public void rbWakeUp()
    {
        if (rb != null)
        {
            rb.WakeUp();
            Debug.Log("[PlayerController] rbWakeUp called - Rigidbody woke up.");
        }
    }

    void OnCollisionEnter2D(Collision2D collision)
    {
        if (collision.collider.CompareTag("Obstacle") ||
            collision.collider.CompareTag("Ground") ||
            collision.collider.CompareTag("Ceiling"))
        {
            Debug.Log($"[PlayerController] OnCollisionEnter2D with '{collision.collider.tag}'. Triggering OnPlayerDied.");
            if (gm == null) gm = GameManager.Instance;
            if (gm != null) gm.OnPlayerDied();
            else Debug.LogError("[PlayerController] OnCollisionEnter2D - GameManager.Instance is null.");
        }
    }

    void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Key"))
            Debug.Log("[PlayerController] OnTriggerEnter2D detected Key trigger. (Key handling is in KeyItem)");
    }

    // public API: ignore input for a short unscaled time window and require release of held inputs
    public void IgnoreInputForSeconds(float seconds)
    {
        ignoreInputUntil = Time.unscaledTime + Mathf.Max(0f, seconds);
        blockUntilRelease = true;
        Debug.Log($"[PlayerController] Ignoring input until unscaled time {ignoreInputUntil:F2} (for {seconds:F2}s). blockUntilRelease={blockUntilRelease}");
    }

    // Cancel any captured pending inputs (clear queued clicks made during pause)
    public void ClearPendingInput()
    {
        jumpRequested = false;
        startRequested = false;
        Debug.Log("[PlayerController] ClearPendingInput called - pending input flags cleared.");
    }

    // Cancel upward momentum so player will fall
    public void ForceDropVertical()
    {
        if (rb == null) return;
        Vector2 lv = rb.linearVelocity;
        if (lv.y > 0f) lv.y = 0f;
        rb.linearVelocity = lv;
        Debug.Log("[PlayerController] ForceDropVertical called - upward velocity cleared.");
    }

    // If airborne (no ground detected below within playerHalfHeight + extra),
    // give a small downward nudge to ensure falling begins so soft-fall is visible.
    public void NudgeDownIfAirborne(float nudge = -0.15f)
    {
        if (rb == null) return;
        if (!IsGrounded())
        {
            Vector2 lv = rb.linearVelocity;
            if (lv.y >= 0f) lv.y = nudge;
            rb.linearVelocity = lv;
            Debug.Log($"[PlayerController] NudgeDownIfAirborne applied (nudge={nudge}).");
        }
        else
        {
            Debug.Log("[PlayerController] NudgeDownIfAirborne skipped because player is grounded.");
        }
    }

    // Ground check using short raycast downward
    public bool IsGrounded()
    {
        // raycast from player position downward for playerHalfHeight + groundCheckExtra
        RaycastHit2D hit = Physics2D.Raycast(transform.position, Vector2.down, playerHalfHeight + groundCheckExtra, groundLayerMask);
        return hit.collider != null;
    }

    // Soft fall: temporarily reduce gravityScale so falling is slower for a short real-time duration.
    public void SoftFallForSeconds(float seconds, float gravityMultiplier)
    {
        if (softFallCoroutine != null)
            StopCoroutine(softFallCoroutine);
        softFallCoroutine = StartCoroutine(SoftFallRoutine(seconds, gravityMultiplier));
    }

    System.Collections.IEnumerator SoftFallRoutine(float seconds, float gravityMultiplier)
    {
        if (rb == null)
            yield break;

        // ensure physics awake and gravity applied before resume
        rb.WakeUp();

        rb.gravityScale = originalGravityScale * Mathf.Clamp(gravityMultiplier, 0.01f, 5f);
        Debug.Log($"[PlayerController] SoftFall started: gravityScale set to {rb.gravityScale:F2} for {seconds:F2}s");

        // use realtime wait so this duration ignores timescale changes
        yield return new WaitForSecondsRealtime(Mathf.Max(0f, seconds));

        // restore original gravity scale (if object still exists)
        if (rb != null)
        {
            rb.gravityScale = originalGravityScale;
            Debug.Log($"[PlayerController] SoftFall ended: gravityScale restored to {rb.gravityScale:F2}");
        }
        softFallCoroutine = null;
    }
}