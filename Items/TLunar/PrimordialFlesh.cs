using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using System;
using System.Collections.ObjectModel;
using TILER2;
using UnityEngine;


/* Notes:
 * There may be a way to implement the regen through TILER2
 */

namespace KevinfromHP.KevinsAdditions
{
    public class PrimordialFlesh : VirtItem_V2<PrimordialFlesh>
    {
        public override string displayName => "Primordial Flesh";
        public override ItemTier itemTier => ItemTier.Lunar;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Damage });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Regen Multiplier", AutoConfigFlags.PreventNetMismatch, 0f, 10f)]
        public float regenMult { get; private set; } = .1f;

        private bool ilFailed = false;


        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "Health Regeneration scales with current movement speed. <style=cDeath>Cannot heal through other means.</style>";
        protected override string GetDescString(string langid = null) => "<style=cIsHealth>Health Regeneration</style> scales to <style=cIsUtility>" + (int)(regenMult * 100) + "</style>% " + "<style=cStack>(+" + (int)(regenMult * 100) + " % per stack)</style>of your current velocity. <style=cDeath>Cannot heal through other means.</style>";
        string testDate = "5/12/2057";
        string testAdministrator = "Dr. Brawn";
        string profileID = "098";
        string subjectName = "XXXXXXXXX";
        string specimen = "Primordial Flesh";
        string tests = "<style=cIsDamage>Test 01:</style> First/Second/Third Degree Burns across 92% of the body." +
            "\n<style=cIsDamage>Results:</style> Wounds all healed from the body, right down to the dead cells recovering back to their prior state." +
            "\n\n<style=cIsDamage>Test 02:</style> Blunt Impact to the skull and ribcage (Accidental wounds - pierced lungs)." +
            "\n<style=cIsDamage>Results:</style> Rapidly regained focus and breathing. Bones repaired themselves and fractured bones melded into the healing organs." +
            "\n\n<style=cIsDamage>Test 03:</style> Removal of both arms." +
            "\n<style=cIsDamage>Results:</style> Both arms grew back to a proper condition. Took a short bit longer than that of prior tests." +
            "\n\n<style=cIsHealth><u>Conclusion:</u></style> Seek further studies of \"Lunar\" Objects for further studies of advanced alien medical and technological advancements." +
            "\n\n\t<style=cStack>(Lore and Item Idea by</style> <style=cIsUtility>Skyline222</style><style=cStack>)</style>";

        protected override string GetLoreString(string langid = null) => KevinsAdditionsPlugin.LabResultsLoreFormatter(testDate, testAdministrator, profileID, subjectName, specimen, tests);

        /*protected override string GetLoreString(string langid = null) => "-Profile: 098 - \"Primordial Flesh\"- \n-Test Subject Name: XXXXXXXXXX-" +
            "\n\n-Test 01: First/Second/Third Degree Burns across 92% of the body-" +
            "\n-Results: Wounds all healed from the body, right down to the dead cells recovering back to their prior state-" +
            "\n\n-Test 02: Blunt Impact to the skull and ribcage (Accidental wounds - pierced lungs)-" +
            "\n-Results: Rapidly regained focus and breathing. Bones repaired themselves and fractured bones melded into the healing organs-" +
            "\n\n-Test 03: Removal of both arms-" +
            "\n-Results: Both arms grew back to a proper condition. Took a short bit longer than that of prior tests.-" +
            "\n\n-End Result: Seek further studies of \"Lunar\" Objects, for further studies of advanced alien medical and technological advancements-" +
            "\n\t(Lore and Item Idea by Skyline222)";*/


        public PrimordialFlesh()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/PrimordialFlesh.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/PrimordialFlesh_icon.png";
        }


        public override void Install()
        {
            base.Install();

            IL.RoR2.CharacterBody.RecalculateStats += IL_SetHealthRegen;
            IL.RoR2.HealthComponent.Heal += IL_ManageHeals;
            if (ilFailed)
            {
                IL.RoR2.CharacterBody.RecalculateStats -= IL_SetHealthRegen;
                IL.RoR2.HealthComponent.Heal -= IL_ManageHeals;
            }
        }
        public override void Uninstall()
        {
            base.Uninstall();

            IL.RoR2.CharacterBody.RecalculateStats -= IL_SetHealthRegen;
            IL.RoR2.HealthComponent.Heal -= IL_ManageHeals;
        }

        public override void StoreItemCount(CharacterBody self)
        {
            PrimordialFleshComponent cpt = self.gameObject.GetComponent<PrimordialFleshComponent>();
            if (GetCount(self) > 0 || cpt)
            {
                if (!cpt)
                    cpt = self.gameObject.AddComponent<PrimordialFleshComponent>();
                cpt.cachedIcnt = GetCount(self);
            }
        }

        private void IL_SetHealthRegen(ILContext il)
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

        private void IL_ManageHeals(ILContext il) /*cancels out anything like bustling fungus or medkit*/
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


