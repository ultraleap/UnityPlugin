/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Attributes;
using LeapInternal;
using System;
using UnityEngine;
#if UNITY_2019_1_OR_NEWER
using UnityEngine.Rendering;
#endif

namespace Leap.Unity
{

    /// <summary>
    /// The LeapXRServiceProvider expands on the standard LeapServiceProvider to
    /// account for the offset of the Leap device with respect to the attached HMD and
    /// warp tracked hand positions based on the motion of the headset to account for the
    /// differing latencies of the two tracking systems.
    /// 
    /// This component can be placed anywhere in your scene as long as mainCamera 
    /// references the XR camera. 
    /// </summary>
    public class LeapXRServiceProvider : LeapServiceProvider
    {

        #region Inspector
        // Manual Device Offset
#if UNITY_ANDROID
        private const float DEFAULT_DEVICE_OFFSET_Y_AXIS = -0.0114f;
        private const float DEFAULT_DEVICE_OFFSET_Z_AXIS = 0.0981f;
        private const float DEFAULT_DEVICE_TILT_X_AXIS = 0f;
#else
        private const float DEFAULT_DEVICE_OFFSET_Y_AXIS = 0f;
        private const float DEFAULT_DEVICE_OFFSET_Z_AXIS = 0.12f;
        private const float DEFAULT_DEVICE_TILT_X_AXIS = 5f;
#endif

#if SVR
        private enum TimewarpMode
        {
            Default,
            Experimental_XR2
        }

        private TimewarpMode _xr2TimewarpMode = TimewarpMode.Default;
#endif
        /// <summary>
        /// Supported modes for device offset. Used for deviceOffsetMode which allows 
        /// manual adjustment of the Tracking Hardware's virtual offset and tilt.
        /// </summary>
        public enum DeviceOffsetMode
        {
            /// <summary>
            /// Defaults to constants set at the top of LeapServiceProvider 
            /// (currently: offset y axis = 0; offset z axis = 0.12; tilt x axis = 5)
            /// </summary>
            Default,
            /// <summary>
            /// Allows to specify the offset as a combination of 
            /// deviceOffsetYAxis, deviceOffsetZAxis and deviceTiltXAxis.
            /// </summary>
            ManualHeadOffset,
            /// <summary>
            /// Allows for the manual placement of the Tracking Hardware.
            /// This device offset mode is incompatible with Temporal Warping.
            /// When choosing this mode, deviceOrigin has to be set.
            /// </summary>
            Transform
        }

        [Tooltip("Allow manual adjustment of the Tracking Hardware's virtual offset and tilt. These "
               + "settings can be used to match the physical position and orientation of the "
               + "Tracking Hardware on a tracked device it is mounted on (such as a VR "
               + "headset). ")]
        [SerializeField, OnEditorChange("deviceOffsetMode")]
        private DeviceOffsetMode _deviceOffsetMode;
        /// <summary>
        /// Allow manual adjustment of the Tracking Hardware's virtual offset and tilt. 
        /// These settings can be used to match the physical position and orientation of the
        /// Tracking Hardware on a tracked device it is mounted on (such as a VR headset)
        /// </summary>
        public DeviceOffsetMode deviceOffsetMode
        {
            get { return _deviceOffsetMode; }
            set
            {
                _deviceOffsetMode = value;
                if (_deviceOffsetMode == DeviceOffsetMode.Default)
                {
                    deviceOffsetYAxis = DEFAULT_DEVICE_OFFSET_Y_AXIS;
                    deviceOffsetZAxis = DEFAULT_DEVICE_OFFSET_Z_AXIS;
                    deviceTiltXAxis = DEFAULT_DEVICE_TILT_X_AXIS;
                }
            }
        }

        [Tooltip("Adjusts the Tracking Hardware's virtual height offset from the tracked "
               + "headset position. This should match the vertical offset of the physical "
               + "device with respect to the headset in meters.")]
        [SerializeField]
        [Range(-0.50F, 0.50F)]
        private float _deviceOffsetYAxis = DEFAULT_DEVICE_OFFSET_Y_AXIS;
        /// <summary>
        /// Adjusts the Tracking Hardware's virtual height offset from the tracked 
        /// headset position. This should match the vertical offset of the physical device
        /// with respect to the headset in meters.
        /// </summary>
        public float deviceOffsetYAxis
        {
            get { return _deviceOffsetYAxis; }
            set { _deviceOffsetYAxis = value; }
        }

        [Tooltip("Adjusts the Tracking Hardware's virtual depth offset from the tracked "
               + "headset position. This should match the forward offset of the physical "
               + "device with respect to the headset in meters.")]
        [SerializeField]
        [Range(-0.50F, 0.50F)]
        private float _deviceOffsetZAxis = DEFAULT_DEVICE_OFFSET_Z_AXIS;
        /// <summary>
        /// Adjusts the Tracking Hardware's virtual depth offset from the tracked 
        /// headset position. This should match the forward offset of the physical 
        /// device with respect to the headset in meters.
        /// </summary>
        public float deviceOffsetZAxis
        {
            get { return _deviceOffsetZAxis; }
            set { _deviceOffsetZAxis = value; }
        }

        [Tooltip("Adjusts the Tracking Hardware's virtual X axis tilt. This should match "
               + "the tilt of the physical device with respect to the headset in degrees.")]
        [SerializeField]
        [Range(-90.0F, 90.0F)]
        private float _deviceTiltXAxis = DEFAULT_DEVICE_TILT_X_AXIS;
        /// <summary>
        /// Adjusts the Tracking Hardware's virtual X axis tilt. This should match 
        /// the tilt of the physical device with respect to the headset in degrees.
        /// </summary>
        public float deviceTiltXAxis
        {
            get { return _deviceTiltXAxis; }
            set { _deviceTiltXAxis = value; }
        }

        [Tooltip("Allows for the manual placement of the Tracking Hardware." +
          "This device offset mode is incompatible with Temporal Warping.")]
        [SerializeField]
        private Transform _deviceOrigin;
        /// <summary>
        /// Allows for the manual placement of the Tracking Hardware.
        /// This device offset mode is incompatible with Temporal Warping.
        /// This is only used if the deviceOffsetMode is 'Transform'.
        /// </summary>
        public Transform deviceOrigin
        {
            get { return _deviceOrigin; }
            set { _deviceOrigin = value; }
        }

        [Tooltip("Specifies the main camera. Required for XR2 based platforms. "
               + "Falls back to Camera.main if not set")]
        [SerializeField]
        private Camera _mainCamera; // Redundant backing field, used to present value in editor at parent level
        /// <summary>
        /// Specifies the main camera. Required for XR2 based platforms.
        /// Falls back to Camera.main if not set.
        /// </summary>
        public Camera mainCamera
        {
            get
            {
                if (_mainCamera != null)
                {
                    if (_mainCamera != MainCameraProvider.mainCamera)
                    {
                        MainCameraProvider.mainCamera = _mainCamera;
                    }
                }

                return _mainCamera;
            }
            set
            {
                _mainCamera = value;
                MainCameraProvider.mainCamera = value;
            }
        }

        // Temporal Warping
#if UNITY_STANDALONE
        private const int DEFAULT_WARP_ADJUSTMENT = 17;
#elif SVR
        private const int DEFAULT_WARP_ADJUSTMENT = 35; // Tuned for XR2 on a Morpheus SKU3
#else
        private const int DEFAULT_WARP_ADJUSTMENT = 17;
#endif

        /// <summary>
        /// Temporal warping prevents the hand coordinate system from 'swimming' or 
        /// 'bouncing' when the headset moves and the user's hands stay still. 
        /// This phenomenon is caused by the differing amounts of latencies inherent 
        /// in the two systems.
        /// </summary>
        public enum TemporalWarpingMode
        {
            /// <summary>
            /// For PC VR and Android VR, temporal warping should set to 'Auto', as the 
            /// correct value can be chosen automatically for these platforms.
            /// </summary>
            Auto,
            /// <summary>
            /// Some non-standard platforms may use 'Manual' mode to adjust their 
            /// latency compensation amount for temporal warping.
            /// </summary>
            Manual,
            /// <summary>
            /// Use 'Images' for scenarios that overlay Tracking Service images on tracked 
            /// hand data.
            /// </summary>
            Images,
            Off
        }
        [Tooltip("Temporal warping prevents the hand coordinate system from 'swimming' or "
               + "'bouncing' when the headset moves and the user's hands stay still. "
               + "This phenomenon is caused by the differing amounts of latencies inherent "
               + "in the two systems. "
               + "For PC VR and Android VR, temporal warping should set to 'Auto', as the "
               + "correct value can be chosen automatically for these platforms. "
               + "Some non-standard platforms may use 'Manual' mode to adjust their "
               + "latency compensation amount for temporal warping. "
               + "Use 'Images' for scenarios that overlay Tracking Service images on tracked "
               + "hand data.")]
        [SerializeField]
        private TemporalWarpingMode _temporalWarpingMode = TemporalWarpingMode.Auto;


        [Tooltip("The time in milliseconds between the current frame's headset position and "
               + "the time at which the Leap frame was captured.")]
        [SerializeField]
        private int _customWarpAdjustment = DEFAULT_WARP_ADJUSTMENT;
        /// <summary>
        /// The time in milliseconds between the current frame's headset position and the
        /// time at which the Leap frame was captured.
        /// Change this only when the TemporalWarpingMode is 'Manual'.
        /// </summary>
        public int warpingAdjustment
        {
            get
            {
                if (_temporalWarpingMode == TemporalWarpingMode.Manual)
                {
                    return _customWarpAdjustment;
                }
                else
                {
                    return DEFAULT_WARP_ADJUSTMENT;
                }
            }
            set
            {
                if (_temporalWarpingMode != TemporalWarpingMode.Manual)
                {
                    Debug.LogWarning("Setting custom warping adjustment amount, but the "
                      + "temporal warping mode is not Manual, so the value will be "
                      + "ignored.");
                }
                _customWarpAdjustment = value;
            }
        }

        // Pre-cull Latching

        [Tooltip("Pass updated transform matrices to hands with materials that utilize the "
               + "VertexOffsetShader. Won't have any effect on hands that don't take into "
               + "account shader-global vertex offsets in their material shaders.")]
        [SerializeField]
        protected bool _updateHandInPrecull = false;
        /// <summary>
        /// Pass updated transform matrices to hands with materials that utilize the 
        /// VertexOffsetShader. Won't have any effect on hands that don't take into 
        /// account shader-global vertex offsets in their material shaders.
        /// </summary>
        public bool updateHandInPrecull
        {
            get { return _updateHandInPrecull; }
            set
            {
                resetShaderTransforms();
                _updateHandInPrecull = value;
            }
        }

        #endregion

        #region Internal Memory

        protected TransformHistory transformHistory = new TransformHistory();
        protected bool manualUpdateHasBeenCalledSinceUpdate;
        protected Vector3 warpedPosition = Vector3.zero;
        protected Quaternion warpedRotation = Quaternion.identity;
        protected Matrix4x4[] _transformArray = new Matrix4x4[2];

        /// <summary>
        /// Contains the Frame.Timestamp of the most recent tracked frame.
        /// It is used for Image warping if the temporalWarpingMode is set to 
        /// Auto, Manual or Images
        /// </summary>
        [NonSerialized]
        public long imageTimeStamp = 0;

        #endregion

        #region Unity Events

        protected override void Reset()
        {
            base.Reset();
            editTimePose = TestHandFactory.TestHandPose.HeadMountedB;

            _interactionVolumeVisualization = InteractionVolumeVisualization.Automatic;
            mainCamera = MainCameraProvider.mainCamera;
            if (mainCamera != null)
            {
                Debug.Log("Camera.Main automatically assigned");
            }
        }

        protected override void OnEnable()
        {
            resetShaderTransforms();

            // Assign the main camera if it looks like one is available and it's not yet been set on the backing field
            // NB this may be the case if the provider is created via AddComponent, as in MRTK
            if (mainCamera == null && MainCameraProvider.mainCamera != null)
            {
                mainCamera = MainCameraProvider.mainCamera;
            }

#if XR_LEGACY_INPUT_AVAILABLE
            if (mainCamera.GetComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>() == null)
            {
                mainCamera.gameObject.AddComponent<UnityEngine.SpatialTracking.TrackedPoseDriver>().UseRelativeTransform = true;
            }
#endif

#if UNITY_2019_1_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                RenderPipelineManager.beginCameraRendering -= onBeginRendering;
                RenderPipelineManager.beginCameraRendering += onBeginRendering;
            }
            else
            {
                Camera.onPreCull -= onPreCull; // No multiple-subscription.
                Camera.onPreCull += onPreCull;
            }
#else
            Camera.onPreCull -= onPreCull; // No multiple-subscription.
            Camera.onPreCull += onPreCull;
#endif

#if UNITY_ANDROID
            base.OnEnable();
#endif
        }

        protected override void OnDisable()
        {
            resetShaderTransforms();

#if UNITY_2019_1_OR_NEWER
            if (GraphicsSettings.renderPipelineAsset != null)
            {
                RenderPipelineManager.beginCameraRendering -= onBeginRendering;
            }
            else
            {
                Camera.onPreCull -= onPreCull; // No multiple-subscription.
            }
#else
            Camera.onPreCull -= onPreCull; // No multiple-subscription.
#endif

#if UNITY_ANDROID
            base.OnDisable();
#endif
        }

        protected override void Start()
        {
            base.Start();

            if (_deviceOffsetMode == DeviceOffsetMode.Transform && _deviceOrigin == null)
            {
                Debug.LogError("Cannot use the Transform device offset mode without " +
                               "specifying a Transform to use as the device origin.", this);
                _deviceOffsetMode = DeviceOffsetMode.Default;
            }

            if (Application.isPlaying && mainCamera == null && _temporalWarpingMode != TemporalWarpingMode.Off)
            {
                Debug.LogError("Cannot perform temporal warping with no pre-cull camera.");
            }
        }

        protected override void Update()
        {
            manualUpdateHasBeenCalledSinceUpdate = false;
            base.Update();
            if (_leapController != null)
            {
                imageTimeStamp = _leapController.FrameTimestamp();
            }
        }

        void LateUpdate()
        {
            var projectionMatrix = mainCamera == null ? Matrix4x4.identity
              : mainCamera.projectionMatrix;
            switch (SystemInfo.graphicsDeviceType)
            {
#if !UNITY_2017_2_OR_NEWER
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D9:
#endif
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D11:
                case UnityEngine.Rendering.GraphicsDeviceType.Direct3D12:
                    for (int i = 0; i < 4; i++)
                    {
                        projectionMatrix[1, i] = -projectionMatrix[1, i];
                    }
                    // Scale and bias from OpenGL -> D3D depth range
                    for (int i = 0; i < 4; i++)
                    {
                        projectionMatrix[2, i] = projectionMatrix[2, i] * 0.5f
                                               + projectionMatrix[3, i] * 0.5f;
                    }
                    break;
            }

            // Update Image Warping
            Vector3 pastPosition; Quaternion pastRotation;
            transformHistory.SampleTransform(imageTimeStamp
                                               - (long)(warpingAdjustment * 1000f),
                                             out pastPosition, out pastRotation);

            // Use _tweenImageWarping
            var currCenterRotation = XRSupportUtil.GetXRNodeCenterEyeLocalRotation();

            var imageReferenceRotation = _temporalWarpingMode != TemporalWarpingMode.Off
                                                              ? pastRotation
                                                              : currCenterRotation;

            Quaternion imageQuatWarp = Quaternion.Inverse(currCenterRotation)
                                       * imageReferenceRotation;
            imageQuatWarp = Quaternion.Euler(imageQuatWarp.eulerAngles.x,
                                             imageQuatWarp.eulerAngles.y,
                                            -imageQuatWarp.eulerAngles.z);
            Matrix4x4 imageMatWarp = projectionMatrix
#if UNITY_2019_2_OR_NEWER
                                     // The camera projection matrices seem to have vertically inverted...
                                     * Matrix4x4.TRS(Vector3.zero, imageQuatWarp, new Vector3(1f, -1f, 1f))

#else
                                    * Matrix4x4.TRS(Vector3.zero, imageQuatWarp, Vector3.one)

#endif
                                    * projectionMatrix.inverse;

            Shader.SetGlobalMatrix("_LeapGlobalWarpedOffset", imageMatWarp);
        }

#if UNITY_2019_1_OR_NEWER
        protected virtual void onBeginRendering(ScriptableRenderContext context, Camera camera) { onPreCull(camera); }
#endif

        protected virtual void onPreCull(Camera preCullingCamera)
        {

            if (preCullingCamera != mainCamera)
            {
                return;
            }

#if UNITY_EDITOR
            if (!Application.isPlaying)
            {
                return;
            }

#endif

            if (mainCamera == null || _leapController == null)
            {
                if (_temporalWarpingMode == TemporalWarpingMode.Auto || _temporalWarpingMode == TemporalWarpingMode.Manual)
                {
                    Debug.LogError("The  camera or controller need to be set for temporal warping to work");
                }

                return;
            }

            Pose trackedPose;
            if (_deviceOffsetMode == DeviceOffsetMode.Default
                || _deviceOffsetMode == DeviceOffsetMode.ManualHeadOffset)
            {
                if (mainCamera.transform.parent != null)
                {
                    var position = mainCamera.transform.parent.InverseTransformPoint(mainCamera.transform.position);
                    var rotation = mainCamera.transform.parent.InverseTransformRotation(mainCamera.transform.rotation);

                    trackedPose = new Pose(position, rotation);
                }
                else
                {
                    trackedPose = mainCamera.transform.ToLocalPose();
                }

            }
            else if (_deviceOffsetMode == DeviceOffsetMode.Transform)
            {
                trackedPose = deviceOrigin.ToPose();
            }
            else
            {
                Debug.LogError($"Unsupported DeviceOffsetMode: {_deviceOffsetMode}");
                return;
            }

            transformHistory.UpdateDelay(trackedPose, _leapController.Now());

            OnPreCullHandTransforms(mainCamera);
        }

        #endregion

        #region LeapServiceProvider Overrides

        protected override long CalculateInterpolationTime(bool endOfFrame = false)
        {
            if (_leapController == null)
            {
                return 0;
            }

#if SVR
            if (_xr2TimewarpMode == TimewarpMode.Experimental_XR2)
            {
                return GetPredictedDisplayTime_LeapTime();
            }
            else
            {
                return _leapController.Now() - 16000;
            }

#elif UNITY_ANDROID
            return _leapController.Now() - 16000;
#else

            return _leapController.Now()
                    - (long)_smoothedTrackingLatency.value
                    + ((updateHandInPrecull && !endOfFrame) ?
                        (long)(Time.smoothDeltaTime * S_TO_NS / Time.timeScale)
                        : 0);

#endif
        }

        /// <summary>
        /// Initializes the policy flags.
        /// The POLICY_OPTIMIZE_HMD flag improves tracking for head-mounted devices.
        /// </summary>
        protected override void initializeFlags()
        {
            if (_preventInitializingTrackingMode) return;
            ChangeTrackingMode(TrackingOptimizationMode.HMD);
        }

        protected override void transformFrame(Frame source, Frame dest)
        {
            LeapTransform leapTransform = LeapTransform.Identity;

            if (mainCamera != null)
            {
                //By default, use the camera transform matrix to transform the frame into 
                leapTransform = mainCamera.transform.GetLeapMatrix();
                leapTransform.scale = Vector.Ones * 1e-3f;

                //If the application is playing then we can try to use temporal warping
                if (Application.isPlaying)
                {
                    leapTransform = GetWarpedMatrix(source.Timestamp);
                }
            }

            dest.CopyFrom(source).Transform(leapTransform);
        }

        #endregion

        #region Internal Methods

        /// <summary>
        /// Resets shader globals for the Hand transforms.
        /// </summary>
        protected void resetShaderTransforms()
        {
            _transformArray[0] = Matrix4x4.identity;
            _transformArray[1] = Matrix4x4.identity;
            Shader.SetGlobalMatrixArray(HAND_ARRAY_GLOBAL_NAME, _transformArray);
        }

        protected virtual LeapTransform GetWarpedMatrix(long timestamp, bool updateTemporalCompensation = true)
        {
            if (mainCamera == null || this == null)
            {
                return LeapTransform.Identity;
            }

            LeapTransform leapTransform = new LeapTransform();

            // If temporal warping is turned off
            if (_temporalWarpingMode == TemporalWarpingMode.Off)
            {
                //Calculate the Current Pose
                Pose currentPose = Pose.identity;
                if (_deviceOffsetMode == DeviceOffsetMode.Transform && deviceOrigin != null)
                {
                    currentPose = deviceOrigin.ToPose();
                }
                else
                {
                    transformHistory.SampleTransform(timestamp, out currentPose.position, out currentPose.rotation);
                }

                warpedPosition = currentPose.position;
                warpedRotation = currentPose.rotation;
            }
            //Calculate a Temporally Warped Pose
            else if (updateTemporalCompensation)
            {
                void DefaultTimeWarping()
                {
                    var imageAdjustment = _temporalWarpingMode == TemporalWarpingMode.Images ? -20000 : 0;
                    var sampleTimestamp = timestamp - (long)(warpingAdjustment * 1000f) - imageAdjustment;
                    transformHistory.SampleTransform(sampleTimestamp, out warpedPosition, out warpedRotation);
                }
#if SVR
                void ExperimentalXR2TimeWarping()
                {
                    // Get the predicted display time for the current frame in milliseconds, then get the predicted head pose
                    float predictedDisplayTime_ms = SxrShim.GetPredictedDisplayTime(SystemInfo.graphicsMultiThreaded);

                    Vector3 predictedWarpedPosition;
                    Quaternion predictedWarpedRotation;
                    SxrShim.GetPredictedHeadPose(predictedDisplayTime_ms, out predictedWarpedRotation, out predictedWarpedPosition);
                    warpedPosition.x = -predictedWarpedPosition.x;
                    warpedPosition.y = -predictedWarpedPosition.y;
                    warpedPosition.z = predictedWarpedPosition.z;
                    warpedRotation = predictedWarpedRotation;
                }

                switch(_xr2TimeWarpMode)
                {
                    case TimewarpMode.Default:
                        DefaultTimeWarping();
                        break;
                    case TimewarpMode.Experimental_XR2:
                        ExperimentalXR2TimeWarping();
                        break;
#else
                DefaultTimeWarping();
#endif
            }

            // Normalize the rotation Quaternion.
            warpedRotation = warpedRotation.ToNormalized();

            // If we are NOT using a transform to offset the tracking
            if (_deviceOffsetMode != DeviceOffsetMode.Transform)
            {
                warpedPosition += warpedRotation * Vector3.up * deviceOffsetYAxis
                                + warpedRotation * Vector3.forward * deviceOffsetZAxis;
                warpedRotation *= Quaternion.Euler(deviceTiltXAxis, 0f, 0f);

                warpedRotation *= Quaternion.Euler(-90f, 180f, 0f);
            }
            else
            {
                warpedRotation *= Quaternion.Euler(-90f, 90f, 90f);
            }


#if !SVR
            // Use the mainCamera parent to transfrom the warped positions so the player can move around
            if (mainCamera.transform.parent != null)
            {
                leapTransform = new LeapTransform(
                  mainCamera.transform.parent.TransformPoint(warpedPosition).ToVector(),
                  mainCamera.transform.parent.TransformRotation(warpedRotation).ToLeapQuaternion(),
                  Vector.Ones * 1e-3f
                );
            }
            else
#endif
            {
                leapTransform = new LeapTransform(
                  warpedPosition.ToVector(),
                  warpedRotation.ToLeapQuaternion(),
                  Vector.Ones * 1e-3f
                );
            }

            leapTransform.MirrorZ();

            return leapTransform;
        }

        protected void transformHands(ref LeapTransform LeftHand, ref LeapTransform RightHand)
        {
            LeapTransform leapTransform = GetWarpedMatrix(0, false);
            LeftHand = new LeapTransform(leapTransform.TransformPoint(LeftHand.translation),
                                         leapTransform.TransformQuaternion(LeftHand.rotation));
            RightHand = new LeapTransform(leapTransform.TransformPoint(RightHand.translation),
                                          leapTransform.TransformQuaternion(RightHand.rotation));
        }

        protected void OnPreCullHandTransforms(Camera camera)
        {
            if (updateHandInPrecull)
            {
                //Don't update pre cull for preview, reflection, or scene view cameras
                if (camera == null)
                {
                    camera = mainCamera;
                }

                switch (camera.cameraType)
                {
                    case CameraType.Preview:
#if UNITY_2017_1_OR_NEWER
                    case CameraType.Reflection:
#endif
                    case CameraType.SceneView:
                        return;
                }

                if (Application.isPlaying && !manualUpdateHasBeenCalledSinceUpdate && _leapController != null)
                {

                    manualUpdateHasBeenCalledSinceUpdate = true;

                    //Find the left and/or right hand(s) to latch
                    Hand leftHand = null, rightHand = null;
                    LeapTransform precullLeftHand = LeapTransform.Identity;
                    LeapTransform precullRightHand = LeapTransform.Identity;
                    for (int i = 0; i < CurrentFrame.Hands.Count; i++)
                    {
                        Hand updateHand = CurrentFrame.Hands[i];
                        if (updateHand.IsLeft && leftHand == null)
                        {
                            leftHand = updateHand;
                        }
                        else if (updateHand.IsRight && rightHand == null)
                        {
                            rightHand = updateHand;
                        }
                    }

                    //Determine their new Transforms
                    var interpolationTime = CalculateInterpolationTime();
                    _leapController.GetInterpolatedLeftRightTransform(
                                      interpolationTime + (ExtrapolationAmount * 1000),
                                      interpolationTime - (BounceAmount * 1000),
                                      (leftHand != null ? leftHand.Id : 0),
                                      (rightHand != null ? rightHand.Id : 0),
                                      _currentDevice,
                                      out precullLeftHand,
                                      out precullRightHand);
                    bool leftValid = precullLeftHand.translation != Vector.Zero;
                    bool rightValid = precullRightHand.translation != Vector.Zero;
                    transformHands(ref precullLeftHand, ref precullRightHand);

                    //Calculate the delta Transforms
                    if (rightHand != null && rightValid)
                    {
                        _transformArray[0] =
                          Matrix4x4.TRS(precullRightHand.translation.ToVector3(),
                                        precullRightHand.rotation.ToQuaternion(),
                                        Vector3.one)
                          * Matrix4x4.Inverse(Matrix4x4.TRS(rightHand.PalmPosition.ToVector3(),
                                                            rightHand.Rotation.ToQuaternion(),
                                                            Vector3.one));
                    }
                    if (leftHand != null && leftValid)
                    {
                        _transformArray[1] =
                          Matrix4x4.TRS(precullLeftHand.translation.ToVector3(),
                                        precullLeftHand.rotation.ToQuaternion(),
                                        Vector3.one)
                          * Matrix4x4.Inverse(Matrix4x4.TRS(leftHand.PalmPosition.ToVector3(),
                                                            leftHand.Rotation.ToQuaternion(),
                                                            Vector3.one));
                    }

                    //Apply inside of the vertex shader
                    Shader.SetGlobalMatrixArray(HAND_ARRAY_GLOBAL_NAME, _transformArray);
                }
            }
        }


#if SVR
        /// <summary>
        /// Return the predicted display time as a leap time
        /// </summary>
        /// <returns></returns>
        private long GetPredictedDisplayTime_LeapTime()
        {

            long leapClock = 0;

            // Predicted display time for the current frame in milliseconds
            float displayTime_ms = SxrShim.GetPredictedDisplayTime(SystemInfo.graphicsMultiThreaded);

            if (_clockRebaser != IntPtr.Zero)
            {
                LeapC.RebaseClock(_clockRebaser, (long)displayTime_ms + _stopwatch.ElapsedMilliseconds, out leapClock);
            }

            return leapClock;
        }
#endif

        #endregion
    }
}