using System;
using Leap.Unity.InputModule;
using UnityEngine;
using UnityEngine.Events;

public class SoundOverrides : MonoBehaviour
{
    // Event delegates triggered by Input
    [Serializable]
    public class PositionEvent : UnityEvent<Vector3> { }
    
    [SerializeField] private LeapInputModule module;

    [Header(" Event Setup")]

    //The event that is triggered upon clicking on a non-canvas UI element.
    [Tooltip("The event that is triggered upon clicking on a non-canvas UI element.")]
    [SerializeField] PositionEvent onClickDown;

    //The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)
    [Tooltip("The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)")]
    [SerializeField] PositionEvent onClickUp;

    //The event that is triggered upon hovering over a non-canvas UI element.
    [Tooltip("The event that is triggered upon hovering over a non-canvas UI element.")]
    [SerializeField] PositionEvent onBeginHover;
        
    //The event that is triggered upon hovering over a non-canvas UI element.
    [Tooltip("The event that is triggered upon ending hovering over a non-canvas UI element.")]
    [SerializeField] PositionEvent onEndHover;
    
    //The event that is triggered upon hovering over a non-canvas UI element.
    [Tooltip("The event that is triggered upon missing a non-canvas UI element.")]
    [SerializeField] PositionEvent onBeginMissed;
    
    //The event that is triggered upon hovering over a non-canvas UI element.
    [Tooltip("The event that is triggered upon ending missing a non-canvas UI element.")]
    [SerializeField] PositionEvent onEndMissed;

    //The event that is triggered while pinching to a point in the environment.
    [Tooltip("The event that is triggered while pinching to a point in the environment.")]
    [SerializeField] PositionEvent onEnvironmentPinch;

    private void OnClickDown(object sender, Vector3 pos) => onClickDown?.Invoke(pos);
    private void OnClickUp(object sender, Vector3 pos) => onClickUp?.Invoke(pos);
    private void OnBeginHover(object sender, Vector3 pos) => onBeginHover?.Invoke(pos);
    private void OnEndHover(object sender, Vector3 pos) => onEndHover?.Invoke(pos);
    private void OnBeginMissed(object sender, Vector3 pos) => onBeginMissed?.Invoke(pos);
    private void OnEndMissed(object sender, Vector3 pos) => onEndMissed?.Invoke(pos);
    private void OnEnvironmentPinch(object sender, Vector3 pos) => onEnvironmentPinch?.Invoke(pos);
    
    public void OnEnable()
    {
        if (module)
        {
            module.onClickDown += OnClickDown;
            module.onClickUp += OnClickUp;
            module.onBeginHover += OnBeginHover;
            module.onEndHover += OnEndHover;
            module.onBeginMissed += OnBeginMissed;
            module.onEndMissed += OnEndMissed;
            module.onEnvironmentPinch += OnEnvironmentPinch;
        }
    }    
    
    public void OnDisable()
    {
        if (module)
        {
            module.onClickDown -= OnClickDown;
            module.onClickUp -= OnClickUp;
            module.onBeginHover -= OnBeginHover;
            module.onEndHover -= OnEndHover;
            module.onBeginMissed -= OnBeginMissed;
            module.onEndMissed -= OnEndMissed; 
            module.onEnvironmentPinch -= OnEnvironmentPinch;
        }
    }
}
