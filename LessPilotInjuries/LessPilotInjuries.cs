using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using System.Reflection;

namespace LessPilotInjuries
{
    [HarmonyPatch(typeof(BattleTech.Pilot), "SetNeedsInjury")]
    public static class BattleTech_Pilot_SetNeedsInjury_Patch
    {
        static bool Prefix(Pilot __instance, InjuryReason reason)
        {
            if (reason == InjuryReason.HeadHit && LessPilotInjuries.IgnoreNextHeadHit.Contains(__instance))
            {
                LessPilotInjuries.IgnoreNextHeadHit.Remove(__instance);
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(BattleTech.Mech), "DamageLocation")]
    public static class BattleTech_Mech_DamageLocation_Patch
    {
        static void Postfix(Mech __instance, int originalHitLoc, WeaponHitInfo hitInfo, ArmorLocation aLoc, Weapon weapon, float totalDamage, int hitIndex, AttackImpactQuality impactQuality)
        {
            if (aLoc == ArmorLocation.Head && totalDamage < LessPilotInjuries.HeadHitIgnoreDamageBelow)
            {
                LessPilotInjuries.IgnoreNextHeadHit.Add(__instance.pilot);
            }
        }
    }
    
    [HarmonyPatch(typeof(BattleTech.GameInstance), "LaunchContract", new Type[] { typeof(Contract), typeof(string) })]
    public static class BattleTech_GameInstance_LaunchContract_Patch
    {
        static void Postfix()
        {
            // reset on new contracts
            LessPilotInjuries.Reset();
        }
    }

    public static class LessPilotInjuries
    {
        public static float HeadHitIgnoreDamageBelow = 5;
        public static HashSet<Pilot> IgnoreNextHeadHit = new HashSet<Pilot>();

        public static void Init()
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.LessPilotInjuries");
            harmony.PatchAll(Assembly.GetExecutingAssembly());
        }

        public static void Reset()
        {
            IgnoreNextHeadHit = new HashSet<Pilot>();
        }
    }
}
