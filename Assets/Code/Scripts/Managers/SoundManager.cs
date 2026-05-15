using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private UIManager uiManager;

    [Header("Sources")]
    public AudioSource sfxSource;
    public AudioSource musicSource;

    [Header("Music")]
    public AudioClip backgroundMusic;

    [Header("UI SFX")]
    public AudioClip MoveUp;
    public AudioClip MoveDown;
    public AudioClip Confirm;
    public AudioClip ChangePanel;

    public SettingsManager.AudioSettings AudioSettings;

    void Start()
    {
        Instance = this;

        if (backgroundMusic != null)
        {
            PlayMusic(backgroundMusic);
        }

        uiManager = FindObjectOfType<UIManager>();
    }

    void Update()
    {
        
    }

    public void PlayMusic(AudioClip clip)
    {
        musicSource.clip = clip;
        musicSource.loop = true;
        musicSource.Play();
    }

    public void PlaySFX(AudioClip clip)
    {   
        if (uiManager != null && uiManager.uiGame != null && uiManager.uiGame.activeSelf)
            return;
        sfxSource.PlayOneShot(clip);
    }
}
