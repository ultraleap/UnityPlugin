using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
using UnityEditorInternal;
#endif

#if UNITY_EDITOR
[InitializeOnLoad]
#endif
public static class InternalUtility {

#if UNITY_EDITOR
  static InternalUtility() {
    EditorApplication.update += destroyLoop;
  }
#endif

  /// <summary>
  /// Call this method from within OnDestroy.  The action will only be invoked if
  /// the object was deleted during EDIT MODE, and that destruction was not caused
  /// by a scene change, playmode change, or application quit.
  /// </summary>
  /// <param name="action"></param>
  public static void InvokeIfUserDestroyed(Action action) {
    if (EditorApplication.isPlayingOrWillChangePlaymode ||
        EditorApplication.isPlaying ||
        EditorApplication.isPaused) {
      return;
    }

    _invokeList.Add(new InvokeStruct(action));
  }

#if UNITY_EDITOR
  private static List<Component> _cachedComponentList0 = new List<Component>();
  private static List<Component> _cachedComponentList1 = new List<Component>();
  private static List<InvokeStruct> _invokeList = new List<InvokeStruct>();
  public static bool TryMoveComponent<T>(T source, GameObject toGameObject, out T result)
    where T : Component {
    ComponentUtility.CopyComponent(source);

    toGameObject.GetComponents(_cachedComponentList0);
    ComponentUtility.PasteComponentAsNew(toGameObject);
    toGameObject.GetComponents(_cachedComponentList1);

    foreach (var component in _cachedComponentList1) {
      if (!_cachedComponentList0.Contains(component) && component is T) {
        Destroy(source);
        result = component as T;
        return true;
      }
    }

    result = null;
    return false;
  }
#endif

  private static List<UnityEngine.Object> toDestroy = new List<UnityEngine.Object>();
  public static void Destroy(UnityEngine.Object obj) {
#if UNITY_EDITOR
    if (Application.isPlaying) {
      UnityEngine.Object.Destroy(obj);
    } else {
      toDestroy.Add(obj);
    }
#else
    Object.Destroy(obj);
#endif
  }

  private static void destroyLoop() {
    if (_invokeList.Count != 0) {
      var scene = SceneManager.GetActiveScene();
      foreach (var action in _invokeList) {
        if (action.scene == scene) {
          try {
            action.action();
          } catch (Exception e) {
            Debug.LogException(e);
          }
        }
      }
      _invokeList.Clear();
    }

    if (toDestroy.Count != 0) {
      for (int i = 0; i < toDestroy.Count; i++) {
        var obj = toDestroy[i];
        if (obj != null) {
          UnityEngine.Object.DestroyImmediate(obj);
        }
      }
      toDestroy.Clear();
    }
  }

  private struct InvokeStruct {
    public Scene scene;
    public Action action;

    public InvokeStruct(Action action) {
      this.action = action;
      scene = SceneManager.GetActiveScene();
    }
  }
}
