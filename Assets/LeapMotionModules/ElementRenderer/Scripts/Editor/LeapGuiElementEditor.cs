using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEditor;
using Leap.Unity;
using Leap.Unity.Query;

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiElementData), editorForChildClasses: true, isFallback = true)]
public class DefaultFeatureDataEditor : CustomEditorBase {
  protected override void OnEnable() {
    base.OnEnable();
    dontShowScriptField();
  }
}

[CanEditMultipleObjects]
[CustomEditor(typeof(LeapGuiElement))]
public class LeapGuiElementEditor : Editor {

  List<LeapGuiElement> elements = new List<LeapGuiElement>();
  List<Editor> editorCache = new List<Editor>();

  Object[] tempArray = new Object[0];
  List<Object> tempList = new List<Object>();

  public override void OnInspectorGUI() {
    base.OnInspectorGUI();

    targets.Query().Where(e => e != null).Select(e => e as LeapGuiElement).FillList(elements);

    if (elements.Count == 0) return;
    var mainElement = elements[0];

    int maxElements = LeapGuiPreferences.elementMax;
    if (elements.Query().Any(e => e.attachedGui != null && e.attachedGui.elements.IndexOf(e) >= maxElements)) {
      string noun = elements.Count == 1 ? "This element" : "Some of these elements";
      string guiName = elements.Count == 1 ? "its gui" : "their guis";
      EditorGUILayout.HelpBox(noun + " may not be properly displayed because there are too many elements on " + guiName + ".  " +
                              "Either lower the number of elements or increase the maximum element count by visiting " +
                              "Edit->Preferences.", MessageType.Warning);
    }

    if (tempArray.Length != elements.Count) {
      tempArray = new Object[elements.Count];
    }

    while (editorCache.Count < mainElement.data.Count) {
      editorCache.Add(null);
    }

    for (int i = 0; i < mainElement.data.Count; i++) {
      var mainDataObj = mainElement.data[i];
      var mainDataType = mainDataObj.GetType();
      var typeIndex = mainElement.data.Query().Where(d => d.GetType() == mainDataObj.GetType()).IndexOf(mainDataObj);

      tempList.Clear();
      tempList.Add(mainDataObj);

      elements.Query().
               Skip(1).
               Select(e =>
                 e.data.Query().
                        OfType(mainDataType).
                        ElementAtOrDefault(typeIndex)).
               Where(d => d != null).
               Cast<Object>().
               AppendList(tempList);

      if (tempList.Count != elements.Count) {
        //Not all elements had a matching data object, so we don't display
        continue;
      }

      tempList.CopyTo(tempArray);

      Editor editor = editorCache[i];
      CreateCachedEditor(tempArray, null, ref editor);
      editorCache[i] = editor;
      editor.serializedObject.Update();

      EditorGUI.BeginChangeCheck();
      EditorGUILayout.LabelField(LeapGuiTagAttribute.GetTag(mainDataType));
      EditorGUI.indentLevel++;

      editor.OnInspectorGUI();

      EditorGUI.indentLevel--;
      if (EditorGUI.EndChangeCheck()) {
        editor.serializedObject.ApplyModifiedProperties();
      }
    }
  }

  private bool HasFrameBounds() {
    return true;
  }

  private Bounds OnGetFrameBounds() {
    Bounds[] allBounds = targets.Query().
                                 Where(e => e != null).
                                 OfType<LeapGuiElement>().
                                 Select(e => e.pickingMesh).
                                 Where(m => m != null).
                                 Select(m => m.bounds).
                                 ToArray();

    Bounds bounds = allBounds[0];
    for (int i = 1; i < allBounds.Length; i++) {
      bounds.Encapsulate(allBounds[i]);
    }
    return bounds;
  }
}
