using System;
using System.Collections;
using System.Globalization;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mision1DashboardController : MonoBehaviour
{
    [Header("1. Controladores y Navegación")]
    public Mision1FlujoController flujoController;
    public GameObject panelDashboard;                  // Arrastra: 6_Panel_Dashboard
    public Button btnFinalizarJuego;                   // Arrastra: Btn_Finalizar_Juego

    [Header("2. Puntaje Principal (Banner_Resultado)")]
    public TMP_Text txtPorcentajeResultado;            // Arrastra: Txt_Porcentaje_Resultado

    [Header("3. Fila 1: Entrega a Tiempo (BannerTiempo)")]
    public TMP_Text txtPuntajeTiempo;                  // Arrastra: Txt_Puntaje_Tiempo
    public Image imgEstrellasTiempo;                   // Arrastra: Estrellas_Relleno (de BannerTiempo)

    [Header("4. Fila 2: Utilización de Recursos (Banner_Recursos)")]
    public TMP_Text txtPuntajeRecursos;                // Arrastra: Txt_Puntaje_Recursos
    public Image imgEstrellasRecursos;                 // Arrastra: Estrellas_Relleno (de Banner_Recursos)

    [Header("5. Fila 3: Eficiencia de Rutas (Banner_Rutas)")]
    public TMP_Text txtPuntajeRutas;                   // Arrastra: Txt_Puntaje_Eficiencia_Rutas
    public Image imgEstrellasRutas;                    // Arrastra: Estrellas_Relleno (de Banner_Rutas)

    [Header("6. Fila 4: Costo Total (Banner_Costo_Total)")]
    public TMP_Text txtTotalGastado;                   // Arrastra: Text (TMP) dentro de Total_Gastado

    [Header("7. Configuración de Animación")]
    public float duracionAnimacion = 1.2f;

    private readonly CultureInfo culturaDecimal = CultureInfo.InvariantCulture;
    private Coroutine rutinaAnimacion;

    private void Awake()
    {
        if (flujoController == null)
            flujoController = FindObjectOfType<Mision1FlujoController>(true);

        if (btnFinalizarJuego != null)
        {
            btnFinalizarJuego.onClick.RemoveAllListeners();
            btnFinalizarJuego.onClick.AddListener(FinalizarYVolverAMisiones);
        }

        ConfigurarImagenEstrellas(imgEstrellasTiempo);
        ConfigurarImagenEstrellas(imgEstrellasRecursos);
        ConfigurarImagenEstrellas(imgEstrellasRutas);
    }

    private void OnEnable()
    {
        CalcularYMostrarResultados();
    }

    private void ConfigurarImagenEstrellas(Image img)
    {
        if (img == null) return;
        img.type = Image.Type.Filled;
        img.fillMethod = Image.FillMethod.Horizontal;
        img.fillOrigin = (int)Image.OriginHorizontal.Left;
    }

    public void CalcularYMostrarResultados()
    {
        if (flujoController == null)
            flujoController = FindObjectOfType<Mision1FlujoController>(true);

        TipoProducto producto = Mision1IncotermManager.ProductoActivo;
        TipoIncoterm incoterm = Mision1IncotermManager.IncotermActivo;

        float presupuestoInicial = flujoController != null ? flujoController.presupuestoInicial : 45000f;
        float gastoRegistrado = 0f;

        if (flujoController != null && flujoController.TotalGastado > 0f)
            gastoRegistrado = flujoController.TotalGastado;
        else if (Mision1FlujoController.UltimoGastoRegistrado > 0f)
            gastoRegistrado = Mision1FlujoController.UltimoGastoRegistrado;

        float costoTotal;
        if (gastoRegistrado >= 20000f)
        {
            costoTotal = gastoRegistrado;
        }
        else
        {
            float costoMercanciaIncoterm = ObtenerCostoMercanciaEIncoterm(producto, incoterm);
            float gastosOperativos = gastoRegistrado > 0f ? gastoRegistrado : 1550f;
            costoTotal = costoMercanciaIncoterm + gastosOperativos;
        }

        float presupuestoRestante = Mathf.Max(0f, presupuestoInicial - costoTotal);

        Debug.Log($"[Dashboard] Producto: {producto} | Incoterm: {incoterm} | Costo Total Final: ${costoTotal:N0}");

        // Métrica 1: Entrega a tiempo (%)
        float entregasATiempo = 90f;
        if (incoterm == TipoIncoterm.CIF) entregasATiempo += 5f;
        else if (incoterm == TipoIncoterm.FOB) entregasATiempo += 2f;
        else if (incoterm == TipoIncoterm.EXW) entregasATiempo -= 12f;

        float duracionSegundos = flujoController != null ? (Time.time - flujoController.TiempoInicioMision) : 180f;
        if (duracionSegundos > 0f && duracionSegundos <= 240f)
            entregasATiempo += 3f;

        entregasATiempo = Mathf.Clamp(Mathf.Round(entregasATiempo), 20f, 100f);

        // Métrica 2: Utilización de recursos (%)
        float costoOptimoProducto = ObtenerCostoMercanciaEIncoterm(producto, TipoIncoterm.FOB) + 1550f;
        float sobrecosto = Mathf.Max(0f, costoTotal - costoOptimoProducto);
        float utilizacionRecursos = 95f - ((sobrecosto / 1000f) * 4f);

        if (presupuestoRestante < 1000f)
            utilizacionRecursos -= 8f;

        utilizacionRecursos = Mathf.Clamp(Mathf.Round(utilizacionRecursos), 20f, 100f);

        // Métrica 3: Eficiencia de rutas (%)
        float eficienciaRutas = 88f;
        if (incoterm == TipoIncoterm.FOB) eficienciaRutas = 94f;
        else if (incoterm == TipoIncoterm.CIF) eficienciaRutas = 85f;
        else if (incoterm == TipoIncoterm.EXW) eficienciaRutas = 72f;

        eficienciaRutas = Mathf.Clamp(Mathf.Round(eficienciaRutas), 20f, 100f);

        // Puntaje Global Ponderado (0.0 a 100.0)
        float puntajeGlobal = (entregasATiempo * 0.35f) + (utilizacionRecursos * 0.35f) + (eficienciaRutas * 0.30f);
        puntajeGlobal = Mathf.Clamp(puntajeGlobal, 0f, 100f);

        // Respaldo en PlayerPrefs
        PlayerPrefs.SetFloat("Mision1_Puntaje", puntajeGlobal);
        PlayerPrefs.SetFloat("Mision1_Entregas", entregasATiempo);
        PlayerPrefs.SetFloat("Mision1_Recursos", utilizacionRecursos);
        PlayerPrefs.SetFloat("Mision1_Rutas", eficienciaRutas);
        PlayerPrefs.SetFloat("Mision1_CostoTotal", costoTotal);
        PlayerPrefs.Save();

        string estudianteActual = (PortalBridgeManager.Instance != null && PortalBridgeManager.Instance.usuarioActual != null && !string.IsNullOrEmpty(PortalBridgeManager.Instance.usuarioActual.codigoInstitucional))
            ? PortalBridgeManager.Instance.usuarioActual.codigoInstitucional
            : "UDLA-EST-0001";

        string nombreEscenarioActual = (PortalBridgeManager.Instance != null && PortalBridgeManager.Instance.escenarioActivo != null && !string.IsNullOrEmpty(PortalBridgeManager.Instance.escenarioActivo.nombreRuta))
            ? PortalBridgeManager.Instance.escenarioActivo.nombreRuta
            : "Importación Marítima - Asia a GYE";

        // Enviar a la Base de Datos Local / API
        DatosPartida resultadoMision = new DatosPartida
        {
            idEstudiante = estudianteActual,
            escenario = nombreEscenarioActual,
            nivel = "Nivel 1",
            fecha = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss"),
            puntajeGlobal = puntajeGlobal,
            entregasATiempo = entregasATiempo,
            utilizacionRecursos = utilizacionRecursos,
            eficienciaRutas = eficienciaRutas,
            costoTotal = costoTotal,
            sincronizadoConPortal = false
        };

        if (DatabaseManager.Instancia != null)
        {
            DatabaseManager.Instancia.GuardarResultadoLocal(resultadoMision);
        }
        else if (PortalBridgeManager.Instance != null)
        {
            var resultadoDto = new ResultadoPartidaDto
            {
                nombreEstudiante = PortalBridgeManager.Instance.usuarioActual?.nombre ?? "Estudiante",
                ruta = resultadoMision.escenario,
                dificultad = "facil",
                costoTotal = resultadoMision.costoTotal,
                entregasATiempoPct = resultadoMision.entregasATiempo,
                utilizacionRecursosPct = resultadoMision.utilizacionRecursos,
                eficienciaRutasPct = resultadoMision.eficienciaRutas,
                eventoAduanaSuperado = true,
                puntajeFinal = Mathf.RoundToInt(resultadoMision.puntajeGlobal),
                fecha = resultadoMision.fecha
            };
            PortalBridgeManager.Instance.GuardarResultadoPartida(resultadoDto);
        }
        else
        {
            Debug.LogWarning("[Dashboard] No se encontró gestor de base de datos para guardar la partida.");
        }

        if (rutinaAnimacion != null)
            StopCoroutine(rutinaAnimacion);

        rutinaAnimacion = StartCoroutine(AnimarResultados(puntajeGlobal, entregasATiempo, utilizacionRecursos, eficienciaRutas, costoTotal));
    }

    private float ObtenerCostoMercanciaEIncoterm(TipoProducto producto, TipoIncoterm incoterm)
    {
        if (producto == TipoProducto.Tecnologia)
        {
            if (incoterm == TipoIncoterm.FOB) return 38000f;
            if (incoterm == TipoIncoterm.CIF) return 40800f;
            if (incoterm == TipoIncoterm.EXW) return 36400f;
        }
        else if (producto == TipoProducto.Repuestos)
        {
            if (incoterm == TipoIncoterm.FOB) return 32000f;
            if (incoterm == TipoIncoterm.CIF) return 34800f;
            if (incoterm == TipoIncoterm.EXW) return 30400f;
        }
        else if (producto == TipoProducto.Textil)
        {
            if (incoterm == TipoIncoterm.FOB) return 27500f;
            if (incoterm == TipoIncoterm.CIF) return 30300f;
            if (incoterm == TipoIncoterm.EXW) return 25900f;
        }

        return 38000f;
    }

    private IEnumerator AnimarResultados(float puntajeFinal, float tiempoFinal, float recursosFinal, float rutasFinal, float costoFinal)
    {
        float transcurrido = 0f;

        while (transcurrido < duracionAnimacion)
        {
            transcurrido += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(transcurrido / duracionAnimacion));

            AplicarValoresUI(
                Mathf.Lerp(0f, puntajeFinal, t),
                Mathf.Lerp(0f, tiempoFinal, t),
                Mathf.Lerp(0f, recursosFinal, t),
                Mathf.Lerp(0f, rutasFinal, t),
                Mathf.Lerp(0f, costoFinal, t)
            );

            yield return null;
        }

        AplicarValoresUI(puntajeFinal, tiempoFinal, recursosFinal, rutasFinal, costoFinal);
    }

    private void AplicarValoresUI(float puntaje, float tiempo, float recursos, float rutas, float costo)
    {
        if (txtPorcentajeResultado != null)
            txtPorcentajeResultado.text = puntaje.ToString("0.0", culturaDecimal);

        if (txtPuntajeTiempo != null)
            txtPuntajeTiempo.text = $"{Mathf.RoundToInt(tiempo)}%";

        if (imgEstrellasTiempo != null)
            imgEstrellasTiempo.fillAmount = tiempo / 100f;

        if (txtPuntajeRecursos != null)
            txtPuntajeRecursos.text = $"{Mathf.RoundToInt(recursos)}%";

        if (imgEstrellasRecursos != null)
            imgEstrellasRecursos.fillAmount = recursos / 100f;

        if (txtPuntajeRutas != null)
            txtPuntajeRutas.text = $"{Mathf.RoundToInt(rutas)}%";

        if (imgEstrellasRutas != null)
            imgEstrellasRutas.fillAmount = rutas / 100f;

        if (txtTotalGastado != null)
            txtTotalGastado.text = $"${costo:N0}";
    }

    public void FinalizarYVolverAMisiones()
    {
        if (panelDashboard != null)
            panelDashboard.SetActive(false);

        if (flujoController != null)
            flujoController.RegresarAlMenuMisiones();
    }
}