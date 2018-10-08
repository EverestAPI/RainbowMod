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
        public bool RainbowEnabled { get; set; }

        [SettingRange(0, 20)]
        public int RainbowSpeed { get; set; } = 10;
        [YamlIgnore]
        [SettingIgnore]
        public float RainbowSpeedFactor => RainbowSpeed / 20f;

        public bool FoxEnabled { get; set; }

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
                    Logger.Log(LogLevel.Warn, "rainbowmod", "Invalid FoxColorLight!");
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
                return FoxColorDark.R.ToString("X2") + FoxColorDark.G.ToString("X2") + FoxColorDark.B.ToString("X2");
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

        public bool WoomyEnabled { get; set; }

        public bool SkateboardEnabled { get; set; }

        public bool DuckToDabEnabled { get; set; }

        public bool DuckToSneezeEnabled { get; set; }

        public bool BaldelineEnabled { get; set; }
    }
}
