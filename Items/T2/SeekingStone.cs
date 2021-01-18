using Mono.Cecil.Cil;
using MonoMod.Cil;
using RoR2;
using RoR2.Projectile;
using RoR2.WwiseUtils;
using System;
using System.Collections.ObjectModel;
using System.Linq;
using TILER2;
using UnityEngine;
using EntityStates;
using System.Collections.Generic;
using UnityEngine.Events;
using UnityEngine.Networking;
using System.Reflection;
using UnityEngine.Serialization;

namespace KevinfromHP.KevinsAdditions
{
    public class SeekingStone : VirtItem_V2<SeekingStone>
    {
        public override string displayName => "Seeking Stone";
        public override ItemTier itemTier => ItemTier.Tier3;
        public override ReadOnlyCollection<ItemTag> itemTags => new ReadOnlyCollection<ItemTag>(new[] { ItemTag.Utility });

        bool ilFailed = false;

        protected override string GetNameString(string langid = null) => displayName;
        protected override string GetPickupString(string langid = null) => "Attacks always seem to find their mark...";
        protected override string GetDescString(string langid = null) => "Reduces <style=cIsUtility>recoil</style> reciprocally <style=cStack>(recoil intensity ÷ stack amount)</style> and provides <style=cIsUtility>aim correction</style>. Aim correction field is in the shape of a parabola <style=cStack>(4x<sup>2</sup> ÷ stack amount)</style>.";
        protected override string GetLoreString(string langid = null) => "There is no lore yet.";


        public SeekingStone()
        {
            modelResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/prefabs/SeekingStone.prefab";
            iconResourcePath = "@KevinsAdditions:Assets/KevinsAdditions/textures/icons/SeekingStone_icon.png";
        }


        public override void Install()
        {
            base.Install();
            IL.RoR2.Projectile.ProjectileManager.InitializeProjectile += ProjectileAssignTarget;
            if (ilFailed)
                IL.RoR2.Projectile.ProjectileManager.InitializeProjectile -= ProjectileAssignTarget;
            else
            {
                IL.EntityStates.BaseState.AddRecoil += ReduceRecoil;
                IL.RoR2.CharacterBody.AddSpreadBloom += ReduceSpread;
                IL.RoR2.ApplyTorqueOnStart.Start += IL_ApplyTorqueOnStart;
                IL.RoR2.Projectile.ProjectileSteerTowardTarget.FixedUpdate += ImplyGravity;
            }
        }

        public override void Uninstall()
        {
            base.Uninstall();

            IL.RoR2.Projectile.ProjectileManager.InitializeProjectile -= ProjectileAssignTarget;
            IL.EntityStates.BaseState.AddRecoil -= ReduceRecoil;
            IL.RoR2.CharacterBody.AddSpreadBloom -= ReduceSpread;
            IL.RoR2.ApplyTorqueOnStart.Start -= IL_ApplyTorqueOnStart;
            IL.RoR2.Projectile.ProjectileSteerTowardTarget.FixedUpdate -= ImplyGravity;
        }

        public override void StoreItemCount(CharacterBody self) //Checks item count and caches it. Also where the component is added
        {
            SeekingStoneComponent cpt = self.gameObject.GetComponent<SeekingStoneComponent>();
            if (GetCount(self) > 0 || cpt)
            {
                if (!cpt)
                    cpt = self.gameObject.AddComponent<SeekingStoneComponent>();
                cpt.cachedIcnt = GetCount(self);
            }
        }

        private void ReduceRecoil(ILContext il)
        {
            var c = new ILCursor(il);
            ILLabel label = c.DefineLabel();

            //Moves the index where it needs to be and divides verticalMin, verticalMax, horizontalMin, and horizontalMax by the item count
            c.Index += 2;
            for (int i = 0; i < 4; i++)
            {
                c.Index++;
                c.Emit(OpCodes.Ldarg_0);
                c.EmitDelegate<Func<BaseState, float>>((baseState) =>
                {
                    if (baseState.characterBody.gameObject.GetComponent<SeekingStoneComponent>())
                        return (float)(baseState.characterBody.gameObject.GetComponent<SeekingStoneComponent>().cachedIcnt) + 1f;
                    return 1f;
                });
                c.Emit(OpCodes.Div);
            }
        }

        private void ReduceSpread(ILContext il)
        {
            var c = new ILCursor(il);

            //Divides value by 1 if there is no component, cachedIcnt + 1 is there is a componenent
            c.Index += 5;
            c.Emit(OpCodes.Ldarg_0);
            c.EmitDelegate<Func<CharacterBody, float>>((characterBody) =>
            {
                if (characterBody.gameObject.GetComponent<SeekingStoneComponent>())
                    return (float)(characterBody.gameObject.GetComponent<SeekingStoneComponent>().cachedIcnt) + 1f;
                return 1f;
            });
            c.Emit(OpCodes.Div);
        }

        private void IL_ApplyTorqueOnStart(ILContext il)
        {
            var c = new ILCursor(il);

            c.TryGotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchLdloc(1),
                x => x.MatchCallOrCallvirt<Rigidbody>("AddRelativeTorque"));
            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc_1);
            c.EmitDelegate<Func<ApplyTorqueOnStart, Vector3, Vector3>>((applyTorqueOnStart, orig) =>
            {
                var projectileController = applyTorqueOnStart.gameObject.GetComponent<ProjectileController>();
                if (projectileController)
                {
                    var cpt = projectileController.Networkowner.GetComponent<SeekingStoneComponent>();
                    if (cpt != null && cpt.cachedIcnt > 0)
                        return Vector3.zero;
                }
                return orig;
            });
            c.Emit(OpCodes.Stloc_1);
        }
        private void ImplyGravity(ILContext il)
        {
            var c = new ILCursor(il);
            bool ILFound;

            ILFound = c.TryGotoNext(
                x => x.MatchLdloc(0),
                x => x.MatchCallOrCallvirt<Vector3>("get_zero"),
                x => x.MatchCallOrCallvirt<Vector3>("op_Inequality"));
            if (!ILFound)
            {
                ilFailed = true;
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Seeking Stone IL patch (grav var read), item will not work; target instructions not found");
                return;
            }

            c.Emit(OpCodes.Ldarg_0);
            c.Emit(OpCodes.Ldloc, 0);
            c.EmitDelegate<Action<ProjectileSteerTowardTarget, Vector3>>((projectileSteerTowardTarget, vector) =>
            {
                var cpt = projectileSteerTowardTarget.gameObject.GetComponent<MaintainTarget>();
                if(cpt != null && cpt.isAffectedByGravity)
                vector += Physics.gravity;
            });
        }

        private void ProjectileAssignTarget(ILContext il)
        {
            var c = new ILCursor(il);
            bool ILFound;
            ILFound = c.TryGotoNext(
                x => x.MatchLdloc(2),
                x => x.MatchCallOrCallvirt<UnityEngine.Object>("op_Implicit"));
            if (!ILFound)
            {
                ilFailed = true;
                KevinsAdditionsPlugin._logger.LogError("Failed to apply Seeking Stone IL patch (ProjectileAssignTarget), item will not work; target instructions not found");
                return;
            }

            c.Emit(OpCodes.Ldloc_0);
            c.Emit(OpCodes.Ldarg_1);
            c.Emit(OpCodes.Ldloc, 4);
            c.Emit(OpCodes.Ldloc, 5);
            c.EmitDelegate<Action<GameObject, FireProjectileInfo, ProjectileTargetComponent, ProjectileSimple>>((gameObject, fireProjectileInfo, targetComponent, projectileSimple) =>
            {
                var cpt = fireProjectileInfo.owner.GetComponent<SeekingStoneComponent>();
                if (!cpt || !projectileSimple)
                    return;
                if (fireProjectileInfo.target == null && cpt.cachedIcnt > 0 && cpt.target)
                {
                    if (!targetComponent)
                        targetComponent = gameObject.AddComponent<ProjectileTargetComponent>();
                    if (gameObject.GetComponent<ProjectileDirectionalTargetFinder>())
                        UnityEngine.Object.Destroy(gameObject.GetComponent<ProjectileDirectionalTargetFinder>());
                    if (gameObject.GetComponent<ProjectileSphereTargetFinder>())
                        UnityEngine.Object.Destroy(gameObject.GetComponent<ProjectileSphereTargetFinder>());
                    //This sets the gameObject's forward to be the aim direction so it's easier to work with
                    /*gameObject.transform.rotation = Util.QuaternionSafeLookRotation(cpt.body.GetComponent<InputBankTest>().aimDirection);

                    gameObject.GetComponent<Rigidbody>().rotation = gameObject.transform.rotation;
                    if(gameObject.GetComponent<Rigidbody>().angularVelocity != Vector3.zero)
                    {
                        KevinsAdditionsPlugin._logger.LogError("Angular Velocity is not zero. Setting to zero");
                        gameObject.GetComponent<Rigidbody>().angularVelocity = Vector3.zero;
                    }*/
                    projectileSimple.updateAfterFiring = true;
                    MaintainTarget projectileFinderComponent = gameObject.AddComponent<MaintainTarget>();

                    var steerComponent = gameObject.GetComponent<ProjectileSteerTowardTarget>();
                    if (!steerComponent)
                        steerComponent = gameObject.AddComponent<ProjectileSteerTowardTarget>();
                    if (gameObject.GetComponent<Rigidbody>().useGravity)
                    {
                        steerComponent.yAxisOnly = true;
                        projectileFinderComponent.isAffectedByGravity = true;
                    }
                    steerComponent.rotationSpeed = 80f - 80f / (1f + 0.2f * cpt.cachedIcnt);
                    steerComponent.transform = gameObject.transform;
                }
            });
        }


    }
    public class SeekingStoneComponent : MonoBehaviour
    {
        public int cachedIcnt;
        public CharacterBody body;
        public HurtBox target;
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
            bullseyeSearch.maxAngleFilter = 60f;
            bullseyeSearch.itemMult = (float)cachedIcnt;
            bullseyeSearch.RefreshCandidates();
            HurtBox hurtBox = bullseyeSearch.GetResults().FirstOrDefault<HurtBox>();
            bool flag = hurtBox;
            if (flag)
            {
                target = hurtBox;
                Vector3 aimDirection = hurtBox.transform.position - ray.origin;
                component.aimDirection = aimDirection;
            }
        }

    }

    public class VirtBullseyeSearch : BullseyeSearch // Runs this version of bullseyesearch instead of the original
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
                this.candidatesEnumerable = this.candidatesEnumerable.Where(new Func<BullseyeSearch.CandidateInfo, bool>(this.FilterAngle));

            candidatesEnumerable = candidatesEnumerable.Where(PointLineDistance);
            if (filterByDistinctEntity)
                candidatesEnumerable = candidatesEnumerable.Distinct(default(BullseyeSearch.CandidateInfo.EntityEqualityComparer));

            Func<BullseyeSearch.CandidateInfo, float> sorter = GetSorter();
            if (sorter != null)
                candidatesEnumerable = candidatesEnumerable.OrderBy(sorter);
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
            //The distance corresponds to the length of x, 
            if (Mathf.Pow(distance, 2f) * (6f / itemMult) <= Vector3.Distance(closestPointOnLine, origin) && enabled)
                return true;
            return false;
        }
    }

    public class MaintainTarget : MonoBehaviour
    {
        private ProjectileTargetComponent targetComponent;
        public HurtBox target;
        public float itemMult;
        public float rotationSpeed;
        public bool targetLost = false;
        private new Transform transform;
        private bool hasTarget;
        private bool hadTargetLastUpdate;
        public bool allowTargetLoss;
        private HurtBox lastFoundHurtBox;
        private Transform lastFoundTransform;
        public UnityEvent onTargetLost;
        public bool isAffectedByGravity = false;

        public virtual void Start()
        {
            if (!NetworkServer.active)
            {
                base.enabled = false;
                return;
            }
            targetComponent = GetComponent<ProjectileTargetComponent>();
            itemMult = gameObject.GetComponent<ProjectileController>().Networkowner.GetComponent<SeekingStoneComponent>().cachedIcnt;
            target = gameObject.GetComponent<ProjectileController>().Networkowner.GetComponent<SeekingStoneComponent>().target;

            //base.transform = gameObject.transform;
            transform = base.transform;
            hasTarget = true;
            targetLost = false;
            allowTargetLoss = true;
            rotationSpeed = 70f - 70f / (1f + .15f * itemMult);
            lastFoundHurtBox = target;
        }

        public virtual void FixedUpdate()
        {
            if (hasTarget && !VirtPassesFilters(lastFoundHurtBox))
            {
                SetTarget(null);
                targetLost = true;
                targetComponent.target = null;
                gameObject.GetComponent<ProjectileSimple>().updateAfterFiring = false;
            }
            if (!targetLost)
                SetTarget(target);
            hasTarget = (targetComponent.target != null);
            if (hadTargetLastUpdate != hasTarget)
            {
                UnityEvent unityEvent2 = onTargetLost;
                if (unityEvent2 != null)
                    unityEvent2.Invoke();
            }
            /*if (hasTarget)
            {
                Vector3 vector = targetComponent.target.transform.position - transform.position;
                if (vector != Vector3.zero)
                    transform.forward = Vector3.RotateTowards(transform.forward, vector, rotationSpeed * 0.0174532924f * Time.fixedDeltaTime, 0f);
            }*/
            hadTargetLastUpdate = hasTarget;
        }

        public virtual bool VirtPassesFilters(HurtBox result)
        {
            if (result.healthComponent.body)
            {
                Vector3 point = result.transform.position;
                var (direction, origin) = (transform.forward, transform.position);
                var distanceAlongDirectionToClosestPoint = Vector3.Dot(point - origin, direction);
                var closestPointOnLine = origin + distanceAlongDirectionToClosestPoint * (direction);
                var distance = Vector3.Distance(point, closestPointOnLine);
                if (7f * Mathf.Pow(distance, 2f) / itemMult <= Vector3.Distance(closestPointOnLine, origin))
                    return true;
            }
            return false;
        }

        private void SetTarget(HurtBox hurtBox)
        {
            this.lastFoundHurtBox = hurtBox;
            this.lastFoundTransform = ((hurtBox != null) ? hurtBox.transform : null);
            this.targetComponent.target = this.lastFoundTransform;
        }
    }
}