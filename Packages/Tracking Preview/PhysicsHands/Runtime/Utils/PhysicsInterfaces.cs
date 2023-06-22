namespace Leap.Unity.Interaction.PhysicsHands
{
    /// <summary>
    /// Reports the hover event, called when a grasp helper is created for the hand.
    /// This is a relatively low accuracy hover.
    /// </summary>
    public interface IPhysicsHandHover
    {
        void OnHandHover(PhysicsHand hand);
        void OnHandHoverExit(PhysicsHand hand);
    }

    /// <summary>
    /// Reports the hover event called when an individual bone hovers your rigidbody.
    /// This will be called dependant on the hover distance set by the hand.
    /// </summary>
    public interface IPhysicsBoneHover
    {
        void OnBoneHover(PhysicsBone bone);
        void OnBoneHoverExit(PhysicsBone bone);
    }

    /// <summary>
    /// Reports the contact event when any part of the hand is in contact with an interactable rigidbody.
    /// This will be called dependant on the contact distance set by the hand.
    /// </summary>
    public interface IPhysicsHandContact
    {
        void OnHandContact(PhysicsHand hand);
        void OnHandContactExit(PhysicsHand hand);
    }

    /// <summary>
    /// Reports the contact event when an an individual bone hovers your rigidbody.
    /// This will be called dependant on the contact distance set by the hand.
    /// </summary>
    public interface IPhysicsBoneContact
    {
        void OnBoneContact(PhysicsBone bone);
        void OnBoneContactExit(PhysicsBone bone);
    }

    /// <summary>
    /// Reports the grab event when a grasp helper starts or stops grasping a rigidbody.
    /// </summary>
    public interface IPhysicsHandGrab
    {
        void OnHandGrab(PhysicsHand hand);
        void OnHandGrabExit(PhysicsHand hand);
    }
}