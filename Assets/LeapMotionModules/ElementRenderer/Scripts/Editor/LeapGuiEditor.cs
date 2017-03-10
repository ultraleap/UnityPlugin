using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Leap.Unity;
using Leap.Unity.Query;

[CustomEditor(typeof(LeapGui))]
public class LeapGuiEditor : CustomEditorBase {
  private const int BUTTON_WIDTH = 60;
  private static Color BUTTON_COLOR = Color.white * 0.95f;
  private static Color BUTTON_HIGHLIGHTED_COLOR = Color.white * 0.6f;

  private LeapGui _gui;
  private SerializedProperty _selectedGroup;

  private GenericMenu _addSpaceMenu;
  private Editor _spaceEditor;

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

    var allSpaces = allTypes.Query().
                             Where(t => !t.IsAbstract).
                             Where(t => !t.IsGenericType).
                             Where(t => t.IsSubclassOf(typeof(LeapGuiSpace)));

    _addSpaceMenu = new GenericMenu();
    foreach (var space in allSpaces) {
      _addSpaceMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(space)),
                            false,
                            () => {
                              _gui.SetSpace(space);
                              CreateCachedEditor(_gui.space, null, ref _spaceEditor);
                              serializedObject.Update();
                            });
    }

    var allRenderers = allTypes.Query().
                                Where(t => !t.IsAbstract).
                                Where(t => !t.IsGenericType).
                                Where(t => t.IsSubclassOf(typeof(LeapGuiRendererBase)));

    _addGroupMenu = new GenericMenu();
    foreach (var renderer in allRenderers) {
      _addGroupMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(renderer)),
                            false,
                            () => {
                              _gui.CreateGroup(renderer);
                              updateGroupEditor();
                            });
    }

    if (_gui.space != null) {
      CreateCachedEditor(_gui.space, null, ref _spaceEditor);
    }

    updateGroupEditor();
  }

  private void OnDisable() {
    if (_spaceEditor != null) DestroyImmediate(_spaceEditor);
    if (_groupEditor != null) DestroyImmediate(_groupEditor);
  }

  public override void OnInspectorGUI() {
    drawScriptField();

    drawToolbar();

    if (_spaceEditor != null && !(_gui.space is LeapGuiRectSpace)) {
      GUILayout.BeginVertical(EditorStyles.helpBox);
      EditorGUILayout.LabelField("Global Warping", EditorStyles.miniButton);
      _spaceEditor.OnInspectorGUI();
      GUILayout.EndVertical();
    }

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

  private void drawToolbar() {
    EditorGUILayout.BeginHorizontal();

    GUI.color = BUTTON_COLOR;
    if (GUILayout.Button("New Group", EditorStyles.toolbarDropDown)) {
      _addGroupMenu.ShowAsContext();
    }

    if (_groupEditor != null) {
      if (GUILayout.Button("Delete Group", EditorStyles.toolbarButton)) {
        _gui.DestroySelectedGroup();
        updateGroupEditor();
      }
    }

    GUI.color = Color.white;
    GUILayout.FlexibleSpace();
    Rect r = GUILayoutUtility.GetLastRect();
    GUI.Label(r, "", EditorStyles.toolbarButton);
    GUI.color = BUTTON_COLOR;

    if (_groupEditor != null || !(_gui.space is LeapGuiRectSpace)) {
      if (GUILayout.Button("Warp Space", EditorStyles.toolbarDropDown)) {
        _addSpaceMenu.ShowAsContext();
      }
    }

    GUI.color = Color.white;

    EditorGUILayout.EndHorizontal();
  }

  private void drawSpaceHeader() {
    EditorGUILayout.BeginHorizontal();

    GUILayout.FlexibleSpace();
    Rect rect = GUILayoutUtility.GetLastRect();
    GUI.Label(rect, "", EditorStyles.toolbarButton);

    if (GUILayout.Button("Change", EditorStyles.toolbarDropDown, GUILayout.Width(BUTTON_WIDTH))) {
      _addSpaceMenu.ShowAsContext();
    }

    EditorGUILayout.EndHorizontal();

    if (_spaceEditor != null) {
      _spaceEditor.OnInspectorGUI();
    }
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
}
