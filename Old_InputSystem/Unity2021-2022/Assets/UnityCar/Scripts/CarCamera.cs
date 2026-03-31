/*
 * UnityCar: CarCamera.cs
 * Version: Unity 21-22+
 * Edits By: DeathwatchGaming
 * License: MIT
 */
 
using UnityEngine;

namespace UnityCar.Scripts
{
    public class CarCamera : MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The car rigidbody.")]
        [SerializeField] private Rigidbody carRigidbody;
        [Tooltip("The car transform.")]
        [SerializeField] private Transform carTransform;
        [Tooltip("The car camera root transform.")]
        [SerializeField] private Transform cameraRoot;
        [Tooltip("The car camera transform.")]
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [Tooltip("If car speed is below this value, then the camera will default to looking forward.")]
        [SerializeField] private float rotationThreshold = 0.1f;
        [Tooltip("How closely the camera matches the car's velocity vector. The lower the value, the smoother the camera rotations, but too much results in not being able to see where you're going.")]
        [SerializeField] private float rotationSpeed = 5.0f;
        [Tooltip("The camera distance determines how closely the camera follows the car's position.")]
        [SerializeField] private float distance = 10.0f;
        [Tooltip("The camera height determines the height of the camera.")]
        [SerializeField] private float height = 2.0f;
        [Tooltip("The rotation damping.")]
        [SerializeField] private float rotationDamping = 3.0f;
        [Tooltip("The height damping.")]
        [SerializeField] private float heightDamping = 2.0f;

        private Quaternion lookRotation;
        private Vector3 desiredPosition;
        private Vector3 carVelocity;
        private float desiredHeight;
        private float currentHeight;
        private float smoothedHeight;

        // Awake is called when a script instance is being loaded
        private void Awake()
        {
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
            carVelocity = carRigidbody.velocity;

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
