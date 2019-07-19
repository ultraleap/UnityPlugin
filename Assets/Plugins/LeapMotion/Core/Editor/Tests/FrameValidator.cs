/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System.Linq;
using NUnit.Framework;

namespace Leap.Unity.Tests {

  public abstract class FrameValidator {
    protected const float TOLERANCE = 0.0001f;
    protected static Finger.FingerType[] _fingers = {
      Finger.FingerType.TYPE_INDEX,
      Finger.FingerType.TYPE_MIDDLE,
      Finger.FingerType.TYPE_PINKY,
      Finger.FingerType.TYPE_RING,
      Finger.FingerType.TYPE_THUMB
    };

    protected static Bone.BoneType[] _bones = {
      Bone.BoneType.TYPE_DISTAL,
      Bone.BoneType.TYPE_INTERMEDIATE,
      Bone.BoneType.TYPE_METACARPAL,
      Bone.BoneType.TYPE_PROXIMAL
    };

    protected Frame _frame;

    [SetUp]
    public virtual void Setup() {
      _frame = createFrame();
    }

    [SetUp]
    public virtual void Teardown() {
      _frame = null;
    }

    protected abstract Frame createFrame();

    [Test]
    public void HandsAreUnique() {
      bool existDuplicates = _frame.Hands.GroupBy(h => h.Id).Any(g => g.Count() > 1);
      Assert.That(existDuplicates, Is.False);
    }

    [Test]
    public void HandsHaveFiveFingers() {
      foreach (Hand hand in _frame.Hands) {
        Assert.That(hand.Fingers.Count, Is.EqualTo(5));
      }
    }

    [Test]
    public void FingersHaveFourBones([ValueSource(typeof(FrameValidator), "_fingers")] Finger.FingerType fingerType,
                                     [ValueSource(typeof(FrameValidator), "_bones")] Bone.BoneType boneType) {
      foreach (Hand hand in _frame.Hands) {
        Bone bone = getBone(hand, fingerType, boneType);
        Assert.That(bone, Is.Not.Null);
      }
    }

    [Test]
    public void BoneLength([ValueSource(typeof(FrameValidator), "_fingers")] Finger.FingerType fingerType,
                           [ValueSource(typeof(FrameValidator), "_bones")] Bone.BoneType boneType) {
      foreach (Hand hand in _frame.Hands) {
        Bone bone = getBone(hand, fingerType, boneType);
        float apparentLength = bone.NextJoint.DistanceTo(bone.PrevJoint);
        float actualLength = bone.Length;
        Assert.That(actualLength, Is.EqualTo(apparentLength).Within(TOLERANCE));
      }
    }

    [Test]
    public void JointsMatch([ValueSource(typeof(FrameValidator), "_fingers")] Finger.FingerType fingerType,
                            [ValueSource(typeof(FrameValidator), "_bones")] Bone.BoneType boneType) {
      foreach (Hand hand in _frame.Hands) {
        Bone prevBone = getBone(hand, fingerType, boneType - 1);
        Bone bone = getBone(hand, fingerType, boneType);
        Bone nextBone = getBone(hand, fingerType, boneType + 1);

        if (prevBone != null) {
          assertVectorsEqual(prevBone.NextJoint, bone.PrevJoint);
        }

        if (nextBone != null) {
          assertVectorsEqual(nextBone.PrevJoint, bone.NextJoint);
        }
      }
    }

    [Test]
    public void CenterIsBetweenJoints([ValueSource(typeof(FrameValidator), "_fingers")] Finger.FingerType fingerType,
                                      [ValueSource(typeof(FrameValidator), "_bones")] Bone.BoneType boneType) {
      foreach (Hand hand in _frame.Hands) {
        Bone bone = getBone(hand, fingerType, boneType);

        Vector jointAverage = (bone.NextJoint + bone.PrevJoint) * 0.5f;
        assertVectorsEqual(jointAverage, bone.Center);
      }
    }

    [Test]
    public void DirectionMatchesJoints([ValueSource(typeof(FrameValidator), "_fingers")] Finger.FingerType fingerType,
                                       [ValueSource(typeof(FrameValidator), "_bones")] Bone.BoneType boneType) {
      foreach (Hand hand in _frame.Hands) {
        Bone bone = getBone(hand, fingerType, boneType);

        //If the joints are at the same position this test is meaningless
        if (bone.NextJoint.DistanceTo(bone.PrevJoint) < TOLERANCE) {
          continue;
        }

        Vector jointDirection = (bone.NextJoint - bone.PrevJoint).Normalized;
        assertVectorsEqual(jointDirection, bone.Direction);
      }
    }

    [Test]
    public void RotationIsValid() {
      foreach (Hand hand in _frame.Hands) {
        Assert.That(hand.Rotation.IsValid());
      }
    }

    protected Bone getBone(Hand hand, Finger.FingerType fingerType, Bone.BoneType boneType) {
      if (boneType < 0 || (int)boneType >= 4) {
        return null;
      }

      foreach (Finger finger in hand.Fingers) {
        if (finger.Type != fingerType) {
          continue;
        }

        return finger.Bone(boneType);
      }
      return null;
    }

    protected void assertVectorsEqual(Vector a, Vector b, string vectorName = "Vector") {
      Assert.That(a.x, Is.EqualTo(b.x).Within(TOLERANCE), vectorName + ".x");
      Assert.That(a.y, Is.EqualTo(b.y).Within(TOLERANCE), vectorName + ".y");
      Assert.That(a.z, Is.EqualTo(b.z).Within(TOLERANCE), vectorName + ".z");
    }
  }
}
