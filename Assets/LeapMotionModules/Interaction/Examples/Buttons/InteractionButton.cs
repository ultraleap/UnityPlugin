using System;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using Leap.Unity.Attributes;

namespace Leap.Unity.UI.Interaction {
  /** A physics-enabled button. Activation is triggered by physically pushing the button back to its unsprung position. */
  [RequireComponent(typeof(InteractionBehaviour))]
  public class InteractionButton : MonoBehaviour {
    //[MinMax(0f, 0.1f)]
    [Tooltip("The minimum and maximum heights the button can exist at.")]
    public Vector2 MinMaxHeight = new Vector2(0f, 0.02f);
    [Tooltip("The height that this button rests at; this value is a lerp in between the min and max height.")]
    public float RestingHeight = 0.5f;

    public UnityEvent OnPress = new UnityEvent();
    public UnityEvent OnUnpress = new UnityEvent();

    public bool isDepressed { get; protected set; }
    public bool depressedThisFrame { get; protected set; }
    public bool unDepressedThisFrame { get; protected set; }

    private InteractionBehaviour behaviour;
    private Rigidbody body;
    private Vector3 InitialLocalPosition;
    private Vector3 PhysicsPosition = Vector3.zero;
    private Vector3 PhysicsVelocity = Vector3.zero;
    private bool physicsOccurred = false;
    private PointerEventData pointerEvent;
    private LeapGuiElement element;
    private Color hoverTint = Color.white;
    private Rigidbody lastDepressor;
    private Vector3 localDepressor;
    private bool isLeftInteracting = false; 

    //Reset the Positions of the UI Elements on both Start and Quit
    void Start() {
      behaviour = GetComponent<InteractionBehaviour>();
      element = GetComponent<LeapGuiElement>();
      body = behaviour.rigidbody;
      InitialLocalPosition = transform.localPosition;

      pointerEvent = new PointerEventData(EventSystem.current);
      pointerEvent.button = PointerEventData.InputButton.Left;
      RaycastResult result = new RaycastResult();
      result.gameObject = gameObject;
      pointerEvent.pointerCurrentRaycast = result;
      pointerEvent.pointerPress = gameObject;
      pointerEvent.rawPointerPress = gameObject;
      transform.localPosition = InitialLocalPosition;
      PhysicsPosition = transform.position;
      body.position = PhysicsPosition;

      MinMaxHeight /= transform.parent.lossyScale.z;
    }

    void FixedUpdate() {
      if (!physicsOccurred) {
        physicsOccurred = true;
        if (!body.IsSleeping()) {
          body.position = PhysicsPosition;
          body.velocity = PhysicsVelocity;
        }
      }
    }

    void Update() {
      depressedThisFrame = false;
      unDepressedThisFrame = false;
      pointerEvent.position = Camera.main.WorldToScreenPoint(transform.transform.position);

      if (physicsOccurred) {
        physicsOccurred = false;

        Vector3 localPhysicsPosition = transform.parent.InverseTransformPoint(body.position);
        localPhysicsPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, localPhysicsPosition.z);
        Vector3 newWorldPhysicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        PhysicsPosition = newWorldPhysicsPosition;

        Vector3 localPhysicsVelocity = transform.parent.InverseTransformVector(body.velocity);
        if (isDepressed && behaviour.isPrimaryHovered && lastDepressor != null) {
          Vector3 curLocalDepressor = transform.InverseTransformPoint(lastDepressor.position);
          localPhysicsVelocity = new Vector3(0f, 0f, ((curLocalDepressor - localDepressor).z) / Time.fixedDeltaTime);
        } else {
          localPhysicsVelocity = new Vector3(0f, 0f, localPhysicsVelocity.z + Mathf.Clamp(((InitialLocalPosition.z - Mathf.Lerp(MinMaxHeight.x, MinMaxHeight.y, RestingHeight) - localPhysicsPosition.z) / transform.parent.lossyScale.z)*0.2f, -20f, 20f));
        }
        Vector3 newWorldPhysicsVelocity = transform.parent.TransformVector(localPhysicsVelocity);
        PhysicsVelocity = newWorldPhysicsVelocity;

        bool oldDepressed = isDepressed;

        if (localPhysicsPosition.z > InitialLocalPosition.z - MinMaxHeight.x) {
          transform.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, InitialLocalPosition.z - MinMaxHeight.x);

          if (behaviour.isPrimaryHovered) {
            isDepressed = true;
          } else {
            PhysicsPosition = transform.parent.TransformPoint(InitialLocalPosition);
            PhysicsVelocity = PhysicsVelocity*0.1f;
            lastDepressor = null;
            isDepressed = false;
          }
        } else if (localPhysicsPosition.z < InitialLocalPosition.z - MinMaxHeight.y) {
          transform.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, InitialLocalPosition.z - MinMaxHeight.y);
          PhysicsPosition = transform.position;
          lastDepressor = null;
          isDepressed = false;
        } else {
          transform.localPosition = localPhysicsPosition;
          lastDepressor = null;
          isDepressed = false;
        }

        if (isDepressed && !oldDepressed) {
          depressedThisFrame = true;
          ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerEnterHandler);
          ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerDownHandler);
          OnPress.Invoke();
          isLeftInteracting = behaviour.primaryHoveringHand.IsLeft;
          behaviour.manager.GetInteractionHand(isLeftInteracting).SetInteractionHoverOverride(true);
        } else if (!isDepressed && oldDepressed) {
          unDepressedThisFrame = true;
          ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerExitHandler);
          ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerClickHandler);
          ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerUpHandler);
          OnUnpress.Invoke();
          behaviour.manager.GetInteractionHand(isLeftInteracting).SetInteractionHoverOverride(false);
        }
      }

      if (element != null) {
        if (behaviour.isPrimaryHovered) {
          hoverTint = isDepressed ? Color.red : Color.Lerp(Color.white, Color.red, 0.5f);
          behaviour.ignoreContact = false;
        } else {
          hoverTint = Color.Lerp(hoverTint, Color.white, 0.1f);
          behaviour.ignoreContact = true;
        }
        element.Tint().tint = hoverTint;
      }
    }

    void OnCollisionEnter(Collision collision) {
      if(collision.rigidbody != null && lastDepressor == null && isDepressed) {
        lastDepressor = collision.rigidbody;
        localDepressor = transform.InverseTransformPoint(collision.rigidbody.position);
      }
    }

    void OnCollisionStay(Collision collision) {
      if (collision.rigidbody != null && lastDepressor == null && isDepressed) {
        lastDepressor = collision.rigidbody;
        localDepressor = transform.InverseTransformPoint(collision.rigidbody.position);
      }
    }
  }
}