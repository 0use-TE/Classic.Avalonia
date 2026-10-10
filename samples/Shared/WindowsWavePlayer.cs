using System.Runtime.InteropServices;

namespace Classic.Samples.Audio;

internal class WindowsWavePlayer : IWavePlayer
{
    [DllImport("winmm.dll", EntryPoint = "PlaySoundW", CharSet = CharSet.Unicode, SetLastError = true)]
    private static extern bool PlaySound(string pszSound, IntPtr hmod, uint fdwSound);

    const uint SND_SYNC = 0x0000;
    const uint SND_ASYNC = 0x0001;
    const uint SND_FILENAME = 0x20000;

    public void Play(string path)
    {
        if (!PlaySound(path, IntPtr.Zero, SND_ASYNC | SND_FILENAME))
        {
            Console.WriteLine("Failed to play sound on Windows.");
        }
    }
}
