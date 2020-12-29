﻿using RoR2;
using RoR2.UI;
using UnityEngine;
using System;
using System.Collections;
using System.Globalization;
using TILER2;
using UnityEngine.Networking;
using R2API;
using static TILER2.StatHooks;
using MonoMod.Cil;
using Mono.Cecil.Cil;
using System.Threading;
using System.Linq.Expressions;


/*----------------------------------------TO DO----------------------------------------
 * Better controls in Imp mode (Is this even reasonably possible?)
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

        public string key;

        public ImpExtract()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/ImpExtract.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/ImpExtract_icon.png";
        }

        public override void SetupAttributes()
        {
            base.SetupAttributes();
            var ImpExtractBuffDef = new R2API.CustomBuff(new BuffDef
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
            if (key == null)
                On.RoR2.UI.ContextManager.Awake += On_DrawHUD;
            On.RoR2.CharacterBody.RemoveBuff += On_BuffEnd;
            On.RoR2.Stage.RespawnCharacter += On_NextStage;
            On.RoR2.CharacterMaster.OnBodyDeath += On_Death;
            On.RoR2.Run.BeginGameOver += On_GameOver;
        }

        public override void Uninstall()
        {
            base.Uninstall();

            GetStatCoefficients -= Evt_TILER2GetStatCoefficients;
            On.RoR2.UI.ContextManager.Awake -= On_DrawHUD;
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
                args.damageMultAdd += .4f;
            }
        }

        //-----------------------------------------------

        private void On_DrawHUD(On.RoR2.UI.ContextManager.orig_Awake orig, ContextManager self)
        {
            orig(self);
            key = Glyphs.GetGlyphString(self.eventSystemLocator, "Interact").ToLower();
        }

        //-----------------------------------------------

        private void IL_DrawHUD(ILContext il)
        {
            var c = new ILCursor(il);


            int locThis = -1;
            int locGlyph = -1;
            c.TryGotoNext(
            x => x.MatchLdarg(out locThis),
            x => x.MatchLdfld<ContextManager>("glyphTMP"),
            x => x.MatchLdloc(out locGlyph),
            x => x.MatchCallOrCallvirt("TMPro.TMP_Text", "set_text"));

            c.Emit(OpCodes.Ldarg, locThis);
            c.Emit(OpCodes.Ldloc, locGlyph);
            c.EmitDelegate<Func<ContextManager, string, string>>((argThis, glyph) =>
            {
                return "<style=cKeyBinding>" + key.ToUpper() + "</style>"; //string.Format(CultureInfo.InvariantCulture, " < style=cKeyBinding>{0}</style>", key.ToUpper());
            });
            c.Emit(OpCodes.Stloc, locGlyph);


            int locDesc = -1;
            c.TryGotoNext(
             x => x.MatchLdarg(out locThis),
             x => x.MatchLdfld<ContextManager>("descriptionTMP"),
             x => x.MatchLdloc(out locDesc),
             x => x.MatchCallOrCallvirt("TMPro.TMP_Text", "set_text"));

            c.Emit(OpCodes.Ldarg, locThis);
            c.Emit(OpCodes.Ldloc, locDesc);
            c.EmitDelegate<Func<ContextManager, string, string>>((argThis, desc) =>
            {
                return "<style=cKeyBinding>" + key.ToUpper() + "</style> Revert Form";
            });
            c.Emit(OpCodes.Stloc, locDesc);


            int locActive = -1;
            c.TryGotoNext(
            x => x.MatchLdarg(0),
            x => x.MatchLdfld<ContextManager>("contextDisplay"),
            x => x.MatchLdloc(out locActive),
            x => x.MatchCallOrCallvirt<GameObject>("SetActive"));

            c.Emit(OpCodes.Ldarg, locThis);
            c.Emit(OpCodes.Ldloc, locActive);
            c.EmitDelegate<Func<ContextManager, bool, bool>>((argThis, active) =>
            {
                active = true;
                return active;
            });
            c.Emit(OpCodes.Stloc, locActive);
        }

        //-----------------------------------------------

        protected override bool PerformEquipmentAction(EquipmentSlot slot)
        {
            CharacterBody sbdy = slot.characterBody;
            if (sbdy == null) return false;
            CharacterMaster master = sbdy.master;
            if (master.lostBodyToDeath) return false; //Testing this
            var cpt = master.gameObject.GetComponent<ImpExtractComponent>();
            if (!cpt) cpt = master.gameObject.AddComponent<ImpExtractComponent>();
            for (int i = 0; i < sbdy.timedBuffs.Count; i++)
            {
                if (sbdy.timedBuffs[i].buffIndex == ImpExtractBuff)
                {
                    sbdy.timedBuffs[i].timer = duration;
                    return true;
                }
            }
            //cpt.origBody = BodyCatalog.FindBodyPrefab(BodyCatalog.GetBodyName(BodyCatalog.FindBodyIndex(sbdy))); //origBody is where the user's original bodyprefab is stored
            cpt.GetVars(ImpExtractBuff, key);
            cpt.Transform(sbdy.master, ImpExtractBuff, duration);
            IL.RoR2.UI.ContextManager.Update += IL_DrawHUD;
            return true;
        }

        //-----------------------------------------------

        private void On_BuffEnd(On.RoR2.CharacterBody.orig_RemoveBuff orig, CharacterBody self, BuffIndex bufftype) //returns to normal form after equip time ends
        {
            orig(self, bufftype);
            if (bufftype.Equals(ImpExtractBuff))
            {
                self.masterObject.GetComponent<ImpExtractComponent>().RemoveImp(true);
                IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
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
                IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
            }
            orig(self, characterMaster);
        }
        private void On_Death(On.RoR2.CharacterMaster.orig_OnBodyDeath orig, CharacterMaster self, CharacterBody body) //returns to original body prefab after dying
        {
            if (NetworkServer.active)
                if (self.gameObject.GetComponent<ImpExtractComponent>() != null && self.gameObject.GetComponent<ImpExtractComponent>().isImp)
                {
                    //if (self.inventory.GetItemCount(ItemIndex.ExtraLife) == 0)
                    self.gameObject.GetComponent<ImpExtractComponent>().RemoveImp(false);
                    IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
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
                        IL.RoR2.UI.ContextManager.Update -= IL_DrawHUD;
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
        public GameObject origBody = null; //Where a user's original bodyprefab is stored
        public string key;
        public BuffIndex buff;
        public CharacterMaster master;
        public bool isImp;
        public bool count;
        public int frame; // counts to 2 frames then goes to 0
        float health;
        float barrier;

        public void GetVars(BuffIndex buff, string key)
        {
            this.key = key;
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

            NetworkInstanceId origbodyid = master.bodyInstanceId;
            master.bodyPrefab = BodyCatalog.FindBodyPrefab("ImpBossBody");
            master.Respawn(body.transform.position, body.transform.rotation, false);
            master.bodyInstanceId = origbodyid;
            body = master.GetBody();
            body.AddTimedBuff(buff, duration);
            isImp = true;
            StartCoroutine(HealthMod(true));
        }

        public void RemoveImp(bool respawn)
        {
            CharacterBody body = master.GetBody();
            barrier = body.healthComponent.barrier / body.maxBarrier;
            NetworkInstanceId origbodyid = master.bodyInstanceId;
            master.bodyPrefab = BodyCatalog.FindBodyPrefab(origBody);
            master.bodyInstanceId = origbodyid;
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
            master = gameObject.GetComponent<CharacterMaster>();
            origBody = BodyCatalog.FindBodyPrefab(master.GetBodyObject()); //origBody is where the user's original bodyprefab is stored
        }
        public void Update()
        {
            if (count) frame++;
            else frame = 0;
            if (master && isImp)
            {
                bool i3 = Input.GetKeyDown(key);
                if (i3)
                {
                    master.GetBody().ClearTimedBuffs(buff);
                    isImp = false;
                }
            }
        }
    }
}