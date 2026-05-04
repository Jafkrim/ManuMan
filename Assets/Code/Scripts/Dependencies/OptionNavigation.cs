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
    private Color blurColor;
    private int confirmPanelOpenedFrame = -1;

    public SettingsManager settingsManager;

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
    }

    void Start()
    {
        if (uiGameBlur != null)
        {
            blurColor = uiGameBlur.color;
        }
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
        if (playerInput != null)
        {
            playerInput.SwitchCurrentActionMap("UI");
            UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);
        }
        
        if (blurRoutine != null)
            StopCoroutine(blurRoutine);

        blurRoutine = StartCoroutine(FadeBlur(0f, 0.2f));
    }

    private void ResolvePlayerInput()
    {
        if (playerInput != null) return;

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) return;

        playerInput = FindObjectOfType<PlayerInput>();
    }

    private void ResolveSettingsManager()
    {
        if (settingsManager != null) return;

        settingsManager = SettingsManager.Instance;
        if (settingsManager != null) return;

        settingsManager = FindObjectOfType<SettingsManager>();
    }

    public int currentTabIndex = 0;
    public int currentSelectionIndex = 0;

    private void OnCloseMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !uiOption.activeSelf) return;

        ResolveSettingsManager();

        if (ConfirmPanel != null && settingsManager != null && settingsManager.HasUnsavedChanges())
        {
            TryOpenConfirmPanel();
            return;
        }

        BackToPauseMenu();
    }

    private void OnTabSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !uiOption.activeSelf) return;

        var value = ctx.ReadValue<float>();
        if (Mathf.Approximately(value, 0f)) return;

        // UnityEngine.Debug.Log("Tab Switch Input Value: " + value);
        var optionRoot = GetOptionRoot();
        if (optionRoot == null) return;

        GameObject buttonTab = GetTabButton(currentTabIndex);
        GameObject panelButton = GetCurrentOption();
        SendPointerExit(buttonTab);
        SendPointerExit(panelButton);

        if (uiOption.activeSelf)
        {
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

            openPanel();

            UnityEngine.Debug.Log("Current Tab Index: " + currentTabIndex);
        }
    }

    private void OnNavigateMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !uiOption.activeSelf) return;

        var value = ctx.ReadValue<float>();
        if (Mathf.Approximately(value, 0f)) return;
        if (ConfirmPanel != null && ConfirmPanel.activeSelf)
        {
            GameObject ApplyButton = GetConfirmApplyButton();
            GameObject CancelButton = GetConfirmCancelButton();
            if (ApplyButton == null || CancelButton == null) return;
            GameObject currentSelected = EventSystem.current.currentSelectedGameObject;

            if (currentSelected == ApplyButton)
            {
                if (value > 0)
                    EventSystem.current.SetSelectedGameObject(CancelButton);
            }
            else if (currentSelected == CancelButton)
            {
                if (value < 0)
                    EventSystem.current.SetSelectedGameObject(ApplyButton);
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
            if (panel == null || panel.childCount == 0) return;

            GameObject previousOption = GetCurrentOption();
            SendPointerExit(previousOption);

            if (value > 0)
            {
                currentSelectionIndex = Mathf.Min(currentSelectionIndex + 1, panel.childCount - 1);
            }
            else if (value < 0)
            {
                currentSelectionIndex = Mathf.Max(currentSelectionIndex - 1, 0);
            }

            SelectCurrentOption();
        }
    }

    private void OnAdjustValue(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !uiOption.activeSelf) return;

        ResolveSettingsManager();
        if (settingsManager == null || settingsManager.tempSettings == null) return;

        var value = ctx.ReadValue<float>();
        int direction = value > 0 ? 1 : value < 0 ? -1 : 0;
        if (direction == 0) return;

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

    private void onPlayer_Confirm(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed || !uiOption.activeSelf) return;

        ResolveSettingsManager();
        if (settingsManager == null || settingsManager.tempSettings == null || ConfirmPanel == null) return;
        
        if (!ConfirmPanel.activeSelf)
        {
            if (settingsManager.HasUnsavedChanges())
                TryOpenConfirmPanel();
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
            SyncCurrentTabUI();
        }
    }

    public void ApplySettings()
    {
        ResolveSettingsManager();
        if (settingsManager == null) return;

        settingsManager.ConfirmAndApply();
    }

    private void SubscribeInputActions()
    {
        if (playerInput == null || playerInput.actions == null) return;

        closeMenuAction = playerInput.actions.FindAction("CloseMenu", false);
        TabSwitchAction = playerInput.actions.FindAction("TabSwitch", false);
        NavigateMenuAction = playerInput.actions.FindAction("NavigateMenu", false);
        AdjustValueAction = playerInput.actions.FindAction("AdjustValue", false);
        Player_Confirm = playerInput.actions.FindAction("Player_Confirm", false);

        if (closeMenuAction != null)
            closeMenuAction.performed += OnCloseMenu;
        if (TabSwitchAction != null)
            TabSwitchAction.performed += OnTabSwitch;
        if (NavigateMenuAction != null)
            NavigateMenuAction.performed += OnNavigateMenu;
        if (AdjustValueAction != null)
            AdjustValueAction.performed += OnAdjustValue;
        if (Player_Confirm != null)
            Player_Confirm.performed += onPlayer_Confirm;
    }

    private void UnsubscribeInputActions()
    {
        if (playerInput == null || playerInput.actions == null) return;

        if (closeMenuAction != null)
            closeMenuAction.performed -= OnCloseMenu;
        if (TabSwitchAction != null)
            TabSwitchAction.performed -= OnTabSwitch;
        if (NavigateMenuAction != null)
            NavigateMenuAction.performed -= OnNavigateMenu;
        if (AdjustValueAction != null)
            AdjustValueAction.performed -= OnAdjustValue;
        if (Player_Confirm != null)
            Player_Confirm.performed -= onPlayer_Confirm;

        closeMenuAction = null;
        TabSwitchAction = null;
        NavigateMenuAction = null;
        AdjustValueAction = null;
        Player_Confirm = null;
    }

    private void openPanel()
    {
        GameObject tabButton = GetTabButton(currentTabIndex);
        if (tabButton == null) return;

        SendPointerEnter(tabButton);

        for (int i = TabCount; i < TabCount * 2; i++)
        {
            if (i == currentTabIndex + TabCount)
                uiOption.transform.GetChild(0).GetChild(i).gameObject.SetActive(true);
            else
                uiOption.transform.GetChild(0).GetChild(i).gameObject.SetActive(false);
        }

        Transform panel = GetCurrentPanel();
        if (panel != null)
        {
            currentSelectionIndex = Mathf.Clamp(currentSelectionIndex, 0, panel.childCount - 1);
        }

        SelectCurrentOption();
        SyncCurrentTabUI();
    }

    public void SyncCurrentTabUI()
    {
        ResolveSettingsManager();
        if (settingsManager == null) return;

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

    private Transform GetOptionRoot()
    {
        if (uiOption == null || uiOption.transform.childCount == 0) return null;
        return uiOption.transform.GetChild(0);
    }

    private Transform GetCurrentPanel()
    {
        var root = GetOptionRoot();
        if (root == null) return null;

        int panelIndex = currentTabIndex + TabCount;
        if (panelIndex < 0 || panelIndex >= root.childCount) return null;
        return root.GetChild(panelIndex);
    }

    private GameObject GetTabButton(int tabIndex)
    {
        var root = GetOptionRoot();
        if (root == null) return null;
        if (tabIndex < 0 || tabIndex >= TabCount || tabIndex >= root.childCount) return null;
        return root.GetChild(tabIndex).gameObject;
    }

    private GameObject GetCurrentOption()
    {
        var panel = GetCurrentPanel();
        if (panel == null) return null;
        if (currentSelectionIndex < 0 || currentSelectionIndex >= panel.childCount) return null;
        return panel.GetChild(currentSelectionIndex).gameObject;
    }

    private GameObject GetOptionByIndex(int index)
    {
        var panel = GetCurrentPanel();
        if (panel == null) return null;
        if (index < 0 || index >= panel.childCount) return null;
        return panel.GetChild(index).gameObject;
    }

    private void SelectCurrentOption()
    {
        GameObject option = GetCurrentOption();
        if (option == null) return;

        SendPointerEnter(option);

        GameObject control = GetOptionControl(option);
        if (control != null)
            EventSystem.current.SetSelectedGameObject(control);
    }

    private GameObject GetOptionControl(GameObject option)
    {
        if (option == null) return null;

        var selectable = option.GetComponentInChildren<Selectable>(true);
        if (selectable != null) return selectable.gameObject;

        if (option.transform.childCount > 1)
            return option.transform.GetChild(1).gameObject;

        return option;
    }

    private void SendPointerEnter(GameObject target)
    {
        if (target == null) return;
        ExecuteEvents.Execute<IPointerEnterHandler>(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
    }

    private void SendPointerExit(GameObject target)
    {
        if (target == null) return;
        ExecuteEvents.Execute<IPointerExitHandler>(target, new PointerEventData(EventSystem.current), ExecuteEvents.pointerExitHandler);
    }

    private void TryOpenConfirmPanel()
    {
        ResolveSettingsManager();
        if (settingsManager == null || ConfirmPanel == null) return;
        if (!settingsManager.HasUnsavedChanges() || ConfirmPanel.activeSelf) return;

        ConfirmPanel.SetActive(true);
        confirmPanelOpenedFrame = Time.frameCount;
        SetConfirmDefaultSelection();
    }

    private void SetConfirmDefaultSelection()
    {
        GameObject ApplyButton = GetConfirmApplyButton();
        if (ApplyButton != null)
            EventSystem.current.SetSelectedGameObject(ApplyButton);
    }

    private GameObject GetConfirmApplyButton()
    {
        if (ConfirmPanel == null || ConfirmPanel.transform.childCount <= 1) return null;
        return ConfirmPanel.transform.GetChild(1).gameObject;
    }

    private GameObject GetConfirmCancelButton()
    {
        if (ConfirmPanel == null || ConfirmPanel.transform.childCount <= 2) return null;
        return ConfirmPanel.transform.GetChild(2).gameObject;
    }

    private void SyncAudioTabUI()
    {
        var audio = settingsManager.tempSettings.audio;
        if (audio == null) return;

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
        if (visual == null) return;

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
        if (option == null) return;

        UpdateValueLabel(option, valueText);
        SetOptionToggleIndex(option, toggleIndex);
    }

    private void UpdateOptionToggle(int optionIndex, bool value, string valueText)
    {
        GameObject option = GetOptionByIndex(optionIndex);
        if (option == null) return;

        UpdateValueLabel(option, valueText);
        UpdateToggleValue(option, value);
    }

    private void AdjustAudioSettings(int direction)
    {
        var audio = settingsManager.tempSettings.audio;
        if (audio == null) return;

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
        if (visual == null) return;

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
        if (option == null) return;

        var slider = option.GetComponentInChildren<Slider>(true);
        if (slider == null) return;

        slider.SetValueWithoutNotify(value);
    }

    private void UpdateToggleValue(bool value)
    {
        UpdateToggleValue(GetCurrentOption(), value);
    }

    private void UpdateToggleValue(GameObject option, bool value)
    {
        if (option == null) return;

        var toggles = option.GetComponentsInChildren<Toggle>(true);
        if (toggles == null || toggles.Length == 0) return;

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
        if (option == null) return;

        var texts = option.GetComponentsInChildren<TMP_Text>(true);
        if (texts == null || texts.Length == 0) return;

        foreach (var text in texts)
        {
            if (text == null) continue;

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
        if (option == null) return;

        var toggles = option.GetComponentsInChildren<Toggle>(true);
        if (toggles == null || toggles.Length == 0) return;

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