using System.Collections.Generic;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  public static class SerializedObjectExtensions {

    /// <summary>
    /// Returns whether or not the given objects holds a serialized reference to a
    /// specific object.  Simply serializes the object and steps through all properties,
    /// so it is not a cheap operation.
    /// </summary>
    public static bool DoesReference(this Object obj, Object reference) {
      SerializedObject sobj = new SerializedObject(obj);
      var it = sobj.GetIterator();
      while (it.Next(true)) {
        if (it.propertyType == SerializedPropertyType.ObjectReference && it.objectReferenceValue == reference) {
          return true;
        }
      }
      return false;
    }
  }

  public static class AttachedObjectHandler {

    public static void Validate<T, K>(T t, ref K k) where T : Component where K : Component {
      if (k == null || t.gameObject == k.gameObject) {
        return;
      }

      T[] otherTs = k.gameObject.GetComponents<T>();
      bool didMoveT = true;
      foreach (var otherT in otherTs) {
        if (otherT.DoesReference(k)) {
          didMoveT = false;
          break;
        }
      }

      var unreferencedKs = getUnreferenced<T, K>(t);

      K newK;
      if (unreferencedKs.Count != 0) {
        newK = unreferencedKs[0];
        unreferencedKs.RemoveAt(0);
      } else {
        newK = InternalUtility.AddComponent(t.gameObject, k.GetType()) as K;
      }

      Undo.RecordObject(newK, "Updated serialized data on " + newK.name);
      EditorUtility.CopySerialized(k, newK);
      Undo.RecordObject(newK, "uddd");

      //If we moved the T, destroy the original K
      if (didMoveT) {
        InternalUtility.Destroy(k);
      }

      foreach (var unreferencedK in unreferencedKs) {
        InternalUtility.Destroy(unreferencedK);
      }
      
      Undo.RecordObject(t, "Changed attached value on " + t.name);

      k = newK;
    }

    public static void Validate<T, K>(T t, List<K> ks) where T : Component where K : Component {
      K mainK = ks.Query().NonNull().FirstOrDefault();

      if (mainK == null) {
        return;
      }

      GameObject mainKObj = mainK.gameObject;
      if (ks.Query().NonNull().Any(k => k.gameObject != mainKObj)) {
        Debug.LogError("Could not validate attached objects because they were on different gameObjects");
        return;
      }

      if (mainKObj == t.gameObject) {
        return;
      }

      T[] otherTs = mainKObj.GetComponents<T>();
      bool didMoveT = true;
      foreach (var otherT in otherTs) {
        if (otherT.DoesReference(mainK)) {
          didMoveT = false;
          break;
        }
      }

      var unreferencedKs = getUnreferenced<T, K>(t);

      List<K> newKs = new List<K>();
      foreach (var oldK in ks) {
        K newK = null;

        //Try to get a new K from the list of unreferenced Ks
        for (int i = 0; i < unreferencedKs.Count; i++) {
          //Type must match exactly
          if (unreferencedKs[i].GetType() == oldK.GetType()) {
            newK = unreferencedKs[i];
            unreferencedKs.RemoveAt(i);
            break;
          }
        }

        if (newK == null) {
          newK = InternalUtility.AddComponent(t.gameObject, oldK.GetType()) as K;
        }

        Undo.RecordObject(newK, "Updated serialized data on " + newK.name);
        EditorUtility.CopySerialized(oldK, newK);
        Undo.RecordObject(newK, "uhhhh");

        newKs.Add(newK);
      }

      if (didMoveT) {
        foreach (var oldK in ks) {
          InternalUtility.Destroy(oldK);
        }
      }

      foreach (var unreferencedK in unreferencedKs) {
        InternalUtility.Destroy(unreferencedK);
      }

      Undo.RecordObject(t, "Changed attached value on " + t.name);

      ks.Clear();
      ks.AddRange(newKs);
    }

    private static List<K> getUnreferenced<T, K>(T t) where T : Component where K : Component {
      var existingsKs = new List<K>(t.gameObject.GetComponents<K>());
      var otherTs = t.gameObject.GetComponents<T>();
      foreach (var otherT in otherTs) {
        if (otherT == t) continue;

        for (int i = existingsKs.Count; i-- != 0;) {
          if (otherT.DoesReference(existingsKs[i])) {
            existingsKs.RemoveAt(i);
          }
        }
      }

      return existingsKs;
    }
  }
}
