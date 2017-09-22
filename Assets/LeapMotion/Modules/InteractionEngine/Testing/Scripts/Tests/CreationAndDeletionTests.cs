/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using NUnit.Framework;
using System.Collections;
using UnityEngine;
using UnityEngine.TestTools;

namespace Leap.Unity.Interaction.Tests {

  public class CreationAndDeletionTests : InteractionEngineTestBase {

    #region General Tests

    [UnityTest]
    public IEnumerator CanCreateAndDelete() {
      yield return wait(beginningTestWait);

      InitTest();
      provider.editTimePose = TestHandFactory.TestHandPose.PoseB;

      yield return wait(aBit);

      var lHandPos = leftHand.leapHand.PalmPosition.ToVector3();
      var rHandPos = rightHand.leapHand.PalmPosition.ToVector3();

      var addBox0 = Spawn(box0, lHandPos - Physics.gravity * 0.1F + Vector3.forward * 0.5F);
      var addBox1 = Spawn(box1, rHandPos - Physics.gravity * 0.1F + Vector3.forward * 0.5F);

      yield return wait(aWhile);

      GameObject.Destroy(addBox0.gameObject);
      GameObject.Destroy(addBox1.gameObject);

      yield return wait(endingTestWait);
    }

    [UnityTest]
    public IEnumerator CanCreateInteractionBehaviourAtRuntime() {
      yield return wait(beginningTestWait);

      InitTest();
      provider.editTimePose = TestHandFactory.TestHandPose.PoseB;
      
      var rHandPos = rightHand.leapHand.PalmPosition.ToVector3();

      var spawnPos = rHandPos;
      var newBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
      newBox.transform.parent = rigObj.transform;
      newBox.transform.position = spawnPos;
      newBox.transform.localScale = Vector3.one * 0.1F;
      var newBody = newBox.AddComponent<Rigidbody>();
      newBody.useGravity = true;
      newBody.isKinematic = false;
      newBox.AddComponent<InteractionBehaviour>();

      yield return wait(aWhile);

      yield return wait(endingTestWait);
    }

    #endregion

    #region Hover Tests

    [UnityTest]
    public IEnumerator CanDeleteObjectWhileHovering() {
      yield return wait(beginningTestWait);

      InitTest();
      provider.editTimePose = TestHandFactory.TestHandPose.PoseB;

      yield return wait(aBit);
      
      var lHandPos = leftHand.leapHand.PalmPosition.ToVector3();
      var addBox0 = Spawn(box0, lHandPos + Vector3.forward * 0.2F);
      addBox0.rigidbody.useGravity = false;

      yield return wait(aBit);

      Assert.That(addBox0.isHovered);

      GameObject.Destroy(addBox0.gameObject);

      yield return wait(endingTestWait);
    }

    #endregion

    #region Contact Tests

    [UnityTest]
    public IEnumerator CanDeleteObjectDuringContact() {
      yield return wait(beginningTestWait);

      InitTest();
      provider.editTimePose = TestHandFactory.TestHandPose.PoseB;

      yield return wait(aWhile);
      
      var lHandPos = leftHand.leapHand.PalmPosition.ToVector3();
      var rHandPos = rightHand.leapHand.PalmPosition.ToVector3();

      var addBox0 = Spawn(box0, lHandPos - Physics.gravity * 0.1F);
      var addBox1 = Spawn(box1, rHandPos - Physics.gravity * 0.1F);

      bool addBox0Contacted = false;
      bool addBox1Contacted = false;
      addBox0.OnContactBegin += () => {
        addBox0Contacted = true;
      };
      addBox1.OnContactBegin += () => {
        addBox1Contacted = true;
      };

      int contactFramesWaited = 0;
      while ((!addBox0Contacted || !addBox1Contacted) && contactFramesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;

        contactFramesWaited++;
      }
      Assert.That(contactFramesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      GameObject.Destroy(addBox0.gameObject);
      GameObject.Destroy(addBox1.gameObject);

      yield return wait(endingTestWait);
    }

    #endregion Contact

    #region Grasping Tests

    [UnityTest]
    public IEnumerator CanDeleteObjectDuringGrasp() {
      yield return wait(beginningTestWait);

      InitTest(GRASP_THROW_RIG, DEFAULT_STAGE);

      // Wait for boxes to rest on the ground.
      yield return wait(aBit);

      // Play the grasping animation.
      recording.Play();

      // Wait for box0 to be grasped.
      bool graspOccurred = false;
      box0.OnGraspBegin += () => {
        graspOccurred = true;
      };
      int framesWaited = 0;
      while (!graspOccurred && framesWaited < WAIT_FOR_INTERACTION_FRAME_LIMIT) {
        yield return null;
        framesWaited++;
      }
      Assert.That(framesWaited != WAIT_FOR_INTERACTION_FRAME_LIMIT);

      // We should have box0 grasped now.
      GameObject.Destroy(box0);

      yield return wait(endingTestWait);
    }

    #endregion

    }

}

#endif // LEAP_TESTS
