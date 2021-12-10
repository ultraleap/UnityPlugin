/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using NUnit.Framework;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

namespace Leap.Unity.Tests
{
    using Encoding;

    public class VectorHandTests
    {

        [Test]
        public void EncodeDecodeTest()
        {
            const float TOLERANCE = 0.01f; //1 cm for all positions

            Frame frame = TestHandFactory.MakeTestFrame(0, includeLeftHand: true, includeRightHand: true, unitType: TestHandFactory.UnitType.UnityUnits);

            foreach (var hand in frame.Hands)
            {

                byte[] bytes;
                {
                    VectorHand vHand = new VectorHand();
                    bytes = new byte[vHand.numBytesRequired];

                    //Encode the hand into the vHand representation
                    vHand.Encode(hand);

                    //Then convert the vHand into a binary representation
                    vHand.FillBytes(bytes);
                }

                Hand result;
                {
                    VectorHand vHand = new VectorHand();

                    //Convert the binary representation back into a vHand
                    int offset = 0;
                    vHand.ReadBytes(bytes, ref offset);

                    //Decode the vHand back into a normal Leap Hand
                    result = new Hand();
                    vHand.Decode(result);
                }

                Assert.That(result.IsLeft, Is.EqualTo(hand.IsLeft));
                Assert.That((result.PalmPosition - hand.PalmPosition).Magnitude, Is.LessThan(TOLERANCE));

                foreach (var resultFinger in result.Fingers)
                {
                    var finger = hand.Fingers.Single(f => f.Type == resultFinger.Type);

                    for (int i = 0; i < 4; i++)
                    {
                        Bone resultBone = resultFinger.bones[i];
                        Bone bone = finger.bones[i];

                        Assert.That((resultBone.NextJoint - bone.NextJoint).Magnitude, Is.LessThan(TOLERANCE));
                        Assert.That((resultBone.PrevJoint - bone.PrevJoint).Magnitude, Is.LessThan(TOLERANCE));
                        Assert.That((resultBone.Center - bone.Center).Magnitude, Is.LessThan(TOLERANCE));
                    }
                }
            }
        }
    }
}