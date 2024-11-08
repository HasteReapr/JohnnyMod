using System;
using System.Collections.Generic;
using System.Text;
using HG;
using JohnnyMod.Survivors.Johnny;
using JohnnyMod.Survivors.Johnny.Components;
using RoR2;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine;

namespace JohnnyMod.Characters.Survivors.Johnny.Components
{
    [RequireComponent(typeof(PointViewer))]
    public class HeadshotOverlay : MonoBehaviour
    {
        public GameObject visualizerPrefab;

        public PointViewer pointViewer;

        public HUD hud;

        public Dictionary<UnityObjectWrapperKey<HurtBox>, GameObject> hurtBoxToVisualizer = new Dictionary<UnityObjectWrapperKey<HurtBox>, GameObject>();

        public List<HurtBox> displayedTargets = new List<HurtBox>();

        public List<HurtBox> previousDisplayedTargets = new List<HurtBox>();

        public void Awake()
        {
            pointViewer = GetComponent<PointViewer>();
            OnTransformParentChanged();
        }

        public void OnTransformParentChanged()
        {
            hud = GetComponentInParent<HUD>();
        }

        public void OnDisable()
        {
            SetDisplayedTargets(Array.Empty<HurtBox>());
            hurtBoxToVisualizer.Clear();
        }

        public void Update()
        {
            List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            if ((bool)hud && (bool)hud.targetMaster)
            {
                TeamIndex teamIndex = hud.targetMaster.teamIndex;
                IReadOnlyList<HurtBox> readOnlySniperTargetsList = HurtBox.readOnlySniperTargetsList;
                int i = 0;
                for (int count = readOnlySniperTargetsList.Count; i < count; i++)
                {
                    HurtBox hurtBox = readOnlySniperTargetsList[i];
                    if ((bool)hurtBox.healthComponent && hurtBox.healthComponent.alive && FriendlyFireManager.ShouldDirectHitProceed(hurtBox.healthComponent, teamIndex) && (object)hurtBox.healthComponent.body != hud.targetMaster.GetBody())
                    {
                        list.Add(hurtBox);
                    }
                }
            }

            SetDisplayedTargets(list);
            list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
        }

        public void OnTargetDiscovered(HurtBox hurtBox)
        {
            if (!hurtBoxToVisualizer.ContainsKey(hurtBox))
            {
                GameObject value = pointViewer.AddElement(new PointViewer.AddElementRequest
                {
                    elementPrefab = hurtBox.healthComponent.GetComponent<CardController>() ? JohnnyAssets.cardVisualizer : visualizerPrefab,
                    target = hurtBox.transform,
                    targetWorldVerticalOffset = 0f,
                    targetWorldRadius = HurtBox.sniperTargetRadius,
                    scaleWithDistance = true
                });
                hurtBoxToVisualizer.Add(hurtBox, value);
            }
            else
            {
                Debug.LogWarning($"Already discovered hurtbox: {hurtBox}");
            }
        }

        public void OnTargetLost(HurtBox hurtBox)
        {
            if (hurtBoxToVisualizer.TryGetValue(hurtBox, out var value))
            {
                pointViewer.RemoveElement(value);
            }
        }

        public void SetDisplayedTargets(IReadOnlyList<HurtBox> newDisplayedTargets)
        {
            Util.Swap(ref displayedTargets, ref previousDisplayedTargets);
            displayedTargets.Clear();
            ListUtils.AddRange(displayedTargets, newDisplayedTargets);
            List<HurtBox> list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            List<HurtBox> list2 = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            ListUtils.FindExclusiveEntriesByReference(displayedTargets, previousDisplayedTargets, list, list2);
            foreach (HurtBox item in list2)
            {
                OnTargetLost(item);
            }

            foreach (HurtBox item2 in list)
            {
                OnTargetDiscovered(item2);
            }

            list2 = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list2);
            list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
        }
    }
}
