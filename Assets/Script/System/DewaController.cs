using UnityEngine;

public class DewaController : MonoBehaviour
{
    public AudioClip DieClip;
    public AudioClip PauseClip;
    public HealthManager HP;
    public InGameFunction Pause;
    public AudioSource aud;
    private bool istalking = false;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        HP = GameObject.FindGameObjectWithTag("Player").GetComponent<HealthManager>();
        Pause = GameObject.FindGameObjectWithTag("SystemUI").GetComponent<InGameFunction>();
        aud = GetComponent<AudioSource>();
    }

    void OnEnable()
    {
        Pause.onPause += onPause;
        HP.OnDie += onDie;
    }
    void OnDisable()
    {
        Pause.onPause-= onPause;
        HP.OnDie -= onDie;
    }
    public void onPause()
    {
        if (!istalking)
        {
        aud.PlayOneShot(PauseClip,1);
        istalking = true;
        }
    }

    public void onDie()
    {
        aud.PlayOneShot(DieClip,1);
    }

}
