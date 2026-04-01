using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public static AudioManager I { get; private set; }

    [Header("Clips")]
    public AudioClip eatClip;
    public AudioClip deathClip;
    public AudioClip clickClip;

    private AudioSource _sfx;

    private void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;

        _sfx = gameObject.AddComponent<AudioSource>();
        _sfx.playOnAwake = false;
        _sfx.loop = false;
    }

    public void PlayEat()
    {
        if (eatClip) _sfx.PlayOneShot(eatClip);
    }

    public void PlayDeath()
    {
        if (deathClip) _sfx.PlayOneShot(deathClip);
    }

    public void PlayClick()
    {
        if (clickClip) _sfx.PlayOneShot(clickClip);
    }
}