using UnityEngine;
using System;
using System.Collections.Generic;
using LeapInternal;
using InteractionEngine.CApi;

public class ShapeDescriptionPool {

  private LEAP_IE_SCENE _scene;

  private Dictionary<float, LEAP_IE_SHAPE_DESCRIPTION_HANDLE> _sphereDescMap;
  private Dictionary<Vector3, LEAP_IE_SHAPE_DESCRIPTION_HANDLE> _obbDescMap;
  private Dictionary<Mesh, LEAP_IE_SHAPE_DESCRIPTION_HANDLE> _meshDescMap;
  private List<LEAP_IE_SHAPE_DESCRIPTION_HANDLE> _allHandles;

  public ShapeDescriptionPool(LEAP_IE_SCENE scene) {
    _scene = scene;

    _sphereDescMap = new Dictionary<float, LEAP_IE_SHAPE_DESCRIPTION_HANDLE>();
    _obbDescMap = new Dictionary<Vector3, LEAP_IE_SHAPE_DESCRIPTION_HANDLE>();
    _meshDescMap = new Dictionary<Mesh, LEAP_IE_SHAPE_DESCRIPTION_HANDLE>();
    _allHandles = new List<LEAP_IE_SHAPE_DESCRIPTION_HANDLE>();
  }

  public void RemoveAllShapes() {
    for (int i = 0; i < _allHandles.Count; i++) {
      LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle = _allHandles[i];
      InteractionC.RemoveShapeDescription(ref _scene, ref handle);
    }

    _allHandles.Clear();
    _sphereDescMap.Clear();
    _obbDescMap.Clear();
    _meshDescMap.Clear();
  }

  /// <summary>
  /// Gets a handle to a sphere shape description of the given radius
  /// </summary>
  /// <param name="radius"></param>
  /// <returns></returns>
  public LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetSphere(float radius) {
    LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle;
    if (!_sphereDescMap.TryGetValue(radius, out handle)) {
      LEAP_IE_SPHERE_DESCRIPTION sphereDesc = new LEAP_IE_SPHERE_DESCRIPTION();
      sphereDesc.shape.type = eLeapIEShapeType.eLeapIEShape_Sphere;
      sphereDesc.radius = radius;

      IntPtr spherePtr = StructAllocator.AllocateStruct(sphereDesc);
      InteractionC.AddShapeDescription(ref _scene, spherePtr, out handle);
      StructAllocator.CleanupAllocations();

      _sphereDescMap[radius] = handle;
      _allHandles.Add(handle);
    }

    return handle;
  }

  /// <summary>
  /// Gets a handle to an OBB description of the given extents
  /// </summary>
  /// <param name="extents"></param>
  /// <returns></returns>
  public LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetOBB(Vector3 extents) {
    LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle;
    if (!_obbDescMap.TryGetValue(extents, out handle)) {
      LEAP_IE_OBB_DESCRIPTION obbDesc = new LEAP_IE_OBB_DESCRIPTION();
      obbDesc.shape.type = eLeapIEShapeType.eLeapIEShape_OBB;
      obbDesc.extents = new LeapInternal.LEAP_VECTOR(extents);

      IntPtr obbPtr = StructAllocator.AllocateStruct(obbDesc);
      InteractionC.AddShapeDescription(ref _scene, obbPtr, out handle);
      StructAllocator.CleanupAllocations();

      _obbDescMap[extents] = handle;
      _allHandles.Add(handle);
    }

    return handle;
  }

  /// <summary>
  /// Gets a handle to a convex mesh description of the provided mesh
  /// </summary>
  /// <param name="mesh"></param>
  /// <returns></returns>
  public LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetConvexPolyhedron(Mesh mesh) {
    LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle;
    if (!_meshDescMap.TryGetValue(mesh, out handle)) {
      LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION meshDesc = new LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION();
      meshDesc.shape.type = eLeapIEShapeType.eLeapIEShape_Convex;
      meshDesc.radius = 0.0f;
      meshDesc.nVerticies = (uint)mesh.vertexCount;
      meshDesc.pVertices = StructAllocator.AllocateArray<LEAP_VECTOR>(mesh.vertexCount);
      for (int i = 0; i < mesh.vertexCount; i++) {
        LEAP_VECTOR v = new LEAP_VECTOR(mesh.vertices[i]);
        StructMarshal<LEAP_VECTOR>.CopyIntoArray(meshDesc.pVertices, v, i);
      }

      IntPtr meshPtr = StructAllocator.AllocateStruct(meshDesc);
      InteractionC.AddShapeDescription(ref _scene, meshPtr, out handle);
      StructAllocator.CleanupAllocations();

      _meshDescMap[mesh] = handle;
      _allHandles.Add(handle);
    }

    return handle;
  }

  /// <summary>
  /// Returns a handle to a description that represents the structure of the colliders
  /// attatched to the gameObject passed in.
  /// </summary>
  /// <param name="obj"></param>
  /// <returns></returns>
  public LEAP_IE_SHAPE_DESCRIPTION_HANDLE GetAuto(GameObject obj) {
    throw new NotImplementedException();
  }

}
