/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Reflection;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CustomEditor(typeof(LeapGraphicRenderer))]
  public class LeapGraphicRendererEditor : CustomEditorBase<LeapGraphicRenderer> {
    private const int BUTTON_WIDTH = 60;
    private static Color BUTTON_COLOR = Color.white * 0.95f;
    private static Color BUTTON_HIGHLIGHTED_COLOR = Color.white * 0.6f;

    private LeapGraphicRenderer _renderer;
    private SerializedProperty _selectedGroup;

    private GenericMenu _addGroupMenu;
    private SerializedProperty _groupProperty;
    private LeapGuiGroupEditor _groupEditor;

    protected override void OnEnable() {
      base.OnEnable();

      if (target == null) {
        return;
      }

      _renderer = target as LeapGraphicRenderer;
      _selectedGroup = serializedObject.FindProperty("_selectedGroup");

      var allTypes = Assembly.GetAssembly(typeof(LeapGraphicRenderer)).GetTypes();

      var allRenderingMethods = allTypes.Query().
                                         Where(t => !t.IsAbstract &&
                                                    !t.IsGenericType &&
                                                     t.IsSubclassOf(typeof(LeapRenderingMethod)));

      _addGroupMenu = new GenericMenu();
      foreach (var renderingMethod in allRenderingMethods) {
        _addGroupMenu.AddItem(new GUIContent(LeapGraphicTagAttribute.GetTagName(renderingMethod)),
                              false,
                              () => {
                                serializedObject.ApplyModifiedProperties();
                                Undo.RecordObject(_renderer, "Created group");
                                EditorUtility.SetDirty(_renderer);
                                _renderer.editor.CreateGroup(renderingMethod);
                                serializedObject.Update();
                                updateGroupProperty();
                              });
      }

      _groupEditor = new LeapGuiGroupEditor(target, serializedObject);
    }

    public override void OnInspectorGUI() {
      using (new ProfilerSample("Draw Graphic Renderer Editor")) {
        updateGroupProperty();

        drawScriptField();

        bool anyVertexLit = false;
        foreach (var camera in FindObjectsOfType<Camera>()) {
          if (camera.actualRenderingPath == RenderingPath.VertexLit) {
            anyVertexLit = true;
            break;
          }
        }

        if (anyVertexLit) {
          EditorGUILayout.HelpBox("The vertex lit rendering path is not supported.", MessageType.Error);
        }

        drawToolbar();

        if (_groupProperty != null) {
          drawGroupHeader();

          GUILayout.BeginVertical(EditorStyles.helpBox);

          _groupEditor.DoGuiLayout(_groupProperty);

          GUILayout.EndVertical();
        } else {
          EditorGUILayout.HelpBox("To get started, create a new rendering group!", MessageType.Info);
        }

        serializedObject.ApplyModifiedProperties();
      }
    }

    private void drawToolbar() {
      EditorGUILayout.BeginHorizontal();

      using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
        var prevColor = GUI.color;
        if (GUILayout.Button("New Group", EditorStyles.toolbarDropDown)) {
          _addGroupMenu.ShowAsContext();
        }

        if (_groupProperty != null) {
          if (GUILayout.Button("Delete Group", EditorStyles.toolbarButton)) {
            serializedObject.ApplyModifiedProperties();
            Undo.RecordObject(_renderer, "Deleted group");
            EditorUtility.SetDirty(_renderer);
            _renderer.editor.DestroySelectedGroup();
            serializedObject.Update();
            _groupEditor.Invalidate();
            updateGroupProperty();
          }
        }
        GUI.color = prevColor;
      }

      GUILayout.FlexibleSpace();
      Rect r = GUILayoutUtility.GetLastRect();
      GUI.Label(r, "", EditorStyles.toolbarButton);

      EditorGUILayout.EndHorizontal();
    }

    private void drawGroupHeader() {
      EditorGUILayout.BeginHorizontal();

      var prevColor = GUI.color;
      for (int i = 0; i < _renderer.groups.Count; i++) {
        if (i == _selectedGroup.intValue) {
          GUI.color = BUTTON_HIGHLIGHTED_COLOR;
        } else {
          GUI.color = BUTTON_COLOR;
        }

        var group = _renderer.groups[i];
        if (GUILayout.Button(group.name, EditorStyles.toolbarButton)) {
          _selectedGroup.intValue = i;
          _groupEditor.Invalidate();
          updateGroupProperty();
        }
      }

      GUI.color = prevColor;
      GUILayout.FlexibleSpace();
      Rect rect = GUILayoutUtility.GetLastRect();
      GUI.Label(rect, "", EditorStyles.toolbarButton);

      EditorGUILayout.EndHorizontal();
    }

    private void updateGroupProperty() {
      var groupArray = serializedObject.FindProperty("_groups");
      if (groupArray.arraySize == 0) {
        _groupProperty = null;
        return;
      }

      int selectedGroupIndex = _selectedGroup.intValue;
      if (selectedGroupIndex < 0 || selectedGroupIndex >= groupArray.arraySize) {
        _selectedGroup.intValue = 0;
        selectedGroupIndex = 0;
      }

      _groupProperty = groupArray.GetArrayElementAtIndex(selectedGroupIndex);
    }

    private bool HasFrameBounds() {
      _renderer.editor.RebuildEditorPickingMeshes();

      return _renderer.groups.Query().
                         SelectMany(g => g.graphics.Query()).
                         Select(e => e.editor.pickingMesh).
                         Any(m => m != null);
    }

    private Bounds OnGetFrameBounds() {
      _renderer.editor.RebuildEditorPickingMeshes();

      return _renderer.groups.Query().
                         SelectMany(g => g.graphics.Query()).
                         Select(e => e.editor.pickingMesh).
                         ValidUnityObjs().
                         Select(m => m.bounds).
                         Fold((a, b) => {
                           a.Encapsulate(b);
                           return a;
                         });
    }
  }
}
