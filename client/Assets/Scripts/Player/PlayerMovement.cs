using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Animations.Rigging;

public class PlayerMovement : MonoBehaviour
{
    public RectTransform PauseMenu;
    public RectTransform leaderBoard;
    public Client client;
    [Range(1f, 10f)] public float mouseSensitive = 3;
    [Range(-180f, 180f)] public float minCameraRotY = -60f;
    [Range(-180f, 180f)] public float maxCameraRotY = 40f;
    private Transform Hcamera;
    private Transform RayForShooting;
    private float rotationY = 20f;

    public float walkSpeed = 3f;
    public float runSpeed = 5f;
    public float crouchSpeed = 2f;
    private float speed;
    public float jumpPower = 3f;

    private CharacterController controller;
    private Vector3 moveDirection = Vector3.zero;
    private Vector3 movement = Vector3.zero;
    public Animator animator;

    public Transform cameraPack;
    public Rig RHandRig;
    public Rig WeaponRig;
    public Rig LHandRig;

    private float gravity = Physics.gravity.y;
    private Vector3 oldPos;
    private bool crouch = true;
    private bool freezMovement = false;
    private float radius;
    private float height;
    private bool isReload = false;
    private bool jumpOver = false;
    public Gun usingGun;

    private Dictionary<string, object> animationParams = new Dictionary<string, object>();
    private Dictionary<string, object> changedAnimationParams = new Dictionary<string, object>();
    private float animationSendInterval = 0.1f;
    private float animationSendTimer = 0f;

    void Awake()
    {
        controller = GetComponent<CharacterController>();
        Hcamera = cameraPack.GetChild(0);
        Cursor.visible = false;
        radius = controller.radius;
        height = controller.height;

        foreach (AnimatorControllerParameter param in animator.parameters)
        {
            switch (param.type)
            {
                case AnimatorControllerParameterType.Bool:
                    animationParams[param.name] = animator.GetBool(param.name);
                    break;
                case AnimatorControllerParameterType.Float:
                    animationParams[param.name] = animator.GetFloat(param.name);
                    break;
                case AnimatorControllerParameterType.Int:
                    animationParams[param.name] = animator.GetInteger(param.name);
                    break;
            }
        }
    }


    void Start()
    {
        speed = walkSpeed;
        oldPos = new Vector3(transform.position.x, 0f, transform.position.z);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            takeBreak();
        }
    }

    private void takeBreak()
    {
        Time.timeScale = 0f;
        PauseMenu.gameObject.SetActive(true);
        //leaderBoard.gameObject.SetActive(true);
    }

    private void SetBool(string name, bool value)
    {
        if (!animationParams.ContainsKey(name) || (bool)animationParams[name] != value)
        {
            animationParams[name] = value;
            changedAnimationParams[name] = value;
            animator.SetBool(name, value);
        }
    }

    private void SetFloat(string name, float value)
    {
        if (!animationParams.ContainsKey(name) || Mathf.Abs((float)animationParams[name] - value) > 0.01f)
        {
            animationParams[name] = value;
            changedAnimationParams[name] = value;
            animator.SetFloat(name, value);
        }
    }

    private void SetInt(string name, int value)
    {
        if (!animationParams.ContainsKey(name) || (int)animationParams[name] != value)
        {
            animationParams[name] = value;
            changedAnimationParams[name] = value;
            animator.SetInteger(name, value);
        }
    }


    public void resume()
    {
        Time.timeScale = 1f;
        Cursor.visible = false;
        PauseMenu.gameObject.SetActive(false);
        //leaderBoard.gameObject.SetActive(false);
    }

    void LateUpdate()
    {
        if (!freezMovement)
        {
            Movement();
            AnimatorSystem();

            if (!isReload)
            {
                if (Input.GetMouseButton(0) && usingGun.CheckAmo() && !isReload)
                {
                    Debug.Log("Shooting!");
                    usingGun.Shoot();

                    // Visualize the ray in the editor with longer duration for debugging
                    Debug.DrawRay(usingGun.shootOut.position, usingGun.shootOut.forward * 100f, Color.red, 2f);
                    
                    // Log ray origin and direction for debugging
                    Debug.Log($"Ray Origin: {usingGun.shootOut.position}, Direction: {usingGun.shootOut.forward}");

                    RaycastHit hit;
                    int layerMask = ~0; // All layers
                    if (Physics.Raycast(usingGun.shootOut.position, usingGun.shootOut.forward, out hit, 100f, layerMask))
                    {
                        Debug.Log($"Hit: {hit.collider.name} at distance {hit.distance}");
                        var networkPlayer = hit.collider.GetComponent<NetworkPlayer>();
                        if (networkPlayer != null)
                        {
                            Debug.Log("Hit player: " + networkPlayer.PlayerId);
                            client.PerformShoot(usingGun.shootOut.position, usingGun.shootOut.forward);
                        }
                        else
                        {
                            Debug.Log($"Hit non-player object: {hit.collider.gameObject.layer}");
                            client.PerformShoot(usingGun.shootOut.position, usingGun.shootOut.forward);
                        }
                    }
                    else
                    {
                        Debug.LogWarning("Ray missed! Check if objects have colliders and are in the right layers");
                        client.PerformShoot(usingGun.shootOut.position, usingGun.shootOut.forward);
                    }
                }
            }
        }

        GravitySystem();
        CameraPack();

        controller.Move(new Vector3(moveDirection.x, gravity, moveDirection.z) * Time.deltaTime);

        animationSendTimer += Time.deltaTime;
        if (animationSendTimer >= animationSendInterval && changedAnimationParams.Count > 0)
        {
            SendAnimationParameters(changedAnimationParams);
            changedAnimationParams.Clear();
            animationSendTimer = 0f;
        }
    }

    void Movement()
    {
        if (controller.isGrounded)
        {
            movement = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical")).normalized;
            moveDirection = Quaternion.AngleAxis(transform.eulerAngles.y, Vector3.up) * movement * speed;
        }

        transform.Rotate(0f, Input.GetAxis("Mouse X") * mouseSensitive * 100f * Time.deltaTime, 0f);

        if (Input.GetMouseButton(1))
        {
            crouch = true;
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.C))
            {
                crouch = !crouch;
            }
        }

        if (Input.GetKey(KeyCode.LeftShift) && movement != Vector3.zero)
        {
            if (crouch)
            {
                speed = Mathf.Lerp(speed, walkSpeed, Time.deltaTime * 5f);
            }
            else
            {
                speed = Mathf.Lerp(speed, runSpeed, Time.deltaTime * 5f);
            }
        }
        else if (movement == Vector3.zero)
        {
            speed = Mathf.Lerp(speed, 0f, Time.deltaTime * 5f);
        }
        else
        {
            if (crouch)
            {
                speed = Mathf.Lerp(speed, crouchSpeed, Time.deltaTime * 5f);
            }
            else
            {
                speed = Mathf.Lerp(speed, walkSpeed, Time.deltaTime * 5f);
            }
        }
    }

    void GravitySystem()
    {
        if (controller.isGrounded)
        {
            gravity = Physics.gravity.y;

            if (Input.GetButton("Jump"))
            {
                gravity = jumpPower;
            }
        }
        else
        {
            gravity += Physics.gravity.y * Time.deltaTime;
        }
    }

    void CameraPack()
    {
        cameraPack.position = Vector3.Lerp(cameraPack.position, transform.position, Time.deltaTime * 20f);
        cameraPack.rotation = transform.rotation;
        rotationY -= Input.GetAxis("Mouse Y") * mouseSensitive * 100f * Time.deltaTime;
        rotationY = Mathf.Clamp(rotationY, minCameraRotY, maxCameraRotY);
        Hcamera.rotation = Quaternion.Euler(rotationY, Hcamera.eulerAngles.y, Hcamera.eulerAngles.z);
    }

    void AnimatorSystem()
    {
        Vector3 animMove = new Vector3(Input.GetAxis("Horizontal"), 0f, Input.GetAxis("Vertical"));
        if (crouch)
        {
            animMove = new Vector3(animMove.x / 2f, 0f, animMove.z / 2f);
        }

        float velocity = (oldPos - new Vector3(transform.position.x, 0f, transform.position.z)).magnitude * 100f;
        if (velocity < 0.5f)
        {
            SetBool("Sprint", false);
        }
        else
        {
            SetBool("Sprint", (Input.GetKey(KeyCode.LeftShift) && movement != Vector3.zero));
        }

        velocity = Mathf.Clamp(velocity, 0f, 1f);

        oldPos = new Vector3(transform.position.x, 0f, transform.position.z);
        SetFloat("MoveX", Mathf.Lerp(animator.GetFloat("MoveX"), animMove.x, Time.deltaTime * 20f));
        SetFloat("MoveZ", Mathf.Lerp(animator.GetFloat("MoveZ"), animMove.z, Time.deltaTime * 20f));
        float turnValue = Input.GetAxis("Mouse X") * 3f;
        turnValue = Mathf.Clamp(turnValue, -1f, 1f);
        SetFloat("TurnValue", Mathf.Lerp(animator.GetFloat("TurnValue"), Input.GetAxis("Mouse X") * 2f, Time.deltaTime * 3f));

        if (!isReload)
        {
            RHandRig.weight = Mathf.Lerp(RHandRig.weight,
                (Input.GetKey(KeyCode.LeftShift) && movement != Vector3.zero) ? 0f : 1f, Time.deltaTime * 10f);
            WeaponRig.weight = Mathf.Lerp(WeaponRig.weight,
                (Input.GetKey(KeyCode.LeftShift) && movement != Vector3.zero) ? 0f : 1f, Time.deltaTime * 10f);
            LHandRig.weight = Mathf.Lerp(LHandRig.weight, 1f, Time.deltaTime * 10f);
        }

        SetBool("OnAir", !controller.isGrounded);
        SetBool("Shoot", Input.GetMouseButton(0) && usingGun.CheckAmo() && !isReload);
        SetBool("Aim", Input.GetMouseButton(1));
        SetFloat("ShootType", Mathf.Lerp(animator.GetFloat("ShootType"), Input.GetMouseButton(1) ? 1f : 0f, Time.deltaTime * 5f));

        if (Input.GetKeyDown(KeyCode.R) && !isReload && usingGun.GetAllAmo() > 0)
        {
            StartCoroutine(Reload(2.4f / 1.2f));
        }

        if (Input.GetKeyDown(KeyCode.Q) && !isReload)
        {
            StartCoroutine(ChangeWeapon(1.9f / 2f));
        }

        if (animator.GetCurrentAnimatorStateInfo(0).IsName("Reload") &&
            animator.GetCurrentAnimatorStateInfo(0).normalizedTime >= 0.98f)
        {
            isReload = false;
            SetBool("Reload", false);
        }
    }

    IEnumerator Reload(float duration)
    {
        animator.SetTrigger("Reload");
        usingGun.StartReload();
        isReload = true;
        RHandRig.weight = 0f;
        WeaponRig.weight = 0f;
        LHandRig.weight = 0f;
        yield return new WaitForSeconds(duration);
        isReload = false;
        usingGun.EndReload();
    }

    IEnumerator ChangeWeapon(float duration)
    {
        animator.SetTrigger("ChangeWeapon");
        isReload = true;
        RHandRig.weight = 0f;
        WeaponRig.weight = 0f;
        LHandRig.weight = 0f;
        yield return new WaitForSeconds(duration);
        isReload = false;
    }

    public void LaunchUpward(float force)
    {
        gravity = force;
    }
     
    private void SendAnimationParameters(Dictionary<string, object> changedParams)
    {
        if (client != null && changedParams.Count > 0)
        {
            client.SendAnimationUpdate(changedParams);
        }

    }
}