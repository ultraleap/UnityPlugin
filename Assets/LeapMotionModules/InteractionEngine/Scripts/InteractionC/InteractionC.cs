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

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_QUATERNION {
    float w;
    float x;
    float y;
    float z;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IT_SCENE {
    public IntPtr pData; //LeapIESceneData*
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_TRANSFORM {
    LEAP_VECTOR position;
    LEAP_QUATERNION rotation;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION {
    eLeapIEShapeType type;
    UInt32 flags;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SPHERE_DESCRIPTION {
    LEAP_IE_SHAPE_DESCRIPTION shape;
    float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_OBB_DESCRIPTION {
    LEAP_IE_SHAPE_DESCRIPTION shape;
    float extents;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_CONVEX_POLYHEDRON_DESCRIPTION {
    LEAP_IE_SHAPE_DESCRIPTION shape;
    UInt32 nVerticies;
    IntPtr pVertices; //LEAP_VECTOR*
    float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_COMPOUND_DESCRIPTION {
    LEAP_IE_SHAPE_DESCRIPTION shape;
    UInt32 nShapes;
    IntPtr pShapes; //LEAP_IE_SHAPE_DESCRIPTION**
    IntPtr pTransforms; //LEAP_IE_TRANSFORM* 
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION_HANDLE {
    UInt32 handle;
    IntPtr pData; // LeapIEShapeDescriptionData*  will be set to null
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_INSTANCE_HANDLE {
    UInt32 handle;
    IntPtr pData; // LeapIEShapeInstanceData*  will be set to null
  }

  public class InteractionC {

    [DllImport("InteractionC", EntryPoint = "LeapIECreateScene")]
    public static extern eLeapIERS LeapIECreateScene(IntPtr scene /*LEAP_IT_SCENE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIEDestroyScene")]
    public static extern eLeapIERS LeapIEDestroyScene(IntPtr scene /*LEAP_IT_SCENE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIESetHands")]
    public static extern eLeapIERS LeapIESetHands(IntPtr scene /*LEAP_IT_SCENE*/,
                                                  UInt32 nHands,
                                                  IntPtr pHands /*LEAP_HAND*/);

    [DllImport("InteractionC", EntryPoint = "LeapIEAddShapeDescription")]
    public static extern eLeapIERS LeapIEAddShapeDescription(IntPtr scene /*LEAP_IT_SCENE*/,
                                                             IntPtr pDescription /*LEAP_IE_SHAPE_DESCRIPTION*/,
                                                             IntPtr handle /*LEAP_IE_SHAPE_DESCRIPTION_HANDLE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIERemoveShapeDescription")]
    public static extern eLeapIERS LeapIERemoveShapeDescription(IntPtr scene /*LEAP_IT_SCENE*/,
                                                                IntPtr handle /*LEAP_IE_SHAPE_DESCRIPTION_HANDLE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIECreateShape")]
    public static extern eLeapIERS LeapIECreateShape(IntPtr scene /*LEAP_IT_SCENE*/,
                                                     IntPtr handle /*LEAP_IE_SHAPE_DESCRIPTION_HANDLE*/,
                                                     IntPtr transform /*LEAP_IE_TRANSFORM*/,
                                                     IntPtr instance /*LEAP_IE_SHAPE_INSTANCE_HANDLE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIEDestroyShape")]
    public static extern eLeapIERS LeapIEDestroyShape(IntPtr scene /*LEAP_IT_SCENE*/,
                                                      IntPtr instance /*LEAP_IE_SHAPE_INSTANCE_HANDLE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIEUpdateShape")]
    public static extern eLeapIERS LeapIEUpdateShape(IntPtr scene /*LEAP_IT_SCENE*/,
                                                     IntPtr transform /*LEAP_IE_TRANSFORM*/,
                                                     IntPtr instance /*LEAP_IE_SHAPE_INSTANCE_HANDLE*/);

    [DllImport("InteractionC", EntryPoint = "LeapIEAdvance")]
    public static extern eLeapIERS LeapIEAdvance(IntPtr scene /*LEAP_IT_SCENE*/,
                                                 UInt32 milliseconds,
                                                 IntPtr controllerTransform /*LEAP_IE_TRANSFORM*/);


  }
}
