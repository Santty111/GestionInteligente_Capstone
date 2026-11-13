using System;

[Serializable]
public class DatosPartida
{
    public string idEstudiante;
    public string escenario;
    public string nivel;
    public string fecha;
    
    // KPIs
    public float puntajeGlobal;
    public float entregasATiempo;
    public float utilizacionRecursos;
    public float eficienciaRutas;
    public float costoTotal;
    
    // Control de sincronización
    public bool sincronizadoConPortal;
}

[Serializable]
public class ListaPartidas
{
    public DatosPartida[] partidas;
}