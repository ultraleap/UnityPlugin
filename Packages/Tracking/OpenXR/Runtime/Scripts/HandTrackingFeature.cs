using JetBrains.Annotations;
using System;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;

#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.XR.OpenXR.Features;
#endif

namespace Ultraleap.Tracking.OpenXR
{
    /// <summary>
    /// Enables OpenXR hand-tracking support via the <see href="https://www.khronos.org/registry/OpenXR/specs/1.0/html/xrspec.html#XR_EXT_hand_tracking">XR_EXT_hand_tracking</see> OpenXR extension.
    /// </summary>
#if UNITY_EDITOR
    [OpenXRFeature(FeatureId = FeatureId,
        Version = "1.0.0",
        UiName = "Ultraleap Hand Tracking",
        Company = "Ultraleap",
        Desc = "Articulated hands using XR_EXT_hand_tracking",
        Category = FeatureCategory.Feature,
        Required = false,
        OpenxrExtensionStrings = "XR_EXT_hand_tracking XR_ULTRALEAP_hand_tracking_forearm",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android }
    )]
#endif
    public class HandTrackingFeature : OpenXRFeature
    {
        [PublicAPI] public const string FeatureId = "com.ultraleap.tracking.openxr.feature.handtracking";

        [Header("Meta Hand-Tracking")]
        [Tooltip("Adds required permissions and features to the Android manifest")]
        public bool metaPermissions = true;

        [Tooltip("Enable Meta high-frequency hand-tracking")]
        public bool metaHighFrequency = true;

        private static class Native
        {
            private const string NativeDLL = "UltraleapOpenXRUnity";
            private const string NativePrefix = "Unity_HandTrackingFeature_";

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "HookGetInstanceProcAddr", ExactSpelling = true)]
            internal static extern IntPtr HookGetInstanceProcAddr(IntPtr func);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "OnInstanceCreate", ExactSpelling = true)]
            internal static extern bool OnInstanceCreate(ulong xrInstance);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "OnInstanceDestroy", ExactSpelling = true)]
            internal static extern void OnInstanceDestroy(ulong xrInstance);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "OnSystemChange", ExactSpelling = true)]
            internal static extern void OnSystemChange(ulong xrSystemId);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "OnSessionCreate", ExactSpelling = true)]
            internal static extern void OnSessionCreate(ulong xrInstance);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "OnSessionDestroy", ExactSpelling = true)]
            internal static extern void OnSessionDestroy(ulong xrInstance);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "OnAppSpaceChange", ExactSpelling = true)]
            internal static extern void OnAppSpaceChange(ulong xrSpace);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "IsHandTrackingSupported", ExactSpelling = true)]
            internal static extern bool IsHandTrackingSupported();

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "IsUltraleapHandTracking", ExactSpelling = true)]
            internal static extern bool IsUltraleapHandTracking();

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "CreateHandTrackers", ExactSpelling = true)]
            internal static extern int CreateHandTrackers(HandJointSet jointSet);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "DestroyHandTrackers", ExactSpelling = true)]
            internal static extern int DestroyHandTrackers();

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "LocateHandJoints", ExactSpelling = true)]
            internal static extern int LocateHandJoints(
                Handedness chirality,
                out uint isActive,
                [Out, NotNull, MarshalAs(UnmanagedType.LPArray, SizeParamIndex = 3)]
                HandJointLocation[] joints,
                uint jointCount);

            [DllImport(NativeDLL, EntryPoint = NativePrefix + "XrResultToString", ExactSpelling = true)]
            private static extern IntPtr XrResultToString(int result);

            internal static string ResultToString(int result) => Marshal.PtrToStringAnsi(XrResultToString(result));
        }

        private bool _supportsHandTracking;
        private bool _isUltraleapTracking;
        [PublicAPI] public bool SupportsHandTracking => enabled && _supportsHandTracking;
        [PublicAPI] public bool IsUltraleapHandTracking => enabled && _isUltraleapTracking;

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func) => Native.HookGetInstanceProcAddr(func);
        protected override void OnInstanceDestroy(ulong xrInstance) => Native.OnInstanceDestroy(xrInstance);
        protected override void OnSessionCreate(ulong xrSession) => Native.OnSessionCreate(xrSession);
        protected override void OnSessionDestroy(ulong xrSession) => Native.OnSessionDestroy(xrSession);
        protected override void OnAppSpaceChange(ulong xrSpace) => Native.OnAppSpaceChange(xrSpace);

        protected override void OnSystemChange(ulong xrSystemId)
        {
            Native.OnSystemChange(xrSystemId);
            _supportsHandTracking = Native.IsHandTrackingSupported();
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!OpenXRRuntime.IsExtensionEnabled("XR_EXT_hand_tracking"))
            {
                Debug.LogWarning("XR_EXT_hand_tracking is not enabled, disabling Hand Tracking");
                return false;
            }

            JointSet = OpenXRRuntime.IsExtensionEnabled("XR_ULTRALEAP_hand_tracking_forearm")
                ? HandJointSet.HandWithForearm
                : HandJointSet.Default;

            if (OpenXRRuntime.GetExtensionVersion("XR_EXT_hand_tracking") < 4)
            {
                Debug.LogWarning("XR_EXT_hand_tracking is not at least version 4, disabling Hand Tracking");
                return false;
            }

            bool succeeded = Native.OnInstanceCreate(xrInstance);
            _isUltraleapTracking = Native.IsUltraleapHandTracking();
            return succeeded;
        }

        protected override void OnSubsystemStart()
        {
            if (!SupportsHandTracking)
            {
                Debug.LogWarning("Hand tracking is not support currently on this device");
                return;
            }

            int result = Native.CreateHandTrackers(JointSet);
            if (IsResultFailure(result))
            {
                Debug.LogError($"Failed to create hand-trackers: {Native.ResultToString(result)}");
            }
        }

        protected override void OnSubsystemStop()
        {
            if (!SupportsHandTracking)
            {
                return;
            }

            int result = Native.DestroyHandTrackers();
            if (IsResultFailure(result))
            {
                Debug.LogError($"Failed to destroy hand-trackers: {Native.ResultToString(result)}");
            }
        }

        internal bool LocateHandJoints(Handedness handedness, HandJointLocation[] handJointLocations)
        {
            if (!SupportsHandTracking)
            {
                return false;
            }

            int result = Native.LocateHandJoints(handedness, out uint isActive, handJointLocations,
                (uint)handJointLocations.Length);
            if (IsResultFailure(result))
            {
                Debug.LogError($"Failed to locate hand-joints: {Native.ResultToString(result)}");
                return false;
            }

            return Convert.ToBoolean(isActive);
        }

        internal HandJointSet JointSet { get; private set; }

        // All OpenXR error codes are negative.
        private static bool IsResultFailure(int result) => result < 0;

#if UNITY_EDITOR
        protected override void GetValidationChecks(List<ValidationRule> rules, BuildTargetGroup targetGroup)
        {
#if UNITY_ANDROID
            // If building for Android, check that we are targeting Android API 29 for maximum compatibility.
            rules.Add(new ValidationRule(this)
            {
                message = "Android target SDK version is not set to 29, hand-tracking may not work on devices " +
                          "running Android 11 or higher",
                helpLink = "https://registry.khronos.org/OpenXR/specs/1.0/loader.html#android-active-runtime-location",
                helpText = "OpenXR applications should not target API levels higher than 29 for maximum " +
                           "compatibility, as runtimes may need to query and load classes from their own packages, " +
                           "which are necessarily not listed in the <queries> tag above.",
                checkPredicate = () => PlayerSettings.Android.targetSdkVersion == AndroidSdkVersions.AndroidApiLevel29,
                error = false,
                fixItAutomatic = true,
                fixItMessage = "Set the Android target SDK version to 29",
                fixIt = () =>
                {
                    if (PlayerSettings.Android.minSdkVersion > AndroidSdkVersions.AndroidApiLevel29)
                    {
                        PlayerSettings.Android.minSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                    }

                    PlayerSettings.Android.targetSdkVersion = AndroidSdkVersions.AndroidApiLevel29;
                },
            });
#endif

            // Check the active input handling supports New (for OpenXR) and Legacy (for Ultraleap Plugin support).
            rules.Add(new ValidationRule(this)
            {
                message = "Active Input Handling is not set to Both. While New is required for OpenXR, Both is " +
                          "recommended as the Ultraleap Unity Plugin does not fully support the New Input System.",
                error = false,
#if !ENABLE_LEGACY_INPUT_MANAGER || !ENABLE_INPUT_SYSTEM
                checkPredicate = () => false,
#else
                checkPredicate = () => true,
#endif
                fixItAutomatic = false,
                fixItMessage = "Enable the Legacy Input Manager and replacement Input System together (Both)",
                fixIt = () => SettingsService.OpenProjectSettings("Project/Player"),
            });

            // Check that the Main camera has a suitable near clipping plane for hand-tracking.
            rules.Add(new ValidationRule(this)
            {
                message = "Main camera near clipping plane is further than recommend and tracked hands may show " +
                          "visual clipping artifacts.",
                error = false,
                checkPredicate = () => Camera.main == null || Camera.main.nearClipPlane <= 0.01,
                fixItAutomatic = true,
                fixItMessage = "Set main camera clipping plane to 0.01",
                fixIt = () =>
                {
                    if (Camera.main != null)
                    {
                        Camera.main.nearClipPlane = 0.01f;
                    }
                },
            });
        }
#endif
    }
}