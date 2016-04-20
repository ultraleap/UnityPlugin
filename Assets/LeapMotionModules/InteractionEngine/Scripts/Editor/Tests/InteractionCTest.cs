using UnityEngine;
using NUnit.Framework;
using System;
using System.IO;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction.Tests {

  [TestFixture(Category = "InteractionC")]
  public abstract class InteractionCTestBase {
    private const string DATA_FOLDER = "InteractionEngine";

    protected INTERACTION_SCENE _scene;

    [SetUp]
    public virtual void Setup() {
      INTERACTION_SCENE_INFO sceneInfo = new INTERACTION_SCENE_INFO();
      sceneInfo.gravity = Vector3.zero.ToCVector();
      sceneInfo.sceneFlags = SceneInfoFlags.None;

      string path = Path.Combine(Application.streamingAssetsPath, DATA_FOLDER);

      InteractionC.CreateScene(ref _scene, ref sceneInfo, path);
    }

    [TearDown]
    public virtual void Teardown() {
      InteractionC.DestroyScene(ref _scene);
    }
  }

  [TestFixture(Category = "InteractionC")]
  public class InteractionCTests : InteractionCTestBase {

    [Test]
    public void CreateAndDestroyScene() { }

    [Test]
    public void UpdateSceneInfo() {
      INTERACTION_SCENE_INFO info = new INTERACTION_SCENE_INFO();
      info.gravity = Vector3.one.ToCVector();
      info.sceneFlags = SceneInfoFlags.HasGravity;

      InteractionC.UpdateSceneInfo(ref _scene, ref info);
    }

    [Test]
    public void EnableDebugFlags() {
      DebugFlags flags = DebugFlags.Lines | DebugFlags.Logging | DebugFlags.Strings;
      InteractionC.EnableDebugFlags(ref _scene, (uint)flags);
    }

    [Test]
    public void UpdateHands() {
      Frame frame = TestHandFactory.MakeTestFrame(0, true, true);
      IntPtr handPtr = HandArrayBuilder.CreateHandArray(frame);

      InteractionC.UpdateHands(ref _scene, 2, handPtr);
      StructAllocator.CleanupAllocations();
    }

    [Test]
    public void UpdateController() {
      INTERACTION_TRANSFORM transform = new INTERACTION_TRANSFORM();
      transform.position = Vector3.one.ToCVector();
      transform.rotation = Quaternion.identity.ToCQuaternion();
      transform.wallTime = 0;

      InteractionC.UpdateController(ref _scene, ref transform);
    }


  }

  public class InteractionCShapeTests : InteractionCTestBase {
    INTERACTION_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;

    public override void Setup() {
      base.Setup();

      INTERACTION_SPHERE_DESCRIPTION sphere = new INTERACTION_SPHERE_DESCRIPTION();
      sphere.shape.type = ShapeType.Sphere;
      sphere.radius = 1.0f;

      IntPtr spherePtr = StructAllocator.AllocateStruct(ref sphere);
      
      InteractionC.AddShapeDescription(ref _scene, spherePtr, out _shapeDescriptionHandle);
    }

    public override void Teardown() {
      InteractionC.RemoveShapeDescription(ref _scene, ref _shapeDescriptionHandle);

      base.Teardown();
    }

    [Test]
    public void AddRemoveShapeDescriptionTest() { }
  }




}
