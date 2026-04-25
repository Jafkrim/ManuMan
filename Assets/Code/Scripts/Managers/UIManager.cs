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

    public GameObject AudioButton;
    public GameObject GraphicsButton;
    public GameObject KeybindsButton;
    public GameObject CreditsButton;

    public GameObject AudioPanel;
    public GameObject GraphicsPanel;
    public GameObject KeybindsPanel;
    public GameObject CreditsPanel;

    // Later delete
    public TextMeshProUGUI text;

    public PlayerInput playerInput;

    private InputAction openMenuAction;
    private InputAction closeMenuAction;
    private InputAction tabSwitchAction;
    private int lastMenuToggleFrame = -1;
    private int lastEscapeHandledFrame = -1;
    private int lastTabSwitchFrame = -1;
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

        ResolveOptionMenuReferences();
        ApplyCurrentTabState();

        if (playerInput != null && playerInput.currentActionMap != null)
        {
            UnityEngine.Debug.Log("Current Action Map: " + playerInput.currentActionMap.name);
        }
        else
        {
            UnityEngine.Debug.LogWarning("UIManager: PlayerInput reference is missing. Assign it in the inspector or keep one active PlayerInput in scene.");
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

    private GameObject FindChildByName(GameObject root, string childName)
    {
        if (root == null) return null;

        Transform[] allChildren = root.GetComponentsInChildren<Transform>(true);
        for (int i = 0; i < allChildren.Length; i++)
        {
            if (allChildren[i].name == childName)
                return allChildren[i].gameObject;
        }

        return null;
    }

    private void ResolveOptionMenuReferences()
    {
        if (uiOption == null) return;

        if (AudioPanel == null)
            AudioPanel = FindChildByName(uiOption, "AudioPanel");

        if (GraphicsPanel == null)
            GraphicsPanel = FindChildByName(uiOption, "GraphicsPanel");

        if (KeybindsPanel == null)
        {
            KeybindsPanel = FindChildByName(uiOption, "KeybindsPanel");
            if (KeybindsPanel == null)
                KeybindsPanel = FindChildByName(uiOption, "KeybindPanel");
        }

        if (CreditsPanel == null)
            CreditsPanel = FindChildByName(uiOption, "CreditsPanel");

        if (AudioButton == null)
            AudioButton = FindChildByName(uiOption, "AudioButton");

        if (GraphicsButton == null)
            GraphicsButton = FindChildByName(uiOption, "GraphicsButton");

        if (KeybindsButton == null)
        {
            KeybindsButton = FindChildByName(uiOption, "KeybindsButton");
            if (KeybindsButton == null)
                KeybindsButton = FindChildByName(uiOption, "KeybindButton");
        }

        if (CreditsButton == null)
            CreditsButton = FindChildByName(uiOption, "CreditsButton");
    }

    private GameObject GetTabButton(int tabIndex)
    {
        return tabIndex switch
        {
            0 => AudioButton,
            1 => GraphicsButton,
            2 => KeybindsButton,
            3 => CreditsButton,
            _ => null
        };
    }

    private GameObject GetCurrentTabPanel()
    {
        return currentTab switch
        {
            0 => AudioPanel,
            1 => GraphicsPanel,
            2 => KeybindsPanel,
            3 => CreditsPanel,
            _ => null
        };
    }

    private void ApplyCurrentTabState()
    {
        ResolveOptionMenuReferences();

        if (AudioPanel != null)
            AudioPanel.SetActive(currentTab == 0);

        if (GraphicsPanel != null)
            GraphicsPanel.SetActive(currentTab == 1);

        if (KeybindsPanel != null)
            KeybindsPanel.SetActive(currentTab == 2);

        if (CreditsPanel != null)
            CreditsPanel.SetActive(currentTab == 3);
    }

    private bool IsInCurrentTabPanel(GameObject obj)
    {
        GameObject panel = GetCurrentTabPanel();
        if (obj == null || panel == null) return false;

        return obj.transform.IsChildOf(panel.transform);
    }

    private void HandleTabSwitchInput(int direction)
    {
        if (uiOption == null) return;
        if (!uiOption.activeSelf) return;
        if (lastTabSwitchFrame == Time.frameCount) return;
        lastTabSwitchFrame = Time.frameCount;

        if (direction < 0)
            PreviousTab();
        else
            NextTab();
    }

    private List<Selectable> GetSortedSelectablesInPanel(GameObject panel)
    {
        var result = new List<Selectable>();
        if (panel == null) return result;

        Selectable[] selectables = panel.GetComponentsInChildren<Selectable>(true);
        for (int i = 0; i < selectables.Length; i++)
        {
            Selectable selectable = selectables[i];
            if (selectable == null) continue;
            if (!selectable.IsInteractable()) continue;
            if (!selectable.gameObject.activeInHierarchy) continue;
            result.Add(selectable);
        }

        result.Sort((a, b) =>
        {
            float ay = a.transform.position.y;
            float by = b.transform.position.y;

            if (Mathf.Abs(ay - by) > 0.001f)
                return by.CompareTo(ay);

            return a.transform.position.x.CompareTo(b.transform.position.x);
        });

        return result;
    }

    private void MoveSelectionInCurrentPanel(int direction)
    {
        if (EventSystem.current == null) return;

        GameObject panel = GetCurrentTabPanel();
        List<Selectable> selectables = GetSortedSelectablesInPanel(panel);
        if (selectables.Count == 0) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        int currentIndex = -1;
        for (int i = 0; i < selectables.Count; i++)
        {
            if (selectables[i] != null && selectables[i].gameObject == selected)
            {
                currentIndex = i;
                break;
            }
        }

        int nextIndex;
        if (currentIndex < 0)
        {
            nextIndex = direction > 0 ? 0 : selectables.Count - 1;
        }
        else
        {
            nextIndex = Mathf.Clamp(currentIndex + direction, 0, selectables.Count - 1);
        }

        EventSystem.current.SetSelectedGameObject(selectables[nextIndex].gameObject);
    }

    private void HandleOptionNavigateInput(int direction)
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null)
        {
            MoveSelectionInCurrentPanel(direction);
            return;
        }

        if (!IsInCurrentTabPanel(selected))
        {
            MoveSelectionInCurrentPanel(direction);
            return;
        }

        if (selected.TryGetComponent<Selectable>(out var selectable))
        {
            Selectable target = direction < 0 ? selectable.FindSelectableOnUp() : selectable.FindSelectableOnDown();
            if (target == null)
                MoveSelectionInCurrentPanel(direction);
        }
    }

    private void AdjustCurrentSelection(int direction)
    {
        if (EventSystem.current == null) return;

        GameObject selected = EventSystem.current.currentSelectedGameObject;
        if (selected == null || !IsInCurrentTabPanel(selected)) return;

        if (selected.TryGetComponent<Slider>(out var slider))
        {
            float range = Mathf.Abs(slider.maxValue - slider.minValue);
            float step = slider.wholeNumbers ? 1f : Mathf.Max(range * 0.05f, 0.01f);
            slider.value = Mathf.Clamp(slider.value + (direction * step), slider.minValue, slider.maxValue);
            return;
        }

        if (selected.TryGetComponent<Scrollbar>(out var scrollbar))
        {
            scrollbar.value = Mathf.Clamp01(scrollbar.value + (direction * 0.05f));
            return;
        }

        if (selected.TryGetComponent<Toggle>(out var toggle))
        {
            toggle.isOn = direction > 0;
            return;
        }

        if (selected.TryGetComponent<TMP_Dropdown>(out var tmpDropdown))
        {
            int count = tmpDropdown.options != null ? tmpDropdown.options.Count : 0;
            if (count <= 0) return;

            int newValue = Mathf.Clamp(tmpDropdown.value + direction, 0, count - 1);
            if (newValue != tmpDropdown.value)
            {
                tmpDropdown.value = newValue;
                tmpDropdown.RefreshShownValue();
            }
            return;
        }

        if (selected.TryGetComponent<Dropdown>(out var dropdown))
        {
            int count = dropdown.options != null ? dropdown.options.Count : 0;
            if (count <= 0) return;

            int newValue = Mathf.Clamp(dropdown.value + direction, 0, count - 1);
            if (newValue != dropdown.value)
            {
                dropdown.value = newValue;
                dropdown.RefreshShownValue();
            }
            return;
        }

        if (selected.TryGetComponent<Selectable>(out var currentSelectable))
        {
            Selectable horizontal = direction < 0 ? currentSelectable.FindSelectableOnLeft() : currentSelectable.FindSelectableOnRight();
            if (horizontal != null)
                EventSystem.current.SetSelectedGameObject(horizontal.gameObject);
        }
    }

    private void HandleOptionMenuKeyboard()
    {
        if (uiOption == null) return;
        if (!uiOption.activeSelf || Keyboard.current == null) return;

        if (Keyboard.current.qKey.wasPressedThisFrame)
            HandleTabSwitchInput(-1);
        else if (Keyboard.current.eKey.wasPressedThisFrame)
            HandleTabSwitchInput(1);

        if (Keyboard.current.wKey.wasPressedThisFrame)
            HandleOptionNavigateInput(-1);
        else if (Keyboard.current.sKey.wasPressedThisFrame)
            HandleOptionNavigateInput(1);

        if (Keyboard.current.aKey.wasPressedThisFrame)
            AdjustCurrentSelection(-1);
        else if (Keyboard.current.dKey.wasPressedThisFrame)
            AdjustCurrentSelection(1);
    }

    private void SubscribeInputActions()
    {
        if (playerInput == null || playerInput.actions == null) return;

        openMenuAction = playerInput.actions.FindAction("OpenMenu", false);
        closeMenuAction = playerInput.actions.FindAction("CloseMenu", false);
        tabSwitchAction = playerInput.actions.FindAction("TabSwitch", false);

        if (openMenuAction != null)
            openMenuAction.performed += OnOpenMenu;

        if (closeMenuAction != null)
            closeMenuAction.performed += OnCloseMenu;

        if (tabSwitchAction != null)
            tabSwitchAction.performed += OnTabSwitch;
    }

    private void UnsubscribeInputActions()
    {
        if (openMenuAction != null)
            openMenuAction.performed -= OnOpenMenu;

        if (closeMenuAction != null)
            closeMenuAction.performed -= OnCloseMenu;

        if (tabSwitchAction != null)
            tabSwitchAction.performed -= OnTabSwitch;

        openMenuAction = null;
        closeMenuAction = null;
        tabSwitchAction = null;
    }

    private void TogglePauseMenu()
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
            
            if (blurRoutine != null)
                StopCoroutine(blurRoutine);

            blurRoutine = StartCoroutine(FadeBlur(1f, 0.2f));
        }
        else
        {
            uiGame.SetActive(true);
            uiPause.SetActive(false);
            uiOption.SetActive(false);
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
            if (playerInput != null)
                playerInput.SwitchCurrentActionMap("Player");
            
            if (blurRoutine != null)
                StopCoroutine(blurRoutine);

            blurRoutine = StartCoroutine(FadeBlur(0f, 0.2f));
        }
    }

    private void HandleEscapePressed()
    {
        if (lastEscapeHandledFrame == Time.frameCount) return;
        lastEscapeHandledFrame = Time.frameCount;

        if (uiOption.activeSelf)
        {
            uiOption.SetActive(false);
            uiPause.SetActive(true);
            uiGame.SetActive(false);
            EventSystem.current?.SetSelectedGameObject(OptionButton);
            return;
        }

        TogglePauseMenu();
    }

    public void OnOpenMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        TogglePauseMenu();

        UnityEngine.Debug.Log("OpenMenu triggered: " + ctx.phase + " | " + ctx.control);
    }

    public void OnCloseMenu(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;

        HandleEscapePressed();
    }

    void Update()
    {
        if (Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            HandleEscapePressed();
        }

        HandleOptionMenuKeyboard();

        // if (Input.GetKeyDown(KeyCode.Escape) && !uiOption.activeSelf)
        // {
        //     if (uiGame.activeSelf)
        //     {
        //         uiGame.SetActive(false);
        //         uiPause.SetActive(true);
        //         Cursor.lockState = CursorLockMode.None;
        //         Cursor.visible = true;
        //         playerInput.SwitchCurrentActionMap("UI");

        //     }
        //     else
        //     {
        //         uiGame.SetActive(true);
        //         uiPause.SetActive(false);
        //         Cursor.lockState = CursorLockMode.Locked;
        //         Cursor.visible = false;
        //         playerInput.SwitchCurrentActionMap("Player");
        //     }
        // }

        // if (Input.GetKeyDown(KeyCode.Escape) && uiOption.activeSelf)
        // {
        //     uiOption.SetActive(false);
        //     uiPause.SetActive(true);
        //     uiGame.SetActive(false);
        // }


        // Later delete
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

    public void ResumeGame()
    {
        uiGame.SetActive(true);
        uiPause.SetActive(false);
        Cursor.visible = false;

        EventSystem.current.SetSelectedGameObject(ResumeButton);
    }

    public void OpenOptionMenu()
    {
        ResolveOptionMenuReferences();

        currentTab = 0;
        ApplyCurrentTabState();

        uiOption.SetActive(true);
        uiPause.SetActive(false);
        uiGame.SetActive(false);

        GameObject defaultTabButton = GetTabButton(currentTab);
        if (defaultTabButton != null)
            EventSystem.current.SetSelectedGameObject(defaultTabButton);
    }
    
    public int currentTab = 0;
    public void NextTab()
    {
        currentTab = (currentTab + 1) % 4;
        ApplyCurrentTabState();

        GameObject tabButton = GetTabButton(currentTab);
        if (tabButton != null)
            EventSystem.current.SetSelectedGameObject(tabButton);
    }

    public void PreviousTab()
    {
        currentTab = (currentTab - 1 + 4) % 4;
        ApplyCurrentTabState();

        GameObject tabButton = GetTabButton(currentTab);
        if (tabButton != null)
            EventSystem.current.SetSelectedGameObject(tabButton);
    }

    public void OnTabSwitch(InputAction.CallbackContext ctx)
    {
        if (!ctx.performed) return;
        if (!uiOption.activeSelf) return;

        string key = ctx.control.path;

        if (key == "<Keyboard>/q")
            HandleTabSwitchInput(-1);
        else if (key == "<Keyboard>/e")
            HandleTabSwitchInput(1);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
