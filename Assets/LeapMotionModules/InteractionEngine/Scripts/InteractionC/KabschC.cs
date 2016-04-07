using System;
using System.Runtime.InteropServices;
using LeapInternal;

namespace Leap.Unity.Interaction.CApi {

  [StructLayout(LayoutKind.Sequential)]
  public struct LEAP_IE_KABSCH {
    public IntPtr Implementation;
  }

  public static class KabschC {
    public const string DLL_NAME = "LeapInteractionEngine";

    /*** Construct ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschConstruct")]
    private static extern ReturnStatus LeapIEKabschConstruct(ref LEAP_IE_KABSCH kabsch);

    public static ReturnStatus Construct(ref LEAP_IE_KABSCH kabsch) {
      Logger.Log("Construct", LogLevel.CreateDestroy);
      var rs = LeapIEKabschConstruct(ref kabsch);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Destruct ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschDestruct")]
    private static extern ReturnStatus LeapIEKabschDestruct(ref LEAP_IE_KABSCH kabsch);

    public static ReturnStatus Destruct(ref LEAP_IE_KABSCH kabsch) {
      Logger.Log("Destruct", LogLevel.CreateDestroy);
      var rs = LeapIEKabschDestruct(ref kabsch);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Reset ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschReset")]
    private static extern ReturnStatus LeapIEKabschReset(ref LEAP_IE_KABSCH kabsch);

    public static ReturnStatus Reset(ref LEAP_IE_KABSCH kabsch) {
      Logger.Log("Reset", LogLevel.AllCalls);
      var rs = LeapIEKabschReset(ref kabsch);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Normalize ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschNormalize")]
    private static extern ReturnStatus LeapIEKabschNormalize(ref LEAP_IE_KABSCH kabsch);

    public static ReturnStatus Normalize(ref LEAP_IE_KABSCH kabsch) {
      Logger.Log("Normalize", LogLevel.AllCalls);
      var rs = LeapIEKabschNormalize(ref kabsch);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Add Point ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschAddPoint")]
    private static extern ReturnStatus LeapIEKabschAddPoint(ref LEAP_IE_KABSCH kabsch,
                                                         ref LEAP_VECTOR point1,
                                                         ref LEAP_VECTOR point2,
                                                             float weight);

    public static ReturnStatus AddPoint(ref LEAP_IE_KABSCH kabsch,
                                     ref LEAP_VECTOR point1,
                                     ref LEAP_VECTOR point2,
                                         float weight) {
      Logger.Log("AddPoint", LogLevel.AllCalls);
      var rs = LeapIEKabschAddPoint(ref kabsch, ref point1, ref point2, weight);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Add Normal ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschAddNormal")]
    private static extern ReturnStatus LeapIEKabschAddNormal(ref LEAP_IE_KABSCH kabsch,
                                                          ref LEAP_VECTOR normal1,
                                                          ref LEAP_VECTOR normal2,
                                                              float weight);

    public static ReturnStatus AddNormal(ref LEAP_IE_KABSCH kabsch,
                                      ref LEAP_VECTOR normal1,
                                      ref LEAP_VECTOR normal2,
                                          float weight) {
      Logger.Log("AddNormal", LogLevel.AllCalls);
      var rs = LeapIEKabschAddNormal(ref kabsch, ref normal1, ref normal2, weight);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Solve ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschSolve")]
    private static extern ReturnStatus LeapIEKabschSolve(ref LEAP_IE_KABSCH kabsch);

    public static ReturnStatus Solve(ref LEAP_IE_KABSCH kabsch) {
      Logger.Log("Solve", LogLevel.AllCalls);
      var rs = LeapIEKabschSolve(ref kabsch);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Solve With Pivot ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschSolveWithPivot")]
    private static extern ReturnStatus LeapIEKabschSolveWithPivot(ref LEAP_IE_KABSCH kabsch,
                                                               ref LEAP_VECTOR pivot);

    public static ReturnStatus SolveWithPivot(ref LEAP_IE_KABSCH kabsch,
                                           ref LEAP_VECTOR pivot) {
      Logger.Log("SolveWithPivot", LogLevel.AllCalls);
      var rs = LeapIEKabschSolveWithPivot(ref kabsch, ref pivot);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Solve With Planar ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschSolveWithPlanar")]
    private static extern ReturnStatus LeapIEKabschSolveWithPlanar(ref LEAP_IE_KABSCH kabsch,
                                                                ref LEAP_VECTOR planeNormal);

    public static ReturnStatus SolveWithPlanar(ref LEAP_IE_KABSCH kabsch,
                                            ref LEAP_VECTOR planeNormal) {
      Logger.Log("SolveWithPlanar", LogLevel.AllCalls);
      var rs = LeapIEKabschSolveWithPlanar(ref kabsch, ref planeNormal);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Rotation ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschGetRotation")]
    private static extern ReturnStatus LeapIEKabschGetRotation(ref LEAP_IE_KABSCH kabsch,
                                                            out LEAP_QUATERNION rotation);

    public static ReturnStatus GetRotation(ref LEAP_IE_KABSCH kabsch,
                                        out LEAP_QUATERNION rotation) {
      Logger.Log("GetRotation", LogLevel.AllCalls);
      var rs = LeapIEKabschGetRotation(ref kabsch, out rotation);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Translation ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschGetTranslation")]
    private static extern ReturnStatus LeapIEKabschGetTranslation(ref LEAP_IE_KABSCH kabsch,
                                                               out LEAP_VECTOR translation);

    public static ReturnStatus GetTranslation(ref LEAP_IE_KABSCH kabsch,
                                        out LEAP_VECTOR translation) {
      Logger.Log("GetTranslation", LogLevel.AllCalls);
      var rs = LeapIEKabschGetTranslation(ref kabsch, out translation);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get Translation With Scale ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschGetTranslationWithScale")]
    private static extern ReturnStatus LeapIEKabschGetTranslationWithScale(ref LEAP_IE_KABSCH kabsch,
                                                                        out LEAP_VECTOR translation);

    public static ReturnStatus GetTranslationWithScale(ref LEAP_IE_KABSCH kabsch,
                                                    out LEAP_VECTOR translation) {
      Logger.Log("GetTranslationWithScale", LogLevel.AllCalls);
      var rs = LeapIEKabschGetTranslationWithScale(ref kabsch, out translation);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

    /*** Get  Scale ***/
    [DllImport(DLL_NAME, EntryPoint = "LeapIEKabschGetScale")]
    private static extern ReturnStatus LeapIEKabschGetScale(ref LEAP_IE_KABSCH kabsch,
                                                         out LEAP_VECTOR translation);

    public static ReturnStatus GetScale(ref LEAP_IE_KABSCH kabsch,
                                     out LEAP_VECTOR translation) {
      Logger.Log("GetScale", LogLevel.AllCalls);
      var rs = LeapIEKabschGetScale(ref kabsch, out translation);
      Logger.HandleReturnStatus(rs);
      return rs;
    }

  }
}
