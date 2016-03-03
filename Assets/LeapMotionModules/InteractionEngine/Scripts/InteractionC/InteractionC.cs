using System;
using System.Runtime.InteropServices;
using LeapInternal;

namespace InteractionEngine.Internal {

  public enum eLeapIERS : uint {
    LeapIERS_Success = 0x00000000,
    eLeapIERS_InvalidArgument = 0x00000001,
    eLeapIERS_ReferencesRemain = 0x00000002,
    eLeapIERS_UnknownError = 0xE2010000
  }

  public enum eLeapIEShapeType : uint {
    eLeapIERS_ShapeSphere,
    eLeapIERS_ShapeBox,
    eLeapIERS_ShapeConvex,
    eLeapIERS_ShapeCompound
  }

  public enum eLeapIEClassification : uint {
    eLeapIEClassification_None,
    eLeapIEClassification_Push,
    eLeapIEClassification_Grab
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_QUATERNION {
    public float w;
    public float x;
    public float y;
    public float z;

    public LEAP_QUATERNION(UnityEngine.Quaternion unity) {
      w = unity.w;
      x = unity.x;
      y = unity.y;
      z = unity.z;
    }

    public UnityEngine.Quaternion ToUnityRotation() {
      return new UnityEngine.Quaternion(x, y, z, w);
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SCENE {
    public IntPtr pData; //LeapIESceneData*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_TRANSFORM {
    public LEAP_VECTOR position;
    public LEAP_QUATERNION rotation;
    public float wallTime;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION {
    public eLeapIEShapeType type;
    public UInt32 flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SPHERE_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_OBB_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public LEAP_VECTOR extents;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public UInt32 nVerticies;
    public IntPtr pVertices; //LEAP_VECTOR*
    public float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_COMPOUND_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public UInt32 nShapes;
    public IntPtr pShapes; //LEAP_IE_SHAPE_DESCRIPTION**
    public IntPtr pTransforms; //LEAP_IE_TRANSFORM* 
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION_HANDLE {
    public UInt32 handle;
    public IntPtr pDEBUG; // LeapIEShapeDescriptionData*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_INSTANCE_HANDLE {
    public UInt32 handle;
    public IntPtr pDEBUG; // LeapIEShapeInstanceData*  will be set to null
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_CLASSIFICATION {
    public eLeapIEClassification classification;
  }

  public class InteractionC {
    public const string DLL_NAME = "LeapInteractionEngine";

    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateScene")]
    public static extern eLeapIERS CreateScene(ref LEAP_IE_SCENE scene);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyScene")]
    public static extern eLeapIERS DestroyScene(ref LEAP_IE_SCENE scene);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateHands")]
    public static extern eLeapIERS UpdateHands(ref LEAP_IE_SCENE scene,
                                               UInt32 nHands,
                                               IntPtr pHands /*LEAP_HAND*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEAddShapeDescription")]
    public static extern eLeapIERS AddShapeDescription(ref LEAP_IE_SCENE scene,
                                                       IntPtr pDescription,
                                                       ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    [DllImport(DLL_NAME, EntryPoint = "LeapIERemoveShapeDescription")]
    public static extern eLeapIERS RemoveShapeDescription(ref LEAP_IE_SCENE scene,
                                                                ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateShape")]
    public static extern eLeapIERS CreateShape(ref LEAP_IE_SCENE scene,
                                                     ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle,
                                                     ref LEAP_IE_TRANSFORM transform,
                                                     ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyShape")]
    public static extern eLeapIERS DestroyShape(ref LEAP_IE_SCENE scene,
                                                      ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateShape")]
    public static extern eLeapIERS UpdateShape(ref LEAP_IE_SCENE scene,
                                                     ref LEAP_IE_TRANSFORM transform,
                                                     ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateController")]
    public static extern eLeapIERS UpdateController(ref LEAP_IE_SCENE scene,
                                                 ref LEAP_IE_TRANSFORM controllerTransform);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetClassification")]
    public static extern eLeapIERS GetClassification(ref LEAP_IE_SCENE scene,
                                                           ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance,
                                                           ref LEAP_IE_SHAPE_CLASSIFICATION classification);
  }
}
