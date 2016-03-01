using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;

namespace Leap
{
    public class Asserter
    {
        public static void CompareAllValues(IFrame reference, IFrame value)
        {
            Assert.AreEqual(value.Id, reference.Id);
            Assert.AreEqual(reference.CurrentFramesPerSecond, value.CurrentFramesPerSecond);

            Assert.AreEqual(reference.Timestamp, value.Timestamp);

            Assert.AreEqual(reference.Hands.Count, value.Hands.Count);

            for (int i = 0; i < reference.Hands.Count; i++)
            {
                CompareAllValues(reference.Hands[i], value.Hands[i]);
            }
        }

        public static void CompareAllValues(IHand value, IHand reference)
        {
            CompareAllValues(value.Arm, reference.Arm);

            Assert.AreEqual(value.Confidence, reference.Confidence);
            Assert.AreEqual(value.Direction, reference.Direction);

            Assert.AreEqual(value.GrabAngle, reference.GrabAngle);
            Assert.AreEqual(value.GrabStrength, reference.GrabStrength);

            Assert.AreEqual(value.Id, reference.Id);
            Assert.AreEqual(value.IsLeft, reference.IsLeft);
            Assert.AreEqual(value.IsRight, reference.IsRight);

            Assert.AreEqual(value.PalmNormal, reference.PalmNormal);
            Assert.AreEqual(value.PalmPosition, reference.PalmPosition);
            Assert.AreEqual(value.PalmVelocity, reference.PalmVelocity);
            Assert.AreEqual(value.PalmWidth, reference.PalmWidth);

            Assert.AreEqual(value.PinchDistance, reference.PinchDistance);
            Assert.AreEqual(value.PinchStrength, reference.PinchStrength);
            
            Assert.AreEqual(value.StabilizedPalmPosition, reference.StabilizedPalmPosition);
            Assert.AreEqual(value.TimeVisible, reference.TimeVisible);

            Assert.AreEqual(value.WristPosition, reference.WristPosition);

            for (int i = 0; i < reference.Fingers.Count; i++)
            {
                IFinger referenceFinger = reference.Fingers[i];

                CompareAllValues(value.Finger(referenceFinger.Id), referenceFinger);
            }
        }

        public static void CompareAllValues(IFinger value, IFinger reference)
        {
            Assert.AreEqual(value.Id, reference.Id);
            Assert.AreEqual(value.HandId, reference.HandId);
            Assert.AreEqual(value.TimeVisible, reference.TimeVisible);

            Assert.AreEqual(value.Type, reference.Type);
            Assert.AreEqual(value.IsExtended, reference.IsExtended);

            Assert.AreEqual(value.Length, reference.Length);
            Assert.AreEqual(value.Width, reference.Width);

            Assert.AreEqual(value.Direction, reference.Direction);

            Assert.AreEqual(value.StabilizedTipPosition, reference.StabilizedTipPosition);
            Assert.AreEqual(value.TipPosition, reference.TipPosition);
            Assert.AreEqual(value.TipVelocity, reference.TipVelocity);

            int i = 0;

            if (reference.Type == Finger.FingerType.TYPE_THUMB)
                i = 1;

            for (; i < 4; i++)
            {
                Bone.BoneType bt = (Bone.BoneType) i;

                CompareAllValues(value.Bone(bt), reference.Bone(bt));
            }
        }

        public static void CompareAllValues(IArm value, IArm reference)
        {
            // These asserts fail once when a hand is detected. Bug somewhere else?
            Assert.AreEqual(value.ElbowPosition, reference.ElbowPosition);
            Assert.AreEqual(value.WristPosition, reference.WristPosition);

            CompareAllValues((IBone)value, (IBone)reference);
        }

        public static void CompareAllValues(IBone value, IBone reference)
        {
            Assert.AreEqual(value.Type, reference.Type);

            if (value.Type == Bone.BoneType.TYPE_INVALID)
                return;

            Assert.AreEqual(value.Basis, reference.Basis);
            Assert.AreEqual(value.Center, reference.Center);
            Assert.AreEqual(value.Direction, reference.Direction);
            Assert.AreEqual(value.NextJoint, reference.NextJoint);
            Assert.AreEqual(value.PrevJoint, reference.PrevJoint);
            Assert.AreEqual(value.Width, reference.Width);
            Assert.AreEqual(value.Length, reference.Length);
        }
    }
}