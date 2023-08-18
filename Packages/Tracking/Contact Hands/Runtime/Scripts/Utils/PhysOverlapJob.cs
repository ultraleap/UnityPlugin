using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Leap.Unity.ContactHands
{
    /// <summary>
    /// Used for singular return instances of an Spherecast
    /// </summary>
#if BURST_AVAILABLE
    [BurstCompile]
#endif
    internal struct PhysCastJob : IJobFor
    {
        [ReadOnly]
        public NativeArray<Vector3> origins;

        [ReadOnly]
        public NativeArray<Vector3> directions;

        [ReadOnly]
        public NativeArray<float> radii;

        [ReadOnly]
        public NativeArray<float> distances;

        [ReadOnly]
        public NativeArray<Quaternion> orientations;

        [ReadOnly]
        public NativeArray<Vector3> halfExtents;

        [NativeDisableParallelForRestriction]
        public NativeArray<SpherecastCommand> sphereCommands;

        [NativeDisableParallelForRestriction]
        public NativeArray<BoxcastCommand> boxCommands;

        public int palmJobs, jointJobs, repetitions;

        public int layerMask;

        public PhysCastJob(int palmJobs, int jointJobs, int repetitions, int layerMask)
        {
            origins = new NativeArray<Vector3>(palmJobs + jointJobs, Allocator.Persistent);
            directions = new NativeArray<Vector3>(palmJobs + jointJobs, Allocator.Persistent);
            distances = new NativeArray<float>(palmJobs + jointJobs, Allocator.Persistent);

            radii = new NativeArray<float>(jointJobs * repetitions, Allocator.Persistent);
            sphereCommands = new NativeArray<SpherecastCommand>(jointJobs * repetitions, Allocator.Persistent);

            orientations = new NativeArray<Quaternion>(palmJobs, Allocator.Persistent);
            halfExtents = new NativeArray<Vector3>(palmJobs * repetitions, Allocator.Persistent);
            boxCommands = new NativeArray<BoxcastCommand>(palmJobs * repetitions, Allocator.Persistent);

            this.palmJobs = palmJobs;
            this.jointJobs = jointJobs;
            this.repetitions = repetitions;
            this.layerMask = layerMask;
        }

        public void Execute(int index)
        {
            int localIndex = index % repetitions;
            int localRepetition = index / repetitions;

            Vector3 origin, direction;
            float distance;
            if (localIndex < palmJobs)
            {
                int palmIndex = (palmJobs * repetitions) + localIndex;
                origin = this.origins[localIndex];
                direction = this.directions[localIndex];
                distance = this.distances[localIndex];
                Quaternion orientation = this.orientations[palmIndex];
                Vector3 halfExtents = this.halfExtents[palmIndex];

                this.boxCommands[palmIndex] = new BoxcastCommand(
                    origin, halfExtents, orientation, direction, distance: distance, layerMask: layerMask);
            }
            else
            {
                int jointIndex = (jointJobs * repetitions) + (localIndex - palmJobs);
                origin = this.origins[localIndex];
                direction = this.directions[localIndex];
                distance = this.distances[localIndex];
                float radius = this.radii[jointIndex];
#if UNITY_2022_3_OR_NEWER
            this.commands[index] = new SpherecastCommand(
                origin, radius, direction, new QueryParameters(layerMask: layerMask, hitTriggers: QueryTriggerInteraction.Ignore), distance: distance);
#else
                this.sphereCommands[jointIndex] = new SpherecastCommand(
                    origin, radius, direction, distance: distance, layerMask: layerMask);
#endif
            }
        }

        internal void Dispose()
        {
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