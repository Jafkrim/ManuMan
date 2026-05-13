using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class GameManager : MonoBehaviour
{

    public int MaxHealth = 100;
    public int CurrentHealth = 70;
    public Slider healthBar;
    public  float MaxSkillDuration = 4f;
    public float CurrentSkillDuration = 0f;
    public bool IsUsingSkill = false;
    public Image skillIcon;
    public float slow = 0.5f;
    void Start()
    {

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

        skillIcon.fillAmount = CurrentSkillDuration / MaxSkillDuration;

        if (Input.GetKeyDown(KeyCode.Space) && !IsUsingSkill)
        {
            IsUsingSkill = true;
            Time.timeScale = slow;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        } 
        else if (Input.GetKeyDown(KeyCode.Space) && IsUsingSkill)
        {
            IsUsingSkill = false;
            Time.timeScale = 1f;
            Time.fixedDeltaTime = 0.02f * Time.timeScale;
        } 
    }
}
