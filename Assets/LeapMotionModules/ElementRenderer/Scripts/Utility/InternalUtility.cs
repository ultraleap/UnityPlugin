using System.Collections.Generic;
using UnityEngine;
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

#if UNITY_EDITOR
  private static List<Component> _cachedComponentList0 = new List<Component>();
  private static List<Component> _cachedComponentList1 = new List<Component>();
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

  private static List<Object> toDestroy = new List<Object>();
  public static void Destroy(Object obj) {
#if UNITY_EDITOR
    if (Application.isPlaying) {
      Object.Destroy(obj);
    } else {
      toDestroy.Add(obj);
    }
#else
    Object.Destroy(obj);
#endif
  }

  private static void destroyLoop() {
    if (toDestroy.Count != 0) {
      for (int i = 0; i < toDestroy.Count; i++) {
        var obj = toDestroy[i];
        if (obj != null) {
          Object.DestroyImmediate(obj);
        }
      }
      toDestroy.Clear();
    }
  }
}
