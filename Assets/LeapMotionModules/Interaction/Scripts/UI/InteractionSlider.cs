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
    [MinMax(-0.25f, 0.25f)]
    public Vector2 horizontalSlideLimits = new Vector2(-0.05f, 0.05f);
    [MinMax(-0.25f, 0.25f)]
    [Tooltip("The minimum and maximum vertical extents that the slider can slide to in world space.")]
    public Vector2 verticalSlideLimits = new Vector2(0f, 0f);

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
        localPhysicsPosition.x = Mathf.Lerp(initialLocalPosition.x + _horizontalSlideLimits.x, initialLocalPosition.x + _horizontalSlideLimits.y, _horizontalSliderPercent);
      }
    }

    public float VerticalSliderPercent {
      get {
        return _verticalSliderPercent;
      }
      set {
        if (!_started) Start();

        _verticalSliderPercent = value;
        localPhysicsPosition.y = Mathf.Lerp(initialLocalPosition.y + _verticalSlideLimits.x, initialLocalPosition.y + _verticalSlideLimits.y, _verticalSliderPercent);
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

    //Slide limits normalized to local space
    protected Vector2 _horizontalSlideLimits;
    protected Vector2 _verticalSlideLimits;

    //Internal Slider Values
    protected float _horizontalSliderPercent;
    protected float _verticalSliderPercent;

    private bool _started = false;

    protected override void Start() {
      if (_started) return;

      _started = true;

      //Conversion of limits to local space
      _horizontalSlideLimits = horizontalSlideLimits;
      _verticalSlideLimits = verticalSlideLimits;

      base.Start();
    }

    protected override void Update() {
      base.Update();

      if (isDepressed || isGrasped) {
        CalculateSliderValues();
      }
    }

    private void CalculateSliderValues() {
      //Calculate the Renormalized Slider Values
      if (_horizontalSlideLimits.x != _horizontalSlideLimits.y) {
        _horizontalSliderPercent = Mathf.InverseLerp(initialLocalPosition.x + _horizontalSlideLimits.x, initialLocalPosition.x + _horizontalSlideLimits.y, localPhysicsPosition.x);
        horizontalSlideEvent.Invoke(HorizontalSliderValue);
      }

      if (_verticalSlideLimits.x != _verticalSlideLimits.y) {
        _verticalSliderPercent = Mathf.InverseLerp(initialLocalPosition.y + _verticalSlideLimits.x, initialLocalPosition.y + _verticalSlideLimits.y, localPhysicsPosition.y);
        verticalSlideEvent.Invoke(VerticalSliderValue);
      }
    }

    protected override Vector3 GetDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(Mathf.Clamp((localPhysicsPosition.x + desiredOffset.x), initialLocalPosition.x + _horizontalSlideLimits.x, initialLocalPosition.x + _horizontalSlideLimits.y),
                         Mathf.Clamp((localPhysicsPosition.y + desiredOffset.y), initialLocalPosition.y + _verticalSlideLimits.x, initialLocalPosition.y + _verticalSlideLimits.y),
                                     (localPhysicsPosition.z + desiredOffset.z));
    }

    protected override void OnDrawGizmosSelected() {
      base.OnDrawGizmosSelected();
      Vector3 originPosition = Application.isPlaying ? initialLocalPosition : transform.localPosition;
      Vector2 limits = (horizontalSlideLimits);

      Gizmos.color = Color.blue;
      Gizmos.DrawLine(originPosition + (Vector3.right* limits.x), originPosition + (Vector3.right * limits.y));
    }
  }
}
