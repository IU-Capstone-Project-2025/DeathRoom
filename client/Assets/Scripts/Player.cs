using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class Player : MonoBehaviour
{
    [Header("References")]
    public Camera playerCam;
    public GameObject gunSpawnPoint;

    [Header("Movement Settings")]
    public float walkSpeed = 5f;
    public float sprintSpeed = 8f;
    public float groundDrag = 5f;
    public float airMultiplier = 0.5f;
    public float jumpForce = 5f;

    [Header("Mouse Look Settings")]
    public float mouseSensitivity = 100f;
    public float minVerticalAngle = -90f;
    public float maxVerticalAngle = 90f;

    [Header("Advanced Physics")]
    public float maxSpeed = 10f;
    public float slopeForce = 10f;
    public float slopeRayLength = 1.5f;

    private Rigidbody rb;
    private float xRotation;
    private Vector3 moveDirection;
    private bool isGrounded;
    private Gun currentGun;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.freezeRotation = true;
        Cursor.lockState = CursorLockMode.Locked;
    }

    void Update()
    {
        HandleMouseLook();
        HandleJump();
        ControlSpeed();

        if (Input.GetKeyDown(KeyCode.E) && currentGun != null)
        {
            currentGun.Shoot();
        }

        GroundCheck();
    }

    void FixedUpdate()
    {
        HandleMovement();
    }

    void HandleMovement()
    {
        float x = Input.GetAxisRaw("Horizontal");
        float z = Input.GetAxisRaw("Vertical");

        moveDirection = transform.right * x + transform.forward * z;
        moveDirection.Normalize();

        float currentSpeed = Input.GetKey(KeyCode.LeftShift) ? sprintSpeed : walkSpeed;
        float forceMultiplier = isGrounded ? 10f : 10f * airMultiplier;

        if (rb.linearVelocity.magnitude < maxSpeed)
        {
            rb.AddForce(moveDirection * currentSpeed * forceMultiplier, ForceMode.Force);
        }

        if (OnSlope())
        {
            rb.AddForce(Vector3.down * slopeForce, ForceMode.Force);
        }
    }

    void HandleMouseLook()
    {
        float mouseX = Input.GetAxis("Mouse X") * mouseSensitivity * Time.deltaTime;
        float mouseY = Input.GetAxis("Mouse Y") * mouseSensitivity * Time.deltaTime;

        xRotation -= mouseY;
        xRotation = Mathf.Clamp(xRotation, minVerticalAngle, maxVerticalAngle);

        playerCam.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        transform.Rotate(Vector3.up * mouseX);
    }

    void HandleJump()
    {
        if (Input.GetButtonDown("Jump") && isGrounded)
        {
            rb.linearVelocity = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            rb.AddForce(transform.up * jumpForce, ForceMode.Impulse);
        }
    }

    void ControlSpeed()
    {
        if (isGrounded && rb.linearVelocity.magnitude > walkSpeed)
        {
            Vector3 flatVel = new Vector3(rb.linearVelocity.x, 0f, rb.linearVelocity.z);
            Vector3 limitedVel = flatVel.normalized * walkSpeed;
            rb.linearVelocity = new Vector3(limitedVel.x, rb.linearVelocity.y, limitedVel.z);
        }

        rb.linearDamping = isGrounded ? groundDrag : 0f;
    }

    void GroundCheck()
    {
        isGrounded = Physics.Raycast(transform.position, Vector3.down, slopeRayLength);
    }

    bool OnSlope()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hit, slopeRayLength))
        {
            return hit.normal != Vector3.up;
        }
        return false;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Weapon"))
        {
            other.gameObject.transform.position = gunSpawnPoint.transform.position;
            other.transform.SetParent(gunSpawnPoint.transform);
            currentGun = other.GetComponent<Gun>();
        }
    }

    void OnDrawGizmos()
    {
        Gizmos.color = Color.red;
        Gizmos.DrawLine(transform.position, transform.position + Vector3.down * slopeRayLength);
    }
}