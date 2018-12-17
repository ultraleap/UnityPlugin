/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using System;
using System.Collections;
using System.Collections.Generic;
using Leap.Unity.Query;
using Leap.Unity.Attributes;
using LeapInternal;

namespace Leap.Unity {

  /// <summary>
  /// Acquires images from a LeapServiceProvider and uploads image data as
  /// shader global data for use by any shaders that render those images.
  /// 
  /// Note: To use the LeapImageRetriever, you must be on version 2.1 or newer
  /// and you must enable "Allow Images" in your Leap Motion settings.
  /// </summary>
  [RequireComponent(typeof(LeapServiceProvider))]
  public class LeapImageRetriever : MonoBehaviour {

    public const string GLOBAL_COLOR_SPACE_GAMMA_NAME =
      "_LeapGlobalColorSpaceGamma";
    public const string GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME =
      "_LeapGlobalGammaCorrectionExponent";
    public const string GLOBAL_CAMERA_PROJECTION_NAME =
      "_LeapGlobalProjection";

    public const int IMAGE_WARNING_WAIT = 10;
    public const int LEFT_IMAGE_INDEX = 0;
    public const int RIGHT_IMAGE_INDEX = 1;
    public const float IMAGE_SETTING_POLL_RATE = 2.0f;

    public LeapPostProcess postProcessing;

    [SerializeField]
    [FormerlySerializedAs("gammaCorrection")]
    private float _gammaCorrection = 1.0f;

    Dictionary<int, IntPtr> _deviceHandles = new Dictionary<int, IntPtr>();
    public IntPtr GetDeviceHandle(int deviceID) {
      IntPtr handle = IntPtr.Zero;
      if(_deviceHandles.TryGetValue(deviceID, out handle)) {
        return handle;
      }
      return handle;
    }

    [Range(1, 3)]
    public uint shaderDataDevice = 1;
    [NonSerialized]
    public int updateSpecificDeviceIDTexture = 0;

    private LeapServiceProvider _provider;
    private EyeTextureData _backingEyeTextureData;
    private EyeTextureData _eyeTextureData {
      get {
        if (_backingEyeTextureData == null) {
          _backingEyeTextureData = new EyeTextureData(this);
        }
        return _backingEyeTextureData;
      }
    }

    /// <summary>
    /// Images that we have requested from the service. Requested in Update and
    /// retrieved in OnPreRender.
    /// </summary>
    protected ProduceConsumeBuffer<Image> _imageQueue =
      new ProduceConsumeBuffer<Image>(256);
    protected Image _currentImage = null;

    public EyeTextureData TextureData { get { return _eyeTextureData; } }

    #if UNITY_EDITOR
    void OnValidate() {
      if (Application.isPlaying) {
        ApplyGammaCorrectionValues();
      } else {
        EyeTextureData.ResetGlobalShaderValues();
      }
    }
    #endif

    private void Awake() {
      _provider = GetComponent<LeapServiceProvider>();
      if (_provider == null) {
        _provider = GetComponentInChildren<LeapServiceProvider>();
      }

      //Enable pooling to reduce overhead of images
      LeapInternal.MemoryManager.EnablePooling = true;

      ApplyGammaCorrectionValues();
    }

    private void OnEnable() {
      subscribeToService();
    }

    private void OnDisable() {
      unsubscribeFromService();
    }

    private void OnDestroy() {
      StopAllCoroutines();
      Controller controller = _provider.GetLeapController();
      if (controller != null) {
        _provider.GetLeapController().DistortionChange -= onDistortionChange;
      }
    }

    Image[] latestDeviceImages = new Image[1];
    private void LateUpdate() {
      Frame imageFrame = _provider.CurrentFrame;

      _currentImage = null;

      if (latestDeviceImages.Length != _deviceHandles.Count) {
        latestDeviceImages = new Image[_deviceHandles.Count];
      }

      /* Use the most recent image that is not newer than the current frame
       * This means that the shown image might be slightly older than the
       * current frame if for some reason a frame arrived before an image did.
       * 
       * Usually however, this is just important when robust mode is enabled.
       * At that time, image ids never line up with tracking ids.
       */
      Image potentialImage;
      while (_imageQueue.TryPeek(out potentialImage)) {
        if (latestDeviceImages.Length == 1 && potentialImage.SequenceId > imageFrame.Id) break;

        if (potentialImage.DeviceID == shaderDataDevice) _currentImage = potentialImage;
        if (potentialImage.DeviceID > 0) latestDeviceImages[potentialImage.DeviceID - 1] = potentialImage;
        _imageQueue.TryDequeue();
      }
  /*  }

    private void OnPreRender() {*/
      if (_currentImage != null) {
        if (_eyeTextureData.CheckStale(_currentImage)) {
          _eyeTextureData.Reconstruct(_currentImage);
        }
      }

      for(int i = 0; i < latestDeviceImages.Length; i++) {
        if (latestDeviceImages[i] != null) {
          bool applyTexture = updateSpecificDeviceIDTexture == 0 || latestDeviceImages[i].DeviceID == updateSpecificDeviceIDTexture;
          _eyeTextureData.UpdateTextures(latestDeviceImages[i], postProcessing, applyTexture);
        }
      }
    }

    public bool UpdateDeviceIDTexture(int deviceID) {
      if (latestDeviceImages .Length > deviceID - 1 && latestDeviceImages[deviceID - 1] != null) {
        bool applyTexture = latestDeviceImages[deviceID - 1].DeviceID == deviceID;
        _eyeTextureData.UpdateTextures(latestDeviceImages[deviceID - 1], postProcessing, applyTexture);
        return true;
      } else {
        return false;
      }
    }

    private void subscribeToService() {
      if (_serviceCoroutine != null) {
        return;
      }

      _serviceCoroutine = StartCoroutine(serviceCoroutine());
    }

    private void unsubscribeFromService() {
      if (_serviceCoroutine != null) {
        StopCoroutine(_serviceCoroutine);
        _serviceCoroutine = null;
      }

      var controller = _provider.GetLeapController();
      if (controller != null) {
        controller.ClearPolicy(Controller.PolicyFlag.POLICY_IMAGES);
        controller.ImageReady -= onImageReady;
        controller.DistortionChange -= onDistortionChange;
      }
    }

    private Coroutine _serviceCoroutine = null;
    private IEnumerator serviceCoroutine() {
      Controller controller = null;
      do {
        controller = _provider.GetLeapController();
        yield return null;
      } while (controller == null);

      controller.SetPolicy(Controller.PolicyFlag.POLICY_IMAGES);
      controller.ImageReady += onImageReady;
      controller.DistortionChange += onDistortionChange;
    }

    private void initializeDeviceHandles() {
      var connection = Connection.GetConnection(0);
      for (int i = 0; i < connection.Devices.Count; i++) {
        bool containsHandle = _deviceHandles.ContainsValue(connection.Devices[i].Handle);
        bool containsKey = _deviceHandles.ContainsKey(i + 1);

        if (!containsHandle) {
          if (containsKey) _deviceHandles.Remove(i + 1);
          _deviceHandles.Add(i + 1, connection.Devices[i].Handle);

          LeapTextureData textureData = new LeapTextureData();
          textureData.leapImageRetriever = this;
          _eyeTextureData.TextureData.Add(textureData);
        }
      }
    }

    private void onImageReady(object sender, ImageEventArgs args) {
      Image image = args.image;
      if(!_deviceHandles.ContainsKey((int)image.DeviceID)) initializeDeviceHandles();
      _imageQueue.TryEnqueue(image);
    }

    public void ApplyGammaCorrectionValues() {
      float gamma = 1f;
      if (QualitySettings.activeColorSpace != ColorSpace.Linear) {
        gamma = -Mathf.Log10(Mathf.GammaToLinearSpace(0.1f));
      }
      Shader.SetGlobalFloat(GLOBAL_COLOR_SPACE_GAMMA_NAME, gamma);
      Shader.SetGlobalFloat(GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME, 1.0f / _gammaCorrection);
    }

    void onDistortionChange(object sender, LeapEventArgs args) {
      //Distortion changes every time an image from a different device comes in, 
      //so suppress this when using multiple devices
      if(_eyeTextureData.TextureData.Count == 1) _eyeTextureData.MarkStale();
    }

    // Helper classes

    public class LeapTextureData {
      public LeapImageRetriever leapImageRetriever;

      private Texture2D _combinedTexture = null;

      public Texture2D CombinedTexture {
        get {
          return _combinedTexture;
        }
      }

      public bool CheckStale(Image image) {
        if (_combinedTexture == null ) {
          return true;
        }

        if (image.Width != _combinedTexture.width || image.Height * 2 != _combinedTexture.height) {
          return true;
        }

        if (_combinedTexture.format != getTextureFormat(image)) {
          return true;
        }

        return false;
      }

      public void Reconstruct(Image image, string globalShaderName, string pixelSizeName, bool setShaderGlobal = true) {
        int combinedWidth = image.Width;
        int combinedHeight = image.Height * 2;

        TextureFormat format = getTextureFormat(image);

        if (_combinedTexture != null) {
          DestroyImmediate(_combinedTexture);
        }

        _combinedTexture = new Texture2D(combinedWidth, combinedHeight, format, false, true);
        _combinedTexture.wrapMode = TextureWrapMode.Clamp;
        _combinedTexture.filterMode = FilterMode.Bilinear;
        _combinedTexture.name = globalShaderName;
        _combinedTexture.hideFlags = HideFlags.DontSave;

        if (setShaderGlobal) {
          Shader.SetGlobalTexture(globalShaderName, _combinedTexture);
          Shader.SetGlobalVector(pixelSizeName, new Vector2(1.0f / image.Width, 1.0f / image.Height));
        }
      }

      public void UpdateTexture(Image image, LeapPostProcess postProcessing, bool applyTexture = true) {
        byte[] array = image.Data(Image.CameraType.LEFT);

        if(postProcessing != null) postProcessing.OnNewImage(image, array);

        if (applyTexture) {
          if (_combinedTexture != null) {
            _combinedTexture.LoadRawTextureData(array);
            _combinedTexture.Apply();
          } else {
            //Create the Texture2D if it does not exist yet
            Reconstruct(image, "_LeapGlobalRawTexture", "", false);
          }
        }
      }

      private TextureFormat getTextureFormat(Image image) {
        switch (image.Format) {
          case Image.FormatType.INFRARED:
            return TextureFormat.Alpha8;
          default:
            throw new Exception("Unexpected image format " + image.Format + "!");
        }
      }

      private int bytesPerPixel(TextureFormat format) {
        switch (format) {
          case TextureFormat.Alpha8:
            return 1;
          default:
            throw new Exception("Unexpected texture format " + format);
        }
      }
    }

    public class LeapDistortionData {
      private Texture2D _combinedTexture = null;
      public LeapImageRetriever leapImageRetriever;

      public Texture2D CombinedTexture {
        get {
          return _combinedTexture;
        }
      }

      public bool CheckStale() {
        return _combinedTexture == null;
      }

      public void Reconstruct(Image image, string shaderName) {
        int combinedWidth = image.DistortionWidth / 2;
        int combinedHeight = image.DistortionHeight * 2;

        if (_combinedTexture != null) {
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

      private void addDistortionData(Image image, Color32[] colors, int startIndex) {
        float[] distortionData = image.Distortion(Image.CameraType.LEFT).
                                       Query().
                                       Concat(image.Distortion(Image.CameraType.RIGHT)).
                                       ToArray();

        for (int i = 0; i < distortionData.Length; i += 2) {
          byte b0, b1, b2, b3;
          encodeFloat(distortionData[i], out b0, out b1);
          encodeFloat(distortionData[i + 1], out b2, out b3);
          colors[i / 2 + startIndex] = new Color32(b0, b1, b2, b3);
        }
      }

      private void encodeFloat(float value, out byte byte0, out byte byte1) {
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

    public class EyeTextureData {
      private const string GLOBAL_RAW_TEXTURE_NAME = "_LeapGlobalRawTexture";
      private const string GLOBAL_DISTORTION_TEXTURE_NAME = "_LeapGlobalDistortion";
      private const string GLOBAL_RAW_PIXEL_SIZE_NAME = "_LeapGlobalRawPixelSize";

      public readonly List<LeapTextureData> TextureData;
      public readonly LeapDistortionData Distortion;
      private bool _isStale = false;

      public static void ResetGlobalShaderValues() {
        Texture2D empty = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
        empty.name = "EmptyTexture";
        empty.hideFlags = HideFlags.DontSave;
        empty.SetPixel(0, 0, new Color(0, 0, 0, 0));

        Shader.SetGlobalTexture(GLOBAL_RAW_TEXTURE_NAME, empty);
        Shader.SetGlobalTexture(GLOBAL_DISTORTION_TEXTURE_NAME, empty);
      }

      public EyeTextureData(LeapImageRetriever leapImageRetriever) {
        TextureData = new List<LeapTextureData>();
        Distortion = new LeapDistortionData();
        Distortion.leapImageRetriever = leapImageRetriever; 
      }

      public bool CheckStale(Image image) {
        return TextureData[((int)image.DeviceID)-1].CheckStale(image) ||
               Distortion.CheckStale() ||
               _isStale;
      }

      public void MarkStale() {
        _isStale = true;
      }

      public void Reconstruct(Image image, bool setShaderGlobal = true) {
        TextureData[((int)image.DeviceID) - 1].Reconstruct(image, GLOBAL_RAW_TEXTURE_NAME, GLOBAL_RAW_PIXEL_SIZE_NAME, setShaderGlobal);
        Distortion.Reconstruct(image, GLOBAL_DISTORTION_TEXTURE_NAME);
        _isStale = false;
      }

      public void UpdateTextures(Image image, LeapPostProcess postProcessing, bool applyTexture = true) {
        TextureData[((int)image.DeviceID) - 1].UpdateTexture(image, postProcessing, applyTexture);
      }
    }

  }
}
