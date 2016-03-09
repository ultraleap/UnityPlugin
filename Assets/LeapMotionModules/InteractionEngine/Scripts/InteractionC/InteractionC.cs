using System;
using System.Runtime.InteropServices;
using LeapInternal;

namespace InteractionEngine.Internal {

  public enum eLeapIERS : uint {
    eLeapIERS_Success,
    eLeapIERS_InvalidArgument,
    eLeapIERS_ReferencesRemain,
    eLeapIERS_NotEnabled,
    eLeapIERS_UnknownError = 0x10000000
  }

  public enum eLeapIEShapeType : uint {
    eLeapIEShape_Sphere,
    eLeapIEShape_OBB,
    eLeapIEShape_Convex,
    eLeapIEShape_Compound,
    eLeapIEShape_ForceTo32Bits = 0x10000000
  }

  public enum eLeapIEClassification : uint {
    eLeapIEClassification_None,
    eLeapIEClassification_Push,
    eLeapIEClassification_Grab,
    eLeapIEClassification_ForceTo32Bits = 0x10000000
  }

  public enum eLeapIEDebugFlags {
    eLeapIEDebugFlags_None,
    eLeapIEDebugFlags_LinesInternal = 0x01,
    eLeapIEDebugFlags_RecordToFile = 0x02,
    eLeapIEDebugFlags_RecordToAnalytics = 0x03,
    eLeapIEDebugFlags_ForceTo32Bits = 0x10000000
  };

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
  public struct LEAP_COLOR {
    float r;
    float g;
    float b;
    float a;

    public LEAP_COLOR(UnityEngine.Color color) {
      r = color.r;
      g = color.g;
      b = color.b;
      a = color.a;
    }

    public UnityEngine.Color ToUnityColor() {
      return new UnityEngine.Color(r, g, b, a);
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

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_DEBUG_LINE {
    LEAP_VECTOR start;
    LEAP_VECTOR end;
    LEAP_COLOR color;
    float duration;
    int depthTest;

    public void Draw() {
      UnityEngine.Debug.DrawLine(start.ToUnityVector(),
                                 end.ToUnityVector(),
                                 color.ToUnityColor(),
                                 duration,
                                 depthTest != 0);
    }
  }

  public class InteractionC {
    public const string DLL_NAME = "LeapInteractionEngine";

    /*** Create Scene ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateScene")]
    public static extern eLeapIERS CreateScene(ref LEAP_IE_SCENE scene);

    /*** Destroy Scene ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyScene")]
    public static extern eLeapIERS DestroyScene(ref LEAP_IE_SCENE scene);

    /*** Update Hands ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateHands")]
    public static extern eLeapIERS UpdateHands(ref LEAP_IE_SCENE scene,
                                                   UInt32 nHands,
                                                   IntPtr pHands);

    public static eLeapIERS UpdateHands(ref LEAP_IE_SCENE scene,
                                            Leap.Frame frame) {
      IntPtr handArray = HandArrayBuilder.CreateHandArray(frame);
      eLeapIERS rs = UpdateHands(ref scene, (uint)frame.Hands.Count, handArray);
      StructAllocator.CleanupAllocations();
      return rs;
    }

    /*** Add Shape Description ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEAddShapeDescription")]
    public static extern eLeapIERS AddShapeDescription(ref LEAP_IE_SCENE scene,
                                                           IntPtr pDescription,
                                                       out LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    /*** Remove Shape Description ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIERemoveShapeDescription")]
    public static extern eLeapIERS RemoveShapeDescription(ref LEAP_IE_SCENE scene,
                                                          ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    /*** Create Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateShape")]
    public static extern eLeapIERS CreateShape(ref LEAP_IE_SCENE scene,
                                               ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle,
                                               ref LEAP_IE_TRANSFORM transform,
                                               out LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    /*** Destroy Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyShape")]
    public static extern eLeapIERS DestroyShape(ref LEAP_IE_SCENE scene,
                                                ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    /*** Update Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateShape")]
    public static extern eLeapIERS UpdateShape(ref LEAP_IE_SCENE scene,
                                               ref LEAP_IE_TRANSFORM transform,
                                               ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    /*** Update Controller ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateController")]
    public static extern eLeapIERS UpdateController(ref LEAP_IE_SCENE scene,
                                                    ref LEAP_IE_TRANSFORM controllerTransform);

    /*** Annotate ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEAnnotate")]
    public static extern eLeapIERS Annotate(ref LEAP_IE_SCENE scene,
                                            ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance,
                                                UInt32 type,
                                                UInt32 bytes,
                                                IntPtr data);

    /*** Get Classification ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetClassification")]
    public static extern eLeapIERS GetClassification(ref LEAP_IE_SCENE scene,
                                                     ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance,
                                                     out LEAP_IE_SHAPE_CLASSIFICATION classification);

    /*** Enable Debug Visualization ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEEnableDebugVisualization")]
    public static extern eLeapIERS EnableDebugVisualization(ref LEAP_IE_SCENE scene,
                                                                UInt32 flags);

    /*** Get Debug Lines ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetDebugLines")]
    public static extern eLeapIERS GetDebugLines(ref LEAP_IE_SCENE scene,
                                                 out UInt32 nLines,
                                                 out IntPtr ppLineBuffer);

    public static void DrawDebugLines(ref LEAP_IE_SCENE scene) {
      UInt32 lines;
      IntPtr arrayPtr;
      GetDebugLines(ref scene, out lines, out arrayPtr);

      for (int i = 0; i < lines; i++) {
        IntPtr linePtr = StructMarshal<IntPtr>.ArrayElementToStruct(arrayPtr, i);
        LEAP_IE_DEBUG_LINE line = StructMarshal<LEAP_IE_DEBUG_LINE>.PtrToStruct(linePtr);
        line.Draw();
      }
    }

  }
}
