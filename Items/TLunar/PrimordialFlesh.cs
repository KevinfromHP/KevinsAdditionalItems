using RoR2;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.MiscUtil;
using Mono.Cecil;
using R2API.Utils;

namespace KevinfromHP.KevinsAdditions
{
    public class PrimordialFlesh : Item_V2<PrimordialFlesh>
    {
        public override string displayName => "Primordial Flesh";
        public override ItemTier itemTier => ItemTier.Lunar;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Regen Multiplier", AutoConfigFlags.PreventNetMismatch, 0f, 10f)]
        public float regenMult { get; private set; } = .1f;

        private bool ilFailed = false;


        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "Base Health Regen scales with current movement speed. \n<style=cDeath>Cannot heal through other means.</style>";
        protected override string GetDescString(string langid = null) => "";
        protected override string GetLoreString(string langid = null) => "-Profile: 098 - \"Primordial Flesh\"- \n-Test Subject Name: XXXXXXXXXX-" +
            "\n\n-Test 01: First/Second/Third Degree Burns across 92% of the body-" +
            "\n-Results: Wounds all healed from the body, right down to the dead cells recovering back to their prior state-" +
            "\n\n-Test 02: Blunt Impact to the skull and ribcage (Accidental wounds - pierced lungs)-" +
            "\n-Results: Rapidly regained focus and breathing. Bones repaired themselves and fractured bones melded into the healing organs-" +
            "\n\n-Test 03: Removal of both arms-" +
            "\n-Results: Both arms grew back to a proper condition. Took a short bit longer than that of prior tests.-" +
            "\n\n-End Result: Seek further studies of \"Lunar\" Objects, for further studies of advanced alien medical and technological advancements-" +
            "\n\t(Lore and Item Idea by Skyline222)";


        public PrimordialFlesh()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/ImpExtract.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon.png";
        }


        public override void Install()
        {
            base.Install();

            On.RoR2.CharacterBody.OnInventoryChanged += GetItemCount;
            IL.RoR2.CharacterBody.RecalculateStats += SetHealthRegen;
            IL.RoR2.HealthComponent.Heal += ManageHeals;
        }
        public override void Uninstall()
        {
            base.Uninstall();

            On.RoR2.CharacterBody.OnInventoryChanged -= GetItemCount;
            IL.RoR2.CharacterBody.RecalculateStats -= SetHealthRegen;
            IL.RoR2.HealthComponent.Heal -= ManageHeals;
        }

        private void GetItemCount(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            PrimordialFleshComponent cpt = self.gameObject.GetComponent<PrimordialFleshComponent>();
            if (!cpt) cpt = self.gameObject.AddComponent<PrimordialFleshComponent>();
            cpt.cachedIcnt = GetCount(self);
        }

        private void SetHealthRegen(ILContext il)
        {
            /* numbers not needed in the regen calc
             * num42 = Cautious Slug
             * num43 = Meat
             * num44 = Acrid Regen
             */
            ILCursor c = new ILCursor(il);
            bool ILFound;

            int locSlug = -1;
            int locMeat = -1;
            int locCroco = -1;
            ILFound = c.TryGotoNext(
                x => x.MatchLdloc(46),
                x => x.MatchAdd(),
                x => x.MatchLdloc(out locSlug),
                x => x.MatchAdd(),
                x => x.MatchLdloc(out locMeat),
                x => x.MatchAdd(),
                x => x.MatchLdloc(50));
            if (!ILFound)
            {
                ilFailed = true;
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Primordial Flesh IL patch (slug-meat var read), item will not work; target instructions not found");
                return;
            }

            ILFound = c.TryGotoNext(
            x => x.MatchLdloc(52),
            x => x.MatchLdloc(out locCroco),
            x => x.MatchAdd(),
            x => x.MatchStloc(52))
            && c.TryGotoNext(MoveType.After,
                x => x.MatchCallOrCallvirt<CharacterBody>("set_regen"));
            if (!ILFound)
            {
                ilFailed = true;
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Primordial Flesh IL patch (croco var read), item will not work; target instructions not found");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, locSlug);
            c.Emit(OpCodes.Ldloc, locMeat);
            c.Emit(OpCodes.Ldloc, locCroco);
            c.EmitDelegate<Action<CharacterBody, float, float, float>>((body, slug, meat, croco) =>
            {
                PrimordialFleshComponent cpt = body.gameObject.GetComponent<PrimordialFleshComponent>();
                if (!cpt || cpt.cachedIcnt == 0)
                    return;

                float excluded = -slug - meat - croco;
                float curse = body.inventory.GetItemCount(ItemIndex.HealthDecay);
                if (curse > 0)
                {
                    body.regen += (body.maxHealth * body.cursePenalty * curse) - excluded; // Undoes curse, adjusts regen
                    body.regen -= body.maxHealth * body.cursePenalty * curse; // Redoes curse
                }
                else
                    body.regen -= excluded;

                float speed = body.characterMotor.velocity.magnitude;
                body.regen *= speed * cpt.cachedIcnt * regenMult;
            });
        }

        private void ManageHeals(ILContext il) /*cancels out anything like bustling fungus or medkit*/
        {
            ILCursor c = new ILCursor(il);
            bool ILFound;
            ILLabel label = c.DefineLabel();

            /*ILFound = c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<HealthComponent>("health"),
                x => x.MatchStloc(0));*/

            /*This essentially just writes the function
             * if(nonRegen && hasItem)
             *      return 0 heals;
             */

            c.Emit(OpCodes.Ldarg_3);
            c.Emit(OpCodes.Brfalse_S, label);
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<HealthComponent, bool>>((healthCPT) =>
              {
                  if (GetCount(healthCPT.body) > 0)
                      return true;
                  return false;
              });
            c.Emit(OpCodes.Brfalse_S, label);
            c.Emit(OpCodes.Ldc_R4, 0f);
            c.Emit(OpCodes.Ret);
            c.MarkLabel(label);
        }
    }

    public class PrimordialFleshComponent : MonoBehaviour
    {
        public int cachedIcnt = 0;
    }
}


