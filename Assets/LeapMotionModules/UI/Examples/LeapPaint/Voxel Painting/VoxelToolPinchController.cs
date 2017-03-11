using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Leap.Unity.UI.Examples.LeapPaint {

  public class VoxelToolPinchController : MonoBehaviour {

    public VoxelPaintTool voxelPaintTool;

    void Update() {
      Hand hand = Hands.Right;
      if (hand != null && hand.IsPinching()) {
        voxelPaintTool.AddSingleVoxel(hand.GetPinchPosition());
      }
    }

  }

}