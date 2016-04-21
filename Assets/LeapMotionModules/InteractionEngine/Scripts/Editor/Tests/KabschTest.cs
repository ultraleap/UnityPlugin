using UnityEngine;
using System.Linq;
using NUnit.Framework;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction.Tests {

  [TestFixture(Category = "KabschC")]
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
    public void RigidTransformTest([Values(0, 1)] float dx,
                                   [Values(0, -20)] float dy,
                                   [Values(0, 5.3f)] float dz,
                                   [Values(0, 90)]   float dax,
                                   [Values(0, 180)]  float day,
                                   [Values(0, 270)]  float daz) {

      Vector3 desiredTranslation = new Vector3(dx, dy, dz);
      Quaternion desiredRotation = Quaternion.Euler(dax, day, daz);

      Vector3 solvedTranslation;
      Quaternion solvedRotation;

      solveWithTRS(desiredTranslation, desiredRotation, out solvedTranslation, out solvedRotation);

      Assert.That(desiredTranslation.x, Is.EqualTo(solvedTranslation.x).Within(0.001f), "X");
      Assert.That(desiredTranslation.y, Is.EqualTo(solvedTranslation.y).Within(0.001f), "Y");
      Assert.That(desiredTranslation.z, Is.EqualTo(solvedTranslation.z).Within(0.001f), "Z");

      Assert.That(Quaternion.Angle(desiredRotation, solvedRotation), Is.EqualTo(0.0f).Within(1), "A");
    }
    
    private void solveWithTRS(Vector3 translation, 
                              Quaternion rotation, 
                              out Vector3 solvedTranslation, 
                              out Quaternion solvedRotation) {
      Vector3[] v0 = new Vector3[3];
      v0[0] = new Vector3(1, 0, 0);
      v0[1] = new Vector3(0, 1, 0);
      v0[2] = new Vector3(0, 0, 1);

      var v1 = v0.Select(v => rotation * v + translation).ToArray();

      for (int i = 0; i < 3; i++) {
        var l0 = (v0[i]).ToCVector();
        var l1 = (v1[i]).ToCVector();
        KabschC.AddPoint(ref _kabsch, ref l0, ref l1, 1.0f);
      }

      KabschC.Solve(ref _kabsch);

      LEAP_VECTOR leapTranslation;
      LEAP_QUATERNION leapRotation;

      KabschC.GetTranslation(ref _kabsch, out leapTranslation);
      KabschC.GetRotation(ref _kabsch, out leapRotation);

      solvedTranslation = leapTranslation.ToVector3();
      solvedRotation = leapRotation.ToQuaternion();
    }
  }
}
