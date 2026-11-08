using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoInterfazInventario2D : MonoBehaviour
{
    [Serializable]
    private struct Oferta
    {
        public string id;
        public string nombre;
        public string categoria;
        public int precio;
        public TipoEntradaInventario2D tipo;

        public Oferta(string id, string nombre, string categoria, int precio, TipoEntradaInventario2D tipo)
        {
            this.id = id;
            this.nombre = nombre;
            this.categoria = categoria;
            this.precio = precio;
            this.tipo = tipo;
        }
    }

    [SerializeField] private bool esTienda;
    [SerializeField] private InventarioJugador2D inventarioJugador;
    [SerializeField] private InventarioJugador2D inventarioAlmacen;
    [SerializeField] private Animator animadorJugador;

    private const string MarcaInventarioInicial = "DemoInventarioEscenasInicializado";
    private const string MarcaAlmacenInicial = "DemoAlmacenInventarioInicializado";
    private static readonly Oferta[] Ofertas =
    {
        new Oferta("espada_lunar", "Hoja lunar", "Arma", 12, TipoEntradaInventario2D.Arma),
        new Oferta("escudo_cristal", "Escudo de cristal", "Arma", 18, TipoEntradaInventario2D.Arma),
        new Oferta("espada_principiante", "Espada de aprendiz", "Arma", 8, TipoEntradaInventario2D.Arma),
        new Oferta("arco_viajero", "Arco viajero", "Arma", 14, TipoEntradaInventario2D.Arma),
        new Oferta("pocion_eco", "Poción de eco", "Objeto", 6, TipoEntradaInventario2D.Objeto),
        new Oferta("gema_azul", "Gema azul", "Objeto", 10, TipoEntradaInventario2D.Objeto)
    };

    private static readonly string[] Animaciones =
    {
        "CrouchIdle", "CrouchMove", "Fall", "Idle", "JumpUp1", "JumpUp1X", "JumpUp2",
        "JumpUp2X", "JumpUp3", "JumpUp3X", "Land", "LedgeClimb", "LedgeClimbCrouch",
        "LedgeGrab", "LedgeHold", "Move", "Stun", "WallClimb", "WallGrab", "WallSlide",
        "bow_action", "bow_anticipation", "bow_idle", "Shield_Anticipation", "Shield_Break",
        "Shield_Cancel", "Shield_Idle", "Shield_Parry", "Sword_Attack_1", "Sword_Attack_2",
        "Sword_Attack_3", "Tome_Action", "Tome_Anticipation", "Tome_Cancel", "Tome_Idle"
    };

    private Vector2 scrollJugador;
    private Vector2 scrollAlmacen;
    private Vector2 scrollTienda;
    private Vector2 scrollVentas;
    private bool mostrarAnimaciones;
    private string mensaje;
    private float mensajeHasta;

    private void Start()
    {
        if (inventarioJugador == null)
            inventarioJugador = FindFirstObjectByType<Jugador2D>()?.GetComponent<InventarioJugador2D>();

        if (animadorJugador == null)
            animadorJugador = FindFirstObjectByType<Jugador2D>()?.GetComponent<Animator>();

        PrepararInventarioDeDemostracion(inventarioJugador);
        PrepararAlmacen();
    }

    private void PrepararInventarioDeDemostracion(InventarioJugador2D inventario)
    {
        if (inventario == null || PlayerPrefs.GetInt(MarcaInventarioInicial, 0) != 0)
            return;

        if (!inventario.TieneDatosGuardados)
        {
            inventario.AgregarMonedas(30);
            inventario.AgregarLlave("eco_key");
            inventario.AgregarArma("espada_principiante");
            inventario.AgregarObjeto("pocion_eco", 2);
        }

        PlayerPrefs.SetInt(MarcaInventarioInicial, 1);
        PlayerPrefs.Save();
    }

    private void PrepararAlmacen()
    {
        if (esTienda || inventarioAlmacen == null ||
            PlayerPrefs.GetInt(MarcaAlmacenInicial, 0) != 0)
            return;

        if (!inventarioAlmacen.TieneDatosGuardados)
        {
            inventarioAlmacen.AgregarLlave("llave_museo");
            inventarioAlmacen.AgregarArma("arco_viajero");
            inventarioAlmacen.AgregarObjeto("gema_azul", 2);
        }

        PlayerPrefs.SetInt(MarcaAlmacenInicial, 1);
        PlayerPrefs.Save();
    }

    private void OnGUI()
    {
        float ancho = Mathf.Min(Screen.width - 32f, 1040f);
        float alto = Mathf.Min(Screen.height - 32f, 780f);
        Rect panel = new Rect((Screen.width - ancho) * 0.5f, 16f, ancho, alto);

        GUI.color = new Color(0.055f, 0.08f, 0.12f, 0.96f);
        GUI.Box(panel, GUIContent.none);
        GUI.color = Color.white;

        GUILayout.BeginArea(new Rect(panel.x + 20f, panel.y + 16f, panel.width - 40f, panel.height - 32f));
        GUILayout.Label(esTienda ? "MERCADO DE LAS RUINAS" : "MOCHILA Y ALMACÉN");
        GUILayout.Label("Ecos: " + (inventarioJugador != null ? inventarioJugador.Monedas : 0));
        VidaJugador2D vidaJugador = inventarioJugador != null
            ? inventarioJugador.GetComponent<VidaJugador2D>()
            : null;
        if (vidaJugador != null)
            GUILayout.Label("Vida: " + vidaJugador.VidaActual + " / " + vidaJugador.VidaMaxima);

        GUILayout.BeginHorizontal();
        if (GUILayout.Button("Inventario / intercambio", GUILayout.Height(34f)))
            SceneManager.LoadScene("InventarioDemo");
        if (GUILayout.Button("Tienda", GUILayout.Height(34f)))
            SceneManager.LoadScene("TiendaDemo");
        if (GUILayout.Button("Volver a Ruinas del Eco", GUILayout.Height(34f)))
            SceneManager.LoadScene("RuinsDemo");
        GUILayout.EndHorizontal();

        if (esTienda)
            DibujarTienda();
        else
            DibujarInventario();

        if (Time.time < mensajeHasta)
            GUILayout.Label(mensaje);

        GUILayout.EndArea();
    }

    private void DibujarInventario()
    {
        GUILayout.BeginHorizontal();
        if (GUILayout.Button(mostrarAnimaciones ? "Ver inventario" : "Galería de animaciones", GUILayout.Height(36f)))
            mostrarAnimaciones = !mostrarAnimaciones;
        GUILayout.EndHorizontal();

        if (mostrarAnimaciones)
        {
            DibujarGaleriaAnimaciones();
            return;
        }

        if (inventarioJugador == null || inventarioAlmacen == null)
        {
            GUILayout.Label("Falta asignar el inventario del jugador o el almacén.");
            return;
        }

        VidaJugador2D vidaJugador = inventarioJugador.GetComponent<VidaJugador2D>();
        if (vidaJugador != null && vidaJugador.VidaActual == vidaJugador.VidaMaxima &&
            GUILayout.Button("Prueba de poción: recibir 1 de daño", GUILayout.Height(32f)))
        {
            if (vidaJugador.RecibirDanio(1))
                MostrarMensaje("Ahora puedes usar una poción desde la mochila.");
            else
                MostrarMensaje("No se pudo recibir daño durante esta acción.");
        }

        GUILayout.Label("Pulsa «Guardar» o «Retirar» para intercambiar una unidad. Las llaves también se pueden guardar.");
        GUILayout.BeginHorizontal();
        float alturaLista = Mathf.Max(120f, Screen.height - 235f);
        scrollJugador = GUILayout.BeginScrollView(scrollJugador, GUILayout.Height(alturaLista));
        DibujarColumnaInventario("MOCHILA", inventarioJugador, inventarioAlmacen, true);
        GUILayout.EndScrollView();
        scrollAlmacen = GUILayout.BeginScrollView(scrollAlmacen, GUILayout.Height(alturaLista));
        DibujarColumnaInventario("ALMACÉN", inventarioAlmacen, inventarioJugador, false);
        GUILayout.EndScrollView();
        GUILayout.EndHorizontal();
    }

    private void DibujarColumnaInventario(
        string titulo,
        InventarioJugador2D origen,
        InventarioJugador2D destino,
        bool guardarEnDestino)
    {
        GUILayout.BeginVertical("box");
        GUILayout.Label(titulo);
        DibujarGrupo("Llaves", origen.Llaves, origen, destino, TipoEntradaInventario2D.Llave,
            guardarEnDestino ? "Guardar" : "Retirar");
        DibujarGrupo("Armas", origen.Armas, origen, destino, TipoEntradaInventario2D.Arma,
            guardarEnDestino ? "Guardar" : "Retirar");
        DibujarGrupo("Objetos", origen.Objetos, origen, destino, TipoEntradaInventario2D.Objeto,
            guardarEnDestino ? "Guardar" : "Retirar");
        GUILayout.EndVertical();
    }

    private void DibujarGrupo(
        string titulo,
        IReadOnlyList<EntradaInventario2D> entradas,
        InventarioJugador2D origen,
        InventarioJugador2D destino,
        TipoEntradaInventario2D tipo,
        string textoBoton)
    {
        GUILayout.Label(titulo);
        List<EntradaInventario2D> copia = new List<EntradaInventario2D>(entradas);
        if (copia.Count == 0)
        {
            GUILayout.Label("  (vacío)");
            return;
        }

        foreach (EntradaInventario2D entrada in copia)
        {
            GUILayout.BeginHorizontal();
            GUILayout.Label(entrada.Id + "  x" + entrada.Cantidad);
            if (origen == inventarioJugador &&
                tipo == TipoEntradaInventario2D.Objeto &&
                entrada.Id == "pocion_eco" &&
                GUILayout.Button("Usar", GUILayout.Width(70f)))
            {
                UsarPocion();
            }
            if (GUILayout.Button(textoBoton, GUILayout.Width(90f)))
            {
                if (origen.IntentarTransferir(destino, tipo, entrada.Id))
                    MostrarMensaje("Intercambiaste 1 " + entrada.Id + ".");
                else
                    MostrarMensaje("No se pudo transferir el objeto.");
            }
            GUILayout.EndHorizontal();
        }
    }

    private void DibujarGaleriaAnimaciones()
    {
        if (animadorJugador == null)
        {
            GUILayout.Label("No se encontró el Animator del jugador.");
            return;
        }

        GUILayout.Label("Selecciona una animación para reproducirla en el personaje. Hay " + Animaciones.Length + " clips.");
        scrollJugador = GUILayout.BeginScrollView(scrollJugador, GUILayout.Height(Mathf.Max(120f, Screen.height - 235f)));
        for (int i = 0; i < Animaciones.Length; i += 3)
        {
            GUILayout.BeginHorizontal();
            for (int columna = 0; columna < 3 && i + columna < Animaciones.Length; columna++)
            {
                string nombre = Animaciones[i + columna];
                if (GUILayout.Button(nombre, GUILayout.Height(38f)))
                    animadorJugador.Play("Galeria_" + nombre, 0, 0f);
            }
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();
    }

    private void DibujarTienda()
    {
        if (inventarioJugador == null)
        {
            GUILayout.Label("No se encontró el inventario del jugador.");
            return;
        }

        GUILayout.Label("COMPRAR");
        scrollTienda = GUILayout.BeginScrollView(scrollTienda, GUILayout.Height(190f));
        foreach (Oferta oferta in Ofertas)
        {
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(oferta.nombre + "  ·  " + oferta.categoria + "  ·  " + oferta.precio + " ecos");
            if (GUILayout.Button("Comprar", GUILayout.Width(110f)))
                Comprar(oferta);
            GUILayout.EndHorizontal();
        }
        GUILayout.EndScrollView();

        GUILayout.Label("VENDER — recibes la mitad del precio de compra. Las llaves no se pueden vender.");
        scrollVentas = GUILayout.BeginScrollView(scrollVentas, GUILayout.Height(Mathf.Max(100f, Screen.height - 475f)));
        DibujarVentas("Armas", inventarioJugador.Armas, TipoEntradaInventario2D.Arma);
        DibujarVentas("Objetos", inventarioJugador.Objetos, TipoEntradaInventario2D.Objeto);
        GUILayout.EndScrollView();
    }

    private void DibujarVentas(
        string titulo,
        IReadOnlyList<EntradaInventario2D> entradas,
        TipoEntradaInventario2D tipo)
    {
        GUILayout.Label(titulo);
        List<EntradaInventario2D> copia = new List<EntradaInventario2D>(entradas);
        if (copia.Count == 0)
        {
            GUILayout.Label("  (vacío)");
            return;
        }

        foreach (EntradaInventario2D entrada in copia)
        {
            Oferta? oferta = BuscarOferta(entrada.Id, tipo);
            GUILayout.BeginHorizontal("box");
            GUILayout.Label(entrada.Id + "  x" + entrada.Cantidad);
            if (!oferta.HasValue)
            {
                GUILayout.Label("Sin precio de venta");
            }
            else if (GUILayout.Button("Vender por " + Mathf.Max(1, oferta.Value.precio / 2), GUILayout.Width(150f)))
            {
                Vender(entrada, tipo, oferta.Value);
            }
            GUILayout.EndHorizontal();
        }
    }

    private void Comprar(Oferta oferta)
    {
        if (!inventarioJugador.IntentarComprar(oferta.tipo, oferta.id, oferta.precio))
        {
            MostrarMensaje(inventarioJugador.Monedas < oferta.precio
                ? "No tienes suficientes ecos para esa compra."
                : "No se pudo completar la compra.");
            return;
        }

        MostrarMensaje("Compraste " + oferta.nombre + ".");
    }

    private void UsarPocion()
    {
        VidaJugador2D vida = inventarioJugador.GetComponent<VidaJugador2D>();
        if (vida == null)
        {
            MostrarMensaje("El jugador no tiene un componente de vida.");
            return;
        }

        if (vida.VidaActual >= vida.VidaMaxima)
        {
            MostrarMensaje("Ya tienes toda la vida.");
            return;
        }

        if (inventarioJugador.IntentarQuitarObjeto("pocion_eco") && vida.Curar(2))
            MostrarMensaje("Usaste una poción y recuperaste vida.");
        else
            MostrarMensaje("No se pudo usar la poción.");
    }

    private void Vender(EntradaInventario2D entrada, TipoEntradaInventario2D tipo, Oferta oferta)
    {
        int precioVenta = Mathf.Max(1, oferta.precio / 2);
        if (!inventarioJugador.IntentarVender(tipo, entrada.Id, precioVenta))
        {
            MostrarMensaje("No se pudo completar la venta.");
            return;
        }

        MostrarMensaje("Vendiste " + oferta.nombre + " por " + precioVenta + " ecos.");
    }

    private static Oferta? BuscarOferta(string id, TipoEntradaInventario2D tipo)
    {
        foreach (Oferta oferta in Ofertas)
        {
            if (oferta.id == id && oferta.tipo == tipo)
                return oferta;
        }

        return null;
    }

    private void MostrarMensaje(string texto)
    {
        mensaje = texto;
        mensajeHasta = Time.time + 3f;
    }
}
