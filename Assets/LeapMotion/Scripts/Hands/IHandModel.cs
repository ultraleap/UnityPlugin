using UnityEngine;
using System.Collections;
using Leap;
#if UNITY_EDITOR
using UnityEditor;
#endif


public enum Chirality { Left, Right, Either };
public enum ModelType { Graphics, Physics };

[ExecuteInEditMode]
public abstract class IHandModel : MonoBehaviour {
  public abstract Chirality Handedness { get; }
  public abstract ModelType HandModelType { get; }
  public virtual void InitHand(){
    //Debug.Log("IHandModel.InitHand()");
  }
  public abstract void UpdateHand();
  public abstract Hand GetLeapHand(); 
  public abstract void SetLeapHand(Hand hand);
  private bool isLeft;
#if UNITY_EDITOR
  void Awake() {
    if (!EditorApplication.isPlaying) {
      //Debug.Log("IHandModel.Awake()");
      if (Handedness == Chirality.Left) {
        isLeft = true;
      }
      SetLeapHand(TestHandFactory.MakeTestHand(0, 0, isLeft).TransformedCopy(GetLeapMatrix()));
      InitHand();
    }
  }
  void Update() {
    if (!EditorApplication.isPlaying) {
      if (Handedness == Chirality.Left) {
        isLeft = true;
      }
      //Debug.Log("IHandModel.Update()");
      SetLeapHand(TestHandFactory.MakeTestHand(0, 0, isLeft).TransformedCopy(GetLeapMatrix()));
      UpdateHand();
    }
  }
#endif
  //Todo move this to a utility.  Needs to be same as Provider
  /** Conversion factor for millimeters to meters. */
  protected const float MM_TO_M = 1e-3f;
  private Matrix GetLeapMatrix() {
    Transform t = this.transform.transform;
    Vector xbasis = new Vector(t.right.x, t.right.y, t.right.z) * t.lossyScale.x * MM_TO_M;
    Vector ybasis = new Vector(t.up.x, t.up.y, t.up.z) * t.lossyScale.y * MM_TO_M;
    Vector zbasis = new Vector(t.forward.x, t.forward.y, t.forward.z) * -t.lossyScale.z * MM_TO_M;
    Vector trans = new Vector(t.position.x, t.position.y, t.position.z);
    return new Matrix(xbasis, ybasis, zbasis, trans);
  }
}


