using RoR2;
using UnityEngine;
using System.Collections;
using TILER2;
using UnityEngine.Networking;
using R2API;
using static TILER2.StatHooks;
using KevinfromHP.KevinsClassics;
using System.Runtime.InteropServices;

/*----------------------------------------TO DO----------------------------------------
 *Yikes, quite a lot on the to-do for this
 * Make it work, add to this when you actually start working on it you lazy fuck
 */


/*
namespace KevinfromHP.KevinsClassics
{
public class ImpExtract : Equipment<ImpExtract>
{
    public override string displayName => "Imp Extract";

    [AutoUpdateEventInfo(AutoUpdateEventFlags.InvalidateDescToken | AutoUpdateEventFlags.InvalidatePickupToken)]
    [AutoItemConfig("Duration of the equipment.", AutoItemConfigFlags.None, 0f, float.MaxValue)]
    public float duration { get; private set; } = 13f;

    public BuffIndex ImpExtractBuff { get; private set; }

    protected override string NewLangName(string langid = null) => displayName;
    protected override string NewLangPickup(string langid = null) => "Transform into an Imp Overlord for" + duration + " seconds.";
    protected override string NewLangDesc(string langid = null) => "Damage increases the further your distance from a target.\n<style=cDeath>Damage decreases the closer your distance to a target.</style>";
    protected override string NewLangLore(string langid = null) => "A seemingly new item you've never seen before...";

    //private bool ILFailed = false;

    private GameObject origBody = null; //Where a user's original bodyprefab is stored
    public CharacterMaster cm = null;



    public ImpExtract()
    {
        modelPathName = "@KevinsClassics:Assets/KevinsClassics/prefabs/ImpExtract.prefab";
        iconPathName = "@KevinsClassics:Assets/KevinsClassics/textures/icons/ImpExtract_icon.png";
        onAttrib += (tokenIdent, namePrefix) =>
        {
            var ImpExtractBuffDef = new R2API.CustomBuff(new BuffDef
            {
                buffColor = Color.white,
                canStack = false,
                isDebuff = false,
                name = namePrefix + "ImpExtract",
                iconPath = "@KevinsClassics:Assets/KevinsClassics/textures/icons/ImpExtractBuff_icon.png"
            });
            ImpExtractBuff = BuffAPI.Add(ImpExtractBuffDef);
        };
    }

    protected override void LoadBehavior()
    {

        GetStatCoefficients += Evt_TILER2GetStatCoefficients;
        On.RoR2.CharacterBody.RemoveBuff += On_RemoveImp;

    }
    protected override void UnloadBehavior()
    {
        GetStatCoefficients -= Evt_TILER2GetStatCoefficients;
        On.RoR2.CharacterBody.RemoveBuff -= On_RemoveImp;
    }
    private void On_RemoveImp(On.RoR2.CharacterBody.orig_RemoveBuff orig, CharacterBody self, BuffIndex bufftype)
    {
        orig(self, bufftype);
        if (bufftype.Equals(ImpExtractBuff))
        {
            NetworkInstanceId origbodyid = self.master.bodyInstanceId;
            self.master.bodyPrefab = BodyCatalog.FindBodyPrefab(origBody);
            self.master.bodyInstanceId = origbodyid;
            self.master.Respawn(self.transform.position, self.transform.rotation, false);
            var cpt = self.GetComponent<ImpExtractComponent>();
            cpt.firstPress = true;
        }
    }

    private void Evt_TILER2GetStatCoefficients(CharacterBody sender, StatHookEventArgs args)
    {
        if (sender.HasBuff(ImpExtractBuff))
        {
            args.healthMultAdd *= .5f;
            args.damageMultAdd += .25f;
        }
    }

    protected override bool OnEquipUseInner(EquipmentSlot slot)
    {
        CharacterBody sbdy = slot.characterBody;
        if (sbdy == null) return false;
        KevinsClassicsPlugin._logger.LogMessage(1);
        var cpt = sbdy.GetComponent<ImpExtractComponent>();
        cm = sbdy.master;
        if (!cpt) cpt = sbdy.gameObject.AddComponent<ImpExtractComponent>();
        for (int i = 0; i < sbdy.timedBuffs.Count; i++)
        {
            if (sbdy.timedBuffs[i].buffIndex == ImpExtractBuff)
            {
                sbdy.timedBuffs[i].timer = duration;
                return true;
            }
        }

        KevinsClassicsPlugin._logger.LogMessage(2);

        if (BodyCatalog.FindBodyIndex(sbdy) != BodyCatalog.FindBodyIndex("ImpBossBody")) //Makes sure you don't accidentally grab the body of the imp and accidentally overwrite their original characterbody
        {
            origBody = BodyCatalog.FindBodyPrefab(BodyCatalog.GetBodyName(BodyCatalog.FindBodyIndex(sbdy))); //origBody is where the user's original bodyprefab is stored
        }
        KevinsClassicsPlugin._logger.LogMessage(3);

        cpt.Transform(sbdy.master, ImpExtractBuff, duration);
        return true;
    }


}

public class ImpExtractComponent : MonoBehaviour
{
    public bool firstPress = true;


    public void Transform(CharacterMaster master, BuffIndex buff, float duration)
    {
        KevinsClassicsPlugin._logger.LogMessage(4);
        StartCoroutine(BecomeImp(master, buff, duration));
    }

    public IEnumerator BecomeImp(CharacterMaster master, BuffIndex buff, float duration)
    {
        KevinsClassicsPlugin._logger.LogMessage(5);
        CharacterBody body = master.GetBody();
        // NetworkInstanceId origBodyId = master.bodyInstanceId;

        yield return new WaitForEndOfFrame();

        KevinsClassicsPlugin._logger.LogMessage(6);
        master.bodyPrefab = BodyCatalog.FindBodyPrefab("ImpBossBody");
        // master.bodyInstanceId = origBodyId;
        master.Respawn(body.transform.position, body.transform.rotation, false);
        body = master.GetBody();
        body.AddTimedBuff(buff, duration);
        var len = body.timedBuffs.Count - 1;
        /*while(body.timedBuffs[len].buffIndex == buff)
        {
            bool i3 = Input.GetKeyDown(KeyCode.E);
            if (i3 && BodyCatalog.FindBodyIndex(body) == BodyCatalog.FindBodyIndex("ImpBossBody") && firstPress)
            {
                firstPress = false; // stops someone from mega spamming their respawn
                body.ClearTimedBuffs(buff);
                break;
            }
            new WaitForEndOfFrame();
        }*/
/*}

}
}*/