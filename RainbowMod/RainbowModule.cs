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
using On.Celeste;
using Mono.Cecil.Cil;

namespace Celeste.Mod.Rainbow {
    public class RainbowModule : EverestModule {

        public static RainbowModule Instance;

        public override Type SettingsType => typeof(RainbowModuleSettings);
        public static RainbowModuleSettings Settings => (RainbowModuleSettings) Instance._Settings;

        private static int trailIndex = 0;

        private static List<MTexture> FoxBangs;
        private static List<MTexture> FoxHair;

        private static MTexture Skateboard;
        private readonly static Vector2 SkateboardPlayerOffset = new Vector2(0, -3);

        private static MTexture Dab;
        private readonly static Vector2 DabPlayerOffset = new Vector2(0, -5);

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
            // This is a runtime mod, but we can still manipulate the game.

#if false // Set this to true if you want to demo new HookGen features.

            // Let's hook Celeste.Player.GetTrailColor
            On.Celeste.Player.GetTrailColor += (orig, player, wasDashB) => {
                Console.WriteLine("1 - Hello, World!");

                // Get the "original" color and manipulate it.
                // This step is optional - we can return anything we want.
                // We can also pass anything to the orig method.
                Color color = orig(player, wasDashB);

                // If the player is facing left, display a modified color.
                if (player.Facing == Facings.Left)
                    return new Color(0xFF, color.G, color.B, color.A);

                return color;
            };

            IL.Celeste.Player.GetTrailColor += (body, il) => {
                // Let'd dump the method before manipulating it.
                Console.WriteLine("MANIPULATING " + body.Method);
                foreach (Instruction i in body.Instructions)
                    Console.WriteLine(i);

                int index = 0;

                // Insert Console.WriteLine(...) at the beginning.
                il.Emit(ref index, OpCodes.Ldstr, "2 - Hello, IL manipulation!");
                il.Emit(ref index, OpCodes.Call, typeof(Console).GetMethod("WriteLine", new Type[] { typeof(string) }));

                // After that, emit an inline delegate call.
                il.EmitDelegateCall(ref index, () => {
                    Console.WriteLine("3 - Hello, C# code in IL!");
                });

                // Note that we can also insert any arbitrary delegate anywhere.
                // We know that the original method ends with ldloc.0, then ret
                // Let's wrap ldloc.0 with a delegate call, then invoke it before ret.
                if (il.GotoNext(ref index, (instrs, i) =>
                    instrs[i + 0].OpCode == OpCodes.Ldloc_0 &&
                    instrs[i + 1].OpCode == OpCodes.Ret
                )) {
                    // Note that we also need to update any branches pointing towards ldloc.0
                    // We only need to do this because we know that there's at least one jump to ldloc.0
                    int indexPush = index;
                    int delegateID = il.EmitDelegatePush<Func<Color, Color>>(ref index, color => {
                        Console.WriteLine("4 - Hello, arbitrary C# code in IL!");
                        color.G = 0xFF;
                        return color;
                    });
                    il.UpdateBranches(index, indexPush);

                    index += 1; // Skip ldloc.0

                    il.EmitDelegateInvoke(ref index, delegateID);
                    // The delegate returns a modified color, which gets returned by the following ret instruction.

                } else {
                    // If we know that there are multiple versions, we could chain them via else-ifs.
                    // Otherwise, do nothing (or throw an exception).
                }

                // Leave the rest of the method unmodified.
            };

#endif

            On.Celeste.PlayerHair.GetHairColor += GetHairColor;
            On.Celeste.Player.GetTrailColor += GetTrailColor;
            On.Celeste.PlayerHair.GetHairTexture += GetHairTexture;
            On.Celeste.PlayerHair.Render += RenderHair;
            On.Celeste.Player.Render += RenderPlayer;
            On.Celeste.PlayerSprite.Render += RenderPlayerSprite;
        }

        public override void LoadContent(bool firstLoad) {
            FoxBangs = GFX.Game.GetAtlasSubtextures("characters/player/foxbangs");
            FoxHair = GFX.Game.GetAtlasSubtextures("characters/player/foxhair");
            Skateboard = GFX.Game["characters/player/skateboard"];
            Dab = GFX.Game["characters/player/dab"];
        }

        public override void Unload() {
            On.Celeste.PlayerHair.GetHairColor -= GetHairColor;
            On.Celeste.Player.GetTrailColor -= GetTrailColor;
            On.Celeste.PlayerHair.GetHairTexture -= GetHairTexture;
            On.Celeste.PlayerHair.Render -= RenderHair;
            On.Celeste.Player.Render -= RenderPlayer;
            On.Celeste.PlayerSprite.Render -= RenderPlayerSprite;
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

        public static void RenderHair(On.Celeste.PlayerHair.orig_Render orig, PlayerHair self) {
            Player player = self.Entity as Player;
            if (player == null || self.GetSprite().Mode == PlayerSpriteMode.Badeline) {
                orig(self);
                return;
            }

            if (Settings.SkateboardEnabled)
                for (int i = 0; i < self.Nodes.Count; i++)
                    self.Nodes[i] = self.Nodes[i] + SkateboardPlayerOffset;
            if (Settings.DuckToDabEnabled && player.Ducking)
                for (int i = 0; i < self.Nodes.Count; i++)
                    self.Nodes[i] = self.Nodes[i] + DabPlayerOffset;

            if (Settings.WoomyEnabled) {
                PlayerSprite sprite = self.GetSprite();
                if (!sprite.HasHair)
                    return;

                const float woomyOffs = 3f;
                Vector2 woomyScaleMul = new Vector2(0.7f, 0.7f);
                Vector2 woomyScaleOffs = new Vector2(-0.2f, -0.2f);

                Vector2 origin = new Vector2(5f, 5f);
                Color colorBorder = self.Border * self.Alpha;

                RenderHairPlayerOutline(self);

                Vector2 pos;
                MTexture tex;
                Color color;
                Vector2 scale;

                self.Nodes[0] = self.Nodes[0].Floor();

                if (colorBorder.A > 0) {
                    tex = self.GetHairTexture(0);
                    scale = self.GetHairScale(0);
                    pos = self.Nodes[0];
                    tex.Draw(pos + new Vector2(-1f, 0f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 1f, 0f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2(0f, -1f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 0f, 1f), origin, colorBorder, scale);

                    tex = self.GetHairTexture(2);
                    scale = self.GetHairScale(sprite.HairCount - 2) * woomyScaleMul + woomyScaleOffs;
                    pos = self.Nodes[0];
                    tex.Draw(pos + new Vector2(-1f - woomyOffs,  0f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 1f - woomyOffs,  0f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 0f - woomyOffs, -1f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 0f - woomyOffs,  1f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2(-1f + woomyOffs,  0f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 1f + woomyOffs,  0f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 0f + woomyOffs, -1f), origin, colorBorder, scale);
                    tex.Draw(pos + new Vector2( 0f + woomyOffs,  1f), origin, colorBorder, scale);

                    for (int i = 1; i < sprite.HairCount; i++) {
                        tex = self.GetHairTexture(i);
                        scale = self.GetHairScale(sprite.HairCount - i - 1) * woomyScaleMul + woomyScaleOffs;
                        pos = self.Nodes[i];
                        tex.Draw(pos + new Vector2(-1f - woomyOffs,  0f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2( 1f - woomyOffs,  0f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2( 0f - woomyOffs, -1f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2( 0f - woomyOffs,  1f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2(-1f + woomyOffs,  0f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2( 1f + woomyOffs,  0f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2( 0f + woomyOffs, -1f), origin, colorBorder, scale);
                        tex.Draw(pos + new Vector2( 0f + woomyOffs,  1f), origin, colorBorder, scale);
                    }
                }

                tex = self.GetHairTexture(0);
                color = self.GetHairColor(0);
                scale = self.GetHairScale(0);
                tex.Draw(self.Nodes[0], origin, color, scale);

                tex = self.GetHairTexture(2);
                color = self.GetHairColor(0);
                scale = self.GetHairScale(sprite.HairCount - 2) * woomyScaleMul + woomyScaleOffs;
                tex.Draw(self.Nodes[0] + new Vector2(-woomyOffs, 0f), origin, color, scale);
                tex.Draw(self.Nodes[0] + new Vector2( woomyOffs, 0f), origin, color, scale);

                for (int i = sprite.HairCount - 1; i >= 1; i--) {
                    tex = self.GetHairTexture(i);
                    color = self.GetHairColor(i);
                    scale = self.GetHairScale(sprite.HairCount - i - 1) * woomyScaleMul + woomyScaleOffs;
                    tex.Draw(self.Nodes[i] + new Vector2(-woomyOffs, 0f), origin, color, scale);
                    tex.Draw(self.Nodes[i] + new Vector2( woomyOffs, 0f), origin, color, scale);
                }

                goto End;
            }

            orig(self);

            End:
            if (Settings.SkateboardEnabled)
                for (int i = 0; i < self.Nodes.Count; i++)
                    self.Nodes[i] = self.Nodes[i] - SkateboardPlayerOffset;
            if (Settings.DuckToDabEnabled && player.Ducking)
                for (int i = 0; i < self.Nodes.Count; i++)
                    self.Nodes[i] = self.Nodes[i] - DabPlayerOffset;
        }

        private static void RenderHairPlayerOutline(PlayerHair self) {
            PlayerSprite sprite = self.GetSprite();
            if (!self.DrawPlayerSpriteOutline)
                return;

            Vector2 origin = sprite.Position;
            Color color = sprite.Color;

            sprite.Color = self.Border * self.Alpha;

            sprite.Position = origin + new Vector2(0f, -1f);
            sprite.Render();
            sprite.Position = origin + new Vector2(0f, 1f);
            sprite.Render();
            sprite.Position = origin + new Vector2(-1f, 0f);
            sprite.Render();
            sprite.Position = origin + new Vector2(1f, 0f);
            sprite.Render();

            sprite.Color = color;
            sprite.Position = origin;
        }

        public static void RenderPlayer(On.Celeste.Player.orig_Render orig, Player self) {
            Vector2 renderPos = self.Sprite.RenderPosition;

            if (Settings.SkateboardEnabled)
                self.Sprite.RenderPosition += SkateboardPlayerOffset;
            if (Settings.DuckToDabEnabled && self.Ducking)
                self.Sprite.RenderPosition += DabPlayerOffset;

            orig(self);

            if (Settings.SkateboardEnabled) {
                Skateboard.Draw(
                    renderPos.Floor() + new Vector2(self.Facing == Facings.Left ? 9 : -8, -4),
                    Vector2.Zero, Color.White,
                    new Vector2(self.Facing == Facings.Left ? -1 : 1, 1)
                );
            }

            if (Settings.SkateboardEnabled)
                self.Sprite.RenderPosition -= SkateboardPlayerOffset;
            if (Settings.DuckToDabEnabled && self.Ducking)
                self.Sprite.RenderPosition -= DabPlayerOffset;
        }

        public static void RenderPlayerSprite(On.Celeste.PlayerSprite.orig_Render orig, PlayerSprite self) {
            Player player = self.Entity as Player;
            if (player == null || self.Mode == PlayerSpriteMode.Badeline) {
                orig(self);
                return;
            }

            if (Settings.DuckToDabEnabled && player.Ducking) {
                Dab.Draw(
                    self.RenderPosition.Floor() + new Vector2(player.Facing == Facings.Left ? 6 : -6, -7),
                    Vector2.Zero, Color.White,
                    self.Scale
                );
                return;
            }

            orig(self);
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
