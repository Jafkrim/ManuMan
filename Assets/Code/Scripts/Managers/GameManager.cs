using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;
using System;
using System.IO; // json
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    [Header("Score System")]
    public float currentTime = 0f;
    public TextMeshProUGUI scoreText;
    
    [Header("Highest Score System")]
    public float highestTime = 0f;
    public TextMeshProUGUI highestScoreText;
    
    public string mainSceneName = "_testUI";
    private bool isGameFinished = false;

    // path for json file
    private string saveFilePath;

    public int MaxHealth = 100;
    public int CurrentHealth = 100;
    public Slider healthBar;
    public  float MaxSkillDuration = 4f;
    public float CurrentSkillDuration = 0f;
    public bool IsUsingSkill = false;
    public Image skillIcon;
    public float slow = 0.3f;
    public GameObject Head;
    public LayerMask levelLayers;
    public float headContactRadius;
    public float headImpactVelocityThreshold;
    public int headImpactDamage = 10;

    private Rigidbody headRigidbody;
    private bool wasHeadInContact = false;

    public UIManager uiManager;

    void Start()
    {
        
        // get path
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        LoadBestScore();

        if (Head != null)
            headRigidbody = Head.GetComponent<Rigidbody>();
    }

    void Update()
    {
        healthBar.value = (float)(1-(float)CurrentHealth / MaxHealth);

        if (IsUsingSkill && CurrentSkillDuration < MaxSkillDuration)
        {
            CurrentSkillDuration += Time.deltaTime * 2f;
        }
        else if (IsUsingSkill && CurrentSkillDuration >= MaxSkillDuration)
        {
            IsUsingSkill = false;
        }
        else if (!IsUsingSkill && CurrentSkillDuration > 0)
        {
            CurrentSkillDuration -= Time.deltaTime * 0.5f;
        }

        if (SceneManager.GetActiveScene().name == mainSceneName && Time.timeScale > 0f && !isGameFinished)
        {
            currentTime += Time.deltaTime;
            UpdateScoreUI(currentTime, scoreText);
        }

        // keybind logic
        if (Keyboard.current != null && Keyboard.current.ctrlKey.isPressed && Keyboard.current.sKey.wasPressedThisFrame)
        {
            FinishGame();
        }
        skillIcon.fillAmount = CurrentSkillDuration / MaxSkillDuration;

        TakeDamage();

    }

    private void UpdateScoreUI(float timeToDisplay, TextMeshProUGUI textElement)
    {
        if (textElement == null) return;

        // format time
        TimeSpan time = TimeSpan.FromSeconds(timeToDisplay);
        textElement.text = time.ToString(@"mm\:ss\.ff");
    }

    public void FinishGame()
    {
        
        if (isGameFinished) return; // prevent double click
        isGameFinished = true;

        // check best time
        if (highestTime == 0f || currentTime < highestTime)
        {
            highestTime = currentTime;
            SaveBestScore();
            UpdateScoreUI(highestTime, highestScoreText);
            Debug.Log("New Best Time Saved: " + highestTime);
        }
        else
        {
            Debug.Log("Finished! But didn't beat the best time.");
        }
    }

    private void SaveBestScore()
    {
        SaveData data = new SaveData();
        data.highestTime = highestTime;

        // save to json
        string json = JsonUtility.ToJson(data);
        File.WriteAllText(saveFilePath, json);
    }

    private void LoadBestScore()
    {
        if (File.Exists(saveFilePath))
        {
            // read json
            string json = File.ReadAllText(saveFilePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            
            highestTime = data.highestTime;
            UpdateScoreUI(highestTime, highestScoreText);
        }
        else
        {
            // no score yet
            if (highestScoreText != null) highestScoreText.text = "--:--.--";
        }
    }


    public void SlowDownTime()
    {
        bool canReadSkillInput = uiManager == null || !uiManager.MenuToggledThisFrame;
        if (uiManager != null && uiManager.uiGame.activeSelf == true && canReadSkillInput)
        {
            if (!IsUsingSkill)
            {
                IsUsingSkill = true;
                Time.timeScale = slow;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            } 
            else if (IsUsingSkill)
            {
                IsUsingSkill = false;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            } 
        }
    }

    public void TakeDamage()
    {
        // if (Head == null || headRigidbody == null) return;

        bool headInContact = Physics.CheckSphere(
            Head.transform.position,
            headContactRadius,
            levelLayers,
            QueryTriggerInteraction.Ignore
        );

        if (headInContact && !wasHeadInContact)
        {
            float speed = headRigidbody.velocity.magnitude;
            if (speed >= headImpactVelocityThreshold)
            {
                CurrentHealth = Mathf.Max(0, CurrentHealth - headImpactDamage);
            }
        }

        wasHeadInContact = headInContact;
    }
    
}

[System.Serializable]
public class SaveData
{
    public float highestTime;
}
