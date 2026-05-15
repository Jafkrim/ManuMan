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
    public float slow = 0.1f;
    public GameObject Head;
    public LayerMask levelLayers;
    public float headContactRadius = 0.2f;
    public float headImpactVelocityThreshold;
    public int headImpactDamage = 10;

    private Rigidbody headRigidbody;
    private bool wasHeadInContact = false;
    private bool wasDead = false;

    public UIManager uiManager;

    public GameObject Player;
    public GameObject loseTrigger;
    public GameObject cehckpointTrigger1;
    public GameObject cehckpointTrigger2;
    public GameObject FinishTrigger;
    [SerializeField] private Vector3 checkpointPosition = new Vector3(4f, 2f, -11f);

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
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
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

        bool canReadSkillInput = uiManager == null || !uiManager.MenuToggledThisFrame;
        if (uiManager != null && uiManager.uiGame.activeSelf == true && canReadSkillInput)
        {
            if (Input.GetKeyDown(KeyCode.E) && !IsUsingSkill)
            {
                IsUsingSkill = true;
                Time.timeScale = slow;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            } 
            else if (Input.GetKeyDown(KeyCode.E) && IsUsingSkill)
            {
                IsUsingSkill = false;
                Time.timeScale = 1f;
                Time.fixedDeltaTime = 0.02f * Time.timeScale;
            } 
        }

        TakeDamage();

        bool isDead = CurrentHealth <= 0;
        if (isDead && !wasDead)
            TeleportPlayerToCheckpoint();
        wasDead = isDead;

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

    public void TeleportPlayerToCheckpoint()
    {
        if (Player == null) return;

        Vector3 targetPosition = checkpointPosition;
        Vector3 delta = targetPosition - Player.transform.position;

        Rigidbody[] bodies = Player.GetComponentsInChildren<Rigidbody>();
        if (bodies.Length > 0)
        {
            foreach (var body in bodies)
            {
                body.position += delta;
                body.velocity = Vector3.zero;
                body.angularVelocity = Vector3.zero;
            }
        }
        else
        {
            Player.transform.position = targetPosition;
        }
    }

    public void SetCheckpoint(Vector3 position)
    {
        checkpointPosition = position + Vector3.up * 2f;
    }
    
}

[System.Serializable]
public class SaveData
{
    public float highestTime;
}
