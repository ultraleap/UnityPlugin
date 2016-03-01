using UnityEngine;
using System;
using System.Collections;
using Leap;
#if UNITY_EDITOR
using UnityEditor;
#endif


public enum Chirality { Left, Right, Either };
public enum ModelType { Graphics, Physics };

[ExecuteInEditMode]
public abstract class IHandModel : MonoBehaviour {
  public event Action OnBegin;
  public event Action OnFinish;
  private bool isTracked = false;
  public bool IsTracked {
    get { return isTracked; }
  }
  public abstract Chirality Handedness { get; }
  public abstract ModelType HandModelType { get; }
  public virtual void InitHand(){
    //Debug.Log("IHandModel.InitHand()");
  }

  public virtual void BeginHand() {
    if (OnBegin != null) {
      OnBegin();
    }
    isTracked = true;
  }
  public abstract void UpdateHand();
  public virtual void FinishHand() {
    if (OnFinish != null) {
      OnFinish();
    }
    isTracked = false;
  }
  public abstract IHand GetLeapHand(); 
  public abstract void SetLeapHand(IHand hand);
#if UNITY_EDITOR
  void Awake() {
    if (!EditorApplication.isPlaying) {
      //Debug.Log("IHandModel.Awake()");
      Matrix leapMatrix = UnityMatrixExtension.GetLeapMatrix(transform);
      SetLeapHand(TestHandFactory.MakeTestHand(0, 0, Handedness == Chirality.Left).TransformedCopy(ref leapMatrix));
      InitHand();
    }
  }
  void Update() {
    if (!EditorApplication.isPlaying) {
      //Debug.Log("IHandModel.Update()");
      Matrix leapMatrix = UnityMatrixExtension.GetLeapMatrix(transform);
      SetLeapHand(TestHandFactory.MakeTestHand(0, 0, Handedness == Chirality.Left).TransformedCopy(ref leapMatrix));
      UpdateHand();
    }
  }
#endif
}


