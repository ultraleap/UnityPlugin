#define ENABLE_LOGGING
using UnityEngine;
using System;
using System.Diagnostics;
using System.Runtime.InteropServices;
using LeapInternal;

namespace Leap.Unity.Interaction.CApi {

  public enum eLeapIERS : uint {
    eLeapIERS_Success,
    eLeapIERS_InvalidHandle,
    eLeapIERS_InvalidArgument,
    eLeapIERS_ReferencesRemain,
    eLeapIERS_NotEnabled,
    eLeapIERS_NeverUpdated,
    eLeapIERS_UnknownError,
    eLeapIERS_BadData,

    eLeapIERS_StoppedOnNonDeterministic,
    eLeapIERS_StoppedOnUnexpectedFailure,
    eLeapIERS_StoppedOnFull,
    eLeapIERS_StoppedFileError,
    eLeapIERS_UnexpectedEOF,
    eLeapIERS_Paused
  }

  public enum eLeapIEShapeType : uint {
    eLeapIEShape_Sphere,
    eLeapIEShape_OBB,
    eLeapIEShape_Convex,
    eLeapIEShape_Compound
  }

  public enum eLeapIEClassification : uint {
    eLeapIEClassification_Physics,
    eLeapIEClassification_Grasp,
    eLeapIEClassification_MAX
  }

  public enum eLeapIEDebugFlags : uint {
    eLeapIEDebugFlags_None,
    eLeapIEDebugFlags_Lines = 0x01
  };

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_QUATERNION {
    public float w;
    public float x;
    public float y;
    public float z;

    public LEAP_QUATERNION(Quaternion unity) {
      w = unity.w;
      x = unity.x;
      y = unity.y;
      z = unity.z;
    }

    public Quaternion ToUnityRotation() {
      return new Quaternion(x, y, z, w);
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_COLOR {
    float r;
    float g;
    float b;
    float a;

    public LEAP_COLOR(Color color) {
      r = color.r;
      g = color.g;
      b = color.b;
      a = color.a;
    }

    public Color ToUnityColor() {
      return new Color(r, g, b, a);
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
    public LEAP_VECTOR[] pVertices;
    public float radius;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_COMPOUND_DESCRIPTION {
    public LEAP_IE_SHAPE_DESCRIPTION shape;
    public UInt32 nShapes;
    public IntPtr pShapes; //LEAP_IE_SHAPE_DESCRIPTION**
    public LEAP_IE_TRANSFORM[] pTransforms;
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_DESCRIPTION_HANDLE : IEquatable<LEAP_IE_SHAPE_DESCRIPTION_HANDLE> {
    public UInt32 handle;

    public bool Equals(LEAP_IE_SHAPE_DESCRIPTION_HANDLE other) {
      return handle == other.handle;
    }

    public override int GetHashCode() {
      return (int)handle;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_SHAPE_INSTANCE_HANDLE : IEquatable<LEAP_IE_SHAPE_INSTANCE_HANDLE> {
    public UInt32 handle;

    public bool Equals(LEAP_IE_SHAPE_INSTANCE_HANDLE other) {
      return handle == other.handle;
    }

    public override int GetHashCode() {
      return (int)handle;
    }
  }

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_HAND_CLASSIFICATION {
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

  [StructLayout(LayoutKind.Sequential, Pack = 1)]
  public struct LEAP_IE_VELOCITY
  {
    public LEAP_IE_SHAPE_INSTANCE_HANDLE handle;
    public LEAP_VECTOR linearVelocity;
    public LEAP_VECTOR angularVelocity;
  }

  public class InteractionC {
    public const string DLL_NAME = "LeapInteractionEngine";

    /*** Create Scene ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateScene")]
    private static extern eLeapIERS LeapIECreateScene(ref LEAP_IE_SCENE scene);

    public static void CreateScene(ref LEAP_IE_SCENE scene) {
      Logger.Log("Create Scene", LogLevel.Info);
      var rs = LeapIECreateScene(ref scene);
      Logger.HandleReturnStatus(rs);
    }

    /*** Destroy Scene ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyScene")]
    private static extern eLeapIERS LeapIEDestroyScene(ref LEAP_IE_SCENE scene);

    public static eLeapIERS DestroyScene(ref LEAP_IE_SCENE scene) {
      Logger.Log("Destroy Scene", LogLevel.Info);
      var rs = LeapIEDestroyScene(ref scene);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Last Error ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetLastError")]
    public static extern eLeapIERS GetLastError(ref LEAP_IE_SCENE scene);

    /*** Add Shape Description ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEAddShapeDescription")]
    private static extern eLeapIERS LeapIEAddShapeDescription(ref LEAP_IE_SCENE scene,
                                                                  IntPtr pDescription,
                                                              out LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    public static eLeapIERS AddShapeDescription(ref LEAP_IE_SCENE scene,
                                                    IntPtr pDescription,
                                                out LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle) {
      Logger.Log("Add Shape Description", LogLevel.CreateDestroy);
      var rs = LeapIEAddShapeDescription(ref scene, pDescription, out handle);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Remove Shape Description ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIERemoveShapeDescription")]
    private static extern eLeapIERS LeapIERemoveShapeDescription(ref LEAP_IE_SCENE scene,
                                                                 ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle);

    public static eLeapIERS RemoveShapeDescription(ref LEAP_IE_SCENE scene,
                                                   ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle) {
      Logger.Log("Remove Shape Description", LogLevel.CreateDestroy);
      var rs = LeapIERemoveShapeDescription(ref scene, ref handle);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Create Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIECreateShape")]
    private static extern eLeapIERS LeapIECreateShape(ref LEAP_IE_SCENE scene,
                                                      ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle,
                                                      ref LEAP_IE_TRANSFORM transform,
                                                      out LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static eLeapIERS CreateShape(ref LEAP_IE_SCENE scene,
                                        ref LEAP_IE_SHAPE_DESCRIPTION_HANDLE handle,
                                        ref LEAP_IE_TRANSFORM transform,
                                        out LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Logger.Log("Create Shape", LogLevel.CreateDestroy);
      var rs = LeapIECreateShape(ref scene, ref handle, ref transform, out instance);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Destroy Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEDestroyShape")]
    private static extern eLeapIERS LeapIEDestroyShape(ref LEAP_IE_SCENE scene,
                                                       ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static eLeapIERS DestroyShape(ref LEAP_IE_SCENE scene,
                                         ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Logger.Log("Destroy Shape", LogLevel.CreateDestroy);
      var rs = LeapIEDestroyShape(ref scene, ref instance);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Update Shape ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateShape")]
    private static extern eLeapIERS LeapIEUpdateShape(ref LEAP_IE_SCENE scene,
                                                      ref LEAP_IE_TRANSFORM transform,
                                                      ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static eLeapIERS UpdateShape(ref LEAP_IE_SCENE scene,
                                        ref LEAP_IE_TRANSFORM transform,
                                        ref LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Logger.Log("Update Shape", LogLevel.AllCalls);
      var rs = LeapIEUpdateShape(ref scene, ref transform, ref instance);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Update Hands ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateHands")]
    private static extern eLeapIERS LeapIEUpdateHands(ref LEAP_IE_SCENE scene,
                                                          UInt32 nHands,
                                                          IntPtr pHands);

    public static eLeapIERS UpdateHands(ref LEAP_IE_SCENE scene,
                                            UInt32 nHands,
                                            IntPtr pHands) {
      Logger.Log("Update Hands", LogLevel.AllCalls);
      var rs = LeapIEUpdateHands(ref scene, nHands, pHands);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Update Controller ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEUpdateController")]
    private static extern eLeapIERS LeapIEUpdateController(ref LEAP_IE_SCENE scene,
                                                           ref LEAP_IE_TRANSFORM controllerTransform);

    public static eLeapIERS UpdateController(ref LEAP_IE_SCENE scene,
                                             ref LEAP_IE_TRANSFORM controllerTransform) {
      Logger.Log("Update Controller", LogLevel.AllCalls);
      var rs = LeapIEUpdateController(ref scene, ref controllerTransform);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Classification ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetClassification")]
    private static extern eLeapIERS LeapIEGetClassification(ref LEAP_IE_SCENE scene,
                                                                UInt32 handId,
                                                            out LEAP_IE_HAND_CLASSIFICATION classification,
                                                            out LEAP_IE_SHAPE_INSTANCE_HANDLE instance);

    public static eLeapIERS GetClassification(ref LEAP_IE_SCENE scene,
                                                  UInt32 handId,
                                              out LEAP_IE_HAND_CLASSIFICATION classification,
                                              out LEAP_IE_SHAPE_INSTANCE_HANDLE instance) {
      Logger.Log("Get Classification", LogLevel.AllCalls);
      var rs = LeapIEGetClassification(ref scene, handId, out classification, out instance);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Velocities ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetVelocities")]
    private static extern eLeapIERS LeapIEGetVelocities(ref LEAP_IE_SCENE scene,
                                                        out UInt32 nVelocities,
                                                        out IntPtr ppVelocitiesBuffer);

    public static eLeapIERS GetVelocities(ref LEAP_IE_SCENE scene, out LEAP_IE_VELOCITY[] velocities)
    {
      Logger.Log("Get Velocities", LogLevel.AllCalls);

      UInt32 nVelocities;
      IntPtr ppVelocitiesBuffer;
      var rs = LeapIEGetVelocities(ref scene, out nVelocities, out ppVelocitiesBuffer);
      Logger.HandleReturnStatus(rs);
      if (rs != eLeapIERS.eLeapIERS_Success || nVelocities == 0)
      {
        velocities = null;
        return rs;
      }

      velocities = new LEAP_IE_VELOCITY[nVelocities];
      for (int i = 0; i < nVelocities; i++)
      {
        velocities[i] = StructMarshal<LEAP_IE_VELOCITY>.ArrayElementToStruct(ppVelocitiesBuffer, i);
      }
      return rs;
    }

    /*** Enable Debug Visualization ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEEnableDebugFlags")]
    private static extern eLeapIERS LeapIEEnableDebugFlags(ref LEAP_IE_SCENE scene,
                                                               UInt32 flags);

    public static eLeapIERS EnableDebugFlags(ref LEAP_IE_SCENE scene,
                                                 UInt32 flags) {
      Logger.Log("Enable Debug Flags", LogLevel.Info);
      var rs = LeapIEEnableDebugFlags(ref scene, flags);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Debug Lines ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEGetDebugLines")]
    private static extern eLeapIERS LeapIEGetDebugLines(ref LEAP_IE_SCENE scene,
                                                        out UInt32 nLines,
                                                        out IntPtr ppLineBuffer);

    public static eLeapIERS GetDebugLines(ref LEAP_IE_SCENE scene,
                                          out UInt32 nLines,
                                          out IntPtr ppLineBuffer) {
      Logger.Log("Get Debug Lines", LogLevel.AllCalls);
      var rs = LeapIEGetDebugLines(ref scene, out nLines, out ppLineBuffer);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    public static void DrawDebugLines(ref LEAP_IE_SCENE scene) {
      UInt32 lines;
      IntPtr arrayPtr;
      GetDebugLines(ref scene, out lines, out arrayPtr);

      for (int i = 0; i < lines; i++) {
        LEAP_IE_DEBUG_LINE line = StructMarshal<LEAP_IE_DEBUG_LINE>.ArrayElementToStruct(arrayPtr, i);
        line.Draw();
      }
    }

  }
}
