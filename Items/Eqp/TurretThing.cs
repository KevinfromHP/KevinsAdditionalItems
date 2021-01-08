/*using BepInEx;
using EntityStates;
using EntityStates.EngiTurret.EngiTurretWeapon;
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


namespace Example // This is your mod workspace. In the next namespace, you will put the skills in the EntityStates, probably for turrets
{
    public class TurretAdjustments
    {
        public static GameObject bodyPrefab; // This will store the grabbed turret bodyprefab
        public static SkillLocator component; // This will be a shared component that the prefab for the turret uses

        //In whatever your BaseUnityPlugin is, just call TurretAdjustments.Start();
        public static void Start()
        {
            CreatePrefab();
            RegisterStates();
            SkillSetup();

            BodyCatalog.getAdditionalEntries += delegate (List<GameObject> list) // Adds the new Turret bodyPrefab to the bodyPrefab catalog list so you can actually do something with it
            {
                list.Add(bodyPrefab);
            };
        }

        internal static void CreatePrefab() // This is where you grab the prefab of the EngiTurretBody
        {
            bodyPrefab = PrefabAPI.InstantiateClone(Resources.Load<GameObject>("Prefabs/CharacterBodies/EngiTurretBody"), "EngiTurretSkilledBody", true, "D:\\All Coding Stuff\\RoR2 Modding\\KevinsAdditions\\Items\\Eqp\\ImpExtras.cs", "CreatePrefab"); //I know the location of the EngiTurretBody prefab because I decompiled Risk of Rain. Switch the directory on this to wherever you are coding
            bodyPrefab.GetComponent<NetworkIdentity>().localPlayerAuthority = true;
        }
        static void RegisterStates() // Register all future skills here in the same way I did.
        {
            LoadoutAPI.AddSkill(typeof(FireGauss)); // This is the vanilla engi turret weapon
            LoadoutAPI.AddSkill(typeof(ExampleState));
        }
        static void SkillSetup()
        {
            foreach (GenericSkill obj in bodyPrefab.GetComponentsInChildren<GenericSkill>()) // This wipes all of the skills that the copied turret may have on it. Don't worry though, I add it back in PrimarySetup()
            {
                BaseUnityPlugin.DestroyImmediate(obj);
            }

            PrimarySetup();
            //Just repeat the same stuff as Primary but specific towards your EntityState
            SecondarySetup();
            UtilitySetup();
            SpecialSetup();
        }

        static void PrimarySetup() // What you see here is the basic way of adding any skill, it just requires some tuning usually.
        {
            component = bodyPrefab.GetComponent<SkillLocator>();
            LanguageAPI.Add("ENGITURRET_PRIMARY_GAUSSSHOT_NAME", "Gauss Shot"); // There is no rhyme or reason to the first part. Just make sure it matches what shows up later down here.
            LanguageAPI.Add("ENGITURRET_PRIMARY_GAUSSSHOT_DESCRIPTION", "This doesn't matter because I doubt anyone will be playing as the turret lol");


            //A lot of this just requires tinkering to figure out.
            SkillDef mySkillDef = ScriptableObject.CreateInstance<SkillDef>();
            mySkillDef.activationState = new SerializableEntityStateType(typeof(FireGauss)); // This is where your EntityState skill is linked to the activation.
            mySkillDef.activationStateMachineName = "Weapon";
            mySkillDef.baseMaxStock = 1;
            mySkillDef.baseRechargeInterval = 0f;
            mySkillDef.beginSkillCooldownOnSkillEnd = false;
            mySkillDef.canceledFromSprinting = false;
            mySkillDef.fullRestockOnAssign = true;
            mySkillDef.interruptPriority = InterruptPriority.Any;
            mySkillDef.isBullets = true;
            mySkillDef.isCombatSkill = true;
            mySkillDef.mustKeyPress = false;
            mySkillDef.noSprint = true;
            mySkillDef.rechargeStock = 1;
            mySkillDef.requiredStock = 1;
            mySkillDef.shootDelay = 0.1f;
            mySkillDef.stockToConsume = 1;
            mySkillDef.icon = Assets.icon1; // I will show you this whenever you mention it. It's how you load assets from unity.
            mySkillDef.skillDescriptionToken = "ENGITURRET_PRIMARY_GAUSSSHOT_DESCRIPTION";
            mySkillDef.skillName = "ENGITURRET_PRIMARY_GAUSSSHOT_NAME";
            mySkillDef.skillNameToken = "ENGITURRET_PRIMARY_GAUSSSHOT_NAME";

            LoadoutAPI.AddSkillDef(mySkillDef); // This adds it as a skill to the skill library that R2API manages.

            component.primary = bodyPrefab.AddComponent<GenericSkill>(); // Adds the GenericSkill we deleted earlier.
            SkillFamily newFamily = ScriptableObject.CreateInstance<SkillFamily>(); //SkillFamilies are what make it so you can have different skills e.g. Acrid's Blight over Plague.
            newFamily.variants = new SkillFamily.Variant[1]; // A SkillFamily with a variant of 1 would just have one option. For more options, see the Character Creation's PrimarySetup.
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
            component.utility.defaultSkillDef = mySkillDef;
        }
        static void SecondarySetup()
        {
        }
        static void UtilitySetup()
        {
        }
        static void SpecialSetup()
        {
        }
    }
}

namespace EntityStates.EngiTurret.EngiTurretWeapon
{
    public class ExampleState : BaseSkillState
    {
        public float baseDuration = 0.5f;
        private float duration;
        public GameObject effectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/Hitspark");
        public GameObject hitEffectPrefab = Resources.Load<GameObject>("prefabs/effects/impacteffects/critspark");
        public GameObject tracerEffectPrefab = Resources.Load<GameObject>("prefabs/effects/tracers/tracerbanditshotgun");
        public override void OnEnter()
        {
            base.OnEnter();
            this.duration = this.baseDuration / base.attackSpeedStat;
            Ray aimRay = base.GetAimRay();
            base.StartAimMode(aimRay, 2f, false);
            base.PlayAnimation("Gesture, Override", "FireShotgun", "FireShotgun.playbackRate", this.duration * 1.1f);
            if (base.isAuthority)
            {
                new BulletAttack
                {
                    owner = base.gameObject,
                    weapon = base.gameObject,
                    origin = aimRay.origin,
                    aimVector = aimRay.direction,
                    minSpread = 0f,
                    maxSpread = base.characterBody.spreadBloomAngle,
                    bulletCount = 1U,
                    procCoefficient = 1f,
                    damage = base.characterBody.damage,
                    force = 3,
                    falloffModel = BulletAttack.FalloffModel.DefaultBullet,
                    tracerEffectPrefab = this.tracerEffectPrefab,
                    hitEffectPrefab = this.hitEffectPrefab,
                    isCrit = base.RollCrit(),
                    HitEffectNormal = false,
                    stopperMask = LayerIndex.world.mask,
                    smartCollision = true,
                    maxDistance = 300f
                }.Fire();
            }
        }
        public override void OnExit()
        {
            base.OnExit();
        }
        public override void FixedUpdate()
        {
            base.FixedUpdate();
            if (base.fixedAge >= this.duration && base.isAuthority)
            {
                this.outer.SetNextStateToMain();
                return;
            }
        }

        public override InterruptPriority GetMinimumInterruptPriority()
        {
            return InterruptPriority.Skill;
        }
    }
}*/