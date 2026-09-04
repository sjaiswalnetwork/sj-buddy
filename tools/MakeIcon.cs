using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;

// Generates the SJ Buddy application icon (.ico) with 16/24/32/48/256 px images. Loaded by tools\make-icon.ps1 (no exe needed).
public static class MakeIcon
{
    public static void Generate(string path)
    {
        int[] sizes = { 16, 24, 32, 48, 256 };
        var entries = new byte[sizes.Length][];
        for (int i = 0; i < sizes.Length; i++)
        {
            using (var bmp = Render(sizes[i]))
                entries[i] = sizes[i] >= 256 ? Png(bmp) : Bmp(bmp);
        }
        using (var fs = new FileStream(path, FileMode.Create))
        using (var bw = new BinaryWriter(fs))
        {
            bw.Write((short)0); bw.Write((short)1); bw.Write((short)sizes.Length);
            int offset = 6 + 16 * sizes.Length;
            for (int i = 0; i < sizes.Length; i++)
            {
                int s = sizes[i];
                bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)(s >= 256 ? 0 : s)); bw.Write((byte)0); bw.Write((byte)0);
                bw.Write((short)1); bw.Write((short)32); bw.Write(entries[i].Length); bw.Write(offset);
                offset += entries[i].Length;
            }
            for (int i = 0; i < sizes.Length; i++) bw.Write(entries[i]);
        }
    }

    static Bitmap Render(int s)
    {
        var bmp = new Bitmap(s, s, PixelFormat.Format32bppArgb);
        using (var g = Graphics.FromImage(bmp))
        {
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.Clear(Color.Transparent);
            float r = s * 0.24f;
            using (var path = RoundRect(0.5f, 0.5f, s - 1, s - 1, r))
            using (var br = new LinearGradientBrush(new PointF(0, 0), new PointF(s, s), Color.FromArgb(86, 150, 255), Color.FromArgb(28, 88, 214)))
                g.FillPath(br, path);
            float ex = s * 0.32f, ey = s * 0.40f, er = s * 0.09f;
            using (var wb = new SolidBrush(Color.White))
            {
                g.FillEllipse(wb, ex - er, ey - er, er * 2, er * 2);
                g.FillEllipse(wb, s - ex - er, ey - er, er * 2, er * 2);
            }
            using (var pen = new Pen(Color.White, Math.Max(1.5f, s * 0.07f)))
            {
                pen.StartCap = LineCap.Round; pen.EndCap = LineCap.Round;
                g.DrawArc(pen, s * 0.28f, s * 0.42f, s * 0.44f, s * 0.32f, 20, 140);
            }
            if (s >= 24)
            {
                float dx = s * 0.80f, dy = s * 0.78f, ds = s * 0.22f;
                using (var p = new GraphicsPath())
                {
                    p.AddArc(dx - ds * 0.5f, dy - ds * 0.3f, ds, ds, 160, 220);
                    p.AddLine(dx + ds * 0.44f, dy + ds * 0.04f, dx, dy - ds * 0.75f);
                    p.AddLine(dx, dy - ds * 0.75f, dx - ds * 0.44f, dy + ds * 0.04f);
                    p.CloseFigure();
                    using (var b = new SolidBrush(Color.FromArgb(120, 220, 255))) g.FillPath(b, p);
                    using (var pen = new Pen(Color.FromArgb(20, 60, 140), Math.Max(1f, s * 0.02f))) g.DrawPath(pen, p);
                }
            }
        }
        return bmp;
    }

    static GraphicsPath RoundRect(float x, float y, float w, float h, float r)
    {
        var p = new GraphicsPath(); float d = r * 2;
        p.AddArc(x, y, d, d, 180, 90); p.AddArc(x + w - d, y, d, d, 270, 90);
        p.AddArc(x + w - d, y + h - d, d, d, 0, 90); p.AddArc(x, y + h - d, d, d, 90, 90);
        p.CloseFigure(); return p;
    }

    static byte[] Png(Bitmap bmp)
    {
        using (var ms = new MemoryStream()) { bmp.Save(ms, ImageFormat.Png); return ms.ToArray(); }
    }

    static byte[] Bmp(Bitmap bmp)
    {
        int w = bmp.Width, h = bmp.Height;
        int rowBytes = ((w + 31) / 32) * 4;
        using (var ms = new MemoryStream())
        using (var bw = new BinaryWriter(ms))
        {
            bw.Write(40); bw.Write(w); bw.Write(h * 2); bw.Write((short)1); bw.Write((short)32); bw.Write(0);
            bw.Write(w * h * 4 + h * rowBytes); bw.Write(0); bw.Write(0); bw.Write(0); bw.Write(0);
            for (int y = h - 1; y >= 0; y--)
                for (int x = 0; x < w; x++) { var c = bmp.GetPixel(x, y); bw.Write(c.B); bw.Write(c.G); bw.Write(c.R); bw.Write(c.A); }
            for (int y = h - 1; y >= 0; y--)
            {
                var row = new byte[rowBytes];
                for (int x = 0; x < w; x++) if (bmp.GetPixel(x, y).A < 128) row[x / 8] |= (byte)(0x80 >> (x % 8));
                bw.Write(row);
            }
            bw.Flush();
            return ms.ToArray();
        }
    }
}
