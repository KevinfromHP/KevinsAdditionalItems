using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using TILER2;
using UnityEngine;

namespace KevinfromHP.KevinsAdditions
{
    public class SeekingStone : Item_V2<SeekingStone>
    {
        public override string displayName => "Seeking Stone";
        public override ItemTier itemTier => ItemTier.Tier2;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        [AutoConfigUpdateActions(AutoConfigUpdateActionTypes.InvalidateLanguage)]
        [AutoConfig("Amount each stack increases projectile speed by.", AutoConfigFlags.PreventNetMismatch, 0f, 1f)]
        public float projectileSpeed { get; private set; } = 0.2f;


        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "Reduces recoil, increases projectile speed, and increases accuracy.";
        protected override string GetDescString(string langid = null) => "Reduces <style=cIsUtility>recoil</style> reciprocally <style=cStack>(recoil intensity ÷ stack amount)</style>, increases <style=cIsUtility>projectile speed by " + (int)(projectileSpeed * 100) + "%</style> <style=cStack>(+" + (int)(projectileSpeed * 100) + "% per stack)</style>, and <style=cIsUtility>aim correction</style>. Aim correction field is in the shape of a parabola <style=cStack>(15x^2 ÷ stack amount)</style>.";
        protected override string GetLoreString(string langid = null) => "Add the fuckinnn lore here also the credits n stuff";

        public SeekingStone()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/SeekingStone.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/icon.png";
        }


        public override void Install()
        {
            base.Install();

            On.EntityStates.BaseState.AddRecoil += ReduceRecoil;
            On.RoR2.CharacterBody.AddSpreadBloom += ReduceSpread;
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile += IncreaseVelocity;
            On.RoR2.CharacterBody.OnInventoryChanged += GetItemCount;
        }

        public override void Uninstall()
        {
            base.Uninstall();

            On.EntityStates.BaseState.AddRecoil -= ReduceRecoil;
            On.RoR2.CharacterBody.AddSpreadBloom -= ReduceSpread;
            On.RoR2.Projectile.ProjectileManager.InitializeProjectile -= IncreaseVelocity;
            On.RoR2.CharacterBody.OnInventoryChanged -= GetItemCount;
        }

        private void GetItemCount(On.RoR2.CharacterBody.orig_OnInventoryChanged orig, CharacterBody self) //Checks item count and caches it. Also where the component is added
        {
            orig(self);
            if (GetCount(self) > 0 || self.gameObject.GetComponent<SeekingStoneComponent>())
            {
                SeekingStoneComponent cpt = self.gameObject.GetComponent<SeekingStoneComponent>();
                if (!cpt) cpt = self.gameObject.AddComponent<SeekingStoneComponent>();
                cpt.cachedIcnt = GetCount(self);
            }
        }

        private void ReduceRecoil(On.EntityStates.BaseState.orig_AddRecoil orig, EntityStates.BaseState self, float verticalMin, float verticalMax, float horizontalMin, float horizontalMax)
        {
            SeekingStoneComponent cpt = self.characterBody.gameObject.GetComponent<SeekingStoneComponent>();
            if (cpt && cpt.cachedIcnt != 0)
            {
                verticalMin /= cpt.cachedIcnt + 1;
                verticalMax /= cpt.cachedIcnt + 1;
                horizontalMin /= cpt.cachedIcnt + 1;
                horizontalMax /= cpt.cachedIcnt + 1;
            }
            orig(self, verticalMin, verticalMax, horizontalMin, horizontalMax);
        }

        private void ReduceSpread(On.RoR2.CharacterBody.orig_AddSpreadBloom orig, CharacterBody self, float value)
        {
            SeekingStoneComponent cpt = self.gameObject.GetComponent<SeekingStoneComponent>();
            if (cpt && cpt.cachedIcnt != 0)
            {
                value /= cpt.cachedIcnt;
            }
            orig(self, value);
        }
        private void IncreaseVelocity(On.RoR2.Projectile.ProjectileManager.orig_InitializeProjectile orig, RoR2.Projectile.ProjectileController projectileController, FireProjectileInfo fireProjectileInfo)
        {
            orig(projectileController, fireProjectileInfo);
            GameObject gameObject = projectileController.gameObject;
            ProjectileSimple component5 = gameObject.GetComponent<ProjectileSimple>();
            projectileController.Networkowner = fireProjectileInfo.owner;
            SeekingStoneComponent cpt = fireProjectileInfo.owner.GetComponent<SeekingStoneComponent>();
            if (component5 && cpt && cpt.cachedIcnt > 0)
                component5.velocity *= 1f + cpt.cachedIcnt * projectileSpeed;
        }

    }
    public class SeekingStoneComponent : MonoBehaviour
    {
        public int cachedIcnt;
        public CharacterBody body;
        VirtBullseyeSearch bullseyeSearch = new VirtBullseyeSearch();

        public void Awake()
        {
            body = gameObject.GetComponent<CharacterBody>();
        }
        public void Update()
        {
            AimBotRoutine();
        }

        private void AimBotRoutine()
        {
            if (cachedIcnt > 0)
            {
                bullseyeSearch.enabled = true;
                AimBot();
            }
            else
                bullseyeSearch.enabled = false;
        }

        public void AimBot()
        {
            InputBankTest component = body.GetComponent<InputBankTest>();
            Ray ray = new Ray(component.aimOrigin, component.aimDirection);
            TeamComponent component2 = body.GetComponent<TeamComponent>();
            bullseyeSearch.teamMaskFilter = TeamMask.all;
            bullseyeSearch.teamMaskFilter.RemoveTeam(component2.teamIndex);
            bullseyeSearch.filterByLoS = true;
            //bullseyeSearch.filterByDistinctEntity = true;
            bullseyeSearch.searchOrigin = ray.origin;
            bullseyeSearch.searchDirection = ray.direction;
            bullseyeSearch.sortMode = BullseyeSearch.SortMode.Angle;
            bullseyeSearch.maxDistanceFilter = cachedIcnt;
            bullseyeSearch.maxAngleFilter = 60;
            bullseyeSearch.itemMult = (float)cachedIcnt;
            bullseyeSearch.RefreshCandidates();
            HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
            bool flag = hurtBox;
            if (flag)
            {
                Vector3 aimDirection = hurtBox.transform.position - ray.origin;
                component.aimDirection = aimDirection;
            }
        }

    }

    public class VirtBullseyeSearch : BullseyeSearch // God I love Rein
    {
        public VirtBullseyeSearch()
        {
            IL.RoR2.BullseyeSearch.RefreshCandidates += il =>
            {
                ILCursor c = new ILCursor(il);
                ILLabel label = c.DefineLabel();
                c.Emit(OpCodes.Ldarg_0);
                c.Emit(OpCodes.Isinst, typeof(VirtBullseyeSearch));
                c.Emit(OpCodes.Dup);
                c.Emit(OpCodes.Brfalse, label);
                c.Emit(OpCodes.Callvirt, typeof(VirtBullseyeSearch).GetMethod("RefreshCandidates"));
                c.Emit(OpCodes.Ret);
                c.MarkLabel(label);
                c.Emit(OpCodes.Pop);
            };
        }


        public float itemMult;
        public bool enabled = true;


        public virtual void RefreshCandidates()
        {
            Func<HurtBox, BullseyeSearch.CandidateInfo> selector = GetSelector();
            candidatesEnumerable = (from hurtBox in HurtBox.readOnlyBullseyesList
                                    where teamMaskFilter.HasTeam(hurtBox.teamIndex)
                                    select hurtBox).Select(selector);
            if (this.filterByAngle)
            {
                this.candidatesEnumerable = this.candidatesEnumerable.Where(new Func<BullseyeSearch.CandidateInfo, bool>(this.FilterAngle));
            }
            candidatesEnumerable = candidatesEnumerable.Where(PointLineDistance);
            if (filterByDistinctEntity)
            {
                candidatesEnumerable = candidatesEnumerable.Distinct(default(BullseyeSearch.CandidateInfo.EntityEqualityComparer));
            }
            Func<BullseyeSearch.CandidateInfo, float> sorter = GetSorter();
            if (sorter != null)
            {
                candidatesEnumerable = candidatesEnumerable.OrderBy(sorter);
            }
        }

        private bool FilterAngle(BullseyeSearch.CandidateInfo candidateInfo)
        {
            return this.minThetaDot <= candidateInfo.dot && candidateInfo.dot <= this.maxThetaDot;
        }

        private bool PointLineDistance(CandidateInfo candidateInfo)
        {
            Vector3 point = candidateInfo.position;
            var (direction, origin) = (searchDirection, searchOrigin);
            var distanceAlongDirectionToClosestPoint = Vector3.Dot(point - origin, direction);
            var closestPointOnLine = origin + distanceAlongDirectionToClosestPoint * (direction);
            var distance = Vector3.Distance(point, closestPointOnLine);
            if ((15f / itemMult) * Mathf.Pow(distance, 2f) <= Vector3.Distance(closestPointOnLine, origin) && enabled)
                return true;
            return false;

        }
    }
}