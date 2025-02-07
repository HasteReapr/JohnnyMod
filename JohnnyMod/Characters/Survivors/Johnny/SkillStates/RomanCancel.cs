using EntityStates;
using JohnnyMod.Survivors.Johnny;
using RoR2;
using UnityEngine;
using UnityEngine.Networking;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    public class RomanCancel : GenericCharacterMain
    {
        private float driftTime = 0.1f;
        private Vector3 prevVel;
        private Vector3 prevLastVel;
        private bool unsetPrevVel;
        private float duration;

        private EntityState bodyState; //this exists so johnny doesnt fucking break his neck after roman cancelling

        public override void OnEnter()
        {
            base.OnEnter();

            if (isAuthority)
            {
                EntityStateMachine Body = EntityStateMachine.FindByCustomName(gameObject, "Body");
                EntityStateMachine Weap = EntityStateMachine.FindByCustomName(gameObject, "Weapon");
                EntityStateMachine Weap2 = EntityStateMachine.FindByCustomName(gameObject, "Weapon2");
                bodyState = Body.state;

                inputBank.sprint.PushState(false);
                inputBank.skill1.PushState(false);
                inputBank.skill2.PushState(false);
                inputBank.skill3.PushState(false);
                inputBank.skill4.PushState(false);

                skillLocator.secondary.RestockSteplike();
                skillLocator.utility.RestockSteplike();
                skillLocator.special.RestockSteplike();

                Body.SetState(new Idle());
                Weap.SetState(new Idle());
                Weap2.SetState(new Idle());

                PlayAnimation("RomanCancel, Override", "RomanCancel");
                GetModelAnimator().Play("RomanCancel");
            }

            EffectData effData = new EffectData
            {
                origin = gameObject.transform.position,
                scale = 2,
            };
            EffectManager.SpawnEffect(JohnnyAssets.romanCancelEffect, effData, transmit: true);

            unsetPrevVel = false;
            duration = 0.35f;
            Util.PlaySound("PlayRC", gameObject);

            if (NetworkServer.active)
            {
                characterBody.AddTimedBuff(RoR2Content.Buffs.HiddenInvincibility, 0.3f);
                Util.CleanseBody(characterBody, removeDebuffs: true, removeBuffs: false, removeCooldownBuffs: true, removeDots: true, removeStun: true, removeNearbyProjectiles: true);
                //this is the roman cancel burst back, to blow enemies away
                BlastAttack explode = new BlastAttack();
                explode.baseDamage = 0f;
                explode.baseForce = 40f;
                explode.radius = 20f;
                explode.crit = false;
                explode.procCoefficient = 0;
                explode.attacker = gameObject;
                explode.inflictor = gameObject;
                explode.damageType = DamageType.NonLethal;
                explode.damageColorIndex = DamageColorIndex.Default;
                explode.teamIndex = gameObject.GetComponent<TeamComponent>().teamIndex;
                explode.falloffModel = BlastAttack.FalloffModel.None;
                explode.position = transform.position;
                explode.Fire();
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if(fixedAge >= duration && isAuthority)
            {
                outer.SetNextStateToMain();
                // we reset the Body ESM to JohnnyMainState. This is done because we can't exactly nullify the inputs through the secondary IdleState
                EntityStateMachine Body = EntityStateMachine.FindByCustomName(gameObject, "Body");
                Body.SetState(bodyState); // we have the state copied and stored here, tho we should probably drop the inputs
            }
        }

        public override bool CanExecuteSkill(GenericSkill skillSlot)
        {
            return false;
        }

        public override void HandleMovements()
        {
            if (!unsetPrevVel)
            {
                //we have this so we dont accidentally set the previous velocity to 0
                prevVel = characterMotor.velocity;
                prevLastVel = characterMotor.lastVelocity;
                unsetPrevVel = true;
            }

            characterMotor.velocity = Vector3.zero;

            if(fixedAge >= driftTime)
            {
                Vector3 moveVec = inputBank.moveVector;

                Vector3 multVel = Vector3.Max(prevVel, prevLastVel);
                Vector3 desiredVel;
                desiredVel.x = multVel.x * 1.05f;
                desiredVel.y = multVel.y * 1.05f;
                desiredVel.z = multVel.z * 1.05f;
                
                characterMotor.velocity = desiredVel;
            }
        }
    }
}
