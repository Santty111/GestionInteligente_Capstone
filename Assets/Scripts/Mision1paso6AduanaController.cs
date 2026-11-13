using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Serialization;
using TMPro;

public class Mision1Paso6AduanaController : MonoBehaviour
{
    [Header("0. Controladores Globales")]
    public Mision1FlujoController flujoController;

    [Header("1. Vista Presencial Oficina / Laptop")]
    public GameObject fondoOficinaAduana;              // Arrastra: Oficina_Aduan
    public Button btnLaptopAduana;                     // Arrastra: Laptop_Aduana

    [Header("2. Sub-panel 6.1: Declaración DAI y Tributos")]
    public GameObject subPanel6_1_DAI;                 // Arrastra: 6_1_Modal_DAI_Tributos
    public TMP_Text txtExpedienteDocumental;
    public TMP_Text txtDesgloseTributos;
    public Button btnPagarTributosSENAE;

    [Header("3. Sub-panel 6.2: Aforo y Salida Autorizada")]
    public GameObject subPanel6_2_Aforo;               // Arrastra: 6_2_Modal_Aforo_Salida
    public GameObject luzNaranjaActiva;
    public GameObject selloSalidaAutorizada;
    public TMP_Text txtDictamenAforo;
    public Button btnIrABodegaje;

    [Header("4. Sub-panel 6.3: Facturación de Bodegaje")]
    public GameObject subPanel6_3_Bodegaje;            // Arrastra: 6_3_Modal_BodegajePortuario
    public TMP_Text txtDetalleTerminal;
    public TMP_Text txtTotalBodega;
    public Button btnPagarBodegaje;

    [Header("5. Sub-panel 6.4: Cruce CAS y Turno de Retiro")]
    public GameObject subPanel6_4_TurnoCAS;            // Arrastra: 6_4_Modal_TurnoRetiro_CAS
    public GameObject badgeCASValidada;
    public TMP_Text txtDatosTransportista;
    public TMP_Text txtTurnoHorario;
    public Button btnFinalizarPaso6;

    [Header("6. Transición al Paso 7 (Pantalla Final)")]
    public GameObject panelPaso6Aduana;                // Arrastra: Panel_Paso6_Aduana
    [FormerlySerializedAs("panelPaso7Transporte")]
    public GameObject panelPaso7Final;                 // Arrastra: Panel_Paso7_Final

    private readonly float costoTributosSENAE = 1200f;
    private readonly float costoBodegajeTerminal = 350f;

    private void Awake()
    {
        if (btnLaptopAduana != null)
        {
            btnLaptopAduana.onClick.RemoveAllListeners();
            btnLaptopAduana.onClick.AddListener(AbrirSistemaECUAPASS);
        }

        if (btnPagarTributosSENAE != null)
        {
            btnPagarTributosSENAE.onClick.RemoveAllListeners();
            btnPagarTributosSENAE.onClick.AddListener(EjecutarPagoTributosSENAE);
        }

        if (btnIrABodegaje != null)
        {
            btnIrABodegaje.onClick.RemoveAllListeners();
            btnIrABodegaje.onClick.AddListener(AbrirModuloBodegaje);
        }

        if (btnPagarBodegaje != null)
        {
            btnPagarBodegaje.onClick.RemoveAllListeners();
            btnPagarBodegaje.onClick.AddListener(EjecutarPagoBodegaje);
        }

        if (btnFinalizarPaso6 != null)
        {
            btnFinalizarPaso6.onClick.RemoveAllListeners();
            btnFinalizarPaso6.onClick.AddListener(TransicionarHaciaPaso7);
        }
    }

    private void OnEnable()
    {
        ReiniciarVistaOficina();
    }

    public void ReiniciarVistaOficina()
    {
        if (fondoOficinaAduana != null)
            fondoOficinaAduana.SetActive(true);

        if (btnLaptopAduana != null)
        {
            btnLaptopAduana.gameObject.SetActive(true);
            btnLaptopAduana.interactable = true;
        }

        DesactivarTodosLosModales();
    }

    public void AbrirSistemaECUAPASS()
    {
        if (btnLaptopAduana != null)
            btnLaptopAduana.interactable = false;

        ConfigurarDatosIniciales();
        ActivarSubFase(1);
    }

    private void ConfigurarDatosIniciales()
    {
        string producto = "Laptops & Monitores 2K (480 unidades)";
        if (Mision1IncotermManager.ProductoActivo == TipoProducto.Textil)
            producto = "Ropa Deportiva & Calzado (620 bultos)";
        else if (Mision1IncotermManager.ProductoActivo == TipoProducto.Repuestos)
            producto = "Repuestos & Motores Livianos (310 cajones)";

        if (txtExpedienteDocumental != null)
        {
            txtExpedienteDocumental.text =
                $"• <b>Mercancía Declarada:</b> <color=#38BDF8>{producto}</color>\n" +
                $"• <b>Bill of Lading:</b> #MSKU-74910283 (<color=#2ECC71>V°B° Naviero Acreditado</color>)\n" +
                $"• <b>Contenedor:</b> 1x40' High Cube Sellado | Recinto: Contecon GYE\n" +
                $"• <b>Régimen Aduanero:</b> Importación a Consumo (Régimen 10)";
        }

        if (txtDesgloseTributos != null)
        {
            txtDesgloseTributos.text =
                $"<b>LIQUIDACIÓN ARANCELARIA SENAE:</b>\n" +
                $"• Arancel Ad-Valorem: $640.00 USD\n" +
                $"• Tasa FODINFA (0.5%): $40.00 USD\n" +
                $"• IVA Aduanero (15%): $520.00 USD\n\n" +
                $"TOTAL TRIBUTOS A PAGAR: <color=#F1C40F><b>${costoTributosSENAE:N0} USD</b></color>";
        }
    }

    public void EjecutarPagoTributosSENAE()
    {
        if (flujoController != null)
            flujoController.ModificarPresupuesto(-costoTributosSENAE);

        Debug.Log($"[SENAE] Tributos cancelados: -${costoTributosSENAE:N0}");

        ActivarSubFase(2);
        StartCoroutine(SecuenciaResolucionAforo());
    }

    private IEnumerator SecuenciaResolucionAforo()
    {
        if (btnIrABodegaje != null)
            btnIrABodegaje.interactable = false;

        if (selloSalidaAutorizada != null)
            selloSalidaAutorizada.SetActive(false);

        if (txtDictamenAforo != null)
            txtDictamenAforo.text = "Analizando matriz de riesgo en ECUAPASS...";

        yield return new WaitForSeconds(1.0f);

        if (luzNaranjaActiva != null)
            luzNaranjaActiva.SetActive(true);

        if (txtDictamenAforo != null)
        {
            txtDictamenAforo.text =
                "<b>CANAL ASIGNADO: AFORO DOCUMENTAL</b>\n" +
                "El especialista aduanero revisó facturas, pólizas y Visto Bueno sin hallar discrepancias. Liquidación aprobada.";
        }

        yield return new WaitForSeconds(1.2f);

        if (selloSalidaAutorizada != null)
            selloSalidaAutorizada.SetActive(true);

        if (btnIrABodegaje != null)
            btnIrABodegaje.interactable = true;
    }

    public void AbrirModuloBodegaje()
    {
        ActivarSubFase(3);

        if (txtDetalleTerminal != null)
        {
            txtDetalleTerminal.text =
                "• <b>Terminal Portuaria:</b> Contecon Guayaquil S.A. (Muelle Fiscal)\n" +
                "• <b>Plazo Naviero:</b> 4 Días Libres concedidos | Días ocupados: 2 días\n" +
                "• <b>Cargos por Sobrestadía:</b> $0.00 (Dentro de la franquicia pactada)\n" +
                "• <b>Servicios Portuarios:</b> Pesaje de balanza fiscal + Manipulación de patio";
        }

        if (txtTotalBodega != null)
            txtTotalBodega.text = $"TOTAL BODEGAJE: <color=#F1C40F><b>${costoBodegajeTerminal:N0} USD</b></color>";
    }

    public void EjecutarPagoBodegaje()
    {
        if (flujoController != null)
            flujoController.ModificarPresupuesto(-costoBodegajeTerminal);

        Debug.Log($"[Terminal] Factura de bodegaje cancelada: -${costoBodegajeTerminal:N0}");

        ActivarSubFase(4);
        ConfigurarTurnoYCAS();
    }

    private void ConfigurarTurnoYCAS()
    {
        if (badgeCASValidada != null)
            badgeCASValidada.SetActive(true);

        if (txtDatosTransportista != null)
        {
            txtDatosTransportista.text =
                "• <b>Empresa Autorizada:</b> TransLogística Interandina S.A.\n" +
                "• <b>Unidad de Carga:</b> Cabezal Kenworth T800 (Placa: GBA-4821)\n" +
                "• <b>Conductor Asignado:</b> Carlos M. Andrade (C.I. 0928371940)\n" +
                "• <b>Ruta Programada:</b> Contecon Guayaquil -> Bodega Central Quito";
        }

        if (txtTurnoHorario != null)
        {
            txtTurnoHorario.text =
                "<b>TURNO DE INGRESO A MUELLE ASIGNADO:</b>\n" +
                "<color=#2ECC71>FECHA: 14/NOV/2026 | HORARIO: 08:30 AM - 10:00 AM</color>\n" +
                "<i>Presentar este Gate Pass en Garita 2 para cargar el contenedor en andén.</i>";
        }
    }

    public void TransicionarHaciaPaso7()
    {
        if (panelPaso7Final != null)
        {
            panelPaso7Final.SetActive(true);
        }
        else
        {
            Debug.LogError("[Paso 6] Falta asignar 'Panel_Paso7_Final' en el Inspector.");
        }

        if (panelPaso6Aduana != null)
        {
            panelPaso6Aduana.SetActive(false);
        }
        else
        {
            gameObject.SetActive(false);
        }
    }

    private void ActivarSubFase(int fase)
    {
        if (subPanel6_1_DAI != null) subPanel6_1_DAI.SetActive(fase == 1);
        if (subPanel6_2_Aforo != null) subPanel6_2_Aforo.SetActive(fase == 2);
        if (subPanel6_3_Bodegaje != null) subPanel6_3_Bodegaje.SetActive(fase == 3);
        if (subPanel6_4_TurnoCAS != null) subPanel6_4_TurnoCAS.SetActive(fase == 4);
    }

    private void DesactivarTodosLosModales()
    {
        if (subPanel6_1_DAI != null) subPanel6_1_DAI.SetActive(false);
        if (subPanel6_2_Aforo != null) subPanel6_2_Aforo.SetActive(false);
        if (subPanel6_3_Bodegaje != null) subPanel6_3_Bodegaje.SetActive(false);
        if (subPanel6_4_TurnoCAS != null) subPanel6_4_TurnoCAS.SetActive(false);
    }
}