/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;
using Leap.Unity.Query;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public class TestRuntimeFeatures : GraphicRendererTestBase {

    /// <summary>
    /// Validate that when you add a graphic with not enough features,
    /// that the process of adding the graphic to the group will automatically
    /// add the required features to the graphic.
    /// </summary>
    [UnityTest]
    public IEnumerator AddEmptyGraphicToGroupWithFeatures() {
      InitTest("OneEmptyDynamicGroupWith4Features");
      yield return null;

      CreateGraphic("DisabledMeshGraphic");

      Assert.That(oneGraphic.featureData, Is.Empty);

      firstGroup.TryAddGraphic(oneGraphic);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup);
      Assert.That(oneGraphic.featureData.Count, Is.EqualTo(4));
    }

    /// <summary>
    /// Verify that when we add a graphic to a group at runtime that 
    /// the feature data it already has does not get overwritten by the
    /// attachment.
    /// </summary>
    [UnityTest]
    public IEnumerator VerifyAddingGraphicDoesntOverwriteFeatureData() {
      InitTest("OneEmptyDynamicGroupWith4Features");
      yield return null;

      CreateGraphic("DisabledMeshGraphicWith4Features");

      Assert.That(oneGraphic.featureData.Count, Is.EqualTo(4));

      var dataCopy = oneGraphic.featureData.Query().Select(d => shallowCopy(d)).ToList();

      firstGroup.TryAddGraphic(oneGraphic);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup);

      for (int i = 0; i < dataCopy.Count; i++) {
        assertValueFieldsEqual(dataCopy[i], oneGraphic.featureData[i]);
      }
    }

    /// <summary>
    /// Verify that when we add a graphic to a group at runtime that the features
    /// match 1-to-1 in order with the features on the group it was added to.
    /// </summary>
    [UnityTest]
    public IEnumerator VerifyAddedGraphicHasFeaturesInSameOrder() {
      InitTest("OneEmptyDynamicGroupWith4Features");
      yield return null;

      CreateGraphic("DisabledMeshGraphic");
      firstGroup.TryAddGraphic(oneGraphic);

      yield return null;

#if UNITY_EDITOR
      for (int i = 0; i < firstGroup.features.Count; i++) {
        var feature = firstGroup.features[i];
        var dataObj = oneGraphic.featureData[i];

        var dataFeatureType = LeapFeatureData.GetFeatureType(dataObj.GetType());
        var featureType = feature.GetType();

        Assert.That(dataFeatureType, Is.EqualTo(featureType));
      }
#endif
    }
  }
}
#endif
