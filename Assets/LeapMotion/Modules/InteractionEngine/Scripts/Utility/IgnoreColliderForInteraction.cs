/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2018.                                 *
 * Leap Motion proprietary and confidential.                                  *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;

/// <summary>
/// Causes any Colliders located on the same GameObject to be ignored by the Interaction
/// Engine. Does not affect parents or children. It is recommended that you use this
/// component only with trigger colliders.
/// 
/// Colliders are still be visible to PhysX, so they can be used, for example, for
/// collision against scene geometry, or for raycasting. However, the Interaction Engine
/// will not allow Interaction Objects to be grasped or hovered by checks between hands
/// and this collider, and soft-contact forces will not be applied on this
/// collider.
/// 
/// There is one important caveat: If the colliders are NOT triggers, they may still
/// experience depenetration forces from PhysX against hand or controller colliders while
/// those colliders are not in soft-contact mode (which activates when hands or colliders
/// intersect objects beyond a certain depth). This inconsistency is immaterial when the
/// ignored collider is a trigger.
/// </summary>
public class IgnoreColliderForInteraction : MonoBehaviour { }
