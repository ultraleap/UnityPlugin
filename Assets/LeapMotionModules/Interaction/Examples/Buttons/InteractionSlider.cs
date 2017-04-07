using UnityEngine;
using UnityEngine.Events;
using Leap.Unity.Attributes;
namespace Leap.Unity.UI.Interaction {

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

    public class FloatEvent : UnityEvent<float> { }
    ///<summary> Triggered while this slider is depressed. </summary>
    public FloatEvent horizontalSlideEvent = new FloatEvent();
    ///<summary> Triggered while this slider is depressed. </summary>
    public FloatEvent verticalSlideEvent = new FloatEvent();

    ///<summary> This slider's horizontal slider value, mapped between the values in the HorizontalValueRange. </summary>
    public float HorizontalSliderValue {
      get { return _HorizontalSliderValue; }
      set {
        if (_HorizontalSlideLimits.x != _HorizontalSlideLimits.y) {
          float alpha = Mathf.InverseLerp(horizontalValueRange.x, horizontalValueRange.y, value);
          localPhysicsPosition.x = Mathf.Lerp(initialLocalPosition.x + _HorizontalSlideLimits.x, initialLocalPosition.x + _HorizontalSlideLimits.y, alpha);
          _HorizontalSliderValue = value;
        }
      }
    }

    ///<summary> This slider's current vertical slider value, mapped between the values in the VerticalValueRange. </summary>
    public float VerticalSliderValue {
      get { return _VerticalSliderValue; }
      set {
        if (_VerticalSlideLimits.x != _VerticalSlideLimits.y) {
          float alpha = Mathf.InverseLerp(verticalValueRange.x, verticalValueRange.y, value);
          localPhysicsPosition.y = Mathf.Lerp(initialLocalPosition.y + _VerticalSlideLimits.x, initialLocalPosition.y + _VerticalSlideLimits.y, alpha);
          _VerticalSliderValue = value;
        }
      }
    }

    //Slide limits normalized to local space
    private Vector2 _HorizontalSlideLimits;
    private Vector2 _VerticalSlideLimits;

    //Internal Slider Values
    private float _HorizontalSliderValue;
    private float _VerticalSliderValue;

    protected override void Start() {
      base.Start();

      //Conversion of limits to local space
      _HorizontalSlideLimits = horizontalSlideLimits / transform.parent.lossyScale.x;
      _VerticalSlideLimits = verticalSlideLimits / transform.parent.lossyScale.y;
      CalculateSliderValues();
    }

    protected void Update() {
      if (isDepressed) {
        CalculateSliderValues();
      }
    }

    private void CalculateSliderValues() {
      //Calculate the Renormalized Slider Values
      if (_HorizontalSlideLimits.x != _HorizontalSlideLimits.y) {
        _HorizontalSliderValue = Mathf.InverseLerp(initialLocalPosition.x + _HorizontalSlideLimits.x, initialLocalPosition.x + _HorizontalSlideLimits.y, localPhysicsPosition.x);
        _HorizontalSliderValue = Mathf.Lerp(horizontalValueRange.x, horizontalValueRange.y, _HorizontalSliderValue);
        horizontalSlideEvent.Invoke(_HorizontalSliderValue);
      }

      if (_VerticalSlideLimits.x != _VerticalSlideLimits.y) {
        _VerticalSliderValue = Mathf.InverseLerp(initialLocalPosition.y + _VerticalSlideLimits.x, initialLocalPosition.y + _VerticalSlideLimits.y, localPhysicsPosition.y);
        _VerticalSliderValue = Mathf.Lerp(verticalValueRange.x, verticalValueRange.y, _VerticalSliderValue);
        verticalSlideEvent.Invoke(_VerticalSliderValue);
      }
    }

    protected override Vector3 GetDepressedConstrainedLocalPosition(Vector3 desiredOffset) {
      return new Vector3(Mathf.Clamp((localPhysicsPosition.x + desiredOffset.x * 0.25f), initialLocalPosition.x + _HorizontalSlideLimits.x, initialLocalPosition.x + _HorizontalSlideLimits.y),
                         Mathf.Clamp((localPhysicsPosition.y + desiredOffset.y * 0.25f), initialLocalPosition.y + _VerticalSlideLimits.x, initialLocalPosition.y + _VerticalSlideLimits.y),
                                     (localPhysicsPosition.z + desiredOffset.z));
    }
  }
}
