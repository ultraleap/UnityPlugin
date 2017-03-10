using System;
using System.Collections.Generic;
using UnityEngine;

public abstract class LeapGuiRendererBase : LeapGuiComponentBase<LeapGui> {

  [HideInInspector]
  public LeapGui gui;

  [HideInInspector]
  public LeapGuiGroup group;

  public abstract SupportInfo GetSpaceSupportInfo(LeapGuiSpace space);

  protected override void OnValidate() {
    base.OnValidate();

#if UNITY_EDITOR
    if (!Application.isPlaying) {
      if (gui != null) {
        gui.ScheduleEditorUpdate();
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

  public abstract bool IsValidElement<Type>();
  public abstract bool IsValidElement(LeapGuiElement element);

  public abstract LeapGuiElement GetValidElementOnObject(GameObject obj);
}

public abstract class LeapGuiRenderer<ElementType> : LeapGuiRendererBase where ElementType : LeapGuiElement {

  public override bool IsValidElement<Type>() {
    return typeof(Type).IsSubclassOf(typeof(ElementType));
  }

  public override bool IsValidElement(LeapGuiElement element) {
    return element is ElementType;
  }

  public override LeapGuiElement GetValidElementOnObject(GameObject obj) {
    return obj.GetComponent<ElementType>();
  }
}
