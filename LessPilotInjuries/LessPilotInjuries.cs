using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Harmony;
using BattleTech;
using System.Reflection;
using System.IO;
using Newtonsoft.Json;

namespace LessPilotInjuries
{
    [HarmonyPatch(typeof(Pilot), "SetNeedsInjury")]
    public static class Pilot_SetNeedsInjury_Patch
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
    
    [HarmonyPatch(typeof(Mech), "DamageLocation")]
    public static class Mech_DamageLocation_Patch
    {
        static void Prefix(Mech __instance, ArmorLocation aLoc, float totalDamage)
        {
            if (aLoc == ArmorLocation.Head && totalDamage < LessPilotInjuries.settings.IgnoreDamageBelow)
            {
                LessPilotInjuries.IgnoreNextHeadHit.Add(__instance.pilot);
                LessPilotInjuries.LogMessage("Ignoring next injury from {0} damage to head", totalDamage);
            }
        }
    }
    
    [HarmonyPatch(typeof(GameInstance), "LaunchContract", new Type[] { typeof(Contract), typeof(string) })]
    public static class GameInstance_LaunchContract_Patch
    {
        static void Prefix()
        {
            LessPilotInjuries.LogMessage("GameInstance_LaunchContract_Patch: Reseting head hits");

            // reset on new contracts
            LessPilotInjuries.Reset();
        }
    }

    internal class Settings
    {
        public float IgnoreDamageBelow = 5;
        public bool Log = true;
    }

    public static class LessPilotInjuries
    {
        public static string LogPath { get; set; } = null;

        internal static Settings settings;
        internal static HashSet<Pilot> IgnoreNextHeadHit { get; set; } = new HashSet<Pilot>();
        
        public static void Init(string path, string settingsJSON)
        {
            var harmony = HarmonyInstance.Create("io.github.mpstark.LessPilotInjuries");
            harmony.PatchAll(Assembly.GetExecutingAssembly());

            // read settings
            try
            {
                settings = JsonConvert.DeserializeObject<Settings>(settingsJSON);
            }
            catch (Exception)
            {
                settings = new Settings();
            }

            if (settings.Log)
            {
                LogPath = Path.Combine(path, "log.txt");
                using (var logWriter = File.CreateText(LogPath))
                {
                    logWriter.WriteLine("IgnoreDamageBelow -- {0}", settings.IgnoreDamageBelow);
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
