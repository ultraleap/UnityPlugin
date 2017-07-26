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
using System;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace Leap.Unity {

  public abstract class LeapTestBase {

    protected GameObject testObj;

    /// <summary>
    /// Should be called at the start of a test. The argument is the name of a prefab to
    /// spawn, or the name of a GameObject in the current scene.
    /// 
    /// This method populates the testObj field with the loaded or spawned object.
    /// 
    /// Module-specific tests can then set additional fields that may be contained within
    /// the testing object by overriding this method.
    /// </summary>
    protected virtual void InitTest(string objectName) {
      GameObject obj = null;

      for (int i = 0; i < SceneManager.sceneCount; i++) {
        var scene = SceneManager.GetSceneAt(i);

        obj = scene.GetRootGameObjects().
                    Query().
                    FirstOrDefault(g => g.name == objectName);

        if (obj != null) {
          obj.SetActive(true);
          break;
        }
      }

      if (obj == null) {
        var prefab = EditorResources.Load<GameObject>(objectName);

        if (prefab == null) {
          throw new Exception("Could not find an object or prefab with the name "
                              + objectName);
        }

        obj = UnityEngine.Object.Instantiate(prefab);
      }

      testObj = obj;
    }

  }

}
#endif
