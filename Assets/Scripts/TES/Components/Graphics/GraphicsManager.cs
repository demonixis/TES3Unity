﻿using Demonixis.Toolbox.XR;
using System.Collections;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

namespace TES3Unity.Components
{
    public sealed class GraphicsManager : MonoBehaviour
    {
        private void Awake()
        {
#if UNITY_EDITOR
            // We need to call this component now because SRP settings is very early
            // And we want to be sure it's called before SRP settings.
            var settingsOverride = FindObjectOfType<GameSettingsOverride>();
            settingsOverride?.ApplyEditorSettingsOverride();
#endif
            var config = GameSettings.Get();
            var target = config.SRPQuality.ToString();

            var assetPath = $"Rendering/UniversalRP/PipelineAssets";
            var volumePath = $"Rendering/UniversalRP/Volumes";

            if (GameSettings.IsMobile())
            {
                target = "Mobile";
            }

            // Setup the Quality level
            var qualityIndex = 4; // Ultra

            if (target == "Mobile")
            {
                qualityIndex = 0;
            }
            else if (config.SRPQuality == SRPQuality.Low)
            {
                qualityIndex = 1;
            }
            else if (config.SRPQuality == SRPQuality.Medium)
            {
                qualityIndex = 2;
            }
			else if (config.SRPQuality == SRPQuality.High)
            {
                qualityIndex = 3;
            }

            QualitySettings.SetQualityLevel(qualityIndex);

            if (config.AntiAliasingMode != AntiAliasingMode.MSAA)
            {
                var asset = (UniversalRenderPipelineAsset)GraphicsSettings.renderPipelineAsset;
                asset.msaaSampleCount = 0;
            }

            // Instantiate URP Volume
            var profile = Resources.Load<VolumeProfile>($"{volumePath}/PostProcess-Profile");
            var volumeGo = new GameObject($"{(profile.name.Replace("-Profile", "-Volume"))}");
            volumeGo.transform.localPosition = Vector3.zero;

            var instanceProfile = Instantiate(profile);

            if (XRManager.Enabled || config.PostProcessingQuality == PostProcessingQuality.Medium)
            {
                instanceProfile.UpdateEffect<Bloom>((b) =>
                {
                    b.dirtIntensity.overrideState = true;
                    b.dirtIntensity.Override(0);
                    b.highQualityFiltering.overrideState = true;
                    b.highQualityFiltering.Override(false);
                });

                instanceProfile.DisableEffect<MotionBlur>();
                instanceProfile.DisableEffect<Vignette>();
            }

            if (config.PostProcessingQuality == PostProcessingQuality.Low)
            {
                instanceProfile.DisableEffect<Bloom>();
            }

            var volume = volumeGo.AddComponent<Volume>();
            volume.isGlobal = true;
            volume.sharedProfile = instanceProfile;

            var skyboxMaterial = new Material(Shader.Find("Skybox/Procedural"));
            var lowQualitySkybox = config.SRPQuality != SRPQuality.Low;
#if UNITY_ANDROID
            lowQualitySkybox = true;
#endif

            if (lowQualitySkybox)
            {
                skyboxMaterial.DisableKeyword("_SUNDISK_HIGH_QUALITY");
                skyboxMaterial.EnableKeyword("_SUNDISK_SIMPLE");
            }

            RenderSettings.skybox = skyboxMaterial;
        }

        private IEnumerator Start()
        {
            var config = GameSettings.Get();
            var camera = Camera.main;
            var wait = new WaitForSeconds(1);

            while (camera == null)
            {
                camera = Camera.main;
                yield return wait;
            }

            // 1. Setup Camera
            camera.farClipPlane = config.CameraFarClip;

            var data = camera.GetComponent<UniversalAdditionalCameraData>();
            data.renderPostProcessing = config.PostProcessingQuality != PostProcessingQuality.None;

            switch (config.AntiAliasingMode)
            {
                case AntiAliasingMode.None:
                case AntiAliasingMode.MSAA:
                    data.antialiasing = AntialiasingMode.None;
                    break;
                case AntiAliasingMode.FXAA:
                    data.antialiasing = AntialiasingMode.FastApproximateAntialiasing;
                    break;
                case AntiAliasingMode.SMAA:
                    data.antialiasing = AntialiasingMode.SubpixelMorphologicalAntiAliasing;
                    break;
            }
        }
    }
}
