using System;
using UnityEngine;

namespace Leap.Unity.Glint {

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

    private IntPtr renderUpdateCallbackPtr = IntPtr.Zero;

    private void Awake() {
      renderUpdateCallbackPtr = GlintPlugin.GetUpdateRenderThreadFunc();
    }

    private void Update() {
      GlintPlugin.Update_MainThread();

      GL.IssuePluginEvent(renderUpdateCallbackPtr, 0 /* unused eventID */);
    }

  }

}