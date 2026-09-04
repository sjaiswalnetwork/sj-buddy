using System;
using System.IO;
using System.Media;

namespace SJBuddy
{
    // Small synthesized sounds (no external files needed).
    public static class Sounds
    {
        static SoundPlayer chime, pop, tada, snore;

        static SoundPlayer Make(double[] freqs, double[] durs, double vol)
        {
            int rate = 22050;
            int total = 0; for (int i = 0; i < durs.Length; i++) total += (int)(durs[i] * rate);
            var data = new byte[total * 2];
            int pos = 0;
            for (int n = 0; n < freqs.Length; n++)
            {
                int len = (int)(durs[n] * rate);
                for (int i = 0; i < len; i++)
                {
                    double env = Math.Min(1.0, Math.Min(i / (0.01 * rate), (len - i) / (0.04 * rate)));
                    double v = Math.Sin(2 * Math.PI * freqs[n] * i / rate) * vol * env;
                    v += Math.Sin(2 * Math.PI * freqs[n] * 2 * i / rate) * vol * env * 0.15;
                    short s = (short)(v * 32767);
                    data[pos++] = (byte)(s & 0xFF); data[pos++] = (byte)((s >> 8) & 0xFF);
                }
            }
            var ms = new MemoryStream();
            var w = new BinaryWriter(ms);
            w.Write(new[] { 'R', 'I', 'F', 'F' }); w.Write(36 + data.Length); w.Write(new[] { 'W', 'A', 'V', 'E' });
            w.Write(new[] { 'f', 'm', 't', ' ' }); w.Write(16); w.Write((short)1); w.Write((short)1); w.Write(rate); w.Write(rate * 2); w.Write((short)2); w.Write((short)16);
            w.Write(new[] { 'd', 'a', 't', 'a' }); w.Write(data.Length); w.Write(data);
            w.Flush(); ms.Position = 0;
            var sp = new SoundPlayer(ms); sp.Load();
            return sp;
        }

        static void Play(ref SoundPlayer sp, double[] f, double[] d, double vol)
        {
            try { if (sp == null) sp = Make(f, d, vol); sp.Play(); } catch { }
        }

        public static void Chime() { Play(ref chime, new[] { 880.0, 1174.66, 1318.5 }, new[] { 0.12, 0.12, 0.28 }, 0.30); }
        public static void Pop() { Play(ref pop, new[] { 520.0, 780.0 }, new[] { 0.05, 0.07 }, 0.25); }
        public static void Tada() { Play(ref tada, new[] { 523.25, 659.25, 783.99, 1046.5 }, new[] { 0.1, 0.1, 0.1, 0.35 }, 0.30); }
        public static void Snore() { Play(ref snore, new[] { 140.0, 120.0 }, new[] { 0.25, 0.25 }, 0.12); }
    }
}
