using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace SJBuddy
{
    // The "learning" layer: rule-based statistics that adapt reminders, chatter and greetings to the user's habits.
    // Privacy: it never records WHAT you type - only WHEN and HOW MUCH.
    public class Brain
    {
        KvStore kv;
        public double[] HourActivity = new double[24];   // keystrokes per hour-of-day (slowly decayed)
        public double[] DowActivity = new double[7];
        public double KpmBaseline = 0;                   // EMA of keys/min while typing
        public double KpmPeak = 0;
        public double Chattiness = 1.0;                  // learned multiplier 0.5..1.6
        public long TotalKeystrokes = 0;
        public int DaysUsed = 0;
        public string LastDay = "";
        public string FirstDay = "";
        public int SipsToday = 0; public string SipsDate = ""; public int TotalSips = 0; public int BestSipsDay = 0;
        public int StreakDays = 0; public string LastStreakDate = "";
        public double AvgSipsPerDay = 0; public int SipDays = 0;
        public double WaterAckLatency = 0; public int WaterAcked = 0; public int WaterIgnored = 0; public int IgnoredInRow = 0;
        public double UsualStart = -1; public double UsualEnd = -1; public string StartRecordedDay = "";
        public double LunchHour = -1;
        public int Xp = 0;
        public int LateNights = 0; public string LateNightDay = "";
        public Dictionary<string, double> Likes = new Dictionary<string, double>();
        public int BreakIgnored = 0; public int BreakTaken = 0;
        public Dictionary<string, int> CharMinutes = new Dictionary<string, int>();
        public double Mood = 0.2;
        public int Clicks = 0; public int Pets = 0; public int ActionsWatched = 0;
        public int LongestTypingMinutes = 0;
        public int TypingMinutes = 0;
        public int LinesShown = 0; public int LinesLiked = 0;
        public int LastLevelAnnounced = 1;

        public Brain() { kv = new KvStore(Paths.BrainFile); Load(); }

        void Load()
        {
            LoadArr(kv.Get("hour_activity", ""), HourActivity);
            LoadArr(kv.Get("dow_activity", ""), DowActivity);
            KpmBaseline = kv.GetDouble("kpm_baseline", 0); KpmPeak = kv.GetDouble("kpm_peak", 0);
            Chattiness = kv.GetDouble("chattiness", 1.0);
            TotalKeystrokes = kv.GetLong("total_keys", 0); DaysUsed = kv.GetInt("days_used", 0);
            LastDay = kv.Get("last_day", ""); FirstDay = kv.Get("first_day", "");
            SipsToday = kv.GetInt("sips_today", 0); SipsDate = kv.Get("sips_date", ""); TotalSips = kv.GetInt("total_sips", 0); BestSipsDay = kv.GetInt("best_sips_day", 0);
            StreakDays = kv.GetInt("streak_days", 0); LastStreakDate = kv.Get("last_streak_date", "");
            AvgSipsPerDay = kv.GetDouble("avg_sips", 0); SipDays = kv.GetInt("sip_days", 0);
            WaterAckLatency = kv.GetDouble("water_ack_latency", 0); WaterAcked = kv.GetInt("water_acked", 0); WaterIgnored = kv.GetInt("water_ignored", 0); IgnoredInRow = kv.GetInt("ignored_in_row", 0);
            UsualStart = kv.GetDouble("usual_start", -1); UsualEnd = kv.GetDouble("usual_end", -1); StartRecordedDay = kv.Get("start_recorded_day", "");
            LunchHour = kv.GetDouble("lunch_hour", -1);
            Xp = kv.GetInt("xp", 0); LateNights = kv.GetInt("late_nights", 0); LateNightDay = kv.Get("late_night_day", "");
            BreakIgnored = kv.GetInt("break_ignored", 0); BreakTaken = kv.GetInt("break_taken", 0);
            Mood = kv.GetDouble("mood", 0.2);
            Clicks = kv.GetInt("clicks", 0); Pets = kv.GetInt("pets", 0); ActionsWatched = kv.GetInt("actions_watched", 0);
            LongestTypingMinutes = kv.GetInt("longest_typing", 0); TypingMinutes = kv.GetInt("typing_minutes", 0);
            LinesShown = kv.GetInt("lines_shown", 0); LinesLiked = kv.GetInt("lines_liked", 0);
            LastLevelAnnounced = kv.GetInt("last_level_announced", 1);
            Likes.Clear();
            foreach (var part in kv.Get("likes", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = part.IndexOf(':'); if (i <= 0) continue; double w;
                if (double.TryParse(part.Substring(i + 1), NumberStyles.Float, CultureInfo.InvariantCulture, out w)) Likes[part.Substring(0, i)] = w;
            }
            CharMinutes.Clear();
            foreach (var part in kv.Get("char_minutes", "").Split(new[] { ';' }, StringSplitOptions.RemoveEmptyEntries))
            {
                int i = part.IndexOf(':'); if (i <= 0) continue; int m;
                if (int.TryParse(part.Substring(i + 1), NumberStyles.Integer, CultureInfo.InvariantCulture, out m)) CharMinutes[part.Substring(0, i)] = m;
            }
        }

        static void LoadArr(string s, double[] arr)
        {
            var parts = s.Split(',');
            for (int i = 0; i < arr.Length && i < parts.Length; i++)
            {
                double v; if (double.TryParse(parts[i], NumberStyles.Float, CultureInfo.InvariantCulture, out v)) arr[i] = v;
            }
        }
        static string SaveArr(double[] arr)
        {
            var sb = new StringBuilder();
            for (int i = 0; i < arr.Length; i++) { if (i > 0) sb.Append(','); sb.Append(Math.Round(arr[i], 2).ToString(CultureInfo.InvariantCulture)); }
            return sb.ToString();
        }

        public void Save()
        {
            kv.Set("hour_activity", SaveArr(HourActivity)); kv.Set("dow_activity", SaveArr(DowActivity));
            kv.Set("kpm_baseline", KpmBaseline); kv.Set("kpm_peak", KpmPeak); kv.Set("chattiness", Chattiness);
            kv.Set("total_keys", TotalKeystrokes); kv.Set("days_used", DaysUsed); kv.Set("last_day", LastDay); kv.Set("first_day", FirstDay);
            kv.Set("sips_today", SipsToday); kv.Set("sips_date", SipsDate); kv.Set("total_sips", TotalSips); kv.Set("best_sips_day", BestSipsDay);
            kv.Set("streak_days", StreakDays); kv.Set("last_streak_date", LastStreakDate); kv.Set("avg_sips", AvgSipsPerDay); kv.Set("sip_days", SipDays);
            kv.Set("water_ack_latency", WaterAckLatency); kv.Set("water_acked", WaterAcked); kv.Set("water_ignored", WaterIgnored); kv.Set("ignored_in_row", IgnoredInRow);
            kv.Set("usual_start", UsualStart); kv.Set("usual_end", UsualEnd); kv.Set("start_recorded_day", StartRecordedDay); kv.Set("lunch_hour", LunchHour);
            kv.Set("xp", Xp); kv.Set("late_nights", LateNights); kv.Set("late_night_day", LateNightDay);
            kv.Set("break_ignored", BreakIgnored); kv.Set("break_taken", BreakTaken); kv.Set("mood", Mood);
            kv.Set("clicks", Clicks); kv.Set("pets", Pets); kv.Set("actions_watched", ActionsWatched);
            kv.Set("longest_typing", LongestTypingMinutes); kv.Set("typing_minutes", TypingMinutes);
            kv.Set("lines_shown", LinesShown); kv.Set("lines_liked", LinesLiked); kv.Set("last_level_announced", LastLevelAnnounced);
            var sb = new StringBuilder();
            foreach (var kvp in Likes) sb.Append(kvp.Key).Append(':').Append(Math.Round(kvp.Value, 2).ToString(CultureInfo.InvariantCulture)).Append(';');
            kv.Set("likes", sb.ToString());
            sb = new StringBuilder();
            foreach (var kvp in CharMinutes) sb.Append(kvp.Key).Append(':').Append(kvp.Value.ToString(CultureInfo.InvariantCulture)).Append(';');
            kv.Set("char_minutes", sb.ToString());
            kv.Save();
        }

        public void Reset()
        {
            kv.Clear();
            for (int i = 0; i < 24; i++) HourActivity[i] = 0;
            for (int i = 0; i < 7; i++) DowActivity[i] = 0;
            KpmBaseline = 0; KpmPeak = 0; Chattiness = 1.0; TotalKeystrokes = 0; DaysUsed = 0; LastDay = ""; FirstDay = "";
            SipsToday = 0; SipsDate = ""; TotalSips = 0; BestSipsDay = 0; StreakDays = 0; LastStreakDate = ""; AvgSipsPerDay = 0; SipDays = 0;
            WaterAckLatency = 0; WaterAcked = 0; WaterIgnored = 0; IgnoredInRow = 0; UsualStart = -1; UsualEnd = -1; StartRecordedDay = ""; LunchHour = -1;
            Xp = 0; LateNights = 0; LateNightDay = ""; Likes.Clear(); BreakIgnored = 0; BreakTaken = 0; CharMinutes.Clear(); Mood = 0.2;
            Clicks = 0; Pets = 0; ActionsWatched = 0; LongestTypingMinutes = 0; TypingMinutes = 0; LinesShown = 0; LinesLiked = 0; LastLevelAnnounced = 1;
            Save();
        }

        // ---------------- events ----------------
        static string Day(DateTime d) { return d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture); }

        public void TouchDay(DateTime now)
        {
            string today = Day(now);
            if (LastDay == today) return;
            if (FirstDay == "") FirstDay = today;
            DaysUsed++;
            LastDay = today;
            for (int i = 0; i < 24; i++) HourActivity[i] *= 0.97;
            for (int i = 0; i < 7; i++) DowActivity[i] *= 0.97;
            if (SipsDate != today)
            {
                if (SipsDate != "")
                {
                    SipDays++;
                    AvgSipsPerDay += (SipsToday - AvgSipsPerDay) / Math.Min(SipDays, 14);
                    if (SipsToday > BestSipsDay) BestSipsDay = SipsToday;
                }
                SipsToday = 0; SipsDate = today;
            }
            string yesterday = Day(now.AddDays(-1));
            if (LastStreakDate != "" && LastStreakDate != yesterday && LastStreakDate != today) StreakDays = 0;
        }

        public void OnKeystrokes(int n, DateTime now)
        {
            TotalKeystrokes += n;
            HourActivity[now.Hour] += n;
            DowActivity[(int)now.DayOfWeek] += n;
        }

        // Called once per minute.
        public void OnMinute(DateTime now, int kpm, bool typing, bool idle, string character)
        {
            TouchDay(now);
            if (typing)
            {
                TypingMinutes++; Xp += 1;
                if (KpmBaseline <= 0) KpmBaseline = kpm; else KpmBaseline = KpmBaseline * 0.95 + kpm * 0.05;
                if (kpm > KpmPeak) KpmPeak = kpm;
            }
            if (!idle)
            {
                double h = now.Hour + now.Minute / 60.0;
                string today = Day(now);
                if (StartRecordedDay != today)
                {
                    StartRecordedDay = today;
                    UsualStart = UsualStart < 0 ? h : UsualStart * 0.8 + h * 0.2;
                }
                UsualEnd = UsualEnd < 0 ? h : UsualEnd * 0.9 + h * 0.1;
                int m; CharMinutes.TryGetValue(character, out m); CharMinutes[character] = m + 1;
            }
            Mood *= 0.995;
        }

        public void OnSip(DateTime now, double latencySec, bool fromReminder)
        {
            TouchDay(now);
            SipsToday++; TotalSips++; Xp += 10; Mood = Math.Min(1, Mood + 0.2);
            if (fromReminder)
            {
                WaterAcked++; IgnoredInRow = 0;
                WaterAckLatency = WaterAcked == 1 ? latencySec : WaterAckLatency * 0.8 + latencySec * 0.2;
            }
            string today = Day(now), yesterday = Day(now.AddDays(-1));
            if (SipsToday == 4)
            {
                if (LastStreakDate == yesterday) StreakDays++; else if (LastStreakDate != today) StreakDays = 1;
                LastStreakDate = today;
            }
            if (SipsToday > BestSipsDay) BestSipsDay = SipsToday;
        }

        public void OnReminderIgnored() { WaterIgnored++; IgnoredInRow++; Mood = Math.Max(-1, Mood - 0.1); }
        public string WaterStyle
        {
            get
            {
                if (IgnoredInRow >= 3) return "serious";
                if (WaterAcked >= 3 && WaterAckLatency > 240) return "persistent";
                return "gentle";
            }
        }
        public void OnClick() { Clicks++; Xp += 2; Mood = Math.Min(1, Mood + 0.1); }
        public void OnPet() { Pets++; Xp += 3; Mood = Math.Min(1, Mood + 0.25); }
        public void OnLineShown(string cat) { LinesShown++; }
        public void OnLineLiked(string cat)
        {
            LinesLiked++; Chattiness = Math.Min(1.6, Chattiness + 0.04);
            double w; Likes.TryGetValue(cat, out w); Likes[cat] = w + 1;
        }
        public void OnAnnoyed() { Chattiness = Math.Max(0.5, Chattiness - 0.12); }
        public void OnLateNight(DateTime now) { string d = Day(now); if (LateNightDay != d) { LateNightDay = d; LateNights++; } }
        public void OnLunchObserved(double hour) { LunchHour = LunchHour < 0 ? hour : LunchHour * 0.7 + hour * 0.3; }
        public void OnBreakTaken() { BreakTaken++; Xp += 5; }
        public void OnBreakIgnored() { BreakIgnored++; }
        public void OnActionWatched() { ActionsWatched++; }
        public double LikeWeight(string cat) { double w; return Likes.TryGetValue(cat, out w) ? 1 + Math.Min(2, w * 0.25) : 1; }
        public string FavouriteCategory()
        {
            string best = null; double bw = 0;
            foreach (var kvp in Likes) if (kvp.Value > bw) { bw = kvp.Value; best = kvp.Key; }
            return best;
        }

        public int Level { get { return (int)Math.Floor(Math.Sqrt(Xp / 50.0)) + 1; } }
        public int XpForNext { get { int l = Level; return l * l * 50; } }

        public string Title(string lang)
        {
            int l = Math.Min(Level, 10);
            string[] en = { "", "Newbie", "Buddy", "Pal", "Good Friend", "Best Friend", "Bestie", "Family", "Legend", "Guru", "Desktop King" };
            string[] hing = { "", "Naya Dost", "Buddy", "Yaar", "Pakka Dost", "Jigri Dost", "Bestie", "Parivaar", "Legend", "Guru", "Desktop Ka Raja" };
            string[] hi = { "", "नया दोस्त", "बडी", "यार", "पक्का दोस्त", "जिगरी दोस्त", "बेस्टी", "परिवार", "लेजेंड", "गुरु", "डेस्कटॉप का राजा" };
            if (lang == "hi") return hi[l];
            if (lang == "en") return en[l];
            return hing[l];
        }

        public string MoodWord(string lang)
        {
            if (Mood > 0.5) return lang == "hi" ? "बहुत खुश" : (lang == "en" ? "very happy" : "bahut khush");
            if (Mood > 0.1) return lang == "hi" ? "खुश" : (lang == "en" ? "happy" : "khush");
            if (Mood > -0.2) return lang == "hi" ? "ठीक-ठाक" : (lang == "en" ? "okay" : "theek-thaak");
            return lang == "hi" ? "थोड़ा उदास" : (lang == "en" ? "a bit low" : "thoda udaas");
        }

        public int[] PeakHours()
        {
            var idx = new List<int>(); for (int i = 0; i < 24; i++) idx.Add(i);
            idx.Sort((a, b) => HourActivity[b].CompareTo(HourActivity[a]));
            var res = new List<int>();
            for (int i = 0; i < 3 && i < idx.Count; i++) if (HourActivity[idx[i]] > 0) res.Add(idx[i]);
            res.Sort();
            return res.ToArray();
        }

        public bool HasActivityProfile { get { double s = 0; for (int i = 0; i < 24; i++) s += HourActivity[i]; return s > 3000 && DaysUsed >= 2; } }

        public bool IsUsuallyActive(int hour)
        {
            if (!HasActivityProfile) return true;
            double s = 0; for (int i = 0; i < 24; i++) s += HourActivity[i];
            return HourActivity[hour] > (s / 24.0) * 0.35;
        }

        static string Hm(double h)
        {
            if (h < 0) return "?";
            int hh = (int)Math.Floor(h); int mm = (int)Math.Round((h - hh) * 60); if (mm == 60) { mm = 0; hh++; }
            return string.Format("{0:00}:{1:00}", hh % 24, mm);
        }

        // Plain-language report of what has been learned so far.
        public List<string> Summary(string petName, string lang)
        {
            var L = new List<string>();
            bool hi = lang == "hi"; bool en = lang == "en";
            L.Add(string.Format(en ? "{0} is Level {1} \"{2}\" with {3} XP (next level at {4})."
                              : hi ? "{0} लेवल {1} \"{2}\" पर है, {3} XP (अगला लेवल {4} पर)।"
                                   : "{0} Level {1} \"{2}\" pe hai, {3} XP (agla level {4} pe).", petName, Level, Title(lang), Xp, XpForNext));
            L.Add(string.Format(en ? "Days together: {0} (since {1}). Mood right now: {2}."
                              : hi ? "साथ बिताए दिन: {0} ({1} से)। अभी का मूड: {2}।"
                                   : "Saath bitaye din: {0} ({1} se). Abhi ka mood: {2}.", DaysUsed, FirstDay == "" ? "today" : FirstDay, MoodWord(lang)));
            L.Add(string.Format(en ? "Water: {0} sips today, {1:0.0} per day on average, best day {2}, streak {3} days, {4} total."
                              : hi ? "पानी: आज {0} घूँट, औसत {1:0.0} प्रति दिन, सबसे अच्छा दिन {2}, स्ट्रीक {3} दिन, कुल {4}।"
                                   : "Paani: aaj {0} sips, average {1:0.0} per day, best day {2}, streak {3} din, total {4}.", SipsToday, SipDays == 0 ? SipsToday : AvgSipsPerDay, BestSipsDay, StreakDays, TotalSips));
            string style = WaterStyle == "serious" ? (en ? "serious (you ignored the last few)" : hi ? "गंभीर (आपने पिछले कुछ नज़रअंदाज़ किए)" : "serious (aapne pichhle kuch ignore kiye)")
                         : WaterStyle == "persistent" ? (en ? "persistent (you take a while to respond)" : hi ? "ज़िद्दी (आप देर से जवाब देते हैं)" : "persistent (aap der se respond karte ho)")
                         : (en ? "gentle" : hi ? "नरम" : "gentle");
            L.Add(string.Format(en ? "Reminders answered {0}, ignored {1}, average response {2}s. Reminder style learned: {3}."
                              : hi ? "रिमाइंडर: {0} माने, {1} अनदेखे, औसत जवाब {2} सेकंड। सीखा हुआ अंदाज़: {3}।"
                                   : "Reminders: {0} maane, {1} ignore, average response {2}s. Learned style: {3}.", WaterAcked, WaterIgnored, (int)WaterAckLatency, style));
            L.Add(string.Format(en ? "Typing: {0:n0} keys in {1} minutes. Your usual speed is {2:0} keys/min, fastest burst {3:0}. Longest non-stop session {4} min."
                              : hi ? "टाइपिंग: {1} मिनट में {0:n0} keys। आपकी सामान्य गति {2:0} keys/min, सबसे तेज़ {3:0}। सबसे लंबा सेशन {4} मिनट।"
                                   : "Typing: {0:n0} keys, {1} minute. Aapki usual speed {2:0} keys/min, fastest burst {3:0}. Longest non-stop session {4} min.", TotalKeystrokes, TypingMinutes, KpmBaseline, KpmPeak, LongestTypingMinutes));
            var peaks = PeakHours();
            string peakStr = peaks.Length == 0 ? "?" : string.Join(", ", Array.ConvertAll(peaks, h => string.Format("{0:00}:00", h)));
            L.Add(string.Format(en ? "Your day: usually starts ~{0}, ends ~{1}, lunch break ~{2}. Peak hours: {3}."
                              : hi ? "आपका दिन: शुरुआत ~{0}, अंत ~{1}, लंच ~{2}। पीक घंटे: {3}।"
                                   : "Aapka din: shuru ~{0}, khatam ~{1}, lunch ~{2}. Peak hours: {3}.", Hm(UsualStart), Hm(UsualEnd), Hm(LunchHour), peakStr));
            string chat = Chattiness < 0.8 ? (en ? "quieter" : hi ? "कम बोलना" : "kam bolna") : Chattiness > 1.2 ? (en ? "chattier" : hi ? "ज़्यादा बोलना" : "zyada bolna") : (en ? "balanced" : hi ? "संतुलित" : "balanced");
            L.Add(string.Format(en ? "Chatter: {0} lines shown, {1} you reacted to. Learned to be {2} (x{3:0.00})."
                              : hi ? "बातें: {0} लाइनें दिखाईं, {1} पर आपने रिएक्ट किया। सीखा: {2} (x{3:0.00})।"
                                   : "Baatein: {0} lines dikhayi, {1} pe aapne react kiya. Seekha: {2} (x{3:0.00}).", LinesShown, LinesLiked, chat, Chattiness));
            string fav = FavouriteCategory();
            if (fav != null) L.Add(string.Format(en ? "You react most to my \"{0}\" lines, so I do more of those." : hi ? "आप मेरी \"{0}\" लाइनों पर सबसे ज़्यादा रिएक्ट करते हैं, तो वैसी और सुनाता हूँ।" : "Aap meri \"{0}\" lines pe sabse zyada react karte ho, toh waisi aur sunata hoon.", fav));
            L.Add(string.Format(en ? "Late-night sessions: {0}. Break suggestions taken {1}, ignored {2}. Clicks {3}, pets {4}."
                              : hi ? "देर रात सेशन: {0}। ब्रेक सुझाव माने {1}, अनदेखे {2}। क्लिक {3}, प्यार {4}।"
                                   : "Late-night sessions: {0}. Break suggestions maane {1}, ignore {2}. Clicks {3}, pets {4}.", LateNights, BreakTaken, BreakIgnored, Clicks, Pets));
            return L;
        }
    }
}
