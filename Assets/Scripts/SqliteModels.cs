using System;
using SQLite;

[Table("usuarios")]
public class UsuarioLocal
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed]
    public string Correo { get; set; }

    public string NombreCompleto { get; set; }
    public string Rol { get; set; }
    public string CodigoInstitucional { get; set; }
    public string UltimoAcceso { get; set; }
    public int BienvenidaCompletada { get; set; } // 0 = Pendiente (Nuevo), 1 = Completada (Ya ingresado)
}

[Table("escenarios")]
public class EscenarioLocal
{
    [PrimaryKey]
    public string Id { get; set; }

    public string Nombre { get; set; }
    public string Dificultad { get; set; }
    public float PresupuestoInicial { get; set; }
    public int Activo { get; set; }
}

[Table("niveles")]
public class NivelLocal
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed]
    public string EscenarioId { get; set; }

    public int Numero { get; set; }
    public float PresupuestoInicial { get; set; }
}

[Table("partidas")]
public class PartidaLocal
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed]
    public string EstudianteId { get; set; }

    public string NivelId { get; set; }
    public string IdempotencyKey { get; set; }
    public string EscenarioNombre { get; set; }
    public string FechaCreacion { get; set; }
    public int Finalizada { get; set; } // 1 = Si, 0 = No
    public int Sincronizado { get; set; } // 0 = Pendiente (offline), 1 = Sincronizado con Neon
}

[Table("kpi")]
public class KpiLocal
{
    [PrimaryKey]
    public string Id { get; set; }

    [Indexed]
    public string PartidaId { get; set; }

    public float CostoTotal { get; set; }
    public float PctEntregasATiempo { get; set; }
    public float UtilizacionRecursos { get; set; }
    public float EficienciaRutas { get; set; }
    public float PuntuacionGlobal { get; set; }
    public string FechaCreacion { get; set; }
}
