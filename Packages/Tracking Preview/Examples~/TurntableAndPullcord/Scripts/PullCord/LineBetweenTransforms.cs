/******************************************************************************
 * Copyright (C) Ultraleap, Inc. 2011-2022.                                   *
 *                                                                            *
 * Use subject to the terms of the Apache License 2.0 available at            *
 * http://www.apache.org/licenses/LICENSE-2.0, or another agreement           *
 * between Ultraleap and you, your company or other organization.             *
 ******************************************************************************/

using UnityEngine;

namespace Leap.Unity.Preview
{
	[RequireComponent(typeof(LineRenderer))]
	public class LineBetweenTransforms : MonoBehaviour
	{

		[SerializeField] private Transform _pointA, _pointB;
		private LineRenderer _line;

		void Start()
		{
			_line = GetComponent<LineRenderer>();
		}

		void LateUpdate()
		{
			if (_line.gameObject.activeInHierarchy)
			{
				_line.SetPosition(0, _pointA.position);
				_line.SetPosition(_line.positionCount - 1, _pointB.position);
			}
		}
	}
}