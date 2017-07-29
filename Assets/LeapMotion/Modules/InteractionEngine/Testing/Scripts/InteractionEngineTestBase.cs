/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS

using Leap.Unity.Query;
using NUnit.Framework;
using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity.Interaction.Tests {

  public abstract class InteractionEngineTestBase : LeapTestBase {

    protected const int WAIT_FOR_INTERACTION_FRAME_LIMIT = 500;

    protected StationaryTestLeapProvider testProvider;

    protected InteractionManager manager;

    protected InteractionHand leftHand;
    protected InteractionHand rightHand;
    protected InteractionVRController leftVRController;
    protected InteractionVRController rightVRController;

    protected InteractionBehaviour box0;
    protected InteractionBehaviour box1;
    protected InteractionBehaviour box2;

    /// <summary>
    /// To be called at the start of an Interaction Engine test. Loads the named object
    /// by finding it in the current scene or otherwise by spawning it by prefab name.
    /// 
    /// After calling this method, the following fields are set:
    /// 
    /// PROVIDERS
    /// - testProvider: The LeapProvider designed for IE unit tests.
    /// 
    /// MANAGERS
    /// - manager: The Interaction Manager.
    /// 
    /// CONTROLLERS
    /// - leftHand: The left Interaction Hand, if there is one.
    /// - rightHand: The right Interaction Hand, if there is one.
    /// - leftVRController: The left VR controller, if there is one.
    /// - rightVRController: The right VR Controller, if there is one.
    /// 
    /// OBJECTS
    /// - box0: An InteractionBehaviour with an attached BoxCollider, if there is one.
    /// - box1: Another InteractionBehaviour with an attached BoxCollider, if it exists.
    /// - box2: Yet another InteractionBehaviour with a BoxCollider, if it exists.
    /// </summary>
    protected override void InitTest(string objectName) {
      base.InitTest(objectName);

      testProvider = testObj.GetComponentInChildren<StationaryTestLeapProvider>();

      manager = testObj.GetComponentInChildren<InteractionManager>();

      foreach (var controller in manager.interactionControllers) {
        if (controller.intHand != null && controller.isLeft) {
          leftHand = controller.intHand;
          continue;
        }
        if (controller.intHand != null && !controller.isLeft) {
          rightHand = controller.intHand;
          continue;
        }

        var vrController = controller as InteractionVRController;
        if (vrController != null && vrController.isLeft) {
          leftVRController = vrController;
          continue;
        }
        if (vrController != null && !vrController.isLeft) {
          rightVRController = vrController;
          continue;
        }
      }

      var intObjs = Pool<List<InteractionBehaviour>>.Spawn();
      try {
        testObj.GetComponentsInChildren<InteractionBehaviour>(true, intObjs);

        // Load "simple box" interaction objects.
        foreach (var simpleBoxObj in intObjs.Query().Where(o => o.primaryHoverColliders.Count == 1
                                                             && o.primaryHoverColliders[0] is BoxCollider)) {
          if (box0 == null) { box0 = simpleBoxObj; continue; }
          if (box1 == null) { box1 = simpleBoxObj; continue; }
          if (box2 == null) { box2 = simpleBoxObj; continue; }
        }
      }
      finally {
        intObjs.Clear();
        Pool<List<InteractionBehaviour>>.Recycle(intObjs);
      }
    }

    [TearDown]
    protected virtual void Teardown() {
      UnityEngine.Object.DestroyImmediate(testObj);

      Debug.ClearDeveloperConsole();

      // Test scene loading NYI (refactor into LeapTestBase?)
      //if (!string.IsNullOrEmpty(_sceneToUnload)) {
      //  SceneManager.UnloadSceneAsync(_sceneToUnload);
      //  _sceneToUnload = null;
      //}
    }

  }

}
#endif
