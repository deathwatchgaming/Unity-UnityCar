/*
 * UnityCar: CarCamera.cs
 * Version: Unity 6+
 * Edits By: DeathwatchGaming
 * License: MIT
 */

// This script is responsible for controlling the camera that follows the car. It uses the car's velocity to determine the camera's rotation and position. The camera will smoothly follow the car and rotate to match the car's velocity vector. If the car is not moving, the camera will default to looking forward. 

// Using directives
using UnityEngine;

namespace UnityCar.Scripts
{
    public class CarCamera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The car rigidbody.")]
        // The car rigidbody is used to get the car's velocity, which is used to determine the camera's rotation.
        [SerializeField] private Rigidbody carRigidbody;
        [Tooltip("The car transform.")]
        // The car transform is used to get the car's position, which is used to determine the camera's position.
        [SerializeField] private Transform carTransform;
        [Tooltip("The car camera root transform.")]
        // The camera root transform is used to determine the camera's position and rotation.
        [SerializeField] private Transform cameraRoot;
        [Tooltip("The car camera transform.")]
        // The camera transform is used to determine the camera's position and rotation.
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [Tooltip("If car speed is below this value, then the camera will default to looking forward.")]
        // The rotation threshold is used to determine when the camera should default to looking forward. If the car's speed is below this value, then the camera will default to looking forward.
        [SerializeField] private float rotationThreshold = 0.1f;
        [Tooltip("How closely the camera matches the car's velocity vector. The lower the value, the smoother the camera rotations, but too much results in not being able to see where you're going.")]
        // The rotation speed is used to determine how closely the camera matches the car's velocity vector. The lower the value, the smoother the camera rotations, but too much results in not being able to see where you're going.  
        [SerializeField] private float rotationSpeed = 5.0f;
        [Tooltip("The camera distance determines how closely the camera follows the car's position.")]
        // The camera distance is used to determine how closely the camera follows the car's position. The lower the value, the closer the camera is to the car.
        [SerializeField] private float distance = 10.0f;
        [Tooltip("The camera height determines the height of the camera.")]
        // The camera height is used to determine the height of the camera.
        [SerializeField] private float height = 2.0f;
        [Tooltip("The rotation damping.")]
        // The rotation damping is used to smooth the camera's rotation.
        [SerializeField] private float rotationDamping = 3.0f;
        [Tooltip("The height damping.")]
        // The height damping is used to smooth the camera's height.
        [SerializeField] private float heightDamping = 2.0f;

        // Private variables

        private Quaternion lookRotation; // The desired rotation of the camera based on the car's velocity vector.
        private Vector3 desiredPosition; // The desired position of the camera based on the car's position and the camera's distance and height.
        private Vector3 carVelocity; // The car's velocity vector, used to determine the camera's rotation.
        private float desiredHeight; // The desired height of the camera based on the car's position and the camera's height.
        private float currentHeight; // The current height of the camera, used to smooth the camera's height.
        private float smoothedHeight; // The smoothed height of the camera, used to smooth the camera's height.

        // Awake is called when a script instance is being loaded
        private void Awake()
        {
            // Get the car's rigidbody and transform components
            carRigidbody = carTransform.GetComponent<Rigidbody>();
            carTransform = cameraRoot.parent.GetComponent<Transform>();
            cameraRoot = GetComponent<Transform>();
            cameraTransform = Camera.main.GetComponent<Transform>();
        }

        // Start is called before the first frame update
        private void Start()
        {
            // Detach the camera so that it can move freely on its own
            cameraRoot.parent = null;
        }

        // FixedUpdate is called at a fixed time interval
        private void FixedUpdate()
        {
            // Moves the camera to match the car's position
            cameraRoot.position = Vector3.Lerp(cameraRoot.position, carTransform.position, distance * Time.fixedDeltaTime);

            // Calculate the desired rotation based on the car's velocity
            carVelocity = carRigidbody.linearVelocity;

            // If the car isn't moving, default to looking forwards
            // Prevents camera from freaking out with a zero velocity getting put into a Quaternion.LookRotation
            if (carVelocity.magnitude < rotationThreshold)
            {
                lookRotation = Quaternion.LookRotation(carTransform.forward);
            }

            else
            {
                lookRotation = Quaternion.LookRotation(carVelocity.normalized);
            }

            // Rotates the camera towards the velocity vector
            // Smoothly rotate the camera towards the desired rotation
            lookRotation = Quaternion.Slerp(cameraRoot.rotation, lookRotation, rotationSpeed * Time.fixedDeltaTime);
            cameraRoot.rotation = lookRotation;

            // Calculate the desired height and position of the camera
            desiredHeight = carTransform.position.y + height;
            currentHeight = cameraTransform.position.y;
            smoothedHeight = Mathf.Lerp(currentHeight, desiredHeight, heightDamping * Time.fixedDeltaTime);

            desiredPosition = carTransform.position - cameraRoot.forward * distance;
            desiredPosition.y = smoothedHeight;

            // Smoothly move the camera to the desired position
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, rotationDamping * Time.fixedDeltaTime);
        }
    }
}
