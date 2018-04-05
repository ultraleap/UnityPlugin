/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using UnityEngine;
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public class TestInstantiation : GraphicRendererTestBase {

    /// <summary>
    /// Validate that when we duplicate an object at runtime to a different
    /// part of the scene, that does not think it is still attached
    /// </summary>
    [UnityTest]
    public IEnumerator InstantiateToRootOfScene() {
      InitTest("OneDynamicGroup");
      yield return null;

      //Instantiates to root of scene, not of the renderer
      var otherGraphic = Object.Instantiate(oneGraphic);

      Assert.That(otherGraphic.isAttachedToGroup, Is.False);
      Assert.That(oneGraphic.attachedGroup.graphics, Does.Not.Contains(otherGraphic));
    }

    /// <summary>
    /// Validate that when we duplicate an object at runtime to a be
    /// a child of the renderer that it is correctly gets added
    /// </summary>
    [UnityTest]
    public IEnumerator InstantiateChildOfRenderer() {
      InitTest("OneDynamicGroup");
      yield return null;

      //Instantiates to root of renderer
      var otherGraphic = Object.Instantiate(oneGraphic, renderer.transform);

      yield return null;

      Assert.That(otherGraphic.isAttachedToGroup, Is.True);
      Assert.That(oneGraphic.attachedGroup.graphics, Does.Contain(otherGraphic));
    }
  }
}
#endif
