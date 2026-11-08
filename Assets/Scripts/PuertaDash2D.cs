using System.Collections.Generic;
using UnityEngine;

[RequireComponent(typeof(BoxCollider2D))]
public class PuertaDash2D : MonoBehaviour
{
    [SerializeField] private Collider2D colisionadorBloqueo;
    [SerializeField] private Collider2D sensor;
    [SerializeField] private SpriteRenderer visualBloqueo;

    private Jugador2D jugadorEnUmbral;
    private readonly HashSet<Collider2D> colisionadoresEnUmbral = new HashSet<Collider2D>();
    private Color colorBloqueado = Color.white;

    private void Awake()
    {
        if (colisionadorBloqueo == null || sensor == null)
        {
            Debug.LogError("Asigna los colliders solido y sensor de la puerta de Dash.", this);
            enabled = false;
            return;
        }

        colisionadorBloqueo.isTrigger = false;
        colisionadorBloqueo.enabled = true;
        sensor.isTrigger = true;
        if (visualBloqueo != null)
            colorBloqueado = visualBloqueo.color;
    }

    private void OnDisable()
    {
        if (colisionadorBloqueo != null)
            colisionadorBloqueo.enabled = true;
        if (visualBloqueo != null)
            visualBloqueo.color = colorBloqueado;
        colisionadoresEnUmbral.Clear();
        jugadorEnUmbral = null;
    }

    private void OnTriggerEnter2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null)
            return;

        jugadorEnUmbral = jugador;
        colisionadoresEnUmbral.Add(otro);
        ActualizarBloqueo();
    }

    private void OnTriggerStay2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null || jugador != jugadorEnUmbral)
            return;

        ActualizarBloqueo();
    }

    private void OnTriggerExit2D(Collider2D otro)
    {
        Jugador2D jugador = otro.GetComponentInParent<Jugador2D>();
        if (jugador == null || jugador != jugadorEnUmbral)
            return;

        colisionadoresEnUmbral.Remove(otro);
        if (colisionadoresEnUmbral.Count > 0)
            return;

        jugadorEnUmbral = null;
        ActualizarBloqueo();
    }

    public bool PuedeAtravesar(Jugador2D jugador)
    {
        return jugador != null &&
               jugador.TieneHabilidad(HabilidadExploracion2D.Dash) &&
               jugador.EstaEnDash &&
               Mathf.Abs(jugador.DireccionDash.x) >= 0.9f;
    }

    private void ActualizarBloqueo()
    {
        bool puedeCruzar = colisionadoresEnUmbral.Count > 0 &&
                           PuedeAtravesar(jugadorEnUmbral);
        colisionadorBloqueo.enabled = !puedeCruzar;
        if (visualBloqueo != null)
            visualBloqueo.color = puedeCruzar
                ? new Color(0.45f, 1f, 0.9f, 0.28f)
                : colorBloqueado;
    }
}
