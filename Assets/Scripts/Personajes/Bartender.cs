using UnityEngine;

// El bartender detrás de la barra: va cambiando entre limpiar un vaso, limpiar la barra y esperar apoyado,
// cada cosa un rato distinto. Saluda de vez en cuando y cuando el mesero llega a la barra (Mesero lo llama).
// Es solo ambiente: no tiene nada que ver con la partida ni con la red.
public class Bartender : MonoBehaviour
{
    [SerializeField] Animator animador;
    [SerializeField] Vector2 segundosPorTarea = new Vector2(5f, 10f);
    [SerializeField, Range(0f, 1f)] float probabilidadSaludo = 0.15f;   // al cambiar de tarea

    static readonly string[] tareas = { "Limpiar_Vaso", "Limpiar_Barra", "Esperar" };
    const string saludo = "Saludar";

    int tarea = -1;
    float finTarea;
    bool saludando;

    void Start()
    {
        CambiarTarea(0.25f);
        // cada partida arranca en otro punto del ciclo
        animador.Play(tareas[tarea], 0, Random.value);
    }

    void Update()
    {
        if (saludando)
        {
            var estado = animador.GetCurrentAnimatorStateInfo(0);
            if (estado.IsName(saludo) && estado.normalizedTime >= 0.95f)
            {
                saludando = false;
                CambiarTarea(0.3f);
            }
            return;
        }
        if (Time.time < finTarea) return;
        if (Random.value < probabilidadSaludo) Saludar();
        else CambiarTarea(0.4f);
    }

    public void Saludar()
    {
        if (saludando) return;
        saludando = true;
        animador.CrossFadeInFixedTime(saludo, 0.25f);
    }

    void CambiarTarea(float transicion)
    {
        int nueva = Random.Range(0, tareas.Length);
        if (nueva == tarea) nueva = (nueva + 1) % tareas.Length;
        tarea = nueva;
        finTarea = Time.time + Random.Range(segundosPorTarea.x, segundosPorTarea.y);
        animador.CrossFadeInFixedTime(tareas[tarea], transicion);
    }
}
