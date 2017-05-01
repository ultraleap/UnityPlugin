using System.Reflection;
using UnityEngine;
using UnityEditor;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CustomEditor(typeof(LeapGraphicRenderer))]
  public class LeapGraphicRendererEditor : CustomEditorBase {
    private const int BUTTON_WIDTH = 60;
    private static Color BUTTON_COLOR = Color.white * 0.95f;
    private static Color BUTTON_HIGHLIGHTED_COLOR = Color.white * 0.6f;

    private LeapGraphicRenderer _renderer;
    private SerializedProperty _selectedGroup;

    private GenericMenu _addGroupMenu;
    private Editor _groupEditor;

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
        _addGroupMenu.AddItem(new GUIContent(LeapGraphicTagAttribute.GetTag(renderingMethod)),
                              false,
                              () => {
                                _renderer.editor.CreateGroup(renderingMethod);
                                updateGroupEditor();
                              });
      }

      updateGroupEditor();
    }

    private void OnDisable() {
      if (_groupEditor != null) DestroyImmediate(_groupEditor);
    }

    public override void OnInspectorGUI() {
      validateEditors();

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

      if (_groupEditor != null) {
        drawGroupHeader();

        GUILayout.BeginVertical(EditorStyles.helpBox);

        _groupEditor.serializedObject.Update();
        _groupEditor.OnInspectorGUI();
        _groupEditor.serializedObject.ApplyModifiedProperties();

        GUILayout.EndVertical();
      } else {
        EditorGUILayout.HelpBox("To get started, create a new rendering group!", MessageType.Info);
      }

      serializedObject.ApplyModifiedProperties();
    }

    private void validateEditors() {
      if (_groupEditor != null && _groupEditor.serializedObject.targetObjects.Query().Any(o => o == null)) {
        _groupEditor = null;

        updateGroupEditor();
      }
    }

    private void drawToolbar() {
      EditorGUILayout.BeginHorizontal();

      using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
        GUI.color = BUTTON_COLOR;
        if (GUILayout.Button("New Group", EditorStyles.toolbarDropDown)) {
          _addGroupMenu.ShowAsContext();
        }

        if (_groupEditor != null) {
          if (GUILayout.Button("Delete Group", EditorStyles.toolbarButton)) {
            _renderer.editor.DestroySelectedGroup();
            updateGroupEditor();
          }
        }
      }

      GUI.color = Color.white;
      GUILayout.FlexibleSpace();
      Rect r = GUILayoutUtility.GetLastRect();
      GUI.Label(r, "", EditorStyles.toolbarButton);

      EditorGUILayout.EndHorizontal();
    }

    private void drawGroupHeader() {
      EditorGUILayout.BeginHorizontal();

      for (int i = 0; i < _renderer.groups.Count; i++) {
        if (i == _selectedGroup.intValue) {
          GUI.color = BUTTON_HIGHLIGHTED_COLOR;
        } else {
          GUI.color = BUTTON_COLOR;
        }

        var group = _renderer.groups[i];
        string tag = LeapGraphicTagAttribute.GetTag(group.renderingMethod.GetType());
        if (GUILayout.Button(tag, EditorStyles.toolbarButton, GUILayout.MaxWidth(60))) {
          _selectedGroup.intValue = i;
          CreateCachedEditor(_renderer.groups[i], null, ref _groupEditor);
        }
      }
      GUI.color = Color.white;

      GUILayout.FlexibleSpace();
      Rect rect = GUILayoutUtility.GetLastRect();
      GUI.Label(rect, "", EditorStyles.toolbarButton);

      EditorGUILayout.EndHorizontal();
    }

    private void updateGroupEditor() {
      serializedObject.Update();
      if (_renderer.groups.Count == 0) {
        if (_groupEditor != null) {

          DestroyImmediate(_groupEditor);
        }
      } else {
        CreateCachedEditor(_renderer.groups[_selectedGroup.intValue], null, ref _groupEditor);
      }
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
