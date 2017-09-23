

using System;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Rendering;

namespace Leap.Unity.Glint {

  public static class Glint {

    #region Supported Graphics Devices

    private static GraphicsDeviceType[] supportedGraphicsDevices = new GraphicsDeviceType[] {
      GraphicsDeviceType.OpenGLES3,
      GraphicsDeviceType.OpenGLCore
    };

    /// <summary>
    /// Returns whether or not the currently-utilized graphics device is supported by
    /// Glint.
    /// </summary>
    public static bool ValidateGraphicsDeviceType() {
      for (int i = 0; i < supportedGraphicsDevices.Length; i++) {
        if (SystemInfo.graphicsDeviceType == supportedGraphicsDevices[i]) {
          return true;
        }
      }
#if UNITY_EDITOR
      StringBuilder sb = new StringBuilder();
      sb.Append("[Glint] Unsupported graphics type detected: ");
      sb.Append(SystemInfo.graphicsDeviceType);
      sb.Append("; supported graphics types are: ");
      for (int i = 0; i < supportedGraphicsDevices.Length; i++) {
        sb.Append(supportedGraphicsDevices[i]);
        if (i != supportedGraphicsDevices.Length - 1) {
          sb.Append(", ");
          if (i == supportedGraphicsDevices.Length - 2) {
            sb.Append("and ");
          }
        }
      }
      Debug.LogError(sb.ToString());
#endif
      return false;
    }

    #endregion


    #region Plugin Initialization

    static Glint() {
      // Register debug logging callback.
      GlintPlugin.SetDebugLogFunc(PluginDebugLogFunc);

      // Register request result callback.
      GlintPlugin.SetRequestResultCallbackFunc(PluginRequestResultFunc);
    }

    #endregion


    #region Request Results

    public class Request {
      public byte[] resultData;
      public Action onRequestResult;
    }

    private static Dictionary<int, Request> s_requests = new Dictionary<int, Request>();

    public static void PluginRequestResultFunc(int requestId,
                                               IntPtr resultDataBytes,
                                               int numBytes) {
      if (requestId == -1) {
        Debug.LogError("Got a result callback for a request with an invalid request "
                     + "ID; unable to continue the request.");
        return;
      }

      Request trackedRequest = new Request();
      if (!s_requests.TryGetValue(requestId, out trackedRequest)) {
        Debug.LogError("No tracked request exists on the Unity side for request ID "
                     + requestId);
        return;
      }

      if (resultDataBytes == IntPtr.Zero) {
        Debug.LogError("Unable to copy request data: No valid pointer provided from "
                     + "native plugin.");
        return;
      }

      if (trackedRequest.resultData == null) {
        Debug.LogError("Unable to copy request data: No valid byte array reference "
                     + "exists on the Unity side for request ID " + requestId);
        return;
      }

      Marshal.Copy(resultDataBytes, trackedRequest.resultData, 0, numBytes);

      if (trackedRequest.onRequestResult == null) {
        Debug.LogError("Request ID " + requestId + " successfully copied result data, "
                     + "but no callback for this request was specified.");
        return;
      }

      trackedRequest.onRequestResult();

      s_requests.Remove(requestId);
    }

    #endregion


    #region Public API

    /// <summary>
    /// Creates an asynchronous Glint request to download texture data as a byte array
    /// from GPU memory to a CPU memory location specified by resultDataToFill.
    /// 
    /// When the request has completed, onRequestResult will be fired on the main thread
    /// as soon as possible.
    /// 
    /// Only one texture download request can be active for a given texture at one time.
    /// </summary>
    public static void RequestTextureDownload(Texture texture,
                                              byte[] resultDataToFill,
                                              Action onRequestResult) {
#if UNITY_EDITOR
      if (!ValidateGraphicsDeviceType()) {
        return;
      }
#endif

      GlintPluginRunner.EnsureRunnerExists();

      IntPtr texHandle = GetTextureHandle(texture);
      if (texHandle == IntPtr.Zero) {
        Debug.Log("Aborting texture download request; unable to retrieve native texture "
                + "handle.");
      }

      int bytesPerPixel = GetBytesPerPixel(texture);
      if (bytesPerPixel == -1) {
        Debug.Log("Aborting texture download request; unable to retrieve texture format "
                + "bytes per pixel.");
      }

      int requestId = GlintPlugin.RequestTextureDownload(texHandle,
                                                         texture.width,
                                                         texture.height,
                                                         GetBytesPerPixel(texture));

      if (requestId == -1) {
        Debug.Log("Aborting request: Tried to add the request, but received an invalid "
                + "request ID from native code.");
      }

      var trackedRequest = new Request() {
        resultData      = resultDataToFill,
        onRequestResult = onRequestResult
      };
      s_requests.Add(requestId, trackedRequest);
    }

    #endregion


    #region Support


    #region Textures

    private static Dictionary<Texture, IntPtr> _texturePointers
                                                 = new Dictionary<Texture, IntPtr>();

    /// <summary>
    /// Returns a texture handle for this texture, or IntPtr.zero if the method was
    /// unable to retrieve the texture's handle.
    /// </summary>
    private static IntPtr GetTextureHandle(Texture texture) {
      IntPtr ptr = IntPtr.Zero;

      if (texture == null) {
        Debug.LogError("Unable to get texture handle for null texture.");
        return ptr;
      }

      if (_texturePointers.TryGetValue(texture, out ptr)) {
        return ptr;
      }

      ptr = texture.GetNativeTexturePtr();
      _texturePointers[texture] = ptr;
      return ptr;
    }

    /// <summary>
    /// Returns the number of bytes per pixel for this texture, or -1 if the method was
    /// unable to retrieve the number of bytes per pixel (or if the texture has a format
    /// that contains a non-integer number of bytes per pixel, which is currently
    /// unsupported).
    /// </summary>
    public static int GetBytesPerPixel(Texture texture) {
      RenderTexture renderTex = texture as RenderTexture;
      if (renderTex != null) {
        return GetBytesPerPixel(renderTex);
      }

      Texture2D texture2D = texture as Texture2D;
      if (texture2D != null) {
        return GetBytesPerPixel(texture2D);
      }

      Debug.Log("Unsupported texture type: " + texture.GetType().Name);
      return -1;
    }

    /// <summary>
    /// Returns the number of bytes per pixel of the RenderTexture. Doesn't support all
    /// RenderTextureFormats -- in which case, returns -1.
    /// </summary>
    public static int GetBytesPerPixel(RenderTexture renderTex) {
      switch (renderTex.format) {
        case RenderTextureFormat.ARGB32:
          return 4;
        case RenderTextureFormat.ARGB4444:
          return 2;
        case RenderTextureFormat.ARGBFloat:
          return 16;
        case RenderTextureFormat.ARGBHalf:
          return 8;
        case RenderTextureFormat.BGRA32:
          return 4;
        default:
          Debug.Log("Unsupported render texture format: " + renderTex.format);
          return -1;
      }
    }

    /// <summary>
    /// Returns the number of bytes per pixel of the Texture2D. Doesn't support all
    /// TextureFormats -- in which case, returns -1.
    /// </summary>
    public static int GetBytesPerPixel(Texture2D texture2D) {
      switch (texture2D.format) {
        case TextureFormat.Alpha8:
          return 1;
        case TextureFormat.ARGB32:
          return 4;
        case TextureFormat.ARGB4444:
          return 2;
        case TextureFormat.ATC_RGBA8:
          return 1;
        case TextureFormat.RGBA32:
          return 4;
        case TextureFormat.RGBA4444:
          return 2;
        case TextureFormat.RGBAFloat:
          return 16;
        case TextureFormat.RGBAHalf:
          return 8;
        default:
          Debug.Log("Unsupported Texture2D format: " + texture2D.format);
          return -1;
      }
    }

    #endregion


    #region Debug Logging

    private static void PluginDebugLogFunc(IntPtr utf8BytesPtr, int numBytes) {
      byte[] utf8Bytes = new byte[numBytes];
      Marshal.Copy(utf8BytesPtr, utf8Bytes, 0, numBytes);

      string message = System.Text.Encoding.UTF8.GetString(utf8Bytes);
      Debug.LogError(message);
    }

    #endregion


    #endregion

  }

}