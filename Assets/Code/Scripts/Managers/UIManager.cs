using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.IO.Compression;
using System.Diagnostics;

public class UIManager : MonoBehaviour
{
    public GameObject uiGame;
    public Image uiGameBlur;
    public GameObject uiPause;
    public GameObject uiOption;

    public GameObject ResumeButton;
    public GameObject OptionButton;
    public GameObject QuitButton;

    // Later delete
    public TextMeshProUGUI text;

    public PlayerInput playerInput;

    private InputAction openMenuAction;
    private InputAction closeMenuAction;
    private InputAction NavigateAction;
    private int lastMenuToggleFrame = -1;
    private Coroutine blurRoutine;
    private Color blurColor;


    private void Awake()
    {
        ResolvePlayerInput();
    }

    private void OnEnable()
    {
        ResolvePlayerInput();
        SubscribeInputActions();
    }

    private void OnDisable()
    {
        UnsubscribeInputActions();
    }

    void Start()
    {
        uiPause.SetActive(false);
        uiGame.SetActive(true);
        uiOption.SetActive(false);

        if (playerInput != null && playerInput.currentActionMap != null)
        {
            UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);
        }
        blurColor = uiGameBlur.color;
        blurColor.a = 0f;
        uiGameBlur.color = blurColor;
        uiGameBlur.gameObject.SetActive(false);
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

    private void ResolvePlayerInput()
    {
        if (playerInput != null) return;

        playerInput = GetComponent<PlayerInput>();
        if (playerInput != null) return;

        playerInput = FindObjectOfType<PlayerInput>();
    }

    private void SubscribeInputActions()
    {
        if (playerInput == null || playerInput.actions == null) return;

        openMenuAction = playerInput.actions.FindAction("OpenMenu", false);
        closeMenuAction = playerInput.actions.FindAction("CloseMenu", false);
        NavigateAction = playerInput.actions.FindAction("NavigateMenu", false);

        if (openMenuAction != null)
            openMenuAction.performed += OnOpenMenu;

        if (closeMenuAction != null)
            closeMenuAction.performed += OnCloseMenu;

        if (NavigateAction != null)
            NavigateAction.performed += OnNavigateMenu;
    }

    private void UnsubscribeInputActions()
    {
        if (openMenuAction != null)
            openMenuAction.performed -= OnOpenMenu;

        if (closeMenuAction != null)
            closeMenuAction.performed -= OnCloseMenu;
        
        if (NavigateAction != null)
            NavigateAction.performed -= OnNavigateMenu;

        openMenuAction = null;
        closeMenuAction = null;
        NavigateAction = null;
    }

    public void TogglePauseMenu()
    {
        if (lastMenuToggleFrame == Time.frameCount) return;
        lastMenuToggleFrame = Time.frameCount;

        if (uiGame.activeSelf)
        {
            uiGame.SetActive(false);
            uiPause.SetActive(true);
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
            if (playerInput != null)
                playerInput.SwitchCurrentActionMap("UI");
                // UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);
            
            if (blurRoutine != null)
                StopCoroutine(blurRoutine);

            blurRoutine = StartCoroutine(FadeBlur(1f, 0.2f));
        }
        else if(uiPause.activeSelf)
        {
            uiGame.SetActive(true);
            uiPause.SetActive(false);
            uiOption.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerInput != null)
                playerInput.SwitchCurrentActionMap("Player");
                UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);
            
            if (blurRoutine != null)
                StopCoroutine(blurRoutine);

            blurRoutine = StartCoroutine(FadeBlur(0f, 0.2f));
        }
        else if(uiOption.activeSelf)
        {
            ResumeGame();
        }
    }

    public void ResumeGame()
    {
        uiGame.SetActive(false);
        uiPause.SetActive(true);
        uiOption.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        EventSystem.current.SetSelectedGameObject(ResumeButton);
        buttonIndex = 0;
        if (playerInput != null)
            playerInput.SwitchCurrentActionMap("UI");
            UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);
        
        if (blurRoutine != null)
            StopCoroutine(blurRoutine);

        blurRoutine = StartCoroutine(FadeBlur(1f, 0.2f));
    }

    public void OpenOptionMenu()
    {
        uiPause.SetActive(false);
        uiOption.SetActive(true);

        var optionManager = FindObjectOfType<OptionManager>();
        optionManager.currentSelectionIndex = 0;
        optionManager.currentTabIndex = 0;

        var settingManager = FindObjectOfType<SettingsManager>();
        settingManager.InitTempSettings();
        optionManager.SyncCurrentTabUI();

        Transform audioButton = uiOption.transform.GetChild(0).GetChild(0);
        Transform OptionPanel = uiOption.transform.GetChild(0);
        GameObject PanelObject = OptionPanel.gameObject;
        GameObject audioObject = audioButton.gameObject;

        EventSystem.current.SetSelectedGameObject(null);
        ExecuteEvents.Execute<IPointerEnterHandler>(audioObject, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);
        for(int i = 4; i < PanelObject.transform.childCount; i++)
        {
            GameObject child = PanelObject.transform.GetChild(i).gameObject;
            child.SetActive(false);
        }
        GameObject AudioPanel = OptionPanel.GetChild(4).gameObject;
        AudioPanel.SetActive(true);
        GameObject firstOption = AudioPanel.transform.GetChild(0).gameObject;
        ExecuteEvents.Execute<IPointerEnterHandler>(firstOption, new PointerEventData(EventSystem.current), ExecuteEvents.pointerEnterHandler);

        GameObject ConfirmPanel = uiOption.transform.GetChild(1).gameObject;
        ConfirmPanel.SetActive(false);
    }

    public void OnOpenMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (uiOption.activeSelf) return;
        buttonIndex = 0;
        EventSystem.current.SetSelectedGameObject(ResumeButton);

        TogglePauseMenu();
    }

    public void OnCloseMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (uiOption.activeSelf) return;
        TogglePauseMenu();
    }
    
    private int buttonIndex = 0;
    public void OnNavigateMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        var value = ctx.ReadValue<float>();
        // UnityEngine.Debug.Log("Navigate Input Value: " + value);
        if (uiPause.activeSelf)
        {
            if (value > 0)
            {
                buttonIndex = (buttonIndex + 1) % 3;
            }
            else if (value < 0)
            {
                buttonIndex = (buttonIndex - 1 + 3) % 3;
            }

            switch (buttonIndex)
            {
                case 0:
                    EventSystem.current.SetSelectedGameObject(ResumeButton);
                    break;
                case 1:
                    EventSystem.current.SetSelectedGameObject(OptionButton);
                    break;
                case 2:
                    EventSystem.current.SetSelectedGameObject(QuitButton);
                    break;
            }
        }
    }

    void Update()
    {

        if (Keyboard.current.anyKey.wasPressedThisFrame)
        {
            foreach (var key in Keyboard.current.allKeys)
            {
                if (key.wasPressedThisFrame)
                {
                    text.text = "Key Pressed: " + key.displayName;
                    UnityEngine.Debug.Log("Key Pressed: " + key.displayName);
                    break;
                }
            }
        }
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
