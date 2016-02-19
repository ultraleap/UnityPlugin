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
      SetLeapHand(TestHandFactory.MakeTestHand(0, 0, isLeft).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(transform)));
      InitHand();
    }
  }
  void Update() {
    if (!EditorApplication.isPlaying) {
      if (Handedness == Chirality.Left) {
        isLeft = true;
      }
      //Debug.Log("IHandModel.Update()");
      SetLeapHand(TestHandFactory.MakeTestHand(0, 0, isLeft).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(transform)));
      UpdateHand();
    }
  }
#endif
}


