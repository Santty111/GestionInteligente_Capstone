using UnityEngine;

[System.Serializable]
public class EscenarioMision
{
    public int id;
    public string nombreMision;
    public string ciudadOrigen;
    public string ciudadDestino;
    public float presupuestoInicial;
    public float distanciaKM;
    public string dificultad; // "Fácil", "Normal", "Difícil"
    [TextArea(2, 4)]
    public string descripcion;
    
    // Parámetros dinámicos para los eventos aduaneros (Canales)
    public float probabilidadAforoRojo; // 0.0 a 1.0 (ej: 0.3 para 30%)
    public float tasaAduaneraBase;
}