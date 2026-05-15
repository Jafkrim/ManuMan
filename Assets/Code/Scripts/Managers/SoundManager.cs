using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SoundManager : MonoBehaviour
{
    public static SoundManager Instance;

    private UIManager uiManager;

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
        uiManager = FindObjectOfType<UIManager>();
    }

    void Update()
    {
        
    }

    public void PlaySFX(AudioClip clip)
    {   
        if (uiManager != null && uiManager.uiGame != null && uiManager.uiGame.activeSelf)
            return;
        sfxSource.PlayOneShot(clip);
    }
}
