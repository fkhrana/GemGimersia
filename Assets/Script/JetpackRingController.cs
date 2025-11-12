using UnityEngine;

public class JetpackRingController : MonoBehaviour
{
    public ParticleSystem ringPulseEffect;

    void Update()
    {
        if (Input.GetButtonDown("Jump"))
        {
            ringPulseEffect.Play();
        }
    }
}
