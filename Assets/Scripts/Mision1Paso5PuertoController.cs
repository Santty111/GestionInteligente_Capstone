using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mision1Paso5PuertoController : MonoBehaviour
{
    [Header("0. Controladores Globales")]
    public Mision1FlujoController flujoController;
    public Mision1IncotermManager incotermManager;

    [Header("1. Sub-panel 5.1: Notificación de Atraque")]
    public GameObject subPanel5_1_DialogoArribo;
    public Button btnAvanzarArribo;

    [Header("2. Sub-panel 5.2: Aviso de Llegada y Valores")]
    public GameObject subPanel5_2_ModalAvisoLlegada;
    public TMP_Text txtInfoBuqueCarga;
    public TMP_Text txtDetalleCobro;
    public Button btnReceptarYPagar;

    [Header("3. Sub-panel 5.3: Garantía de Contenedor")]
    public GameObject subPanel5_3_ModalGarantia;
    public GameObject selloGarantiaFirmada;
    public Button btnFirmarGarantia;

    [Header("4. Sub-panel 5.4: Visto Bueno Naviero")]
    public GameObject subPanel5_4_DialogoVistoBueno;
    public GameObject selloVistoBuenoEcuapass;
    public Button btnSolicitarVB;

    [Header("5. Sub-panel 5.5: CAS y Paso a Aduana SENAE")]
    public GameObject subPanel5_5_DialogoCAS;
    public Button btnContinuarSENAE;

    [Header("6. Transición de Paneles (Puerto -> Aduana)")]
    public GameObject panelPaso5Puerto; // Arrastra: Panel_Paso5_LlegadaPuerto
    public GameObject panelPaso6Aduana; // Arrastra: Panel_Paso6_Aduana

    private float montoLiquidacionNaviera = 0f;
    private string nombreMercancia = "Laptops & Monitores 2K";

    private void Awake()
    {
        if (btnAvanzarArribo != null)
        {
            btnAvanzarArribo.onClick.RemoveAllListeners();
            btnAvanzarArribo.onClick.AddListener(AbrirModalAvisoLlegada);
        }

        if (btnReceptarYPagar != null)
        {
            btnReceptarYPagar.onClick.RemoveAllListeners();
            btnReceptarYPagar.onClick.AddListener(PagarValoresNavieros);
        }

        if (btnFirmarGarantia != null)
        {
            btnFirmarGarantia.onClick.RemoveAllListeners();
            btnFirmarGarantia.onClick.AddListener(FirmarCartaGarantia);
        }

        if (btnSolicitarVB != null)
        {
            btnSolicitarVB.onClick.RemoveAllListeners();
            btnSolicitarVB.onClick.AddListener(EmitirVistoBueno);
        }

        if (btnContinuarSENAE != null)
        {
            btnContinuarSENAE.onClick.RemoveAllListeners();
            btnContinuarSENAE.onClick.AddListener(TransicionarHaciaAduanaSENAE);
        }
    }

    private void OnEnable()
    {
        if (btnFirmarGarantia != null) btnFirmarGarantia.interactable = true;
        if (btnSolicitarVB != null) btnSolicitarVB.interactable = true;
        if (btnContinuarSENAE != null) btnContinuarSENAE.interactable = true;

        if (selloGarantiaFirmada != null) selloGarantiaFirmada.SetActive(false);
        if (selloVistoBuenoEcuapass != null) selloVistoBuenoEcuapass.SetActive(false);

        ActivarFase(1);
        ConfigurarTextosSegunPartida();
    }

    public void ConfigurarTextosSegunPartida()
    {
        switch (Mision1IncotermManager.ProductoActivo)
        {
            case TipoProducto.Tecnologia:
                nombreMercancia = "Laptops & Monitores 2K (480 cajas)";
                break;
            case TipoProducto.Textil:
                nombreMercancia = "Ropa Deportiva & Calzado (620 bultos)";
                break;
            case TipoProducto.Repuestos:
                nombreMercancia = "Repuestos & Motores Livianos (310 cajones)";
                break;
        }

        if (txtInfoBuqueCarga != null)
        {
            txtInfoBuqueCarga.text = $"• <b>Buque:</b> CMA CGM COCHIN | Muelle Contecon Guayaquil\n" +
                                     $"• <b>Mercancía Declarada:</b> <color=#38BDF8>{nombreMercancia}</color>\n" +
                                     $"• <b>Unidad de Carga:</b> 1x40' HQ Sellado (#MSKU-74910283)";
        }

        float gastosLocalesPuerto = 650f;
        float fleteMaritimoDestino = 0f;

        if (Mision1IncotermManager.IncotermActivo == TipoIncoterm.CIF)
        {
            fleteMaritimoDestino = 0f;
            montoLiquidacionNaviera = gastosLocalesPuerto;

            if (txtDetalleCobro != null)
            {
                txtDetalleCobro.text =
                    $"<b>DESGLOSE DE LIQUIDACIÓN NAVIERA - TÉRMINO CIF:</b>\n\n" +
                    $"• Flete Marítimo Internacional: <color=#2ECC71>$0 USD (Freight Prepaid en Origen)</color>\n" +
                    $"• Gastos de Terminal en Destino (THC Guayaquil): $420 USD\n" +
                    $"• Derechos de Emisión Documental & Liberación B/L: $230 USD\n\n" +
                    $"TOTAL A LIQUIDAR A LA NAVIERA: <color=#F1C40F><b>${montoLiquidacionNaviera:N0} USD</b></color>";
            }
        }
        else if (Mision1IncotermManager.IncotermActivo == TipoIncoterm.FOB)
        {
            fleteMaritimoDestino = 2800f;
            montoLiquidacionNaviera = fleteMaritimoDestino + gastosLocalesPuerto;

            if (txtDetalleCobro != null)
            {
                txtDetalleCobro.text =
                    $"<b>DESGLOSE DE LIQUIDACIÓN NAVIERA - TÉRMINO FOB:</b>\n\n" +
                    $"• Flete Marítimo Internacional (Freight Collect): <color=#E74C3C>$2,800 USD</color>\n" +
                    $"• Gastos de Terminal en Destino (THC Guayaquil): $420 USD\n" +
                    $"• Derechos de Emisión Documental & Liberación B/L: $230 USD\n\n" +
                    $"TOTAL A LIQUIDAR A LA NAVIERA: <color=#F1C40F><b>${montoLiquidacionNaviera:N0} USD</b></color>";
            }
        }
        else // EXW
        {
            fleteMaritimoDestino = 3100f;
            montoLiquidacionNaviera = fleteMaritimoDestino + gastosLocalesPuerto;

            if (txtDetalleCobro != null)
            {
                txtDetalleCobro.text =
                    $"<b>DESGLOSE DE LIQUIDACIÓN NAVIERA - TÉRMINO EXW:</b>\n\n" +
                    $"• Flete Marítimo Internacional (Freight Collect): <color=#E74C3C>$3,100 USD</color>\n" +
                    $"• Gastos de Terminal en Destino (THC Guayaquil): $420 USD\n" +
                    $"• Derechos de Emisión Documental & Liberación B/L: $230 USD\n\n" +
                    $"TOTAL A LIQUIDAR A LA NAVIERA: <color=#F1C40F><b>${montoLiquidacionNaviera:N0} USD</b></color>";
            }
        }
    }

    public void AbrirModalAvisoLlegada()
    {
        ActivarFase(2);
    }

    public void PagarValoresNavieros()
    {
        if (flujoController != null)
        {
            flujoController.ModificarPresupuesto(-montoLiquidacionNaviera);
        }

        Debug.Log($"[Puerto] Liquidación naviera pagada: -${montoLiquidacionNaviera:N0}");
        ActivarFase(3);
    }

    public void FirmarCartaGarantia()
    {
        StartCoroutine(AnimarFirmaGarantia());
    }

    private IEnumerator AnimarFirmaGarantia()
    {
        if (btnFirmarGarantia != null)
            btnFirmarGarantia.interactable = false;

        if (selloGarantiaFirmada != null)
            selloGarantiaFirmada.SetActive(true);

        yield return new WaitForSeconds(1.2f);
        ActivarFase(4);
    }

    public void EmitirVistoBueno()
    {
        StartCoroutine(AnimarEmisionVistoBueno());
    }

    private IEnumerator AnimarEmisionVistoBueno()
    {
        if (btnSolicitarVB != null)
            btnSolicitarVB.interactable = false;

        if (selloVistoBuenoEcuapass != null)
            selloVistoBuenoEcuapass.SetActive(true);

        yield return new WaitForSeconds(1.5f);
        ActivarFase(5);
    }

    public void TransicionarHaciaAduanaSENAE()
    {
        Debug.Log("[Paso 5] Clic detectado en 'btnContinuarSENAE'. Iniciando transición segura al Paso 6...");

        // 1. Activar el Paso 6
        if (panelPaso6Aduana != null)
        {
            panelPaso6Aduana.SetActive(true);
            Debug.Log("[Paso 5] 'panelPaso6Aduana' activado correctamente.");
        }
        else
        {
            Debug.LogError("[ERROR] La casilla 'Panel Paso 6 Aduana' está vacía en el Inspector.");
        }

        // 2. Apagar ÚNICAMENTE el panel del Paso 5 (NUNCA el padre 5_Panel_Mision1_Importacion)
        if (panelPaso5Puerto != null)
        {
            panelPaso5Puerto.SetActive(false);
            Debug.Log("[Paso 5] 'panelPaso5Puerto' desactivado sin afectar al objeto raíz.");
        }
        else
        {
            // Solo si panelPaso5Puerto no se asignó en Inspector se usa gameObject como respaldo
            gameObject.SetActive(false);
        }
    }

    private void ActivarFase(int fase)
    {
        if (subPanel5_1_DialogoArribo != null) subPanel5_1_DialogoArribo.SetActive(fase == 1);
        if (subPanel5_2_ModalAvisoLlegada != null) subPanel5_2_ModalAvisoLlegada.SetActive(fase == 2);
        if (subPanel5_3_ModalGarantia != null) subPanel5_3_ModalGarantia.SetActive(fase == 3);
        if (subPanel5_4_DialogoVistoBueno != null) subPanel5_4_DialogoVistoBueno.SetActive(fase == 4);
        if (subPanel5_5_DialogoCAS != null) subPanel5_5_DialogoCAS.SetActive(fase == 5);
    }
}