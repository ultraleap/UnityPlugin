/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2024.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Examples
{
    public class SpawnObjectAtPosition : MonoBehaviour
    {
        public Transform objectToSpawn;
        public Transform spawnPoint;

        public void SpawnObject()
        {
            Instantiate(objectToSpawn, spawnPoint.position, Quaternion.identity);
        }
    }
}