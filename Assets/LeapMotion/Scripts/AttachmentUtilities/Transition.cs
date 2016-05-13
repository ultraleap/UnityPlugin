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
    [Range (0, 1)]
    public float Simulate = 0.0f; //0..1
    public float Speed = 1.0f;
    public float progress = 0;
  
    public UnityEvent OnComplete;
  #if UNITY_EDITOR
    void Update() {
      if (!EditorApplication.isPlaying) {
        updateTransition(1 - Simulate);
      }
    }
  #endif
  
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
        progress = (Time.time - start)/Duration;
        updateTransition(progress);
        yield return null;
      } while(progress <= 1);
      progress = 1;
      OnComplete.Invoke();
    }
  
    IEnumerator transitionOut(){
      float start = Time.time;
      do {
        progress = progress - (Time.time - start)/Duration;
        updateTransition(progress);
        yield return null;
      } while(progress >= 0);
      progress = 0;
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
      if(AnimateScale){
        Vector3 localScale = transform.localScale;
        localScale.x = XScale.Evaluate(interpolationPoint) * RelativeOnScale.x;
        localScale.y = YScale.Evaluate(interpolationPoint) * RelativeOnScale.y;
        localScale.z = ZScale.Evaluate(interpolationPoint) * RelativeOnScale.z;
        transform.localScale = localScale;
      }
    }
  }
}