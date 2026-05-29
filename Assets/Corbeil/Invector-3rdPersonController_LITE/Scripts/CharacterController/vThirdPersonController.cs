using UnityEngine;

namespace Invector.vCharacterController
{
    public class vThirdPersonController : vThirdPersonAnimator
    {
        [Header("--- COMBO SETTINGS ---")]
        public int comboCount = 0;
        public int maxCombo = 4;          // Modifié à 4 pour permettre 4 attaques
        public float comboResetTime = 1.5f; // Augmenté pour laisser le temps de cliquer
        private float lastAttackTime;

        [Header("--- CROUCH SETTINGS ---")]
        private bool isCrouching = false;
        private bool isCrouchWalking = false;
        private bool wasMoving = false;

        private Weapon currentWeapon;

        public virtual void ControlAnimatorRootMotion()
        {
            if (!this.enabled) return;

            if (inputSmooth == Vector3.zero)
            {
                transform.position = animator.rootPosition;
                transform.rotation = animator.rootRotation;
            }

            if (useRootMotion)
                MoveCharacter(moveDirection);
        }

        public virtual void ControlLocomotionType()
        {
            if (lockMovement) return;

            if (locomotionType.Equals(LocomotionType.FreeWithStrafe) && !isStrafing || locomotionType.Equals(LocomotionType.OnlyFree))
            {
                SetControllerMoveSpeed(freeSpeed);
                SetAnimatorMoveSpeed(freeSpeed);
            }
            else if (locomotionType.Equals(LocomotionType.OnlyStrafe) || locomotionType.Equals(LocomotionType.FreeWithStrafe) && isStrafing)
            {
                isStrafing = true;
                SetControllerMoveSpeed(strafeSpeed);
                SetAnimatorMoveSpeed(strafeSpeed);
            }

            if (!useRootMotion)
                MoveCharacter(moveDirection);
        }

        public virtual void ControlRotationType()
        {
            if (lockRotation) return;

            bool validInput = input != Vector3.zero || (isStrafing ? strafeSpeed.rotateWithCamera : freeSpeed.rotateWithCamera);

            if (validInput)
            {
                inputSmooth = Vector3.Lerp(inputSmooth, input, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);

                Vector3 dir = (isStrafing && (!isSprinting || sprintOnlyFree == false) || (freeSpeed.rotateWithCamera && input == Vector3.zero)) && rotateTarget ? rotateTarget.forward : moveDirection;
                RotateToDirection(dir);
            }
        }

        public virtual void UpdateMoveDirection(Transform referenceTransform = null)
        {
            if (input.magnitude <= 0.01)
            {
                moveDirection = Vector3.Lerp(moveDirection, Vector3.zero, (isStrafing ? strafeSpeed.movementSmooth : freeSpeed.movementSmooth) * Time.deltaTime);
                return;
            }

            if (referenceTransform && !rotateByWorld)
            {
                var right = referenceTransform.right;
                right.y = 0;
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                moveDirection = (inputSmooth.x * right) + (inputSmooth.z * forward);
            }
            else
            {
                moveDirection = new Vector3(inputSmooth.x, 0, inputSmooth.z);
            }
        }

        public virtual void Sprint(bool value)
        {
            var sprintConditions = (input.sqrMagnitude > 0.1f && isGrounded &&
                !(isStrafing && !strafeSpeed.walkByDefault && (horizontalSpeed >= 0.5 || horizontalSpeed <= -0.5 || verticalSpeed <= 0.1f)));

            if (value && sprintConditions)
            {
                if (input.sqrMagnitude > 0.1f)
                {
                    if (isGrounded && useContinuousSprint)
                    {
                        isSprinting = !isSprinting;
                    }
                    else if (!isSprinting)
                    {
                        isSprinting = true;
                    }
                }
                else if (!useContinuousSprint && isSprinting)
                {
                    isSprinting = false;
                }
            }
            else if (isSprinting)
            {
                isSprinting = false;
            }
        }

        public virtual void Strafe()
        {
            isStrafing = !isStrafing;
        }

        public virtual void Jump()
        {
            jumpCounter = jumpTimer;
            isJumping = true;

            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        // --- SYSTÈME D'ATTAQUE ---
        public virtual void Attack()
        {
            if (currentWeapon == null)
            {
                Debug.Log("Aucune arme équipée, impossible d'attaquer.");
                return;
            }

            if (isGrounded && !isJumping)
            {
                if (Time.time > lastAttackTime + comboResetTime)
                {
                    comboCount = 0;
                }

                // Configuration de l'ID pour l'Animator
                animator.SetInteger("AttackID", comboCount);

                // NETTOYAGE DU TRIGGER : Empêche le bug du bouton bloqué
                animator.ResetTrigger("Attack");
                animator.SetTrigger("Attack");

                // Lancement des dégâts de l'arme
                currentWeapon.StartAttack();
                lastAttackTime = Time.time;

                // Incrémentation du combo pour le prochain coup
                comboCount++;

                if (comboCount >= maxCombo)
                {
                    comboCount = 0;
                }
            }
        }

        public virtual void Crouch()
        {
            isCrouching = !isCrouching;
            animator.SetBool("IsCrouching", isCrouching);

            if (isCrouching)
            {
                freeSpeed.walkSpeed = 1.5f;
                strafeSpeed.walkSpeed = 1.5f;
            }
            else
            {
                freeSpeed.walkSpeed = 2.5f;
                strafeSpeed.walkSpeed = 2.5f;
            }
        }

        protected virtual void UpdateCrouchWalking()
        {
            bool isMoving = input.sqrMagnitude > 0.1f;
            bool newCrouchWalking = isCrouching && isMoving;

            if (newCrouchWalking != isCrouchWalking)
            {
                isCrouchWalking = newCrouchWalking;
                animator.SetBool("IsCrouchWalking", isCrouchWalking);
            }

            wasMoving = isMoving;
        }

        public override void UpdateAnimator()
        {
            base.UpdateAnimator();
            UpdateCrouchWalking();
        }

        public void SetCurrentWeapon(Weapon weapon)
        {
            currentWeapon = weapon;
        }

        public void OnAttackAnimationEnd()
        {
            if (currentWeapon != null)
            {
                currentWeapon.EndAttack();
            }
        }

        public void GetHit()
        {
            animator.SetTrigger("GetHitTrigger");
        }
    }
}