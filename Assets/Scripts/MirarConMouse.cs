using UnityEngine;
using UnityEngine.InputSystem;   // el Input System nuevo (para leer el mouse)

public class MirarConMouse : MonoBehaviour
{
    [SerializeField] float sensibilidad = 0.15f;      // grados por cada píxel que se mueve el mouse
    [SerializeField] float limiteHorizontal = 100f;   // cuánto puedo girar a cada lado
    [SerializeField] float limiteArriba = -10f;       // pitch mínimo (mirar arriba)
    [SerializeField] float limiteAbajo = 60f;         // pitch máximo (mirar abajo)

    float giroHorizontal;   // yaw acumulado
    float giroVertical;     // pitch acumulado

        void Start()
    {
        giroVertical = transform.localEulerAngles.x;   // arranca con los 30° que le pusiste
        giroHorizontal = 0f;                            // mirando al frente
    }

        void Update()
    {
        if (Mouse.current == null) return;                  // no hay mouse conectado
        if (!Mouse.current.rightButton.isPressed) return;   // solo gira con clic derecho

        Vector2 delta = Mouse.current.delta.ReadValue();    // cuánto se movió el mouse este frame

        giroHorizontal += delta.x * sensibilidad;
        giroVertical   -= delta.y * sensibilidad;           // restar: mouse arriba = mirar arriba

        giroHorizontal = Mathf.Clamp(giroHorizontal, -limiteHorizontal, limiteHorizontal);
        giroVertical   = Mathf.Clamp(giroVertical, limiteArriba, limiteAbajo);

        transform.localRotation = Quaternion.Euler(giroVertical, giroHorizontal, 0f);
    }

    // Al cambiar de silla: vuelve a mirar al frente desde la inclinación nueva
    public void Recentrar()
    {
        giroVertical = transform.localEulerAngles.x;
        giroHorizontal = 0f;
    }
}
