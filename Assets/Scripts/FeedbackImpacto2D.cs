using System.Collections;
using UnityEngine;

public class FeedbackImpacto2D : MonoBehaviour
{
    private SpriteRenderer[] sprites;
    private Color[] coloresOriginales;
    private Coroutine feedbackActivo;

    private void Awake()
    {
        sprites = GetComponentsInChildren<SpriteRenderer>(true);
    }

    private void OnDisable()
    {
        if (feedbackActivo != null)
            StopCoroutine(feedbackActivo);

        feedbackActivo = null;
        RestaurarColores();
    }

    public void MostrarImpacto(float duracion = 0.1f)
    {
        if (InterfazJuego2D.ReducirDestellos)
        {
            IniciarFeedback(new Color(1f, 0.92f, 0.72f, 0.14f),
                Mathf.Max(0.01f, duracion), 0f);
            return;
        }

        IniciarFeedback(Color.white, Mathf.Max(0.01f, duracion), 0f);
    }

    public void MostrarInvulnerabilidad(float duracion, float intervalo)
    {
        if (InterfazJuego2D.ReducirDestellos)
        {
            IniciarFeedback(new Color(1f, 0.45f, 0.32f, 0.16f),
                Mathf.Max(0.01f, duracion), 0f);
            return;
        }

        IniciarFeedback(new Color(1f, 0.32f, 0.28f, 0.38f),
            Mathf.Max(0.01f, duracion), Mathf.Max(0.01f, intervalo));
    }

    private void IniciarFeedback(Color tinte, float duracion, float intervalo)
    {
        if (sprites == null)
            sprites = GetComponentsInChildren<SpriteRenderer>(true);
        if (sprites.Length == 0)
            return;

        if (feedbackActivo != null)
        {
            StopCoroutine(feedbackActivo);
            RestaurarColores();
        }

        coloresOriginales = new Color[sprites.Length];
        for (int i = 0; i < sprites.Length; i++)
            coloresOriginales[i] = sprites[i] != null ? sprites[i].color : Color.white;

        feedbackActivo = StartCoroutine(AnimarFeedback(tinte, duracion, intervalo));
    }

    private IEnumerator AnimarFeedback(Color tinte, float duracion, float intervalo)
    {
        float tiempoTranscurrido = 0f;
        while (tiempoTranscurrido < duracion)
        {
            bool mostrarTinte = intervalo <= 0f ||
                Mathf.FloorToInt(tiempoTranscurrido / intervalo) % 2 == 0;

            for (int i = 0; i < sprites.Length; i++)
            {
                if (sprites[i] != null)
                    sprites[i].color = mostrarTinte
                        ? Color.Lerp(coloresOriginales[i], tinte, tinte.a)
                        : coloresOriginales[i];
            }

            yield return null;
            tiempoTranscurrido += Time.deltaTime;
        }

        RestaurarColores();
        feedbackActivo = null;
    }

    private void RestaurarColores()
    {
        if (sprites == null || coloresOriginales == null)
            return;

        for (int i = 0; i < sprites.Length && i < coloresOriginales.Length; i++)
        {
            if (sprites[i] != null)
                sprites[i].color = coloresOriginales[i];
        }
    }
}
