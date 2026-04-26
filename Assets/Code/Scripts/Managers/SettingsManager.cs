using System.IO;
using UnityEngine;
using UnityEngine.Audio;
using UnityEngine.InputSystem;  // Future
using UnityEngine.Rendering.Universal;
using UnityEngine.SceneManagement;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [SerializeField] private SettingsData _currentSettings = new SettingsData();
    [SerializeField] private SettingsData _tempSettings;

    public SettingsData currentSettings => _currentSettings;    // Final settings. Applied to the game
    public SettingsData tempSettings => _tempSettings;          // Temporary settings. Used in options menu, not yet saved or applied

    private string SavePath => Path.Combine(Application.persistentDataPath, "gamesettings.json");

    [Header("References")]
    public AudioMixer gameMixer;
    public UniversalRenderPipelineAsset urpaLowQuality;
    public UniversalRenderPipelineAsset urpaHighQuality;
    public InputActionAsset gameInput;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            LoadSettings();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        SettingsMapper.ApplySettings();
    }

    private void LoadSettings()
    {
        if (File.Exists(SavePath))
        {
            try
            {
                string json = File.ReadAllText(SavePath);
                _currentSettings = JsonUtility.FromJson<SettingsData>(json);
            }
            catch (System.Exception e)
            {
                Debug.LogError($"Failed to load settings: {e.Message}");
                _currentSettings = new SettingsData();  // Default settings
            }
        }
        else
        {
            Debug.Log("No save file found.");
            _currentSettings = new SettingsData();
            SaveSettings(); // Default settings
        }
    }

    private void SaveSettings()
    {
        try
        {
            string json = JsonUtility.ToJson(_currentSettings, true);
            File.WriteAllText(SavePath, json);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to save settings: {e.Message}");
        }
    }

    // Called by UIManager:
    // when entering the options menu, the temporary settings are used to track changes without affecting the current settings until the user confirms
    public void InitTempSettings()
    {
        string json = JsonUtility.ToJson(_currentSettings);
        _tempSettings = JsonUtility.FromJson<SettingsData>(json);
    }

    // Called by UIManager: when deciding whether to show the "Apply Changes" UI element:
    // 1. when user suddenly press confirm changes after entering the options menu without altering any setting (do not show "Apply Changes" UI element);
    // 2. when user press confirm changes after altering some settings (show "Apply Changes" UI element);
    // 3. when user exits the options menu without confirming changes (show "Apply Changes" UI element if there are unsaved changes, otherwise do not show it)
    public bool HasUnsavedChanges()
    {
        if (_tempSettings == null) return false;

        return JsonUtility.ToJson(_currentSettings) != JsonUtility.ToJson(_tempSettings);
    }

    // Called by UIManager:
    // when confirming and applying changes in "Apply Changes" UI element,  overwrite the current settings, save to JSON file, and apply to the game;
    public void ConfirmAndApply()
    {
        _currentSettings = _tempSettings;
        SaveSettings();
        SettingsMapper.ApplySettings();
    }

    private void UpdateQualityPreset(QualityPreset qp)
    {
        _tempSettings.visual.qualityPreset = qp;

        if (qp != QualityPreset.Custom) ApplyPresetLogic(qp);
    }

    private void ApplyPresetLogic(QualityPreset qp)
    {
        var v = _tempSettings.visual;
        v.baseQualityPreset = qp;

        switch (qp)
        {
            case QualityPreset.Potato:
                v.textureMipmap = TextureMipmapPreset.Eighth;
                v.antiAliasing = AntiAliasingPreset.Off;
                v.renderScale = RenderScalePreset.Percent075;
                v.upscalingFilter = UpscalingFilterPreset.NearestNeighbor;
                v.shadowQuality = ShadowQualityPreset.Off;
                v.screenSpaceEffect = ScreenSpaceEffectPreset.Off;
                break;

            case QualityPreset.Low:
                v.textureMipmap = TextureMipmapPreset.Quarter;
                v.antiAliasing = AntiAliasingPreset.Fxaa;
                v.renderScale = RenderScalePreset.Percent087;
                v.upscalingFilter = UpscalingFilterPreset.Fsr1;
                v.shadowQuality = ShadowQualityPreset.Low;
                v.screenSpaceEffect = ScreenSpaceEffectPreset.Ssao;
                break;

            case QualityPreset.Med:
                v.textureMipmap = TextureMipmapPreset.Half;
                v.antiAliasing = AntiAliasingPreset.Taa;
                v.renderScale = RenderScalePreset.Percent100;
                v.upscalingFilter = UpscalingFilterPreset.Fsr1;
                v.shadowQuality = ShadowQualityPreset.Medium;
                v.screenSpaceEffect = ScreenSpaceEffectPreset.SsaoSsgi;
                break;

            case QualityPreset.High:
                v.textureMipmap = TextureMipmapPreset.Full;
                v.antiAliasing = AntiAliasingPreset.Taa;
                v.renderScale = RenderScalePreset.Percent113;
                v.upscalingFilter = UpscalingFilterPreset.Bilinear;
                v.shadowQuality = ShadowQualityPreset.High;
                v.screenSpaceEffect = ScreenSpaceEffectPreset.SsgiSsr;
                break;

            case QualityPreset.God:
                v.textureMipmap = TextureMipmapPreset.Full;
                v.antiAliasing = AntiAliasingPreset.Taa;
                v.renderScale = RenderScalePreset.Percent125;
                v.upscalingFilter = UpscalingFilterPreset.Bilinear;
                v.shadowQuality = ShadowQualityPreset.High;
                v.screenSpaceEffect = ScreenSpaceEffectPreset.Full;
                break;
        }
    }

    // Called by UIManager:
    // when adjusting slider in the options menu, long pressing the button will adjust the slider speed
    public void AdjustSlider(ref int volumeField, int direction, int multiplier = 1)
    {
        if (_tempSettings == null) return;

        int step = 1 * multiplier; 
        volumeField = Mathf.Clamp(volumeField + (direction * step), 0, 100);
    }

    // Called by UIManager: when adjusting horizontal button bar in the options menu, traversing through the options in a specific order
    // where the direction determines whether to go to the next or previous option,
    // also switching quality preset to custom if the adjusted setting is not resolution or vSync and the current quality preset is not custom
    public void AdjustHButtonBar<T>(ref T field, int direction, bool isPreset = false) where T : System.Enum
    {
        if (_tempSettings == null) return;

        T[] values = (T[])System.Enum.GetValues(typeof(T));
        int currentIndex = System.Array.IndexOf(values, field);
        int maxIndex = isPreset ? 1 : values.Length - 1;
        int nextIndex = Mathf.Clamp(currentIndex + direction, 0, maxIndex);

        if (isPreset)
        {
            UpdateQualityPreset((QualityPreset)nextIndex);
        }
        else
        {
            field = values[nextIndex];

            if (typeof(T) != typeof(ResolutionPreset) && typeof(T) != typeof(VSyncPreset)) { _tempSettings.visual.qualityPreset = QualityPreset.Custom; }
        }
    }

    // Called by UIManager: when adjusting toggle button in the options menu,
    // any toggle button will simply flip the boolean value,
    // no need to determine the direction
    public void AdjustToggleButton()
    {
        if (_tempSettings == null) return;

        _tempSettings.visual.isFullScreen = !_tempSettings.visual.isFullScreen;
    }

    // Future
    // Called by UIManager:
    // when adjusting keybind in the options menu
    public void AdjustKeybind()
    {
    }

    public enum QualityPreset
    {
        Potato = 0,
        Low = 1,
        Med = 2,
        High = 3,
        God = 4,
        Custom = 5
    }

    public enum ResolutionPreset
    {
        R1920x1080 = 0,
        R1600x900 = 1,
        R1280x720 = 2,
        R854x480 = 3
    }

    public enum VSyncPreset
    {
        Off = 0,
        Full = 1,
        Half = 2
    }

    public enum TextureMipmapPreset
    {
        Full = 0,
        Half = 1,
        Quarter = 2,
        Eighth = 3
    }

    public enum AntiAliasingPreset
    {
        Off = 0,
        Fxaa = 1,
        Taa = 2
    }

    public enum RenderScalePreset
    {
        Percent075 = 0,
        Percent087 = 1,
        Percent100 = 2,
        Percent113 = 3,
        Percent125 = 4
    }

    public enum UpscalingFilterPreset
    {
        Bilinear = 1,
        NearestNeighbor = 2,
        Fsr1 = 3
    }

    public enum ShadowQualityPreset
    {
        Off = 0,
        Low = 1,
        Medium = 2,
        High = 3
    }

    public enum ScreenSpaceEffectPreset
    {
        Off = 0,
        Ssao = 1,
        SsaoSsgi = 2,
        SsgiSsr = 3,
        Full = 4
    }

    [System.Serializable]
    public class SettingsData
    {
        public AudioSettings audio = new();
        public VisualSettings visual = new();
        public KeybindSettings keybind = new();
    }

    [System.Serializable]
    public class AudioSettings
    {
        [Range(0, 100)] public int masterVolume = 100;
        [Range(0, 100)] public int musicVolume = 100;
        [Range(0, 100)] public int environmentVolume = 100;
        [Range(0, 100)] public int dialogueVolume = 100;
        [Range(0, 100)] public int uiVolume = 100;
        [Range(0, 100)] public int mechanismVolume = 100;
    }

    [System.Serializable]
    public class VisualSettings
    {
        public QualityPreset qualityPreset = QualityPreset.Low;
        public QualityPreset baseQualityPreset = QualityPreset.Low;   // Should only be used when qualityPreset is Custom, to store the last chosen preset
        public ResolutionPreset resolution = ResolutionPreset.R1920x1080;
        public bool isFullScreen = true;
        public VSyncPreset vSync = VSyncPreset.Full;
        public TextureMipmapPreset textureMipmap = TextureMipmapPreset.Quarter;
        public AntiAliasingPreset antiAliasing = AntiAliasingPreset.Fxaa;
        public RenderScalePreset renderScale = RenderScalePreset.Percent087;
        public UpscalingFilterPreset upscalingFilter = UpscalingFilterPreset.Fsr1;
        public ShadowQualityPreset shadowQuality = ShadowQualityPreset.Low;
        public ScreenSpaceEffectPreset screenSpaceEffect = ScreenSpaceEffectPreset.Ssao;
    }

    // Future
    [System.Serializable]
    public class KeybindSettings
    {
    }
}