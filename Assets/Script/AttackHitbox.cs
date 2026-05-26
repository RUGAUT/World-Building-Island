using Invector.vCharacterController;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    // Cette fonction est appelée automatiquement par Unity quand le trigger entre en collision avec un autre collider
    private void OnTriggerEnter(Collider other)
    {
        // On vérifie si l'objet touché a le tag "Player"
        if (other.CompareTag("Player"))
        {
            // On récupère le script du joueur sur l'objet touché
            vThirdPersonController playerController = other.GetComponent<vThirdPersonController>();

            // Si on a bien trouvé le script, on appelle la méthode GetHit()
            if (playerController != null)
            {
                playerController.GetHit();
            }
        }
    }
}