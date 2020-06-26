/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
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
