using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("UI SFX")]
    public AudioClip MoveUp;
    public AudioClip MoveDown;
    public AudioClip Confirm;
    public AudioClip ChangePanel;

    [Header("Music")]
    public AudioClip backgroundMusic;

    public SettingsManager.AudioSettings AudioSettings;

    void Start()
    {
        Instance = this;

        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }
    }

    void Update()
    {
        
    }

    public void PlaySFX(AudioClip clip)
    {   
        sfxSource.PlayOneShot(clip);
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }
}
