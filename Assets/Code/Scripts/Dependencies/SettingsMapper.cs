using System.Reflection;
using UnityEngine;
using UnityEngine.InputSystem;  // Future
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public static class SettingsMapper
{
    // Called by SettingsManager:
    // when a new scene is loaded
    // when temporary settings are applied in the options menu
    public static void ApplySettings()
    {
        var sm = SettingsManager.Instance;
        var a = sm.currentSettings.audio;
        var v = sm.currentSettings.visual;
        var k = sm.currentSettings.keybind;
        var c = Camera.main;

        ApplyAudio(a);
        ApplyQualityPreset(v);
        ApplyResolution(v.resolution, v.isFullScreen);
        ApplyVSync(v.vSync);
        ApplyTextureMipmap(v.textureMipmap);
        ApplyAntiAliasing(c, v.antiAliasing);
        ApplyRenderScale(v.renderScale);
        ApplyUpscalingFilter(v.upscalingFilter);
        ApplyShadowQuality(v.shadowQuality);
        ApplyScreenSpaceEffects(v.screenSpaceEffect);
        ApplyKeybind(k);
    }

    private static void ApplyAudio(SettingsManager.AudioSettings a)
    {
        if (SettingsManager.Instance.gameMixer == null) return;

        SetMixerVolume("masterVolume", a.masterVolume);
        SetMixerVolume("musicVolume", a.musicVolume);
        SetMixerVolume("environmentVolume", a.environmentVolume);
        SetMixerVolume("dialogueVolume", a.dialogueVolume);
        SetMixerVolume("uiVolume", a.uiVolume);
        SetMixerVolume("mechanismVolume", a.mechanismVolume);
    }

    private static void SetMixerVolume(string audioName, int mv)
    {
        var m = SettingsManager.Instance.gameMixer;
        float dB = (mv <= 0) ? -80f : Mathf.Log10(mv / 100f) * 20f;
        m.SetFloat(audioName, dB);
    }

    private static void ApplyQualityPreset(SettingsManager.VisualSettings v)
    {
        SettingsManager.QualityPreset qp = (v.qualityPreset == SettingsManager.QualityPreset.Custom) 
            ? v.baseQualityPreset 
            : v.qualityPreset;

        switch (qp)
        {
            // Low sample quality
            case SettingsManager.QualityPreset.Potato:
            case SettingsManager.QualityPreset.Low:
            case SettingsManager.QualityPreset.Med:
                QualitySettings.SetQualityLevel(0, true);
                break;

            // High sample quality
            case SettingsManager.QualityPreset.High:
            case SettingsManager.QualityPreset.God:
                QualitySettings.SetQualityLevel(1, true);
                break;
        }
    }

    private static void ApplyResolution(SettingsManager.ResolutionPreset r, bool f)
    {
        Vector2Int res = r switch
        {
            SettingsManager.ResolutionPreset.R1920x1080 => new(1920, 1080),
            SettingsManager.ResolutionPreset.R1600x900 => new(1600, 900),
            SettingsManager.ResolutionPreset.R1280x720 => new(1280, 720),
            SettingsManager.ResolutionPreset.R854x480 => new(854, 480),
            _ => new(1920, 1080)
        };

        Screen.SetResolution(res.x, res.y, f ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    private static void ApplyVSync(SettingsManager.VSyncPreset v)
    {
        QualitySettings.vSyncCount = (int)v;
        
        if (v == SettingsManager.VSyncPreset.Off) { Application.targetFrameRate = 144; }
        else { Application.targetFrameRate = -1; }
    }

    private static void ApplyTextureMipmap(SettingsManager.TextureMipmapPreset m)
    {
        QualitySettings.globalTextureMipmapLimit = (int)m;
    }

    private static void ApplyAntiAliasing(Camera cam, SettingsManager.AntiAliasingPreset aa)
    {
        if (cam == null) return;

        var data = cam.GetUniversalAdditionalCameraData();

        data.antialiasing = aa switch
        {
            SettingsManager.AntiAliasingPreset.Off => AntialiasingMode.None,
            SettingsManager.AntiAliasingPreset.Fxaa => AntialiasingMode.FastApproximateAntialiasing,
            SettingsManager.AntiAliasingPreset.Taa => AntialiasingMode.TemporalAntiAliasing,
            _ => AntialiasingMode.None
        };
    }

    private static void ApplyRenderScale(SettingsManager.RenderScalePreset rs)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urp == null) return;

        urp.renderScale = rs switch
        {
            SettingsManager.RenderScalePreset.Percent075 => 0.75f,
            SettingsManager.RenderScalePreset.Percent087 => 0.87f,
            SettingsManager.RenderScalePreset.Percent100 => 1f,
            SettingsManager.RenderScalePreset.Percent113 => 1.13f,
            SettingsManager.RenderScalePreset.Percent125 => 1.25f,
            _ => 1f
        };
    }

    private static void ApplyUpscalingFilter(SettingsManager.UpscalingFilterPreset uf)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urp == null) return;

        urp.upscalingFilter = uf switch
        {
            SettingsManager.UpscalingFilterPreset.Bilinear => UpscalingFilterSelection.Linear,
            SettingsManager.UpscalingFilterPreset.NearestNeighbor => UpscalingFilterSelection.Point,
            SettingsManager.UpscalingFilterPreset.Fsr1 => UpscalingFilterSelection.FSR,
            _ => UpscalingFilterSelection.Linear
        };
    }

    private static void ApplyShadowQuality(SettingsManager.ShadowQualityPreset sq)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urp == null) return;

        var type = typeof(UniversalRenderPipelineAsset);
        FieldInfo mainLightResField = type.GetField("m_MainLightShadowmapResolution", BindingFlags.Instance | BindingFlags.NonPublic);
        FieldInfo addLightResField = type.GetField("m_AdditionalLightsShadowmapResolution", BindingFlags.Instance | BindingFlags.NonPublic);

        switch (sq)
        {
            case SettingsManager.ShadowQualityPreset.Off:
                urp.shadowDistance = 0;
                mainLightResField?.SetValue(urp, 512);
                addLightResField?.SetValue(urp, 256);
                break;
            case SettingsManager.ShadowQualityPreset.Low:
                urp.shadowDistance = 30;
                mainLightResField?.SetValue(urp, 512);
                addLightResField?.SetValue(urp, 256);
                break;
            case SettingsManager.ShadowQualityPreset.Medium:
                urp.shadowDistance = 45;
                mainLightResField?.SetValue(urp, 1024);
                addLightResField?.SetValue(urp, 512);
                break;
            case SettingsManager.ShadowQualityPreset.High:
                urp.shadowDistance = 60;
                mainLightResField?.SetValue(urp, 2048);
                addLightResField?.SetValue(urp, 1024);
                break;
        }
    }

    private static void ApplyScreenSpaceEffects(SettingsManager.ScreenSpaceEffectPreset sse)
    {
        var urp = GraphicsSettings.currentRenderPipeline as UniversalRenderPipelineAsset;

        if (urp == null) return;

        var fieldInfo = typeof(UniversalRenderPipelineAsset).GetField("m_RendererDataList", BindingFlags.NonPublic | BindingFlags.Instance);
        var rendererDataList = fieldInfo?.GetValue(urp) as ScriptableRendererData[];
        
        if (rendererDataList == null || rendererDataList.Length == 0) return;

        var rendererData = rendererDataList[0];

        foreach (var feature in rendererData.rendererFeatures)
        {
            bool isSsao = feature.name.Contains("ScreenSpaceAmbientOcclusion");
            bool isSsgi = feature.name.Contains("ScreenSpaceGlobalIllumination");
            bool isSsr = feature.name.Contains("ReflectRenderPass");

            if (!isSsao && !isSsgi && !isSsr) continue;

            bool activate = sse switch
            {
                SettingsManager.ScreenSpaceEffectPreset.Off => false,
                SettingsManager.ScreenSpaceEffectPreset.Ssao => isSsao,
                SettingsManager.ScreenSpaceEffectPreset.SsaoSsgi => isSsao || isSsgi,
                SettingsManager.ScreenSpaceEffectPreset.SsgiSsr => isSsgi || isSsr,
                SettingsManager.ScreenSpaceEffectPreset.Full => true,
                _ => false
            };

            feature.SetActive(activate);
        }
    }

    // Future
    private static void ApplyKeybind(SettingsManager.KeybindSettings k)
    {
    }
}