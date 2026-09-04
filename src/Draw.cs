using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;

namespace SJBuddy
{
    public enum Eyes { Open, Closed, Happy, Wide, Sleepy, Hearts, Stars, Dizzy, Angry, Wink, Squint, Sad, Focus }
    public enum Mouth { Smile, Neutral, Open, Laugh, O, Sad, Grin, Tongue, Wavy, Flat, Yawn, Teeth }

    // Everything that describes how the character looks in one frame.
    public class Pose
    {
        public float DX, DY, Tilt;
        public float SX = 1f, SY = 1f;
        public int Facing = 1;
        public float ArmL = 10f, ArmR = 10f;     // 0 = hanging down, 90 = straight out, 180 = straight up
        public Eyes Eyes = Eyes.Open;
        public Mouth Mouth = Mouth.Smile;
        public float PupilX, PupilY;            // -1..1
        public string Prop;                      // laptop, water, cup, phone, food items, copter, ...
        public string Fx;                        // zzz, notes, hearts, sweat, bulb, question, exclaim, anger, stars, sparkle, bubbles
        public float Blush;
        public float LegPhase;
        public bool Walking;
        public float HeadBob;
        public float Brow;                       // -1 sad .. 0 .. 1 angry
        public Pose Clone() { return (Pose)MemberwiseClone(); }
    }

    // Small GDI+ drawing helpers (cartoon style: thick rounded outlines).
    public static class DU
    {
        public static readonly Color Ink = Color.FromArgb(38, 34, 36);

        public static Color A(Color c, int a) { return Color.FromArgb(Math.Max(0, Math.Min(255, a)), c.R, c.G, c.B); }
        public static Color Rgb(int r, int g, int b) { return Color.FromArgb(r, g, b); }

        public static Pen P(Color c, float w)
        {
            var p = new Pen(c, w); p.LineJoin = LineJoin.Round; p.StartCap = LineCap.Round; p.EndCap = LineCap.Round; return p;
        }

        public static void Ell(Graphics g, Color fill, Color? line, float lw, float cx, float cy, float w, float h)
        {
            var r = new RectangleF(cx - w / 2, cy - h / 2, w, h);
            if (fill.A > 0) using (var b = new SolidBrush(fill)) g.FillEllipse(b, r);
            if (line.HasValue && lw > 0) using (var p = P(line.Value, lw)) g.DrawEllipse(p, r);
        }

        public static void Line(Graphics g, Color c, float w, float x1, float y1, float x2, float y2)
        {
            using (var p = P(c, w)) g.DrawLine(p, x1, y1, x2, y2);
        }

        public static void Arc(Graphics g, Color c, float w, float cx, float cy, float rw, float rh, float start, float sweep)
        {
            using (var p = P(c, w)) g.DrawArc(p, cx - rw / 2, cy - rh / 2, rw, rh, start, sweep);
        }

        public static GraphicsPath RR(float x, float y, float w, float h, float r)
        {
            var p = new GraphicsPath();
            float d = r * 2; if (d > w) d = w; if (d > h) d = h; if (d <= 0) d = 0.1f;
            p.AddArc(x, y, d, d, 180, 90); p.AddArc(x + w - d, y, d, d, 270, 90);
            p.AddArc(x + w - d, y + h - d, d, d, 0, 90); p.AddArc(x, y + h - d, d, d, 90, 90);
            p.CloseFigure();
            return p;
        }

        public static void FillRR(Graphics g, Color fill, Color? line, float lw, float x, float y, float w, float h, float r)
        {
            if (w <= 0 || h <= 0) return;
            using (var p = RR(x, y, w, h, r)) Path(g, p, fill, line, lw);
        }

        public static void Path(Graphics g, GraphicsPath p, Color fill, Color? line, float lw)
        {
            if (fill.A > 0) using (var b = new SolidBrush(fill)) g.FillPath(b, p);
            if (line.HasValue && lw > 0) using (var pen = P(line.Value, lw)) g.DrawPath(pen, p);
        }

        public static void Poly(Graphics g, Color fill, Color? line, float lw, params float[] xy)
        {
            var pts = new PointF[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            if (fill.A > 0) using (var b = new SolidBrush(fill)) g.FillPolygon(b, pts);
            if (line.HasValue && lw > 0) using (var p = P(line.Value, lw)) g.DrawPolygon(p, pts);
        }

        // Smooth closed blob through the given points.
        public static void Blob(Graphics g, Color fill, Color? line, float lw, float tension, params float[] xy)
        {
            var pts = new PointF[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            if (fill.A > 0) using (var b = new SolidBrush(fill)) g.FillClosedCurve(b, pts, FillMode.Winding, tension);
            if (line.HasValue && lw > 0) using (var p = P(line.Value, lw)) g.DrawClosedCurve(p, pts, tension, FillMode.Winding);
        }

        public static void Curve(Graphics g, Color c, float w, float tension, params float[] xy)
        {
            var pts = new PointF[xy.Length / 2];
            for (int i = 0; i < pts.Length; i++) pts[i] = new PointF(xy[i * 2], xy[i * 2 + 1]);
            using (var p = P(c, w)) g.DrawCurve(p, pts, tension);
        }

        public static void Heart(Graphics g, float cx, float cy, float s, Color c)
        {
            using (var b = new SolidBrush(c))
            {
                g.FillEllipse(b, cx - s * 0.5f, cy - s * 0.42f, s * 0.55f, s * 0.55f);
                g.FillEllipse(b, cx - s * 0.05f, cy - s * 0.42f, s * 0.55f, s * 0.55f);
                g.FillPolygon(b, new[] { new PointF(cx - s * 0.49f, cy - s * 0.08f), new PointF(cx + s * 0.49f, cy - s * 0.08f), new PointF(cx, cy + s * 0.5f) });
            }
        }

        public static void Star(Graphics g, float cx, float cy, float r, Color c, Color? line)
        {
            var pts = new PointF[10];
            for (int i = 0; i < 10; i++)
            {
                double a = (-90 + i * 36) * Math.PI / 180; float rr = i % 2 == 0 ? r : r * 0.45f;
                pts[i] = new PointF(cx + (float)Math.Cos(a) * rr, cy + (float)Math.Sin(a) * rr);
            }
            using (var b = new SolidBrush(c)) g.FillPolygon(b, pts);
            if (line.HasValue) using (var p = P(line.Value, 1.5f)) g.DrawPolygon(p, pts);
        }

        public static void Sparkle(Graphics g, float cx, float cy, float r, Color c)
        {
            var pts = new PointF[8];
            for (int i = 0; i < 8; i++)
            {
                double a = (-90 + i * 45) * Math.PI / 180; float rr = i % 2 == 0 ? r : r * 0.28f;
                pts[i] = new PointF(cx + (float)Math.Cos(a) * rr, cy + (float)Math.Sin(a) * rr);
            }
            using (var b = new SolidBrush(c)) g.FillPolygon(b, pts);
        }

        public static void Drop(Graphics g, float cx, float cy, float s, Color fill, Color? line)
        {
            var p = new GraphicsPath();
            p.AddArc(cx - s * 0.5f, cy - s * 0.3f, s, s, 160, 220);
            p.AddLine(cx + s * 0.44f, cy + s * 0.04f, cx, cy - s * 0.75f);
            p.AddLine(cx, cy - s * 0.75f, cx - s * 0.44f, cy + s * 0.04f);
            p.CloseFigure();
            Path(g, p, fill, line, 2f);
            p.Dispose();
        }

        public static Font MakeFont(string family, float px, bool bold)
        {
            try { return new Font(family, px, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel); }
            catch { return new Font(FontFamily.GenericSansSerif, px, bold ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel); }
        }

        // Centered text that stays readable even when the character is mirrored (facing left).
        public static void Text(Graphics g, string s, float cx, float cy, float px, Color c, bool bold, string family, int facing)
        {
            var st = g.Save();
            g.TranslateTransform(cx, cy);
            if (facing < 0) g.ScaleTransform(-1, 1);
            using (var f = MakeFont(family, px, bold))
            using (var b = new SolidBrush(c))
            {
                var sz = g.MeasureString(s, f);
                g.DrawString(s, f, b, -sz.Width / 2, -sz.Height / 2);
            }
            g.Restore(st);
        }

        public static float Clamp(float v, float lo, float hi) { return v < lo ? lo : (v > hi ? hi : v); }
    }

    public class EyeSpec
    {
        public float LX, LY, RX, RY, W, H;
        public Color? White = Color.White;
        public Color? Iris = null; public float IrisR = 0;
        public Color Pupil = DU.Ink; public float PupilW = 8, PupilH = 8;
        public float Range = 6;
        public int Lashes = 0;
        public Color Lid = Color.White;
        public float Outline = 2.5f;
        public bool Highlight = true;
    }

    public abstract class Character
    {
        public abstract string Id { get; }
        public abstract string Name { get; }
        public abstract Color Theme { get; }
        public abstract string[] Specials { get; }
        public abstract float HeadTop { get; }
        public virtual float HeadHalfWidth { get { return 66; } }
        public virtual string Food { get { return "doracake"; } }
        public virtual float LaptopBaseY { get { return -66; } }
        public virtual float LaptopLidH { get { return 48; } }
        public abstract void Draw(Graphics g, Pose p, float t);
        public PointF HandR, HandL;
        protected static readonly Color Ink = DU.Ink;

        // ---------- arms ----------
        protected PointF ArmTo(Graphics g, float sx, float sy, float tx, float ty, Color col, float width, Color hand, float handR)
        {
            DU.Line(g, Ink, width + 4.5f, sx, sy, tx, ty);
            DU.Line(g, col, width, sx, sy, tx, ty);
            if (handR > 0) DU.Ell(g, hand, Ink, 2.5f, tx, ty, handR * 2, handR * 2);
            return new PointF(tx, ty);
        }

        protected PointF Arm(Graphics g, float sx, float sy, float len, float angle, int side, Color col, float width, Color hand, float handR)
        {
            double a = angle * Math.PI / 180.0;
            float hx = sx + (float)(Math.Sin(a) * len * side);
            float hy = sy + (float)(Math.Cos(a) * len);
            return ArmTo(g, sx, sy, hx, hy, col, width, hand, handR);
        }

        // Draws both arms (and the laptop / hand prop when relevant).
        protected void Arms(Graphics g, Pose p, float t, float shx, float shy, float len, Color col, float width, Color hand, float handR)
        {
            if (p.Prop == "laptop")
            {
                Laptop(g, p, t);
                float bob = (float)Math.Sin(t * 14) * 3;
                HandL = ArmTo(g, -shx, shy, -22, LaptopBaseY + 3 + bob, col, width, hand, handR);
                HandR = ArmTo(g, shx, shy, 22, LaptopBaseY + 3 - bob, col, width, hand, handR);
                return;
            }
            HandL = Arm(g, -shx, shy, len, p.ArmL, -1, col, width, hand, handR);
            HandR = Arm(g, shx, shy, len, p.ArmR, 1, col, width, hand, handR);
            HandProp(g, p, t, HandR);
        }

        // ---------- legs ----------
        protected void Legs(Graphics g, Pose p, float x, float top, float w, Color col, Color shoe, float shoeW, float shoeH, Color? sock, bool stripes)
        {
            for (int s = -1; s <= 1; s += 2)
            {
                float swing = p.Walking ? (float)Math.Sin(p.LegPhase + (s < 0 ? 0 : Math.PI)) : 0;
                float lx = x * s + swing * 9;
                float lift = p.Walking ? Math.Max(0, swing) * 8 : 0;
                float bottom = -6 - lift;
                DU.FillRR(g, col, Ink, 2.5f, lx - w / 2, top, w, bottom - top + 3, w * 0.3f);
                if (sock.HasValue)
                {
                    DU.FillRR(g, sock.Value, Ink, 1.5f, lx - w / 2 - 2, bottom - 12, w + 4, 11, 2);
                    if (stripes)
                    {
                        DU.Line(g, Color.FromArgb(230, 58, 46), 2, lx - w / 2 - 2, bottom - 9, lx + w / 2 + 2, bottom - 9);
                        DU.Line(g, Color.FromArgb(70, 130, 230), 2, lx - w / 2 - 2, bottom - 5, lx + w / 2 + 2, bottom - 5);
                    }
                }
                DU.Ell(g, shoe, Ink, 2.5f, lx + s * 2, bottom + 1, shoeW, shoeH);
            }
        }

        // ---------- eyes ----------
        protected void EyesStd(Graphics g, Pose p, EyeSpec e)
        {
            Eyes st = p.Eyes;
            Eyes l = st == Eyes.Wink ? Eyes.Open : st;
            Eyes r = st == Eyes.Wink ? Eyes.Happy : st;
            OneEye(g, p, e, e.LX, e.LY, true, l);
            OneEye(g, p, e, e.RX, e.RY, false, r);
        }

        void OneEye(Graphics g, Pose p, EyeSpec e, float cx, float cy, bool left, Eyes st)
        {
            float w = e.W, h = e.H;
            switch (st)
            {
                case Eyes.Closed:
                    DU.Arc(g, Ink, 3f, cx, cy - h * 0.12f, w * 0.9f, h * 0.6f, 15, 150);
                    return;
                case Eyes.Happy:
                    DU.Arc(g, Ink, 3.5f, cx, cy + h * 0.2f, w * 0.9f, h * 0.75f, 195, 150);
                    return;
                case Eyes.Hearts:
                    DU.Heart(g, cx, cy, w * 1.0f, Color.FromArgb(235, 64, 110)); return;
                case Eyes.Stars:
                    DU.Star(g, cx, cy, w * 0.6f, Color.FromArgb(255, 200, 40), Ink); return;
                case Eyes.Dizzy:
                    for (int k = 0; k < 3; k++) DU.Arc(g, Ink, 2.5f, cx, cy, w * (0.25f + k * 0.24f), h * (0.25f + k * 0.24f), k * 120, 300);
                    return;
            }
            bool wide = st == Eyes.Wide; bool squint = st == Eyes.Squint;
            if (wide) { w *= 1.15f; h *= 1.2f; }
            if (squint) { h *= 0.45f; }
            float ox = DU.Clamp(p.PupilX, -1, 1) * e.Range, oy = DU.Clamp(p.PupilY, -1, 1) * e.Range * 0.7f;
            if (st == Eyes.Focus) oy += e.Range * 0.35f;
            if (e.White.HasValue)
            {
                DU.Ell(g, e.White.Value, Ink, e.Outline, cx, cy, w, h);
                float pw = e.PupilW, ph = e.PupilH; if (wide) { pw *= 0.75f; ph *= 0.75f; }
                if (e.Iris.HasValue) { float ir = e.IrisR * (wide ? 0.9f : 1f); DU.Ell(g, e.Iris.Value, null, 0, cx + ox, cy + oy, ir * 2, ir * 2); }
                DU.Ell(g, e.Pupil, null, 0, cx + ox, cy + oy, pw, ph);
                if (e.Highlight) { float hr = Math.Max(2.5f, pw * 0.4f); DU.Ell(g, Color.White, null, 0, cx + ox - pw * 0.25f, cy + oy - ph * 0.25f, hr, hr); }
            }
            else
            {
                DU.Ell(g, e.Pupil, null, 0, cx + ox * 0.3f, cy + oy * 0.3f, w, h);
                if (e.Highlight) DU.Ell(g, Color.White, null, 0, cx + ox * 0.3f - w * 0.2f, cy + oy * 0.3f - h * 0.25f, w * 0.32f, h * 0.26f);
            }
            if (st == Eyes.Sleepy)
            {
                DU.Ell(g, e.Lid, null, 0, cx, cy - h * 0.62f, w * 1.35f, h * 1.24f);
                DU.Arc(g, Ink, 2.5f, cx, cy - h * 0.62f, w * 1.35f, h * 1.24f, 25, 130);
            }
            for (int i = 0; i < e.Lashes; i++)
            {
                double a = (-90 + (i - (e.Lashes - 1) / 2.0) * 30) * Math.PI / 180;
                float x1 = cx + (float)Math.Cos(a) * w / 2, y1 = cy + (float)Math.Sin(a) * h / 2;
                DU.Line(g, Ink, 3, x1, y1, x1 + (float)Math.Cos(a) * 11, y1 + (float)Math.Sin(a) * 11);
            }
            float side = left ? -1 : 1;
            if (st == Eyes.Angry || p.Brow > 0.5f)
                DU.Line(g, Ink, 3.5f, cx + side * w * 0.6f, cy - h * 0.95f, cx - side * w * 0.45f, cy - h * 0.55f);
            else if (st == Eyes.Sad || p.Brow < -0.5f)
                DU.Line(g, Ink, 3.5f, cx + side * w * 0.6f, cy - h * 0.6f, cx - side * w * 0.45f, cy - h * 0.95f);
        }

        // ---------- mouth ----------
        protected void MouthStd(Graphics g, Pose p, float cx, float cy, float w, Color inside, Color tongue, float lw)
        {
            switch (p.Mouth)
            {
                case Mouth.Smile: DU.Arc(g, Ink, lw, cx, cy - w * 0.18f, w, w * 0.5f, 20, 140); break;
                case Mouth.Neutral: DU.Line(g, Ink, lw, cx - w * 0.2f, cy, cx + w * 0.2f, cy); break;
                case Mouth.Flat: DU.Line(g, Ink, lw, cx - w * 0.35f, cy, cx + w * 0.35f, cy); break;
                case Mouth.Sad: DU.Arc(g, Ink, lw, cx, cy + w * 0.2f, w * 0.7f, w * 0.4f, 200, 140); break;
                case Mouth.O: DU.Ell(g, inside, Ink, lw, cx, cy + 2, w * 0.22f, w * 0.26f); break;
                case Mouth.Open:
                    DU.Ell(g, inside, Ink, lw, cx, cy + 4, w * 0.45f, w * 0.5f);
                    DU.Ell(g, tongue, null, 0, cx, cy + 4 + w * 0.13f, w * 0.26f, w * 0.16f);
                    break;
                case Mouth.Yawn:
                    DU.Ell(g, inside, Ink, lw, cx, cy + 8, w * 0.42f, w * 0.7f);
                    DU.Ell(g, tongue, null, 0, cx, cy + 8 + w * 0.2f, w * 0.24f, w * 0.16f);
                    break;
                case Mouth.Laugh:
                    {
                        var path = new GraphicsPath();
                        path.AddArc(cx - w / 2, cy - w * 0.42f, w, w * 0.85f, 0, 180);
                        path.CloseFigure();
                        using (var b = new SolidBrush(inside)) g.FillPath(b, path);
                        var oldClip = g.Clip;
                        g.SetClip(path, CombineMode.Intersect);
                        using (var b = new SolidBrush(Color.White)) g.FillRectangle(b, cx - w / 2, cy - 2, w, w * 0.12f);
                        DU.Ell(g, tongue, null, 0, cx, cy + w * 0.3f, w * 0.45f, w * 0.26f);
                        g.Clip = oldClip;
                        using (var pen = DU.P(Ink, lw)) g.DrawPath(pen, path);
                        path.Dispose();
                        break;
                    }
                case Mouth.Grin:
                    {
                        var path = new GraphicsPath();
                        path.AddArc(cx - w / 2, cy - w * 0.28f, w, w * 0.56f, 0, 180);
                        path.CloseFigure();
                        DU.Path(g, path, Color.White, Ink, lw);
                        DU.Line(g, Ink, 1.5f, cx - w * 0.4f, cy + w * 0.12f, cx + w * 0.4f, cy + w * 0.12f);
                        path.Dispose();
                        break;
                    }
                case Mouth.Teeth:
                    DU.Arc(g, Ink, lw, cx, cy - w * 0.18f, w, w * 0.5f, 20, 140);
                    DU.FillRR(g, Color.White, Ink, 2, cx - 13, cy + w * 0.04f, 12, 15, 2);
                    DU.FillRR(g, Color.White, Ink, 2, cx + 1, cy + w * 0.04f, 12, 15, 2);
                    break;
                case Mouth.Tongue:
                    DU.Arc(g, Ink, lw, cx, cy - w * 0.18f, w, w * 0.5f, 20, 140);
                    DU.Ell(g, tongue, Ink, 2, cx, cy + w * 0.12f, w * 0.24f, w * 0.22f);
                    break;
                case Mouth.Wavy:
                    DU.Curve(g, Ink, lw, 0.6f, cx - w * 0.35f, cy, cx - w * 0.12f, cy - w * 0.09f, cx + w * 0.12f, cy + w * 0.09f, cx + w * 0.35f, cy);
                    break;
            }
        }

        // ---------- effects around the head ----------
        protected void Fx(Graphics g, Pose p, float t)
        {
            if (string.IsNullOrEmpty(p.Fx)) return;
            float hx = 34, hy = HeadTop - 6; int f = p.Facing;
            switch (p.Fx)
            {
                case "zzz":
                    for (int i = 0; i < 3; i++)
                    {
                        float ph = (t * 0.45f + i * 0.33f) % 1f;
                        float x = hx + 10 + ph * 34 + i * 8, y = hy - ph * 46 - 4; float sz = 13 + i * 4 + ph * 6;
                        DU.Text(g, "Z", x, y, sz, DU.A(Ink, (int)(255 * (1 - ph))), true, "Segoe UI", f);
                    }
                    break;
                case "notes":
                    for (int i = 0; i < 3; i++)
                    {
                        float ph = (t * 0.5f + i * 0.33f) % 1f;
                        float x = hx + 8 + (float)Math.Sin((ph + i) * 6.28) * 10 + i * 12, y = hy - ph * 50;
                        DU.Text(g, i % 2 == 0 ? "♪" : "♫", x, y, 24, DU.A(Color.FromArgb(60, 60, 200), (int)(255 * (1 - ph))), true, "Segoe UI Symbol", f);
                    }
                    break;
                case "hearts":
                    for (int i = 0; i < 4; i++)
                    {
                        float ph = (t * 0.55f + i * 0.25f) % 1f;
                        float x = -30 + i * 22 + (float)Math.Sin((ph * 4 + i) * 3.14) * 8, y = hy - ph * 55;
                        DU.Heart(g, x, y, 12 + i * 2, DU.A(Color.FromArgb(240, 70, 120), (int)(255 * (1 - ph))));
                    }
                    break;
                case "sweat":
                    { float ph = (t * 0.8f) % 1f; DU.Drop(g, HeadHalfWidth - 4, HeadTop + 40 + ph * 22, 13, Color.FromArgb(120, 190, 250), Ink); }
                    break;
                case "bulb":
                    {
                        float bl = 0.6f + 0.4f * (float)Math.Abs(Math.Sin(t * 5)); var yel = Color.FromArgb(255, 220, 60);
                        DU.Ell(g, DU.A(yel, (int)(255 * bl)), Ink, 2.5f, 0, hy - 34, 30, 30);
                        DU.FillRR(g, Color.Gray, Ink, 2, -7, hy - 20, 14, 10, 2);
                        for (int i = 0; i < 5; i++)
                        {
                            double a = (-150 + i * 30) * Math.PI / 180;
                            DU.Line(g, DU.A(yel, (int)(255 * bl)), 3, (float)Math.Cos(a) * 22, hy - 34 + (float)Math.Sin(a) * 22, (float)Math.Cos(a) * 30, hy - 34 + (float)Math.Sin(a) * 30);
                        }
                    }
                    break;
                case "question": DU.Text(g, "?", hx + 6, hy - 24 + (float)Math.Sin(t * 4) * 4, 36, Color.FromArgb(70, 90, 200), true, "Segoe UI", f); break;
                case "exclaim": DU.Text(g, "!", hx + 2, hy - 24 - (float)Math.Abs(Math.Sin(t * 8)) * 6, 40, Color.FromArgb(230, 60, 50), true, "Segoe UI", f); break;
                case "anger":
                    {
                        float ax = HeadHalfWidth - 6, ay = HeadTop + 26; var red = Color.FromArgb(220, 40, 40);
                        for (int i = 0; i < 4; i++)
                        {
                            double a = (45 + i * 90) * Math.PI / 180;
                            DU.Arc(g, red, 3, ax + (float)Math.Cos(a) * 8, ay + (float)Math.Sin(a) * 8, 12, 12, (float)(i * 90 + 100), 140);
                        }
                    }
                    break;
                case "stars":
                    for (int i = 0; i < 3; i++) { double a = t * 4 + i * 2.094; DU.Star(g, (float)Math.Cos(a) * 34, hy - 8 + (float)Math.Sin(a) * 10, 9, Color.FromArgb(255, 205, 40), Ink); }
                    break;
                case "sparkle":
                    for (int i = 0; i < 5; i++) { float ph = (float)Math.Abs(Math.Sin(t * 3 + i * 1.3)); DU.Sparkle(g, -60 + i * 30, hy - 10 - (i % 2) * 22, 5 + ph * 7, DU.A(Color.FromArgb(255, 230, 90), (int)(120 + 135 * ph))); }
                    break;
                case "bubbles":
                    for (int i = 0; i < 5; i++)
                    {
                        float ph = (t * 0.35f + i * 0.2f) % 1f;
                        float x = 40 + i * 14 + (float)Math.Sin(ph * 6 + i) * 8, y = HeadTop + 90 - ph * 130; float r = 6 + i * 2;
                        DU.Ell(g, DU.A(Color.FromArgb(160, 210, 255), (int)(140 * (1 - ph))), DU.A(Color.FromArgb(90, 160, 240), (int)(220 * (1 - ph))), 1.5f, x, y, r * 2, r * 2);
                    }
                    break;
            }
        }

        // ---------- props ----------
        protected void Laptop(Graphics g, Pose p, float t)
        {
            float by = LaptopBaseY, lh = LaptopLidH;
            DU.FillRR(g, Color.FromArgb(52, 56, 66), Ink, 2.5f, -40, by - lh - 4, 80, lh + 4, 4);
            DU.FillRR(g, Color.FromArgb(70, 74, 86), Ink, 2.5f, -46, by - 4, 92, 9, 2);
            DU.Line(g, DU.A(Color.FromArgb(120, 200, 255), 90), 2, -32, by + 1, 32, by + 1);
        }

        // Ground-level props drawn behind the body.
        protected void PreProps(Graphics g, Pose p, float t)
        {
            switch (p.Prop)
            {
                case "scooter": Scooter(g, t); break;
                case "shiro": Shiro(g, t); break;
                case "mouse": Mouse(g, t); break;
                case "door": Door(g, t); break;
                case "chalk": Board(g, p, t); break;
                case "net": Jellyfish(g, t); break;
                case "cape": Cape(g, t); break;
            }
        }

        void Scooter(Graphics g, float t)
        {
            var blue = Color.FromArgb(120, 180, 230); var dark = Color.FromArgb(60, 60, 70);
            for (int i = 0; i < 3; i++) { float ph = (t * 0.7f + i * 0.33f) % 1f; DU.Ell(g, DU.A(Color.Gray, (int)(120 * (1 - ph))), null, 0, -84 - ph * 30, -20 - ph * 10 - i * 4, 10 + ph * 10, 8 + ph * 8); }
            DU.Ell(g, dark, Ink, 3, -48, -13, 30, 30); DU.Ell(g, Color.LightGray, null, 0, -48, -13, 10, 10);
            DU.Ell(g, dark, Ink, 3, 54, -13, 30, 30); DU.Ell(g, Color.LightGray, null, 0, 54, -13, 10, 10);
            DU.Blob(g, blue, Ink, 3, 0.4f, -70, -30, -60, -58, -20, -64, 8, -44, 8, -24, -30, -22);
            DU.FillRR(g, Color.FromArgb(70, 50, 40), Ink, 2.5f, -62, -66, 52, 12, 6);
            DU.FillRR(g, blue, Ink, 3, -10, -30, 54, 10, 3);
            DU.Blob(g, blue, Ink, 3, 0.3f, 40, -26, 44, -60, 52, -84, 62, -84, 60, -58, 56, -26);
            DU.Line(g, dark, 5, 50, -86, 30, -96); DU.Line(g, dark, 5, 50, -86, 70, -92);
            DU.Ell(g, Color.FromArgb(255, 240, 180), Ink, 2, 62, -74, 12, 12);
        }

        void Shiro(Graphics g, float t)
        {
            float x = -96, y = -4; var w = Color.White;
            DU.Line(g, Ink, 4, x - 18, y - 24, x - 30 + (float)Math.Sin(t * 10) * 6, y - 40);
            DU.Ell(g, w, Ink, 2.5f, x, y - 20, 44, 34);
            DU.Ell(g, w, Ink, 2.5f, x - 10, y - 8, 12, 10); DU.Ell(g, w, Ink, 2.5f, x + 10, y - 8, 12, 10);
            DU.Ell(g, w, Ink, 2.5f, x + 8, y - 44, 34, 30);
            DU.Ell(g, w, Ink, 2.5f, x - 6, y - 56, 12, 16); DU.Ell(g, w, Ink, 2.5f, x + 22, y - 56, 12, 16);
            DU.Ell(g, Ink, null, 0, x + 2, y - 46, 5, 6); DU.Ell(g, Ink, null, 0, x + 14, y - 46, 5, 6);
            DU.Ell(g, Ink, null, 0, x + 8, y - 38, 7, 5);
        }

        void Mouse(Graphics g, float t)
        {
            float x = -140 + ((t * 160) % 280); float y = -8;
            var grey = Color.FromArgb(150, 150, 160);
            DU.Line(g, Color.FromArgb(200, 120, 140), 2.5f, x - 12, y, x - 34, y - 6 + (float)Math.Sin(t * 20) * 4);
            DU.Ell(g, grey, Ink, 2, x, y - 2, 26, 16);
            DU.Ell(g, grey, Ink, 2, x + 14, y - 4, 14, 12);
            DU.Ell(g, Color.Pink, Ink, 1.5f, x + 12, y - 12, 8, 8); DU.Ell(g, Color.Pink, Ink, 1.5f, x + 20, y - 12, 8, 8);
            DU.Ell(g, Ink, null, 0, x + 18, y - 5, 3, 3); DU.Ell(g, Ink, null, 0, x + 22, y - 2, 3, 3);
        }

        void Door(Graphics g, float t)
        {
            var pink = Color.FromArgb(240, 140, 170); var frame = Color.FromArgb(200, 90, 120);
            float x = 86, y = -80;
            DU.FillRR(g, frame, Ink, 3, x - 36, y - 80, 72, 160, 4);
            DU.FillRR(g, pink, Ink, 2.5f, x - 28, y - 72, 56, 150, 3);
            DU.FillRR(g, DU.A(Color.White, 60), null, 0, x - 20, y - 64, 40, 56, 2);
            DU.FillRR(g, DU.A(Color.White, 60), null, 0, x - 20, y + 2, 40, 56, 2);
            DU.Ell(g, Color.Gold, Ink, 2, x + 18, y + 2, 9, 9);
        }

        void Board(Graphics g, Pose p, float t)
        {
            float x = 112, y = -186; var wood = Color.FromArgb(120, 80, 40);
            DU.Line(g, wood, 5, x - 40, y + 44, x - 46, 0); DU.Line(g, wood, 5, x + 40, y + 44, x + 46, 0);
            DU.FillRR(g, wood, Ink, 3, x - 64, y - 44, 128, 88, 4);
            DU.FillRR(g, Color.FromArgb(40, 90, 60), null, 0, x - 58, y - 38, 116, 76, 2);
            DU.Text(g, "2 + 2 = 4", x, y - 14, 20, Color.White, true, "Comic Sans MS", p.Facing);
            DU.Text(g, "a² + b² = c²", x, y + 14, 15, Color.FromArgb(255, 240, 180), false, "Segoe UI", p.Facing);
        }

        void Jellyfish(Graphics g, float t)
        {
            float jx = 100 + (float)Math.Sin(t * 1.2) * 18, jy = -200 + (float)Math.Sin(t * 1.7) * 14;
            var pink = Color.FromArgb(250, 160, 220);
            for (int i = 0; i < 4; i++) { float tx = jx - 15 + i * 10; DU.Line(g, DU.A(pink, 200), 3, tx, jy + 8, tx + (float)Math.Sin(t * 5 + i) * 6, jy + 36); }
            var path = new GraphicsPath(); path.AddArc(jx - 22, jy - 16, 44, 40, 180, 180); path.CloseFigure();
            DU.Path(g, path, pink, Ink, 2.5f); path.Dispose();
            DU.Ell(g, DU.A(Color.White, 120), null, 0, jx - 6, jy - 6, 10, 6);
        }

        void Cape(Graphics g, float t)
        {
            var red = Color.FromArgb(220, 40, 50); float fl = (float)Math.Sin(t * 6) * 8;
            DU.Blob(g, red, Ink, 3, 0.3f, -40, -100, 40, -100, 62 + fl, -40, 50, -16, -50, -16, -62 - fl, -40);
        }

        protected void HeadProp(Graphics g, Pose p, float t)
        {
            if (p.Prop == "copter")
            {
                float top = HeadTop;
                DU.Line(g, Color.FromArgb(250, 210, 60), 5, 0, top + 2, 0, top - 18);
                float w = 70 * Math.Max(0.12f, Math.Abs((float)Math.Cos(t * 22)));
                DU.Ell(g, Color.FromArgb(250, 210, 60), Ink, 2.5f, 0, top - 20, w, 8);
            }
        }

        protected void HandProp(Graphics g, Pose p, float t, PointF h)
        {
            float x = h.X, y = h.Y;
            switch (p.Prop)
            {
                case "water":
                    {
                        var st = g.Save(); g.TranslateTransform(x, y); g.RotateTransform(-8 + (float)Math.Sin(t * 3) * 3);
                        DU.FillRR(g, DU.A(Color.FromArgb(200, 230, 255), 150), Ink, 2.5f, -12, -32, 24, 36, 5);
                        float lvl = 0.62f + 0.05f * (float)Math.Sin(t * 2);
                        DU.FillRR(g, DU.A(Color.FromArgb(70, 160, 240), 210), null, 0, -10, -32 + 36 * (1 - lvl), 20, 36 * lvl - 2, 4);
                        DU.Line(g, Color.FromArgb(240, 80, 80), 3, 4, -30, 12, -48);
                        DU.Line(g, DU.A(Color.White, 170), 2, -7, -26, -7, -8);
                        g.Restore(st); break;
                    }
                case "cup":
                    DU.FillRR(g, Color.White, Ink, 2.5f, x - 11, y - 22, 22, 22, 4);
                    DU.FillRR(g, Color.FromArgb(150, 90, 40), null, 0, x - 9, y - 20, 18, 6, 2);
                    DU.Arc(g, Ink, 2.5f, x + 13, y - 12, 12, 12, 270, 180);
                    for (int i = 0; i < 3; i++) { float ph = (t * 0.6f + i * 0.33f) % 1f; DU.Line(g, DU.A(Color.Gray, (int)(160 * (1 - ph))), 2, x - 6 + i * 6, y - 24 - ph * 10, x - 6 + i * 6 + (float)Math.Sin(ph * 6) * 4, y - 30 - ph * 18); }
                    break;
                case "phone":
                    DU.FillRR(g, Color.FromArgb(40, 40, 48), Ink, 2, x - 10, y - 18, 20, 34, 4);
                    DU.FillRR(g, Color.FromArgb(120, 190, 255), null, 0, x - 8, y - 15, 16, 26, 2);
                    break;
                case "chocobi":
                    DU.FillRR(g, Color.FromArgb(240, 150, 60), Ink, 2.5f, x - 13, y - 30, 26, 32, 4);
                    DU.Star(g, x, y - 14, 9, Color.FromArgb(255, 235, 120), Ink);
                    for (int i = 0; i < 3; i++) { float ph = (t * 0.6f + i * 0.33f) % 1f; DU.Star(g, x - 10 + i * 10, y - 36 - ph * 20, 4, DU.A(Color.FromArgb(120, 70, 30), (int)(255 * (1 - ph))), null); }
                    break;
                case "patty":
                    DU.Ell(g, Color.FromArgb(235, 180, 90), Ink, 2.5f, x, y - 24, 40, 18);
                    DU.Blob(g, Color.FromArgb(90, 190, 80), null, 0, 0.6f, x - 22, y - 18, x - 12, y - 12, x - 2, y - 18, x + 8, y - 12, x + 18, y - 18, x + 22, y - 14, x - 22, y - 14);
                    DU.FillRR(g, Color.FromArgb(120, 70, 40), Ink, 2, x - 20, y - 14, 40, 8, 3);
                    DU.FillRR(g, Color.FromArgb(235, 180, 90), Ink, 2.5f, x - 20, y - 7, 40, 9, 4);
                    DU.Ell(g, Color.FromArgb(255, 240, 200), null, 0, x - 8, y - 27, 4, 3); DU.Ell(g, Color.FromArgb(255, 240, 200), null, 0, x + 4, y - 25, 4, 3);
                    break;
                case "doracake":
                    DU.Ell(g, Color.FromArgb(200, 130, 60), Ink, 2.5f, x, y - 6, 38, 12);
                    DU.FillRR(g, Color.FromArgb(90, 50, 30), null, 0, x - 15, y - 14, 30, 8, 3);
                    DU.Ell(g, Color.FromArgb(215, 145, 70), Ink, 2.5f, x, y - 18, 38, 12);
                    break;
                case "jalebi":
                    for (int k = 0; k < 3; k++) DU.Arc(g, Color.FromArgb(240, 140, 30), 5, x, y - 14, 10 + k * 9, 8 + k * 8, k * 70, 300);
                    DU.Line(g, Color.FromArgb(245, 200, 90), 4, x + 14, y - 4, x + 30, y - 24); DU.Line(g, Color.FromArgb(245, 200, 90), 4, x + 18, y - 2, x + 34, y - 20);
                    break;
                case "modak":
                    DU.Blob(g, Color.FromArgb(255, 245, 215), Ink, 2.5f, 0.3f, x - 16, y, x + 16, y, x + 10, y - 16, x, y - 30, x - 10, y - 16);
                    for (int i = -1; i <= 1; i++) DU.Line(g, DU.A(Ink, 120), 1.5f, x + i * 7, y - 4, x, y - 26);
                    break;
                case "net":
                    DU.Line(g, Color.FromArgb(120, 80, 40), 4, x, y, x + 40, y - 60);
                    DU.Ell(g, DU.A(Color.FromArgb(200, 230, 255), 70), Ink, 2.5f, x + 52, y - 76, 40, 30);
                    for (int i = -2; i <= 2; i++) DU.Line(g, DU.A(Ink, 80), 1, x + 52 + i * 7, y - 90, x + 52 + i * 7, y - 62);
                    break;
                case "spatula":
                    {
                        DU.Line(g, Color.FromArgb(70, 70, 80), 4, x, y, x + 22, y - 30);
                        DU.FillRR(g, Color.FromArgb(180, 180, 190), Ink, 2, x + 14, y - 46, 22, 18, 3);
                        float ph = (t * 1.2f) % 1f; float py = y - 50 - (float)Math.Sin(ph * 3.1416) * 60;
                        var st = g.Save(); g.TranslateTransform(x + 25, py); g.RotateTransform(ph * 360);
                        DU.FillRR(g, Color.FromArgb(120, 70, 40), Ink, 2, -14, -4, 28, 8, 3); g.Restore(st);
                        break;
                    }
                case "wand":
                    DU.Line(g, Color.FromArgb(240, 120, 160), 3.5f, x, y, x + 16, y - 26);
                    DU.Ell(g, DU.A(Color.FromArgb(200, 240, 255), 60), Color.FromArgb(240, 120, 160), 3, x + 20, y - 32, 14, 14);
                    break;
                case "chalk":
                    DU.Line(g, Color.FromArgb(120, 80, 40), 4, x, y, x + 40, y - 40);
                    break;
                case "register":
                    DU.FillRR(g, Color.FromArgb(200, 60, 60), Ink, 2.5f, x - 16, y - 22, 32, 26, 3);
                    DU.FillRR(g, Color.White, null, 0, x - 12, y - 18, 24, 18, 2);
                    for (int i = 0; i < 3; i++) DU.Line(g, DU.A(Ink, 120), 1.5f, x - 9, y - 13 + i * 5, x + 9, y - 13 + i * 5);
                    break;
                case "jar":
                    DU.FillRR(g, DU.A(Color.FromArgb(230, 240, 255), 160), Ink, 2.5f, x - 13, y - 26, 26, 30, 5);
                    DU.FillRR(g, Color.FromArgb(230, 120, 40), null, 0, x - 11, y - 14, 22, 16, 3);
                    DU.FillRR(g, Color.FromArgb(200, 60, 60), Ink, 2, x - 14, y - 31, 28, 7, 2);
                    break;
                case "torch":
                    DU.FillRR(g, Color.FromArgb(240, 200, 60), Ink, 2.5f, x - 6, y - 30, 12, 30, 3);
                    DU.FillRR(g, Color.FromArgb(80, 80, 90), Ink, 2.5f, x - 9, y - 36, 18, 8, 2);
                    for (int i = -1; i <= 1; i++) DU.Line(g, DU.A(Color.FromArgb(255, 240, 120), (int)(120 + 100 * Math.Abs(Math.Sin(t * 6)))), 3, x + i * 6, y - 38, x + i * 14, y - 56);
                    break;
                case "mic":
                    DU.Line(g, Color.FromArgb(60, 60, 70), 5, x, y, x + 10, y - 24);
                    DU.Ell(g, Color.FromArgb(90, 90, 100), Ink, 2.5f, x + 13, y - 31, 18, 18);
                    break;
                case "dumbbell":
                    DU.Line(g, Color.FromArgb(90, 90, 100), 5, x - 16, y, x + 16, y);
                    DU.FillRR(g, Color.FromArgb(50, 50, 60), Ink, 2, x - 24, y - 8, 10, 16, 2); DU.FillRR(g, Color.FromArgb(50, 50, 60), Ink, 2, x + 14, y - 8, 10, 16, 2);
                    break;
                case "book":
                    DU.FillRR(g, Color.FromArgb(60, 110, 200), Ink, 2.5f, x - 18, y - 24, 36, 26, 3);
                    DU.FillRR(g, Color.White, null, 0, x - 14, y - 20, 32, 18, 2);
                    break;
            }
        }
    }
}
