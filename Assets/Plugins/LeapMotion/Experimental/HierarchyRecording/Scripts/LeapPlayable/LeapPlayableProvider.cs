/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 * Ultraleap proprietary and confidential.                                    *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;
using Leap.Unity.RuntimeGizmos;

namespace Leap.Unity.Recording {

  public class LeapPlayableProvider : LeapProvider {

    private Frame _frame;

    public override Frame CurrentFixedFrame {
      get {
        return _frame;
      }
    }

    public override Frame CurrentFrame {
      get {
        return _frame;
      }
    }

    public void SetCurrentFrame(Frame frame) {
      _frame = frame;
      DispatchUpdateFrameEvent(frame);
      DispatchFixedFrameEvent(frame);
    }
  }
}
