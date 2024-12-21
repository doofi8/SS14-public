using Content.Shared.DoAfter;
using Robust.Shared.Serialization;

namespace Content.Shared.Imperial.NGAntag.Events
{
    [Serializable, NetSerializable]
    public sealed partial class BecomeNGAntagDoAfterEvent : SimpleDoAfterEvent
    {
    }
}
