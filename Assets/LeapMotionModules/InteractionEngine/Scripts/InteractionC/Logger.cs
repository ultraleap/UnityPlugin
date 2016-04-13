#define ENABLE_LOGGING
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
    public static LogLevel logLevel = LogLevel.Warning;

    [Conditional("ENABLE_LOGGING")]
    public static void HandleReturnStatus(INTERACTION_SCENE scene, string methodName, LogLevel methodLevel, ReturnStatus rs) {
      string extraText = InteractionC.GetLastErrorString(ref scene);
      handleReturnStatusInternal(methodName, methodLevel, rs, extraText);
    }

    [Conditional("ENABLE_LOGGING")]
    public static void HandleReturnStatus(string methodName, LogLevel methodLevel, ReturnStatus rs) {
      handleReturnStatusInternal(methodName, methodLevel, rs, null);
    }

    public static void handleReturnStatusInternal(string methodName, LogLevel methodLevel, ReturnStatus rs, string extraErrorText) {
      string message;
      LogLevel level;
      rs.GetInfo(out message, out level);

      LogLevel maxLevel = (LogLevel)Math.Max((int)level, (int)methodLevel);

      if (maxLevel >= logLevel) {
        string totalMessage = methodName + " returned " + message;

        if (maxLevel == LogLevel.Error) {
          if (!string.IsNullOrEmpty(extraErrorText)) {
            totalMessage += "\n";
            totalMessage += extraErrorText;
          }

          throw new Exception(totalMessage);
        }

        if (maxLevel == LogLevel.Warning) {
          UnityEngine.Debug.LogWarning(totalMessage);
        } else {
          UnityEngine.Debug.Log(totalMessage);
        }
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

    public static void GetInfo(this ReturnStatus rs, out string message, out LogLevel logLevel) {
      switch (rs) {
        case ReturnStatus.Success:
          message = "Success";
          logLevel = LogLevel.Verbose;
          return;
        case ReturnStatus.InvalidHandle:
          message = "Invalid Handle";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.InvalidArgument:
          message = "Invalid Argument";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.ReferencesRemain:
          message = "References Remain";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.NotEnabled:
          message = "Not Enabled";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.NeverUpdated:
          message = "Never Updated";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.UnknownError:
          message = "Unknown Error";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.BadData:
          message = "Bad Data";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.StoppedOnNonDeterministic:
          message = "Stopped on Non Deterministic";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.StoppedOnUnexpectedFailure:
          message = "Stopped on Unexpected Failure";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.StoppedOnFull:
          message = "Stopped on Full";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.StoppedFileError:
          message = "Stopped on File Error";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.UnexpectedEOF:
          message = "Unexpected End Of File";
          logLevel = LogLevel.Error;
          return;
        case ReturnStatus.Paused:
          message = "Paused";
          logLevel = LogLevel.Verbose;
          return;
        default:
          throw new ArgumentException("Unexpected return status " + rs);
      }
    }
  }
}
