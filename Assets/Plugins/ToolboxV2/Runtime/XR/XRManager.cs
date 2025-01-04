#if (UNITY_STANDALONE_WIN || UNITY_ANDROID || UNITY_VISIONOS) && !DISABLE_UNITY_XR
#define UNITY_XR_SUPPORTED
#endif

#if (UNITY_STANDALONE_WIN || UNITY_ANDROID) && OCULUS_XR_ENABLED && UNITY_XR_SUPPORTED
#define OCULUS_SUPPORTED
#endif
#if (UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_ANDROID) && OPENXR_ENABLED && UNITY_XR_SUPPORTED
#define OPENXR_SUPPORTED
#endif

using System.Collections;
using UnityEngine;
using UnityEngine.XR;
using System.Collections.Generic;
using UnityEngine.XR.Management;
using System;
using Demonixis.ToolboxV2.Utils;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Input;

namespace Demonixis.ToolboxV2.XR
{
    #region Enumerations

    public enum XRVendor
    {
        None = 0,
        Meta,
        WindowsMr,
        OpenVR,
        Sony,
        Pico,
        WaveVR,
        Apple,
        AndroidXR,
        Unknown
    }

    public enum XRHeadset
    {
        None = 0,
        OculusRiftCv1,
        OculusRiftS,
        OculusQuest,
        OculusQuest2,
        OculusQuest3,
        OculusQuest3S,
        OculusQuestPro,
        OculusGo,
        HtcVive,
        ValveIndex,
        WindowsMr,
        Psvr,
        Psvr2,
        PicoNeo3,
        PicoNeo4,
        ViveFocus3,
        ViveXRElite,
        AppleVisionPro,
        AndroidXR,
        Unknown
    }

    #endregion

    public static class XRManager
    {
        private static bool? _xrEnabled;
        private static readonly List<InputDevice> InputDevices = new();
        public static bool Enabled => IsXREnabled(false);
        public static XRVendor Vendor { get; private set; }
        public static XRHeadset Headset { get; private set; }

        public static bool OpenXREnabled => IsOpenXREnabled();

        public static bool IsOpenXREnabled()
        {
#if OPENXR_SUPPORTED
            var xrLoader = GetXRLoader();
            if (xrLoader == null) return false;
            return xrLoader is UnityEngine.XR.OpenXR.OpenXRLoader;
#else
            return false;
#endif
        }

        public static bool HandTrackingSupported()
        {
#if UNITY_ANDROID || UNITY_EDITOR || UNITY_STANDALONE_WIN
            return Vendor is XRVendor.Meta or XRVendor.Pico or XRVendor.Apple or XRVendor.AndroidXR;
#elif UNITY_VISIONOS
            return true;
#else
            return false;
#endif
        }

        public static bool HasMotionControllers()
        {
            return Headset != XRHeadset.AppleVisionPro && Headset != XRHeadset.AndroidXR;
        }

        public static XRVendor GetVendor()
        {
#if UNITY_VISIONOS
            return XRVendor.Apple;
#endif
#if UNITY_XR_SUPPORTED
            if (!_xrEnabled.HasValue)
                IsXREnabled();

            return Vendor;
#else
            return XRVendor.None;
#endif
        }

        public static XRLoader GetXRLoader()
        {
#if UNITY_XR_SUPPORTED
            var settings = XRGeneralSettings.Instance;
            if (settings == null) return null;

            var manager = settings.Manager;
            return manager != null ? manager.activeLoader : null;
#else
            return null;
#endif
        }

        public static XRInputSubsystem GetXRInput()
        {
#if UNITY_XR_SUPPORTED
            var xrLoader = GetXRLoader();
            if (xrLoader == null) return null;
            return xrLoader.GetLoadedSubsystem<XRInputSubsystem>();
#else
            return null;
#endif
        }

        public static XRDisplaySubsystem GetDisplay()
        {
#if UNITY_XR_SUPPORTED
            var xrLoader = GetXRLoader();
            if (xrLoader == null) return null;
            return xrLoader.GetLoadedSubsystem<XRDisplaySubsystem>();
#else
            return null;
#endif
        }

        public static bool IsControllerConnected(bool left)
        {
#if UNITY_XR_SUPPORTED
            var input = GetXRInput();
            input.TryGetInputDevices(InputDevices);

            foreach (var device in InputDevices)
            {
                if (!device.isValid) continue;
                if (device.characteristics == InputDeviceCharacteristics.Left && left) return true;
                if (device.characteristics == InputDeviceCharacteristics.Right && !left) return true;
            }
#endif

            return false;
        }

        public static void SetRenderScale(float scale)
        {
#if UNITY_XR_SUPPORTED
            //XRSettings.renderViewportScale = Mathf.Clamp(scale, 0.5f, 1.0f);
#endif
        }

        public static bool IsXREnabled(bool force = false)
        {
#if UNITY_VISIONOS
            Vendor = XRVendor.Apple;
            Headset = XRHeadset.AppleVisionPro;
            return true;
#endif
#if UNITY_XR_SUPPORTED
            if (_xrEnabled.HasValue && !force)
                return _xrEnabled.Value;

            var loader = GetXRLoader();

            _xrEnabled = loader != null;

#if OPENXR_SUPPORTED
            if (loader is UnityEngine.XR.OpenXR.OpenXRLoader)
            {
                Vendor = ParseVendor(UnityEngine.XR.OpenXR.OpenXRRuntime.name);
                Headset = ParseHeadset(UnityEngine.XR.OpenXR.OpenXRRuntime.name);
            }
#endif

#if OCULUS_SUPPORTED
            if (loader is Unity.XR.Oculus.OculusLoader)
            {
                Vendor = XRVendor.Meta;

                switch (Unity.XR.Oculus.Utils.GetSystemHeadsetType())
                {
                    case Unity.XR.Oculus.SystemHeadset.Oculus_Quest:
                    case Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest:
                        Headset = XRHeadset.OculusQuest;
                        break;
                    case Unity.XR.Oculus.SystemHeadset.Oculus_Link_Quest_2:
                    case Unity.XR.Oculus.SystemHeadset.Oculus_Quest_2:
                        Headset = XRHeadset.OculusQuest2;
                        break;
                    case Unity.XR.Oculus.SystemHeadset.Rift_CV1:
                        Headset = XRHeadset.OculusRiftCV1;
                        break;
                    case Unity.XR.Oculus.SystemHeadset.Rift_S:
                        Headset = XRHeadset.OculusRiftS;
                        break;
                    case Unity.XR.Oculus.SystemHeadset.Meta_Quest_3:
                    case Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_3:
                        Headset = XRHeadset.OculusQuest3;
                        break;
                    case Unity.XR.Oculus.SystemHeadset.Meta_Quest_Pro:
                    case Unity.XR.Oculus.SystemHeadset.Meta_Link_Quest_Pro:
                        Headset = XRHeadset.OculusQuestPro;
                        break;
                    default:
                        Headset = XRHeadset.OculusQuest2;
                        break;
                }
            }
#endif

            return _xrEnabled.Value;
#else
            return false;
#endif
        }

        public static void TryInitialize()
        {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS && !UNITY_ANDROID
            var manager = XRGeneralSettings.Instance.Manager;
            if (manager == null) return;
            if (manager.activeLoader != null) return;

            manager.InitializeLoaderSync();
            manager.StartSubsystems();
#endif
        }

        public static void TryShutdown()
        {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS && !UNITY_ANDROID
            var manager = XRGeneralSettings.Instance.Manager;
            if (manager == null) return;
            if (manager.activeLoader == null) return;

            manager.DeinitializeLoader();
            manager.StopSubsystems();
#endif
        }

        private static bool InArray(string word, params string[] terms)
        {
            foreach (var term in terms)
            {
                if (word.Contains(term))
                    return true;
            }

            return false;
        }

        private static bool InArrayAnd(string word, params string[] terms)
        {
            var counter = 0;
            foreach (var term in terms)
            {
                if (word.Contains(term))
                    counter++;
            }

            return counter == terms.Length;
        }

        public static XRVendor ParseVendor(string name)
        {
#if UNITY_XR_SUPPORTED
            if (string.IsNullOrEmpty(name))
                return XRVendor.None;

            var mobile = PlatformUtility.IsMobilePlatform();
            name = name.ToLower();

#if UNITY_EDITOR
            if (name == "meta xr simulator")
                return XRVendor.Meta;
#endif

            if (name.Contains("oculus"))
            {
                return XRVendor.Meta;
            }

            if (name.Contains("android") && name.Contains("xr"))
            {
                return XRVendor.AndroidXR;
            }

            if (InArray(name, "apple", "vision", "polyspatial"))
            {
                return XRVendor.Apple;
            }

            if (InArray(name, "windows", "mixedreality", "holographic"))
            {
                return XRVendor.WindowsMr;
            }

            if (mobile && InArray(name, "xr elite", "focus", "vive"))
            {
                return XRVendor.WaveVR;
            }

            if (!mobile && InArray(name, "valve", "vive", "steam", "openvr"))
            {
                return XRVendor.OpenVR;
            }

            if (InArray(name, "sony", "psvr"))
            {
                return XRVendor.Sony;
            }

            if (InArray(name, "pico"))
            {
                return XRVendor.Pico;
            }
#endif

            return XRVendor.Unknown;
        }

        public static XRHeadset ParseHeadset(string name)
        {
#if UNITY_XR_SUPPORTED
            if (string.IsNullOrEmpty(name))
                return XRHeadset.None;

            name = name.ToLower();

#if UNITY_EDITOR
            if (name == "meta xr simulator")
                return XRHeadset.OculusQuest3;
#endif
            
            if (name.Contains("android") && name.Contains("xr"))
            {
                return XRHeadset.AndroidXR;
            }

            if (name.Contains("oculus") || name.Contains("meta"))
            {
                if (InArray(name, "rift", "cv1"))
                {
                    return XRHeadset.OculusRiftCv1;
                }

                if (InArray(name, "rift", "rift s", "rift-s"))
                {
                    return XRHeadset.OculusRiftS;
                }

                if (InArray(name, "go"))
                {
                    return XRHeadset.OculusGo;
                }

                if (name.Contains("quest"))
                {
                    if (name.Contains("pro"))
                        return XRHeadset.OculusQuestPro;

                    if (name.Contains("2"))
                        return XRHeadset.OculusQuest2;

                    if (name.Contains("3s"))
                        return XRHeadset.OculusQuest3S;

                    if (name.Contains("3"))
                        return XRHeadset.OculusQuest3;

                    return XRHeadset.OculusQuest;
                }

                return XRHeadset.OculusQuest;
            }

            if (InArray(name, "apple", "vision", "polyspatial"))
            {
                return XRHeadset.AppleVisionPro;
            }

            if (InArray(name, "windows", "acer", "samsung", "reverb"))
            {
                return XRHeadset.WindowsMr;
            }

            if (name.Contains("vive"))
            {
                if (name.Contains("focus"))
                    return XRHeadset.ViveFocus3;

                if (InArray(name, "vive", "xr", "elite"))
                    return XRHeadset.ViveXRElite;

                return XRHeadset.HtcVive;
            }

            if (InArray("valve", "index"))
            {
                return XRHeadset.ValveIndex;
            }

            if (InArray("sony", "psvr"))
            {
                if (name.Contains("2"))
                    return XRHeadset.Psvr2;

                return XRHeadset.Psvr;
            }

            if (name.Contains("pico"))
            {
                if (name.Contains("3"))
                    return XRHeadset.PicoNeo3;

                return XRHeadset.PicoNeo4;
            }
#endif
            return XRHeadset.Unknown;
        }

        public static IEnumerator GetXRInfos(Action<XRVendor, XRHeadset> callback)
        {
#if UNITY_XR_SUPPORTED
            var vendor = XRVendor.None;
            var headset = XRHeadset.None;

            var head = new List<InputDevice>();
            var left = new List<InputDevice>();
            var right = new List<InputDevice>();

            do
            {
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.HeadMounted | InputDeviceCharacteristics.TrackedDevice, head);
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Left |
                    InputDeviceCharacteristics.TrackedDevice, left);
                UnityEngine.XR.InputDevices.GetDevicesWithCharacteristics(
                    InputDeviceCharacteristics.Controller | InputDeviceCharacteristics.Right |
                    InputDeviceCharacteristics.TrackedDevice, right);
                yield return null;
            } while (head.Count + left.Count + right.Count < 2);

            foreach (var h in head)
            {
                vendor = ParseVendor(h.name);
                headset = ParseHeadset(h.name);
            }

            Vendor = Vendor;
            Headset = headset;
            callback(Vendor, Headset);
#else
            callback(XRVendor.None, XRHeadset.None);
            yield break;
#endif
        }

        public static bool SetTrackingOriginMode(TrackingOriginModeFlags origin, bool recenter)
        {
#if UNITY_XR_SUPPORTED
            var xrInput = GetXRInput();
            if (xrInput == null) return false;

            if (xrInput.TrySetTrackingOriginMode(origin))
            {
                if (recenter)
                    return Recenter();

                return true;
            }
#endif

            return false;
        }

        public static bool Recenter()
        {
#if UNITY_XR_SUPPORTED
            var subsystems = new List<XRInputSubsystem>();
            SubsystemManager.GetSubsystems(subsystems);

            foreach (var subsystem in subsystems)
            {
                if (!subsystem.TryRecenter())
                    return false;
            }

            return true;
#else
            return false;
#endif
        }

        public static void Vibrate(MonoBehaviour target, XRNode node, float amplitude = 0.5f, float seconds = 0.25f)
        {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
            if (!target.gameObject.activeSelf || !target.gameObject.activeInHierarchy) return;

            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);

            if (!device.TryGetHapticCapabilities(out HapticCapabilities capabilities)) return;

            if (capabilities.supportsBuffer)
            {
                byte[] buffer = { };

                if (GenerateBuzzClip(seconds, node, ref buffer))
                    device.SendHapticBuffer(0, buffer);
            }
            else if (capabilities.supportsImpulse)
                device.SendHapticImpulse(0, amplitude, seconds);

            target.StartCoroutine(StopVibrationCoroutine(device, seconds));
#endif
        }

        private static IEnumerator StopVibrationCoroutine(InputDevice device, float delay)
        {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
            if (delay > 0)
                yield return CoroutineFactory.WaitForSeconds(delay);

            device.StopHaptics();
#else
            yield break;
#endif
        }

        public static bool GenerateBuzzClip(float seconds, XRNode node, ref byte[] clip)
        {
#if UNITY_XR_SUPPORTED && !UNITY_VISIONOS
            var device = UnityEngine.XR.InputDevices.GetDeviceAtXRNode(node);
            var result = device.TryGetHapticCapabilities(out HapticCapabilities caps);

            if (result)
            {
                var clipCount = (int)(caps.bufferFrequencyHz * seconds);
                clip = new byte[clipCount];

                for (int i = 0; i < clipCount; i++)
                {
                    clip[i] = byte.MaxValue;
                }
            }

            return result;
#else
            clip = null;
            return false;
#endif
        }
    }
}