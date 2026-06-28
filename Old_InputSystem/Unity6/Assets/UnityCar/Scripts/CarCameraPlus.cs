/*
 * UnityCar: CarCameraPlus.cs
 * Version: Unity 6+ (Old Input)
 * Edits By: DeathwatchGaming
 * License: MIT
 */

// Old Input System version
// This script provides a dual-mode camera (Follow + Orbit) with robust anti-clipping, mode transition safety, 
// and full support for both cursor locked and unlocked states.

using UnityEngine; // Unity core library for MonoBehaviour, Transform, Vector3, Quaternion, Physics, etc.

namespace UnityCar.Scripts // Namespace for organization of UnityCar scripts
{
    public class CarCameraPlus : MonoBehaviour // Main camera controller class inheriting from MonoBehaviour
    {
        [Header("References")]
        [Tooltip("The car rigidbody.")]
        // Reference to the car's Rigidbody component. Used to read velocity for Follow mode rotation.
        [SerializeField] private Rigidbody carRigidbody;
        
        [Tooltip("The car transform.")]
        // Reference to the car's Transform. Used as the target for camera positioning and rotation.
        [SerializeField] private Transform carTransform;
        
        [Tooltip("The car camera root transform.")]
        // The camera root transform is used to determine the camera's position and rotation. This is detached from the car for smooth movement.
        [SerializeField] private Transform cameraRoot;
        
        [Tooltip("The car camera transform.")]
        // Reference to the actual Camera component's Transform. Used for final positioning and LookAt.
        [SerializeField] private Transform cameraTransform;

        [Header("Settings")]
        [Tooltip("If car speed is below this value (in m/s), then the camera will default to looking forward.")]
        // The rotation threshold is used to determine when the camera should default to looking forward. 
        // Prevents camera from freaking out with a near-zero velocity getting put into a Quaternion.LookRotation.
        [SerializeField] private float rotationThreshold = 1.0f;
        
        [Tooltip("How closely the camera matches the car's velocity vector.")]
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

        [Header("Mode Settings")]
        [Tooltip("The key to switch modes.")]
        // KeyCode to switch modes
        [SerializeField] private KeyCode toggleModeKey = KeyCode.C;

        [Header("Orbit Mode Settings")]
        [Tooltip("Auto rotation speed (degrees per second) when mouse is idle in Orbit Mode.")]
        // Auto rotation speed (degrees per second) when mouse is idle in Orbit Mode.
        [SerializeField] private float autoRotateSpeed = 15.0f;
        
        [Tooltip("Mouse sensitivity for manual rotation in Orbit Mode.")]
        // Mouse sensitivity for manual rotation in Orbit Mode.
        [SerializeField] private float mouseSensitivity = 150.0f;
        
        [Tooltip("Minimum time (seconds) of mouse inactivity before auto-rotation starts in Orbit Mode.")]
        // Minimum time (seconds) of mouse inactivity before auto-rotation starts in Orbit Mode.
        [SerializeField] private float mouseIdleThreshold = 1.0f;
        
        [Tooltip("Invert mouse X axis.")]
        // Option to invert horizontal mouse look.
        [SerializeField] private bool invertMouseX = false;
        
        [Tooltip("Invert mouse Y axis.")]
        // Option to invert vertical mouse look.
        [SerializeField] private bool invertMouseY = true;
        
        [Tooltip("Minimum orbit distance (prevents getting too close and triggering look-up bug).")]
        // Minimum orbit distance (prevents getting too close and triggering look-up bug).
        [SerializeField] private float minOrbitDistance = 5.0f;
        
        [Tooltip("Maximum orbit distance.")]
        // Maximum orbit distance.
        [SerializeField] private float maxOrbitDistance = 20.0f;

        [Header("Anti-Clipping Settings")]
        [Tooltip("Layer mask for ground and obstacles to prevent camera clipping.")]
        // Layer mask for ground and obstacles to prevent camera clipping.
        [SerializeField] private LayerMask obstacleLayer = ~0; // Default to everything
        [Tooltip("Minimum camera height above ground/obstacles.")]
        // Minimum camera height above ground/obstacles.
        [SerializeField] private float minHeightAboveGround = 0.5f;
        [Tooltip("How much to smooth height adjustments when avoiding clipping.")]
        // How much to smooth height adjustments when avoiding clipping.
        [SerializeField] private float clipDamping = 8.0f;
        [Tooltip("Radius of the camera collision sphere for more robust obstacle avoidance.")]
        // Radius of the camera collision sphere for more robust obstacle avoidance.
        [SerializeField] private float collisionRadius = 0.3f;
        [Tooltip("How aggressively to correct height violations (higher = faster snap back).")]
        // How aggressively to correct height violations (higher = faster snap back).
        [SerializeField] private float heightCorrectionSpeed = 20.0f;

        [Header("Internal Constants & Buffers")]
        [Tooltip("Extra height buffer used when forcing safe position on mode switch.")]
        [SerializeField] private float safeHeightBuffer = 1.0f;
        [Tooltip("Extra height buffer used on initialization.")]
        [SerializeField] private float initHeightBuffer = 2.0f;
        [Tooltip("Raycast start height above car.")]
        [SerializeField] private float raycastStartHeight = 1.0f;
        [Tooltip("Extra distance added to collision checks.")]
        [SerializeField] private float collisionExtraDistance = 2.0f;
        [Tooltip("Look-at target height offset in Orbit mode.")]
        [SerializeField] private float orbitLookAtHeight = 1.5f;
        [Tooltip("Mouse delta threshold for detecting movement.")]
        [SerializeField] private float mouseMoveThreshold = 0.02f;
        [Tooltip("Pitch limits for Orbit mode.")]
        [SerializeField] private float minPitch = 5f;
        [SerializeField] private float maxPitch = 60f;
        [Tooltip("Safe fallback direction multiplier when very close to car.")]
        [SerializeField] private float fallbackDirectionMultiplier = 5f;
        [Tooltip("Upward component for fallback direction.")]
        [SerializeField] private float fallbackUpComponent = 2f;
        [Tooltip("LookAt strength multiplier when forcing orientation.")]
        [SerializeField] private float forceLookAtStrength = 8f;

        // Private variables

        private Quaternion lookRotation; // The desired rotation of the camera based on the car's velocity vector.
        private Vector3 desiredPosition; // The desired position of the camera based on the car's position and the camera's distance and height.
        private Vector3 carVelocity; // The car's velocity vector, used to determine the camera's rotation.
        private float desiredHeight; // The desired height of the camera based on the car's position and the camera's height.
        private float currentHeight; // The current height of the camera, used to smooth the camera's height.
        private float smoothedHeight; // The smoothed height of the camera, used to smooth the camera's height.

        // Mode management
        private bool isOrbitMode = false; // Current camera mode (false = Follow, true = Orbit)
        private float currentYaw = 0f; // Current yaw angle for Orbit mode
        private float currentPitch = 20f; // Current pitch angle for Orbit mode (slight downward tilt by default)
        private float lastMouseMoveTime = 0f; // Time of last mouse movement for auto-rotate timeout
        private Vector3 lastMousePosition; // Used for mouse delta calculation when cursor is not locked
        private bool justSwitchedMode = false; // Flag for stronger correction after mode switch
        private bool forceOrbitLookAt = false; // Extra enforcement after mode switch
        private float lookAtForceFrames = 0f; // Frames to force look-at after exiting Orbit

        private void Awake()
        {
            // Get the car's rigidbody and transform components
            carRigidbody = carTransform.GetComponent<Rigidbody>();
            carTransform = cameraRoot.parent.GetComponent<Transform>();
            cameraRoot = GetComponent<Transform>();
            cameraTransform = Camera.main.GetComponent<Transform>();
        }

        private void Start()
        {
            // Detach the camera so that it can move freely on its own
            cameraRoot.parent = null;

            lastMousePosition = Input.mousePosition;
   
            // Initialize camera safely above ground
            InitializeSafePosition();
        }

        private void Update()
        {
            // Toggle camera mode
            if (Input.GetKeyDown(toggleModeKey))
            {
                bool wasOrbit = isOrbitMode;
                isOrbitMode = !isOrbitMode;
                
                if (isOrbitMode)
                {
                    // Robust entry into Orbit mode - calculate proper direction to car
                    Vector3 toCar = carTransform.position + Vector3.up * orbitLookAtHeight - cameraTransform.position;
                    float dist = toCar.magnitude;

                    if (dist < minOrbitDistance)
                    {
                        toCar = cameraRoot.forward * fallbackDirectionMultiplier + Vector3.up * fallbackUpComponent;
                    }

                    Quaternion targetRot = Quaternion.LookRotation(toCar.normalized);

                    currentYaw = targetRot.eulerAngles.y;
                    currentPitch = targetRot.eulerAngles.x;

                    if (currentPitch > 180f) 
                    {
                        currentPitch -= 360f;
                    }

                    cameraRoot.rotation = Quaternion.Euler(currentPitch, currentYaw, 0f);

                    lastMouseMoveTime = Time.time;
                    forceOrbitLookAt = true;
                }

                else if (wasOrbit)
                {
                    ForceSafeHeight();
                    lookAtForceFrames = 1.0f;
                }
                
                justSwitchedMode = true;
            }

            // Handle mouse input for orbit mode
            if (isOrbitMode)
            {
                Vector2 mouseInput = GetMouseInput();

                if (Mathf.Abs(mouseInput.x) > mouseMoveThreshold || Mathf.Abs(mouseInput.y) > mouseMoveThreshold)
                {
                    lastMouseMoveTime = Time.time;
                    
                    currentYaw += mouseInput.x * mouseSensitivity * Time.deltaTime;
                    currentPitch -= mouseInput.y * mouseSensitivity * Time.deltaTime;
                    currentPitch = Mathf.Clamp(currentPitch, minPitch, maxPitch);
                }
            }
        }

        // Old Input System mouse input
        private Vector2 GetMouseInput()
        {
            float mouseDeltaX = 0f;
            float mouseDeltaY = 0f;

            if (Cursor.lockState == CursorLockMode.Locked)
            {
                mouseDeltaX = Input.GetAxis("Mouse X");
                mouseDeltaY = Input.GetAxis("Mouse Y");
            }
            else
            {
                Vector3 currentMousePos = Input.mousePosition;
                mouseDeltaX = currentMousePos.x - lastMousePosition.x;
                mouseDeltaY = currentMousePos.y - lastMousePosition.y;
                lastMousePosition = currentMousePos;
            }

            if (invertMouseX) 
            {
                mouseDeltaX = -mouseDeltaX;
            }

            if (invertMouseY) 
            {
                mouseDeltaY = -mouseDeltaY;
            }

            return new Vector2(mouseDeltaX, mouseDeltaY);
        }

        private void FixedUpdate()
        {
            // Always keep camera root roughly following car position (shared between modes)
            cameraRoot.position = Vector3.Lerp(cameraRoot.position, carTransform.position, distance * Time.fixedDeltaTime);

            if (!isOrbitMode)
            {
                // Original Follow Mode logic
                // Calculate the desired rotation based on the car's velocity
                carVelocity = carRigidbody.linearVelocity;

                // If the car isn't moving (or moving very slowly), default to looking forwards
                // Prevents camera from freaking out with a near-zero velocity getting put into a Quaternion.LookRotation
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
            }

            else
            {
                // Orbit Mode logic
                // Auto-rotate if mouse has been idle
                if (Time.time - lastMouseMoveTime > mouseIdleThreshold)
                {
                    currentYaw += autoRotateSpeed * Time.fixedDeltaTime;
                }

                // Apply rotation to camera root (yaw + pitch)
                Quaternion targetRotation = Quaternion.Euler(currentPitch, currentYaw, 0f);
                cameraRoot.rotation = Quaternion.Slerp(cameraRoot.rotation, targetRotation, rotationSpeed * Time.fixedDeltaTime);

                // Calculate desired height and position for orbit with distance clamping
                desiredHeight = carTransform.position.y + height;
                float clampedDistance = Mathf.Clamp(distance, minOrbitDistance, maxOrbitDistance);
                desiredPosition = carTransform.position - cameraRoot.forward * clampedDistance;
                desiredPosition.y = desiredHeight;
            }

            // Critical height safety
            EnforceMinimumHeight();

            // Anti-clipping with full collision detection
            AdjustForClipping();

            // Smoothly move the camera
            cameraTransform.position = Vector3.Lerp(cameraTransform.position, desiredPosition, rotationDamping * Time.fixedDeltaTime);
        }

        private void LateUpdate()
        {
            // Final hard clamp every frame to prevent underground camera
            float minSafeY = carTransform.position.y + minHeightAboveGround;

            if (cameraTransform.position.y < minSafeY)
            {
                Vector3 pos = cameraTransform.position;
                pos.y = minSafeY;
                cameraTransform.position = pos;
            }

            if (justSwitchedMode)
            {
                justSwitchedMode = false;
                ForceSafeHeight();
            }

            // Force look-at for several frames after exiting Orbit
            if (lookAtForceFrames > 0)
            {
                ForceLookAtCar();
                lookAtForceFrames -= Time.deltaTime;
            }

            // Robust Orbit LookAt enforcement
            if (isOrbitMode)
            {
                Vector3 lookTarget = carTransform.position + Vector3.up * orbitLookAtHeight;
                Vector3 direction = lookTarget - cameraTransform.position;

                if (direction.magnitude < minOrbitDistance)
                {
                    direction = cameraRoot.forward * fallbackDirectionMultiplier + Vector3.up * fallbackUpComponent;
                }

                if (forceOrbitLookAt)
                {
                    cameraTransform.rotation = Quaternion.LookRotation(direction);
                    forceOrbitLookAt = false;
                }

                else
                {
                    Quaternion targetLook = Quaternion.LookRotation(direction);
                    cameraTransform.rotation = Quaternion.Slerp(cameraTransform.rotation, targetLook, rotationSpeed * forceLookAtStrength * Time.deltaTime);
                }
            }
        }

        private void EnforceMinimumHeight()
        {
            float minSafeY = carTransform.position.y + minHeightAboveGround;
            float correction = heightCorrectionSpeed * Time.fixedDeltaTime;
            
            if (desiredPosition.y < minSafeY)
            {
                desiredPosition.y = Mathf.Lerp(desiredPosition.y, minSafeY, correction * (justSwitchedMode ? 3f : 1f));
            }
            
            if (smoothedHeight < minSafeY)
            {
                smoothedHeight = Mathf.Lerp(smoothedHeight, minSafeY, correction * (justSwitchedMode ? 3f : 1f));
            }
        }

        private void ForceSafeHeight()
        {
            float safeY = Mathf.Max(cameraTransform.position.y, carTransform.position.y + height + safeHeightBuffer);
            
            desiredPosition = cameraTransform.position;
            desiredPosition.y = safeY;
            
            currentHeight = safeY;
            smoothedHeight = safeY;
            desiredHeight = safeY;
            
            cameraTransform.position = desiredPosition;
        }

        private void ForceLookAtCar()
        {
            Vector3 lookTarget = carTransform.position + Vector3.up * orbitLookAtHeight;
            cameraTransform.rotation = Quaternion.LookRotation(lookTarget - cameraTransform.position);
        }

        private void AdjustForClipping()
        {
            Vector3 startPos = carTransform.position + Vector3.up * raycastStartHeight;
            Vector3 direction = (desiredPosition - startPos).normalized;
            float maxDistance = Vector3.Distance(startPos, desiredPosition) + collisionExtraDistance;

            if (Physics.SphereCast(startPos, collisionRadius, direction, out RaycastHit hit, maxDistance, obstacleLayer))
            {
                Vector3 safePosition = hit.point - direction * (minHeightAboveGround + collisionRadius);
                desiredPosition = Vector3.Lerp(desiredPosition, safePosition, clipDamping * Time.fixedDeltaTime);
            }
            
            else
            {
                float safeY = Mathf.Max(desiredPosition.y, carTransform.position.y + minHeightAboveGround);
                desiredPosition.y = Mathf.Lerp(desiredPosition.y, safeY, clipDamping * Time.fixedDeltaTime);
            }
        }

        private void InitializeSafePosition()
        {
            Vector3 safeStart = carTransform.position + Vector3.up * (height + initHeightBuffer);
            cameraTransform.position = safeStart;
            desiredPosition = safeStart;
            currentHeight = safeStart.y;
            smoothedHeight = safeStart.y;
            desiredHeight = safeStart.y;
        }
    }
}