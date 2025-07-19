using DeathRoom.Common.network;
using DeathRoom.Common.dto;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour {
    [Header("Interpolation Settings")]
    public float interpolationSpeed = 10f;
    public float maxDistance = 1f;
    
    [Header("Animation Settings")]
    public float animationUpdateRate = 10f;
    public float movementThreshold = 0.05f;
    public float animationSmoothTime = 0.1f;
    
    [Header("Components")]
    public Animator animator;
    
    private PlayerState currentState;
    private Vector3 targetPosition;
    private Vector3 lastTargetPosition;
    private Quaternion targetRotation;
    private Vector3 lastPosition;
    private bool isMoving = false;
    private float lastUpdateTime;
    
    // Animation smoothing variables
    private float lastAnimationUpdateTime;
    private Vector3 smoothedVelocity;
    private Vector3 velocitySmoothing;
    private bool wasMovingLastFrame;
    
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
        
        // Validate position data
        if (float.IsNaN(pos.x) || float.IsNaN(pos.y) || float.IsNaN(pos.z)) {
            Debug.LogError($"NetworkPlayer {Username} (ID: {PlayerId}) received invalid initial position: {pos}");
            pos = Vector3.zero; // Fallback to origin
        }
        
        transform.position = pos;
        transform.rotation = rot;
        
        targetPosition = pos;
        lastTargetPosition = pos;
        targetRotation = rot;
        lastPosition = pos;
        lastUpdateTime = Time.time;
        lastAnimationUpdateTime = Time.time;
        smoothedVelocity = Vector3.zero;
        wasMovingLastFrame = false;
        
        Debug.Log($"NetworkPlayer initialized: {Username} (ID: {PlayerId}) at {pos} with rotation {rot.eulerAngles}");
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
        
        // Add validation for position data
        if (float.IsNaN(newPosition.x) || float.IsNaN(newPosition.y) || float.IsNaN(newPosition.z)) {
            Debug.LogError($"NetworkPlayer {Username} (ID: {PlayerId}) received invalid position: {newPosition}");
            return;
        }
        
        Debug.Log($"NetworkPlayer {Username} (ID: {PlayerId}) update: pos {newPosition}, current pos {transform.position}, distance {distance:F2}");
        
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
        
        // Only update animation at limited rate to prevent jerkiness
        if (Time.time - lastAnimationUpdateTime >= 1f / animationUpdateRate) {
            UpdateAnimation();
            lastAnimationUpdateTime = Time.time;
        }
    }

    void Update()
    {
        // Validate target position before interpolation
        if (float.IsNaN(targetPosition.x) || float.IsNaN(targetPosition.y) || float.IsNaN(targetPosition.z)) {
            Debug.LogError($"NetworkPlayer {Username} (ID: {PlayerId}) has invalid target position: {targetPosition}");
            return;
        }
        
        // Interpolate position and rotation
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f)
        {
            Vector3 newPos = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
            
            // Validate interpolated position
            if (!float.IsNaN(newPos.x) && !float.IsNaN(newPos.y) && !float.IsNaN(newPos.z)) {
                transform.position = newPos;
            }
        }
        
        if (Quaternion.Angle(transform.rotation, targetRotation) > 0.1f)
        {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
        
        // Calculate movement for animation with smoothing
        Vector3 currentVelocity = (transform.position - lastPosition) / Time.deltaTime;
        
        // Validate velocity calculation
        if (float.IsNaN(currentVelocity.x) || float.IsNaN(currentVelocity.y) || float.IsNaN(currentVelocity.z)) {
            currentVelocity = Vector3.zero;
        }
        
        // Smooth the velocity using SmoothDamp for natural animation transitions
        smoothedVelocity = Vector3.SmoothDamp(smoothedVelocity, currentVelocity, ref velocitySmoothing, animationSmoothTime);
        
        isMoving = smoothedVelocity.magnitude > movementThreshold;
        lastPosition = transform.position;
        
        // Update animations in Update loop for smoother transitions
        UpdateAnimationInUpdate();
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
        
        // This method is called from network updates - just store the target movement
        // Actual animation updates happen in UpdateAnimationInUpdate() for smoother results
        Vector3 targetMovement = targetPosition - lastTargetPosition;
        
        Debug.Log($"NetworkPlayer {Username}: Network animation update - targetMovement: {targetMovement.magnitude:F2}");
        
        if (currentState != null) {
            bool isDead = currentState.HealthPoint <= 0;
            Debug.Log($"NetworkPlayer {Username}: Health: {currentState.HealthPoint}, isDead: {isDead}");
        } else {
            Debug.LogWarning($"NetworkPlayer {Username} (ID: {PlayerId}): currentState is null in UpdateAnimation");
        }
    }
    
    void UpdateAnimationInUpdate() {
        if (animator == null) return;
        
        // Use smoothed velocity for natural animation transitions
        Vector3 localVelocity = transform.InverseTransformDirection(smoothedVelocity);
        
        // Smooth transition between moving and idle states
        bool currentlyMoving = smoothedVelocity.magnitude > movementThreshold;
        
        if (!currentlyMoving) {
            // Gradually reduce animation parameters to zero for smooth idle transition
            float currentMoveX = animator.GetFloat("MoveX");
            float currentMoveZ = animator.GetFloat("MoveZ");
            
            animator.SetFloat("MoveX", Mathf.Lerp(currentMoveX, 0f, Time.deltaTime * 5f));
            animator.SetFloat("MoveZ", Mathf.Lerp(currentMoveZ, 0f, Time.deltaTime * 5f));
            animator.SetBool("Sprint", false);
        } else {
            // Smoothly update movement parameters
            float targetMoveX = Mathf.Clamp(localVelocity.x, -1f, 1f);
            float targetMoveZ = Mathf.Clamp(localVelocity.z, -1f, 1f);
            
            float currentMoveX = animator.GetFloat("MoveX");
            float currentMoveZ = animator.GetFloat("MoveZ");
            
            animator.SetFloat("MoveX", Mathf.Lerp(currentMoveX, targetMoveX, Time.deltaTime * 8f));
            animator.SetFloat("MoveZ", Mathf.Lerp(currentMoveZ, targetMoveZ, Time.deltaTime * 8f));
            
            // Sprint only if moving fast enough
            bool shouldSprint = smoothedVelocity.magnitude > 3f;
            animator.SetBool("Sprint", shouldSprint);
        }
        
        // Only reset other animation states when transitioning from moving to idle
        if (wasMovingLastFrame && !currentlyMoving) {
            animator.SetFloat("TurnValue", 0f);
            animator.SetFloat("ShootType", 0f);
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
        }
        
        wasMovingLastFrame = currentlyMoving;
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
