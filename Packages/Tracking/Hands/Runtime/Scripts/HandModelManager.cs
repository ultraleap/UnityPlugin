using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text.RegularExpressions;
using UnityEngine;

namespace Leap.Unity.HandsModule
{
    /// <summary>
    /// A script to manage the active hand models in the scene
    /// </summary>
    public class HandModelManager : MonoBehaviour
    {
        [System.Serializable]
        public class HandModelPair
        {
            public string HandModelPairId;
            public HandModelBase Left;
            public HandEnableDisable LeftEnableDisable;
            public HandModelBase Right;
            public HandEnableDisable RightEnableDisable;

            public bool ContainsHandModel(HandModelBase handModel)
            {
                return Left == handModel || Right == handModel;
            }
        }

        public Action<HandModelBase> OnHandModelEnabled;
        public Action<HandModelBase> OnHandModelDisabled;

        public List<HandModelPair> handModelPairs;

        private void Reset()
        {
            handModelPairs = new List<HandModelPair>();
            RegisterAllUnregisteredHandModels();
        }

        /// <summary>
        /// Find any unregistered hand models, and register them with the hand model manager
        /// </summary>
        public void RegisterAllUnregisteredHandModels()
        {
            HandModelBase[] unpairedHandModels = FindObjectsOfType<HandModelBase>(true);

            for (int i = 0; i < unpairedHandModels.Length; i++)
            {
                if (!IsRegistered(unpairedHandModels[i]))
                {
                    RegisterHandModel(unpairedHandModels[i]);
                }
            }
        }

        /// <summary>
        /// Check if a hand model is registered with the Hand Model Manager
        /// </summary>
        /// <param name="handModel">The hand model to check</param>
        /// <returns>Returns true if the hand model is registered, and false if not</returns>
        public bool IsRegistered(HandModelBase handModel)
        {
            for (int i = 0; i < handModelPairs.Count; i++)
            {
                if (handModelPairs[i].ContainsHandModel(handModel))
                {
                    return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Registers a hand model with the Hand Model Manager.
        /// </summary>
        /// <param name="handModel"> The hand model to pair</param>
        /// <param name="attemptToRegisterPair"> if true, Hand Model Manager will attempt to pair it with another hand model</param>
        public void RegisterHandModel(HandModelBase handModel, bool attemptToRegisterPair = true)
        {
            int handModelPairIndex = GenerateHandModelIndex(handModel, attemptToRegisterPair);

            RegisterHandModel(handModel, handModelPairIndex);
        }

        /// <summary>
        /// Registers a hand model with the provided handModelPairIndex
        /// </summary>
        /// <param name="handModel">The hand model to register</param>
        /// <param name="handModelPairIndex"></param>
        private void RegisterHandModel(HandModelBase handModel, int handModelPairIndex)
        {
            HandEnableDisable handEnableDisable = handModel.GetComponent<HandEnableDisable>();
            if (handEnableDisable == null)
            {
                Debug.LogWarning("Registered a hand model without HandEnableDisable");
            }

            if (handModel.Handedness == Chirality.Left)
            {
                handModelPairs[handModelPairIndex].Left = handModel;
                handModelPairs[handModelPairIndex].LeftEnableDisable = handEnableDisable;
            }
            else
            {
                handModelPairs[handModelPairIndex].Right = handModel;
                handModelPairs[handModelPairIndex].RightEnableDisable = handEnableDisable;
            }

            if (handModelPairs[handModelPairIndex].HandModelPairId == null || handModelPairs[handModelPairIndex].HandModelPairId == "")
            {
                handModelPairs[handModelPairIndex].HandModelPairId = GetDistinctHandModelId(handModel.gameObject.transform.parent.name);
            }
        }

        protected string GetDistinctHandModelId(string desiredHandModelId)
        {
            int iteration = 0;
            var name = desiredHandModelId;
            List<string> existingIds = handModelPairs.Select(hmp => hmp.HandModelPairId).ToList();

            while (existingIds.Contains(name))
            {
                name = desiredHandModelId + "_[" + (iteration++) + "]";
            }

            return name;
        }

        /// <summary>
        /// Generates a hand model index to register with the hand model list
        /// If attempting to find a pair & the pair is already registered, uses that pair's index
        /// Otherwise assigns the next index in the list
        /// </summary>
        /// <param name="handModel">the hand model to register</param>
        /// <param name="attemptToFindPair">if true, attempts to find a pair & register it if unregistered</param>
        /// <returns>The correct hand model index</returns>
        private int GenerateHandModelIndex(HandModelBase handModel, bool attemptToFindPair)
        {
            int handModelPairIndex = handModelPairs.Count;

            if (!attemptToFindPair)
            {
                handModelPairs.Add(new HandModelPair());
            }

            // Look for hand model pair in previously registered hands
            for (int i = 0; i < handModelPairs.Count; i++)
            {
                HandModelBase potentialRegisteredHandPair;

                if (handModel.Handedness == Chirality.Left)
                {
                    potentialRegisteredHandPair = handModelPairs[i].Right;
                }
                else
                {
                    potentialRegisteredHandPair = handModelPairs[i].Left;
                }

                if (handModel.gameObject.transform.parent == potentialRegisteredHandPair.gameObject.transform.parent)
                {
                    handModelPairIndex = i;
                    return handModelPairIndex;
                }
            }

            //If not found in previously registered hands, look for in the parent transform
            HandModelBase potentialHandPair = handModel.gameObject.transform.parent.GetComponentsInChildren<HandModelBase>().FirstOrDefault(hmb => hmb != handModel);

            handModelPairs.Add(new HandModelPair());

            //Register the found pair, then return the pair index
            if (potentialHandPair != null)
            {
                RegisterHandModel(potentialHandPair, handModelPairIndex);
            }

            return handModelPairIndex;
        }

        /// <summary>
        /// Unregisters a hand with the Hand Model Manager.
        /// If the HandModelPair no longer has a left and right hand it will be removed from the HandModelPair list
        /// </summary>
        /// <param name="handModel">The hand model to unregister</param>
        public void UnregisterHandModel(HandModelBase handModel)
        {
            for (int i = 0; i < handModelPairs.Count; i++)
            {
                if (handModelPairs[i].ContainsHandModel(handModel))
                {
                    if (handModel.Handedness == Chirality.Left)
                    {
                        handModelPairs[i].Left = null;
                    }
                    else
                    {
                        handModelPairs[i].Right = null;
                    }

                    if (handModelPairs[i].Left == null && handModelPairs[i].Right == null)
                    {
                        handModelPairs.RemoveAt(i);
                    }
                    return;
                }
            }
        }

        #region Getters

        /// <summary>
        /// Returns all hands which will be active if a hand appears in the scene
        /// This includes hands whose EnableDisable is not frozen, or are frozen in an active state
        /// </summary>
        /// <returns>Returns all hands which will be active if a hand appears in the scene</returns>
        public List<HandModelBase> GetActiveHandsInScene()
        {
            List<HandModelBase> activeHands = new List<HandModelBase>();
            foreach (HandModelPair handModelPair in handModelPairs)
            {
                if (!handModelPair.LeftEnableDisable.FreezeHandState ||
                    handModelPair.LeftEnableDisable.FreezeHandState && handModelPair.Left.isActiveAndEnabled)
                {
                    activeHands.Add(handModelPair.Left);
                }

                if (!handModelPair.RightEnableDisable.FreezeHandState ||
                    handModelPair.RightEnableDisable.FreezeHandState && handModelPair.Right.isActiveAndEnabled)
                {
                    activeHands.Add(handModelPair.Right);
                }
            }
            return activeHands;
        }

        /// <summary>
        /// Returns a hand model pair by index, if given a valid index
        /// </summary>
        /// <param name="index">The index of the hand model pair</param>
        /// <param name="handModelPair">The hand model pair to be returned</param>
        /// <returns>Returns true if the pair have been found, and false if not</returns>
        public bool GetHandModelPair(int index, out HandModelPair handModelPair)
        {
            handModelPair = null;
            if (index >= 0 && index < handModelPairs.Count)
            {
                handModelPair = handModelPairs[index];
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a hand model pair by index, if given a valid index
        /// </summary>
        /// <param name="handModelPairID">The handModelPairID of the hand model pair</param>
        /// <param name="handModelPair">The hand model pair to be returned</param>
        /// <returns>Returns true if the pair have been found, and false if not</returns>
        public bool GetHandModelPair(string handModelPairID, out HandModelPair handModelPair)
        {
            handModelPair = null;
            if (handModelPairID != null)
            {
                handModelPair = handModelPairs.FirstOrDefault(hmp => hmp.HandModelPairId == handModelPairID);
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a hand model by index, if given a valid index
        /// </summary>
        /// <param name="chirality">The chirality of the hand model</param>
        /// <param name="index">The index of the hand model</param>
        /// <param name="handModelBase">The hand model to be returned</param>
        /// <returns>Returns true if the pair have been found, and false if not</returns>
        public bool GetHandModel(Chirality chirality, int index, out HandModelBase handModelBase)
        {
            handModelBase = null;

            HandModelPair handModelPair;
            bool success = GetHandModelPair(index, out handModelPair);
            if (success)
            {
                if (chirality == Chirality.Left)
                {
                    handModelBase = handModelPair.Left;
                }
                else
                {
                    handModelBase = handModelPair.Right;
                }
                return true;
            }
            return false;
        }

        /// <summary>
        /// Returns a hand model by index, if given a valid index
        /// </summary>
        /// <param name="chirality">The chirality of the hand model</param>
        /// <param name="handModelPairID">The handModelPairID of the hand model</param>
        /// <param name="handModelBase">The hand model to be returned</param>
        /// <returns>Returns true if the pair have been found, and false if not</returns>
        public bool GetHandModel(Chirality chirality, string handModelPairID, out HandModelBase handModelBase)
        {
            handModelBase = null;

            HandModelPair handModelPair;
            bool success = GetHandModelPair(handModelPairID, out handModelPair);
            if (success)
            {
                if (chirality == Chirality.Left)
                {
                    handModelBase = handModelPair.Left;
                }
                else
                {
                    handModelBase = handModelPair.Right;
                }
                return true;
            }
            return false;
        }
        #endregion

        #region EnableHandModels
        /// <summary>
        /// Enable a hand model pair in the scene
        /// </summary>
        /// <param name="index">The index of the pair to enable</param>
        /// <param name="disableOtherHands">If true, disable all other active hands</param>
        public void EnableHandModelPair(int index, bool disableOtherHands = true)
        {
            EnableHandModel(Chirality.Left, index, disableOtherHands);
            EnableHandModel(Chirality.Right, index, disableOtherHands);
        }

        /// <summary>
        /// Enable a hand model pair in the scene
        /// </summary>
        /// <param name="handModelPairID">The string id of the hand model pair to enable</param>
        /// <param name="disableOtherHands">If true, disable all other active hand models</param>
        public void EnableHandModelPair(string handModelPairID, bool disableOtherHands = true)
        {
            EnableHandModelPair(Chirality.Left, handModelPairID, disableOtherHands);
            EnableHandModelPair(Chirality.Right, handModelPairID, disableOtherHands);
        }

        /// <summary>
        /// Enable a single hand model in the scene, based on chirality
        /// </summary>
        /// <param name="chirality">The chirality to enable</param>
        /// <param name="index">The index of the hand model to enable</param>
        /// <param name="disableOtherHandsOfSameChirality">If true, disable all other active hand models of the same chirality</param>
        public void EnableHandModel(Chirality chirality, int index, bool disableOtherHandsOfSameChirality = true)
        {
            HandModelPair handModelPair = handModelPairs[index];
            EnableHandModel(chirality, handModelPair, disableOtherHandsOfSameChirality);
        }

        /// <summary>
        /// Enable a single hand model in the scene, based on chirality
        /// </summary>
        /// <param name="chirality">The chirality to enable</param>
        /// <param name="handModelPairID">The pair id of the hand model to enable</param>
        /// <param name="disableOtherHandsOfSameChirality">If true, disable all other active hand models of the same chirality</param>
        public void EnableHandModelPair(Chirality chirality, string handModelPairID, bool disableOtherHandsOfSameChirality = true)
        {
            HandModelPair handModelPair = handModelPairs.ToList().Where(hmp => hmp.HandModelPairId == handModelPairID).FirstOrDefault();
            EnableHandModel(chirality, handModelPair, disableOtherHandsOfSameChirality);
        }

        /// <summary>
        /// Enable a single hand model in the scene, based on chirality
        /// </summary>
        /// <param name="chirality">The chirality to enable</param>
        /// <param name="handModelPair">The pair to enable</param>
        /// <param name="disableOtherHandsOfSameChirality">If true, disable all other active hand models of the same chirality</param>
        private void EnableHandModel(Chirality chirality, HandModelPair handModelPair, bool disableOtherHandsOfSameChirality)
        {
            if (disableOtherHandsOfSameChirality)
            {
                DisableAllHandModelsByChirality(chirality);
            }

            if (chirality == Chirality.Left)
            {
                if (handModelPair.LeftEnableDisable != null)
                {
                    handModelPair.LeftEnableDisable.FreezeHandState = false;
                }
                if (handModelPair.Left != null)
                {
                    if (handModelPair.Left.IsTracked)
                    {
                        handModelPair.Left.gameObject.SetActive(true);
                    }
                    OnHandModelEnabled?.Invoke(handModelPair.Left);
                }
            }
            else
            {
                if (handModelPair.RightEnableDisable != null)
                {
                    handModelPair.RightEnableDisable.FreezeHandState = false;
                }

                if (handModelPair.Right != null && handModelPair.Right.IsTracked)
                {
                    if (handModelPair.Left.IsTracked)
                    {
                        handModelPair.Right.gameObject.SetActive(true);
                    }
                    OnHandModelEnabled?.Invoke(handModelPair.Right);
                }
            }
        }
        #endregion

        #region DisableHandModels
        /// <summary>
        /// Disables all hand models of a certain chirality
        /// </summary>
        /// <param name="chirality">The chirality by which to disable</param>
        private void DisableAllHandModelsByChirality(Chirality chirality)
        {
            for (int i = 0; i < handModelPairs.Count; i++)
            {
                DisableHandModel(chirality, handModelPairs[i]);
            }
        }

        /// <summary>
        /// Disables a hand model pair
        /// </summary>
        /// <param name="index">The index of the pair to disable</param>
        public void DisableHandModelPair(int index)
        {
            DisableHandModel(Chirality.Left, index);
            DisableHandModel(Chirality.Right, index);
        }

        /// <summary>
        /// Disables a hand model pair
        /// </summary>
        /// <param name="handModelPairId">The hand model pair id of the pair to disable</param>
        public void DisableHandModelPair(string handModelPairId)
        {
            DisableHandModel(Chirality.Left, handModelPairId);
            DisableHandModel(Chirality.Right, handModelPairId);
        }

        /// <summary>
        /// Disables a hand model based on its chirality
        /// </summary>
        /// <param name="chirality">The chirality by which to disable</param>
        /// <param name="index">The index of the model to disable</param>
        public void DisableHandModel(Chirality chirality, int index)
        {
            HandModelPair handModelPair = handModelPairs[index];
            DisableHandModel(chirality, handModelPair);
        }

        /// <summary>
        /// Disables a hand model based on its chirality
        /// </summary>
        /// <param name="chirality">The chirality by which to disable</param>
        /// <param name="handModelPairId">The model pair id of the model to disable</param>
        public void DisableHandModel(Chirality chirality, string handModelPairId)
        {
            HandModelPair handModelPair = handModelPairs.ToList().Where(hmp => hmp.HandModelPairId == handModelPairId).FirstOrDefault();
            DisableHandModel(chirality, handModelPair);
        }

        /// <summary>
        /// Disables a hand model based on its chirality
        /// </summary>
        /// <param name="chirality">The chirality by which to disable</param>
        /// <param name="handModelPair">The hand model pair of which to disable a member of</param>
        private void DisableHandModel(Chirality chirality, HandModelPair handModelPair)
        {
            if (chirality == Chirality.Left)
            {
                if (handModelPair.Left != null)
                {
                    handModelPair.Left.gameObject.SetActive(false);
                    OnHandModelDisabled?.Invoke(handModelPair.Left);
                }
                if (handModelPair.LeftEnableDisable != null)
                {
                    handModelPair.LeftEnableDisable.FreezeHandState = true;
                }
            }
            else
            {
                if (handModelPair.Right != null)
                {
                    handModelPair.Right.gameObject.SetActive(false);
                    OnHandModelDisabled?.Invoke(handModelPair.Right);
                }
                if (handModelPair.RightEnableDisable != null)
                {
                    handModelPair.RightEnableDisable.FreezeHandState = true;
                }
            }
        }
        #endregion
    }
}
