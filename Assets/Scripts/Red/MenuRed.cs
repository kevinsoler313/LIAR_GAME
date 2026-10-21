using System.Net;
using System.Net.Sockets;
using TMPro;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

// Menú para crear una partida (host) o unirse a una (cliente) en la red local.
public class MenuRed : MonoBehaviour
{
    [SerializeField] ushort puerto = 7777;   // el mismo en todos los equipos
    [SerializeField] string escenaMenu = "MainMenu";   // a dónde vuelve el botón Volver

    [Header("UI")]
    [SerializeField] GameObject panelMenu;       // se esconde cuando ya estás conectado
    [SerializeField] TMP_InputField campoIP;     // aquí el cliente escribe la IP del host
    [SerializeField] TMP_Text textoMiIP;         // muestra la IP de este equipo
    [SerializeField] TMP_Text textoEstado;       // "Conectando...", errores, etc.
    [SerializeField] Button botonCrear;
    [SerializeField] Button botonUnirse;

    void Start()
    {
        panelMenu.SetActive(true);
        textoEstado.text = "";
        textoMiIP.text = "Tu IP: " + ObtenerIPLocal();

        // Netcode nos avisa cuando alguien se conecta o se desconecta
        NetworkManager.Singleton.OnClientConnectedCallback += AlConectarse;
        NetworkManager.Singleton.OnClientDisconnectCallback += AlDesconectarse;
    }

    void OnDestroy()
    {
        if (NetworkManager.Singleton == null) return;
        NetworkManager.Singleton.OnClientConnectedCallback -= AlConectarse;
        NetworkManager.Singleton.OnClientDisconnectCallback -= AlDesconectarse;
    }

    // ---------- Botones ----------

    public void CrearPartida()
    {
        // "0.0.0.0" = escuchar en todas las redes del equipo.
        // Si se deja por defecto (127.0.0.1) solo se podría conectar este mismo equipo.
        Transporte().SetConnectionData("127.0.0.1", puerto, "0.0.0.0");

        if (NetworkManager.Singleton.StartHost())
            textoEstado.text = "Partida creada";
        else
            textoEstado.text = "No se pudo crear la partida (¿el puerto " + puerto + " está ocupado?)";
    }

    public void UnirsePartida()
    {
        string ip = campoIP.text.Trim();
        if (ip == "") ip = "127.0.0.1";   // vacío = este mismo equipo (útil para probar)

        if (!IPAddress.TryParse(ip, out _))
        {
            textoEstado.text = "Esa IP no es válida";
            return;
        }

        Transporte().SetConnectionData(ip, puerto);

        if (NetworkManager.Singleton.StartClient())
        {
            textoEstado.text = "Conectando a " + ip + "...";
            BotonesActivos(false);   // para no darle dos veces
        }
        else
        {
            textoEstado.text = "No se pudo iniciar la conexión";
        }
    }

    public void Volver()
    {
        // si ya habíamos arrancado la red, la apagamos y borramos el NetworkManager,
        // porque vive entre escenas y al volver a entrar a Bar quedarían dos
        if (NetworkManager.Singleton != null)
        {
            NetworkManager.Singleton.Shutdown();
            Destroy(NetworkManager.Singleton.gameObject);
        }
        SceneManager.LoadScene(escenaMenu);
    }

    // ---------- Eventos de red ----------

    void AlConectarse(ulong clientId)
    {
        // solo nos importa cuando el que se conectó soy yo
        if (clientId == NetworkManager.Singleton.LocalClientId)
            panelMenu.SetActive(false);
    }

    void AlDesconectarse(ulong clientId)
    {
        // en el host esto salta cuando se va otro jugador: no hay que hacer nada aquí
        if (NetworkManager.Singleton.IsServer) return;

        // en el cliente: o no se pudo conectar, o el host cerró la partida
        panelMenu.SetActive(true);
        BotonesActivos(true);
        textoEstado.text = "Te desconectaste o no se encontró la partida";
    }

    // ---------- Ayudas ----------

    UnityTransport Transporte()
    {
        return NetworkManager.Singleton.GetComponent<UnityTransport>();
    }

    void BotonesActivos(bool activos)
    {
        botonCrear.interactable = activos;
        botonUnirse.interactable = activos;
        campoIP.interactable = activos;
    }

    // Truco: "conectar" un socket UDP no envía nada, pero hace que el sistema
    // elija la tarjeta de red que usaría, y de ahí sacamos la IP local.
    string ObtenerIPLocal()
    {
        try
        {
            using (var socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                return ((IPEndPoint)socket.LocalEndPoint).Address.ToString();
            }
        }
        catch
        {
            // sin internet: buscamos la primera IPv4 que no sea 127.0.0.1
            foreach (var ip in Dns.GetHostEntry(Dns.GetHostName()).AddressList)
                if (ip.AddressFamily == AddressFamily.InterNetwork && !IPAddress.IsLoopback(ip))
                    return ip.ToString();
            return "desconocida";
        }
    }
}
