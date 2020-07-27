/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2020.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
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
