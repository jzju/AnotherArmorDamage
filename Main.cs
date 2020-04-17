using System;
using TaleWorlds.Core;
using TaleWorlds.MountAndBlade;
using TaleWorlds.Library;
using HarmonyLib;
using System.Xml;
using System.Collections.Generic;

namespace AnotherArmorDamage
{
    public static class Vars
    {
        public static Dictionary<string, float> dict = new Dictionary<string, float> { };
    }
    public static class MyPatcher
    {
        public static void DoPatching()
        {
            var harmony = new Harmony("com.jj.dmg");
            harmony.PatchAll();
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("ConvertBaseAttackMagnitude")]
    class RemoveDamageFactor
    {
        static bool Prefix(WeaponComponentData weapon, StrikeType strikeType, float baseMagnitude, ref float __result)
        {
            __result = baseMagnitude;
            return false;
        }
    }

    [HarmonyPatch(typeof(Mission))]
    [HarmonyPatch("ComputeBlowMagnitudeMissile")]
    class MissileMultiplier
    {
        static bool Prefix(ref AttackCollisionData acd,
                          ItemObject weaponItem,
                          bool isVictimAgentNull,
                          float momentumRemaining,
                          float missileTotalDamage,
                          out float baseMagnitude,
                          out float specialMagnitude,
                          Vec3 victimVel)
        {
            double num1 = (isVictimAgentNull ? (double)acd.MissileVelocity.Length : (double)(victimVel - acd.MissileVelocity).Length) / (double)acd.MissileStartingBaseSpeed;
            float num2 = (float)(num1 * num1);
            baseMagnitude = num2 * missileTotalDamage * momentumRemaining * Vars.dict["MissileFactor"];
            specialMagnitude = baseMagnitude;
            return false;
        }
    }

    [HarmonyPatch(typeof(CombatStatCalculator))]
    [HarmonyPatch("ComputeRawDamageNew")]
    class ArmorDamage
    {
        static bool Prefix(DamageTypes damageType, float magnitude, float armorEffectiveness, ref float __result)
        {
            // magnitude is around 20 for one handed sword swing and 40 for two handed axe swing
            float bluntReduction = 100f / (100f + armorEffectiveness);
            float bluntDmg = magnitude * bluntReduction;
            float bleedDmg = 0f;
            switch (damageType)
            {
                case DamageTypes.Cut:
                    if (armorEffectiveness < Vars.dict["CutLimit"])
                    {
                        bleedDmg = Math.Max(0f, magnitude - Vars.dict["CutArmor"] * armorEffectiveness);
                        bleedDmg *= Vars.dict["CutDmg"];
                    }
                    break;
                case DamageTypes.Pierce:
                    bleedDmg = Math.Max(0f, magnitude - Vars.dict["PierceArmor"] * armorEffectiveness);
                    bleedDmg *= Vars.dict["PierceDmg"];
                    break;
                case DamageTypes.Blunt:
                    bluntDmg *= Vars.dict["BluntDmg"];
                    break;
            }
            //if (magnitude > 0)
            //    InformationManager.DisplayMessage(new InformationMessage("magnitude " + magnitude + " bluntDmg " + bluntDmg + " bleedDmg " + bleedDmg + " armorEffectiveness " + armorEffectiveness));
            __result = bluntDmg + bleedDmg;
            return false;
        }
    }


    class Main : MBSubModuleBase
    {
        protected override void OnSubModuleLoad()
        {
            XmlDocument xmlDocument = new XmlDocument();
            xmlDocument.Load(BasePath.Name + "Modules/AnotherArmorDamage/config.xml");
            foreach (XmlNode childNode in xmlDocument.SelectSingleNode("/config").ChildNodes)
            {
                Vars.dict.Add(childNode.Name, float.Parse(childNode.InnerText));
            }
            MyPatcher.DoPatching();
        }
    }
}
