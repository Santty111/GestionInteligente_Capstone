using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class OficinaSistemaTMSController : MonoBehaviour
{
    [Header("1. Interacción con la Laptop")]
    public Button btnLaptop;                  // Objeto 'Btn_Laptop'
    public GameObject pantallaLaptopModal;    // Objeto 'Pantalla_Laptop_Modal'
    public Button btnCerrarModal;             // Objeto 'Btn_CerrarModal' [X]
    public TMP_Text txtBarraTitulo;           // Objeto 'Txt_BarraTitulo'

    [Header("2. Misión 1 (Activa)")]
    public Button btnMision1;                 // Objeto 'Fnd_msn1'

    [Header("3. Misiones Bloqueadas (2 al 6)")]
    public Button[] botonesMisionesBloqueadas;// Arrastra aquí Fnd_msn2 hasta Fnd_msn6

    [Header("4. Transición a Panel 5")]
    public GameObject panelOficina;           // Objeto '4_Panel_Oficina'
    public GameObject panel5;                 // Tu objeto correspondiente al Panel 5

    private void Start()
    {
        // Abrir y cerrar la laptop
        if (btnLaptop != null)
            btnLaptop.onClick.AddListener(AbrirLaptop);

        if (btnCerrarModal != null)
            btnCerrarModal.onClick.AddListener(CerrarLaptop);

        // Misión 1: salto directo al Panel 5
        if (btnMision1 != null)
            btnMision1.onClick.AddListener(AbrirPanel5);

        // Misiones 2 a 6: aviso de bloqueo
        if (botonesMisionesBloqueadas != null)
        {
            for (int i = 0; i < botonesMisionesBloqueadas.Length; i++)
            {
                int numeroMision = i + 2;
                if (botonesMisionesBloqueadas[i] != null)
                {
                    botonesMisionesBloqueadas[i].onClick.AddListener(() => AvisarMisionBloqueada(numeroMision));
                }
            }
        }

        // Asegurar que la laptop inicie cerrada
        if (pantallaLaptopModal != null)
            pantallaLaptopModal.SetActive(false);
    }

    public void AbrirLaptop()
    {
        if (pantallaLaptopModal != null)
            pantallaLaptopModal.SetActive(true);

        if (btnLaptop != null)
            btnLaptop.interactable = false;

        RestablecerTitulo();
    }

    public void CerrarLaptop()
    {
        if (pantallaLaptopModal != null)
            pantallaLaptopModal.SetActive(false);

        if (btnLaptop != null)
            btnLaptop.interactable = true;
    }

    public void AbrirPanel5()
    {
        // Apaga la oficina completa y enciende el Panel 5
        if (panelOficina != null)
            panelOficina.SetActive(false);

        if (panel5 != null)
            panel5.SetActive(true);
    }

    public void AvisarMisionBloqueada(int numeroMision)
    {
        if (txtBarraTitulo != null)
        {
            txtBarraTitulo.text = $"<color=#E74C3C>MISIÓN {numeroMision} BLOQUEADA</color>";
            CancelInvoke(nameof(RestablecerTitulo));
            Invoke(nameof(RestablecerTitulo), 2f);
        }
    }

    private void RestablecerTitulo()
    {
        if (txtBarraTitulo != null)
            txtBarraTitulo.text = "SISTEMA TMS - MÓDULO DE IMPORTACIONES V1.0";
    }
}