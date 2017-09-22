using System;
using UnityEngine;

namespace Leap.Unity.Glint {

  /// <summary>
  /// Updates requests, calling into the Glint native plugin at least once every update
  /// on the main thread and once each render on the render thread.
  /// </summary>
  [AddComponentMenu("")]
  class GlintPluginRunner : MonoBehaviour {

    public const string RUNNER_OBJECT_NAME = "__Glint Plugin Runner__";

    #region Static Loading

    private static GlintPluginRunner s_instance = null;
    static GlintPluginRunner() {
      if (s_instance == null && Application.isPlaying) {
        var obj = new GameObject(RUNNER_OBJECT_NAME);
        DontDestroyOnLoad(obj);
        s_instance = obj.AddComponent<GlintPluginRunner>();
      }
    }

    #endregion

    private IntPtr renderUpdateCallbackPtr = IntPtr.Zero;

    private void Awake() {
      renderUpdateCallbackPtr = GlintPlugin.GetUpdateRenderThreadFunc();
    }

    // Call Update_MainThread during both Update and LateUpdate to try and catch requsts'
    // first updates on the same frame as the request, and to try and catch as many
    // completed Request callbacks as early as possible.
    // TODO: Script execution order: The plugin runner should run very early.

    private void Update() {
      GlintPlugin.Update_MainThread();
    }

    private void LateUpdate() {
      GlintPlugin.Update_MainThread();

      // This will call the Update_RenderThread() function in the native plugin
      // from the render thread.
      GL.IssuePluginEvent(renderUpdateCallbackPtr, 0 /* unused eventID */);
    }

  }

}