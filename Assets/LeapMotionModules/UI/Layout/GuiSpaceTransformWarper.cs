using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[ExecuteInEditMode]
[RequireComponent(typeof(LeapGuiElement))]
public class GuiSpaceTransformWarper : MonoBehaviour {

  public Transform toWarp;

  private LeapGuiElement _element;
  private LeapGui _attachedGui;
  private ITransformer _spaceWarper; // Space Warper, Transformer of Curved Spaces

  void Start() {
    TryInitialize();
  }

  private void TryInitialize() {
    try {
      _element = GetComponent<LeapGuiElement>();
      _attachedGui = GetComponentInParent<LeapGui>();
      _spaceWarper = _attachedGui.space.GetTransformer(_element.anchor);
    }
    catch (System.Exception) { }
  }

  void Update() {
    if (_spaceWarper == null) {
      TryInitialize();
    }

    if (_spaceWarper != null && toWarp != null) {
      toWarp.transform.position = _attachedGui.transform.TransformPoint(
                                    _spaceWarper.TransformPoint(
                                      _attachedGui.transform.InverseTransformPoint(
                                        this.transform.position)));

      toWarp.transform.rotation = _attachedGui.transform.TransformRotation(
                                    _spaceWarper.TransformRotation(
                                      _attachedGui.transform.InverseTransformRotation(
                                        this.transform.rotation)));
    }
  }

}
