using Leap.Unity.UI.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples {

  public class ThrottleHandle : MonoBehaviour {

    private Vector3 _baseScaleVector;
    private float _contactScale = 1.3F;

    private bool _contactingHand;

    private InteractionBehaviour _interactionObj;

    void Start() {
      _baseScaleVector = this.transform.localScale;

      _interactionObj = GetComponent<InteractionBehaviour>();
      _interactionObj.OnObjectContactBegin += OnObjectContactBegin;
      _interactionObj.OnObjectContactEnd   += OnObjectContactEnd;
      _interactionObj.OnGraspBegin         += OnGraspBegin;
      _interactionObj.OnGraspEnd           += OnGraspEnd;
      _interactionObj.OnGraspedMovement    += OnGraspedMovement;
    }

    private void OnObjectContactBegin(Hand hand) {
      _contactingHand = true;
    }

    private void OnObjectContactEnd(Hand hand) {
      _contactingHand = false;
    }

    private void OnGraspBegin(Hand hand) {

    }

    private void OnGraspEnd(Hand hand) {

    }

    private void OnGraspedMovement(Vector3 newPos, Quaternion newRot, Hand hand) {

    }

    void Update() {
      Vector3 targetScaleVector = _baseScaleVector * (_contactingHand ? 1F : _contactScale);

      this.transform.localScale = Vector3.Lerp(this.transform.localScale, targetScaleVector, 20F * Time.deltaTime);
    }

  }


}