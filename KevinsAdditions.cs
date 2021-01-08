using BepInEx;
using BepInEx.Configuration;
using R2API;
using R2API.Utils;
using RoR2;
using RoR2.Skills;
using System;
using System.Collections.Generic;
using System.Reflection;
using TILER2;
using UnityEngine;
using static TILER2.MiscUtil;
using Path = System.IO.Path;


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
            "3.5.11";
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
#if DEBUG
            On.RoR2.Networking.GameNetworkManager.OnClientConnect += (self, user, t) => { };
#endif
            _logger = Logger;

            Logger.LogDebug("Loading assets...");
            /*using (var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KevinsAdditions.kevinsadditions_assets"))
            {
                var bundle = AssetBundle.LoadFromStream(stream);
                var provider = new AssetBundleResourcesProvider("@KevinsAdditions", bundle);
                ResourcesAPI.AddProvider(provider);
            }*/
            ResourcesAPI.AddProvider(Assets.PopulateAssets());
            cfgFile = new ConfigFile(Path.Combine(Paths.ConfigPath, ModGuid + ".cfg"), true);

            masterItemList = T2Module.InitAll<CatalogBoilerplate>(new T2Module.ModInfo
            {
                displayName = "Kevin's Additional Items",
                longIdentifier = "KevinsAdditions",
                shortIdentifier = "KAI",
                mainConfigFile = cfgFile
            });

            T2Module.SetupAll_PluginAwake(masterItemList);

            Logger.LogDebug("Adding Imp Mechanics...");
            ImpPlayerAdjustments.AddExtras();
        }

        private void Start()
        {
            T2Module.SetupAll_PluginStart(masterItemList);
            CatalogBoilerplate.ConsoleDump(Logger, masterItemList);
        }
    }
    public static class Assets
    {
        public static AssetBundle MainAssetBundle = null;
        public static AssetBundleResourcesProvider Provider;

        public static Texture charPortrait;

        public static Sprite iconP;
        public static Sprite icon1;
        public static Sprite icon2;
        public static Sprite icon3;
        public static Sprite icon4;

        public static IResourceProvider PopulateAssets()
        {
            if (MainAssetBundle == null)
            {
                using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KevinsAdditions.kevinsadditions_assets"))
                {
                    MainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                    Provider = new AssetBundleResourcesProvider("@KevinsAdditions", MainAssetBundle);
                }
            }

            // include this if you're using a custom soundbank
            /*using (Stream manifestResourceStream2 = Assembly.GetExecutingAssembly().GetManifestResourceStream("ExampleSurvivor.ExampleSurvivor.bnk"))
            {
                byte[] array = new byte[manifestResourceStream2.Length];
                manifestResourceStream2.Read(array, 0, array.Length);
                SoundAPI.SoundBanks.Add(array);
            }*/

            // and now we gather the assets
            //charPortrait = MainAssetBundle.LoadAsset<Sprite>("ExampleSurvivorBody").texture;

            iconP = MainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon1 = MainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon2 = MainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon3 = MainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon4 = MainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");

            return Provider;
        }
    }
}