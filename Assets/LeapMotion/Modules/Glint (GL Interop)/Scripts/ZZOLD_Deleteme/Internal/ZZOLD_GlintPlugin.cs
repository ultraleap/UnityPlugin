//using System;
//using System.Collections.Generic;
//using System.Runtime.InteropServices;
//using UnityEngine;

//namespace Leap.Unity.ZZOLD_GLINT.Internal {

//  /// <summary>
//  /// Wrapper and interface for the native Glint plugin. For general usage of Glint,
//  /// refer to the Glint static class.
//  /// </summary>
//  public static class ZZOLD_GlintPlugin {

//    #region DLLImport

//    [DllImport("Glint")]
//    private static extern int getLastStatus();

//    /// <summary>
//    /// Initiates a Glint texture request. This is the first step in the chain.
//    /// 
//    /// Should be called on Update.
//    /// </summary>
//    [DllImport("Glint")]
//    private static extern int RequestTextureData(IntPtr nativeTexPtr,
//                                                int width, int height, int pixelSize);

//    /// <summary>
//    /// Returns a function pointer to the render thread RequestTexture function.
//    /// 
//    /// This is the second step. This method needs to be called on the render thread,
//    /// and then some time should be allowed to pass to allow the GPU to process the
//    /// request.
//    /// </summary>
//    [DllImport("Glint")]
//    private static extern IntPtr GetRequestTextureEventFunc();

//    /// <summary>
//    /// Returns a function pointer to the render thread CopyTexture function.
//    /// 
//    /// This is the third step. This function should be called after some time has passed
//    /// from the RequestTexture call on the render thread.
//    /// 
//    /// Currently this function will BLOCK in OpenGL due to a Map() call. Let time pass
//    /// before calling this to prevent halting!
//    /// </summary>
//    [DllImport("Glint")]
//    private static extern IntPtr GetCopyTextureEventFunc();

//    /// <summary>
//    /// The fourth and final step in retrieving the texture data; it must be called on
//    /// the main Unity thread. This is ideally called as directly after the CopyTexture
//    /// render thread call as possible, since the data will be ready after the Copy call.
//    /// 
//    /// dataSize should be the length of data and should be width * height * pixelSize.
//    /// 
//    /// The profileBuffer is passed so the plugin can surface how long each step took
//    /// to the Unity application; it doesn't have anything to do with actually copying
//    /// texture data.
//    /// </summary>
//    [DllImport("Glint")]
//    private static extern int RetrieveTextureData(IntPtr nativeTexPtr,
//                                                  float[] data, int dataSize,
//                                                  float[] profileBuffer, int profileSize);

//    #endregion

//    private static Dictionary<Texture, IntPtr> _texturePointers
//                                               = new Dictionary<Texture, IntPtr>();

//    // Profiling.
//    private static float[] profilingDataBuffer = new float[3];

//    public static ZZOLD_Status GetLastStatus() {
//      return (ZZOLD_Status)getLastStatus();
//    }

//    public static void RequestTextureData(Texture texture) {
//      if (texture == null) {
//        Debug.LogError("[Glint] Cannot request data for null texture.");
//        return;
//      }

//      // TODO: Support more than just 4-float RGBA format.
//      int requestIdx = RequestTextureData(GetTexturePtr(texture), texture.width,
//                                                                  texture.height,
//                                                                  4 * sizeof(float));

//      if (requestIdx == -1) {
//        Debug.LogError("[Glint] Unable to request texture data. "
//                      + "Plugin status: " + (ZZOLD_Status)getLastStatus());
//        return;
//      }

//      // Call the RequestTextureEventFunc.
//      GL.IssuePluginEvent(GetRequestTextureEventFunc(), requestIdx);
//    }

//    public static bool RetrieveTextureData(Texture texture, ref float[] data) {
//      float f0, f1, f2;
//      return RetrieveTextureData(texture, ref data, out f0, out f1, out f2);
//    }

//    public static bool RetrieveTextureData(Texture texture, ref float[] data,
//                                           out float renderMapMs,
//                                           out float renderCopyMs,
//                                           out float mainCopyMs) {
//      renderMapMs = -1;
//      renderCopyMs = -1;
//      mainCopyMs = -1;

//      if (texture == null) {
//        Debug.LogError("[Glint] Cannot retrieve data for null texture.");
//        return false;
//      }
//      if (data == null) {
//        Debug.LogError("[Glint] Cannot retrieve data into null data.");
//        return false;
//      }

//      int resourceIdx = RetrieveTextureData(GetTexturePtr(texture),
//                                            data,
//                                            data.Length * sizeof(float),
//                                            profilingDataBuffer,
//                                            profilingDataBuffer.Length * sizeof(float));
//      renderMapMs = profilingDataBuffer[0];
//      renderCopyMs = profilingDataBuffer[1];
//      mainCopyMs = profilingDataBuffer[2];

//      ZZOLD_Status lastStatus = (ZZOLD_Status)getLastStatus();
//      if (lastStatus == ZZOLD_Status.Success_3_CopySuccessful) {
//        // We successfully retrieved the texture data!
//        return true;
//      }

//      if (resourceIdx == -1 && lastStatus != ZZOLD_Status.Success) {
//        Debug.LogError("[Glint] Error retrieving data."
//                      + "Plugin status: " + lastStatus);
//        return false;
//      }

//      // If we haven't gotten the texture data yet but we did get a valid resource index,
//      // the plugin is waiting for us to IssuePluginEvent so it can attempt actual
//      // retrieval on the render thread.
//      GL.IssuePluginEvent(GetCopyTextureEventFunc(), resourceIdx);

//      return false;
//    }

//    private static IntPtr GetTexturePtr(Texture texture) {
//      IntPtr ptr;
//      if (_texturePointers.TryGetValue(texture, out ptr)) {
//        return ptr;
//      }

//      ptr = texture.GetNativeTexturePtr();
//      _texturePointers[texture] = ptr;
//      return ptr;
//    }

//  }

//}
