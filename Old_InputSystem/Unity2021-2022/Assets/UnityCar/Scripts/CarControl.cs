/*
 * UnityCar: CarControl.cs
 * Version: Unity 2021-2022 (Old Input)
 * Edits By: DeathwatchGaming
 * License: MIT
 */

using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityCar.Scripts
{
	public class CarControl : MonoBehaviour
	{
		[Serializable]
		public struct Wheel
		{
			public WheelCollider wheelCollider;
			public Transform wheelMesh;
			public bool steerable;
			public bool motorized;
		}

		[SerializeField] private List<Wheel> wheels = new List<Wheel>(new Wheel[4]);

		[Header("Car Properties")]
		[Tooltip("The rigidbody mass amount.")]
		[SerializeField] private float rigidBodyMass = 1500f;
		[Tooltip("The center of gravity offset amount.")]
		[SerializeField] private float centerOfGravityOffset = -1f;
		[Tooltip("The motor torque amount.")]
		[SerializeField] private float motorTorque = 2000f;
		[Tooltip("The brake torque amount.")]
		[SerializeField] private float brakeTorque = 2000f;
		[Tooltip("The maximum speed amount.")]
		[SerializeField] private float maxSpeed = 20f;
		[Tooltip("The steering range amount.")]
		[SerializeField] private float steeringRange = 30f;
		[Tooltip("The steering range amount at maximum speed.")]
		[SerializeField] private float steeringRangeAtMaxSpeed = 10f;
	    
		private Rigidbody rigidBody;
		private Vector3 centerOfMass;
		private Vector3 wheelPosition;
		private Quaternion wheelRotation;
		
		private float motorInput;
		private float steerInput;
		private float forwardSpeed;
		private float speedFactor;
		private float currentMotorTorque;
		private float currentSteerRange;
		private bool isAccelerating;

		// Start is called before the first frame update
		private void Start()
		{
			// Get the rigidbody
			rigidBody = GetComponent<Rigidbody>();

			// Set the rigidbody mass
			rigidBody.mass = rigidBodyMass;

			// Adjust center of mass to improve stability and prevent rolling
			centerOfMass = rigidBody.centerOfMass;
			centerOfMass.y += centerOfGravityOffset;
			rigidBody.centerOfMass = centerOfMass;
		}

		// Update is called every frame
		private void Update()
		{
			UpdateWheels();
		}

		// FixedUpdate is called at a fixed time interval
		private void FixedUpdate()
		{
			// Get player input for acceleration and steering
			motorInput = Input.GetAxis("Vertical"); // Forward / backward input
			steerInput = Input.GetAxis("Horizontal"); // Steering input

			// Calculate current speed along the car's forward axis
			forwardSpeed = Vector3.Dot(transform.forward, rigidBody.velocity);
			speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed)); // Normalized speed factor

			// Reduce motor torque and steering at high speeds for better handling
			currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
			currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

			// Determine if the player is accelerating or trying to reverse
			isAccelerating = Mathf.Sign(motorInput) == Mathf.Sign(forwardSpeed);

			foreach (var wheel in wheels)
			{
				// Apply steering to wheels that support steering
				if (wheel.steerable)
				{
					wheel.wheelCollider.steerAngle = steerInput * currentSteerRange;
				}

				if (isAccelerating)
				{
					// Apply torque to motorized wheels
					if (wheel.motorized)
					{
						wheel.wheelCollider.motorTorque = motorInput * currentMotorTorque;
					}

					// Release brakes when accelerating
					wheel.wheelCollider.brakeTorque = 0f;
 				}

 				else 
 				{
					// Apply brakes when reversing direction
					wheel.wheelCollider.motorTorque = 0f;
					wheel.wheelCollider.brakeTorque = Mathf.Abs(motorInput) * brakeTorque;
 				}
 			}
		}

		// Update the wheel visuals
		private void UpdateWheels()
		{
			foreach (var wheel in wheels)
			{
				// Get the Wheel collider's world pose values and
				// use them to set the wheel model's position and rotation
				wheel.wheelCollider.GetWorldPose(out wheelPosition, out wheelRotation);
				wheel.wheelMesh.transform.position = wheelPosition;
				wheel.wheelMesh.transform.rotation = wheelRotation;
			}
		}
	}
}
