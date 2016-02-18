/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// The model for our rigid hand made out of various polyhedra.
public class RigidHand : SkeletalHand {
  public override ModelType HandModelType {
    get {
      return ModelType.Physics;
    }
  }
  public float filtering = 0.5f;

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
      CapsuleCollider capsule = forearm.GetComponent<CapsuleCollider> ();
      if (capsule != null) {
        // Initialization
        capsule.direction = 2;
        forearm.localScale = new Vector3 (1f, 1f, 1f);
        
        // Update
        capsule.radius = GetArmWidth () / 2f;
        capsule.height = GetArmLength () + GetArmWidth ();
      }

      Rigidbody forearmBody = forearm.GetComponent<Rigidbody> ();
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
