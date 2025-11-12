using UnityEngine;

public class JetpackRingController : MonoBehaviour
{
    public ParticleSystem ringPulseEffect;
    public AudioSource ringPulseSound;

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            ringPulseEffect.Play();
            ringPulseSound.Play();
        }
    }
}
