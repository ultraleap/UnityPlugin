using UnityEngine;
using UnityEditor;
using System.Linq;
using System.Collections.Generic;

public static class Hotkeys {

  [MenuItem("GameObject/Make Group %g")]
  public static void MakeGroup() {
    GameObject[] objs = Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
    if (objs.Length == 0) {
      return;
    }

    Transform first = objs[0].transform;

    List<Transform> hierarchy = new List<Transform>();

    Transform parent = first.parent;
    while (parent != null) {
      hierarchy.Add(parent);
      parent = parent.parent;
    }

    int index = 0;
    parent = hierarchy.FirstOrDefault();

    if (parent != null) {
      foreach (var obj in objs) {
        Transform t = obj.transform;
        while (!t.IsChildOf(parent)) {
          index++;
          if (index >= hierarchy.Count) {
            parent = null;
            break;
          } else {
            parent = hierarchy[index];
          }
        }
      }
    }

    GameObject root = new GameObject("Root");
    root.transform.SetParent(parent);
    root.transform.localPosition = Vector3.zero;
    root.transform.localRotation = Quaternion.identity;
    root.transform.localScale = Vector3.one;

    foreach (var obj in objs) {
      obj.transform.SetParent(root.transform, worldPositionStays: true);
    }

    Selection.activeGameObject = root;
  }

  [MenuItem("GameObject/Promote %t")]
  public static void Remove() {
    GameObject[] objs = Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
    if (objs.Length == 0) {
      return;
    }

    List<GameObject> toSelect = new List<GameObject>();
    foreach (var obj in objs) {
      List<Transform> children = new List<Transform>();
      foreach (Transform child in obj.transform) {
        children.Add(child);
      }

      foreach (var child in children) {
        toSelect.Add(child.gameObject);
        child.SetParent(obj.transform.parent, worldPositionStays: true);
      }
      Object.DestroyImmediate(obj);
    }

    Selection.objects = toSelect.ToArray();
  }

  [MenuItem("GameObject/Reset Local Transform %e")]
  public static void ResetAll() {
    GameObject[] objs = Selection.GetFiltered<GameObject>(SelectionMode.ExcludePrefab | SelectionMode.OnlyUserModifiable | SelectionMode.Editable);
    if (objs.Length == 0) {
      return;
    }

    foreach (var obj in objs) {
      obj.transform.localPosition = Vector3.zero;
      obj.transform.localRotation = Quaternion.identity;
      obj.transform.localScale = Vector3.one;
    }
  }
}
