using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogoBienvenidaController : MonoBehaviour
{
    [Header("1. Elementos de Interfaz")]
    public TMP_Text txtLeyenda;                 // Objeto 'Leyenda' (TextMeshPro)
    public Button btnSiguiente;                 // Objeto 'Btn_Siguiente'
    public Button btnVerMisiones;               // Objeto 'Btn_VerMisiones'
    public TMP_Text txtIndicadorClic;           // Opcional: indicador visual de clic

    [Header("2. Transición entre Pantallas")]
    public GameObject panelInicioJuego;         // Este objeto: '3_Panel_InicioJuego'
    public GameObject panelOficina;             // Siguiente pantalla: '4_Panel_Oficina'

    [Header("3. Efectos de Audio (Opcional)")]
    public AudioSource audioSource;
    public AudioClip clipAvanzarTexto;
    public AudioClip clipBotonFinal;

    [Header("4. Diálogo de Inducción - Importación Marítima")]
    [TextArea(2, 4)]
    public string[] lineasDialogo = new string[]
    {
        "Hola, mi nombre es Melissa. Soy la supervisora de operaciones logísticas y este es tu primer día en nuestro centro de distribución.",
        "Estoy muy emocionada por compartir esta aventura contigo, espero que puedas disfrutar y aprender",
        "La naviera ya nos envió el aviso de llegada y liquidamos los gastos locales para obtener el visto bueno y la casilla de autorización de salida (CAS).",
        "Tu objetivo operativo inicia ahora: supervisar la declaración aduanera ante el SENAE, cumplir con el aforo asignado y coordinar la salida con la bodega portuaria.",
        "Una vez cancelado el bodegaje y obtenido el turno de retiro con el camión, nos trasladaremos a la oficina para revisar el expediente digital antes de que expiren los días libres de sobrestadía.",
        "Ingresa a la oficina técnica haciendo clic en 'VER MISIONES' y accede a la laptop de operaciones para autorizar el despacho."
    };

    private int indiceActual = 0;

    private void Awake()
    {
        if (btnSiguiente != null)
        {
            btnSiguiente.onClick.RemoveAllListeners();
            btnSiguiente.onClick.AddListener(AvanzarDialogo);
        }

        if (btnVerMisiones != null)
        {
            btnVerMisiones.onClick.RemoveAllListeners();
            btnVerMisiones.onClick.AddListener(IrALaOficina);
        }
    }

    private void OnEnable()
    {
        IniciarSecuencia();
    }

    public void IniciarSecuencia()
    {
        indiceActual = 0;

        // Ocultar botón de misiones al arrancar
        if (btnVerMisiones != null)
            btnVerMisiones.gameObject.SetActive(false);

        // Activar el botón 'Btn_Siguiente'
        if (btnSiguiente != null)
            btnSiguiente.gameObject.SetActive(true);

        if (txtIndicadorClic != null)
            txtIndicadorClic.gameObject.SetActive(true);

        MostrarLineaActual();
    }

    public void AvanzarDialogo()
    {
        if (indiceActual < lineasDialogo.Length - 1)
        {
            indiceActual++;
            ReproducirSonido(clipAvanzarTexto);
            MostrarLineaActual();
        }
        else
        {
            // ÚLTIMA LÍNEA: Ocultar 'Btn_Siguiente'
            if (btnSiguiente != null)
                btnSiguiente.gameObject.SetActive(false);

            // Encender el botón de navegación hacia la oficina
            if (btnVerMisiones != null)
                btnVerMisiones.gameObject.SetActive(true);

            if (txtIndicadorClic != null)
                txtIndicadorClic.gameObject.SetActive(false);

            ReproducirSonido(clipBotonFinal);
        }
    }

    private void MostrarLineaActual()
    {
        if (txtLeyenda != null && lineasDialogo.Length > 0)
        {
            txtLeyenda.text = lineasDialogo[indiceActual];
        }

        // Si es la última línea del guion, ocultar 'Btn_Siguiente' y habilitar 'Btn_VerMisiones'
        if (indiceActual == lineasDialogo.Length - 1)
        {
            if (btnSiguiente != null)
                btnSiguiente.gameObject.SetActive(false);

            if (btnVerMisiones != null)
                btnVerMisiones.gameObject.SetActive(true);

            if (txtIndicadorClic != null)
                txtIndicadorClic.gameObject.SetActive(false);
        }
    }

    public void IrALaOficina()
    {
        ReproducirSonido(clipBotonFinal);

        // Encender primero la oficina y después apagar el panel de bienvenida
        if (panelOficina != null)
            panelOficina.SetActive(true);

        if (panelInicioJuego != null)
            panelInicioJuego.SetActive(false);
    }

    private void ReproducirSonido(AudioClip clip)
    {
        if (audioSource != null && clip != null)
            audioSource.PlayOneShot(clip);
    }
}