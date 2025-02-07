using BepInEx.Configuration;
using JohnnyMod.Modules;
using JohnnyMod.Modules.Characters;
using JohnnyMod.Survivors.Johnny.Components;
using JohnnyMod.Survivors.Johnny.SkillStates;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using UnityEngine;

namespace JohnnyMod.Survivors.Johnny
{
    public class JohnnySurvivor : SurvivorBase<JohnnySurvivor>
    {
        //used to load the assetbundle for this character. must be unique
        public override string assetBundleName => "JohnnyAssets"; //if you do not change this, you are giving permission to deprecate the mod

        //the name of the prefab we will create. conventionally ending in "Body". must be unique
        public override string bodyName => "JohnnyBody"; //if you do not change this, you get the point by now

        //name of the ai master for vengeance and goobo. must be unique
        public override string masterName => "JohnnyMonsterMaster"; //if you do not

        //the names of the prefabs you set up in unity that we will use to build your character
        public override string modelPrefabName => "mdlJohnny";
        public override string displayPrefabName => "JohnnyDisplay";

        public const string Johnny_PREFIX = JohnnyPlugin.DEVELOPER_PREFIX + "_Johnny_";

        public override Type characterDeathState => typeof(JohnnyDeath);

        //used when registering your survivor's language tokens
        public override string survivorTokenPrefix => Johnny_PREFIX;
        
        public override BodyInfo bodyInfo => new BodyInfo
        {
            bodyName = bodyName,
            bodyNameToken = Johnny_PREFIX + "NAME",
            subtitleNameToken = Johnny_PREFIX + "SUBTITLE",

            characterPortrait = assetBundle.LoadAsset<Texture>("texJohnnyIcon"),
            bodyColor = Color.white,
            sortPosition = 100,

            crosshair = Asset.LoadCrosshair("Standard"),
            podPrefab = LegacyResourcesAPI.Load<GameObject>("Prefabs/NetworkedObjects/SurvivorPod"),

            maxHealth = 100,
            healthRegen = 4.5f,
            moveSpeed = 10,
            armor = 5f,

            jumpCount = 2,
        };

        public override CustomRendererInfo[] customRendererInfos => new CustomRendererInfo[]
        {
                //hard stuff
                new CustomRendererInfo
                {
                    childName = "Hands",
                    material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                new CustomRendererInfo
                {
                    childName = "Body",
                    //material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                new CustomRendererInfo
                {
                    childName = "BuckleSmall",
                    //material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                new CustomRendererInfo
                {
                    childName = "Belt",
                    //material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                new CustomRendererInfo
                {
                    childName = "BuckleBig",
                    //material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                new CustomRendererInfo
                {
                    childName = "Pants",
                    //material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                new CustomRendererInfo
                {
                    childName = "Shoes",
                    //material = assetBundle.LoadMaterial("mtrlJohnHard"),
                },
                //soft stuff
                new CustomRendererInfo
                {
                    childName = "Hat",
                    material = assetBundle.LoadMaterial("mtrlJohnSoft"),
                },
                new CustomRendererInfo
                {
                    childName = "Jacket",
                    //material = assetBundle.LoadMaterial("mtrlJohnSoft"),
                },
                //weapon
                new CustomRendererInfo
                {
                    childName = "KatanaBlade",
                    material = assetBundle.LoadMaterial("mtrlJohnWeap"),
                },
                new CustomRendererInfo
                {
                    childName = "KatanaHilt",
                    //material = assetBundle.LoadMaterial("matJohnny"),
                },
                new CustomRendererInfo
                {
                    childName = "KatanaSheath",
                    //material = assetBundle.LoadMaterial("matJohnny"),
                },
                new CustomRendererInfo
                {
                    childName = "SwordSimp",
                    //material = assetBundle.LoadMaterial("matJohnny"),
                },
        };

        public override UnlockableDef characterUnlockableDef => JohnnyUnlockables.characterUnlockableDef;
        
        public override ItemDisplaysBase itemDisplays => new JohnnyItemDisplays();

        //public override Type characterDeathState => typeof(JohnnyDeath);

        //set in base classes
        public override AssetBundle assetBundle { get; protected set; }

        public override GameObject bodyPrefab { get; protected set; }
        public override CharacterBody prefabCharacterBody { get; protected set; }
        public override GameObject characterModelObject { get; protected set; }
        public override CharacterModel prefabCharacterModel { get; protected set; }
        public override GameObject displayPrefab { get; protected set; }

        public override void Initialize()
        {
            //uncomment if you have multiple characters
            //ConfigEntry<bool> characterEnabled = Config.CharacterEnableConfig("Survivors", "Johnny");

            //if (!characterEnabled.Value)
            //    return;

            base.Initialize();
        }

        public override void InitializeCharacter()
        {
            //need the character unlockable before you initialize the survivordef
            JohnnyUnlockables.Init();

            base.InitializeCharacter();

            JohnnyConfig.Init();
            JohnnyStates.Init();
            JohnnyTokens.Init();

            JohnnyAssets.Init(assetBundle);
            JohnnyBuffs.Init(assetBundle);

            InitializeEntityStateMachines();
            InitializeSkills();
            InitializeSkins();
            InitializeCharacterMaster();

            AdditionalBodySetup();

            AddHooks();
        }

        private void AdditionalBodySetup()
        {
            AddHitboxes();
            bodyPrefab.AddComponent<JohnnyTensionController>();
            bodyPrefab.GetComponent<CharacterBody>().bodyFlags |= CharacterBody.BodyFlags.SprintAnyDirection;
            //bodyPrefab.AddComponent<JohnnyRCControl>();

            if (displayPrefab) displayPrefab.AddComponent<MenuSoundComponent>();
        }

        public void AddHitboxes()
        {
            //example of how to create a HitBoxGroup. see summary for more details
            Prefabs.SetupHitBoxGroup(characterModelObject, "SwordGroup", "SwordHitbox");
            //Prefabs.SetupHitBoxGroup(characterModelObject, "KatanaGroup", "KatanaHitbox");
            //Prefabs.SetupHitBoxGroup(characterModelObject, "MistFinerGroup", "MistFinerHitbox");
        }

        public override void InitializeEntityStateMachines() 
        {
            //clear existing state machines from your cloned body (probably commando)
            //omit all this if you want to just keep theirs
            Prefabs.ClearEntityStateMachines(bodyPrefab);

            //the main "Body" state machine has some special properties
            //Prefabs.AddMainEntityStateMachine(bodyPrefab, "Body", typeof(EntityStates.GenericCharacterMain), typeof(EntityStates.SpawnTeleporterState));
            Prefabs.AddMainEntityStateMachine(bodyPrefab, "Body", typeof(JohnnyMainState), typeof(EntityStates.SpawnTeleporterState));
            //if you set up a custom main characterstate, set it up here
                //don't forget to register custom entitystates in your JohnnyStates.cs

            Prefabs.AddMainEntityStateMachine(bodyPrefab, "RomanCancel", typeof(RomanIdle), typeof(RomanIdle), false);

            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon");
            Prefabs.AddEntityStateMachine(bodyPrefab, "Weapon2");
        }

        #region skills
        public override void InitializeSkills()
        {
            //remove the genericskills from the commando body we cloned
            Skills.ClearGenericSkills(bodyPrefab);
            //add our own
            //AddPassiveSkill();
            CreatePassives();
            AddPrimarySkills();
            AddSecondarySkills();
            AddUtiitySkills();
            AddSpecialSkills();
        }

        private void CreatePassives()
        {
            //this exists so we can assign Vault to the passive skill family.
            GenericSkill passiveSkill = Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, "LOADOUT_SKILL_PASSIVE", "johnnypassive");

            SkillDef stepDashDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                //skillName = "JohnnyDash",
                //skillNameToken = Johnny_PREFIX + "UTILITY_DASH_NAME",
                //skillDescriptionToken = Johnny_PREFIX + "UTILITY_DASH_DESCRIPTION",
                //skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityIcon"),
                skillName = "JohnnyDash",
                skillNameToken = Johnny_PREFIX + "PASSIVE_NAME",
                skillDescriptionToken = Johnny_PREFIX + "PASSIVE_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texSpecialIcon"), //texPassiveIcon
                keywordTokens = new string[] { Johnny_PREFIX + "KEYWORD_TENSION", Johnny_PREFIX + "KEYWORD_STEPDASH", Johnny_PREFIX + "KEYWORD_RC" },

                activationState = new EntityStates.SerializableEntityStateType(typeof(StepDash)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 1.25f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = true,
            });

            JohnnyStaticValues.StepDash = stepDashDef;

            Skills.AddSkillsToFamily(passiveSkill.skillFamily, stepDashDef);
        }

        //if this is your first look at skilldef creation, take a look at Secondary first
        private void AddPrimarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Primary);

            //the primary skill is created using a constructor for a typical primary
            //it is also a SteppedSkillDef. Custom Skilldefs are very useful for custom behaviors related to casting a skill. see ror2's different skilldefs for reference
            SteppedSkillDef primarySkillDef1 = Skills.CreateSkillDef<SteppedSkillDef>(new SkillDefInfo
                (
                    "JohnnySlash",
                    Johnny_PREFIX + "PRIMARY_SLASH_NAME",
                    Johnny_PREFIX + "PRIMARY_SLASH_DESCRIPTION",
                    assetBundle.LoadAsset<Sprite>("texPrimaryIcon"),
                    new EntityStates.SerializableEntityStateType(typeof(SkillStates.SlashCombo)),
                    "Weapon2",
                    true
                ));
            //custom Skilldefs can have additional fields that you can set manually
            primarySkillDef1.stepCount = 3;
            primarySkillDef1.stepGraceDuration = 0.5f;

            Skills.AddPrimarySkills(bodyPrefab, primarySkillDef1);
        }

        private void AddSecondarySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Secondary);

            //here is a basic skill def with all fields accounted for
            SkillDef mistFinerDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "JohnnyMistFiner",
                skillNameToken = Johnny_PREFIX + "SECONDARY_MIST_NAME",
                skillDescriptionToken = Johnny_PREFIX + "SECONDARY_MIST_DESCRIPTION",
                keywordTokens = new string[] { "KEYWORD_AGILE" },
                skillIcon = assetBundle.LoadAsset<Sprite>("texSecondaryIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.MistFiner)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 3f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = true,
                beginSkillCooldownOnSkillEnd = true,

                isCombatSkill = true,
                canceledFromSprinting = true,
                cancelSprintingOnActivation = true,
                forceSprintDuringState = false,

            });

            Skills.AddSecondarySkills(bodyPrefab, mistFinerDef);
        }

        private void AddUtiitySkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Utility);

            SkillDef vaultDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "JohnnyVault",
                skillNameToken = Johnny_PREFIX + "UTILITY_VAULT_NAME",
                skillDescriptionToken = Johnny_PREFIX + "UTILITY_VAULT_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texUtilityVault"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(Vault)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.PrioritySkill,

                baseRechargeInterval = 4f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = false,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });

            JohnnyStaticValues.Vault = vaultDef;
            Skills.AddUtilitySkills(bodyPrefab, vaultDef);


            //here's a skilldef of a typical movement skill.
            SkillDef mistStepDef = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "JohnnyMistFinerDash",
                skillNameToken = Johnny_PREFIX + "UTILITY_STEPDASH_NAME",
                skillDescriptionToken = Johnny_PREFIX + "UTILITY_STEPDASH_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texMistFinderDashIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(MistFinerDash)),
                activationStateMachineName = "Weapon2",
                interruptPriority = EntityStates.InterruptPriority.Stun,

                baseRechargeInterval = 4f,
                baseMaxStock = 1,

                rechargeStock = 1,
                requiredStock = 1,
                stockToConsume = 1,

                resetCooldownTimerOnUse = false,
                fullRestockOnAssign = true,
                dontAllowPastMaxStocks = false,
                mustKeyPress = false,
                beginSkillCooldownOnSkillEnd = false,

                isCombatSkill = false,
                canceledFromSprinting = true,
                cancelSprintingOnActivation = false,
                forceSprintDuringState = false,
            });

            JohnnyStaticValues.MistFinerDash = mistStepDef;
        }

        private void AddSpecialSkills()
        {
            Skills.CreateGenericSkillWithSkillFamily(bodyPrefab, SkillSlot.Special);

            //a basic skill. some fields are omitted and will just have default values
            SkillDef specialSkillDef1 = Skills.CreateSkillDef(new SkillDefInfo
            {
                skillName = "JohnnyDeal",
                skillNameToken = Johnny_PREFIX + "SPECIAL_DEAL_NAME",
                skillDescriptionToken = Johnny_PREFIX + "SPECIAL_DEAL_DESCRIPTION",
                skillIcon = assetBundle.LoadAsset<Sprite>("texSpecialIcon"),

                activationState = new EntityStates.SerializableEntityStateType(typeof(SkillStates.Deal)),
                //setting this to the "weapon2" EntityStateMachine allows us to cast this skill at the same time primary, which is set to the "weapon" EntityStateMachine
                activationStateMachineName = "Weapon",
                interruptPriority = EntityStates.InterruptPriority.Skill,

                baseMaxStock = 2,
                baseRechargeInterval = 4f,

                isCombatSkill = true,
                mustKeyPress = false,
            });

            Skills.AddSpecialSkills(bodyPrefab, specialSkillDef1);
        }
        #endregion skills
        
        #region skins
        public override void InitializeSkins()
        {
            ModelSkinController skinController = prefabCharacterModel.gameObject.AddComponent<ModelSkinController>();
            ChildLocator childLocator = prefabCharacterModel.GetComponent<ChildLocator>();

            CharacterModel.RendererInfo[] defaultRendererinfos = prefabCharacterModel.baseRendererInfos;

            List<SkinDef> skins = new List<SkinDef>();

            #region DefaultSkin
            //this creates a SkinDef with all default fields
            SkinDef defaultSkin = Skins.CreateSkinDef("DEFAULT_SKIN",
                assetBundle.LoadAsset<Sprite>("texMainSkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject);

            //these are your Mesh Replacements. The order here is based on your CustomRendererInfos from earlier
                //pass in meshes as they are named in your assetbundle
            //currently not needed as with only 1 skin they will simply take the default meshes
                //uncomment this when you have another skin
            //defaultSkin.meshReplacements = Modules.Skins.getMeshReplacements(assetBundle, defaultRendererinfos,
            //    "meshJohnnySword",
            //    "meshJohnnyGun",
            //    "meshJohnny");

            //add new skindef to our list of skindefs. this is what we'll be passing to the SkinController
            skins.Add(defaultSkin);
            #endregion

            //uncomment this when you have a mastery skin
            #region MasterySkin
            
            SkinDef masterySkin = Modules.Skins.CreateSkinDef(Johnny_PREFIX + "MASTERY_SKIN_NAME",
                assetBundle.LoadAsset<Sprite>("texMasterySkin"),
                defaultRendererinfos,
                prefabCharacterModel.gameObject,
                JohnnyUnlockables.masterySkinUnlockableDef);

            masterySkin.rendererInfos[0].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");
            masterySkin.rendererInfos[1].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");
            masterySkin.rendererInfos[2].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");
            masterySkin.rendererInfos[3].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");
            masterySkin.rendererInfos[4].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");
            masterySkin.rendererInfos[5].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");
            masterySkin.rendererInfos[6].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnHardMastery");

            masterySkin.rendererInfos[7].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnSoftMastery");
            masterySkin.rendererInfos[8].defaultMaterial = assetBundle.LoadMaterial("mtrlJohnSoftMastery");

            skins.Add(masterySkin);
            
            #endregion

            skinController.skins = skins.ToArray();
        }
        #endregion skins

        //Character Master is what governs the AI of your character when it is not controlled by a player (artifact of vengeance, goobo)
        public override void InitializeCharacterMaster()
        {
            //you must only do one of these. adding duplicate masters breaks the game.

            //if you're lazy or prototyping you can simply copy the AI of a different character to be used
            //Modules.Prefabs.CloneDopplegangerMaster(bodyPrefab, masterName, "Merc");

            //how to set up AI in code
            JohnnyAI.Init(bodyPrefab, masterName);

            //how to load a master set up in unity, can be an empty gameobject with just AISkillDriver components
            //assetBundle.LoadMaster(bodyPrefab, masterName);
        }

        private void AddHooks()
        {
            R2API.RecalculateStatsAPI.GetStatCoefficients += RecalculateStatsAPI_GetStatCoefficients;
        }

        private void RecalculateStatsAPI_GetStatCoefficients(CharacterBody sender, R2API.RecalculateStatsAPI.StatHookEventArgs args)
        {

            if (sender.HasBuff(JohnnyBuffs.armorBuff))
            {
                args.armorAdd += 300;
            }
        }
    }
}