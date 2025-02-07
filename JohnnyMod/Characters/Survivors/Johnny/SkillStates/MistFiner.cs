using EntityStates;
using JohnnyMod.Characters.Survivors.Johnny.Components;
using JohnnyMod.Survivors.Johnny;
using RoR2;
using RoR2.HudOverlay;
using RoR2.Projectile;
using RoR2.Skills;
using UnityEngine;
using UnityEngine.AddressableAssets;

namespace JohnnyMod.Survivors.Johnny.SkillStates
{
    public class MistFiner : BaseSkillState
    {
        public static float damageCoefficient = JohnnyStaticValues.mistFinerDamageCoeffecient;
        public static float procCoefficient = 1f;
        public static float baseDuration = 0.6f;
        //delay on firing is usually ass-feeling. only set this if you know what you're doing
        public static float firePercentTime = 0.0f;
        public static float recoil = 3f;
        public static float range = 75f;
        public static GameObject tracerEffectPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/Effects/Tracers/TracerGoldGat");

        private float duration;
        private float fireTime;
        private bool hasFired;
        private string muzzleString;

        private Animator animator;
        private ChildLocator childLoc;

        private float lvl1time = 1f;
        private float lvl2time = 2f;

        private bool tier1 = false;
        private bool tier2 = false;
        private OverlayController overlayController;
        private JohnnyHeadshotVisualizer headshotVisualizer;

        public override void OnEnter()
        {
            base.OnEnter();
            duration = baseDuration / attackSpeedStat;
            lvl1time = 1 / attackSpeedStat;
            lvl2time = 2 / attackSpeedStat;
            fireTime = firePercentTime * duration;
            characterBody.SetAimTimer(2f);
            muzzleString = "KatanaHilt";

            childLoc = GetModelChildLocator();
            childLoc.FindChild("GhostHilt").gameObject.SetActive(false);
            childLoc.FindChild("KatanaHilt").gameObject.SetActive(true);
            childLoc.FindChild("KatanaBlade").gameObject.SetActive(true);
            childLoc.FindChild("SwordSimp").gameObject.SetActive(true);

            animator = GetModelAnimator();

            PlayAnimation("UpperBody, Override", "MistFinerIntro");
            animator.SetBool("MistFiner.channeled", true);

            //Replaces Utility skill with Mist Finer Dash
            if (base.skillLocator.utility != null)
            {
                base.skillLocator.utility.SetSkillOverride(gameObject, JohnnyStaticValues.MistFinerDash, GenericSkill.SkillOverridePriority.Contextual);
            }
            this.overlayController = HudOverlayManager.AddOverlay(this.gameObject, new OverlayCreationParams
            {
                prefab = JohnnyAssets.headshotOverlay,
                childLocatorEntry = "ScopeContainer"
            });
            overlayController.onInstanceAdded += this.OverlayController_onInstanceAdded;
        }

        private void OverlayController_onInstanceAdded(OverlayController arg1, GameObject arg2)
        {
            headshotVisualizer = arg2.GetComponentInChildren<JohnnyHeadshotVisualizer>();
            headshotVisualizer.visualizerPrefab = JohnnyAssets.headshotVisualizer;
        }

        public override void OnExit()
        {
            base.OnExit();
            if (this.overlayController != null)
            {
                overlayController.onInstanceAdded -= this.OverlayController_onInstanceAdded;
                HudOverlayManager.RemoveOverlay(this.overlayController);
                this.overlayController = null;
            }
            animator.SetBool("MistFiner.channeled", false);
            childLoc.FindChild("GhostHilt").gameObject.SetActive(true);
            childLoc.FindChild("KatanaHilt").gameObject.SetActive(false);
            childLoc.FindChild("KatanaBlade").gameObject.SetActive(false);
            childLoc.FindChild("SwordSimp").gameObject.SetActive(false);
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (fixedAge >= lvl1time && !tier1)
            {
                tier1 = true;
                AkSoundEngine.SetRTPCValue("MFLevel", 0, gameObject);
                Util.PlaySound("PlayMistFinerLvlUp", gameObject);
                //spawn the level up effect on the sheath
                EffectData effectData = new EffectData
                {
                    origin = childLoc.FindChild("SheenSpawn").transform.position,
                    scale = 1f,
                };
                EffectManager.SpawnEffect(JohnnyAssets.mistFinerLvlUp, effectData, transmit: true);
                if (headshotVisualizer)
                    headshotVisualizer.UpdatePrefab(JohnnyAssets.headshotVisualizer1);
            }
            if (fixedAge >= lvl2time && tier1 && !tier2)
            {
                tier2 = true;
                AkSoundEngine.SetRTPCValue("MFLevel", 1, gameObject);
                Util.PlaySound("PlayMistFinerLvlUp", gameObject);
                //spawn the level up effect on the sheath
                EffectData effectData = new EffectData
                {
                    origin = childLoc.FindChild("SheenSpawn").transform.position,
                    scale = 1f,
                };
                EffectManager.SpawnEffect(JohnnyAssets.mistFinerLvlUp, effectData, transmit: true);
                if (headshotVisualizer)
                    headshotVisualizer.UpdatePrefab(JohnnyAssets.headshotVisualizer2);
            }

            if (fixedAge >= fireTime && inputBank.skill2.justReleased)
            {
                //we dont unreplace the skill until we fire, if its in OnExit you can't use stepdash
                //unreplaces the skill
                if (base.skillLocator.utility != null)
                {
                    base.skillLocator.utility.UnsetSkillOverride(gameObject, JohnnyStaticValues.MistFinerDash, GenericSkill.SkillOverridePriority.Contextual);
                    animator.SetBool("MistFiner.channeled", false);
                }
                //we dont fire until the key is released, it should make it fire 
                Fire();
            }

            if (fixedAge >= duration && isAuthority && hasFired)
            {
                outer.SetNextStateToMain();
                return;
            }
        }

        private void Fire()
        {
            if (!hasFired)
            {
                hasFired = true;

                characterBody.AddSpreadBloom(1.5f);
                EffectManager.SimpleMuzzleFlash(EntityStates.Commando.CommandoWeapon.FirePistol2.muzzleEffectPrefab, gameObject, muzzleString, false);

                animator.SetBool("MistFiner.channeled", false);
                PlayAnimation("UpperBody, Override", "MistFinerFire");

                if (isAuthority)
                {
                    Ray aimRay = GetAimRay();

                    float damage = damageCoefficient * damageStat;
                    if (tier1) damage = 10f * damageStat;
                    if (tier2) damage = 15f * damageStat; //m,ake this 2.5 :itwouldseemtroll:
                    //Mathf.Lerp(damage, damage * 2.5f, lvl2time)

                    new BulletAttack
                    {
                        bulletCount = 1,
                        aimVector = aimRay.direction,
                        origin = aimRay.origin,
                        damage = damage,
                        damageColorIndex = DamageColorIndex.Default,
                        damageType = DamageType.Generic,
                        falloffModel = BulletAttack.FalloffModel.None,
                        maxDistance = range,
                        hitMask = LayerIndex.CommonMasks.bullet,
                        minSpread = 0f,
                        maxSpread = 0f,
                        isCrit = RollCrit(),
                        owner = gameObject,
                        muzzleName = muzzleString,
                        smartCollision = true,
                        procChainMask = default,
                        procCoefficient = procCoefficient,
                        radius = 1,
                        sniper = false,
                        stopperMask = LayerIndex.world.collisionMask,
                        //tracerEffectPrefab = JohnnyAssets.mistFinerZap,
                        weapon = null,
                        spreadPitchScale = 1f,
                        spreadYawScale = 1f,
                        queryTriggerInteraction = QueryTriggerInteraction.UseGlobal,
                        hitEffectPrefab = EntityStates.Commando.CommandoWeapon.FirePistol2.hitEffectPrefab,
                        modifyOutgoingDamageCallback = delegate (BulletAttack bulletAttack, ref BulletAttack.BulletHit hitInfo, DamageInfo damageInfo)
                        {
                            if (BulletAttack.IsSniperTargetHit(in hitInfo))
                            {
                                damageInfo.damage *= 1.5f;
                                damageInfo.damageType |= DamageType.BypassArmor | DamageType.WeakPointHit;
                                damageInfo.damageColorIndex = DamageColorIndex.Sniper;

                            }
                        }
                    }.Fire();

                    EffectData effectData = new EffectData
                    {
                        origin = aimRay.origin,
                        scale = 1f,
                        rotation = Util.QuaternionSafeLookRotation(aimRay.direction),
                    };
                    GameObject mistEffect = JohnnyAssets.mistFinerZap;
                    if (tier1) mistEffect = JohnnyAssets.mistFinerZap2;
                    if (tier2) mistEffect = JohnnyAssets.mistFinerZap3;
                    EffectManager.SpawnEffect(mistEffect, effectData, transmit: true);
                    Util.PlaySound("PlayMistFiner", gameObject);
                }
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Stun;
        }
    }
}