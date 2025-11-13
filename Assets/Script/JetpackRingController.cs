using UnityEngine;

public class JetpackRingController : MonoBehaviour
{
    public ParticleSystem ringPulseEffect;
    public AudioSource ringPulseSound;

    void Update()
    {
        // Skip if not active (during cutscene or disabled by GameManager)
        if (!enabled) return;

        if (Input.GetKeyDown(KeyCode.Space) || Input.GetMouseButtonDown(0))
        {
            if (ringPulseEffect != null)
                ringPulseEffect.Play();

            if (ringPulseSound != null && ringPulseSound.enabled)
                ringPulseSound.Play();
        }
    }
}
