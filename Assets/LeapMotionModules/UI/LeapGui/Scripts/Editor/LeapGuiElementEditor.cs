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
      Editor.CreateCachedEditor(tempArray, null, ref editor);
      editorCache[i] = editor;

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
}
