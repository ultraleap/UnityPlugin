/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2021.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace UHI.Tracking.InteractionEngine.Examples
{

    /// <summary>
    /// This script keeps a GameObject in front of the ship, off to the side a bit. The
    /// skybox cannot provide a frame of reference for the linear motion of the ship, so
    /// the spawned object provides one instead.
    ///
    /// This script assumes the ship is moving along the world forward axis.
    /// </summary>
    [AddComponentMenu("")]
    public class LinearReferenceSpawner : MonoBehaviour
    {

        public Spaceship spaceship;
        public GameObject toSpawn;

        public float forwardSpawnMultiplier = 1F;
        public Vector3 spawnOffset = Vector3.left * 1.5F;

        private GameObject _spawnedObj;

        void Update()
        {
            bool justSpawned = false;
            if (_spawnedObj == null)
            {
                _spawnedObj = GameObject.Instantiate(toSpawn);
                justSpawned = true;
            }

            if (justSpawned
                || (_spawnedObj.transform.position - spaceship.transform.position).z < -1F)
            {
                setSpawnPosition();
                if (justSpawned)
                    _spawnedObj.transform.position += Vector3.forward * 2F;
            }
        }

        private void setSpawnPosition()
        {
            Vector3 spawnPos = spaceship.transform.position;
            spawnPos += spaceship.velocity * forwardSpawnMultiplier;
            spawnPos += spawnOffset;
            _spawnedObj.transform.position = spawnPos;
        }

        public void Respawn()
        {
            setSpawnPosition();
        }

    }

}