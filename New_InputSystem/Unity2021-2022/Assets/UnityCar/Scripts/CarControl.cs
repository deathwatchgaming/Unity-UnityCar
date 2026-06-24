/*
 * UnityCar: CarControl.cs
 * Version: Unity 2021-2022 (New Input)
 * Edits By: DeathwatchGaming
 * License: MIT
 */

// This script is responsible for controlling the car's movement, steering, and braking based on player input.

// Using directives
using System;
using UnityEngine;
using System.Collections.Generic;

namespace UnityCar.Scripts
{
	public class CarControl : MonoBehaviour
	{
		// Define a struct to hold information about each wheel, including its collider, mesh, and whether it is steerable or motorized.
		[Serializable]
		public struct Wheel
		{
			[Tooltip("The WheelCollider component that handles the physics of the wheel.")]
			// The WheelCollider component that handles the physics of the wheel.
			public WheelCollider wheelCollider;
			[Tooltip("The Transform of the wheel mesh that represents the visual model of the wheel.")]
			// The Transform of the wheel mesh that represents the visual model of the wheel.
			public Transform wheelMesh;
			[Tooltip("A boolean indicating whether the wheel can steer (turn left/right).")]
			// A boolean indicating whether the wheel can steer (turn left/right).
			public bool steerable;
			[Tooltip("A boolean indicating whether the wheel is powered by the motor (drives the car).")]
			// A boolean indicating whether the wheel is powered by the motor (drives the car).
			public bool motorized;
		}

		// Create a list of wheels, initialized with a capacity for 4 wheels (front-left, front-right, rear-left, rear-right).
		[SerializeField] private List<Wheel> wheels = new List<Wheel>(new Wheel[4]);

		[Header("Car Properties")]
		[Tooltip("The rigidbody mass amount.")]
		// The rigidbody mass amount.
		[SerializeField] private float rigidBodyMass = 1500f;
		[Tooltip("The center of gravity offset amount.")]
		// The center of gravity offset amount.
		[SerializeField] private float centerOfGravityOffset = -1f;
		[Tooltip("The motor torque amount.")]
		// The motor torque amount.
		[SerializeField] private float motorTorque = 2000f;
		[Tooltip("The brake torque amount.")]
		// The brake torque amount.
		[SerializeField] private float brakeTorque = 2000f;
		[Tooltip("The maximum speed amount.")]
		// The maximum speed amount.
		[SerializeField] private float maxSpeed = 20f;
		[Tooltip("The steering range amount.")]
		// The steering range amount.
		[SerializeField] private float steeringRange = 30f;
		[Tooltip("The steering range amount at maximum speed.")]
		// The steering range amount at maximum speed.
		[SerializeField] private float steeringRangeAtMaxSpeed = 10f;

		// Private variables for internal use
	    
		private Rigidbody rigidBody; // Reference to the car's Rigidbody component
		private Vector3 centerOfMass; // The center of mass of the car, adjusted for stability
		private Vector3 wheelPosition; // The position of the wheel in world space
		private Quaternion wheelRotation; // The rotation of the wheel in world space

		private CarInputActions carControls; // Reference to the new input system
		private Vector2 inputVector; // The input vector from the player (x for steering, y for acceleration/braking)	
		
		private float motorInput; // The input value for motor torque (forward/backward)
		private float steerInput; // The input value for steering (left/right)
		private float forwardSpeed; // The current speed of the car along its forward axis
		private float speedFactor; // A normalized factor representing the car's speed relative to its maximum speed
		private float currentMotorTorque; // The current motor torque applied to the wheels, adjusted based on speed
		private float currentSteerRange; // The current steering range applied to the wheels, adjusted based on speed
		private bool isAccelerating; // A boolean indicating whether the car is currently accelerating (true) or reversing (false)
		private bool isBrakingKey; // A boolean indicating whether the brake key is currently pressed (true) or not (false)

		// Awake is called when the script instance is being loaded
		private void Awake()
		{
			carControls = new CarInputActions(); // Initialize Input Actions				
		}

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

		// Enable the input actions when the script is enabled
		private void OnEnable()
		{
			carControls.Enable();
		}

		// Disable the input actions when the script is disabled
		private void OnDisable()
		{
			carControls.Disable();
		}	

		// Update is called every frame
		private void Update()
		{
			// Check braking key value
			isBrakingKey = carControls.Car.Brake.IsPressed();

			// Update the wheel visuals to match the physics simulation
			UpdateWheels();
		}

		// FixedUpdate is called at a fixed time interval
		private void FixedUpdate()
		{
			// Read the Vector2 input from the new Input System
			inputVector = carControls.Car.Movement.ReadValue<Vector2>();

			// Get player input for acceleration and steering
			motorInput = inputVector.y; // Forward / backward input
			steerInput = inputVector.x; // Steering input

			// Calculate current speed along the car's forward axis
			forwardSpeed = Vector3.Dot(transform.forward, rigidBody.velocity);
			speedFactor = Mathf.InverseLerp(0, maxSpeed, Mathf.Abs(forwardSpeed)); // Normalized speed factor

			// Reduce motor torque and steering at high speeds for better handling
			currentMotorTorque = Mathf.Lerp(motorTorque, 0, speedFactor);
			currentSteerRange = Mathf.Lerp(steeringRange, steeringRangeAtMaxSpeed, speedFactor);

			// Determine if the player is accelerating or trying to reverse
			isAccelerating = Mathf.Sign(motorInput) == Mathf.Sign(forwardSpeed);

			// Apply motor torque, steering, and braking to each wheel based on the player's input
			foreach (var wheel in wheels)
			{
				// Apply steering to wheels that support steering
				if (wheel.steerable)
				{
					wheel.wheelCollider.steerAngle = steerInput * currentSteerRange;
				}

				// Apply motor torque and braking to wheels that are motorized
				if (isAccelerating)
				{
					// Apply torque to motorized wheels
					if (wheel.motorized)
					{
						wheel.wheelCollider.motorTorque = motorInput * currentMotorTorque;
					}

					// Apply braking if brake key is pressed
					if (isBrakingKey)
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
 			}
		}

		// Update the wheel visuals
		private void UpdateWheels()
		{
			// Update the position and rotation of each wheel mesh to match the corresponding WheelCollider's world pose
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
