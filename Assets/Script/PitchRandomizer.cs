using Unity.Mathematics;
using UnityEngine;

public class PitchRandomizer : MonoBehaviour
{
    private AudioSource aud;
    void Awake()
    {
        aud = GetComponent<AudioSource>();
        aud.pitch = UnityEngine.Random.Range(0f,2f);
        aud.Play();
    }
}
