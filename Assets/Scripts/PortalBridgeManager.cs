using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using UnityEngine;
using Npgsql;

public class PortalBridgeManager : MonoBehaviour
{
    private static PortalBridgeManager _instance;
    public static PortalBridgeManager Instance
    {
        get
        {
            if (_instance == null)
            {
                _instance = FindAnyObjectByType<PortalBridgeManager>();
                if (_instance == null)
                {
                    GameObject go = new GameObject("PortalBridge");
                    _instance = go.AddComponent<PortalBridgeManager>();
                    DontDestroyOnLoad(go);
                    Debug.Log("[PortalBridgeManager] Instancia creada automáticamente.");
                }
            }
            return _instance;
        }
        private set => _instance = value;
    }

    [Header("Configuración de Conexión Neon PostgreSQL")]
    [Tooltip("Cadena de conexión directa a la base de datos Neon PostgreSQL")]
    public string cadenaConexionPostgres = "Host=ep-fragrant-tree-b5zv9zsg-pooler.c-7.us-east-2.aws.neon.tech; Database=neondb; Username=neondb_owner; Password=npg_Lb2SnucAoPT5; SSL Mode=Require; Trust Server Certificate=true;";

    [Header("Opciones de Ejecución")]
    [Tooltip("Si está activo, usa credenciales simuladas en memoria sin consultar internet.")]
    public bool usarModoOffline = false;
    public int timeoutSegundos = 10;

    [Header("Estado de Sesión Actual")]
    public PerfilUsuarioDto usuarioActual;
    public ConfiguracionEscenarioDto escenarioActivo;
    public List<ResultadoPartidaDto> historialLocal = new List<ResultadoPartidaDto>();

    private SynchronizationContext mainThreadContext;

    private void Awake()
    {
        if (_instance == null)
        {
            _instance = this;
            if (transform.parent != null)
            {
                transform.SetParent(null);
            }
            DontDestroyOnLoad(gameObject);
            mainThreadContext = SynchronizationContext.Current;
            CargarDatosPorDefectoOffline();
        }
        else if (_instance != this)
        {
            Destroy(gameObject);
        }
    }

    private void CargarDatosPorDefectoOffline()
    {
        usuarioActual = new PerfilUsuarioDto
        {
            estudianteId = 1,
            usuarioIdGuid = "10000000-0000-0000-0000-000000000002",
            estudianteIdGuid = "30000000-0000-0000-0000-000000000001",
            nombre = "Santiago Conlago",
            correo = "santiago.conlago@udla.edu.ec",
            rol = "Estudiante",
            codigoInstitucional = "UDLA-EST-0001"
        };

        escenarioActivo = new ConfiguracionEscenarioDto
        {
            escenarioId = 1,
            escenarioIdGuid = "70000000-0000-0000-0000-000000000001",
            nivelIdGuid = "80000000-0000-0000-0000-000000000001",
            nombreRuta = "Escenario Nacional - Prueba",
            dificultad = "facil",
            presupuestoInicial = 50000f,
            capacidadMaximaCamion = 100f,
            incluyeEventoAduanaSENAE = true
        };
    }

    // --- AUTENTICACIÓN / LOGIN ---

    public void IniciarSesion(string correo, string password, Action<bool, string> callback)
    {
        if (usarModoOffline)
        {
            usuarioActual.correo = string.IsNullOrEmpty(correo) ? "santiago.conlago@udla.edu.ec" : correo;
            usuarioActual.rol = correo.Contains("docente") ? "Docente" : "Estudiante";

            // Determinar si es usuario nuevo en modo offline
            bool bienvenidaCompletada = PlayerPrefs.GetInt("BienvenidaCompletada_" + usuarioActual.correo.ToLower(), 0) == 1
                                     || PlayerPrefs.GetInt("BienvenidaCompletada_" + usuarioActual.usuarioIdGuid, 0) == 1;
            bool esNuevoSqlite = DatabaseManager.Instancia == null || DatabaseManager.Instancia.EsUsuarioNuevo(usuarioActual.usuarioIdGuid, usuarioActual.correo);
            usuarioActual.esNuevo = !bienvenidaCompletada && esNuevoSqlite;

            DatabaseManager.Instancia?.GuardarUsuarioLocal(usuarioActual);

            Debug.Log($"[Modo Offline] Login simulado exitoso como: {usuarioActual.rol}. ¿Es nuevo?: {usuarioActual.esNuevo}");
            callback?.Invoke(true, usuarioActual.rol);
            return;
        }

        // Ejecutar consulta a PostgreSQL en hilo secundario
        Task.Run(async () =>
        {
            try
            {
                var resultadoLogin = await AutenticarEnPostgresAsync(correo.Trim(), password);
                
                EjecutarEnHiloPrincipal(() =>
                {
                    if (resultadoLogin.exito)
                    {
                        Debug.Log($"<color=green>[Neon DB] Login exitoso para {usuarioActual.nombre} ({usuarioActual.correo}) con rol {usuarioActual.rol}</color>");
                        callback?.Invoke(true, usuarioActual.rol);
                    }
                    else
                    {
                        Debug.LogWarning($"[Neon DB] Login fallido: {resultadoLogin.mensaje}");
                        callback?.Invoke(false, resultadoLogin.mensaje);
                    }
                });
            }
            catch (Exception ex)
            {
                EjecutarEnHiloPrincipal(() =>
                {
                    Debug.LogError($"[Neon DB] Error de conexión: {ex.Message}");
                    callback?.Invoke(false, $"Error al conectar con la base de datos: {ex.Message}");
                });
            }
        });
    }

    private async Task<(bool exito, string mensaje)> AutenticarEnPostgresAsync(string correo, string password)
    {
        using (var conn = new NpgsqlConnection(cadenaConexionPostgres))
        {
            await conn.OpenAsync();

            string query = @"
                SELECT u.id, u.correo, u.nombre_completo, u.password_hash, u.activo, 
                       r.nombre AS rol_nombre, 
                       e.id AS estudiante_id, e.codigo_institucional
                FROM usuarios u
                LEFT JOIN usuarios_roles ur ON u.id = ur.usuario_id
                LEFT JOIN roles r ON ur.rol_id = r.id
                LEFT JOIN estudiantes e ON u.id = e.usuario_id
                WHERE LOWER(u.correo) = LOWER(@correo)
                LIMIT 1;
            ";

            using (var cmd = new NpgsqlCommand(query, conn))
            {
                cmd.Parameters.AddWithValue("@correo", correo);

                using (var reader = await cmd.ExecuteReaderAsync())
                {
                    if (!await reader.ReadAsync())
                    {
                        return (false, "El correo electrónico no se encuentra registrado.");
                    }

                    var userId = reader.GetGuid(0);
                    var correoDb = reader.GetString(1);
                    var nombreCompleto = reader.GetString(2);
                    var hashDb = reader.IsDBNull(3) ? "" : reader.GetString(3);
                    var activo = reader.GetBoolean(4);
                    var rol = reader.IsDBNull(5) ? "Estudiante" : reader.GetString(5);
                    var estId = reader.IsDBNull(6) ? Guid.Empty : reader.GetGuid(6);
                    var codInst = reader.IsDBNull(7) ? "" : reader.GetString(7);

                    if (!activo)
                    {
                        return (false, "La cuenta de usuario está desactivada.");
                    }

                    // Verificación de contraseña con BCrypt
                    bool passwordValido = false;
                    try
                    {
                        if (hashDb.StartsWith("$2a$") || hashDb.StartsWith("$2b$") || hashDb.StartsWith("$2y$"))
                        {
                            passwordValido = BCrypt.Net.BCrypt.Verify(password, hashDb);
                        }
                        else
                        {
                            passwordValido = (password == hashDb);
                        }
                    }
                    catch
                    {
                        passwordValido = (password == hashDb);
                    }

                    if (!passwordValido)
                    {
                        return (false, "Contraseña incorrecta.");
                    }

                    // Asignar perfil de usuario activo
                    usuarioActual = new PerfilUsuarioDto
                    {
                        estudianteId = 1,
                        usuarioIdGuid = userId.ToString(),
                        estudianteIdGuid = estId != Guid.Empty ? estId.ToString() : "",
                        nombre = nombreCompleto,
                        correo = correoDb,
                        rol = rol,
                        codigoInstitucional = codInst
                    };
                }
            }

            // Si es estudiante pero no tenía registro en la tabla estudiantes, crear uno automáticamente
            if (string.IsNullOrEmpty(usuarioActual.estudianteIdGuid) && usuarioActual.rol.ToLower().Contains("estudiante"))
            {
                Guid nuevoEstId = Guid.NewGuid();
                string insertEst = "INSERT INTO estudiantes (id, usuario_id, codigo_institucional, creado_en) VALUES (@id, @uId, @codigo, NOW());";
                using (var cmdEst = new NpgsqlCommand(insertEst, conn))
                {
                    cmdEst.Parameters.AddWithValue("@id", nuevoEstId);
                    cmdEst.Parameters.AddWithValue("@uId", Guid.Parse(usuarioActual.usuarioIdGuid));
                    cmdEst.Parameters.AddWithValue("@codigo", "UDLA-EST-AUTO");
                    await cmdEst.ExecuteNonQueryAsync();
                }
                usuarioActual.estudianteIdGuid = nuevoEstId.ToString();
                usuarioActual.codigoInstitucional = "UDLA-EST-AUTO";
            }

            // Cargar escenario activo desde la base de datos
            await CargarEscenarioActivoAsync(conn);

            // Verificar si el usuario ya tiene partidas registradas en Neon
            int partidasCount = 0;
            if (!string.IsNullOrEmpty(usuarioActual.estudianteIdGuid) && Guid.TryParse(usuarioActual.estudianteIdGuid, out Guid estGuid))
            {
                try
                {
                    using (var cmdPartidas = new NpgsqlCommand("SELECT COUNT(*) FROM partidas WHERE estudiante_id = @estId;", conn))
                    {
                        cmdPartidas.Parameters.AddWithValue("@estId", estGuid);
                        var countRes = await cmdPartidas.ExecuteScalarAsync();
                        if (countRes != null && int.TryParse(countRes.ToString(), out int c))
                        {
                            partidasCount = c;
                        }
                    }
                }
                catch (Exception ex)
                {
                    Debug.LogWarning($"[Neon DB] No se pudo verificar partidas en la nube: {ex.Message}");
                }
            }

            // Verificar si completó inducción en PlayerPrefs o SQLite local
            bool bienvenidaCompletadaPrefs = 
                PlayerPrefs.GetInt("BienvenidaCompletada_" + usuarioActual.correo.ToLower(), 0) == 1 ||
                PlayerPrefs.GetInt("BienvenidaCompletada_" + usuarioActual.usuarioIdGuid, 0) == 1;

            bool esNuevoSqlite = DatabaseManager.Instancia == null || DatabaseManager.Instancia.EsUsuarioNuevo(usuarioActual.usuarioIdGuid, usuarioActual.correo);

            usuarioActual.esNuevo = (partidasCount == 0) && !bienvenidaCompletadaPrefs && esNuevoSqlite;

            Debug.Log($"<color=cyan>[Neon DB] Usuario verificado: {usuarioActual.nombre}. Partidas en nube: {partidasCount}. ¿Es nuevo?: {usuarioActual.esNuevo}</color>");

            // Cachear usuario localmente en SQLite
            EjecutarEnHiloPrincipal(() =>
            {
                DatabaseManager.Instancia?.GuardarUsuarioLocal(usuarioActual);
            });

            // Registrar último acceso
            try
            {
                using (var updateCmd = new NpgsqlCommand("UPDATE usuarios SET ultimo_acceso_en = NOW() WHERE LOWER(correo) = LOWER(@correo);", conn))
                {
                    updateCmd.Parameters.AddWithValue("@correo", correo);
                    await updateCmd.ExecuteNonQueryAsync();
                }
            }
            catch (Exception ex)
            {
                Debug.LogWarning($"[Neon DB] No se pudo actualizar ultimo_acceso_en: {ex.Message}");
            }

            return (true, "OK");
        }
    }

    private async Task CargarEscenarioActivoAsync(NpgsqlConnection conn)
    {
        string sqlEscenario = @"
            SELECT e.id, e.nombre, e.dificultad, 
                   n.id AS nivel_id, n.numero, n.presupuesto_inicial
            FROM escenarios e
            LEFT JOIN niveles n ON e.id = n.escenario_id
            WHERE e.activo = true
            ORDER BY n.numero ASC
            LIMIT 1;
        ";

        using (var cmd = new NpgsqlCommand(sqlEscenario, conn))
        using (var reader = await cmd.ExecuteReaderAsync())
        {
            if (await reader.ReadAsync())
            {
                var escId = reader.GetGuid(0);
                var escNombre = reader.GetString(1);
                var dificultad = reader.GetString(2);
                var nivelId = reader.IsDBNull(3) ? Guid.Empty : reader.GetGuid(3);
                var pres = reader.IsDBNull(5) ? 50000f : Convert.ToSingle(reader.GetDecimal(5));

                escenarioActivo = new ConfiguracionEscenarioDto
                {
                    escenarioId = 1,
                    escenarioIdGuid = escId.ToString(),
                    nivelIdGuid = nivelId != Guid.Empty ? nivelId.ToString() : "",
                    nombreRuta = escNombre,
                    dificultad = dificultad,
                    presupuestoInicial = pres,
                    capacidadMaximaCamion = 100f,
                    incluyeEventoAduanaSENAE = true
                };

                Debug.Log($"[Neon DB] Escenario activo cargado: {escNombre} (Dificultad: {dificultad}, Presupuesto: ${pres:N2})");
            }
        }
    }

    // --- GUARDAR RESULTADOS DE PARTIDA EN NEON POSTGRESQL ---

    public void GuardarResultadoPartida(ResultadoPartidaDto resultado, Action<bool> onCompletado = null)
    {
        historialLocal.Add(resultado);

        if (usarModoOffline)
        {
            onCompletado?.Invoke(true);
            return;
        }

        Task.Run(async () =>
        {
            try
            {
                using (var conn = new NpgsqlConnection(cadenaConexionPostgres))
                {
                    await conn.OpenAsync();

                    Guid partidaId = Guid.NewGuid();
                    Guid kpiId = Guid.NewGuid();
                    Guid resultadoPartidaId = Guid.NewGuid();

                    // Obtener o resolver estudiante_id
                    Guid estGuid;
                    if (!Guid.TryParse(usuarioActual?.estudianteIdGuid, out estGuid) || estGuid == Guid.Empty)
                    {
                        // Buscar si existe estudiante con este usuario
                        if (usuarioActual != null && Guid.TryParse(usuarioActual.usuarioIdGuid, out var uGuid))
                        {
                            using (var qCmd = new NpgsqlCommand("SELECT id FROM estudiantes WHERE usuario_id = @uId LIMIT 1;", conn))
                            {
                                qCmd.Parameters.AddWithValue("@uId", uGuid);
                                var res = await qCmd.ExecuteScalarAsync();
                                if (res != null)
                                {
                                    estGuid = (Guid)res;
                                }
                                else
                                {
                                    estGuid = Guid.NewGuid();
                                    using (var cCmd = new NpgsqlCommand("INSERT INTO estudiantes (id, usuario_id, codigo_institucional, creado_en) VALUES (@id, @uId, 'UDLA-EST', NOW());", conn))
                                    {
                                        cCmd.Parameters.AddWithValue("@id", estGuid);
                                        cCmd.Parameters.AddWithValue("@uId", uGuid);
                                        await cCmd.ExecuteNonQueryAsync();
                                    }
                                }
                            }
                        }
                        else
                        {
                            // Fallback al estudiante de prueba existente
                            estGuid = Guid.Parse("30000000-0000-0000-0000-000000000001");
                        }
                    }

                    // Resolver nivel_id
                    Guid nivelGuid;
                    if (!Guid.TryParse(escenarioActivo?.nivelIdGuid, out nivelGuid) || nivelGuid == Guid.Empty)
                    {
                        using (var nCmd = new NpgsqlCommand("SELECT id FROM niveles LIMIT 1;", conn))
                        {
                            var nRes = await nCmd.ExecuteScalarAsync();
                            nivelGuid = nRes != null ? (Guid)nRes : Guid.Parse("80000000-0000-0000-0000-000000000001");
                        }
                    }

                    using (var trans = await conn.BeginTransactionAsync())
                    {
                        try
                        {
                            // 1. Insertar en tabla 'partidas'
                            string insertPartida = @"
                                INSERT INTO partidas (
                                    id, estudiante_id, nivel_id, idempotency_key, 
                                    version_registro, origen_registro, estado_sincronizacion, 
                                    fecha_creacion, fecha_recepcion, finalizada, eliminado
                                ) VALUES (
                                    @id, @estId, @nivelId, @idempotency,
                                    1, 'local'::origen_registro_enum, 'sincronizado'::sync_status_enum,
                                    NOW(), NOW(), true, false
                                );
                            ";

                            using (var cmd = new NpgsqlCommand(insertPartida, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@id", partidaId);
                                cmd.Parameters.AddWithValue("@estId", estGuid);
                                cmd.Parameters.AddWithValue("@nivelId", nivelGuid);
                                cmd.Parameters.AddWithValue("@idempotency", Guid.NewGuid().ToString());
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // 2. Insertar en tabla 'kpi'
                            string insertKpi = @"
                                INSERT INTO kpi (
                                    id, partida_id, costo_total, pct_entregas_a_tiempo, 
                                    utilizacion_recursos, eficiencia_rutas, puntuacion_global, fecha_creacion
                                ) VALUES (
                                    @id, @partidaId, @costo, @entregas, 
                                    @recursos, @eficiencia, @puntaje, NOW()
                                );
                            ";

                            using (var cmd = new NpgsqlCommand(insertKpi, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@id", kpiId);
                                cmd.Parameters.AddWithValue("@partidaId", partidaId);
                                cmd.Parameters.AddWithValue("@costo", (decimal)resultado.costoTotal);
                                cmd.Parameters.AddWithValue("@entregas", (decimal)resultado.entregasATiempoPct);
                                cmd.Parameters.AddWithValue("@recursos", (decimal)resultado.utilizacionRecursosPct);
                                cmd.Parameters.AddWithValue("@eficiencia", (decimal)resultado.eficienciaRutasPct);
                                cmd.Parameters.AddWithValue("@puntaje", (decimal)resultado.puntajeFinal);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            // 3. Insertar en tabla 'resultados_partida' (usada por el portal para reportes y dashboard)
                            string insertResultado = @"
                                INSERT INTO resultados_partida (
                                    id, estudiante_id, kpi_id, generado_en
                                ) VALUES (
                                    @id, @estId, @kpiId, NOW()
                                );
                            ";

                            using (var cmd = new NpgsqlCommand(insertResultado, conn, trans))
                            {
                                cmd.Parameters.AddWithValue("@id", resultadoPartidaId);
                                cmd.Parameters.AddWithValue("@estId", estGuid);
                                cmd.Parameters.AddWithValue("@kpiId", kpiId);
                                await cmd.ExecuteNonQueryAsync();
                            }

                            await trans.CommitAsync();

                            EjecutarEnHiloPrincipal(() =>
                            {
                                Debug.Log($"<color=green>[Neon DB] ¡PARTIDA Y RESULTADOS PUBLICADOS EXITOSAMENTE!</color>");
                                Debug.Log($"[Neon DB] Partida: {partidaId} | KPIs: {kpiId} | Puntaje: {resultado.puntajeFinal} pts | Costo: ${resultado.costoTotal:N2}");
                                onCompletado?.Invoke(true);
                            });
                        }
                        catch (Exception exTrans)
                        {
                            await trans.RollbackAsync();
                            throw exTrans;
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                EjecutarEnHiloPrincipal(() =>
                {
                    Debug.LogError($"[Neon DB] Error guardando partida en Neon: {ex.Message}\n{ex.StackTrace}");
                    onCompletado?.Invoke(false);
                });
            }
        });
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

    [ContextMenu("Probar Conexión Directa a Neon")]
    public void ProbarConexionNeon()
    {
        Debug.Log("[Neon DB] Probando conexión directa a Neon PostgreSQL...");
        Task.Run(async () =>
        {
            try
            {
                using (var conn = new NpgsqlConnection(cadenaConexionPostgres))
                {
                    await conn.OpenAsync();
                    using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM usuarios;", conn))
                    {
                        var count = await cmd.ExecuteScalarAsync();
                        EjecutarEnHiloPrincipal(() =>
                        {
                            Debug.Log($"<color=green>[Neon DB] ¡CONEXIÓN EXITOSA! Total usuarios registrados: {count}</color>");
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                EjecutarEnHiloPrincipal(() =>
                {
                    Debug.LogError($"[Neon DB] Error en prueba de conexión: {ex.Message}");
                });
            }
        });
    }

    private void EjecutarEnHiloPrincipal(Action accion)
    {
        if (mainThreadContext != null)
        {
            mainThreadContext.Post(_ => accion(), null);
        }
        else
        {
            accion();
        }
    }
}

[Serializable]
public class PerfilUsuarioDto
{
    public int estudianteId;
    public string usuarioIdGuid;
    public string estudianteIdGuid;
    public string nombre;
    public string correo;
    public string rol;
    public string codigoInstitucional;
    public bool esNuevo = true;
}

[Serializable]
public class ConfiguracionEscenarioDto
{
    public int escenarioId;
    public string escenarioIdGuid;
    public string nivelIdGuid;
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
