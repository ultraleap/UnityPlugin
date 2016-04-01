using UnityEngine;
using System.Linq;
using NUnit.Framework;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction.Test {

  public class KabschTest {
    LEAP_IE_KABSCH _kabsch;

    [SetUp]
    public void Setup() {
      KabschC.Construct(ref _kabsch);
    }

    [TearDown]
    public void Teardown() {
      KabschC.Destruct(ref _kabsch);
    }

    [Test]
    public void TranslationTest() {
      Vector3[] v0 = new Vector3[3];
      v0[0] = new Vector3(1, 0, 0);
      v0[1] = new Vector3(0, 1, 0);
      v0[2] = new Vector3(0, 0, 1);

      var v1 = v0.Select(v => v + Vector3.one).ToArray();

      for (int i = 0; i < 3; i++) {
        var l0 = new LEAP_VECTOR(v0[i]);
        var l1 = new LEAP_VECTOR(v1[i]);
        KabschC.AddPoint(ref _kabsch, ref l0, ref l1, 1.0f);
      }

      KabschC.Solve(ref _kabsch);

      LEAP_VECTOR leapTranslation;
      KabschC.GetTranslation(ref _kabsch, out leapTranslation);
      Vector3 translation = leapTranslation.ToUnityVector();

      Assert.That(translation, Is.EqualTo(Vector3.one));
    }
  }
}
