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
            "3.5.12";
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

            Logger.LogDebug("Replacing Item Shaders with Hopoo shaders...");
            Assets.ReplaceShaders();

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
            On.RoR2.CharacterBody.OnInventoryChanged += On_InventoryChanged;
        }
        private void On_InventoryChanged(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self) //Checks item count and caches it. Also where the component is added
        {
            orig(self);
            foreach (CatalogBoilerplate x in masterItemList)
            {
                if (x is VirtItem_V2 item)
                    item.StoreItemCount(self);
            }
        }
        public static string OrderManifestLoreFormatter(string deviceName, string estimatedDelivery, string sentTo, string trackingNumber, string shippingMethod, string orderDetails)
        {
            string[] Manifest =
            {
                $"<align=left>Estimated Delivery:<indent=70%>Sent To:</indent></align>",
                $"<align=left>{estimatedDelivery}<indent=70%>{sentTo}</indent></align>",
                "",
                $"<indent=1%><style=cIsDamage><size=125%><u>  Shipping Details:\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0</u></size></style></indent>",
                "",
                $"<indent=2%>-Order: <style=cIsUtility>{deviceName}</style></indent>",
                $"<indent=4%><style=cStack>Tracking Number:  {trackingNumber}</style></indent>",
                "",
                $"<indent=2%>-Shipping Method: <style=cIsHealth>{shippingMethod}</style></indent>",
                "",
                "",
                $"<indent=2%>-Order Details: {orderDetails}</indent>",
                "",
                "",
                "",
                "<style=cStack>Delivery brought to you by the brand new </style><style=cIsUtility>Orbital Drop-Crate System (TM)</style>. <style=cStack><u>No refunds.</u></style>"
            };
            return String.Join("\n", Manifest);
        }
        public static string LabResultsLoreFormatter(string testDate, string testAdministrator, string profileID, string subjectName, string specimenName, string tests)
        {
            string[] Manifest =
            {
                $"<align=left>Test Date:<indent=70%>Test Administrator:</indent></align>",
                $"<align=left>{testDate}<indent=70%>{testAdministrator}</indent></align>",
                "",
                $"<indent=2%>-Specimen: <style=cIsUtility>\"{specimenName}\"</style></indent>",
                $"<indent=4%><style=cStack>Sample ID: {profileID}</style></indent>",
                $"<indent=2%>-Subject Name: <style=cIsHealth>{subjectName}</style></indent>",
                "",
                $"<indent=1%><style=cIsDamage><size=125%><u>  Test Results:\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0\u00A0</u></size></style></indent>",
                $"<indent=2%>{tests}</indent>",
            };
            return String.Join("\n", Manifest);
        }

    }
    public static class Assets
    {
        public static AssetBundle mainAssetBundle = null;
        public static AssetBundleResourcesProvider Provider;

        public static Texture charPortrait;

        public static Sprite iconP;
        public static Sprite icon1;
        public static Sprite icon2;
        public static Sprite icon3;
        public static Sprite icon4;

        public static IResourceProvider PopulateAssets()
        {
            using (var assetStream = Assembly.GetExecutingAssembly().GetManifestResourceStream("KevinsAdditions.kevinsadditions_assets"))
            {
                mainAssetBundle = AssetBundle.LoadFromStream(assetStream);
                Provider = new AssetBundleResourcesProvider("@KevinsAdditions", mainAssetBundle);
            }

            iconP = mainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon1 = mainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon2 = mainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon3 = mainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");
            icon4 = mainAssetBundle.LoadAsset<Sprite>("@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon");

            return Provider;
        }

        public static void ReplaceShaders()
        {
            var materials = mainAssetBundle.LoadAllAssets<Material>();
            //KevinsAdditionsPlugin._logger.LogError("materials is this long: " + materials.Length);
            for (int i = 0; i < materials.Length; i++)
            {
                //KevinsAdditionsPlugin._logger.LogError("material " + materials[i].name);
                if (materials[i].shader.name == "Standard")
                    materials[i].shader = Resources.Load<Shader>("shaders/deferred/hgstandard");

                //Imp Extract Rematerial
                if (materials[i].name == "ImpExtractGlass")
                {
                    materials[i].shader = Resources.Load<Shader>("shaders/fx/hgintersectioncloudremap");
                    var infusion = Resources.Load<GameObject>("prefabs/pickupmodels/PickupInfusion");
                    MeshRenderer[] meshRenderers = infusion.GetComponentsInChildren<MeshRenderer>();
                    foreach (MeshRenderer meshRenderer in meshRenderers)
                    {
                        if (meshRenderer.material.name.ToLower().Contains("glass"))
                        {
                            materials[i].CopyPropertiesFromMaterial(meshRenderer.material);
                            List<string> properties = new List<string>();
                            materials[i].GetTexturePropertyNames(properties);
                            foreach(string property in properties)
                            {
                                KevinsAdditionsPlugin._logger.LogError(property);
                            }
                        }
                    }
                }
            }
        }
    }
}