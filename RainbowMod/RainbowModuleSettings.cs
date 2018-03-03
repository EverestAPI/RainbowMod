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

        [YamlMember(Alias = "FoxColorLight")]
        [SettingIgnore]
        public string FoxColorLightHex {
            get {
                return FoxColorLight.R.ToString("X2") + FoxColorLight.G.ToString("X2") + FoxColorLight.B.ToString("X2");
            }
            set {
                if (string.IsNullOrEmpty(value))
                    return;
                try {
                    FoxColorLight = Calc.HexToColor(value);
                } catch (Exception e) {
                    Logger.Log(LogLevel.Warn, "rainbowmod", "Invalid FoxColorDark!");
                    e.LogDetailed();
                }
            }
        }
        [YamlIgnore]
        [SettingIgnore]
        public Color FoxColorLight { get; set; }

        [YamlMember(Alias = "FoxColorDark")]
        [SettingIgnore]
        public string FoxColorDarkHex {
            get {
                return (0x00FFFFFF & FoxColorDark.PackedValue).ToString("X6");
            }
            set {
                if (string.IsNullOrEmpty(value))
                    return;
                try {
                    FoxColorDark = Calc.HexToColor(value);
                } catch (Exception e) {
                    Logger.Log(LogLevel.Warn, "rainbowmod", "Invalid FoxColorDark!");
                    e.LogDetailed();
                }
            }
        }
        [YamlIgnore]
        [SettingIgnore]
        public Color FoxColorDark { get; set; }

    }
    [Flags]
    public enum RainbowModMode {
        Off = 0,
        Rainbow = 1 << 0,
        Fox = 1 << 1,
        Both = Rainbow | Fox
    }
}
