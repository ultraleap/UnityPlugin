
using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;

namespace Leap.Unity
{
    public static class UtilsLegacy
    {
        /// <summary>
        /// Calls Physics.IgnoreCollision for each Collider in the first GameObject against
        /// each Collider in the second GameObject.
        /// 
        /// If you have many colliders that need to ignore collisions, consider utilizing
        /// Layer collision settings as an optimization.
        /// </summary>

        public static void IgnoreCollisions(GameObject first, GameObject second,
                                            bool ignore = true)
        {
            if (first == null || second == null)
                return;

            var firstColliders = Pool<List<Collider>>.Spawn(); firstColliders.Clear();
            var secondColliders = Pool<List<Collider>>.Spawn(); secondColliders.Clear();
            try
            {
                first.GetComponentsInChildren(firstColliders);
                second.GetComponentsInChildren(secondColliders);

                for (int i = 0; i < firstColliders.Count; ++i)
                {
                    for (int j = 0; j < secondColliders.Count; ++j)
                    {
                        if (firstColliders[i] != secondColliders[j] &&
                            firstColliders[i].enabled && secondColliders[j].enabled)
                        {
                            Physics.IgnoreCollision(firstColliders[i], secondColliders[j], ignore);
                        }
                    }
                }
            }
            finally
            {
                firstColliders.Clear(); Pool<List<Collider>>.Recycle(firstColliders);
                secondColliders.Clear(); Pool<List<Collider>>.Recycle(secondColliders);
            }
        }

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
                                                   where OwnerType : Component
        {
            ownedComponents.Clear();
            Stack<Transform> toVisit = Pool<Stack<Transform>>.Spawn();
            List<ComponentType> componentsBuffer = Pool<List<ComponentType>>.Spawn();

            try
            {
                toVisit.Push(rootObj.transform);
                Transform curTransform;
                while (toVisit.Count > 0)
                {
                    curTransform = toVisit.Pop();

                    // Recursively search children and children's children.
                    foreach (var child in curTransform.GetChildren())
                    {
                        // Ignore children with OwnerType components of their own; its own OwnerType
                        // component owns its own ComponentType components and the ComponentType
                        // components of its children.
                        if (child.GetComponent<OwnerType>() == null
                            && (includeInactiveObjects || child.gameObject.activeInHierarchy))
                        {
                            toVisit.Push(child);
                        }
                    }

                    // Since we'll visit every valid child, all we need to do is add the
                    // ComponentType components of every transform we visit.
                    componentsBuffer.Clear();
                    curTransform.GetComponents<ComponentType>(componentsBuffer);
                    foreach (var component in componentsBuffer)
                    {
                        ownedComponents.Add(component);
                    }
                }
            }
            finally
            {
                toVisit.Clear();
                Pool<Stack<Transform>>.Recycle(toVisit);

                componentsBuffer.Clear();
                Pool<List<ComponentType>>.Recycle(componentsBuffer);
            }
        }

        /// <summary> Gets whether the target object is part of a prefab asset (excluding prefab instances.) Compiles differently pre- and post-2018.3. Also compiles differently in builds, where this method always returns false. </summary>
        public static bool IsObjectPartOfPrefabAsset(UnityEngine.Object obj)
        {
#if UNITY_EDITOR
            // Exclude objects that are not part of any prefab, and exclude prefab _instances_.
            return UnityEditor.PrefabUtility.IsPartOfAnyPrefab(obj) &&
              UnityEditor.PrefabUtility.GetPrefabInstanceStatus(obj) == UnityEditor.PrefabInstanceStatus.NotAPrefab;
#else
            return false;
#endif
        }

        public static string MakeRelativePath(string relativeTo, string path)
        {
            if (string.IsNullOrEmpty(relativeTo)) throw new ArgumentNullException("relativeTo");
            if (string.IsNullOrEmpty(path)) throw new ArgumentNullException("path");

            Uri relativeToUri = new Uri(relativeTo);
            Uri pathUri = new Uri(path);

            if (relativeToUri.Scheme != pathUri.Scheme) { return path; } // path can't be made relative.

            Uri relativeUri = relativeToUri.MakeRelativeUri(pathUri);
            string relativePath = Uri.UnescapeDataString(relativeUri.ToString());

            if (pathUri.Scheme.Equals("file", StringComparison.InvariantCultureIgnoreCase))
            {
                relativePath = relativePath.Replace(Path.AltDirectorySeparatorChar, Path.DirectorySeparatorChar);
            }

            return relativePath;
        }
    }
}