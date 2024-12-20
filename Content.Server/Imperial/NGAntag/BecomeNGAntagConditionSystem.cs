using System.Linq;
using Content.Server.Objectives.Systems;
using Content.Server.Roles;
using Content.Server.Objectives.Components;
using Content.Server.Imperial.NGAntag.Components;
using Content.Shared.Objectives.Components;
using Content.Shared.Mind;
using Robust.Shared.Random;
using Content.Shared.NukeOps;

namespace Content.Server.Imperial.NGAntag
{
    public sealed class BecomeNGAntagConditionSystem : EntitySystem
    {
        [Dependency] private readonly IRobustRandom _random = default!;
        [Dependency] private readonly SharedMindSystem _mind = default!;
        [Dependency] private readonly RoleSystem _role = default!;
        [Dependency] private readonly TargetObjectiveSystem _target = default!;

        public override void Initialize()
        {
            base.Initialize();

            SubscribeLocalEvent<BecomeNGAntagConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);

            SubscribeLocalEvent<NGAntagPickRandomPersonComponent, ObjectiveAssignedEvent>(OnNGAntagAssigned);
        }

        private void OnGetProgress(EntityUid uid, BecomeNGAntagConditionComponent comp, ref ObjectiveGetProgressEvent args)
        {
            if (!_target.GetTarget(uid, out var target))
                return;

            args.Progress = GetProgress(target.Value);
        }

        private void OnNGAntagAssigned(EntityUid uid, NGAntagPickRandomPersonComponent comp, ref ObjectiveAssignedEvent args)
        {
            if (!TryComp<TargetObjectiveComponent>(uid, out var target))
            {
                args.Cancelled = true;
                return;
            }

            if (target.Target != null)
                return;

            var allHumans = _mind.GetAliveHumans(args.MindId);
            if (allHumans.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            if (!TryComp<MindComponent>(args.MindId, out var antagMindComp))
                return;

            var targetMind = args.MindId;
            var potentialTargets = allHumans.Where(mind => !IsNGAntag(mind)
                                                        && !IsNukeOpsAntag(mind)
                                                        && !IsLoneNukeOpsAntag(mind)
                                                        && !IsOneMapId(mind, targetMind)).ToList();
            if (potentialTargets.Count == 0)
            {
                args.Cancelled = true;
                return;
            }

            var objective_target = _random.Pick(potentialTargets);
            _target.SetTarget(uid, objective_target, target);

            Console.WriteLine(args.MindId.ToString());

            if (!TryComp<MindComponent>(target.Target, out var targetMindComp))
                return;

            if (!TryComp<NGAntagComponent>(antagMindComp.CurrentEntity, out var ngAntagComp))
                return;

            ngAntagComp.Target = targetMindComp.CurrentEntity;
        }

        public bool IsNGAntag(EntityUid mindId)
        {
            if (!_role.MindHasRole<NGAntagRoleComponent>(mindId))
                return false;

            return true;
        }

        public bool IsNukeOpsAntag(EntityUid mindId)
        {
            if (!_role.MindHasRole<NukeopsRoleComponent>(mindId))
                return false;

            return true;
        }

        public bool IsLoneNukeOpsAntag(EntityUid mindId)
        {
            if (!TryComp<MindComponent>(mindId, out var comp))
                return false;

            if (!HasComp<NukeOperativeComponent>(comp.CurrentEntity))
                return false;

            return true;
        }

        public bool IsOneMapId(EntityUid mindId, EntityUid targerMindId)
        {
            if (!TryComp<MindComponent>(mindId, out var comp))
                return false;

            if (comp.CurrentEntity == null)
                return false;

            if (!(Transform(comp.CurrentEntity.Value).MapUid == Transform(targerMindId).MapUid))
                return false;

            return true;
        }

        private float GetProgress(EntityUid target)
        {
            if (!TryComp<MindComponent>(target, out var mind) || mind.OwnedEntity == null)
                return 0f;

            return IsNGAntag(target) ? 1f : 0f;
        }
    }
}
