using System.Reflection;
using UnityEngine;
using UnityEditor;
using UnityEditorInternal;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer {

  [CustomEditor(typeof(LeapGraphicGroup))]
  public class LeapGuiGroupEditor : CustomEditorBase<LeapGraphicGroup> {
    public const int BUTTON_WIDTH = 30;

    private GenericMenu _addRenderingMethodMenu;
    private Editor _rendererEditor;

    private ReorderableList _featureList;
    private GenericMenu _addFeatureMenu;

    protected override void OnEnable() {
      base.OnEnable();

      dontShowScriptField();

      _featureList = new ReorderableList(target.features,
                                         typeof(LeapGraphicFeatureBase),
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

      if (target.renderingMethod != null) {
        CreateCachedEditor(target.renderingMethod, null, ref _rendererEditor);
      }

      var allTypes = Assembly.GetAssembly(typeof(LeapGraphicRenderer)).GetTypes();

      var allRenderingMethods = allTypes.Query().
                                         Where(t => !t.IsAbstract &&
                                                    !t.IsGenericType &&
                                                     t.IsSubclassOf(typeof(LeapRenderingMethod)));

      _addRenderingMethodMenu = new GenericMenu();
      foreach (var renderingMethod in allRenderingMethods) {
        _addRenderingMethodMenu.AddItem(new GUIContent(LeapGraphicTagAttribute.GetTag(renderingMethod)),
                                        false,
                                        () => {
                                          target.editor.ChangeRenderingMethod(renderingMethod, addFeatures: false);
                                          serializedObject.Update();
                                          CreateCachedEditor(target.renderingMethod, null, ref _rendererEditor);
                                          target.renderer.editor.ScheduleEditorUpdate();
                                        });
      }

      var allFeatures = allTypes.Query().
                                 Where(t => !t.IsAbstract &&
                                            !t.IsGenericType &&
                                             t.IsSubclassOf(typeof(LeapGraphicFeatureBase)));

      _addFeatureMenu = new GenericMenu();
      foreach (var feature in allFeatures) {
        _addFeatureMenu.AddItem(new GUIContent(LeapGraphicTagAttribute.GetTag(feature)),
                                false,
                                () => target.editor.AddFeature(feature));
      }

      specifyCustomDecorator("_features", featureDecorator);
      specifyCustomDrawer("_features", drawFeatures);
      specifyCustomDrawer("_renderingMethod", drawRenderer);
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
          _addRenderingMethodMenu.ShowAsContext();
        }
      }

      if (_rendererEditor != null) {
        _rendererEditor.serializedObject.Update();
        _rendererEditor.OnInspectorGUI();
        _rendererEditor.serializedObject.ApplyModifiedProperties();
      }
    }

    private void featureDecorator(SerializedProperty property) {
      int graphicMax = LeapGraphicPreferences.graphicMax;

      var anyRectsInvalid = target.features.Query().
                                            OfType<LeapSpriteFeature>().
                                            SelectMany(f => f.featureData.Query().
                                                                          Select(d => d.sprite).
                                                                          NonNull()).
                                            Select(s => SpriteAtlasUtil.GetAtlasedRect(s)).
                                            Any(r => r.Area() == 0);

      if (anyRectsInvalid) {
        EditorGUILayout.HelpBox("Due to a Unity bug, packed sprites may be invalid until " +
                                "PlayMode has been entered at least once.", MessageType.Warning);
      }

      if (target.graphics.Count > graphicMax) {
        EditorGUILayout.HelpBox("This gui currently has " + target.graphics.Count.ToString() +
                                " graphics, which is greater than the maximum of " +
                                graphicMax + ".  Visit Edit->Preferences to change the maximum graphic count.",
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

      EditorGUI.LabelField(left, "Graphic Features", EditorStyles.miniButtonLeft);

      using (new EditorGUI.DisabledGroupScope(EditorApplication.isPlaying)) {
        EditorGUI.BeginDisabledGroup(target.features.Count == 0);
        if (GUI.Button(middle, "-", EditorStyles.miniButtonMid) && _featureList.index >= 0) {
          Undo.RecordObject(target, "Removed feature");
          target.editor.RemoveFeature(target.features[_featureList.index]);
        }
        EditorGUI.EndDisabledGroup();

        if (GUI.Button(right, "+", EditorStyles.miniButtonRight)) {
          _addFeatureMenu.ShowAsContext();
        }
      }

      _featureList.DoLayoutList();
    }

    // Feature list callbacks

    private void drawFeatureHeaderCallback(Rect rect) {
      Rect left, right;
      rect.SplitHorizontallyWithRight(out left, out right, BUTTON_WIDTH);

      EditorGUI.LabelField(left, "Graphic Features", EditorStyles.miniButtonLeft);

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
      if (feature == null) {
        return;
      }

      string featureName = LeapGraphicTagAttribute.GetTag(target.features[index].GetType());

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
        target.renderer.editor.ScheduleEditorUpdate();
      }
    }

    private void onReorderFeaturesCallback(ReorderableList list) {
      Undo.RecordObject(target, "Reordered feature list");
      serializedObject.Update();
    }
  }
}
