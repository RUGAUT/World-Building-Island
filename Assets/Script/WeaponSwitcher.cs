using UnityEngine;
using Invector.vCharacterController;

public class WeaponSwitcher : MonoBehaviour
{
    [Header("Configurations")]
    public GameObject weapon; // Ton modèle d'arme
    public Transform handSocket; // L'emplacement dans la main
    public Transform backSocket; // L'emplacement sur le dos/hanche

    private bool isEquipped = false;

    private Weapon weaponScript; // Référence au script Weapon sur l'objet de l'arme
    private vThirdPersonController playerController; // Référence au contrôleur du joueur

    void Start()
    {
        // On cherche le script Weapon sur l'objet de l'arme
        if (weapon != null)
        {
            weaponScript = weapon.GetComponent<Weapon>();
        }
        // On cherche le contrôleur du joueur dans un objet parent
        playerController = GetComponentInParent<vThirdPersonController>();

        // On s'assure que l'arme est rangée au début
        AttachWeapon(backSocket);
    }

    void Update()
    {
        // Appuie sur 'E' pour dégainer/ranger
        if (Input.GetKeyDown(KeyCode.E))
        {
            ToggleWeapon();
        }
    }

    void ToggleWeapon()
    {
        isEquipped = !isEquipped;

        if (isEquipped)
        {
            AttachWeapon(handSocket);
            if (playerController != null && weaponScript != null)
            {
                playerController.SetCurrentWeapon(weaponScript);
            }
        }
        else
        {
            AttachWeapon(backSocket);
            if (playerController != null)
            {
                playerController.SetCurrentWeapon(null);
            }
        }
    }

    void AttachWeapon(Transform targetSocket)
    {
        weapon.transform.SetParent(targetSocket);
        weapon.transform.localPosition = Vector3.zero;
        weapon.transform.localRotation = Quaternion.identity;
    }
}