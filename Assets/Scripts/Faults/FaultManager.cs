using UnityEngine;

namespace WaferSaw
{
    public sealed class FaultManager
    {
        public FaultKind Active { get; private set; }
        public float Severity { get; private set; }
        public bool Repaired { get; private set; }
        public void Inject(FaultKind kind) { Active = kind; Severity = 0; Repaired = false; }
        public void Tick(float dt)
        {
            if (Active != FaultKind.None && !Repaired) Severity = Mathf.Min(1, Severity + dt / 18);
        }
        public void Repair() { Repaired = true; Severity = 0; }
        public void Reset() { Active = FaultKind.None; Severity = 0; Repaired = false; }
    }
}
