using UnityEngine.TestTools;
using NUnit.Framework;
using System.Collections;

namespace Leap.Unity.GraphicalRenderer.Tests {

  public class AddRemoveTests : GraphicRendererTestBase {

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
    /// Validate that we can add a graphic that was spawned in
    /// as a prefab and then added by enabling the gameobject.
    /// </summary>
    [UnityTest]
    public IEnumerator CanAddPrefabByEnabling() {
      InitTest("OneEmptyDynamicGroup");
      yield return null;

      CreateGraphic("DisabledMeshGraphic");

      Assert.That(oneGraphic.OnAwake().hasNotFired);
      Assert.That(oneGraphic.OnEnable().hasNotFired);
      Assert.That(oneGraphic.OnStart().hasNotFired);

      Assert.That(oneGraphic.isAttachedToGroup, Is.False);
      Assert.That(firstGroup.graphics, Is.Empty);

      yield return null;

      Assert.That(oneGraphic.isAttachedToGroup, Is.False);
      Assert.That(firstGroup.graphics, Is.Empty);

      yield return null;

      oneGraphic.gameObject.SetActive(true);

      Assert.That(oneGraphic.OnAwake().hasFired);
      Assert.That(oneGraphic.OnEnable().hasFired);

      yield return null;

      Assert.That(oneGraphic.OnStart().hasFired);

      Assert.That(oneGraphic.isAttachedToGroup);
      Assert.That(oneGraphic.attachedGroup, Is.EqualTo(firstGroup));
      Assert.That(firstGroup.graphics, Contains.Item(oneGraphic));
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
  }
}
