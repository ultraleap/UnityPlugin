using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using UnityEditor.Callbacks;

[InitializeOnLoad]
public static class InternalUtility {

  static InternalUtility() {
    EditorApplication.update += destroyLoop;
  }

#if UNITY_EDITOR
  private static List<Component> _cachedComponentList0 = new List<Component>();
  private static List<Component> _cachedComponentList1 = new List<Component>();
  public static bool TryMoveComponent(Component source, GameObject toGameObject, out Component result) {
    UnityEditorInternal.ComponentUtility.CopyComponent(source);

    toGameObject.GetComponents(_cachedComponentList0);
    UnityEditorInternal.ComponentUtility.PasteComponentAsNew(toGameObject);
    toGameObject.GetComponents(_cachedComponentList1);

    foreach (var component in _cachedComponentList1) {
      if (!_cachedComponentList0.Contains(component)) {
        Destroy(source);
        result = component;
        return true;
      }
    }

    result = null;
    return false;
  }

  private static List<Object> toDestroy = new List<Object>();
  public static void Destroy(Object obj) {
    toDestroy.Add(obj);
  }

  private static void destroyLoop() {
    if (toDestroy.Count != 0) {
      foreach (var obj in toDestroy) {
        Object.DestroyImmediate(obj);
      }
      toDestroy.Clear();
    }
  }
#endif

}
