using Celeste.Mod;
using FMOD.Studio;
using HookedMethod;
using HM = HookedMethod.HookedMethod;
using Microsoft.Xna.Framework;
using Monocle;
using MonoMod.Detour;
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

        private static Hook h_GetHairColor;
        private static Hook h_GetTrailColor;
        private static Hook h_GetHairTexture;

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
            // Runtime hooks are quite different from static patches.
            h_GetHairColor = new Hook(
                MethodInfoWithDef.CreateAndResolveDef(typeof(PlayerHair).GetMethod("GetHairColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)),
                GetHairColor
            );

            h_GetTrailColor = new Hook(
                MethodInfoWithDef.CreateAndResolveDef(typeof(Player).GetMethod("GetTrailColor", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic)),
                GetTrailColor
            );

            MethodInfo m_GetHairTexture = typeof(PlayerHair).GetMethod("GetHairTexture", BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
            if (m_GetHairTexture != null) {
                h_GetHairTexture = new Hook(
                    MethodInfoWithDef.CreateAndResolveDef(m_GetHairTexture),
                    GetHairTexture
                );
            }
        }

        public override void LoadContent() {
            FoxBangs = GFX.Game.GetAtlasSubtextures("characters/player/foxbangs");
            FoxHair = GFX.Game.GetAtlasSubtextures("characters/player/foxhair");
        }

        public override void Unload() {
            // TODO: Undoing hooks in HookedMethod?
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

        public static object GetHairColor(HM hook, HM.OriginalMethod origM, HM.Parameters args) {
            // C# 7:
            var (self, index) = args.As<PlayerHair, int>();
            // C# 6:
            // PlayerHair self = (PlayerHair) args.RawParams[0];
            // int index = (int) args.RawParams[1];

            Color colorOrig = origM.As<Color>(args.RawParams);
            if (Settings.Mode == RainbowModMode.Off || self.GetSprite().Mode == PlayerSpriteMode.Badeline)
                return colorOrig;

            Color color = colorOrig;

            if ((Settings.Mode & RainbowModMode.Fox) == RainbowModMode.Fox) {
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

            if ((Settings.Mode & RainbowModMode.Rainbow) == RainbowModMode.Rainbow) {
                float wave = self.GetWave() * 60f;
                wave *= Settings.RainbowSpeedFactor;
                Color colorRainbow = ColorFromHSV((index / (float) self.GetSprite().HairCount) * 180f + wave, 0.6f, 0.6f);
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

        public static object GetTrailColor(HM hook, HM.OriginalMethod origM, HM.Parameters args) {
            // C# 7:
            var (self, wasDashB) = args.As<Player, bool>();
            // C# 6:
            // Player self = (Player) args.RawParams[0];
            // bool wasDashB = (bool) args.RawParams[1];

            if ((Settings.Mode & RainbowModMode.Rainbow) != RainbowModMode.Rainbow || self.Sprite.Mode == PlayerSpriteMode.Badeline || self.Hair == null)
                return origM.As<Color>(args.RawParams);

            return self.Hair.GetHairColor((trailIndex++) % self.Hair.GetSprite().HairCount);
        }

        public static MTexture GetHairTexture(HM hook, HM.OriginalMethod origM, HM.Parameters args) {
            // C# 7:
            var (self, index) = args.As<PlayerHair, int>();
            // C# 6:
            // PlayerHair self = (PlayerHair) args.RawParams[0];
            // int index = (int) args.RawParams[1];

            MTexture orig = origM.As<MTexture>(args.RawParams);
            if ((Settings.Mode & RainbowModMode.Fox) != RainbowModMode.Fox || self.GetSprite().Mode == PlayerSpriteMode.Badeline)
                return orig;

            if (index == 0)
                return FoxBangs[self.GetSprite().HairFrame];

            return FoxHair[index % FoxHair.Count];
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
            int v = (int) (value);
            int p = (int) (value * (1 - saturation));
            int q = (int) (value * (1 - f * saturation));
            int t = (int) (value * (1 - (1 - f) * saturation));

            if (hi == 0)
                return new Color(255, v, t, p);
            else if (hi == 1)
                return new Color(255, q, v, p);
            else if (hi == 2)
                return new Color(255, p, v, t);
            else if (hi == 3)
                return new Color(255, p, q, v);
            else if (hi == 4)
                return new Color(255, t, p, v);
            else
                return new Color(255, v, p, q);
        }

    }
}
