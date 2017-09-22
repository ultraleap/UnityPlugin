using System;
using System.Runtime.InteropServices;

namespace Leap.Unity.Glint {

  class GlintPlugin {

    [DllImport("Glint")]
    public static extern void Update_MainThread();

    [DllImport("Glint")]
    public static extern void Update_RenderThread();

    [DllImport("Glint")]
    public static extern IntPtr GetUpdateRenderThreadFunc();

  }

}