using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class SidebarTabManager : MonoBehaviour
{
    [System.Serializable]
    public class TabSidebar
    {
        public string nombre;
        public Button boton;
        public Image fondoResaltado;        // Fondo suave del botón
        public GameObject barraIndicadora;  // Barrita vertical verde a la izquierda
        public Transform contenedorIcono;   // El icono (para escalarlo un poco al seleccionarlo)
    }

    [Header("Lista de Pestañas")]
    public List<TabSidebar> pestanas = new List<TabSidebar>();

    [Header("Color de Fondo Activo")]
    public Color colorFondoActivo = new Color(0.18f, 0.80f, 0.44f, 0.15f); // Verde tenue
    public Color colorFondoInactivo = new Color(1f, 1f, 1f, 0f);           // Totalmente transparente

    private void Start()
    {
        for (int i = 0; i < pestanas.Count; i++)
        {
            int index = i;
            if (pestanas[index].boton != null)
            {
                pestanas[index].boton.onClick.AddListener(() => ActivarPestana(index));
            }
        }

        // Deja la primera pestaña (Inicio) seleccionada al arrancar
        if (pestanas.Count > 0)
        {
            ActivarPestana(0);
        }
    }

    public void ActivarPestana(int indiceActivo)
    {
        for (int i = 0; i < pestanas.Count; i++)
        {
            bool seleccionada = (i == indiceActivo);
            var tab = pestanas[i];

            // 1. Enciende o apaga la barra verde lateral
            if (tab.barraIndicadora != null)
            {
                tab.barraIndicadora.SetActive(seleccionada);
            }

            // 2. Cambia el fondo del botón
            if (tab.fondoResaltado != null)
            {
                tab.fondoResaltado.color = seleccionada ? colorFondoActivo : colorFondoInactivo;
            }

            // 3. Aumenta sutilmente el tamaño del icono (feedback visual agradable)
            if (tab.contenedorIcono != null)
            {
                tab.contenedorIcono.localScale = seleccionada ? Vector3.one * 1.12f : Vector3.one;
            }
        }
    }
}