/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

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
