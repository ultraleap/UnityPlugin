/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using NUnit.Framework;

namespace Leap.Unity.Tests {

  [TestFixture(Category = "TestHandFactory")]
  public class HandFactoryTwoHands : FrameValidator {
    protected override Frame createFrame() {
      return TestHandFactory.MakeTestFrame(0, true, true);
    }

    [Test]
    public void CorrectHandCount() {
      Assert.That(_frame.Hands.Count, Is.EqualTo(2));
    }
  }

  [TestFixture(Category = "TestHandFactory")]
  public class HandFactoryLeft : FrameValidator {
    protected override Frame createFrame() {
      return TestHandFactory.MakeTestFrame(0, true, false);
    }

    [Test]
    public void CorrectHandCount() {
      Assert.That(_frame.Hands.Count, Is.EqualTo(1));
    }
  }

  [TestFixture(Category = "TestHandFactory")]
  public class HandFactoryRight : FrameValidator {
    protected override Frame createFrame() {
      return TestHandFactory.MakeTestFrame(0, false, true);
    }

    [Test]
    public void CorrectHandCount() {
      Assert.That(_frame.Hands.Count, Is.EqualTo(1));
    }
  }
}
