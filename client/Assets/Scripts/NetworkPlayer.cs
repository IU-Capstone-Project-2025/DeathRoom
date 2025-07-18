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
    private Vector3 lastTargetPosition;
    private Quaternion targetRotation;
    private Vector3 lastPosition;
    private bool isMoving = false;
    private float lastUpdateTime;
    
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
        lastTargetPosition = pos;
        targetRotation = rot;
        lastPosition = pos;
        lastUpdateTime = Time.time;
        
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
            lastTargetPosition = targetPosition;
            targetPosition = newPosition;
            targetRotation = newRotation;
            lastUpdateTime = Time.time;
            Debug.Log($"NetworkPlayer {Username} teleported due to large distance: {distance:F2}");
        } else {
            lastTargetPosition = targetPosition;
            targetPosition = newPosition;
            targetRotation = newRotation;
            lastUpdateTime = Time.time;
        }
        
        UpdateAnimation();
    }

    void Update()
    {
        // Interpolate position and rotation
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        }
        
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
        
        // Calculate movement for animation
        float velocity = Vector3.Distance(lastPosition, transform.position) / Time.deltaTime;
        isMoving = velocity > 0.1f;
        lastPosition = transform.position;
    }


    void UpdateAnimation() {
        if (animator == null) {
            Debug.LogWarning($"NetworkPlayer {Username} (ID: {PlayerId}): Animator is null, skipping animation update");
            return;
        }
        
        if (transform == null) {
            Debug.LogError($"NetworkPlayer {Username} (ID: {PlayerId}): Transform is null!");
            return;
        }
        
        // Calculate movement based on target position changes, not interpolation
        Vector3 targetMovement = targetPosition - lastTargetPosition;
        Vector3 localVelocity = transform.InverseTransformDirection(targetMovement.normalized);
        
        // If target hasn't changed (no real movement), set all animation parameters to zero/false
        if (targetMovement.magnitude < 0.01f) {
            localVelocity = Vector3.zero;
            
            // Reset all animation parameters to idle state
            animator.SetFloat("MoveX", 0f);
            animator.SetFloat("MoveZ", 0f);
            animator.SetFloat("TurnValue", 0f);
            animator.SetFloat("ShootType", 0f);
            animator.SetBool("Sprint", false);
            animator.SetBool("OnAir", false);
            animator.SetBool("Jump", false);
            animator.SetBool("FlipForward", false);
            animator.SetBool("Kick", false);
            animator.SetBool("Shoot", false);
            animator.SetBool("Reload", false);
            animator.SetBool("Aim", false);
            animator.SetBool("JumpOver", false);
            animator.SetBool("HitWall", false);
            animator.SetBool("ChangeWeapon", false);
        } else {
            // Normal movement - set movement parameters
            animator.SetFloat("MoveX", localVelocity.x);
            animator.SetFloat("MoveZ", localVelocity.z);
            animator.SetBool("Sprint", targetMovement.magnitude > 0.5f); // Sprint if significant movement
        }
        
        Debug.Log($"NetworkPlayer {Username}: UpdateAnimation - targetMovement: {targetMovement.magnitude:F2}, localVel: ({localVelocity.x:F2}, {localVelocity.z:F2}), isMoving: {isMoving}");
        
        if (currentState != null) {
            bool isDead = currentState.HealthPoint <= 0;
            Debug.Log($"NetworkPlayer {Username}: Health: {currentState.HealthPoint}, isDead: {isDead}");
        } else {
            Debug.LogWarning($"NetworkPlayer {Username} (ID: {PlayerId}): currentState is null in UpdateAnimation");
        }
    }

    public void ApplyAnimationUpdate(PlayerAnimationPacket packet)
    {
        if (animator == null) return;

        foreach (var param in packet.BoolParams)
        {
            animator.SetBool(param.Key, param.Value);
        }
        foreach (var param in packet.FloatParams)
        {
            animator.SetFloat(param.Key, param.Value);
        }
        foreach (var param in packet.IntParams)
        {
            animator.SetInteger(param.Key, param.Value);
        }
    }

    void OnDestroy() {
        Debug.Log($"NetworkPlayer destroyed: {Username}");
    }
}
