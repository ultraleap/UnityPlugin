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
    private static Dictionary<Type, LeapGraphicTagAttribute> _tagCache = new Dictionary<Type, LeapGraphicTagAttribute>();
    private static Dictionary<string, Type> _stringTypeCache = new Dictionary<string, Type>();

    public readonly string name;
    public readonly int order;

    public LeapGraphicTagAttribute(string name, int order = 0) {
      this.name = name;
      this.order = order;
    }

    public static string GetTagName(Type type) {
      var tag = GetTag(type);
      return tag == null ? type.Name : tag.name;
    }

    public static string GetTagName(string typeName) {
      var tag = GetTag(typeName);
      return tag == null ? typeName : tag.name;
    }

    public static LeapGraphicTagAttribute GetTag(Type type) {
      LeapGraphicTagAttribute tag;
      if (!_tagCache.TryGetValue(type, out tag)) {
        object[] attributes = type.GetCustomAttributes(typeof(LeapGraphicTagAttribute), inherit: true);
        if (attributes.Length == 1) {
          tag = attributes[0] as LeapGraphicTagAttribute;
        }
        _tagCache[type] = tag;
      }

      return tag;
    }

    public static LeapGraphicTagAttribute GetTag(string typeName) {
      Type type;
      if (!_stringTypeCache.TryGetValue(typeName, out type)) {
        type = typeof(LeapGraphicTagAttribute).Assembly.GetTypes().FirstOrDefault(t => t.Name == typeName);
        _stringTypeCache[typeName] = type;
      }

      if (type == null) {
        return null;
      } else {
        return GetTag(type);
      }
    }
  }
}
