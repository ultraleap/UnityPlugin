/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2025.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using NUnit.Framework;
using UnityEditor;
using UnityEngine.TestTools;
using Leap;
using Leap.HandsModule;
using System.Transactions;
using System;
using System.Drawing;
using JetBrains.Annotations;
using UnityEngine.Experimental.AI;
using System.Text;
using System.Linq;
using System.CodeDom;
using UnityEditor.VersionControl;

namespace Leap.EditorTests
{
    internal class HandBinder2EditorTests
    {


        /// <summary>
        /// Helper class for defining the core structure of a body armature, used to test that
        /// we can successfully parse full body armatures and extract the correct hand / arm 
        /// tracking points
        /// </summary>
        internal class BodyBoneNode
        {
            public readonly BodyArmatureBone BoneType;
            public readonly string BoneName;
            public readonly List<BodyBoneNode> Children = new List<BodyBoneNode>();

            public BodyBoneNode(BodyArmatureBone boneType, string boneName, IEnumerable<BodyBoneNode>? children)
            {
                BoneType = boneType;
                BoneName = boneName;

                if (children != null)
                {
                    Children.AddRange(children);
                }
            }
        }

        /// <summary>
        /// A default VRChat like rig, see https://creators.vrchat.com/avatars/rig-requirements
        /// Likely based on VRM model armature
        /// </summary>
        public static BodyBoneNode DefaultBodyArmature_VRChat = new BodyBoneNode(
            boneType: BodyArmatureBone.CHEST, boneName: "Chest", children: new List<BodyBoneNode>()
                {
                    new (BodyArmatureBone.NECK, "Neck", new List<BodyBoneNode>() {
                        new(BodyArmatureBone.HEAD, "Head", null) }
                    ),

                    new (BodyArmatureBone.SPINE, "Spine", new List<BodyBoneNode>() {
                        new (BodyArmatureBone.PELVIS, "Pelvis", new List<BodyBoneNode>() {
                            new (BodyArmatureBone.HIP, "Hip", new List<BodyBoneNode>() {
                                new (BodyArmatureBone.KNEE, "Knee", new List<BodyBoneNode>() {
                                    new (BodyArmatureBone.ANKLE, "Ankle", new List<BodyBoneNode>() {
                                        new (BodyArmatureBone.TOE, "Toe", null)
                                    })
                                })
                            })
                        })
                    }),

                    new (BodyArmatureBone.SHOULDER, "Shoulder", new List<BodyBoneNode>() {
                        new(BodyArmatureBone.UPPER_ARM, "Upper", null) }
                    )
                }
        );

        public static readonly Dictionary<BoundFingerType, string> DefaultRiggedFingers = new Dictionary<BoundFingerType, string>()
        {
            { BoundFingerType.THUMB, "Thumb" },
            { BoundFingerType.INDEX, "Index"},
            { BoundFingerType.MIDDLE, "Middle" },
            { BoundFingerType.RING, "Ring"},
            { BoundFingerType.LITTLE, "Little"}
        };

        [Test]
        public void BoneNamingConventionTests_AllVariants()
        {
            var bonePrefixVariants = new[] { String.Empty, "Prefix", "Prefix:" };
            var validBones = new[] { BoundFingerBoneType.ELBOW, BoundFingerBoneType.WRIST};
            var endBoneNameVariants = new[] { String.Empty, "end" };

            foreach (var prefix in bonePrefixVariants)
            {
                foreach (var validBone in validBones)
                {
                    foreach (var validBoneName in NamingConvention.FingerBoneTypeToNameMap[validBone])
                    {
                        foreach (var boneChirality in Enum.GetValues(typeof(BoneChirality)))
                        {
                            foreach (var chiralitySeparatorPosition in Enum.GetValues(typeof(ChiralitySeparatorPosition)))
                            {
                                foreach (var chiralityName in (Chirality)boneChirality == Chirality.Left ? NamingConvention.ChiralityIdentifiers_Left : NamingConvention.ChiralityIdentifiers_Right)
                                {
                                    foreach (var separator in NamingConvention.BoneSeparators)
                                    {
                                        foreach (var endBoneName in endBoneNameVariants)
                                        {
                                            string boneName = GenerateFullBoneName(prefix, validBoneName, (BoneChirality)boneChirality, (ChiralitySeparatorPosition)chiralitySeparatorPosition, chiralityName, chiralityName, separator, endBoneName);

                                            // Check we can decode all typical variants of a bone name (according to Blender's naming conventions), extracting the correct information
                                            NamingConvention namingConvention = NamingConvention.Determine(boneName, validBone == BoundFingerBoneType.ELBOW ? BoundBoneType.ELBOW : BoundBoneType.WRIST);

                                            Assert.AreEqual(namingConvention.Prefix, prefix);
                                            Assert.AreEqual(namingConvention.Separator, separator);
                                            Assert.AreEqual(namingConvention.ChiralityIdentifer, chiralityName);
                                            Assert.AreEqual(namingConvention.ChiralityPosition, chiralitySeparatorPosition);
                                            Assert.AreEqual(namingConvention.UsesSeparator, true); 
                                        }
                                    }
                                } 
                            }
                        }
                    }
                }
            }
        }
        
        [Test]
        public void BoneNamingConventionTests2()
        {
            var boolValues = new[] { false, true };

            foreach (var digitNamingConvention in Enum.GetValues(typeof(DigitNameListGenerator.DigitNamingOptions)))
            {
                foreach (bool isThumb in boolValues)
                {
                    foreach (bool generateMetacarpals in boolValues)
                    {
                        foreach (bool generateTips in boolValues)
                        {
                            var nameList = DigitNameListGenerator.Generate(isThumb,
                                                                            (DigitNameListGenerator.DigitNamingOptions)digitNamingConvention,
                                                                            generateMetacarpals,
                                                                            generateTips);

                            
                        }
                    }
                }
            }
        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToGenericHandRig()
        {
            GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Packages/com.ultraleap.tracking/Tests/Editor/Resources/Rig40.prefab");

            if (prefab != null)
            {
                var skinnedMeshRenderer = prefab.GetComponentInChildren<SkinnedMeshRenderer>();
                var bindingMap = AutomaticBindingMapper.Bind(skinnedMeshRenderer, new BindingConversionSettings() { RigHasMetacarpalsForAllFingers = true });
                Assert.AreEqual(bindingMap.BoundTransformMap.Count, 26);
            }
        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToFullRig()
        {
            var syntheticRig = GenerateSyntheticRig(
                rigName: "TestRig",
                transformRootName: "TestRigRootTransform",
                bonePrefix: "1:",
                bodyArmature: HandBinder2EditorTests.DefaultBodyArmature_VRChat,
                chiralitySeparatorPosition: ChiralitySeparatorPosition.suffix,
                chirality: BoneChirality.NONE,
                leftChiralityName: "L",
                rightChiralityName: "R",
                separator: '_',
                hasElbow: true, "Elbow",
                wristName: "Hand",
                fingers: DefaultRiggedFingers,
                fingerBones: DigitNameListGenerator.DefaultNamedFingerBones_AllBones,
                thumbBones: DigitNameListGenerator.DefaultNamedThumbBones_AllBones,
                generateLeafBones: true
                );

            if (syntheticRig != null)
            {
                LogTransformHierarchy(syntheticRig);
                var skinnedMeshRenderer = syntheticRig.GetComponentInChildren<SkinnedMeshRenderer>();
                var bindingMap = AutomaticBindingMapper.Bind(skinnedMeshRenderer, new BindingConversionSettings() { RigHasMetacarpalsForAllFingers = true });

                // All bones should be bound
                Assert.AreEqual(bindingMap.BoundTransformMap.Count, 26);
            }
        }

        private void LogTransformHierarchy(GameObject syntheticRig)
        {
            StringBuilder sb = new();
            List<bool> lastChild = new List<bool>();
            BuildTransformHierarchyString(syntheticRig, ref lastChild, ref sb);
            Debug.Log(sb.ToString());
        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToRigWithNumberedFingerBones()
        {

        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToRigWithAplhabeticalFingerBones()
        {

        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToRigWithFingerMetacarpalsAndTips()
        {

        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToRigWithoutFingerMetacarpalsAndTips()
        {

        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToRigWithFingerMetacarpalsAndWithoutTips()
        {

        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToRigWithoutFingerMetacarpalsAndWithoutTips()
        {

        }

        [Test]
        public void AutomaticBindingMapper_CanBindCorrectlyToSparseRig()
        {

        }

        /// <summary>
        /// Generates a set of nested game object representing a synthetic hand rig based on the information 
        /// supplied. Should be flexbile enough to cover almost all variants of hand rigs and their names
        /// typically seen. Generally follows blender conventions / VRM conventions.
        /// *Only generates the transform hierarchy with bone names*. Does not generate bones in the rig. 
        /// Also the mesh is not a valid rigged mesh, just a dummy object given to the SkinnedMeshRenderer
        /// </summary>
        /// <param name="rigName">Name of the rig</param>
        /// <param name="transformRootName">Name to give to the root transform</param>
        /// <param name="bonePrefix">Name to prefix all bone names with</param>
        /// <param name="bodyArmature">A body armature structure, which will be expanded using the name info, optional</param>
        /// <param name="chiralitySeparatorPosition">Should the chirality info appear at the start or the end of the bone name?</param>
        /// <param name="chirality">Chirality of the hand, if no body armature is supplied</param>
        /// <param name="leftChiralityName">String used to identify a bone as left chirality</param>
        /// <param name="rightChiralityName">String used to identify a bone as right chirality</param>
        /// <param name="separator">The separator used to separate chirality info from the bone name</param>
        /// <param name="hasElbow">Should the rig have an elbow</param>
        /// <param name="elbowName">Name given to the elbow bone fragment</param>
        /// <param name="wristName">Name given to the wrist bone fragment</param>
        /// <param name="fingers">A list of fingers and names to use in the rig, can be a subset of a full hand</param>
        /// <param name="fingerBones">A list of finger bones and names to use, can be a subset of a full finger</param>
        /// <param name="thumbBones">A list of thumb bones and names to use, can be a subset of a full thumb</param>
        /// <param name="generateLeafBones">Should leaf bones be created? These are an extra bone placed on the end of a bone hierarchy, with the _end suffix</param>
        /// <returns></returns>
        private GameObject GenerateSyntheticRig(
            string rigName = "Generated_Rig",
            string transformRootName = "Root",
            string bonePrefix = "",
            BodyBoneNode bodyArmature = null,
            ChiralitySeparatorPosition chiralitySeparatorPosition = ChiralitySeparatorPosition.suffix,
            BoneChirality chirality = BoneChirality.LEFT,
            string leftChiralityName = "L",
            string rightChiralityName = "R",
            char separator = '_',
            bool hasElbow = false,
            string elbowName = "Elbow",
            string wristName = "Wrist",
            Dictionary<BoundFingerType, string>? fingers = null, // Annoyingly we cannot assign a default dictionary as its not a compile time constant 
            Dictionary<BoundFingerBoneType, string>? fingerBones = null,
            Dictionary<BoundFingerBoneType, string>? thumbBones = null,
            bool generateLeafBones = false)
        {
            GameObject root = new GameObject() { name = rigName };
            GameObject meshGO = new GameObject() { name = "HandMesh" };

            // We generate a dummy mesh, but this is not representative of a hand or body
            // it's just something to give to a SkinnedMeshRenderer
            GenerateDummyMesh(meshGO);
            meshGO.transform.parent = root.transform;

            var skinnedMeshRenderer = meshGO.AddComponent<SkinnedMeshRenderer>();
            skinnedMeshRenderer.sharedMesh = meshGO.GetComponent<MeshFilter>().sharedMesh;
            GameObject transformRoot = new GameObject() { name = transformRootName };
            transformRoot.transform.parent = root.transform;

            GameObject current = transformRoot;
            if (bodyArmature != null)
            {
                var value = GenerateBodyArmature(current);
                GenerateHand(value.leftArmRoot, boneChirality: BoneChirality.LEFT);
                GenerateHand(value.rightArmRoot, boneChirality: BoneChirality.RIGHT);
            }
            else
            {
                GenerateHand(current, chirality);
            }

            skinnedMeshRenderer.rootBone = transformRoot.transform;

            return root;

            void GenerateHand(GameObject currentRoot, BoneChirality boneChirality = BoneChirality.LEFT)
            {
                if (hasElbow)
                {
                    currentRoot = AddChildTransform(currentRoot, GenerateFullBoneNameLocal(elbowName, boneChirality), returnObjectAdded: true);
                }

                currentRoot = AddChildTransform(currentRoot, GenerateFullBoneNameLocal(wristName, boneChirality), returnObjectAdded: true);

                foreach (var finger in fingers)
                {
                    AddDigit(currentRoot, finger, boneChirality);
                }
            }

            void AddDigit(GameObject parentGO, KeyValuePair<BoundFingerType, string> currentFinger, BoneChirality boneChirality)
            {
                var bones = currentFinger.Key == BoundFingerType.THUMB ? thumbBones : fingerBones;

                string boneName = String.Empty;
                foreach (KeyValuePair<BoundFingerBoneType, string> fingerBone in bones)
                {
                    boneName = $"{currentFinger.Value}{separator}{fingerBone.Value}";
                    parentGO = AddChildTransform(parentGO, GenerateFullBoneNameLocal(boneName, boneChirality), returnObjectAdded: true);
                }

                if (generateLeafBones && !String.IsNullOrEmpty(boneName))
                {
                    AddChildTransform(parentGO, GenerateFullBoneNameLocal(boneName, boneChirality, "end"), true);
                }
            }

            (GameObject leftArmRoot, GameObject rightArmRoot) GenerateBodyArmature(GameObject parentGO)
            {
                GameObject leftArmRoot = new GameObject("Unset"), rightArmRoot = new GameObject("Unset");
                ProcessBodyNode(parentGO, bodyArmature, BoneChirality.NONE, ref leftArmRoot, ref rightArmRoot);
                return (leftArmRoot, rightArmRoot);
            }

            void ProcessBodyNode(GameObject parentGO, BodyBoneNode node, BoneChirality boneChirality, ref GameObject leftArmRoot, ref GameObject rightArmRoot)
            {
                switch (node.BoneType)
                {
                    case BodyArmatureBone.SHOULDER:
                    case BodyArmatureBone.HIP:
                        // Split the rig into two arms/legs
                        AddGameObjectAndProcessChildren(parentGO, node, boneChirality: BoneChirality.LEFT, ref leftArmRoot, ref rightArmRoot);
                        AddGameObjectAndProcessChildren(parentGO, node, boneChirality: BoneChirality.RIGHT, ref leftArmRoot, ref rightArmRoot);
                        break;

                    default:
                        var addedGO = AddGameObjectAndProcessChildren(parentGO, node, boneChirality, ref leftArmRoot, ref rightArmRoot);
                        break;
                }

                GameObject AddGameObjectAndProcessChildren(GameObject parentGO, BodyBoneNode node, BoneChirality boneChirality, ref GameObject leftArmRoot, ref GameObject? rightArmRoot)
                {
                    parentGO = AddChildTransform(parentGO, GenerateFullBoneNameLocal(node.BoneName, boneChirality), returnObjectAdded: true);

                    Debug.Log($"{node.BoneType} {hasElbow} {boneChirality}");
                    if ((node.BoneType == BodyArmatureBone.UPPER_ARM && hasElbow) ||
                         node.BoneType == BodyArmatureBone.LOWER_ARM && !hasElbow)
                    {
                        if (boneChirality == BoneChirality.LEFT)
                        {
                            Debug.Log($"Setting leftArmRootTo {parentGO.name}");
                            leftArmRoot = parentGO;
                        }
                        else if (boneChirality == BoneChirality.RIGHT)
                        {
                            rightArmRoot = parentGO;
                        }

                        return parentGO;
                    }

                    if (node.Children.Any())
                    {
                        foreach (var child in node.Children)
                        {
                            ProcessBodyNode(parentGO, child, boneChirality, ref leftArmRoot, ref rightArmRoot);
                        }
                    }
                    else
                    {
                        if (generateLeafBones)
                        {
                            AddChildTransform(parentGO, GenerateFullBoneNameLocal(node.BoneName, boneChirality, "end"), true);
                        }
                    }

                    return parentGO;
                }
            }

            string GenerateFullBoneNameLocal(string boneName, BoneChirality boneChirality, string endBoneName = "")
            {
                return GenerateFullBoneName(bonePrefix, boneName, boneChirality, chiralitySeparatorPosition, leftChiralityName, rightChiralityName, separator, endBoneName);
            }
        }

        string GenerateFullBoneName(string bonePrefix, string boneName, BoneChirality boneChirality, ChiralitySeparatorPosition chiralitySeparatorPosition, string leftChiralityName, string rightChiralityName, char separator, string endBoneName = "")
        {
            string chiralityString = String.Empty;

            switch (boneChirality)
            {
                case BoneChirality.LEFT:
                    chiralityString = chiralitySeparatorPosition == ChiralitySeparatorPosition.prefix ? $"{leftChiralityName}{separator}" : $"{separator}{leftChiralityName}";
                    break;
                case BoneChirality.RIGHT:
                    chiralityString = chiralitySeparatorPosition == ChiralitySeparatorPosition.prefix ? $"{rightChiralityName}{separator}" : $"{separator}{rightChiralityName}";
                    break;
                default:
                    break;
            }

            if (!String.IsNullOrEmpty(endBoneName) && !endBoneName.StartsWith(separator))
            {
                endBoneName = separator + endBoneName;
            }

            if (chiralitySeparatorPosition == ChiralitySeparatorPosition.prefix)
            {
                return $"{bonePrefix}{chiralityString}{boneName}{endBoneName}";
            }
            else
            {
                return $"{bonePrefix}{boneName}{endBoneName}{chiralityString}";
            }
        }

        private GameObject AddChildTransform(GameObject parentGO, string goName, bool returnObjectAdded = true)
        {
            var newGO = new GameObject() { name = goName };
            newGO.transform.parent = parentGO.transform;
            if (returnObjectAdded)
            {
                return newGO;
            }
            else
            {
                return parentGO;
            }
        }

        private void GenerateDummyMesh(GameObject meshGO)
        {
            float width = 1, height = 1;
            MeshRenderer meshRenderer = meshGO.AddComponent<MeshRenderer>();
            meshRenderer.sharedMaterial = new Material(Shader.Find("Standard"));

            MeshFilter meshFilter = meshGO.AddComponent<MeshFilter>();

            Mesh mesh = new Mesh();

            Vector3[] vertices = new Vector3[4]
            {
                new Vector3(0, 0, 0),
                new Vector3(width, 0, 0),
                new Vector3(0, height, 0),
                new Vector3(width, height, 0)
            };
            mesh.vertices = vertices;

            int[] tris = new int[6]
            {
                // lower left triangle
                0, 2, 1,
                // upper right triangle
                2, 3, 1
            };
            mesh.triangles = tris;

            Vector3[] normals = new Vector3[4]
            {
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward,
                -Vector3.forward
            };
            mesh.normals = normals;

            Vector2[] uv = new Vector2[4]
            {
                new Vector2(0, 0),
                new Vector2(1, 0),
                new Vector2(0, 1),
                new Vector2(1, 1)
            };
            mesh.uv = uv;
            meshFilter.mesh = mesh;
        }

        private string FindAssetPath(string assetName)
        {
            var allAssetPaths = AssetDatabase.GetAllAssetPaths();
            var fileName = "Rig40.prefab";
            for (int i = 0; i < allAssetPaths.Length; ++i)
            {
                if (allAssetPaths[i].EndsWith(fileName))
                    return allAssetPaths[i];
            }

            return string.Empty;
        }

        void BuildTransformHierarchyString(GameObject currentRoot, ref List<bool> lastChild, ref StringBuilder output)
        {
            Append(currentRoot, ref lastChild, ref output);

            var children = currentRoot.transform.GetChildren();

            int childCount = 0;
            foreach (var child in children)
            {
                childCount++;
            }

            int currentChildIndex = 1;
            lastChild.Add(false);

            foreach (var child in children)
            {
                if (currentChildIndex == childCount)
                {
                    lastChild[lastChild.Count - 1] = true;
                }

                BuildTransformHierarchyString(child.gameObject, ref lastChild, ref output);
                currentChildIndex++;
            }

            lastChild.RemoveAt(lastChild.Count - 1);

            //⌞_|___ 
            void Append(GameObject go, ref List<bool> lastChild, ref StringBuilder output)
            {
                string depthString = String.Empty;

                int index = 0;

                foreach (var lastChildAtDepth in lastChild)
                {
                    if (index == lastChild.Count - 1)
                    {
                        depthString += "'---";
                    }
                    else
                    {
                        if (lastChildAtDepth)
                        {
                            depthString += "    ";
                        }
                        else
                        {
                            depthString += "|   ";
                        }
                    }

                    index++;
                }
                output.AppendLine($"{depthString}----> {go.name}");
            }
        }
    }


    internal enum BodyArmatureBone
    {
        SPINE,
        PELVIS,
        CHEST,
        HIP,
        KNEE,
        ANKLE,
        TOE,
        SHOULDER,
        UPPER_ARM,
        LOWER_ARM,
        NECK,
        HEAD
    };

    internal enum BoneChirality
    {
        NONE,
        LEFT,
        RIGHT
    }

    // See https://docs.blender.org/manual/en/4.0/animation/armatures/bones/editing/naming.html#armature-editing-naming-conventions
    internal enum ChiralitySeparatorPosition
    {
        prefix,
        suffix
    }

    /// <summary>
    /// Utility class for generating hand digit names based on requested options
    /// </summary>
    internal static class DigitNameListGenerator
    {
        internal static readonly Dictionary<BoundFingerBoneType, string> DefaultNamedFingerBones_AllBones = new Dictionary<BoundFingerBoneType, string>()
        {
            { BoundFingerBoneType.METACARPAL, "Meta" },
            { BoundFingerBoneType.PROXIMAL, "Proximal"},
            { BoundFingerBoneType.INTERMEDIATE, "Intermediate"},
            { BoundFingerBoneType.DISTAL, "Distal"},
            { BoundFingerBoneType.TIP, "Tip"}
        };

        internal static readonly Dictionary<BoundFingerBoneType, string> DefaultNamedThumbBones_AllBones = new Dictionary<BoundFingerBoneType, string>()
        {
            { BoundFingerBoneType.METACARPAL, "Meta" },
            { BoundFingerBoneType.PROXIMAL, "Proximal"},
            { BoundFingerBoneType.DISTAL, "Distal"},
            { BoundFingerBoneType.TIP, "Tip"}
        };

        internal enum DigitNamingOptions
        {
            DefaultBoneNames,
            NumericList,
            AlphabeticallyList
        }

        internal static Dictionary<BoundFingerBoneType, string> Generate(bool isThumb, DigitNamingOptions boneNameConvention, bool generateMetacarpals, bool generateTips)
        {
            var list = new Dictionary<BoundFingerBoneType, string>();

            var toGenerate = new List<BoundFingerBoneType>();

            if (isThumb || generateMetacarpals)
            {
                toGenerate.Add(BoundFingerBoneType.METACARPAL);
            }

            if (isThumb)
            {
                toGenerate.AddRange(new List<BoundFingerBoneType>() { BoundFingerBoneType.PROXIMAL, BoundFingerBoneType.DISTAL });
            }
            else
            {
                toGenerate.AddRange(new List<BoundFingerBoneType>() { BoundFingerBoneType.PROXIMAL, BoundFingerBoneType.INTERMEDIATE, BoundFingerBoneType.DISTAL });
            }

            if (generateTips)
            {
                toGenerate.Add(BoundFingerBoneType.TIP);
            }

            int index = 0;
            foreach (var bone in toGenerate)
            {
                list.Add(bone, GetBoneName(index++,bone));
            }

            return list;

            string GetBoneName(int index, BoundFingerBoneType bone)
            {
                switch (boneNameConvention)
                {
                    case DigitNamingOptions.NumericList:
                        return $"{index}";

                    case DigitNamingOptions.AlphabeticallyList:
                        return char.ToString((char)(index + 97)); // Lowercase a = char value of 97

                    case DigitNamingOptions.DefaultBoneNames:
                    default:
                        return DefaultNamedFingerBones_AllBones[bone];
                }
            }
        }
    }
}
