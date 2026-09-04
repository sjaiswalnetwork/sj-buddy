using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SJBuddy
{
    public static class Characters
    {
        public static readonly string[] Ids = { "shinchan", "spongebob", "doraemon", "jethalal", "bhide" };
        public static Character Get(string id)
        {
            switch ((id ?? "").ToLowerInvariant())
            {
                case "shinchan": return new Shinchan();
                case "spongebob": return new SpongeBob();
                case "jethalal": return new Jethalal();
                case "bhide": return new Bhide();
                default: return new Doraemon();
            }
        }
        public static string DisplayName(string id) { return Get(id).Name; }
    }

    // ------------------------------------------------------------------ DORAEMON
    public class Doraemon : Character
    {
        static readonly Color Blue = Color.FromArgb(30, 144, 232), Red = Color.FromArgb(232, 50, 46), Yellow = Color.FromArgb(246, 201, 14);
        public override string Id { get { return "doraemon"; } }
        public override string Name { get { return "Doraemon"; } }
        public override Color Theme { get { return Blue; } }
        public override string[] Specials { get { return new[] { "copter", "door", "mouse", "torch" }; } }
        public override float HeadTop { get { return -264; } }
        public override float HeadHalfWidth { get { return 77; } }
        public override string Food { get { return "doracake"; } }

        public override void Draw(Graphics g, Pose p, float t)
        {
            PreProps(g, p, t);
            for (int s = -1; s <= 1; s += 2)
            {
                float swing = p.Walking ? (float)Math.Sin(p.LegPhase + (s < 0 ? 0 : Math.PI)) : 0;
                DU.Ell(g, Color.White, Ink, 3, 24 * s + swing * 10, -10 - Math.Max(0, swing) * 8, 46, 20);
            }
            DU.Ell(g, Blue, Ink, 3, 0, -78, 118, 108);
            DU.Ell(g, Color.White, Ink, 2.5f, 0, -70, 82, 76);
            DU.Arc(g, Ink, 2.5f, 0, -62, 58, 44, 0, 180); DU.Line(g, Ink, 2.5f, -29, -62, 29, -62);
            Arms(g, p, t, 52, -100, 42, Blue, 22, Color.White, 14);
            DU.Ell(g, Red, Ink, 2.5f, 0, -130, 104, 20);
            DU.Ell(g, Yellow, Ink, 2.5f, 0, -122, 22, 22); DU.Line(g, Ink, 2, -8, -124, 8, -124);
            DU.Ell(g, Ink, null, 0, 0, -118, 5, 5); DU.Line(g, Ink, 2, 0, -117, 0, -112);

            var st = g.Save(); g.TranslateTransform(0, p.HeadBob);
            DU.Ell(g, Blue, Ink, 3, 0, -190, 154, 148);
            DU.Ell(g, Color.White, Ink, 2.5f, 0, -180, 128, 114);
            EyesStd(g, p, new EyeSpec { LX = -15, LY = -224, RX = 15, RY = -224, W = 28, H = 38, White = Color.White, Pupil = Ink, PupilW = 8, PupilH = 12, Range = 7, Lid = Color.White });
            DU.Ell(g, Red, Ink, 2.5f, 0, -202, 20, 20); DU.Ell(g, DU.A(Color.White, 170), null, 0, -3, -206, 7, 6);
            DU.Line(g, Ink, 2.5f, 0, -192, 0, -158);
            for (int s = -1; s <= 1; s += 2)
            {
                DU.Line(g, Ink, 2.5f, 22 * s, -190, 62 * s, -197);
                DU.Line(g, Ink, 2.5f, 22 * s, -182, 64 * s, -182);
                DU.Line(g, Ink, 2.5f, 22 * s, -174, 62 * s, -167);
            }
            MouthStd(g, p, 0, -158, 80, Color.FromArgb(150, 30, 40), Color.FromArgb(240, 120, 140), 3);
            if (p.Blush > 0) for (int s = -1; s <= 1; s += 2) DU.Ell(g, DU.A(Color.FromArgb(250, 120, 140), (int)(130 * p.Blush)), null, 0, 44 * s, -166, 22, 12);
            HeadProp(g, p, t);
            g.Restore(st);
            Fx(g, p, t);
        }
    }

    // ------------------------------------------------------------------ SPONGEBOB
    public class SpongeBob : Character
    {
        static readonly Color Yellow = Color.FromArgb(247, 233, 76), Olive = Color.FromArgb(197, 194, 79), Brown = Color.FromArgb(139, 90, 43), Blue = Color.FromArgb(95, 182, 232), Red = Color.FromArgb(230, 58, 46), Orange = Color.FromArgb(230, 150, 60);
        public override string Id { get { return "spongebob"; } }
        public override string Name { get { return "SpongeBob"; } }
        public override Color Theme { get { return Color.FromArgb(240, 200, 40); } }
        public override string[] Specials { get { return new[] { "net", "spatula", "wand", "ready" }; } }
        public override float HeadTop { get { return -238; } }
        public override float HeadHalfWidth { get { return 72; } }
        public override string Food { get { return "patty"; } }
        public override float LaptopBaseY { get { return -30; } }
        public override float LaptopLidH { get { return 36; } }

        public override void Draw(Graphics g, Pose p, float t)
        {
            PreProps(g, p, t);
            Legs(g, p, 22, -38, 9, Yellow, Color.FromArgb(30, 30, 30), 30, 12, Color.White, true);
            DU.FillRR(g, Brown, Ink, 3, -72, -68, 144, 34, 4);
            DU.FillRR(g, Color.FromArgb(30, 30, 30), null, 0, -72, -68, 144, 8, 2);
            DU.FillRR(g, Ink, null, 0, -54, -56, 28, 12, 2); DU.FillRR(g, Ink, null, 0, 26, -56, 28, 12, 2);
            DU.FillRR(g, Color.White, Ink, 2.5f, -70, -84, 140, 18, 2);
            DU.Poly(g, Color.White, Ink, 2, -18, -84, 0, -68, 0, -84); DU.Poly(g, Color.White, Ink, 2, 18, -84, 0, -68, 0, -84);
            DU.Poly(g, Red, Ink, 2, -6, -80, 6, -80, 0, -56); DU.FillRR(g, Red, Ink, 2, -5, -86, 10, 7, 2);

            // wavy sponge body
            var pts = new List<PointF>(); float x0 = -70, x1 = 70, y0 = -236, y1 = -82; int n = 9;
            for (int i = 0; i <= n; i++) pts.Add(new PointF(x0 + (x1 - x0) * i / n, y0 + (i % 2 == 0 ? 0 : 5)));
            for (int i = 1; i <= n; i++) pts.Add(new PointF(x1 + (i % 2 == 0 ? 0 : -5), y0 + (y1 - y0) * i / n));
            for (int i = n - 1; i >= 0; i--) pts.Add(new PointF(x0 + (x1 - x0) * i / n, y1 + (i % 2 == 0 ? 0 : -5)));
            for (int i = n - 1; i >= 1; i--) pts.Add(new PointF(x0 + (i % 2 == 0 ? 0 : 5), y0 + (y1 - y0) * i / n));
            var arr = pts.ToArray();
            using (var b = new SolidBrush(Yellow)) g.FillClosedCurve(b, arr, FillMode.Winding, 0.5f);
            using (var pen = DU.P(Ink, 3)) g.DrawClosedCurve(pen, arr, 0.5f, FillMode.Winding);
            float[] holes = { -48, -210, 22, 16, 42, -218, 18, 14, -30, -166, 14, 12, 52, -160, 20, 18, -55, -122, 16, 12, 36, -104, 14, 10, 2, -94, 12, 10, -12, -222, 10, 8, 58, -192, 12, 10, -60, -196, 10, 8 };
            for (int i = 0; i < holes.Length; i += 4) DU.Ell(g, Olive, null, 0, holes[i], holes[i + 1], holes[i + 2], holes[i + 3]);

            Arms(g, p, t, 70, -150, 42, Yellow, 9, Yellow, 10);
            DU.Ell(g, Color.White, Ink, 2, -70, -150, 18, 18); DU.Ell(g, Color.White, Ink, 2, 70, -150, 18, 18);

            var st = g.Save(); g.TranslateTransform(0, p.HeadBob);
            EyesStd(g, p, new EyeSpec { LX = -26, LY = -180, RX = 26, RY = -180, W = 46, H = 46, White = Color.White, Iris = Blue, IrisR = 12, Pupil = Ink, PupilW = 12, PupilH = 12, Range = 10, Lashes = 3, Lid = Yellow });
            var st2 = g.Save(); g.TranslateTransform(4, -134); g.RotateTransform(10);
            DU.Ell(g, Yellow, Ink, 3, 0, 0, 18, 46);
            g.Restore(st2);
            for (int s = -1; s <= 1; s += 2)
            {
                DU.Ell(g, Orange, null, 0, 46 * s, -118, 4, 4); DU.Ell(g, Orange, null, 0, 40 * s, -108, 4, 4); DU.Ell(g, Orange, null, 0, 52 * s, -104, 4, 4);
                DU.Arc(g, Orange, 2.5f, 54 * s, -112, 12, 24, s < 0 ? 90 : 270, 180);
            }
            var p2 = p.Mouth == Mouth.Smile ? p.Clone() : p;
            if (p.Mouth == Mouth.Smile) p2.Mouth = Mouth.Teeth;
            MouthStd(g, p2, 0, -100, 84, Color.FromArgb(150, 30, 40), Color.FromArgb(240, 120, 140), 3);
            if (p.Blush > 0) for (int s = -1; s <= 1; s += 2) DU.Ell(g, DU.A(Color.FromArgb(250, 120, 140), (int)(130 * p.Blush)), null, 0, 50 * s, -128, 22, 12);
            HeadProp(g, p, t);
            g.Restore(st);
            Fx(g, p, t);
        }
    }

    // ------------------------------------------------------------------ SHINCHAN
    public class Shinchan : Character
    {
        static readonly Color Skin = Color.FromArgb(247, 217, 181), Pink = Color.FromArgb(245, 150, 150), Shirt = Color.FromArgb(229, 50, 45), Shorts = Color.FromArgb(247, 226, 122), Straps = Color.FromArgb(154, 216, 240), Shoe = Color.FromArgb(242, 193, 78);
        public override string Id { get { return "shinchan"; } }
        public override string Name { get { return "Shinchan"; } }
        public override Color Theme { get { return Shirt; } }
        public override string[] Specials { get { return new[] { "buriburi", "chocobi", "actionkamen", "shiro" }; } }
        public override float HeadTop { get { return -227; } }
        public override float HeadHalfWidth { get { return 66; } }
        public override string Food { get { return "chocobi"; } }
        public override float LaptopBaseY { get { return -58; } }
        public override float LaptopLidH { get { return 44; } }

        public override void Draw(Graphics g, Pose p, float t)
        {
            PreProps(g, p, t);
            Legs(g, p, 19, -18, 15, Skin, Shoe, 32, 12, Color.White, false);
            DU.FillRR(g, Shorts, Ink, 2.5f, -46, -46, 92, 34, 6); DU.Line(g, Ink, 2, 0, -46, 0, -14);
            DU.FillRR(g, Shirt, Ink, 3, -48, -106, 96, 64, 12);
            if (p.Prop == "cape") DU.Star(g, 0, -78, 13, Color.FromArgb(255, 220, 60), Ink);
            DU.FillRR(g, Straps, Ink, 2, -34, -106, 12, 44, 5); DU.FillRR(g, Straps, Ink, 2, 22, -106, 12, 44, 5);
            Arms(g, p, t, 46, -96, 40, Shirt, 16, Skin, 11);

            var st = g.Save(); g.TranslateTransform(0, p.HeadBob);
            DU.Ell(g, Skin, Ink, 2.5f, -66, -160, 14, 22); DU.Ell(g, Skin, Ink, 2.5f, 66, -160, 14, 22);
            DU.Ell(g, Skin, Ink, 3, 0, -166, 132, 122);
            var hair = new GraphicsPath();
            hair.AddArc(-66, -227, 132, 122, 205, 130);
            hair.AddCurve(new[] { new PointF(59.8f, -191.8f), new PointF(44, -199), new PointF(30, -193), new PointF(14, -201), new PointF(0, -195), new PointF(-14, -201), new PointF(-30, -193), new PointF(-44, -199), new PointF(-59.8f, -191.8f) }, 0.5f);
            hair.CloseFigure();
            DU.Path(g, hair, Ink, Ink, 2); hair.Dispose();
            // signature thick eyebrows (kept clear of the hairline)
            if (p.Brow > 0.5f) { DU.Line(g, Ink, 8, -48, -184, -12, -174); DU.Line(g, Ink, 8, 48, -184, 12, -174); }
            else if (p.Brow < -0.5f) { DU.Line(g, Ink, 8, -48, -174, -12, -184); DU.Line(g, Ink, 8, 48, -174, 12, -184); }
            else { DU.Arc(g, Ink, 8, -30, -176, 40, 16, 195, 150); DU.Arc(g, Ink, 8, 30, -176, 40, 16, 195, 150); }
            EyesStd(g, p, new EyeSpec { LX = -27, LY = -152, RX = 27, RY = -152, W = 24, H = 30, White = null, Pupil = Ink, Range = 5, Lid = Skin });
            DU.Ell(g, DU.A(Pink, 150), null, 0, -48, -137, 18, 12); DU.Ell(g, DU.A(Pink, 150), null, 0, 48, -137, 18, 12);
            var p2 = p.Mouth == Mouth.Neutral ? p.Clone() : p;
            if (p.Mouth == Mouth.Neutral) p2.Mouth = Mouth.Wavy;
            MouthStd(g, p2, 0, -122, 42, Color.FromArgb(150, 30, 40), Color.FromArgb(240, 120, 140), 2.5f);
            if (p.Blush > 0) for (int s = -1; s <= 1; s += 2) DU.Ell(g, DU.A(Color.FromArgb(250, 100, 120), (int)(120 * p.Blush)), null, 0, 48 * s, -137, 22, 14);
            HeadProp(g, p, t);
            g.Restore(st);
            Fx(g, p, t);
        }
    }

    // ------------------------------------------------------------------ JETHALAL
    public class Jethalal : Character
    {
        static readonly Color Skin = Color.FromArgb(212, 149, 106), Pants = Color.FromArgb(59, 59, 79);
        static readonly Color[] Stripes = { Color.FromArgb(232, 67, 147), Color.FromArgb(39, 174, 96), Color.FromArgb(243, 156, 18), Color.FromArgb(41, 128, 185), Color.FromArgb(142, 68, 173) };
        public override string Id { get { return "jethalal"; } }
        public override string Name { get { return "Jethalal"; } }
        public override Color Theme { get { return Color.FromArgb(243, 156, 18); } }
        public override string[] Specials { get { return new[] { "chai", "garba", "mataji", "jalebi" }; } }
        public override float HeadTop { get { return -239; } }
        public override float HeadHalfWidth { get { return 59; } }
        public override string Food { get { return "jalebi"; } }
        public override float LaptopBaseY { get { return -64; } }
        public override float LaptopLidH { get { return 46; } }

        public override void Draw(Graphics g, Pose p, float t)
        {
            PreProps(g, p, t);
            Legs(g, p, 21, -34, 26, Pants, Color.FromArgb(25, 25, 25), 34, 12, null, false);
            DU.Blob(g, Color.White, Ink, 3, 0.45f, -50, -118, 50, -118, 58, -80, 57, -42, 42, -26, -42, -26, -57, -42, -58, -80);
            DU.Line(g, Ink, 1.5f, 0, -112, 0, -30);
            for (int i = 0; i < 3; i++) DU.Ell(g, Ink, null, 0, 0, -96 + i * 20, 4, 4);
            DU.Line(g, Ink, 2.5f, -18, -118, 0, -98); DU.Line(g, Ink, 2.5f, 18, -118, 0, -98);
            for (int i = 0; i < Stripes.Length; i++) DU.Line(g, Stripes[i], 3.5f, -52 + i * 5, -114, -28 + i * 5, -90);
            DU.FillRR(g, Color.FromArgb(0, 0, 0, 0), Ink, 2, 20, -92, 26, 24, 3);
            for (int i = 0; i < 4; i++) DU.Line(g, Stripes[i], 2.5f, 22, -90 + i * 3, 44, -90 + i * 3);
            Arms(g, p, t, 54, -106, 44, Color.White, 20, Skin, 12);

            var st = g.Save(); g.TranslateTransform(0, p.HeadBob);
            DU.Ell(g, Skin, Ink, 2.5f, -60, -178, 14, 24); DU.Ell(g, Skin, Ink, 2.5f, 60, -178, 14, 24);
            DU.Ell(g, Skin, Ink, 3, 0, -176, 118, 126);
            var hair = new GraphicsPath();
            hair.AddArc(-59, -239, 118, 126, 190, 160);
            hair.AddCurve(new[] { new PointF(58.1f, -186.9f), new PointF(46, -212), new PointF(24, -224), new PointF(0, -220), new PointF(-24, -226), new PointF(-44, -216), new PointF(-58.1f, -186.9f) }, 0.5f);
            hair.CloseFigure();
            DU.Path(g, hair, Ink, Ink, 2); hair.Dispose();
            DU.FillRR(g, Ink, null, 0, -61, -192, 8, 26, 2); DU.FillRR(g, Ink, null, 0, 53, -192, 8, 26, 2);
            float bi = -204 + p.Brow * 5, bo = -204 - p.Brow * 3;
            DU.Curve(g, Ink, 5, 0.5f, -44, bo, -28, -208, -12, bi);
            DU.Curve(g, Ink, 5, 0.5f, 44, bo, 28, -208, 12, bi);
            EyesStd(g, p, new EyeSpec { LX = -24, LY = -190, RX = 24, RY = -190, W = 26, H = 18, White = Color.White, Iris = Color.FromArgb(90, 58, 26), IrisR = 6.5f, Pupil = Ink, PupilW = 7, PupilH = 7, Range = 5, Lid = Skin });
            DU.Arc(g, Ink, 2.5f, 0, -172, 20, 18, 0, 180);
            DU.Blob(g, Ink, null, 0, 0.4f, -26, -158, -12, -163, 0, -161, 12, -163, 26, -158, 22, -151, 0, -149, -22, -151);
            MouthStd(g, p, 0, -140, 52, Color.FromArgb(150, 30, 40), Color.FromArgb(240, 120, 140), 2.8f);
            if (p.Blush > 0) for (int s = -1; s <= 1; s += 2) DU.Ell(g, DU.A(Color.FromArgb(250, 100, 120), (int)(110 * p.Blush)), null, 0, 40 * s, -162, 22, 12);
            HeadProp(g, p, t);
            g.Restore(st);
            Fx(g, p, t);
        }
    }

    // ------------------------------------------------------------------ BHIDE
    public class Bhide : Character
    {
        static readonly Color Skin = Color.FromArgb(229, 185, 142), Kurta = Color.FromArgb(183, 146, 78), KurtaDark = Color.FromArgb(143, 110, 51), Pants = Color.FromArgb(91, 70, 54), Shoe = Color.FromArgb(43, 29, 18);
        public override string Id { get { return "bhide"; } }
        public override string Name { get { return "Bhide"; } }
        public override Color Theme { get { return Kurta; } }
        public override string[] Specials { get { return new[] { "scooter", "chalk", "register", "jar" }; } }
        public override float HeadTop { get { return -239; } }
        public override float HeadHalfWidth { get { return 54; } }
        public override string Food { get { return "modak"; } }
        public override float LaptopBaseY { get { return -64; } }
        public override float LaptopLidH { get { return 46; } }

        public override void Draw(Graphics g, Pose p, float t)
        {
            PreProps(g, p, t);
            Legs(g, p, 20, -32, 24, Pants, Shoe, 32, 12, null, false);
            DU.FillRR(g, Kurta, Ink, 3, -52, -120, 104, 92, 10);
            for (int x = -40; x <= 40; x += 12) DU.Line(g, DU.A(KurtaDark, 70), 1.5f, x, -110, x, -36);
            DU.Line(g, Ink, 1.5f, 0, -118, 0, -70);
            for (int i = 0; i < 3; i++) DU.Ell(g, Ink, null, 0, 0, -108 + i * 12, 4, 4);
            DU.FillRR(g, KurtaDark, Ink, 2, -14, -126, 28, 10, 3);
            Arms(g, p, t, 50, -110, 44, Kurta, 18, Skin, 11);

            var st = g.Save(); g.TranslateTransform(0, p.HeadBob);
            DU.Ell(g, Skin, Ink, 2.5f, -55, -180, 13, 22); DU.Ell(g, Skin, Ink, 2.5f, 55, -180, 13, 22);
            DU.Ell(g, Skin, Ink, 3, 0, -178, 108, 122);
            var hair = new GraphicsPath();
            hair.AddArc(-54, -239, 108, 122, 185, 170);
            hair.AddCurve(new[] { new PointF(53.8f, -183.3f), new PointF(48, -206), new PointF(40, -222), new PointF(24, -214), new PointF(0, -218), new PointF(-24, -214), new PointF(-40, -222), new PointF(-48, -206), new PointF(-53.8f, -183.3f) }, 0.5f);
            hair.CloseFigure();
            DU.Path(g, hair, Ink, Ink, 2); hair.Dispose();
            DU.Line(g, Ink, 4, -38, -207 - p.Brow * 3, -14, -207 + p.Brow * 5);
            DU.Line(g, Ink, 4, 38, -207 - p.Brow * 3, 14, -207 + p.Brow * 5);
            EyesStd(g, p, new EyeSpec { LX = -22, LY = -188, RX = 22, RY = -188, W = 20, H = 13, White = Color.White, Iris = Color.FromArgb(62, 42, 20), IrisR = 5, Pupil = Ink, PupilW = 5.5f, PupilH = 5.5f, Range = 3.5f, Lid = Skin, Outline = 2 });
            DU.FillRR(g, DU.A(Color.White, 45), Ink, 3.5f, -38, -199, 32, 22, 5);
            DU.FillRR(g, DU.A(Color.White, 45), Ink, 3.5f, 6, -199, 32, 22, 5);
            DU.Line(g, Ink, 3, -6, -190, 6, -190);
            DU.Line(g, Ink, 3, -38, -192, -54, -186); DU.Line(g, Ink, 3, 38, -192, 54, -186);
            DU.Line(g, DU.A(Color.White, 150), 2, -33, -182, -27, -195); DU.Line(g, DU.A(Color.White, 150), 2, 11, -182, 17, -195);
            DU.Arc(g, Ink, 2.5f, 0, -172, 16, 14, 0, 180);
            MouthStd(g, p, 0, -152, 44, Color.FromArgb(150, 30, 40), Color.FromArgb(240, 120, 140), 2.5f);
            if (p.Blush > 0) for (int s = -1; s <= 1; s += 2) DU.Ell(g, DU.A(Color.FromArgb(250, 100, 120), (int)(110 * p.Blush)), null, 0, 38 * s, -166, 20, 11);
            HeadProp(g, p, t);
            g.Restore(st);
            Fx(g, p, t);
        }
    }
}
