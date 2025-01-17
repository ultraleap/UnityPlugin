/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.EventSystems;

namespace Leap.InputModule
{

    /// <summary>
    /// A physics-enabled button. Activation is triggered by physically pushing the button back to its unsprung position.
    /// Requires a SpringJoint.
    /// </summary>
    public class PhysicsUI : MonoBehaviour
    {
        [Tooltip("The physically-enabled body of the button")]
        /** The spring-loaded game object that serves as the movable, pressable portion of the button.*/
        public Transform ButtonFace;
        [Tooltip("OPTIONAL: If you have a dropshadow image that you would like to opacity fade upon compression, add one here")]
        /** An optional drop-shadow image. The opacity of this shadow is modified as the button is compressed. */
        public UnityEngine.UI.Image Shadow;

        private float MaxShadowOpacity;
        private Rigidbody body;
        private SpringJoint SpringJoint;
        private Vector3 InitialLocalPosition;
        private Vector3 PhysicsPosition = Vector3.zero;
        private Vector3 PhysicsVelocity = Vector3.zero;
        private bool physicsOccurred = false;
        private bool isDepressed = false;
        private bool prevDepressed = false;
        private PointerEventData pointerEvent;

        //Reset the Positions of the UI Elements on both Start and Quit
        void Start()
        {
            if (ButtonFace != null)
            {
                if (Shadow != null)
                {
                    MaxShadowOpacity = Shadow.color.a;
                    Shadow.color = new Color(Shadow.color.r, Shadow.color.g, Shadow.color.b, 0f);
                }

                body = ButtonFace.GetComponent<Rigidbody>();
                SpringJoint = ButtonFace.GetComponent<SpringJoint>();
                InitialLocalPosition = ButtonFace.localPosition;

                pointerEvent = new PointerEventData(EventSystem.current);
                pointerEvent.button = PointerEventData.InputButton.Left;
                RaycastResult result = new RaycastResult();
                result.gameObject = gameObject;
                pointerEvent.pointerCurrentRaycast = result;
                pointerEvent.pointerPress = gameObject;
                pointerEvent.rawPointerPress = gameObject;
                ButtonFace.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, SpringJoint.connectedAnchor.z);
                PhysicsPosition = transform.TransformPoint(new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, SpringJoint.connectedAnchor.z));
                body.position = PhysicsPosition;
            }
            else
            {
                Debug.LogWarning("Ensure that you have a UI Element allotted in the Layer Transform!");
            }
        }

        void FixedUpdate()
        {
            if (!physicsOccurred)
            {
                physicsOccurred = true;
                body.position = PhysicsPosition;
#if UNITY_6000_0_OR_NEWER
                body.linearVelocity = PhysicsVelocity;
#else
                body.velocity = PhysicsVelocity;
#endif
            }
        }

        void Update()
        {
            pointerEvent.position = Camera.main.WorldToScreenPoint(ButtonFace.transform.position);
            if (physicsOccurred)
            {
                physicsOccurred = false;

                Vector3 localPhysicsPosition = transform.InverseTransformPoint(body.position);
                localPhysicsPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, localPhysicsPosition.z);
                Vector3 newWorldPhysicsPosition = transform.TransformPoint(localPhysicsPosition);
                PhysicsPosition = newWorldPhysicsPosition;

#if UNITY_6000_0_OR_NEWER
                Vector3 localPhysicsVelocity = transform.InverseTransformDirection(body.linearVelocity);
#else
                Vector3 localPhysicsVelocity = transform.InverseTransformDirection(body.velocity);
#endif 
                localPhysicsVelocity = new Vector3(0f, 0f, localPhysicsVelocity.z);
                Vector3 newWorldPhysicsVelocity = transform.TransformDirection(localPhysicsVelocity);
                PhysicsVelocity = newWorldPhysicsVelocity;

                if (localPhysicsPosition.z > 0)
                {
                    ButtonFace.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, 0f);
                    PhysicsVelocity = PhysicsVelocity / 2f;
                    isDepressed = true;
                }
                else if (localPhysicsPosition.z < SpringJoint.connectedAnchor.z * 2f)
                {
                    ButtonFace.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, SpringJoint.connectedAnchor.z * 2f);
                    PhysicsPosition = ButtonFace.position;
                    isDepressed = false;
                }
                else
                {
                    ButtonFace.localPosition = localPhysicsPosition;
                    isDepressed = false;
                }

                if (SpringJoint && Shadow != null)
                {
                    float LayerHeight = Mathf.Abs(ButtonFace.localPosition.z);
                    float RestingHeight = Mathf.Abs(SpringJoint.connectedAnchor.z);
                    Shadow.color = new Color(Shadow.color.r, Shadow.color.g, Shadow.color.b, Mathf.Lerp(0f, MaxShadowOpacity, 1 - (Mathf.Abs(LayerHeight - RestingHeight) / RestingHeight)));
                }
            }

            if (isDepressed && !prevDepressed)
            {
                prevDepressed = true;
                ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerEnterHandler);
                ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerDownHandler);
            }
            else if (!isDepressed && prevDepressed)
            {
                prevDepressed = false;
                ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerExitHandler);
                ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerClickHandler);
                ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerUpHandler);
            }
        }
    }
}