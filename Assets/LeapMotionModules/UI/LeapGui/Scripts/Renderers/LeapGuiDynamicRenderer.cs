using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
#if UNITY_EDITOR
using UnityEditor;
#endif
using Leap.Unity.Query;
using Leap.Unity.Attributes;

public class LeapGuiDynamicRenderer : LeapGuiRenderer {

  public override void OnEnableRenderer() {
    throw new NotImplementedException();
  }

  public override void OnDisableRenderer() {
    throw new NotImplementedException();
  }

  public override void OnUpdateRenderer() {
    throw new NotImplementedException();
  }

  public override void OnEnableRendererEditor() {
    throw new NotImplementedException();
  }

  public override void OnDisableRendererEditor() {
    throw new NotImplementedException();
  }

  public override void OnUpdateRendererEditor() {
    throw new NotImplementedException();
  }
}
