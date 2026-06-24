/*
 * UnityCar: CarCamera.cs
 * Version: Unity 6+
 * Edits By: DeathwatchGaming
 * License: MIT
 */

// This script is responsible for controlling the camera that follows the car in the UnityCar project. It ensures that the camera smoothly follows the car's position and rotation based on its velocity, providing a dynamic and immersive driving experience.

// Using directives
using UnityEngine;

namespace UnityCar.Scripts
{
    public class CarCamera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The car rigidbody.")]
        // The Rigidbody component of the car, used to access its velocity for camera rotation calculations.
        [SerializeField] private Rigidbody carRigidbody;
        [Tooltip("The car transform.")]
        // The Transform component of the car, used to determine its position and forward direction for camera positioning and rotation.
        [SerializeField] private Transform carTransform;
        [Tooltip("The car camera root transform.")]
        // The root Transform of the camera, which is detached from the car to allow independent movement and rotation.
        [SerializeField] private Transform cameraRoot;
        [Tooltip("The car camera transform.")]
        // The Transform component of the camera itself, used to set its position and rotation based on the car's movement.
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [Tooltip("If car speed is below this value, then the camera will default to looking forward.")]
        // The threshold below which the camera will default to looking forward instead of following the car's velocity vector, preventing erratic camera behavior at low speeds.
        [SerializeField] private float rotationThreshold = 0.1f;
        [Tooltip("How closely the camera matches the car's velocity vector. The lower the value, the smoother the camera rotations, but too much results in not being able to see where you're going.")]
        // The speed at which the camera rotates to match the car's velocity vector, allowing for smooth transitions in camera orientation based on the car's movement.
        [SerializeField] private float rotationSpeed = 5.0f;
        [Tooltip("The camera distance determines how closely the camera follows the car's position.")]
        // The distance at which the camera follows the car, allowing for a customizable view of the car's surroundings while driving.
        [SerializeField] private float distance = 10.0f;
        [Tooltip("The camera height determines the height of the camera.")]
        // The height at which the camera follows the car, allowing for a customizable vertical view of the car's surroundings while driving.
        [SerializeField] private float height = 2.0f;
        [Tooltip("The rotation damping.")]
        // The damping factor for the camera's rotation, controlling how quickly the camera aligns with the car's velocity vector for smooth transitions.
        [SerializeField] private float rotationDamping = 3.0f;
        [Tooltip("The height damping.")]
        // The damping factor for the camera's height, controlling how quickly the camera adjusts its vertical position to match the car's height for smooth transitions.
        [SerializeField] private float heightDamping = 2.0f;

        // Private variables for internal calculations

        private Quaternion lookRotation; // The desired rotation of the camera based on the car's velocity vector.
        private Vector3 desiredPosition; // The desired position of the camera based on the car's position, distance, and height.
        private Vector3 carVelocity; // The current velocity of the car, used to determine the camera's rotation and orientation.
        private float desiredHeight; // The desired height of the camera based on the car's position and the specified height offset.
        private float currentHeight; // The current height of the camera, used to smoothly transition to the desired height.
        private float smoothedHeight; // The smoothed height of the camera, calculated using linear interpolation to create a smooth transition between the current and desired heights.

        // Awake is called when a script instance is being loaded
        private void Awake()
        {
            // Get references to the car's Rigidbody and Transform components, as well as the camera's Transform component, to enable camera control based on the car's movement and position.
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
