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
        public override void OnEnter()
        {
            base.OnEnter();
            tensionCTRL = GetComponent<JohnnyTensionController>();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            bool canRC = tensionCTRL.tension >= 50;

            if (canRC && isAuthority &&
                inputBank.sprint.down &&
                inputBank.skill3.down)
            {
                inputBank.sprint.PushState(false);
                inputBank.skill1.PushState(false);
                inputBank.skill2.PushState(false);
                inputBank.skill3.PushState(false);
                inputBank.skill4.PushState(false);

                tensionCTRL.AddTension(-50);
                outer.SetState(new RomanCancel());
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }
    }
}
