using UnityEngine;
using NUnit.Framework;
using System.IO;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction.Tests {

  [TestFixture(Category = "ShapeDescriptionPool")]
  public abstract class ShapePoolTestBase {
    private const string DATA_FOLDER = "InteractionEngine";

    protected INTERACTION_SCENE _scene;
    protected ShapeDescriptionPool _pool;
    protected Mesh _mesh;

    [SetUp]
    public virtual void Setup() {
      INTERACTION_SCENE_INFO sceneInfo = new INTERACTION_SCENE_INFO();
      sceneInfo.gravity = Vector3.zero.ToCVector();
      sceneInfo.sceneFlags = SceneInfoFlags.None;

      string path = Path.Combine(Application.streamingAssetsPath, DATA_FOLDER);

      InteractionC.CreateScene(ref _scene, ref sceneInfo, path);

      _pool = new ShapeDescriptionPool(_scene);

      _mesh = new Mesh();
      _mesh.hideFlags = HideFlags.DontSave;
      _mesh.vertices = new Vector3[] { Vector3.left, Vector3.right, Vector3.up };
    }

    [TearDown]
    public virtual void Teardown() {
      _pool.RemoveAllShapes();
      _pool = null;

      Object.DestroyImmediate(_mesh);

      InteractionC.DestroyScene(ref _scene);
    }
  }
  
  public class ShapePoolGetShape : ShapePoolTestBase {
    [Test]
    public void CreateSphere() {
      _pool.GetSphere(1);
    }

    [Test]
    public void CreateObb() {
      _pool.GetOBB(Vector3.one);
    }

    [Test]
    public void CreateCapsule() {
      _pool.GetCapsule(Vector3.left, Vector3.right, 1);
    }

    [Test]
    public void CreateMesh() {
      _pool.GetConvexPolyhedron(_mesh);
    }
  }
  
  public class ShapePoolGetAuto : ShapePoolTestBase {
    protected GameObject _obj;
    protected GameObject _child;

    public override void Setup() {
      base.Setup();
      _obj = new GameObject("Test Object");
      _obj.hideFlags = HideFlags.DontSave;

      _child = new GameObject("Child Test Object");
      _child.hideFlags = HideFlags.DontSave;
      _child.transform.parent = _obj.transform;
      _child.transform.localPosition = Vector3.one;
    }

    public override void Teardown() {
      base.Teardown();
      Object.DestroyImmediate(_obj);
    }

    [Test]
    public void Sphere([Values(true, false)] bool atCenter) {
      addSphere(_obj, atCenter: atCenter);
      _pool.GetAuto(_obj);
    }

    [Test]
    public void Box([Values(true, false)] bool atCenter) {
      addBox(_obj, atCenter);
      _pool.GetAuto(_obj);
    }

    [Test]
    public void Capsule([Values(0, 1, 2)] int direction) {
      addCapsule(_obj, direction);
      _pool.GetAuto(_obj);
    }

    [Test]
    public void Mesh() {
      addMesh(_obj);
      _pool.GetAuto(_obj);
    }

    [Test]
    public void Compound() {
      addSphere(_obj, false);
      addBox(_obj, false);
      addCapsule(_obj, 0);
      addMesh(_obj);
      _pool.GetAuto(_obj);
    }

    [Test]
    public void WithChild() {
      addSphere(_child, false);
      _pool.GetAuto(_obj);
    }

    protected void addSphere(GameObject obj, bool atCenter) {
      var sphere = obj.AddComponent<SphereCollider>();
      sphere.center = atCenter ? Vector3.zero : Vector3.one;
      sphere.radius = 1;
    }

    protected void addBox(GameObject obj, bool atCenter) {
      var box = obj.AddComponent<BoxCollider>();
      box.center = atCenter ? Vector3.zero : Vector3.one;
      box.size = Vector3.one;
    }

    protected void addCapsule(GameObject obj, int direction) {
      var capsule = obj.AddComponent<CapsuleCollider>();
      capsule.center = Vector3.one;
      capsule.height = 1;
      capsule.radius = 1;
      capsule.direction = direction;
    }

    protected void addMesh(GameObject obj) {
      var meshC = obj.AddComponent<MeshCollider>();
      meshC.sharedMesh = _mesh;
    }
  }
}
