/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2023.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using NUnit.Framework;
using System;

namespace Leap.Unity.Tests
{
    public class HandFactoryTwoHands : FrameValidator
    {
        protected override Frame createFrame()
        {
            return TestHandFactory.MakeTestFrame(0, true, true);
        }

        [Test]
        public void CorrectHandCount()
        {
            Assert.That(_frame.Hands.Count, Is.EqualTo(2));
        }
    }

    public class HandFactoryLeft : FrameValidator
    {
        protected override Frame createFrame()
        {
            return TestHandFactory.MakeTestFrame(0, true, false);
        }

        [Test]
        public void CorrectHandCount()
        {
            Assert.That(_frame.Hands.Count, Is.EqualTo(1));
        }
    }

    public class HandFactoryRight : FrameValidator
    {
        protected override Frame createFrame()
        {
            return TestHandFactory.MakeTestFrame(0, false, true);
        }

        [Test]
        public void CorrectHandCount()
        {
            Assert.That(_frame.Hands.Count, Is.EqualTo(1));
        }
    }
}