using UnityEngine;

public class JetpackSquashStretch : MonoBehaviour
{
    [Header("Settings")]
    public Transform target;           // The object to scale (e.g., your Jetpack)
    public float squashScaleY = 0.7f;  // How much to squash vertically
    public float stretchScaleY = 1.3f; // How much to stretch vertically
    public float animationSpeed = 8f;  // Speed of the squash/stretch

    private Vector3 originalScale;
  

    void Start()
    {
        if (target == null)
            target = transform;

        originalScale = target.localScale;
    }

    void Update()
    {
        // Trigger on jump
        if (Input.GetButtonDown("Jump"))
        {
            StartCoroutine(SquashStretch());
        }
    }

    private System.Collections.IEnumerator SquashStretch()
    {


        // Squash down (simulate push-off)
        yield return ScaleTo(new Vector3(originalScale.x, squashScaleY * originalScale.y, originalScale.z));

        // Return to normal
        yield return ScaleTo(originalScale);

    }

    private System.Collections.IEnumerator ScaleTo(Vector3 targetScale)
    {
        while (Vector3.Distance(target.localScale, targetScale) > 0.01f)
        {
            target.localScale = Vector3.Lerp(target.localScale, targetScale, Time.deltaTime * animationSpeed);
            yield return null;
        }
        target.localScale = targetScale;
    }
}
