/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using System;
using System.Reflection;
using System.Linq.Expressions;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace Leap.Unity.Recording {
  using Query;

#if UNITY_EDITOR

  /// <summary>
  /// This struct mimics the function of AnimationUtility.GetFloatValue.
  /// It trades construct time for access time, once the accessor is 
  /// constructed, access is roughly 25 times faster compared to GetFloatValue.
  /// </summary>
  public struct PropertyAccessor {
    private static MethodInfo GetFloatMethod;
    private static MethodInfo GetColorMethod;
    private static MethodInfo GetVectorMethod;

    static PropertyAccessor() {
      Type[] intArr = new Type[] { typeof(int) };
      GetFloatMethod = typeof(Material).GetMethod("GetFloat", intArr);
      GetColorMethod = typeof(Material).GetMethod("GetColor", intArr);
      GetVectorMethod = typeof(Material).GetMethod("GetVector", intArr);
    }

    private Func<float> _accessor;

    /// <summary>
    /// Constructs a new PropertyAccessor that can access a single float value
    /// specified by the root GameObject and a specific EditorCurveBinding.
    /// 
    /// If the optional failureIsZero param is set to true, any errors during construction
    /// will result in an accessor that always returns zero instead of throwing any errors.
    /// </summary>
    public PropertyAccessor(GameObject root, EditorCurveBinding binding, bool failureIsZero = false) {
      try {
        //First get the target object the binding points to
        GameObject target = getTargetObject(root, binding);
        if (target == null) {
          throw new InvalidOperationException("Target object cannot be null");
        }

        //Get the specific component the binding points to
        Component component = target.GetComponent(binding.type);
        if (component == null) {
          throw new InvalidOperationException("Could not find a component of type " + binding.type + " on object " + target.name);
        }

        //Then get an Expression that represents accessing the bound value
        Expression propertyExpr;
        {
          string[] propertyPath = binding.propertyName.Split('.');

          //Material properties require special logic
          if (propertyPath[0] == "material" && component is Renderer) {
            propertyExpr = buildMaterialExpression(component, propertyPath);
          } else {
            propertyExpr = buildFieldExpression(component, propertyPath, binding);
          }
        }

        //Compile the expression into a lambda we can execute
        _accessor = Expression.Lambda<Func<float>>(propertyExpr).Compile();
      } catch (Exception e) {
        if (failureIsZero) {
          //If we get any errors, catch them so that recording can continue
          //But error loudly and default to a curve of zero
          Debug.LogError("Exception when trying to construct PropertyAccessor, curves will be incorrect.");
          Debug.LogException(e);
          _accessor = () => 0;
        } else {
          throw e;
        }
      }
    }

    /// <summary>
    /// Access the current value of the animation property.
    /// </summary>
    public float Access() {
      return _accessor();
    }

    /// <summary>
    /// Given a root object and a curve binding, return the actual target GameObject that the 
    /// binding points to.
    /// </summary>
    private static GameObject getTargetObject(GameObject root, EditorCurveBinding binding) {
      string[] names = binding.path.Split('/');
      GameObject target = root;

      foreach (var name in names) {
        for (int i = 0; i < target.transform.childCount; i++) {
          var child = target.transform.GetChild(i);
          if (child.gameObject.name == name) {
            target = child.gameObject;
            break;
          }
        }
      }

      return target;
    }

    /// <summary>
    /// Returns a new expression that represents the access of a single float value taken from
    /// a material used on the renderer component, specified by the given property path.
    /// </summary>
    private static Expression buildMaterialExpression(Component component, string[] propertyPath) {
      //We are only _reading_ values so no need to instantiate a new material
      Material material = (component as Renderer).sharedMaterial;
      if (material == null) {
        throw new InvalidOperationException("Could not record property because material was null");
      }

      Shader shader = material.shader;
      if (shader == null) {
        throw new InvalidOperationException("Could not record property because shader was null");
      }

      //material is the first element in the property path, 
      //the second (index 1) element is the name of the shader property we are accessing
      string propertyName = propertyPath[1];

      //Search for the type of the property in question
      ShaderUtil.ShaderPropertyType? propertyType = null;
      int shaderPropCount = ShaderUtil.GetPropertyCount(shader);
      for (int i = 0; i < shaderPropCount; i++) {
        if (ShaderUtil.GetPropertyName(shader, i) == propertyName) {
          propertyType = ShaderUtil.GetPropertyType(shader, i);
          break;
        }
      }

      if (!propertyType.HasValue) {
        throw new InvalidOperationException("Could not find property " + propertyName + " in shader " + shader);
      }

      //We convert the property name to id for faster access
      var idExpr = Expression.Constant(Shader.PropertyToID(propertyName));
      var matExpr = Expression.Property(Expression.Constant(component), "sharedMaterial");

      //Based on the type of the property, we build an expression that invokes the correct
      //method GetFloat for float types, GetColor for color types, ect....
      Expression propertyExpr;
      switch (propertyType.Value) {
        case ShaderUtil.ShaderPropertyType.Float:
        case ShaderUtil.ShaderPropertyType.Range:
          propertyExpr = Expression.Call(matExpr, GetFloatMethod, idExpr);
          break;
        case ShaderUtil.ShaderPropertyType.Color:
          propertyExpr = Expression.Call(matExpr, GetColorMethod, idExpr);
          break;
        case ShaderUtil.ShaderPropertyType.Vector:
          propertyExpr = Expression.Call(matExpr, GetVectorMethod, idExpr);
          break;
        default:
          throw new NotImplementedException("Can not handle property type " + propertyType.Value);
      }

      //The value we accessed might be a struct with additional fields we need to dive into
      //0    = 'material'
      //1    = name of shader property
      //2... = fields of the accessed property
      for (int i = 2; i < propertyPath.Length; i++) {
        propertyExpr = Expression.PropertyOrField(propertyExpr, propertyPath[i]);
      }

      return propertyExpr;
    }

    /// <summary>
    /// Returns a new expression that represents the access of a single float value taken from
    /// a field somewhere in the given component, specified by the given property path.
    /// </summary>
    private static Expression buildFieldExpression(Component component, string[] propertyPath, EditorCurveBinding binding) {
      //First build an expression that accesses the base field value
      //We get an aproximate field/property name because many engine types don't have property names
      //that actually correspond to real c# properties we can access
      Expression fieldExpr = Expression.PropertyOrField(Expression.Constant(component),
                                                        getClosestPropertyName(binding.type, propertyPath[0]));

      //For the remainder of the property path, we access additional fields located inside the value
      for (int i = 1; i < propertyPath.Length; i++) {
        fieldExpr = Expression.PropertyOrField(fieldExpr, propertyPath[i]);
      }

      return fieldExpr;
    }

    /// <summary>
    /// For a given type, and a given name, find an actual field/property name that is as close 
    /// as possible to the given name.  
    ///  - If an exact match is possible, that is used first
    ///  - If the given name has a 2char prefix removed and can match, that is used second
    ///  - If the given name contains any substring of a field, that is used last
    /// </summary>
    private static string getClosestPropertyName(Type type, string bindingProperty) {
      //We only care about instance members, but they can be public or non-public
      BindingFlags flags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic;

      //Properties and fields can both be categorized as MemberInfo
      var members = type.GetFields(flags).
                         Query().
                         Cast<MemberInfo>().
                         Concat(type.GetProperties(flags).
                                     Query().
                                     Cast<MemberInfo>()).
                                     ToList();

      {
        MemberInfo exactMatch = members.Query().FirstOrDefault(f => f.Name == bindingProperty);
        if (exactMatch != null) {
          return exactMatch.Name;
        }
      }

      {
        MemberInfo minusPrefix = members.Query().FirstOrDefault(f => bindingProperty.Substring(2).ToLower() == f.Name.ToLower());
        if (minusPrefix != null) {
          return minusPrefix.Name;
        }
      }

      {
        MemberInfo containsMatch = members.Query().FirstOrDefault(f => bindingProperty.ToLower().Contains(f.Name.ToLower()));
        if (containsMatch != null) {
          return containsMatch.Name;
        }
      }

      throw new InvalidOperationException("Cannot find a field or property with a name close to " + bindingProperty + " for type " + type);
    }
  }
#endif
}
