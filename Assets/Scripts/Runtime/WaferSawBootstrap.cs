using UnityEngine;

namespace WaferSaw
{
    [DefaultExecutionOrder(-200)]
    public sealed class WaferSawBootstrap : MonoBehaviour
    {
        private void Awake()
        {
            if (GetComponent<MachineController>() == null) Build();
        }
        public void Build()
        {
            var controller = gameObject.AddComponent<MachineController>();
            gameObject.AddComponent<MachineView>().Build(controller);
            gameObject.AddComponent<DashboardUI>().Build(controller);
        }
    }
}
