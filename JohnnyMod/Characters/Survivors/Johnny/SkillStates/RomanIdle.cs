using RoR2;
using EntityStates;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;
using JohnnyMod.Survivors.Johnny.Components;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    public class RomanIdle : Idle
    {
        private JohnnyTensionController tensionCTRL;
        private PlayerCharacterMasterController pcmc;
        public override void OnEnter()
        {
            base.OnEnter();
            tensionCTRL = GetComponent<JohnnyTensionController>();
            pcmc = characterBody.master.playerCharacterMasterController;
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            bool canRC = tensionCTRL.tension >= 50;

            if (!isAuthority)
                return;

            if (canRC)
            {
                if (inputBank.sprint.down && inputBank.skill3.down)
                {
                    tensionCTRL.AddTension(-50);
                    outer.SetState(new RomanCancel());
                }
                else
                {
                    var player = pcmc ? pcmc.networkUser?.inputPlayer : null; 
                    if (player != null && player.GetButton(18) && player.GetButton(9))
                    {
                        tensionCTRL.AddTension(-50);
                        outer.SetState(new RomanCancel());
                    }
                }
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
