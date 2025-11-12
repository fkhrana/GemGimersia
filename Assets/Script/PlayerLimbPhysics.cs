using UnityEngine;

public class PlayerLimbPhysics : MonoBehaviour
{
    public Rigidbody2D playerRb;
    public Transform head, leftHand, rightHand, legs;

    [Header("Rotation multipliers")]
    public float tiltAmount = 15f;
    public float smooth = 5f;

    [Header("Lag Settings")]
    public float headLag = 0.15f;
    public float handLag = 0.1f;
    public float legLag = 0.2f;

    [Header("Direction Sync (optional)")]
    public PlayerController controller;


    // internal velocity refs for SmoothDampAngle
    float headVel, leftVel, rightVel, legVel;
    float currentHeadRot, currentLeftRot, currentRightRot, currentLegRot;

    void LateUpdate()
    {
        if (playerRb == null) return;

        float vy = playerRb.linearVelocity.y;

        // Check facing direction (assuming you flipped the root scale)
        float direction = controller != null ? controller.direction : Mathf.Sign(transform.localScale.x);


        // Target rotations based on movement
        float headTarget = Mathf.Clamp(-vy * 3f, -tiltAmount, tiltAmount);
        float handTarget = Mathf.Clamp(vy * 2f, -tiltAmount * 0.7f, tiltAmount * 0.7f);
        float legTarget = Mathf.Clamp(vy * -2f, -tiltAmount, tiltAmount);

        // Smooth delayed motion
        currentHeadRot = Mathf.SmoothDampAngle(currentHeadRot, headTarget, ref headVel, headLag);
        currentLeftRot = Mathf.SmoothDampAngle(currentLeftRot, handTarget, ref leftVel, handLag);
        currentRightRot = Mathf.SmoothDampAngle(currentRightRot, handTarget, ref rightVel, handLag);
        currentLegRot = Mathf.SmoothDampAngle(currentLegRot, legTarget, ref legVel, legLag);

        // Apply mirrored rotations based on direction
        if (head) head.localRotation = Quaternion.Euler(0f, 0f, currentHeadRot * direction);
        if (leftHand) leftHand.localRotation = Quaternion.Euler(0f, 0f, currentLeftRot * direction);
        if (rightHand) rightHand.localRotation = Quaternion.Euler(0f, 0f, currentRightRot * direction);
        if (legs) legs.localRotation = Quaternion.Euler(0f, 0f, currentLegRot * direction);

    }
}
