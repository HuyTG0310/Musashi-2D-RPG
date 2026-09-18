using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource effectAudioSource;
    public AudioClip attackClip;
    public AudioClip throwShurikenClip;
    public AudioClip dashClip;
    public AudioClip enemyHurtClip;
    public AudioClip enemyDeathClip;

    public void PlayAttackSound()
    {
        effectAudioSource.PlayOneShot(attackClip);
    }

    public void PlayThrowShurikenSound()
    {
        effectAudioSource.PlayOneShot(throwShurikenClip, 0.5f);
    }

    public void PlayDashSound()
    {
        effectAudioSource.PlayOneShot(dashClip);
    }


    public void PlayEnemyHurtSound()
    {
        effectAudioSource.PlayOneShot(enemyHurtClip);
    }

    public void PlayEnemyDeathSound()
    {
        effectAudioSource.PlayOneShot(enemyDeathClip);
    }

}
