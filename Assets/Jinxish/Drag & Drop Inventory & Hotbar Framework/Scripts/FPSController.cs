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
        void Start()
        {
            characterController = GetComponent<CharacterController>();
        }

        void Update()
        {

            #region Handles Movment
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            var keyboard = Keyboard.current;
            var mouse = Mouse.current;

            // Press Left Shift to run
            bool isRunning = keyboard != null && keyboard.leftShiftKey.isPressed;

            float verticalInput = 0f;
            float horizontalInput = 0f;

            if (keyboard != null)
            {
                if (keyboard.wKey.isPressed || keyboard.upArrowKey.isPressed) verticalInput += 1f;
                if (keyboard.sKey.isPressed || keyboard.downArrowKey.isPressed) verticalInput -= 1f;
                if (keyboard.dKey.isPressed || keyboard.rightArrowKey.isPressed) horizontalInput += 1f;
                if (keyboard.aKey.isPressed || keyboard.leftArrowKey.isPressed) horizontalInput -= 1f;
            }

            float curSpeedX = canMove ? (isRunning ? runSpeed : walkSpeed) * verticalInput : 0;
            float curSpeedY = canMove ? (isRunning ? runSpeed : walkSpeed) * horizontalInput : 0;
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            #endregion

            #region Handles Jumping
            bool isJumping = keyboard != null && keyboard.spaceKey.isPressed;
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

            if (canMove && mouse != null)
            {
                Vector2 mouseDelta = mouse.delta.ReadValue();

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

