namespace Content.Server.Imperial.NGAntag.Components;

[RegisterComponent]
public sealed partial class CurseGiftComponent : Component
{
    [DataField]
    public TimeSpan CurseDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public List<string> GiftGoal = new() { "PresentRandomUnsafeNG3" };

    [DataField]
    public float MaxCurseRange = 2.5f;

    [DataField]
    public EntityUid OwnerGift;
}
