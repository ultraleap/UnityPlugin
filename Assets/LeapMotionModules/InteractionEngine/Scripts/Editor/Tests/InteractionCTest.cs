using UnityEngine;
using NUnit.Framework;
using System;
using System.IO;
using System.Collections.Generic;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction.Tests {

  [TestFixture(Category = "InteractionC")]
  public abstract class InteractionCTestBase {
    private const string DATA_FOLDER = "InteractionEngine";

    protected INTERACTION_SCENE _scene;

    [SetUp]
    public virtual void Setup() {
      InteractionC.CreateScene(ref _scene);

      INTERACTION_SCENE_INFO sceneInfo = new INTERACTION_SCENE_INFO();
      sceneInfo.sceneFlags = SceneInfoFlags.ContactEnabled;

      InteractionC.UpdateSceneInfo(ref _scene, ref sceneInfo);
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

    [Test]
    public void GetDebugLines() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags.Lines);
      InteractionC.GetDebugLines(ref _scene, new List<INTERACTION_DEBUG_LINE>());
    }

    [Test]
    public void GetDebugStrings() {
      InteractionC.EnableDebugFlags(ref _scene, (uint)DebugFlags.Strings);

      List<string> strings = new List<string>();
      InteractionC.GetDebugStrings(ref _scene, strings);
    }
  }

  public class InteractionCShapeTests : InteractionCTestBase {
    INTERACTION_SHAPE_DESCRIPTION_HANDLE _shapeDescriptionHandle;
    INTERACTION_SHAPE_INSTANCE_HANDLE _shapeInstanceHandle;

    public override void Setup() {
      base.Setup();

      INTERACTION_SPHERE_DESCRIPTION sphere = new INTERACTION_SPHERE_DESCRIPTION();
      sphere.shape.type = ShapeType.Sphere;
      sphere.radius = 1.0f;

      IntPtr spherePtr = StructAllocator.AllocateStruct(ref sphere);

      InteractionC.AddShapeDescription(ref _scene, spherePtr, out _shapeDescriptionHandle);

      INTERACTION_CREATE_SHAPE_INFO info = new INTERACTION_CREATE_SHAPE_INFO();
      info.shapeFlags = ShapeInfoFlags.None;

      INTERACTION_TRANSFORM transform = new INTERACTION_TRANSFORM();
      transform.position = Vector3.zero.ToCVector();
      transform.rotation = Quaternion.identity.ToCQuaternion();
      transform.wallTime = 0;

      InteractionC.CreateShapeInstance(ref _scene, ref _shapeDescriptionHandle, ref transform, ref info, out _shapeInstanceHandle);
    }

    public override void Teardown() {
      InteractionC.DestroyShapeInstance(ref _scene, ref _shapeInstanceHandle);
      InteractionC.RemoveShapeDescription(ref _scene, ref _shapeDescriptionHandle);

      base.Teardown();
    }

    [Test]
    public void CreateDestroyInstance() { }

    [Test]
    public void UpdateInstance() {
      INTERACTION_TRANSFORM transform = new INTERACTION_TRANSFORM();
      transform.position = Vector3.one.ToCVector();
      transform.rotation = Quaternion.identity.ToCQuaternion();
      transform.wallTime = 0;

      INTERACTION_UPDATE_SHAPE_INFO info = new INTERACTION_UPDATE_SHAPE_INFO();
      info.angularAcceleration = Vector3.zero.ToCVector();
      info.angularVelocity = Vector3.zero.ToCVector();
      info.linearAcceleration = Vector3.zero.ToCVector();
      info.linearVelocity = Vector3.zero.ToCVector();

      InteractionC.UpdateShapeInstance(ref _scene, ref transform, ref info, ref _shapeInstanceHandle);
    }

    [Test]
    public void EnableContactThenUpdate() {
      INTERACTION_SCENE_INFO info = new INTERACTION_SCENE_INFO();
      info.sceneFlags = SceneInfoFlags.ContactEnabled;

      InteractionC.UpdateSceneInfo(ref _scene, ref info);

      UpdateInstance();
    }
  }
}
