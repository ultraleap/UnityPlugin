/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Leap.Unity.Attributes;
using System;

namespace Leap.Unity.Interaction {

  ///<summary>
  /// A physics-enabled slider. Sliding is triggered by physically pushing the slider to its compressed position.
  /// Increasing the horizontal and vertical slide limits allows it to act as either a 1D or 2D slider.
  ///</summary>
  public class InteractionSlider : InteractionButton {

    #region Inspector & Properties

    public enum SliderType {
      Vertical,
      Horizontal,
      TwoDimensional
    }

    [Header("Slider Settings")]
    public SliderType sliderType = SliderType.Horizontal;

    public bool dispatchSlideValueOnStart = true;

    [Tooltip("Manually specify slider limits even if the slider's parent has a RectTransform.")]
    [DisableIf("_parentHasRectTransform", isEqualTo: false)]
    public bool overrideRectLimits = false;
    [SerializeField, HideInInspector]
    #pragma warning disable 0414
    private bool _parentHasRectTransform = false;
    #pragma warning restore 0414

    [Header("Horizontal Axis")]
    public float defaultHorizontalValue;

    [Tooltip("The minimum and maximum values that the slider reports on the horizontal axis.")]
    [FormerlySerializedAs("horizontalValueRange")]
    [SerializeField]
    private Vector2 _horizontalValueRange = new Vector2(0f, 1f);
    public float minHorizontalValue {
      get {
        return _horizontalValueRange.x;
      }
      set {
        if (value != _horizontalValueRange.x) {
          _horizontalValueRange.x = value;
          HorizontalSlideEvent(HorizontalSliderValue);
        }
      }
    }

    public float maxHorizontalValue {
      get {
        return _horizontalValueRange.y;
      }
      set {
        if (value != _horizontalValueRange.y) {
          _horizontalValueRange.y = value;
          HorizontalSlideEvent(HorizontalSliderValue);
        }
      }
    }

    [Tooltip("The minimum and maximum horizontal extents that the slider can slide to in world space.")]
    [MinMax(-0.5f, 0.5f)]
    public Vector2 horizontalSlideLimits = new Vector2(-0.05f, 0.05f);

    [Tooltip("The number of discrete quantized notches **beyond the first** that this "
           + "slider can occupy on the horizontal axis. A value of zero indicates a "
           + "continuous (non-quantized) slider for this axis.")]
    [MinValue(0)]
    public int horizontalSteps = 0;

    [System.Serializable]
    public class FloatEvent : UnityEvent<float> { }
    ///<summary> Triggered while this slider is depressed. </summary>
    [SerializeField]
    [FormerlySerializedAs("horizontalSlideEvent")]
    private FloatEvent _horizontalSlideEvent = new FloatEvent();

    [Header("Vertical Axis")]
    public float defaultVerticalValue;

    [Tooltip("The minimum and maximum values that the slider reports on the horizontal axis.")]
    [FormerlySerializedAs("verticalValueRange")]
    [SerializeField]
    private Vector2 _verticalValueRange = new Vector2(0f, 1f);
    public float minVerticalValue {
      get {
        return _verticalValueRange.x;
      }
      set {
        if (value != _verticalValueRange.x) {
          _verticalValueRange.x = value;
          VerticalSlideEvent(VerticalSliderValue);
        }
      }
    }

    public float maxVerticalValue {
      get {
        return _verticalValueRange.y;
      }
      set {
        if (value != _verticalValueRange.y) {
          _verticalValueRange.y = value;
          VerticalSlideEvent(VerticalSliderValue);
        }
      }
    }

    [MinMax(-0.5f, 0.5f)]
    [Tooltip("The minimum and maximum vertical extents that the slider can slide to in world space.")]
    public Vector2 verticalSlideLimits = new Vector2(0f, 0f);

    [Tooltip("The number of discrete quantized notches **beyond the first** that this "
           + "slider can occupy on the vertical axis. A value of zero indicates a "
           + "continuous (non-quantized) slider for this axis.")]
    [MinValue(0)]
    public int verticalSteps = 0;

    ///<summary> Triggered while this slider is depressed. </summary>
    [SerializeField]
    [FormerlySerializedAs("verticalSlideEvent")]
    private FloatEvent _verticalSlideEvent = new FloatEvent();

    public Action<float> HorizontalSlideEvent = (f) => { };
    public Action<float> VerticalSlideEvent = (f) => { };

    public float HorizontalSliderPercent {
      get {
        return _horizontalSliderPercent;
      }
      set {
        if (!_started) {
          Debug.LogWarning("An object is attempting to access this slider's value before it has been initialized!  Initializing now; this could yield unexpected behaviour...", this);
          Start();
        }

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
        if (!_started) {
          Debug.LogWarning("An object is attempting to access this slider's value before it has been initialized!  Initializing now; this could yield unpected behaviour...", this);
          Start();
        }

        _verticalSliderPercent = value;
        localPhysicsPosition.y = Mathf.Lerp(initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y, _verticalSliderPercent);
        physicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        rigidbody.position = physicsPosition;
      }
    }

    ///<summary> This slider's horizontal slider value, mapped between the values in the HorizontalValueRange. </summary>
    public float HorizontalSliderValue {
      get {
        return Mathf.Lerp(_horizontalValueRange.x, _horizontalValueRange.y, _horizontalSliderPercent);
      }
      set {
        HorizontalSliderPercent = Mathf.InverseLerp(_horizontalValueRange.x, _horizontalValueRange.y, value);
      }
    }

    ///<summary> This slider's current vertical slider value, mapped between the values in the VerticalValueRange. </summary>
    public float VerticalSliderValue {
      get {
        return Mathf.Lerp(_verticalValueRange.x, _verticalValueRange.y, _verticalSliderPercent);
      }
      set {
        VerticalSliderPercent = Mathf.InverseLerp(_verticalValueRange.x, _verticalValueRange.y, value);
      }
    }

    private void calculateSliderValues() {
      // Calculate renormalized slider values.
      if (horizontalSlideLimits.x != horizontalSlideLimits.y) {
        _horizontalSliderPercent = Mathf.InverseLerp(initialLocalPosition.x + horizontalSlideLimits.x, initialLocalPosition.x + horizontalSlideLimits.y, localPhysicsPosition.x);
        HorizontalSlideEvent(HorizontalSliderValue);
      }

      if (verticalSlideLimits.x != verticalSlideLimits.y) {
        _verticalSliderPercent = Mathf.InverseLerp(initialLocalPosition.y + verticalSlideLimits.x, initialLocalPosition.y + verticalSlideLimits.y, localPhysicsPosition.y);
        VerticalSlideEvent(VerticalSliderValue);
      }
    }

    public float normalizedHorizontalValue {
      get {
        return _horizontalSliderPercent;
      }
      set {
        var newValue = Mathf.Clamp01(value);
        if (newValue != _horizontalSliderPercent) {
          _horizontalSliderPercent = newValue;
          HorizontalSlideEvent(HorizontalSliderValue);
        }
      }
    }

    public float normalizedVerticalValue {
      get {
        return _verticalSliderPercent;
      }
      set {
        var newValue = Mathf.Clamp01(value);
        if (newValue != _verticalSliderPercent) {
          _verticalSliderPercent = newValue;
          VerticalSlideEvent(VerticalSliderValue);
        }
      }
    }

    /// <summary>
    /// Returns the number of horizontal steps past the minimum value of the slider, for
    /// sliders with a non-zero number of horizontal steps. This value is independent of
    /// the horizontal value range of the slider. For example a slider with a
    /// horizontalSteps value of 9 could have horizontalStepValues of 0-9.
    /// </summary>
    public int horizontalStepValue {
      get {
        float range = _horizontalValueRange.y - _horizontalValueRange.x;
        if (range == 0F) return 0;
        else {
          return (int)(_horizontalSliderPercent * horizontalSteps * 1.001F);
        }
      }
    }

    /// <summary>
    /// Gets whether the slider was slide in the latest Update().
    /// </summary>
    public bool wasSlid {
      get { return _wasSlid && _sawWasSlid; }
    }

    #endregion

    #region Internal State

    protected float _horizontalSliderPercent;
    protected float _verticalSliderPercent;
    protected RectTransform parent;

    private bool _started = false;

    private bool _sawWasSlid = false;
    private bool _wasSlid = false;

    #endregion

    #region Unity Events

    protected override void OnValidate() {
      base.OnValidate();

      if (this.transform.parent != null && this.transform.parent.GetComponent<RectTransform>() != null) {
        _parentHasRectTransform = true;
      }
      else {
        _parentHasRectTransform = false;
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

    protected override void Start() {
      if (_started) return;

      _started = true;

      calculateSliderLimits();

      switch (sliderType) {
        case SliderType.Horizontal:
          verticalSlideLimits = new Vector2(0, 0);
          break;
        case SliderType.Vertical:
          horizontalSlideLimits = new Vector2(0, 0);
          break;
      }

      base.Start();

      HorizontalSlideEvent += _horizontalSlideEvent.Invoke;
      VerticalSlideEvent   += _verticalSlideEvent.Invoke;

      HorizontalSlideEvent += (f) => { _wasSlid = true; };
      VerticalSlideEvent   += (f) => { _wasSlid = true; };

      HorizontalSliderValue = defaultHorizontalValue;
      VerticalSliderValue = defaultVerticalValue;

      if (dispatchSlideValueOnStart) {
        HorizontalSlideEvent(HorizontalSliderValue);
        VerticalSlideEvent(VerticalSliderValue);
      }
    }

    protected override void Update() {
      base.Update();

      if (!Application.isPlaying) { return; }

      if (isPressed || isGrasped) {
        calculateSliderValues();
      }

      // Whenever "_wasSlid" is set to true (however many times between Update() cycles),
      // this logic produces a single Update() cycle through which slider.wasSlid
      // will return true. (This allows observing MonoBehaviours to simply check
      // "wasSlid" during their Update() cycle to perform updating logic.)
      if (_wasSlid && !_sawWasSlid) {
        _sawWasSlid = true;
      }
      else if (_sawWasSlid) {
        _wasSlid = false;
        _sawWasSlid = false;
      }
    }

    #endregion

    #region Public API

    public void RecalculateSliderLimits() {
      calculateSliderLimits();
    }

    #endregion

    #region Internal Methods

    private void calculateSliderLimits() {
      if (transform.parent != null) {
        parent = transform.parent.GetComponent<RectTransform>();

        if (overrideRectLimits) return;

        if (parent != null) {
          if (parent.rect.width < 0f || parent.rect.height < 0f) {
            Debug.LogError("Parent Rectangle dimensions negative; can't set slider boundaries!", parent.gameObject);
            enabled = false;
          } else {
            var self = transform.GetComponent<RectTransform>();
            if (self != null) {
              horizontalSlideLimits = new Vector2(parent.rect.xMin - transform.localPosition.x + self.rect.width / 2F, parent.rect.xMax - transform.localPosition.x - self.rect.width / 2F);
              if (horizontalSlideLimits.x > horizontalSlideLimits.y) {
                horizontalSlideLimits = new Vector2(0F, 0F);
              }
              if (Mathf.Abs(horizontalSlideLimits.x) < 0.0001F) {
                horizontalSlideLimits.x = 0F;
              }
              if (Mathf.Abs(horizontalSlideLimits.y) < 0.0001F) {
                horizontalSlideLimits.y = 0F;
              }

              verticalSlideLimits = new Vector2(parent.rect.yMin - transform.localPosition.y + self.rect.height / 2F, parent.rect.yMax - transform.localPosition.y - self.rect.height / 2F);
              if (verticalSlideLimits.x > verticalSlideLimits.y) {
                verticalSlideLimits = new Vector2(0F, 0F);
              }
              if (Mathf.Abs(verticalSlideLimits.x) < 0.0001F) {
                verticalSlideLimits.x = 0F;
              }
              if (Mathf.Abs(verticalSlideLimits.y) < 0.0001F) {
                verticalSlideLimits.y = 0F;
              }
            } else {
              horizontalSlideLimits = new Vector2(parent.rect.xMin - transform.localPosition.x, parent.rect.xMax - transform.localPosition.x);
              verticalSlideLimits = new Vector2(parent.rect.yMin - transform.localPosition.y, parent.rect.yMax - transform.localPosition.y);
            }
          }
        }
      }
    }

    protected override Vector3 constrainDepressedLocalPosition(Vector3 desiredOffset) {
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

    #endregion

    #region Gizmos

    protected override void OnDrawGizmosSelected() {
      base.OnDrawGizmosSelected();

      if (transform.parent != null) {
        Vector3 originPosition = Application.isPlaying ? initialLocalPosition : transform.localPosition;
        if (Application.isPlaying && startingPositionMode == StartingPositionMode.Relaxed) {
          originPosition = originPosition + Vector3.back * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, restingHeight);
        }

        // Actual slider slide limits
        Gizmos.color = Color.blue;
        Gizmos.DrawWireCube(originPosition +
          new Vector3((sliderType == SliderType.Vertical ? 0F : (horizontalSlideLimits.x + horizontalSlideLimits.y) * 0.5f),
                      (sliderType == SliderType.Horizontal ? 0F : (verticalSlideLimits.x + verticalSlideLimits.y) * 0.5f),
                      0f),
          new Vector3((sliderType == SliderType.Vertical ? 0F : horizontalSlideLimits.y - horizontalSlideLimits.x),
                      (sliderType == SliderType.Horizontal ? 0F : verticalSlideLimits.y - verticalSlideLimits.x),
                      0f));

        var self = GetComponent<RectTransform>();
        if (self != null) {
          // Apparent slide limits (against own rect limits)
          parent = transform.parent.GetComponent<RectTransform>();
          if (parent != null) {
            var parentRectHorizontal = new Vector2(parent.rect.xMin - originPosition.x, parent.rect.xMax - originPosition.x);
            var parentRectVertical = new Vector2(parent.rect.yMin - originPosition.y, parent.rect.yMax - originPosition.y);
            Gizmos.color = Color.Lerp(Color.blue, Color.cyan, 0.5F);
            Gizmos.DrawWireCube(originPosition +
              new Vector3((parentRectHorizontal.x + parentRectHorizontal.y) * 0.5f,
                          (parentRectVertical.x + parentRectVertical.y) * 0.5f,
                          (startingPositionMode == StartingPositionMode.Relaxed ?
                            Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, 0.5F) - Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, 1 - restingHeight)
                          : -1F * Mathf.Lerp(minMaxHeight.x, minMaxHeight.y, 0.5F))),
              new Vector3(parentRectHorizontal.y - parentRectHorizontal.x, parentRectVertical.y - parentRectVertical.x, (minMaxHeight.y - minMaxHeight.x)));

            // Own rect width/height
            Gizmos.color = Color.cyan;
            Gizmos.DrawWireCube(originPosition,
              self.rect.width * Vector3.right
            + self.rect.height * Vector3.up);
          }
        }
      }
    }

    #endregion
  }
}
