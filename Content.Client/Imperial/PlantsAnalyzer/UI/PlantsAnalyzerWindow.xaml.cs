using System.Linq;
using Content.Client.UserInterface.Controls;
using Content.Shared.Imperial.PlantsAnalyzer;
using Robust.Client.AutoGenerated;
using Robust.Client.UserInterface.XAML;
using Robust.Shared.Utility;
using Content.Shared.IdentityManagement;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.Imperial.PlantsAnalyzer.UI
{
    [GenerateTypedNameReferences]
    public sealed partial class PlantsAnalyzerWindow : FancyWindow
    {
        private readonly IEntityManager _entityManager;

        public PlantsAnalyzerWindow()
        {
            RobustXamlLoader.Load(this);

            var dependencies = IoCManager.Instance!;
            _entityManager = dependencies.Resolve<IEntityManager>();
        }

        public void Populate(PlantsAnalyzerScannedUserMessage msg)
        {
            var target = _entityManager.GetEntity(msg.TargetEntity);

            if (target == null)
            {
                NoPlantDataText.Visible = true;
                return;
            }

            NoPlantDataText.Visible = false;

            ScanModeLabel.Text = msg.ScanMode
                ? Loc.GetString("plants-analyzer-window-scan-mode-active")
                : Loc.GetString("plants-analyzer-window-scan-mode-inactive");
            ScanModeLabel.FontColorOverride = msg.ScanMode ? Color.Green : Color.Red;

            SpriteView.SetEntity(target.Value);
            SpriteView.Visible = msg.ScanMode;
            NoDataTex.Visible = !msg.ScanMode;

            var name = new FormattedMessage();
            name.PushColor(Color.White);
            name.AddText(Identity.Name(target.Value, _entityManager));
            NameLabel.SetMessage(name);

            SpeciesLabel.Text = msg.PlantName;

            PotencyLevelLabel.Text = $"{msg.PotencyLevel:F1}";
            ProductionLevelLabel.Text = $"{msg.ProductionLevel:F1} un";
            PestLevelLabel.Text = $"{msg.PestLevel * 10:F1} %";
            WeedLevelLabel.Text = $"{msg.WeedLevel * 10:F1} %";
            ToxinsLabel.Text = $"{msg.Toxins * 10:F1} %";
            AgeLabel.Text = $"{msg.Age}";
            HealthLabel.Text = $"{msg.Health:F1} / 110";
            MutationLevelLabel.Text = $"{msg.MutationLevel:F1}";

            DeadLabel.Visible = msg.IsDead;
            DeadLabel.Text = msg.IsDead ? Loc.GetString("plants-analyzer-window-plant-dead") : Loc.GetString("plants-analyzer-window-plant-alive");

            OptimalConditionsLabel.Visible = !string.IsNullOrEmpty(msg.OptimalConditions);
            OptimalConditionsLabel.Text = msg.OptimalConditions;
            OptimalConditionsLabel.Modulate = Color.Yellow;

            KudzuWarningLabel.Visible = msg.HasKudzu;
            KudzuWarningLabel.Modulate = Color.Red;

            MutationsLabel.Text = $"{Loc.GetString("plants-analyzer-window-mutations")} {msg.Mutations}";
            var chemicals = msg.Chemicals;
            if (chemicals?.Any() == true)
            {
                var chemicalsWithLineBreaks = chemicals;

                ChemicalsLabelWithLineBreaks.Text = $"{Loc.GetString("plants-analyzer-window-chemicals")} {chemicalsWithLineBreaks}";
            }
            else
            {
                ChemicalsLabelWithLineBreaks.Text = Loc.GetString("plants-analyzer-window-no-chemicals");
            }

            var showAlerts = msg.ImproperHeat || msg.ImproperPressure || msg.ImproperLight;

            AlertsDivider.Visible = showAlerts;
            AlertsContainer.Visible = showAlerts;

            if (showAlerts)
            {
                AlertsContainer.DisposeAllChildren();

                if (msg.ImproperHeat)
                {
                    AlertsContainer.AddChild(new RichTextLabel
                    {
                        Text = Loc.GetString("plants-analyzer-window-entity-improper-heat-text"),
                        Margin = new Thickness(0, 4),
                        MaxWidth = 300
                    });
                }

                if (msg.ImproperPressure)
                {
                    AlertsContainer.AddChild(new RichTextLabel
                    {
                        Text = Loc.GetString("plants-analyzer-window-entity-improper-pressure-text"),
                        Margin = new Thickness(0, 4),
                        MaxWidth = 300
                    });
                }

                if (msg.ImproperLight)
                {
                    AlertsContainer.AddChild(new RichTextLabel
                    {
                        Text = Loc.GetString("plants-analyzer-window-entity-improper-light-text"),
                        Margin = new Thickness(0, 4),
                        MaxWidth = 300
                    });
                }
            }
        }
    }
}
