/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity {

#if UNITY_EDITOR
  [InitializeOnLoad]
#endif
  public static class InternalUtility {

#if UNITY_EDITOR
    public static Action OnAnySave;

    private static List<UnityEngine.Object> toDestroy = new List<UnityEngine.Object>();
    private static List<InvokeStruct> _invokeList = new List<InvokeStruct>();

    static InternalUtility() {
      EditorApplication.update += destroyLoop;
    }

    public static bool IsPrefab(Component component) {
      return PrefabUtility.GetPrefabType(component.gameObject) == PrefabType.Prefab;
    }

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
#endif

    /// <summary>
    /// This method functions in the same was as gameObject.AddComponent, except it
    /// includes Undo functionality by default when running in the editor.
    /// </summary>
    public static T AddComponent<T>(GameObject obj) where T : Component {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        return Undo.AddComponent<T>(obj);
      } else
#endif
      {
        return obj.AddComponent<T>();
      }
    }

    /// <summary>
    /// This method functions in the same was as gameObject.AddComponent, except it
    /// includes Undo functionality by default when running in the editor.
    /// </summary>
    public static Component AddComponent(GameObject obj, Type type) {
#if UNITY_EDITOR
      if (!Application.isPlaying) {
        return Undo.AddComponent(obj, type);
      } else
#endif
      {
        return obj.AddComponent(type);
      }
    }

    /// <summary>
    /// This method functions in the same way as Object.Destroy(), except it
    /// includes Undo functionality by default when running in the editor, and
    /// is safe to call from within 'forbidden' callbacks like OnValidate.  
    /// 
    /// Like Object.Destroy this method doesn't actually destroy the object 
    /// right away, but instead destroys it at a slightly later point in time.
    /// </summary>
    public static void Destroy(UnityEngine.Object obj) {
#if UNITY_EDITOR
      if (Application.isPlaying) {
        UnityEngine.Object.Destroy(obj);
      } else {
        toDestroy.Add(obj);
      }
#else
    UnityEngine.Object.Destroy(obj);
#endif
    }

#if UNITY_EDITOR
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
            Undo.DestroyObjectImmediate(obj);
          }
        }
        toDestroy.Clear();
      }
    }

    private static void dispatchOnAnySave() {
      if (OnAnySave != null) {
        OnAnySave();
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

    public class SaveWatcher : UnityEditor.AssetModificationProcessor {
      static string[] OnWillSaveAssets(string[] paths) {
        EditorApplication.delayCall += dispatchOnAnySave;
        return paths;
      }
    }
#endif
  }
}
