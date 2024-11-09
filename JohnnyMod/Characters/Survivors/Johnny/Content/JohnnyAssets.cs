using RoR2;
using UnityEngine;
using JohnnyMod.Modules;
using System;
using RoR2.Projectile;
using R2API;
using UnityEngine.Networking;
using JohnnyMod.Survivors.Johnny.Components;
using RoR2.Audio;
using UnityEngine.AddressableAssets;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine.UI;
using JohnnyMod.Characters.Survivors.Johnny.Components;

namespace JohnnyMod.Survivors.Johnny
{
    public static class JohnnyAssets
    {
        // particle effects
        public static GameObject swordSwingEffect;
        public static GameObject swordHitImpactEffect;

        public static GameObject cardPopEffect;
        public static GameObject mistFinerZap;
        public static GameObject mistFinerZap2;
        public static GameObject mistFinerZap3;
        public static GameObject mistFinerLvlUp;
        public static GameObject romanCancelEffect;

        public static GameObject bombExplosionEffect;

        //projectiles
        public static GameObject bombProjectilePrefab;

        public static GameObject cardProjectile;
        public static GameObject blackCardProjectile;

        private static AssetBundle _assetBundle;

        internal static Shader hotpoo = Resources.Load<Shader>("Shaders/Deferred/HGStandard");

        // ui stuff
        public static GameObject tensionGauge;

        public static GameObject headshotOverlay, headshotVisualizer, headshotVisualizer1, headshotVisualizer2;
        public static GameObject cardOverlay, cardVisualizer;

        public static void Init(AssetBundle assetBundle)
        {

            _assetBundle = assetBundle;

            CreateEffects();

            CreateProjectiles();

            CreateUIElements();
        }

        #region UI
        private static void CreateUIElements()
        {
            /*GameObject chargeBar = GameObject.Instantiate(_assetBundle.LoadAsset<GameObject>("ChargeBar"));
            GameObject chargeRing = GameObject.Instantiate(_assetBundle.LoadAsset<GameObject>("ChargeRing"));

            RectTransform rect = chargeBar.GetComponent<RectTransform>();

            rect.localScale = new Vector3(0.25f, 0.25f, 1f);*/
            tensionGauge = _assetBundle.LoadAsset<GameObject>("JohnnyTensionGauge");
            //tensionGauge.GetComponent<Animator>().runtimeAnimatorController = LegacyResourcesAPI.Load<RuntimeAnimatorController>("RoR2/DLC1/VoidSurvivor/animVoidSurvivorCorruptionUISimplified.controller");

            CreateCardOverlay();
            CreateHeadshotOverlay();
        }

        private static void CreateCardOverlay()
        {
            // card visual overlay
            cardOverlay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerScopeLightOverlay.prefab").WaitForCompletion().InstantiateClone("JohnnyCardOverlay", false);
            SniperTargetViewer viewer = cardOverlay.GetComponentInChildren<SniperTargetViewer>();
            cardOverlay.transform.Find("ScopeOverlay").gameObject.SetActive(false);

            cardVisualizer = viewer.visualizerPrefab.InstantiateClone("JohnnyCardVisualizer", false);
            cardVisualizer.transform.Find("Scaler/Outer").gameObject.SetActive(false);

            Image headshotImage = cardVisualizer.transform.Find("Scaler/Rectangle").GetComponent<Image>();
            headshotImage.color = Color.red;
            headshotImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/Base/Captain/texCaptainCrosshairInner.png").WaitForCompletion();

            var visualizer = viewer.gameObject.AddComponent<JohnnyTargetVisualizer>();
            visualizer.visualizerPrefab = cardVisualizer;
            MonoBehaviour.Destroy(viewer);
        }

        private static void CreateHeadshotOverlay()
        {
            // card visual overlay
            headshotOverlay = Addressables.LoadAssetAsync<GameObject>("RoR2/DLC1/Railgunner/RailgunnerScopeLightOverlay.prefab").WaitForCompletion().InstantiateClone("JohnnyHeadshotOverlay", false);
            SniperTargetViewer viewer = headshotOverlay.GetComponentInChildren<SniperTargetViewer>();
            headshotOverlay.transform.Find("ScopeOverlay").gameObject.SetActive(false);

            headshotVisualizer = viewer.visualizerPrefab.InstantiateClone("JohnnyHeadshotVisualizer", false);
            headshotVisualizer.transform.Find("Scaler/Outer").gameObject.SetActive(false);

            Image headshotImage = headshotVisualizer.transform.Find("Scaler/Rectangle").GetComponent<Image>();
            headshotImage.color = Color.cyan;
            headshotImage.sprite = Addressables.LoadAssetAsync<Sprite>("RoR2/DLC1/Railgunner/texCrosshairRailgunSniperCenterAlt.png").WaitForCompletion();

            headshotVisualizer1 = headshotVisualizer.InstantiateClone("JohnnyHeadshot1Visualizer", false);
            headshotVisualizer1.transform.Find("Scaler/Rectangle").GetComponent<Image>().color = Color.blue;

            headshotVisualizer2 = headshotVisualizer.InstantiateClone("JohnnyHeadshot2Visualizer", false);
            headshotVisualizer2.transform.Find("Scaler/Rectangle").GetComponent<Image>().color = Color.red;

            var visualizer = viewer.gameObject.AddComponent<JohnnyHeadshotVisualizer>();
            visualizer.visualizerPrefab = headshotVisualizer;
            MonoBehaviour.Destroy(viewer);
        }
        #endregion

        #region effects
        private static void CreateEffects()
        {
            CreateBombExplosionEffect();

            swordSwingEffect = _assetBundle.LoadEffect("JohnnySwordSwingEffect", true);
            swordHitImpactEffect = _assetBundle.LoadEffect("ImpactJohnnySlash");

            cardPopEffect = _assetBundle.LoadEffect("CardPop");
            mistFinerZap = _assetBundle.LoadEffect("mistFinerEffect");
            mistFinerZap2 = _assetBundle.LoadEffect("mistFinerEffectLvl2");
            mistFinerZap3 = _assetBundle.LoadEffect("mistFinerEffectLvl3");
            mistFinerLvlUp = _assetBundle.LoadEffect("mfLevelUp");
            romanCancelEffect = _assetBundle.LoadEffect("RomanCancel");
        }

        private static void CreateBombExplosionEffect()
        {
            bombExplosionEffect = _assetBundle.LoadEffect("BombExplosionEffect", "JohnnyBombExplosion");

            if (!bombExplosionEffect)
                return;

            ShakeEmitter shakeEmitter = bombExplosionEffect.AddComponent<ShakeEmitter>();
            shakeEmitter.amplitudeTimeDecay = true;
            shakeEmitter.duration = 0.5f;
            shakeEmitter.radius = 200f;
            shakeEmitter.scaleShakeRadiusWithLocalScale = false;

            shakeEmitter.wave = new Wave
            {
                amplitude = 1f,
                frequency = 40f,
                cycleOffset = 0f
            };

        }
        #endregion effects

        #region projectiles
        private static void CreateProjectiles()
        {
            CreateCardProjectile();
            Content.AddProjectilePrefab(cardProjectile);
            Content.AddCharacterBodyPrefab(cardProjectile);

            /*CreateBlackCard();
            Content.AddProjectilePrefab(blackCardProjectile);
            Content.AddCharacterBodyPrefab(blackCardProjectile);*/
        }

        private static void CreateCardProjectile()
        {
            cardProjectile = _assetBundle.LoadAsset<GameObject>("JohnCardWhite").InstantiateClone("JohnnyWhiteCardProj", true);
            cardProjectile.GetComponent<Rigidbody>().mass = 0;
            cardProjectile.GetComponent<CharacterBody>().BaseImportance = 5000;
            cardProjectile.GetComponent<CharacterBody>().doNotReassignToTeamBasedCollisionLayer = true;
            cardProjectile.gameObject.layer = LayerIndex.playerBody.intVal;
            cardProjectile.GetComponent<CharacterBody>().bodyFlags |= CharacterBody.BodyFlags.IgnoreFallDamage | CharacterBody.BodyFlags.Masterless | CharacterBody.BodyFlags.ImmuneToLava | CharacterBody.BodyFlags.IgnoreKnockback | CharacterBody.BodyFlags.ImmuneToVoidDeath;

            var cardController = cardProjectile.AddComponent<CardController>();
            cardController.projectileHealthComponent = cardProjectile.GetComponent<HealthComponent>();

            var HBG = cardProjectile.transform.GetChild(0).GetComponent<HurtBoxGroup>();

            var hurtBox = cardProjectile.transform.GetChild(0).GetChild(0).GetComponent<HurtBox>();
            hurtBox.gameObject.layer = LayerIndex.entityPrecise.intVal;
            hurtBox.hurtBoxGroup = HBG;
            hurtBox.isBullseye = true;
            hurtBox.isSniperTarget = false;

            HBG.mainHurtBox = hurtBox;
            HBG.bullseyeCount = 1;

            cardController.targetHurtbox = hurtBox;
        }

        private static void CreateCardFromGhost()
        {
            cardProjectile = Asset.CloneProjectilePrefab("VagrantTrackingBomb", "JohnnyWhiteCardProj");

            cardProjectile.GetComponent<ProjectileSimple>().lifetime = 10;
            cardProjectile.GetComponent<ProjectileSimple>().desiredForwardSpeed = 64;
            cardProjectile.GetComponent<ProjectileSimple>().updateAfterFiring = false;

            cardProjectile.GetComponent<Rigidbody>().mass = 1;
            cardProjectile.GetComponent<Rigidbody>().detectCollisions = true;
            cardProjectile.GetComponent<Rigidbody>().interpolation = RigidbodyInterpolation.Interpolate;
            cardProjectile.GetComponent<Rigidbody>().collisionDetectionMode = CollisionDetectionMode.Continuous;

            cardProjectile.GetComponent<SphereCollider>().radius = 0.1f;

            cardProjectile.GetComponent<HealthComponent>().dontShowHealthbar = true;

            cardProjectile.GetComponent<CharacterBody>().maxHealth = 1;
            cardProjectile.GetComponent<CharacterBody>().baseMaxHealth = 1;

            cardProjectile.GetComponent<TeamComponent>().teamIndex = TeamIndex.Neutral;
            cardProjectile.GetComponent<TeamComponent>().hideAllyCardDisplay = true;

            cardProjectile.GetComponent<TeamFilter>().teamIndex = TeamIndex.Neutral;
            cardProjectile.GetComponent<TeamFilter>().NetworkteamIndexInternal = 0;
            cardProjectile.GetComponent<TeamFilter>().teamIndexInternal = 0;

            cardProjectile.transform.GetChild(0).GetChild(0).GetComponent<SphereCollider>().radius = 0.1f;

            ProjectileController cardCTRL = cardProjectile.GetComponent<ProjectileController>();
            cardCTRL.ghostPrefab = _assetBundle.LoadAsset<GameObject>("JohnCardWhiteGhost");
            cardCTRL.flightSoundLoop = new LoopSoundDef();

            cardProjectile.AddComponent<CardController>();

            var cardController = cardProjectile.AddComponent<CardController>();
            cardController.projectileHealthComponent = cardProjectile.GetComponent<HealthComponent>();

            var HBG = cardProjectile.transform.GetChild(0).GetComponent<HurtBoxGroup>();

            var hurtBox = cardProjectile.transform.GetChild(0).GetChild(0).GetComponent<HurtBox>();
            hurtBox.hurtBoxGroup = HBG;

            HBG.mainHurtBox = hurtBox;
            HBG.bullseyeCount = 1;

            //get rid of EVERYTHING that we dont use
            UnityEngine.Object.Destroy(cardProjectile.transform.GetChild(1).gameObject);
            UnityEngine.Object.Destroy(cardProjectile.transform.GetChild(2).gameObject);
            UnityEngine.Object.Destroy(cardProjectile.transform.GetChild(3).gameObject);
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<ProjectileImpactExplosion>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<ProjectileDamage>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<AkGameObj>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<ProjectileTargetComponent>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<ProjectileDirectionalTargetFinder>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<ProjectileSteerTowardTarget>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<AssignTeamFilterToTeamComponent>());
            UnityEngine.Object.Destroy(cardProjectile.GetComponent<ModelLocator>());
        }

        private static void CreateBlackCard()
        {
            blackCardProjectile = _assetBundle.LoadAsset<GameObject>("JohnCardBlack").InstantiateClone("JohnnyBlackCardProj", true);

            blackCardProjectile.AddComponent<CardController>();

            var cardController = blackCardProjectile.AddComponent<CardController>();
            cardController.projectileHealthComponent = blackCardProjectile.GetComponent<HealthComponent>();

            var HBG = blackCardProjectile.GetComponent<HurtBoxGroup>();

            var hurtBox = blackCardProjectile.transform.GetChild(0).GetComponent<HurtBox>();
            hurtBox.hurtBoxGroup = HBG;

            HBG.mainHurtBox = hurtBox;
            HBG.bullseyeCount = 1;
        }

        private static void CreateBombProjectile()
        {
            //highly recommend setting up projectiles in editor, but this is a quick and dirty way to prototype if you want
            bombProjectilePrefab = Asset.CloneProjectilePrefab("CommandoGrenadeProjectile", "JohnnyBombProjectile");

            //remove their ProjectileImpactExplosion component and start from default values
            UnityEngine.Object.Destroy(bombProjectilePrefab.GetComponent<ProjectileImpactExplosion>());
            ProjectileImpactExplosion bombImpactExplosion = bombProjectilePrefab.AddComponent<ProjectileImpactExplosion>();
            
            bombImpactExplosion.blastRadius = 16f;
            bombImpactExplosion.blastDamageCoefficient = 1f;
            bombImpactExplosion.falloffModel = BlastAttack.FalloffModel.None;
            bombImpactExplosion.destroyOnEnemy = true;
            bombImpactExplosion.lifetime = 12f;
            bombImpactExplosion.impactEffect = bombExplosionEffect;
            bombImpactExplosion.lifetimeExpiredSound = Content.CreateAndAddNetworkSoundEventDef("JohnnyBombExplosion");
            bombImpactExplosion.timerAfterImpact = true;
            bombImpactExplosion.lifetimeAfterImpact = 0.1f;

            ProjectileController bombController = bombProjectilePrefab.GetComponent<ProjectileController>();

            if (_assetBundle.LoadAsset<GameObject>("JohnnyBombGhost") != null)
                bombController.ghostPrefab = _assetBundle.CreateProjectileGhostPrefab("JohnnyBombGhost");
            
            bombController.startSound = "";
        }
        #endregion projectiles

        #region dependancies
        private static GameObject CreateGhostPrefab(string ghostName)
        {
            GameObject ghostPrefab = _assetBundle.LoadAsset<GameObject>(ghostName).InstantiateClone(ghostName);
            ghostPrefab.AddComponent<NetworkIdentity>();
            ghostPrefab.AddComponent<ProjectileGhostController>();

            ConvertAllRenderersToHopooShader(ghostPrefab);

            return ghostPrefab;
        }

        internal static void ConvertAllRenderersToHopooShader(GameObject objectToConvert)
        {
            foreach (Renderer i in objectToConvert.GetComponentsInChildren<Renderer>())
            {
                if (i)
                {
                    if (i.material)
                    {
                        i.material.shader = hotpoo;
                    }
                }
            }
        }
        #endregion
    }
}
