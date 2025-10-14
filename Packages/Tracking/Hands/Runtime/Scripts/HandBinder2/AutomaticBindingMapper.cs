/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2025.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using Leap;
using log4net.Layout.Pattern;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEditor.Animations;
using UnityEngine;

namespace Leap.HandsModule
{
    public class BindingConversionSettings
    {
        public bool RigHasMetacarpalsForAllFingers = false;

        public Chirality TargetChirality = Chirality.Left;
    }

    public enum ChiralityPosition
    {
        NotPresent,
        Prefix,
        Suffix
    }

    public class NamingConvention
    {
        public static readonly Dictionary<BoundFingerBoneType, List<string>> FingerBoneTypeToNameMap = new Dictionary<BoundFingerBoneType, List<string>>()
        {
            { BoundFingerBoneType.METACARPAL, new List<string>() { "Meta" } },
            { BoundFingerBoneType.PROXIMAL, new List<string>() { "Proximal" } },
            { BoundFingerBoneType.INTERMEDIATE, new List<string>() { "Intermediate" } },
            { BoundFingerBoneType.DISTAL, new List<string>() { "Distal" } },
            { BoundFingerBoneType.TIP, new List<string>() { "Tip" } },

            { BoundFingerBoneType.ELBOW, new List<string>() { "Elbow", "Forearm" } },
            { BoundFingerBoneType.WRIST, new List<string>() { "Wrist", "Hand" } }
        };

        public static readonly Dictionary<BoundFingerType, List<string>> FingerTypeToNameMap = new Dictionary<BoundFingerType, List<string>>()
        {
            { BoundFingerType.THUMB, new List<string>() { "Thumb" } },
            { BoundFingerType.INDEX, new List<string>() { "Index" } },
            { BoundFingerType.MIDDLE, new List<string>() { "Middle" } },
            { BoundFingerType.RING, new List<string>() { "Ring" } },
            { BoundFingerType.LITTLE, new List<string>() { "Little", "Pinky" } },
        };

        public char Separator;
        public bool UsesSeparator = false;

        public string ChiralityIdentifer;

        public ChiralityPosition ChiralityPosition = ChiralityPosition.NotPresent;

        public string Prefix;

        /// <summary>
        /// A set of valid separators, commonly recognized by blender to split a bone name into parts
        /// See https://docs.blender.org/manual/en/latest/animation/armatures/bones/editing/naming.html
        /// </summary>
        public static char[] BoneSeparators = { '_', ' ', '-', '_' };

        /// <summary>
        /// A set of valid identifiers to mark the chirality of a bone, supported by Blender
        /// See https://docs.blender.org/manual/en/latest/animation/armatures/bones/editing/naming.html
        /// Note L/l and R/r are only vaild if a separator is used
        /// </summary>
        public static string[] ChiralityIdentifiers_Left = { "L", "l", "Left", "LEFT" };
        public static string[] ChiralityIdentifiers_Right = { "R", "r", "Right", "RIGHT" };

        /// <summary>
        /// A set of valid identifiers to mark the chirality of a bone, supported by Blender
        /// See https://docs.blender.org/manual/en/latest/animation/armatures/bones/editing/naming.html
        /// Note L/l and R/r are only vaild if a separator is used
        /// </summary>
        public static string[] ChiralityIdentifiers_Left_NoSeparator = { "Left", "LEFT" };
        public static string[] ChiralityIdentifiers_Right_NoSeparator = { "Right", "RIGHT" };

        /// <summary>
        /// Given a likely node mapped to hand data, normally something like the wrist or elbow, determines the naming convention used to name nodes
        /// </summary>
        /// <param name="nodeName">The name</param>
        /// <returns>The naming convention used</returns>
        public static NamingConvention Determine(string nodeName, BoundBoneType boundType)
        {
            NamingConvention namingConvention = new();

            if (boundType != BoundBoneType.WRIST || boundType != BoundBoneType.ELBOW)
            {
                Debug.LogWarning("This function only supports the Elbow or Wrist node");
                return namingConvention;
            }

            // Might need to harden this to take the boundType
            (int startIndex, int endIndex) = FindIndexOfMatchToName(nodeName, FingerBoneTypeToNameMap);
            string prefix = nodeName.Substring(0, startIndex);
            string suffix = nodeName.Substring(endIndex, nodeName.Length - endIndex);

            // Identify the separator, if used
            foreach (var separator in BoneSeparators)
            {
                if (nodeName.Contains(separator))
                {
                    namingConvention.Separator = separator;
                    namingConvention.UsesSeparator = true;
                    break;
                }
            }

            // Identify the chirality string and placement, if used
            var legalChiralityIdentifiers = namingConvention.UsesSeparator ? ChiralityIdentifiers_Left.Concat(ChiralityIdentifiers_Right) :
                                                                            ChiralityIdentifiers_Left_NoSeparator.Concat(ChiralityIdentifiers_Right_NoSeparator);

            namingConvention.ChiralityIdentifer = legalChiralityIdentifiers.Where(cid => nodeName.Contains(cid)).FirstOrDefault();

            if (!String.IsNullOrEmpty(namingConvention.ChiralityIdentifer))
            {
                if (prefix.Contains(namingConvention.ChiralityIdentifer))
                {
                    namingConvention.ChiralityPosition = ChiralityPosition.Prefix;

                    namingConvention.Prefix = nodeName.Substring(0, prefix.IndexOf(namingConvention.ChiralityIdentifer));
                }
                else if (suffix.Contains(namingConvention.ChiralityIdentifer))
                {
                    namingConvention.ChiralityPosition = ChiralityPosition.Suffix;
                    namingConvention.Prefix = prefix;
                }
                else
                {
                    Debug.LogError($"Chriality identifier {namingConvention.ChiralityIdentifer} looks to be in an unexpected place");
                }
            }

            // Identify any prefix
            return namingConvention;

            (int startIndex, int endIndex) FindIndexOfMatchToName<T>(string name, Dictionary<T, List<string>> map)
            {
                foreach (var item in map)
                {
                    foreach (var nameToken in item.Value)
                    {
                        if (name.Contains(nameToken, StringComparison.CurrentCultureIgnoreCase))
                        {
                            int startIndex = name.IndexOf(nameToken);
                            return (startIndex, startIndex + nameToken.Length);
                        }
                    }
                }

                return (-1, -1);
            }  
        }
    }

    /// <summary>
    /// Analyses the bones in a skinned mesh renderer and maps them to hand tracking data 
    /// </summary>
    public class AutomaticBindingMapper : MonoBehaviour
    {
        /// <summary>
        /// Outputs a BindingMap, which maps transforms in the rig to hand tracking bones, given a skinned mesh renderer
        /// Supports full bipedal skeletons and common naming conventions supported by blender, for separators, chirality and leaf nodes.
        /// Does not support front/back or top/bottom
        /// </summary>
        /// <param name="target">A skinned mesh renderer, e.g. as produced when a rigged FBX file is imported</param>
        /// <param name="settings">Conversion options</param>
        /// <returns>The binding map</returns>
        public static BindingMap Bind(SkinnedMeshRenderer target, BindingConversionSettings settings)
        {
            BindingMap map = new BindingMap();

            if (target == null)
            {
                Debug.LogError("Must supply a valid skinned mesh renderer to generate a binding map");
                return null;
            }

            Transform rootBoneTransform = target.rootBone;

            if (rootBoneTransform == null)
            {
                Debug.LogError("The skinned mesh renderer requires a root bone to be able to generate a binding map");
                return null;
            }

            // Identify the root bone type ...
            NamingConvention namingConvention;
            (map.RootHandOrArmJoint, namingConvention) = FindHandDataRootAndNamingConvention(rootBoneTransform, settings.TargetChirality);
            map.RootBoneJointType = NameToBoneType(map.RootHandOrArmJoint.name);
            map.RigBoneNamingConvention = namingConvention;

            switch (map.RootBoneJointType)
            {
                case BoundBoneType.ELBOW:
                    map[BoundBoneType.ELBOW] = new BoundBoneData(map.RootHandOrArmJoint, map.RootBoneJointType);

                    var (isWrist, wristTransform) = IsTheChildOfTheElbowAWrist(map.RootHandOrArmJoint);

                    if (isWrist)
                        map[BoundBoneType.WRIST] = new BoundBoneData(wristTransform, BoundBoneType.WRIST);
                    else
                        Debug.LogWarning("Could not find a transform that looks like a wrist joint. Not mapping a full hand");
                    break;

                case BoundBoneType.WRIST:
                    map[BoundBoneType.WRIST] = new BoundBoneData(map.RootHandOrArmJoint, map.RootBoneJointType);
                    break;

                default:
                    Debug.LogWarning("Transform hierarchy for skinned mesh renderer does not contain a wrist or elbow");
                    break;
            }

            // We should now have identified the elbow (if present) and a wrist (if present) - we now need to map the fingers
            if (map[BoundBoneType.WRIST] != null)
            {
                foreach (var digitRoot in map[BoundBoneType.WRIST].BoundSkinnedMeshRendererTransform.GetChildren())
                {
                    MapDigit(digitRoot, map, settings.RigHasMetacarpalsForAllFingers);
                }
            }
            else // Rig only contains a subset of hand joints, try to brute force map them
            {
                Debug.LogWarning("Rig only contains a subset of hand joints (e.g. no wrist), this is not currently supported");
            }
            return map;
        }

        private static void MapDigit(Transform digitRoot, BindingMap map, bool rigHasMetacarpalsForAllFingers)
        {
            // Some rigs use numbers or alphabetical suffixes for the bones in a finger, not names. If explict names aren't used then
            // we really need to know if metacarpals are being used *if* there are fewer than 5 joints (meta->proximal->intermediate->distal->tip)
            // as that's ambiguous between a tip being rigged or a metacarpal being rigged
            int boneCount = GetTransformHierarchyDepth(digitRoot);
            bool isThumb = IsThumb(digitRoot);

            if (isThumb ? boneCount > 4 : boneCount > 5)
            {
                Debug.LogWarning($"Found more than 5 bones in the digit {digitRoot.name}. Is it possible the rig has been imported with unecessary leaf bones?");
            }

            bool boneBindingsAreAmbiguous = isThumb ? boneCount < 4 && !FingerBonesAllUseBoneNames(digitRoot) : boneCount < 5 && !FingerBonesAllUseBoneNames(digitRoot);

            if (boneBindingsAreAmbiguous)
            {
                Debug.Log("Hand contains finger bones that do not explicitly name the bone, but use a list identifier instead (_1, _a, etc.).");

                BoundFingerType fingerType = DetermineFingerType(digitRoot);
                BoundFingerBoneType fingerBoneType = BoundFingerBoneType.PROXIMAL;

                if (rigHasMetacarpalsForAllFingers)
                {
                    fingerBoneType = BoundFingerBoneType.METACARPAL;
                }

                Debug.Log($"Mapping the {boneCount} bones in the finger for {fingerType} starting at {fingerBoneType}");
                var node = digitRoot;
                do
                {
                    var boneType = FingerAndFingerBoneToBoneType(fingerType, fingerBoneType);
                    map[boneType] = new BoundBoneData(node, boneType);

                    fingerBoneType = GetNextFingerBone(fingerBoneType);

                    foreach (var child in node.GetChildren())
                    {
                        node = child;
                        break;
                    }

                } while (node != null);
            }
            else
            {
                var node = digitRoot;
                var previousBoneType = BoundBoneType.UNKNOWN;
                do
                {
                    var boneType = NameToBoneType(node.name);

                    if (boneType != BoundBoneType.UNKNOWN)
                    {
                        if (previousBoneType == boneType)
                        {
                            // We've just had something like tip and tip_end. We don't support leaf bones. Skip it and warn the user 
                            if (node.name.EndsWith("_end"))
                            {
                                Debug.Log($"{node.name} will appear to be a duplicate of the previous bone, recommend importing the rig without leaf bones");
                                return;
                            }
                        }
                        map[boneType] = new BoundBoneData(node, boneType);
                    }
                    else
                    {
                        Debug.LogWarning($"Could not find a name match for {node.name} when mapping transform names to bones in a finger");
                    }

                    foreach (var child in node.GetChildren())
                    {
                        node = child;
                        break;
                    }

                    previousBoneType = boneType;

                } while (node != null);
            }
        }

        private static BoundBoneType FingerAndFingerBoneToBoneType(BoundFingerType fingerType, BoundFingerBoneType fingerJointType)
            => (fingerType, fingerJointType) switch
            {
                (BoundFingerType.UNKNOWN, BoundFingerBoneType.ELBOW) => BoundBoneType.ELBOW,
                (BoundFingerType.UNKNOWN, BoundFingerBoneType.WRIST) => BoundBoneType.WRIST,

                (BoundFingerType.THUMB, BoundFingerBoneType.METACARPAL) => BoundBoneType.THUMB_METACARPAL,
                (BoundFingerType.THUMB, BoundFingerBoneType.PROXIMAL) => BoundBoneType.THUMB_PROXIMAL,
                (BoundFingerType.THUMB, BoundFingerBoneType.INTERMEDIATE) => BoundBoneType.UNKNOWN,  // Not supported
                (BoundFingerType.THUMB, BoundFingerBoneType.DISTAL) => BoundBoneType.THUMB_DISTAL,
                (BoundFingerType.THUMB, BoundFingerBoneType.TIP) => BoundBoneType.THUMB_TIP,

                (BoundFingerType.INDEX, BoundFingerBoneType.METACARPAL) => BoundBoneType.INDEX_METACARPAL,
                (BoundFingerType.INDEX, BoundFingerBoneType.PROXIMAL) => BoundBoneType.INDEX_PROXIMAL,
                (BoundFingerType.INDEX, BoundFingerBoneType.INTERMEDIATE) => BoundBoneType.INDEX_INTERMEDIATE,
                (BoundFingerType.INDEX, BoundFingerBoneType.DISTAL) => BoundBoneType.INDEX_DISTAL,
                (BoundFingerType.INDEX, BoundFingerBoneType.TIP) => BoundBoneType.INDEX_TIP,

                (BoundFingerType.MIDDLE, BoundFingerBoneType.METACARPAL) => BoundBoneType.MIDDLE_METACARPAL,
                (BoundFingerType.MIDDLE, BoundFingerBoneType.PROXIMAL) => BoundBoneType.MIDDLE_PROXIMAL,
                (BoundFingerType.MIDDLE, BoundFingerBoneType.INTERMEDIATE) => BoundBoneType.MIDDLE_INTERMEDIATE,
                (BoundFingerType.MIDDLE, BoundFingerBoneType.DISTAL) => BoundBoneType.MIDDLE_DISTAL,
                (BoundFingerType.MIDDLE, BoundFingerBoneType.TIP) => BoundBoneType.MIDDLE_TIP,

                (BoundFingerType.RING, BoundFingerBoneType.METACARPAL) => BoundBoneType.RING_METACARPAL,
                (BoundFingerType.RING, BoundFingerBoneType.PROXIMAL) => BoundBoneType.RING_PROXIMAL,
                (BoundFingerType.RING, BoundFingerBoneType.INTERMEDIATE) => BoundBoneType.RING_INTERMEDIATE,
                (BoundFingerType.RING, BoundFingerBoneType.DISTAL) => BoundBoneType.RING_DISTAL,
                (BoundFingerType.RING, BoundFingerBoneType.TIP) => BoundBoneType.RING_TIP,

                (BoundFingerType.LITTLE, BoundFingerBoneType.METACARPAL) => BoundBoneType.LITTLE_METACARPAL,
                (BoundFingerType.LITTLE, BoundFingerBoneType.PROXIMAL) => BoundBoneType.LITTLE_PROXIMAL,
                (BoundFingerType.LITTLE, BoundFingerBoneType.INTERMEDIATE) => BoundBoneType.LITTLE_INTERMEDIATE,
                (BoundFingerType.LITTLE, BoundFingerBoneType.DISTAL) => BoundBoneType.LITTLE_DISTAL,
                (BoundFingerType.LITTLE, BoundFingerBoneType.TIP) => BoundBoneType.LITTLE_TIP,

                (_, _) => BoundBoneType.UNKNOWN
            };

        private static BoundFingerBoneType GetNextFingerBone(BoundFingerBoneType fingerBone, bool isThumb = false)
        {
            switch (fingerBone)
            {
                case BoundFingerBoneType.ELBOW:
                    return BoundFingerBoneType.WRIST;

                case BoundFingerBoneType.WRIST:
                    return BoundFingerBoneType.METACARPAL;

                case BoundFingerBoneType.METACARPAL:
                    return BoundFingerBoneType.PROXIMAL;

                case BoundFingerBoneType.PROXIMAL:
                    return isThumb ? BoundFingerBoneType.DISTAL : BoundFingerBoneType.INTERMEDIATE;

                case BoundFingerBoneType.INTERMEDIATE:

                    if (isThumb)
                        Debug.LogWarning("Thumb does not have an intermediate phalange from an anatomical perspective. However, Leap Motion data includes it and has a zero length metacarpal, but with valid rotation");

                    return BoundFingerBoneType.DISTAL;

                case BoundFingerBoneType.DISTAL:
                    return BoundFingerBoneType.TIP;

                default:
                    return BoundFingerBoneType.UNKNOWN;
            }
        }

        private static BoundFingerBoneType GetPreviousFingerBone(BoundFingerBoneType fingerBone, bool isThumb = false)
        {
            switch (fingerBone)
            {
                case BoundFingerBoneType.TIP:
                    return BoundFingerBoneType.DISTAL;

                case BoundFingerBoneType.DISTAL:
                    return isThumb ? BoundFingerBoneType.PROXIMAL : BoundFingerBoneType.INTERMEDIATE;

                case BoundFingerBoneType.INTERMEDIATE:

                    if (isThumb)
                        Debug.LogWarning("Thumb does not have an intermediate phalange from an anatomical perspective. However, Leap Motion data includes it and has a zero length metacarpal, but with valid rotation");

                    return BoundFingerBoneType.PROXIMAL;

                case BoundFingerBoneType.PROXIMAL:
                    return BoundFingerBoneType.METACARPAL;

                case BoundFingerBoneType.METACARPAL:
                    return BoundFingerBoneType.WRIST;

                case BoundFingerBoneType.WRIST:
                    return BoundFingerBoneType.ELBOW;

                default:
                    return BoundFingerBoneType.UNKNOWN;
            }
        }

        private static bool FingerBonesAllUseBoneNames(Transform digitRoot)
        {
            List<Transform> allChildren = new List<Transform>();
            digitRoot.GetAllChildren(allChildren);

            foreach (var child in allChildren)
            {
                if (NameToFingerBoneType(child.name) == BoundFingerBoneType.UNKNOWN)
                    return false;
            }

            return true;
        }

        private static bool IsThumb(Transform digitRoot)
        {
            return DetermineFingerType(digitRoot) == BoundFingerType.THUMB;
        }

        private static BoundFingerType DetermineFingerType(Transform digitRoot)
        {
            List<Transform> allChildren = new List<Transform>();
            digitRoot.GetAllChildren(allChildren);

            foreach (var child in allChildren)
            {
                if (NameToFingerType(child.name) != BoundFingerType.UNKNOWN)
                    return NameToFingerType(child.name);
            }

            return BoundFingerType.UNKNOWN;
        }

        private static int GetTransformHierarchyDepth(Transform digitRoot)
        {
            var node = digitRoot;
            int depth = 1;

            while (node.childCount > 0)
            {
                foreach (var child in node.GetChildren())
                {
                    depth++;
                    node = child;
                    break;
                }
            }

            return depth;
        }

        private static (bool isWrist, Transform wristTransform) IsTheChildOfTheElbowAWrist(Transform rootHandOrArmJoint)
        {
            var children = rootHandOrArmJoint.GetChildren();

            if (rootHandOrArmJoint.childCount > 0)
            {
                foreach (var child in children)
                {
                    var boneType = NameToBoneType(child.name);
                    return (rootHandOrArmJoint.childCount == 1 && boneType == BoundBoneType.WRIST, child);
                }
            }

            return (false, null);
        }


        /// <summary>
        /// Perform a breath first search of the nodes (which map to bone origins)
        /// </summary>
        /// <param name="root"></param>
        /// <returns>The first transform that maps to a known hand tracking data source</returns>
        private static (Transform root, NamingConvention namingConvention) FindHandDataRootAndNamingConvention(Transform root, Chirality targetChirality)
        {
            if (root == null)
                return (null, null);

            Queue<Transform> queue = new Queue<Transform>();
            queue.Enqueue(root);

            while (queue.Count > 0)
            {
                Transform current = queue.Dequeue();

                if (NameToBoneType(current.name) != BoundBoneType.UNKNOWN)
                {
                    NamingConvention context = NamingConvention.Determine(current.name, NameToBoneType(current.name));
                    if (targetChirality == NameToChirality(current.name))
                    {
                        return (current, context);
                    }
                    else
                    {
                        // Don't search the children as the chirality is wrong
                        continue;
                    }
                }

                foreach (var child in current.GetChildren())
                {
                    queue.Enqueue(child);
                }
            }

            return (null,null);
        }

        private static Chirality NameToChirality(string name)
        {
            Chirality chirality = Chirality.Left;

            return chirality;
        }

        private static BoundFingerType NameToFingerType(string name)
        {
            return FindMatchToName<BoundFingerType>(name, NamingConvention.FingerTypeToNameMap);
        }

        private static BoundFingerBoneType NameToFingerBoneType(string name)
        {
            return FindMatchToName<BoundFingerBoneType>(name, NamingConvention.FingerBoneTypeToNameMap);
        }

        private static BoundBoneType NameToBoneType(string name)
        {
            return FingerAndFingerBoneToBoneType(NameToFingerType(name), NameToFingerBoneType(name));
        }

        private static T FindMatchToName<T>(string name, Dictionary<T, List<string>> map)
        {
            foreach (var item in map)
            {
                foreach (var nameToken in item.Value)
                {
                    if (name.Contains(nameToken, StringComparison.CurrentCultureIgnoreCase))
                    {
                        return item.Key;
                    }
                }
            }

            return default(T);
        }
    }
}
