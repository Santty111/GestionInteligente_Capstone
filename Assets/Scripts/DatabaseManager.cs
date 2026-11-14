using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using UnityEngine;
using SQLite;

public class DatabaseManager : MonoBehaviour
{
    private static DatabaseManager _instancia;
    public static DatabaseManager Instancia
    {
        get
        {
            if (_instancia == null)
            {
                _instancia = FindAnyObjectByType<DatabaseManager>();
                if (_instancia == null)
                {
                    GameObject go = new GameObject("DatabaseManager");
                    _instancia = go.AddComponent<DatabaseManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[DatabaseManager] Instancia creada automáticamente.");
                }
            }
            return _instancia;
        }
        private set => _instancia = value;
    }

    [Header("Configuración de SQLite")]
    [Tooltip("Nombre del archivo local de SQLite")]
    public string nombreArchivoSqlite = "gestion_logistica.db";

    private string rutaBaseDatosSqlite;
    private string rutaBaseDatosJson;

    private void Awake()
    {
        if (_instancia == null)
        {
            _instancia = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);

            // Rutas de persistencia
            rutaBaseDatosSqlite = Path.Combine(Application.persistentDataPath, nombreArchivoSqlite);
            rutaBaseDatosJson = Path.Combine(Application.persistentDataPath, "BaseDatosLocal_Resultados.json");

            // Inicializar base de datos local SQLite
            InicializarBaseDatosSqlite();
        }
        else if (_instancia != this)
        {
            Destroy(gameObject);
        }
    }

    // --- INICIALIZACIÓN DE SQLITE ---

    private void InicializarBaseDatosSqlite()
    {
        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                // Crear tablas si no existen
                db.CreateTable<UsuarioLocal>();
                db.CreateTable<EscenarioLocal>();
                db.CreateTable<NivelLocal>();
                db.CreateTable<PartidaLocal>();
                db.CreateTable<KpiLocal>();

                // Sembrar datos iniciales si la tabla de escenarios está vacía
                if (db.Table<EscenarioLocal>().Count() == 0)
                {
                    db.Insert(new EscenarioLocal
                    {
                        Id = "70000000-0000-0000-0000-000000000001",
                        Nombre = "Escenario Nacional - Prueba",
                        Dificultad = "facil",
                        PresupuestoInicial = 50000f,
                        Activo = 1
                    });
                }

                if (db.Table<NivelLocal>().Count() == 0)
                {
                    db.Insert(new NivelLocal
                    {
                        Id = "80000000-0000-0000-0000-000000000001",
                        EscenarioId = "70000000-0000-0000-0000-000000000001",
                        Numero = 1,
                        PresupuestoInicial = 50000f
                    });
                }

                int partidasCount = db.Table<PartidaLocal>().Count();
                int usuariosCount = db.Table<UsuarioLocal>().Count();

                Debug.Log($"<color=cyan>[SQLite] Base de datos local lista en: {rutaBaseDatosSqlite}</color>");
                Debug.Log($"[SQLite] Estado: {usuariosCount} usuarios, {partidasCount} partidas registradas.");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SQLite] Error inicializando base de datos local: {ex.Message}\n{ex.StackTrace}");
        }
    }

    // --- GUARDAR RESULTADOS DE PARTIDA ---

    public void GuardarResultadoLocal(DatosPartida nuevaPartida)
    {
        string partidaId = Guid.NewGuid().ToString();
        string kpiId = Guid.NewGuid().ToString();

        // 1. Guardar siempre en SQLite local (Offline-first)
        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                var partidaLocal = new PartidaLocal
                {
                    Id = partidaId,
                    EstudianteId = PortalBridgeManager.Instance?.usuarioActual?.estudianteIdGuid ?? "30000000-0000-0000-0000-000000000001",
                    NivelId = PortalBridgeManager.Instance?.escenarioActivo?.nivelIdGuid ?? "80000000-0000-0000-0000-000000000001",
                    IdempotencyKey = Guid.NewGuid().ToString(),
                    EscenarioNombre = nuevaPartida.escenario,
                    FechaCreacion = nuevaPartida.fecha,
                    Finalizada = 1,
                    Sincronizado = 0 // Marcado inicialmente como no sincronizado
                };

                var kpiLocal = new KpiLocal
                {
                    Id = kpiId,
                    PartidaId = partidaId,
                    CostoTotal = nuevaPartida.costoTotal,
                    PctEntregasATiempo = nuevaPartida.entregasATiempo,
                    UtilizacionRecursos = nuevaPartida.utilizacionRecursos,
                    EficienciaRutas = nuevaPartida.eficienciaRutas,
                    PuntuacionGlobal = nuevaPartida.puntajeGlobal,
                    FechaCreacion = nuevaPartida.fecha
                };

                db.Insert(partidaLocal);
                db.Insert(kpiLocal);

                Debug.Log($"<color=cyan>[SQLite] Partida guardada localmente (ID: {partidaId}) en SQLite.</color>");
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SQLite] Error al guardar partida en SQLite: {ex.Message}");
        }

        // 2. Respaldo adicional en JSON
        GuardarRespaldoJson(nuevaPartida);

        // 3. Sincronizar con Neon PostgreSQL si hay conexión
        if (PortalBridgeManager.Instance != null && !PortalBridgeManager.Instance.usarModoOffline)
        {
            var resultadoDto = new ResultadoPartidaDto
            {
                nombreEstudiante = PortalBridgeManager.Instance.usuarioActual?.nombre ?? "Estudiante",
                ruta = nuevaPartida.escenario,
                dificultad = "facil",
                costoTotal = nuevaPartida.costoTotal,
                entregasATiempoPct = nuevaPartida.entregasATiempo,
                utilizacionRecursosPct = nuevaPartida.utilizacionRecursos,
                eficienciaRutasPct = nuevaPartida.eficienciaRutas,
                eventoAduanaSuperado = true,
                puntajeFinal = (float)Math.Round(nuevaPartida.puntajeGlobal, 2),
                fecha = nuevaPartida.fecha
            };

            PortalBridgeManager.Instance.GuardarResultadoPartida(resultadoDto, exito =>
            {
                if (exito)
                {
                    nuevaPartida.sincronizadoConPortal = true;
                    MarcarPartidaSincronizadaEnSqlite(partidaId);
                    Debug.Log("<color=green>[DatabaseManager] ¡Partida sincronizada exitosamente con Neon PostgreSQL!</color>");
                }
                else
                {
                    Debug.LogWarning("[DatabaseManager] No se pudo sincronizar en la nube. La partida quedó guardada en SQLite para sincronizarse luego.");
                }
            });
        }
    }

    private void MarcarPartidaSincronizadaEnSqlite(string partidaId)
    {
        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                var partida = db.Find<PartidaLocal>(partidaId);
                if (partida != null)
                {
                    partida.Sincronizado = 1;
                    db.Update(partida);
                    Debug.Log($"[SQLite] Partida {partidaId} marcada como SINCRONIZADA.");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SQLite] Error actualizando estado de sincronización: {ex.Message}");
        }
    }

    // --- COLA DE SINCRONIZACIÓN DE PARTIDAS PENDIENTES ---

    [ContextMenu("Sincronizar Partidas Pendientes con Neon")]
    public void SincronizarPartidasPendientes()
    {
        if (PortalBridgeManager.Instance == null || PortalBridgeManager.Instance.usarModoOffline)
        {
            Debug.LogWarning("[SQLite Sync] Modo offline activo o PortalBridgeManager no disponible.");
            return;
        }

        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                var pendientes = db.Table<PartidaLocal>().Where(p => p.Sincronizado == 0).ToList();
                Debug.Log($"[SQLite Sync] Se encontraron {pendientes.Count} partidas pendientes de sincronizar.");

                foreach (var p in pendientes)
                {
                    var kpi = db.Table<KpiLocal>().FirstOrDefault(k => k.PartidaId == p.Id);
                    if (kpi != null)
                    {
                        var resultadoDto = new ResultadoPartidaDto
                        {
                            nombreEstudiante = PortalBridgeManager.Instance.usuarioActual?.nombre ?? "Estudiante",
                            ruta = p.EscenarioNombre,
                            dificultad = "facil",
                            costoTotal = kpi.CostoTotal,
                            entregasATiempoPct = kpi.PctEntregasATiempo,
                            utilizacionRecursosPct = kpi.UtilizacionRecursos,
                            eficienciaRutasPct = kpi.EficienciaRutas,
                            eventoAduanaSuperado = true,
                            puntajeFinal = (float)Math.Round(kpi.PuntuacionGlobal, 2),
                            fecha = p.FechaCreacion
                        };

                        string idActual = p.Id;
                        PortalBridgeManager.Instance.GuardarResultadoPartida(resultadoDto, subido =>
                        {
                            if (subido)
                            {
                                MarcarPartidaSincronizadaEnSqlite(idActual);
                            }
                        });
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogError($"[SQLite Sync] Error en sincronización de cola: {ex.Message}");
        }
    }

    // --- REGISTRAR USUARIO EN SQLITE LOCAL ---

    public void GuardarUsuarioLocal(PerfilUsuarioDto perfil)
    {
        if (perfil == null) return;

        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                string uId = !string.IsNullOrEmpty(perfil.usuarioIdGuid) ? perfil.usuarioIdGuid : Guid.NewGuid().ToString();
                var usuarioExistente = db.Find<UsuarioLocal>(uId);
                int bienvenida = usuarioExistente != null ? usuarioExistente.BienvenidaCompletada : (perfil.esNuevo ? 0 : 1);

                var usuario = new UsuarioLocal
                {
                    Id = uId,
                    Correo = perfil.correo,
                    NombreCompleto = perfil.nombre,
                    Rol = perfil.rol,
                    CodigoInstitucional = perfil.codigoInstitucional,
                    UltimoAcceso = DateTime.UtcNow.ToString("o"),
                    BienvenidaCompletada = bienvenida
                };

                db.InsertOrReplace(usuario);
                Debug.Log($"[SQLite] Usuario '{usuario.NombreCompleto}' cacheado localmente. Bienvenida completada: {bienvenida == 1}");
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SQLite] No se pudo cachear el usuario local: {ex.Message}");
        }
    }

    // --- ESTADO DE BIENVENIDA / USUARIO NUEVO ---

    public bool EsUsuarioNuevo(string usuarioId, string correo)
    {
        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                // Si el estudiante ya tiene partidas guardadas, definitivamente ya ha jugado
                if (!string.IsNullOrEmpty(usuarioId))
                {
                    int cantPartidas = db.Table<PartidaLocal>().Where(p => p.EstudianteId == usuarioId).Count();
                    if (cantPartidas > 0) return false;
                }

                // Buscar usuario en caché local
                UsuarioLocal u = null;
                if (!string.IsNullOrEmpty(usuarioId))
                {
                    u = db.Find<UsuarioLocal>(usuarioId);
                }
                if (u == null && !string.IsNullOrEmpty(correo))
                {
                    u = db.Table<UsuarioLocal>().FirstOrDefault(x => x.Correo.ToLower() == correo.ToLower());
                }

                if (u != null && u.BienvenidaCompletada == 1)
                {
                    return false;
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SQLite] Error consultando si el usuario es nuevo: {ex.Message}");
        }

        return true;
    }

    public void MarcarBienvenidaCompletada(string usuarioId, string correo)
    {
        try
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                UsuarioLocal u = null;
                if (!string.IsNullOrEmpty(usuarioId))
                {
                    u = db.Find<UsuarioLocal>(usuarioId);
                }
                if (u == null && !string.IsNullOrEmpty(correo))
                {
                    u = db.Table<UsuarioLocal>().FirstOrDefault(x => x.Correo.ToLower() == correo.ToLower());
                }

                if (u != null)
                {
                    u.BienvenidaCompletada = 1;
                    u.UltimoAcceso = DateTime.UtcNow.ToString("o");
                    db.Update(u);
                    Debug.Log($"<color=cyan>[SQLite] Usuario '{u.NombreCompleto}' marcado con BienvenidaCompletada = 1.</color>");
                }
                else if (!string.IsNullOrEmpty(usuarioId))
                {
                    db.Insert(new UsuarioLocal
                    {
                        Id = usuarioId,
                        Correo = correo ?? "",
                        NombreCompleto = PortalBridgeManager.Instance?.usuarioActual?.nombre ?? "Estudiante",
                        Rol = PortalBridgeManager.Instance?.usuarioActual?.rol ?? "Estudiante",
                        CodigoInstitucional = PortalBridgeManager.Instance?.usuarioActual?.codigoInstitucional ?? "",
                        UltimoAcceso = DateTime.UtcNow.ToString("o"),
                        BienvenidaCompletada = 1
                    });
                    Debug.Log($"<color=cyan>[SQLite] Nuevo usuario {usuarioId} creado y marcado con BienvenidaCompletada = 1.</color>");
                }
            }
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[SQLite] Error al marcar bienvenida completada: {ex.Message}");
        }
    }

    // --- INSPECCIÓN Y UTILIDADES ---

    [ContextMenu("Verificar Base de Datos SQLite")]
    public void VerificarBaseDatosSqlite()
    {
        Debug.Log($"=== REPORTE DE BASE DE DATOS SQLITE ===");
        Debug.Log($"Ubicación: {rutaBaseDatosSqlite}");
        Debug.Log($"Existe archivo: {File.Exists(rutaBaseDatosSqlite)}");

        if (File.Exists(rutaBaseDatosSqlite))
        {
            using (var db = new SQLiteConnection(rutaBaseDatosSqlite))
            {
                Debug.Log($"Tabla usuarios: {db.Table<UsuarioLocal>().Count()} registros");
                Debug.Log($"Tabla escenarios: {db.Table<EscenarioLocal>().Count()} registros");
                Debug.Log($"Tabla niveles: {db.Table<NivelLocal>().Count()} registros");
                Debug.Log($"Tabla partidas: {db.Table<PartidaLocal>().Count()} registros ({db.Table<PartidaLocal>().Where(p => p.Sincronizado == 0).Count()} pendientes)");
                Debug.Log($"Tabla kpi: {db.Table<KpiLocal>().Count()} registros");
            }
        }
    }

    private void GuardarRespaldoJson(DatosPartida nuevaPartida)
    {
        try
        {
            List<DatosPartida> historial = new List<DatosPartida>();
            if (File.Exists(rutaBaseDatosJson))
            {
                string jsonExistente = File.ReadAllText(rutaBaseDatosJson);
                var listaExistente = JsonUtility.FromJson<ListaPartidas>(jsonExistente);
                if (listaExistente != null && listaExistente.partidas != null)
                {
                    historial.AddRange(listaExistente.partidas);
                }
            }
            historial.Add(nuevaPartida);
            ListaPartidas lista = new ListaPartidas { partidas = historial.ToArray() };
            File.WriteAllText(rutaBaseDatosJson, JsonUtility.ToJson(lista, true));
        }
        catch (Exception ex)
        {
            Debug.LogWarning($"[Respaldo JSON] {ex.Message}");
        }
    }
}
