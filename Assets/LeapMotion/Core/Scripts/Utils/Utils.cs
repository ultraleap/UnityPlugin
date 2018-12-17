/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using UnityEngine.Assertions;
using System;
using System.IO;
using System.Collections.Generic;
using Leap.Unity.Query;

namespace Leap.Unity {

  public static class Utils {

    #region C# Utilities

    #region Generic Utils

    /// <summary>
    /// Swaps the references of a and b.  Note that you can pass
    /// in references to array elements if you want!
    /// </summary>
    public static void Swap<T>(ref T a, ref T b) {
      T temp = a;
      a = b;
      b = temp;
    }

    /// <summary>
    /// Utility extension to swap the elements at index a and index b.
    /// </summary>
    public static void Swap<T>(this IList<T> list, int a, int b) {
      T temp = list[a];
      list[a] = list[b];
      list[b] = temp;
    }

    /// <summary>
    /// Utility extension to swap the elements at index a and index b.
    /// </summary>
    public static void Swap<T>(this T[] array, int a, int b) {
      Swap(ref array[a], ref array[b]);
    }

    /// <summary>
    /// System.Array.Reverse is actually surprisingly complex / slow.  This
    /// is a basic generic implementation of the reverse algorithm.
    /// </summary>
    public static void Reverse<T>(this T[] array) {
      int mid = array.Length / 2;
      int i = 0;
      int j = array.Length;
      while (i < mid) {
        array.Swap(i++, --j);
      }
    }

    /// <summary>
    /// System.Array.Reverse is actually surprisingly complex / slow.  This
    /// is a basic generic implementation of the reverse algorithm.
    /// </summary>
    public static void Reverse<T>(this T[] array, int start, int length) {
      int mid = start + length / 2;
      int i = start;
      int j = start + length;
      while (i < mid) {
        array.Swap(i++, --j);
      }
    }

    /// <summary>
    /// Shuffle the given list into a different permutation.
    /// </summary>
    public static void Shuffle<T>(this IList<T> list) {
      for (int i = 0; i < list.Count; i++) {
        Utils.Swap(list, i, UnityEngine.Random.Range(i, list.Count));
      }
    }

    public static void DoubleCapacity<T>(ref T[] array) {
      T[] newArray = new T[array.Length * 2];
      Array.Copy(array, newArray, array.Length);
      array = newArray;
    }

    /// <summary>
    /// Returns whether or not two lists contain the same elements ignoring order.
    /// </summary>
    public static bool AreEqualUnordered<T>(IList<T> a, IList<T> b) {
      var _count = Pool<Dictionary<T, int>>.Spawn();
      try {
        int _nullCount = 0;

        foreach (var i in a) {
          if (i == null) {
            _nullCount++;
          } else {
            int count;
            if (!_count.TryGetValue(i, out count)) {
              count = 0;
            }
            _count[i] = count + 1;
          }
        }

        foreach (var i in b) {
          if (i == null) {
            _nullCount--;
          } else {
            int count;
            if (!_count.TryGetValue(i, out count)) {
              return false;
            }
            _count[i] = count - 1;
          }
        }

        if (_nullCount != 0) {
          return false;
        }

        foreach (var pair in _count) {
          if (pair.Value != 0) {
            return false;
          }
        }

        return true;
      } finally {
        _count.Clear();
        Pool<Dictionary<T, int>>.Recycle(_count);
      }
    }

    // http://stackoverflow.com/a/19317229/2471635
    /// <summary>
    /// Returns whether this type implements the argument interface type.
    /// If the argument type is not an interface, returns false.
    /// </summary>
    public static bool ImplementsInterface(this Type type, Type ifaceType) {
      Type[] intf = type.GetInterfaces();
      for (int i = 0; i < intf.Length; i++) {
        if (intf[i] == ifaceType) {
          return true;
        }
      }
      return false;
    }

    public static bool IsActiveRelativeToParent(this Transform obj, Transform parent) {
      Assert.IsTrue(obj.IsChildOf(parent));

      if (!obj.gameObject.activeSelf) {
        return false;
      } else {
        if (obj.parent == null || obj.parent == parent) {
          return true;
        } else {
          return obj.parent.IsActiveRelativeToParent(parent);
        }
      }
    }

    /// <summary>
    /// Given a list of comparable types, return an ordering that orders the
    /// elements into sorted order.  The ordering is a list of indices where each
    /// index refers to the element located at that index in the original list.
    /// </summary>
    public static List<int> GetSortedOrder<T>(this IList<T> list) where T : IComparable<T> {
      Assert.IsNotNull(list);

      List<int> ordering = new List<int>();
      for (int i = 0; i < list.Count; i++) {
        ordering.Add(i);
      }

      ordering.Sort((a, b) => list[a].CompareTo(list[b]));

      return ordering;
    }

    /// <summary>
    /// Given a list and an ordering, order the list according to the ordering.
    /// This method assumes the ordering is a valid ordering.
    /// </summary>
    public static void ApplyOrdering<T>(this IList<T> list, List<int> ordering) {
      Assert.IsNotNull(list);
      Assert.IsNotNull(ordering);
      Assert.AreEqual(list.Count, ordering.Count, "List must be the same length as the ordering.");

      List<T> copy = Pool<List<T>>.Spawn();
      try {
        copy.AddRange(list);
        for (int i = 0; i < list.Count; i++) {
          list[i] = copy[ordering[i]];
        }
      } finally {
        copy.Clear();
        Pool<List<T>>.Recycle(copy);
      }
    }

    public static string MakeRelativePath(string relativeTo, string path) {
      if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentNullException("relativeTo");
      if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

      Uri relativeToUri = new Uri(relativeTo);
      Uri pathUri = new Uri(path);

      if (relativeToUri.Scheme != pathUri.Scheme) { return path; } // path can't be made relative.

      Uri relativeUri = relativeToUri.MakeRelativeUri(pathUri);
      string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

      if (pathUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase)) {
        relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
      }

      return relativePath;
    }

    #endregion

    #region String Utils
    /// <summary>
    /// Trims a specific number of characters off of the end of the
    /// provided string.  When the number of trimmed characters is
    /// equal to or greater than the length of the string, the empty
    /// string is always returned.
    /// </summary>
    public static string TrimEnd(this string str, int characters) {
      return str.Substring(0, Mathf.Max(0, str.Length - characters));
    }

    /// <summary>
    /// Trims a specific number of characters off of the beginning of
    /// the provided string.  When the number of trimmed characters is
    /// equal to or greater than the length of the string, the empty
    /// string is always returned.
    /// </summary>
    public static string TrimStart(this string str, int characters) {
      return str.Substring(Mathf.Min(str.Length, characters));
    }

    /// <summary>
    /// Capitalizes a simple string.  Only looks at the first character,
    /// so if your string has any kind of non-letter character as the first
    /// character this method will do nothing.
    /// </summary>
    public static string Capitalize(this string str) {
      char c = str[0];
      if (char.IsLetter(c)) {
        return char.ToUpper(c) + str.Substring(1);
      } else {
        return str;
      }
    }

    /// <summary>
    /// Takes a variable-like name and turns it into a nice human readable
    /// name.  Examples:
    /// 
    /// _privateVar     =>  Private Var
    /// multBy32        =>  Mult By 32
    /// the_key_code    =>  The Key Code
    /// CamelCaseToo    =>  Camel Case Too
    /// _is2_equalTo_5  =>  Is 2 Equal To 5
    /// GetTheSCUBANow  =>  Get The SCUBA Now
    /// m_privateVar    =>  Private Var
    /// kConstantVar    =>  Constant Var
    /// </summary>
    public static string GenerateNiceName(string value) {
      string result = "";
      string curr = "";

      Func<char, bool> wordFunc = c => {
        //Can't build any further if it's already capitalized
        if (curr.Length > 0 && char.IsUpper(curr[0])) {
          return false;
        }

        //Can't add non-letters to words
        if (!char.IsLetter(c)) {
          return false;
        }

        curr = c + curr;
        return true;
      };

      Func<char, bool> acronymFunc = c => {
        //Can't add non-letters to acronyms
        if (!char.IsLetter(c)) {
          return false;
        }

        //Can't add lowercase letters to acronyms
        if (char.IsLower(c)) {
          return false;
        }

        curr = c + curr;
        return true;
      };

      Func<char, bool> numberFunc = c => {
        //Can't add non-digits to a number
        if (!char.IsDigit(c)) {
          return false;
        }

        curr = c + curr;
        return true;
      };

      Func<char, bool> fluffFunc = c => {
        //Can't add digits or numbers to 'fluff'
        if (char.IsDigit(c) || char.IsLetter(c)) {
          return false;
        }

        return true;
      };


      Func<char, bool> currFunc = null;
      int currIndex = value.Length;

      while (currIndex != 0) {
        currIndex--;
        char c = value[currIndex];

        if (currFunc != null && currFunc(c)) {
          continue;
        }

        if (curr != "") {
          result = " " + curr.Capitalize() + result;
          curr = "";
        }

        if (acronymFunc(c)) {
          currFunc = acronymFunc;
        } else if (wordFunc(c)) {
          currFunc = wordFunc;
        } else if (numberFunc(c)) {
          currFunc = numberFunc;
        } else if (fluffFunc(c)) {
          currFunc = fluffFunc;
        } else {
          throw new Exception("Unexpected state, no function matched character " + c);
        }
      }

      if (curr != "") {
        result = curr.Capitalize() + result;
      }

      result = result.Trim();

      if (result.StartsWith("M ") || result.StartsWith("K ")) {
        result = result.Substring(2);
      }

      return result.Trim();
    }
    #endregion

    #region Print Utils

    /// <summary>
    /// Prints the elements of an array in a bracket-enclosed, comma-delimited list,
    /// prefixed by the elements' type.
    /// </summary>
    public static string ToArrayString<T>(this IEnumerable<T> enumerable) {
      var str = "[" + typeof(T).Name + ": ";
      bool addedFirstElement = false;
      foreach (var t in enumerable) {
        if (addedFirstElement) {
          str += ", ";
        }
        str += t.ToString();

        addedFirstElement = true;
      }
      str += "]";

      return str;
    }

    #endregion

    #region Math Utils

    public static int Repeat(int x, int m) {
      int r = x % m;
      return r < 0 ? r + m : r;
    }

    public static int Sign(int value) {
      if (value == 0) {
        return 0;
      } else if (value > 0) {
        return 1;
      } else {
        return -1;
      }
    }

    /// <summary>
    /// Returns a vector that is perpendicular to this vector.
    /// The returned vector will have the same length as the
    /// input vector.
    /// </summary>
    public static Vector2 Perpendicular(this Vector2 vector) {
      return new Vector2(vector.y, -vector.x);
    }

    /// <summary>
    /// Returns a vector that is perpendicular to this vector.
    /// The returned vector is not guaranteed to be a unit vector,
    /// nor is its length guaranteed to be the same as the source
    /// vector's.
    /// </summary>
    public static Vector3 Perpendicular(this Vector3 vector) {
      float x2 = vector.x * vector.x;
      float y2 = vector.y * vector.y;
      float z2 = vector.z * vector.z;

      float mag0 = z2 + x2;
      float mag1 = y2 + x2;
      float mag2 = z2 + y2;

      if (mag0 > mag1) {
        if (mag0 > mag2) {
          return new Vector3(-vector.z, 0, vector.x);
        } else {
          return new Vector3(0, vector.z, -vector.y);
        }
      } else {
        if (mag1 > mag2) {
          return new Vector3(vector.y, -vector.x, 0);
        } else {
          return new Vector3(0, vector.z, -vector.y);
        }
      }
    }

    public static bool ContainsNaN(this Vector3 v) {
      return float.IsNaN(v.x)
          || float.IsNaN(v.y)
          || float.IsNaN(v.z);
    }

    public static bool IsBetween(this float f, float f0, float f1) {
      if (f0 > f1) Utils.Swap(ref f0, ref f1);

      return f0 <= f && f <= f1;
    }

    public static bool IsBetween(this double d, double d0, double d1) {
      if (d0 > d1) Utils.Swap(ref d0, ref d1);

      return d0 <= d && d <= d1;
    }

    /// <summary>
    /// Extrapolates using time values for positions a and b at extrapolatedTime.
    /// </summary>
    public static Vector3 TimedExtrapolate(Vector3 a, float aTime,
                                           Vector3 b, float bTime,
                                           float extrapolatedTime) {
      return Vector3.LerpUnclamped(a, b, extrapolatedTime.MapUnclamped(aTime, bTime, 0f, 1f));
    }

    /// <summary>
    /// Extrapolates using time values for rotations a and b at extrapolatedTime.
    /// </summary>
    public static Quaternion TimedExtrapolate(Quaternion a, float aTime,
                                              Quaternion b, float bTime,
                                              float extrapolatedTime) {
      return Quaternion.SlerpUnclamped(a, b, extrapolatedTime.MapUnclamped(aTime, bTime, 0f, 1f));
    }

    /// <summary>
    /// A specification of the generic NextTuple method that only works for integers ranging
    /// from 0 inclusive to maxValue exclusive.
    /// </summary>
    public static bool NextTuple(IList<int> tuple, int maxValue) {
      return NextTuple(tuple, i => (i + 1) % maxValue);
    }

    /// <summary>
    /// Given one tuple of a collection of possible tuples, mutate it into the next tuple in the 
    /// in the lexicographic sequence, or into the first tuple if the last tuple has been reached.
    /// 
    /// The items of the tuple must be comparable to each other.  The getNext function takes an 
    /// item and returns the next item in the lexicographic sequence, or the first item if there
    /// is no next item.
    /// </summary>
    /// <returns>
    /// Returns true if the new tuple comes after the input tuple, false otherwise.
    /// </returns>
    public static bool NextTuple<T>(IList<T> tuple, Func<T, T> nextItem) where T : IComparable<T> {
      int index = tuple.Count - 1;
      while (index >= 0) {
        T value = tuple[index];
        T newValue = nextItem(value);
        tuple[index] = newValue;

        if (newValue.CompareTo(value) > 0) {
          return true;
        }

        index--;
      }

      return false;
    }

    #endregion

    #region Array Utils

    /// <summary>
    /// Sets all elements in the array of type T to default(T).
    /// </summary>
    public static T[] ClearWithDefaults<T>(this T[] arr) {
      for (int i = 0; i < arr.Length; i++) {
        arr[i] = default(T);
      }
      return arr;
    }

    /// <summary>
    /// Sets all elements in the array of type T to the argument value.
    /// </summary>
    public static T[] ClearWith<T>(this T[] arr, T value) {
      for (int i = 0; i < arr.Length; i++) {
        arr[i] = value;
      }
      return arr;
    }

    #endregion

    #region List Utils

    public static void EnsureListExists<T>(ref List<T> list) {
      if (list == null) {
        list = new List<T>();
      }
    }

    public static void EnsureListCount<T>(this List<T> list, int count) {
      if (list.Count == count) return;

      while (list.Count < count) {
        list.Add(default(T));
      }

      while (list.Count > count) {
        list.RemoveAt(list.Count - 1);
      }
    }

    public static void EnsureListCount<T>(this List<T> list, int count, Func<T> createT, Action<T> deleteT = null) {
      while (list.Count < count) {
        list.Add(createT());
      }

      while (list.Count > count) {
        T tempT = list[list.Count - 1];
        list.RemoveAt(list.Count - 1);

        if (deleteT != null) {
          deleteT(tempT);
        }
      }
    }

    /// <summary>
    /// Adds t0, then t1 to this list.
    /// </summary>
    public static void Add<T>(this List<T> list, T t0, T t1) {
      list.Add(t0);
      list.Add(t1);
    }

    /// <summary>
    /// Adds t0, t1, then t2 to this list.
    /// </summary>
    public static void Add<T>(this List<T> list, T t0, T t1, T t2) {
      list.Add(t0);
      list.Add(t1);
      list.Add(t2);
    }

    /// <summary>
    /// Adds t0, t1, t2, then t3 to this list.
    /// </summary>
    public static void Add<T>(this List<T> list, T t0, T t1, T t2, T t3) {
      list.Add(t0);
      list.Add(t1);
      list.Add(t2);
      list.Add(t3);
    }

    #endregion

    #endregion

    #region Unity Utilities

    #region Unity Object Utils

    /// <summary>
    /// Usage is the same as FindObjectOfType, but this method will also return objects
    /// that are inactive.
    /// 
    /// Use this method to search for singleton-pattern objects even if they are disabled,
    /// but be warned that it's not cheap to call!
    /// </summary>
    public static T FindObjectInHierarchy<T>() where T : UnityEngine.Object {
      return Resources.FindObjectsOfTypeAll<T>().Query()
        .Where(o => {
#if UNITY_EDITOR
          // Exclude prefabs.
          var prefabType = UnityEditor.PrefabUtility.GetPrefabType(o);
          if (prefabType == UnityEditor.PrefabType.ModelPrefab
          || prefabType == UnityEditor.PrefabType.Prefab) {
            return false;
          }
#endif
          return true;
        })
        .FirstOrDefault();
    }

    #endregion

    #region Transform Utils

    /// <summary>
    /// Returns the children of this Transform in sibling index order.
    /// </summary>
    public static ChildrenEnumerator GetChildren(this Transform t) {
      return new ChildrenEnumerator(t);
    }

    public struct ChildrenEnumerator : IEnumerator<Transform> {
      private Transform _t;
      private int _idx;
      private int _count;

      public ChildrenEnumerator(Transform t) {
        _t = t;
        _idx = -1;
        _count = t.childCount;
      }

      public ChildrenEnumerator GetEnumerator() { return this; }

      public bool MoveNext() {
        if (_idx < _count) _idx += 1;
        if (_idx == _count) { return false; } else { return true; }
      }
      public Transform Current {
        get { return _t == null ? null : _t.GetChild(_idx); }
      }
      object System.Collections.IEnumerator.Current { get { return Current; } }
      public void Reset() {
        _idx = -1;
        _count = _t.childCount;
      }
      public void Dispose() { }
    }

    /// <summary>
    /// Sets the localPosition, localRotation, and localScale to their default values:
    /// Vector3.zero, Quaternion.identity, and Vector3.one.
    /// </summary>
    public static void ResetLocalTransform(this Transform t) {
      t.localPosition = Vector3.zero;
      t.localRotation = Quaternion.identity;
      t.localScale = Vector3.one;
    }

    /// <summary>
    /// Sets the localPosition and localRotation of this Transform to Vector3.zero and
    /// Quaternion.identity. Doesn't affect localScale.
    /// </summary>
    public static void ResetLocalPose(this Transform t) {
      t.localPosition = Vector3.zero;
      t.localRotation = Quaternion.identity;
    }

    #endregion

    #region Component Utils

    /// <summary>
    /// Recursively searches the hierarchy of the argument Transform to find all of the
    /// Components of type ComponentType (the first type argument) that should be "owned"
    /// by the OwnerType component type (the second type argument).
    /// 
    /// If a child GameObject itself has an OwnerType component, that
    /// child is ignored, and its children are ignored -- the assumption being that such
    /// a child owns itself and any ComponentType components beneath it.
    /// 
    /// For example, a call to FindOwnedChildComponents with ComponentType Collider and
    /// OwnerType Rigidbody would return all of the Colliders that are attached to the
    /// rootObj Rigidbody, but none of the colliders that are attached to a rootObj's
    /// child's own Rigidbody.
    /// 
    /// Optionally, ComponentType components of inactive GameObjects can be included
    /// in the returned list; by default, these components are skipped.
    /// 
    /// This is not a cheap method to call, but it does not allocate garbage, so it is safe
    /// for use at runtime.
    /// </summary>
    /// 
    /// <typeparam name="ComponentType">
    /// The component type to search for.
    /// </typeparam>
    /// 
    /// <typeparam name="OwnerType">
    /// The component type that assumes ownership of any ComponentType in its own Transform
    /// or its Transform's children/grandchildren.
    /// </typeparam>
    public static void FindOwnedChildComponents<ComponentType, OwnerType>
                                               (OwnerType rootObj,
                                                List<ComponentType> ownedComponents,
                                                bool includeInactiveObjects = false)
                                               where OwnerType : Component {
      ownedComponents.Clear();
      Stack<Transform> toVisit = Pool<Stack<Transform>>.Spawn();
      List<ComponentType> componentsBuffer = Pool<List<ComponentType>>.Spawn();

      try {
        toVisit.Push(rootObj.transform);
        Transform curTransform;
        while (toVisit.Count > 0) {
          curTransform = toVisit.Pop();

          // Recursively search children and children's children.
          foreach (var child in curTransform.GetChildren()) {
            // Ignore children with OwnerType components of their own; its own OwnerType
            // component owns its own ComponentType components and the ComponentType
            // components of its children.
            if (child.GetComponent<OwnerType>() == null
                && (includeInactiveObjects || child.gameObject.activeInHierarchy)) {
              toVisit.Push(child);
            }
          }

          // Since we'll visit every valid child, all we need to do is add the
          // ComponentType components of every transform we visit.
          componentsBuffer.Clear();
          curTransform.GetComponents<ComponentType>(componentsBuffer);
          foreach (var component in componentsBuffer) {
            ownedComponents.Add(component);
          }
        }
      } finally {
        toVisit.Clear();
        Pool<Stack<Transform>>.Recycle(toVisit);

        componentsBuffer.Clear();
        Pool<List<ComponentType>>.Recycle(componentsBuffer);
      }
    }

    #endregion

    #region Orientation Utils

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Useful for billboarding Quads and UI elements whose forward vectors should match
    /// rather than oppose the Main Camera's forward vector.
    /// 
    /// Optionally, you may also pass an upwards vector, which will be provided to the underlying
    /// Quaternion.LookRotation. Vector3.up will be used by default.
    /// </summary>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, Vector3.up);
    }

    /// <summary>
    /// Similar to Unity's Transform.LookAt(), but resolves the forward vector of this
    /// Transform to point away from the argument Transform.
    /// 
    /// Allows specifying an upwards parameter; this is passed as the upwards vector to the Quaternion.LookRotation.
    /// </summary>
    /// <param name="thisTransform"></param>
    /// <param name="transform"></param>
    public static void LookAwayFrom(this Transform thisTransform, Transform transform, Vector3 upwards) {
      thisTransform.rotation = Quaternion.LookRotation(thisTransform.position - transform.position, upwards);
    }

    #endregion

    #region Vector3 Utils

    /// <summary>
    /// Returns a Vector3 containing the X, Y, and Z components of this Vector4. Note
    /// that an implicit conversion exists from Vector4 to Vector3 already, so this
    /// extension method is only useful if you specifically want an explicit conversion.
    /// </summary>
    public static Vector3 ToVector3(this Vector4 v4) {
      return new Vector3(v4.x, v4.y, v4.z);
    }

    /// <summary>
    /// Returns this vector converted from world space to the local space of the argument
    /// Transform.
    /// </summary>
    public static Vector3 InLocalSpace(this Vector3 v, Transform t) {
      return t.InverseTransformPoint(v);
    }

    

    #endregion

    #region Quaternion Utils

    public static bool ContainsNaN(this Quaternion q) {
      return float.IsNaN(q.x)
          || float.IsNaN(q.y)
          || float.IsNaN(q.z)
          || float.IsNaN(q.w);
    }

    /// <summary>
    /// Converts the quaternion into an axis and an angle and returns the vector
    /// axis * angle. Angle magnitude is measured in degrees, not radians; this requires
    /// conversion to radians if being used to set the angular velocity of a PhysX
    /// Rigidbody.
    /// </summary>
    public static Vector3 ToAngleAxisVector(this Quaternion q) {
      float angle;
      Vector3 axis;
      q.ToAngleAxis(out angle, out axis);
      return axis * angle;
    }

    /// <summary>
    /// Returns a Quaternion described by the provided angle axis vector. Expects the
    /// magnitude (angle) to be in degrees, not radians.
    /// </summary>
    public static Quaternion QuaternionFromAngleAxisVector(Vector3 angleAxisVector) {
      if (angleAxisVector == Vector3.zero) return Quaternion.identity;
      return Quaternion.AngleAxis(angleAxisVector.magnitude, angleAxisVector);
    }

    /// <summary>
    /// Returns a normalized Quaternion from the input quaternion. If the input
    /// quaternion is zero-length (AKA the default Quaternion), the identity Quaternion
    /// is returned.
    /// </summary>
    public static Quaternion ToNormalized(this Quaternion quaternion) {
      float x = quaternion.x, y = quaternion.y, z = quaternion.z, w = quaternion.w;
      float magnitude = Mathf.Sqrt(x * x + y * y + z * z + w * w);

      if (Mathf.Approximately(magnitude, 0f)) {
        return Quaternion.identity;
      }

      return new Quaternion(x / magnitude, y / magnitude, z / magnitude, w / magnitude);
    }

    /// <summary>
    /// Returns the rotation that makes a transform at fromPosition point its forward
    /// vector at targetPosition and keep its rightward vector parallel with the horizon
    /// defined by a normal of Vector3.up.
    /// 
    /// For example, this will point an interface panel at a user camera while
    /// maintaining the alignment of text and other elements with the horizon line.
    /// </summary>
    /// <returns></returns>
    public static Quaternion FaceTargetWithoutTwist(Vector3 fromPosition,
                                                    Vector3 targetPosition,
                                                    bool flip180 = false) {
      return FaceTargetWithoutTwist(fromPosition, targetPosition, Vector3.up, flip180);
    }

    /// <summary>
    /// Returns the rotation that makes a transform at fromPosition point its forward
    /// vector at targetPosition and keep its rightward vector parallel with the horizon
    /// defined by the upwardDirection normal.
    /// 
    /// For example, this will point an interface panel at a user camera while
    /// maintaining the alignment of text and other elements with the horizon line.
    /// </summary>
    public static Quaternion FaceTargetWithoutTwist(Vector3 fromPosition,
                                                    Vector3 targetPosition,
                                                    Vector3 upwardDirection,
                                                    bool flip180 = false) {
      Vector3 objToTarget = targetPosition - fromPosition;
      return Quaternion.LookRotation((flip180 ? -1 : 1) * objToTarget,
                                     upwardDirection);
    }

    public static Quaternion Flipped(this Quaternion q) {
      return new Quaternion(-q.x, -q.y, -q.z, -q.w);
    }

    #region Compression

    /// <summary>
    /// Fills the provided bytes buffer starting at the offset with a compressed form
    /// of the argument quaternion. The offset is also shifted by 4 bytes.
    /// 
    /// Use Utils.DecompressBytesToQuat to decode this representation. This encoding ONLY
    /// works with normalized Quaternions, taking advantage of the fact that their
    /// components sum to 1 to only encode three of Quaternion components. As a result,
    /// this method encodes a Quaternion as a single unsigned integer (4 bytes).
    /// 
    /// Sources:
    /// https://bitbucket.org/Unity-Technologies/networking/pull-requests/9/quaternion-compression-for-sending/diff
    /// and
    /// http://stackoverflow.com/questions/3393717/c-sharp-converting-uint-to-byte
    /// </summary>
    public static void CompressQuatToBytes(Quaternion quat,
                                             byte[] buffer,
                                             ref int offset) {
      int largest = 0;
      float a, b, c;

      float abs_w = Mathf.Abs(quat.w);
      float abs_x = Mathf.Abs(quat.x);
      float abs_y = Mathf.Abs(quat.y);
      float abs_z = Mathf.Abs(quat.z);

      float largest_value = abs_x;

      if (abs_y > largest_value) {
        largest = 1;
        largest_value = abs_y;
      }
      if (abs_z > largest_value) {
        largest = 2;
        largest_value = abs_z;
      }
      if (abs_w > largest_value) {
        largest = 3;
        largest_value = abs_w;
      }
      if (quat[largest] >= 0f) {
        a = quat[(largest + 1) % 4];
        b = quat[(largest + 2) % 4];
        c = quat[(largest + 3) % 4];
      }
      else {
        a = -quat[(largest + 1) % 4];
        b = -quat[(largest + 2) % 4];
        c = -quat[(largest + 3) % 4];
      }

      // serialize
      const float minimum = -1.0f / 1.414214f;        // note: 1.0f / sqrt(2)
      const float maximum = +1.0f / 1.414214f;
      const float delta = maximum - minimum;
      const uint maxIntegerValue = (1 << 10) - 1; // 10 bits
      const float maxIntegerValueF = (float)maxIntegerValue;
      float normalizedValue;
      uint integerValue;

      uint sentData = ((uint)largest) << 30;
      // a
      normalizedValue = Mathf.Clamp01((a - minimum) / delta);
      integerValue = (uint)Mathf.Floor(normalizedValue * maxIntegerValueF + 0.5f);
      sentData = sentData | ((integerValue & maxIntegerValue) << 20);
      // b
      normalizedValue = Mathf.Clamp01((b - minimum) / delta);
      integerValue = (uint)Mathf.Floor(normalizedValue * maxIntegerValueF + 0.5f);
      sentData = sentData | ((integerValue & maxIntegerValue) << 10);
      // c
      normalizedValue = Mathf.Clamp01((c - minimum) / delta);
      integerValue = (uint)Mathf.Floor(normalizedValue * maxIntegerValueF + 0.5f);
      sentData = sentData | (integerValue & maxIntegerValue);

      BitConverterNonAlloc.GetBytes(sentData, buffer, ref offset);
    }

    /// <summary>
    /// Reads 4 bytes from the argument bytes array (starting at the provided offset) and
    /// returns a Quaternion as encoded by the Utils.CompressedQuatToBytes function. Also
    /// increments the provided offset by 4.
    /// 
    /// See the Utils.CompressedQuatToBytes documentation for more details on the
    /// byte representation this method expects.
    /// 
    /// Sources:
    /// https://bitbucket.org/Unity-Technologies/networking/pull-requests/9/quaternion-compression-for-sending/diff
    /// and
    /// http://stackoverflow.com/questions/3393717/c-sharp-converting-uint-to-byte
    /// </summary>
    public static Quaternion DecompressBytesToQuat(byte[] bytes, ref int offset) {
      uint readData = BitConverterNonAlloc.ToUInt32(bytes, ref offset);

      int largest = (int)(readData >> 30);
      float a, b, c;

      const float minimum = -1.0f / 1.414214f;        // note: 1.0f / sqrt(2)
      const float maximum = +1.0f / 1.414214f;
      const float delta = maximum - minimum;
      const uint maxIntegerValue = (1 << 10) - 1; // 10 bits
      const float maxIntegerValueF = (float)maxIntegerValue;
      uint integerValue;
      float normalizedValue;
      // a
      integerValue = (readData >> 20) & maxIntegerValue;
      normalizedValue = (float)integerValue / maxIntegerValueF;
      a = (normalizedValue * delta) + minimum;
      // b
      integerValue = (readData >> 10) & maxIntegerValue;
      normalizedValue = (float)integerValue / maxIntegerValueF;
      b = (normalizedValue * delta) + minimum;
      // c
      integerValue = readData & maxIntegerValue;
      normalizedValue = (float)integerValue / maxIntegerValueF;
      c = (normalizedValue * delta) + minimum;

      Quaternion value = Quaternion.identity;
      float d = Mathf.Sqrt(1f - a * a - b * b - c * c);
      value[largest] = d;
      value[(largest + 1) % 4] = a;
      value[(largest + 2) % 4] = b;
      value[(largest + 3) % 4] = c;

      return value;
    }

    #endregion

    #endregion

    #region Matrix4x4 Utils

    public static Matrix4x4 CompMul(Matrix4x4 m, float f) {
#if UNITY_2017_1_OR_NEWER
      return new Matrix4x4(m.GetColumn(0) * f,
                           m.GetColumn(1) * f,
                           m.GetColumn(2) * f,
                           m.GetColumn(3) * f);
#else
      Matrix4x4 toReturn = m;
      for (int i = 0; i < 4; i++) {
        toReturn.SetColumn(i, toReturn.GetColumn(i) * f);
      }
      return toReturn;
#endif

    }

    #endregion

    #region Physics Utils

    /// <summary>
    /// Calls Physics.IgnoreCollision for each Collider in the first GameObject against
    /// each Collider in the second GameObject.
    /// 
    /// If you have many colliders that need to ignore collisions, consider utilizing
    /// Layer collision settings as an optimization.
    /// </summary>
    public static void IgnoreCollisions(GameObject first, GameObject second,
                                        bool ignore = true) {
      if (first == null || second == null)
        return;

      var firstColliders = Pool<List<Collider>>.Spawn(); firstColliders.Clear();
      var secondColliders = Pool<List<Collider>>.Spawn(); secondColliders.Clear();
      try {
        first.GetComponentsInChildren(firstColliders);
        second.GetComponentsInChildren(secondColliders);

        for (int i = 0; i < firstColliders.Count; ++i) {
          for (int j = 0; j < secondColliders.Count; ++j) {
            if (firstColliders[i] != secondColliders[j] &&
                firstColliders[i].enabled && secondColliders[j].enabled) {
              Physics.IgnoreCollision(firstColliders[i], secondColliders[j], ignore);
            }
          }
        }
      }
      finally {
        firstColliders.Clear(); Pool<List<Collider>>.Recycle(firstColliders);
        secondColliders.Clear(); Pool<List<Collider>>.Recycle(secondColliders);
      }
    }

    #endregion

    #region Collider Utils

    #region Capsule Collider Utils

    public static Vector3 GetDirection(this CapsuleCollider capsule) {
      switch (capsule.direction) {
        case 0: return Vector3.right;
        case 1: return Vector3.up;
        case 2: default: return Vector3.forward;
      }
    }

    public static float GetEffectiveRadius(this CapsuleCollider capsule) {
      return capsule.radius * capsule.GetEffectiveRadiusMultiplier();
    }

    public static float GetEffectiveRadiusMultiplier(this CapsuleCollider capsule) {
      var effRadiusMult = 0f;
      switch (capsule.direction) {
        case 0:
          effRadiusMult = Swizzle.Swizzle.yz(capsule.transform.lossyScale).CompMax();
          break;
        case 1:
          effRadiusMult = Swizzle.Swizzle.xz(capsule.transform.lossyScale).CompMax();
          break;
        case 2:
        default:
          effRadiusMult = Swizzle.Swizzle.xy(capsule.transform.lossyScale).CompMax();
          break;
      }
      return effRadiusMult;
    }

    public static void GetCapsulePoints(this CapsuleCollider capsule, out Vector3 a,
                                                                      out Vector3 b) {
      var effRadiusMult = capsule.GetEffectiveRadiusMultiplier();
      var capsuleDir = capsule.GetDirection();

      a = capsuleDir * (capsule.height / 2f);
      b = -a;

      a = capsule.transform.TransformPoint(a);
      b = capsule.transform.TransformPoint(b);

      a -= capsuleDir * effRadiusMult * capsule.radius;
      b += capsuleDir * effRadiusMult * capsule.radius;
    }

    /// <summary>
    /// Manipulates capsule.transform.position, capsule.transform.rotation, and capsule.height
    /// so that the line segment defined by the capsule connects world-space points a and b.
    /// </summary>
    public static void SetCapsulePoints(this CapsuleCollider capsule, Vector3 a, Vector3 b) {
      capsule.center = Vector3.zero;

      capsule.transform.position = (a + b) / 2F;

      Vector3 capsuleDirection = capsule.GetDirection();

      Vector3 capsuleDirWorldSpace = capsule.transform.TransformDirection(capsuleDirection);
      Quaternion necessaryRotation = Quaternion.FromToRotation(capsuleDirWorldSpace, a - capsule.transform.position);
      capsule.transform.rotation = necessaryRotation * capsule.transform.rotation;

      Vector3 aCapsuleSpace = capsule.transform.InverseTransformPoint(a);
      float capsuleSpaceDistToA = aCapsuleSpace.magnitude;
      capsule.height = (capsuleSpaceDistToA + capsule.radius) * 2;
    }

    #endregion

    /// <summary>
    /// Recursively searches the hierarchy of the argument GameObject to find all of the
    /// Colliders that are attached to the object's Rigidbody (or that _would_ be
    /// attached to its Rigidbody if it doesn't have one) and adds them to the provided
    /// colliders list. Warning: The provided "colliders" List will be cleared before
    /// use.
    ///
    /// Colliders that are the children of other Rigidbody elements beneath the argument
    /// object are ignored. Optionally, colliders of inactive GameObjects can be included
    /// in the returned list; by default, these colliders are skipped.
    /// </summary>
    public static void FindColliders<T>(GameObject obj, List<T> colliders,
                                        bool includeInactiveObjects = false)
                                    where T : Collider {
      colliders.Clear();
      Stack<Transform> toVisit = Pool<Stack<Transform>>.Spawn();
      List<T> collidersBuffer = Pool<List<T>>.Spawn();

      try {
        // Traverse the hierarchy of this object's transform to find
        // all of its Colliders.
        toVisit.Push(obj.transform);
        Transform curTransform;
        while (toVisit.Count > 0) {
          curTransform = toVisit.Pop();

          // Recursively search children and children's children
          foreach (var child in curTransform.GetChildren()) {
            // Ignore children with Rigidbodies of their own; its own Rigidbody
            // owns its own colliders and the colliders of its children
            if (child.GetComponent<Rigidbody>() == null
                && (includeInactiveObjects || child.gameObject.activeSelf)) {
              toVisit.Push(child);
            }
          }

          // Since we'll visit every valid child, all we need to do is add the colliders
          // of every transform we visit.
          collidersBuffer.Clear();
          curTransform.GetComponents<T>(collidersBuffer);
          foreach (var collider in collidersBuffer) {
            colliders.Add(collider);
          }
        }
      } finally {
        toVisit.Clear();
        Pool<Stack<Transform>>.Recycle(toVisit);

        collidersBuffer.Clear();
        Pool<List<T>>.Recycle(collidersBuffer);
      }
    }

    #endregion

    #region Color Utils

    public static Color WithAlpha(this Color color, float alpha) {
      return new Color(color.r, color.g, color.b, alpha);
    }

    /// <summary>
    /// Just like ColorUtility.TryParseHtmlString but throws a useful
    /// error message if it fails.
    /// </summary>
    public static Color ParseHtmlColorString(string htmlString) {
      Color color;
      if (!ColorUtility.TryParseHtmlString(htmlString, out color)) {
        throw new ArgumentException("The string [" + htmlString + "] is not a valid color code.  Valid color codes include:\n" +
                                    "#RGB\n" +
                                    "#RGBA\n" +
                                    "#RRGGBB\n" +
                                    "#RRGGBBAA\n" +
                                    "For more information, see the documentation for ColorUtility.TryParseHtmlString.");
      }

      return color;
    }

    /// <summary>
    /// Lerps this color towards the argument color in HSV space and returns the lerped
    /// color.
    /// </summary>
    public static Color LerpHSV(this Color color, Color towardsColor, float t) {
      float h0, s0, v0;
      Color.RGBToHSV(color, out h0, out s0, out v0);

      float h1, s1, v1;
      Color.RGBToHSV(towardsColor, out h1, out s1, out v1);

      // Cyclically lerp hue. (Input hues are always between 0 and 1.)
      if (h0 - h1 < -0.5f) h0 += 1f;
      if (h0 - h1 > 0.5f) h1 += 1f;
      float hL = Mathf.Lerp(h0, h1, t) % 1f;

      float sL = Mathf.Lerp(s0, s1, t);
      float vL = Mathf.Lerp(v0, v1, t);
      return Color.HSVToRGB(hL, sL, vL);
    }

    /// <summary>
    /// Cyclically lerps hue arguments by t.
    /// </summary>
    public static float LerpHue(float h0, float h1, float t) {
      // Enforce hue values between 0f and 1f.
      if (h0 < 0f) h0 = 1f - (-h0 % 1f);
      if (h1 < 0f) h1 = 1f - (-h1 % 1f);
      if (h0 > 1f) h0 = h0 % 1f;
      if (h1 > 1f) h1 = h1 % 1f;

      if (h0 - h1 < -0.5f) h0 += 1f;
      if (h0 - h1 > 0.5f) h1 += 1f;
      return Mathf.Lerp(h0, h1, t) % 1f;
    }

    #endregion

    #region Gizmo Utils

    public static void DrawCircle(Vector3 center,
                                  Vector3 normal,
                                  float radius,
                                  Color color,
                                  int quality = 32,
                                  float duration = 0,
                                  bool depthTest = true) {
      Vector3 planeA = Vector3.Slerp(normal, -normal, 0.5f);
      DrawArc(360, center, planeA, normal, radius, color, quality);
    }

    /* Adapted from: Zarrax (http://math.stackexchange.com/users/3035/zarrax), Parametric Equation of a Circle in 3D Space?, 
     * URL (version: 2014-09-09): http://math.stackexchange.com/q/73242 */
    public static void DrawArc(float arc,
                               Vector3 center,
                               Vector3 forward,
                               Vector3 normal,
                               float radius,
                               Color color,
                               int quality = 32) {

      Gizmos.color = color;
      Vector3 right = Vector3.Cross(normal, forward).normalized;
      float deltaAngle = arc / quality;
      Vector3 thisPoint = center + forward * radius;
      Vector3 nextPoint = new Vector3();
      for (float angle = 0; Mathf.Abs(angle) <= Mathf.Abs(arc); angle += deltaAngle) {
        float cosAngle = Mathf.Cos(angle * Constants.DEG_TO_RAD);
        float sinAngle = Mathf.Sin(angle * Constants.DEG_TO_RAD);
        nextPoint.x = center.x + radius * (cosAngle * forward.x + sinAngle * right.x);
        nextPoint.y = center.y + radius * (cosAngle * forward.y + sinAngle * right.y);
        nextPoint.z = center.z + radius * (cosAngle * forward.z + sinAngle * right.z);
        Gizmos.DrawLine(thisPoint, nextPoint);
        thisPoint = nextPoint;
      }
    }

    public static void DrawCone(Vector3 origin,
                                Vector3 direction,
                                float angle,
                                float height,
                                Color color,
                                int quality = 4,
                                float duration = 0,
                                bool depthTest = true) {

      float step = height / quality;
      for (float q = step; q <= height; q += step) {
        DrawCircle(origin + direction * q, direction, Mathf.Tan(angle * Constants.DEG_TO_RAD) * q, color, quality * 8, duration, depthTest);
      }
    }

    #endregion

    #region Texture Utils

    private static TextureFormat[] _incompressibleFormats = new TextureFormat[] {
      TextureFormat.R16,
      TextureFormat.EAC_R,
      TextureFormat.EAC_R_SIGNED,
      TextureFormat.EAC_RG,
      TextureFormat.EAC_RG_SIGNED,
      TextureFormat.ETC_RGB4_3DS,
      TextureFormat.ETC_RGBA8_3DS
    };

    /// <summary>
    /// Returns whether or not the given format is a valid input to EditorUtility.CompressTexture();
    /// </summary>
    public static bool IsCompressible(TextureFormat format) {
      if (format < 0) {
        return false;
      }

      return Array.IndexOf(_incompressibleFormats, format) < 0;
    }

    #endregion

    #region Rect Utils

    /// <summary>
    /// Returns the area of the Rect, width * height.
    /// </summary>
    public static float Area(this Rect rect) {
      return rect.width * rect.height;
    }

    /// <summary>
    /// Returns a new Rect with the argument as an outward margin on each border of this
    /// Rect; the result is a larger Rect.
    /// </summary>
    public static Rect Extrude(this Rect r, float margin) {
      return new Rect(r.x - margin, r.y - margin,
                      r.width + (margin * 2f), r.height + (margin * 2f));
    }

    /// <summary>
    /// Returns a new Rect with the argument padding as a margin relative to each
    /// border of the provided Rect.
    /// </summary>
    public static Rect PadInner(this Rect r, float padding) {
      return PadInner(r, padding, padding, padding, padding);
    }

    /// <summary>
    /// Returns a new Rect with the argument padding as a margin inward from each
    /// corresponding border of the provided Rect. The returned Rect will never collapse
    /// to have a width or height less than zero, and its resulting size will never be
    /// larger than the input rect.
    /// </summary>
    public static Rect PadInner(this Rect r, float padTop, float padBottom,
                                             float padLeft, float padRight) {
      var x = r.x + padLeft;
      var y = r.y + padBottom;
      var w = r.width - padRight - padLeft;
      var h = r.height - padTop - padBottom;
      if (w < 0f) {
        x = r.x + (padLeft / (padLeft + padRight)) * r.width;
        w = 0;
      }
      if (h < 0f) {
        y = r.y + (padBottom / (padBottom + padTop)) * r.height;
        h = 0;
      }
      return new Rect(x, y, w, h);
    }

    #region Pad, No Out

    public static Rect PadTop(this Rect r, float padding) {
      return PadInner(r, padding, 0f, 0f, 0f);
    }

    public static Rect PadBottom(this Rect r, float padding) {
      return PadInner(r, 0f, padding, 0f, 0f);
    }

    public static Rect PadLeft(this Rect r, float padding) {
      return PadInner(r, 0f, 0f, padding, 0f);
    }

    public static Rect PadRight(this Rect r, float padding) {
      return PadInner(r, 0f, 0f, 0f, padding);
    }

    #endregion

    #region Pad, With Out

    /// <summary>
    /// Returns the Rect if padded on the top by the padding amount, and optionally
    /// outputs the remaining margin into marginRect.
    /// </summary>
    public static Rect PadTop(this Rect r, float padding, out Rect marginRect) {
      marginRect = r.TakeTop(padding);
      return PadTop(r, padding);
    }

    /// <summary>
    /// Returns the Rect if padded on the bottom by the padding amount, and optionally
    /// outputs the remaining margin into marginRect.
    /// </summary>
    public static Rect PadBottom(this Rect r, float padding, out Rect marginRect) {
      marginRect = r.TakeBottom(padding);
      return PadBottom(r, padding);
    }

    /// <summary>
    /// Returns the Rect if padded on the left by the padding amount, and optionally
    /// outputs the remaining margin into marginRect.
    /// </summary>
    public static Rect PadLeft(this Rect r, float padding, out Rect marginRect) {
      marginRect = r.TakeLeft(padding);
      return PadLeft(r, padding);
    }

    /// <summary>
    /// Returns the Rect if padded on the right by the padding amount, and optionally
    /// outputs the remaining margin into marginRect.
    /// </summary>
    public static Rect PadRight(this Rect r, float padding, out Rect marginRect) {
      marginRect = r.TakeRight(padding);
      return PadRight(r, padding);
    }

    #endregion

    #region Pad Percent, Two Sides

    public static Rect PadTopBottomPercent(this Rect r, float padPercent) {
      float padHeight = r.height * padPercent;
      return r.PadInner(padHeight, padHeight, 0f, 0f);
    }

    public static Rect PadLeftRightPercent(this Rect r, float padPercent) {
      float padWidth = r.width * padPercent;
      return r.PadInner(0f, 0f, padWidth, padWidth);
    }

    #endregion

    #region Pad Percent

    public static Rect PadTopPercent(this Rect r, float padPercent) {
      float padHeight = r.height * padPercent;
      return PadTop(r, padHeight);
    }

    public static Rect PadBottomPercent(this Rect r, float padPercent) {
      float padHeight = r.height * padPercent;
      return PadBottom(r, padHeight);
    }

    public static Rect PadLeftPercent(this Rect r, float padPercent) {
      return PadLeft(r, r.width * padPercent);
    }

    public static Rect PadRightPercent(this Rect r, float padPercent) {
      return PadRight(r, r.width * padPercent);
    }

    #endregion

    #region Take, No Out

    /// <summary>
    /// Return a margin of the given height on the top of the input Rect.
    /// You can't Take more than there is Rect to take from.
    /// <summary>
    public static Rect TakeTop(this Rect r, float heightFromTop) {
      heightFromTop = Mathf.Clamp(heightFromTop, 0f, r.height);
      return new Rect(r.x, r.y + r.height - heightFromTop, r.width, heightFromTop);
    }

    /// <summary>
    /// Return a margin of the given height on the bottom of the input Rect.
    /// You can't Take more than there is Rect to take from.
    /// <summary>
    public static Rect TakeBottom(this Rect r, float heightFromBottom) {
      heightFromBottom = Mathf.Clamp(heightFromBottom, 0f, r.height);
      return new Rect(r.x, r.y, r.width, heightFromBottom);
    }

    /// <summary>
    /// Return a margin of the given width on the left side of the input Rect.
    /// You can't Take more than there is Rect to take from.
    /// <summary>
    public static Rect TakeLeft(this Rect r, float widthFromLeft) {
      widthFromLeft = Mathf.Clamp(widthFromLeft, 0f, r.width);
      return new Rect(r.x, r.y, widthFromLeft, r.height);
    }

    /// <summary>
    /// Return a margin of the given width on the right side of the input Rect.
    /// You can't Take more than there is Rect to take from.
    /// <summary>
    public static Rect TakeRight(this Rect r, float widthFromRight) {
      widthFromRight = Mathf.Clamp(widthFromRight, 0f, r.width);
      return new Rect(r.x + r.width - widthFromRight, r.y, r.height, widthFromRight);
    }

    #endregion

    #region Take, With Out

    /// <summary>
    /// Return a margin of the given width on the top of the input Rect, and
    /// optionally outputs the rest of the Rect into theRest.
    /// <summary>
    public static Rect TakeTop(this Rect r, float padding, out Rect theRest) {
      theRest = r.PadTop(padding);
      return r.TakeTop(padding);
    }

    /// <summary>
    /// Return a margin of the given width on the bottom of the input Rect, and
    /// optionally outputs the rest of the Rect into theRest.
    /// <summary>
    public static Rect TakeBottom(this Rect r, float padding, out Rect theRest) {
      theRest = r.PadBottom(padding);
      return r.TakeBottom(padding);
    }

    /// <summary>
    /// Return a margin of the given width on the left side of the input Rect, and
    /// optionally outputs the rest of the Rect into theRest.
    /// <summary>
    public static Rect TakeLeft(this Rect r, float padding, out Rect theRest) {
      theRest = r.PadLeft(padding);
      return r.TakeLeft(padding);
    }

    /// <summary>
    /// Return a margin of the given width on the right side of the input Rect, and
    /// optionally outputs the rest of the Rect into theRest.
    /// <summary>
    public static Rect TakeRight(this Rect r, float padding, out Rect theRest) {
      theRest = r.PadRight(padding);
      return r.TakeRight(padding);
    }

    #endregion

    /// <summary>
    /// Returns a horizontal strip of lineHeight of this rect (from the top by default) and
    /// provides what's left of this rect after the line is removed as theRest.
    /// </summary>
    public static Rect TakeHorizontal(this Rect r, float lineHeight,
                                      out Rect theRest,
                                      bool fromTop = true) {
      theRest = new Rect(r.x, (fromTop ? r.y + lineHeight : r.y), r.width, r.height - lineHeight);
      return new Rect(r.x, (fromTop ? r.y : r.y + r.height - lineHeight), r.width, lineHeight);
    }

    public static void SplitHorizontallyWithLeft(this Rect rect, out Rect left, out Rect right, float leftWidth) {
      left = rect;
      left.width = leftWidth;
      right = rect;
      right.x += left.width;
      right.width = rect.width - leftWidth;
    }

    #region Enumerators

    /// <summary>
    /// Slices numLines horizontal line Rects from this Rect and returns an enumerator that
    /// will return each line Rect.
    /// 
    /// The height of each line is the height of the Rect divided by the number of lines
    /// requested.
    /// </summary>
    public static HorizontalLineRectEnumerator TakeAllLines(this Rect r, int numLines) {
      return new HorizontalLineRectEnumerator(r, numLines);
    }

    public struct HorizontalLineRectEnumerator {
      Rect rect;
      int numLines;
      int index;

      public HorizontalLineRectEnumerator(Rect rect, int numLines) {
        this.rect = rect;
        this.numLines = numLines;
        this.index = -1;
      }

      public float eachHeight { get { return this.rect.height / numLines; } }

      public Rect Current {
        get { return new Rect(rect.x, rect.y + eachHeight * index, rect.width, eachHeight); }
      }
      public bool MoveNext() {
        index += 1;
        return index < numLines;
      }
      public HorizontalLineRectEnumerator GetEnumerator() { return this; }

      public void Reset() {
        index = -1;
      }

      public Query<Rect> Query() {
        List<Rect> rects = Pool<List<Rect>>.Spawn();
        try {

          foreach (var rect in this) {
            rects.Add(rect);
          }
          return new Query<Rect>(rects);

        } finally {
          rects.Clear();
          Pool<List<Rect>>.Recycle(rects);
        }
      }
    }

    #endregion

    #endregion

    #endregion

    #region Leap Utilities

    #region Pose Utils

    /// <summary>
    /// Returns a pose such that fromPose.Then(thisPose) will have this position
    /// and the fromPose's rotation.
    /// </summary>
    public static Pose From(this Vector3 position, Pose fromPose) {
      return new Pose(position, fromPose.rotation).From(fromPose);
    }

    public static Pose GetPose(this Rigidbody rigidbody) {
      return new Pose(rigidbody.position, rigidbody.rotation);
    }

    /// <summary>
    /// Returns a Pose that has its position and rotation mirrored on the X axis.
    /// </summary>
    public static Pose MirroredX(this Pose pose) {
      var v = pose.position;
      var q = pose.rotation;
      return new Pose(new Vector3(-v.x, v.y, v.z),
                      new Quaternion(-q.x, q.y, q.z, -q.w).Flipped());
    }

    /// <summary>
    /// Returns a Pose that has its position and rotation flipped.
    /// </summary>
    public static Pose Negated(this Pose pose) {
      var v = pose.position;
      var q = pose.rotation;
      return new Pose(new Vector3(-v.x, -v.y, -v.z),
                      new Quaternion(-q.z, -q.y, -q.z, q.w));
    }

    #endregion

    #endregion

    #region Value Mapping Utils ("Map")

    /// <summary>
    /// Maps the value between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax.
    /// The input value is clamped between valueMin and valueMax; if this is not desired, see MapUnclamped.
    /// </summary>
    public static float Map(this float value, float valueMin, float valueMax, float resultMin, float resultMax) {
      if (valueMin == valueMax) return resultMin;
      return Mathf.Lerp(resultMin, resultMax, ((value - valueMin) / (valueMax - valueMin)));
    }

    /// <summary>
    /// Maps the value between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax,
    /// without clamping the result value between resultMin and resultMax.
    /// </summary>
    public static float MapUnclamped(this float value, float valueMin, float valueMax, float resultMin, float resultMax) {
      if (valueMin == valueMax) return resultMin;
      return Mathf.LerpUnclamped(resultMin, resultMax, ((value - valueMin) / (valueMax - valueMin)));
    }

    /// <summary>
    /// Maps each Vector2 component between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax.
    /// The input values are clamped between valueMin and valueMax; if this is not desired, see MapUnclamped.
    /// </summary>
    public static Vector2 Map(this Vector2 value, float valueMin, float valueMax, float resultMin, float resultMax) {
      return new Vector2(value.x.Map(valueMin, valueMax, resultMin, resultMax),
                        value.y.Map(valueMin, valueMax, resultMin, resultMax));
    }

    /// <summary>
    /// Maps each Vector2 component between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax,
    /// without clamping the result value between resultMin and resultMax.
    /// </summary>
    public static Vector2 MapUnclamped(this Vector2 value, float valueMin, float valueMax, float resultMin, float resultMax) {
      return new Vector2(value.x.MapUnclamped(valueMin, valueMax, resultMin, resultMax),
                        value.y.MapUnclamped(valueMin, valueMax, resultMin, resultMax));
    }

    /// <summary>
    /// Maps each Vector3 component between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax.
    /// The input values are clamped between valueMin and valueMax; if this is not desired, see MapUnclamped.
    /// </summary>
    public static Vector3 Map(this Vector3 value, float valueMin, float valueMax, float resultMin, float resultMax) {
      return new Vector3(value.x.Map(valueMin, valueMax, resultMin, resultMax),
                        value.y.Map(valueMin, valueMax, resultMin, resultMax),
                        value.z.Map(valueMin, valueMax, resultMin, resultMax));
    }

    /// <summary>
    /// Maps each Vector3 component between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax,
    /// without clamping the result value between resultMin and resultMax.
    /// </summary>
    public static Vector3 MapUnclamped(this Vector3 value, float valueMin, float valueMax, float resultMin, float resultMax) {
      return new Vector3(value.x.MapUnclamped(valueMin, valueMax, resultMin, resultMax),
                        value.y.MapUnclamped(valueMin, valueMax, resultMin, resultMax),
                        value.z.MapUnclamped(valueMin, valueMax, resultMin, resultMax));
    }

    /// <summary>
    /// Maps each Vector4 component between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax.
    /// The input values are clamped between valueMin and valueMax; if this is not desired, see MapUnclamped.
    /// </summary>
    public static Vector4 Map(this Vector4 value, float valueMin, float valueMax, float resultMin, float resultMax) {
      return new Vector4(value.x.Map(valueMin, valueMax, resultMin, resultMax),
                        value.y.Map(valueMin, valueMax, resultMin, resultMax),
                        value.z.Map(valueMin, valueMax, resultMin, resultMax),
                        value.w.Map(valueMin, valueMax, resultMin, resultMax));
    }

    /// <summary>
    /// Maps each Vector4 component between valueMin and valueMax to its linearly proportional equivalent between resultMin and resultMax,
    /// without clamping the result value between resultMin and resultMax.
    /// </summary>
    public static Vector4 MapUnclamped(this Vector4 value, float valueMin, float valueMax, float resultMin, float resultMax) {
      return new Vector4(value.x.MapUnclamped(valueMin, valueMax, resultMin, resultMax),
                        value.y.MapUnclamped(valueMin, valueMax, resultMin, resultMax),
                        value.z.MapUnclamped(valueMin, valueMax, resultMin, resultMax),
                        value.w.MapUnclamped(valueMin, valueMax, resultMin, resultMax));
    }

    /// <summary>
    /// Returns a vector between resultMin and resultMax based on the input value's position
    /// between valueMin and valueMax.
    /// The input value is clamped between valueMin and valueMax.
    /// </summary>
    public static Vector2 Map(float input, float valueMin, float valueMax, Vector2 resultMin, Vector2 resultMax) {
      return Vector2.Lerp(resultMin, resultMax, Mathf.InverseLerp(valueMin, valueMax, input));
    }

    /// <summary>
    /// Returns a vector between resultMin and resultMax based on the input value's position
    /// between valueMin and valueMax.
    /// The input value is clamped between valueMin and valueMax.
    /// </summary>
    public static Vector3 Map(float input, float valueMin, float valueMax, Vector3 resultMin, Vector3 resultMax) {
      return Vector3.Lerp(resultMin, resultMax, Mathf.InverseLerp(valueMin, valueMax, input));
    }

    /// <summary>
    /// Returns a vector between resultMin and resultMax based on the input value's position
    /// between valueMin and valueMax.
    /// The input value is clamped between valueMin and valueMax.
    /// </summary>
    public static Vector4 Map(float input, float valueMin, float valueMax, Vector4 resultMin, Vector4 resultMax) {
      return Vector4.Lerp(resultMin, resultMax, Mathf.InverseLerp(valueMin, valueMax, input));
    }

    /// <summary>
    /// Returns a new Vector2 via component-wise multiplication.
    /// This operation is equivalent to Vector3.Scale(A, B).
    /// </summary>
    public static Vector2 CompMul(this Vector2 A, Vector2 B) {
      return new Vector2(A.x * B.x, A.y * B.y);
    }

    /// <summary>
    /// Returns a new Vector3 via component-wise multiplication.
    /// This operation is equivalent to Vector3.Scale(A, B).
    /// </summary>
    public static Vector3 CompMul(this Vector3 A, Vector3 B) {
      return new Vector3(A.x * B.x, A.y * B.y, A.z * B.z);
    }

    /// <summary>
    /// Returns a new Vector4 via component-wise multiplication.
    /// This operation is equivalent to Vector3.Scale(A, B).
    /// </summary>
    public static Vector4 CompMul(this Vector4 A, Vector4 B) {
      return new Vector4(A.x * B.x, A.y * B.y, A.z * B.z, A.w * B.w);
    }

    /// <summary>
    /// Returns a new Vector3 via component-wise division.
    /// This operation is the inverse of A.CompMul(B).
    /// </summary>
    public static Vector2 CompDiv(this Vector2 A, Vector2 B) {
      return new Vector2(A.x / B.x, A.y / B.y);
    }

    /// <summary>
    /// Returns a new Vector3 via component-wise division.
    /// This operation is the inverse of A.CompMul(B).
    /// </summary>
    public static Vector3 CompDiv(this Vector3 A, Vector3 B) {
      return new Vector3(A.x / B.x, A.y / B.y, A.z / B.z);
    }

    /// <summary>
    /// Returns a new Vector4 via component-wise division.
    /// This operation is the inverse of A.CompMul(B).
    /// </summary>
    public static Vector4 CompDiv(this Vector4 A, Vector4 B) {
      return new Vector4(A.x / B.x, A.y / B.y, A.z / B.z, A.w / B.w);
    }

    /// <summary>
    /// Returns the sum of the components of the input vector.
    /// </summary>
    public static float CompSum(this Vector2 v) {
      return v.x + v.y;
    }

    /// <summary>
    /// Returns the sum of the components of the input vector.
    /// </summary>
    public static float CompSum(this Vector3 v) {
      return v.x + v.y + v.z;
    }

    /// <summary>
    /// Returns the sum of the components of the input vector.
    /// </summary>
    public static float CompSum(this Vector4 v) {
      return v.x + v.y + v.z + v.w;
    }

    /// <summary>
    /// Returns the largest component of the input vector.
    /// </summary>
    public static float CompMax(this Vector2 v) {
      return Mathf.Max(v.x, v.y);
    }

    /// <summary>
    /// Returns the largest component of the input vector.
    /// </summary>
    public static float CompMax(this Vector3 v) {
      return Mathf.Max(Mathf.Max(v.x, v.y), v.z);
    }

    /// <summary>
    /// Returns the largest component of the input vector.
    /// </summary>
    public static float CompMax(this Vector4 v) {
      return Mathf.Max(Mathf.Max(Mathf.Max(v.x, v.y), v.z), v.w);
    }

    /// <summary>
    /// Returns the smallest component of the input vector.
    /// </summary>
    public static float CompMin(this Vector2 v) {
      return Mathf.Min(v.x, v.y);
    }

    /// <summary>
    /// Returns the smallest component of the input vector.
    /// </summary>
    public static float CompMin(this Vector3 v) {
      return Mathf.Min(Mathf.Min(v.x, v.y), v.z);
    }

    /// <summary>
    /// Returns the smallest component of the input vector.
    /// </summary>
    public static float CompMin(this Vector4 v) {
      return Mathf.Min(Mathf.Min(Mathf.Min(v.x, v.y), v.z), v.w);
    }

    /// <summary>
    /// Returns a new Vector2 via component-wise Lerp.
    /// </summary>
    public static Vector2 CompLerp(this Vector2 A, Vector2 B, Vector2 Ts) {
      return new Vector2(Mathf.Lerp(A.x, B.x, Ts.x), Mathf.Lerp(A.y, B.y, Ts.y));
    }

    /// <summary>
    /// Returns a new Vector3 via component-wise Lerp.
    /// </summary>
    public static Vector3 CompLerp(this Vector3 A, Vector3 B, Vector3 Ts) {
      return new Vector3(Mathf.Lerp(A.x, B.x, Ts.x), Mathf.Lerp(A.y, B.y, Ts.y),
        Mathf.Lerp(A.z, B.z, Ts.z));
    }

    /// <summary>
    /// Returns a new Vector4 via component-wise Lerp.
    /// </summary>
    public static Vector4 CompLerp(this Vector4 A, Vector4 B, Vector4 Ts) {
      return new Vector4(Mathf.Lerp(A.x, B.x, Ts.x), Mathf.Lerp(A.y, B.y, Ts.y),
        Mathf.Lerp(A.z, B.z, Ts.z), Mathf.Lerp(A.w, B.w, Ts.w));
    }

    #endregion

    #region From/Then Utilities

    #region Float

    /// <summary>
    /// Additive From syntax for floats. Evaluated as this float plus the additive
    /// inverse of the other float, usually expressed as thisFloat - otherFloat.
    /// 
    /// For less trivial uses of From/Then syntax, refer to their implementations for
    /// Quaternions and Matrix4x4s.
    /// </summary>
    public static float From(this float thisFloat, float otherFloat) {
      return thisFloat - otherFloat;
    }

    /// <summary>
    /// Additive To syntax for floats. Evaluated as this float plus the additive
    /// inverse of the other float, usually expressed as otherFloat - thisFloat.
    /// 
    /// For less trivial uses of From/Then syntax, refer to their implementations for
    /// Quaternions and Matrix4x4s.
    /// </summary>
    public static float To(this float thisFloat, float otherFloat) {
      return otherFloat - thisFloat;
    }

    /// <summary>
    /// Additive Then syntax for floats. Literally, thisFloat + otherFloat.
    /// </summary>
    public static float Then(this float thisFloat, float otherFloat) {
      return thisFloat + otherFloat;
    }

    #endregion

    #region Vector3

    /// <summary>
    /// Additive From syntax for Vector3. Literally thisVector - otherVector.
    /// </summary>
    public static Vector3 From(this Vector3 thisVector, Vector3 otherVector) {
      return thisVector - otherVector;
    }

    /// <summary>
    /// Additive To syntax for Vector3. Literally otherVector - thisVector.
    /// </summary>
    public static Vector3 To(this Vector3 thisVector, Vector3 otherVector) {
      return otherVector - thisVector;
    }

    /// <summary>
    /// Additive Then syntax for Vector3. Literally thisVector + otherVector.
    /// For example: A.Then(B.From(A)) == B.
    /// </summary>
    public static Vector3 Then(this Vector3 thisVector, Vector3 otherVector) {
      return thisVector + otherVector;
    }

    #endregion

    #region Quaternion

    /// <summary>
    /// A.From(B) produces the quaternion that rotates from B to A.
    /// Combines with Then() to produce readable, predictable results:
    /// B.Then(A.From(B)) == A.
    /// </summary>
    public static Quaternion From(this Quaternion thisQuaternion, Quaternion otherQuaternion) {
      return Quaternion.Inverse(otherQuaternion) * thisQuaternion;
    }

    /// <summary>
    /// A.To(B) produces the quaternion that rotates from A to B.
    /// Combines with Then() to produce readable, predictable results:
    /// B.Then(B.To(A)) == A.
    /// </summary>
    public static Quaternion To(this Quaternion thisQuaternion, Quaternion otherQuaternion) {
      return Quaternion.Inverse(thisQuaternion) * otherQuaternion;
    }

    /// <summary>
    /// Rotates this quaternion by the other quaternion. This is a rightward syntax for
    /// Quaternion multiplication, which normally obeys left-multiply ordering.
    /// </summary>
    public static Quaternion Then(this Quaternion thisQuaternion, Quaternion otherQuaternion) {
      return thisQuaternion * otherQuaternion;
    }

    #endregion

    #region Pose

    /// <summary>
    /// From syntax for Pose structs; A.From(B) returns the Pose that transforms to
    /// Pose A from Pose B. Also see To() and Then().
    /// 
    /// For example, A.Then(B.From(A)) == B.
    /// </summary>
    public static Pose From(this Pose thisPose, Pose otherPose) {
      return otherPose.inverse * thisPose;
    }

    /// <summary>
    /// To syntax for Pose structs; A.To(B) returns the Pose that transforms from Pose A
    /// to Pose B. Also see From() and Then().
    /// 
    /// For example, A.Then(A.To(B)) == B.
    /// </summary>
    public static Pose To(this Pose thisPose, Pose otherPose) {
      return thisPose.inverse * otherPose;
    }

    /// <summary>
    /// Returns the other pose transformed by this pose. This pose could be understood as
    /// the parent pose, and the other pose transformed from local this-pose space to
    /// world space.
    /// 
    /// This is similar to matrix multiplication: A * B == A.Then(B). However, order of
    /// operations is more explicit with this syntax.
    /// </summary>
    public static Pose Then(this Pose thisPose, Pose otherPose) {
      return thisPose * otherPose;
    }

    #endregion

    #region Matrix4x4

    /// <summary>
    /// A.From(B) produces the matrix that transforms from B to A.
    /// Combines with Then() to produce readable, predictable results:
    /// B.Then(A.From(B)) == A.
    /// 
    /// Warning: Scale factors of zero will invalidate this behavior.
    /// </summary>
    public static Matrix4x4 From(this Matrix4x4 thisMatrix, Matrix4x4 otherMatrix) {
      return thisMatrix * otherMatrix.inverse;
    }

    /// <summary>
    /// A.To(B) produces the matrix that transforms from A to B.
    /// Combines with Then() to produce readable, predictable results:
    /// B.Then(B.To(A)) == A.
    /// 
    /// Warning: Scale factors of zero will invalidate this behavior.
    /// </summary>
    public static Matrix4x4 To(this Matrix4x4 thisMatrix, Matrix4x4 otherMatrix) {
      return otherMatrix * thisMatrix.inverse;
    }

    /// <summary>
    /// Transforms this matrix by the other matrix. This is a rightward syntax for
    /// matrix multiplication, which normally obeys left-multiply ordering.
    /// </summary>
    public static Matrix4x4 Then(this Matrix4x4 thisMatrix, Matrix4x4 otherMatrix) {
      return otherMatrix * thisMatrix;
    }

    #endregion

    #endregion

  }

}
