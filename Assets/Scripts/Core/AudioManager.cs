using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AudioManager : MonoBehaviour
{
    public AudioSource sfxSource;

    public void PlayerSFX(AudioClip clip)
    {
        if(clip != null)
        {
            sfxSource.PlayOneShot(clip);
        }
    }

}
