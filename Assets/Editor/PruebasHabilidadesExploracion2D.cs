using System;
using System.Reflection;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasHabilidadesExploracion2D
{
    [MenuItem("Demo/Validar habilidad de exploracion: Dash")]
    public static void Ejecutar()
    {
        ProbarDesbloqueoYMovimiento();
        ProbarPasoProtegido();
        Debug.Log("Pruebas de Dash correctas: desbloqueo, movimiento, combate y barrera protegida.");
    }

    private static void ProbarDesbloqueoYMovimiento()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Exigir(!jugador.DashDesbloqueado, "El Dash debe comenzar bloqueado en la escena de progresion.");
            Exigir(jugador.DesbloquearHabilidad(HabilidadExploracion2D.Dash),
                "La recogida debe desbloquear el Dash mediante el catalogo compartido.");
            Exigir(jugador.DashDesbloqueado &&
                   jugador.TieneHabilidad(HabilidadExploracion2D.Dash),
                "El movimiento debe consultar el mismo estado desbloqueado.");
            Exigir(!jugador.DesbloquearHabilidad(HabilidadExploracion2D.Dash),
                "La recogida repetida no debe conceder un segundo desbloqueo.");
            Exigir(!jugador.DesbloquearDash(),
                "El metodo previo de desbloqueo debe seguir siendo idempotente.");

            Establecer(jugador, "entradaMovimiento", Vector2.right);
            Invocar(jugador, "IniciarDash");
            Exigir(jugador.EstaEnDash && jugador.DireccionDash == Vector2.right,
                "El Dash horizontal debe usar la entrada de movimiento.");
            Exigir(Mathf.Approximately(jugadorObjeto.GetComponent<Rigidbody2D>().linearVelocity.x,
                    jugador.velocidadDash),
                "El movimiento del Dash debe aplicar su velocidad configurada.");
            Exigir(!jugadorObjeto.GetComponent<VidaJugador2D>().RecibirDanio(1, Vector2.left),
                "El jugador debe conservar la proteccion de daño durante el Dash.");

            Invocar(jugador, "Atacar");
            Exigir(Mathf.Approximately((float)Leer(jugador, "ataqueTerminaEn"), 0f),
                "El jugador no debe iniciar un ataque durante el Dash.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(jugadorObjeto);
            UnityEngine.Object.DestroyImmediate(acciones);
        }
    }

    private static void ProbarPasoProtegido()
    {
        InputActionAsset acciones = CrearAcciones();
        GameObject jugadorObjeto = CrearJugador(acciones);
        GameObject puertaObjeto = CrearPuerta(out PuertaDash2D puerta);
        try
        {
            Jugador2D jugador = jugadorObjeto.GetComponent<Jugador2D>();
            Collider2D colliderJugador = jugador.GetComponent<Collider2D>();
            Invocar(puerta, "OnTriggerEnter2D", colliderJugador);
            Invocar(puerta, "OnTriggerStay2D", colliderJugador);
            Exigir(puerta.GetComponent<BoxCollider2D>().enabled,
                "El obstaculo debe bloquear a quien todavia no desbloqueo el Dash.");

            jugador.DesbloquearHabilidad(HabilidadExploracion2D.Dash);
            Invocar(puerta, "OnTriggerStay2D", colliderJugador);
            Exigir(puerta.GetComponent<BoxCollider2D>().enabled,
                "Desbloquear el Dash no debe abrir la barrera caminando.");

            Establecer(jugador, "dashTerminaEn", Time.time + 1f);
            Establecer(jugador, "direccionDash", Vector2.up);
            Invocar(puerta, "OnTriggerStay2D", colliderJugador);
            Exigir(puerta.GetComponent<BoxCollider2D>().enabled,
                "Un Dash vertical no debe abrir una barrera horizontal.");

            Establecer(jugador, "direccionDash", Vector2.right);
            Invocar(puerta, "OnTriggerStay2D", colliderJugador);
            Exigir(!puerta.GetComponent<BoxCollider2D>().enabled,
                "La barrera solo debe abrirse mientras se cruza con un Dash horizontal.");

            Establecer(jugador, "dashTerminaEn", Time.time - 1f);
            Invocar(puerta, "OnTriggerStay2D", colliderJugador);
            Exigir(puerta.GetComponent<BoxCollider2D>().enabled,
                "La barrera debe cerrarse al terminar el Dash, sin dejar pasar caminando.");

            Invocar(puerta, "OnTriggerExit2D", colliderJugador);
            Exigir(puerta.GetComponent<BoxCollider2D>().enabled,
                "La barrera debe quedar cerrada despues de atravesarla.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(puertaObjeto);
            UnityEngine.Object.DestroyImmediate(jugadorObjeto);
            UnityEngine.Object.DestroyImmediate(acciones);
        }
    }

    private static GameObject CrearJugador(InputActionAsset acciones)
    {
        GameObject objeto = new GameObject("Prueba Dash - jugador");
        objeto.SetActive(false);
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        objeto.AddComponent<BoxCollider2D>();

        Jugador2D jugador = objeto.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        Establecer(jugador, "dashDesbloqueado", false);
        VidaJugador2D vida = objeto.AddComponent<VidaJugador2D>();
        objeto.SetActive(true);
        Invocar(jugador, "Awake");
        Invocar(vida, "Awake");
        return objeto;
    }

    private static GameObject CrearPuerta(out PuertaDash2D puerta)
    {
        GameObject objeto = new GameObject("Prueba Dash - barrera");
        objeto.SetActive(false);
        BoxCollider2D bloqueo = objeto.AddComponent<BoxCollider2D>();
        BoxCollider2D sensor = objeto.AddComponent<BoxCollider2D>();
        SpriteRenderer visual = objeto.AddComponent<SpriteRenderer>();
        puerta = objeto.AddComponent<PuertaDash2D>();
        Establecer(puerta, "colisionadorBloqueo", bloqueo);
        Establecer(puerta, "sensor", sensor);
        Establecer(puerta, "visualBloqueo", visual);
        objeto.SetActive(true);
        Invocar(puerta, "Awake");
        return objeto;
    }

    private static InputActionAsset CrearAcciones()
    {
        InputActionAsset asset = ScriptableObject.CreateInstance<InputActionAsset>();
        InputActionMap jugador = asset.AddActionMap("Player");
        jugador.AddAction("Move");
        jugador.AddAction("Jump", InputActionType.Button);
        jugador.AddAction("Attack", InputActionType.Button);
        jugador.AddAction("Sprint", InputActionType.Button);
        jugador.AddAction("Crouch", InputActionType.Button);
        jugador.AddAction("Interact", InputActionType.Button);
        return asset;
    }

    private static void Invocar(object objetivo, string nombre, params object[] argumentos)
    {
        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (metodo == null)
            throw new MissingMethodException(objetivo.GetType().Name, nombre);
        metodo.Invoke(objetivo, argumentos);
    }

    private static void Establecer(object objetivo, string nombre, object valor)
    {
        FieldInfo campo = objetivo.GetType().GetField(
            nombre,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);
        campo.SetValue(objetivo, valor);
    }

    private static object Leer(object objetivo, string nombre)
    {
        FieldInfo campo = objetivo.GetType().GetField(
            nombre,
            BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic
        );
        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);
        return campo.GetValue(objetivo);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }
}
