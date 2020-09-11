using BepInEx;
using MonoMod.Cil;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
using Mono.Cecil.Cil;
using System;
using TMPro;
using UnityEngine.Networking;
using Path = System.IO.Path;
using System.Collections.ObjectModel;
using TILER2;
using static TILER2.MiscUtil;
using ThinkInvisible.ClassicItems;

namespace KevinfromHP.KevinsClassics
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, "2.0.0")]
    [BepInDependency(ClassicItemsPlugin.ModGuid, "4.5.0")]
    [BepInDependency("com.funkfrog_sipondo.sharesuite", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI))]
    public class KevinsClassicsPlugin : BaseUnityPlugin
    {
        public const string ModVer =
#if DEBUG
                "0." +
#endif
            "1.0.1";
        public const string ModName = "KevinsClassics";
        public const string ModGuid = "com.KevinfromHP.KevinsClassics";

        private static ConfigFile cfgFile;

        internal static FilingDictionary<ItemBoilerplate> masterItemList = new FilingDictionary<ItemBoilerplate>();

        public class GlobalConfig : AutoItemConfigContainer
        {
            [AutoItemConfig("If true, hides the dynamic description text on trading card-style pickup models. Enabling this may slightly improve performance.",
                AutoItemConfigFlags.DeferForever)]
            public bool hideDesc { get; private set; } = true;

            [AutoItemConfig("If true, descriptions on trading card-style pickup models will be the (typically longer) description text of the item. If false, pickup text will be used instead.",
                AutoItemConfigFlags.DeferForever)]
            public bool longDesc { get; private set; } = true;

            [AutoItemConfig("If true, trading card-style pickup models will have customized spin behavior which makes descriptions more readable. Disabling this may slightly improve compatibility and performance.",
                AutoItemConfigFlags.DeferForever)]
            public bool spinMod { get; private set; } = true;
        }
            
        public static readonly GlobalConfig globalConfig = new GlobalConfig();

        private static readonly ReadOnlyDictionary<ItemTier, string> modelNameMap = new ReadOnlyDictionary<ItemTier, string>(new Dictionary<ItemTier, string>{
            {ItemTier.Boss, "BossCard"},
            {ItemTier.Lunar, "LunarCard"},
            {ItemTier.Tier1, "CommonCard"},
            {ItemTier.Tier2, "UncommonCard"},
            {ItemTier.Tier3, "RareCard"}
        });

        internal static BepInEx.Logging.ManualLogSource _logger;

#if DEBUG
        public void Update()
        {
            var i3 = Input.GetKeyDown(KeyCode.F3);
            var i4 = Input.GetKeyDown(KeyCode.F4);
            var i5 = Input.GetKeyDown(KeyCode.F5);
            var i6 = Input.GetKeyDown(KeyCode.F6);
            var i7 = Input.GetKeyDown(KeyCode.F7);
            if (i3 || i4 || i5 || i6 || i7)
            {
                var trans = PlayerCharacterMasterController.instances[0].master.GetBodyObject().transform;

                List<PickupIndex> spawnList;
                if (i3) spawnList = Run.instance.availableTier1DropList;
                else if (i4) spawnList = Run.instance.availableTier2DropList;
                else if (i5) spawnList = Run.instance.availableTier3DropList;
                else if (i6) spawnList = Run.instance.availableEquipmentDropList;
                else spawnList = Run.instance.availableLunarDropList;

                PickupDropletController.CreatePickupDroplet(spawnList[Run.instance.spawnRng.RangeInt(0, spawnList.Count)], trans.position, new Vector3(0f, -5f, 0f));
            }
        }
#endif

        private void Awake()
        {
            _logger = Logger;

            Logger.LogDebug("Performing plugin setup:");

#if DEBUG
            Logger.LogWarning("Running test build with debug enabled! If you're seeing this after downloading the mod from Thunderstore, please panic.");
#endif

            Logger.LogDebug("Loading assets...");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KevinsClassics.kevinsclassics_assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@KevinsClassics", bundle);
                ResourcesAPI.AddProvider(provider);
            }

            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            Logger.LogDebug("Loading global configs...");

            globalConfig.BindAll(cfgFile, "KevinsClassics", "Global");

            Logger.LogDebug("Instantiating item classes...");
            masterItemList = ItemBoilerplate.InitAll("KevinsClassics");


            Logger.LogDebug("Loading item configs...");
            foreach (ItemBoilerplate x in masterItemList)
            {
                x.ConfigEntryChanged += (sender, args) => {
                    if ((args.flags & (AutoUpdateEventFlags.InvalidateNameToken | (globalConfig.longDesc ? AutoUpdateEventFlags.InvalidateDescToken : AutoUpdateEventFlags.InvalidatePickupToken))) == 0) return;
                    if (x.pickupDef != null)
                    {
                        var ctsf = x.pickupDef.displayPrefab?.transform;
                        if (!ctsf) return;
                        var cfront = ctsf.Find("cardfront");
                        if (!cfront) return;

                        cfront.Find("carddesc").GetComponent<TextMeshPro>().text = Language.GetString(globalConfig.longDesc ? x.descToken : x.pickupToken);
                        cfront.Find("cardname").GetComponent<TextMeshPro>().text = Language.GetString(x.nameToken);
                    }
                    if (x.logbookEntry != null)
                    {
                        x.logbookEntry.modelPrefab = x.pickupDef.displayPrefab;
                    }
                };
                x.SetupConfig(cfgFile);
            }

            Logger.LogDebug("Registering item attributes...");

            int longestName = 0;
            foreach (ItemBoilerplate x in masterItemList)
            {
                string mpnOvr = null;
                if (x is Item item) mpnOvr = "@ClassicItems:Assets/ClassicItems/models/" + modelNameMap[item.itemTier] + ".prefab";
                else if (x is Equipment eqp) mpnOvr = "@ClassicItems:Assets/ClassicItems/models/" + (eqp.eqpIsLunar ? "LqpCard.prefab" : "EqpCard.prefab");
                var ipnOvr = "@KevinsClassics:Assets/KevinsClassics/icons/" + x.itemCodeName + "_icon.png";

                if (mpnOvr != null)
                {
                    typeof(ItemBoilerplate).GetProperty(nameof(ItemBoilerplate.modelPathName)).SetValue(x, mpnOvr);
                    typeof(ItemBoilerplate).GetProperty(nameof(ItemBoilerplate.iconPathName)).SetValue(x, ipnOvr);
                }

                x.SetupAttributes("KEVINSCLASSICS", "KC");
                if (x.itemCodeName.Length > longestName) longestName = x.itemCodeName.Length;
            }

            Logger.LogMessage("Index dump follows (pairs of name / index):");
            foreach (ItemBoilerplate x in masterItemList)
            {
                if (x is Equipment eqp)
                    Logger.LogMessage("Equipment KC" + x.itemCodeName.PadRight(longestName) + " / " + ((int)eqp.regIndex).ToString());
                else if (x is Item item)
                    Logger.LogMessage("     Item KC" + x.itemCodeName.PadRight(longestName) + " / " + ((int)item.regIndex).ToString());
                else if (x is Artifact afct)
                    Logger.LogMessage(" Artifact KC" + x.itemCodeName.PadRight(longestName) + " / " + ((int)afct.regIndex).ToString());
                else
                    Logger.LogMessage("    Other KC" + x.itemCodeName.PadRight(longestName) + " / N/A");
            }

            Logger.LogDebug("Tweaking vanilla stuff...");

            On.RoR2.PickupCatalog.Init += On_PickupCatalogInit;


            On.RoR2.UI.LogBook.LogBookController.BuildPickupEntries += On_LogbookBuildPickupEntries;

            if (globalConfig.spinMod)
                IL.RoR2.PickupDisplay.Update += IL_PickupDisplayUpdate;

            Logger.LogDebug("Registering shared buffs...");
            //used only for purposes of Death Mark; applied by Permafrost and Snowglobe

            Logger.LogDebug("Registering item behaviors...");

            foreach (ItemBoilerplate x in masterItemList)
            {
                x.SetupBehavior();
            }

            Logger.LogDebug("Initial setup done!");
        }

        private void Start()
        {
            Logger.LogDebug("Performing late setup:");
            Logger.LogDebug("Setting up lang token overrides...");
            Scepter.instance.PatchLang();
            Language.CCLanguageReload(new ConCommandArgs());
            Logger.LogDebug("Late setup done!");
        }

       private void IL_PickupDisplayUpdate(ILContext il)
        {
            ILCursor c = new ILCursor(il);

            bool ILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchLdfld<PickupDisplay>("modelObject"));
            GameObject puo = null;
            if (ILFound)
            {
                c.Emit(OpCodes.Dup);
                c.EmitDelegate<Action<GameObject>>(x => {
                    puo = x;
                });
            }
            else
            {
                Logger.LogError("Failed to apply vanilla IL patch (pickup model spin modifier)");
                return;
            }

            ILFound = c.TryGotoNext(MoveType.After,
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PickupDisplay>("spinSpeed"),
                x => x.MatchLdarg(0),
                x => x.MatchLdfld<PickupDisplay>("localTime"),
                x => x.MatchMul());
            if (ILFound)
            {
                c.EmitDelegate<Func<float, float>>((origAngle) => {
                    if (!puo || !puo.GetComponent<SpinModFlag>() || !NetworkClient.active || PlayerCharacterMasterController.instances.Count == 0) return origAngle;
                    var body = PlayerCharacterMasterController.instances[0].master.GetBody();
                    if (!body) return origAngle;
                    var btsf = body.coreTransform;
                    if (!btsf) btsf = body.transform;
                    return RoR2.Util.QuaternionSafeLookRotation(btsf.position - puo.transform.position).eulerAngles.y
                        + (float)Math.Tanh(((origAngle / 100.0f) % 6.2832f - 3.1416f) * 2f) * 180f
                        + 180f
                        - (puo.transform.parent?.eulerAngles.y ?? 0f);
                });
            }
            else
            {
                Logger.LogError("Failed to apply vanilla IL patch (pickup model spin modifier)");
            }

        }

        private void On_PickupCatalogInit(On.RoR2.PickupCatalog.orig_Init orig)
        {
            orig();

            int x = 1;
            Logger.LogDebug("Processing pickup models...");

            foreach (ItemBoilerplate bpl in masterItemList)
            {
                PickupIndex pind;
                if (bpl is Equipment equipment) pind = PickupCatalog.FindPickupIndex(equipment.regIndex);
                else if (bpl is Item item) pind = PickupCatalog.FindPickupIndex(item.regIndex);
                else continue;
                var pickup = PickupCatalog.GetPickupDef(pind);
                pickup.displayPrefab = pickup.displayPrefab.InstantiateClone("KC" + bpl.itemCodeName + "PickupCardPrefab", false);
            }

            int replacedDescs = 0;
            var tmpfont = Resources.Load<TMP_FontAsset>("tmpfonts/misc/tmpRiskOfRainFont Bold OutlineSDF");
            var tmpmtl = Resources.Load<Material>("tmpfonts/misc/tmpRiskOfRainFont Bold OutlineSDF");

            foreach (var pickup in PickupCatalog.allPickups)
            {
                var ctsf = pickup.displayPrefab?.transform;
                if (!ctsf) continue;
                var cfront = ctsf.Find("cardfront");
                if (cfront == null) continue;
                var croot = cfront.Find("carddesc");
                var cnroot = cfront.Find("cardname");
                var csprite = ctsf.Find("ovrsprite");
                csprite.GetComponent<MeshRenderer>().material.mainTexture = pickup.iconTexture;

                if (globalConfig.spinMod)
                    pickup.displayPrefab.AddComponent<SpinModFlag>();
                string pname;
                string pdesc;
                Color prar = new Color(1f, 0f, 1f);
                if (pickup.interactContextToken == "EQUIPMENT_PICKUP_CONTEXT")
                {
                    var eqp = EquipmentCatalog.GetEquipmentDef(pickup.equipmentIndex);
                    if (eqp == null) continue;
                    pname = Language.GetString(eqp.nameToken);
                    pdesc = Language.GetString(globalConfig.longDesc ? eqp.descriptionToken : eqp.pickupToken);
                    prar = new Color(1f, 0.7f, 0.4f);
                }
                else if (pickup.interactContextToken == "ITEM_PICKUP_CONTEXT")
                {
                    var item = ItemCatalog.GetItemDef(pickup.itemIndex);
                    if (item == null) continue;
                    pname = Language.GetString(item.nameToken);
                    pdesc = Language.GetString(globalConfig.longDesc ? item.descriptionToken : item.pickupToken);
                    Logger.LogMessage("Checkpoint 8");
                    switch (item.tier)
                    {
                        case ItemTier.Boss: prar = new Color(1f, 1f, 0f); break;
                        case ItemTier.Lunar: prar = new Color(0f, 0.6f, 1f); break;
                        case ItemTier.Tier1: prar = new Color(0.8f, 0.8f, 0.8f); break;
                        case ItemTier.Tier2: prar = new Color(0.2f, 1f, 0.2f); break;
                        case ItemTier.Tier3: prar = new Color(1f, 0.2f, 0.2f); break;
                    }
                }
                else continue;

                if (globalConfig.hideDesc)
                {
                    Destroy(croot.gameObject);
                    Destroy(cnroot.gameObject);
                }
                else
                {
                    /*var cdsc = croot.gameObject.AddComponent<TextMeshPro>();
                    cdsc.richText = true;
                    cdsc.enableWordWrapping = true;
                    cdsc.alignment = TextAlignmentOptions.Center;
                    cdsc.margin = new Vector4(4f, 1.874178f, 4f, 1.015695f);
                    cdsc.enableAutoSizing = true;
                    cdsc.overrideColorTags = false;
                    cdsc.fontSizeMin = 1;
                    cdsc.fontSizeMax = 8;
                    _ = cdsc.renderer;
                    cdsc.font = tmpfont;
                    cdsc.material = tmpmtl;
                    cdsc.color = Color.black;
                    cdsc.text = pdesc;

                    var cname = cnroot.gameObject.AddComponent<TextMeshPro>();
                    cname.richText = true;
                    cname.enableWordWrapping = false;
                    cname.alignment = TextAlignmentOptions.Center;
                    cname.margin = new Vector4(6.0f, 1.2f, 6.0f, 1.4f);
                    cname.enableAutoSizing = true;
                    cname.overrideColorTags = true;
                    cname.fontSizeMin = 1;
                    cname.fontSizeMax = 10;
                    _ = cname.renderer;
                    cname.font = tmpfont;
                    cname.material = tmpmtl;
                    cname.outlineColor = prar;
                    cname.outlineWidth = 0.15f;
                    cname.color = Color.black;
                    cname.fontStyle = FontStyles.Bold;
                    cname.text = pname;*/
                }
                replacedDescs++;
            }
            Logger.LogMessage((globalConfig.hideDesc ? "Destroyed " : "Inserted ") + replacedDescs + " pickup model descriptions.");
        }

        private RoR2.UI.LogBook.Entry[] On_LogbookBuildPickupEntries(On.RoR2.UI.LogBook.LogBookController.orig_BuildPickupEntries orig)
        {
            var retv = orig();
            Logger.LogDebug("Processing logbook models...");
            int replacedModels = 0;
            foreach (RoR2.UI.LogBook.Entry e in retv)
            {
                if (!(e.extraData is PickupIndex)) continue;
                if (e.modelPrefab == null) continue;
                if (e.modelPrefab.transform.Find("cardfront"))
                {
                    e.modelPrefab = PickupCatalog.GetPickupDef((PickupIndex)e.extraData).displayPrefab;
                    replacedModels++;
                }
            }
            Logger.LogDebug("Modified " + replacedModels + " logbook models.");
            return retv;
        }
    }
    public class SpinModFlag : MonoBehaviour { }
}