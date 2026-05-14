using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class OptionNavigation : MonoBehaviour
{
    private const int TabCount = 4;

    private const int TabAudio = 0;
    private const int TabGraphics = 1;
    private const int TabKeybinds = 2;
    private const int TabCredits = 3;

    public GameObject uiOption;
    public GameObject uiPause;
    public Image uiGameBlur;

    public GameObject AudioButton;
    public GameObject GraphicsButton;
    public GameObject KeybindsButton;
    public GameObject CreditsButton;
    public GameObject ConfirmPanel;

    public PlayerInput playerInput;
    private InputAction closeMenuAction;
    private InputAction TabSwitchAction;
    private InputAction NavigateMenuAction;
    private InputAction AdjustValueAction;
    private InputAction Player_Confirm;

    private Coroutine blurRoutine;
    private Coroutine adjustRepeatRoutine;
    private Color blurColor;
    private int confirmPanelOpenedFrame = -1;
    private int adjustDirection = 0;
    public float adjustHoldDelay = 0.35f;
    public float adjustRepeatRate = 0.08f;

    public SettingsManager settingsManager;
    public SoundManager soundManager;
    public UIManager pauseManager;

    private Dictionary<int, int> tabSelectionMemory = new Dictionary<int, int>();

    public void SaveCurrentIndex()
    {
        tabSelectionMemory[currentTabIndex] = currentSelectionIndex;
    }

    public void LoadCurrentIndex()
    {
        if (tabSelectionMemory.TryGetValue(currentTabIndex, out int index))
            currentSelectionIndex = index;
        else
            currentSelectionIndex = 0;
    }

    private void Awake()
    {
        ResolvePlayerInput();
        ResolveSettingsManager();
    }

    private void OnEnable()
    {
        ResolvePlayerInput();
        ResolveSettingsManager();
        SubscribeInputActions();
    }

    private void OnDisable()
    {
        UnsubscribeInputActions();
        StopAdjustRepeat();
    }

    void Start()
    {
        blurColor = uiGameBlur.color;
    }

    private IEnumerator FadeBlur(float targetAlpha, float duration)
    {
        uiGameBlur.gameObject.SetActive(true);

        float startAlpha = uiGameBlur.color.a;
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float normalized = t / duration;

            blurColor.a = Mathf.Lerp(startAlpha, targetAlpha, normalized);
            uiGameBlur.color = blurColor;

            yield return null;
        }

        blurColor.a = targetAlpha;
        uiGameBlur.color = blurColor;

        if (targetAlpha == 0f)
            uiGameBlur.gameObject.SetActive(false);
    }

    public void BackToPauseMenu()
    {
        uiPause.SetActive(true);
        uiOption.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EventSystem.current.SetSelectedGameObject(uiPause.transform.GetChild(0).GetChild(0).gameObject);
        pauseManager.buttonIndex = 0;
        soundManager.PlaySFX(soundManager.ChangePanel);
        playerInput.SwitchCurrentActionMap("UI");
        UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);

        if (blurRoutine != null)
            StopCoroutine(blurRoutine);

        blurRoutine = StartCoroutine(FadeBlur(0f, 0.2f));
    }

    private void ResolvePlayerInput()
    {
        playerInput = FindObjectOfType<PlayerInput>();
    }

    private void ResolveSettingsManager()
    {
        settingsManager = SettingsManager.Instance;
    }

    public int currentTabIndex = 0;
    public int currentSelectionIndex = 0;

    private void OnCloseMenu(InputAction.CallbackContext ctx)
    {
        ResolveSettingsManager();

        if (settingsManager.HasUnsavedChanges())
        {
            OpenConfirmPanel();
            return;
        }
        if (uiOption.activeSelf)
        {
            BackToPauseMenu();
        }
    }

    private void OnTabSwitch(InputAction.CallbackContext ctx)
    {
        var value = ctx.ReadValue<float>();

        // UnityEngine.Debug.Log("Tab Switch Input Value: " + value);

        GameObject buttonTab = GetTabButton(currentTabIndex);
        GameObject panelButton = GetCurrentOption();
        SendPointerExit(buttonTab);
        SendPointerExit(panelButton);

        if (uiOption.activeSelf)
        {
            SyncTempSettingsFromCurrent();
            SaveCurrentIndex();

            if (value > 0)
            {
                currentTabIndex = Mathf.Min(currentTabIndex + 1, TabCount - 1);
            }
            else if (value < 0)
            {
                currentTabIndex = Mathf.Max(currentTabIndex - 1, 0);
            }

            LoadCurrentIndex();

            soundManager.PlaySFX(soundManager.ChangePanel);
            openPanel();

            UnityEngine.Debug.Log("Current Tab Index: " + currentTabIndex);
        }
    }

    private void OnNavigateMenu(InputAction.CallbackContext ctx)
    {
        var value = ctx.ReadValue<float>();
        if (ConfirmPanel.activeSelf)
        {
            GameObject ApplyButton = GetConfirmApplyButton();
            GameObject CancelButton = GetConfirmCancelButton();
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

            if (currentSelected == ApplyButton)
            {
                if (value > 0)
                    EventSystem.current.SetSelectedGameObject(CancelButton);
                    soundManager.PlaySFX(soundManager.MoveDown);
            }
            else if (currentSelected == CancelButton)
            {
                if (value < 0)
                    EventSystem.current.SetSelectedGameObject(ApplyButton);
                    soundManager.PlaySFX(soundManager.MoveUp);
            }
            else
            {
                EventSystem.current.SetSelectedGameObject(ApplyButton);
            }

            return;
        } 
        else 
        {
            Transform panel = GetCurrentPanel();
            GameObject previousOption = GetCurrentOption();
            SendPointerExit(previousOption);

            if (value > 0)
            {
                currentSelectionIndex = Mathf.Min(currentSelectionIndex + 1, panel.childCount - 1);
                soundManager.PlaySFX(soundManager.MoveDown);
            }
            else if (value < 0)
            {
                currentSelectionIndex = Mathf.Max(currentSelectionIndex - 1, 0);
                soundManager.PlaySFX(soundManager.MoveUp);
            }

            SelectCurrentOption();
        }
    }

    private void OnAdjustValue(InputAction.CallbackContext ctx)
    {
        if (!uiOption.activeSelf) return;
        if (ConfirmPanel.activeSelf) return;

        ResolveSettingsManager();

        if (ctx.canceled)
        {
            StopAdjustRepeat();
            return;
        }

        if (!ctx.started) return;

        var value = ctx.ReadValue<float>();
        int direction = value > 0 ? 1 : value < 0 ? -1 : 0;
        if (direction == 1)
        {
            soundManager.PlaySFX(soundManager.MoveUp);
        }
         else if (direction == -1)
        {
            soundManager.PlaySFX(soundManager.MoveDown);
        }

        ApplyAdjust(direction);

        if (currentTabIndex == TabAudio)
        {
            adjustDirection = direction;
            if (adjustRepeatRoutine == null)
                adjustRepeatRoutine = StartCoroutine(RepeatAdjust());
        }
    }

    private void onPlayer_Confirm(InputAction.CallbackContext ctx)
    {
        ResolveSettingsManager();
        
        if (!ConfirmPanel.activeSelf)
        {
            if (settingsManager.HasUnsavedChanges())
                OpenConfirmPanel();
            return;
        }

        if (confirmPanelOpenedFrame == Time.frameCount) return;

        GameObject ApplyButton = GetConfirmApplyButton();
        GameObject CancelButton = GetConfirmCancelButton();
        if (EventSystem.current.currentSelectedGameObject == ApplyButton)
        {
            ApplySettings();
            var PauseManager = FindObjectOfType<UIManager>();
            ConfirmPanel.SetActive(false);
            PauseManager.ResumeGame();
        }
        else if (EventSystem.current.currentSelectedGameObject == CancelButton)
        {
            ConfirmPanel.SetActive(false);
            SyncTempSettingsFromCurrent();
            SyncCurrentTabUI();
        }
    }

    public void ApplySettings()
    {
        ResolveSettingsManager();
        settingsManager.ConfirmAndApply();
    }

    private void SubscribeInputActions()
    {
        closeMenuAction = playerInput.actions.FindAction("CloseMenu", false);
        TabSwitchAction = playerInput.actions.FindAction("TabSwitch", false);
        NavigateMenuAction = playerInput.actions.FindAction("NavigateMenu", false);
        AdjustValueAction = playerInput.actions.FindAction("AdjustValue", false);
        Player_Confirm = playerInput.actions.FindAction("Player_Confirm", false);

        closeMenuAction.performed += OnCloseMenu;
        TabSwitchAction.performed += OnTabSwitch;
        NavigateMenuAction.performed += OnNavigateMenu;
        AdjustValueAction.started += OnAdjustValue;
        AdjustValueAction.canceled += OnAdjustValue;
        Player_Confirm.performed += onPlayer_Confirm;
    }

    private void UnsubscribeInputActions()
    {
        closeMenuAction.performed -= OnCloseMenu;
        TabSwitchAction.performed -= OnTabSwitch;
        NavigateMenuAction.performed -= OnNavigateMenu;
        AdjustValueAction.started -= OnAdjustValue;
        AdjustValueAction.canceled -= OnAdjustValue;
        Player_Confirm.performed -= onPlayer_Confirm;

        closeMenuAction = null;
        TabSwitchAction = null;
        NavigateMenuAction = null;
        AdjustValueAction = null;
        Player_Confirm = null;
    }

    private void StopAdjustRepeat()
    {
        adjustDirection = 0;
        if (adjustRepeatRoutine != null)
            StopCoroutine(adjustRepeatRoutine);
        adjustRepeatRoutine = null;
    }

    private IEnumerator RepeatAdjust()
    {
        float elapsed = 0f;
        while (elapsed < adjustHoldDelay)
        {
            if (!uiOption.activeSelf || adjustDirection == 0) yield break;
            elapsed += Time.unscaledDeltaTime;
            yield return null;
        }

        while (uiOption.activeSelf && adjustDirection != 0)
        {
            if (adjustDirection == 1)
            {
                soundManager.PlaySFX(soundManager.MoveUp);
            }
             else if (adjustDirection == -1)
            {
                soundManager.PlaySFX(soundManager.MoveDown);
            }
            ApplyAdjust(adjustDirection);
            yield return new WaitForSecondsRealtime(adjustRepeatRate);
        }

        adjustRepeatRoutine = null;
    }

    private void ApplyAdjust(int direction)
    {
        switch (currentTabIndex)
        {
            case TabAudio:
                AdjustAudioSettings(direction);
                break;
            case TabGraphics:
                AdjustGraphicsSettings(direction);
                break;
            case TabKeybinds:
                settingsManager.AdjustKeybind();
                break;
            case TabCredits:
                break;
        }
    }

    private void openPanel()
    {
        GameObject tabButton = GetTabButton(currentTabIndex);
        SendPointerEnter(tabButton);

        for (int i = TabCount; i < TabCount * 2; i++)
        {
            if (i == currentTabIndex + TabCount)
                uiOption.transform.GetChild(0).GetChild(i).gameObject.SetActive(true);
            else
                uiOption.transform.GetChild(0).GetChild(i).gameObject.SetActive(false);
        }

        Transform panel = GetCurrentPanel();
        currentSelectionIndex = Mathf.Clamp(currentSelectionIndex, 0, panel.childCount - 1);

        SelectCurrentOption();
        SyncCurrentTabUI();
    }

    public void SyncCurrentTabUI()
    {
        ResolveSettingsManager();

        if (settingsManager.tempSettings == null)
            settingsManager.InitTempSettings();

        switch (currentTabIndex)
        {
            case TabAudio:
                SyncAudioTabUI();
                break;
            case TabGraphics:
                SyncGraphicsTabUI();
                break;
            case TabKeybinds:
            case TabCredits:
                break;
        }
    }

    public void SyncTempSettingsFromCurrent()
    {
        ResolveSettingsManager();
        settingsManager.InitTempSettings();
    }

    private Transform GetOptionRoot()
    {
        return uiOption.transform.GetChild(0);
    }

    private Transform GetCurrentPanel()
    {
        var root = GetOptionRoot();
        int panelIndex = currentTabIndex + TabCount;
        return root.GetChild(panelIndex);
    }

    private GameObject GetTabButton(int tabIndex)
    {
        var root = GetOptionRoot();
        return root.GetChild(tabIndex).gameObject;
    }

    private GameObject GetCurrentOption()
    {
        var panel = GetCurrentPanel();
        return panel.GetChild(currentSelectionIndex).gameObject;
    }

    private GameObject GetOptionByIndex(int index)
    {
        var panel = GetCurrentPanel();
        return panel.GetChild(index).gameObject;
    }

    private void SelectCurrentOption()
    {
        GameObject option = GetCurrentOption();
        SendPointerEnter(option);

        GameObject control = GetOptionControl(option);
        EventSystem.current.SetSelectedGameObject(control);
    }

    private GameObject GetOptionControl(GameObject option)
    {
        var selectable = option.GetComponentInChildren<Selectable>(true);
        if (selectable != null) return selectable.gameObject;

        if (option.transform.childCount > 1)
            return option.transform.GetChild(1).gameObject;

        return option;
    }

    private void SendPointerEnter(GameObject target)
    {
        ExecuteEvents.Execute<IPointerEnterHandler>(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
    }

    private void SendPointerExit(GameObject target)
    {
        ExecuteEvents.Execute<IPointerExitHandler>(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
    }

    private void OpenConfirmPanel()
    {
        ResolveSettingsManager();
        ConfirmPanel.SetActive(true);
        confirmPanelOpenedFrame = Time.frameCount;
        SetConfirmDefaultSelection();
    }

    private void SetConfirmDefaultSelection()
    {
        GameObject ApplyButton = GetConfirmApplyButton();
        EventSystem.current.SetSelectedGameObject(ApplyButton);
    }

    private GameObject GetConfirmApplyButton()
    {
        return ConfirmPanel.transform.GetChild(1).gameObject;
    }

    private GameObject GetConfirmCancelButton()
    {
        return ConfirmPanel.transform.GetChild(2).gameObject;
    }

    private void SyncAudioTabUI()
    {
        var audio = settingsManager.tempSettings.audio;
        UpdateSlider(GetOptionByIndex(0), audio.masterVolume);
        UpdateSlider(GetOptionByIndex(1), audio.musicVolume);
        UpdateSlider(GetOptionByIndex(2), audio.environmentVolume);
        UpdateSlider(GetOptionByIndex(3), audio.dialogueVolume);
        UpdateSlider(GetOptionByIndex(4), audio.uiVolume);
        UpdateSlider(GetOptionByIndex(5), audio.mechanismVolume);
    }

    // sync graphics settings to UI
    private void SyncGraphicsTabUI()
    {
        var visual = settingsManager.tempSettings.visual;
        UpdateOptionValueAndToggle(0, visual.qualityPreset.ToString(), (int)visual.qualityPreset);
        UpdateOptionValueAndToggle(1, FormatResolution(visual.resolution), (int)visual.resolution);
        UpdateOptionToggle(2, visual.fullScreen, visual.fullScreen ? "On" : "Off");
        UpdateOptionValueAndToggle(3, visual.vSync.ToString(), (int)visual.vSync);
        UpdateOptionValueAndToggle(4, visual.textureMipmap.ToString(), (int)visual.textureMipmap);
        UpdateOptionValueAndToggle(5, visual.antiAliasing.ToString(), (int)visual.antiAliasing);
        UpdateOptionValueAndToggle(6, visual.renderScale.ToString(), (int)visual.renderScale);
        UpdateOptionValueAndToggle(7, visual.upscalingFilter.ToString(), (int)visual.upscalingFilter);
        UpdateOptionValueAndToggle(8, visual.shadowQuality.ToString(), (int)visual.shadowQuality);
        UpdateOptionValueAndToggle(9, visual.screenSpaceEffect.ToString(), (int)visual.screenSpaceEffect);
    }

    private void UpdateOptionValueAndToggle(int optionIndex, string valueText, int toggleIndex)
    {
        GameObject option = GetOptionByIndex(optionIndex);
        UpdateValueLabel(option, valueText);
        SetOptionToggleIndex(option, toggleIndex);
    }

    private void UpdateOptionToggle(int optionIndex, bool value, string valueText)
    {
        GameObject option = GetOptionByIndex(optionIndex);
        UpdateValueLabel(option, valueText);
        UpdateToggleValue(option, value);
    }

    private void AdjustAudioSettings(int direction)
    {
        var audio = settingsManager.tempSettings.audio;
        switch (currentSelectionIndex)
        {
            case 0:
                settingsManager.AdjustSlider(ref audio.masterVolume, direction);
                UpdateSliderValue(audio.masterVolume);
                break;
            case 1:
                settingsManager.AdjustSlider(ref audio.musicVolume, direction);
                UpdateSliderValue(audio.musicVolume);
                break;
            case 2:
                settingsManager.AdjustSlider(ref audio.environmentVolume, direction);
                UpdateSliderValue(audio.environmentVolume);
                break;
            case 3:
                settingsManager.AdjustSlider(ref audio.dialogueVolume, direction);
                UpdateSliderValue(audio.dialogueVolume);
                break;
            case 4:
                settingsManager.AdjustSlider(ref audio.uiVolume, direction);
                UpdateSliderValue(audio.uiVolume);
                break;
            case 5:
                settingsManager.AdjustSlider(ref audio.mechanismVolume, direction);
                UpdateSliderValue(audio.mechanismVolume);
                break;
        }

        UnityEngine.Debug.Log("Adjusted Audio Setting: " + currentSelectionIndex + " New Value: " + audio.masterVolume + ", " + audio.musicVolume + ", " + audio.environmentVolume + ", " + audio.dialogueVolume + ", " + audio.uiVolume + ", " + audio.mechanismVolume);
    }

    private void AdjustGraphicsSettings(int direction)
    {
        var visual = settingsManager.tempSettings.visual;
        switch (currentSelectionIndex)
        {
            case 0:
                settingsManager.AdjustHButtonBar(ref visual.qualityPreset, direction, true);
                SyncGraphicsTabUI();
                break;
            case 1:
                settingsManager.AdjustHButtonBar(ref visual.resolution, direction);
                UpdateValueLabel(FormatResolution(visual.resolution));
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.resolution);
                break;
            case 2:
                settingsManager.AdjustToggleButton(ref visual.fullScreen, true);
                UpdateToggleValue(visual.fullScreen);
                break;
            case 3:
                settingsManager.AdjustHButtonBar(ref visual.vSync, direction);
                UpdateValueLabel(visual.vSync.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.vSync);
                break;
            case 4:
                settingsManager.AdjustHButtonBar(ref visual.textureMipmap, direction);
                UpdateValueLabel(visual.textureMipmap.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.textureMipmap);
                break;
            case 5:
                settingsManager.AdjustHButtonBar(ref visual.antiAliasing, direction);
                UpdateValueLabel(visual.antiAliasing.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.antiAliasing);
                break;
            case 6:
                settingsManager.AdjustHButtonBar(ref visual.renderScale, direction);
                UpdateValueLabel(visual.renderScale.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.renderScale);
                break;
            case 7:
                settingsManager.AdjustHButtonBar(ref visual.upscalingFilter, direction);
                UpdateValueLabel(visual.upscalingFilter.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.upscalingFilter);
                break;
            case 8:
                settingsManager.AdjustHButtonBar(ref visual.shadowQuality, direction);
                UpdateValueLabel(visual.shadowQuality.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.shadowQuality);
                break;
            case 9:
                settingsManager.AdjustHButtonBar(ref visual.screenSpaceEffect, direction);
                UpdateValueLabel(visual.screenSpaceEffect.ToString());
                SetOptionToggleIndex(GetCurrentOption(), (int)visual.screenSpaceEffect);
                break;
        }
        if (currentSelectionIndex >= 4 && currentSelectionIndex <= 9)
        {
            UpdateOptionValueAndToggle(0, visual.qualityPreset.ToString(), (int)visual.qualityPreset);
        }
    }

    private void UpdateSliderValue(int value)
    {
        UpdateSlider(GetCurrentOption(), value);
    }

    private void UpdateSlider(GameObject option, int value)
    {
        var slider = option.GetComponentInChildren<Slider>(true);
        slider.SetValueWithoutNotify(value);
    }

    private void UpdateToggleValue(bool value)
    {
        UpdateToggleValue(GetCurrentOption(), value);
    }

    private void UpdateToggleValue(GameObject option, bool value)
    {
        var toggles = option.GetComponentsInChildren<Toggle>(true);
        if (toggles.Length == 1)
        {
            toggles[0].SetIsOnWithoutNotify(value);
            return;
        }

        SetOptionToggleIndex(option, value ? 1 : 0);
    }

    private void UpdateValueLabel(string valueText)
    {
        UpdateValueLabel(GetCurrentOption(), valueText);
    }

    private void UpdateValueLabel(GameObject option, string valueText)
    {
        var texts = option.GetComponentsInChildren<TMP_Text>(true);
        foreach (var text in texts)
        {
            if (text.gameObject.name.IndexOf("value", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                text.text = valueText;
                return;
            }
        }

        if (texts.Length > 1)
            texts[texts.Length - 1].text = valueText;
    }

    private void SetOptionToggleIndex(GameObject option, int toggleIndex)
    {
        var toggles = option.GetComponentsInChildren<Toggle>(true);
        int clampedIndex = Mathf.Clamp(toggleIndex, 0, toggles.Length - 1);
        for (int i = 0; i < toggles.Length; i++)
        {
            toggles[i].SetIsOnWithoutNotify(i == clampedIndex);
        }
    }

    private string FormatResolution(SettingsManager.ResolutionPreset resolution)
    {
        return resolution switch
        {
            SettingsManager.ResolutionPreset.R1920x1080 => "1920x1080",
            SettingsManager.ResolutionPreset.R1600x900 => "1600x900",
            SettingsManager.ResolutionPreset.R1280x720 => "1280x720",
            SettingsManager.ResolutionPreset.R854x480 => "854x480",
            _ => resolution.ToString()
        };
    }

}