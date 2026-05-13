using System.Collections;
using System.IO;
using TMPro;
using Unity.Entities;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SecondChanceSystem : MonoBehaviour
{
    public GameObject QTE;
    public Transform GameUI;

    [SerializeField] private float qteDuration = 3f;
    [SerializeField] private float successWindow = 1f;

    private GameObject qteInstance;
    private Transform hitIndicator;
    private Vector3 hitIndicatorStartScale;
    private Vector3 HitIndicatorEndScale;
    private string expectedKey;
    private float qteElapsed;
    private Coroutine qteRoutine;
    private bool qteActive;

    void Update()
    {

        if (Keyboard.current.pKey.wasPressedThisFrame)
        {
            QuickTime();
        }

        if (!qteActive)
        {
            return;
        }

        bool aPressed = Keyboard.current.aKey.wasPressedThisFrame;
        bool sPressed = Keyboard.current.sKey.wasPressedThisFrame;
        bool dPressed = Keyboard.current.dKey.wasPressedThisFrame;
        bool fPressed = Keyboard.current.fKey.wasPressedThisFrame;

        if (aPressed || sPressed || dPressed || fPressed)
        {
            if (IsExpectedKeyPressed())
            {
                bool inSuccessWindow = qteElapsed >= (qteDuration - successWindow);
                EndQte(inSuccessWindow);
            }
            else
            {
                EndQte(false);
            }
        }
    }

    public void QuickTime()
    {
        // Debug.Log("QTE Started!");
        if (qteRoutine != null)
        {
            StopCoroutine(qteRoutine);
            qteRoutine = null;
        }

        if (qteInstance != null)
        {
            Destroy(qteInstance);
            qteInstance = null;
        }

        qteActive = false;

        qteInstance = Instantiate(QTE, Vector2.zero, Quaternion.identity, GameUI);
        GameObject HitCircle = qteInstance.transform.GetChild(0).gameObject;
        hitIndicator = qteInstance.transform.GetChild(1);
        TextMeshProUGUI KeyIndicator = qteInstance.transform.GetChild(0).GetChild(0).GetComponent<TMPro.TextMeshProUGUI>();

        qteInstance.transform.localPosition = Vector2.zero;
        hitIndicatorStartScale = hitIndicator.localScale;
        HitIndicatorEndScale = HitCircle.transform.localScale * 0.8f;

        string[] strings = { "A", "S", "D", "F" };
        expectedKey = strings[Random.Range(0, strings.Length)];
        KeyIndicator.text = expectedKey;

        Vector2[] randomPosition = {new Vector2(0, 60), new Vector2(50, 50), new Vector2(-50, 50)};
        HitCircle.transform.localPosition = randomPosition[Random.Range(0, randomPosition.Length)];
        hitIndicator.transform.localPosition = HitCircle.transform.localPosition;

        qteInstance.SetActive(true);
        qteRoutine = StartCoroutine(RunQte());
    }

    private IEnumerator RunQte()
    {
        qteElapsed = 0f;
        qteActive = true;

        while (qteElapsed < qteDuration && qteActive)
        {
            qteElapsed += Time.deltaTime;
            float t = Mathf.Clamp01(qteElapsed / qteDuration);
            hitIndicator.localScale = Vector3.Lerp(hitIndicatorStartScale, HitIndicatorEndScale, t);

            yield return null;
        }

        if (qteActive)
        {
            EndQte(false);
        }
    }

    private void EndQte(bool success)
    {
        qteActive = false;

        if (success)
        {
            Success();
        }
        else
        {
            Fail();
        }

        if (qteRoutine != null)
        {
            StopCoroutine(qteRoutine);
            qteRoutine = null;
        }

        if (qteInstance != null)
        {
            Destroy(qteInstance);
            qteInstance = null;
        }
    }

    private bool IsExpectedKeyPressed()
    {
        switch (expectedKey)
        {
            case "A":
                return Keyboard.current.aKey.wasPressedThisFrame;
            case "S":
                return Keyboard.current.sKey.wasPressedThisFrame;
            case "D":
                return Keyboard.current.dKey.wasPressedThisFrame;
            case "F":
                return Keyboard.current.fKey.wasPressedThisFrame;
            default:
                return false;
        }
    }

    public void Success()
    {
        Debug.Log("Success!");
    }

    public void Fail()
    {
        Debug.Log("Failed!");
    }
}
