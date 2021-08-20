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

namespace Leap.Unity{
  /** 
   * A finger object consisting of discrete, component parts for each bone.
   * 
   * The graphic objects can include both bones and joints, but both are optional.
   */
  public class SkeletalFinger : FingerModel {
  
    /** Initializes the finger bones and joints by setting their positions and rotations. */
    public override void InitFinger() {
      SetPositions();
    }
  
    /** Updates the finger bones and joints by setting their positions and rotations. */
    public override void UpdateFinger() {  
      SetPositions();
    }
  
    protected void SetPositions() {
      for (int i = 0; i < bones.Length; ++i) {
        if (bones[i] != null) {
          bones[i].transform.position = GetBoneCenter(i);
          bones[i].transform.rotation = GetBoneRotation(i);
        }
      }
  
      for (int i = 0; i < joints.Length; ++i) {
        if (joints[i] != null) {
          joints[i].transform.position = GetJointPosition(i + 1);
          joints[i].transform.rotation = GetBoneRotation(i + 1);
        }
      }
    }
  }
}
