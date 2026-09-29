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
        public float crouchSpeed = 3f;
        public float jumpPower = 7f;
        public float gravity = 10f;


        public float lookSpeed = 2f;
        public float lookXLimit = 80f;

        public float normalHeight = 2f;
        public float crouchHeight = 1f;

        Vector3 moveDirection = Vector3.zero;
        float rotationX = 0;

        public bool canMove = true;


        CharacterController characterController;
        bool isCrouching = false;

        void Start()
        {
            characterController = GetComponent<CharacterController>();
            if (characterController != null)
            {
                normalHeight = characterController.height;
            }
        }

        void Update()
        {
            if (Keyboard.current == null || Mouse.current == null) return;

            #region Handles Movment
            Vector3 forward = transform.TransformDirection(Vector3.forward);
            Vector3 right = transform.TransformDirection(Vector3.right);

            bool isRunning = Keyboard.current.leftShiftKey.isPressed && !isCrouching;

            // Handle Crouch
            if (Keyboard.current.cKey.wasPressedThisFrame || Keyboard.current.leftCtrlKey.wasPressedThisFrame)
            {
                isCrouching = !isCrouching;
                if (characterController != null)
                {
                    characterController.height = isCrouching ? crouchHeight : normalHeight;
                }
            }

            float speedToUse = walkSpeed;
            if (isCrouching) speedToUse = crouchSpeed;
            else if (isRunning) speedToUse = runSpeed;


            float inputVertical = 0f;
            if (Keyboard.current.wKey.isPressed || Keyboard.current.upArrowKey.isPressed) inputVertical += 1f;
            if (Keyboard.current.sKey.isPressed || Keyboard.current.downArrowKey.isPressed) inputVertical -= 1f;

            float inputHorizontal = 0f;
            if (Keyboard.current.dKey.isPressed || Keyboard.current.rightArrowKey.isPressed) inputHorizontal += 1f;
            if (Keyboard.current.aKey.isPressed || Keyboard.current.leftArrowKey.isPressed) inputHorizontal -= 1f;


            float curSpeedX = canMove ? speedToUse * inputVertical : 0;
            float curSpeedY = canMove ? speedToUse * inputHorizontal : 0;
            float movementDirectionY = moveDirection.y;
            moveDirection = (forward * curSpeedX) + (right * curSpeedY);

            #endregion

            #region Handles Jumping
            if (Keyboard.current.spaceKey.isPressed && canMove && characterController.isGrounded && !isCrouching)
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
                Vector2 mouseDelta = Mouse.current.delta.ReadValue() * 0.1f; // Adjust sensitivity scaling

                rotationX += -mouseDelta.y * lookSpeed;
                rotationX = Mathf.Clamp(rotationX, -lookXLimit, lookXLimit);

                if (playerCamera != null)
                {
                    playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0, 0);
                }

                transform.rotation *= Quaternion.Euler(0, mouseDelta.x * lookSpeed, 0);
            }

            #endregion
        }
    }
}
