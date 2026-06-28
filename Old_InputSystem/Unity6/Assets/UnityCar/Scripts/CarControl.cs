/*
 * UnityCar: CarControl.cs
 * Version: Unity 6+ (Old Input)
 * Edits By: DeathwatchGaming
 * License: MIT
 */

// This script is responsible for controlling the car's movement, steering, and braking using the Unity "Legacy" Old Input System. It handles the physics of the car through WheelColliders and updates the visual representation of the wheels accordingly.

// Using directives
using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityCar.Scripts
{
	public class CarControl : MonoBehaviour
	{
		// Define a struct to hold information about each wheel
		[Serializable]
		public struct Wheel
		{
			[Tooltip("The WheelCollider component for the wheel.")]
			// The WheelCollider component for the wheel
			public WheelCollider wheelCollider;
			[Tooltip("The Transform component for the visual representation of the wheel.")]
			// The Transform component for the visual representation of the wheel
			public Transform wheelMesh;
			[Tooltip("Whether the wheel can steer (front wheels) or not (rear wheels).")]
			// Whether the wheel can steer (front wheels) or not (rear wheels)
			public bool steerable;
			[Tooltip("Whether the wheel is powered by the motor (rear wheels) or not (front wheels).")]
			// Whether the wheel is powered by the motor (rear wheels) or not (front wheels)
			public bool motorized;
		}

		// List of wheels for the car, initialized with 4 wheels
		[SerializeField] private List<Wheel> wheels = new List<Wheel>(new Wheel[4]);

		[Header("Car Properties")]
		[Tooltip("The rigidbody mass amount.")]
		// The rigidbody mass amount
		[SerializeField] private float rigidBodyMass = 1500f;
		[Tooltip("The center of gravity offset amount.")]
		// The center of gravity offset amount
		[SerializeField] private float centerOfGravityOffset = -1f;
		[Tooltip("The motor torque amount.")]
		// The motor torque amount
		[SerializeField] private float motorTorque = 2000f;
		[Tooltip("The brake torque amount.")]
		// The brake torque amount
		[SerializeField] private float brakeTorque = 2000f;
		[Tooltip("The maximum speed amount.")]
		// The maximum speed amount
		[SerializeField] private float maxSpeed = 20f;
		[Tooltip("The steering range amount.")]
		// The steering range amount
		[SerializeField] private float steeringRange = 30f;
		[Tooltip("The steering range amount at maximum speed.")]
		// The steering range amount at maximum speed
		[SerializeField] private float steeringRangeAtMaxSpeed = 10f;

		// Private variables for internal calculations
	    
		private Rigidbody rigidBody; // Reference to the car's Rigidbody component
		private Vector3 centerOfMass; // The center of mass for the car's Rigidbody
		private Vector3 wheelPosition; // The position of the wheel for visual representation
		private Quaternion wheelRotation; // The rotation of the wheel for visual representation
		
		private float motorInput; // Input value for motor torque (forward/backward)
		private float steerInput; // Input value for steering (left/right)
		private float forwardSpeed; // The current speed of the car along its forward axis
		private float speedFactor; // A normalized factor representing the car's speed relative to its maximum speed
		private float currentMotorTorque; // The current motor torque applied to the wheels based on speed
		private float currentSteerRange; // The current steering range applied to the wheels based on speed
		private bool isAccelerating; // Flag to determine if the player is accelerating or trying to reverse

		// Start is called before the first frame update
		private void Start()
		{
			// Get the rigidbody
			rigidBody = GetComponent<Rigidbody>();

			// Set the rigidbody mass
			rigidBody.mass = rigidBodyMass;

			// Adjust center of mass to improve stability and prevent rolling
			centerOfMass = rigidBody.centerOfMass; // Get the current center of mass
			centerOfMass.y += centerOfGravityOffset; // Adjust the center of mass downward to improve stability
			rigidBody.centerOfMass = centerOfMass; // Set the new center of mass for the rigidbody
		}

		// Update is called every frame
		private void Update()
		{
			// Update the wheel visuals to match the physics simulation
			UpdateWheels();
		}

		// FixedUpdate is called at a fixed time interval
		private void FixedUpdate()
		{
			// Get player input for acceleration and steering
			motorInput = Input.GetAxis("Vertical"); // Forward / backward input
			steerInput = Input.GetAxis("Horizontal"); // Steering input

			// Calculate current speed along the car's forward axis
			forwardSpeed = Vector3.Dot(transform.forward, rigidBody.linearVelocity);
			speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed)); // Normalized speed factor

			// Reduce motor torque and steering at high speeds for better handling
			currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
			currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

			// Determine if the player is accelerating or trying to reverse
			isAccelerating = Mathf.Sign(motorInput) == Mathf.Sign(forwardSpeed);

			// Apply motor torque, steering and brakes to the wheels based on player input and current speed
			foreach (var wheel in wheels)
			{
				// Apply steering to wheels that support steering
				if (wheel.steerable)
				{
					wheel.wheelCollider.steerAngle = steerInput * currentSteerRange;
				}

				// Apply motor torque and brakes to wheels that support motorization
				if (isAccelerating)
				{
					// Apply torque to motorized wheels
					if (wheel.motorized)
					{
						wheel.wheelCollider.motorTorque = motorInput * currentMotorTorque;
					}

					// Apply brakes when brake key is applied
					if (Input.GetKey(KeyCode.Space))
					{
						// Apply brakes
						wheel.wheelCollider.motorTorque = 0f;
						wheel.wheelCollider.brakeTorque = Mathf.Abs(motorInput) * brakeTorque;
					}

					else
					{
						// Release brakes when accelerating
						wheel.wheelCollider.brakeTorque = 0f;
					}										
 				}

 				else 
 				{
					// Apply brakes when reversing direction
					wheel.wheelCollider.motorTorque = 0f;
					wheel.wheelCollider.brakeTorque = Mathf.Abs(motorInput) * brakeTorque;
 				}

				// Apply braking when brake key is pressed and no movement input (neutral)
				if (Input.GetKey(KeyCode.Space) && Mathf.Approximately(motorInput, 0f))
				{
					wheel.wheelCollider.motorTorque = 0f;
					wheel.wheelCollider.brakeTorque = brakeTorque;
				}				
 			}
		}

		// Update the wheel visuals
		private void UpdateWheels()
		{
			// Update the position and rotation of each wheel mesh to match the corresponding WheelCollider
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
