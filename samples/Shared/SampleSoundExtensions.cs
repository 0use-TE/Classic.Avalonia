using System;
using System.Collections.Generic;
using System.IO;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Platform;
using Classic.CommonControls.Dialogs;
using Classic.Samples.Audio;

namespace Classic.Samples;

internal static class SampleSoundExtensions
{
    public static AppBuilder UseMessageBoxSounds(this AppBuilder builder)
    {
        var sounds = new Dictionary<string, string>();
        var assembly = typeof(SampleSoundExtensions).Assembly.GetName().Name;
        builder.AfterSetup(_ => Control.LoadedEvent.AddClassHandler<MessageBox>((message, _) =>
        {
            var name = message.Icon switch
            {
                MessageBoxIcon.Error or MessageBoxIcon.Warning => "chord",
                MessageBoxIcon.Information or MessageBoxIcon.Question => "ding",
                _ => null
            };
            if (name == null) return;
            if (!sounds.TryGetValue(name, out var path))
            {
                using var source = AssetLoader.Open(new Uri("avares://" + assembly + "/Audio/" + name + ".wav"));
                path = Path.GetTempFileName();
                using (var target = File.Create(path)) source.CopyTo(target);
                sounds[name] = path;
            }
            WavePlayer.Player.Play(path);
        }));
        return builder;
    }
}
