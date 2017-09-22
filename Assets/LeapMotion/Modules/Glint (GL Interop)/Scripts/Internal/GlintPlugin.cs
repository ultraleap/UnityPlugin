using System;
using System.Runtime.InteropServices;

namespace Leap.Unity.Glint {

  class GlintPlugin {

    #region Updates

    [DllImport("Glint")]
    public static extern void Update_MainThread();

    [DllImport("Glint")]
    public static extern void Update_RenderThread();

    [DllImport("Glint")]
    public static extern IntPtr GetUpdateRenderThreadFunc();

    #endregion


    #region Debug Logging

    public delegate void DebugLogFunc(IntPtr utf8BytesPtr, int numBytes);

    [DllImport("Glint")]
    public static extern void SetDebugLogFunc(DebugLogFunc debugLogFunc);

    #endregion


    #region Request Result Callbacks

    public delegate void RequestResultFunc(int requestId,
                                           IntPtr resultDataBytesPtr,
                                           int numBytes);

    [DllImport("Glint")]
    public static extern void SetRequestResultCallbackFunc(RequestResultFunc resultFunc);

    #endregion


    #region Glint Requests

    [DllImport("Glint")]
    public static extern int RequestTextureDownload(IntPtr texHandle,
                                                    int width,
                                                    int height,
                                                    int bytesPerPixel);

    #endregion

  }

}