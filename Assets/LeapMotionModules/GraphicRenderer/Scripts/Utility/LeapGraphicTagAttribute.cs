/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Linq;
using System.Collections.Generic;

namespace Leap.Unity.GraphicalRenderer {

  [AttributeUsage(AttributeTargets.Class, AllowMultiple = false)]
  public class LeapGraphicTagAttribute : Attribute {
    private static Dictionary<Type, string> _tagNameCache = new Dictionary<Type, string>();
    private static Dictionary<string, Type> _stringTypeCache = new Dictionary<string, Type>();

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

    public static string GetTag(string typeName) {
      Type type;
      if (!_stringTypeCache.TryGetValue(typeName, out type)) {
        type = typeof(LeapGraphicTagAttribute).Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
        _stringTypeCache[typeName] = type;
      }

      if (type == null) {
        return typeName;
      } else {
        return GetTag(type);
      }
    }
  }
}
