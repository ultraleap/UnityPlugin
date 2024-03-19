using JetBrains.Annotations;
using System;
using System.Runtime.InteropServices;
using Ultraleap.Tracking.OpenXR.Interop;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;

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
        OpenxrExtensionStrings =
            "XR_EXT_hand_tracking XR_ULTRALEAP_hand_tracking_forearm XR_ULTRALEAP_hand_tracking_hints",
        BuildTargetGroups = new[] { BuildTargetGroup.Standalone, BuildTargetGroup.Android }
    )]
#endif
    public class HandTrackingFeature : OpenXRFeature
    {
        [PublicAPI] public const string FeatureId = "com.ultraleap.tracking.openxr.feature.handtracking";

        [Header("Meta Hand-Tracking")] [Tooltip("Adds required permissions and features to the Android manifest")]
        public bool metaPermissions = true;

        [Tooltip("Enable Meta high-frequency hand-tracking")]
        public bool metaHighFrequency = true;

        private bool _supportsHandTracking;
        private bool _isUltraleapTracking;
        private bool _supportsHandTrackingHints;

        /// <summary>
        /// True if the XR_EXT_hand_tracking OpenXR extension is supported (and is a supported revision) and the
        /// system indicates it supports hand-tracking.
        /// </summary>
        [PublicAPI]
        public bool SupportsHandTracking => enabled && _supportsHandTracking;

        /// <summary>
        /// Indicates if the tracking is provided by Ultraleap as opposed to another OpenXR implementation.
        /// </summary>
        [PublicAPI]
        public bool IsUltraleapHandTracking => enabled && _isUltraleapTracking;

        /// <summary>
        /// True if the XR_ULTRALEAP_hand_tracking_hints OpenXR extension is supported (and is a supported revision).
        /// </summary>
        [PublicAPI]
        public bool SupportsHandTrackingHints => enabled && _supportsHandTrackingHints;

        /// <summary>
        /// Sets a specific set of hints, if this does not include previously set ones, they will be cleared.
        /// </summary>
        /// <param name="hints">The hints you wish to set</param>
        public void SetHandTrackingHints(string[] hints)
        {
            if (SupportsHandTrackingHints)
            {
                Native.SetHandTrackingHints(hints, (uint)hints.Length);
            }
        }

        /// <summary>
        /// Clears all hand-tracking hints that have been previously set.
        /// </summary>
        public void ClearHandTrackingHints() => SetHandTrackingHints(new string[] { });

        //protected override IntPtr HookGetInstanceProcAddr(IntPtr func) => Native.HookGetInstanceProcAddr(func);

        private XrInstance _xrInstance;
        private XrSession _xrSession;
        private XrSystemId _xrSystemId;
        private XrSpace _xrAppSpace;

        private GetInstanceProcAddrDelegate _xrGetInstanceProcAddr;
        private WaitFrameDelegate _xrWaitFrame;
        private GetSystemPropertiesDelegate _xrGetSystemProperties;
        private CreateHandTrackerExtDelegate _xrCreateHandTrackerExt;
        private DestroyHandTrackerExtDelegate _xrDestroyHandTrackerExt;

        private GetInstanceProcAddrDelegate _xrGetInstanceProcAddrHook;
        private WaitFrameDelegate _xrWaitFrameHook;

        private XrTime _predictedFrameDisplayTime = 0;
        private XrDuration _predicatedFrameDisplayPeriod = 0;

        private XrHandTrackerExt _leftHandTracker;
        private XrHandTrackerExt _rightHandTracker;

        private XrResult GetInstanceProcAddr(XrInstance xrInstance, in string functionName, out IntPtr xrFunction)
        {
            if (functionName == "xrWaitFrame")
            {
                if (xrInstance.IsNull)
                {
                    xrFunction = IntPtr.Zero;
                    return XrResult.HandleInvalid;
                }

                _xrWaitFrameHook = OnWaitFrame;
                xrFunction = Marshal.GetFunctionPointerForDelegate(_xrWaitFrameHook);
                return XrResult.Success;
            }

            return _xrGetInstanceProcAddr(xrInstance, functionName, out xrFunction);
        }

        private XrResult OnWaitFrame(XrSession xrSession, in XrFrameWaitInfo xrFrameWaitInfo,
            out XrFrameState xrFrameState)
        {
            // Call the function on the runtime first so the frame-state is populated.
            XrResult result = _xrWaitFrame(xrSession, xrFrameWaitInfo, out xrFrameState);

            // If that was successful, record the predicted times for later lookup in the hand-tracking functions.
            if (result.Succeeded())
            {
                _predictedFrameDisplayTime = xrFrameState.PredictedDisplayTime;
                _predicatedFrameDisplayPeriod = xrFrameState.PredictedDisplayPeriod;
            }

            return result;
        }

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func)
        {
            // Store the original function and override with out intercepted version.
            _xrGetInstanceProcAddr = Marshal.GetDelegateForFunctionPointer<GetInstanceProcAddrDelegate>(func);
            _xrGetInstanceProcAddrHook = GetInstanceProcAddr;
            return Marshal.GetFunctionPointerForDelegate(_xrGetInstanceProcAddrHook);
        }

        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            if (!OpenXRRuntime.IsExtensionEnabled("XR_EXT_hand_tracking"))
            {
                Debug.LogWarning("XR_EXT_hand_tracking is not enabled, disabling Hand Tracking");
                return false;
            }

            JointSet = OpenXRRuntime.IsExtensionEnabled("XR_ULTRALEAP_hand_tracking_forearm")
                ? XrHandJointSetExt.HandWithForearm
                : XrHandJointSetExt.Default;

            if (OpenXRRuntime.GetExtensionVersion("XR_EXT_hand_tracking") < 4)
            {
                Debug.LogWarning("XR_EXT_hand_tracking is not at least version 4, disabling Hand Tracking");
                return false;
            }

            if (OpenXRRuntime.IsExtensionEnabled("XR_ULTRALEAP_hand_tracking_hints"))
            {
                _supportsHandTrackingHints = true;
            }

            // TODO: Look up all the functions we need to call OpenXR functionality.
            _xrWaitFrame = GetInstanceDelegate<WaitFrameDelegate>(xrInstance, "xrWaitFrame");
            _xrGetSystemProperties =
                GetInstanceDelegate<GetSystemPropertiesDelegate>(xrInstance, "xrGetSystemProperties");
            _xrCreateHandTrackerExt =
                GetInstanceDelegate<CreateHandTrackerExtDelegate>(xrInstance, "xrCreateHandTrackerEXT");
            _xrDestroyHandTrackerExt =
                GetInstanceDelegate<DestroyHandTrackerExtDelegate>(xrInstance, "xrDestroyHandTrackerEXT");

            // TODO: Implement the ability to detect Ultraleap hand-tracking.
            _isUltraleapTracking = true;
            //_isUltraleapTracking = Native.IsUltraleapHandTracking();

            // Indicate we have initialised successfully.
            return true;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            // Clear all function pointers
            _xrGetInstanceProcAddr = null;
            _xrWaitFrame = null;
            _xrGetSystemProperties = null;
            _xrCreateHandTrackerExt = null;
            _xrDestroyHandTrackerExt = null;

            // Clear any function hooks.
            _xrGetInstanceProcAddrHook = null;
            _xrWaitFrameHook = null;

            // Clear tracked state.
            _predictedFrameDisplayTime = 0;
            _predicatedFrameDisplayPeriod = 0;
        }

        protected override void OnSessionCreate(ulong xrSession) => _xrSession = xrSession;
        protected override void OnSessionDestroy(ulong xrSession) => _xrSession = 0;
        protected override void OnAppSpaceChange(ulong xrSpace) => _xrAppSpace = xrSpace;

        protected override void OnSystemChange(ulong xrSystemId)
        {
            // Store the current system id in-case we need to look at it later.
            _xrSystemId = xrSystemId;

            // Lookup the system properties, and locate the hand-tracking properties in the extension chain.
            var handTrackingProperties = new XrSystemHandTrackingPropertiesExt
            {
                Type = XrStructureType.SystemHandTrackingProperties
            };
            var systemProperties = new XrSystemProperties { Type = XrStructureType.SystemProperties };

            // Now we need to pin the above memory to ensure that it's safe to use this from the following native
            // functions
            {
                // Pin the memory for the extension structure and update the pointer chain.
                var handTrackingPropertiesPin = GCHandle.Alloc(handTrackingProperties, GCHandleType.Pinned);
                systemProperties.Next = handTrackingPropertiesPin.AddrOfPinnedObject();
                
                if (_xrGetSystemProperties(_xrInstance, _xrSystemId, systemProperties).Failed())
                {
                    Debug.LogError("Failed to retrieve system properties.");
                    _supportsHandTracking = false;
                }
            
                // Indicate that hand-tracking is supported if the appropriate system property is set.
                _supportsHandTracking = handTrackingProperties.SupportsHandTracking;
                
                // Ensure that we free the memory for the extension chain.
                handTrackingPropertiesPin.Free();
            }
        }

        protected override void OnSubsystemStart()
        {
            if (!SupportsHandTracking)
            {
                Debug.LogWarning("Hand tracking is not support currently on this device");
                return;
            }

            // Create the left and right hand hand-trackers.
            if (_xrCreateHandTrackerExt(_xrSession, new XrHandTrackerCreateInfoExt(XrHandExt.Left, JointSet),
                    out _leftHandTracker).Failed())
            {
                Debug.LogError("Failed to create XrHandTrackerEXT for the left hand");
                // TODO: Consider a different failure pathway here, exception?
            }

            if (_xrCreateHandTrackerExt(_xrSession, new XrHandTrackerCreateInfoExt(XrHandExt.Right, JointSet),
                    out _rightHandTracker).Failed())
            {
                Debug.LogError("Failed to create XrHandTrackerEXT for the right hand");
                // TODO: Consider a different failure pathway here, exception?
            }
        }

        protected override void OnSubsystemStop()
        {
            if (!SupportsHandTracking)
            {
                return;
            }

            // Destroy the hand-trackers if they've been created.
            if (_leftHandTracker != 0) _xrDestroyHandTrackerExt(_leftHandTracker);
            if (_rightHandTracker != 0) _xrDestroyHandTrackerExt(_rightHandTracker);
        }
        
        private TDelegate GetInstanceDelegate<TDelegate>(XrInstance xrInstance, in string functionName)
        {
            if (_xrGetInstanceProcAddr(xrInstance, functionName, out IntPtr functionPtr) != XrResult.Success)
            {
                throw new Exception($"Failed to lookup OpenXR function {functionName}");
            }

            return Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPtr);
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

        internal XrHandJointSetExt JointSet { get; private set; }

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