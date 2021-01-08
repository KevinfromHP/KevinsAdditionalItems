using RoR2;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.StatHooks;

namespace KevinfromHP.KevinsAdditions
{
    public class AdamantiumChestplate : Item_V2<AdamantiumChestplate>
    {
        public override string displayName => "Adamantium Chestplate";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("How much armor each item gives", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float armorAdd { get; private set; } = 15f;


        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "Gain " + armorAdd + " armor.";
        protected override string GetDescString(string langid = null) => "Gain " + armorAdd + " <style=cIsUtility>armor</style> <style=cStack>(+" + armorAdd + " per stack)</style>.";
        protected override string GetLoreString(string langid = null) => "Order: Adamantium Chestplate \nTracking Number: 09** \nEstimated Delivery: 12/23/2056 \nShipping Method: Standard \nShipping Address: System Police Station 13, Port of Marv, Ganymede Shipping Details:" +
            "\n\nLast night was big, shutting down a massive distribution center for one of the biggest criminal rings this side of the galaxy. Not only did we seize tons of narcotics, illicit stimulant syringes, spiked beverages, and crates of those blasted berries, but also a ton of unmarked credits.That bust left us with a mountain of...'evidence'...evidence that netted the station far more than it needed to pay back its debts. So I felt it was only fair to celebrate with a bit of a splurge here." +
            "\n\nThese decommissioned chestplates are admittedly not technically the true top-of-the-line in the protection industry. But the army always keeps the best toys to itself, so this is the next best thing. I'm sure Jaime will be excited, given how the damn kid seems to attract trouble, and hits, like a magnet." +
            "\n\t(Lore by Keroro1454)";


        public AdamantiumChestplate()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/AdamantiumChestplate.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/AdamantiumChestplate_icon.png";
        }


        public override void Install()
        {
            base.Install();

            GetStatCoefficients += Evt_TILER2GetStatCoefficients;
        }

        public override void Uninstall()
        {
            base.Uninstall();

            GetStatCoefficients -= Evt_TILER2GetStatCoefficients;
        }

        private void Evt_TILER2GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            var icnt = GetCount(sender);
            args.armorAdd += icnt * armorAdd;

        }
    }
}