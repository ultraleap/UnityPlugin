using UnityEngine;
using UnityEngine.Assertions;
using System.Collections;

namespace Leap
{
    public class Asserter
    {
        public static void CompareAllValues(IFrame reference, IFrame previousFrame, IFrame value, IFrame previousValueFrame)
        {
            Assert.AreEqual(value.IsValid, reference.IsValid);

            //if (value.IsValid == false)
            //    return;

            Assert.AreEqual(value.Id, reference.Id);
            Assert.AreEqual(reference.CurrentFramesPerSecond, value.CurrentFramesPerSecond);

            Assert.AreEqual(reference.RotationAngle(previousFrame), value.RotationAngle(previousValueFrame));
            Assert.AreEqual(reference.RotationAngle(previousFrame, Vector.Forward), value.RotationAngle(previousValueFrame, Vector.Forward));
            Assert.AreEqual(reference.RotationAngle(previousFrame, Vector.Backward), value.RotationAngle(previousValueFrame, Vector.Backward));
            Assert.AreEqual(reference.RotationAngle(previousFrame, Vector.Left), value.RotationAngle(previousValueFrame, Vector.Left));
            Assert.AreEqual(reference.RotationAngle(previousFrame, Vector.Right), value.RotationAngle(previousValueFrame, Vector.Right));
            Assert.AreEqual(reference.RotationAngle(previousFrame, Vector.Up), value.RotationAngle(previousValueFrame, Vector.Up));
            Assert.AreEqual(reference.RotationAngle(previousFrame, Vector.Down), value.RotationAngle(previousValueFrame, Vector.Down));

            Assert.AreEqual(reference.RotationAxis(previousFrame), value.RotationAxis(previousValueFrame));
            Assert.AreEqual(reference.RotationMatrix(previousFrame), value.RotationMatrix(previousValueFrame));
            Assert.AreEqual(reference.RotationProbability(previousFrame), value.RotationProbability(previousValueFrame));
            Assert.AreEqual(reference.ScaleFactor(previousFrame), value.ScaleFactor(previousValueFrame));
            Assert.AreEqual(reference.ScaleProbability(previousFrame), value.ScaleProbability(previousValueFrame));

            Assert.AreEqual(reference.Translation(previousFrame), value.Translation(previousValueFrame));
            Assert.AreEqual(reference.TranslationProbability(previousFrame), value.TranslationProbability(previousValueFrame));

            Assert.AreEqual(reference.Timestamp, value.Timestamp);

            Assert.AreEqual(reference.Hands.Count, value.Hands.Count);

            for (int i = 0; i < reference.Hands.Count; i++)
            {
                CompareAllValues(reference.Hands[i], previousFrame, value.Hands[i], previousValueFrame);
            }
        }

        public static void CompareAllValues(IHand value, IFrame previousFrame, IHand reference, IFrame previousValueFrame)
        {
            Assert.AreEqual(value.IsValid, reference.IsValid);

            //if (value.IsValid == false)
            //    return;

            CompareAllValues(value.Arm, reference.Arm);

            // The original Basis calculation should generate a left-handed
            // basis for the left hand, and a right-handed base for the
            // right hand, but there's a bug in it.
            //Debug.Log("IsLeft? " + value.IsLeft);
            //Assert.AreEqual(value.Basis, reference.Basis);

            Assert.AreEqual(value.Confidence, reference.Confidence);
            Assert.AreEqual(value.Direction, reference.Direction);

            Assert.AreEqual(value.FrameId, reference.FrameId);
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

            Assert.AreEqual(value.RotationAngle(previousFrame), reference.RotationAngle(previousValueFrame));
            Assert.AreEqual(value.RotationAxis(previousFrame), reference.RotationAxis(previousValueFrame));
            Assert.AreEqual(value.RotationMatrix(previousFrame), reference.RotationMatrix(previousValueFrame));
            Assert.AreEqual(value.RotationProbability(previousFrame), reference.RotationProbability(previousValueFrame));

            Assert.AreEqual(value.ScaleFactor(previousFrame), reference.ScaleFactor(previousValueFrame));
            Assert.AreEqual(value.ScaleProbability(previousFrame), reference.ScaleProbability(previousValueFrame));

            // There are known issues calculating the SphereCenter: it doesn't
            // scale the minimum and maximum sphere radii. Will be removed next update anyway
            // Assert.AreEqual(value.SphereCenter ,  reference.SphereCenter);
            // Assert.AreEqual(value.SphereRadius ,  reference.SphereRadius);

            Assert.AreEqual(value.StabilizedPalmPosition, reference.StabilizedPalmPosition);
            Assert.AreEqual(value.TimeVisible, reference.TimeVisible);

            Assert.AreEqual(value.Translation(previousFrame), reference.Translation(previousValueFrame));
            Assert.AreEqual(value.TranslationProbability(previousFrame), reference.TranslationProbability(previousValueFrame));
            Assert.AreEqual(value.WristPosition, reference.WristPosition);

            for (int i = 0; i < reference.Fingers.Count; i++)
            {
                IFinger referenceFinger = reference.Fingers[i];

                CompareAllValues(value.Finger(referenceFinger.Id), referenceFinger);
            }
        }

        public static void CompareAllValues(IFinger value, IFinger reference)
        {
            Assert.AreEqual(value.IsValid, reference.IsValid);

            //if (value.IsValid == false)
            //    return;

            Assert.AreEqual(value.Id, reference.Id);
            Assert.AreEqual(value.FrameId, reference.FrameId);
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
                Bone.BoneType bt = (Bone.BoneType)i;

                CompareAllValues(value.Bone(bt), reference.Bone(bt));
            }
        }

        public static void CompareAllValues(IArm value, IArm reference)
        {
            Assert.AreEqual(value.IsValid, reference.IsValid);

            if (value.IsValid == false)
                return;

            Assert.AreEqual(value.ElbowPosition, reference.ElbowPosition);
            Assert.AreEqual(value.WristPosition, reference.WristPosition);

            CompareAllValues((IBone)value, (IBone)reference);
        }

        public static void CompareAllValues(IBone value, IBone reference)
        {
            Assert.AreEqual(value.IsValid, reference.IsValid);

            if (value.IsValid == false)
                return;

            Assert.AreEqual(value.Type, reference.Type);
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