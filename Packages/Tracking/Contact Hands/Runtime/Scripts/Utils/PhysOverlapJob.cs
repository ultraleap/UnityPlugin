using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    /// <summary>
    /// Used for singular raycast based returns, iterates over each repetition then hands.
    /// </summary>
#if BURST_AVAILABLE
    [BurstCompile]
#endif
    internal struct PhysCastJob : IJobFor
    {
        /// <summary>
        /// Tracked status of the hand
        /// </summary>
        [ReadOnly]
        public NativeArray<bool> tracked;

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
        public NativeArray<SpherecastCommand> sphereCommands;

        [NativeDisableParallelForRestriction]
        public NativeArray<BoxcastCommand> boxCommands;

        public int palmJobs, jointJobs, repetitions, hands, totalPalmJobs, totalJointJobs;

        public int layerMask;

        public PhysCastJob(int palmJobs, int jointJobs, int repetitions, int hands, LayerMask layerMask)
        {
            tracked = new NativeArray<bool>(hands, Allocator.Persistent);

            origins = new NativeArray<Vector3>((palmJobs + jointJobs) * hands, Allocator.Persistent);
            directions = new NativeArray<Vector3>((palmJobs + jointJobs) * hands, Allocator.Persistent);
            distances = new NativeArray<float>((palmJobs + jointJobs) * hands, Allocator.Persistent);

            radii = new NativeArray<float>(jointJobs * repetitions * hands, Allocator.Persistent);
            sphereCommands = new NativeArray<SpherecastCommand>(jointJobs * repetitions * hands, Allocator.Persistent);

            orientations = new NativeArray<Quaternion>(hands, Allocator.Persistent);

            halfExtents = new NativeArray<Vector3>(palmJobs * repetitions * hands, Allocator.Persistent);
            boxCommands = new NativeArray<BoxcastCommand>(palmJobs * repetitions * hands, Allocator.Persistent);

            this.palmJobs = palmJobs;
            this.jointJobs = jointJobs;
            this.totalPalmJobs = palmJobs * repetitions * hands;
            this.totalJointJobs = jointJobs * repetitions * hands;
            this.repetitions = repetitions;
            this.hands = hands;
            this.layerMask = layerMask.value;
        }

        public void Execute(int index)
        {
            // Get the current repetition
            int localRep = index / (palmJobs + jointJobs) % repetitions;

            // Get the current hand
            int localHand = index / ((palmJobs + jointJobs) * repetitions) % hands;

            if (!tracked[localHand])
            {
                return;
            }

            // Get the current index within the cycle
            int localIndex = index % (palmJobs + jointJobs);
            // Get what the current cycle is
            int currentCycle = (localRep * repetitions) + localHand;

            Vector3 origin, direction;
            float distance;
            if (localIndex < palmJobs)
            {
                // Get the palm index relative to the cycle
                int localPalmIndex = Mathf.Clamp(localIndex, 0, palmJobs - 1);
                // Get the global palm index over all cycles
                int globalPalmIndex = (currentCycle * palmJobs) + localPalmIndex;

                origin = this.origins[localPalmIndex];
                direction = this.directions[localPalmIndex];
                distance = this.distances[localPalmIndex];
                Quaternion orientation = this.orientations[localHand];
                Vector3 halfExtents = this.halfExtents[globalPalmIndex];

#if UNITY_2022_3_OR_NEWER
                this.commands[index] = new BoxcastCommand(
                    origin, halfExtents, orientation, direction, new QueryParameters(layerMask: layerMask, hitTriggers: QueryTriggerInteraction.Ignore), distance: distance);
#else
                this.boxCommands[globalPalmIndex] = new BoxcastCommand(
                    origin, halfExtents, orientation, direction, distance: distance, layerMask: layerMask);
#endif
            }
            else
            {
                // Get the joint index relative to the cycle
                int localJointIndex = Mathf.Clamp(localIndex - palmJobs, 0, jointJobs - 1);
                // Get the global palm index over all cycles
                int globalJointIndex = (currentCycle * jointJobs) + localJointIndex;

                origin = this.origins[localJointIndex];
                direction = this.directions[localJointIndex];
                distance = this.distances[localJointIndex];
                float radius = this.radii[globalJointIndex];

#if UNITY_2022_3_OR_NEWER
                this.sphereCommands[index] = new SpherecastCommand(
                    origin, radius, direction, new QueryParameters(layerMask: layerMask, hitTriggers: QueryTriggerInteraction.Ignore), distance: distance);
#else
                this.sphereCommands[globalJointIndex] = new SpherecastCommand(
                    origin, radius, direction, distance: distance, layerMask: layerMask);
#endif
            }
        }

        internal void Dispose()
        {
            tracked.Dispose();
            origins.Dispose();
            directions.Dispose();
            radii.Dispose();
            distances.Dispose();
            orientations.Dispose();
            halfExtents.Dispose();
            sphereCommands.Dispose();
            boxCommands.Dispose();
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

#if UNITY_2022_3_OR_NEWER
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

#endif
}