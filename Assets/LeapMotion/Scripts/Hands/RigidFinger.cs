/******************************************************************************\
* Copyright (C) Leap Motion, Inc. 2011-2014.                                   *
* Leap Motion proprietary. Licensed under Apache 2.0                           *
* Available at http://www.apache.org/licenses/LICENSE-2.0.html                 *
\******************************************************************************/

using UnityEngine;
using System.Collections;
using Leap;

// The finger model for our rigid hand made out of various cubes.
public class RigidFinger : SkeletalFinger {

  public float filtering = 0.5f;

  void Start() {
    for (int i = 0; i < bones.Length; ++i) {
      if (bones[i] != null) {
        bones[i].GetComponent<Rigidbody>().maxAngularVelocity = Mathf.Infinity;
      }
    }
  }

  public override void UpdateFinger() {
    for (int i = 0; i < bones.Length; ++i) {
      if (bones[i] != null) {
        // Set bone dimensions.
        CapsuleCollider capsule = bones[i].GetComponent<CapsuleCollider>();
        if (capsule != null)
        {
          // Initialization
          capsule.direction = 2;
          bones[i].localScale = new Vector3(1f, 1f, 1f);

          // Update
          capsule.radius = GetBoneWidth(i) / 2f;
          capsule.height = GetBoneLength(i) + GetBoneWidth(i);
        }

        Rigidbody boneBody = bones[i].GetComponent<Rigidbody>();
        if (boneBody) {
          boneBody.MovePosition(GetBoneCenter(i));
          boneBody.MoveRotation(GetBoneRotation(i));
        } else {
          bones[i].position = GetBoneCenter(i);
          bones[i].rotation = GetBoneRotation(i);
        }
      }
    }
  }
}
