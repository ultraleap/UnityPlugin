using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using System;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  [CanEditMultipleObjects]
  [CustomEditor(typeof(InteractionBehaviour), true)]
  public class InteractionBehaviourEditor : InteractionBehaviourBaseEditor {

    protected override void OnEnable() {
      base.OnEnable();

      specifyCustomDecorator("_graphicalAnchor", graphicalAnchor);
      specifyCustomDrawer("_pushingEnabled", pushingDrawer);
    }

    private void graphicalAnchor(SerializedProperty prop) {
      UnityEngine.Object objectValue = prop.objectReferenceValue;
      if (objectValue == null) {
        using (new GUILayout.HorizontalScope()) {
          EditorGUILayout.HelpBox("It is recommended to use the graphical anchor to improve interaction fidelity.", MessageType.Warning);
          if (GUILayout.Button("Auto-Fix")) {
            autoGenerateGraphicalAnchor(prop);
          }
        }
      } else {
        Transform graphicalAnchor = objectValue as Transform;
        bool isIncorrect = false;
        isIncorrect |= graphicalAnchor.transform.localPosition != Vector3.zero;
        isIncorrect |= graphicalAnchor.transform.localRotation != Quaternion.identity;
        if (isIncorrect) {
          using (new GUILayout.HorizontalScope()) {
            EditorGUILayout.HelpBox("Graphical anchor should have no position or rotation offset.", MessageType.Warning);
            if (GUILayout.Button("Auto-Fix")) {
              graphicalAnchor.localPosition = Vector3.zero;
              graphicalAnchor.localRotation = Quaternion.identity;
              prop.objectReferenceValue = graphicalAnchor;
            }
          }
        }
      }
    }

    private void pushingDrawer(SerializedProperty prop) {
      bool shouldShow = false;

      if (_interactionBehaviour == null) {
        shouldShow = true;
      } else {
        Rigidbody rigidbody = _interactionBehaviour.GetComponent<Rigidbody>();
        if (!rigidbody.isKinematic) {
          shouldShow = true;
        }
      }

      if (shouldShow) {
        EditorGUILayout.PropertyField(prop);
      }
    }

    private void autoGenerateGraphicalAnchor(SerializedProperty prop) {
      //Create graphical anchor
      GameObject graphicalAnchor = null;

      //Increment group to ensure that our operations are self contained within a single Undo group.
      Undo.IncrementCurrentGroup();

      try {
        graphicalAnchor = new GameObject("Graphical Anchor");
        graphicalAnchor.transform.SetParent(_interactionBehaviour.transform);
        graphicalAnchor.transform.localPosition = Vector3.zero;
        graphicalAnchor.transform.localRotation = Quaternion.identity;
        graphicalAnchor.transform.localScale = Vector3.one;
        graphicalAnchor.transform.SetSiblingIndex(0);
        Undo.RegisterCreatedObjectUndo(graphicalAnchor, "Created Graphical Anchor");

        prop.objectReferenceValue = graphicalAnchor;

        var oldToNew = new Dictionary<UnityEngine.Object, UnityEngine.Object>();

        //Deep copy the object to a new object
        deepCopyGraphicalComponents(_interactionBehaviour.gameObject, graphicalAnchor, graphicalAnchor, oldToNew);

        //Point references from old components to the new ones
        repairReferences(_interactionBehaviour.gameObject, oldToNew);

        //Destroy all old components
        destroyOldComponents(oldToNew);

        //Cleanup empty objects 
        cleanupEmptyObjects(_interactionBehaviour.gameObject, graphicalAnchor);
      } catch (Exception e) {
        //If any exceptions are encountered, Undo back to initial state
        Undo.PerformUndo();
        Debug.LogError("Could not perform automatic graphical anchor creation!");
        Debug.LogException(e);
      }
    }

    private void deepCopyGraphicalComponents(GameObject obj,
                                             GameObject anchor,
                                             GameObject graphicalAnchor,
                                             Dictionary<UnityEngine.Object, UnityEngine.Object> oldToNew) {
      if (obj == graphicalAnchor) {
        return;
      }

      GameObject newObj = new GameObject(obj.name);
      newObj.transform.parent = anchor.transform;
      newObj.transform.position = obj.transform.position;
      newObj.transform.rotation = obj.transform.rotation;

      if (anchor == graphicalAnchor) {
        newObj.transform.localScale = Vector3.one;
      } else {
        newObj.transform.localScale = obj.transform.localScale;
      }

      Undo.RegisterCreatedObjectUndo(newObj, "Created Graphical Object");

      var toCopy = getComponentsToCopy(obj);
      foreach (var oldComponent in toCopy) {
        Component newComponent = transferComponent(oldComponent, newObj);
        oldToNew[oldComponent] = newComponent;
      }

      foreach (Transform child in obj.transform) {
        deepCopyGraphicalComponents(child.gameObject, newObj, graphicalAnchor, oldToNew);
      }
    }

    private void repairReferences(GameObject parentObject, Dictionary<UnityEngine.Object, UnityEngine.Object> oldToNew) {
      Component[] components = parentObject.GetComponentsInChildren<Component>(true);
      for (int i = 0; i < components.Length; i++) {

        SerializedObject oldSerializedObject = new SerializedObject(components[i]);
        SerializedProperty iterator = oldSerializedObject.GetIterator();
        bool didChange = false;

        while (iterator.Next(true)) {
          if (iterator.propertyType == SerializedPropertyType.ObjectReference) {
            UnityEngine.Object oldReference = iterator.objectReferenceValue;
            UnityEngine.Object newReference;
            if (oldReference != null && oldToNew.TryGetValue(oldReference, out newReference)) {
              iterator.objectReferenceValue = newReference;
              didChange = true;
            }
          }
        }

        if (didChange) {
          oldSerializedObject.ApplyModifiedProperties();
        }
      }
    }

    private void destroyOldComponents(Dictionary<UnityEngine.Object, UnityEngine.Object> oldToNew) {
      foreach (var toDestroy in oldToNew.Keys) {
        Undo.DestroyObjectImmediate(toDestroy);
      }
    }

    private bool cleanupEmptyObjects(GameObject anchor, GameObject graphicalAnchor) {
      List<Transform> childList = new List<Transform>();
      foreach (Transform child in anchor.transform) {
        childList.Add(child);
      }

      bool canDestroy = true;

      for (int i = 0; i < childList.Count; i++) {
        canDestroy &= cleanupEmptyObjects(childList[i].gameObject, graphicalAnchor);
      }

      //Can only destroy if there are no components except transform
      canDestroy &= anchor.GetComponents<Component>().Length == 1;

      canDestroy &= anchor != graphicalAnchor;

      if (canDestroy) {
        Undo.DestroyObjectImmediate(anchor);
      }

      return canDestroy;
    }

    private IEnumerable<Component> getComponentsToCopy(GameObject gameObject) {
      Renderer[] renderers = gameObject.GetComponentsInChildren<Renderer>(true);
      for (int i = 0; i < renderers.Length; i++) {
        Component component = renderers[i];
        if (component.gameObject == gameObject) {
          yield return component;
        }
      }

      MeshFilter[] meshFilters = gameObject.GetComponents<MeshFilter>();
      for (int i = 0; i < meshFilters.Length; i++) {
        yield return meshFilters[i];
      }
    }

    private Component transferComponent(Component src, GameObject dst) {
      if (!ComponentUtility.CopyComponent(src)) {
        throw new Exception("Could not copy component " + src);
      }

      Component newComponent = Undo.AddComponent(dst, src.GetType());

      if (!ComponentUtility.PasteComponentValues(newComponent)) {
        throw new Exception("Could not paste component values onto " + newComponent);
      }

      return newComponent;
    }
  }
}
