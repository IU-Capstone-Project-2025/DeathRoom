using DeathRoom.Network;
using UnityEngine;

public class NetworkPlayer : MonoBehaviour {
    [Header("Interpolation Settings")]
    public float interpolationSpeed = 10f;
    public float maxDistance = 1f; // Максимальное расстояние для телепортации вместо интерполяции
    
    [Header("Components")]
    public Animator animator;
    
    private PlayerState currentState;
    private Vector3 targetPosition;
    private Quaternion targetRotation;
    private Vector3 lastPosition;
    private bool isMoving = false;
    
    public string Username { get; private set; }
    public int PlayerId { get; private set; }
    
    void Start() {
        // Получаем аниматор если не назначен
        if (animator == null) {
            animator = GetComponent<Animator>();
        }
        
        // Отключаем компоненты локального игрока
        DisableLocalPlayerComponents();
    }
    
    void DisableLocalPlayerComponents() {
        // Отключаем компоненты, которые должны работать только для локального игрока
        var playerMovement = GetComponent<PlayerMovement>();
        if (playerMovement != null) {
            playerMovement.enabled = false;
        }
        
        var characterController = GetComponent<CharacterController>();
        if (characterController != null) {
            characterController.enabled = false;
        }
        
        // Отключаем камеру
        var cameras = GetComponentsInChildren<Camera>();
        foreach (var cam in cameras) {
            cam.enabled = false;
        }
        
        // Отключаем аудио слушатели
        var audioListeners = GetComponentsInChildren<AudioListener>();
        foreach (var listener in audioListeners) {
            listener.enabled = false;
        }
    }
    
    public void Initialize(PlayerState playerState) {
        currentState = playerState;
        Username = playerState.Username;
        PlayerId = playerState.PlayerId;
        
        // Сразу устанавливаем позицию без интерполяции
        var pos = new Vector3(playerState.Position.X, playerState.Position.Y, playerState.Position.Z);
        var rot = Quaternion.Euler(playerState.Rotation.X, playerState.Rotation.Y, playerState.Rotation.Z);
        
        transform.position = pos;
        transform.rotation = rot;
        
        targetPosition = pos;
        targetRotation = rot;
        lastPosition = pos;
        
        Debug.Log($"NetworkPlayer initialized: {Username} at {pos}");
    }
    
    public void UpdateState(PlayerState newState) {
        if (newState == null) return;
        
        currentState = newState;
        
        Vector3 newPosition = new Vector3(newState.Position.X, newState.Position.Y, newState.Position.Z);
        Quaternion newRotation = Quaternion.Euler(newState.Rotation.X, newState.Rotation.Y, newState.Rotation.Z);
        
        // Проверяем, нужно ли телепортировать игрока (слишком большая дистанция)
        float distance = Vector3.Distance(transform.position, newPosition);
        if (distance > maxDistance) {
            // Телепортируем сразу
            transform.position = newPosition;
            transform.rotation = newRotation;
            targetPosition = newPosition;
            targetRotation = newRotation;
        } else {
            // Плавная интерполяция
            targetPosition = newPosition;
            targetRotation = newRotation;
        }
        
        // Обновляем анимацию
        UpdateAnimation();
    }
    
    void Update() {
        // Интерполяция позиции и поворота
        if (Vector3.Distance(transform.position, targetPosition) > 0.01f) {
            transform.position = Vector3.Lerp(transform.position, targetPosition, interpolationSpeed * Time.deltaTime);
        }
        
        if (Quaternion.Angle(transform.rotation, targetRotation) > 1f) {
            transform.rotation = Quaternion.Lerp(transform.rotation, targetRotation, interpolationSpeed * Time.deltaTime);
        }
        
        // Проверяем движение
        float velocity = Vector3.Distance(lastPosition, transform.position) / Time.deltaTime;
        isMoving = velocity > 0.1f;
        lastPosition = transform.position;
    }
    
    void UpdateAnimation() {
        if (animator == null) return;
        
        // Базовые параметры анимации
        Vector3 velocity = (targetPosition - transform.position) / Time.deltaTime;
        Vector3 localVelocity = transform.InverseTransformDirection(velocity);
        
        // Устанавливаем параметры аниматора
        animator.SetFloat("MoveX", localVelocity.x);
        animator.SetFloat("MoveZ", localVelocity.z);
        animator.SetBool("Sprint", isMoving && velocity.magnitude > 3f);
        
        // Здоровье игрока для анимации смерти
        if (currentState != null) {
            bool isDead = currentState.Health <= 0;
            animator.SetBool("Dead", isDead);
        }
    }
    
    void OnDestroy() {
        Debug.Log($"NetworkPlayer destroyed: {Username}");
    }
}