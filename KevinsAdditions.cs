using BepInEx;
using R2API;
using R2API.Utils;
using RoR2;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using BepInEx.Configuration;
using Path = System.IO.Path;
using TILER2;
using static TILER2.MiscUtil;

namespace KevinfromHP.KevinsAdditions
{
    [BepInPlugin(ModGuid, ModName, ModVer)]
    [BepInDependency(R2API.R2API.PluginGUID, R2API.R2API.PluginVersion)]
    [BepInDependency(TILER2Plugin.ModGuid, "3.0.4")]
    [BepInDependency("com.funkfrog_sipondo.sharesuite", BepInDependency.DependencyFlags.SoftDependency)]
    [NetworkCompatibility(CompatibilityLevel.EveryoneMustHaveMod, VersionStrictness.EveryoneNeedSameModVersion)]
    [R2APISubmoduleDependency(nameof(ItemAPI), nameof(LanguageAPI), nameof(ResourcesAPI), nameof(PlayerAPI), nameof(PrefabAPI), nameof(BuffAPI), nameof(LoadoutAPI))]
    public class KevinsAdditionsPlugin : BaseUnityPlugin
    {
        public const string ModVer =
#if DEBUG
                "0." +
#endif
            "3.5.9";
        public const string ModName = "KevinsAdditions";
        public const string ModGuid = "com.KevinfromHP.KevinsAdditions";

        private static ConfigFile cfgFile;
        internal static FilingDictionary<CatalogBoilerplate> masterItemList = new FilingDictionary<CatalogBoilerplate>();
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

            Logger.LogDebug("Loading assets...");
            using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KevinsAdditions.kevinsadditions_assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@KevinsAdditions", bundle);
                ResourcesAPI.AddProvider(provider);
            }
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            masterItemList = T2Module.InitAll<CatalogBoilerplate>(new T2Module.ModInfo 
            {
                displayName = "Kevin's Additional Items",
                longIdentifier = "KevinsAdditions",
                shortIdentifier = "KAI",
                mainConfigFile = cfgFile
            });

            T2Module.SetupAll_PluginAwake(masterItemList);
        }

        private void Start()
        {
            T2Module.SetupAll_PluginStart(masterItemList);
            CatalogBoilerplate.ConsoleDump(Logger, masterItemList);
        }

    }
}