using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.InputSystem;

namespace InventoryFramework
{
    [RequireComponent(typeof(CharacterController))]
    public class FPSController : MonoBehaviour
    {
        public Camera playerCamera;
        public float walkSpeed = 6f;
        public float runSpeed = 12f;
        public float jumpPower = 7f;
        public float gravity = 10f;


        public float lookSpeed = 2f;
        public float lookXLimit = 45f;


        Vector3 moveDirection = Vector3.zero;
        float rotationX = 0;

        public bool canMove = true;


        CharacterController characterController;
        private InputSystem_Actions inputActions;

        private void Awake()
        {
            inputActions = new InputSystem_Actions();
        }

        private void OnEnable()
        {
            inputActions.Player.Enable();
        }

        private void OnDisable()
        {
            inputActions.Player.Disable();
        }

        void Start()
        {
            characterController = GetComponent<CharacterController>();
        }

        void Update()
        {

            #region Handles Movment
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            // Press Left Shift to run
            bool isRunning = inputActions.Player.Sprint.IsPressed();

            Vector2 moveInput = inputActions.Player.Move.ReadValue<Vector2>();
            float verticalInput = moveInput.y;
            float horizontalInput = moveInput.x;

            float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * verticalInput : 0;
            float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * horizontalInput : 0;
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            #endregion

            #region Handles Jumping
            bool isJumping = inputActions.Player.Jump.IsPressed();
            if (isJumping && canMove && characterController.isGrounded)
            {
                moveDirection.y = jumpPower;
            }
            else
            {
                moveDirection.y = movementDirectionY;
            }

            if (!characterController.isGrounded)
            {
                moveDirection.y -= gravity * Time.deltaTime;
            }

            #endregion

            #region Handles Rotation
            characterController.Move(moveDirection * Time.deltaTime);

            if (canMove)
            {
                Vector2 mouseDelta = inputActions.Player.Look.ReadValue<Vector2>();

                // Unity's old Input.GetAxis("Mouse X/Y") returns values scaled by sensitivity and framerate.
                // With new Input System delta, we might need a small multiplier. Let's multiply by 0.1f as a reasonable default for raw delta.
                rotationX += -mouseDelta.y * lookSpeed * 0.1f;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);
                playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                transform.rotation *= Quaternion.Euler(0, mouseDelta.x * lookSpeed * 0.1f, 0);
            }

            #endregion
        }
    }
}

