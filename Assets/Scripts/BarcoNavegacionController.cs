using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class BarcoNavegacionController : MonoBehaviour
{
    [Header("1. Referencias")]
    public RectTransform rectBarco;                    // RectTransform de Barcolcon
    public Button btnBarco;                            // Componente Button de Barcolcon
    public GameObject avisoAtracado;                   // Txt_Aviso_Atraco (opcional)
    public Mision1IncotermManager incotermManager;     // Script principal de la misión

    [Header("2. Coordenadas de Navegación Directa")]
        public Vector2 puntoSalida = new Vector2(-887f, 424f);       // Esquina superior izquierda
    public Vector2 puertoGuayaquil = new Vector2(-140f, -151f);  //Terminal Contecon en Guayaquil

    [Header("3. Parámetros de Simulación")]
    [Tooltip("Duración en segundos del viaje directo de inicio a fin")]
    public float duracionTravesia = 4.0f;

    private bool travesiaCompletada = false;

    private void Awake()
    {
        if (rectBarco == null)
            rectBarco = GetComponent<RectTransform>();

        if (btnBarco == null)
            btnBarco = GetComponent<Button>();

        // Evita que el barco se vuelva transparente mientras interactable está desactivado
        if (btnBarco != null)
        {
            ColorBlock cb = btnBarco.colors;
            cb.disabledColor = Color.white;
            btnBarco.colors = cb;
        }
    }

    private void Start()
    {
        if (btnBarco != null)
        {
            btnBarco.onClick.RemoveAllListeners();
            btnBarco.onClick.AddListener(AlHacerClicEnBarco);
        }
    }

    private void OnEnable()
    {
        ReiniciarYComenzarRuta();
    }

    public void ReiniciarYComenzarRuta()
    {
        StopAllCoroutines();
        travesiaCompletada = false;

        // 1. Desactivar botón y cartel de aviso mientras navega
        if (btnBarco != null)
            btnBarco.interactable = false;

        if (avisoAtracado != null)
            avisoAtracado.SetActive(false);

        // 2. Posicionar en el punto de salida
        if (rectBarco != null)
            rectBarco.anchoredPosition = puntoSalida;

        // 3. Iniciar la trayectoria directa
        StartCoroutine(RutinaNavegacionDirecta());
    }

    private IEnumerator RutinaNavegacionDirecta()
    {
        float tiempo = 0f;

        // Interpolación continua y suave de Inicio directo a Fin
        while (tiempo < duracionTravesia)
        {
            tiempo += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, tiempo / duracionTravesia);
            rectBarco.anchoredPosition = Vector2.Lerp(puntoSalida, puertoGuayaquil, t);
            yield return null;
        }

        // Asegurar posición final exacta en Guayaquil
        rectBarco.anchoredPosition = puertoGuayaquil;

        // Al culminar la ruta, activar el botón de arribo
        LlegadaAPuertoContecon();
    }

    private void LlegadaAPuertoContecon()
    {
        travesiaCompletada = true;

        // Habilitar interacción de clic
        if (btnBarco != null)
            btnBarco.interactable = true;

        if (avisoAtracado != null)
            avisoAtracado.SetActive(true);

        // Rebote visual de aviso de llegada
        StartCoroutine(EfectoPulsoAtraco());

        Debug.Log("[TMS] Buque mercante arribó directamente a Contecon Guayaquil. Botón de puerto habilitado.");
    }

    private IEnumerator EfectoPulsoAtraco()
    {
        Vector3 escalaOriginal = rectBarco.localScale;
        Vector3 escalaAumento = escalaOriginal * 1.25f;

        float duracion = 0.25f;
        float t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            rectBarco.localScale = Vector3.Lerp(escalaOriginal, escalaAumento, t / duracion);
            yield return null;
        }

        t = 0f;
        while (t < duracion)
        {
            t += Time.deltaTime;
            rectBarco.localScale = Vector3.Lerp(escalaAumento, escalaOriginal, t / duracion);
            yield return null;
        }

        rectBarco.localScale = escalaOriginal;
    }

    public void AlHacerClicEnBarco()
    {
        if (!travesiaCompletada) return;

        Debug.Log("[TMS] Clic en buque arribado. Avanzando a los trámites portuarios en Ecuador...");

        if (incotermManager != null)
        {
            incotermManager.ContinuarHaciaArriboPuerto();
        }
    }
}