/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

namespace Leap.Unity {
  /** A physics model for our rigid hand made out of various Unity Collider. */
  public class RigidHand : SkeletalHand {
    public override ModelType HandModelType {
      get {
        return ModelType.Physics;
      }
    }
    public float filtering = 0.5f;

    public override bool SupportsEditorPersistence() {
      return true;
    }

    public override void InitHand() {
      base.InitHand();
    }

    public override void UpdateHand() {

      for (int f = 0; f < fingers.Length; ++f) {
        if (fingers[f] != null) {
          fingers[f].UpdateFinger();
        }
      }

      if (palm != null) {
        Rigidbody palmBody = palm.GetComponent<Rigidbody>();
        if (palmBody) {
          palmBody.MovePosition(GetPalmCenter());
          palmBody.MoveRotation(GetPalmRotation());
        } else {
          palm.position = GetPalmCenter();
          palm.rotation = GetPalmRotation();
        }
      }

      if (forearm != null) {
        // Set arm dimensions.
        CapsuleCollider capsule = forearm.GetComponent<CapsuleCollider>();
        if (capsule != null) {
          // Initialization
          capsule.direction = 2;
          forearm.localScale = new Vector3(1f / transform.lossyScale.x, 1f / transform.lossyScale.y, 1f / transform.lossyScale.z);

          // Update
          capsule.radius = GetArmWidth() / 2f;
          capsule.height = GetArmLength() + GetArmWidth();
        }

        Rigidbody forearmBody = forearm.GetComponent<Rigidbody>();
        if (forearmBody) {
          forearmBody.MovePosition(GetArmCenter());
          forearmBody.MoveRotation(GetArmRotation());
        } else {
          forearm.position = GetArmCenter();
          forearm.rotation = GetArmRotation();
        }
      }
    }
  }
}
