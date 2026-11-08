using System;
using System.Collections.Generic;
using System.Reflection;
using System.Linq;
using UnityEditor;
using UnityEngine;
using UnityEngine.InputSystem;

public static class PruebasEnemigosPrototipo2D
{
    private const string RutaAcciones = "Assets/InputSystem_Actions.inputactions";
    private static readonly Type[] TiposEnemigo =
    {
        typeof(EnemigoTerrestrePrototipo2D),
        typeof(EnemigoTiradorPrototipo2D),
        typeof(EnemigoRondaPlataformas2D),
        typeof(EnemigoVoladorPrototipo2D),
        typeof(EnemigoResonadorPrototipo2D)
    };

    [MenuItem("Demo/Validar cinco arquetipos de enemigos")]
    public static void Ejecutar()
    {
        InputActionAsset acciones = AssetDatabase.LoadAssetAtPath<InputActionAsset>(RutaAcciones);
        if (acciones == null)
            throw new InvalidOperationException("No se encontro " + RutaAcciones);

        int ataquesComprobados = 0;
        int gruposDanioComprobados = 0;
        foreach (Type tipo in TiposEnemigo)
        {
            ProbarAtaqueConAviso(tipo, acciones);
            ataquesComprobados++;
            ProbarDanoInvulnerabilidadYMuerte(tipo);
            gruposDanioComprobados++;
        }

        Debug.Log("Pruebas de enemigos correctas: " + ataquesComprobados +
                  " ataques con aviso y " + gruposDanioComprobados +
                  " grupos de dano/invulnerabilidad/derrota.");
    }

    private static void ProbarAtaqueConAviso(Type tipo, InputActionAsset acciones)
    {
        GameObject jugador = CrearJugador(acciones);
        GameObject enemigoObjeto = CrearEnemigo(tipo, jugador.GetComponent<Jugador2D>());
        HashSet<GameObject> proyectilesPrevios = EncontrarProyectiles().ToHashSet();
        try
        {
            EnemigoPrototipo2D enemigo = enemigoObjeto.GetComponent<EnemigoPrototipo2D>();
            VidaJugador2D vida = jugador.GetComponent<VidaJugador2D>();
            Invocar(enemigo, "Update");

            Exigir(enemigo.EstadoActual == EnemigoPrototipo2D.Estado.AnticipandoAtaque,
                tipo.Name + ": al detectar al jugador debe mostrar primero el aviso.");
            Exigir(vida.VidaActual == vida.VidaMaxima,
                tipo.Name + ": no debe danar al jugador durante la anticipacion.");
            Exigir(enemigoObjeto.transform.localScale.x > 1f,
                tipo.Name + ": la anticipacion debe tener una senal visual.");

            Establecer(enemigo, "estadoTerminaEn", Time.time - 1f);
            Invocar(enemigo, "Update");
            Exigir(enemigo.EstadoActual == EnemigoPrototipo2D.Estado.Atacando,
                tipo.Name + ": el ataque debe empezar tras terminar la anticipacion.");

            if (tipo == typeof(EnemigoVoladorPrototipo2D))
            {
                Invocar(enemigo, "FixedUpdate");
                Exigir(enemigoObjeto.GetComponent<Rigidbody2D>().linearVelocity.x < 0f,
                    "El volador debe lanzarse hacia la posicion fijada del jugador.");
            }

            if (tipo == typeof(EnemigoTiradorPrototipo2D) ||
                tipo == typeof(EnemigoResonadorPrototipo2D))
            {
                List<GameObject> proyectilesActuales = EncontrarProyectiles();
                Exigir(proyectilesActuales.Any(proyectil => !proyectilesPrevios.Contains(proyectil)),
                    tipo.Name + ": el ataque debe crear un proyectil.");
            }
        }
        finally
        {
            Limpiar(EncontrarProyectiles().Where(proyectil => !proyectilesPrevios.Contains(proyectil)));
            UnityEngine.Object.DestroyImmediate(enemigoObjeto);
            UnityEngine.Object.DestroyImmediate(jugador);
        }
    }

    private static void ProbarDanoInvulnerabilidadYMuerte(Type tipo)
    {
        GameObject enemigoObjeto = CrearEnemigo(tipo, null);
        try
        {
            EnemigoPrototipo2D enemigo = enemigoObjeto.GetComponent<EnemigoPrototipo2D>();
            Exigir(enemigo.RecibirDanio(1, Vector2.left, 2f),
                tipo.Name + ": debe aceptar un impacto valido.");
            Exigir(enemigo.VidaActual == enemigo.VidaMaxima - 1,
                tipo.Name + ": debe reducir la vida tras el impacto.");
            Exigir(!enemigo.RecibirDanio(1, Vector2.left, 2f),
                tipo.Name + ": debe rechazar impactos durante la invulnerabilidad.");

            Establecer(enemigo, "invulnerableHasta", Time.time - 1f);
            Exigir(enemigo.RecibirDanio(enemigo.VidaMaxima, Vector2.left, 0f),
                tipo.Name + ": debe aceptar dano letal tras expirar la invulnerabilidad.");
            Exigir(enemigo.EstadoActual == EnemigoPrototipo2D.Estado.Muerto,
                tipo.Name + ": debe pasar al estado derrotado.");
            Exigir(!enemigo.GetComponent<Collider2D>().enabled,
                tipo.Name + ": debe desactivar su collider al morir.");
            Exigir(!enemigo.RecibirDanio(1, Vector2.left, 0f),
                tipo.Name + ": no debe aceptar dano despues de morir.");
        }
        finally
        {
            UnityEngine.Object.DestroyImmediate(enemigoObjeto);
        }
    }

    private static GameObject CrearJugador(InputActionAsset acciones)
    {
        GameObject objeto = new GameObject("Prueba arquetipo - jugador");
        objeto.SetActive(false);
        objeto.AddComponent<Rigidbody2D>().gravityScale = 0f;
        objeto.AddComponent<BoxCollider2D>();
        Jugador2D jugador = objeto.AddComponent<Jugador2D>();
        jugador.accionesEntrada = acciones;
        VidaJugador2D vida = objeto.AddComponent<VidaJugador2D>();
        objeto.SetActive(true);
        Invocar(vida, "Awake");
        return objeto;
    }

    private static GameObject CrearEnemigo(Type tipo, Jugador2D jugador)
    {
        GameObject objeto = new GameObject("Prueba arquetipo - " + tipo.Name);
        objeto.SetActive(false);
        objeto.transform.position = new Vector3(1.5f, 0f);
        objeto.AddComponent<SpriteRenderer>();
        Rigidbody2D rb = objeto.AddComponent<Rigidbody2D>();
        rb.gravityScale = 0f;
        rb.freezeRotation = true;
        objeto.AddComponent<BoxCollider2D>();

        EnemigoPrototipo2D enemigo = (EnemigoPrototipo2D)objeto.AddComponent(tipo);
        Establecer(enemigo, "vidaMaxima", 3);
        Establecer(enemigo, "rangoDeteccion", 10f);
        Establecer(enemigo, "rangoAtaque", 8f);
        Establecer(enemigo, "anticipacionAtaque", 0.6f);
        Establecer(enemigo, "tiempoInvulnerableTrasGolpe", 0.5f);
        if (jugador != null)
            Establecer(enemigo, "jugador", jugador);

        if (tipo == typeof(EnemigoRondaPlataformas2D))
            AsignarRuta(enemigo);
        objeto.SetActive(true);
        Invocar(enemigo, "Awake");
        return objeto;
    }

    private static void AsignarRuta(EnemigoPrototipo2D enemigo)
    {
        Transform[] ruta = new Transform[4];
        Vector2[] posiciones =
        {
            new Vector2(-1f, 1f),
            new Vector2(1f, 1f),
            new Vector2(1f, -1f),
            new Vector2(-1f, -1f)
        };
        for (int i = 0; i < ruta.Length; i++)
        {
            ruta[i] = new GameObject("Punto de prueba " + i).transform;
            ruta[i].SetParent(enemigo.transform, false);
            ruta[i].localPosition = posiciones[i];
        }

        Establecer(enemigo, "puntosRuta", ruta);
    }

    private static List<GameObject> EncontrarProyectiles()
    {
        List<GameObject> resultados = new List<GameObject>();
        foreach (ProyectilPrototipoEnemigo2D proyectil in
                 UnityEngine.Object.FindObjectsByType<ProyectilPrototipoEnemigo2D>(FindObjectsSortMode.None))
            resultados.Add(proyectil.gameObject);
        return resultados;
    }

    private static void Invocar(object objetivo, string nombre)
    {
        MethodInfo metodo = objetivo.GetType().GetMethod(
            nombre,
            BindingFlags.Instance | BindingFlags.NonPublic
        );
        if (metodo == null)
            throw new MissingMethodException(objetivo.GetType().Name, nombre);
        metodo.Invoke(objetivo, null);
    }

    private static void Establecer(object objetivo, string nombre, object valor)
    {
        FieldInfo campo = null;
        Type tipo = objetivo.GetType();
        while (tipo != null && campo == null)
        {
            campo = tipo.GetField(nombre,
                BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic |
                BindingFlags.DeclaredOnly);
            tipo = tipo.BaseType;
        }

        if (campo == null)
            throw new MissingFieldException(objetivo.GetType().Name, nombre);
        campo.SetValue(objetivo, valor);
    }

    private static void Exigir(bool condicion, string mensaje)
    {
        if (!condicion)
            throw new InvalidOperationException(mensaje);
    }

    private static void Limpiar(IEnumerable<GameObject> objetos)
    {
        foreach (GameObject objeto in objetos)
        {
            if (objeto != null)
                UnityEngine.Object.DestroyImmediate(objeto);
        }
    }
}
