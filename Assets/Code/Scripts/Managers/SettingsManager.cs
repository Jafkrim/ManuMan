using UnityEngine;
using System.IO;
using TMPro;
using UnityEngine.Audio;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using UnityEngine.InputSystem;

public class SettingsManager : MonoBehaviour
{
    [System.Serializable]
    public class SettingData {
        [Header("Audio")]
        public int masterVolume = 100;          // 0-100
        public int musicVolume = 100;           // 0-100
        public int environmentVolume = 100;     // 0-100
        public int dialogueVolume = 100;        // 0-100
        public int uiVolume = 100;              // 0-100
        public int mechanicsVolume = 100;       // 0-100

        [Header("Graphics")]
        public int qualityPresetIndex = 2;      // 0:Low, 1:High, 2:Custom
        public bool isFullScreen = true;        // true:Full Screen, false:Windowed
        public int resolutionIndex = 0;         // 0:1920px1080p60hz, 1:...
        public int vSyncIndex = 2;              // 0:Unlimited, 1:Full Rate, 2:Half Rate
        public int textureMipmapIndex = 2;      // 0:Full Resolution, 1:Half Resolution, 2: Quarter Resolution
        public int antiAliasingIndex = 2;       // 0:None, 1:FXAA, 2:TAA
        public int renderScaleIndex = 2;        // 0:75%, 1:88%, 2:100%, 3:113%, 4:125%
        public int upscalingFilterIndex = 2;    // 0:Bilinear, 1:Nearest-Neighbor, 2:FSR1.0
        public int shadowQualityIndex = 2;      // 0:Off, 1:Low, 2:Medium, 3:High
        public bool useSoftShadows = false;     // true:Soft Shadows, off: Hard Shadows                         <-- Quality depends on shadowQualityIndex (1:Low, 2:Medium, 3:High)
        public bool useSSAO = false;            // true:SSAO, false: off
        public bool useSSGI = false;            // true:SSGI, false: off
        public bool useSSR = false;             // true:SSR, false: off

        [Header("Keybinds")]
        public string inputBindingOverridesJson;    // IDK how InputSystem works
    }
}