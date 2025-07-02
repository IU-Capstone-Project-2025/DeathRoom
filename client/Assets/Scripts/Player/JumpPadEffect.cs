using UnityEngine;

public class JumpPadEffect : MonoBehaviour
{
    private Vector3 velocity;
    private float duration;
    private float timer;
    private CharacterController controller;

    public void Initialize(Vector3 velocity, float duration, CharacterController controller)
    {
        this.velocity = velocity;
        this.duration = duration;
        this.controller = controller;
        timer = 0f;
    }

    private void Update()
    {
        if (timer < duration)
        {
            controller.Move(velocity * Time.deltaTime);
            timer += Time.deltaTime;
        }
        else
        {
            Destroy(this); 
        }
    }
}
