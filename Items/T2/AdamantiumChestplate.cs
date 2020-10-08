using RoR2;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.StatHooks;

namespace KevinfromHP.KevinsClassics
{
    public class AdamantiumChestplate : Item<AdamantiumChestplate>
    {
        public override string displayName => "Adamantium Chestplate";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken)]
        [AutoItemConfig("How much armor each item gives", AutoItemConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float armorAdd { get; private set; } = 10f;
        protected override string NewLangName(string langid = null) => displayName;
        protected override string NewLangPickup(string langid = null) => "Gain " + armorAdd + " armor.";
        protected override string NewLangDesc(string langid = null) => "Gain " + armorAdd + " armor (+" + armorAdd + " per stack).";
        protected override string NewLangLore(string langid = null) => "A seemingly new item you've never seen before...";


        public AdamantiumChestplate()
        {
            modelPathName = "@KevinsClassics:Assets/KevinsClassics/prefabs/AdamantiumChestplate.prefab";
            iconPathName = "@KevinsClassics:Assets/KevinsClassics/textures/icons/AdamantiumChestplate_icon.png";
        }


        protected override void LoadBehavior()
        {
            GetStatCoefficients += Evt_TILER2GetStatCoefficients;
        }

        protected override void UnloadBehavior()
        {
            GetStatCoefficients -= Evt_TILER2GetStatCoefficients;
        }

        private void Evt_TILER2GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            var icnt = GetCount(sender.inventory);
            args.armorAdd += icnt * armorAdd;

        }
    }
}