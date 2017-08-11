using System;
using UnityEngine;
using UnityEngine.Rendering;
using Leap.Unity.Glint.Internal;
#if UNITY_EDITOR
using System.Text;
#endif

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

    #region Public API

    private static int _requestWaitTimeInFrames = 1;
    /// <summary>
    /// Specify the number of frames Glint should wait before attempting to map GPU
    /// memory into CPU memory and perform the final memory copy of a GPU data request.
    /// 
    /// This value is 1 by default, and must be at least 1. Modifying this value will only
    /// affect subsequent requests initiated by RequestAsync.
    /// </summary>
    public static int requestWaitTimeInFrames {
      get { return _requestWaitTimeInFrames; }
      set { _requestWaitTimeInFrames = Mathf.Max(1, value); }
    }

    /// <summary>
    /// Requests that the GPU write pixel data from gpuTexture into cpuData. Once the GPU
    /// is ready, it will write data into cpuData. Glint will attempt to resolve this
    /// operation N frames later (default 1) depending on the current value of
    /// requestWaitTimeInFrames. Then it will retrieve the texture data, copy it into
    /// managed memory, and call onRetrieved.
    /// </summary>
    /// <remarks>
    /// Currently, the OpenGL implementation uses glMapBufferRange once Glint attempts to
    /// actually retrieve the requested texture data; glMapBufferRange maps GPU memory to
    /// CPU memory, and _BLOCKS_ until the GPU is finished with the requested memory if
    /// the GPU is still working with it. So if you are experiencing blocking, try
    /// increasing your requestWaitTimeInFrames. (TODO: Fix this with OpenGL fence sync
    /// objects.)
    /// </remarks>
    public static void RequestAsync(Texture gpuTexture, float[] cpuData,
                                    Action onRetrieved, int? overrideFrameWaitTime = null) {
#if UNITY_EDITOR
      if (!ValidateGraphicsDeviceType()) {
        return;
      }
#endif

      int waitTimeInFrames = overrideFrameWaitTime.HasValue ? overrideFrameWaitTime.Value
                                                            : requestWaitTimeInFrames;
      waitTimeInFrames = Mathf.Max(0, waitTimeInFrames);

      GlintRequestRunner.CreateRequest(gpuTexture, cpuData, onRetrieved, waitTimeInFrames);
    }

    #endregion

    #region Profiling

    public static class Profiling {

      public static float lastRenderMapMs = 0f;
      public static float lastRenderCopyMs = 0f;
      public static float lastMainCopyMs = 0f;

    }

    #endregion

  }

}
