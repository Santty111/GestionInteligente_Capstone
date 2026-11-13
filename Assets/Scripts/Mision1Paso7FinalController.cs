using UnityEngine;
using UnityEngine.UI;

public class Mision1Paso7FinalController : MonoBehaviour
{
    [Header("1. Elementos de la Pantalla Final")]
    public GameObject panelPaso7Final;                     // Arrastra: Panel_Paso7_Final
    public Button btnPantallaFinal;                        // Arrastra: Btn_Pantalla_Final (SIGUIENTE)

    [Header("2. Transición al Dashboard de Desempeño")]
    public GameObject panelDashboard;                      // Arrastra: 6_Panel_Dashboard
    public Mision1DashboardController dashboardController; // Arrastra: 6_Panel_Dashboard
    public GameObject panelMision1Completo;                // Arrastra: 5_Panel_Mision1_Importacion
    public GameObject hudPresupuesto;                      // Arrastra: Badge_Presupuesto

    private void Awake()
    {
        if (btnPantallaFinal != null)
        {
            btnPantallaFinal.onClick.RemoveAllListeners();
            btnPantallaFinal.onClick.AddListener(IrAlDashboard);
        }
    }

    public void IrAlDashboard()
    {
        if (panelDashboard != null)
            panelDashboard.SetActive(true);

        if (dashboardController == null && panelDashboard != null)
            dashboardController = panelDashboard.GetComponent<Mision1DashboardController>();

        if (dashboardController != null)
            dashboardController.CalcularYMostrarResultados();

        if (hudPresupuesto != null)
            hudPresupuesto.SetActive(false);

        if (panelPaso7Final != null)
            panelPaso7Final.SetActive(false);

        if (panelMision1Completo != null)
            panelMision1Completo.SetActive(false);
    }
}