using Invector.vCharacterController;
using UnityEngine;

public class AttackHitbox : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            vThirdPersonController playerController = other.GetComponent<vThirdPersonController>();

            if (playerController != null)
            {
                playerController.GetHit();
            }
        }
    }
}