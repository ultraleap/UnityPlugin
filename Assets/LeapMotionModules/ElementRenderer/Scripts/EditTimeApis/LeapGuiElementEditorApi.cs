using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract partial class LeapGuiElement : MonoBehaviour {

  public readonly EditorApi editor;

  protected class EditorApi {
    private readonly LeapGuiElement _element;

    public Mesh pickingMesh;

    public EditorApi(LeapGuiElement element) {
      _element = element;
    }

    public void OnAttachedToGui(LeapGuiGroup group, Transform anchor) {
      if (!Application.isPlaying) {
        _preferredRendererType = group.renderer.GetType();
      }
    }

  }




}
