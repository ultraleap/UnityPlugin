using Unity.Burst;
using Unity.Collections;
using Unity.Jobs;
using UnityEngine;

namespace Leap.Unity.Interaction.PhysicsHands
{
    /// <summary>
    /// Used for singular return instances of an Spherecast
    /// </summary>
    [BurstCompile]
    public struct PhysOverlapJob : IJobFor
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
                origin, radius, direction, distance: distance, layerMask: layerMask);
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

    public struct PhysMultiOverlapEnumerator
    {
        private readonly NativeArray<RaycastHit> results;
        private readonly int startingIndex;
        private readonly int maxHits;

        private int localIndex;

        public PhysMultiOverlapEnumerator(ref NativeArray<RaycastHit> results, int raycastIndex, int maxHits)
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

#if UNITY_2022_3_OR_NEWER
    // TODO: Convert to work with 2022+ maxhit values.
    [BurstCompile]
    public struct PhysMultiOverlapJob : IJobFor
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
                origin, radius, direction, distance: distance, layerMask: layerMask);
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

#endif


}