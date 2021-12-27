using System.Collections;
using UnityEngine;

namespace XGLabs
{
    [RequireComponent(typeof(CharacterController))]
    public class FirstPersonController : MonoBehaviour
    {
        public bool CanMove { get; private set; } = true;
        private bool isSprinting => canSprint && Input.GetKey(sprintKey);
        private bool shouldJump => characterController.isGrounded && Input.GetKeyDown(jumpKey);
        private bool shouldCrouch => !duringCrouchAnimation && characterController.isGrounded && Input.GetKeyDown(crouchKey);
        private bool shouldDash => !duringCrouchAnimation && !isCrouching && !isSprinting && !isSliding && Input.GetKeyDown(dashKey);


        [Header("Functional Options")]
        [Tooltip("Check if Player should Sprint or not")]
        [SerializeField] private bool canSprint = true;
        [Tooltip("Check if Player should Jump or not")]
        [SerializeField] private bool CanJump = true;
        [Tooltip("Check if Player should Crouch or not")]
        [SerializeField] private bool CanCrouch = true;
        [Tooltip("Check if Player should Crouch or not")]
        [SerializeField] private bool CanDash = true;
        [Tooltip("Check if Player should Headbob or not")]
        [SerializeField] private bool CanHeadbob = true;
        [Tooltip("Check if Player should Slide on Slopes or not")]
        [SerializeField] private bool CanSlideOnSlopes = true;

        [Header("Controls")]
        [Tooltip("Keyboard Key for Sprint Function")]
        [SerializeField] private KeyCode sprintKey = KeyCode.LeftShift;
        [Tooltip("Keyboard Key for Jump Function")]
        [SerializeField] private KeyCode jumpKey = KeyCode.Space;
        [Tooltip("Keyboard Key for Crouch Function")]
        [SerializeField] private KeyCode crouchKey = KeyCode.LeftControl;
        [Tooltip("Keyboard Key for Dash Function")]
        [SerializeField] private KeyCode dashKey = KeyCode.LeftAlt;

        [Header("Camera Settings")]
        [Tooltip("This is Vertical Mouse Speed")]
        [SerializeField, Range(1, 10)] private float lookSpeedX = 2.0f;
        [Tooltip("This is Horizontal Mouse Speed")]
        [SerializeField, Range(1, 10)] private float lookSpeedY = 2.0f;
        [Tooltip("This is how high you can see")]
        [SerializeField, Range(1, 90)] private float upperLookLimit = 60.0f;
        [Tooltip("This is how low you can see")]
        [SerializeField, Range(1, 90)] private float lowerLookLimit = 45.0f;

        [Header("Headbob Settings")]
        [Tooltip("Walking Headbob Speed")]
        [SerializeField] private float walkBobSpeed = 14f;
        [Tooltip("Walking Headbob Camera Movement")]
        [SerializeField] private float walkBobAmount = 0.05f;
        [Tooltip("Sprinting Headbob Speed")]
        [SerializeField] private float sprintBobSpeed = 18f;
        [Tooltip("Sprinting Headbob Camera Movement")]
        [SerializeField] private float sprintBobAmount = 0.1f;
        [Tooltip("Crouching Headbob Speed")]
        [SerializeField] private float crouchBobSpeed = 7f;
        [Tooltip("Crouching Headbob Camera Movement")]
        [SerializeField] private float crouchBobAmount = 0.025f;
        private float defaultYPos = 0f;
        private float timer;

        [Header("Player Movement")]
        [Tooltip("This is the Player's Walk Speed")]
        [SerializeField] private float walkSpeed = 10.0f;
        [Tooltip("This is the Player's Sprint Speed")]
        [SerializeField] private float sprintSpeed = 20.0f;
        [Tooltip("This is the Player's Crouch Speed")]
        [SerializeField] private float crouchSpeed = 2.5f;
        [Tooltip("This is the Player's Slope Sliding Speed")]
        [SerializeField] private float slopeSlidingSpeed = 8.0f;

        [Header("Player Jump")]
        [Tooltip("This is the Player's Jump Force")]
        [SerializeField] private float jumpForce = 12.0f;
        [Tooltip("This is the Player's Gravitational Force")]
        [SerializeField] private float gravity = 30.0f;

        [Header("Player Crouch")]
        [Tooltip("This is the Player's Crouch limit")]
        [SerializeField] private float crouchHeight = 0.5f;
        [Tooltip("This is the Player's Standing Height")]
        [SerializeField] private float StandingHeight = 2.0f;
        [Tooltip("How much time it takes Player to crouch")]
        [SerializeField] private float timeToCrouch = 0.25f;
        [Tooltip("This is the Player's Crouching Center Position")]
        [SerializeField] private Vector3 crouchingCenter = new Vector3(0f, 0.5f, 0f);
        [Tooltip("This is the Player's Standing Height Position")]
        [SerializeField] private Vector3 standingCenter = new Vector3(0f, 0f, 0f);
        private bool isCrouching;
        private bool duringCrouchAnimation;

        [Header("Player Dash")]
        [SerializeField] private float dashSpeed = 5.0f;
        [SerializeField] private float dashTime = 0.3f;

        //Sliding Settings
        private Vector3 hitPointNormal;

        private bool isSliding
        {
            get
            {
                if (characterController.isGrounded && Physics.Raycast(transform.position, Vector3.down, out RaycastHit slopeHit, 2f))
                {
                    hitPointNormal = slopeHit.normal;
                    return Vector3.Angle(hitPointNormal, Vector3.up) > characterController.slopeLimit;
                }
                else
                {
                    return false;
                }
            }
        }


        //Private Variables
        private Camera playerCamera;
        private CharacterController characterController;

        private Vector3 moveDirection;
        private Vector2 currentInput;

        private float rotationX = 0f;

        void Awake()
        {
            playerCamera = GetComponentInChildren<Camera>();
            characterController = GetComponent<CharacterController>();

            defaultYPos = playerCamera.transform.localPosition.y;

            //Locks and Hides Cursor
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        void Update()
        {
            if (CanMove)
            {
                Movement();
                CameraRotation();

                if (CanHeadbob)
                    Headbob();

                if (CanJump)
                    Jump();

                if (CanCrouch)
                    Crouch();

                if (CanDash)
                    Dash();

                FinalUpdates();
            }
        }

        private void Headbob()
        {
            if (!characterController.isGrounded) return;

            if (Mathf.Abs(moveDirection.x) > 0.1f || Mathf.Abs(moveDirection.z) > 0.1f)
            {
                timer += Time.deltaTime * (isCrouching ? crouchBobSpeed : isSprinting ? sprintBobSpeed : walkBobSpeed);
                playerCamera.transform.localPosition = new Vector3(
                    playerCamera.transform.localPosition.x,
                    defaultYPos + Mathf.Sin(timer) * (isCrouching ? crouchBobAmount : isSprinting ? sprintBobAmount : walkBobAmount),
                    playerCamera.transform.localPosition.z);
            }
        }

        private void Movement()
        {
            currentInput = new Vector2((isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Vertical"), (isCrouching ? crouchSpeed : isSprinting ? sprintSpeed : walkSpeed) * Input.GetAxis("Horizontal"));

            float moveDirectionY = moveDirection.y;
            moveDirection = (transform.TransformDirection(Vector3.forward) * currentInput.x) + (transform.TransformDirection(Vector3.right) * currentInput.y);
            moveDirection.y = moveDirectionY;
        }

        private void Jump()
        {
            if(shouldJump)
                moveDirection.y = jumpForce;
        }

        private void Crouch()
        {
            if (shouldCrouch)
                StartCoroutine(CrouchStand());
        }

        private void Dash()
        {
            if (shouldDash)
                StartCoroutine(Dashing());
        }

        private void CameraRotation()
        {
            rotationX -= Input.GetAxis("Mouse Y") * lookSpeedY;
            rotationX = Mathf.Clamp(rotationX, -upperLookLimit, lowerLookLimit);
            playerCamera.transform.localRotation = Quaternion.Euler(rotationX, 0f, 0f);
            transform.rotation *= Quaternion.Euler(0, Input.GetAxis("Mouse X") * lookSpeedX, 0f);
        }

        private void FinalUpdates()
        {
            if(!characterController.isGrounded)
                moveDirection.y -= gravity * Time.deltaTime;

            if (CanSlideOnSlopes && isSliding)
                moveDirection += new Vector3(hitPointNormal.x, -hitPointNormal.y, hitPointNormal.z) * slopeSlidingSpeed;

            characterController.Move(moveDirection * Time.deltaTime);
        }

        private IEnumerator CrouchStand()
        {
            if(isCrouching && Physics.Raycast(playerCamera.transform.position, Vector3.up, 1f))
                yield break;

            duringCrouchAnimation = true;

            float timeElapsed = 0;
            float targetHeight = isCrouching ? StandingHeight : crouchHeight;
            float currentHeight = characterController.height;
            Vector3 targetCenter = isCrouching ? standingCenter : crouchingCenter;
            Vector3 currentCenter = characterController.center;

            while(timeElapsed < timeToCrouch)
            {
                characterController.height = Mathf.Lerp(currentHeight, targetHeight, timeElapsed / timeToCrouch);
                characterController.center = Vector3.Lerp(currentCenter, targetCenter, timeElapsed / timeToCrouch);
                timeElapsed += Time.deltaTime;
                yield return null;
            }

            characterController.height = targetHeight;
            characterController.center = targetCenter;

            isCrouching = !isCrouching;

            duringCrouchAnimation = false;
        }

        private IEnumerator Dashing()
        {
            float startTime = Time.time;

            while(Time.time < startTime + dashTime)
            {
                characterController.Move(moveDirection * dashSpeed * Time.deltaTime);

                yield return null;
            }
        }

    }
}
