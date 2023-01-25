/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System;
using UnityEngine;
using UnityEngine.Events;

namespace Leap.Unity.InputModule
{
    public class SoundOverrides : MonoBehaviour
    {
        // Event delegates triggered by Input
        [Serializable]
        public class PositionEvent : UnityEvent<Vector3> { }

        [SerializeField] private UIInputModule module;

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

        private void OnClickDown(object sender, Vector3 pos) => onClickDown?.Invoke(pos);
        private void OnClickUp(object sender, Vector3 pos) => onClickUp?.Invoke(pos);
        private void OnBeginHover(object sender, Vector3 pos) => onBeginHover?.Invoke(pos);
        private void OnEndHover(object sender, Vector3 pos) => onEndHover?.Invoke(pos);
        private void OnBeginMissed(object sender, Vector3 pos) => onBeginMissed?.Invoke(pos);
        private void OnEndMissed(object sender, Vector3 pos) => onEndMissed?.Invoke(pos);

        private void OnEnable()
        {
            if (!module)
            {
                Debug.Log($"You must set a valid {nameof(UIInputModule)} on this script for it to function");
                return;
            }

            if (module is IInputModuleEventHandler eventHandler)
            {
                eventHandler.OnClickDown += OnClickDown;
                eventHandler.OnClickUp += OnClickUp;
                eventHandler.OnBeginHover += OnBeginHover;
                eventHandler.OnEndHover += OnEndHover;
                eventHandler.OnBeginMissed += OnBeginMissed;
                eventHandler.OnEndMissed += OnEndMissed;
            }
        }

        private void OnDisable()
        {
            if (!module)
            {
                return;
            }

            if (module is IInputModuleEventHandler eventHandler)
            {
                eventHandler.OnClickDown -= OnClickDown;
                eventHandler.OnClickUp -= OnClickUp;
                eventHandler.OnBeginHover -= OnBeginHover;
                eventHandler.OnEndHover -= OnEndHover;
                eventHandler.OnBeginMissed -= OnBeginMissed;
                eventHandler.OnEndMissed -= OnEndMissed;
            }
        }
    }
}