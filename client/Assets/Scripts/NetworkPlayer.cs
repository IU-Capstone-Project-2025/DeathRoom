using DeathRoom.Common.network;
using DeathRoom.Common.dto;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour {
    [Header("Interpolation Settings")]
    public float interpolationSpeed = 10f;
    public float maxDistance = 1f;
    
    [Header("Components")]
    public Animator animator;
    
    private PlayerState currentState;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 lastPosition;
    private bool isMoving = false;
    
    public string Username { get; private set; }
    public int PlayerId { get; private set; }
    
    void Start() { DisableLocalPlayerComponents();}
    
    void DisableLocalPlayerComponents() {
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null) {
            playerMovement.enabled = false;
        }
        
        var characterController = GetComponent<CharacterController>();
        if (characterController != null) {
            characterController.enabled = false;
        }
        
        var cameras = GetComponentsInChildren<Camera>();
        foreach (var cam in cameras) {
            cam.enabled = false;
        }
        
        var audioListeners = GetComponentsInChildren<AudioListener>();
        foreach (var listener in audioListeners) {
            listener.enabled = false;
        }
    }
    
    public void Initialize(PlayerState playerState) {
        currentState = playerState;
        Username = playerState.Username;
        PlayerId = playerState.Id;
        
      
        var pos = playerState.Position.ToUnityVector3();
        var rot = Quaternion.Euler(
            playerState.Rotation.X,
            playerState.Rotation.Y,
            playerState.Rotation.Z
        );
        
        transform.position = pos;
        transform.rotation = rot;
        
        targetPosition = pos;
        targetRotation = rot;
        lastPosition = pos;
        
        Debug.Log($"NetworkPlayer initialized: {Username} (ID: {PlayerId}) at {pos}");
    }
    
    public void UpdateState(PlayerState newState) {
        if (newState == null) return;
        
        currentState = newState;

        Vector3 newPosition = newState.Position.ToUnityVector3();
        Quaternion newRotation = Quaternion.Euler(
            newState.Rotation.X,
            newState.Rotation.Y,
            newState.Rotation.Z
        );
        
        float distance = Vector3.Distance(transform.position, newPosition);
        
        Debug.Log($"NetworkPlayer {Username} (ID: {PlayerId}) update: pos {newPosition}, distance {distance:F2}");
        
        if (distance > maxDistance) {
            transform.position = newPosition;
            transform.rotation = newRotation;
            targetPosition = newPosition;
            targetRotation = newRotation;
            Debug.Log($"NetworkPlayer {Username} teleported due to large distance: {distance:F2}");
        } else {
            targetPosition = newPosition;
            targetRotation = newRotation;
        }
        
        UpdateAnimation();
    }

    void Update()
    {
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * interpolationSpeed);
        }
        else
        {
            transform.position = targetPosition;
        }

        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.01f)
        {
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, Time.deltaTime * interpolationSpeed);
        }
        else
        {
            transform.rotation = targetRotation;
        }
        float velocity = Vector3.Distance(lastPosition, transform.position) / Time.deltaTime;
        isMoving = velocity > 0.1f;
        lastPosition = transform.position;
    }


    void UpdateAnimation()
    {
        if (animator == null || currentState == null) return;

        animator.SetBool("IsCrouching", currentState.IsCrouching);
        animator.SetBool("IsOnAir", currentState.IsOnAir);
        animator.SetBool("IsRunning", currentState.IsRunning);
        animator.SetBool("IsShooting", currentState.IsShooting);
        animator.SetBool("IsAiming", currentState.IsAiming);

        animator.SetFloat("MoveX", currentState.MoveX);
        animator.SetFloat("MoveZ", currentState.MoveZ);

        bool isDead = currentState.HealthPoint <= 0;
        animator.SetBool("Dead", isDead);

    }

    void OnDestroy() {
        Debug.Log($"NetworkPlayer destroyed: {Username}");
    }
}
