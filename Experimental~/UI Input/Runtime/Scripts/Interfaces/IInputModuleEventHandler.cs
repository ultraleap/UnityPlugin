using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.EventSystems;

namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Defines the set of events supported by the UI input module
    /// </summary>
    public interface IInputModuleEventHandler
    {
        //The event that is triggered upon clicking on a non-canvas UI element.
        EventHandler<Vector3> OnClickDown { get; set; }

        //The event that is triggered upon lifting up from a non-canvas UI element (Not 1:1 with onClickDown!)
        EventHandler<Vector3> OnClickUp { get; set; }

        //The event that is triggered upon hovering over a non-canvas UI element.
        EventHandler<Vector3> OnBeginHover { get; set; }

        //The event that is triggered when the pointer stops hovering over a non-canvas UI element.
        EventHandler<Vector3> OnEndHover { get; set; }

        EventHandler<Vector3> OnBeginMissed { get; set; }

        EventHandler<Vector3> OnEndMissed { get; set; }

        void HandlePointerExitAndEnterProxy(PointerEventData EventData, GameObject CurrentGameObjectUnderPointer);
        RaycastResult FindFirstRaycastProxy(List<RaycastResult> candidates);

    }
}
