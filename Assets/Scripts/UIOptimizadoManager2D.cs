using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class UIOptimizadoManager2D : MonoBehaviour
{
    public enum PestanaUI { Inventario, Tienda, Mapa, Opciones }

    [Header("Paneles Principales")]
    [SerializeField] private GameObject panelInventario;
    [SerializeField] private GameObject panelTienda;
    [SerializeField] private GameObject panelMapa;

    [Header("Navegación de Pestañas")]
    [SerializeField] private Button botonInventario;
    [SerializeField] private Button botonTienda;
    [SerializeField] private Button botonMapa;

    [Header("Vista Previa y Descripción de Objetos")]
    [SerializeField] private Text textoTituloObjeto;
    [SerializeField] private Text textoDescripcionObjeto;
    [SerializeField] private Image iconoObjetoSeleccionado;

    private PestanaUI pestanaActual = PestanaUI.Inventario;

    private void Start()
    {
        ConfigurarBotones();
        MostrarPestana(PestanaUI.Inventario);
    }

    private void ConfigurarBotones()
    {
        if (botonInventario != null) botonInventario.onClick.AddListener(() => MostrarPestana(PestanaUI.Inventario));
        if (botonTienda != null) botonTienda.onClick.AddListener(() => MostrarPestana(PestanaUI.Tienda));
        if (botonMapa != null) botonMapa.onClick.AddListener(() => MostrarPestana(PestanaUI.Mapa));
    }

    public void MostrarPestana(PestanaUI nuevaPestana)
    {
        pestanaActual = nuevaPestana;

        if (panelInventario != null) panelInventario.SetActive(pestanaActual == PestanaUI.Inventario);
        if (panelTienda != null) panelTienda.SetActive(pestanaActual == PestanaUI.Tienda);
        if (panelMapa != null) panelMapa.SetActive(pestanaActual == PestanaUI.Mapa);

        RefrescarContenido();
    }

    private void RefrescarContenido()
    {
        switch (pestanaActual)
        {
            case PestanaUI.Inventario:
                // Cargar cuadrilla optimizada de inventario (pools de iconos)
                break;
            case PestanaUI.Tienda:
                // Cargar artículos disponibles e información de Ecos/Monedas
                break;
            case PestanaUI.Mapa:
                // Actualizar posición del jugador en la cuadrícula de zonas exploradas
                break;
        }
    }

    public void MostrarDetallesObjeto(string nombre, string descripcion, Sprite icono)
    {
        if (textoTituloObjeto != null) textoTituloObjeto.text = nombre;
        if (textoDescripcionObjeto != null) textoDescripcionObjeto.text = descripcion;
        if (iconoObjetoSeleccionado != null)
        {
            iconoObjetoSeleccionado.sprite = icono;
            iconoObjetoSeleccionado.gameObject.SetActive(icono != null);
        }
    }
}