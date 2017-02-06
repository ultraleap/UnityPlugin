using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public abstract class TransformHandle : MonoBehaviour {

  protected TransformTool _tool;

  protected virtual void Start() {
    _tool = GetComponentInParent<TransformTool>();
  }

}
