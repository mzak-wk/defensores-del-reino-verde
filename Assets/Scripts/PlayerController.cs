using UnityEngine;

public class PlayerController : MonoBehaviour
{
    public float velocidad = 5f;
    public Animator animator;

    // Como tu sprite por defecto mira a la IZQUIERDA,
    // arrancamos con mirandoDerecha = false
    private bool mirandoDerecha = false;

    void Update()
    {
        // Leemos ambos ejes de movimiento
        float inputX = Input.GetAxis("Horizontal");
        float inputY = Input.GetAxis("Vertical");

        // Vector de movimiento
        Vector3 movimiento = new Vector3(inputX, inputY, 0f);

        // ---------- ANIMACIÓN ----------
        float movementValue = movimiento.magnitude;
        animator.SetFloat("movement", movementValue);

        // ---------- MOVIMIENTO ----------
        transform.position += movimiento * velocidad * Time.deltaTime;

        // ---------- FLIP HORIZONTAL ----------
        ManejarFlip(inputX);
    }

    void ManejarFlip(float inputX)
    {
        // Si no se mueve en X, no cambiamos orientación
        if (inputX > 0 && !mirandoDerecha)
        {
            Girar();
        }
        else if (inputX < 0 && mirandoDerecha)
        {
            Girar();
        }
    }

    void Girar()
    {
        mirandoDerecha = !mirandoDerecha;

        Vector3 escala = transform.localScale;
        escala.x *= -1;
        transform.localScale = escala;
    }
}
