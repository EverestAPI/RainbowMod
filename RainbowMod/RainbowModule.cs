using Celeste.Mod;
using FMOD.Studio;
using Microsoft.Xna.Framework;
using Monocle;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace Celeste.Mod.Rainbow {
    public class RainbowModule : EverestModule {

        public static RainbowModule Instance;

        public override Type SettingsType => typeof(RainbowModuleSettings);
        public static RainbowModuleSettings Settings => (RainbowModuleSettings) Instance._Settings;

        private static int trailIndex = 0;

        private static List<MTexture> FoxBangs;
        private static List<MTexture> FoxHair;

        public RainbowModule() {
            Instance = this;
        }

        public override void LoadSettings() {
            base.LoadSettings();

            bool updated = false;

            if (Settings.FoxColorLight.A == 0) {
                Settings.FoxColorLight = new Color(0.8f, 0.5f, 0.05f, 1f);
                updated = true;
            }
            if (Settings.FoxColorDark.A == 0) {
                Settings.FoxColorDark = new Color(0.1f, 0.05f, 0f, 1f);
                updated = true;
            }

            if (updated) {
                SaveSettings();
            }
        }

        public override void Load() {
            On.Celeste.PlayerHair.GetHairColor += GetHairColor;
            On.Celeste.Player.GetTrailColor += GetTrailColor;
            On.Celeste.PlayerHair.GetHairTexture += GetHairTexture;
        }

        public override void LoadContent(bool firstLoad) {
            FoxBangs = GFX.Game.GetAtlasSubtextures("characters/player/foxbangs");
            FoxHair = GFX.Game.GetAtlasSubtextures("characters/player/foxhair");
        }

        public override void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= GetHairColor;
            On.Celeste.Player.GetTrailColor -= GetTrailColor;
            On.Celeste.PlayerHair.GetHairTexture -= GetHairTexture;
        }

        public override void CreateModMenuSection(TextMenu menu, bool inGame, EventInstance snapshot) {
            base.CreateModMenuSection(menu, inGame, snapshot);

            if (inGame) {
                menu.Add(new TextMenu.Button(Dialog.Clean("modoptions_rainbowmodule_reloadcolors")).Pressed(() => {
                    // Temporarily store current settings, load new settings and replace colors.
                    RainbowModuleSettings settings = Settings;
                    LoadSettings();
                    settings.FoxColorLight = Settings.FoxColorLight;
                    settings.FoxColorDark = Settings.FoxColorDark;
                    _Settings = settings;
                }));
            }
        }

        public static Color GetHairColor(On.Celeste.PlayerHair.orig_GetHairColor orig, PlayerHair self, int index) {
            Color colorOrig = orig(self, index);
            if (!(self.Entity is Player) || self.GetSprite().Mode == PlayerSpriteMode.Badeline)
                return colorOrig;

            Color color = colorOrig;

            if (Settings.FoxEnabled) {
                Color colorFox;
                if (index % 2 == 0) {
                    colorFox = Settings.FoxColorLight;
                    color = new Color(
                        (color.R / 255f) * 0.1f + (colorFox.R / 255f) * 0.9f,
                        (color.G / 255f) * 0.05f + (colorFox.G / 255f) * 0.95f,
                        (color.B / 255f) * 0.2f + (colorFox.B / 255f) * 0.8f,
                        color.A
                    );
                } else {
                    colorFox = Settings.FoxColorDark;
                    color = new Color(
                        (color.R / 255f) * 0.1f + (colorFox.R / 255f) * 0.7f,
                        (color.G / 255f) * 0.1f + (colorFox.G / 255f) * 0.7f,
                        (color.B / 255f) * 0.1f + (colorFox.B / 255f) * 0.7f,
                        color.A
                    );
                }
            }

            if (Settings.RainbowEnabled) {
                float wave = self.GetWave() * 60f;
                wave *= Settings.RainbowSpeedFactor;
                Color colorRainbow = ColorFromHSV((index / (float) self.GetSprite().HairCount) * 180f + wave, 0.6f, 1.0f);
                color = new Color(
                    (color.R / 255f) * 0.3f + (colorRainbow.R / 255f) * 0.7f,
                    (color.G / 255f) * 0.3f + (colorRainbow.G / 255f) * 0.7f,
                    (color.B / 255f) * 0.3f + (colorRainbow.B / 255f) * 0.7f,
                    color.A
                );
            }

            color.A = colorOrig.A;
            return color;
        }

        public static Color GetTrailColor(On.Celeste.Player.orig_GetTrailColor orig, Player self, bool wasDashB) {
            if (!Settings.RainbowEnabled || self.Sprite.Mode == PlayerSpriteMode.Badeline || self.Hair == null)
                return orig(self, wasDashB);

            return self.Hair.GetHairColor((trailIndex++) % self.Hair.GetSprite().HairCount);
        }

        public static MTexture GetHairTexture(On.Celeste.PlayerHair.orig_GetHairTexture orig, PlayerHair self, int index) {
            if (!(self.Entity is Player) || self.GetSprite().Mode == PlayerSpriteMode.Badeline)
                return orig(self, index);

            if (Settings.FoxEnabled) {
                if (index == 0)
                    return FoxBangs[self.GetSprite().HairFrame];
                return FoxHair[index % FoxHair.Count];
            }

            return orig(self, index);
        }

        // Conversion algorithms found randomly on the net - best source for HSV <-> RGB ever:tm:

        private static void ColorToHSV(Color c, out float h, out float s, out float v) {
            float r = c.R / 255f;
            float g = c.G / 255f;
            float b = c.B / 255f;
            float min, max, delta;
            min = Math.Min(Math.Min(r, g), b);
            max = Math.Max(Math.Max(r, g), b);
            v = max;
            delta = max - min;
            if (max != 0) {
                s = delta / max;

                if (r == max)
                    h = (g - b) / delta;
                else if (g == max)
                    h = 2 + (b - r) / delta;
                else
                    h = 4 + (r - g) / delta;
                h *= 60f;
                if (h < 0)
                    h += 360f;
            } else {
                s = 0f;
                h = 0f;
            }
        }

        private static Color ColorFromHSV(float hue, float saturation, float value) {
            int hi = (int) (Math.Floor(hue / 60f)) % 6;
            float f = hue / 60f - (float) Math.Floor(hue / 60f);

            value = value * 255;
            int v = (int) Math.Round(value);
            int p = (int) Math.Round(value * (1 - saturation));
            int q = (int) Math.Round(value * (1 - f * saturation));
            int t = (int) Math.Round(value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return new Color(v, t, p, 255);
            else if (hi == 1)
                return new Color(q, v, p, 255);
            else if (hi == 2)
                return new Color(p, v, t, 255);
            else if (hi == 3)
                return new Color(p, q, v, 255);
            else if (hi == 4)
                return new Color(t, p, v, 255);
            else
                return new Color(v, p, q, 255);
        }

    }
}
