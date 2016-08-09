using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity{
  
  [ExecuteInEditMode]
  public class Transition : MonoBehaviour {
  
    public bool AnimatePosition = false;
    public Vector3 OutPosition = Vector3.zero;
    public AnimationCurve XPosition = new AnimationCurve(new Keyframe(-1,1), new Keyframe(0,0), new Keyframe(1,1));
    public AnimationCurve YPosition = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    public AnimationCurve ZPosition = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    public bool AnimateRotation = false;
    public Vector3 OutRotation = Vector3.zero;
    public AnimationCurve XRotation = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    public AnimationCurve YRotation = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    public AnimationCurve ZRotation = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    public bool AnimateScale = false;
    public AnimationCurve XScale = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 1), new Keyframe(1, 1));
    public AnimationCurve YScale = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 1), new Keyframe(1, 1));
    public AnimationCurve ZScale = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 1), new Keyframe(1, 1));
    public bool AnimateColor = false;
    public string ColorShaderPropertyName = "_Color";
    public Color OutColor = Color.black;
    public AnimationCurve ColorCurve = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));

    [Range(.001f, 2.0f)]
    public float Duration = 0.5f; //seconds
    [Range (-1, 1)]
    public float Simulate = 0.0f;
    
    private float progress = 0.0f;
    private MaterialPropertyBlock materialProperties;
    private Vector3 localPosition;
    private Quaternion localRotation;
    private Vector3 localScale;

    public UnityEvent OnComplete;

  #if UNITY_EDITOR
    private void Reset() {
      captureInitialState();
    }

    private void Update() {
      if (!EditorApplication.isPlaying) {
        updateTransition(Simulate);
      }
    }
  #endif
  
    private void Awake(){
      materialProperties = new MaterialPropertyBlock();
      captureInitialState();
      updateTransition(0.0f);
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
  
    protected virtual void captureInitialState() {
      localPosition = transform.localPosition;
      localRotation = transform.localRotation;
      localScale = transform.localScale;
    }

    public virtual void GotoOnState() {
      transform.localPosition = localPosition;
      transform.localRotation = localRotation;
      transform.localScale = localScale;
    }

    protected IEnumerator transitionIn(){
      float start = Time.time;
      do {
        progress = (Time.time - start)/Duration;
        updateTransition(progress - 1);
        yield return null;
      } while(progress <= 1);
      progress = 0;
      OnComplete.Invoke();
    }

    protected IEnumerator transitionOut(){
      float start = Time.time;
      do {
        progress = (Time.time - start)/Duration;
        updateTransition(progress);
        yield return null;
      } while(progress <= 1);
      progress = 0;
      OnComplete.Invoke();
    }

    protected virtual void updateTransition(float interpolationPoint){
      if (AnimatePosition) doAnimatePosition(interpolationPoint);
      if (AnimateRotation) doAnimateRotation(interpolationPoint);
      if (AnimateScale) doAnimateScale(interpolationPoint);
      if (AnimateColor) doAnimateColor(interpolationPoint);
    }

    protected virtual void doAnimatePosition(float interpolationPoint) {
      Vector3 localPosition = transform.localPosition;
      localPosition.x = XPosition.Evaluate(interpolationPoint) * OutPosition.x;
      localPosition.y = YPosition.Evaluate(interpolationPoint) * OutPosition.y;
      localPosition.z = ZPosition.Evaluate(interpolationPoint) * OutPosition.z;
      transform.localPosition = localPosition;
    }

    protected virtual void doAnimateRotation(float interpolationPoint) {
      Quaternion transitionRotation = Quaternion.Euler(localRotation.x + XRotation.Evaluate(interpolationPoint) * OutRotation.x,
                                                       localRotation.y + YRotation.Evaluate(interpolationPoint) * OutRotation.y,
                                                       localRotation.z + ZRotation.Evaluate(interpolationPoint) * OutRotation.z);
      transform.localRotation = transitionRotation;
    }

    protected virtual void doAnimateScale(float interpolationPoint) {
      Vector3 tempScale = localScale;
      tempScale.x = XScale.Evaluate(interpolationPoint);
      tempScale.y = YScale.Evaluate(interpolationPoint);
      tempScale.z = ZScale.Evaluate(interpolationPoint);
      transform.localScale = tempScale;
    }

    protected virtual void doAnimateColor(float interpolationPoint) {
      Transform[] children = GetComponentsInChildren<Transform>(true);
      for (int g = 0; g < children.Length; g++) {
        Renderer renderer = children[g].gameObject.GetComponent<Renderer>();
        if (renderer != null) {
          materialProperties = new MaterialPropertyBlock();
          renderer.GetPropertyBlock(materialProperties);
          materialProperties.SetColor(ColorShaderPropertyName, Color.Lerp(renderer.sharedMaterial.color, OutColor, ColorCurve.Evaluate(interpolationPoint)));
          renderer.SetPropertyBlock(materialProperties);
        }
      }
    }

  }
}