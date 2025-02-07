using EntityStates;
using JohnnyMod.Survivors.Johnny;
using RoR2;
using RoR2.Projectile;
using UnityEngine;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    public class Deal : BaseSkillState
    {
        public static float BaseDuration = 0.65f;

        public float duration = 0.65f;

        private bool hasFired = false;
        private Ray aimRay;

        private ChildLocator childLoc;

        public override void OnEnter()
        {
            base.OnEnter();

            duration = BaseDuration / attackSpeedStat;
            aimRay = GetAimRay();

            Util.PlaySound("PlayDeal", gameObject);

            childLoc = GetModelChildLocator();
            childLoc.FindChild("GhostHilt").gameObject.SetActive(true);
            childLoc.FindChild("KatanaHilt").gameObject.SetActive(false);
            childLoc.FindChild("KatanaBlade").gameObject.SetActive(false);
            childLoc.FindChild("SwordSimp").gameObject.SetActive(false);

            PlayAnimation("Gesture, Override", "Deal", "Deal.playbackRate", this.duration);
            //this is only existing so i can do the ammend thing
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (!hasFired)
            {
                Fire();
            }

            if (isAuthority && fixedAge >= duration)
                outer.SetNextStateToMain();
        }

        private void Fire()
        {
            hasFired = true;
            if (isAuthority)
            {
                FireProjectileInfo CardInfo = new FireProjectileInfo()
                {
                    owner = gameObject,
                    damage = 1 * characterBody.damage,
                    force = 0,
                    position = aimRay.origin,
                    crit = characterBody.RollCrit(),
                    //position = FindModelChild(handString).position,
                    rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    projectilePrefab = JohnnyAssets.cardProjectile,
                    speedOverride = 64,
                    //damageTypeOverride = characterBody.HasBuff(Modules.Buffs.assassinDrugsBuff) ? (DamageType?)Modules.Projectiles.poisonDmgType : (DamageType?)Modules.Projectiles.poisonDmgType,
                };

                ProjectileManager.instance.FireProjectile(CardInfo);
            }
        }

        public override void OnExit()
        {
            base.OnExit();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.PrioritySkill;
        }
    }
}