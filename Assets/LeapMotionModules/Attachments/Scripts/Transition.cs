using UnityEngine;
using UnityEngine.Events;
#if UNITY_EDITOR
using UnityEditor;
#endif
using System.Collections;
using System.Collections.Generic;

namespace Leap.Unity{
  
  /**
  * The Transition class animates the position, rotation, scale, and color of child game objects.
  * Use a Transition to animate hand attachments when they turn on or off. For example, you could
  * make an arm HUD rotate or fade into view when activated.
  *
  * The Transition component should be placed on an empty game object that is a child of one of the hand 
  * attachment transforms (i.e. for the palm, one of the fingers, etc.). The attached objects to be affected 
  * by the transition should be children of that empty game object.
  *
  * Assign the Transition component to the AttachmentController.Transition property. The AttachmentController 
  * will then call the Transition methods at the appropriate times.
  * 
  * The Transition component uses AnimationCurve objects to control the animation. The curves you use should cover a 
  * domain of -1 to 1. The portion of the curve from -1 to 0 is used for the "in" transition (off to on state); the portion
  * from 0 to 1 is used for the "out" transition (on to off state).
  *
  * You can use the Simulate slider in the Unity inspector to observe the affect of the transition in the editor.
  * @since 4.1.4
  */
  [ExecuteInEditMode]
  public class Transition : MonoBehaviour {
  
    /**
    * Specifies whether to animate position.
    * The position of the Transition game object is animated. Any child objects maintain their
    * respective local positions relative to the transition object.
    * @since 4.1.3
    */
    public bool AnimatePosition = false;

    /**
    * The position of the transition object in the fully transitioned state.
    * @since 4.1.3
    */
    public Vector3 OutPosition = Vector3.zero;

    /**
    * The curve controlling 
    * @since 4.1.3
    */
    public AnimationCurve PositionCurve = new AnimationCurve(new Keyframe(-1,1), new Keyframe(0,0), new Keyframe(1,1));

    /**
    *
    * @since 4.1.3
    */
    public bool AnimateRotation = false;

    /**
    *
    * @since 4.1.3
    */
    public Vector3 OutRotation = Vector3.zero;

    /**
    *
    * @since 4.1.3
    */

    public AnimationCurve RotationCurve = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    /**
    *
    * @since 4.1.3
    */

    public bool AnimateScale = false;
    
    /**
    *
    * @since 4.1.3
    */
    public Vector3 OutScale = Vector3.one;

    /**
    *
    * @since 4.1.3
    */
    public AnimationCurve ScaleCurve = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));
    
    /**
    *
    * @since 4.1.3
    */
    public bool AnimateColor = false;

    /**
    *
    * @since 4.1.3
    */
    public string ColorShaderPropertyName = "_Color";
    
    /**
    *
    * @since 4.1.3
    */
    public Color OutColor = Color.black;
    
    /**
    *
    * @since 4.1.3
    */
    public AnimationCurve ColorCurve = new AnimationCurve(new Keyframe(-1, 1), new Keyframe(0, 0), new Keyframe(1, 1));

    /**
    *
    * @since 4.1.3
    */
    [Range(.001f, 2.0f)]
    public float Duration = 0.5f; //seconds
    
    /**
    *
    * @since 4.1.3
    */
    [Range (-1, 1)]
    public float Simulate = 0.0f;
    
    private float progress = 0.0f;
    private MaterialPropertyBlock materialProperties;
    private Vector3 localPosition;
    private Quaternion localRotation;
    private Vector3 localScale;

    /**
    *
    * @since 4.1.3
    */
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

    /**
    *
    * @since 4.1.3
    */
    public void TransitionIn(){
      if (isActiveAndEnabled) {
        StopAllCoroutines();
        StartCoroutine(transitionIn());
      }
    }

    /**
    *
    * @since 4.1.3
    */
    public void TransitionOut(){
      if (isActiveAndEnabled) {
        StopAllCoroutines();
        StartCoroutine(transitionOut());
      }
    }

    /**
    *
    * @since 4.1.3
    */
    protected virtual void captureInitialState() {
      localPosition = transform.localPosition;
      localRotation = transform.localRotation;
      localScale = transform.localScale;
    }

    /**
    *
    * @since 4.1.3
    */
    public virtual void GotoOnState() {
      transform.localPosition = localPosition;
      transform.localRotation = localRotation;
      transform.localScale = localScale;
    }

    /**
    *
    * @since 4.1.3
    */
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

    /**
    *
    * @since 4.1.3
    */
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

    /**
    *
    * @since 4.1.3
    */
    protected virtual void updateTransition(float interpolationPoint){
      if (AnimatePosition) doAnimatePosition(interpolationPoint);
      if (AnimateRotation) doAnimateRotation(interpolationPoint);
      if (AnimateScale) doAnimateScale(interpolationPoint);
      if (AnimateColor) doAnimateColor(interpolationPoint);
    }

    /**
    *
    * @since 4.1.3
    */
    protected virtual void doAnimatePosition(float interpolationPoint) {
      Vector3 localPosition = transform.localPosition;
      localPosition.x = PositionCurve.Evaluate(interpolationPoint) * OutPosition.x;
      localPosition.y = PositionCurve.Evaluate(interpolationPoint) * OutPosition.y;
      localPosition.z = PositionCurve.Evaluate(interpolationPoint) * OutPosition.z;
      transform.localPosition = localPosition;
    }

    /**
    *
    * @since 4.1.3
    */
    protected virtual void doAnimateRotation(float interpolationPoint) {
      Quaternion transitionRotation = Quaternion.Euler(localRotation.x + RotationCurve.Evaluate(interpolationPoint) * OutRotation.x,
                                                       localRotation.y + RotationCurve.Evaluate(interpolationPoint) * OutRotation.y,
                                                       localRotation.z + RotationCurve.Evaluate(interpolationPoint) * OutRotation.z);
      transform.localRotation = transitionRotation;
    }

    /**
    *
    * @since 4.1.3
    */
    protected virtual void doAnimateScale(float interpolationPoint) {
      Vector3 tempScale = localScale;
      tempScale.x = ScaleCurve.Evaluate(interpolationPoint) * OutScale.x;
      tempScale.y = ScaleCurve.Evaluate(interpolationPoint) * OutScale.y;
      tempScale.z = ScaleCurve.Evaluate(interpolationPoint) * OutScale.z;
      transform.localScale = Vector3.Lerp(localScale, OutScale, ScaleCurve.Evaluate(interpolationPoint));
    }

    /**
    *
    * @since 4.1.3
    */
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