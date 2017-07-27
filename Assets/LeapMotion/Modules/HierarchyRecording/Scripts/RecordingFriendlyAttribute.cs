using System;

namespace Leap.Unity.Recording {

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
  public class RecordingFriendlyAttribute : Attribute {

    public static bool IsRecordingFriendly(object obj) {
      return obj.GetType().GetCustomAttributes(typeof(RecordingFriendlyAttribute), inherit: true).Length > 0;
    }
  }
}
