using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public class DemoGaleriaParticulas2D : MonoBehaviour
{
    [SerializeField] private string[] nombresEfectos;
    [SerializeField] private GameObject[] prefabsEfectos;
    [SerializeField] private Transform[] puntosVista;
    [SerializeField] private int[] indicesEfectosContinuos;

    private GameObject[] instanciasContinuas;
    private float[] puedeReproducirEn;
    private Camera camara;
    private GUIStyle estiloTitulo;
    private GUIStyle estiloNombre;
    private GUIStyle estiloBoton;

    private void Start()
    {
        camara = Camera.main;
        if (camara == null)
        {
            Debug.LogError("La galeria de particulas necesita una camara principal.", this);
            enabled = false;
            return;
        }

        if (nombresEfectos == null || prefabsEfectos == null || puntosVista == null ||
            nombresEfectos.Length == 0 ||
            nombresEfectos.Length != prefabsEfectos.Length ||
            nombresEfectos.Length != puntosVista.Length)
        {
            Debug.LogError("La galeria necesita un nombre, prefab y punto de vista por efecto.", this);
            enabled = false;
            return;
        }

        instanciasContinuas = new GameObject[prefabsEfectos.Length];
        puedeReproducirEn = new float[prefabsEfectos.Length];
        for (int i = 0; i < prefabsEfectos.Length; i++)
        {
            if (prefabsEfectos[i] == null || puntosVista[i] == null)
            {
                Debug.LogError("Falta asignar un prefab o punto de vista para el efecto " + i + ".", this);
                enabled = false;
                return;
            }

            if (EsContinuo(i))
                Reproducir(i);
        }
    }

    private void OnGUI()
    {
        if (!enabled || camara == null)
            return;

        if (estiloTitulo == null)
        {
            estiloTitulo = new GUIStyle(GUI.skin.label)
            {
                fontSize = 18,
                fontStyle = FontStyle.Bold,
                normal = { textColor = new Color(0.52f, 0.88f, 1f) }
            };
            estiloNombre = new GUIStyle(GUI.skin.label)
            {
                fontSize = 13,
                fontStyle = FontStyle.Bold,
                alignment = TextAnchor.MiddleCenter,
                wordWrap = true,
                normal = { textColor = Color.white }
            };
            estiloBoton = new GUIStyle(GUI.skin.button)
            {
                fontSize = 12,
                fontStyle = FontStyle.Bold
            };
        }

        GUI.color = new Color(0.055f, 0.08f, 0.14f, 0.92f);
        GUI.Box(new Rect(12f, 12f, Screen.width - 24f, 66f), GUIContent.none);
        GUI.color = Color.white;
        GUI.Label(new Rect(28f, 18f, Screen.width - 250f, 29f),
            "FX LAB  |  CARTOON FX REMASTER", estiloTitulo);
        GUI.Label(new Rect(28f, 44f, Screen.width - 250f, 24f),
            "30 efectos originales de combate, magia y entorno. Pulsa una muestra para verla.");

        for (int i = 0; i < nombresEfectos.Length; i++)
        {
            Vector3 pantalla = camara.WorldToScreenPoint(puntosVista[i].position);
            if (pantalla.z <= 0f)
                continue;

            float y = Screen.height - pantalla.y;
            GUI.color = new Color(0.06f, 0.09f, 0.15f, 0.8f);
            GUI.Box(new Rect(pantalla.x - 100f, y - 58f, 200f, 80f), GUIContent.none);
            GUI.color = Color.white;
            GUI.Label(new Rect(pantalla.x - 92f, y - 55f, 184f, 36f), nombresEfectos[i], estiloNombre);
            if (GUI.Button(new Rect(pantalla.x - 63f, y - 18f, 126f, 30f), "REPRODUCIR", estiloBoton))
                Reproducir(i);
        }

        GUI.color = new Color(0.055f, 0.08f, 0.14f, 0.94f);
        GUI.Box(new Rect(12f, Screen.height - 54f, Screen.width - 24f, 42f), GUIContent.none);
        GUI.color = Color.white;
        if (GUI.Button(new Rect(24f, Screen.height - 48f, 188f, 30f), "Volver a Ruinas del Eco", estiloBoton))
            SceneManager.LoadScene("RuinsDemo");
        GUI.Label(new Rect(226f, Screen.height - 45f, Screen.width - 250f, 24f),
            "Basado en Cartoon FX Remaster Free / JMO. Prefabs reutilizables en Assets/Demo/Prefabs/Efectos.");
    }

    private void Reproducir(int indice)
    {
        if (indice < 0 || indice >= prefabsEfectos.Length || Time.time < puedeReproducirEn[indice])
            return;

        puedeReproducirEn[indice] = Time.time + 0.2f;
        bool continuo = EsContinuo(indice);
        GameObject instancia;

        if (continuo && instanciasContinuas[indice] != null)
        {
            instancia = instanciasContinuas[indice];
            foreach (ParticleSystem sistema in instancia.GetComponentsInChildren<ParticleSystem>())
            {
                sistema.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                sistema.Play(true);
            }
            return;
        }

        instancia = Instantiate(prefabsEfectos[indice], puntosVista[indice].position, Quaternion.identity);
        instancia.name = nombresEfectos[indice];
        ParticleSystem[] sistemas = instancia.GetComponentsInChildren<ParticleSystem>();
        if (sistemas.Length == 0)
        {
            Debug.LogError("El prefab no contiene ningun ParticleSystem: " + prefabsEfectos[indice].name, instancia);
            Destroy(instancia);
            return;
        }

        float duracion = 0f;
        foreach (ParticleSystem sistema in sistemas)
        {
            ParticleSystem.MainModule principal = sistema.main;
            duracion = Mathf.Max(duracion,
                principal.startDelay.constantMax + principal.duration + principal.startLifetime.constantMax);
            sistema.Play(true);
        }

        if (continuo)
            instanciasContinuas[indice] = instancia;
        else
            Destroy(instancia, Mathf.Clamp(duracion + 1.5f, 5f, 15f));
    }

    private bool EsContinuo(int indice)
    {
        if (indicesEfectosContinuos == null)
            return false;

        foreach (int indiceContinuo in indicesEfectosContinuos)
        {
            if (indiceContinuo == indice)
                return true;
        }

        return false;
    }
}
