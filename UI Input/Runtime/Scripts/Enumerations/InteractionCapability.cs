namespace Leap.Unity.InputModule
{
    /// Defines the interaction modes :
    ///
    /// - Hybrid: Both tactile and projective interaction. The active mode depends on the ProjectiveToTactileTransitionDistance value.
    /// - Tactile: The user must physically touch the controls.
    /// - Projective: A cursor is projected from the user's knuckle.
    public enum InteractionCapability  // Bitfield might be better here with Hybrid being both bits set ....
    {
        Hybrid,
        Tactile,
        Projective
    };
}
