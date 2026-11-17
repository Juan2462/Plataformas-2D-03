using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

[System.Serializable]
public struct MarcadorMapa
{
    public string idUnico;
    public enum TipoMarcador { Checkpoint, Tienda, Zona }
    public TipoMarcador tipo;
    public RectTransform posicionIconoUI;
}

public class MapaMundoUI2D : MonoBehaviour
{
    [Header("Referencias")]
    [SerializeField] private DatosMapaMundo2D datosMapa;
    [SerializeField] private GameObject prefabIconoCheckpoint;
    [SerializeField] private GameObject prefabIconoTienda;
    [SerializeField] private Transform contenedorMapaUI;

    [Header("Lista de Marcadores en Escena")]
    [SerializeField] private List<MarcadorMapa> marcadoresDelMundo = new List<MarcadorMapa>();

    private Dictionary<string, GameObject> iconosInstanciados = new Dictionary<string, GameObject>();

    private void Start()
    {
        ActualizarMarcadoresDinamicos();
    }

    public void ActualizarMarcadoresDinamicos()
    {
        if (datosMapa == null) return;

        foreach (var marcador in marcadoresDelMundo)
        {
            bool mostrar = false;

            if (marcador.tipo == MarcadorMapa.TipoMarcador.Checkpoint)
            {
                mostrar = datosMapa.EstaCheckpointActivado(marcador.idUnico);
            }
            else if (marcador.tipo == MarcadorMapa.TipoMarcador.Tienda)
            {
                mostrar = datosMapa.EstaTiendaDescubierta(marcador.idUnico);
            }

            // Gestionar la creación o visibilidad del icono en la UI del mapa
            if (mostrar)
            {
                if (!iconosInstanciados.ContainsKey(marcador.idUnico))
                {
                    GameObject prefabAUsar = marcador.tipo == MarcadorMapa.TipoMarcador.Checkpoint ? prefabIconoCheckpoint : prefabIconoTienda;
                    if (prefabAUsar != null && contenedorMapaUI != null)
                    {
                        GameObject instancia = Instantiate(prefabAUsar, contenedorMapaUI);
                        instancia.GetComponent<RectTransform>().anchoredPosition = marcador.posicionIconoUI.anchoredPosition;
                        iconosInstanciados.Add(marcador.idUnico, instancia);
                    }
                }
            }
            else if (iconosInstanciados.ContainsKey(marcador.idUnico))
            {
                Destroy(iconosInstanciados[marcador.idUnico]);
                iconosInstanciados.Remove(marcador.idUnico);
            }
        }
    }
}