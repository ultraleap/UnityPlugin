namespace Leap.Unity.InputModule
{
    /// <summary>
    /// Defines the interaction modes :
    /// - Both: Both direct and indirect interaction. The active mode depends on the ProjectiveToTactileTransitionDistance value.
    /// - Direct: The user must physically touch the controls.
    /// - Indirect: A cursor is projected from the user's knuckle.
    /// </summary>
    public enum InteractionCapability  // Bitfield might be better here with Hybrid being both bits set ....
    {
        Both,
        Direct,
        Indirect
    };
}
