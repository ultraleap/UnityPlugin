using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LeapGuiRenderer : ScriptableObject {

  [HideInInspector]
  public LeapGui gui;

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
  public abstract void OnUpdateRendererEditor();
}
