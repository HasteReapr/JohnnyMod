using JohnnyMod.Survivors.Johnny.SkillStates;
using RoR2;
using System.Linq;
using UnityEngine;
using UnityEngine.Networking;

namespace JohnnyMod.Survivors.Johnny.Components
{
    internal class JohnnyRCControl : NetworkBehaviour
    {
        /// <summary>
        /// This is a controller to read the inputs for the Roman Cancel. This has to be done so you can Roman Cancel out of being frozen.
        /// </summary>
        private PlayerCharacterMasterController PCMC;
        private EntityStateMachine JohnESM;
        private JohnnyTensionController tensionCTRL;

        private void OnEnable()
        {
            CharacterBody charBody = base.gameObject.GetComponent<CharacterBody>();
            PCMC = charBody.master.playerCharacterMasterController;
            tensionCTRL = GetComponent<JohnnyTensionController>();
            JohnESM = EntityStateMachine.FindByCustomName(gameObject, "RomanCancel");
        }

        private void FixedUpdate()
        {
            if(PCMC == null)
            {
                CharacterBody charBody = base.gameObject.GetComponent<CharacterBody>();
                PCMC = charBody.master.playerCharacterMasterController;
            }
            if(tensionCTRL == null)
            {
                tensionCTRL = GetComponent<JohnnyTensionController>();
            }
            if(JohnESM == null)
            {
                JohnESM = EntityStateMachine.FindByCustomName(gameObject, "RomanCancel");
            }
            bool canRC = tensionCTRL.tension >= 50;
            
            if (canRC && hasAuthority && 
                PCMC.bodyInputs.sprint.down &&
                PCMC.bodyInputs.skill3.down)
            {
                PCMC.bodyInputs.skill1.PushState(false);
                PCMC.bodyInputs.skill2.PushState(false);
                PCMC.bodyInputs.skill3.PushState(false);
                PCMC.bodyInputs.skill4.PushState(false);
                tensionCTRL.AddTension(-50);
                JohnESM.state.outer.SetState(new RomanCancel());
            }
        }
    }
}