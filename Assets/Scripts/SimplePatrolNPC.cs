using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SimplePatrolNPC : MonoBehaviour
{
    [Header("Patrol Settings")]
    [SerializeField] private float patrolRadius = 10f;
    [SerializeField] private float moveSpeed = 2f;
    [SerializeField] private float waypointStopDuration = 2f;
    [SerializeField] private float waypointReachThreshold = 0.5f;
    
    [Header("Bobbing Settings")]
    [SerializeField] private float idleBobSpeed = 1f;
    [SerializeField] private float idleBobHeight = 0.3f;
    [SerializeField] private float moveBobSpeed = 3f;
    [SerializeField] private float moveBobHeight = 0.15f;
    
    [Header("Sound Settings")]
    [SerializeField] private AudioSource audioSource;
    [SerializeField] private AudioClip idleSound;
    [SerializeField] private AudioClip moveSound;
    [SerializeField] private float idleSoundInterval = 3f;
    [SerializeField] private float moveSoundInterval = 1.5f;
    
    private Vector3 startPosition;
    private Vector3 targetWaypoint;
    private float waitTimer;
    private bool isWaiting;
    private float bobTimer;
    private float soundTimer;
    
    private enum NPCState { Idle, Moving }
    private NPCState currentState = NPCState.Idle;
    
    void Start()
    {
        startPosition = transform.position;
        
        if (audioSource == null)
            audioSource = GetComponent<AudioSource>();
        
        SetNewRandomWaypoint();
    }
    
    void Update()
    {
        switch (currentState)
        {
            case NPCState.Idle:
                HandleIdleState();
                break;
            case NPCState.Moving:
                HandleMovingState();
                break;
        }
        
        ApplyBobbing();
        HandleSounds();
    }
    
    void HandleIdleState()
    {
        waitTimer += Time.deltaTime;
        
        if (waitTimer >= waypointStopDuration)
        {
            SetNewRandomWaypoint();
            currentState = NPCState.Moving;
            waitTimer = 0f;
        }
    }
    
    void HandleMovingState()
    {
        Vector3 targetPos = new Vector3(targetWaypoint.x, transform.position.y, targetWaypoint.z);
        Vector3 direction = (targetPos - transform.position).normalized;
        
        transform.position += direction * moveSpeed * Time.deltaTime;
        
        // Rotate to face movement direction
        if (direction != Vector3.zero)
        {
            Quaternion targetRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * 5f);
        }
        
        float distanceToWaypoint = Vector3.Distance(
            new Vector3(transform.position.x, 0, transform.position.z),
            new Vector3(targetWaypoint.x, 0, targetWaypoint.z)
        );
        
        if (distanceToWaypoint <= waypointReachThreshold)
        {
            currentState = NPCState.Idle;
            isWaiting = true;
        }
    }
    
    void ApplyBobbing()
    {
        bobTimer += Time.deltaTime;
        
        float bobSpeed = currentState == NPCState.Idle ? idleBobSpeed : moveBobSpeed;
        float bobHeight = currentState == NPCState.Idle ? idleBobHeight : moveBobHeight;
        
        float yOffset = Mathf.Sin(bobTimer * bobSpeed) * bobHeight;
        
        Vector3 pos = transform.position;
        pos.y = startPosition.y + yOffset;
        transform.position = pos;
    }
    
    void HandleSounds()
    {
        if (audioSource == null) return;
        
        soundTimer += Time.deltaTime;
        
        float interval = currentState == NPCState.Idle ? idleSoundInterval : moveSoundInterval;
        AudioClip clipToPlay = currentState == NPCState.Idle ? idleSound : moveSound;
        
        if (soundTimer >= interval && clipToPlay != null)
        {
            audioSource.PlayOneShot(clipToPlay);
            soundTimer = 0f;
        }
    }
    
    void SetNewRandomWaypoint()
    {
        Vector2 randomCircle = Random.insideUnitCircle * patrolRadius;
        targetWaypoint = startPosition + new Vector3(randomCircle.x, 0, randomCircle.y);
    }
    
    void OnDrawGizmosSelected()
    {
        Vector3 center = Application.isPlaying ? startPosition : transform.position;
        
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(center, patrolRadius);
        
        if (Application.isPlaying)
        {
            Gizmos.color = Color.red;
            Gizmos.DrawSphere(targetWaypoint, 0.3f);
            Gizmos.DrawLine(transform.position, targetWaypoint);
        }
    }
}
