using UnityEngine;
using Invector.vCharacterController;

public class vLockMovementBehaviour : StateMachineBehaviour
{
    // S'exécute quand l'animation d'attaque commence
    override public void OnStateEnter(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var cc = animator.GetComponent<vThirdPersonController>();
        if (cc != null)
        {
            cc.lockMovement = true; // Bloque le déplacement
            cc.lockRotation = true; // Optionnel : bloque aussi la rotation
        }
    }

    // S'exécute quand l'animation d'attaque se termine ou est interrompue
    override public void OnStateExit(Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
    {
        var cc = animator.GetComponent<vThirdPersonController>();
        if (cc != null)
        {
            cc.lockMovement = false; // Redonne le contrôle au joueur
            cc.lockRotation = false;
        }
    }
}