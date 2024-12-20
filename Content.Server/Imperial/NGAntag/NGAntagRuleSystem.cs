using Content.Server.Antag;
using Content.Server.GameTicking.Rules;
using Content.Server.Imperial.NGAntag.Components;
using Content.Server.Mind;
using Content.Server.Objectives;
using Content.Server.Roles;
using Content.Server.Ghost;
using Content.Shared.Imperial.NGAntag.Events;
using Content.Shared.Mind;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Popups;
using Content.Shared.Actions;
using Content.Shared.Throwing;
using Robust.Shared.Random;

namespace Content.Server.Imperial.NGAntag
{
    public sealed class NGAntagRuleSystem : GameRuleSystem<NGAntagRuleComponent>
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly AntagSelectionSystem _antag = default!;
        [Dependency] private readonly MindSystem _mindSystem = default!;
        [Dependency] private readonly SharedActionsSystem _actionsSystem = default!;
        [Dependency] private readonly SharedHandsSystem _hands = default!;
        [Dependency] private readonly SharedPopupSystem _popup = default!;
        [Dependency] private readonly GhostSystem _ghost = default!;
        [Dependency] private readonly EntityLookupSystem _lookup = default!;
        [Dependency] private readonly ThrowingSystem _throwing = default!;
        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<NGAntagRuleComponent, AfterAntagEntitySelectedEvent>(AfterAntagSelected);
            SubscribeLocalEvent<NGAntagRoleComponent, GetBriefingEvent>(OnGetBriefing);

            SubscribeLocalEvent<SpawnCursedGiftSpellEvent>(OnSpawnCursedGiftSpell);
            SubscribeLocalEvent<SpawnAshGiftSpellEvent>(OnSpawnAshGiftSpell);
            SubscribeLocalEvent<TryToFindTargetSpellEvent>(TryToFindTargetSpell);
        }

        private void AfterAntagSelected(Entity<NGAntagRuleComponent> rule, ref AfterAntagEntitySelectedEvent args)
        {
            var ent = args.EntityUid;
            MakeNGAntag(ent, rule);
        }
        public void MakeNGAntag(EntityUid ngAntag, NGAntagRuleComponent ngAntagRule)
        {
            if (!_mindSystem.TryGetMind(ngAntag, out var mindId, out var mind))
                return;

            if (HasComp<NGAntagRoleComponent>(mindId))
                return;

            EnsureComp<NGAntagRoleComponent>(mindId);
            EnsureComp<NGAntagComponent>(ngAntag);
            _antag.SendBriefing(ngAntag, MakeBriefing(), Color.LightGreen, ngAntagRule.GreetingSound);
            _actionsSystem.AddAction(ngAntag, "ActionSpawnCursedGift");
            _actionsSystem.AddAction(ngAntag, "ActionTryToFindTarget");
        }

        private void OnGetBriefing(EntityUid uid, NGAntagRoleComponent ngAntag, ref GetBriefingEvent args)
        {
            if (!TryComp<MindComponent>(args.Mind, out var mind) || mind.CurrentEntity == null)
                return;

            args.Append(MakeBriefing());
        }

        private string MakeBriefing()
        {
            var briefing = Loc.GetString("ngAntag-role-greeting");

            briefing += "\n \n" + Loc.GetString("ngAntag-briefing") + "\n";
            return briefing;
        }

        private void OnSpawnAshGiftSpell(SpawnAshGiftSpellEvent ev)
        {
            if (ev.Handled)
                return;
            ev.Handled = true;

            var result = Spawn("PresentRandomAshNG3", Transform(ev.Performer).Coordinates);

            _throwing.TryThrow(result, _random.NextAngle().ToWorldVec(), 3);

            NerfEffectsOnSpawnCursedGift(ev.Performer);
        }

        private void OnSpawnCursedGiftSpell(SpawnCursedGiftSpellEvent ev)
        {
            if (ev.Handled)
                return;
            ev.Handled = true;

            if (!TryComp<NGAntagComponent>(ev.Performer, out var compUser))
                return;

            if (Deleted(compUser.LastCursedGift))
                compUser.LastCursedGift = null;

            if (compUser.LastCursedGift != null)
            {

                var message = _hands.TryPickupAnyHand(ev.Performer, compUser.LastCursedGift.Value)
                    ? "ngAntag-cursed-gift-recalled"
                    : "ngAntag-hands-full";
                _popup.PopupEntity(Loc.GetString(message), ev.Performer, ev.Performer);
                NerfEffectsOnSpawnCursedGift(ev.Performer);
                return;
            }
            var result = Spawn("CursedGift", Transform(ev.Performer).Coordinates);
            _throwing.TryThrow(result, _random.NextAngle().ToWorldVec(), 3);

            if (!TryComp<CurseGiftComponent>(result, out var compGift))
                return;

            NerfEffectsOnSpawnCursedGift(ev.Performer);
            compGift.OwnerGift = ev.Performer;
            compUser.LastCursedGift = result;
        }

        private void TryToFindTargetSpell(TryToFindTargetSpellEvent ev)
        {
            if (ev.Handled)
                return;
            ev.Handled = true;

            if (!TryComp<NGAntagComponent>(ev.Performer, out var compUser))
                return;

            if (compUser.Target == null)
            {
                _popup.PopupEntity(Loc.GetString("cursed-gift-say-no-target"), ev.Performer, ev.Performer);
                return;
            }

            var antag_mapUid = Transform(ev.Performer).MapUid;
            var target_mapUid = Transform(compUser.Target.Value).MapUid;

            if (antag_mapUid != target_mapUid)
            {
                _popup.PopupEntity("Цель возле Вас не найдена", ev.Performer, ev.Performer);
                return;
            }

            var antag_coords = Transform(ev.Performer).Coordinates;
            var target_coords = Transform(compUser.Target.Value).Coordinates;

            var distance = Math.Sqrt(Math.Pow(target_coords.X - antag_coords.X, 2) + Math.Pow(target_coords.Y - antag_coords.Y, 2));
            var distanceInt = (int) Math.Floor(distance);

            string GetMeterWord(int distance)
            {
                int lastDigit = distance % 10;
                int lastTwoDigits = distance % 100;

                if (lastDigit == 1 && lastTwoDigits != 11)
                    return "метр";
                else if ((lastDigit >= 2 && lastDigit <= 4) && (lastTwoDigits < 10 || lastTwoDigits >= 20))
                    return "метра";
                else
                    return "метров";
            }

            string meterWord = GetMeterWord(distanceInt);
            _popup.PopupEntity($"Расстояние до цели: {distanceInt} {meterWord}", ev.Performer, ev.Performer, PopupType.Medium);
        }

        // Много грифа
        /*private void EffectsOnSpawnCursedGift(EntityUid uid)
        {
            var entities = _lookup.GetEntitiesInRange(uid, 5f);
            var tags = GetEntityQuery<TagComponent>();
            var entityStorage = GetEntityQuery<EntityStorageComponent>();
            var items = GetEntityQuery<ItemComponent>();

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = _ghost.DoGhostBooEvent(ent);

                if (tags.HasComponent(ent) && _tag.HasAnyTag(ent, "Window"))
                {
                    var dspec = new DamageSpecifier();
                    dspec.DamageDict.Add("Structural", 15);
                    _damage.TryChangeDamage(ent, dspec, origin: uid);
                }

                if (entityStorage.TryGetComponent(ent, out var entstorecomp))
                    _entityStorage.OpenStorage(ent, entstorecomp);

                if (items.HasComponent(ent) &&
                    TryComp<PhysicsComponent>(ent, out var phys) && phys.BodyType != BodyType.Static)
                    _throwing.TryThrow(ent, _random.NextAngle().ToWorldVec());

                if (handled)
                    booCounter++;

                if (booCounter >= 10f)
                    break;
            }
        }*/

        private void NerfEffectsOnSpawnCursedGift(EntityUid uid)
        {
            var entities = _lookup.GetEntitiesInRange(uid, 5f);

            var booCounter = 0;
            foreach (var ent in entities)
            {
                var handled = _ghost.DoGhostBooEvent(ent);

                if (handled)
                    booCounter++;

                if (booCounter >= 10f)
                    break;
            }
        }
    }
}
