using System.Collections;
using System.Collections.Generic;
using UnityEngine;
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

    void Start()
    {
        // get path
        saveFilePath = Application.persistentDataPath + "/savefile.json";
        LoadBestScore();
    }

        void Update()
    {
        // timer logic
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
    }


    private void UpdateScoreUI(float timeToDisplay, TextMeshProUGUI textElement)
    {
        if (textElement == null) return;

        // format time
        TimeSpan time = TimeSpan.FromSeconds(timeToDisplay);
        textElement.text = time.ToString(@"mm\:ss\.ff");
    }

    // finish game
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

}

// save data
[System.Serializable]
public class SaveData
{
    public float highestTime;
}
