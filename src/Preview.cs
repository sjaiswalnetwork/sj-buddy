using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.Drawing.Text;

namespace SJBuddy
{
    public static class Preview
    {
        // Draws one character with its pose transform at (cx, groundY) scaled by 'scale'.
        public static void DrawCharacter(Graphics g, Character c, Pose p, float t, float scale, float cx, float groundY, bool shadow)
        {
            var st = g.Save();
            g.SmoothingMode = SmoothingMode.AntiAlias;
            g.TextRenderingHint = TextRenderingHint.AntiAlias;
            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
            g.InterpolationMode = InterpolationMode.HighQualityBicubic;
            g.TranslateTransform(cx, groundY);
            g.ScaleTransform(scale, scale);
            if (shadow)
            {
                float lift = Math.Min(1f, Math.Max(0f, -p.DY) / 120f);
                float sh = 1 - lift * 0.5f;
                DU.Ell(g, DU.A(Color.Black, (int)(55 * sh)), null, 0, p.DX, 3, 130 * sh, 18 * sh);
            }
            g.TranslateTransform(p.DX, p.DY);
            g.RotateTransform(p.Tilt);
            g.ScaleTransform(p.SX * p.Facing, p.SY);
            c.Draw(g, p, t);
            g.Restore(st);
        }

        // Debug/marketing sheet: every character in a set of poses.
        public static void RenderSheet(string path)
        {
            var rnd = new Random(7);
            var names = new List<string>();
            var poses = new List<Func<Character, Pose>>();
            names.Add("idle"); poses.Add(c => new Pose());
            names.Add("typing"); poses.Add(c => { var p = new Pose(); p.Prop = "laptop"; p.Eyes = Eyes.Focus; p.Mouth = Mouth.Neutral; p.PupilX = -0.2f; p.PupilY = 0.6f; return p; });
            names.Add("sleep"); poses.Add(c => { var p = new Pose(); p.Eyes = Eyes.Closed; p.Mouth = Mouth.Neutral; p.Fx = "zzz"; p.Tilt = -6; p.ArmL = p.ArmR = 5; return p; });
            names.Add("happy"); poses.Add(c => { var p = new Pose(); p.Eyes = Eyes.Happy; p.Mouth = Mouth.Laugh; p.Fx = "hearts"; p.ArmL = p.ArmR = 140; p.Blush = 0.8f; return p; });
            names.Add("water"); poses.Add(c => { var p = new Pose(); p.Prop = "water"; p.ArmR = 100; p.Eyes = Eyes.Wide; p.Mouth = Mouth.O; p.Fx = "exclaim"; return p; });
            names.Add("dance"); poses.Add(c => Acts.Get("dance", c, rnd).Step(new Pose(), 0.31f, 1.3f));
            names.Add("eat"); poses.Add(c => Acts.Get("eat", c, rnd).Step(new Pose(), 0.42f, 1.0f));
            names.Add("walk"); poses.Add(c => Acts.Get("walk", c, rnd).Step(new Pose(), 0.5f, 1.0f));
            names.Add("special 1"); poses.Add(c => Acts.Get(c.Specials[0], c, rnd).Step(new Pose(), 0.5f, 1.0f));
            names.Add("special 2"); poses.Add(c => Acts.Get(c.Specials[1], c, rnd).Step(new Pose(), 0.5f, 1.0f));
            names.Add("special 3"); poses.Add(c => Acts.Get(c.Specials[2], c, rnd).Step(new Pose(), 0.5f, 1.0f));
            names.Add("special 4"); poses.Add(c => Acts.Get(c.Specials[3], c, rnd).Step(new Pose(), 0.5f, 1.0f));

            int cw = 230, ch = 330; float scale = 0.85f;
            var ids = Characters.Ids;
            using (var bmp = new Bitmap(cw * poses.Count, ch * ids.Length, PixelFormat.Format32bppArgb))
            using (var g = Graphics.FromImage(bmp))
            {
                g.Clear(Color.FromArgb(245, 246, 250));
                using (var pen = new Pen(Color.FromArgb(225, 228, 235)))
                {
                    for (int i = 1; i < poses.Count; i++) g.DrawLine(pen, i * cw, 0, i * cw, bmp.Height);
                    for (int i = 1; i < ids.Length; i++) g.DrawLine(pen, 0, i * ch, bmp.Width, i * ch);
                }
                for (int r = 0; r < ids.Length; r++)
                {
                    var c = Characters.Get(ids[r]);
                    for (int col = 0; col < poses.Count; col++)
                    {
                        var p = poses[col](c);
                        DrawCharacter(g, c, p, 1.0f, scale, col * cw + cw / 2, r * ch + ch - 22, true);
                        DU.Text(g, c.Name + " - " + names[col], col * cw + cw / 2, r * ch + 14, 13, Color.FromArgb(90, 90, 110), false, "Segoe UI", 1);
                    }
                }
                bmp.Save(path, ImageFormat.Png);
            }
        }
    }
}
