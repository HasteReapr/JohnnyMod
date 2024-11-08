using System;
using System.Collections.Generic;
using System.Text;
using HG;
using JohnnyMod.Survivors.Johnny;
using JohnnyMod.Survivors.Johnny.Components;
using JohnnyMod.Survivors.Johnny.SkillStates;
using RoR2;
using RoR2.HudOverlay;
using RoR2.UI;
using UnityEngine;

namespace JohnnyMod.Characters.Survivors.Johnny.Components
{
    [RequireComponent(typeof(PointViewer))]
    public class JohnnyTargetVisualizer : MonoBehaviour
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
            var list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            if (hud && hud.targetBodyObject)
            {
                foreach (var hurtBox in CardController.cardHurtBoxList)
                {
                    if (hurtBox && hurtBox.healthComponent && hurtBox.healthComponent.alive && Vector3.Distance(hurtBox.transform.position, hud.targetBodyObject.transform.position) < MistFiner.range)
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
                hurtBoxToVisualizer.Add(hurtBox, pointViewer.AddElement(new PointViewer.AddElementRequest
                {
                    elementPrefab = visualizerPrefab,
                    target = hurtBox.transform,
                    targetWorldVerticalOffset = 0f,
                    targetWorldRadius = HurtBox.sniperTargetRadius,
                    scaleWithDistance = true
                }));
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
            var list = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            var list2 = CollectionPool<HurtBox, List<HurtBox>>.RentCollection();
            ListUtils.FindExclusiveEntriesByReference(displayedTargets, previousDisplayedTargets, list, list2);
            foreach (var item in list2)
            {
                OnTargetLost(item);
            }

            foreach (var item2 in list)
            {
                OnTargetDiscovered(item2);
            }

            list2 = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list2);
            list = CollectionPool<HurtBox, List<HurtBox>>.ReturnCollection(list);
        }
    }
}
