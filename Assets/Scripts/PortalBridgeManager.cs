using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

public class PortalBridgeManager : MonoBehaviour
{
    public static PortalBridgeManager Instance { get; private set; }

    [Header("Configuración de Conexión")]
    [Tooltip("Activado para trabajar independiente en Unity sin depender del Portal Docente.")]
    public bool usarModoOffline = true;
    public string urlBasePortal = "http://localhost:5000/api/unity";
    public int timeoutSegundos = 2;

    [Header("Estado de Sesión Actual")]
    public PerfilUsuarioDto usuarioActual;
    public ConfiguracionEscenarioDto escenarioActivo;
    public List<ResultadoPartidaDto> historialLocal = new List<ResultadoPartidaDto>();

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
            CargarDatosPorDefectoOffline();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void CargarDatosPorDefectoOffline()
    {
        usuarioActual = new PerfilUsuarioDto
        {
            estudianteId = 1,
            nombre = "Santiago Conlago",
            correo = "santiago.conlago@udla.edu.ec",
            rol = "Estudiante"
        };

        escenarioActivo = new ConfiguracionEscenarioDto
        {
            escenarioId = 1,
            nombreRuta = "Quito - Guayaquil",
            dificultad = "Intermedio",
            presupuestoInicial = 50000f,
            capacidadMaximaCamion = 100f,
            incluyeEventoAduanaSENAE = true
        };
    }

    public void IniciarSesion(string correo, string password, Action<bool, string> callback)
    {
        if (usarModoOffline)
        {
            usuarioActual.correo = string.IsNullOrEmpty(correo) ? "santiago.conlago@udla.edu.ec" : correo;
            usuarioActual.rol = correo.Contains("docente") ? "Docente" : "Estudiante";
            Debug.Log($"[Modo Offline] Login exitoso como: {usuarioActual.rol}");
            callback?.Invoke(true, usuarioActual.rol);
        }
        else
        {
            StartCoroutine(LoginRemotoCoroutine(correo, password, callback));
        }
    }

    private IEnumerator LoginRemotoCoroutine(string correo, string password, Action<bool, string> callback)
    {
        string jsonBody = $"{{\"correo\":\"{correo}\",\"password\":\"{password}\"}}";
        using (UnityWebRequest req = new UnityWebRequest($"{urlBasePortal}/login", "POST"))
        {
            byte[] bodyRaw = Encoding.UTF8.GetBytes(jsonBody);
            req.uploadHandler = new UploadHandlerRaw(bodyRaw);
            req.downloadHandler = new DownloadHandlerBuffer();
            req.SetRequestHeader("Content-Type", "application/json");
            req.timeout = timeoutSegundos;

            yield return req.SendWebRequest();

            if (req.result == UnityWebRequest.Result.Success)
            {
                usuarioActual = JsonUtility.FromJson<PerfilUsuarioDto>(req.downloadHandler.text);
                callback?.Invoke(true, usuarioActual.rol);
            }
            else
            {
                Debug.LogWarning("[Fallback Offline] Sin conexión con el servidor. Accediendo con credenciales locales.");
                callback?.Invoke(true, usuarioActual.rol);
            }
        }
    }

    public string ExportarHistorialACsvLocal()
    {
        StringBuilder sb = new StringBuilder();
        sb.AppendLine("Estudiante,Ruta,Dificultad,CostoTotal,EntregasATiempoPct,UtilizacionRecursosPct,EficienciaRutasPct,EventoAduanaSuperado,PuntajeFinal,Fecha");

        foreach (var r in historialLocal)
        {
            sb.AppendLine($"{r.nombreEstudiante},{r.ruta},{r.dificultad},{r.costoTotal},{r.entregasATiempoPct},{r.utilizacionRecursosPct},{r.eficienciaRutasPct},{r.eventoAduanaSuperado},{r.puntajeFinal},{r.fecha}");
        }

        string rutaArchivo = Path.Combine(Application.persistentDataPath, "Reporte_KPI_Logistica.csv");
        File.WriteAllText(rutaArchivo, sb.ToString(), Encoding.UTF8);
        Debug.Log($"[CSV Generado en]: {rutaArchivo}");
        return rutaArchivo;
    }
}

[Serializable]
public class PerfilUsuarioDto
{
    public int estudianteId;
    public string nombre;
    public string correo;
    public string rol;
}

[Serializable]
public class ConfiguracionEscenarioDto
{
    public int escenarioId;
    public string nombreRuta;
    public string dificultad;
    public float presupuestoInicial;
    public float capacidadMaximaCamion;
    public bool incluyeEventoAduanaSENAE;
}

[Serializable]
public class ResultadoPartidaDto
{
    public int estudianteId;
    public string nombreEstudiante;
    public string ruta;
    public string dificultad;
    public float costoTotal;
    public float entregasATiempoPct;
    public float utilizacionRecursosPct;
    public float eficienciaRutasPct;
    public bool eventoAduanaSuperado;
    public int puntajeFinal;
    public string fecha;
}
