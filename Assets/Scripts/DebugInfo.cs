using System;
using TMPro;
using UnityEngine;
using UnityTools;

namespace DefaultNamespace
{
    public class DebugInfo : MonoBehaviour
    {
        AutoGet<TextMeshProUGUI> text;

        public DebugInfo()
        {
            text = new (this);
        }

        private void Update()
        {
            text.value.text = $"" +
                              $"mouse pos: {(Vector2) Input.mousePosition}\n" +
                              $"screen res: {Screen.width},{Screen.height}\n" +
                              $"pointer relative:{((Vector2) Input.mousePosition) / new Vector2(Screen.width, Screen.height)}";
        }
    }
}