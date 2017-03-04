using UnityEngine;
using Leap.Unity.Attributes;

namespace Procedural.DynamicMesh {

  public class ProceduralMesh : MonoBehaviour {

    [SerializeField]
    private MeshBuilder _builder = new MeshBuilder();

    [SerializeField]
    private MeshFilter _dest;

    [Disable]
    [SerializeField]
    private Mesh _mesh;

    public Mesh mesh {
      get {
        return _mesh;
      }
    }

    public void UpdateWithMesh(RawMesh rawMesh) {
      if (_mesh == null) {
        _mesh = new Mesh();
        _mesh.name = "Generated mesh " + Random.Range(100, 200);
      }

      _builder.Build(rawMesh, _mesh);

      MeshFilter filter = GetComponent<MeshFilter>();
      if (filter != null) {
        filter.sharedMesh = _mesh;
      }

      if (_dest != null) {
        _dest.sharedMesh = _mesh;
      }
    }
  }
}
