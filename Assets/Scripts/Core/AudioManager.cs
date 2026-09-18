using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource effectAudioSource;
    public AudioClip attackClip;
    public AudioClip throwShurikenClip;


    public void PlayAttackSound()
    {
        effectAudioSource.PlayOneShot(attackClip);
    }

    public void PlayThrowShurikenSound()
    {
        effectAudioSource.PlayOneShot(throwShurikenClip, 0.5f);
    }

}
