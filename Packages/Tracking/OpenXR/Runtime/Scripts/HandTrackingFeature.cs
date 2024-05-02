using JetBrains.Annotations;
using Leap.Unity;
using System;
using System.Runtime.InteropServices;
using Ultraleap.Tracking.OpenXR.Interop;
using UnityEngine;
using UnityEngine.XR.OpenXR;
using UnityEngine.XR.OpenXR.Features;
using UnityEngine.XR.OpenXR.NativeTypes;
using Constants = Ultraleap.Tracking.OpenXR.Interop.Constants;

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
                _xrSetHandTrackingHintsUltraleap(hints, (uint)hints.Length);
            }
        }

        /// <summary>
        /// Clears all hand-tracking hints that have been previously set.
        /// </summary>
        public void ClearHandTrackingHints() => SetHandTrackingHints(new string[] { });

        private XrInstance _xrInstance;
        private XrSession _xrSession;
        private XrSystemId _xrSystemId;
        private XrSpace _xrAppSpace;

        private GetInstanceProcAddrDelegate _xrGetInstanceProcAddr;
        private GetSystemPropertiesDelegate _xrGetSystemProperties;

        private CreateHandTrackerExtDelegate _xrCreateHandTrackerExt;
        private DestroyHandTrackerExtDelegate _xrDestroyHandTrackerExt;
        private LocateHandJointsExtDelegate _xrLocateHandJointsExt;

        private SetHandTrackingHintsUltraleapDelegate _xrSetHandTrackingHintsUltraleap;

        private XrHandTrackerExt _leftHandTracker;
        private XrHandTrackerExt _rightHandTracker;

        protected override IntPtr HookGetInstanceProcAddr(IntPtr func) => Native.HookGetInstanceProcAddr(func);
        
        
        protected override bool OnInstanceCreate(ulong xrInstance)
        {
            // Keep a copy of the instance.
            _xrInstance = xrInstance;

            if (!OpenXRRuntime.IsExtensionEnabled("XR_EXT_hand_tracking") ||
                OpenXRRuntime.GetExtensionVersion("XR_EXT_hand_tracking") < 4)
            {
                Debug.LogWarning("XR_EXT_hand_tracking is not enabled, disabling Hand Tracking");
                return false;
            }

            try
            {
                _xrGetInstanceProcAddr =
                    GetInstanceDelegate<GetInstanceProcAddrDelegate>("xrGetInstanceProcAddr");
                _xrGetSystemProperties =
                    GetInstanceDelegate<GetSystemPropertiesDelegate>("xrGetSystemProperties");
                _xrCreateHandTrackerExt =
                    GetInstanceDelegate<CreateHandTrackerExtDelegate>("xrCreateHandTrackerEXT");
                _xrDestroyHandTrackerExt =
                    GetInstanceDelegate<DestroyHandTrackerExtDelegate>("xrDestroyHandTrackerEXT");
                _xrLocateHandJointsExt =
                    GetInstanceDelegate<LocateHandJointsExtDelegate>("xrLocateHandJointsExt");
            }
            catch
            {
                Debug.LogError("XR_EXT_hand_tracking was enabled but functions lookups returned null");
                return false;
            }

            // Enable the elbow joint-set if it is available.
            JointSet = OpenXRRuntime.IsExtensionEnabled("XR_ULTRALEAP_hand_tracking_forearm")
                ? XrHandJointSetExt.HandWithForearmUltraleap
                : XrHandJointSetExt.Default;

            // Enable the Ultraleap hinting extension if supported.
            if (OpenXRRuntime.IsExtensionEnabled("XR_ULTRALEAP_hand_tracking_hints"))
            {
                try
                {
                    _xrSetHandTrackingHintsUltraleap =
                        GetInstanceDelegate<SetHandTrackingHintsUltraleapDelegate>("xrSetHandTrackingHintsULTRALEAP");
                    _supportsHandTrackingHints = true;
                }
                catch
                {
                    _supportsHandTrackingHints = false;
                }
            }

            // TODO: Implement the ability to detect Ultraleap hand-tracking.
            _isUltraleapTracking = true;
            //_isUltraleapTracking = Native.IsUltraleapHandTracking();

            // Indicate we have initialised successfully.
            return true;
        }

        protected override void OnInstanceDestroy(ulong xrInstance)
        {
            // Clear our instance handle (and associated system id).
            _xrInstance = 0;
            _xrSystemId = 0;

            // Clear all function pointers
            _xrGetSystemProperties = null;
            _xrCreateHandTrackerExt = null;
            _xrDestroyHandTrackerExt = null;
            _xrLocateHandJointsExt = null;
            _xrSetHandTrackingHintsUltraleap = null;
        }

        protected override void OnSessionCreate(ulong xrSession) => _xrSession = xrSession;

        protected override void OnSessionDestroy(ulong xrSession)
        {
            // Clear the session handle and the associated space handle.
            _xrSession = 0;
            _xrAppSpace = 0;
        }

        protected override void OnAppSpaceChange(ulong xrSpace) => _xrAppSpace = xrSpace;

        protected override void OnSystemChange(ulong xrSystemId)
        {
            // Store the current system id in-case we need to look at it later.
            _xrSystemId = xrSystemId;

            // Construct an extension chain for querying the system properties including hand-tracking.
            var handTrackingProperties = new XrSystemHandTrackingPropertiesExt(IntPtr.Zero);
            var handTrackingPropertiesHandle = GCHandle.Alloc(handTrackingProperties, GCHandleType.Pinned);
            var systemProperties = new XrSystemProperties(handTrackingPropertiesHandle.AddrOfPinnedObject());

            if (_xrGetSystemProperties(_xrInstance, _xrSystemId, systemProperties).Failed())
            {
                Debug.LogError("Failed to retrieve system properties.");
                _supportsHandTracking = false;
            }

            // Indicate that hand-tracking is supported if the appropriate system property is set.
            _supportsHandTracking = handTrackingProperties.SupportsHandTracking;

            // Free the GCHandles.
            handTrackingPropertiesHandle.Free();
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

        private TDelegate GetInstanceDelegate<TDelegate>(in string functionName) where TDelegate : Delegate
        {
            if (functionName == "xrGetInstanceProcAddr")
            { 
                return Marshal.GetDelegateForFunctionPointer<TDelegate>(xrGetInstanceProcAddr);
            }
            
            if (_xrGetInstanceProcAddr(_xrInstance, functionName, out IntPtr functionPtr).Failed())
            {
                throw new Exception($"Failed to lookup address of {functionName}");
            }

            return Marshal.GetDelegateForFunctionPointer<TDelegate>(functionPtr);
        }

        public XrHandJointSetExt JointSet { get; set; }

        public int JointCount =>
            JointSet switch
            {
                XrHandJointSetExt.Default => Constants.HandJointCountExt,
                XrHandJointSetExt.HandWithForearmUltraleap => Constants.HandForearmJointCountUltraleap,
                _ => throw new ArgumentOutOfRangeException()
            };

        private XrHandTrackerExt GetHandTrackerForHand(XrHandExt hand) =>
            hand == XrHandExt.Left ? _leftHandTracker : _rightHandTracker;

        internal bool LocateHandJoints(XrHandExt hand, ref XrHandJointLocationExt[] jointLocations,
            ref XrHandJointVelocityExt[] jointVelocities)
        {
            if (!SupportsHandTracking)
            {
                return false;
            }

            // Get the frame state to look up our predicted display time.
            var frameState = Native.GetCurrentFrameState();

            // Input structure.
            var locateInfo = new XrHandJointsLocateInfoExt
            {
                Type = XrStructureType.HandJointsLocateInfoExt,
                Next = IntPtr.Zero,
                BaseSpace = _xrAppSpace,
                Time = frameState.PredictedDisplayTime,
            };

            // Output query structures.
            // TODO: Make velocity optional?
            var velocitiesDataHandle = GCHandle.Alloc(jointVelocities, GCHandleType.Pinned);
            var velocities = new XrHandJointVelocitiesExt
            {
                Type = XrStructureType.HandJointLocationsExt,
                Next = IntPtr.Zero,
                JointCount = (uint)jointVelocities.Length,
                JointVelocitiesPtr = velocitiesDataHandle.AddrOfPinnedObject(),
            };
            var velocitiesHandle = GCHandle.Alloc(velocities, GCHandleType.Pinned);

            var locationsDataHandle = GCHandle.Alloc(jointLocations, GCHandleType.Pinned);
            var locations = new XrHandJointLocationsExt
            {
                Type = XrStructureType.HandJointLocationsExt,
                Next = velocitiesHandle.AddrOfPinnedObject(),
                JointCount = (uint)jointLocations.Length,
                IsActive = false,
                JointLocationsPtr = locationsDataHandle.AddrOfPinnedObject(),
            };

            XrResult result = _xrLocateHandJointsExt(GetHandTrackerForHand(hand), locateInfo, locations);
            if (result.Failed())
            {
                throw new Exception($"Failed to locate hand-joints: {result.ToString()}");
            }

            return locations.IsActive;
        }

        private static class Native
        {
            private const string LibraryName = "UnityOpenXRUltraleapHandTracking";
            private const string LibraryPrefix = "Unity_";

            [DllImport(LibraryName, EntryPoint = LibraryPrefix + nameof(HookGetInstanceProcAddr))]
            internal static extern IntPtr HookGetInstanceProcAddr(IntPtr func);

            [DllImport(LibraryName, EntryPoint = LibraryPrefix + nameof(GetCurrentFrameState))]
            internal static extern XrFrameState GetCurrentFrameState();
        }

        #region Validation Checks

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

        #endregion
    }
}