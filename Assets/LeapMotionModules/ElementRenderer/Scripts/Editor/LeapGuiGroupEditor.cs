using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Leap.Unity;
using Leap.Unity.Query;

[CustomEditor(typeof(LeapGuiGroup))]
public class LeapGuiGroupEditor : CustomEditorBase<LeapGuiGroup> {
  public const int BUTTON_WIDTH = 30;

  private GenericMenu _addRendererMenu;
  private Editor _rendererEditor;

  private ReorderableList _featureList;
  private GenericMenu _addFeatureMenu;

  protected override void OnEnable() {
    base.OnEnable();

    dontShowScriptField();

    _featureList = new ReorderableList(target.features,
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

    if (target.renderer != null) {
      CreateCachedEditor(target.renderer, null, ref _rendererEditor);
    }

    var allTypes = Assembly.GetAssembly(typeof(LeapGui)).GetTypes();

    var allRenderers = allTypes.Query().
                                Where(t => !t.IsAbstract).
                                Where(t => !t.IsGenericType).
                                Where(t => t.IsSubclassOf(typeof(LeapGuiRendererBase)));

    _addRendererMenu = new GenericMenu();
    foreach (var renderer in allRenderers) {
      _addRendererMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(renderer)),
                               false,
                               () => {
                                 target.editor.ChangeRenderer(renderer);
                                 serializedObject.Update();
                                 CreateCachedEditor(target.renderer, null, ref _rendererEditor);
                                 target.gui.editor.ScheduleEditorUpdate();
                               });
    }

    var allFeatures = allTypes.Query().
                               Where(t => !t.IsAbstract).
                               Where(t => !t.IsGenericType).
                               Where(t => t.IsSubclassOf(typeof(LeapGuiFeatureBase)));

    _addFeatureMenu = new GenericMenu();
    foreach (var feature in allFeatures) {
      _addFeatureMenu.AddItem(new GUIContent(LeapGuiTagAttribute.GetTag(feature)),
                              false,
                              () => target.editor.AddFeature(feature));
    }

    specifyCustomDecorator("_features", featureDecorator);
    specifyCustomDrawer("_features", drawFeatures);
    specifyCustomDrawer("_renderer", drawRenderer);
  }

  private void OnDisable() {
    if (_rendererEditor != null) DestroyImmediate(_rendererEditor);
  }

  private void drawRenderer(SerializedProperty property) {
    Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
    Rect left, right;
    rect.SplitHorizontallyWithRight(out left, out right, BUTTON_WIDTH * 2);

    EditorGUI.LabelField(left, "Renderer", EditorStyles.miniButtonLeft);
    using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
      if (GUI.Button(right, "v", EditorStyles.miniButtonRight)) {
        _addRendererMenu.ShowAsContext();
      }
    }

    if (_rendererEditor != null) {
      _rendererEditor.serializedObject.Update();
      _rendererEditor.OnInspectorGUI();
      _rendererEditor.serializedObject.ApplyModifiedProperties();
    }
  }

  private void featureDecorator(SerializedProperty property) {
    int elementMax = LeapGuiPreferences.elementMax;

    if (target.elements.Count > elementMax) {
      EditorGUILayout.HelpBox("This gui currently has " + target.elements.Count.ToString() +
                              " elements, which is greater than the maximum of " +
                              elementMax + ".  Visit Edit->Preferences to change the maximum element count.",
                              MessageType.Warning);
      return;
    }
  }

  private void drawFeatures(SerializedProperty property) {
    EditorGUILayout.Space();

    Rect rect = EditorGUILayout.GetControlRect(GUILayout.MaxHeight(EditorGUIUtility.singleLineHeight));
    Rect left, middle, right;
    rect.SplitHorizontallyWithRight(out middle, out right, BUTTON_WIDTH);
    middle.SplitHorizontallyWithRight(out left, out middle, BUTTON_WIDTH);

    EditorGUI.LabelField(left, "Element Features", EditorStyles.miniButtonLeft);

    using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
      EditorGUI.BeginDisabledGroup(target.features.Count == 0);
      if (GUI.Button(middle, "-", EditorStyles.miniButtonMid) && _featureList.index >= 0) {
        target.editor.RemoveFeature(target.features[_featureList.index]);
        EditorUtility.SetDirty(target);
      }
      EditorGUI.EndDisabledGroup();

      if (GUI.Button(right, "+", EditorStyles.miniButtonRight)) {
        _addFeatureMenu.ShowAsContext();
        EditorUtility.SetDirty(target);
      }
    }

    EditorGUI.BeginChangeCheck();

    Undo.RecordObject(target, "Changed feature list.");
    _featureList.DoLayoutList();

    if (EditorGUI.EndChangeCheck()) {
      EditorUtility.SetDirty(target);
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
    return target.features[index].GetEditorHeight() + EditorGUIUtility.singleLineHeight;
  }

  delegate void Action<T1, T2, T3, T4, T5>(T1 t1, T2 t2, T3 t3, T4 t4, T5 t5);

  private void drawFeatureCallback(Rect rect, int index, bool isActive, bool isFocused) {
    rect = rect.SingleLine();
    var feature = target.features[index];

    string featureName = LeapGuiTagAttribute.GetTag(target.features[index].GetType());

    int lastIndexOf = featureName.LastIndexOf('/');
    if (lastIndexOf >= 0) {
      featureName = featureName.Substring(lastIndexOf + 1);
    }

    GUIContent featureLabel = new GUIContent(featureName);

    Color originalColor = GUI.color;

    if (!EditorApplication.isPlaying &&
        target.supportInfo != null &&
        index < target.supportInfo.Count) {
      var supportInfo = target.supportInfo[index];
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
      target.gui.editor.ScheduleEditorUpdate();
      EditorUtility.SetDirty(feature);
    }
  }

  private void onReorderFeaturesCallback(ReorderableList list) {
    EditorUtility.SetDirty(target);
    serializedObject.Update();
  }
}
