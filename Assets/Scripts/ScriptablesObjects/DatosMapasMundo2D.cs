using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "DatosMapaMundo", menuName = "Metroidvania/Datos Mapa Mundo")]
public class DatosMapaMundo2D : ScriptableObject
{
    [SerializeField] private List<string> checkpointsActivados = new List<string>();
    [SerializeField] private List<string> tiendasDescubiertas = new List<string>();
    [SerializeField] private string zonaActual;

    public void RegistrarCheckpoint(string idPunto)
    {
        if (!checkpointsActivados.Contains(idPunto))
        {
            checkpointsActivados.Add(idPunto);
        }
    }

    public void RegistrarTienda(string idTienda)
    {
        if (!tiendasDescubiertas.Contains(idTienda))
        {
            tiendasDescubiertas.Add(idTienda);
        }
    }

    public bool EstaCheckpointActivado(string idPunto) => checkpointsActivados.Contains(idPunto);
    public bool EstaTiendaDescubierta(string idTienda) => tiendasDescubiertas.Contains(idTienda);

    public void LimpiarProgresoPrueba()
    {
        checkpointsActivados.Clear();
        tiendasDescubiertas.Clear();
    }
}