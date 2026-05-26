using UnityEngine;

namespace Invector.vCharacterController
{
    public class vThirdPersonController : vThirdPersonAnimator
    {
        [Header("--- COMBO SETTINGS ---")]
        public int comboCount = 0;        // Compteur actuel du combo
        public int maxCombo = 3;          // Nombre total d'attaques (0, 1, 2...)
        public float comboResetTime = 1f; // Temps avant que le combo ne se réinitialise
        private float lastAttackTime;     // Timer interne

        [Header("--- CROUCH SETTINGS ---")]
        private bool isCrouching = false;  // État d'accroupissement
        private bool isCrouchWalking = false; // État de marche accroupie

        // Ajoutons une variable pour suivre l'état précédent et éviter les mises à jour inutiles
        private bool wasMoving = false;

        // --- MODIFICATION : Référence à l'arme actuellement équipée ---
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
                // calculate input smooth
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
                //get the right-facing direction of the referenceTransform
                var right = referenceTransform.right;
                right.y = 0;
                //get the forward direction relative to referenceTransform Right
                var forward = Quaternion.AngleAxis(-90, Vector3.up) * right;
                // determine the direction the player will face based on input and the referenceTransform's right and forward directions
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
            // trigger jump behaviour
            jumpCounter = jumpTimer;
            isJumping = true;

            // trigger jump animations
            if (input.sqrMagnitude < 0.1f)
                animator.CrossFadeInFixedTime("Jump", 0.1f);
            else
                animator.CrossFadeInFixedTime("JumpMove", .2f);
        }

        // --- SYSTÈME D'ATTAQUE AVEC COMBO (MODIFIÉ) ---
        public virtual void Attack()
        {
            // --- MODIFICATION : On vérifie d'abord si une arme est équipée ---
            if (currentWeapon == null)
            {
                Debug.Log("Aucune arme équipée, impossible d'attaquer.");
                return; // On arrête la fonction ici si aucune arme
            }

            // On ne peut attaquer que si on est au sol et qu'on ne saute pas
            if (isGrounded && !isJumping)
            {
                // 1. Si on a attendu trop longtemps depuis la dernière attaque, on remet le combo à 0
                if (Time.time > lastAttackTime + comboResetTime)
                {
                    comboCount = 0;
                }

                // 2. On envoie l'ID (0, 1 ou 2) à l'Animator pour qu'il choisisse la bonne animation
                animator.SetInteger("AttackID", comboCount);

                // 3. On déclenche l'attaque
                animator.SetTrigger("Attack");

                // --- MODIFICATION : On informe l'arme que l'attaque commence ---
                currentWeapon.StartAttack();

                // 4. On met à jour le temps de la dernière attaque
                lastAttackTime = Time.time;

                // 5. On prépare le prochain coup
                comboCount++;

                // 6. Si on dépasse le nombre max d'animations, on revient à 0
                if (comboCount >= maxCombo)
                {
                    comboCount = 0;
                }
            }
        }

        // --- FONCTION D'ACCROUPISSSEMENT ---
        public virtual void Crouch()
        {
            // Bascule entre l'état debout et accroupi
            isCrouching = !isCrouching;

            // Met à jour le paramètre de l'Animator
            animator.SetBool("IsCrouching", isCrouching);

            // Ajuster la vitesse de déplacement si nécessaire
            if (isCrouching)
            {
                // Réduire la vitesse de déplacement lorsque accroupi
                freeSpeed.walkSpeed = 1.5f;  // Vitesse de marche accroupie
                strafeSpeed.walkSpeed = 1.5f;  // Vitesse de marche latérale accroupie
            }
            else
            {
                // Restaurer la vitesse de déplacement normale
                freeSpeed.walkSpeed = 2.5f;  // Vitesse de marche normale
                strafeSpeed.walkSpeed = 2.5f;  // Vitesse de marche latérale normale
            }
        }

        // Méthode pour mettre à jour l'état de marche accroupie
        protected virtual void UpdateCrouchWalking()
        {
            // Vérifie si le personnage est en mouvement
            bool isMoving = input.sqrMagnitude > 0.1f;

            // Détermine si le personnage est en marche accroupie (accroupi ET en mouvement)
            bool newCrouchWalking = isCrouching && isMoving;

            // Met à jour l'état seulement s'il a changé
            if (newCrouchWalking != isCrouchWalking)
            {
                isCrouchWalking = newCrouchWalking;
                animator.SetBool("IsCrouchWalking", isCrouchWalking);

                // Debug pour vérifier les changements d'état
                Debug.Log($"IsCrouchWalking: {isCrouchWalking}, IsCrouching: {isCrouching}, IsMoving: {isMoving}");
            }

            wasMoving = isMoving;
        }

        // Surcharge de la méthode UpdateAnimator pour inclure la mise à jour de la marche accroupie
        public override void UpdateAnimator()
        {
            base.UpdateAnimator();
            UpdateCrouchWalking();
        }

        // --- MODIFICATION : Nouvelles fonctions pour gérer l'arme actuelle ---

        /// <summary>
        /// Appelé par le WeaponSwitcher pour définir l'arme actuellement active.
        /// </summary>
        /// <param name="weapon">Le script de l'arme, ou null si aucune arme.</param>
        public void SetCurrentWeapon(Weapon weapon)
        {
            currentWeapon = weapon;
        }

        /// <summary>
        /// Cette fonction sera appelée par un événement d'animation à la fin de chaque attaque.
        /// </summary>
        public void OnAttackAnimationEnd()
        {
            if (currentWeapon != null)
            {
                currentWeapon.EndAttack();
            }
        }

        // --- AJOUT : Fonction pour déclencher l'animation de prise de dégâts ---
        /// <summary>
        /// Appelée par un ennemi pour déclencher l'animation de prise de dégâts.
        /// </summary>
        public void GetHit()
        {
            // Déclenche l'animation de prise de dégâts
            animator.SetTrigger("GetHitTrigger");
        }
    }
}