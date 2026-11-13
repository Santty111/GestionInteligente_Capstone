using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class Mision1FlujoInicialController : MonoBehaviour
{
    [Header("1. Pantalla de Descripción (Misión 1)")]
    public GameObject panelDescripcionMision; // Objeto 'Ventana_Sistema1'
    public Button btnInicioMision;             // Objeto 'Btn_inicio_Mision'
    public Button btnVolverMisiones;           // Objeto 'Btn_VolverMisiones'

    [Header("2. Menú Cuadrícula de Misiones")]
    public GameObject panelMenuMisiones;       // Panel donde están los 6 candados/misiones

    [Header("3. Pantalla de Llamada")]
    public GameObject panelLlamada;            // Objeto 'Pantalla_Llamada'
    public TMP_Text txtDialogoLlamada;         // Objeto 'Text (TMP)' dentro de Barra_Txt
    public Button btnAvanzarLlamada;           // Objeto 'Barra_Txt' (con Button) para dar clic y avanzar

    [Header("4. Diálogo de la Llamada")]
    [TextArea(2, 4)]
    public string[] lineasLlamada = new string[]
    {
        "¡Hola! Te llamo desde el patio de contenedores en Contecon Guayaquil.",
        "El buque mercante acaba de atracar en el muelle internacional y tu contenedor 40' HQ ya fue descargado.",
        "La naviera ya emitió el aviso de llegada con los gastos locales a cancelar y contamos con 4 días libres de sobrestadía.",
        "Debemos liquidar los valores para obtener el visto bueno y la CAS antes de pasar la carga a la aduana con el SENAE."
    };

    private int indiceLinea = 0;

    private void Start()
    {
        // Asignar listeners de botones
        if (btnInicioMision != null)
            btnInicioMision.onClick.AddListener(IniciarLlamada);

        if (btnVolverMisiones != null)
            btnVolverMisiones.onClick.AddListener(RegresarAMenuMisiones);

        if (btnAvanzarLlamada != null)
            btnAvanzarLlamada.onClick.AddListener(AvanzarDialogoLlamada);
    }

    // --- Flujo de Transiciones ---

    public void IniciarLlamada()
    {
        // Apaga la pantalla de descripción
        if (panelDescripcionMision != null)
            panelDescripcionMision.SetActive(false);

        // Enciende la pantalla de llamada
        if (panelLlamada != null)
            panelLlamada.SetActive(true);

        // Reinicia e inicia el diálogo del agente portuario
        indiceLinea = 0;
        MostrarLineaLlamada();
    }

    public void RegresarAMenuMisiones()
    {
        // Oculta la pantalla de descripción
        if (panelDescripcionMision != null)
            panelDescripcionMision.SetActive(false);

        // Reactiva la cuadrícula con las 6 misiones de la laptop
        if (panelMenuMisiones != null)
            panelMenuMisiones.SetActive(true);
    }

    // --- Control de Diálogo de la Llamada ---

    public void AvanzarDialogoLlamada()
    {
        if (indiceLinea < lineasLlamada.Length - 1)
        {
            indiceLinea++;
            MostrarLineaLlamada();
        }
        else
        {
            Debug.Log("[Llamada] Fin de la llamada. Listo para conectar la siguiente fase.");
        }
    }

    private void MostrarLineaLlamada()
    {
        if (txtDialogoLlamada != null && lineasLlamada.Length > 0)
        {
            txtDialogoLlamada.text = lineasLlamada[indiceLinea];
        }
    }
}