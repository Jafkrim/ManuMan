using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class UIManager : MonoBehaviour
{
    public GameObject EventSystem;

    public GameObject Game;
    public GameObject PauseMenu;
    public GameObject OptionMenu;

    public GameObject ResumeButton;
    public GameObject OptionButton;
    public GameObject QuitButton;

    void Start()
    {
        PauseMenu.SetActive(false);
        Game.SetActive(true);
        OptionMenu.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape) && !OptionMenu.activeSelf)
        {
            if (Game.activeSelf)
            {
                Game.SetActive(false);
                PauseMenu.SetActive(true);
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
                EventSystem.GetComponent<UnityEngine.EventSystems.EventSystem>().SetSelectedGameObject(ResumeButton);
            }
            else
            {
                Game.SetActive(true);
                PauseMenu.SetActive(false);
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible = false;
            }
        }

        if (Input.GetKeyDown(KeyCode.Escape) && OptionMenu.activeSelf)
        {
            OptionMenu.SetActive(false);
            PauseMenu.SetActive(true);
            Game.SetActive(false);
        }
    }

    public void ResumeGame()
    {
        Game.SetActive(true);
        PauseMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    public void OpenOptionMenu()
    {
        OptionMenu.SetActive(true);
        PauseMenu.SetActive(false);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        Game.SetActive(false);
    }

    public void QuitGame()
    {
        Application.Quit();
    }
}
