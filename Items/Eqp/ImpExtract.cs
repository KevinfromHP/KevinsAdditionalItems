using R2API;
using RoR2;
using System.Collections;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.StatHooks;

/*----------------------------------------TO DO----------------------------------------
 * Better controls in Imp mode (Is this even reasonably possible?)
 * Looked into it, yes it is.
 */

namespace KevinfromHP.KevinsAdditions
{
    public class ImpExtract : Equipment_V2<ImpExtract>
    {
        public override string displayName => "Imp Extract";

        [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken | AutoUpdateEventFlags.InvalidatePickupToken)]
        [AutoConfig("Duration of the equipment.", AutoConfigFlags.PreventNetMismatch, 0f, float.MaxValue)]
        public float duration { get; private set; } = 15f;

        public BuffIndex ImpExtractBuff { get; private set; }
        private bool ilFailed = false;


        public override float cooldown { get; protected set; } = 100f;
        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "Transform into an Imp Overlord for " + duration + " seconds.";
        protected override string GetDescString(string langid = null) => "<style=cIsDamage>Transform</style> into an <style=cDeath>Imp Overlord</style> for <style=cIsUtility>" + duration + "</style> seconds. Damage increased by <style=cIsDamage>40%</style> while active.";
        protected override string GetLoreString(string langid = null) => "Order: Crux Brand Silly Red Seltzer! \nNumber: 09** \nEstimated Delivery: 04/12/2056 \nShipping Method: High Priority/Fragile \nShipping Address: Crux Fairgrounds, Tent 1, Earth \nShipping Details:" +
            "\n\nVeril-" +
            "\n\nI did exactly as you said, even used the stupid little bottle you sent me. Mehri doesn't suspect a thing, and they certainly won't notice a bit of this gunk missing. I expect my payment, in full, for pulling this off, and don't think I forgot the extra you promised if I got it in the bottle. " +
            "I don't understand why you or your weird little gang want this stuff, though. It's so gross, and...well, I can't quite describe it, but I feel wrong whenever I'm around it. As if \"something\" is watching me..." +
            "\n\n...And it's like that \"something\" hates me." +
            "\n\t(Lore by Keroro1454)";

        //public static bool assignedComponent = false;

        public ImpExtract()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/ImpExtract.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/ImpExtract_icon.png";
        }

        public override void SetupAttributes()
        {
            base.SetupAttributes();
            var ImpExtractBuffDef = new CustomBuff(new BuffDef
            {
                buffColor = Color.white,
                canStack = false,
                isDebuff = false,
                name = "KAIImpExtract",
                iconPath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/ImpExtractBuff_icon.png"
            });
            ImpExtractBuff = BuffAPI.Add(ImpExtractBuffDef);
        }

        //-----------------------------------------------

        public override void Install()
        {
            base.Install();

            GetStatCoefficients += Evt_TILER2GetStatCoefficients;
            On.RoR2.CharacterBody.RemoveBuff += On_BuffEnd;
            On.RoR2.Stage.RespawnCharacter += On_NextStage;
            On.RoR2.CharacterMaster.OnBodyDeath += On_Death;
            On.RoR2.Run.BeginGameOver += On_GameOver;
        }

        public override void Uninstall()
        {
            base.Uninstall();

            GetStatCoefficients -= Evt_TILER2GetStatCoefficients;
            On.RoR2.CharacterBody.RemoveBuff -= On_BuffEnd;
            On.RoR2.Stage.RespawnCharacter -= On_NextStage;
            On.RoR2.CharacterMaster.OnBodyDeath -= On_Death;
            On.RoR2.Run.BeginGameOver -= On_GameOver;
        }

        //-----------------------------------------------

        private void Evt_TILER2GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
        {
            if (sender.HasBuff(ImpExtractBuff))
            {
                args.healthMultAdd -= .75f;
                args.damageMultAdd += .45f;
            }
        }


        //-----------------------------------------------

        protected override bool PerformEquipmentAction(EquipmentSlot slot)
        {
            /*foreach (NetworkUser networkUser in NetworkUser.instancesList)
            {
                CharacterMaster networkMaster = networkUser.master;
                var netCpt = networkMaster.gameObject.AddComponent<ImpExtractComponent>();
                netCpt.GetVars(ImpExtractBuff);
            }*/
            CharacterBody sbdy = slot.characterBody;
            if (sbdy == null) return false;
            CharacterMaster master = sbdy.master;
            if (master.lostBodyToDeath) return false;
            var cpt = master.gameObject.GetComponent<ImpExtractComponent>();
            if (!cpt) cpt = master.gameObject.AddComponent<ImpExtractComponent>();
            cpt.buff = ImpExtractBuff;
            for (int i = 0; i < sbdy.timedBuffs.Count; i++) // checks to see if they already have the buff. If so, just renew it instead of respawning.
            {
                if (sbdy.timedBuffs[i].buffIndex == ImpExtractBuff)
                {
                    sbdy.timedBuffs[i].timer = duration;
                    return true;
                }
            }
            cpt.Transform(sbdy.master, ImpExtractBuff, duration);
            return true;
        }

        //-----------------------------------------------

        private void On_BuffEnd(On.RoR2.CharacterBody.orig_RemoveBuff orig, CharacterBody self, BuffIndex bufftype) //returns to normal form after equip time ends
        {
            orig(self, bufftype);
            if (bufftype.Equals(ImpExtractBuff))
            {
                self.masterObject.GetComponent<ImpExtractComponent>().RemoveImp(true);
                //IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
            }
        }
        private void On_NextStage(On.RoR2.Stage.orig_RespawnCharacter orig, Stage self, CharacterMaster characterMaster) //returns to original body prefab when spawning into a stage
        {
            if (!NetworkServer.active || !characterMaster)
            {
                return;
            }
            if (self.gameObject.GetComponent<ImpExtractComponent>() != null && self.gameObject.GetComponent<ImpExtractComponent>().isImp)
            {
                self.gameObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
                //IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
            }
            orig(self, characterMaster);
        }
        private void On_Death(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body) //returns to original body prefab after dying
        {
            if (NetworkServer.active)
            {
                if (self.gameObject.GetComponent<ImpExtractComponent>() != null && self.gameObject.GetComponent<ImpExtractComponent>().isImp)
                {
                    //if (self.inventory.GetItemCount(ItemIndex.ExtraLife) == 0)
                    self.gameObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
                    //IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
                }
            }
            orig(self, body);
        }
        private void On_GameOver(On.RoR2.Run.orig_BeginGameOver orig, Run self, GameEndingDef gameEndingDef) //goes to original body prefab on gameover
        {
            for (int i = 0; i < NetworkUser.readOnlyInstancesList.Count; i++)
            {
                NetworkUser networkUser = NetworkUser.readOnlyInstancesList[i];
                if (networkUser && networkUser.isParticipating)
                    if (networkUser.masterObject.GetComponent<ImpExtractComponent>() != null && networkUser.masterObject.GetComponent<ImpExtractComponent>().isImp)
                    {
                        networkUser.masterObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
                        //IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
                    }
            }
            orig(self, gameEndingDef);
        }
    }


    //----------------------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------------------
    //----------------------------------------------------------------------------------------------------------------


    public class ImpExtractComponent : MonoBehaviour
    {
        public GameObject origBodyPrefab; //Where a user's original bodyprefab is stored
        public BuffIndex buff;
        public CharacterMaster master;
        public bool isImp;
        public bool count;
        public int frame; // counts to 2 frames then goes to 0
        float health;
        float barrier;

        public void GetVars(BuffIndex buff)
        {
            this.buff = buff;
        }

        public void Transform(CharacterMaster master, BuffIndex buff, float duration)
        {
            StartCoroutine(BecomeImp(buff, duration));
        }

        public IEnumerator BecomeImp(BuffIndex buff, float duration)
        {
            CharacterBody body = master.GetBody();
            health = body.healthComponent.health / body.maxHealth;
            if (health > 1f) health = 1f;
            barrier = body.healthComponent.barrier;
            yield return new WaitForEndOfFrame();

            master.bodyPrefab = BodyCatalog.FindBodyPrefab("ImpBossPlayerBody");
            master.Respawn(body.transform.position, body.transform.rotation, false);
            body = master.GetBody();

            body.AddTimedBuff(buff, duration);
            //isImp = true;

            StartCoroutine(HealthMod(true));
        }

        public void RemoveImp(bool respawn)
        {
            CharacterBody body = master.GetBody();
            barrier = body.healthComponent.barrier / body.maxBarrier;
            master.bodyPrefab = origBodyPrefab;
            if (respawn)
            {
                master.Respawn(body.transform.position, body.transform.rotation, false);
                StartCoroutine(HealthMod(false));
            }
        }

        public IEnumerator HealthMod(bool become)
        {
            count = true;
            yield return new WaitUntil(() => frame == 2);
            count = false;
            var body = master.GetBody();
            if (become)
            {
                body.healthComponent.health = body.maxHealth * health;
                body.healthComponent.barrier = barrier;
            }
            else
            {
                body.healthComponent.barrier = body.maxBarrier * barrier;
            }
        }
        //-----------------------------------------------

        public void Awake()
        {
            frame = 0;
            isImp = false;
            count = false;
            master = gameObject.GetComponent<CharacterMaster>();
            origBodyPrefab = BodyCatalog.FindBodyPrefab(master.GetBodyObject()); //origBody is where the user's original bodyprefab is stored
        }
        public void Update()
        {
            if (count) frame++;
            else frame = 0;
        }
    }
}