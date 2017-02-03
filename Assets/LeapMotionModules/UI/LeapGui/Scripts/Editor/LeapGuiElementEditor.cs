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

    if (tempArray.Length != elements.Count) {
      tempArray = new Object[elements.Count];
    }

    for (int i = 0; i < mainElement.data.Count; i++) {
      var mainDataObj = mainElement.data[i];
      var typeIndex = mainElement.data.Query().Where(d => d.GetType() == mainDataObj.GetType()).IndexOf(mainDataObj);

      elements.Query().
               Skip(1).
               Select(e =>
                 e.data.Query().
                        Where(d => d.GetType() == mainDataObj.GetType()).
                        Skip(typeIndex).
                        FirstOrDefault()).
               Where(d => d != null).
               Select(d => d as Object).
               FillList(tempList);

      Editor editor = null;
      if (editorCache.Count <= i) {
        editorCache.Add(null);
      } else {
        editor = editorCache[i];
      }

      Editor.CreateCachedEditor(objs, null, ref editor);
      editorCache[i] = editor;

      EditorGUI.BeginChangeCheck();
      EditorGUILayout.LabelField(LeapGuiTagAttribute.GetTag(gui.features[i].GetType()));
      EditorGUI.indentLevel++;

      editor.OnInspectorGUI();

      EditorGUI.indentLevel--;
      if (EditorGUI.EndChangeCheck()) {
        editor.serializedObject.ApplyModifiedProperties();
      }
    }
  }
}
