using UnityEngine;
using System.Collections;

public class Zombie : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 3f;
    public float chaseSpeed = 5f;
    public float rotationSpeed = 5f;
    public float detectionRange = 10f;
    public float attackRange = 2f;
    public float returnThreshold = 0.5f; // How close to get to home position

    [Header("Audio Settings")]
    public AudioClip idleSound;
    public AudioClip chaseSound;
    public AudioClip returnSound;
    public float idleSoundInterval = 5f;
    public float chaseSoundInterval = 3f;

    [Header("Patrol Settings")]
    public bool patrolEnabled = false;
    public Transform[] waypoints;
    public float waypointThreshold = 1f;
    public float waitTimeAtWaypoint = 2f;

    private Transform player;
    private AudioSource audioSource;
    private Animator animator;
    private Vector3 homePosition;
    private Quaternion homeRotation;
    private int currentWaypointIndex = 0;
    private float waitTimer = 0f;
    private bool isWaiting = false;

    // States
    private enum ZombieState { Idle, Chasing, Returning, Patrolling }
    private ZombieState currentState = ZombieState.Idle;

    void Start()
    {
        player = GameObject.FindGameObjectWithTag("Player").transform;
        audioSource = GetComponent<AudioSource>();
        animator = GetComponent<Animator>();

        // Store initial position and rotation
        homePosition = transform.position;
        homeRotation = transform.rotation;

        // Start idle sound routine
        StartCoroutine(PlayIdleSounds());
    }

    void Update()
    {
        float distanceToPlayer = Vector3.Distance(transform.position, player.position);

        switch (currentState)
        {
            case ZombieState.Idle:
                UpdateIdleState(distanceToPlayer);
                break;

            case ZombieState.Chasing:
                UpdateChaseState(distanceToPlayer);
                break;

            case ZombieState.Returning:
                UpdateReturnState();
                break;

            case ZombieState.Patrolling:
                UpdatePatrolState();
                break;
        }
    }

    void UpdateIdleState(float distanceToPlayer)
    {
        // Check if player is in detection range and visible
        if (distanceToPlayer <= detectionRange && HasLineOfSight())
        {
            StartChasing();
        }
        else if (patrolEnabled && waypoints.Length > 0)
        {
            StartPatrolling();
        }
    }

    void UpdateChaseState(float distanceToPlayer)
    {
        // Continue chasing if player is still visible
        if (distanceToPlayer <= detectionRange && HasLineOfSight())
        {
            ChasePlayer();

            // Check if we should attack
            if (distanceToPlayer <= attackRange)
            {
                AttackPlayer();
            }
        }
        else
        {
            // Lost sight of player, start returning
            StartReturning();
        }
    }

    void UpdateReturnState()
    {
        // Check if we've reached home position
        if (Vector3.Distance(transform.position, homePosition) <= returnThreshold)
        {
            // Reached home, return to idle
            ReturnComplete();
        }
        else
        {
            // Move back to home position
            ReturnToHome();
        }
    }

    void UpdatePatrolState()
    {
        if (isWaiting)
        {
            waitTimer -= Time.deltaTime;
            if (waitTimer <= 0f)
            {
                isWaiting = false;
                MoveToNextWaypoint();
            }
        }
        else
        {
            // Check if reached current waypoint
            if (Vector3.Distance(transform.position, waypoints[currentWaypointIndex].position) <= waypointThreshold)
            {
                // Wait at waypoint
                isWaiting = true;
                waitTimer = waitTimeAtWaypoint;
                animator.SetBool("IsWalking", false);
            }
            else
            {
                // Move to waypoint
                MoveToWaypoint();
            }
        }

        // Check for player detection while patrolling
        if (Vector3.Distance(transform.position, player.position) <= detectionRange && HasLineOfSight())
        {
            StartChasing();
        }
    }

    bool HasLineOfSight()
    {
        RaycastHit hit;
        Vector3 direction = player.position - transform.position;

        if (Physics.Raycast(transform.position, direction, out hit, detectionRange))
        {
            if (hit.transform == player)
            {
                return true;
            }
        }
        return false;
    }

    void ChasePlayer()
    {
        // Rotate towards player
        Vector3 direction = player.position - transform.position;
        direction.y = 0; // Keep zombie upright
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

        // Move towards player
        transform.position += transform.forward * chaseSpeed * Time.deltaTime;
        animator.SetBool("IsWalking", true);
    }

    void AttackPlayer()
    {
        // Here you would implement attack logic
        animator.SetTrigger("Attack");
        // You could add damage to player here
    }

    void ReturnToHome()
    {
        // Rotate towards home position
        Vector3 direction = homePosition - transform.position;
        direction.y = 0;
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

        // Move towards home
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        animator.SetBool("IsWalking", true);
    }

    void MoveToWaypoint()
    {
        Transform target = waypoints[currentWaypointIndex];

        // Rotate towards waypoint
        Vector3 direction = target.position - transform.position;
        direction.y = 0;
        Quaternion rotation = Quaternion.LookRotation(direction);
        transform.rotation = Quaternion.Slerp(transform.rotation, rotation, rotationSpeed * Time.deltaTime);

        // Move towards waypoint
        transform.position += transform.forward * moveSpeed * Time.deltaTime;
        animator.SetBool("IsWalking", true);
    }

    void MoveToNextWaypoint()
    {
        currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Length;
    }

    void StartChasing()
    {
        currentState = ZombieState.Chasing;
        PlayChaseSound();
        animator.SetBool("IsChasing", true);
        animator.SetBool("IsWalking", true);
        StopAllCoroutines();
        StartCoroutine(PlayChaseSounds());
    }

    void StartReturning()
    {
        currentState = ZombieState.Returning;
        PlayReturnSound();
        animator.SetBool("IsChasing", false);
        animator.SetBool("IsWalking", true);
        StopAllCoroutines();
    }

    void StartPatrolling()
    {
        currentState = ZombieState.Patrolling;
        animator.SetBool("IsWalking", true);
    }

    void ReturnComplete()
    {
        currentState = ZombieState.Idle;
        transform.rotation = homeRotation;
        animator.SetBool("IsWalking", false);
        StartCoroutine(PlayIdleSounds());
    }

    IEnumerator PlayIdleSounds()
    {
        while (true)
        {
            PlayIdleSound();
            yield return new WaitForSeconds(idleSoundInterval);
        }
    }

    IEnumerator PlayChaseSounds()
    {
        while (currentState == ZombieState.Chasing)
        {
            PlayChaseSound();
            yield return new WaitForSeconds(chaseSoundInterval);
        }
    }

    void PlayIdleSound()
    {
        if (idleSound != null && !audioSource.isPlaying)
        {
            audioSource.clip = idleSound;
            audioSource.Play();
        }
    }

    void PlayChaseSound()
    {
        if (chaseSound != null)
        {
            audioSource.clip = chaseSound;
            audioSource.Play();
        }
    }

    void PlayReturnSound()
    {
        if (returnSound != null)
        {
            audioSource.clip = returnSound;
            audioSource.Play();
        }
    }

    void OnDrawGizmosSelected()
    {
        // Draw detection range
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRange);

        // Draw attack range
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, attackRange);

        // Draw home position marker
        if (Application.isPlaying)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(homePosition, Vector3.one);
        }

        // Draw patrol path if enabled
        if (patrolEnabled && waypoints != null && waypoints.Length > 0)
        {
            Gizmos.color = Color.blue;
            for (int i = 0; i < waypoints.Length; i++)
            {
                if (waypoints[i] != null)
                {
                    Gizmos.DrawSphere(waypoints[i].position, 0.5f);
                    if (i < waypoints.Length - 1 && waypoints[i + 1] != null)
                    {
                        Gizmos.DrawLine(waypoints[i].position, waypoints[i + 1].position);
                    }
                }
            }
        }
    }
}