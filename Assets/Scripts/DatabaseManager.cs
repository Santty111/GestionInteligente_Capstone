using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;

public class DatabaseManager : MonoBehaviour
{
    public static DatabaseManager Instancia { get; private set; }

    [Header("Configuración del Portal Docente")]
    // Cambia esto por la URL de tu Portal Docente (ej. https://localhost:7143 o tu dominio)
    public string urlPortalDocente = "https://localhost:7143"; 
    
    private string rutaBaseDatosLocal;
    private List<DatosPartida> historialPartidas = new List<DatosPartida>();

    private void Awake()
    {
        if (Instancia == null)
        {
            Instancia = this;
            DontDestroyOnLoad(gameObject); // Persiste entre escenas
            
            // Define la ruta de la base de datos local (AppData en Windows / Application Support en Mac)
            rutaBaseDatosLocal = Path.Combine(Application.persistentDataPath, "BaseDatosLocal_Resultados.json");
            CargarBaseDatosLocal();
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // --- 1. LÓGICA DE BASE DE DATOS LOCAL ---

    public void GuardarResultadoLocal(DatosPartida nuevaPartida)
    {
        historialPartidas.Add(nuevaPartida);
        
        ListaPartidas lista = new ListaPartidas { partidas = historialPartidas.ToArray() };
        string json = JsonUtility.ToJson(lista, true);
        
        File.WriteAllText(rutaBaseDatosLocal, json);
        Debug.Log($"[BD Local] Partida guardada localmente en: {rutaBaseDatosLocal}");

        // Intentar enviar al portal inmediatamente
        StartCoroutine(SincronizarConPortalDocente(nuevaPartida));
    }

    private void CargarBaseDatosLocal()
    {
        if (File.Exists(rutaBaseDatosLocal))
        {
            string json = File.ReadAllText(rutaBaseDatosLocal);
            ListaPartidas lista = JsonUtility.FromJson<ListaPartidas>(json);
            if (lista != null && lista.partidas != null)
            {
                historialPartidas.AddRange(lista.partidas);
                Debug.Log($"[BD Local] {historialPartidas.Count} partidas cargadas desde caché local.");
            }
        }
    }

    // --- 2. LÓGICA DE CONEXIÓN AL PORTAL DOCENTE (API REST) ---

    private IEnumerator SincronizarConPortalDocente(DatosPartida partida)
    {
        string endpoint = urlPortalDocente + "/api/resultados/registrar"; // Deberás crear este endpoint en tu .NET 8
        string jsonPayload = JsonUtility.ToJson(partida);

        using (UnityWebRequest request = new UnityWebRequest(endpoint, "POST"))
        {
            byte[] bodyRaw = System.Text.Encoding.UTF8.GetBytes(jsonPayload);
            request.uploadHandler = new UploadHandlerRaw(bodyRaw);
            request.downloadHandler = new DownloadHandlerBuffer();
            request.SetRequestHeader("Content-Type", "application/json");

            // Si tienes un token JWT de login, lo pasarías aquí:
            // request.SetRequestHeader("Authorization", "Bearer " + token);

            Debug.Log("[API] Enviando resultado al Portal Docente...");
            yield return request.SendWebRequest();

            if (request.result == UnityWebRequest.Result.Success)
            {
                Debug.Log("[API] ¡Resultado guardado con éxito en el Portal Docente!");
                partida.sincronizadoConPortal = true;
                
                // Actualizar la base local para marcarlo como sincronizado
                ListaPartidas lista = new ListaPartidas { partidas = historialPartidas.ToArray() };
                File.WriteAllText(rutaBaseDatosLocal, JsonUtility.ToJson(lista, true));
            }
            else
            {
                Debug.LogWarning($"[API] Error de conexión: {request.error}. El resultado quedó guardado en la BD Local y se reintentará luego.");
            }
        }
    }
}