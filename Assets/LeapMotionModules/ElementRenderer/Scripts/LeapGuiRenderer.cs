using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Space;

public abstract class LeapGuiRendererBase : LeapGuiComponentBase<LeapGui> {
  public const string DATA_FOLDER_NAME = "_ElementData";

  [HideInInspector]
  public LeapGui gui;

  [HideInInspector]
  public LeapGuiGroup group;

  public abstract SupportInfo GetSpaceSupportInfo(LeapSpace space);

  protected override void OnValidate() {
    base.OnValidate();

#if UNITY_EDITOR
    if (!Application.isPlaying) {
      if (gui != null) {
        gui.editor.ScheduleEditorUpdate();
      }
    }
#endif
  }

  protected bool isHeavyUpdate { get; private set; }

  /// <summary>
  /// Called when the leap gui is enabled at runtime.
  /// </summary>
  public abstract void OnEnableRenderer();

  /// <summary>
  /// Called when the leap gui is disabled at runtime.
  /// </summary>
  public abstract void OnDisableRenderer();

  /// <summary>
  /// Called from LateUpdate during runtime.  Use this to update the
  /// renderer using any changes made to the gui during this frame.
  /// </summary>
  public abstract void OnUpdateRenderer();

#if UNITY_EDITOR
  /// <summary>
  /// Called curing edit time when this renderer becomes a renderer for a 
  /// leap gui.  Use this for any edit-time construction you need.
  /// </summary>
  public abstract void OnEnableRendererEditor();

  /// <summary>
  /// Called during edit time when this renderer is no longer the renderer
  /// for a leap gui.  Use this for edit-time clean up.
  /// </summary>
  public abstract void OnDisableRendererEditor();

  /// <summary>
  /// Called during edit time to update the renderer status.  This is 
  /// called every time a change is performed to the gui, but it is
  /// not called all the time!
  /// </summary>
  public virtual void OnUpdateRendererEditor(bool isHeavyUpdate) {
    this.isHeavyUpdate = isHeavyUpdate;
  }
#endif

  public abstract bool IsValidElement<T>();
  public abstract bool IsValidElement(LeapGuiElement element);

  public abstract LeapGuiElement GetValidElementOnObject(GameObject obj);

  protected void CreateOrSave<T>(ref T t, string assetName) where T : SceneTiedAsset {
    T newT = t;
    if (SceneTiedAsset.CreateOrSave(ref newT,
                                    gameObject.scene,
                                    DATA_FOLDER_NAME,
                                    assetName,
                                    _persistentId)) {
      Undo.RecordObject(this, "Updated element data");
      t = newT;
      EditorUtility.SetDirty(this);
    }
  }
}

public abstract class LeapGuiRenderer<ElementType> : LeapGuiRendererBase
  where ElementType : LeapGuiElement {
  public const string ASSET_PATH = "Assets/Generated/RendererData/";

  public override bool IsValidElement<T>() {
    Type t = typeof(T);
    Type elementType = typeof(ElementType);

    return t == elementType || (t.IsSubclassOf(elementType));
  }

  public override bool IsValidElement(LeapGuiElement element) {
    return element is ElementType;
  }

  public override LeapGuiElement GetValidElementOnObject(GameObject obj) {
    return obj.GetComponent<ElementType>();
  }
}
