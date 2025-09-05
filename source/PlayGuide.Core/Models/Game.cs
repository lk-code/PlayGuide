namespace PlayGuide.Core.Models;

public class Game
{
    public string Name { get; set; } = string.Empty;
    public string ExecutablePath { get; set; } = string.Empty;
    public string Publisher { get; set; } = string.Empty;
    public string Version { get; set; } = string.Empty;
    public string IconPath { get; set; } = string.Empty;
    public string InstallLocation { get; set; } = string.Empty;
}
