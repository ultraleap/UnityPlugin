using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Leap.Unity;
using Leap.Unity.Query;

[CustomEditor(typeof(LeapGui))]
public class LeapGuiEditor : CustomEditorBase {
  private const int BUTTON_WIDTH = 30;

  private LeapGui gui;
  private ReorderableList _featureList;
  private GenericMenu _addFeatureMenu;

  private GenericMenu _addSpaceMenu;
  private Editor _spaceEditor;

  private GenericMenu _addRendererMenu;
  private Editor _rendererEditor;

  protected override void OnEnable() {
    base.OnEnable();

    if (target == null) {
      return;
    }

    gui = target as LeapGui;

    _featureList = new ReorderableList(gui.features,
                                       typeof(LeapGuiFeatureBase),
                                       draggable: true,
                                       displayHeader: false,
                                       displayAddButton: false,
                                       displayRemoveButton: false);

    _featureList.showDefaultBackground = false;
    _featureList.headerHeight = 0;
    _featureList.elementHeight = EditorGUIUtility.singleLineHeight;
    _featureList.elementHeightCallback = featureHeightCallback;
    _featureList.drawElementCallback = drawFeatureCallback;
    _featureList.onReorderCallback = onReorderFeaturesCallback;

    var allTypes = Assembly.GetAssembly(typeof(LeapGui)).GetTypes();

    var allFeatures = allTypes.Query().
                               Where(t => !t.IsAbstract).
                               Where(t => !t.IsGenericType).
                               Where(t => t.IsSubclassOf(typeof(LeapGuiFeatureBase)));

    _addFeatureMenu = new GenericMenu();
    foreach (var feature in allFeatures) {
      _addFeatureMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(feature)),
                              false,
                              () => gui.AddFeature(feature));
    }

    var allSpaces = allTypes.Query().
                             Where(t => !t.IsAbstract).
                             Where(t => !t.IsGenericType).
                             Where(t => t.IsSubclassOf(typeof(LeapGuiSpace)));

    _addSpaceMenu = new GenericMenu();
    foreach (var space in allSpaces) {
      _addSpaceMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(space)),
                            false,
                            () => {
                              gui.SetSpace(space);
                              _spaceEditor = CreateEditor(gui.space);
                              serializedObject.Update();
                            });
    }

    var allRenderers = allTypes.Query().
                                Where(t => !t.IsAbstract).
                                Where(t => !t.IsGenericType).
                                Where(t => t.IsSubclassOf(typeof(LeapGuiRenderer)));

    _addRendererMenu = new GenericMenu();
    foreach (var renderer in allRenderers) {
      _addRendererMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(renderer)),
                               false,
                               () => {
                                 gui.SetRenderer(renderer);
                                 _rendererEditor = CreateEditor(gui.renderer);
                                 serializedObject.Update();
                               });
    }

    if (gui.renderer != null) {
      _rendererEditor = CreateEditor(gui.renderer);
    }

    if (gui.space != null) {
      _spaceEditor = CreateEditor(gui.space);
    }

    specifyCustomDecorator("_features", featureDecorator);
    specifyCustomDrawer("_features", drawFeatures);
    specifyCustomDrawer("_space", drawSpace);
    specifyCustomDrawer("_renderer", drawRenderer);
  }

  private void featureDecorator(SerializedProperty property) {
    int elementMax = LeapGuiPreferences.elementMax;

    if (gui.elements.Count > elementMax) {
      EditorGUILayout.HelpBox("This gui currently has " + gui.elements.Count.ToString() +
                              " elements, which is greater than the maximum of " +
                              elementMax + ".  Visit Edit->Preferences to change the maximum element count.",
                              MessageType.Warning);
      return;
    }
  }

  private void drawFeatures(SerializedProperty property) {
    Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
    Rect left, middle, right;
    rect.SplitHorizontallyWithRight(out middle, out right, BUTTON_WIDTH);
    middle.SplitHorizontallyWithRight(out left, out middle, BUTTON_WIDTH);

    EditorGUI.LabelField(left, "Element Features", EditorStyles.miniButtonLeft);

    EditorGUI.BeginDisabledGroup(gui.features.Count == 0);
    if (GUI.Button(middle, "-", EditorStyles.miniButtonMid) && _featureList.index >= 0) {
      gui.features.RemoveAt(_featureList.index);
      gui.ScheduleFullUpdate();
      EditorUtility.SetDirty(gui);
    }
    EditorGUI.EndDisabledGroup();

    if (GUI.Button(right, "+", EditorStyles.miniButtonRight)) {
      _addFeatureMenu.ShowAsContext();
      EditorUtility.SetDirty(gui);
    }

    EditorGUI.BeginChangeCheck();

    Undo.RecordObject(target, "Changed feature list.");
    _featureList.DoLayoutList();

    if (EditorGUI.EndChangeCheck()) {
      EditorUtility.SetDirty(gui);
    }
  }

  private void drawSpace(SerializedProperty property) {
    Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
    Rect left, right;
    rect.SplitHorizontallyWithRight(out left, out right, BUTTON_WIDTH * 2);

    EditorGUI.LabelField(left, "Space", EditorStyles.miniButtonLeft);
    if (GUI.Button(right, "v", EditorStyles.miniButtonRight)) {
      _addSpaceMenu.ShowAsContext();
    }

    if (_spaceEditor != null) {
      _spaceEditor.DrawDefaultInspector();
    }

    EditorGUILayout.Space();
  }

  private void drawRenderer(SerializedProperty property) {
    Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
    Rect left, right;
    rect.SplitHorizontallyWithRight(out left, out right, BUTTON_WIDTH * 2);

    EditorGUI.LabelField(left, "Renderer", EditorStyles.miniButtonLeft);
    if (GUI.Button(right, "v", EditorStyles.miniButtonRight)) {
      _addRendererMenu.ShowAsContext();
    }

    if (_rendererEditor != null) {
      _rendererEditor.DrawDefaultInspector();
    }
  }

  // Feature list callbacks

  private void drawFeatureHeaderCallback(Rect rect) {
    Rect left, right;
    rect.SplitHorizontallyWithRight(out left, out right, BUTTON_WIDTH);

    EditorGUI.LabelField(left, "Gui Element Features", EditorStyles.miniButtonLeft);

    if (GUI.Button(right, "+", EditorStyles.miniButtonRight)) {
      _addFeatureMenu.ShowAsContext();
    }
  }

  private float featureHeightCallback(int index) {
    return gui.features[index].GetEditorHeight() + EditorGUIUtility.singleLineHeight;
  }

  private void drawFeatureCallback(Rect rect, int index, bool isActive, bool isFocused) {
    rect = rect.SingleLine();
    var feature = gui.features[index];

    string featureName = LeapGuiTagAttribute.GetTag(gui.features[index].GetType());
    GUIContent featureLabel = new GUIContent(featureName);

    Color originalColor = GUI.color;

    if (!EditorApplication.isPlaying && gui.supportInfo != null) {
      var supportInfo = gui.supportInfo[index];
      switch (supportInfo.support) {
        case SupportType.Warning:
          GUI.color = Color.yellow;
          featureLabel.tooltip = supportInfo.message;
          break;
        case SupportType.Error:
          GUI.color = Color.red;
          featureLabel.tooltip = supportInfo.message;
          break;
      }
    }

    Vector2 size = EditorStyles.label.CalcSize(featureLabel);

    Rect labelRect = rect;
    labelRect.width = size.x;

    GUI.Box(labelRect, "");
    EditorGUI.LabelField(labelRect, featureLabel);
    GUI.color = originalColor;

    Undo.RecordObject(feature, "Modified Gui Feature");

    EditorGUI.BeginChangeCheck();
    feature.DrawFeatureEditor(rect.NextLine().Indent(), isActive, isFocused);
    if (EditorGUI.EndChangeCheck()) {
      EditorUtility.SetDirty(feature);
    }
  }

  private void onReorderFeaturesCallback(ReorderableList list) {
    EditorUtility.SetDirty(target);
    serializedObject.Update();
  }

  private bool HasFrameBounds() {
    return true;
  }

  private Bounds OnGetFrameBounds() {
    Bounds[] allBounds = gui.elements.Query().
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

