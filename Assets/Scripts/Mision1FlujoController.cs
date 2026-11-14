using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mision1FlujoController : MonoBehaviour
{
    [Header("1. Navegación Global de Paneles")]
    public GameObject panelMision1Completo;     // Arrastra: 5_Panel_Mision1_Importacion
    public GameObject panelOficinaCompleto;     // Arrastra: 4_Panel_Oficina
    public GameObject modalLaptopOficina;       // Arrastra: Pantalla_Laptop_Modal (dentro de 4)
    public GameObject gridMisiones;             // Arrastra: Ventana_Sistema (dentro de 4)

    [Header("2. HUD de Presupuesto")]
    public GameObject hudPresupuesto;           // Arrastra: Badge_Presupuesto
    public TMP_Text txtPresupuestoValor;        // Arrastra: Txt_Presupuesto_Valor
    public float presupuestoInicial = 60000f;   // Presupuesto asignado
    private float presupuestoActual;
    private float totalGastado;

    public float PresupuestoActual => presupuestoActual;
    public float TotalGastado => totalGastado;
    public float TiempoInicioMision { get; private set; }
    public static float UltimoGastoRegistrado { get; private set; }

    [Header("3. Pantalla de Descripción")]
    public GameObject panelDescripcionMision;   // Arrastra: Ventana_Sistema1
    public Button btnVolverMisiones;            // Arrastra: Btn_VolverMisiones
    public Button btnInicioMision;              // Arrastra: Btn_inicio_Mision

    [Header("4. Pantalla de Llamada")]
    public GameObject pantallaLlamada;          // Arrastra: Pantalla_Llamada
    public TMP_Text txtDialogoLlamada;          // Arrastra: Text (TMP) dentro de Barra_Txt
    public Button btnSiguienteLlamada;          // Arrastra: Btn_Siguiente_Llamada

    [Header("5. Siguiente Pantalla (Selección de Producto)")]
    public GameObject panelSelectorProductos;   // Arrastra: Panel_Selector_Productos

    [Header("6. Guion del Agente Portuario")]
    [TextArea(2, 4)]
    public string[] dialogoAgentePortuario = new string[]
    {
        "¡Hola! Te llamo desde la terminal portuaria. Iniciamos la operación de importación marítima.",
        "Antes de firmar con la naviera y el proveedor en Asia, debemos ingresar la orden de compra al sistema.",
        "Ingresa al módulo comercial y selecciona qué tipo de mercancía vamos a importar en este despacho."
    };

    [Header("7. Paneles de Pasos a Reiniciar")]
    public GameObject[] subPanelesAReiniciar;   // Arrastra aquí los paneles desde Selector Incoterm hasta Paso 7

    private int indiceLinea = 0;

    private void Awake()
    {
        presupuestoActual = presupuestoInicial;
        totalGastado = 0f;

        if (btnVolverMisiones != null)
        {
            btnVolverMisiones.onClick.RemoveAllListeners();
            btnVolverMisiones.onClick.AddListener(RegresarAlMenuMisiones);
        }

        if (btnInicioMision != null)
        {
            btnInicioMision.onClick.RemoveAllListeners();
            btnInicioMision.onClick.AddListener(IniciarMisionYMostrarPresupuesto);
        }

        if (btnSiguienteLlamada != null)
        {
            btnSiguienteLlamada.onClick.RemoveAllListeners();
            btnSiguienteLlamada.onClick.AddListener(AvanzarDialogo);
        }
    }

    private void OnEnable()
    {
        ReiniciarEstadoInicialMision();
    }

    public void ReiniciarEstadoInicialMision()
    {
        if (hudPresupuesto != null)
            hudPresupuesto.SetActive(false);

        if (pantallaLlamada != null)
            pantallaLlamada.SetActive(false);

        if (panelSelectorProductos != null)
            panelSelectorProductos.SetActive(false);

        if (subPanelesAReiniciar != null)
        {
            for (int i = 0; i < subPanelesAReiniciar.Length; i++)
            {
                if (subPanelesAReiniciar[i] != null)
                    subPanelesAReiniciar[i].SetActive(false);
            }
        }

        if (panelDescripcionMision != null)
            panelDescripcionMision.SetActive(true);

        indiceLinea = 0;
    }

    public void IniciarMisionYMostrarPresupuesto()
    {
        if (panelDescripcionMision != null)
            panelDescripcionMision.SetActive(false);

        if (panelSelectorProductos != null)
            panelSelectorProductos.SetActive(false);

        if (subPanelesAReiniciar != null)
        {
            for (int i = 0; i < subPanelesAReiniciar.Length; i++)
            {
                if (subPanelesAReiniciar[i] != null)
                    subPanelesAReiniciar[i].SetActive(false);
            }
        }

        presupuestoActual = presupuestoInicial;
        totalGastado = 0f;
        UltimoGastoRegistrado = 0f;
        TiempoInicioMision = Time.time;
        ActualizarTextoPresupuesto();

        if (hudPresupuesto != null)
        {
            hudPresupuesto.SetActive(true);
            hudPresupuesto.transform.SetAsLastSibling();
        }

        if (pantallaLlamada != null)
            pantallaLlamada.SetActive(true);

        if (btnSiguienteLlamada != null)
            btnSiguienteLlamada.gameObject.SetActive(true);

        indiceLinea = 0;
        MostrarLineaActual();
    }

    public void ActualizarTextoPresupuesto()
    {
        if (txtPresupuestoValor != null)
        {
            txtPresupuestoValor.text = $"Presupuesto: <color=#2ECC71>${presupuestoActual:N0}</color>";
        }
    }

    public void ModificarPresupuesto(float monto)
    {
        presupuestoActual += monto;

        if (monto < 0f)
        {
            totalGastado += Mathf.Abs(monto);
            UltimoGastoRegistrado = totalGastado;
        }

        ActualizarTextoPresupuesto();
    }

    public void RegresarAlMenuMisiones()
    {
        // 1. Ocultar HUD de presupuesto
        if (hudPresupuesto != null)
            hudPresupuesto.SetActive(false);

        // 2. Ocultar panel de Misión 1 completa
        if (panelMision1Completo != null)
            panelMision1Completo.SetActive(false);
        else
            gameObject.SetActive(false);

        // 3. Reiniciar subpaneles de la misión para que al volver a entrar empiece limpia
        ReiniciarEstadoInicialMision();

        // 4. Encender Oficina y abrir la laptop con el grid de misiones
        var oficina = FindAnyObjectByType<OficinaSistemaTMSController>(FindObjectsInactive.Include);
        if (oficina != null)
        {
            if (oficina.panelOficina != null)
                oficina.panelOficina.SetActive(true);
            else if (panelOficinaCompleto != null)
                panelOficinaCompleto.SetActive(true);

            oficina.AbrirLaptop();
        }
        else
        {
            if (panelOficinaCompleto != null)
                panelOficinaCompleto.SetActive(true);

            if (modalLaptopOficina != null)
                modalLaptopOficina.SetActive(true);

            if (gridMisiones != null)
                gridMisiones.SetActive(true);
        }

        Debug.Log("<color=green>[Mision1FlujoController] Regreso exitoso a la Oficina y al Grid de Misiones.</color>");
    }

    public void AvanzarDialogo()
    {
        if (dialogoAgentePortuario == null || dialogoAgentePortuario.Length == 0) return;

        if (indiceLinea < dialogoAgentePortuario.Length - 1)
        {
            indiceLinea++;
            MostrarLineaActual();
        }
        else
        {
            FinalizarLlamadaYPasarAProductos();
        }
    }

    private void MostrarLineaActual()
    {
        if (txtDialogoLlamada != null && dialogoAgentePortuario != null && dialogoAgentePortuario.Length > 0)
        {
            txtDialogoLlamada.text = dialogoAgentePortuario[indiceLinea];
        }
    }

    private void FinalizarLlamadaYPasarAProductos()
    {
        if (panelSelectorProductos != null)
            panelSelectorProductos.SetActive(true);

        if (pantallaLlamada != null)
            pantallaLlamada.SetActive(false);
    }
}