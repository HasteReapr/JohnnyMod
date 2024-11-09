using EntityStates;
using JohnnyMod.Modules.BaseStates;
using RoR2;
using UnityEngine;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    public class SlashCombo : BaseMeleeAttack
    {
        private ChildLocator childLoc;

        public override void OnEnter()
        {
            hitboxGroupName = "SwordGroup";

            damageType = DamageType.Generic;
            damageCoefficient = JohnnyStaticValues.swordDamageCoefficient;
            procCoefficient = 1f;
            pushForce = 300f;
            bonusForce = Vector3.zero;
            baseDuration = 1f;

            childLoc = GetModelChildLocator();
            childLoc.FindChild("GhostHilt").gameObject.SetActive(false);
            childLoc.FindChild("KatanaHilt").gameObject.SetActive(true);
            childLoc.FindChild("KatanaBlade").gameObject.SetActive(true);
            childLoc.FindChild("SwordSimp").gameObject.SetActive(true);

            //0-1 multiplier of baseduration, used to time when the hitbox is out (usually based on the run time of the animation)
            //for example, if attackStartPercentTime is 0.5, the attack will start hitting halfway through the ability. if baseduration is 3 seconds, the attack will start happening at 1.5 seconds
            attackStartPercentTime = 0.25f;
            attackEndPercentTime = 0.6f;

            //this is the point at which the attack can be interrupted by itself, continuing a combo
            earlyExitPercentTime = 0.85f;

            hitStopDuration = 0.012f;
            attackRecoil = 0.5f;
            hitHopVelocity = 4f;

            swingSoundString = "Play_merc_sword_swing";
            hitSoundString = "";
            muzzleString = swingIndex % 2 == 0 ? "Swing1" : "Swing2";
            playbackRateParam = "Slash.playbackRate";
            swingEffectPrefab = JohnnyAssets.swordSwingEffect;
            hitEffectPrefab = JohnnyAssets.swordHitImpactEffect;

            base.OnEnter();
        }

        protected override void PlayAttackAnimation()
        {
            PlayCrossfade("UpperBody, Override", "Swing" + (1 + swingIndex), playbackRateParam, duration, 0.1f);
        }

        protected override void PlaySwingEffect()
        {
            base.PlaySwingEffect();
        }

        protected override void OnHitEnemyAuthority()
        {
            base.OnHitEnemyAuthority();
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (base.fixedAge > this.duration * 0.5f && skillLocator && skillLocator.special && inputBank && inputBank.skill4.down)
                this.skillLocator.special.ExecuteIfReady();
        }

        public override void OnExit()
        {
            base.OnExit();

            //Util.PlaySound("PlaySheath", gameObject);

            childLoc.FindChild("GhostHilt").gameObject.SetActive(true);
            childLoc.FindChild("KatanaHilt").gameObject.SetActive(false);
            childLoc.FindChild("KatanaBlade").gameObject.SetActive(false);
            childLoc.FindChild("SwordSimp").gameObject.SetActive(false);
        }
    }
}