using System;
using System.Collections.Generic;
using UnityEngine;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
public class LeapGuiTagAttribute : Attribute {
  private static Dictionary<Type, string> _tagNameCache = new Dictionary<Type, string>();

  public readonly string tagName;

  public LeapGuiTagAttribute(string tagName) {
    this.tagName = tagName;
  }

  public static string GetTag(Type type) {
    string tagName;
    if (!_tagNameCache.TryGetValue(type, out tagName)) {
      object[] attributes = type.GetCustomAttributes(typeof(LeapGuiTagAttribute), inherit: true);
      if (attributes.Length == 1) {
        tagName = (attributes[0] as LeapGuiTagAttribute).tagName;
      } else {
        tagName = type.Name;
      }
      _tagNameCache[type] = tagName;
    }

    return tagName;
  }
}
