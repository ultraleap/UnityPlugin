/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2016.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/
using UnityEngine;
using UnityEngine.Serialization;
using System;
using System.Collections;
using Leap;

namespace Leap.Unity {
  // To use the LeapImageRetriever you must be on version 2.1+
  // and enable "Allow Images" in the Leap Motion settings.

  /** LeapImageRetriever acquires images from a LeapServiceProvider and uploads them to gpu for use by shaders */
  [RequireComponent(typeof(Camera))]
  public class LeapImageRetriever : MonoBehaviour {
    public const string GLOBAL_COLOR_SPACE_GAMMA_NAME = "_LeapGlobalColorSpaceGamma";
    public const string GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME = "_LeapGlobalGammaCorrectionExponent";
    public const string GLOBAL_CAMERA_PROJECTION_NAME = "_LeapGlobalProjection";
    public const int IMAGE_WARNING_WAIT = 10;
    public const int LEFT_IMAGE_INDEX = 0;
    public const int RIGHT_IMAGE_INDEX = 1;
    public const float IMAGE_SETTING_POLL_RATE = 2.0f;

    [SerializeField]
    LeapServiceProvider _provider;

    [SerializeField]
    [FormerlySerializedAs("gammaCorrection")]
    private float _gammaCorrection = 1.0f;

    [SerializeField]
    protected long ImageTimeout = 9000; //microseconds

    private EyeTextureData _eyeTextureData = new EyeTextureData();

    //Image that we have requested from the service.  Are requested in Update and retrieved in OnPreRender
    protected Image _requestedImage = new Image();

    protected bool imagesEnabled = true;
    private bool checkingImageState = false;

    public EyeTextureData TextureData {
      get {
        return _eyeTextureData;
      }
    }

    public class LeapTextureData {
      private Texture2D _combinedTexture = null;
      private byte[] _intermediateArray = null;

      public Texture2D CombinedTexture {
        get {
          return _combinedTexture;
        }
      }

      public bool CheckStale(Image image) {
        if (_combinedTexture == null || _intermediateArray == null) {
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

      public void Reconstruct(Image image, string globalShaderName, string pixelSizeName) {
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

        _intermediateArray = new byte[combinedWidth * combinedHeight * bytesPerPixel(format)];

        Shader.SetGlobalTexture(globalShaderName, _combinedTexture);
        Shader.SetGlobalVector(pixelSizeName, new Vector2(1.0f / image.Width, 1.0f / image.Height));
      }

      public void UpdateTexture(Image image) {
        Array.Copy(image.Data, 0, _intermediateArray, 0, _intermediateArray.Length);
        _combinedTexture.LoadRawTextureData(_intermediateArray);
        _combinedTexture.Apply();
      }

      private TextureFormat getTextureFormat(Image image) {
        switch (image.Format) {
          case Image.FormatType.INFRARED:
            return TextureFormat.Alpha8;
          case Image.FormatType.IBRG:
          case (Image.FormatType)4:       //Hack, Dragonfly still reports a weird format type
            return TextureFormat.RGBA32;
          default:
            throw new System.Exception("Unexpected image format " + image.Format + "!");
        }
      }

      private int bytesPerPixel(TextureFormat format) {
        switch (format) {
          case TextureFormat.Alpha8:
            return 1;
          case TextureFormat.RGBA32:
          case TextureFormat.BGRA32:
          case TextureFormat.ARGB32:
            return 4;
          default:
            throw new System.Exception("Unexpected texture format " + format);
        }
      }
    }

    public class LeapDistortionData {
      private Texture2D _combinedTexture = null;

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
        float[] distortionData = image.Distortion;

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
      private const string IR_SHADER_VARIANT_NAME = "LEAP_FORMAT_IR";
      private const string RGB_SHADER_VARIANT_NAME = "LEAP_FORMAT_RGB";
      private const string GLOBAL_BRIGHT_TEXTURE_NAME = "_LeapGlobalBrightnessTexture";
      private const string GLOBAL_RAW_TEXTURE_NAME = "_LeapGlobalRawTexture";
      private const string GLOBAL_DISTORTION_TEXTURE_NAME = "_LeapGlobalDistortion";
      private const string GLOBAL_BRIGHT_PIXEL_SIZE_NAME = "_LeapGlobalBrightnessPixelSize";
      private const string GLOBAL_RAW_PIXEL_SIZE_NAME = "_LeapGlobalRawPixelSize";

      public readonly LeapTextureData BrightTexture;
      public readonly LeapTextureData RawTexture;
      public readonly LeapDistortionData Distortion;
      private bool _isStale = false;

      public static void ResetGlobalShaderValues() {
        Texture2D empty = new Texture2D(1, 1, TextureFormat.ARGB32, false, false);
        empty.name = "EmptyTexture";
        empty.hideFlags = HideFlags.DontSave;
        empty.SetPixel(0, 0, new Color(0, 0, 0, 0));

        Shader.SetGlobalTexture(GLOBAL_BRIGHT_TEXTURE_NAME, empty);
        Shader.SetGlobalTexture(GLOBAL_RAW_TEXTURE_NAME, empty);
        Shader.SetGlobalTexture(GLOBAL_DISTORTION_TEXTURE_NAME, empty);
      }

      public EyeTextureData() {
        BrightTexture = new LeapTextureData();
        RawTexture = new LeapTextureData();
        Distortion = new LeapDistortionData();
      }

      public bool CheckStale(Image bright, Image raw) {
        return BrightTexture.CheckStale(bright) ||
               RawTexture.CheckStale(raw) ||
               Distortion.CheckStale() ||
               _isStale;
      }

      public void MarkStale() {
        _isStale = true;
      }

      public void Reconstruct(Image bright, Image raw) {
        BrightTexture.Reconstruct(bright, GLOBAL_BRIGHT_TEXTURE_NAME, GLOBAL_BRIGHT_PIXEL_SIZE_NAME);
        RawTexture.Reconstruct(raw, GLOBAL_RAW_TEXTURE_NAME, GLOBAL_RAW_PIXEL_SIZE_NAME);

        Distortion.Reconstruct(raw, GLOBAL_DISTORTION_TEXTURE_NAME);

        switch (raw.Format) {
          case Image.FormatType.INFRARED:
            Shader.DisableKeyword(RGB_SHADER_VARIANT_NAME);
            Shader.EnableKeyword(IR_SHADER_VARIANT_NAME);
            break;
          case (Image.FormatType)4:
            Shader.DisableKeyword(IR_SHADER_VARIANT_NAME);
            Shader.EnableKeyword(RGB_SHADER_VARIANT_NAME);
            break;
          default:
            Debug.LogWarning("Unexpected format type " + raw.Format);
            break;
        }

        _isStale = false;
      }

      public void UpdateTextures(Image bright, Image raw) {
        BrightTexture.UpdateTexture(bright);
        RawTexture.UpdateTexture(raw);
      }
    }

#if UNITY_EDITOR
    void OnValidate() {
      if (Application.isPlaying) {
        ApplyGammaCorrectionValues();
      } else {
        EyeTextureData.ResetGlobalShaderValues();
      }
    }
#endif

    void Start() {
      if (_provider == null) {
        Debug.LogWarning("Cannot use LeapImageRetriever if there is no LeapProvider!");
        enabled = false;
        return;
      }

      ApplyGammaCorrectionValues();
      ApplyCameraProjectionValues(GetComponent<Camera>());
    }

    void OnEnable() {
      Controller controller = _provider.GetLeapController();
      if (controller != null) {
        onController(controller);
      } else {
        StartCoroutine(waitForController());
      }

      LeapVRCameraControl.OnLeftPreRender += ApplyCameraProjectionValues;
      LeapVRCameraControl.OnRightPreRender += ApplyCameraProjectionValues;
    }

    void OnDisable() {
      StopAllCoroutines();
      Controller controller = _provider.GetLeapController();
      if (controller != null) {
        _provider.GetLeapController().DistortionChange -= onDistortionChange;
      }

      LeapVRCameraControl.OnLeftPreRender -= ApplyCameraProjectionValues;
      LeapVRCameraControl.OnRightPreRender -= ApplyCameraProjectionValues;
    }

    void OnDestroy() {
      StopAllCoroutines();
      Controller controller = _provider.GetLeapController();
      if (controller != null) {
        _provider.GetLeapController().DistortionChange -= onDistortionChange;
      }
    }

    void OnPreRender() {
      if (imagesEnabled) {
        Controller controller = _provider.GetLeapController();
        long start = controller.Now();
        while (!_requestedImage.IsComplete) {
          if (controller.Now() - start > ImageTimeout) break;
        }
        if (_requestedImage.IsComplete) {
          if (_eyeTextureData.CheckStale(_requestedImage, _requestedImage)) {
            _eyeTextureData.Reconstruct(_requestedImage, _requestedImage);
          }
          _eyeTextureData.UpdateTextures(_requestedImage, _requestedImage);
        } else if (!checkingImageState) {
          StartCoroutine(checkImageMode());
        }
      }
    }

    void Update() {
      if (imagesEnabled) {
        Frame imageFrame = _provider.CurrentFrame;
        Controller controller = _provider.GetLeapController();
        _requestedImage = controller.RequestImages(imageFrame.Id, Image.ImageType.DEFAULT);
      } else if (!checkingImageState) {
        StartCoroutine(checkImageMode());
      }
    }

    private IEnumerator waitForController() {
      Controller controller = null;
      do {
        controller = _provider.GetLeapController();
        yield return null;
      } while (controller == null);
      onController(controller);
    }

    private IEnumerator checkImageMode() {
      checkingImageState = true;
      yield return new WaitForSeconds(IMAGE_SETTING_POLL_RATE);
      _provider.GetLeapController().Config.Get<Int32>("images_mode", delegate (Int32 enabled) {
        this.imagesEnabled = enabled == 0 ? false : true;
        checkingImageState = false;
      });
    }

    private void onController(Controller controller) {
      controller.DistortionChange += onDistortionChange;
      controller.Connect += delegate {
        _provider.GetLeapController().Config.Get("images_mode", (Int32 enabled) => {
          this.imagesEnabled = enabled == 0 ? false : true;
        });
      };
      if (!checkingImageState) {
        StartCoroutine(checkImageMode());
      }
    }

    public void ApplyGammaCorrectionValues() {
      float gamma = 1f;
      if (QualitySettings.activeColorSpace != ColorSpace.Linear) {
        gamma = -Mathf.Log10(Mathf.GammaToLinearSpace(0.1f));
      }
      Shader.SetGlobalFloat(GLOBAL_COLOR_SPACE_GAMMA_NAME, gamma);
      Shader.SetGlobalFloat(GLOBAL_GAMMA_CORRECTION_EXPONENT_NAME, 1.0f / _gammaCorrection);
    }

    public void ApplyCameraProjectionValues(Camera camera) {
      //These parameters are used during undistortion of the images to ensure they
      //line up properly with the scene
      Vector4 projection = new Vector4();
      projection.x = camera.projectionMatrix[0, 2];
      projection.y = 0f;
      projection.z = camera.projectionMatrix[0, 0];
      projection.w = camera.projectionMatrix[1, 1];
      Shader.SetGlobalVector(GLOBAL_CAMERA_PROJECTION_NAME, projection);
    }

    void onDistortionChange(object sender, LeapEventArgs args) {
      _eyeTextureData.MarkStale();
    }
  }
}
