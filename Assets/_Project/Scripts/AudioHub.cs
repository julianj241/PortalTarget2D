// AudioHub.cs
using UnityEngine;

public class AudioHub : MonoBehaviour
{
    public static AudioHub I;

    [Header("Assign two AudioSources")]
    public AudioSource sfx;
    public AudioSource bgm;

    [Header("SFX Clips")]
    public AudioClip shoot;
    public AudioClip hit;
    public AudioClip miss;
    public AudioClip hazard;
    public AudioClip portal;

    void Awake()
    {
        if (I != null) { Destroy(gameObject); return; }
        I = this;
        DontDestroyOnLoad(gameObject);
    }

    public static void PlayShoot()  => I?.sfx?.PlayOneShot(I.shoot);
    public static void PlayHit()    => I?.sfx?.PlayOneShot(I.hit);
    public static void PlayMiss()   => I?.sfx?.PlayOneShot(I.miss);
    public static void PlayHazard() => I?.sfx?.PlayOneShot(I.hazard);
    public static void PlayPortal() => I?.sfx?.PlayOneShot(I.portal);
}
