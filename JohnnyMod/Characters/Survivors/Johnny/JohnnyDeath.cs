using EntityStates;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    public class JohnnyDeath : GenericCharacterDeath
    {
        public override void OnEnter()
        {
            base.OnEnter();

            Util.PlaySound("PlayLostVoice", gameObject);
        }
    }
}