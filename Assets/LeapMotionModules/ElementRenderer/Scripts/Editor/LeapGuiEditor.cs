using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Leap.Unity;
using Leap.Unity.Space;
using Leap.Unity.Query;

[CustomEditor(typeof(LeapGui))]
public class LeapGuiEditor : CustomEditorBase {
  private const int BUTTON_WIDTH = 60;
  private static Color BUTTON_COLOR = Color.white * 0.95f;
  private static Color BUTTON_HIGHLIGHTED_COLOR = Color.white * 0.6f;

  private LeapGui _gui;
  private SerializedProperty _selectedGroup;

  private GenericMenu _addGroupMenu;
  private Editor _groupEditor;

  protected override void OnEnable() {
    base.OnEnable();

    if (target == null) {
      return;
    }

    _gui = target as LeapGui;
    _selectedGroup = serializedObject.FindProperty("_selectedGroup");

    var allTypes = Assembly.GetAssembly(typeof(LeapGui)).GetTypes();

    var allRenderers = allTypes.Query().
                                Where(t => !t.IsAbstract).
                                Where(t => !t.IsGenericType).
                                Where(t => t.IsSubclassOf(typeof(LeapGuiRendererBase)));

    _addGroupMenu = new GenericMenu();
    foreach (var renderer in allRenderers) {
      _addGroupMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(renderer)),
                            false,
                            () => {
                              _gui.editor.CreateGroup(renderer);
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
          _gui.editor.DestroySelectedGroup();
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

    for (int i = 0; i < _gui.groups.Count; i++) {
      if (i == _selectedGroup.intValue) {
        GUI.color = BUTTON_HIGHLIGHTED_COLOR;
      } else {
        GUI.color = BUTTON_COLOR;
      }

      var group = _gui.groups[i];
      string tag = LeapGuiTagAttribute.GetTag(group.renderer.GetType());
      if (GUILayout.Button(tag, EditorStyles.toolbarButton, GUILayout.MaxWidth(60))) {
        _selectedGroup.intValue = i;
        CreateCachedEditor(_gui.groups[i], null, ref _groupEditor);
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
    if (_gui.groups.Count == 0) {
      if (_groupEditor != null) {

        DestroyImmediate(_groupEditor);
      }
    } else {
      CreateCachedEditor(_gui.groups[_selectedGroup.intValue], null, ref _groupEditor);
    }
  }

  private bool HasFrameBounds() {
    _gui.editor.RebuildEditorPickingMeshes();

    return _gui.groups.Query().
                       SelectMany(g => g.elements.Query()).
                       Select(e => e.editor.pickingMesh).
                       Any(m => m != null);
  }

  private Bounds OnGetFrameBounds() {
    _gui.editor.RebuildEditorPickingMeshes();

    return _gui.groups.Query().
                       SelectMany(g => g.elements.Query()).
                       Select(e => e.editor.pickingMesh).
                       NonNull().
                       Select(m => m.bounds).
                       Fold((a, b) => {
                         a.Encapsulate(b);
                         return a;
                       });
  }
}
