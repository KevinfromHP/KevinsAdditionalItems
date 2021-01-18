using R2API;
using RoR2;
using System.Collections;
using TILER2;
using UnityEngine;
using UnityEngine.Networking;
using static TILER2.StatHooks;
using KevinfromHP.KevinsAdditions;
using Mono.Cecil.Cil;
using MonoMod.Cil;
using System;
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

        string deviceName = "<style=cIsHealth>Crux Brand Silly Red Seltzer!</style>";
        string estimatedDelivery = "04/12/2056";
        string sentTo = "Crux Fairgrounds, Tent 1, Earth";
        string trackingNumber = "09**";
        string shippingMethod = "High Priority/Fragile";
        string orderDetails = "\n\nVeril-" +
            "\n\nI did exactly as you said, even used the stupid little bottle you sent me. Mehri doesn't suspect a thing, and they certainly won't notice a bit of this gunk missing. I expect my payment, in full, for pulling this off, and don't think I forgot the extra you promised if I got it in the bottle. " +
            "I don't understand why you or your weird little gang want this stuff, though. It's so gross, and...well, I can't quite describe it, but I feel wrong whenever I'm around it. As if \"something\" is watching me..." +
            "\n\n...And it's like that \"something\" hates me." +
            "\n\n\t<style=cStack>(Lore by</style> <style=cIsUtility>Keroro1454</style><style=cStack>)</style>";
        protected override string GetLoreString(string langid = null) => KevinsAdditionsPlugin.OrderManifestLoreFormatter(deviceName, estimatedDelivery, sentTo, trackingNumber, shippingMethod, orderDetails);
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

            IL.RoR2.Stage.RespawnCharacter += IL_NextStage;
            if (ilFailed)
                IL.RoR2.Stage.RespawnCharacter -= IL_NextStage;
            else
            {
                GetStatCoefficients += Evt_TILER2GetStatCoefficients;
                On.RoR2.CharacterBody.RemoveBuff += On_BuffEnd;
                On.RoR2.CharacterMaster.OnBodyDeath += On_Death;
                On.RoR2.Run.BeginGameOver += On_GameOver;
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();

            IL.RoR2.Stage.RespawnCharacter -= IL_NextStage;
            GetStatCoefficients -= Evt_TILER2GetStatCoefficients;
            On.RoR2.CharacterBody.RemoveBuff -= On_BuffEnd;
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

        //This is really weird. The first call has to return false because whenever you hold down the key for longer than a frame, it uses the equipment twice because of the characterbody switch. So, the first call (when they are still normal) returns false, and then it returns true when they are an imp (the call where it resets the timer)
        protected override bool PerformEquipmentAction(EquipmentSlot slot)
        {
            CharacterBody sbdy = slot.characterBody;
            if (sbdy == null) return false;
            CharacterMaster master = sbdy.master;
            if (master.lostBodyToDeath) return false;
            var cpt = master.gameObject.GetComponent<ImpExtractComponent>();
            if (!cpt) cpt = master.gameObject.AddComponent<ImpExtractComponent>();
            cpt.buff = ImpExtractBuff;
            if (cpt.isImp)
                for (int i = 0; i < sbdy.timedBuffs.Count; i++) // checks to see if they already have the buff. If so, just renew it instead of respawning.
                {
                    if (sbdy.timedBuffs[i].buffIndex == ImpExtractBuff)
                    {
                        sbdy.timedBuffs[i].timer = duration;
                        return true;
                    }
                }

            cpt.BecomeImp(duration);
            slot.characterBody = cpt.master.GetBody();
            return false;
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
        private void IL_NextStage(ILContext il) //returns to original body prefab when spawning into a stage
        {
            var c = new ILCursor(il);
            bool ILFound;



            ILFound = c.TryGotoNext(
                x => x.MatchLdarg(0),
                x => x.MatchCallOrCallvirt<Stage>("GetPlayerSpawnTransform"),
                x => x.MatchStloc(0));
            if (!ILFound)
            {
                ilFailed = true;
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Imp Extract IL patch (NextStage var read), item will not work; target instructions not found");
                return;
            }

            c.Emit(OpCodes.Ldarg_1);
            c.EmitDelegate<Action<CharacterMaster>>((master) =>
            {
                if (master.gameObject.GetComponent<ImpExtractComponent>() ? master.gameObject.GetComponent<ImpExtractComponent>().isImp : false)
                    master.gameObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
            });
        }
        private void On_Death(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body) //returns to original body prefab after dying
        {
            if (NetworkServer.active)
            {
                if (self.gameObject.GetComponent<ImpExtractComponent>() != null && self.gameObject.GetComponent<ImpExtractComponent>().isImp)
                    self.gameObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
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
                        networkUser.masterObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
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
        public bool canTransform = true;
        float health;
        float barrier;

        public void GetVars(BuffIndex buff)
        {
            this.buff = buff;
        }

        public void Transform(CharacterMaster master, BuffIndex buff, float duration)
        {
            //StartCoroutine(BecomeImp(buff, duration));
            BecomeImp(duration);
        }

        public void /*IEnumerator*/ BecomeImp(/*BuffIndex buff, */float duration)
        {
            CharacterBody body = master.GetBody();
            health = body.healthComponent.health / body.maxHealth;
            if (health > 1f) health = 1f;
            barrier = body.healthComponent.barrier;

            master.bodyPrefab = BodyCatalog.FindBodyPrefab("ImpBossPlayerBody");
            master.Respawn(body.transform.position, body.transform.rotation, false);
            body = master.GetBody();
            //yield return new WaitForEndOfFrame();

            body.AddTimedBuff(buff, duration);
            isImp = true;
            canTransform = false;
            StartCoroutine(HealthMod(true));
            //HealthMod(true);
            canTransform = true;
        }

        public void RemoveImp(bool respawn)
        {
            CharacterBody body = master.GetBody();
            barrier = body.healthComponent.barrier / body.maxBarrier;
            master.bodyPrefab = origBodyPrefab;
            isImp = false;
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