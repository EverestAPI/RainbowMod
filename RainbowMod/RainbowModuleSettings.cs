using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using YamlDotNet.Serialization;

namespace Celeste.Mod.Rainbow {
    public class RainbowModuleSettings : EverestModuleSettings {

        public RainbowModMode Mode { get; set; } = RainbowModMode.Off;

        [SettingRange(0, 20)]
        public int RainbowSpeed { get; set; } = 10;
        [YamlIgnore]
        [SettingIgnore]
        public float RainbowSpeedFactor => RainbowSpeed / 20f;

    }
    [Flags]
    public enum RainbowModMode {
        Off = 0,
        Rainbow = 1 << 0,
        Fox = 1 << 1,
        Both = Rainbow | Fox
    }
}
