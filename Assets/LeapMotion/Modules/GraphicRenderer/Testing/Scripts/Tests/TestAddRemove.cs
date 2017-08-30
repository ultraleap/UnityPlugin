/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

#if LEAP_TESTS
using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public class TestAddRemove : GraphicRendererTestBase {

    /// <summary>
    /// Validate that a pre-added graphic reports itself as being
    /// attached to the correct group for all of the initial 
    /// callbacks.
    /// </summary>
    [UnityTest]
    public IEnumerator PreAddedInValidState() {
      InitTest("OneDynamicGroup");
      yield return null;

      Assert.That(oneGraphic.OnAwake().wasAttached);
      Assert.That(oneGraphic.OnAwake().attachedGroup, Is.EqualTo(firstGroup));

      Assert.That(oneGraphic.OnEnable().wasAttached);
      Assert.That(oneGraphic.OnEnable().attachedGroup, Is.EqualTo(firstGroup));

      Assert.That(oneGraphic.OnStart().wasAttached);
      Assert.That(oneGraphic.OnStart().attachedGroup, Is.EqualTo(firstGroup));
    }

    /// <summary>
    /// Test whether or not it is possible to detach and reattach
    /// a graphic.
    /// </summary>
    [UnityTest]
    public IEnumerator CanRemoveAndAdd() {
      InitTest("OneDynamicGroup");
      yield return null;

      Assert.That(firstGroup.graphics, Contains.Item(oneGraphic));
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));

      bool didRemove = firstGroup.TryRemoveGraphic(oneGraphic);
      Assert.That(didRemove);

      //Wait for delayed detach
      yield return null;

      Assert.That(firstGroup.graphics, Does.Not.Contains(oneGraphic));
      Assert.That(oneGraphic.attachedGroup, Is.Null);
      Assert.That(oneGraphic.isAttachedToGroup, Is.False);

      bool didAdd = firstGroup.TryAddGraphic(oneGraphic);
      Assert.That(didAdd);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup, Is.True);
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));
    }

    /// <summary>
    /// Test whether or not it is possible to detach and reattach
    /// a graphic.
    /// </summary>
    [UnityTest]
    public IEnumerator CanRemoveAndAddSameFrame() {
      InitTest("OneDynamicGroup");
      yield return null;

      bool didRemove = firstGroup.TryRemoveGraphic(oneGraphic);
      bool didAdd = firstGroup.TryAddGraphic(oneGraphic);

      Assert.That(didRemove);
      Assert.That(didAdd);
      Assert.That(oneGraphic.isAttachedToGroup);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup, Is.True);
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));
    }

    /// <summary>
    /// Test whether or not TryAddGraphic fails when it is already
    /// added to the group.
    /// </summary>
    [UnityTest]
    public IEnumerator DontAddTwice() {
      InitTest("OneDynamicGroup");
      yield return null;

      bool didAddTwice = firstGroup.TryAddGraphic(oneGraphic);
      Assert.IsFalse(didAddTwice);
    }

    /// <summary>
    /// Test whether or not it is possible to switch from one group
    /// to another in the same frame.
    /// </summary>
    [UnityTest]
    public IEnumerator SwitchGroupsSameFrame() {
      InitTest("TwoDynamicGroups");
      yield return null;

      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));

      bool didRemove = firstGroup.TryRemoveGraphic(oneGraphic);
      bool didAdd = secondGroup.TryAddGraphic(oneGraphic);

      Assert.That(didRemove);
      Assert.That(didAdd);

      yield return null;

      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(secondGroup));
    }

    /// <summary>
    /// Validate that when we add a single graphic to two different groups
    /// on the same frame that things don't break.
    /// </summary>
    [UnityTest]
    public IEnumerator AddToTwoGroupsSameFrame() {
      InitTest("TwoEmptyDynamicGroups");
      yield return null;

      CreateGraphic("DisabledMeshGraphic");

      bool didAddToFirst = firstGroup.TryAddGraphic(oneGraphic);
      bool didAddToSecond = secondGroup.TryAddGraphic(oneGraphic);

      Assert.IsTrue(didAddToFirst);
      Assert.IsFalse(didAddToSecond);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup);
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));

      Assert.That(firstGroup.graphics, Contains.Item(oneGraphic));
      Assert.That(secondGroup.graphics, Is.Empty);
    }

    /// <summary>
    /// Validate that we cannot add to a second group when we are already
    /// added to a first.
    /// </summary>
    [UnityTest]
    public IEnumerator CantAddToASecondGroup() {
      InitTest("TwoDynamicGroups");
      yield return null;

      bool didAdd = secondGroup.TryAddGraphic(oneGraphic);

      Assert.IsFalse(didAdd);
      Assert.That(oneGraphic.isAttachedToGroup);
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));
      Assert.IsFalse(oneGraphic.willbeAttached);
      Assert.That(secondGroup.graphics, Is.Empty);
    }

    /// <summary>
    /// Assert that when we remove a graphic from one group and add it
    /// to another, that we cannot turn around on the same frame and add
    /// it back to the group it is supposed to be removed from.
    /// </summary>
    [UnityTest]
    public IEnumerator CantCancelTheRemoveIfWillAttach() {
      InitTest("TwoDynamicGroups");
      yield return null;

      bool didRemove = firstGroup.TryRemoveGraphic(oneGraphic);
      bool didAddToSecond = secondGroup.TryAddGraphic(oneGraphic);
      bool didAddToFirst = firstGroup.TryAddGraphic(oneGraphic);

      Assert.That(oneGraphic.isAttachedToGroup);
      Assert.That(oneGraphic.willbeAttached);
      Assert.That(oneGraphic.willbeDetached);

      Assert.That(didRemove);
      Assert.That(didAddToSecond);
      Assert.IsFalse(didAddToFirst);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup);
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(secondGroup));
      Assert.IsFalse(oneGraphic.willbeDetached);
      Assert.IsFalse(oneGraphic.willbeAttached);
      Assert.That(firstGroup.graphics, Is.Empty);
      Assert.That(secondGroup.graphics, Contains.Item(oneGraphic));
    }

    /// <summary>
    /// Verify that if one calls TryAddGraphic and then TryRemoveGraphic
    /// within the same frame, that it doesn't actually perform an add
    /// or a remove, it just cancels the willAdd request.
    /// </summary>
    [UnityTest]
    public IEnumerator CanCancelTheAddWithARemove() {
      InitTest("OneEmptyDynamicGroup");
      yield return null;

      CreateGraphic("DisabledMeshGraphic");

      bool didAdd = firstGroup.TryAddGraphic(oneGraphic);
      bool didRemove = firstGroup.TryRemoveGraphic(oneGraphic);

      Assert.That(didAdd);
      Assert.That(didRemove);
      Assert.IsFalse(oneGraphic.willbeAttached);
      Assert.IsFalse(oneGraphic.willbeDetached);
      Assert.IsFalse(oneGraphic.isAttachedToGroup);

      yield return null;

      Assert.IsFalse(oneGraphic.isAttachedToGroup);
      Assert.That(firstGroup.graphics, Is.Empty);
    }

    /// <summary>
    /// Verify that when we switch a graphic from one group to another
    /// that everything still works correctly even if they have different
    /// feature sets.
    /// </summary>
    [UnityTest]
    public IEnumerator CanSwitchToAGroupWithADifferentFeatureSet([Values(true, false)] bool reverseGroup) {
      InitTest("TwoDynamicGroupsWithDifferentFeatures");
      yield return null;

      LeapGraphicGroup first, second;
      if (reverseGroup) {
        first = secondGroup;
        second = firstGroup;
      } else {
        first = firstGroup;
        second = secondGroup;
      }

      CreateGraphic("DisabledMeshGraphic");

      bool didAddToFirst = first.TryAddGraphic(oneGraphic);
      Assert.That(didAddToFirst);

      yield return null;

      bool didDetatchFromFirst = oneGraphic.TryDetach();
      Assert.That(didDetatchFromFirst);

      yield return null;

      bool didAddToSecond = second.TryAddGraphic(oneGraphic);
      Assert.That(didAddToSecond);

      yield return null;

      bool didDetatchFromSecond = oneGraphic.TryDetach();
      Assert.That(didDetatchFromSecond);

      yield return null;
    }

    /// <summary>
    /// Verify behavior when we add/remove graphics on the same frame.  Validate both the
    /// case where we add more than we remove, and when we remove more than we add.
    /// </summary>
    [UnityTest]
    public IEnumerator CanAddAndRemoveManyOnSameFrame([Values(true, false)] bool addMany) {
      InitTest("OneEmptyDynamicGroupWith4Features");
      yield return null;

      var first = CreateGraphic("DisabledMeshGraphic");
      var second = CreateGraphic("DisabledMeshGraphic");
      var third = CreateGraphic("DisabledMeshGraphic");

      if (addMany) {
        firstGroup.TryAddGraphic(first);
      } else {
        firstGroup.TryAddGraphic(second);
        firstGroup.TryAddGraphic(third);
      }

      yield return null;

      if (addMany) {
        firstGroup.TryAddGraphic(second);
        firstGroup.TryAddGraphic(third);
        first.TryDetach();
      } else {
        firstGroup.TryAddGraphic(first);
        second.TryDetach();
        third.TryDetach();
      }

      yield return null;

      foreach (var feature in firstGroup.features) {
        var featureData = feature.GetField("featureData") as IList;
        Assert.That(featureData.Count, Is.EqualTo(firstGroup.graphics.Count));
        for (int i = 0; i < featureData.Count; i++) {
          LeapFeatureData data = featureData[i] as LeapFeatureData;
          Assert.That(data.graphic, Is.EqualTo(firstGroup.graphics[i]));
        }
      }
    }
  }
}
#endif
