using UnityEngine;
using UnityEngine.SceneManagement;
using System.IO;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    public static SettingsManager Instance;

    [Header("Settings Data")]
    public SettingData currentSettings = new SettingData();
    private SettingData tempSettings;

    [Header("References")]
    public AudioMixer gameMixer;
    public UniversalRenderPipelineAsset urpaLowQuality;
    public UniversalRenderPipelineAsset urpaHighQuality;
    public InputActionAsset gameInput;
    private Camera mainCamera;
    private string savePath;

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); SceneManager.sceneLoaded += OnSceneLoaded; }
        else { Destroy(gameObject); return; }

        savePath = Path.Combine(Application.persistentDataPath, "gamesettings.json");
        LoadSettings();
        tempSettings = CloneSettings(currentSettings);
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode) { mainCamera = null; ApplySettings(currentSettings); }
    private void OnDestroy() => SceneManager.sceneLoaded -= OnSceneLoaded;

    public void ChangeIntSetting(ref int settingField, int direction, int maxIndex, bool isQualityPreset = false)
    {
        int nextValue = Mathf.Clamp(settingField + direction, 0, maxIndex);
        if (nextValue == settingField) return;

        settingField = nextValue;
        if (isQualityPreset) ApplyPresetToTemp(nextValue);
        else tempSettings.qualityPreset = 2; // Switch to Custom

        ApplySettings(tempSettings);
    }

    private void ApplyPresetToTemp(int presetIndex)
    {
        if (presetIndex == 0) // Low
        {
            tempSettings.textureMipmap = 2;
            tempSettings.antiAliasing = 0;
            tempSettings.renderScale = 0;
            tempSettings.upscalingFilter = 2;
            tempSettings.shadowQuality = 1;
            tempSettings.screenSpaceEffect = 0;
        }
        else if (presetIndex == 1) // High
        {
            tempSettings.textureMipmap = 0;
            tempSettings.antiAliasing = 2;
            tempSettings.renderScale = 4;
            tempSettings.upscalingFilter = 0;
            tempSettings.shadowQuality = 3;
            tempSettings.screenSpaceEffect = 4;
        }
    }

    public void ApplySettings(SettingData data)
    {
        // Audio
        string[] p = { "masterVolume", "musicVolume", "environmentVolume", "dialogueVolume", "uiVolume", "mechanismVolume" };
        int[] v = { data.masterVolume, data.musicVolume, data.environmentVolume, data.dialogueVolume, data.uiVolume, data.mechanismVolume };
        for (int i = 0; i < p.Length; i++) SetVolume(p[i], v[i]);

        // Fullscreen and Resolution
        if (data.isFullScreen)
        {
            Screen.SetResolution(Screen.currentResolution.width, Screen.currentResolution.height, FullScreenMode.FullScreenWindow);
        }
        else
        {
            // Hardcoded 16:9
            Vector2Int[] resSteps = { 
                new Vector2Int(1920, 1080), 
                new Vector2Int(1600, 900), 
                new Vector2Int(1280, 720), 
                new Vector2Int(854, 480) 
            };

            int idx = Mathf.Clamp(data.resolution, 0, resSteps.Length - 1);
            Vector2Int target = resSteps[idx];

            Screen.SetResolution(target.x, target.y, FullScreenMode.Windowed);
        }

        // URP Type and Texture Mipmap
        GraphicsSettings.renderPipelineAsset = (data.qualityPreset == 0) ? urpaLowQuality : urpaHighQuality;
        QualitySettings.SetQualityLevel(data.qualityPreset, true);
        QualitySettings.globalTextureMipmapLimit = data.textureMipmap;

        // Camera Anti-Aliasing
        if (mainCamera == null) mainCamera = Camera.main;
        if (mainCamera != null) {
            var camData = mainCamera.GetUniversalAdditionalCameraData();
            camData.antialiasing = data.antiAliasing == 2 ? AntialiasingMode.TemporalAntiAliasing : (AntialiasingMode)data.antiAliasing;
        }

        // Render Scale and Upscaling Filter
        var activeAsset = (UniversalRenderPipelineAsset)GraphicsSettings.currentRenderPipeline;
        if (activeAsset != null) {
            float[] scales = { 0.75f, 0.875f, 1.0f, 1.125f, 1.25f };
            activeAsset.renderScale = scales[Mathf.Clamp(data.renderScale, 0, 4)];
            activeAsset.upscalingFilter = (UpscalingFilterSelection)data.upscalingFilter;
        }

        // Shadows and V-Sync
        ApplyShadowDetails(data.shadowQuality);
        QualitySettings.vSyncCount = data.vSync;
        Screen.fullScreen = data.isFullScreen;

        // Screen Space Effects
        ApplyRendererFeatures(data.screenSpaceEffect);
    }

    private void ApplyShadowDetails(int index)
    {
        QualitySettings.shadows = (index == 0) ? UnityEngine.ShadowQuality.Disable : UnityEngine.ShadowQuality.All;
        if (index == 0) return;

        float[] shadowDistances = { 0f, 30f, 45f, 60f };
        QualitySettings.shadowDistance = shadowDistances[Mathf.Clamp(index, 0, 3)];

        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline != null) {
            int[] mainRes = { 0, 512, 1024, 2048 };
            int[] atlasRes = { 0, 256, 512, 1024 };
            var type = typeof(UniversalRenderPipelineAsset);
            var mainF = type.GetField("m_MainLightShadowmapResolution", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            var atlasF = type.GetField("m_AdditionalLightsShadowmapResolution", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
            if (mainF != null) mainF.SetValue(pipeline, mainRes[Mathf.Clamp(index, 0, 3)]);
            if (atlasF != null) atlasF.SetValue(pipeline, atlasRes[Mathf.Clamp(index, 0, 3)]);
        }
    }

    private void ApplyRendererFeatures(int index)
    {
        var pipeline = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;
        if (pipeline == null) return;
        var property = typeof(UniversalRenderPipelineAsset).GetProperty("scriptableRendererData", System.Reflection.BindingFlags.NonPublic | System.Reflection.BindingFlags.Instance);
        var rendererData = property?.GetValue(pipeline) as ScriptableRendererData;
        if (rendererData == null) return;

        foreach (var feature in rendererData.rendererFeatures) {
            if (feature.name.Contains("Screen Space Ambient Occlusion")) feature.SetActive(index == 1 || index == 2 || index == 4);
            if (feature.name.Contains("Screen Space Global Illumination")) feature.SetActive(index == 2 || index == 3 || index == 4);
            if (feature.name.Contains("Reflect Render Pass")) feature.SetActive(index == 3 || index == 4);
        }
    }

    private void SetVolume(string param, int val) {
        if (gameMixer == null) return;
        float dB = val <= 0 ? -80f : Mathf.Log10(val / 100f) * 20f;
        gameMixer.SetFloat(param, dB);
    }

    public void ConfirmAndSave() { currentSettings = CloneSettings(tempSettings); SaveSettings(); ApplySettings(currentSettings); }
    public void CloseWithoutSaving() { tempSettings = CloneSettings(currentSettings); RevertInput(); ApplySettings(currentSettings); }
    
    public void SaveSettings() {
        currentSettings.inputBindingOverridesJson = gameInput.SaveBindingOverridesAsJson();
        File.WriteAllText(savePath, JsonUtility.ToJson(currentSettings, true));
    }

    public void LoadSettings() {
    if (File.Exists(savePath)) {
        currentSettings = JsonUtility.FromJson<SettingData>(File.ReadAllText(savePath));
        if (!string.IsNullOrEmpty(currentSettings.inputBindingOverridesJson))
            gameInput.LoadBindingOverridesFromJson(currentSettings.inputBindingOverridesJson);
    } else {
        SaveSettings(); // Create a JSON file with default settings
    }
    ApplySettings(currentSettings);
}

    private void RevertInput() {
        gameInput.RemoveAllBindingOverrides();
        if (!string.IsNullOrEmpty(currentSettings.inputBindingOverridesJson))
            gameInput.LoadBindingOverridesFromJson(currentSettings.inputBindingOverridesJson);
    }

    private SettingData CloneSettings(SettingData source) => JsonUtility.FromJson<SettingData>(JsonUtility.ToJson(source));
    public void UpdateTempKeybinds() => tempSettings.inputBindingOverridesJson = gameInput.SaveBindingOverridesAsJson();

    // UI Wrappers
    public void UI_Preset(int dir) => ChangeIntSetting(ref tempSettings.qualityPreset, dir, 1, true);
    public void UI_Fullscreen() => tempSettings.isFullScreen = !tempSettings.isFullScreen;
    public void UI_Resolution(int dir) => ChangeIntSetting(ref tempSettings.resolution, dir, 3);
    public void UI_AntiAliasing(int dir) => ChangeIntSetting(ref tempSettings.antiAliasing, dir, 2);
    public void UI_RenderScale(int dir) => ChangeIntSetting(ref tempSettings.renderScale, dir, 4);
    public void UI_Shadows(int dir) => ChangeIntSetting(ref tempSettings.shadowQuality, dir, 3);
    public void UI_TextureMipmap(int dir) => ChangeIntSetting(ref tempSettings.textureMipmap, dir, 2);
    public void UI_Upscaling(int dir) => ChangeIntSetting(ref tempSettings.upscalingFilter, dir, 2);
    public void UI_SSX(int dir) => ChangeIntSetting(ref tempSettings.screenSpaceEffect, dir, 4);

    [System.Serializable]
    public class SettingData {
        [Header("Audio")]
        [Range(0, 100)] public int masterVolume = 100;
        [Range(0, 100)] public int musicVolume = 100;
        [Range(0, 100)] public int environmentVolume = 100;
        [Range(0, 100)] public int dialogueVolume = 100;
        [Range(0, 100)] public int uiVolume = 100;
        [Range(0, 100)] public int mechanismVolume = 100;

        [Header("Graphics")]
        [Range(0, 2), Tooltip("0:Low\n1:High\n2:Custom")] public int qualityPreset = 1;
        public bool isFullScreen = true;
        [Range(0, 3), Tooltip("0:1080p\n1:900p\n2:720p\n3:480p")] public int resolution = 0;
        [Range(0, 2), Tooltip("0:Off\n1:Full rate\n2:Half rate")] public int vSync = 1;
        [Range(0, 3), Tooltip("0:Full resolution\n1:Half resolution\n2:Quarter resolution\n3:Eighth resolution")] public int textureMipmap = 0;
        [Range(0, 2), Tooltip("0:None\n1:FXAA\n2:TAA")] public int antiAliasing = 2;
        [Range(0, 4), Tooltip("0:75%\n1:87.5%\n2:100%\n3:112.5%\n4:125%")] public int renderScale = 4;
        [Range(0, 2), Tooltip("0:Bilinear\n1:Nearest-Neighbor\n2:FSR1.0")] public int upscalingFilter = 0;
        [Range(0, 3), Tooltip("0:Off\n1:Low (30m, 512 main, 256 atlas)\n2:Med (45m, 1024 main, 512 atlas)\n3:High (60m, 2048 main, 1024 atlas)")] public int shadowQuality = 3;
        [Range(0, 4), Tooltip("0:Off\n1:SSAO\n2:SSAO + SSGI\n3:SSGI + SSR\n4:SSAO + SSGI + SSR")] public int screenSpaceEffect = 4;

        [Header("Keybinds")]
        public string inputBindingOverridesJson;
    }
}