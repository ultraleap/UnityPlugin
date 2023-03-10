/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap.Unity.Query;
using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Serialization;

namespace Leap.Unity
{

    /// <summary>
    /// Acquires images from a LeapServiceProvider and uploads image data as shader global
    /// data for use by any shaders that render those images.
    /// 
    /// Note: To use the LeapImageRetriever, you must be on version 2.1 or newer and you
    /// must enable "Allow Images" in your Leap Motion settings.
    /// </summary>
    public class LeapImageRetriever : MonoBehaviour
    {
        public const string GLOBAL_COLOR_SPACE_GAMMA_NAME = "_LeapGlobalColorSpaceGamma";
        public const string GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME = "_LeapGlobalGammaCorrectionExponent";
        public const string GLOBAL_CAMERA_PROJECTION_NAME = "_LeapGlobalProjection";
        public const int IMAGE_WARNING_WAIT = 10;
        public const int LEFT_IMAGE_INDEX = 0;
        public const int RIGHT_IMAGE_INDEX = 1;
        public const float IMAGE_SETTING_POLL_RATE = 2.0f;

        [SerializeField]
        [FormerlySerializedAs("gammaCorrection")]
        private float _gammaCorrection = 1.0f;

        [SerializeField] private LeapServiceProvider _provider;
        private EyeTextureData _eyeTextureData = new EyeTextureData();

        //Image that we have requested from the service.  Are requested in Update and retrieved in OnPreRender
        protected ProduceConsumeBuffer<Image> _imageQueue = new ProduceConsumeBuffer<Image>(128);
        protected Image _currentImage = null;

        private long _prevSequenceId;
        private bool _needQueueReset;

        // If image IDs from the libtrack server do not reset with the Visualiser, it triggers out-of-sequence
        // checks and we lose images. Detecting this and setting an offset allows us to compensate.
        private long _frameIDOffset = -1;

        public EyeTextureData TextureData
        {
            get
            {
                return _eyeTextureData;
            }
        }

        public class LeapTextureData
        {
            private Texture2D _combinedTexture = null;
            private byte[] _intermediateArray = null;

            public Texture2D CombinedTexture
            {
                get
                {
                    return _combinedTexture;
                }
            }

            public bool CheckStale(Image image)
            {
                if (_combinedTexture == null || _intermediateArray == null)
                {
                    return true;
                }

                if (image.Width != _combinedTexture.width || image.Height * 2 != _combinedTexture.height)
                {
                    return true;
                }

                if (_combinedTexture.format != getTextureFormat(image))
                {
                    return true;
                }

                return false;
            }

            public void Reconstruct(Image image, string globalShaderName, string pixelSizeName)
            {
                int combinedWidth = image.Width;
                int combinedHeight = image.Height * 2;

                TextureFormat format = getTextureFormat(image);

                if (_combinedTexture != null)
                {
                    DestroyImmediate(_combinedTexture);
                }

                _combinedTexture = new Texture2D(combinedWidth, combinedHeight, format, false, true);
                _combinedTexture.wrapMode = TextureWrapMode.Clamp;
                _combinedTexture.filterMode = FilterMode.Bilinear;
                _combinedTexture.name = globalShaderName;
                _combinedTexture.hideFlags = HideFlags.DontSave;

                _intermediateArray = new byte[combinedWidth * combinedHeight * bytesPerPixel(format)];

                Shader.SetGlobalTexture(globalShaderName, _combinedTexture);
                Shader.SetGlobalVector(pixelSizeName, new Vector2(1.0f / image.Width, 1.0f / image.Height));
            }

            public void UpdateTexture(Image image)
            {
                _combinedTexture.LoadRawTextureData(image.Data(Image.CameraType.LEFT));
                _combinedTexture.Apply();
            }

            private TextureFormat getTextureFormat(Image image)
            {
                switch (image.Format)
                {
                    case Image.FormatType.INFRARED:
                        return TextureFormat.Alpha8;
                    default:
                        throw new Exception("Unexpected image format " + image.Format + "!");
                }
            }

            private int bytesPerPixel(TextureFormat format)
            {
                switch (format)
                {
                    case TextureFormat.Alpha8:
                        return 1;
                    default:
                        throw new Exception("Unexpected texture format " + format);
                }
            }
        }

        public class LeapDistortionData
        {
            private Texture2D _combinedTexture = null;

            public Texture2D CombinedTexture
            {
                get
                {
                    return _combinedTexture;
                }
            }

            public bool CheckStale()
            {
                return _combinedTexture == null;
            }

            public void Reconstruct(Image image, string shaderName)
            {
                int combinedWidth = image.DistortionWidth / 2;
                int combinedHeight = image.DistortionHeight * 2;

                if (_combinedTexture != null)
                {
                    DestroyImmediate(_combinedTexture);
                }

                Color32[] colorArray = new Color32[combinedWidth * combinedHeight];
                _combinedTexture = new Texture2D(combinedWidth, combinedHeight, TextureFormat.RGBA32, false, true);
                _combinedTexture.filterMode = FilterMode.Bilinear;
                _combinedTexture.wrapMode = TextureWrapMode.Clamp;
                _combinedTexture.hideFlags = HideFlags.DontSave;

                addDistortionData(image, colorArray, 0);

                _combinedTexture.SetPixels32(colorArray);
                _combinedTexture.Apply();

                Shader.SetGlobalTexture(shaderName, _combinedTexture);
            }

            private void addDistortionData(Image image, Color32[] colors, int startIndex)
            {
                float[] distortionData = image.Distortion(Image.CameraType.LEFT).
                                               Query().
                                               Concat(image.Distortion(Image.CameraType.RIGHT)).
                                               ToArray();

                for (int i = 0; i < distortionData.Length; i += 2)
                {
                    byte b0, b1, b2, b3;
                    encodeFloat(distortionData[i], out b0, out b1);
                    encodeFloat(distortionData[i + 1], out b2, out b3);
                    colors[i / 2 + startIndex] = new Color32(b0, b1, b2, b3);
                }
            }

            private void encodeFloat(float value, out byte byte0, out byte byte1)
            {
                // The distortion range is -0.6 to +1.7. Normalize to range [0..1).
                value = (value + 0.6f) / 2.3f;
                float enc_0 = value;
                float enc_1 = value * 255.0f;

                enc_0 = enc_0 - (int)enc_0;
                enc_1 = enc_1 - (int)enc_1;

                enc_0 -= 1.0f / 255.0f * enc_1;

                byte0 = (byte)(enc_0 * 256.0f);
                byte1 = (byte)(enc_1 * 256.0f);
            }
        }

        public class EyeTextureData
        {
            private const string GLOBAL_RAW_TEXTURE_NAME = "_LeapGlobalRawTexture";
            private const string GLOBAL_DISTORTION_TEXTURE_NAME = "_LeapGlobalDistortion";
            private const string GLOBAL_RAW_PIXEL_SIZE_NAME = "_LeapGlobalRawPixelSize";

            public readonly LeapTextureData TextureData;
            public readonly LeapDistortionData Distortion;
            private bool _isStale = false;

            public static void ResetGlobalShaderValues()
            {
                Texture2D empty = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
                empty.name = "EmptyTexture";
                empty.hideFlags = HideFlags.DontSave;
                empty.SetPixel(0, 0, new Color(0, 0, 0, 0));

                Shader.SetGlobalTexture(GLOBAL_RAW_TEXTURE_NAME, empty);
                Shader.SetGlobalTexture(GLOBAL_DISTORTION_TEXTURE_NAME, empty);
            }

            public EyeTextureData()
            {
                TextureData = new LeapTextureData();
                Distortion = new LeapDistortionData();
            }

            public bool CheckStale(Image image)
            {
                return TextureData.CheckStale(image) ||
                       Distortion.CheckStale() ||
                       _isStale;
            }

            public void MarkStale()
            {
                _isStale = true;
            }

            public void Reconstruct(Image image)
            {
                TextureData.Reconstruct(image, GLOBAL_RAW_TEXTURE_NAME, GLOBAL_RAW_PIXEL_SIZE_NAME);
                Distortion.Reconstruct(image, GLOBAL_DISTORTION_TEXTURE_NAME);
                _isStale = false;
            }

            public void UpdateTextures(Image image)
            {
                TextureData.UpdateTexture(image);
            }
        }

#if UNITY_EDITOR
        void OnValidate()
        {
            if (Application.isPlaying)
            {
                ApplyGammaCorrectionValues();
            }
            else
            {
                EyeTextureData.ResetGlobalShaderValues();
            }
        }
#endif

        private void Awake()
        {
            if (_provider == null)
            {
                Debug.Log("Provider not assigned");
                this.enabled = false;
                return;
            }

            Camera.onPreRender -= OnCameraPreRender;
            Camera.onPreRender += OnCameraPreRender;

            //Enable pooling to reduce overhead of images
            LeapInternal.MemoryManager.EnablePooling = true;

            ApplyGammaCorrectionValues();
#if UNITY_2019_3_OR_NEWER
            //SRP require subscribing to RenderPipelineManagers
            if(UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null) {
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= onBeginRendering;
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering += onBeginRendering;
            }
#endif
        }

        private void OnEnable()
        {
            subscribeToService();
        }

        private void OnDisable()
        {
            unsubscribeFromService();
        }

        private void OnDestroy()
        {
            StopAllCoroutines();

            Controller controller = _provider.GetLeapController();
            if (controller != null)
            {
                controller.DistortionChange -= onDistortionChange;
                controller.Disconnect -= onDisconnect;
                controller.ImageReady -= onImageReady;
                controller.FrameReady -= onFrameReady;
            }
#if UNITY_2019_3_OR_NEWER
            //SRP require subscribing to RenderPipelineManagers
            if (UnityEngine.Rendering.GraphicsSettings.renderPipelineAsset != null)
            {
                UnityEngine.Rendering.RenderPipelineManager.beginCameraRendering -= onBeginRendering;
            }
#endif
        }

        private void LateUpdate()
        {

            var xrProvider = _provider as LeapXRServiceProvider;
            if (xrProvider != null)
            {
                if (xrProvider.mainCamera == null) { return; }
            }

            Frame imageFrame = _provider.CurrentFrame;

            _currentImage = null;

            if (_needQueueReset)
            {
                while (_imageQueue.TryDequeue()) { }
                _needQueueReset = false;
            }

            /* Use the most recent image that is not newer than the current frame
             * This means that the shown image might be slightly older than the current
             * frame if for some reason a frame arrived before an image did.
             * 
             * Usually however, this is just important when robust mode is enabled.
             * At that time, image ids never line up with tracking ids.
             */
            Image potentialImage;
            while (_imageQueue.TryPeek(out potentialImage))
            {
                if (_frameIDOffset == -1) // Initialise to incoming image ID
                {
                    _frameIDOffset = potentialImage.SequenceId + 1;

                    if (_frameIDOffset != 0)
                    {
                        Debug.LogWarning("Incoming image ID was " + potentialImage.SequenceId + " but we expected zero. Compensating..");
                    }
                }
                if (potentialImage.SequenceId > imageFrame.Id)
                {
                    break;
                }

                _currentImage = potentialImage;
                _imageQueue.TryDequeue();
            }
        }

        public void Reconstruct()
        {
            if (_currentImage != null)
            {
                if (_eyeTextureData.CheckStale(_currentImage))
                {
                    _eyeTextureData.Reconstruct(_currentImage);
                }

                _eyeTextureData.UpdateTextures(_currentImage);
            }
        }

        private void OnCameraPreRender(Camera cam)
        {
            if (_currentImage != null)
            {
                if (_eyeTextureData.CheckStale(_currentImage))
                {
                    _eyeTextureData.Reconstruct(_currentImage);
                }

                _eyeTextureData.UpdateTextures(_currentImage);
            }
        }

#if UNITY_2019_3_OR_NEWER
        private void onBeginRendering(UnityEngine.Rendering.ScriptableRenderContext scriptableRenderContext, Camera camera)
        {
            OnCameraPreRender(camera);
        }
#endif

        private void subscribeToService()
        {
            if (_serviceCoroutine != null)
            {
                return;
            }

            _serviceCoroutine = StartCoroutine(serviceCoroutine());
        }

        private void unsubscribeFromService()
        {
            if (_serviceCoroutine != null)
            {
                StopCoroutine(_serviceCoroutine);
                _serviceCoroutine = null;
            }

            var controller = _provider.GetLeapController();
            if (controller != null)
            {
                controller.ClearPolicy(Controller.PolicyFlag.POLICY_IMAGES);
                controller.Disconnect -= onDisconnect;
                controller.ImageReady -= onImageReady;
                controller.DistortionChange -= onDistortionChange;
                controller.FrameReady -= onFrameReady;
            }
            _eyeTextureData.MarkStale();
        }

        private Coroutine _serviceCoroutine = null;
        private IEnumerator serviceCoroutine()
        {
            Controller controller = null;
            do
            {
                controller = _provider.GetLeapController();
                yield return null;
            } while (controller == null);

            controller.FrameReady += onFrameReady;
            controller.Disconnect += onDisconnect;
            controller.ImageReady += onImageReady;
            controller.DistortionChange += onDistortionChange;
        }

        private void onImageReady(object sender, ImageEventArgs args)
        {
            Image image = args.image;

            if (!_imageQueue.TryEnqueue(image))
            {
                Debug.LogWarning("Image buffer filled up. This is unexpected and means images are being provided faster than " +
                                 "LeapImageRetriever can consume them.  This might happen if the application has stalled " +
                                 "or we recieved a very high volume of images suddenly.");
                _needQueueReset = true;
            }

            if (image.SequenceId < _prevSequenceId)
            {
                //We moved back in time, so we should reset the queue so it doesn't get stuck
                //on the previous image, which will be very old.
                //this typically happens when the service is restarted while the application is running.
                _needQueueReset = true;
            }
            _prevSequenceId = image.SequenceId;
        }

        private void onFrameReady(object sender, FrameEventArgs args)
        {
            var controller = _provider.GetLeapController();
            if (controller != null)
            {
                controller.FrameReady -= onFrameReady;
                controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            }
        }

        private void onDisconnect(object sender, ConnectionLostEventArgs args)
        {
            var controller = _provider.GetLeapController();
            if (controller != null)
            {
                controller.FrameReady += onFrameReady;
                controller.ClearPolicy(Controller.PolicyFlag.POLICY_IMAGES);
            }
        }

        public void ApplyGammaCorrectionValues()
        {
            float gamma = 1f;
            if (QualitySettings.activeColorSpace != ColorSpace.Linear)
            {
                gamma = -Mathf.Log10(Mathf.GammaToLinearSpace(0.1f));
            }
            Shader.SetGlobalFloat(GLOBAL_COLOR_SPACE_GAMMA_NAME, gamma);
            Shader.SetGlobalFloat(GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME, 1.0f / _gammaCorrection);
        }

        void onDistortionChange(object sender, LeapEventArgs args)
        {
            _eyeTextureData.MarkStale();
        }
    }
}