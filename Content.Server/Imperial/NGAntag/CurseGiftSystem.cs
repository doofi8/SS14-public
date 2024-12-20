using Content.Server.Imperial.NGAntag.Components;
using Content.Server.Chat.Systems;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Imperial.NGAntag.Events;
using Content.Shared.Body.Components;
using Content.Shared.Popups;
using Content.Shared.Inventory;
using Content.Shared.Actions;
using Content.Shared.Timing;
using Content.Shared.Interaction.Events;
using Robust.Shared.Prototypes;
using Content.Server.Antag;
using Robust.Shared.Player;

namespace Content.Server.Imperial.NGAntag;

public sealed class CurseGiftSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doAfterSystem = default!;
    [Dependency] private readonly NGAntagRuleSystem _ngAntagRuleSystem = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
    [Dependency] private readonly ChatSystem _chat = default!;
    [Dependency] private readonly UseDelaySystem _delay = default!;
    [Dependency] private readonly AntagSelectionSystem _antag = default!;


    [ValidatePrototypeId<EntityPrototype>]
    private const string DefaultNGAntagRule = "NGAntag";
    public override void Initialize()
    {
        SubscribeLocalEvent<CurseGiftComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<CurseGiftComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<CurseGiftComponent, BecomeNGAntagDoAfterEvent>(OnDoAfter);
    }

    private void OnAfterInteract(Entity<CurseGiftComponent> uid, ref AfterInteractEvent args)
    {
        if (args.Target == null || !args.CanReach || !HasComp<BodyComponent>(args.Target))
        {
            return;
        }

        if (!TryComp<NGAntagComponent>(args.User, out var compRole) || uid.Comp.OwnerGift != args.User)
        {
            _popup.PopupEntity(Loc.GetString("cursed-gift-cancellation-you"), args.User, args.User);
            return;
        }

        if (compRole.Target != args.Target.Value)
        {
            _popup.PopupEntity(Loc.GetString("cursed-gift-cancellation-target"), args.User, args.User);
            return;
        }

        if (TryComp<NGAntagComponent>(args.Target.Value, out var compTarget) && compTarget.LastConveyed != args.User)
        {
            _popup.PopupEntity(Loc.GetString("cursed-gift-target-already-curse"), args.User, args.User);
            return;
        }

        _popup.PopupEntity(Loc.GetString("cursed-gift-alert"), args.Target.Value, args.Target.Value, PopupType.Large);

        var doAfterCancelled = !_doAfterSystem.TryStartDoAfter(new DoAfterArgs(EntityManager, args.User, uid.Comp.CurseDelay, new BecomeNGAntagDoAfterEvent(), uid, target: args.Target, used: uid)
        {
            NeedHand = true,
            BreakOnDamage = true,
        });

        if (args.Target == args.User || doAfterCancelled)
            return;
    }

    private void OnDoAfter(Entity<CurseGiftComponent> uid, ref BecomeNGAntagDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target == null)
            return;

        BeginCurseEntity(uid, args.Target.Value);

        if (TryComp<NGAntagComponent>(args.Target.Value, out var compTarget))
        {
            compTarget.LastConveyed = args.User;
        }

        if (TryComp<NGAntagComponent>(args.User, out var compRole))
        {
            EntityManager.DeleteEntity(compRole.LastCursedGift);
            compRole.TargetGoaled = true;
            compRole.LastCursedGift = null;
        }

        if (TryComp<ActionsContainerComponent>(args.User, out var compActionCont))
        {
            foreach (var action in compActionCont.Container.ContainedEntities)
            {
                if (TryComp<MetaDataComponent>(action, out var metaComp))
                {
                    if (metaComp.EntityPrototype != null && (metaComp.EntityPrototype.ID == "ActionSpawnCursedGift"
                                                             || metaComp.EntityPrototype.ID == "ActionTryToFindTarget"))
                    {
                        _actionsSystem.RemoveAction(action);
                    }
                }
            }
        }

        _inventory.SpawnItemsOnEntity(args.User, uid.Comp.GiftGoal);

        _popup.PopupEntity(Loc.GetString("cursed-gift-goal"), args.User, args.User, PopupType.Large);
        args.Handled = true;
    }

    private void OnUseInHand(EntityUid uid, CurseGiftComponent component, UseInHandEvent args)
    {
        if (!TryComp(component.Owner, out UseDelayComponent? useDelay) || _delay.IsDelayed((component.Owner, useDelay)))
            return;

        if (args.User != component.OwnerGift)
        {
            _chat.TrySendInGameICMessage(component.Owner, Loc.GetString("cursed-gift-say-not-owner"), InGameICChatType.Speak, true);
            _delay.TryResetDelay((component.Owner, useDelay));
            return;
        }

        if (TryComp<NGAntagComponent>(args.User, out var userComp) && args.User == component.OwnerGift && userComp.Target != null)
        {
            int minutes = userComp.CurseTimer.Minutes;
            int seconds = userComp.CurseTimer.Seconds;

            string GetMinuteWord(int minute)
            {
                if (minute % 10 == 1 && minute % 100 != 11)
                    return "минуту";
                else if (minute % 10 >= 2 && minute % 10 <= 4 && (minute % 100 < 10 || minute % 100 >= 20))
                    return "минуты";
                else
                    return "минут";
            }

            string GetSecondWord(int second)
            {
                if (second % 10 == 1 && second % 100 != 11)
                    return "секунду";
                else if (second % 10 >= 2 && second % 10 <= 4 && (second % 100 < 10 || second % 100 >= 20))
                    return "секунды";
                else
                    return "секунд";
            }

            string minuteWord = GetMinuteWord(minutes);
            string secondWord = GetSecondWord(seconds);

            _chat.TrySendInGameICMessage(component.Owner,
                Loc.GetString("cursed-gift-say-timer") + $" {minutes} {minuteWord} и {seconds} {secondWord}!",
                InGameICChatType.Speak, true);

            _delay.TryResetDelay((component.Owner, useDelay));
            return;
        }

        _chat.TrySendInGameICMessage(component.Owner, Loc.GetString("cursed-gift-say-no-target"), InGameICChatType.Speak, true);
        _delay.TryResetDelay((component.Owner, useDelay));
        return;
    }

    private void BeginCurseEntity(Entity<CurseGiftComponent> curseGift, EntityUid target)
    {
        if (!TryComp<ActorComponent>(target, out var comp))
            return;

        _antag.ForceMakeAntag<NGAntagRuleComponent>(comp.PlayerSession, DefaultNGAntagRule);
    }

}
