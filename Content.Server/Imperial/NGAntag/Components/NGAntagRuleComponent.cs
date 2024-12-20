using Content.Shared.Roles;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server.Imperial.NGAntag.Components;

[RegisterComponent]
public sealed partial class NGAntagRuleComponent : Component
{
    [DataField]
    public ProtoId<AntagPrototype> NGAntagPrototypeId = "NGAntag";

    [DataField]
    public SoundSpecifier? GreetingSound = new SoundPathSpecifier("/Audio/Imperial/NG/NGAntag/ngAntag_greeting.ogg");
}
