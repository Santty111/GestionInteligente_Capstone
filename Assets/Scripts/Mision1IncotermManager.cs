using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public enum TipoProducto { Tecnologia, Textil, Repuestos }
public enum TipoIncoterm { FOB, CIF, EXW }

public class Mision1IncotermManager : MonoBehaviour
{
    [Header("0. Sincronización HUD Presupuesto")]
    public Mision1FlujoController flujoController;

    [Header("1. Selector de Productos")]
    public GameObject panelSelectorProductos;          // Panel_Selector_Productos
    public Button btnElegirElec;                       // Btn_Elegir_Elec
    public Button btnElegirTex;                        // Btn_Elegir_Tex
    public Button btnElegirRep;                        // Btn_Elegir_Rep

    [Header("2. Selector de Incoterms")]
    public GameObject panelSelectorIncoterm;           // Panel_Selector_Incoterm
    public Button btnElegirFOB;                        // Btn_Seleccionar_FOB
    public Button btnElegirCIF;                        // Btn_Seleccionar_CIF
    public Button btnElegirEXW;                        // Btn_Seleccionar_EXW

    [Header("3. Panel Paso 2 (Documentación en Origen)")]
    public GameObject panelDocumentacionOrigen;        // Panel_Paso2_DocumentacionOrigen
    public TMP_Text txtResumenIncoterm;                // Txt_ResumenIncoterm
    public TMP_Text txtDetalleFactura;                 // Txt_Detalle_Factura
    public TMP_Text txtDetalleBL;                      // Txt_Detalle_BL
    public TMP_Text txtImpactoFinanciero;              // Txt_ImpactoFinanciero
    public Button btnAutorizarZarpe;                   // Btn_AutorizarZarpe

    [Header("4. Panel Paso 3 (Llamada Carga Completa)")]
    public GameObject panelCargaCompleta;              // Panel_Paso3_CargaCompleta
    public TMP_Text txtDialogoFinal;                   // Dialogo_Final
    public Button btnSiguienteFinalPuerto;             // Arrastra: Btn_Siguiente_Final_Puerto

    [Header("5. Panel Paso 4 (Oficina y Mapa de Navegación)")]
    public GameObject panelPaso4Mapa;                  // Arrastra: Panel_Paso4_Mapa
    public Button btnMapaPared;                        // Arrastra: Btn_Mapa
    public GameObject panelMapaGrande;                 // Arrastra: Panel_Mapa
    public Button btnCerrarMapa;                       // Opcional: Botón X o para cerrar el mapa

    [Header("6. Panel Paso 5 (Escena Presencial Muelle Puerto)")]
    public GameObject panelPaso5Puerto;                // Arrastra: Panel_Paso5_LlegadaPuerto

    // Estado del juego
    public static TipoProducto ProductoActivo = TipoProducto.Tecnologia;
    public static TipoIncoterm IncotermActivo = TipoIncoterm.FOB;

    private float presupuesto = 45000f;
    private string nombreMercancia = "Laptops & Monitores 2K";
    private float valorComercialMercancia = 38000f;
    private string descripcionBultos = "480 cajas acondicionadas";

    private string[] lineasLlamadaZarpe;
    private int indiceLlamadaZarpe = 0;

    private void Start()
    {
        // 1. Selector de Productos
        if (btnElegirElec != null) btnElegirElec.onClick.AddListener(() => ConfirmarProducto(TipoProducto.Tecnologia));
        if (btnElegirTex != null) btnElegirTex.onClick.AddListener(() => ConfirmarProducto(TipoProducto.Textil));
        if (btnElegirRep != null) btnElegirRep.onClick.AddListener(() => ConfirmarProducto(TipoProducto.Repuestos));

        // 2. Selector de Incoterms
        if (btnElegirFOB != null) btnElegirFOB.onClick.AddListener(() => ConfirmarIncoterm(TipoIncoterm.FOB));
        if (btnElegirCIF != null) btnElegirCIF.onClick.AddListener(() => ConfirmarIncoterm(TipoIncoterm.CIF));
        if (btnElegirEXW != null) btnElegirEXW.onClick.AddListener(() => ConfirmarIncoterm(TipoIncoterm.EXW));

        // 3. Botón Zarpe: Liquidación financiera y llamada del buque
        if (btnAutorizarZarpe != null)
            btnAutorizarZarpe.onClick.AddListener(AbrirLlamadaCargaCompleta);

        // 4. Botón Siguiente en llamada de zarpe
        if (btnSiguienteFinalPuerto != null)
        {
            btnSiguienteFinalPuerto.onClick.RemoveAllListeners();
            btnSiguienteFinalPuerto.onClick.AddListener(AvanzarLlamadaZarpe);
        }

        // 5. Interacción con el Mapa en la Pared (Paso 4)
        if (btnMapaPared != null)
            btnMapaPared.onClick.AddListener(AbrirMapaGrande);

        if (btnCerrarMapa != null)
            btnCerrarMapa.onClick.AddListener(CerrarMapaGrande);
    }

    // --- FASE 1: Selección de Mercancía ---
    public void ConfirmarProducto(TipoProducto elegido)
    {
        ProductoActivo = elegido;

        switch (ProductoActivo)
        {
            case TipoProducto.Tecnologia:
                nombreMercancia = "Laptops & Monitores 2K";
                valorComercialMercancia = 38000f;
                descripcionBultos = "480 cajas acondicionadas (Tecnología)";
                break;
            case TipoProducto.Textil:
                nombreMercancia = "Ropa Deportiva & Calzado";
                valorComercialMercancia = 27500f;
                descripcionBultos = "620 bultos comprimidos (Confección Textil)";
                break;
            case TipoProducto.Repuestos:
                nombreMercancia = "Repuestos & Motores Livianos";
                valorComercialMercancia = 32000f;
                descripcionBultos = "310 cajones reforzados (Automotriz)";
                break;
        }

        if (panelSelectorProductos != null)
            panelSelectorProductos.SetActive(false);

        if (panelSelectorIncoterm != null)
            panelSelectorIncoterm.SetActive(true);
    }

    // --- FASE 2: Selección de Incoterm y Documentación de Origen ---
    public void ConfirmarIncoterm(TipoIncoterm elegido)
    {
        IncotermActivo = elegido;

        if (panelSelectorIncoterm != null)
            panelSelectorIncoterm.SetActive(false);

        if (panelDocumentacionOrigen != null)
        {
            panelDocumentacionOrigen.SetActive(true);
            ConfigurarDocumentosOrigen();
        }
    }

    private void ConfigurarDocumentosOrigen()
    {
        switch (IncotermActivo)
        {
            case TipoIncoterm.FOB:
                if (txtResumenIncoterm != null)
                    txtResumenIncoterm.text = $"<b>Término Activo: FOB</b> | Mercancía: <color=#00F5D4>{nombreMercancia}</color>\nProveedor entrega en muelle de Shanghai y emite B/L.";

                if (txtDetalleFactura != null)
                    txtDetalleFactura.text = $"• Mercancía: <b>{nombreMercancia}</b>.\n• Factura Comercial FOB: <b>${valorComercialMercancia:N0} USD</b>.\n• Bultos: {descripcionBultos} estibados a bordo.";

                if (txtDetalleBL != null)
                    txtDetalleBL.text = $"• <b>B/L #MSKU-74910283:</b> Emitido formalmente.\n• Modalidad: <i>Freight Collect</i> (flete marítimo por pagar al arribo).\n• Contenedor: 40' HQ sellado.";

                if (txtImpactoFinanciero != null)
                    txtImpactoFinanciero.text = "<color=#2ECC71>Liquidación en Origen: $0 deducidos | Flete marítimo se pagará en puerto destino</color>";
                break;

            case TipoIncoterm.CIF:
                float valorConFleteYSeguro = valorComercialMercancia + 2800f;
                if (txtResumenIncoterm != null)
                    txtResumenIncoterm.text = $"<b>Término Activo: CIF</b> | Mercancía: <color=#00F5D4>{nombreMercancia}</color>\nProveedor asume transporte marítimo internacional y póliza.";

                if (txtDetalleFactura != null)
                    txtDetalleFactura.text = $"• Mercancía: <b>{nombreMercancia}</b> con seguro internacional.\n• Factura CIF: <b>${valorConFleteYSeguro:N0} USD</b>.\n• Bultos: {descripcionBultos}.";

                if (txtDetalleBL != null)
                    txtDetalleBL.text = $"• <b>B/L #MSKU-74910283:</b> Sellado y liberado.\n• Modalidad: <i>Freight Prepaid</i> (flete marítimo cubierto por el vendedor).\n• Carga asegurada rumbo a terminal Contecon.";

                if (txtImpactoFinanciero != null)
                    txtImpactoFinanciero.text = "<color=#38BDF8>Liquidación en Origen: $0 deducidos | El proveedor cubrió el flete y seguro</color>";
                break;

            case TipoIncoterm.EXW:
                DeducirPresupuesto(900f);

                if (txtResumenIncoterm != null)
                    txtResumenIncoterm.text = $"<b>Término Activo: EXW</b> | Mercancía: <color=#00F5D4>{nombreMercancia}</color>\nMáximo riesgo: Retiro en fábrica y transporte interno en Asia a tu cargo.";

                if (txtDetalleFactura != null)
                    txtDetalleFactura.text = $"• Mercancía retirada en fábrica: <b>{nombreMercancia}</b>.\n• Factura en fábrica: <b>${(valorComercialMercancia - 2500f):N0} USD</b>.\n• Bultos: {descripcionBultos} inspeccionados en fábrica.";

                if (txtDetalleBL != null)
                    txtDetalleBL.text = $"• <b>B/L #MSKU-74910283:</b> Tramitado por tu agente aduanal en Asia.\n• Carga ingresada al muelle tras flete local en origen.";

                if (txtImpactoFinanciero != null)
                    txtImpactoFinanciero.text = "<color=#E74C3C>Liquidación en Origen: -$900 deducidos (Flete terrestre Asia + Despacho salida)</color>";
                break;
        }
    }

    // --- FASE 3: Cobro y Llamada tras Zarpe ---
    public void AbrirLlamadaCargaCompleta()
    {
        float montoADescontar = 0f;

        if (IncotermActivo == TipoIncoterm.CIF)
            montoADescontar = valorComercialMercancia + 2800f;
        else if (IncotermActivo == TipoIncoterm.FOB)
            montoADescontar = valorComercialMercancia;
        else if (IncotermActivo == TipoIncoterm.EXW)
            montoADescontar = valorComercialMercancia - 2500f;

        DeducirPresupuesto(montoADescontar);

        if (panelDocumentacionOrigen != null)
            panelDocumentacionOrigen.SetActive(false);

        if (panelCargaCompleta != null)
            panelCargaCompleta.SetActive(true);

        if (btnSiguienteFinalPuerto != null)
            btnSiguienteFinalPuerto.gameObject.SetActive(true);

        string recordatorioIncoterm = "";
        if (IncotermActivo == TipoIncoterm.CIF)
            recordatorioIncoterm = $"La factura CIF de ${montoADescontar:N0} fue debitada. Flete y seguro marítimo quedaron saldados; tu saldo restante cubrirá la aduana SENAE y los gastos portuarios en Ecuador.";
        else if (IncotermActivo == TipoIncoterm.FOB)
            recordatorioIncoterm = $"Factura FOB de ${montoADescontar:N0} cancelada al fabricante. Recuerda que el flete marítimo viene por cobrar (Freight Collect) y deberás liquidarlo al arribar a Contecon.";
        else
            recordatorioIncoterm = $"Mercancía en fábrica cancelada por ${montoADescontar:N0}. Mantén reserva para el flete internacional y los trámites de desaduanamiento en Guayaquil.";

        lineasLlamadaZarpe = new string[]
        {
            $"¡Excelente noticia! La compra de {nombreMercancia} ha sido liquidada y la carga ya está estibada en el contenedor 40' HQ a bordo del buque.",
            "El barco portacontenedores acaba de zarpar e inicia su travesía transoceánica rumbo a Ecuador.",
            "Apenas el buque se aproxime a puerto, la naviera emitirá el aviso de llegada oficial notificando el arribo a Contecon Guayaquil.",
            recordatorioIncoterm,
            "Regresemos a la oficina. Revisa el mapa de monitoreo marítimo en la pared para rastrear la travesía del buque hacia nuestras costas."
        };

        indiceLlamadaZarpe = 0;
        MostrarLineaLlamadaZarpe();
    }

    public void AvanzarLlamadaZarpe()
    {
        if (lineasLlamadaZarpe == null || lineasLlamadaZarpe.Length == 0) return;

        if (indiceLlamadaZarpe < lineasLlamadaZarpe.Length - 1)
        {
            indiceLlamadaZarpe++;
            MostrarLineaLlamadaZarpe();
        }
        else
        {
            FinalizarLlamadaYPasarAOficinaMapa();
        }
    }

    private void MostrarLineaLlamadaZarpe()
    {
        if (txtDialogoFinal != null && lineasLlamadaZarpe != null && lineasLlamadaZarpe.Length > 0)
        {
            txtDialogoFinal.text = lineasLlamadaZarpe[indiceLlamadaZarpe];
        }
    }

    // --- FASE 4: Oficina y Mapa Interactivo ---
    private void FinalizarLlamadaYPasarAOficinaMapa()
    {
        if (btnSiguienteFinalPuerto != null)
            btnSiguienteFinalPuerto.gameObject.SetActive(false);

        if (panelCargaCompleta != null)
            panelCargaCompleta.SetActive(false);

        if (panelPaso4Mapa != null)
            panelPaso4Mapa.SetActive(true);

        if (panelMapaGrande != null)
            panelMapaGrande.SetActive(false);
    }

    public void AbrirMapaGrande()
    {
        if (panelMapaGrande != null)
            panelMapaGrande.SetActive(true);
    }

    public void CerrarMapaGrande()
    {
        if (panelMapaGrande != null)
            panelMapaGrande.SetActive(false);
    }

    // --- FASE 5: Transición al Muelle Presencial (Paso 5) ---
    public void ContinuarHaciaArriboPuerto()
    {
        if (panelMapaGrande != null)
            panelMapaGrande.SetActive(false);

        if (panelPaso4Mapa != null)
            panelPaso4Mapa.SetActive(false);

        if (panelPaso5Puerto != null)
        {
            panelPaso5Puerto.SetActive(true);
        }
        else
        {
            Debug.LogWarning("[TMS] Falta asignar el objeto 'panelPaso5Puerto' en el Inspector.");
        }
    }

    // --- Gestión Financiera Centralizada ---
    public void DeducirPresupuesto(float monto)
    {
        presupuesto -= monto;
        if (flujoController != null)
        {
            flujoController.ModificarPresupuesto(-monto);
        }
    }
}