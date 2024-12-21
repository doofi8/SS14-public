namespace Content.Server.Imperial.NGAntag.Components
{
    [RegisterComponent]
    public sealed partial class NGAntagTargetComponent : Component
    {
        [DataField]
        public bool Claimed = false;
    }
}
