using RoR2;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.MiscUtil;
using Mono.Cecil;

namespace KevinfromHP.KevinsAdditions
{
    public class ArtemisBlessing : Item_V2<ArtemisBlessing>
    {
        public override string displayName => "Artemis' Blessing";
        public override ItemTier itemTier => ItemTier.Lunar;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Median Distance from target (Point where damage is unaffected)", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float medDist { get; private set; } = 25f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Minimum Damage from Distances closer than Median Range (Will continue to reduce if stacked!)", AutoConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float minReduction { get; private set; } = 0.1f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Maximum Distance where damage will continue to increase", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float maxDist { get; private set; } = 300f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Damage Addition/Reduction per meter from Median Range", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float effectMult { get; private set; } = 0.025f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Damage falloff change for non-shotgun bullets (the minimum percent of damage possible e.g. 50% of original damage)", AutoConfigFlags.PreventNetMismatch, 0.5f, 1f)]
        public float falloffBullet { get; private set; } = 0.8f;

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Damage falloff change for shotgun pellets (the minimum percent of damage possible e.g. 50% of original damage)", AutoConfigFlags.PreventNetMismatch, 0.25f, 1f)]
        public float falloffShotgun { get; private set; } = 0.8f;

        private bool ilFailed = false;


        //public BuffIndex ArtemisBlessingBuff { get; private set; }
        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "The further you are, the more damage you do.\n<style=cDeath>Don't get too close, though...</style>";
        protected override string GetDescString(string langid = null) => "Damage increases by <style=cIsDamage>" + (int)(effectMult * 100) + "%</style> " + "<style=cStack>(+" + (int)(effectMult * 100) + "% per stack)</style> per <style=cIsUtility>meter</style> away from a target.\n" +
            "<style=cDeath>Damage decreases by the same amount the closer your distance to a target.</style> Distance where damage output is unaffected is <style=cIsUtility>   " + (int)medDist + "m</style>.";
        protected override string GetLoreString(string langid = null) => "My love! How I anguish to see you like this, laid out limp upon my arms. You, who was once so powerful, so full of life, he who could conquer any beast. To have been felled by such a devious trick of hers', the vile scorpion. It is such a crime the gods must avert their eyes in shame for allowing this to happen." +
            "\n\nBut you shall not be forgotten, doomed as a shade below for all eternity. I shall bless your body, and place you high in the skies, to glow with strength forever. Distant above even the gods, from far away you shall remind everyone of your power." +
            "\n\n - Inscription upon a club, found in an empty marble tomb" +
            "\n\t(Lore by Keroro1454, item idea by TheGoldenOne)";


        public ArtemisBlessing()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/ArtemisBlessing.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/ArtemisBlessing_icon.png";
        }


        public override void Install()
        {
            base.Install();

            On.RoR2.CharacterBody.OnInventoryChanged += GetItemCount;
            IL.RoR2.BulletAttack.DefaultHitCallback += IL_CBDefaultHitCallback;
            IL.RoR2.HealthComponent.TakeDamage += IL_CBTakeDamage;
            if (ilFailed)
            {
                IL.RoR2.BulletAttack.DefaultHitCallback -= IL_CBDefaultHitCallback;
                IL.RoR2.HealthComponent.TakeDamage -= IL_CBTakeDamage;
            }
        }
        public override void Uninstall()
        {
            base.Uninstall();

            On.RoR2.CharacterBody.OnInventoryChanged -= GetItemCount;
            IL.RoR2.BulletAttack.DefaultHitCallback -= IL_CBDefaultHitCallback;
            IL.RoR2.HealthComponent.TakeDamage -= IL_CBTakeDamage;
        }

        private void GetItemCount(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            ArtemisBlessingComponent cpt = self.gameObject.GetComponent<ArtemisBlessingComponent>();
            if (!cpt) cpt = self.gameObject.AddComponent<ArtemisBlessingComponent>();
            cpt.cachedIcnt = GetCount(self);
        }


        private void IL_CBDefaultHitCallback(ILContext il) // Reduces the damage falloff when equipped
        {
            var c = new ILCursor(il);
            bool ILFound;

            ArtemisBlessingComponent cpt = null;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Action<BulletAttack>>((thing) =>
            {
                cpt = thing.owner.GetComponent<ArtemisBlessingComponent>();
            });

            if (!cpt) return;

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
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (bulletfalloff var read), item will not work; target instructions not found");
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
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (shotgunfalloff var read), item will not work; target instructions not found");
                return;
            }
            c.Next.Operand = falloffShotgun;
            c.Index += 8;
            c.Next.Operand = 1f - falloffShotgun;
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
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (damage var read), item will not work; target instructions not found");
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
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (damage var read), item will not work; target instructions not found");
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
                    ArtemisBlessingComponent cpt = chrm.GetBodyObject().GetComponent<ArtemisBlessingComponent>();
                    if (!cpt || cpt.cachedIcnt == 0) return origdmg;
                    float aDist = (chrm.GetBody().corePosition - body.body.corePosition).magnitude;

                    return origdmg * (1 + (Math.Max((Math.Min(aDist, maxDist) - medDist) * effectMult, minReduction - 1.0f) * cpt.cachedIcnt)); //Damage Calculation
                });
                c.Emit(OpCodes.Stloc, locDmg);
            }
            else
            {
                ilFailed = true;
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Artemis' Blessing IL patch (damage var write), item will not work; target instructions not found");
                return;
            }

        }

    }

    public class ArtemisBlessingComponent : MonoBehaviour
    {
        public int cachedIcnt = 0;
    }
}

