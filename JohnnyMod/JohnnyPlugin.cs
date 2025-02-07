using BepInEx;
using JohnnyMod.Survivors.Johnny;
using JohnnyMod.Survivors.Johnny.Components;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Security;
using System.Security.Permissions;
using UnityEngine.Networking;

[module: UnverifiableCode]
[assembly: SecurityPermission(SecurityAction.RequestMinimum, SkipVerification = true)]

//rename this namespace
namespace JohnnyMod
{
    //[BepInDependency("com.rune580.riskofoptions", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [BepInPlugin(MODUID, MODNAME, MODVERSION)]
    public class JohnnyPlugin : BaseUnityPlugin
    {
        public const string MODUID = "com.HasteReapr.JohnnyMod";
        public const string MODNAME = "JohnnyMod";
        public const string MODVERSION = "1.1.0";

        public const string DEVELOPER_PREFIX = "HASTEREAPR";

        public static JohnnyPlugin instance;

        void Awake()
        {
            instance = this;

            Log.Init(Logger);

            Modules.Language.Init();

            new JohnnySurvivor().Initialize();

            Hook();

            //handles all of the emoteAPI compatability stuff
            if (EmoteAPICompat.enabled) EmoteAPICompat.EmoteHook();

            new Modules.ContentPacks().Initialize();
        }

        private void Hook()
        {
            On.RoR2.MapZone.TryZoneStart += MapZone_TryZoneStart;
            //Run.onClientGameOverGlobal += Run_onClientGameOverGlobal;
            On.RoR2.Run.OnClientGameOver += Run_OnClientGameOver;
        }

        private void Run_OnClientGameOver(On.RoR2.Run.orig_OnClientGameOver orig, Run self, RunReport runReport)
        {
            orig(self, runReport);
            try
            {
                if (NetworkServer.active)
                {
                    //dont jumpscare me please
                    Util.PlaySound("PlayWinVoice", self.gameObject);
                }
            }
            catch (System.Exception)
            {
                Log.Error("Had issue with RunOnClientGameOver call. But seeing this means the vanilla version ran.");
            }
        }

        private void Run_onClientGameOverGlobal(Run arg1, RunReport arg2)
        {
            bool isJohgn = false;
            for (int x = 0; x < arg2.playerInfoCount; x++)
            {
                Log.Message("Scanning for Johgnny");
                if (arg2.playerInfos[x].bodyName.Equals("JohnnyBody"))
                    isJohgn = true;
            }

            if (isJohgn)
            {
                if (arg2.gameEnding.isWin)
                {
                    Util.PlaySound("PlayWinVoice", arg1.gameObject);
                }
                else
                {
                    Util.PlaySound("PlayLostVoice", arg1.gameObject);
                }
            }
            Log.Message("Trying to play the win voice");
            Util.PlaySound("PlayWinVoice", arg1.gameObject);

            if (arg2.gameEnding.isWin)
            {
                Util.PlaySound("PlayWinVoice", arg1.gameObject);
            }
            else
            {
                Util.PlaySound("PlayLostVoice", arg1.gameObject);
            }
        }

        private void MapZone_TryZoneStart(On.RoR2.MapZone.orig_TryZoneStart orig, MapZone self, UnityEngine.Collider other)
        {
            // if we have the card component get out of this method and dont kys
            if (other.GetComponent<CardController>() && other.GetComponent<TeamComponent>().teamIndex != TeamIndex.Player)
            {
                return;
            }

            orig(self, other);
        }
    }
}
