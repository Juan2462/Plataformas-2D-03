using System;
using System.Collections;
using UnityEngine;

public class JefeAvanzadoFases2D : MonoBehaviour
{
    public enum FaseJefe { Fase1, Fase2Enfurecido }
    
    [Header("Estadísticas")]
    [SerializeField] private int vidaMaxima = 20;
    private int vidaActual;
    [SerializeField] private float velocidadMovimiento = 3f;
    private FaseJefe faseActual = FaseJefe.Fase1;

    [Header("Referencias y Proyectiles")]
    [SerializeField] private Transform puntoDisparo;
    [SerializeField] private GameObject prefabProyectil;
    [SerializeField] private Transform jugador;

    [Header("Tiempos de Ataque")]
    [SerializeField] private float tiempoEntreAtaques = 2f;
    private float temporizadorAtaque;
    private bool estaAtacando = false;

    public int VidaActual => vidaActual;
    public FaseJefe FaseActual => faseActual;

    private void Awake()
    {
        vidaActual = vidaMaxima;
        temporizadorAtaque = tiempoEntreAtaques;
    }

    private void Update()
    {
        if (jugador == null) return;

        // Comportamiento según la fase actual
        if (!estaAtacando)
        {
            temporizadorAtaque -= Time.deltaTime;
            if (temporizadorAtaque <= 0f)
            {
                EjecutarPatronAtaque();
                temporizadorAtaque = faseActual == FaseJefe.Fase2Enfurecido ? tiempoEntreAtaques * 0.6f : tiempoEntreAtaques;
            }
        }
    }

    private void EjecutarPatronAtaque()
    {
        // En Fase 2, alterna entre ráfagas de proyectiles y embestidas rápidas
        if (faseActual == FaseJefe.Fase2Enfurecido)
        {
            int patron = UnityEngine.Random.Range(0, 2);
            if (patron == 0) StartCoroutine(PatronRafagaProyectiles(3));
            else StartCoroutine(PatronEmbestidaSismica());
        }
        else
        {
            StartCoroutine(PatronRafagaProyectiles(1));
        }
    }

    private IEnumerator PatronRafagaProyectiles(int cantidad)
    {
        estaAtacando = true;
        for (int i = 0; i < cantidad; i++)
        {
            if (prefabProyectil != null && puntoDisparo != null)
            {
                GameObject proj = Instantiate(prefabProyectil, puntoDisparo.position, Quaternion.identity);
                Rigidbody2D rbProj = proj.GetComponent<Rigidbody2D>();
                if (rbProj != null)
                {
                    Vector2 direccion = (jugador.position - puntoDisparo.position).normalized;
                    rbProj.linearVelocity = direccion * 6f;
                }
            }
            yield return new WaitForSeconds(0.4f);
        }
        estaAtacando = false;
    }

    private IEnumerator PatronEmbestidaSismica()
    {
        estaAtacando = true;
        // Anticipación (parpadeo o pausa)
        yield return new WaitForSeconds(0.5f);
        
        // Embestida rápida hacia la posición del jugador
        Vector3 posicionDestino = new Vector3(jugador.position.x, transform.position.y, 0f);
        float tiempo = 0f;
        Vector3 posicionInicial = transform.position;

        while (tiempo < 0.3f)
        {
            transform.position = Vector3.Lerp(posicionInicial, posicionDestino, tiempo / 0.3f);
            tiempo += Time.deltaTime;
            yield return null;
        }

        // Aplicar estado alterado o daño en área al aterrizar
        AplicarOndaExpansiva();
        yield return new WaitForSeconds(0.8f); // Recuperación
        estaAtacando = false;
    }

    private void AplicarOndaExpansiva()
    {
        Collider2D[] colliders = Physics2D.OverlapCircleAll(transform.position, 2.5f);
        foreach (var col in colliders)
        {
            if (col.TryGetComponent<VidaJugador2D>(out var vidaJugador))
            {
                // Inflige daño y opcionalmente aplica estado alterado (ej. lentitud)
                vidaJugador.RecibirDanio(1);
            }
        }
    }

    public void RecibirDanio(int danio)
    {
        vidaActual -= danio;
        if (vidaActual <= vidaMaxima / 2 && faseActual == FaseJefe.Fase1)
        {
            TransicionFase2();
        }

        if (vidaActual <= 0)
        {
            DerrotarJefe();
        }
    }

    private void TransicionFase2()
    {
        faseActual = FaseJefe.Fase2Enfurecido;
        velocidadMovimiento *= 1.5f;
        // Aquí puedes activar efectos de partículas de la galería de efectos (ej. Fuego o Sombra)
    }

    private void DerrotarJefe()
    {
        Destroy(gameObject);
    }
}