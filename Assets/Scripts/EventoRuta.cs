using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class OpcionEvento
{
    public string textoBoton;
    public float costoDinero;       // Se resta del presupuesto
    public float costoTiempoHoras;  // Afecta el reloj de tránsito
    public float impactoPuntaje;    // Suma o resta al KPI final
}

[System.Serializable]
public class EventoRuta
{
    public string titulo;
    [TextArea(2, 3)]
    public string descripcion;
    public List<OpcionEvento> opciones = new List<OpcionEvento>();
}