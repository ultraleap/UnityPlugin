using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class LeapGuiRenderer : ScriptableObject {

  public LeapGui gui;

  public abstract void OnEnableRenderer();
  public abstract void OnDisableRenderer();
  public abstract void OnUpdateRenderer();

  public abstract void OnEnableRendererEditor();
  public abstract void OnDisableRendererEditor();
  public abstract void OnUpdateRendererEditor();
}
