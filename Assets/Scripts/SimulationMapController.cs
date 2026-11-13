using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class SimulationMapController : MonoBehaviour
{
    [Header("HUD")]
    public TMP_Text txtPresupuesto;
    public TMP_Text txtRutaActiva;
    public TMP_Text txtEstadoMision;
    public Button btnVolverMenu;

    [Header("Nodos y Marcador")]
    public Button btnNodoGuayaquil;
    public Button btnNodoManta;
    public Button btnNodoEsmeraldas;
    public Transform nodoSENAE;
    public Transform nodoDestinoUI;
    public GameObject marcadorCamion;

    [Header("Modal Despacho")]
    public GameObject modalDespacho;
    public TMP_Text txtTituloModal;
    public TMP_Text txtDescripcionModal;
    public TMP_Dropdown dropdownTipoCarga;
    public Slider sliderCapacidad;
    public TMP_Text txtCapacidadValor;
    public Button btnConfirmarDespacho;
    public Button btnCerrarModal;

    [Header("Modal Eventos en Ruta")]
    public GameObject modalEventoRuta;
    public TMP_Text txtTituloEvento;
    public TMP_Text txtDescripcionEvento;
    public Button[] botonesOpciones; // Asignar los 4 botones aquí

    [Header("Modal Aduana SENAE")]
    public GameObject modalAduana;
    public TMP_Text txtAduanaResultado;
    public TMP_Text txtAduanaCostoTiempo;
    public Button btnPagarTasaAduanera;

    [Header("Panel Resultados")]
    public GameObject panelResultados;
    public TMP_Text txtKPI_Resultado;
    public TMP_Text txtKPI_Tiempo;
    public TMP_Text txtKPI_Finanzas;
    public Button btnReiniciarPartida;
    public Button btnSalirResultados;

    [Header("Navegación")]
    public GameObject panelSimulacion;
    public GameObject panelMainMenu;

    private List<EscenarioMision> listaEscenarios = new List<EscenarioMision>();
    private EscenarioMision escenarioSeleccionado;

    // Catálogos de eventos por dificultad
    private List<EventoRuta> eventosFacil = new List<EventoRuta>();
    private List<EventoRuta> eventosNormal = new List<EventoRuta>();
    private List<EventoRuta> eventosDificil = new List<EventoRuta>();

    private Vector3 posicionOrigenMision;
    private float presupuestoActual;
    private float tiempoTranscurridoHoras = 0f;
    private float puntajeAcumulado = 100f;
    private bool esperandoDecisionEvento = false;

    private void Awake()
    {
        GenerarEscenarios();
        InicializarEventosFacil();
        InicializarEventosNormal();
        InicializarEventosDificil();
    }

    private void OnEnable()
    {
        if (modalDespacho != null) modalDespacho.SetActive(false);
        if (modalEventoRuta != null) modalEventoRuta.SetActive(false);
        if (modalAduana != null) modalAduana.SetActive(false);
        if (panelResultados != null) panelResultados.SetActive(false);
        if (marcadorCamion != null) marcadorCamion.SetActive(false);

        tiempoTranscurridoHoras = 0f;
        puntajeAcumulado = 100f;
        esperandoDecisionEvento = false;

        if (txtEstadoMision != null)
            txtEstadoMision.text = "Seleccione un Punto de Partida (Nodo) en el Mapa";
    }

    private void Start()
    {
        if (btnNodoGuayaquil != null) btnNodoGuayaquil.onClick.AddListener(() => SeleccionarMision(0, btnNodoGuayaquil.transform));
        if (btnNodoManta != null) btnNodoManta.onClick.AddListener(() => SeleccionarMision(1, btnNodoManta.transform));
        if (btnNodoEsmeraldas != null) btnNodoEsmeraldas.onClick.AddListener(() => SeleccionarMision(2, btnNodoEsmeraldas.transform));

        if (btnVolverMenu != null) btnVolverMenu.onClick.AddListener(VolverAlMenuPrincipal);
        if (sliderCapacidad != null) sliderCapacidad.onValueChanged.AddListener(ActualizarTextoCapacidad);
        if (btnConfirmarDespacho != null) btnConfirmarDespacho.onClick.AddListener(ConfirmarSalidaCamion);
        if (btnCerrarModal != null) btnCerrarModal.onClick.AddListener(() => modalDespacho.SetActive(false));

        if (btnPagarTasaAduanera != null) btnPagarTasaAduanera.onClick.AddListener(ContinuarDesdeAduana);
        if (btnReiniciarPartida != null) btnReiniciarPartida.onClick.AddListener(ReintentarMision);
        if (btnSalirResultados != null) btnSalirResultados.onClick.AddListener(VolverAlMenuPrincipal);
    }

    private void GenerarEscenarios()
    {
        listaEscenarios.Clear();
        // Fácil: 75 segundos reales
        listaEscenarios.Add(new EscenarioMision {
            id = 0, nombreMision = "Ruta Costeña Sur", ciudadOrigen = "Guayaquil", ciudadDestino = "Quito",
            presupuestoInicial = 45000f, distanciaKM = 420f, dificultad = "Fácil",
            descripcion = "Misión introductoria. 75 segundos de trayecto hasta Aduana. Eventos mecánicos leves.",
            probabilidadAforoRojo = 0.15f, tasaAduaneraBase = 150f
        });
        // Normal: 90 segundos reales
        listaEscenarios.Add(new EscenarioMision {
            id = 1, nombreMision = "Enlace Manabita", ciudadOrigen = "Manta", ciudadDestino = "Quito",
            presupuestoInicial = 50000f, distanciaKM = 380f, dificultad = "Normal",
            descripcion = "Cadena de frío y tráfico montañoso. 90 segundos de trayecto con eventos imprevistos.",
            probabilidadAforoRojo = 0.35f, tasaAduaneraBase = 250f
        });
        // Difícil: 105 segundos reales
        listaEscenarios.Add(new EscenarioMision {
            id = 2, nombreMision = "Corredor Verde Norte", ciudadOrigen = "Esmeraldas", ciudadDestino = "Quito",
            presupuestoInicial = 60000f, distanciaKM = 320f, dificultad = "Difícil",
            descripcion = "Zona de derrumbes y alta tensión aduanera. 105 segundos de recorrido crítico.",
            probabilidadAforoRojo = 0.65f, tasaAduaneraBase = 350f
        });
    }

    #region Catálogos de Eventos

    private void InicializarEventosFacil()
    {
        eventosFacil.Clear();

        // 1. Pinchazo de neumático
        eventosFacil.Add(new EventoRuta {
            titulo = "Pinchazo de Neumático",
            descripcion = "El camión sufre la pérdida de presión en una llanta trasera en la vía Durán-Tambo.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Usar llanta de repuesto propia", costoDinero = 0f, costoTiempoHoras = 0.8f, impactoPuntaje = 0f },
                new OpcionEvento { textoBoton = "Llamar a vulcanizadora local", costoDinero = 60f, costoTiempoHoras = 0.4f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Continuar despacio hasta gasolinera", costoDinero = 180f, costoTiempoHoras = 1.2f, impactoPuntaje = -4f },
                new OpcionEvento { textoBoton = "Comprar neumático nuevo de emergencia", costoDinero = 220f, costoTiempoHoras = 0.3f, impactoPuntaje = 1f }
            }
        });

        // 2. Control de pesaje intermedio
        eventosFacil.Add(new EventoRuta {
            titulo = "Báscula de Pesaje Obligatoria",
            descripcion = "El Ministerio de Transporte solicita control de peso por eje antes de subir a la cordillera.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Hacer fila regular y pesar normalmente", costoDinero = 0f, costoTiempoHoras = 0.5f, impactoPuntaje = 1f },
                new OpcionEvento { textoBoton = "Presentar ticket digital de pre-pesaje", costoDinero = 15f, costoTiempoHoras = 0.1f, impactoPuntaje = 3f },
                new OpcionEvento { textoBoton = "Redistribuir estiba en sitio para equilibrar", costoDinero = 40f, costoTiempoHoras = 0.9f, impactoPuntaje = 0f },
                new OpcionEvento { textoBoton = "Aceptar calibración técnica obligatoria", costoDinero = 75f, costoTiempoHoras = 0.4f, impactoPuntaje = -1f }
            }
        });

        // 3. Peaje con congestionamiento
        eventosFacil.Add(new EventoRuta {
            titulo = "Congestión en Peaje",
            descripcion = "Larga fila de vehículos en la estación de peaje de Yaguachi.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Pagar en ventanilla manual", costoDinero = 5f, costoTiempoHoras = 0.6f, impactoPuntaje = -1f },
                new OpcionEvento { textoBoton = "Usar carril de telepeaje rápido", costoDinero = 10f, costoTiempoHoras = 0.1f, impactoPuntaje = 3f },
                new OpcionEvento { textoBoton = "Esperar apertura de carril auxiliar", costoDinero = 5f, costoTiempoHoras = 0.4f, impactoPuntaje = 0f },
                new OpcionEvento { textoBoton = "Pagar pase exprés preferencial", costoDinero = 25f, costoTiempoHoras = 0.1f, impactoPuntaje = 1f }
            }
        });

        // 4. Llovizna en el piedemonte
        eventosFacil.Add(new EventoRuta {
            titulo = "Llovizna y Pista Resbalosa",
            descripcion = "Comienza a llover en el sector de Bucay, reduciendo la adherencia de los frenos.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Reducir velocidad preventiva a 40 km/h", costoDinero = 0f, costoTiempoHoras = 0.7f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Mantener velocidad con frenado de motor", costoDinero = 20f, costoTiempoHoras = 0.2f, impactoPuntaje = -2f },
                new OpcionEvento { textoBoton = "Parar 15 minutos a esperar que escampe", costoDinero = 0f, costoTiempoHoras = 0.9f, impactoPuntaje = -1f },
                new OpcionEvento { textoBoton = "Seguir a convoy de transporte pesado", costoDinero = 10f, costoTiempoHoras = 0.4f, impactoPuntaje = 1f }
            }
        });

        // 5. Ajuste de amarre de carga
        eventosFacil.Add(new EventoRuta {
            titulo = "Holgura en Cinchas de Carga",
            descripcion = "El sensor de vibración indica que las bandas de sujeción de la carga se aflojaron.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Tensar cinchas manualmente en la berma", costoDinero = 0f, costoTiempoHoras = 0.5f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Colocar bandas de seguridad adicionales", costoDinero = 35f, costoTiempoHoras = 0.3f, impactoPuntaje = 3f },
                new OpcionEvento { textoBoton = "Revisar únicamente en la próxima gasolinera", costoDinero = 0f, costoTiempoHoras = 0.1f, impactoPuntaje = -5f },
                new OpcionEvento { textoBoton = "Contratar estibador de paso para reasegurar", costoDinero = 50f, costoTiempoHoras = 0.4f, impactoPuntaje = 1f }
            }
        });

        // 6. Calibración de temperatura de cabina
        eventosFacil.Add(new EventoRuta {
            titulo = "Recarga de Refrigerante de Motor",
            descripcion = "El marcador de temperatura del motor sube ligeramente al iniciar las primeras cuestas.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Rellenar refrigerante de reserva del camión", costoDinero = 0f, costoTiempoHoras = 0.4f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Comprar galón nuevo de refrigerante premium", costoDinero = 30f, costoTiempoHoras = 0.2f, impactoPuntaje = 3f },
                new OpcionEvento { textoBoton = "Apagar aire acondicionado y forzar subida", costoDinero = 0f, costoTiempoHoras = 0.1f, impactoPuntaje = -3f },
                new OpcionEvento { textoBoton = "Esperar 30 min a enfriamiento pasivo", costoDinero = 0f, costoTiempoHoras = 0.8f, impactoPuntaje = 0f }
            }
        });
    }

    private void InicializarEventosNormal()
    {
        eventosNormal.Clear();

        // 1. Alerta en unidad de refrigeración (Thermo King)
        eventosNormal.Add(new EventoRuta {
            titulo = "Fluctuación Térmica en Contenedor",
            descripcion = "La carga de mariscos subió de -18°C a -14°C por fallo en el compresor secundario.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Encender generador diésel auxiliar", costoDinero = 180f, costoTiempoHoras = 0.3f, impactoPuntaje = 5f },
                new OpcionEvento { textoBoton = "Detenerse en taller de refrigeración en vía", costoDinero = 320f, costoTiempoHoras = 1.4f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Ajustar termostato al máximo y acelerar", costoDinero = 60f, costoTiempoHoras = 0.1f, impactoPuntaje = -6f },
                new OpcionEvento { textoBoton = "Comprar bloques de hielo seco de soporte", costoDinero = 140f, costoTiempoHoras = 0.8f, impactoPuntaje = 1f }
            }
        });

        // 2. Desvío por bacheo en vía Chone
        eventosNormal.Add(new EventoRuta {
            titulo = "Cierre de Carril por Mantenimiento",
            descripcion = "Maquinaria pesada asfaltando tramo; paso alternado cada 20 minutos.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Esperar turno en la fila de contraflujo", costoDinero = 0f, costoTiempoHoras = 1.0f, impactoPuntaje = 1f },
                new OpcionEvento { textoBoton = "Tomar variante rural de segundo orden", costoDinero = 120f, costoTiempoHoras = 0.5f, impactoPuntaje = -3f },
                new OpcionEvento { textoBoton = "Coordinar paso prioritario con banderillero", costoDinero = 30f, costoTiempoHoras = 0.2f, impactoPuntaje = -2f },
                new OpcionEvento { textoBoton = "Detener unidad y adelantar descanso de chofer", costoDinero = 0f, costoTiempoHoras = 1.5f, impactoPuntaje = 3f }
            }
        });

        // 3. Falla de sensor de frenos de aire
        eventosNormal.Add(new EventoRuta {
            titulo = "Fuga Menor en Circuito Neumático",
            descripcion = "Pérdida de presión de aire en el eje del remolque durante descenso pronunciado.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Purgar válvulas y ajustar acoples rápidos", costoDinero = 40f, costoTiempoHoras = 0.8f, impactoPuntaje = 4f },
                new OpcionEvento { textoBoton = "Sustituir manguera con repuesto de a bordo", costoDinero = 70f, costoTiempoHoras = 0.5f, impactoPuntaje = 5f },
                new OpcionEvento { textoBoton = "Bajar con freno de motor sin detenerse", costoDinero = 0f, costoTiempoHoras = 0.1f, impactoPuntaje = -8f },
                new OpcionEvento { textoBoton = "Remolque asistido hasta estación de servicio", costoDinero = 250f, costoTiempoHoras = 1.6f, impactoPuntaje = 2f }
            }
        });

        // 4. Retén de control antinarcóticos
        eventosNormal.Add(new EventoRuta {
            titulo = "Inspección Policial de Carretera",
            descripcion = "Agentes policiales seleccionan el furgón para escaneo canino aleatorio.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Colaborar con apertura y sellado rápido", costoDinero = 0f, costoTiempoHoras = 0.9f, impactoPuntaje = 4f },
                new OpcionEvento { textoBoton = "Presentar precinto de seguridad certificado", costoDinero = 20f, costoTiempoHoras = 0.3f, impactoPuntaje = 6f },
                new OpcionEvento { textoBoton = "Exigir presencia de fiscal para abrir candado", costoDinero = 0f, costoTiempoHoras = 2.2f, impactoPuntaje = -4f },
                new OpcionEvento { textoBoton = "Solicitar inspección externa sin romper sellos", costoDinero = 50f, costoTiempoHoras = 0.6f, impactoPuntaje = 2f }
            }
        });

        // 5. Fatiga de conductor
        eventosNormal.Add(new EventoRuta {
            titulo = "Horas de Conducción Cumplidas",
            descripcion = "La tacografía digital marca 5 horas continuas de manejo sin descanso reglamentario.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Pausa obligatoria de 30 minutos en parador", costoDinero = 25f, costoTiempoHoras = 0.9f, impactoPuntaje = 5f },
                new OpcionEvento { textoBoton = "Relevar con copiloto certificado registrado", costoDinero = 90f, costoTiempoHoras = 0.2f, impactoPuntaje = 6f },
                new OpcionEvento { textoBoton = "Tomar bebida energizante y continuar ruta", costoDinero = 10f, costoTiempoHoras = 0.1f, impactoPuntaje = -7f },
                new OpcionEvento { textoBoton = "Reducir marcha en carril derecho", costoDinero = 0f, costoTiempoHoras = 0.7f, impactoPuntaje = -3f }
            }
        });

        // 6. Derrame de combustible en calzada
        eventosNormal.Add(new EventoRuta {
            titulo = "Calzada Resbaladiza por Diésel",
            descripcion = "Un vehículo anterior regó combustible en curva cerrada antes de Santo Domingo.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Reducir a primera marcha y activar tracción 4x2", costoDinero = 0f, costoTiempoHoras = 0.7f, impactoPuntaje = 4f },
                new OpcionEvento { textoBoton = "Detenerse hasta que bomberos echen aserrín", costoDinero = 0f, costoTiempoHoras = 1.3f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Adelantar por el arcén de tierra", costoDinero = 80f, costoTiempoHoras = 0.3f, impactoPuntaje = -6f },
                new OpcionEvento { textoBoton = "Seguir huella seca de autobús interprovincial", costoDinero = 0f, costoTiempoHoras = 0.4f, impactoPuntaje = 1f }
            }
        });

        // 7. Sobrecosto imprevisto de combustible
        eventosNormal.Add(new EventoRuta {
            titulo = "Desabastecimiento en Estación Barata",
            descripcion = "La estación subsidiada no tiene diésel disponible; la privada vende con recargo comercial.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Comprar diésel premium con recargo", costoDinero = 160f, costoTiempoHoras = 0.2f, impactoPuntaje = 3f },
                new OpcionEvento { textoBoton = "Avanzar 25 km con tanque de reserva al límite", costoDinero = 0f, costoTiempoHoras = 0.6f, impactoPuntaje = -5f },
                new OpcionEvento { textoBoton = "Cargar solo 10 galones para llegar a Quito", costoDinero = 45f, costoTiempoHoras = 0.3f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Esperar llegada del tanquero abastecedor", costoDinero = 0f, costoTiempoHoras = 1.8f, impactoPuntaje = -2f }
            }
        });

        // 8. Falla en transmisión satelital GPS
        eventosNormal.Add(new EventoRuta {
            titulo = "Pérdida de Señal Satelital del Furgón",
            descripcion = "La central de monitoreo pierde contacto con el tacógrafo por sombra celular en cañón.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Reportarse vía llamada satelital manual", costoDinero = 35f, costoTiempoHoras = 0.3f, impactoPuntaje = 5f },
                new OpcionEvento { textoBoton = "Detenerse en zona con antena celular", costoDinero = 0f, costoTiempoHoras = 0.8f, impactoPuntaje = 2f },
                new OpcionEvento { textoBoton = "Continuar sin emitir reporte hasta Aduana", costoDinero = 0f, costoTiempoHoras = 0.1f, impactoPuntaje = -7f },
                new OpcionEvento { textoBoton = "Reiniciar módem y antena del vehículo", costoDinero = 15f, costoTiempoHoras = 0.4f, impactoPuntaje = 3f }
            }
        });
    }

    private void InicializarEventosDificil()
    {
        eventosDificil.Clear();

        // 1. Derrumbe en cordillera occidental (2 opciones)
        eventosDificil.Add(new EventoRuta {
            titulo = "Derrumbe Masivo de Rocas",
            descripcion = "Deslave bloquea ambos sentidos de la vía Alóag-Santo Domingo.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Desviar por vía Calacalí-La Independencia (+110 km)", costoDinero = 480f, costoTiempoHoras = 3.5f, impactoPuntaje = -8f },
                new OpcionEvento { textoBoton = "Contratar retroexcavadora comunitaria privada", costoDinero = 750f, costoTiempoHoras = 1.8f, impactoPuntaje = -12f }
            }
        });

        // 2. Rotura de precinto aduanero en bache
        eventosDificil.Add(new EventoRuta {
            titulo = "Rotura de Sello Aduanero",
            descripcion = "Impacto severo con socavón parte el candado satelital de la aduana.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Notificar de inmediato y esperar perito de fiscalía", costoDinero = 300f, costoTiempoHoras = 4.0f, impactoPuntaje = -10f },
                new OpcionEvento { textoBoton = "Colocar cincha genérica y declarar incidente en destino", costoDinero = 80f, costoTiempoHoras = 0.3f, impactoPuntaje = -25f }
            }
        });

        // 3. Falla de frenos en bajada pronunciada
        eventosDificil.Add(new EventoRuta {
            titulo = "Cristalización de Frenos",
            descripcion = "Los tambores superan los 400°C; pérdida del 70% de frenado hidráulico.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Entrar forzado a rampa de frenado de grava", costoDinero = 950f, costoTiempoHoras = 3.0f, impactoPuntaje = -14f },
                new OpcionEvento { textoBoton = "Detener con caja de cambios y rozar muro lateral", costoDinero = 1400f, costoTiempoHoras = 1.5f, impactoPuntaje = -20f }
            }
        });

        // 4. Bloqueo de vía por paro social
        eventosDificil.Add(new EventoRuta {
            titulo = "Bloqueo Comunitario Indígena",
            descripcion = "Comuneros cierran paso exigiendo compensación vial.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Negociar salvoconducto humanitario de carga perecible", costoDinero = 350f, costoTiempoHoras = 2.0f, impactoPuntaje = -10f },
                new OpcionEvento { textoBoton = "Esperar despeje de fuerza pública", costoDinero = 0f, costoTiempoHoras = 5.5f, impactoPuntaje = -18f }
            }
        });

        // 5. Fisura en cárter de motor
        eventosDificil.Add(new EventoRuta {
            titulo = "Fuga Catastrófica de Aceite",
            descripcion = "Piedra afilada perfora el cárter; presión de aceite en cero.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Grúa pesada urgente hasta taller especializado", costoDinero = 1200f, costoTiempoHoras = 3.5f, impactoPuntaje = -15f },
                new OpcionEvento { textoBoton = "Soldadura química en frío en la carretera", costoDinero = 250f, costoTiempoHoras = 1.8f, impactoPuntaje = -22f }
            }
        });

        // 6. Intento de asalto en curva ciega
        eventosDificil.Add(new EventoRuta {
            titulo = "Piratas del Asfalto",
            descripcion = "Vehículos sin placas intentan cerrarle el paso al camión.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Activar botón de pánico satelital y acelerar a UPC", costoDinero = 120f, costoTiempoHoras = 0.8f, impactoPuntaje = -8f },
                new OpcionEvento { textoBoton = "Frenar en gasolinera iluminada con custodia privada", costoDinero = 400f, costoTiempoHoras = 2.0f, impactoPuntaje = -11f }
            }
        });

        // 7. Derrumbe de puente secundario
        eventosDificil.Add(new EventoRuta {
            titulo = "Socavamiento de Puente Bailey",
            descripcion = "Crecida de río socava bases de puente provisional.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Retorno completo y cruce por gabarra fluvial", costoDinero = 600f, costoTiempoHoras = 4.5f, impactoPuntaje = -16f },
                new OpcionEvento { textoBoton = "Pagar peaje a hacendado para pasar por trocha privada", costoDinero = 500f, costoTiempoHoras = 2.2f, impactoPuntaje = -13f }
            }
        });

        // 8. Rotura de eje motriz posterior
        eventosDificil.Add(new EventoRuta {
            titulo = "Corte de Semieje de Tracción",
            descripcion = "Esfuerzo de torsión en cuesta parte el semieje derecho.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Trasbordar carga a unidad nodriza de apoyo", costoDinero = 1600f, costoTiempoHoras = 4.0f, impactoPuntaje = -19f },
                new OpcionEvento { textoBoton = "Instalar semieje de repuesto con mecánico móvil", costoDinero = 850f, costoTiempoHoras = 3.0f, impactoPuntaje = -14f }
            }
        });

        // 9. Niebla cerrada con visibilidad nula
        eventosDificil.Add(new EventoRuta {
            titulo = "Cero Visibilidad en La Virgen",
            descripcion = "Densidad extrema de neblina en páramo andino (menos de 2 metros).",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Detención total en paradero seguro hasta amanecer", costoDinero = 150f, costoTiempoHoras = 4.0f, impactoPuntaje = -12f },
                new OpcionEvento { textoBoton = "Contratar camioneta guía local con luces exploradoras", costoDinero = 280f, costoTiempoHoras = 1.8f, impactoPuntaje = -9f }
            }
        });

        // 10. Desprendimiento de manguera de turbo
        eventosDificil.Add(new EventoRuta {
            titulo = "Descompresión Súbita de Admisión",
            descripcion = "Pérdida del 80% de potencia del motor en plena subida.",
            opciones = new List<OpcionEvento> {
                new OpcionEvento { textoBoton = "Sustituir abrazadera reforzada de titanio", costoDinero = 180f, costoTiempoHoras = 1.0f, impactoPuntaje = -7f },
                new OpcionEvento { textoBoton = "Remolque asistido hasta la cumbre", costoDinero = 700f, costoTiempoHoras = 2.5f, impactoPuntaje = -15f }
            }
        });
    }

    #endregion

    public void SeleccionarMision(int index, Transform nodoTransform)
    {
        if (index >= listaEscenarios.Count) return;
        escenarioSeleccionado = listaEscenarios[index];
        posicionOrigenMision = nodoTransform.localPosition;

        presupuestoActual = escenarioSeleccionado.presupuestoInicial;
        puntajeAcumulado = 100f;
        tiempoTranscurridoHoras = 0f;

        if (txtPresupuesto != null) txtPresupuesto.text = $"Presupuesto: ${presupuestoActual:N0}";
        if (txtRutaActiva != null) txtRutaActiva.text = $"Misión: {escenarioSeleccionado.ciudadOrigen} ➔ {escenarioSeleccionado.ciudadDestino}";

        if (txtTituloModal != null) txtTituloModal.text = $"{escenarioSeleccionado.nombreMision.ToUpper()} ({escenarioSeleccionado.dificultad})";
        if (txtDescripcionModal != null) txtDescripcionModal.text = escenarioSeleccionado.descripcion;

        if (modalDespacho != null) modalDespacho.SetActive(true);
    }

    public void ActualizarTextoCapacidad(float valor)
    {
        if (txtCapacidadValor != null)
            txtCapacidadValor.text = $"Capacidad asignada: {valor:0}% ({valor * 0.2f:0.0} / 20 Ton)";
    }

    public void ConfirmarSalidaCamion()
    {
        if (escenarioSeleccionado == null) return;
        if (modalDespacho != null) modalDespacho.SetActive(false);

        if (marcadorCamion != null)
        {
            marcadorCamion.transform.localPosition = posicionOrigenMision;
            marcadorCamion.SetActive(true);
        }

        if (txtEstadoMision != null)
            txtEstadoMision.text = $"<color=#F39C12>Misión activa: {escenarioSeleccionado.ciudadOrigen} ➔ Aduana SENAE...</color>";

        StartCoroutine(ViajeConEventosDinamicos());
    }

    private IEnumerator ViajeConEventosDinamicos()
    {
        // Calibración de tiempo real según dificultad
        float duracionViajeSegundos = 75f; // Fácil por defecto
        List<EventoRuta> bancoEventos = eventosFacil;
        int opcionesPorEvento = 4;
        int eventosMinimos = 2;
        int eventosMaximos = 4;

        if (escenarioSeleccionado.dificultad == "Normal")
        {
            duracionViajeSegundos = 90f; // 1:30 min
            bancoEventos = eventosNormal;
            opcionesPorEvento = 4;
            eventosMinimos = 3;
            eventosMaximos = 5;
        }
        else if (escenarioSeleccionado.dificultad == "Difícil")
        {
            duracionViajeSegundos = 105f; // 1:45 min
            bancoEventos = eventosDificil;
            opcionesPorEvento = 2; // Solo 2 opciones en difícil
            eventosMinimos = 5;
            eventosMaximos = 8;
        }

        // Programar marcas de tiempo aleatorias para disparar eventos
        int totalEventosADisparar = Random.Range(eventosMinimos, eventosMaximos + 1);
        List<float> tiemposDisparo = new List<float>();
        for (int i = 0; i < totalEventosADisparar; i++)
        {
            tiemposDisparo.Add(Random.Range(duracionViajeSegundos * 0.15f, duracionViajeSegundos * 0.88f));
        }
        tiemposDisparo.Sort();

        float tiempoTranscurridoViaje = 0f;
        int indiceEventoActual = 0;
        Vector3 inicio = posicionOrigenMision;
        Vector3 destino = nodoSENAE.localPosition;

        while (tiempoTranscurridoViaje < duracionViajeSegundos)
        {
            if (!esperandoDecisionEvento)
            {
                tiempoTranscurridoViaje += Time.deltaTime;
                tiempoTranscurridoHoras += Time.deltaTime * 0.12f;

                float t = Mathf.Clamp01(tiempoTranscurridoViaje / duracionViajeSegundos);
                if (marcadorCamion != null)
                    marcadorCamion.transform.localPosition = Vector3.Lerp(inicio, destino, t);

                // Verificar si corresponde disparar un evento
                if (indiceEventoActual < tiemposDisparo.Count && tiempoTranscurridoViaje >= tiemposDisparo[indiceEventoActual])
                {
                    indiceEventoActual++;
                    EventoRuta ev = bancoEventos[Random.Range(0, bancoEventos.Count)];
                    DispararEventoUI(ev, opcionesPorEvento);
                }
            }

            yield return null;
        }

        // Llegada a Aduana
        DetenerEnAduana();
    }

    private void DispararEventoUI(EventoRuta evento, int cantidadOpciones)
    {
        esperandoDecisionEvento = true;

        if (txtTituloEvento != null) txtTituloEvento.text = evento.titulo.ToUpper();
        if (txtDescripcionEvento != null) txtDescripcionEvento.text = evento.descripcion;

        for (int i = 0; i < botonesOpciones.Length; i++)
        {
            if (i < cantidadOpciones && i < evento.opciones.Count)
            {
                botonesOpciones[i].gameObject.SetActive(true);
                OpcionEvento opc = evento.opciones[i];

                TMP_Text txtBtn = botonesOpciones[i].GetComponentInChildren<TMP_Text>();
                if (txtBtn != null)
                    txtBtn.text = $"{opc.textoBoton}\n<size=12><color=#F1C40F>Cost: -${opc.costoDinero:0}</color> | <color=#3498DB>+{opc.costoTiempoHoras:0.0}h</color></size>";

                botonesOpciones[i].onClick.RemoveAllListeners();
                botonesOpciones[i].onClick.AddListener(() => ResolverOpcion(opc));
            }
            else
            {
                botonesOpciones[i].gameObject.SetActive(false);
            }
        }

        if (modalEventoRuta != null) modalEventoRuta.SetActive(true);
    }

    private void ResolverOpcion(OpcionEvento opcion)
    {
        presupuestoActual -= opcion.costoDinero;
        tiempoTranscurridoHoras += opcion.costoTiempoHoras;
        puntajeAcumulado += opcion.impactoPuntaje;

        if (txtPresupuesto != null) txtPresupuesto.text = $"Presupuesto: ${presupuestoActual:N0}";

        if (modalEventoRuta != null) modalEventoRuta.SetActive(false);
        esperandoDecisionEvento = false;
    }

    private void DetenerEnAduana()
    {
        if (txtEstadoMision != null)
            txtEstadoMision.text = "<color=#E67E22>Retenido: Inspección en Aduana SENAE</color>";

        float chance = Random.value;
        string canal = "Verde";
        float costoAduana = escenarioSeleccionado.tasaAduaneraBase;
        float retrasoHoras = 1f;

        if (chance < escenarioSeleccionado.probabilidadAforoRojo)
        {
            canal = "Rojo (Inspección Física)";
            costoAduana += 1200f;
            retrasoHoras = 6f;
            puntajeAcumulado -= 10f;
        }
        else if (chance < escenarioSeleccionado.probabilidadAforoRojo + 0.25f)
        {
            canal = "Naranja (Documental)";
            costoAduana += 400f;
            retrasoHoras = 2.5f;
            puntajeAcumulado -= 4f;
        }

        presupuestoActual -= costoAduana;
        tiempoTranscurridoHoras += retrasoHoras;

        if (txtPresupuesto != null) txtPresupuesto.text = $"Presupuesto: ${presupuestoActual:N0}";
        if (txtAduanaResultado != null) txtAduanaResultado.text = $"RESULTADO: CANAL {canal.ToUpper()}";
        if (txtAduanaCostoTiempo != null)
            txtAduanaCostoTiempo.text = $"Tasa/Sanción: -${costoAduana:N0}\nRetraso: +{retrasoHoras} Horas";

        if (modalAduana != null) modalAduana.SetActive(true);
    }

    public void ContinuarDesdeAduana()
    {
        if (modalAduana != null) modalAduana.SetActive(false);

        if (txtEstadoMision != null)
            txtEstadoMision.text = $"<color=#3498DB>Liberado de Aduana. En tramo final hacia {escenarioSeleccionado.ciudadDestino}...</color>";

        StartCoroutine(SegundaFaseMapeo());
    }

    private IEnumerator SegundaFaseMapeo()
    {
        float duracionTramoFinal = 20f; // Tramo final de subida a Quito
        float t = 0f;
        Vector3 inicio = nodoSENAE.localPosition;
        Vector3 destino = nodoDestinoUI.localPosition;

        while (t < duracionTramoFinal)
        {
            t += Time.deltaTime;
            tiempoTranscurridoHoras += Time.deltaTime * 0.15f;
            if (marcadorCamion != null)
                marcadorCamion.transform.localPosition = Vector3.Lerp(inicio, destino, t / duracionTramoFinal);

            yield return null;
        }

        CompletarMision();
    }

    private void CompletarMision()
    {
        if (txtEstadoMision != null)
            txtEstadoMision.text = "<color=#2ECC71>Misión Finalizada con Éxito</color>";

        float kpiFinal = Mathf.Clamp(puntajeAcumulado, 5f, 100f);

        if (txtKPI_Resultado != null) txtKPI_Resultado.text = $"CALIFICACIÓN: {kpiFinal:0}%";
        if (txtKPI_Tiempo != null) txtKPI_Tiempo.text = $"Tiempo Total de Tránsito: {tiempoTranscurridoHoras:0.0} Horas";
        if (txtKPI_Finanzas != null) txtKPI_Finanzas.text = $"Presupuesto Restante: ${presupuestoActual:N0} (De ${escenarioSeleccionado.presupuestoInicial:N0})";

        if (panelResultados != null) panelResultados.SetActive(true);
    }

    public void ReintentarMision()
    {
        if (panelResultados != null) panelResultados.SetActive(false);
        StopAllCoroutines();

        if (marcadorCamion != null) marcadorCamion.SetActive(false);

        esperandoDecisionEvento = false;
        tiempoTranscurridoHoras = 0f;
        puntajeAcumulado = 100f;

        if (escenarioSeleccionado != null)
        {
            presupuestoActual = escenarioSeleccionado.presupuestoInicial;
            if (txtPresupuesto != null) txtPresupuesto.text = $"Presupuesto: ${presupuestoActual:N0}";
            if (txtEstadoMision != null) txtEstadoMision.text = "Reintentando: Configure la carga y despache nuevamente";
            if (modalDespacho != null) modalDespacho.SetActive(true);
        }
    }

    public void VolverAlMenuPrincipal()
    {
        StopAllCoroutines();
        if (panelSimulacion != null) panelSimulacion.SetActive(false);
        if (panelMainMenu != null) panelMainMenu.SetActive(true);
    }
}