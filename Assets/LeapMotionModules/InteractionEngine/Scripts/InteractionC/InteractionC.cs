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
  public struct LEAP_IT_SCENE {
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
  public struct _LEAP_IE_SHAPE_CLASSIFICATION {
    public eLeapIEClassification classification;
  }

  public class InteractionC {
    public const string DLL_NAME = "InteractionC";

    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateScene")]
    public static extern eLeapIERS LeapIECreateScene(IntPtr scene /*LEAP_IT_SCENE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyScene")]
    public static extern eLeapIERS LeapIEDestroyScene(IntPtr scene /*LEAP_IT_SCENE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIESetHands")]
    public static extern eLeapIERS LeapIESetHands(IntPtr scene /*LEAP_IT_SCENE*/,
                                                  UInt32 nHands,
                                                  IntPtr pHands /*LEAP_HAND*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEAddShapeDescription")]
    public static extern eLeapIERS LeapIEAddShapeDescription(IntPtr scene /*LEAP_IT_SCENE*/,
                                                             IntPtr pDescription /*LEAP_IE_SHAPE_DESCRIPTION*/,
                                                             IntPtr handle /*LEAP_IE_SHAPE_DESCRIPTION_HANDLE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIERemoveShapeDescription")]
    public static extern eLeapIERS LeapIERemoveShapeDescription(IntPtr scene /*LEAP_IT_SCENE*/,
                                                                IntPtr handle /*LEAP_IE_SHAPE_DESCRIPTION_HANDLE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateShape")]
    public static extern eLeapIERS LeapIECreateShape(IntPtr scene /*LEAP_IT_SCENE*/,
                                                     IntPtr handle /*LEAP_IE_SHAPE_DESCRIPTION_HANDLE*/,
                                                     IntPtr transform /*LEAP_IE_TRANSFORM*/,
                                                     IntPtr instance /*LEAP_IE_SHAPE_INSTANCE_HANDLE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyShape")]
    public static extern eLeapIERS LeapIEDestroyShape(IntPtr scene /*LEAP_IT_SCENE*/,
                                                      IntPtr instance /*LEAP_IE_SHAPE_INSTANCE_HANDLE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateShape")]
    public static extern eLeapIERS LeapIEUpdateShape(IntPtr scene /*LEAP_IT_SCENE*/,
                                                     IntPtr transform /*LEAP_IE_TRANSFORM*/,
                                                     IntPtr instance /*LEAP_IE_SHAPE_INSTANCE_HANDLE*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEAdvance")]
    public static extern eLeapIERS LeapIEAdvance(IntPtr scene /*LEAP_IT_SCENE*/,
                                                 IntPtr controllerTransform /*LEAP_IE_TRANSFORM*/);

    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetClassification")]
    public static extern eLeapIERS LeapIEGetClassification(IntPtr scene /*LEAP_IT_SCENE*/,
                                                           IntPtr instance /*controllerTransform*/,
                                                           IntPtr classification /*LEAP_IE_SHAPE_CLASSIFICATION*/);
  }
}
