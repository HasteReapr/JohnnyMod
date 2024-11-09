using RoR2;
using RoR2.HudOverlay;
using RoR2.Orbs;
using RoR2.Projectile;
using RoR2.UI;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;
using UnityEngine.UI;

namespace JohnnyMod.Survivors.Johnny.Components
{
    public class JohnnyTensionController : NetworkBehaviour, IOnDamageDealtServerReceiver
    {
        [SerializeField]
        [Header("UI")]
        public GameObject overlayPrefab;
        
        [SerializeField]
        public string overlayChildLocatorEntry = "CrosshairExtras";

        private const float MAX_TENSION = 100;
        private const float TENSION_PER_HIT = 3; //we multiply this by the % max health of damage dealt. so if its 10% damage its 1 tension
        private const float TENSION_PER_SECOND = 2f;

        private float _tension;
        private float _prevTension;

        private OverlayController overlayController;
        private OverlayController cardOverlayController;
        private HGTextMeshProUGUI uiTensionPerc;
        private ChildLocator overlayInstanceChildLocator;
        private List<ImageFillController> fillUIList = new List<ImageFillController>();

        private void OnEnable()
        {
            overlayPrefab = JohnnyAssets.tensionGauge;
            overlayController = HudOverlayManager.AddOverlay(gameObject, new OverlayCreationParams
            {
                prefab = overlayPrefab,
                childLocatorEntry = overlayChildLocatorEntry
            });
            overlayController.onInstanceAdded += OverlayController_onInstanceAdded;
            overlayController.onInstanceRemove += OverlayController_onInstanceRemove;

            cardOverlayController = HudOverlayManager.AddOverlay(this.gameObject, new OverlayCreationParams
            {
                prefab = JohnnyAssets.cardOverlay,
                childLocatorEntry = "ScopeContainer"
            });
        }

        private void OnDisable()
        {
            if (this.cardOverlayController != null)
            {
                HudOverlayManager.RemoveOverlay(this.cardOverlayController);
                this.cardOverlayController = null;
            }
        }

        private void OverlayController_onInstanceRemove(OverlayController arg1, GameObject arg2)
        {
            fillUIList.Remove(arg2.GetComponent<ImageFillController>());
        }

        private void OverlayController_onInstanceAdded(OverlayController arg1, GameObject arg2)
        {
            fillUIList.Add(arg2.GetComponent<ImageFillController>());
            overlayInstanceChildLocator = arg2.GetComponent<ChildLocator>();
            uiTensionPerc = arg2.GetComponentInChildren<HGTextMeshProUGUI>();
        }

        private void FixedUpdate()
        {
            //float num = (this.charBody.outOfCombat ? this.tensionGainedPerSecond : this.tensionGainedPerSecondInCombat);
            AddTension(TENSION_PER_SECOND * Time.fixedDeltaTime);
            UpdateUI();
        }
        
        private void UpdateUI()
        {
            foreach(ImageFillController imageFillCTRL in fillUIList)
            {
                if (imageFillCTRL.name == "Drain")
                {
                    imageFillCTRL.SetTValue(this._prevTension / MAX_TENSION);
                }
                else
                {
                    imageFillCTRL.SetTValue(this.tension / MAX_TENSION);
                }
            }
            if (uiTensionPerc)
            {
                StringBuilder stringBuilder = HG.StringBuilderPool.RentStringBuilder();
                stringBuilder.AppendInt(Mathf.FloorToInt(tension), 1U, 3U).Append("%");
                this.uiTensionPerc.SetText(stringBuilder);
                HG.StringBuilderPool.ReturnStringBuilder(stringBuilder);
            }
            if (this.overlayInstanceChildLocator)
            {
                this.overlayInstanceChildLocator.FindChild("RomanCancelThreshold").rotation = Quaternion.Euler(0, 0, -360 * tensionFraction);
            }
        }

        public void OnDamageDealtServer(DamageReport damageReport)
        {
            var hc = damageReport.victimBody ? damageReport.victimBody.healthComponent : null;
            if (hc != null && hc.fullCombinedHealth > 0f)
            {
                float num = damageReport.damageDealt / hc.fullCombinedHealth; // the percent of damage dealt
                float numClamped = Mathf.Clamp(num, 0.1f, 1f); // this heavily nerfs the amount of tension we get bc holy shit we were getting a lot of it
                this.AddTension(numClamped * TENSION_PER_HIT); // num * tensionPerHit which will give us the % of damage we dealt * our tension gain. This means you have to fully kill 100 enemies to get 100% tension
            }
        }

        [Server]
        public void AddTension(float amount)
        {
            if (!NetworkServer.active)
            {
                Debug.LogWarning("[Server] function 'System.Void JohnnyMod.JohnnyTensionController::AddTension(System.Single)' called on client.");
                return;
            }
            this.Network_tension = Mathf.Clamp(this.tension + amount, 0, MAX_TENSION);
        }

        public float Network_tension
        {
            get
            {
                return this._tension;
            }
            [param: In]
            set
            {
                if(NetworkServer.localClientActive && !base.syncVarHookGuard)
                {
                    base.syncVarHookGuard = true;

                    base.syncVarHookGuard = false;
                }
                base.SetSyncVar<float>(value, ref this._tension, 1U);
            }
        }

        public float tension
        {
            get
            {
                return this._tension;
            }
        }

        public float tensionFraction
        {
            get
            {
                return this._tension / MAX_TENSION;
            }
        }

        public float tensionPercent
        {
            get
            {
                return this.tensionFraction * 100;
            }
        }
    }
}
