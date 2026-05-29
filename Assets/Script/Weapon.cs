using UnityEngine;

public class Weapon : MonoBehaviour
{
    private bool isAttacking = false;
    private Collider weaponCollider;

    [Header("Réglages de l'arme")]
    public float degatsDeLarme = 25f;

    void Awake()
    {
        weaponCollider = GetComponent<Collider>();

        if (weaponCollider != null)
        {
            weaponCollider.isTrigger = true;
            weaponCollider.enabled = false;
        }
    }

    public void StartAttack()
    {
        isAttacking = true;
        if (weaponCollider != null)
        {
            weaponCollider.enabled = true;
        }

        // --- SÉCURITÉ --- 
        // Force la désactivation de l'arme après 1.2 secondes au cas où l'animation est coupée
        CancelInvoke("EndAttack");
        Invoke("EndAttack", 1.2f);
    }

    public void EndAttack()
    {
        isAttacking = false;
        if (weaponCollider != null)
        {
            weaponCollider.enabled = false;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (isAttacking && other.CompareTag("Animal"))
        {
            AnimalAI animal = other.GetComponent<AnimalAI>();
            if (animal != null)
            {
                animal.PrendreDegats(degatsDeLarme);
            }
        }
    }
}