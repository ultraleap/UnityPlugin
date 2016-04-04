using System;
using System.Diagnostics;

namespace Leap.Unity.Interaction.CApi {

  public enum LogLevel {
    Verbose,
    AllCalls,
    CreateDestroy,
    Info,
    Warning,
    Error
  }

  public static class Logger {
    public static LogLevel logLevel = LogLevel.Info;

    [Conditional("ENABLE_LOGGING")]
    public static void HandleReturnStatus(eLeapIERS rs) {
      string message;
      LogLevel logLevel;
      rs.GetInfo(out message, out logLevel);

      if (logLevel == LogLevel.Error) {
        throw new Exception(message);
      }

      Log(message, logLevel);
    }

    public static void GetInfo(this eLeapIERS rs, out string message, out LogLevel logLevel) {
      switch (rs) {
        case eLeapIERS.eLeapIERS_Success:
          message = "Success";
          logLevel = LogLevel.Verbose;
          return;
        case eLeapIERS.eLeapIERS_InvalidHandle:
          message = "Invalid Handle";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_InvalidArgument:
          message = "Invalid Argument";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_ReferencesRemain:
          message = "References Remain";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_NotEnabled:
          message = "Not Enabled";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_NeverUpdated:
          message = "Never Updated";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_UnknownError:
          message = "Unknown Error";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_BadData:
          message = "Bad Data";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_StoppedOnNonDeterministic:
          message = "Stopped on Non Deterministic";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_StoppedOnUnexpectedFailure:
          message = "Stopped on Unexpected Failure";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_StoppedOnFull:
          message = "Stopped on Full";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_StoppedFileError:
          message = "Stopped on File Error";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_UnexpectedEOF:
          message = "Unexpected End Of File";
          logLevel = LogLevel.Error;
          return;
        case eLeapIERS.eLeapIERS_Paused:
          message = "Paused";
          logLevel = LogLevel.Verbose;
          return;
        default:
          throw new ArgumentException("Unexpected return status " + rs);
      }
    }

    [Conditional("ENABLE_LOGGING")]
    public static void Log(string message, LogLevel level) {
      if (level >= logLevel) {
        if (level == LogLevel.Error) {
          UnityEngine.Debug.LogError(message);
        } else if (level == LogLevel.Warning) {
          UnityEngine.Debug.LogWarning(message);
        } else {
          UnityEngine.Debug.Log(message);
        }
      }
    }
  }
}
