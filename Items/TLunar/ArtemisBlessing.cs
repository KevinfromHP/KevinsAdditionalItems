using RoR2;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.MiscUtil;
using Mono.Cecil;

namespace KevinfromHP.KevinsClassics
{
    public class ArtemisBlessing : Item<ArtemisBlessing>
    {
        public override string displayName => "Artemis' Blessing";
        public override ItemTier itemTier => ItemTier.Lunar;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Median Distance from target (Point where damage is unaffected)", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float medDist { get; private set; } = 25f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Minimum Damage from Distances closer than Median Range (Will continue to reduce if stacked!)", AutoItemConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float minReduction { get; private set; } = 0.1f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Maximum Distance where damage will continue to increase", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float maxDist { get; private set; } = 300f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage Addition/Reduction per meter from Median Range", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float effectMult { get; private set; } = 0.025f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage falloff change for non-shotgun bullets (the minimum percent of damage possible e.g. 50% of original damage)", AutoItemConfigFlags.PreventNetMismatch, 0.5f, 1f)]
        public float falloffBullet { get; private set; } = 0.8f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage falloff change for shotgun pellets (the minimum percent of damage possible e.g. 50% of original damage)", AutoItemConfigFlags.PreventNetMismatch, 0.25f, 1f)]
        public float falloffShotgun { get; private set; } = 0.8f;

        public bool inclDeploys { get; private set; } = false;
        private bool ilFailed = false;


        //public BuffIndex ArtemisBlessingBuff { get; private set; }
        protected override string NewLangName(string langid = null) => displayName;
        protected override string NewLangPickup(string langid = null) => "The further you are, the more damage you do.\n<style=cDeath>Don't get too close, though...</style>";
        protected override string NewLangDesc(string langid = null) => "Damage increases the further your distance from a target.\n<style=cDeath>Damage decreases the closer your distance to a target.</style>";
        protected override string NewLangLore(string langid = null) => "A seemingly new item you've never seen before...";


        public ArtemisBlessing()
        {
            modelPathName = "@KevinsClassics:Assets/KevinsClassics/prefabs/ArtemisBlessing.prefab";
            iconPathName = "@KevinsClassics:Assets/KevinsClassics/textures/icons/ArtemisBlessing_icon.png";
        }


        protected override void LoadBehavior()
        {
            IL.RoR2.BulletAttack.DefaultHitCallback += IL_CBDefaultHitCallback;
            IL.RoR2.HealthComponent.TakeDamage += IL_CBTakeDamage;
            if (ilFailed)
            {
                IL.RoR2.BulletAttack.DefaultHitCallback -= IL_CBDefaultHitCallback;
                IL.RoR2.HealthComponent.TakeDamage -= IL_CBTakeDamage;
            }
        }


        protected override void UnloadBehavior()
        {
            IL.RoR2.BulletAttack.DefaultHitCallback -= IL_CBDefaultHitCallback;
            IL.RoR2.HealthComponent.TakeDamage -= IL_CBTakeDamage;
        }

        private void IL_CBDefaultHitCallback(ILContext il) // Reduces the damage falloff when equipped
        {
            var c = new ILCursor(il);
            bool ILFound;

            int icnt = 0;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<BulletAttack>>((thing) =>
            {
                icnt = GetCount(thing.owner.GetComponent<CharacterBody>().master.inventory);
            });

            if (icnt == 0) return;

            ILFound = c.TryGotoNext(
                x => x.MatchLdcR4(0.5f),
                x => x.MatchLdcR4(60f), 
                x => x.MatchLdcR4(25f),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<BulletAttack.BulletHit>("distance"),
                x => x.MatchCallOrCallvirt("UnityEngine.Mathf", "InverseLerp"),
                x => x.MatchCallOrCallvirt("UnityEngine.Mathf", "Clamp01"),
                x => x.MatchLdcR4(0.5f));

            if (!ILFound)
            {
                ilFailed = true;
                KevinsClassicsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (bulletfalloff var read), item will not work; target instructions not found");
                return;
            }
            else
            {
                c.Next.Operand = falloffBullet;
                c.Index += 8;
                c.Next.Operand = 1f - falloffBullet;
            }

            ILFound = c.TryGotoNext(
                x => x.MatchLdcR4(0.25f),
                x => x.MatchLdcR4(25f),
                x => x.MatchLdcR4(7f),
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<BulletAttack.BulletHit>("distance"),
                x => x.MatchCallOrCallvirt("UnityEngine.Mathf", "InverseLerp"),
                x => x.MatchCallOrCallvirt("UnityEngine.Mathf", "Clamp01"),
                x => x.MatchLdcR4(0.75f));

            if (!ILFound)
            {
                ilFailed = true;
                KevinsClassicsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (shotgunfalloff var read), item will not work; target instructions not found");
                return;
            }
            else
            {
                c.Next.Operand = falloffShotgun;
                c.Index += 8;
                c.Next.Operand = 1f - falloffShotgun;
            }
        }


        private void IL_CBTakeDamage(ILContext il)
        {
            var c = new ILCursor(il);
            bool ILFound;

            int locDmg = -1;
            ILFound = c.TryGotoNext(
                x => x.MatchLdarg(1),
                x => x.MatchLdfld<DamageInfo>("damage"),
                x => x.MatchStloc(out locDmg));

            if (!ILFound)
            {
                ilFailed = true;
                KevinsClassicsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (damage var read), item will not work; target instructions not found");
                return;
            }

            FieldReference locEnemy = null;
            int locThis = -1;
            ILFound = c.TryGotoNext(
                x => x.MatchLdloc(2),
                x => x.MatchLdarg(out locThis),
                x => x.MatchLdfld(out locEnemy),
                x => x.MatchCallOrCallvirt<CharacterBody>("get_teamComponent"),
                x => x.MatchCallOrCallvirt<TeamComponent>("get_teamIndex"));

            if (!ILFound)
            {
                ilFailed = true;
                KevinsClassicsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (damage var read), item will not work; target instructions not found");
                return;
            }

            int locChrm = -1;
            ILFound = c.TryGotoNext(
                x => x.MatchLdloc(out locChrm),
                x => x.MatchCallOrCallvirt<CharacterMaster>("get_inventory"),
                x => x.MatchLdcI4((int)ItemIndex.Crowbar))
                && c.TryGotoPrev(MoveType.After,
                x => x.OpCode == OpCodes.Brfalse);

            if (ILFound)
            {
                c.Emit(OpCodes.Ldloc, locChrm);
                c.Emit(OpCodes.Ldarg, locThis);
                c.Emit(OpCodes.Ldloc, locDmg);
                c.EmitDelegate<Func<CharacterMaster, HealthComponent, float, float>>((chrm, body, origdmg) =>
                {
                    var icnt = GetCount(chrm.inventory);
                    if (icnt == 0) return origdmg;
                    float aDist = (chrm.GetBody().corePosition - body.body.corePosition).magnitude;

                    //Damage Calculation
                    return origdmg * (1 + (Math.Max((Math.Min(aDist, maxDist) - medDist) * effectMult, minReduction - 1.0f) * icnt));
                });
                c.Emit(OpCodes.Stloc, locDmg);
            }
            else
            {
                ilFailed = true;
                KevinsClassicsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (damage var write), item will not work; target instructions not found");
                return;
            }

        }

    }

    public class ArtemisBlessingComponent : MonoBehaviour
    {
        public int cachedIcnt = 0;
        public float cachedDist = 0;
    }
}

