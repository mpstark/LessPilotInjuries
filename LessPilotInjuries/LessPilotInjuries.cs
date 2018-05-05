using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using System.Reflection;
using System.IO;

namespace LessPilotInjuries
{
    [HarmonyPatch(typeof(BattleTech.Pilot), "SetNeedsInjury")]
    public static class BattleTech_Pilot_SetNeedsInjury_Patch
    {
        static bool Prefix(Pilot __instance, InjuryReason reason)
        {
            if (reason == InjuryReason.HeadHit && LessPilotInjuries.IgnoreNextHeadHit.Contains(__instance))
            {
                LessPilotInjuries.LogMessage("Ignored injury!");
                LessPilotInjuries.IgnoreNextHeadHit.Remove(__instance);
                return false;
            }

            return true;
        }
    }
    
    [HarmonyPatch(typeof(BattleTech.Mech), "DamageLocation")]
    public static class BattleTech_Mech_DamageLocation_Patch
    {
        static void Prefix(Mech __instance, ArmorLocation aLoc, float totalDamage)
        {
            if (aLoc == ArmorLocation.Head && totalDamage < LessPilotInjuries.HeadHitIgnoreDamageBelow)
            {
                LessPilotInjuries.IgnoreNextHeadHit.Add(__instance.pilot);
                LessPilotInjuries.LogMessage("Ignoring next injury from {0} damage to head", totalDamage);
            }
        }
    }
    
    [HarmonyPatch(typeof(BattleTech.GameInstance), "LaunchContract", new Type[] { typeof(Contract), typeof(string) })]
    public static class BattleTech_GameInstance_LaunchContract_Patch
    {
        static void Prefix()
        {
            LessPilotInjuries.LogMessage("BattleTech_GameInstance_LaunchContract_Patch: Reseting head hits");

            // reset on new contracts
            LessPilotInjuries.Reset();
        }
    }

    public static class LessPilotInjuries
    {
        public static string LogPath { get; set; } = null;
        public static float HeadHitIgnoreDamageBelow { get; set; } = 5;
        public static HashSet<Pilot> IgnoreNextHeadHit { get; set; } = new HashSet<Pilot>();
        
        public static void Init(string path, Dictionary<string, string> settings)
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.LessPilotInjuries");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // read settings
            if (settings != null)
            {
                float _ignoreDamageBelow;
                if (settings.ContainsKey("IgnoreDamageBelow") && float.TryParse(settings["IgnoreDamageBelow"], out _ignoreDamageBelow))
                {
                    HeadHitIgnoreDamageBelow = _ignoreDamageBelow;
                }

                bool _log = false;
                if (settings.ContainsKey("Log") && bool.TryParse(settings["Log"], out _log) && _log)
                {
                    LogPath = Path.Combine(path, "log.txt");
                    using (var logWriter = File.CreateText(LogPath))
                    {
                        logWriter.WriteLine("HeadHitIgnoreDamageBelow -- {0}", HeadHitIgnoreDamageBelow);
                    }
                }
            }
        }
        
        public static void Reset()
        {
            IgnoreNextHeadHit = new HashSet<Pilot>();
        }

        public static void LogMessage(string message, params object[] formatThings)
        {
            if (LogPath != null)
            {
                using (var logWriter = File.AppendText(LessPilotInjuries.LogPath))
                {
                    logWriter.WriteLine(DateTime.Now + " - " + message, formatThings);
                }
            }
        }
    }
}
