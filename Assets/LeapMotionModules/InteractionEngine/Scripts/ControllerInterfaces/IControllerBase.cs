/******************************************************************************
 * Copyright (C) Leap Motion, Inc. 2011-2017.                                 *
 * Leap Motion proprietary and  confidential.                                 *
 *                                                                            *
 * Use subject to the terms of the Leap Motion SDK Agreement available at     *
 * https://developer.leapmotion.com/sdk_agreement, or another agreement       *
 * between Leap Motion and you, your company or other organization.           *
 ******************************************************************************/

using UnityEngine;
using System.Diagnostics;

namespace Leap.Unity.Interaction {

  /** 
  * The IControllerBase interface defines the controller functions
  * used by the Interaction Engine to construct and initialize instances
  * of a controller implementation.
  *
  * IControllerBase is the base class for the controllers used by 
  * the InteractionMaterial to implement various aspects of hand-to-object interaction.
  *
  * @since 4.1.4
  */
  public abstract class IControllerBase : ScriptableObject {
    protected InteractionBehaviour _obj;

    /**
    * Construct a controller instance given an InteractionBehavior object.
    * @since 4.1.4
    */
    public static T CreateInstance<T>(InteractionBehaviour obj) where T : IControllerBase {
      T controller = CreateInstance<T>();
      controller.Init(obj);
      return controller;
    }

    /**
    * Construct a controller instance given an InteractionBehavior object and another instance
    * to use as a template.
    * @since 4.1.4
    */
    public static T CreateInstance<T>(InteractionBehaviour obj, T template) where T : IControllerBase {
      if (template == null) {
        return null;
      }

      T controller = Instantiate(template);
      controller.Init(obj);
      return controller;
    }

    /**
    * Called when the Interaction Engine wants to validate the internal state of the controller
    * instance. 
    * @since 4.1.4
    */
    [Conditional("UNITY_ASSERTIONS")]
    public virtual void Validate() { }

    /**
    * Initialize the controller given an InteractionBehavior object.
    * @since 4.1.4
    */
    protected virtual void Init(InteractionBehaviour obj) {
      _obj = obj;
    }
  }
}
