using HG;
using RoR2;
using RoR2.Orbs;
using RoR2.Projectile;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using UnityEngine;
using UnityEngine.Networking;
using R2API;

namespace JohnnyMod.Survivors.Johnny.Components
{
    public class CardController : NetworkBehaviour, IOnIncomingDamageServerReceiver, IProjectileImpactBehavior
    {
        public HealthComponent projectileHealthComponent;
        public JohnnyTensionController JohnnyStandee;
        public GameObject Johnnybody;

        private bool gravityStop = false;
        private bool gravityStarted = false;
        private bool startFuse = false;
        private float gravityCD = 0.75f;
        private float fuseTime = 0.1f;
        private float babyBoomFuse = 0.6f;
        private bool popBabies = false;
        private int boomCount = 0;
        private bool inAir = true;

        private TeamIndex teamIndex = TeamIndex.Neutral;
        private int origLayer, origLayerHitbox;

        private DamageInfo dmgInfo = null;

        private ProjectileSimple projSimp;
        private Rigidbody rigidBody;
        private HurtBox targetHurtbox;
        
        public static List<HurtBox> cardHurtBoxList = new List<HurtBox>();

        private void Start()
        {
            rigidBody = this.GetComponent<Rigidbody>();
            projSimp = this.GetComponent<ProjectileSimple>();

            this.origLayer = this.gameObject.layer;
            this.origLayerHitbox = this.transform.GetChild(0).GetChild(0).gameObject.layer;

            this.gameObject.layer = LayerIndex.fakeActor.intVal;

            this.StartCoroutine(nameof(SwitchLayer));
        }


        private IEnumerator SwitchLayer()
        {
            yield return new WaitForFixedUpdate();
            yield return new WaitForFixedUpdate();

            if (this.TryGetComponent<HealthComponent>(out var hc))
            {
                IOnIncomingDamageServerReceiver value = this;
                if (!hc.onIncomingDamageReceivers.Contains(value))
                    ArrayUtils.ArrayAppend(ref hc.onIncomingDamageReceivers, in value);
            }

            this.gameObject.layer = origLayer;
            this.transform.GetChild(0).GetChild(0).gameObject.layer = origLayerHitbox;
        }

        private void OnEnable()
        {
            this.targetHurtbox = this.transform.GetChild(0).GetChild(0).GetComponent<HurtBox>();
            cardHurtBoxList.Add(this.targetHurtbox);
        }

        private void OnDisable()
        {
            cardHurtBoxList.Remove(this.targetHurtbox);
        }

        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            if (damageInfo.attacker && damageInfo.inflictor != this.gameObject &&
               (damageInfo.attacker.GetComponent<JohnnyTensionController>() ||
                damageInfo.attacker.GetComponent<CardController>()))
            {
                PopCard(damageInfo);
            }
            else damageInfo.rejected = true;
        }

        private void FixedUpdate()
        {
            gravityCD -= Time.fixedDeltaTime;

            //Check for gravityStarted so we can turn this off as soon as it collides with something
            if(gravityCD <= 0 && !gravityStarted)
            {
                gravityStop = true;
            }

            if (gravityStop && !gravityStarted && !startFuse)
            {
                projSimp.desiredForwardSpeed = 0;
                rigidBody.velocity = Vector3.zero;
                rigidBody.isKinematic = false;
                rigidBody.mass = 1;
                rigidBody.useGravity = true;
                var quat = transform.rotation.eulerAngles;
                transform.rotation = Quaternion.Euler(90, quat.y, quat.z);
                this.gravityStarted = true;
            }

            if (startFuse)
            {
                fuseTime -= Time.fixedDeltaTime;
            }

            if (popBabies)
            {
                babyBoomFuse -= Time.fixedDeltaTime;
            }

            if(fuseTime <= 0 && !popBabies)
            {
                Kaboom(dmgInfo);
            }
            
            if(babyBoomFuse <= 0 && popBabies)
            {
                BabyKaboom(dmgInfo);
            }
        }

        public void PopCard(DamageInfo damageInfo)
        {
            if (dmgInfo == null)
            {
                // this is used later for the blast attacks, so make a copy
                dmgInfo = new DamageInfo()
                {
                    attacker = damageInfo.attacker,
                    canRejectForce = damageInfo.canRejectForce,
                    crit = damageInfo.crit,
                    damage = damageInfo.damage,
                    delayedDamageSecondHalf = damageInfo.delayedDamageSecondHalf,
                    damageColorIndex = damageInfo.damageColorIndex,
                    damageType = damageInfo.damageType,
                    dotIndex = damageInfo.dotIndex,
                    force = damageInfo.force,
                    inflictor = damageInfo.inflictor,
                    position = damageInfo.position,
                    procChainMask = damageInfo.procChainMask,
                    procCoefficient = damageInfo.procCoefficient,
                    rejected = damageInfo.rejected,
                };
                // modify incoming damage so its not all applied to the card
                (damageInfo.GetModdedDamageTypeHolder() ?? new DamageAPI.ModdedDamageTypeHolder()).CopyTo(dmgInfo);
                damageInfo.procCoefficient = 0f;
                damageInfo.force = Vector3.zero;

                teamIndex = dmgInfo.attacker.GetComponent<TeamComponent>().teamIndex;
            }
            else if (startFuse && !popBabies)
            {
                dmgInfo.damage += damageInfo.damage * 0.5f;
                dmgInfo.damageType |= damageInfo.damageType;
                var holder = damageInfo.GetModdedDamageTypeHolder();
                if (holder != null)
                    dmgInfo.GetModdedDamageTypeHolder().Add(holder);
            }
        }

        public void Kaboom(DamageInfo damageInfo)
        {
            popBabies = true;

            float dmgMult = inAir ? 2.5f : 2f;
            var explode = new BlastAttack
            {
                baseDamage = damageInfo.damage * dmgMult,
                radius = 15f,
                baseForce = 0f,
                crit = damageInfo.crit,
                procCoefficient = 1f,
                attacker = damageInfo.attacker,
                inflictor = base.gameObject,
                damageType = damageInfo.damageType | DamageType.Stun1s,
                damageColorIndex = DamageColorIndex.WeakPoint,
                teamIndex = teamIndex,
                procChainMask = damageInfo.procChainMask,
                falloffModel = BlastAttack.FalloffModel.Linear,
                position = transform.position,
            };
            
            dmgInfo.GetModdedDamageTypeHolder().CopyTo(explode);

            explode.Fire();

            //make the card stop so the baby pops dont fall down :pensive:
            rigidBody.velocity = Vector3.zero;
            rigidBody.mass = 0;
            rigidBody.useGravity = false;

            EffectData effectData = new EffectData
            {
                origin = transform.position,
                scale = 1f
            };
            EffectManager.SpawnEffect(JohnnyAssets.cardPopEffect, effectData, transmit: true);
            Util.PlaySound("PlayCardPop", gameObject);
        }

        public void BabyKaboom(DamageInfo damageInfo)
        {
            BlastAttack explode = new BlastAttack
            {
                baseDamage = damageInfo.damage * 0.1f,
                radius = 10f,
                baseForce = 0f,
                crit = damageInfo.crit,
                procCoefficient = 1,
                attacker = damageInfo.attacker,
                inflictor = base.gameObject,
                damageType = damageInfo.damageType | DamageType.Stun1s | DamageType.LunarRuin,
                damageColorIndex = DamageColorIndex.WeakPoint,
                teamIndex = teamIndex,
                procChainMask = damageInfo.procChainMask,
                falloffModel = BlastAttack.FalloffModel.None,
                position = transform.position + (Random.insideUnitSphere * 2f)
            };
            dmgInfo.GetModdedDamageTypeHolder().CopyTo(explode);

            explode.Fire();

            boomCount++;
            babyBoomFuse = 0.05f;

            if (boomCount > 10)
            {
                Destroy(base.gameObject);
            }
        }

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            inAir = false;
        }

        private void OnEnter()
        {
            gravityCD = 0.75f;
            gravityStarted = false;
            gravityStop = true;

            fuseTime = 0.1f;
            startFuse = false;
            dmgInfo = null;
            boomCount = 0;
            popBabies = false;

            inAir = true;

            //this.GetComponent<TeamFilter>().teamIndex = TeamIndex.Neutral;
            //disable gravity when we are initially spawned, will be re-enabled later
            rigidBody = this.GetComponent<Rigidbody>();
            rigidBody.useGravity = false;
            rigidBody.mass = 0;

            projSimp = this.GetComponent<ProjectileSimple>();

            this.GetComponent<TeamComponent>().teamIndex = TeamIndex.Neutral;
            this.GetComponent<TeamFilter>().teamIndex = TeamIndex.Neutral;
        }
    }
}