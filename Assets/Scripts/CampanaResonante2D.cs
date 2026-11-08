using System;
using System.Collections;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// Campana de ceramica elastica y resonante.
/// Sirve como elemento de plataformeo metroidvania y reactivo al resonador de Sira.
/// Al ser golpeada hacia abajo con pogo, impulsa a Sira verticalmente con fuerza
/// elastica aumentada y reproduce una resonancia acustica/visual (squash and stretch).
/// Tambien responde a tajos horizontales y verticales emitiendo resonancia.
/// </summary>
[DisallowMultipleComponent]
public class CampanaResonante2D : MonoBehaviour, IRebotablePogo2D, IDamageable2D
{
    [Header("Propiedades de Rebote")]
    [SerializeField, Min(1f)] private float fuerzaRebotePersonalizada = 14.5f;
    [SerializeField] private bool usarFuerzaPersonalizada = true;

    [Header("Eventos de Resonancia")]
    [SerializeField] private UnityEvent alResonar;

    public event Action ResonanciaActivada;

    private SpriteRenderer spriteRenderer;
    private Vector3 escalaOriginal = Vector3.one;
    private Color colorOriginal = Color.white;
    private Coroutine animacionActual;

    public float FuerzaRebote => fuerzaRebotePersonalizada;

    private void Awake()
    {
        spriteRenderer = GetComponent<SpriteRenderer>();
        escalaOriginal = transform.localScale;
        if (spriteRenderer != null)
            colorOriginal = spriteRenderer.color;
    }

    public bool AlRecibirPogo(Jugador2D jugador, Vector2 puntoContacto)
    {
        if (jugador == null)
            return false;

        float fuerza = usarFuerzaPersonalizada ? fuerzaRebotePersonalizada : jugador.fuerzaRebotePogo;
        jugador.EjecutarRebotePogo(fuerza);

        DispararResonancia(esPogo: true);
        return true;
    }

    public bool RecibirDanio(int cantidad, Vector2 posicionAtacante, float retroceso)
    {
        DispararResonancia(esPogo: false);
        return true;
    }

    private void DispararResonancia(bool esPogo)
    {
        alResonar?.Invoke();
        ResonanciaActivada?.Invoke();

        if (gameObject.activeInHierarchy)
        {
            if (animacionActual != null)
                StopCoroutine(animacionActual);
            animacionActual = StartCoroutine(AnimarResonancia(esPogo));
        }
    }

    private IEnumerator AnimarResonancia(bool esPogo)
    {
        float duracion = 0.22f;
        float tiempo = 0f;

        Vector3 escalaImpacto = esPogo
            ? new Vector3(escalaOriginal.x * 1.35f, escalaOriginal.y * 0.7f, escalaOriginal.z)
            : new Vector3(escalaOriginal.x * 0.85f, escalaOriginal.y * 1.25f, escalaOriginal.z);

        Color colorResonancia = new Color(1f, 0.95f, 0.7f, 1f);

        while (tiempo < duracion)
        {
            tiempo += Time.deltaTime;
            float t = tiempo / duracion;
            float rebote = Mathf.Sin(t * Mathf.PI);

            transform.localScale = Vector3.Lerp(escalaOriginal, escalaImpacto, rebote);
            if (spriteRenderer != null)
                spriteRenderer.color = Color.Lerp(colorOriginal, colorResonancia, rebote);

            yield return null;
        }

        transform.localScale = escalaOriginal;
        if (spriteRenderer != null)
            spriteRenderer.color = colorOriginal;
        animacionActual = null;
    }

    public void Configurar(float nuevaFuerzaRebote)
    {
        fuerzaRebotePersonalizada = Mathf.Max(1f, nuevaFuerzaRebote);
        usarFuerzaPersonalizada = true;
    }
}
