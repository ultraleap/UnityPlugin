/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.Attributes;
using System.Collections.Generic;

namespace Leap.Unity.Interaction {

  ///<summary>
  /// A physics-enabled slider. Sliding is triggered by physically pushing the slider to its compressed position. 
  /// Increasing the horizontal and vertical slide limits allows it to act as either a 1D or 2D slider.
  ///</summary>
  public class InteractionSlider : InteractionButton {

    [Space, Space]
    [Tooltip("The minimum and maximum values that the slider reports on the horizontal axis.")]
    public Vector2 horizontalValueRange = new Vector2(0f, 1f);
    [Tooltip("The minimum and maximum values that the slider reports on the horizontal axis.")]
    public Vector2 verticalValueRange = new Vector2(0f, 1f);

    [Space]
    [Tooltip("The minimum and maximum horizontal extents that the slider can slide to in world space.")]
    [MinMax(-0.5f, 0.5f)]
    public Vector2 horizontalSlideLimits = new Vector2(-0.05f, 0.05f);
    [MinMax(-0.5f, 0.5f)]
    [Tooltip("The minimum and maximum vertical extents that the slider can slide to in world space.")]
    public Vector2 verticalSlideLimits = new Vector2(0f, 0f);

    [Tooltip("The number of discrete quantized notches that this slider can occupy on the horizontal axis.")]
    [MinValue(0)]
    public int horizontalSteps = 0;
    [Tooltip("The number of discrete quantized notches that this slider can occupy on the vertical axis.")]
    [MinValue(0)]
    public int verticalSteps = 0;

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }
    ///<summary> Triggered while this slider is depressed. </summary>
    public FloatEvent horizontalSlideEvent = new FloatEvent();
    ///<summary> Triggered while this slider is depressed. </summary>
    public FloatEvent verticalSlideEvent = new FloatEvent();

    public float HorizontalSliderPercent {
      get {
        return _horizontalSliderPercent;
      }
      set {
        if (!_started) Start();

        _horizontalSliderPercent = value;
        localPhysicsPosition.x = Mathf.Lerp(initialLocalPosition.x + horizontalSlideLimits.x, initialLocalPosition.x + horizontalSlideLimits.y, _horizontalSliderPercent);
        physicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        rigidbody.position = physicsPosition;
      }
    }

    public float VerticalSliderPercent {
      get {
        return _verticalSliderPercent;
      }
      set {
        if (!_started) Start();

        _verticalSliderPercent = value;
        localPhysicsPosition.y = Mathf.Lerp(initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y, _verticalSliderPercent);
        physicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        rigidbody.position = physicsPosition;
      }
    }

    ///<summary> This slider's horizontal slider value, mapped between the values in the HorizontalValueRange. </summary>
    public float HorizontalSliderValue {
      get {
        return Mathf.Lerp(horizontalValueRange.x, horizontalValueRange.y, _horizontalSliderPercent);
      }
      set {
        HorizontalSliderPercent = Mathf.InverseLerp(horizontalValueRange.x, horizontalValueRange.y, value);
      }
    }

    ///<summary> This slider's current vertical slider value, mapped between the values in the VerticalValueRange. </summary>
    public float VerticalSliderValue {
      get {
        return Mathf.Lerp(verticalValueRange.x, verticalValueRange.y, _verticalSliderPercent);
      }
      set {
        VerticalSliderPercent = Mathf.InverseLerp(verticalValueRange.x, verticalValueRange.y, value);
      }
    }

    //Internal Slider Values
    protected float _horizontalSliderPercent;
    protected float _verticalSliderPercent;
    protected RectTransform parent;

    private bool _started = false;

    protected override void Start() {
      if (_started) return;

      _started = true;

      if (transform.parent != null) {
        parent = transform.parent.GetComponent<RectTransform>();
        if (parent != null) {
          if (parent.rect.width < 0f || parent.rect.height < 0f) {
            Debug.LogError("Parent Rectangle dimensions negative; can't set slider boundaries!", parent.gameObject);
            enabled = false;
          } else {
            horizontalSlideLimits = new Vector2(parent.rect.xMin - transform.localPosition.x, parent.rect.xMax - transform.localPosition.x);
            verticalSlideLimits = new Vector2(parent.rect.yMin - transform.localPosition.y, parent.rect.yMax - transform.localPosition.y);
          }
        }
      }

      base.Start();
    }

    protected override void Update() {
      base.Update();

      if (isDepressed || isGrasped) {
        calculateSliderValues();
      }
    }

    protected override void OnEnable() {
      base.OnEnable();
      OnContactStay += calculateSliderValues;
    }

    protected override void OnDisable() {
      OnContactStay -= calculateSliderValues;
      base.OnDisable();
    }

    private void calculateSliderValues() {
      //Calculate the Renormalized Slider Values
      if (horizontalSlideLimits.x != horizontalSlideLimits.y) {
        _horizontalSliderPercent = Mathf.InverseLerp(initialLocalPosition.x + horizontalSlideLimits.x, initialLocalPosition.x + horizontalSlideLimits.y, localPhysicsPosition.x);
        horizontalSlideEvent.Invoke(HorizontalSliderValue);
      }

      if (verticalSlideLimits.x != verticalSlideLimits.y) {
        _verticalSliderPercent = Mathf.InverseLerp(initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y, localPhysicsPosition.y);
        verticalSlideEvent.Invoke(VerticalSliderValue);
      }
    }

    protected override Vector3 getDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      Vector3 unSnappedPosition = 
        new Vector3(Mathf.Clamp((localPhysicsPosition.x + desiredOffset.x), initialLocalPosition.x + horizontalSlideLimits.x, initialLocalPosition.x + horizontalSlideLimits.y),
                    Mathf.Clamp((localPhysicsPosition.y + desiredOffset.y), initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y),
                                (localPhysicsPosition.z + desiredOffset.z));

      float hSliderPercent = Mathf.InverseLerp(initialLocalPosition.x + horizontalSlideLimits.x, initialLocalPosition.x + horizontalSlideLimits.y, unSnappedPosition.x);
      if (horizontalSteps > 0) {
        hSliderPercent = Mathf.Round(hSliderPercent * (horizontalSteps)) / (horizontalSteps);
      }

      float vSliderPercent = Mathf.InverseLerp(initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y, unSnappedPosition.y);
      if (verticalSteps > 0) {
        vSliderPercent = Mathf.Round(vSliderPercent * (verticalSteps)) / (verticalSteps);
      }

      return new Vector3(Mathf.Lerp(initialLocalPosition.x + horizontalSlideLimits.x, initialLocalPosition.x + horizontalSlideLimits.y, hSliderPercent),
                         Mathf.Lerp(initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y, vSliderPercent),
                                   (localPhysicsPosition.z + desiredOffset.z));
    }

    protected override void OnDrawGizmosSelected() {
      base.OnDrawGizmosSelected();
      if (transform.parent != null) {
        Vector3 originPosition = Application.isPlaying ? initialLocalPosition : transform.localPosition;

        parent = transform.parent.GetComponent<RectTransform>();
        if (parent != null) {
          horizontalSlideLimits = new Vector2(parent.rect.xMin - originPosition.x, parent.rect.xMax - originPosition.x);
          verticalSlideLimits = new Vector2(parent.rect.yMin - originPosition.y, parent.rect.yMax - originPosition.y);
        }

        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(originPosition + 
          new Vector3((horizontalSlideLimits.x + horizontalSlideLimits.y) * 0.5f, (verticalSlideLimits.x + verticalSlideLimits.y) * 0.5f, 0f),
          new Vector3(horizontalSlideLimits.y - horizontalSlideLimits.x, verticalSlideLimits.y - verticalSlideLimits.x, 0f));
      }
    }
  }
}
