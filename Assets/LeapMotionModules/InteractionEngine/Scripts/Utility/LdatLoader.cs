using UnityEngine;
using System;
using System.IO;
using System.Runtime.InteropServices;

namespace Leap.Unity.Interaction.CApi {

  public static class LdatLoader {
    /// <summary>
    /// Loads an ldat file loaded at a path relative to the streaming assets folder.  The file
    /// is stored into the provided scene info struct.  This method returns a disposable that 
    /// releases the ldat resources when disposed.
    /// </summary>
    public static IDisposable LoadLdat(ref INTERACTION_SCENE_INFO info, string ldatPath) {
      string ldatFullPath = Path.Combine(getStreamingAssetsPath(), ldatPath);

      WWW ldat = new WWW(ldatFullPath);
      while (!ldat.isDone) {
        System.Threading.Thread.Sleep(1);
      }

      if (!string.IsNullOrEmpty(ldat.error)) {
        throw new Exception(ldat.error + ": " + ldatFullPath);
      }

      info.ldatData = Marshal.AllocHGlobal(ldat.bytes.Length);

      info.ldatSize = (uint)ldat.bytes.Length;
      info.ldatData = Marshal.AllocHGlobal(ldat.bytes.Length);
      Marshal.Copy(ldat.bytes, 0, info.ldatData, ldat.bytes.Length);

      return new ReleaseUponDispose(info.ldatData);
    }

    private static string getStreamingAssetsPath() {
#if UNITY_STANDALONE_WIN || UNITY_WSA || UNITY_EDITOR_WIN
      return "file:///" + Application.streamingAssetsPath;
#else
    return Application.streamingAssetsPath;
#endif
    }

    private class ReleaseUponDispose : IDisposable {
      private IntPtr _ptr;

      public ReleaseUponDispose(IntPtr ptr) {
        _ptr = ptr;
      }

      public void Dispose() {
        Marshal.FreeHGlobal(_ptr);
      }
    }
  }
}
