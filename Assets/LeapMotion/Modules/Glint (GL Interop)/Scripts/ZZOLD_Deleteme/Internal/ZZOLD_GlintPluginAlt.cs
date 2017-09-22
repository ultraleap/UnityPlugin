using System.Collections;
using System.Collections.Generic;
using System.Runtime.InteropServices;
using UnityEngine;

public static class ZZOLD_GlintPluginAlt {

  //#region DLL Interop

  //[DllImport("Glint")]
  //private static extern int GetRequestStatus(IntPtr nativeTexPtr);

  ///// <summary>
  ///// Initiates a Glint texture request. This is the first step in the chain.
  ///// 
  ///// Should be called on Update.
  ///// </summary>
  //[DllImport("Glint")]
  //private static extern int RequestTextureData(IntPtr nativeTexPtr,
  //                                            int width, int height, int pixelSize);

  ///// <summary>
  ///// Returns a function pointer to the render thread RequestTexture function.
  ///// 
  ///// This is the second step. This method needs to be called on the render thread,
  ///// and then some time should be allowed to pass to allow the GPU to process the
  ///// request.
  ///// </summary>
  //[DllImport("Glint")]
  //private static extern IntPtr GetRequestTextureEventFunc();

  ///// <summary>
  ///// Returns a function pointer to the render thread CopyTexture function.
  ///// 
  ///// This is the third step. This function should be called after some time has passed
  ///// from the RequestTexture call on the render thread.
  ///// 
  ///// Currently this function will BLOCK in OpenGL due to a Map() call. Let time pass
  ///// before calling this to prevent halting!
  ///// </summary>
  //[DllImport("Glint")]
  //private static extern IntPtr GetCopyTextureEventFunc();

  ///// <summary>
  ///// The fourth and final step in retrieving the texture data; it must be called on
  ///// the main Unity thread. This is ideally called as directly after the CopyTexture
  ///// render thread call as possible, since the data will be ready after the Copy call.
  ///// 
  ///// dataSize should be the length of data and should be width * height * pixelSize.
  ///// 
  ///// The profileBuffer is passed so the plugin can surface how long each step took
  ///// to the Unity application; it doesn't have anything to do with actually copying
  ///// texture data.
  ///// </summary>
  //[DllImport("Glint")]
  //private static extern int RetrieveTextureData(IntPtr nativeTexPtr,
  //                                              float[] data, int dataSize,
  //                                              float[] profileBuffer, int profileSize);

}
