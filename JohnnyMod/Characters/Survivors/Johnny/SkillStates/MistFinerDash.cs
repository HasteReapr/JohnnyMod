using System;
using EntityStates;
using RoR2;
using UnityEngine;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    //this is just Huntress's mini blink thing
    public class MistFinerDash : BaseSkillState
    {
        private Transform modelTransform;
        public static GameObject blinkPrefab;
        public float duration = 0.2f;
        public float spdCoef = 25f;
        public string beginSoundString;
        public string endSoundString;

        private CharacterModel characterModel;
        private HurtBoxGroup hurtboxGroup;
        private HurtBoxGroup hurtboxGroupTransform;
        private float stopwatch;
        private Vector3 blinkVector = Vector3.zero;

        public override void OnEnter()
        {
            base.OnEnter();
            this.modelTransform = base.GetModelTransform();
            if (modelTransform)
            {
                this.characterModel = this.modelTransform.GetComponent<CharacterModel>();
                this.hurtboxGroup = this.modelTransform.GetComponent<HurtBoxGroup>();
            }
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount++;
            }
            if (this.hurtboxGroup)
            {
                this.hurtboxGroup.hurtBoxesDeactivatorCounter += 1;
            }
            this.blinkVector = this.GetBlinkVector();

            Util.PlaySound("Play_huntress_shift_start", gameObject);

            PlayAnimation("UpperBody, Override", "MistFinerLoop");
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            this.stopwatch += base.GetDeltaTime();
            if (base.characterMotor && base.characterDirection)
            {
                base.characterMotor.velocity = Vector3.zero;
                base.characterMotor.rootMotion += this.blinkVector * (this.moveSpeedStat * this.spdCoef * base.GetDeltaTime());
            }
            if (this.stopwatch >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
            }
        }

        public override void OnExit()
        {
            if (this.characterModel)
            {
                this.characterModel.invisibilityCount--;
            }
            if (this.hurtboxGroup)
            {
                this.hurtboxGroup.hurtBoxesDeactivatorCounter -= 1;
            }
            if (base.characterMotor)
            {
                base.characterMotor.disableAirControlUntilCollision = false;
            }
            if (base.skillLocator.utility != null)
            {
                base.skillLocator.utility.UnsetSkillOverride(gameObject, JohnnyStaticValues.MistFinerDash, GenericSkill.SkillOverridePriority.Contextual);
            }

            Util.PlaySound("Play_huntress_shift_end", gameObject);

            GetModelAnimator().SetBool("MistFiner.channeled", false);
            base.OnExit();
        }

        private Vector3 GetBlinkVector()
        {
            return ((base.inputBank.moveVector == Vector3.zero) ? base.characterDirection.forward : base.inputBank.moveVector).normalized;
        }
    }
}
