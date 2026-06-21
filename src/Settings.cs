using System;
using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;
using TaleWorlds.CampaignSystem;
using TaleWorlds.Library;

namespace VampireOverhaul
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "VampireOverhaul";
        public override string DisplayName => "Vampire Overhaul";
        public override string FormatType => "json";
        public override string FolderName => "VampireOverhaul";

        public void RegisterSettings()
        {
            _ = Instance;
        }

        [SettingPropertyBool("Enable Vampire Features", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("General")]
        public bool EnableVampireFeatures { get; set; } = true;

        [SettingPropertyBool("Enable Vampire Mechanics", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("General")]
        public bool EnableVampireMechanics { get; set; } = false;

        [SettingPropertyFloatingInteger("Max Blood Lust", 50f, 1000f, "0", Order = 0, RequireRestart = false)]
        [SettingPropertyGroup("Blood Lust")]
        public float MaxBloodLust { get; set; } = 100f;

        [SettingPropertyFloatingInteger("Blood Gain Multiplier", 0.5f, 3.0f, "0%", Order = 1, RequireRestart = false)]
        [SettingPropertyGroup("Blood Lust")]
        public float BloodGainMultiplier { get; set; } = 1.0f;

        [SettingPropertyFloatingInteger("Blood Decay Rate", 0.0f, 5.0f, "0.00", Order = 2, RequireRestart = false)]
        [SettingPropertyGroup("Blood Lust")]
        public float BloodDecayRate { get; set; } = 0.5f;

        [SettingPropertyFloatingInteger("Blood Reduction on Kill", 0f, 100f, "0", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Blood Lust")]
        public float BloodReductionOnKill { get; set; } = 15f;

        [SettingPropertyFloatingInteger("Blood Lust Warning Threshold", 0.5f, 0.95f, "0%", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Blood Lust")]
        public float BloodLustWarningThreshold { get; set; } = 0.7f;

        [SettingPropertyFloatingInteger("Feral Damage Multiplier", 1.0f, 4.0f, "0%", Order = 3, RequireRestart = false)]
        [SettingPropertyGroup("Feral State")]
        public float FeralDamageMultiplier { get; set; } = 1.5f;

        [SettingPropertyFloatingInteger("Feral Speed Multiplier", 1.0f, 2.5f, "0%", Order = 4, RequireRestart = false)]
        [SettingPropertyGroup("Feral State")]
        public float FeralSpeedMultiplier { get; set; } = 1.2f;

        private float _debugBloodLustPercentFallback;

        [SettingPropertyFloatingInteger(
            "Set Blood Lust",
            0f,
            100f,
            "0'%'",
            Order = 0,
            RequireRestart = false,
            HintText = "Sets Blood Lust as a percentage of your configured Max Blood Lust. Applies immediately in a campaign.")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public float DebugBloodLustPercent
        {
            get
            {
                float maxBloodLust = MaxBloodLust;
                if (maxBloodLust <= 0f)
                {
                    return 0f;
                }

                VampireComponent? vampire = Campaign.Current?.GetCampaignBehavior<VampireComponent>();
                if (vampire == null)
                {
                    return _debugBloodLustPercentFallback;
                }

                return MathF.Clamp(vampire.CurrentBloodLust / maxBloodLust * 100f, 0f, 100f);
            }
            set
            {
                _debugBloodLustPercentFallback = MathF.Clamp(value, 0f, 100f);
                TrySetBloodLust(MaxBloodLust * _debugBloodLustPercentFallback / 100f);
                NotifyDebugBloodLustChanged();
            }
        }

        [SettingPropertyButton(
            "Clear Blood Lust",
            Content = "Set to 0%",
            Order = 1,
            RequireRestart = false,
            HintText = "Instantly sets Blood Lust to 0.")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public Action DebugClearBloodLust
        {
            get => () => ApplyDebugBloodLustPercent(0f);
            set { }
        }

        [SettingPropertyButton(
            "Moderate Blood Lust",
            Content = "Set to 50%",
            Order = 2,
            RequireRestart = false,
            HintText = "Sets Blood Lust to 50% of max.")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public Action DebugHalfBloodLust
        {
            get => () => ApplyDebugBloodLustPercent(50f);
            set { }
        }

        [SettingPropertyButton(
            "High Blood Lust",
            Content = "Set to 75%",
            Order = 3,
            RequireRestart = false,
            HintText = "Sets Blood Lust to 75% of max.")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public Action DebugHighBloodLust
        {
            get => () => ApplyDebugBloodLustPercent(75f);
            set { }
        }

        [SettingPropertyButton(
            "Event Threshold",
            Content = "Set to 70%",
            Order = 4,
            RequireRestart = false,
            HintText = "Sets Blood Lust to 70% of max (random event threshold).")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public Action DebugEventBloodLust
        {
            get => () => ApplyDebugBloodLustPercent(70f);
            set { }
        }

        [SettingPropertyButton(
            "Feral Blood Lust",
            Content = "Set to 95%",
            Order = 5,
            RequireRestart = false,
            HintText = "Sets Blood Lust to 95% of max (feral threshold).")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public Action DebugFeralBloodLust
        {
            get => () => ApplyDebugBloodLustPercent(95f);
            set { }
        }

        [SettingPropertyButton(
            "Max Blood Lust",
            Content = "Set to 100%",
            Order = 6,
            RequireRestart = false,
            HintText = "Sets Blood Lust to your configured max.")]
        [SettingPropertyGroup("Debug", GroupOrder = 99)]
        public Action DebugMaxBloodLust
        {
            get => () => ApplyDebugBloodLustPercent(100f);
            set { }
        }

        private void ApplyDebugBloodLustPercent(float percent)
        {
            _debugBloodLustPercentFallback = MathF.Clamp(percent, 0f, 100f);
            TrySetBloodLust(MaxBloodLust * _debugBloodLustPercentFallback / 100f);
            NotifyDebugBloodLustChanged();
        }

        private void NotifyDebugBloodLustChanged()
        {
            OnPropertyChanged(nameof(DebugBloodLustPercent));
        }

        public bool TrySetBloodLust(float value, bool showMessage = true)
        {
            if (Campaign.Current == null)
            {
                if (showMessage)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "[Debug] Load a campaign before setting Blood Lust.", Colors.Red));
                }

                return false;
            }

            VampireComponent? vampire = Campaign.Current.GetCampaignBehavior<VampireComponent>();
            if (vampire == null)
            {
                return false;
            }

            if (!EnableVampireMechanics)
            {
                if (showMessage)
                {
                    InformationManager.DisplayMessage(new InformationMessage(
                        "[Debug] Enable Vampire Mechanics in General settings first.", Colors.Red));
                }

                return false;
            }

            if (!vampire.IsVampire)
            {
                vampire.IsVampire = true;
            }

            float clamped = MathF.Clamp(value, 0f, MaxBloodLust);
            vampire.CurrentBloodLust = clamped;
            _debugBloodLustPercentFallback = MaxBloodLust > 0f
                ? clamped / MaxBloodLust * 100f
                : 0f;

            if (showMessage)
            {
                InformationManager.DisplayMessage(new InformationMessage(
                    $"[Debug] Blood Lust set to {clamped:F0} / {MaxBloodLust:F0} ({_debugBloodLustPercentFallback:F0}%).",
                    Colors.Yellow));
            }

            return true;
        }
    }
}