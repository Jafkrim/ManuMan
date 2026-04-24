using UnityEngine;
using System.IO;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    [Header("Settings Data")]
    public SettingData currentSettings = new SettingData();
    private SettingData tempSettings;

    [Header("References")]
    public AudioMixer gameMixer;
    public UniversalRenderPipelineAsset urpaLowQuality;
    public UniversalRenderPipelineAsset urpaHighQuality;
    public Camera mainCamera;
    public InputActionAsset gameInput;
    private string savePath;

    [System.Serializable]
    public class SettingData {
        [Header("Audio")]
        public int masterVolume = 100;              // 0-100
        public int musicVolume = 100;               // 0-100
        public int environmentVolume = 100;         // 0-100
        public int dialogueVolume = 100;            // 0-100
        public int uiVolume = 100;                  // 0-100
        public int mechanismVolume = 100;           // 0-100

        [Header("Graphics")]
        public int qualityPresetIndex = 2;          // 0:Low, 1:High, 2:Custom
        public bool isFullScreen = true;            // true:Full Screen, false:Windowed
        public int resolutionIndex = 0;             // 0:1920px1080p60hz, 1:...
        public int vSyncIndex = 1;                  // 0:Unlimited, 1:Full Rate, 2:Half Rate
        public int textureMipmapIndex = 0;          // 0:Full Resolution, 1:Half Resolution, 2: Quarter Resolution
        public int antiAliasingIndex = 2;           // 0:None, 1:FXAA, 2:TAA
        public int renderScaleIndex = 4;            // 0:75%, 1:88%, 2:100%, 3:113%, 4:125%
        public int upscalingFilterIndex = 1;        // 0:Bilinear, 1:Nearest-Neighbor, 2:FSR1.0
        public int shadowQualityIndex = 3;          // 0:Off, 1:Low, 2:Medium, 3:High
        public bool useSoftShadows = true;          // true:Soft Shadows, off: Hard Shadows
        public int screenSpaceEffectIndex = 4;      // 0:Off, 1:Low (SSAO), 2:Medium A (SSAO+SSGI), 3:Medium B (SSAO+SSR), 4:High (SSAO+SSGI+SSR)

        [Header("Keybinds")]
        public string inputBindingOverridesJson;    // IDK how InputSystem works
    }
}