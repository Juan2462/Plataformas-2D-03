using System;
using System.Collections.Generic;
using UnityEngine;

public enum HabilidadExploracion2D
{
    Dash,
    DescargaPresion,
    DobleSalto,
    EscaladaParedes
}

[DisallowMultipleComponent]
public sealed class HabilidadesExploracion2D : MonoBehaviour
{
    [SerializeField] private List<HabilidadExploracion2D> habilidadesIniciales =
        new List<HabilidadExploracion2D>();

    private readonly HashSet<HabilidadExploracion2D> desbloqueadas =
        new HashSet<HabilidadExploracion2D>();

    private void Awake()
    {
        foreach (HabilidadExploracion2D habilidad in habilidadesIniciales)
            desbloqueadas.Add(habilidad);

        foreach (HabilidadExploracion2D habilidad in
                 (HabilidadExploracion2D[])Enum.GetValues(typeof(HabilidadExploracion2D)))
        {
            if (ProgresoPartida2D.TieneHabilidad(habilidad))
                desbloqueadas.Add(habilidad);
        }
    }

    public bool Tiene(HabilidadExploracion2D habilidad)
    {
        return desbloqueadas.Contains(habilidad);
    }

    public bool Desbloquear(HabilidadExploracion2D habilidad)
    {
        if (!Enum.IsDefined(typeof(HabilidadExploracion2D), habilidad))
        {
            Debug.LogError("La habilidad de exploracion no esta definida en el catalogo.", this);
            return false;
        }

        bool nueva = desbloqueadas.Add(habilidad);
        if (nueva)
            ProgresoPartida2D.RegistrarHabilidad(habilidad);

        return nueva;
    }
}
