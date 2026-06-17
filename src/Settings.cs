using MCM.Abstractions.Attributes;
using MCM.Abstractions.Attributes.v2;
using MCM.Abstractions.Base.Global;

namespace VampireOverhaul
{
    public class Settings : AttributeGlobalSettings<Settings>
    {
        public override string Id => "VampireOverhaul";
        public override string DisplayName => "Vampire Overhaul";

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
    }
}