using System.Net;
using System.Net.Sockets;
using LiarGame.Red;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

// Menú para crear una partida (host) o unirse a una (cliente) en la red local.
public class MenuRed : MonoBehaviour
{
    [SerializeField] RedJuego red;
    [SerializeField] ushort puerto = 7777;              // el mismo en todos los equipos
    [SerializeField] string escenaMenu = "MainMenu";    // a dónde vuelve el botón Volver

    [Header("UI")]
    [SerializeField] GameObject panelMenu;       // se esconde cuando ya estás conectado
    [SerializeField] TMP_InputField campoIP;     // aquí el cliente escribe la IP del host
    [SerializeField] TMP_Text textoMiIP;         // muestra la IP de este equipo
    [SerializeField] TMP_Text textoEstado;       // "Conectando...", errores, etc.
    [SerializeField] Button botonCrear;
    [SerializeField] Button botonUnirse;

    void OnEnable()
    {
        red.AlConectarse += AlConectarse;
        red.AlDesconectarse += AlDesconectarse;
    }

    void OnDisable()
    {
        red.AlConectarse -= AlConectarse;
        red.AlDesconectarse -= AlDesconectarse;
    }

    void Start()
    {
        panelMenu.SetActive(true);
        textoEstado.text = "";
        textoMiIP.text = "Tu IP: " + ObtenerIPLocal();
    }

    // ---------- Botones ----------

    public void CrearPartida()
    {
        BotonesActivos(false);
        if (red.CrearPartida(puerto, out string error))
        {
            textoEstado.text = "Creando partida...";
        }
        else
        {
            textoEstado.text = error;
            BotonesActivos(true);
        }
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

        BotonesActivos(false);   // para no darle dos veces
        textoEstado.text = "Conectando a " + ip + "...";
        red.Unirse(ip, puerto);
    }

    public void Volver()
    {
        red.Desconectar();   // cierra la conexión (y el servidor, si éramos el host)
        SceneManager.LoadScene(escenaMenu);
    }

    // ---------- Eventos de red ----------

    void AlConectarse()
    {
        panelMenu.SetActive(false);
    }

    // No se pudo conectar, el host cerró la partida, o nos sacaron (por ejemplo, mesa llena)
    void AlDesconectarse(string motivo)
    {
        panelMenu.SetActive(true);
        BotonesActivos(true);
        textoEstado.text = motivo;
    }

    // ---------- Ayudas ----------

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
