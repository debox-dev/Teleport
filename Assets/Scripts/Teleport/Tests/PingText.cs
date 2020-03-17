using UnityEngine;
using UnityEngine.UI;
using DeBox.Teleport.Unity;


namespace DeBox.Teleport.Tests
{
    [RequireComponent(typeof(Text))]
    public class PingText : MonoBehaviour
    {
        [SerializeField]
        private BaseTeleportManager _teleportManager = null;

        private Text _text;

        private void Start()
        {
            _text = GetComponent<Text>();
        }

        private void Update()
        {
            _text.text = "Ping: " + _teleportManager.ClientPing.ToString();
        }
    }
}
