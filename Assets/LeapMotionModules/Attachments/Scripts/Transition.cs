using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;

namespace Leap.Unity{
  
  [ExecuteInEditMode]
  public class Transition : MonoBehaviour {
  
    public bool AnimatePosition = false;
    public Vector3 RelativeOnPosition = Vector3.zero;
    public AnimationCurve XPosition;
    public AnimationCurve YPosition;
    public AnimationCurve ZPosition;
    public bool AnimateRotation = false;
    public Vector3 RelativeOnRotation = Vector3.zero;
    public AnimationCurve XRotation;
    public AnimationCurve YRotation;
    public AnimationCurve ZRotation;
    public bool AnimateScale = false;
    public Vector3 RelativeOnScale = Vector3.one;
    public AnimationCurve XScale;
    public AnimationCurve YScale;
    public AnimationCurve ZScale;
    [Range(.001f, 2.0f)]
    public float Duration = 0.5f; //seconds
    [Range (-1, 1)]
    public float Simulate = 0.0f;
    [Range (0, 1)]
    public float Progress = 1.0f; //Start with full transition influence
  
    public UnityEvent OnComplete;

  #if UNITY_EDITOR
    void Update() {
      if (!EditorApplication.isPlaying) {
        updateTransition(Progress);
      }
    }
  #endif
  
    private void Awake(){
      updateTransition(1.0f);
    }

    public void TransitionIn(){
      if (isActiveAndEnabled) {
        StopAllCoroutines();
        StartCoroutine(transitionIn());
      }
    }
  
    public void TransitionOut(){
      if (isActiveAndEnabled) {
        StopAllCoroutines();
        StartCoroutine(transitionOut());
      }
    }
  
    IEnumerator transitionIn(){
      float start = Time.time;
      do {
        Progress = Progress - (Time.time - start)/Duration;
        updateTransition(Progress);
        yield return null;
      } while(Progress >= 0);
      Progress = 0;
      OnComplete.Invoke();
    }
  
    IEnumerator transitionOut(){
      float start = Time.time;
      do {
        Progress = (Time.time - start)/Duration;
        updateTransition(Progress);
        yield return null;
      } while(Progress <= 1);
      Progress = 1;
      OnComplete.Invoke();
    }
  
    void updateTransition(float interpolationPoint){
      if(AnimatePosition){
        Vector3 localPosition = transform.localPosition;
        localPosition.x = XPosition.Evaluate(interpolationPoint) * RelativeOnPosition.x;
        localPosition.y = YPosition.Evaluate(interpolationPoint) * RelativeOnPosition.y;
        localPosition.z = ZPosition.Evaluate(interpolationPoint) * RelativeOnPosition.z;
        transform.localPosition = localPosition;
      }
      if(AnimateRotation){
        Quaternion transitionRotation = Quaternion.Euler(transform.localRotation.x + XRotation.Evaluate(interpolationPoint) * RelativeOnRotation.x,
                                                         transform.localRotation.y + YRotation.Evaluate(interpolationPoint) * RelativeOnRotation.y,
                                                         transform.localRotation.z + ZRotation.Evaluate(interpolationPoint) * RelativeOnRotation.z);
        transform.localRotation = transitionRotation;
      }
      if (AnimateScale) {
        Vector3 localScale = transform.localScale;
        localScale.x = XScale.Evaluate(1 - interpolationPoint) * RelativeOnScale.x;
        localScale.y = YScale.Evaluate(1 - interpolationPoint) * RelativeOnScale.y;
        localScale.z = ZScale.Evaluate(1 - interpolationPoint) * RelativeOnScale.z;
        transform.localScale = localScale;
      }
    }
  }
}