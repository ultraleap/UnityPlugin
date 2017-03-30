using UnityEngine;
using UnityEngine.EventSystems;
using Leap.Unity.Attributes;

namespace Leap.Unity.UI.Interaction {
  /** A physics-enabled button. Activation is triggered by physically pushing the button back to its unsprung position. */
  [RequireComponent(typeof(InteractionBehaviour))]
  public class InteractionButton : MonoBehaviour {

    //[MinMax(0f, 0.1f)]
    public Vector2 MinMaxHeight = new Vector2(0f, 0.02f);

    public float RestingHeight = 0.5f;

    private InteractionBehaviour behaviour;
    private Rigidbody body;
    private Vector3 InitialLocalPosition;
    private Vector3 PhysicsPosition = Vector3.zero;
    private Vector3 PhysicsVelocity = Vector3.zero;
    private bool physicsOccurred = false;
    private bool isDepressed = false;
    private bool prevDepressed = false;
    private PointerEventData pointerEvent;
    private LeapGuiElement element;
    private Color hoverTint = Color.white;

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
        body.position = PhysicsPosition;
        //Apply the spring force here
        body.velocity = PhysicsVelocity + new Vector3(0f, 0f, ((InitialLocalPosition.z - Mathf.Lerp(MinMaxHeight.x, MinMaxHeight.y, RestingHeight) - transform.parent.InverseTransformPoint(body.position).z)* transform.parent.lossyScale.z) * 1000f * Time.fixedDeltaTime);
      }
    }

    void Update() {
      pointerEvent.position = Camera.main.WorldToScreenPoint(transform.transform.position);

      if (physicsOccurred) {
        physicsOccurred = false;

        Vector3 localPhysicsPosition = transform.parent.InverseTransformPoint(body.position);
        localPhysicsPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, localPhysicsPosition.z);
        Vector3 newWorldPhysicsPosition = transform.parent.TransformPoint(localPhysicsPosition);
        PhysicsPosition = newWorldPhysicsPosition;

        Vector3 localPhysicsVelocity = transform.parent.InverseTransformDirection(body.velocity);
        localPhysicsVelocity = new Vector3(0f, 0f, localPhysicsVelocity.z);
        Vector3 newWorldPhysicsVelocity = transform.parent.TransformDirection(localPhysicsVelocity);
        PhysicsVelocity = newWorldPhysicsVelocity;
        
        if (localPhysicsPosition.z > InitialLocalPosition.z - MinMaxHeight.x) {
          transform.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, InitialLocalPosition.z - MinMaxHeight.x);
          PhysicsVelocity = PhysicsVelocity / 2f;
          isDepressed = true;
        } else if (localPhysicsPosition.z < InitialLocalPosition.z - MinMaxHeight.y) {
          transform.localPosition = new Vector3(InitialLocalPosition.x, InitialLocalPosition.y, InitialLocalPosition.z - MinMaxHeight.y);
          PhysicsPosition = transform.position;
          isDepressed = false;
        } else {
          transform.localPosition = localPhysicsPosition;
          isDepressed = false;
        }
      }
      
      if (isDepressed && !prevDepressed) {
        prevDepressed = true;
        ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerEnterHandler);
        ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerDownHandler);
      } else if (!isDepressed && prevDepressed) {
        prevDepressed = false;
        ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerExitHandler);
        ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerClickHandler);
        ExecuteEvents.Execute(gameObject, pointerEvent, ExecuteEvents.pointerUpHandler);
      }

      if (element != null) {
        if (behaviour.isPrimaryHovered) {
          hoverTint = Color.red;
          behaviour.ignoreContact = false;
        } else {
          hoverTint = Color.Lerp(hoverTint, Color.white, 0.1f);
          behaviour.ignoreContact = true;
        }
        element.Tint().tint = hoverTint;
      }
    }
  }
}