using System;
using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class LeapGraphicTagAttribute : Attribute {
    private static Dictionary<Type, string> _tagNameCache = new Dictionary<Type, string>();

    public readonly string tagName;

    public LeapGraphicTagAttribute(string tagName) {
      this.tagName = tagName;
    }

    public static string GetTag(Type type) {
      string tagName;
      if (!_tagNameCache.TryGetValue(type, out tagName)) {
        object[] attributes = type.GetCustomAttributes(typeof(LeapGraphicTagAttribute), inherit: true);
        if (attributes.Length == 1) {
          tagName = (attributes[0] as LeapGraphicTagAttribute).tagName;
        } else {
          tagName = type.Name;
        }
        _tagNameCache[type] = tagName;
      }

      return tagName;
    }
  }
}
