using BepInEx;
using EntityStates;
using EntityStates.ImpBossPlayer;
using EntityStates.ImpBossMonster;
using KinematicCharacterController;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Projectile;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.Networking;


namespace KevinfromHP.KevinsAdditions
{
    public class ImpPlayerAdjustments
    {
        public static GameObject bodyPrefab;
        public static SkillLocator component;

        public static void AddExtras()
        {
            CreatePrefab();
            RegisterStates();
            SkillSetup();

            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list)
            {
                list.Add(bodyPrefab);
            };

            On.EntityStates.ImpBossMonster.FireVoidspikes.OnEnter += On_Enter;
        }

        internal static void CreatePrefab()
        {
            bodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/ImpBossBody"), "ImpBossPlayerBody", true, "D:\\All Coding Stuff\\RoR2 Modding\\KevinsAdditions\\Items\\Eqp\\ImpExtras.cs", "AddExtras");
            bodyPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }
        static void RegisterStates()
        {
            LoadoutAPI.AddSkill(typeof(FireVoidspikes));
            LoadoutAPI.AddSkill(typeof(GroundPound));
            LoadoutAPI.AddSkill(typeof(BlinkState));
            LoadoutAPI.AddSkill(typeof(RevertFormState));
        }
        static void SkillSetup()
        {
            /*foreach (GenericSkill obj in bodyPrefab.GetComponentsInChildren<GenericSkill>())
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }*/

            component = bodyPrefab.GetComponent<SkillLocator>();
            //PrimarySetup();
            //SecondarySetup();
            //UtilitySetup();
            SpecialSetup();
        }

        static void PrimarySetup()
        {
            LanguageAPI.Add("PLAYERIMPOVERLORD_PRIMARY_VOIDSPIKES_NAME", "Void Spikes");
            LanguageAPI.Add("PLAYERIMPOVERLORD_PRIMARY_VOIDSPIKES_DESCRIPTION", "Fire 6 \"Void Spikes\", dealing <style=cIsDamage>400% damage</style> and <style=cDeath>bleed</style> on hit.");

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(FireVoidspikes));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 3;
            mySkillDef.baseRechargeInterval = 4f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = false;
            mySkillDef.interruptPriority = InterruptPriority.Death;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = true;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon1;
            mySkillDef.skillDescriptionToken = "PLAYERIMPOVERLORD_PRIMARY_VOIDSPIKES_DESCRIPTION";
            mySkillDef.skillName = "PLAYERIMPOVERLORD_PRIMARY_VOIDSPIKES_NAME";
            mySkillDef.skillNameToken = "PLAYERIMPOVERLORD_PRIMARY_VOIDSPIKES_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.primary = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.primary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.primary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
            component.primary.baseSkill = mySkillDef;
            component.primary.skillDef = mySkillDef;
            component.primary.defaultSkillDef = mySkillDef;
        }
        static void SecondarySetup()
        {
            LanguageAPI.Add("PLAYERIMPOVERLORD_SECONDARY_GROUNDPOUND_NAME", "Ground Pound");
            LanguageAPI.Add("PLAYERIMPOVERLORD_SECONDARY_GROUNDPOUND_DESCRIPTION", "Furiously pound the ground below you six times, dealing <style=cIsDamage>400% damage</style> each hit.");

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(GroundPound));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 1f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = false;
            mySkillDef.interruptPriority = InterruptPriority.Death;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = true;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon2;
            mySkillDef.skillDescriptionToken = "PLAYERIMPOVERLORD_SECONDARY_GROUNDPOUND_DESCRIPTION";
            mySkillDef.skillName = "PLAYERIMPOVERLORD_SECONDARY_GROUNDPOUND_NAME";
            mySkillDef.skillNameToken = "PLAYERIMPOVERLORD_SECONDARY_GROUNDPOUND_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.secondary = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.secondary.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.secondary.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
            component.secondary.baseSkill = mySkillDef;
            component.secondary.skillDef = mySkillDef;
            component.utility.defaultSkillDef = mySkillDef;

        }
        static void UtilitySetup()
        {
            //KevinsAdditionsPlugin._logger.LogError("The Imp Overlord has " + component.skillSlotCount + " at his disposal.");
            LanguageAPI.Add("PLAYERIMPOVERLORD_UTILITY_BLINK_NAME", "Blink");
            LanguageAPI.Add("PLAYERIMPOVERLORD_UTILITY_BLINK_DESCRIPTION", "Blink to the nearest enemy, landing on top of them and dealing damage.");

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(BlinkState));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 9f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = false;
            mySkillDef.interruptPriority = InterruptPriority.Death;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = true;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon3;
            mySkillDef.skillDescriptionToken = "PLAYERIMPOVERLORD_UTILITY_BLINK_DESCRIPTION";
            mySkillDef.skillName = "PLAYERIMPOVERLORD_UTILITY_BLINK_NAME";
            mySkillDef.skillNameToken = "PLAYERIMPOVERLORD_UTILITY_BLINK_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.utility = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.utility.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.utility.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
            component.utility.baseSkill = mySkillDef;
            component.utility.skillDef = mySkillDef;
            component.utility.defaultSkillDef = mySkillDef;
        }
        static void SpecialSetup()
        {
            LanguageAPI.Add("PLAYERIMPOVERLORD_SPECIAL_REVERTFORM_NAME", "Revert Form");
            LanguageAPI.Add("PLAYERIMPOVERLORD_SPECIAL_REVERTFORM_DESCRIPTION", "Revert to your original form.");

            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(RevertFormState));
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0.5f;
            mySkillDef.beginSkillCooldownOnSkillEnd = true;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Death;
            mySkillDef.isBullets = false;
            mySkillDef.isCombatSkill = false;
            mySkillDef.mustKeyPress = true;
            mySkillDef.noSprint = false;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon4;
            mySkillDef.skillDescriptionToken = "PLAYERIMPOVERLORD_SPECIAL_REVERTFORM_DESCRIPTION";
            mySkillDef.skillName = "PLAYERIMPOVERLORD_SPECIAL_REVERTFORM_NAME";
            mySkillDef.skillNameToken = "PLAYERIMPOVERLORD_SPECIAL_REVERTFORM_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef);

            component.special = bodyPrefab.AddComponent<GenericSkill>();
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>();
            newFamily.variants = new SkillFamily.Variant[1];
            LoadoutAPI.AddSkillFamily(newFamily);
            component.special.SetFieldValue("_skillFamily", newFamily);
            SkillFamily skillFamily = component.special.skillFamily;

            skillFamily.variants[0] = new SkillFamily.Variant
            {
                skillDef = mySkillDef,
                unlockableName = "",
                viewableNode = new ViewablesCatalog.Node(mySkillDef.skillNameToken, false, null)
            };
            component.special.baseSkill = mySkillDef;
            component.special.skillDef = mySkillDef;
            component.special.defaultSkillDef = mySkillDef;
        }

        private static void On_Enter(On.EntityStates.ImpBossMonster.FireVoidspikes.orig_OnEnter orig, EntityStates.ImpBossMonster.FireVoidspikes self)
        {
            orig(self);
            if (self.characterBody.isPlayerControlled)
                self.StartAimMode(self.GetAimRay(), 2f, false);
        }
    }
}

namespace EntityStates.ImpBossPlayer
{

    public class RevertFormState : BaseSkillState
    {
        public static GameObject initialEffect;
        public static GameObject deathEffect;
        private static float duration = 3.3166666f;
        private float stopwatch;
        private Animator animator;
        private bool hasPlayedDeathEffect;
        private bool hasReverted;

        public override void OnEnter()
        {
            base.OnEnter();

        }

        public override void OnExit()
        {
            base.OnExit();
        }

        private void RevertForm()
        {
            if (base.characterBody && base.characterBody.isPlayerControlled && !this.hasReverted)
            {
                CharacterMaster cm = base.characterBody.master;
                KevinfromHP.KevinsAdditions.ImpExtractComponent cpt = cm.gameObject.GetComponent<KevinfromHP.KevinsAdditions.ImpExtractComponent>();
                base.characterBody.ClearTimedBuffs(cpt.buff);
            }
        }

        public override void FixedUpdate()
        {
            base.FixedUpdate();

            if (!this.hasReverted)
                RevertForm();

            if (base.isAuthority)
                this.outer.SetNextStateToMain();
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }

}
