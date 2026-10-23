using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// El cerebro de la partida en red. Hay una copia en cada equipo,
// pero solo el host (IsServer) toma decisiones: baraja, reparte, valida jugadas.
public class PartidaRed : NetworkBehaviour
{
    [SerializeField] Baraja baraja;
    [SerializeField] int cartasPorJugador = 8;     // el mismo número que tiene tu Repartidor
    [SerializeField] GameObject botonEmpezar;
    [SerializeField] MazoMesa mazoMesa;
    [SerializeField] Repartidor repartidor;

    // --- PÚBLICO: todos lo ven ---
    public NetworkVariable<bool> enJuego = new NetworkVariable<bool>(false);
    public NetworkVariable<int> turno = new NetworkVariable<int>(-1);   // lo usaremos en el 6.4

    // --- SECRETO: solo tiene datos en el host ---
    readonly List<int>[] manos = { new List<int>(), new List<int>(), new List<int>(), new List<int>() };
    readonly List<int> monto = new List<int>();
    int[] cartasPorRevelar;  
    // ---------- Ciclo de red ----------

    public override void OnNetworkSpawn()
    {
        enJuego.OnValueChanged += AlCambiarEnJuego;
    }

    public override void OnNetworkDespawn()
    {
        enJuego.OnValueChanged -= AlCambiarEnJuego;
    }

    void AlCambiarEnJuego(bool antes, bool ahora)
    {
        Debug.Log("[PartidaRed] ¿En juego? " + ahora + (IsServer ? " (host)" : " (cliente)"));
    }

    // ---------- Botón EMPEZAR ----------

    void Update()
    {
        if (botonEmpezar == null) return;

        bool mostrar = IsSpawned                                       // la red ya arrancó
                    && IsServer                                        // soy el host
                    && !enJuego.Value                                  // no ha empezado
                    && NetworkManager.ConnectedClientsIds.Count >= 2;  // hay con quién jugar

        if (botonEmpezar.activeSelf != mostrar)
            botonEmpezar.SetActive(mostrar);
    }

    // Lo llama el botón EMPEZAR (solo existe en el host).
    public void Empezar()
    {
        if (!IsServer || enJuego.Value) return;   // nadie más puede empezar, y no dos veces

        var ocupadas = SillasOcupadas();
        var mazo = baraja.MezclarIds();           // del 6.1: todo el mazo en números

        foreach (var m in manos) m.Clear();
        monto.Clear();

        // Igual que tu Repartidor: una carta a cada silla ocupada, ronda por ronda
        for (int ronda = 0; ronda < cartasPorJugador; ronda++)
            for (int s = 0; s < 4; s++)
            {
                if (!ocupadas[s] || mazo.Count == 0) continue;
                int arriba = mazo.Count - 1;      // se saca la de arriba, como hace MazoMesa.Robar()
                manos[s].Add(mazo[arriba]);
                mazo.RemoveAt(arriba);
            }

        enJuego.Value = true;                     // esto SÍ viaja: todos se enteran

        // Para comprobar: solo el host puede imprimir esto, porque solo él lo sabe
        for (int s = 0; s < 4; s++)
        {
            if (!ocupadas[s]) continue;
            string texto = "";
            foreach (int id in manos[s]) texto += baraja.PorId(id) + " ";
            Debug.Log("[PartidaRed] Silla " + s + ": " + texto);
        }
        Repartir();
    }
    public void Repartir(){
        
        foreach (var cliente in NetworkManager.ConnectedClientsList)
        {
            var jugador = cliente.PlayerObject.GetComponent<JugadorRed>();
            var silla = jugador.silla.Value;
            var mano = manos[silla].ToArray();
            RecibirRepartoRpc(mano, SillasOcupadas(), RpcTarget.Single(cliente.ClientId, RpcTargetUse.Temp));
        }
    }

    [Rpc(SendTo.SpecifiedInParams)]
    public void RecibirRepartoRpc(int[] mano, bool[] ocupadas,RpcParams rpcParamas){
        cartasPorRevelar = mano;
        mazoMesa.ConstruirOculto();
        repartidor.FijarActivos(ocupadas);
        repartidor.antesDeTerminar.AddListener(RevelarMisCartas);
        repartidor.Repartir();
    }

    void RevelarMisCartas(){
        repartidor.antesDeTerminar.RemoveListener(RevelarMisCartas);

        int miSilla = SillasRed.Instancia.SillaLocal;
        var misCartas = repartidor.Mano(miSilla);
        int cuantas = Mathf.Min(misCartas.Count, cartasPorRevelar.Length);
        for(int i=0;i<cuantas;i++){
            misCartas[i].Revelar(baraja.PorId(cartasPorRevelar[i]));
        }
    }
    // ---------- Ayudas ----------

    // Solo funciona en el host: revisa los jugadores conectados y marca sus sillas.
    bool[] SillasOcupadas()
    {
        var ocupadas = new bool[4];
        foreach (var cliente in NetworkManager.ConnectedClientsList)
        {
            var jugador = cliente.PlayerObject.GetComponent<JugadorRed>();
            if (jugador.silla.Value >= 0)
                ocupadas[jugador.silla.Value] = true;
        }
        return ocupadas;
    }
}
