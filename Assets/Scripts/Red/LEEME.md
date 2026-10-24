# Red de LIAR GAME (WebSockets propios, sin Netcode)

## Cómo funciona

- El **host** abre un servidor WebSocket dentro del juego (puerto 7777) y se conecta a sí mismo como un jugador más.
- Los demás jugadores se conectan a `ws://IP-del-host:7777/`.
- El servidor **no sabe del juego**: solo da un número (id) a cada conexión, avisa al host quién entra o sale, y **reenvía mensajes**.
- La **lógica del juego vive en el host** (`PartidaRed`, `MesaRed`): baraja, reparte y decide.

```
Jugador ──► servidor ──► Host        (los jugadores solo pueden hablarle al host)
Host    ──► servidor ──► Todos / uno (solo el host puede hablarle a todos o a uno en privado)
```

## Carpetas

| Carpeta | Qué hay | ¿Sabe del juego? |
|---|---|---|
| `Nucleo/Transporte` | `ServidorWebSocket` (WebSocket hecho a mano sobre TCP, RFC 6455) y `ClienteWebSocket` (usa `ClientWebSocket` de .NET) | No |
| `Nucleo/Protocolo` | `Mensaje` (el "sobre" en JSON), `Destino` y los mensajes del sistema | No |
| `Nucleo/Sesion` | `SalaServidor` (reglas de reenvío) y `RedJuego` (el componente que usa el juego) | No |
| `Juego` | `MenuRed`, `MesaRed` (sillas), `PartidaRed` (reparto), `SillasRed`, `MensajesJuego` | Sí |

`Nucleo` es un ensamblado aparte (`LiarGame.Red.asmdef`): Unity no deja que use nada del juego.
Así la red se puede reutilizar y no se mezcla con las cartas.

## Cómo agregar un mensaje nuevo (por ejemplo, "jugar carta")

1. En `Juego/MensajesJuego.cs`: agrega el tipo (`public const string JugarCarta = "juego.jugarCarta";`)
   y una clase `[Serializable]` con sus datos.
2. Quien lo **recibe** se suscribe en `OnEnable` y se desuscribe en `OnDisable`:
   `red.Escuchar(TiposJuego.JugarCarta, AlJugarCarta, Origen.Cualquiera);`
3. Quien lo **manda**: `red.EnviarAlHost(TiposJuego.JugarCarta, new DatosJugarCarta { ... });`

`Origen` dice de quién se acepta el mensaje:
- `Origen.Host`: órdenes que solo puede dar el host (estado, sillas, reparto).
- `Origen.Cualquiera`: pedidos de los jugadores; el host debe validarlos.
- `Origen.Servidor`: avisos del propio servidor (alguien entró o salió).

## Hilos

Los sockets leen en hilos de fondo. **Nunca** tocan objetos de Unity: guardan lo que llega en una cola,
y `RedJuego.Update()` la procesa en el hilo principal. Por eso tus métodos `AlRecibir...` pueden usar
cualquier cosa de Unity sin problema.
