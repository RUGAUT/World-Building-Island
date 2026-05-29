using UnityEngine;
using UnityEngine.AI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimalAI : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator anim;

    [Header("Santé")]
    public float pointsDeVie = 100f;
    private bool estMort = false;

    [Header("Type d'Animal")]
    public bool estAgressif = true;

    [Header("Réglages de déplacement")]
    public float rayonDeBalade = 15f;
    public float attenteMin = 3f;
    public float attenteMax = 6f;
    public float vitesseMarche = 2f;
    public float vitesseCourse = 6f;

    [Header("Détection et Combat")]
    public float distanceDetection = 15f;
    public float distanceAttaque = 2.5f;
    public float dureeAnimationAttaque = 1.2f;
    public float cooldownAttaque = 1.5f;
    public Transform joueur;
    public Collider attackHitbox;

    [Header("Hit / Blessure")]
    public float dureeAnimationHit = 0.5f;
    private bool enHit = false;
    private float finHitChrono = 0f;

    [Header("VFX / Effets")]
    [Tooltip("Le préfabriqué de particules à jouer lors d'un dégât (ex: sang, étincelles)")]
    public GameObject vfxDegatsPrefab;
    [Tooltip("Optionnel : Un point précis sur l'animal (ex: le torse) où faire apparaître le VFX. Si vide, apparaît au centre de l'animal.")]
    public Transform pointSpawnVFX;
    [Tooltip("Temps en secondes avant que le VFX ne soit supprimé de la scène")]
    public float dureeVieVFX = 2f;

    [Header("Animations")]
    public int nombreDeIdles = 3;

    private float chrono;
    private float tempsAttenteActuel;
    private bool estEnPause = false;
    private bool enPoursuiteOuFuite = false;
    private bool enAttaque = false;
    private float finAttaqueChrono = 0f;
    private float finCooldownAttaqueChrono = 0f;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        anim = GetComponent<Animator>();
        if (joueur == null) joueur = GameObject.FindGameObjectWithTag("Player")?.transform;
        tempsAttenteActuel = Random.Range(attenteMin, attenteMax);

        if (attackHitbox != null) attackHitbox.enabled = false;

        NouvelleDestination();
    }

    void Update()
    {
        if (estMort) return;

        if (enHit)
        {
            if (Time.time > finHitChrono)
            {
                enHit = false;
                agent.isStopped = false;
            }
            else return;
        }

        if (enAttaque)
        {
            RegarderLaCible();
            if (Time.time > finAttaqueChrono)
            {
                enAttaque = false;
                anim.SetBool("IsAttacking", false);
                finCooldownAttaqueChrono = Time.time + cooldownAttaque;
                if (attackHitbox != null) attackHitbox.enabled = false;
            }
            return;
        }

        if (Time.time < finCooldownAttaqueChrono) return;

        agent.isStopped = false;
        GererComportement();
        MettreAJourAnimations();
    }

    public void PrendreDegats(float degats)
    {
        if (estMort) return;

        pointsDeVie -= degats;

        // --- APPARITION DU VFX ---
        if (vfxDegatsPrefab != null)
        {
            // Si aucun point de spawn n'est défini, on prend la position de l'animal légèrement surélevée
            Vector3 positionSpawn = pointSpawnVFX != null ? pointSpawnVFX.position : transform.position + Vector3.up;

            // Instanciation et destruction automatique du VFX
            GameObject vfxInstance = Instantiate(vfxDegatsPrefab, positionSpawn, Quaternion.identity);
            Destroy(vfxInstance, dureeVieVFX);
        }
        // -------------------------

        if (pointsDeVie <= 0)
        {
            Mourir();
            return;
        }

        if (enHit) return;

        enHit = true;
        finHitChrono = Time.time + dureeAnimationHit;
        agent.isStopped = true;
        agent.velocity = Vector3.zero;

        if (enAttaque)
        {
            enAttaque = false;
            anim.SetBool("IsAttacking", false);
            if (attackHitbox != null) attackHitbox.enabled = false;
        }

        anim.SetTrigger("Hit");
        estAgressif = true;
        enPoursuiteOuFuite = true;
        agent.speed = vitesseCourse;
    }

    void Mourir()
    {
        estMort = true;
        agent.isStopped = true;
        agent.enabled = false;

        if (attackHitbox != null) attackHitbox.enabled = false;

        enAttaque = false;
        anim.SetBool("IsAttacking", false);

        anim.SetTrigger("Die");
    }

    void GererComportement()
    {
        if (joueur == null) return;
        float distanceDuJoueur = Vector3.Distance(transform.position, joueur.position);

        if (distanceDuJoueur <= distanceAttaque && estAgressif && Time.time >= finCooldownAttaqueChrono)
        {
            Attaquer();
        }
        else if (distanceDuJoueur < distanceDetection)
        {
            if (enAttaque) anim.SetBool("IsAttacking", false);
            agent.isStopped = false;
            agent.speed = vitesseCourse;
            enPoursuiteOuFuite = true;
            if (estAgressif) agent.SetDestination(joueur.position);
            else agent.SetDestination(CalculerMeilleureDestinationFuite(joueur.position));
        }
        else
        {
            if (enPoursuiteOuFuite)
            {
                enPoursuiteOuFuite = false;
                agent.speed = vitesseMarche;
                agent.isStopped = false;
                NouvelleDestination();
            }
            LogiqueBalade();
        }
    }

    void Attaquer()
    {
        if (!enAttaque && Time.time >= finCooldownAttaqueChrono)
        {
            enAttaque = true;
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            anim.SetBool("IsAttacking", true);
            finAttaqueChrono = Time.time + dureeAnimationAttaque;
            if (attackHitbox != null) attackHitbox.enabled = true;
        }
    }

    void RegarderLaCible()
    {
        Vector3 direction = (joueur.position - transform.position).normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(new Vector3(direction.x, 0, direction.z));
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * 10f);
        }
    }

    Vector3 CalculerMeilleureDestinationFuite(Vector3 positionMenace)
    {
        Vector3 meilleureDestination = transform.position;
        float meilleurScore = -1f;
        for (int i = 0; i < 8; i++)
        {
            float angle = i * 45f;
            Vector3 direction = Quaternion.Euler(0, angle, 0) * Vector3.forward;
            Vector3 pointTest = transform.position + direction * distanceDetection;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(pointTest, out hit, distanceDetection, NavMesh.AllAreas))
            {
                float distanceDeLaMenace = Vector3.Distance(hit.position, positionMenace);
                if (distanceDeLaMenace > meilleurScore)
                {
                    meilleurScore = distanceDeLaMenace;
                    meilleureDestination = hit.position;
                }
            }
        }
        return meilleureDestination;
    }

    void MettreAJourAnimations()
    {
        float vitesseActuelle = agent.velocity.magnitude;
        anim.SetFloat("Speed", vitesseActuelle);
        anim.SetBool("IsRunning", enPoursuiteOuFuite && vitesseActuelle > 0.1f);
        Vector3 localVelocity = transform.InverseTransformDirection(agent.velocity);
        anim.SetFloat("Turn", localVelocity.x);
    }

    void LogiqueBalade()
    {
        agent.isStopped = false;
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            if (!estEnPause)
            {
                anim.SetFloat("IdleIndex", (float)Random.Range(0, nombreDeIdles));
                tempsAttenteActuel = Random.Range(attenteMin, attenteMax);
                estEnPause = true;
                chrono = 0;
            }
            chrono += Time.deltaTime;
            if (chrono >= tempsAttenteActuel)
            {
                NouvelleDestination();
                estEnPause = false;
            }
        }
    }

    void NouvelleDestination()
    {
        agent.speed = vitesseMarche;
        Vector3 pointAleatoire = transform.position + Random.insideUnitSphere * rayonDeBalade;
        NavMeshHit hit;
        if (NavMesh.SamplePosition(pointAleatoire, out hit, rayonDeBalade, NavMesh.AllAreas))
            agent.SetDestination(hit.position);
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        if (estMort) return;
        Gizmos.color = Color.green;
        Gizmos.DrawWireSphere(transform.position, rayonDeBalade);
        Gizmos.color = Color.blue;
        Gizmos.DrawWireSphere(transform.position, distanceDetection);
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, distanceAttaque);
    }
#endif
}