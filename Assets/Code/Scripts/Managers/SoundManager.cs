using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    [Header("Sources")]
    public AudioSource sfxSource;

    [Header("UI SFX")]
    public AudioClip MoveUp;
    public AudioClip MoveDown;
    public AudioClip Confirm;
    public AudioClip ChangePanel;

    public SettingsManager.AudioSettings AudioSettings;

    void Start()
    {
        Instance = this;
    }

    void Update()
    {
        
    }

    public void PlaySFX(AudioClip clip)
    {   
        sfxSource.PlayOneShot(clip);
    }
}
