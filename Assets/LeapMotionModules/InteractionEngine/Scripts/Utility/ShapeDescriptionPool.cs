using UnityEngine;
using System;
using System.Collections.Generic;
using LeapInternal;
using Leap.Unity.Interaction.CApi;

namespace Leap.Unity.Interaction {

  public class ShapeDescriptionPool {
    private INTERACTION_SCENE _scene;

    private Dictionary<float, INTERACTION_SHAPE_DESCRIPTION_HANDLE> _sphereDescMap;
    private Dictionary<Vector3, INTERACTION_SHAPE_DESCRIPTION_HANDLE> _obbDescMap;
    private Dictionary<Mesh, INTERACTION_SHAPE_DESCRIPTION_HANDLE> _meshDescMap;
    private List<INTERACTION_SHAPE_DESCRIPTION_HANDLE> _allHandles;

    public ShapeDescriptionPool(INTERACTION_SCENE scene) {
      _scene = scene;

      _sphereDescMap = new Dictionary<float, INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
      _obbDescMap = new Dictionary<Vector3, INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
      _meshDescMap = new Dictionary<Mesh, INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
      _allHandles = new List<INTERACTION_SHAPE_DESCRIPTION_HANDLE>();
    }

    /// <summary>
    /// Removes and destroys all descriptions from the internal scene.
    /// </summary>
    public void RemoveAllShapes() {
      for (int i = 0; i < _allHandles.Count; i++) {
        INTERACTION_SHAPE_DESCRIPTION_HANDLE handle = _allHandles[i];
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
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetSphere(float radius) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      if (!_sphereDescMap.TryGetValue(radius, out handle)) {
        IntPtr spherePtr = allocateSphere(radius);
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
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetOBB(Vector3 extents) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      if (!_obbDescMap.TryGetValue(extents, out handle)) {
        IntPtr obbPtr = allocateObb(extents);
        InteractionC.AddShapeDescription(ref _scene, obbPtr, out handle);
        StructAllocator.CleanupAllocations();

        _obbDescMap[extents] = handle;
        _allHandles.Add(handle);
      }

      return handle;
    }

    /// <summary>
    /// Gets a handle to a convex mesh description that describes the given capsule.
    /// </summary>
    /// <param name="p0"></param>
    /// <param name="p1"></param>
    /// <param name="radius"></param>
    /// <returns></returns>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetCapsule(Vector3 p0, Vector3 p1, float radius) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      IntPtr capsulePtr = allocateCapsule(p0, p1, radius);
      InteractionC.AddShapeDescription(ref _scene, capsulePtr, out handle);
      StructAllocator.CleanupAllocations();
      _allHandles.Add(handle);
      return handle;
    }

    /// <summary>
    /// Gets a handle to a convex mesh description of the provided mesh.  Any changes
    /// to the mesh will not be reflected in the description once it is generated.
    /// </summary>
    /// <param name="mesh"></param>
    /// <returns></returns>
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetConvexPolyhedron(Mesh mesh) {
      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      if (!_meshDescMap.TryGetValue(mesh, out handle)) {
        IntPtr meshPtr = allocateConvex(mesh, 1.0f);
        InteractionC.AddShapeDescription(ref _scene, meshPtr, out handle);
        StructAllocator.CleanupAllocations();

        _meshDescMap[mesh] = handle;
        _allHandles.Add(handle);
      }

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
    /// <param name="obj"></param>
    /// <returns></returns>
    private List<Collider> _tempColliderList = new List<Collider>();
    public INTERACTION_SHAPE_DESCRIPTION_HANDLE GetAuto(GameObject parentObject) {
      if (!isUniformScale(parentObject.transform)) {
        throw new InvalidOperationException("The GameObject " + parentObject + " did not have a uniform scale.");
      }

      parentObject.GetComponentsInChildren(_tempColliderList);

      if (_tempColliderList.Count == 0) {
        throw new InvalidOperationException("The GameObject " + parentObject + " did not have any colliders.");
      }

      //Optimization for a single collider
      if (_tempColliderList.Count == 1) {
        Collider collider = _tempColliderList[0];

        if (collider.gameObject != parentObject) {
          //TODO: break out of single collider optimization in this case
          throw new NotImplementedException("Child colliders are currently not supported.");
        }

        float scale = parentObject.transform.lossyScale.x;

        if (collider is SphereCollider) {
          SphereCollider sphereCollider = collider as SphereCollider;

          if (sphereCollider.center != Vector3.zero) {
            //TODO: break out of single collider optimization in this case
            throw new NotImplementedException("Colliders with non-zero centers are currently not supported.");
          }

          return GetSphere(sphereCollider.radius * scale);
        } else if (collider is BoxCollider) {
          BoxCollider boxCollider = collider as BoxCollider;

          if (boxCollider.center != Vector3.zero) {
            //TODO: break out of single collider optimization in this case
            throw new NotImplementedException("Colliders with non-zero centers are currently not supported.");
          }

          return GetOBB(boxCollider.size * 0.5f * scale);
        }

        throw new NotImplementedException("The collider type " + collider.GetType() + " is currently not supported.");
      }

      if (_tempColliderList.Count > 1) {
        throw new NotImplementedException("Using more than one collider for GetAuto() is currently not supported.");
      }

      INTERACTION_COMPOUND_DESCRIPTION compoundDesc = new INTERACTION_COMPOUND_DESCRIPTION();
      compoundDesc.shape.type = ShapeType.Compound;
      compoundDesc.nShapes = (uint)_tempColliderList.Count;
      compoundDesc.pShapes = StructAllocator.AllocateArray<IntPtr>(_tempColliderList.Count);
      compoundDesc.pTransforms = new INTERACTION_TRANSFORM[_tempColliderList.Count];

      for (int i = 0; i < _tempColliderList.Count; i++) {
        Collider collider = _tempColliderList[i];

        if (!isUniformScale(collider.transform)) {
          throw new InvalidOperationException("Collider " + collider + " did not have a uniform scale!");
        }

        float globalScale = collider.transform.lossyScale.x;
        Vector3 globalPos;
        Quaternion globalRot;

        IntPtr shapePtr;

        if (collider is SphereCollider) {
          SphereCollider sphereCollider = collider as SphereCollider;
          globalPos = collider.transform.position + collider.transform.TransformPoint(sphereCollider.center);
          globalRot = collider.transform.rotation;

          shapePtr = allocateSphere(sphereCollider.radius * globalScale);
        } else if (collider is BoxCollider) {
          BoxCollider boxCollider = collider as BoxCollider;
          globalPos = collider.transform.position + collider.transform.TransformPoint(boxCollider.center);
          globalRot = collider.transform.rotation;

          shapePtr = allocateObb(boxCollider.size * globalScale * 0.5f);
        } else if (collider is CapsuleCollider) {
          CapsuleCollider capsuleCollider = collider as CapsuleCollider;
          globalPos = collider.transform.position + collider.transform.TransformPoint(capsuleCollider.center);
          globalRot = collider.transform.rotation;

          Vector3 axis;
          switch (capsuleCollider.direction) {
            case 0:
              axis = Vector3.right;
              break;
            case 1:
              axis = Vector3.up;
              break;
            case 2:
              axis = Vector3.forward;
              break;
            default:
              throw new InvalidOperationException("Unexpected direction " + capsuleCollider.direction);
          }

          Vector3 p0 = axis * globalScale * (capsuleCollider.height - capsuleCollider.radius * 0.5f);
          Vector3 p1 = -axis * globalScale * (capsuleCollider.height - capsuleCollider.radius * 0.5f);
          shapePtr = allocateCapsule(p0, p1, capsuleCollider.radius * globalScale);
        } else if (collider is MeshCollider) {
          MeshCollider meshCollider = collider as MeshCollider;
          globalPos = collider.transform.position;
          globalRot = collider.transform.rotation;

          shapePtr = allocateConvex(meshCollider.sharedMesh, globalScale);
        } else {
          throw new InvalidOperationException("Unexpected collider type " + collider.GetType());
        }

        INTERACTION_TRANSFORM ieTransform = new INTERACTION_TRANSFORM();
        ieTransform.position = (parentObject.transform.InverseTransformPoint(globalPos) * parentObject.transform.lossyScale.x).ToCVector();
        ieTransform.rotation = (Quaternion.Inverse(parentObject.transform.rotation) * globalRot).ToCQuaternion();

        StructMarshal<IntPtr>.CopyIntoArray(compoundDesc.pShapes, ref shapePtr, i);
        compoundDesc.pTransforms[i] = ieTransform;
      }

      INTERACTION_SHAPE_DESCRIPTION_HANDLE handle;
      IntPtr compoundPtr = StructAllocator.AllocateStruct(ref compoundDesc);

      InteractionC.AddShapeDescription(ref _scene, compoundPtr, out handle);
      StructAllocator.CleanupAllocations();

      _tempColliderList.Clear();
      _allHandles.Add(handle);

      return handle;
    }

    private bool isUniformScale(Transform transform) {
      while (transform != null) {
        Vector3 localScale = transform.localScale;
        if (!Mathf.Approximately(localScale.x, localScale.y) || !Mathf.Approximately(localScale.x, localScale.z)) {
          return false;
        }
        transform = transform.parent;
      }
      return true;
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

    private IntPtr allocateConvex(Mesh mesh, float scale) {
      INTERACTION_CONVEX_POLYHEDRON_DESCRIPTION meshDesc = new INTERACTION_CONVEX_POLYHEDRON_DESCRIPTION();
      meshDesc.shape.type = ShapeType.Convex;
      meshDesc.radius = 0.0f;
      meshDesc.nVerticies = (uint)mesh.vertexCount;
      meshDesc.pVertices = new LEAP_VECTOR[mesh.vertexCount];
      for (int i = 0; i < mesh.vertexCount; i++) {
        LEAP_VECTOR v = (mesh.vertices[i] * scale).ToCVector();
        meshDesc.pVertices[i] = v;
      }

      IntPtr meshPtr = StructAllocator.AllocateStruct(ref meshDesc);
      return meshPtr;
    }

  }
}
