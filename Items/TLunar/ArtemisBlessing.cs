using RoR2;
using UnityEngine;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.MiscUtil;
using static TILER2.StatHooks;
using Mono.Cecil;
using R2API.Utils;
using ThinkInvisible.ClassicItems;

namespace KevinfromHP.KevinsClassics {
    public class ArtemisBlessing : Item<ArtemisBlessing> {
        public override string displayName => "Artemis' Blessing";
		public override ItemTier itemTier => ItemTier.Lunar;
		public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[]{ItemTag.Damage});

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Median Distance from target (Point where damage is unaffected)", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float medDist {get;private set;} = 25f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Minimum Damage from Distances closer than Median Range (Will continue to reduce if stacked!)", AutoItemConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float minReduction {get;private set;} = 0.1f;
        
        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Maximum Distance where damage will continue to increase", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float maxDist {get;private set;} = 300f;

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("Damage Addition/Reduction per meter from Median Range", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float effectMult { get; private set; } = 0.025f;

        public bool inclDeploys { get; private set; } = false;
        private bool ilFailed = false;


        public BuffIndex ArtemisBlessingBuff {get;private set;}
        protected override string NewLangName(string langid = null) => displayName;
        protected override string NewLangPickup(string langid = null) => "The further you are, the more damage you do. Don't get too close, though...";
        protected override string NewLangDesc(string langid = null) => "Damage increases the further your distance from a target.\n<style=cDeath>Damage decreases the closer your distance to a target.</style>";
        protected override string NewLangLore(string langid = null) => "A seemingly new item you've never seen before...";

        public ArtemisBlessing()
        {
            onAttrib += (tokenIdent, namePrefix) => {
                var ArtemisBlessingBuffDef = new R2API.CustomBuff(new BuffDef
                {
                    buffColor = new Color(0.85f, 0.8f, 0.3f),
                    canStack = true,
                    isDebuff = false,
                    name = namePrefix + "ArtemisBlessing",
                    iconPath = "@KevinsClassics:Assets/KevinsClassics/icons/ArtemisBlessing_icon.png"
                });
                ArtemisBlessingBuff = R2API.BuffAPI.Add(ArtemisBlessingBuffDef);
            };
        }

        protected override void LoadBehavior()
        {
            IL.RoR2.HealthComponent.TakeDamage += IL_CBTakeDamage;
            if (ilFailed) IL.RoR2.HealthComponent.TakeDamage -= IL_CBTakeDamage;
            else
            {
                On.RoR2.CharacterBody.OnInventoryChanged += On_CBInventoryChanged;
            }
        }

        protected override void UnloadBehavior()
        {
            IL.RoR2.HealthComponent.TakeDamage -= IL_CBTakeDamage;
            //IL.RoR2.HealthComponent.TakeDamage -= IL_CBGetDistance;
            On.RoR2.CharacterBody.OnInventoryChanged -= On_CBInventoryChanged;
        }

        private void OnConfigEntryChanged(object sender, AutoUpdateEventArgs args)
        {
            AliveList().ForEach(cm => {
                if (cm.hasBody) UpdateABBuff(cm.GetBody());
            });
        }

        private void On_CBInventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self)
        {
            orig(self);
            var cpt = self.GetComponent<ArtemisBlessingComponent>();
            if (!cpt) cpt = self.gameObject.AddComponent<ArtemisBlessingComponent>();
            var newIcnt = GetCount(self);
            if (cpt.cachedIcnt != newIcnt)
            {
                cpt.cachedIcnt = newIcnt;
                UpdateABBuff(self);
            }
        }

        void UpdateABBuff(CharacterBody cb)
        {
            var cpt = cb.GetComponent<ArtemisBlessingComponent>();
            int currBuffStacks = cb.GetBuffCount(ArtemisBlessingBuff);
            if (cpt.cachedIcnt != currBuffStacks)
                cb.SetBuffCount(ArtemisBlessingBuff, cpt.cachedIcnt);
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
                c.EmitDelegate<Func<CharacterMaster, HealthComponent, float, float>>((chrm, body, origdmg) => {



                    var icnt = chrm.GetBody().GetComponent<ArtemisBlessingComponent>().cachedIcnt;
                    if (icnt == 0) return origdmg;
                    float aDist = (chrm.GetBody().corePosition - body.body.corePosition).magnitude;

                    //Damage Calculation
                    //float distCoef = Math.Abs((Math.Min(aDist, 300f) - 25) * 0.05f);
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

