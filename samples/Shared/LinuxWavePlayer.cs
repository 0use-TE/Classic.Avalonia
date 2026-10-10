namespace Classic.Samples.Audio;

internal class LinuxWavePlayer : IWavePlayer
{
    public void Play(string path)
    {
        var process = new System.Diagnostics.Process
        {
            StartInfo = new System.Diagnostics.ProcessStartInfo
            {
                FileName = "aplay",
                Arguments = "\"" + path + "\"",
                RedirectStandardOutput = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
    }
}
