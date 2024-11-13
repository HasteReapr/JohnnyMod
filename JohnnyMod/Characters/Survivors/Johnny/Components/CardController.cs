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
        public HurtBox targetHurtbox;

        private bool gravityStarted = false;
        private float gravityCD = 0.75f;
        private float fuseTime = 0.1f;
        private bool timeForKaboom = false;
        private int boomCount = 0;
        private bool inAir = true;

        private BlastAttack blastAttack = null;

        private ProjectileSimple projSimp;
        private Rigidbody rigidBody;
        
        public static List<HurtBox> cardHurtBoxList = new List<HurtBox>();

        private void Start()
        {
            rigidBody = this.GetComponent<Rigidbody>();
            projSimp = this.GetComponent<ProjectileSimple>();
        }

        private void OnEnable()
        {
            cardHurtBoxList.Add(this.targetHurtbox);
        }

        private void OnDisable()
        {
            cardHurtBoxList.Remove(this.targetHurtbox);
        }

        private void FixedUpdate()
        {
            if (this.projSimp)
                this.projSimp.SetLifetime(projSimp.lifetime);

            //Check for gravityStarted so we can turn this off as soon as it collides with something
            if (!gravityStarted)
            {
                gravityCD -= Time.fixedDeltaTime;
                if (gravityCD <= 0f)
                    StartGravity();
            }

            if (timeForKaboom)
            {
                fuseTime -= Time.fixedDeltaTime;

                if (fuseTime <= 0)
                {
                    if (boomCount == 0)
                        Kaboom();
                    else
                        BabyKaboom();
                }

                if (boomCount > 10)
                {
                    Destroy(base.gameObject);
                }
            }
        }

        private void StartGravity()
        {
            if (projSimp)
                projSimp.desiredForwardSpeed = 0;

            if (rigidBody)
            {
                rigidBody.velocity = Vector3.zero;
                rigidBody.isKinematic = false;
                rigidBody.mass = 1;
                rigidBody.useGravity = true;
            }
            var quat = transform.rotation.eulerAngles;
            transform.rotation = Quaternion.Euler(90, quat.y, quat.z);
            this.gravityStarted = true;
        }

        private void StopGravity()
        {
            if (rigidBody)
            {
                //make the card stop so the baby pops dont fall down :pensive:
                rigidBody.velocity = Vector3.zero;
                rigidBody.mass = 0;
                rigidBody.useGravity = false;
            }

            this.gravityStarted = true;
        }

        public void PopCard(DamageInfo damageInfo)
        {
            if (blastAttack == null)
            {
                // this is used later for the blast attacks, so make a copy
                blastAttack = new BlastAttack
                {
                    baseDamage = damageInfo.damage,
                    radius = 15f,
                    baseForce = 0f,
                    crit = damageInfo.crit,
                    procCoefficient = 1f,
                    attacker = damageInfo.attacker,
                    inflictor = base.gameObject,
                    damageType = damageInfo.damageType | DamageType.Stun1s,
                    damageColorIndex = DamageColorIndex.WeakPoint,
                    teamIndex = damageInfo.attacker.GetComponent<TeamComponent>().teamIndex,
                    procChainMask = damageInfo.procChainMask,
                    falloffModel = BlastAttack.FalloffModel.Linear,
                    position = transform.position,
                };
                (damageInfo.GetModdedDamageTypeHolder() ?? new DamageAPI.ModdedDamageTypeHolder()).CopyTo(blastAttack);

                StopGravity();

                this.timeForKaboom = true;

                if (this.projectileHealthComponent)
                    this.projectileHealthComponent.Suicide();
            }
            else if (boomCount == 0)
            {
                blastAttack.baseDamage += damageInfo.damage * 0.25f;
                blastAttack.damageType |= damageInfo.damageType;
                blastAttack.crit |= damageInfo.crit;

                var holder = damageInfo.GetModdedDamageTypeHolder();
                if (holder != null)
                    blastAttack.GetModdedDamageTypeHolder().Add(holder);
            }
        }

        public void Kaboom()
        {
            blastAttack.baseDamage *= inAir ? 2f : 1.5f;
            blastAttack.position = transform.position;
            blastAttack.Fire();

            boomCount++;
            fuseTime = 0.6f;

            // modify attack once for the little kabooms, dont modify in babyKaboom 10x like an idiot
            blastAttack.baseDamage *= 0.05f;
            blastAttack.falloffModel = BlastAttack.FalloffModel.None;

            EffectManager.SpawnEffect(JohnnyAssets.cardPopEffect, new EffectData
            {
                origin = transform.position,
                scale = 1f
            }, transmit: true);
            Util.PlaySound("PlayCardPop", gameObject);
        }

        public void BabyKaboom()
        {
            var pos = transform.position + (Random.insideUnitSphere * 5f);
            blastAttack.position = pos;
            blastAttack.Fire();

            boomCount++;
            fuseTime = 0.05f;

            EffectManager.SpawnEffect(JohnnyAssets.cardPopEffect, new EffectData
            {
                origin = pos,
                scale = 0.2f
            }, transmit: true);
        }

        public void OnIncomingDamageServer(DamageInfo damageInfo)
        {
            // filter out non-johnny, and only accept certain inflictors so things like fireworks/frost relic dont pop it
            if (damageInfo.attacker && damageInfo.attacker.GetComponent<JohnnyTensionController>() && damageInfo.inflictor != this.gameObject &&
               (!damageInfo.inflictor || damageInfo.inflictor == damageInfo.attacker || damageInfo.inflictor.GetComponent<CardController>()))
            {
                PopCard(damageInfo);
            }
            
            damageInfo.rejected = true;
        }

        public void OnProjectileImpact(ProjectileImpactInfo impactInfo)
        {
            inAir = false;

            // apply hurt state, its like stun but mostly helps with attack interruption good
            var body = Util.HurtBoxColliderToBody(impactInfo.collider);
            if (body && body.TryGetComponent<SetStateOnHurt>(out var setStateOnHurt))
                setStateOnHurt.SetPain();
        }
        /*
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
        }*/
    }
}