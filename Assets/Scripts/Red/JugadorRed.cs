using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// Va en el prefab "player". Netcode crea uno por cada equipo conectado, y existe en TODAS las copias del juego:
// en mi PC hay 4 JugadorRed (el mío y los de los demás), pero solo uno es mío (IsOwner).
// El host le asigna una silla libre; cada copia coloca ese cuerpo en su silla,
// y si es el mío, además sienta mi cámara ahí.
public class JugadorRed : NetworkBehaviour
{
    // -1 = todavía sin silla. Solo el host (servidor) la puede escribir; todos la leen.
    public NetworkVariable<int> silla = new NetworkVariable<int>(-1);

    [SerializeField] Vector3 ajusteSentado = new Vector3(0f, 1.15f, 0f);   // dónde queda el cuerpo respecto al puesto

    // Todos los jugadores que existen en esta copia del juego (para saber qué sillas están ocupadas)
    static readonly List<JugadorRed> todos = new List<JugadorRed>();

    public override void OnNetworkSpawn()
    {
        todos.Add(this);

        if (IsServer)
        {
            silla.Value = BuscarSillaLibre();
            if (silla.Value < 0)
            {
                // ya hay 4: lo sacamos de la partida
                NetworkManager.DisconnectClient(OwnerClientId, "La mesa está llena");
                return;
            }
        }

        // Los que se conectan después reciben el valor ya puesto (sin que salte OnValueChanged),
        // por eso lo colocamos aquí directamente, y además escuchamos por si cambia más adelante.
        if (silla.Value >= 0) Colocar(silla.Value);
        silla.OnValueChanged += AlCambiarSilla;
    }

    public override void OnNetworkDespawn()
    {
        todos.Remove(this);   // su silla queda libre para el siguiente que entre
        silla.OnValueChanged -= AlCambiarSilla;
    }

    void AlCambiarSilla(int anterior, int nueva)
    {
        if (nueva >= 0) Colocar(nueva);
    }

    int BuscarSillaLibre()
    {
        var ocupadas = new bool[SillasRed.Instancia.Cantidad];
        foreach (var j in todos)
            if (j != this && j.silla.Value >= 0) ocupadas[j.silla.Value] = true;

        for (int i = 0; i < ocupadas.Length; i++)
            if (!ocupadas[i]) return i;
        return -1;
    }

    void Colocar(int s)
    {
        var sillas = SillasRed.Instancia;

        // el cuerpo se sienta en su silla (esto lo hace cada copia por su cuenta, por eso no hace falta NetworkTransform)
        transform.SetPositionAndRotation(sillas.PosicionSilla(s) + ajusteSentado, sillas.RotacionSilla(s));
        name = "Jugador silla " + s + (IsOwner ? " (yo)" : "");

        if (!IsOwner) return;

        // Solo para MI jugador: mi cámara y mi mano van a esta silla
        sillas.Sentar(s);

        // mi propio cuerpo me taparía la vista: en mi pantalla no lo dibujo (los demás sí lo ven)
        foreach (var r in GetComponentsInChildren<Renderer>()) r.enabled = false;
    }
}
