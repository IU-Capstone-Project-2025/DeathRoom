using UnityEngine;

public class JumpPad : MonoBehaviour
{
    public float jumpForce = 10f;                // Сила прыжка
    public Vector3 direction = Vector3.up;       // Направление прыжка (по умолчанию вверх)
    public float duration = 0.2f;                // Время действия импульса

    private void OnTriggerEnter(Collider other)
    {
        CharacterController controller = other.GetComponent<CharacterController>();
        if (controller != null)
        {
            Debug.Log("started applying jumpad effect");
            JumpPadEffect effect = other.gameObject.AddComponent<JumpPadEffect>();
            effect.Initialize(direction.normalized * jumpForce, duration, controller);
        }
    }
}
