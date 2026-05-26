using UnityEngine;

public class Weapon : MonoBehaviour
{
    private bool isAttacking = false;

    [Header("Réglages de l'arme")]
    public float degatsDeLarme = 25f; // <--- AJOUT : Définissez les dégâts ici

    public void StartAttack()
    {
        isAttacking = true;
    }

    public void EndAttack()
    {
        isAttacking = false;
    }

    void OnTriggerEnter(Collider other)
    {
        if (isAttacking && other.CompareTag("Animal"))
        {
            AnimalAI animal = other.GetComponent<AnimalAI>();
            if (animal != null)
            {
                // CORRECTION : On passe l'argument 'degatsDeLarme' ici
                animal.PrendreDegats(degatsDeLarme);
            }
        }
    }
}