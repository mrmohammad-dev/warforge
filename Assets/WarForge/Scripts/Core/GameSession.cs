namespace WarForge.Core
{
    /// <summary>Runtime state that survives scene reloads (restart / return to menu).</summary>
    public static class GameSession
    {
        public static string SelectedVehicleId = "wf_warden";
        public static string SelectedMissionId = "training_range";
        public static bool StartMissionImmediately;
    }
}
