using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class ShapeDescriptionPool {
    public const int DECIMAL_CACHING_PRECISION = 1000;

    private INTERACTION_SCENE _scene;

    private Dictionary<float, INTERACTION_SHAPE_DESCRIPTION_HANDLE> _sphereDescMap;
    private Dictionary<Vector3, INTERACTION_SHAPE_DESCRIPTION_HANDLE> _obbDescMap;
    private Dictionary<Mesh, INTERACTION_SHAPE_DESCRIPTION_HANDLE> _meshDescMap;

    private Dictionary<INTERACTION_SHAPE_DESCRIPTION_HANDLE, ShapeInfo> _allHandles;

    public struct ShapeInfo {
      public bool isCached;

      public ShapeInfo(bool isCached) {
        this.isCached = isCached;
      }
    }

    public ShapeDescriptionPool(INTERACTION_SCENE scene) {
      _scene = scene;

      _sphereDescMap = new Dictionary<float, INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
      _obbDescMap = new Dictionary<Vector3, INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
      _meshDescMap = new Dictionary<Mesh, INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
      _allHandles = new Dictionary<INTERACTION_SHAPE_DESCRIPTION_HANDLE, ShapeInfo>();
    }

    /// <summary>
    /// Removes and destroys all descriptions from the internal scene.
    /// </summary>
    public void RemoveAllShapes() {
      foreach (var handle in _allHandles.Keys) {
        var localHandle = handle;
        InteractionC.RemoveShapeDescription(ref _scene, ref localHandle);
      }

      _allHandles.Clear();
      _sphereDescMap.Clear();
      _obbDescMap.Clear();
      _meshDescMap.Clear();
    }

    /// <summary>
    /// Returns a shape handle into the pool. 
    /// </summary>
    public void ReturnShape(INTERACTION_SHAPE_DESCRIPTION_HANDLE handle) {
      ShapeInfo info;
      if (!_allHandles.TryGetValue(handle, out info)) {
        throw new InvalidOperationException("Tried to return a handle that was not allocated.");
      }

      if (!info.isCached) {
        _allHandles.Remove(handle);
        InteractionC.RemoveShapeDescription(ref _scene, ref handle);
      }
    }

    /// <summary>
    /// Gets a handle to a sphere shape description of the given radius
    /// </summary>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetSphere(float radius) {
      float roundedRadius = (int)(radius * DECIMAL_CACHING_PRECISION);

      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      if (!_sphereDescMap.TryGetValue(roundedRadius, out handle)) {
        IntPtr spherePtr = allocateSphere(radius);
        InteractionC.AddShapeDescription(ref _scene, spherePtr, out handle);
        StructAllocator.CleanupAllocations();

        _sphereDescMap[roundedRadius] = handle;
        _allHandles[handle] = new ShapeInfo(isCached: true);
      }

      return handle;
    }

    /// <summary>
    /// Gets a handle to an OBB description of the given extents
    /// </summary>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetOBB(Vector3 extents) {
      Vector3 roundedExtents = new Vector3();
      roundedExtents.x = (int)(extents.x * DECIMAL_CACHING_PRECISION);
      roundedExtents.y = (int)(extents.y * DECIMAL_CACHING_PRECISION);
      roundedExtents.z = (int)(extents.z * DECIMAL_CACHING_PRECISION);

      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      if (!_obbDescMap.TryGetValue(roundedExtents, out handle)) {
        IntPtr obbPtr = allocateObb(extents);
        InteractionC.AddShapeDescription(ref _scene, obbPtr, out handle);
        StructAllocator.CleanupAllocations();

        _obbDescMap[roundedExtents] = handle;
        _allHandles[handle] = new ShapeInfo(isCached: true);
      }

      return handle;
    }

    /// <summary>
    /// Gets a handle to a convex mesh description that describes the given capsule.
    /// </summary>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetCapsule(Vector3 p0, Vector3 p1, float radius) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      IntPtr capsulePtr = allocateCapsule(p0, p1, radius);
      InteractionC.AddShapeDescription(ref _scene, capsulePtr, out handle);
      StructAllocator.CleanupAllocations();
      _allHandles[handle] = new ShapeInfo(isCached: false);
      return handle;
    }

    /// <summary>
    /// Gets a handle to a convex mesh description of the provided mesh.  Any changes
    /// to the mesh will not be reflected in the description once it is generated.
    /// </summary>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetConvexPolyhedron(MeshCollider meshCollider) {
      if (meshCollider.sharedMesh == null) { throw new NotImplementedException("MeshCollider missing sharedMesh."); }

      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      if (!_meshDescMap.TryGetValue(meshCollider.sharedMesh, out handle)) {
        IntPtr meshPtr = allocateConvex(meshCollider, Matrix4x4.identity);
        InteractionC.AddShapeDescription(ref _scene, meshPtr, out handle);
        StructAllocator.CleanupAllocations();

        _meshDescMap[meshCollider.sharedMesh] = handle;
        _allHandles[handle] = new ShapeInfo(isCached: false);
      }

      return handle;
    }

    /// <summary>
    /// Gets a handle to a convex mesh description of the provided mesh.  Any changes
    /// to the mesh will not be reflected in the description once it is generated.
    /// This version always allocates a new handle for the transformed data.
    /// </summary>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetConvexPolyhedron(MeshCollider meshCollider, Matrix4x4 transform) {
      if (meshCollider.sharedMesh == null) { throw new NotImplementedException("MeshCollider missing sharedMesh."); }

      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      IntPtr meshPtr = allocateConvex(meshCollider, transform);
      InteractionC.AddShapeDescription(ref _scene, meshPtr, out handle);
      StructAllocator.CleanupAllocations();

      _allHandles[handle] = new ShapeInfo(isCached: false);

      return handle;
    }

    /// <summary>
    /// Returns a handle to a description that represents the structure of the colliders
    /// attatched to the gameObject passed in.  Changes to the following properties will not
    /// have any affect on the description once it is generated:
    ///   scale of parent object
    ///   position of child objects
    ///   rotation of child objects
    ///   scale of child objects
    ///   collider properties
    /// </summary>
    private List<Collider> _tempColliderList = new List<Collider>();
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetCollision(GameObject parentObject) {
      parentObject.GetComponentsInChildren<Collider>(_tempColliderList);

      // Remove Colliders that are children of other IInteractionBehaviour.
      Transform parentTransform = parentObject.transform;
      for (int i = _tempColliderList.Count; i-- > 0; ) {
        Transform it = _tempColliderList[i].transform;
        while (it != parentTransform) {
          if (it.GetComponent<IInteractionBehaviour>() != null) {
            _tempColliderList.RemoveAt(i);
            break;
          }
          it = it.parent;
        }
      }

      if (_tempColliderList.Count == 0) {
        throw new InvalidOperationException("The GameObject " + parentObject + " did not have any colliders.");
      }

      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle = new INTERACTION_SHAPE_DESCRIPTION_HANDLE();

      // Try optimized encodings for a single collider.  Everything else is a compound.
      if (_tempColliderList.Count == 1) {
        if (getCollisionSingleInternal(parentObject, ref handle)) {
          return handle;
        }
      }

      INTERACTION_COMPOUND_DESCRIPTION compoundDesc = new INTERACTION_COMPOUND_DESCRIPTION();
      compoundDesc.shape.type = ShapeType.Compound;
      compoundDesc.nShapes = (uint)_tempColliderList.Count;
      compoundDesc.pShapes = StructAllocator.AllocateArray<IntPtr>(_tempColliderList.Count);
      compoundDesc.pTransforms = new INTERACTION_TRANSFORM[_tempColliderList.Count];

      // The parent's relative pose is a components of the parents transform that
      // child transforms are considered to be relative two.  In this case scale
      // and shear are not considered a property of the parents relative pose and
      // therefore these calcualtions will allow the child to inherit the parents
      // scale.

      Matrix4x4 parentRelativePose = Matrix4x4.TRS(parentObject.transform.position, parentObject.transform.rotation, Vector3.one);
      Matrix4x4 inverseParentRelativePose = parentRelativePose.inverse;

      for (int i = 0; i < _tempColliderList.Count; i++) {
        Collider collider = _tempColliderList[i];

        // Transform to apply to collision shape.
        Matrix4x4 localToParentRelative = inverseParentRelativePose * collider.transform.localToWorldMatrix;

        // Values used to construct INTERACTION_TRANSFORM.
        Vector3 parentRelativePos = Vector3.zero;
        Quaternion parentRelativeRot = Quaternion.identity;

        IntPtr shapePtr;
        if (collider is MeshCollider) {
          // Mesh always has an identity associated transform that can be baked into the verts.
          MeshCollider meshCollider = collider as MeshCollider;
          shapePtr = allocateConvex(meshCollider, localToParentRelative);
        } else {
          //  Rotation and scale are lossy when shear is involved.
          parentRelativeRot = Quaternion.Inverse(parentObject.transform.rotation) * collider.transform.rotation;

          Vector3 scaleAlongLocalToParentAxes = new Vector3(localToParentRelative.GetColumn(0).magnitude,
                                                            localToParentRelative.GetColumn(1).magnitude,
                                                            localToParentRelative.GetColumn(2).magnitude);
          if (collider is SphereCollider) {
            SphereCollider sphereCollider = collider as SphereCollider;
            parentRelativePos = localToParentRelative.MultiplyPoint(sphereCollider.center);

            float aproximateScale = Mathf.Max(scaleAlongLocalToParentAxes.x, Mathf.Max(scaleAlongLocalToParentAxes.y, scaleAlongLocalToParentAxes.z));
            shapePtr = allocateSphere(sphereCollider.radius * aproximateScale);
          } else if (collider is BoxCollider) {
            BoxCollider boxCollider = collider as BoxCollider;
            parentRelativePos = localToParentRelative.MultiplyPoint(boxCollider.center);

            Vector3 extents = boxCollider.size * 0.5f;
            extents.Scale(scaleAlongLocalToParentAxes);
            shapePtr = allocateObb(extents);
          } else if (collider is CapsuleCollider) {
            CapsuleCollider capsuleCollider = collider as CapsuleCollider;
            if ((uint)capsuleCollider.direction >= 3u)
              throw new InvalidOperationException("Unexpected capsule direction " + capsuleCollider.direction);

            parentRelativePos = localToParentRelative.MultiplyPoint(capsuleCollider.center);

            Vector3 primaryAxis = new Vector3((capsuleCollider.direction == 0) ? 1 : 0, (capsuleCollider.direction == 1) ? 1 : 0, (capsuleCollider.direction == 2) ? 1 : 0);
            float primaryAxisScale = scaleAlongLocalToParentAxes[capsuleCollider.direction];
            float perpendicularScale = Mathf.Max(scaleAlongLocalToParentAxes[(capsuleCollider.direction + 1) % 3], scaleAlongLocalToParentAxes[(capsuleCollider.direction + 2) % 3]);

            float scaledHeight = capsuleCollider.height * primaryAxisScale;
            float scaledRadius = capsuleCollider.radius * perpendicularScale;
            float interiorExtent = (scaledHeight * 0.5f) - scaledRadius;

            Vector3 p0 = primaryAxis * interiorExtent;
            shapePtr = allocateCapsule(p0, -p0, scaledRadius);
          } else {
            throw new InvalidOperationException("Unsupported collider type " + collider.GetType());
          }
        }

        StructMarshal<IntPtr>.CopyIntoArray(compoundDesc.pShapes, ref shapePtr, i);

        INTERACTION_TRANSFORM ieTransform = new INTERACTION_TRANSFORM();
        ieTransform.position = parentRelativePos.ToCVector();
        ieTransform.rotation = parentRelativeRot.ToCQuaternion();
        compoundDesc.pTransforms[i] = ieTransform;
      }

      IntPtr compoundPtr = StructAllocator.AllocateStruct(ref compoundDesc);
      InteractionC.AddShapeDescription(ref _scene, compoundPtr, out handle);
      StructAllocator.CleanupAllocations();

      _tempColliderList.Clear();
      _allHandles[handle] = new ShapeInfo(isCached: false);

      return handle;
    }

    private bool getCollisionSingleInternal(GameObject parentObject, ref INTERACTION_SHAPE_DESCRIPTION_HANDLE shape) {
      Collider collider = _tempColliderList[0];

      if (collider.gameObject != parentObject) {
        return false; // Potential for optimization.  See GetCollision().
      }

      Vector3 scale3 = parentObject.transform.lossyScale;

      if (collider is SphereCollider) {
        SphereCollider sphereCollider = collider as SphereCollider;

        if (sphereCollider.center != Vector3.zero) {
          return false;
        }

        float aproximateScale = Mathf.Max(scale3.x, Mathf.Max(scale3.y, scale3.z));
        shape = GetSphere(sphereCollider.radius * aproximateScale);
        return true;
      } else if (collider is BoxCollider) {
        BoxCollider boxCollider = collider as BoxCollider;

        if (boxCollider.center != Vector3.zero) {
          return false;
        }

        Vector3 extents = boxCollider.size * 0.5f;
        extents.Scale(scale3);
        shape = GetOBB(extents);
        return true;
      } else if (collider is MeshCollider) {
        MeshCollider meshCollider = collider as MeshCollider;

        // Create a new mesh scaled as needed.  Scale is not dynamic and must be baked in.
        shape = GetConvexPolyhedron(meshCollider, Matrix4x4.TRS(Vector3.zero, Quaternion.identity, scale3));
        return true;
      }

      return false; // Compound
    }

    private IntPtr allocateSphere(float radius) {
      INTERACTION_SPHERE_DESCRIPTION sphereDesc = new INTERACTION_SPHERE_DESCRIPTION();
      sphereDesc.shape.type = ShapeType.Sphere;
      sphereDesc.radius = radius;

      IntPtr spherePtr = StructAllocator.AllocateStruct(ref sphereDesc);
      return spherePtr;
    }

    private IntPtr allocateObb(Vector3 extents) {
      INTERACTION_OBB_DESCRIPTION obbDesc = new INTERACTION_OBB_DESCRIPTION();
      obbDesc.shape.type = ShapeType.OBB;
      obbDesc.extents = extents.ToCVector();

      IntPtr obbPtr = StructAllocator.AllocateStruct(ref obbDesc);
      return obbPtr;
    }

    private IntPtr allocateCapsule(Vector3 p0, Vector3 p1, float radius) {
      INTERACTION_CONVEX_POLYHEDRON_DESCRIPTION meshDesc = new INTERACTION_CONVEX_POLYHEDRON_DESCRIPTION();
      meshDesc.shape.type = ShapeType.Convex;
      meshDesc.radius = radius;
      meshDesc.nVerticies = 2;
      meshDesc.pVertices = new LEAP_VECTOR[2];
      meshDesc.pVertices[0] = p0.ToCVector();
      meshDesc.pVertices[1] = p1.ToCVector();

      IntPtr capsulePtr = StructAllocator.AllocateStruct(ref meshDesc);
      return capsulePtr;
    }

    private IntPtr allocateConvex(MeshCollider meshCollider, Matrix4x4 localToParentRelative) {

      if (meshCollider.sharedMesh == null) { throw new NotImplementedException("MeshCollider missing sharedMesh."); }
      if (meshCollider.convex == false) { throw new NotImplementedException("MeshCollider must be convex."); }

      Mesh mesh = meshCollider.sharedMesh;

      INTERACTION_CONVEX_POLYHEDRON_DESCRIPTION meshDesc = new INTERACTION_CONVEX_POLYHEDRON_DESCRIPTION();
      meshDesc.shape.type = ShapeType.Convex;
      meshDesc.radius = 0.0f;
      meshDesc.nVerticies = (uint)mesh.vertexCount;
      meshDesc.pVertices = new LEAP_VECTOR[mesh.vertexCount];

      for (int i = 0; i < mesh.vertexCount; i++) {
        LEAP_VECTOR v = localToParentRelative.MultiplyPoint(mesh.vertices[i]).ToCVector();
        meshDesc.pVertices[i] = v;
      }

      IntPtr meshPtr = StructAllocator.AllocateStruct(ref meshDesc);
      return meshPtr;
    }
  }
}
