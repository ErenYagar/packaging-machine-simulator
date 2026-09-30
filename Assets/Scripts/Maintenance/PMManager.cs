using System.Linq;

namespace WaferSaw
{
    public sealed class PMManager
    {
        public static readonly string[] Items = { "Inspect Blade", "Clean Wafer Chuck", "Check Cooling Water", "Check Vacuum Line", "Check Spindle", "Verify Alignment" };
        private readonly bool[] checks = new bool[6];
        public bool Completed => checks.All(value => value);
        public int Count => checks.Count(value => value);
        public bool IsChecked(int index) => checks[index];
        public void Set(int index, bool value) { checks[index] = value; }
    }
}
