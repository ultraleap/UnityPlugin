#if UNITY_2023_3_OR_NEWER
using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using Unity.Jobs.LowLevel.Unsafe;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
#if BURST_AVAILABLE
    [BurstCompile]
#endif
    internal struct PhysSphereCastJob : IJobFor
    {
        /// <summary>
        /// Origins of all casts
        /// </summary>
        [ReadOnly]
        public NativeArray<Vector3> origins;

        /// <summary>
        /// Directions of all casts
        /// </summary>
        [ReadOnly]
        public NativeArray<Vector3> directions;

        /// <summary>
        /// Distances of all casts
        /// </summary>
        [ReadOnly]
        public NativeArray<float> distances;

        /// <summary>
        /// Radius of the spherecasts
        /// </summary>
        [ReadOnly]
        public NativeArray<float> radii;

        [NativeDisableParallelForRestriction]
        public NativeArray<SpherecastCommand> commands;

        public int cycleCount, jobsPerCycle, totalJobs;

        public int layerMask;

        /// <summary>
        /// Takes a set of origins, directions, and distances, and then applies them to a configurable amount of jobs. 
        /// Radii is always unique and will need to be assigned as such.
        /// </summary>
        /// <param name="cycleCount">How many times the jobs should repeat.</param>
        /// <param name="jobsPerCycle">How many individual and unique elements do you want to process.</param>
        public PhysSphereCastJob(int cycleCount, int jobsPerCycle, LayerMask layerMask)
        {
            this.cycleCount = cycleCount;
            this.jobsPerCycle = jobsPerCycle;
            this.totalJobs = cycleCount * jobsPerCycle;

            origins = new NativeArray<Vector3>(this.jobsPerCycle, Allocator.Persistent);
            directions = new NativeArray<Vector3>(this.jobsPerCycle, Allocator.Persistent);
            distances = new NativeArray<float>(this.jobsPerCycle, Allocator.Persistent);
            radii = new NativeArray<float>(this.totalJobs, Allocator.Persistent);
            commands = new NativeArray<SpherecastCommand>(this.totalJobs, Allocator.Persistent);

            this.layerMask = layerMask.value;
        }

        public void Execute(int index)
        {
            int baseIndex = index % jobsPerCycle;

            Vector3 origin = this.origins[baseIndex];
            Vector3 direction = this.directions[baseIndex];
            float distance = this.distances[baseIndex];
            float radius = this.radii[index];


                this.sphereCommands[index] = new SpherecastCommand(
                    origin, radius, direction, new QueryParameters(layerMask: layerMask, hitTriggers: QueryTriggerInteraction.Ignore), distance: distance);
        }

        public void Dispose()
        {
            origins.Dispose();
            directions.Dispose();
            distances.Dispose();
            radii.Dispose();
            commands.Dispose();
        }
    }

#if BURST_AVAILABLE
    [BurstCompile]
#endif
    internal struct PhysBoxCastJob : IJobFor
    {
        /// <summary>
        /// Origins of all casts
        /// </summary>
        [ReadOnly]
        public NativeArray<Vector3> origins;

        /// <summary>
        /// Directions of all casts
        /// </summary>
        [ReadOnly]
        public NativeArray<Vector3> directions;

        /// <summary>
        /// Distances of all casts
        /// </summary>
        [ReadOnly]
        public NativeArray<float> distances;

        /// <summary>
        /// Orientations of the boxcasts
        /// </summary>
        [ReadOnly]
        public NativeArray<Quaternion> orientations;

        /// <summary>
        /// Half extents of the boxcasts
        /// </summary>
        [ReadOnly]
        public NativeArray<Vector3> halfExtents;

        [NativeDisableParallelForRestriction]
        public NativeArray<BoxcastCommand> commands;

        public int cycleCount, jobsPerCycle, totalJobs;

        public int layerMask;

        /// <summary>
        /// Takes a set of origins, directions, and distances, and then applies them to a configurable amount of jobs. 
        /// Radii is always unique and will need to be assigned as such.
        /// </summary>
        /// <param name="cycleCount">How many times the jobs should repeat.</param>
        /// <param name="jobsPerCycle">How many individual and unique elements do you want to process.</param>
        public PhysBoxCastJob(int cycleCount, int jobsPerCycle, LayerMask layerMask)
        {
            this.cycleCount = cycleCount;
            this.jobsPerCycle = jobsPerCycle;
            this.totalJobs = cycleCount * jobsPerCycle;

            origins = new NativeArray<Vector3>(this.jobsPerCycle, Allocator.Persistent);
            directions = new NativeArray<Vector3>(this.jobsPerCycle, Allocator.Persistent);
            distances = new NativeArray<float>(this.jobsPerCycle, Allocator.Persistent);
            orientations = new NativeArray<Quaternion>(this.jobsPerCycle, Allocator.Persistent);
            halfExtents = new NativeArray<Vector3>(this.totalJobs, Allocator.Persistent);
            commands = new NativeArray<BoxcastCommand>(this.totalJobs, Allocator.Persistent);

            this.layerMask = layerMask.value;
        }

        public void Execute(int index)
        {
            int baseIndex = index % jobsPerCycle;

            Vector3 origin = this.origins[baseIndex];
            Vector3 direction = this.directions[baseIndex];
            float distance = this.distances[baseIndex];
            Quaternion orientation = this.orientations[baseIndex];
            Vector3 halfExtents = this.halfExtents[index];


#if UNITY_2023_3_OR_NEWER
                this.sphereCommands[index] = new SpherecastCommand(
                    origin, radius, direction, new QueryParameters(layerMask: layerMask, hitTriggers: QueryTriggerInteraction.Ignore), distance: distance);
#else
            this.commands[index] = new BoxcastCommand(
                origin, halfExtents, orientation, direction, distance: distance, layerMask: layerMask);
#endif
        }

        public void Dispose()
        {
            origins.Dispose();
            directions.Dispose();
            distances.Dispose();
            halfExtents.Dispose();
            orientations.Dispose();
            commands.Dispose();
        }
    }

    /// <summary>
    /// Helper struct to do most of the hand overlaps automatically with customisable radius amounts
    /// </summary>
    internal struct HandCastJob
    {
        public int palmJobCount, jointJobCount;
        public PhysBoxCastJob palmJob;
        public PhysSphereCastJob jointJob;
        public NativeArray<RaycastHit> palmResults, jointResults;

        public int cycles, palmJobsPerCycle, jointJobsPerCycle;
        public float[] extraRadius;

        public HandCastJob(int cycleCount, int palmJobsPerCycle, int jointJobsPerCycle, LayerMask layerMask, float[] extraRadius = null)
        {
            this.cycles = cycleCount;
            this.palmJobsPerCycle = palmJobsPerCycle;
            this.jointJobsPerCycle = jointJobsPerCycle;

            palmJob = new PhysBoxCastJob(cycleCount, palmJobsPerCycle, layerMask);
            palmResults = new NativeArray<RaycastHit>(cycleCount * palmJobsPerCycle, Allocator.Persistent);
            palmJobCount = Mathf.Max(cycleCount * palmJobsPerCycle / JobsUtility.JobWorkerCount, 1);

            jointJob = new PhysSphereCastJob(cycleCount, jointJobsPerCycle, layerMask);
            jointResults = new NativeArray<RaycastHit>(cycleCount * jointJobsPerCycle, Allocator.Persistent);
            jointJobCount = Mathf.Max(cycleCount * jointJobsPerCycle / JobsUtility.JobWorkerCount, 1);

            this.extraRadius = extraRadius;
        }

        public void UpdateHand(ContactHand hand)
        {
            Vector3 origin = Vector3.zero, halfExtents = Vector3.zero, direction = Vector3.zero;
            Quaternion orientation = Quaternion.identity;
            float distance = 0;

            UpdatePalm(hand, ref origin, ref halfExtents, ref direction, ref orientation, ref distance);
            UpdateJointSafetyCasts(hand, ref origin, tip: ref direction, radius: ref distance);
        }

        private void UpdatePalm(ContactHand hand, ref Vector3 origin, ref Vector3 halfExtents, ref Vector3 direction, ref Quaternion orientation, ref float distance)
        {
            GetPalmBoxCastParams(hand.palmBone.transform, hand.palmBone.palmCollider,
                    out origin, out halfExtents, out orientation, out direction, out distance);

            for (int i = 0; i < cycles * palmJobsPerCycle; i++)
            {
                if (i < palmJobsPerCycle)
                {
                    palmJob.origins[i] = origin;
                    palmJob.directions[i] = direction;
                    palmJob.distances[i] = distance;
                    palmJob.orientations[i] = orientation;
                }

                if (extraRadius != null && extraRadius.Length > i / palmJobsPerCycle)
                {
                    palmJob.halfExtents[i] = halfExtents + (Vector3.one * extraRadius[i / palmJobsPerCycle]);
                }
                else
                {
                    palmJob.halfExtents[i] = halfExtents;
                }
            }
        }

        private void GetPalmBoxCastParams(Transform transform, BoxCollider collider, out Vector3 origin, out Vector3 halfExtents, out Quaternion orientation, out Vector3 direction, out float distance)
        {
            distance = (Vector3.Scale(PhysExts.AbsVec3(transform.lossyScale), collider.size) * 0.25f).y;
            PhysExts.ToWorldSpaceBoxOffset(collider, -Vector3.up * distance,
                out origin, out halfExtents, out orientation);
            distance = halfExtents.y;
            halfExtents.y /= 2f;
            direction = transform.up;
        }

        private void UpdateJointSafetyCasts(ContactHand hand, ref Vector3 origin, ref Vector3 tip, ref float radius)
        {
            for (int i = 0; i < cycles * jointJobsPerCycle; i++)
            {
                PhysExts.ToWorldSpaceCapsule(hand.bones[i % jointJobsPerCycle].boneCollider, out origin, out tip, out radius);

                if (i < jointJobsPerCycle)
                {
                    jointJob.origins[i] = origin;
                    jointJob.directions[i] = (tip - origin).normalized;
                    jointJob.distances[i] = Vector3.Distance(origin, tip);
                    Debug.DrawRay(origin, jointJob.directions[i] * jointJob.distances[i], Color.red);
                }

                if(extraRadius != null && extraRadius.Length > i / jointJobsPerCycle)
                {
                    jointJob.radii[i] = radius + extraRadius[i / jointJobsPerCycle];
                }
                else
                {
                    jointJob.radii[i] = radius;
                }
            }
        }

        public void ScheduleJobs(out JobHandle handle)
        {
            JobHandle palmHandle = palmJob.ScheduleParallel(palmJob.commands.Length, 64, default);
            palmHandle = BoxcastCommand.ScheduleBatch(palmJob.commands, palmResults, palmJobCount, palmHandle);

            handle = jointJob.ScheduleParallel(jointJob.commands.Length, 64, palmHandle);
            handle = SpherecastCommand.ScheduleBatch(jointJob.commands, jointResults, jointJobCount, handle);
        }

        public void Dispose()
        {
            palmJob.Dispose();
            palmResults.Dispose();

            jointJob.Dispose();
            jointResults.Dispose();
        }
    }


#if UNITY_2021_3_OR_NEWER
    public struct PhysMultiRaycastHitEnumerator
    {
        private readonly NativeArray<RaycastHit> results;
        private readonly int startingIndex;
        private readonly int maxHits;

        private int localIndex;

        public PhysMultiRaycastHitEnumerator(ref NativeArray<RaycastHit> results, int raycastIndex, int maxHits)
        {
            this.results = results;
            this.startingIndex = raycastIndex * maxHits;
            this.maxHits = maxHits;

            this.localIndex = 0;
        }

        public bool HasNextHit(out RaycastHit hit)
        {
            if (this.localIndex >= this.maxHits)
            {
                // Reached the end
                hit = default;
                return false;
            }

            int hitIndex = this.startingIndex + this.localIndex;
            hit = this.results[hitIndex];
            if (hit.colliderInstanceID == 0)
            {
                // Documentation says that iteration should stop as soon as a collider is null
                return false;
            }

            // Move to next
            ++this.localIndex;

            return true;
        }
    }
#endif

#if BURST_AVAILABLE
    [BurstCompile]
#endif
    public struct PhysMultiSpherecastJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<Vector3> origins;

        [ReadOnly]
        public NativeArray<Vector3> directions;

        [ReadOnly]
        public NativeArray<float> radii;

        [ReadOnly]
        public NativeArray<float> distances;

        [NativeDisableParallelForRestriction]
        public NativeArray<SpherecastCommand> commands;

        public int layerMask;

        public void Execute(int index)
        {
            Vector3 origin = this.origins[index];
            Vector3 direction = this.directions[index];
            float distance = this.distances[index];
            float radius = this.radii[index];
            this.commands[index] = new SpherecastCommand(
                origin, radius, direction, new QueryParameters(layerMask: layerMask, hitMultipleFaces: true, hitTriggers: QueryTriggerInteraction.Ignore), distance: distance);
        }

        internal void Dispose()
        {
            origins.Dispose();
            directions.Dispose();
            radii.Dispose();
            distances.Dispose();
            commands.Dispose();
        }
    }

#if BURST_AVAILABLE
    [BurstCompile]
#endif
    public struct PhysMultiOverlapJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<Vector3> point0;

        [ReadOnly]
        public NativeArray<Vector3> point1;

        [ReadOnly]
        public NativeArray<float> radii;

        [NativeDisableParallelForRestriction]
        public NativeArray<OverlapCapsuleCommand> commands;

        public int layerMask;

        public void Execute(int index)
        {
            Vector3 point0 = this.point0[index];
            Vector3 point1 = this.point1[index];
            float radius = this.radii[index];
            this.commands[index] = new OverlapCapsuleCommand(
                point0, point1, radius, new QueryParameters(layerMask: layerMask, hitMultipleFaces: true, hitTriggers: QueryTriggerInteraction.Ignore));
        }

        internal void Dispose()
        {
            point0.Dispose();
            point1.Dispose();
            radii.Dispose();
            commands.Dispose();
        }
    }

    public struct PhysMultiColliderHitEnumerator
    {
        private readonly NativeArray<ColliderHit> results;
        private readonly int startingIndex;
        private readonly int maxHits;

        private int localIndex;

        public PhysMultiColliderHitEnumerator(ref NativeArray<ColliderHit> results, int raycastIndex, int maxHits)
        {
            this.results = results;
            this.startingIndex = raycastIndex * maxHits;
            this.maxHits = maxHits;

            this.localIndex = 0;
        }

        public bool HasNextHit(out ColliderHit hit)
        {
            if (this.localIndex >= this.maxHits)
            {
                // Reached the end
                hit = default;
                return false;
            }

            int hitIndex = this.startingIndex + this.localIndex;
            hit = this.results[hitIndex];
            if (hit.instanceID == 0)
            {
                // Documentation says that iteration should stop as soon as a collider is null
                return false;
            }

            // Move to next
            ++this.localIndex;

            return true;
        }
    }
}
#endif