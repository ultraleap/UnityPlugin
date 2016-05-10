using UnityEngine;
using System;
#if UNITY_EDITOR
using UnityEditor;
#endif

/** IHandModel defines abstract methods as a template for building Leap hand models*/
namespace Leap.Unity{
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
    public abstract Hand GetLeapHand(); 
    public abstract void SetLeapHand(Hand hand);

  #if UNITY_EDITOR
    void Awake() {
      if (!EditorApplication.isPlaying) {
        SetLeapHand(TestHandFactory.MakeTestHand(0, 0, Handedness == Chirality.Left).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(transform)));
        InitHand();
      }
    }

    void Update() {
      if (!EditorApplication.isPlaying) {
        SetLeapHand(TestHandFactory.MakeTestHand(0, 0, Handedness == Chirality.Left).TransformedCopy(UnityMatrixExtension.GetLeapMatrix(transform)));
        UpdateHand();
      }
    }
  #endif
  }
}
