using System;
using System.Collections.Generic;

namespace SJBuddy
{
    // One animated action: a function of (base pose, normalized time u, global time t) -> pose for this frame.
    public class Act
    {
        public string Id;
        public float Dur;
        public Func<Pose, float, float, Pose> Step;
        public float MoveX;        // logical px per second of window movement (positive = facing direction)
        public bool NeedsWander;
        public float Weight = 1f;
        public string LineCat;     // line category to say when the action starts
    }

    public static class Acts
    {
        static float Sin(double x) { return (float)Math.Sin(x); }
        static float Abs(float x) { return Math.Abs(x); }
        const float PI = (float)Math.PI;

        public static readonly string[] Generic = { "look", "yawn", "stretch", "wave", "jump", "dance", "spin", "walk", "sneeze", "laugh", "think", "eat", "peek", "sing", "exercise", "phone", "drink" };

        static Act A(string id, float dur, Func<Pose, float, float, Pose> step)
        {
            var a = new Act(); a.Id = id; a.Dur = dur; a.Step = step; a.LineCat = "act_" + id; return a;
        }

        public static Act Get(string id, Character c, Random rnd)
        {
            switch (id)
            {
                case "look": return A(id, 1.8f, (p, u, t) => { var q = p.Clone(); q.Eyes = Eyes.Open; q.PupilX = u < 0.5f ? 1 : -1; q.Tilt = u < 0.5f ? 4 : -4; return q; });
                case "yawn": return A(id, 2.4f, (p, u, t) => { var q = p.Clone(); q.Eyes = Eyes.Closed; q.Mouth = u < 0.15f ? Mouth.O : (u < 0.8f ? Mouth.Yawn : Mouth.Neutral); q.ArmR = 20 + 130 * Sin(PI * u); q.Tilt = -4 * Sin(PI * u); q.SY = 1 + 0.04f * Sin(PI * u); q.Prop = null; return q; });
                case "stretch": return A(id, 2.2f, (p, u, t) => { var q = p.Clone(); q.ArmL = q.ArmR = 20 + 150 * Sin(PI * u); q.Eyes = Eyes.Closed; q.Mouth = Mouth.Flat; q.SY = 1 + 0.06f * Sin(PI * u); q.Fx = u > 0.7f ? "sparkle" : null; q.Prop = null; return q; });
                case "wave": return A(id, 2.2f, (p, u, t) => { var q = p.Clone(); q.ArmR = 130 + 25 * Sin(u * PI * 6); q.Eyes = Eyes.Happy; q.Mouth = Mouth.Smile; q.Tilt = 3 * Sin(u * PI * 6); q.Prop = null; return q; });
                case "jump": return A(id, 1.6f, (p, u, t) => { var q = p.Clone(); float h = Abs(Sin(u * PI * 2)); q.DY = -55 * h; q.SY = 1 + 0.08f * h; q.SX = 1 - 0.05f * h; q.Eyes = Eyes.Wide; q.Mouth = Mouth.Laugh; q.ArmL = q.ArmR = 160 * h; q.Prop = null; return q; });
                case "dance": return A(id, 4f, (p, u, t) => { var q = p.Clone(); float s = Sin(u * PI * 8); q.DX = 12 * s; q.Tilt = 8 * s; q.ArmL = 90 + 60 * s; q.ArmR = 90 - 60 * s; q.Mouth = Mouth.Laugh; q.Eyes = Eyes.Happy; q.Fx = "notes"; q.DY = -6 * Abs(s); q.Prop = null; return q; });
                case "spin":
                    return A(id, 2.8f, (p, u, t) =>
                    {
                        var q = p.Clone(); q.Prop = null;
                        if (u < 0.65f) { float sx = (float)Math.Cos(u / 0.65f * PI * 4); q.SX = Math.Abs(sx) < 0.08f ? 0.08f : sx; q.ArmL = q.ArmR = 80; q.Eyes = Eyes.Wide; q.Mouth = Mouth.O; }
                        else { q.Eyes = Eyes.Dizzy; q.Fx = "stars"; q.Tilt = 6 * Sin(t * 10); q.Mouth = Mouth.Wavy; q.ArmL = q.ArmR = 40; }
                        return q;
                    });
                case "walk":
                    {
                        int dir = rnd.Next(2) == 0 ? 1 : -1;
                        var a = A(id, 2.5f + (float)rnd.NextDouble() * 1.8f, (p, u, t) => { var q = p.Clone(); q.Walking = true; q.LegPhase = t * 9; q.Facing = dir; q.ArmL = 15 + 12 * Sin(t * 9); q.ArmR = 15 - 12 * Sin(t * 9); q.DY = -2 * Abs(Sin(t * 9)); q.Eyes = Eyes.Open; q.Mouth = Mouth.Smile; q.Prop = null; return q; });
                        a.MoveX = 55; a.NeedsWander = true; return a;
                    }
                case "sneeze":
                    return A(id, 2f, (p, u, t) =>
                    {
                        var q = p.Clone(); q.Prop = null;
                        if (u < 0.55f) { float k = u / 0.55f; q.Eyes = Eyes.Squint; q.Mouth = Mouth.O; q.Tilt = -9 * k; q.SY = 1 + 0.05f * k; q.ArmR = 60 * k; }
                        else if (u < 0.85f) { q.Eyes = Eyes.Closed; q.Mouth = Mouth.Open; q.Tilt = 12; q.SY = 0.93f; q.Fx = "exclaim"; q.ArmR = 80; }
                        else { q.Eyes = Eyes.Closed; q.Mouth = Mouth.Neutral; q.Tilt = 2; q.ArmR = 100; }
                        return q;
                    });
                case "laugh": return A(id, 2.4f, (p, u, t) => { var q = p.Clone(); float s = Sin(u * PI * 8); q.DY = -5 * Abs(s); q.Mouth = Mouth.Laugh; q.Eyes = Eyes.Happy; q.Tilt = -6 + 3 * s; q.ArmL = q.ArmR = 40 + 20 * s; q.Blush = 0.5f; q.Prop = null; return q; });
                case "think":
                    return A(id, 3.2f, (p, u, t) =>
                    {
                        var q = p.Clone(); q.Prop = null; q.PupilX = 0.6f; q.PupilY = -0.8f; q.ArmR = 150; q.Mouth = Mouth.Neutral; q.Eyes = Eyes.Open;
                        if (u < 0.65f) q.Fx = "question"; else { q.Fx = "bulb"; q.Eyes = Eyes.Wide; q.Mouth = Mouth.O; q.ArmR = 120; }
                        return q;
                    });
                case "eat":
                    return A(id, 3.4f, (p, u, t) =>
                    {
                        var q = p.Clone(); q.Prop = c.Food; float bite = Sin(u * PI * 5);
                        q.ArmR = 100 + 60 * Math.Max(0, bite); q.Mouth = bite > 0.3f ? Mouth.Open : Mouth.Smile; q.Eyes = bite > 0.3f ? Eyes.Closed : Eyes.Happy; q.Blush = 0.4f;
                        if (u > 0.85f) { q.Prop = null; q.Mouth = Mouth.Smile; q.Eyes = Eyes.Happy; q.Fx = "hearts"; q.ArmR = 20; }
                        return q;
                    });
                case "peek":
                    {
                        var a = A(id, 3.4f, (p, u, t) => { var q = p.Clone(); q.Prop = null; float d = u < 0.3f ? u / 0.3f : (u < 0.7f ? 1 : (1 - u) / 0.3f); q.DY = 250 * d; q.Eyes = Eyes.Wide; q.Mouth = Mouth.O; q.ArmL = q.ArmR = 20; return q; });
                        a.Weight = 0.5f; return a;
                    }
                case "sing": return A(id, 3.6f, (p, u, t) => { var q = p.Clone(); q.Prop = "mic"; q.Mouth = ((int)(u * 8)) % 2 == 0 ? Mouth.Open : Mouth.O; q.Eyes = Eyes.Closed; q.Fx = "notes"; q.Tilt = 5 * Sin(u * PI * 4); q.ArmR = 110; q.ArmL = 30 + 20 * Sin(u * PI * 4); return q; });
                case "exercise": return A(id, 4f, (p, u, t) => { var q = p.Clone(); q.Prop = "dumbbell"; float s = Abs(Sin(u * PI * 4)); q.SY = 1 - 0.12f * s; q.ArmL = q.ArmR = 60 + 100 * s; q.Eyes = Eyes.Focus; q.Mouth = Mouth.Flat; q.Fx = u > 0.5f ? "sweat" : null; return q; });
                case "phone":
                    {
                        var a = A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "phone"; q.ArmR = -30; q.ArmL = 10; q.PupilX = 0.3f; q.PupilY = 0.9f; q.Eyes = Eyes.Open; q.Mouth = ((int)(u * 6)) % 3 == 0 ? Mouth.Smile : Mouth.Neutral; q.HeadBob = 3; return q; });
                        a.Weight = 0.7f; return a;
                    }
                case "drink": return A(id, 3f, (p, u, t) => { var q = p.Clone(); q.Prop = "water"; float s = Abs(Sin(u * PI * 2)); q.ArmR = 100 + 55 * s; q.Mouth = s > 0.6f ? Mouth.O : Mouth.Smile; q.Eyes = s > 0.6f ? Eyes.Closed : Eyes.Happy; if (u > 0.9f) q.Fx = "sparkle"; return q; });
                case "celebrate": return A(id, 2.6f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.ArmL = q.ArmR = 165; q.DY = -30 * Abs(Sin(u * PI * 3)); q.Eyes = Eyes.Stars; q.Mouth = Mouth.Laugh; q.Fx = "sparkle"; return q; });
                case "love": return A(id, 2.6f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.Eyes = Eyes.Hearts; q.Mouth = Mouth.Smile; q.Blush = 1; q.Fx = "hearts"; q.Tilt = 5 * Sin(u * PI * 3); q.ArmL = q.ArmR = 30; return q; });
                case "scared": return A(id, 2f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.Eyes = Eyes.Wide; q.Mouth = Mouth.Open; q.DY = -35 * Abs(Sin(u * PI * 2)); q.ArmL = q.ArmR = 150; q.Fx = "sweat"; return q; });
                case "angry": return A(id, 2.2f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.Eyes = Eyes.Angry; q.Mouth = Mouth.Sad; q.Fx = "anger"; q.ArmL = q.ArmR = 30; q.Tilt = 4 * Sin(u * PI * 6); return q; });
                case "salute": return A(id, 2f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.ArmR = 160; q.Eyes = Eyes.Open; q.Mouth = Mouth.Grin; q.SY = 1.03f; return q; });
                case "nod": return A(id, 1.6f, (p, u, t) => { var q = p.Clone(); q.HeadBob = 5 * Sin(u * PI * 6); q.Eyes = Eyes.Happy; q.Mouth = Mouth.Smile; return q; });

                // ---- character specials ----
                case "copter": return A(id, 4.2f, (p, u, t) => { var q = p.Clone(); q.Prop = "copter"; float h = u < 0.25f ? u / 0.25f : (u < 0.75f ? 1 : (1 - u) / 0.25f); q.DY = -90 * h + Sin(t * 3) * 6 * h; q.ArmL = q.ArmR = 60; q.Eyes = Eyes.Happy; q.Mouth = Mouth.Laugh; q.Walking = h > 0.5f; q.LegPhase = t * 6; return q; });
                case "door": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "door"; q.ArmR = 100; q.Eyes = Eyes.Open; q.Mouth = Mouth.Grin; q.PupilX = 0.8f; q.Fx = u > 0.5f ? "sparkle" : null; return q; });
                case "mouse": return A(id, 2.8f, (p, u, t) => { var q = p.Clone(); q.Prop = "mouse"; q.Eyes = Eyes.Wide; q.Mouth = Mouth.Open; q.DY = -40 * Abs(Sin(u * PI * 3)); q.ArmL = q.ArmR = 160; q.Fx = "sweat"; q.SY = 1.05f; return q; });
                case "torch": return A(id, 3f, (p, u, t) => { var q = p.Clone(); q.Prop = "torch"; q.ArmR = 120; q.Eyes = Eyes.Happy; q.Mouth = Mouth.Smile; q.Fx = "sparkle"; return q; });
                case "net": return A(id, 3.6f, (p, u, t) => { var q = p.Clone(); q.Prop = "net"; q.ArmR = 120 + 20 * Sin(u * PI * 6); q.Eyes = Eyes.Wide; q.Mouth = Mouth.Laugh; q.DX = 10 * Sin(u * PI * 3); q.Walking = true; q.LegPhase = t * 8; q.PupilX = 0.9f; q.PupilY = -0.6f; return q; });
                case "spatula": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "spatula"; q.ArmR = 100; q.Eyes = Eyes.Happy; q.Mouth = Mouth.Grin; q.Fx = u > 0.6f ? "sparkle" : null; return q; });
                case "wand": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "wand"; q.ArmR = 130; q.Mouth = Mouth.O; q.Eyes = Eyes.Happy; q.Fx = "bubbles"; return q; });
                case "ready": return A(id, 2.4f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.DY = -30 * Abs(Sin(u * PI * 4)); q.ArmR = 170; q.ArmL = 30; q.Eyes = Eyes.Wide; q.Mouth = Mouth.Laugh; q.SY = 1.04f; return q; });
                case "buriburi": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.DX = 10 * Sin(u * PI * 10); q.Tilt = -6 * Sin(u * PI * 10); q.ArmL = q.ArmR = 60; q.Eyes = Eyes.Happy; q.Mouth = Mouth.Grin; q.Fx = "notes"; q.SY = 1 + 0.03f * Sin(u * PI * 20); return q; });
                case "chocobi": return Get("eat", c, rnd);
                case "actionkamen": return A(id, 3f, (p, u, t) => { var q = p.Clone(); q.Prop = "cape"; q.ArmR = 170; q.ArmL = 40; q.Eyes = Eyes.Open; q.Brow = 0.8f; q.Mouth = Mouth.Grin; q.Fx = "sparkle"; q.SY = 1.04f; q.DY = u < 0.3f ? -40 * Sin(u / 0.3f * PI) : 0; return q; });
                case "shiro": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "shiro"; q.PupilX = -1; q.PupilY = 0.6f; q.ArmL = 60 + 30 * Sin(u * PI * 4); q.Eyes = Eyes.Happy; q.Mouth = Mouth.Smile; q.Fx = u > 0.5f ? "hearts" : null; return q; });
                case "chai": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "cup"; float s = Abs(Sin(u * PI * 2)); q.ArmR = 100 + 50 * s; q.Mouth = s > 0.6f ? Mouth.O : Mouth.Smile; q.Eyes = s > 0.6f ? Eyes.Closed : Eyes.Happy; q.Blush = 0.3f; return q; });
                case "garba": return A(id, 4f, (p, u, t) => { var q = p.Clone(); q.Prop = null; float s = Sin(u * PI * 6); q.DX = 14 * s; q.Tilt = 10 * s; q.ArmL = 120 + 40 * s; q.ArmR = 120 - 40 * s; q.Mouth = Mouth.Laugh; q.Eyes = Eyes.Happy; q.Fx = "notes"; q.DY = -8 * Abs(s); q.Walking = true; q.LegPhase = u * PI * 6; return q; });
                case "mataji": return A(id, 2.6f, (p, u, t) => { var q = p.Clone(); q.Prop = null; q.ArmL = q.ArmR = 165 + 10 * Sin(u * PI * 8); q.Eyes = Eyes.Wide; q.Mouth = Mouth.Open; q.Fx = "sweat"; q.Tilt = -5 + 3 * Sin(u * PI * 8); q.SY = 1.03f; return q; });
                case "jalebi": return Get("eat", c, rnd);
                case "scooter":
                    {
                        var a = A(id, 4.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "scooter"; q.DY = -16 + Sin(t * 20) * 1.5f; q.ArmR = 60; q.ArmL = 60; q.Eyes = Eyes.Open; q.Mouth = Mouth.Grin; q.Tilt = 2 * Sin(t * 20); q.PupilX = 0.8f; return q; });
                        a.MoveX = 60; a.NeedsWander = true; return a;
                    }
                case "chalk": return A(id, 4f, (p, u, t) => { var q = p.Clone(); q.Prop = "chalk"; q.ArmR = 100 + 15 * Sin(u * PI * 6); q.Eyes = Eyes.Open; q.PupilX = 1; q.Mouth = ((int)(u * 8)) % 2 == 0 ? Mouth.Open : Mouth.Neutral; q.Brow = 0.2f; return q; });
                case "register": return A(id, 3.5f, (p, u, t) => { var q = p.Clone(); q.Prop = "register"; q.ArmR = 30 + 5 * Sin(u * PI * 10); q.PupilY = 0.9f; q.PupilX = 0.5f; q.Mouth = Mouth.Flat; q.Eyes = Eyes.Focus; q.HeadBob = 3; return q; });
                case "jar": return A(id, 3f, (p, u, t) => { var q = p.Clone(); q.Prop = "jar"; q.ArmR = 120; q.Eyes = Eyes.Happy; q.Mouth = Mouth.Grin; q.Fx = "sparkle"; return q; });
            }
            return null;
        }

        // Weighted random pick of an idle action, biased by what the user has reacted to before.
        public static string PickRandom(Character c, Brain b, bool wander, Random rnd)
        {
            var ids = new List<string>(); var ws = new List<double>();
            foreach (var id in Generic)
            {
                if (id == "walk" && !wander) continue;
                if (id == "drink") continue; // reserved for water time
                double w = 1.0; if (id == "peek") w = 0.5; if (id == "phone") w = 0.7; if (id == "look") w = 1.6;
                ids.Add(id); ws.Add(w * b.LikeWeight("act_" + id));
            }
            foreach (var id in c.Specials)
            {
                if (id == "scooter" && !wander) continue;
                ids.Add(id); ws.Add(1.5 * b.LikeWeight("act_" + id));
            }
            double total = 0; foreach (var w in ws) total += w;
            double r = rnd.NextDouble() * total;
            for (int i = 0; i < ids.Count; i++) { r -= ws[i]; if (r <= 0) return ids[i]; }
            return ids[ids.Count - 1];
        }
    }
}
