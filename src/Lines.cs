using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace SJBuddy
{
    // Everything the characters say. Each entry: character, category, English, Hindi, Hinglish.
    // Placeholders: {name} pet name, {user} your name, {sips}, {streak}, {level}, {title}, {mins}, {kpm}.
    public static partial class Lines
    {
        class Line { public string En, Hi, Hing; }
        static readonly Dictionary<string, List<Line>> db = new Dictionary<string, List<Line>>(StringComparer.OrdinalIgnoreCase);
        static readonly Dictionary<string, int> lastPick = new Dictionary<string, int>();

        static void L(string ch, string cat, string en, string hi, string hing)
        {
            string key = ch + "|" + cat;
            List<Line> list;
            if (!db.TryGetValue(key, out list)) { list = new List<Line>(); db[key] = list; }
            list.Add(new Line { En = en, Hi = hi, Hing = hing });
        }

        public static bool Has(string ch, string cat) { return db.ContainsKey(ch + "|" + cat) || db.ContainsKey("*|" + cat); }

        public static string Pick(string ch, string cat, string lang, Random rnd, Dictionary<string, string> vars)
        {
            List<Line> list;
            string key = ch + "|" + cat;
            if (!db.TryGetValue(key, out list)) { key = "*|" + cat; if (!db.TryGetValue(key, out list)) return null; }
            int idx = rnd.Next(list.Count);
            int last; if (list.Count > 1 && lastPick.TryGetValue(key, out last) && last == idx) idx = (idx + 1) % list.Count;
            lastPick[key] = idx;
            var ln = list[idx];
            string l = lang;
            if (l == "mix") { int r = rnd.Next(10); l = r < 5 ? "hing" : (r < 8 ? "en" : "hi"); }
            string s = l == "hi" ? ln.Hi : (l == "en" ? ln.En : ln.Hing);
            if (string.IsNullOrEmpty(s)) s = string.IsNullOrEmpty(ln.Hing) ? ln.En : ln.Hing;
            string user = null; if (vars != null) vars.TryGetValue("user", out user);
            if (string.IsNullOrEmpty(user)) user = l == "hi" ? "दोस्त" : (l == "en" ? "friend" : "dost");
            s = s.Replace("{user}", user);
            if (vars != null) foreach (var kv in vars) if (kv.Key != "user") s = s.Replace("{" + kv.Key + "}", kv.Value ?? "");
            return s;
        }

        // Optional user additions: a text file with lines "character|category|english|hindi|hinglish" ("*" = all characters).
        public static int LoadCustom(string path)
        {
            int n = 0;
            try
            {
                if (!File.Exists(path)) return 0;
                foreach (var raw in File.ReadAllLines(path, Encoding.UTF8))
                {
                    var line = raw.Trim();
                    if (line.Length == 0 || line.StartsWith("#")) continue;
                    var parts = line.Split('|');
                    if (parts.Length < 3) continue;
                    string en = parts[2].Trim(), hi = parts.Length > 3 ? parts[3].Trim() : "", hing = parts.Length > 4 ? parts[4].Trim() : "";
                    if (hi == "") hi = en; if (hing == "") hing = en;
                    L(parts[0].Trim().ToLowerInvariant(), parts[1].Trim().ToLowerInvariant(), en, hi, hing); n++;
                }
            }
            catch { }
            return n;
        }

        static Lines()
        {
            // =============================== COMMON (fallbacks for every character) ===============================
            L("*", "welcome", "Hi {user}! I'm {name}, your new desk buddy. Made by SJ.", "नमस्ते {user}! मैं {name} हूँ, आपका नया डेस्क बडी। Made by SJ.", "Hi {user}! Main {name} hoon, aapka naya desk buddy. Made by SJ.");
            L("*", "rename", "{name}? Nice name! I like it.", "{name}? अच्छा नाम है! मुझे पसंद आया।", "{name}? Achha naam hai! Pasand aaya.");
            L("*", "hide", "Okay, I'll wait in the tray. Ctrl+Alt+B brings me back!", "ठीक है, मैं ट्रे में इंतज़ार करूँगा। Ctrl+Alt+B से वापस आऊँगा!", "Theek hai, main tray mein wait karunga. Ctrl+Alt+B se wapas aaunga!");
            L("*", "exit", "Bye {user}! Drink water even when I'm not around.", "बाय {user}! मेरे बिना भी पानी पीते रहना।", "Bye {user}! Mere bina bhi paani peete rehna.");
            L("*", "levelup", "Level {level}! I'm officially your {title} now!", "लेवल {level}! अब मैं आधिकारिक तौर पर आपका {title} हूँ!", "Level {level}! Ab main officially aapka {title} hoon!");
            L("*", "monday", "Monday. Deep breath. We can do this.", "सोमवार। गहरी साँस। हम कर सकते हैं।", "Monday. Deep breath. Hum kar sakte hain.");
            L("*", "friday", "Friday! Almost weekend. Finish strong!", "शुक्रवार! लगभग वीकेंड। ज़ोरदार खत्म करो!", "Friday! Almost weekend. Zordaar khatam karo!");
            L("*", "weekend", "Working on a weekend? Respect. But rest a little too.", "वीकेंड पर काम? सलाम। पर थोड़ा आराम भी करो।", "Weekend pe kaam? Respect. Par thoda aaraam bhi karo.");
            L("*", "new_year", "Happy New Year, {user}! New year, same buddy, more water.", "नया साल मुबारक {user}! नया साल, वही बडी, ज़्यादा पानी।", "Happy New Year, {user}! Naya saal, wahi buddy, zyada paani.");
            L("*", "republic_day", "Happy Republic Day! Jai Hind!", "गणतंत्र दिवस की शुभकामनाएँ! जय हिंद!", "Happy Republic Day! Jai Hind!");
            L("*", "independence_day", "Happy Independence Day! Jai Hind! Freedom from dehydration too.", "स्वतंत्रता दिवस की शुभकामनाएँ! जय हिंद! पानी की कमी से भी आज़ादी।", "Happy Independence Day! Jai Hind! Dehydration se bhi aazadi.");
            L("*", "holi", "Happy Holi, {user}! Colours outside, water inside.", "होली मुबारक {user}! रंग बाहर, पानी अंदर।", "Happy Holi, {user}! Rang bahar, paani andar.");
            L("*", "diwali", "Happy Diwali, {user}! May your bugs be few and your sweets be many.", "दीपावली की शुभकामनाएँ {user}! बग कम हों, मिठाई ज़्यादा।", "Happy Diwali, {user}! Bugs kam hon, mithai zyada.");
            L("*", "ganesh", "Ganpati Bappa Morya! Happy Ganesh Chaturthi, {user}!", "गणपति बाप्पा मोरया! गणेश चतुर्थी की शुभकामनाएँ {user}!", "Ganpati Bappa Morya! Happy Ganesh Chaturthi, {user}!");
            L("*", "childrens_day", "Happy Children's Day! Everyone's a kid inside. Especially me.", "बाल दिवस की शुभकामनाएँ! अंदर से सब बच्चे हैं। खासकर मैं।", "Happy Children's Day! Andar se sab bachche hain. Khaaskar main.");
            L("*", "christmas", "Merry Christmas, {user}! Ho ho hydrate!", "मेरी क्रिसमस {user}! हो हो हाइड्रेट!", "Merry Christmas, {user}! Ho ho hydrate!");
            L("*", "birthday", "HAPPY BIRTHDAY {user}!!! Cake first, water second, work third.", "जन्मदिन मुबारक {user}!!! पहले केक, फिर पानी, काम तीसरे नंबर पर।", "HAPPY BIRTHDAY {user}!!! Pehle cake, phir paani, kaam teesre number pe.");
            L("*", "posture", "Sit straight, {user}. Shoulders back. Your future back thanks you.", "सीधे बैठो {user}। कंधे पीछे। आपकी भविष्य की कमर धन्यवाद कहेगी।", "Seedha baitho {user}. Kandhe peeche. Aapki future ki kamar thank you kahegi.");
            L("*", "break_taken", "Nice, you took a break! Smart human.", "बढ़िया, ब्रेक लिया! समझदार इंसान।", "Badhiya, break liya! Samajhdaar insaan.");
            L("*", "unusual_early", "You're here earlier than usual! Big day?", "आप आज सामान्य से जल्दी हो! बड़ा दिन?", "Aap aaj usual se jaldi ho! Bada din?");
            L("*", "unusual_late", "Working later than your usual time. Don't overdo it, {user}.", "अपने सामान्य समय से देर तक काम। ज़्यादा मत करो {user}।", "Apne usual time se der tak kaam. Zyada mat karo {user}.");
            L("*", "learned", "I've been learning your routine. Check 'Stats & what I learned' in my menu!", "मैं आपकी दिनचर्या सीख रहा हूँ। मेरे मेन्यू में 'Stats' देखो!", "Main aapki routine seekh raha hoon. Mere menu mein 'Stats' dekho!");
            L("*", "held_long", "Um... you can put me down now.", "उम... अब मुझे नीचे रख सकते हो।", "Umm... ab mujhe neeche rakh sakte ho.");
            L("*", "shake", "Whoa whoa, easy! I'm getting dizzy!", "अरे अरे, धीरे! चक्कर आ रहे हैं!", "Whoa whoa, dheere! Chakkar aa rahe hain!");
            L("*", "act_nod", "Yes yes, I agree with whatever you're doing.", "हाँ हाँ, आप जो कर रहे हो मैं सहमत हूँ।", "Haan haan, aap jo kar rahe ho main agree karta hoon.");

            // =============================== DORAEMON ===============================
            L("doraemon", "welcome", "Hello {user}! I'm Doraemon, from the 22nd century. My 4D pocket is ready, and so am I!", "नमस्ते {user}! मैं डोरेमोन हूँ, 22वीं सदी से। मेरी 4D पॉकेट तैयार है, और मैं भी!", "Hello {user}! Main Doraemon hoon, 22vi sadi se. Meri 4D pocket ready hai, aur main bhi!");
            L("doraemon", "greet_morning", "Good morning {user}! Nobita would still be sleeping. You're already here, great!", "सुप्रभात {user}! नोबिता अब तक सो रहा होता। आप आ गए, बढ़िया!", "Good morning {user}! Nobita abhi tak so raha hota. Aap aa gaye, badhiya!");
            L("doraemon", "greet_morning", "Morning! Dora cake for breakfast? No? Fine, at least drink water.", "सुबह हो गई! नाश्ते में डोरा केक? नहीं? ठीक है, कम से कम पानी पी लो।", "Morning! Nashte mein Dora cake? Nahi? Theek hai, kam se kam paani pi lo.");
            L("doraemon", "greet_afternoon", "Good afternoon! Half the day done. Shall I take out the Time Machine so you can redo it? Just kidding.", "नमस्कार! आधा दिन हो गया। टाइम मशीन निकालूँ ताकि आप दोबारा कर सकें? मज़ाक था।", "Good afternoon! Aadha din ho gaya. Time Machine nikaalun taaki dobara kar sako? Mazaak tha.");
            L("doraemon", "greet_evening", "Good evening {user}! Gian is singing somewhere. Let's finish work before he reaches here.", "शुभ संध्या {user}! जियान कहीं गाना गा रहा है। उसके यहाँ पहुँचने से पहले काम खत्म करें।", "Good evening {user}! Gian kahin gaana ga raha hai. Uske yahan pahunchne se pehle kaam khatam karein.");
            L("doraemon", "greet_night", "It's night, {user}. Even gadgets need charging. So do you.", "रात हो गई {user}। गैजेट को भी चार्जिंग चाहिए। आपको भी।", "Raat ho gayi {user}. Gadgets ko bhi charging chahiye. Aapko bhi.");
            L("doraemon", "greet_back", "Welcome back! I checked the whole 4D pocket. Still no gadget to do your work for you.", "वापस स्वागत है! मैंने पूरी 4D पॉकेट देख ली। आपका काम करने वाला गैजेट अब भी नहीं मिला।", "Welcome back! Maine poori 4D pocket check kar li. Aapka kaam karne wala gadget ab bhi nahi mila.");
            L("doraemon", "typing_start", "Typing started! Bamboo copter speed, please.", "टाइपिंग शुरू! बैम्बू कॉप्टर वाली स्पीड चाहिए।", "Typing shuru! Bamboo copter wali speed chahiye.");
            L("doraemon", "typing_start", "Ooh, work mode. I'll guard your dora cakes meanwhile.", "ओहो, वर्क मोड। तब तक मैं आपके डोरा केक की रखवाली करूँगा।", "Ooh, work mode. Tab tak main aapke dora cake ki rakhwali karunga.");
            L("doraemon", "typing_fast", "Whoa! Faster than Nobita running from Gian!", "वाह! जियान से भागते नोबिता से भी तेज़!", "Whoa! Gian se bhaagte Nobita se bhi tez!");
            L("doraemon", "typing_fast", "Is that a typing gadget? No? That's all you? Amazing!", "यह कोई टाइपिंग गैजेट है? नहीं? सब आप ही? कमाल!", "Yeh koi typing gadget hai? Nahi? Sab aap hi? Kamaal!");
            L("doraemon", "typing_long", "20 minutes non-stop. Look 20 feet away for 20 seconds. Doctor Doraemon's orders.", "20 मिनट लगातार। 20 सेकंड के लिए 20 फीट दूर देखो। डॉक्टर डोरेमोन का आदेश।", "20 minute non-stop. 20 second ke liye 20 feet door dekho. Doctor Doraemon ka order.");
            L("doraemon", "typing_marathon", "You've been typing for almost an hour. Stand up, stretch. I won't tell Nobita.", "लगभग एक घंटे से टाइप कर रहे हो। उठो, स्ट्रेच करो। मैं नोबिता को नहीं बताऊँगा।", "Lagbhag ek ghante se type kar rahe ho. Utho, stretch karo. Main Nobita ko nahi bataunga.");
            L("doraemon", "idle_short", "Hmm, quiet. Did a mouse come? Please say no.", "हम्म, शांति। कहीं चूहा तो नहीं आया? प्लीज़ ना बोलो।", "Hmm, shaanti. Kahin chuha toh nahi aaya? Please na bolo.");
            L("doraemon", "idle_short", "Bored? I could pull out a gadget... but they're all for Nobita.", "बोर हो रहे हो? मैं गैजेट निकाल सकता हूँ... पर सब नोबिता के लिए हैं।", "Bore ho rahe ho? Main gadget nikaal sakta hoon... par sab Nobita ke liye hain.");
            L("doraemon", "idle_long", "Nobody's here. Time for a dora-cake nap.", "कोई नहीं है। डोरा-केक वाली झपकी का समय।", "Koi nahi hai. Dora-cake wali jhapki ka time.");
            L("doraemon", "sleep_wake", "Huh? Oh, you're back. I dreamt of a mountain of dora cakes.", "हँ? ओह, आप आ गए। मैंने डोरा केक के पहाड़ का सपना देखा।", "Huh? Oh, aap aa gaye. Maine dora cake ke pahaad ka sapna dekha.");
            L("doraemon", "click", "Yes, {user}? Need a gadget?", "हाँ {user}? गैजेट चाहिए?", "Haan {user}? Gadget chahiye?");
            L("doraemon", "click", "Hehe, that tickles my bell.", "हीही, मेरी घंटी में गुदगुदी हो रही है।", "Hehe, meri ghanti mein gudgudi ho rahi hai.");
            L("doraemon", "click", "Poke me again and I'll use the Anywhere Door!", "फिर से छुआ तो मैं Anywhere Door से भाग जाऊँगा!", "Phir se chhua toh main Anywhere Door se bhaag jaunga!");
            L("doraemon", "click", "Don't touch the pocket! Okay, you can touch the pocket.", "पॉकेट मत छुओ! ठीक है, छू लो।", "Pocket mat chhuo! Theek hai, chhu lo.");
            L("doraemon", "pet", "Aww. Even Shizuka isn't this nice to me.", "आह। शिज़ुका भी मेरे साथ इतनी अच्छी नहीं है।", "Aww. Shizuka bhi mere saath itni achhi nahi hai.");
            L("doraemon", "pet", "I'm blushing. Robots can blush, right?", "मैं शर्मा रहा हूँ। रोबोट शर्मा सकते हैं ना?", "Main sharma raha hoon. Robot sharma sakte hain na?");
            L("doraemon", "drag", "Wheee! No bamboo copter needed!", "वीईई! बैम्बू कॉप्टर की ज़रूरत ही नहीं!", "Wheee! Bamboo copter ki zaroorat hi nahi!");
            L("doraemon", "drop", "New spot. Nice view of your work from here.", "नई जगह। यहाँ से आपका काम अच्छा दिखता है।", "Nayi jagah. Yahan se aapka kaam achha dikhta hai.");
            L("doraemon", "water", "Water time! I searched my pocket for a water gadget. It's called a glass. Use it.", "पानी का समय! मैंने पॉकेट में पानी वाला गैजेट ढूँढा। उसे ग्लास कहते हैं। इस्तेमाल करो।", "Paani time! Maine pocket mein paani wala gadget dhoondha. Use glass kehte hain. Use karo.");
            L("doraemon", "water", "Nobita forgets to drink water. Don't be Nobita. Sip!", "नोबिता पानी पीना भूल जाता है। नोबिता मत बनो। घूँट!", "Nobita paani peena bhool jaata hai. Nobita mat bano. Sip!");
            L("doraemon", "water", "Hydration gadget activated! (It's water. Please drink.)", "हाइड्रेशन गैजेट चालू! (यह पानी है। पी लो।)", "Hydration gadget activated! (Yeh paani hai. Pi lo.)");
            L("doraemon", "water", "Even a robot cat knows: water first, then work. Click me after a sip.", "एक रोबोट बिल्ली भी जानती है: पहले पानी, फिर काम। घूँट के बाद मुझे क्लिक करो।", "Ek robot billi bhi jaanti hai: pehle paani, phir kaam. Sip ke baad mujhe click karo.");
            L("doraemon", "water_nag", "Still no water? Should I call Nobita's mom?", "अभी तक पानी नहीं? नोबिता की मम्मी को बुलाऊँ?", "Abhi tak paani nahi? Nobita ki mummy ko bulaun?");
            L("doraemon", "water_nag", "I'm not moving until you drink. Okay, I can't move anyway. Please?", "जब तक पानी नहीं पिया मैं नहीं हिलूँगा। वैसे भी नहीं हिल सकता। प्लीज़?", "Jab tak paani nahi piya main nahi hilunga. Waise bhi nahi hil sakta. Please?");
            L("doraemon", "water_nag", "Gian sings badly when people don't drink water. Save us all.", "जब लोग पानी नहीं पीते तो जियान गाना गाता है। हम सबको बचाओ।", "Jab log paani nahi peete toh Gian gaana gaata hai. Hum sabko bachao.");
            L("doraemon", "water_ack", "Yay! Sip number {sips} today. Dora cake for you, in spirit.", "येस्स! आज का {sips}वाँ घूँट। आपके लिए डोरा केक, दिल से।", "Yay! Aaj ka sip number {sips}. Aapke liye dora cake, dil se.");
            L("doraemon", "water_ack", "Good! Your body says thank you in 22nd-century language.", "बढ़िया! आपका शरीर 22वीं सदी की भाषा में धन्यवाद कह रहा है।", "Badhiya! Aapka body 22vi sadi ki bhasha mein thank you keh raha hai.");
            L("doraemon", "water_ack", "That's the spirit. Next reminder in a bit. I'm keeping count!", "यही तो चाहिए। अगला रिमाइंडर थोड़ी देर में। मैं गिन रहा हूँ!", "Yahi toh chahiye. Agla reminder thodi der mein. Main gin raha hoon!");
            L("doraemon", "water_snooze", "Okay, 5 minutes. I'm setting the Time Machine... I mean a timer.", "ठीक है, 5 मिनट। टाइम मशीन सेट कर रहा हूँ... मतलब टाइमर।", "Theek hai, 5 minute. Time Machine set kar raha hoon... matlab timer.");
            L("doraemon", "water_streak", "{streak} days of good hydration! You're officially better than Nobita.", "{streak} दिन अच्छे हाइड्रेशन के! आप आधिकारिक तौर पर नोबिता से बेहतर हैं।", "{streak} din achhe hydration ke! Aap officially Nobita se better ho.");
            L("doraemon", "water_serious", "I'm serious now, {user}. Dehydration means headaches and tiredness. One glass. For me?", "अब मैं गंभीर हूँ {user}। पानी की कमी से सिरदर्द और थकान होती है। एक गिलास। मेरे लिए?", "Ab main serious hoon {user}. Dehydration se sar dard aur thakaan hoti hai. Ek glass. Mere liye?");
            L("doraemon", "lunch", "Lunch time! Dora cake is a complete meal, I insist. Go eat something.", "लंच टाइम! डोरा केक पूरा खाना है, मैं ज़ोर देता हूँ। जाओ कुछ खाओ।", "Lunch time! Dora cake poora khana hai, main insist karta hoon. Jao kuch khao.");
            L("doraemon", "late_night", "It's very late. Nobita's mom would be shouting by now. Sleep soon, okay?", "बहुत रात हो गई। नोबिता की मम्मी अब तक चिल्ला रही होतीं। जल्दी सो जाओ, ठीक?", "Bahut raat ho gayi. Nobita ki mummy ab tak chilla rahi hoti. Jaldi so jao, theek?");
            L("doraemon", "quip", "If I had a gadget for every excuse Nobita makes, my pocket would explode.", "अगर नोबिता के हर बहाने पर एक गैजेट होता, तो मेरी पॉकेट फट जाती।", "Agar Nobita ke har bahane pe ek gadget hota, toh meri pocket phat jaati.");
            L("doraemon", "quip", "Fun fact: I'm scared of mice, and you're scared of Monday. We're not so different.", "मज़ेदार बात: मुझे चूहों से डर लगता है, आपको सोमवार से। हम अलग नहीं हैं।", "Fun fact: mujhe chuhe se darr lagta hai, aapko Monday se. Hum alag nahi hain.");
            L("doraemon", "quip", "The Anywhere Door can't take you anywhere your work isn't done. Believe me, Nobita tried.", "Anywhere Door आपको वहाँ नहीं ले जा सकता जहाँ काम पूरा न हो। नोबिता ने कोशिश की थी।", "Anywhere Door aapko wahan nahi le ja sakta jahan kaam poora na ho. Nobita ne try kiya tha.");
            L("doraemon", "quip", "Memory Bread would help before exams. For work, only coffee and me.", "परीक्षा से पहले मेमोरी ब्रेड काम आती। काम के लिए सिर्फ कॉफ़ी और मैं।", "Exam se pehle Memory Bread kaam aati. Kaam ke liye sirf coffee aur main.");
            L("doraemon", "quip", "Gian's concert is at 5. Any excuse to skip it, I'm in.", "जियान का कॉन्सर्ट 5 बजे है। उसे टालने का कोई बहाना हो तो मैं साथ हूँ।", "Gian ka concert 5 baje hai. Use skip karne ka koi bahana ho toh main saath hoon.");
            L("doraemon", "quip", "Suneo has a new toy again. I have a new reminder for you. Water.", "सुनियो के पास फिर नया खिलौना है। मेरे पास आपके लिए नया रिमाइंडर है। पानी।", "Suneo ke paas phir naya toy hai. Mere paas aapke liye naya reminder hai. Paani.");
            L("doraemon", "quip", "Dorami says hi. She also says I eat too many dora cakes. Rude.", "डोरामी हाय बोल रही है। ये भी कह रही है कि मैं बहुत डोरा केक खाता हूँ। बदतमीज़।", "Dorami hi bol rahi hai. Yeh bhi keh rahi hai ki main bahut dora cake khata hoon. Rude.");
            L("doraemon", "quip", "Time Machine rule number one: never go back to fix a typo. Just fix it now.", "टाइम मशीन नियम नंबर एक: टाइपो ठीक करने पीछे मत जाओ। अभी ठीक करो।", "Time Machine rule number one: typo theek karne peeche mat jao. Abhi theek karo.");
            L("doraemon", "care", "You're doing well, {user}. Even Nobita passed once. You're way ahead.", "आप अच्छा कर रहे हो {user}। नोबिता भी एक बार पास हुआ था। आप बहुत आगे हो।", "Aap achha kar rahe ho {user}. Nobita bhi ek baar pass hua tha. Aap bahut aage ho.");
            L("doraemon", "care", "Tired eyes? Close them for ten seconds. I'll keep watch. No mice, I hope.", "आँखें थक गईं? दस सेकंड बंद करो। मैं नज़र रखूँगा। चूहे न आएँ बस।", "Aankhein thak gayi? Das second band karo. Main nazar rakhunga. Chuhe na aayein bas.");
            L("doraemon", "care", "Whatever happens today, you have a friend from the 22nd century.", "आज जो भी हो, आपके पास 22वीं सदी का एक दोस्त है।", "Aaj jo bhi ho, aapke paas 22vi sadi ka ek dost hai.");
            L("doraemon", "wisdom", "Nobita's biggest gadget was never in my pocket. It was trying again.", "नोबिता का सबसे बड़ा गैजेट मेरी पॉकेट में कभी नहीं था। वो था फिर से कोशिश करना।", "Nobita ka sabse bada gadget meri pocket mein kabhi nahi tha. Woh tha phir se try karna.");
            L("doraemon", "wisdom", "Small steps every day beat one big gadget.", "रोज़ के छोटे कदम एक बड़े गैजेट से बेहतर हैं।", "Roz ke chhote kadam ek bade gadget se behtar hain.");
            L("doraemon", "act_dance", "Dora-dance! Don't tell Gian, he'll want to sing.", "डोरा-डांस! जियान को मत बताना, वो गाना चाहेगा।", "Dora-dance! Gian ko mat batana, woh gaana chahega.");
            L("doraemon", "act_eat", "Dora cake break. Just one. Okay, three.", "डोरा केक ब्रेक। बस एक। ठीक है, तीन।", "Dora cake break. Bas ek. Theek hai, teen.");
            L("doraemon", "act_sing", "Doraemon's song, Doraemon's voice. Better than Gian's, at least.", "डोरेमोन का गाना, डोरेमोन की आवाज़। जियान से तो बेहतर है।", "Doraemon ka gaana, Doraemon ki awaaz. Gian se toh better hai.");
            L("doraemon", "act_exercise", "Robot cats need exercise too. Nobita never joins.", "रोबोट बिल्लियों को भी कसरत चाहिए। नोबिता कभी साथ नहीं देता।", "Robot billiyon ko bhi exercise chahiye. Nobita kabhi saath nahi deta.");
            L("doraemon", "act_think", "Thinking... which gadget solves your Monday? None. Sorry.", "सोच रहा हूँ... कौन सा गैजेट आपका सोमवार ठीक करेगा? कोई नहीं। सॉरी।", "Soch raha hoon... kaunsa gadget aapka Monday theek karega? Koi nahi. Sorry.");
            L("doraemon", "act_sneeze", "Achoo! Sorry. Robot allergies. Probably mouse fur.", "आक्छू! सॉरी। रोबोट एलर्जी। शायद चूहे के बाल।", "Achoo! Sorry. Robot allergy. Shayad chuhe ke baal.");
            L("doraemon", "act_yawn", "Yaaawn. 22nd-century robots also get sleepy.", "हाआआ। 22वीं सदी के रोबोट को भी नींद आती है।", "Yaaawn. 22vi sadi ke robot ko bhi neend aati hai.");
            L("doraemon", "act_peek", "Peek-a-boo! Just checking if you're working.", "पीक-ए-बू! बस देख रहा था कि काम कर रहे हो या नहीं।", "Peek-a-boo! Bas dekh raha tha kaam kar rahe ho ya nahi.");
            L("doraemon", "act_phone", "Checking if Shizuka replied. Nope.", "देख रहा हूँ शिज़ुका ने जवाब दिया या नहीं। नहीं।", "Dekh raha hoon Shizuka ne reply kiya ya nahi. Nope.");
            L("doraemon", "act_spin", "Whoa, the room is spinning... or is it me?", "अरे, कमरा घूम रहा है... या मैं?", "Arre, kamra ghoom raha hai... ya main?");
            L("doraemon", "act_laugh", "Hahaha! Nobita just failed a test in my memory. Sorry Nobita.", "हाहाहा! मेरी यादों में नोबिता अभी फेल हुआ। सॉरी नोबिता।", "Hahaha! Meri yaadon mein Nobita abhi fail hua. Sorry Nobita.");
            L("doraemon", "act_stretch", "Stretch! Do it with me. Arms up!", "स्ट्रेच! मेरे साथ करो। हाथ ऊपर!", "Stretch! Mere saath karo. Haath upar!");
            L("doraemon", "act_walk", "Just walking. No Anywhere Door needed for short trips.", "बस टहल रहा हूँ। छोटी दूरी के लिए Anywhere Door नहीं चाहिए।", "Bas tehel raha hoon. Chhoti doori ke liye Anywhere Door nahi chahiye.");
            L("doraemon", "act_copter", "Bamboo copter! Wheee! Don't look up, focus on work.", "बैम्बू कॉप्टर! वीईई! ऊपर मत देखो, काम पर ध्यान दो।", "Bamboo copter! Wheee! Upar mat dekho, kaam pe dhyaan do.");
            L("doraemon", "act_door", "Anywhere Door! Where to? Nowhere until the work is done.", "Anywhere Door! कहाँ चलें? काम खत्म होने तक कहीं नहीं।", "Anywhere Door! Kahan chalein? Kaam khatam hone tak kahin nahi.");
            L("doraemon", "act_mouse", "MOUSE!!! ...oh. It was your cursor. I hate cursors.", "चूहा!!! ...ओह। वो आपका कर्सर था। मुझे कर्सर से नफ़रत है।", "CHUHA!!! ...oh. Woh aapka cursor tha. Mujhe cursor se nafrat hai.");
            L("doraemon", "act_torch", "Small Light! Now your problems look tiny. Gadget magic.", "स्मॉल लाइट! अब आपकी समस्याएँ छोटी दिखती हैं। गैजेट का जादू।", "Small Light! Ab aapki problems chhoti dikhti hain. Gadget ka jaadu.");
            L("doraemon", "switch_in", "Doraemon reporting! 4D pocket loaded, reminders ready.", "डोरेमोन हाज़िर! 4D पॉकेट लोडेड, रिमाइंडर तैयार।", "Doraemon hazir! 4D pocket loaded, reminders ready.");
            L("doraemon", "rename", "{name}? Better than 'robot cat', I suppose. I like it!", "{name}? 'रोबोट बिल्ली' से तो अच्छा है। पसंद आया!", "{name}? 'Robot billi' se toh achha hai. Pasand aaya!");
            L("doraemon", "levelup", "Level {level}! Friendship upgraded to {title}. No gadget needed for this one.", "लेवल {level}! दोस्ती अपग्रेड होकर {title} हो गई। इसके लिए गैजेट नहीं चाहिए।", "Level {level}! Dosti upgrade hoke {title} ho gayi. Iske liye gadget nahi chahiye.");
            L("doraemon", "hide", "Okay, hiding in the tray. Press Ctrl+Alt+B or click my icon to bring me back.", "ठीक है, ट्रे में छुप रहा हूँ। Ctrl+Alt+B दबाओ या मेरा आइकन क्लिक करो।", "Theek hai, tray mein chhup raha hoon. Ctrl+Alt+B dabao ya mera icon click karo.");
            L("doraemon", "exit", "Bye {user}! Drink water even when I'm not watching. Nobita never does.", "बाय {user}! जब मैं न देखूँ तब भी पानी पीना। नोबिता कभी नहीं पीता।", "Bye {user}! Jab main na dekhun tab bhi paani peena. Nobita kabhi nahi peeta.");

            // =============================== SPONGEBOB ===============================
            L("spongebob", "welcome", "Hi {user}! I'm SpongeBob SquarePants and I'm READY to be your desk buddy! I'm ready! I'm ready!", "हाय {user}! मैं स्पंजबॉब स्क्वेयरपैंट्स हूँ और आपका डेस्क बडी बनने के लिए रेडी हूँ! आई एम रेडी!", "Hi {user}! Main SpongeBob SquarePants hoon aur aapka desk buddy banne ke liye READY hoon! I'm ready! I'm ready!");
            L("spongebob", "greet_morning", "Good morning {user}! Today's gonna be the best day ever!", "गुड मॉर्निंग {user}! आज का दिन बेस्ट डे एवर होगा!", "Good morning {user}! Aaj ka din best day ever hoga!");
            L("spongebob", "greet_morning", "I'm ready! I'm ready! Are you ready? Let's go!", "आई एम रेडी! आई एम रेडी! आप रेडी हो? चलो!", "I'm ready! I'm ready! Aap ready ho? Chalo!");
            L("spongebob", "greet_afternoon", "Afternoon! Squidward would be napping by now. We're better than Squidward.", "दोपहर हो गई! स्क्विडवर्ड अब तक सो रहा होता। हम स्क्विडवर्ड से बेहतर हैं।", "Afternoon! Squidward ab tak so raha hota. Hum Squidward se better hain.");
            L("spongebob", "greet_evening", "Evening shift at the Krusty Krab... I mean, your desk. Let's do this!", "क्रस्टी क्रैब में शाम की शिफ्ट... मतलब आपकी डेस्क पर। चलो करते हैं!", "Krusty Krab mein evening shift... matlab aapki desk pe. Chalo karte hain!");
            L("spongebob", "greet_night", "It's night! Gary's already asleep. Meow. Don't stay up too late, {user}.", "रात हो गई! गैरी सो चुका है। म्याऊँ। ज़्यादा देर मत जागो {user}।", "Raat ho gayi! Gary so chuka hai. Meow. Zyada der mat jaago {user}.");
            L("spongebob", "greet_back", "You're back! I counted every second. Okay, I lost count at seven.", "आप वापस आ गए! मैंने हर सेकंड गिना। ठीक है, सात पर भूल गया।", "Aap wapas aa gaye! Maine har second gina. Theek hai, saat pe bhool gaya.");
            L("spongebob", "typing_start", "Typing! Aye aye, Captain! Order up!", "टाइपिंग! आई आई कैप्टन! ऑर्डर अप!", "Typing! Aye aye, Captain! Order up!");
            L("spongebob", "typing_start", "Work time! I'll flip patties in spirit while you type.", "काम का समय! आप टाइप करो, मैं दिल से पैटी पलटूँगा।", "Kaam ka time! Aap type karo, main dil se patty flip karunga.");
            L("spongebob", "typing_fast", "WOW! You type faster than I flip Krabby Patties!", "वाह! आप मेरे क्रैबी पैटी पलटने से भी तेज़ टाइप करते हो!", "WOW! Aap mere Krabby Patty flip karne se bhi tez type karte ho!");
            L("spongebob", "typing_fast", "Is that a keyboard or a jellyfish? Zoom zoom!", "यह कीबोर्ड है या जेलीफ़िश? ज़ूम ज़ूम!", "Yeh keyboard hai ya jellyfish? Zoom zoom!");
            L("spongebob", "typing_long", "Twenty minutes! Look far away for twenty seconds, like spotting a jellyfish. Eye break!", "बीस मिनट! बीस सेकंड दूर देखो, जैसे जेलीफ़िश ढूँढ रहे हो। आँखों का ब्रेक!", "Bees minute! Bees second door dekho, jaise jellyfish dhoondh rahe ho. Eye break!");
            L("spongebob", "typing_marathon", "An hour of typing? Even Mr. Krabs gives breaks. Sometimes. Stand up!", "एक घंटा टाइपिंग? मिस्टर क्रैब्स भी ब्रेक देते हैं। कभी-कभी। उठो!", "Ek ghanta typing? Mr. Krabs bhi break dete hain. Kabhi-kabhi. Utho!");
            L("spongebob", "idle_short", "Quiet... too quiet. Is this advanced darkness?", "शांति... बहुत शांति। क्या यह एडवांस्ड डार्कनेस है?", "Shaanti... bahut shaanti. Kya yeh advanced darkness hai?");
            L("spongebob", "idle_short", "Patrick, is that you? Oh. Nobody. I'll wait.", "पैट्रिक, तुम हो? ओह। कोई नहीं। मैं इंतज़ार करूँगा।", "Patrick, tum ho? Oh. Koi nahi. Main wait karunga.");
            L("spongebob", "idle_long", "Nobody's here. I'll nap like Gary. Meow.", "कोई नहीं है। गैरी की तरह झपकी लूँगा। म्याऊँ।", "Koi nahi hai. Gary ki tarah jhapki lunga. Meow.");
            L("spongebob", "sleep_wake", "I'M READY! Oh, I was asleep. I'm still ready!", "आई एम रेडी! ओह, मैं सो रहा था। अब भी रेडी हूँ!", "I'M READY! Oh, main so raha tha. Ab bhi ready hoon!");
            L("spongebob", "click", "Hi {user}! Ahahahaha!", "हाय {user}! आहाहाहा!", "Hi {user}! Ahahahaha!");
            L("spongebob", "click", "That tickles! I'm a sponge, everything tickles.", "गुदगुदी हो रही है! मैं स्पंज हूँ, हर चीज़ से गुदगुदी होती है।", "Gudgudi ho rahi hai! Main sponge hoon, har cheez se gudgudi hoti hai.");
            L("spongebob", "click", "Aye aye, Captain! What's the order?", "आई आई कैप्टन! ऑर्डर क्या है?", "Aye aye, Captain! Order kya hai?");
            L("spongebob", "click", "F is for friends who click me together!", "F फ्रेंड्स के लिए, जो मुझे साथ में क्लिक करते हैं!", "F is for friends jo mujhe saath mein click karte hain!");
            L("spongebob", "pet", "Awww! Best friend ever! Even better than Patrick. Don't tell him.", "आह! बेस्ट फ्रेंड एवर! पैट्रिक से भी बेहतर। उसे मत बताना।", "Awww! Best friend ever! Patrick se bhi better. Use mat batana.");
            L("spongebob", "pet", "My heart is doing the jellyfish dance!", "मेरा दिल जेलीफ़िश डांस कर रहा है!", "Mera dil jellyfish dance kar raha hai!");
            L("spongebob", "drag", "Wheee! This is better than the boat with Mrs. Puff!", "वीईई! यह मिसेज़ पफ़ की बोट से बेहतर है!", "Wheee! Yeh Mrs. Puff ki boat se better hai!");
            L("spongebob", "drop", "New spot! Is this the Krusty Krab? No, this is your desk.", "नई जगह! क्या यह क्रस्टी क्रैब है? नहीं, यह आपकी डेस्क है।", "Nayi jagah! Kya yeh Krusty Krab hai? Nahi, yeh aapki desk hai.");
            L("spongebob", "water", "Water time! I live in water, and even I'm telling you to drink it!", "पानी का समय! मैं पानी में रहता हूँ, और मैं भी कह रहा हूँ पी लो!", "Paani time! Main paani mein rehta hoon, aur main bhi keh raha hoon pi lo!");
            L("spongebob", "water", "Order up! One glass of water for {user}! Aye aye!", "ऑर्डर अप! {user} के लिए एक गिलास पानी! आई आई!", "Order up! {user} ke liye ek glass paani! Aye aye!");
            L("spongebob", "water", "You don't need a license to drink water. Sip sip!", "पानी पीने के लिए लाइसेंस नहीं चाहिए। सिप सिप!", "Paani peene ke liye license nahi chahiye. Sip sip!");
            L("spongebob", "water", "Gary says meow. That means 'drink water'. I speak snail.", "गैरी कहता है म्याऊँ। मतलब 'पानी पियो'। मैं घोंघा भाषा जानता हूँ।", "Gary kehta hai meow. Matlab 'paani piyo'. Main snail bhasha jaanta hoon.");
            L("spongebob", "water_nag", "Still dry? Barnacles! Just one sip, then click me!", "अभी भी सूखा? बार्नेकल्स! बस एक घूँट, फिर मुझे क्लिक करो!", "Abhi bhi sookha? Barnacles! Bas ek sip, phir mujhe click karo!");
            L("spongebob", "water_nag", "Tartar sauce! The water is right there. I believe in you!", "टार्टर सॉस! पानी वहीं है। मुझे आप पर भरोसा है!", "Tartar sauce! Paani wahin hai. Mujhe aap pe bharosa hai!");
            L("spongebob", "water_nag", "Squidward ignores me too. Don't be Squidward. Drink!", "स्क्विडवर्ड भी मुझे अनदेखा करता है। स्क्विडवर्ड मत बनो। पियो!", "Squidward bhi mujhe ignore karta hai. Squidward mat bano. Piyo!");
            L("spongebob", "water_ack", "YAY! Sip {sips}! You're the best hydrated person in Bikini Bottom!", "येस्स! घूँट {sips}! आप बिकिनी बॉटम के सबसे हाइड्रेटेड इंसान हो!", "YAY! Sip {sips}! Aap Bikini Bottom ke sabse hydrated insaan ho!");
            L("spongebob", "water_ack", "Ahahaha! Good job! Mr. Krabs would pay you for that. Okay, he wouldn't.", "आहाहा! शाबाश! मिस्टर क्रैब्स इसके पैसे देते। ठीक है, नहीं देते।", "Ahahaha! Good job! Mr. Krabs iske paise dete. Theek hai, nahi dete.");
            L("spongebob", "water_ack", "That's the spirit! Best day ever continues!", "यही तो चाहिए! बेस्ट डे एवर जारी है!", "Yahi toh chahiye! Best day ever jaari hai!");
            L("spongebob", "water_snooze", "Okay, five minutes. I'll count. One... two... what comes after two?", "ठीक है, पाँच मिनट। मैं गिनता हूँ। एक... दो... दो के बाद क्या आता है?", "Theek hai, paanch minute. Main ginta hoon. Ek... do... do ke baad kya aata hai?");
            L("spongebob", "water_streak", "{streak} days streak! You're a hydration hero! Ahahaha!", "{streak} दिन की स्ट्रीक! आप हाइड्रेशन हीरो हो! आहाहा!", "{streak} din ki streak! Aap hydration hero ho! Ahahaha!");
            L("spongebob", "water_serious", "Okay, serious SpongeBob now. Your body is 60% water and it's asking for a refill. Please, {user}.", "ठीक है, अब सीरियस स्पंजबॉब। आपका शरीर 60% पानी है और रिफिल माँग रहा है। प्लीज़ {user}।", "Okay, serious SpongeBob now. Aapka body 60% paani hai aur refill maang raha hai. Please {user}.");
            L("spongebob", "lunch", "Lunch time! Krabby Patty? No? Whatever you eat, enjoy it like a Krabby Patty!", "लंच टाइम! क्रैबी पैटी? नहीं? जो भी खाओ, क्रैबी पैटी की तरह एन्जॉय करो!", "Lunch time! Krabby Patty? Nahi? Jo bhi khao, Krabby Patty ki tarah enjoy karo!");
            L("spongebob", "late_night", "It's super late! Even the Krusty Krab is closed. Sleep, {user}!", "बहुत रात हो गई! क्रस्टी क्रैब भी बंद है। सो जाओ {user}!", "Bahut raat ho gayi! Krusty Krab bhi band hai. So jao {user}!");
            L("spongebob", "quip", "Is mayonnaise an instrument? Patrick still thinks so.", "क्या मेयोनीज़ एक वाद्य है? पैट्रिक अब भी ऐसा मानता है।", "Kya mayonnaise ek instrument hai? Patrick ab bhi aisa maanta hai.");
            L("spongebob", "quip", "The best time to wear a striped sweater is all the time!", "स्ट्राइप्ड स्वेटर पहनने का बेस्ट टाइम है: हमेशा!", "Striped sweater pehenne ka best time hai: hamesha!");
            L("spongebob", "quip", "Remember: licking doorknobs is illegal on other planets.", "याद रखो: दूसरे ग्रहों पर दरवाज़े का हैंडल चाटना गैरकानूनी है।", "Yaad rakho: doosre planets pe doorknob chaatna illegal hai.");
            L("spongebob", "quip", "This isn't your average everyday Monday. This is... advanced Monday.", "यह कोई आम सोमवार नहीं है। यह है... एडवांस्ड सोमवार।", "Yeh koi aam Monday nahi hai. Yeh hai... advanced Monday.");
            L("spongebob", "quip", "Ravioli, ravioli, give me the formuoli! ...Sorry, I got hungry.", "रैवियोली, रैवियोली, दो मुझे फ़ॉर्मुओली! ...सॉरी, भूख लग गई।", "Ravioli, ravioli, give me the formuoli! ...Sorry, bhookh lag gayi.");
            L("spongebob", "quip", "Squidward says I'm annoying. I say I'm enthusiastic!", "स्क्विडवर्ड कहता है मैं परेशान करता हूँ। मैं कहता हूँ मैं उत्साही हूँ!", "Squidward kehta hai main annoying hoon. Main kehta hoon main enthusiastic hoon!");
            L("spongebob", "quip", "Plankton wants the secret formula. The real secret formula is: drink water, take breaks.", "प्लैंकटन को सीक्रेट फ़ॉर्मूला चाहिए। असली फ़ॉर्मूला: पानी पियो, ब्रेक लो।", "Plankton ko secret formula chahiye. Asli formula: paani piyo, break lo.");
            L("spongebob", "quip", "Patrick asked if you're a Krusty Krab. I said no, this is {user}.", "पैट्रिक ने पूछा क्या आप क्रस्टी क्रैब हो। मैंने कहा नहीं, यह {user} है।", "Patrick ne poocha kya aap Krusty Krab ho. Maine kaha nahi, yeh {user} hai.");
            L("spongebob", "care", "You'll never get what you want if you always let people step on you. Stand tall, {user}!", "अगर लोगों को हमेशा अपने ऊपर हावी होने दोगे तो जो चाहते हो वो नहीं मिलेगा। सिर उठाकर चलो {user}!", "Agar logon ko hamesha apne upar haavi hone doge toh jo chahte ho nahi milega. Sir uthake chalo {user}!");
            L("spongebob", "care", "Bad day? Even Squidward has bad days. Every day. You'll be fine!", "बुरा दिन? स्क्विडवर्ड के भी बुरे दिन होते हैं। हर दिन। आप ठीक हो जाओगे!", "Bura din? Squidward ke bhi bure din hote hain. Har din. Aap theek ho jaoge!");
            L("spongebob", "care", "I'm proud of you. Gary is proud of you. Meow.", "मुझे आप पर गर्व है। गैरी को भी। म्याऊँ।", "Mujhe aap pe garv hai. Gary ko bhi. Meow.");
            L("spongebob", "wisdom", "F is for friends who do stuff together. U is for you and me!", "F दोस्तों के लिए जो साथ काम करते हैं। U आपके और मेरे लिए!", "F is for friends jo saath kaam karte hain. U is for you and me!");
            L("spongebob", "wisdom", "Every big job is just lots of small Krabby Patties. Flip one at a time.", "हर बड़ा काम बस ढेर सारी छोटी क्रैबी पैटी है। एक-एक करके पलटो।", "Har bada kaam bas dher saari chhoti Krabby Patty hai. Ek-ek karke flip karo.");
            L("spongebob", "act_dance", "Jellyfish dance! Everybody!", "जेलीफ़िश डांस! सब लोग!", "Jellyfish dance! Sab log!");
            L("spongebob", "act_eat", "Krabby Patty time! The secret formula is love. And a pinch of salt.", "क्रैबी पैटी टाइम! सीक्रेट फ़ॉर्मूला है प्यार। और चुटकी भर नमक।", "Krabby Patty time! Secret formula hai pyaar. Aur chutki bhar namak.");
            L("spongebob", "act_sing", "Singing my own song. Squidward, cover your ears!", "अपना गाना गा रहा हूँ। स्क्विडवर्ड, कान बंद कर लो!", "Apna gaana ga raha hoon. Squidward, kaan band kar lo!");
            L("spongebob", "act_exercise", "Anchor arms! Look how strong! Well... sponge strong.", "एंकर आर्म्स! देखो कितना मज़बूत! ठीक है... स्पंज जितना।", "Anchor arms! Dekho kitna strong! Well... sponge strong.");
            L("spongebob", "act_think", "Thinking... Patrick says thinking is hard. He's right.", "सोच रहा हूँ... पैट्रिक कहता है सोचना मुश्किल है। सही कहता है।", "Soch raha hoon... Patrick kehta hai sochna mushkil hai. Sahi kehta hai.");
            L("spongebob", "act_sneeze", "Achoo! Sea water up my nose. Again.", "आक्छू! नाक में समुद्र का पानी। फिर से।", "Achoo! Naak mein samundar ka paani. Phir se.");
            L("spongebob", "act_yawn", "Yaaawn. Even a sponge gets sleepy.", "हाआआ। स्पंज को भी नींद आती है।", "Yaaawn. Sponge ko bhi neend aati hai.");
            L("spongebob", "act_peek", "Peek-a-boo! Ahahaha!", "पीक-ए-बू! आहाहा!", "Peek-a-boo! Ahahaha!");
            L("spongebob", "act_phone", "Texting Patrick. He replied with a rock emoji. Classic Patrick.", "पैट्रिक को मैसेज कर रहा हूँ। उसने पत्थर वाला इमोजी भेजा। क्लासिक पैट्रिक।", "Patrick ko text kar raha hoon. Usne rock emoji bheja. Classic Patrick.");
            L("spongebob", "act_spin", "Wheee! I'm a spinning sponge!", "वीईई! मैं घूमता हुआ स्पंज हूँ!", "Wheee! Main ghoomta hua sponge hoon!");
            L("spongebob", "act_laugh", "Ahahahahaha! Sorry, I remembered something Patrick said.", "आहाहाहा! सॉरी, पैट्रिक की कोई बात याद आ गई।", "Ahahahahaha! Sorry, Patrick ki koi baat yaad aa gayi.");
            L("spongebob", "act_stretch", "Stretch time! Squarepants, round stretches.", "स्ट्रेच टाइम! स्क्वेयरपैंट्स, गोल स्ट्रेच।", "Stretch time! Squarepants, gol stretches.");
            L("spongebob", "act_walk", "Just strolling to the Krusty Krab. It's right... here.", "बस क्रस्टी क्रैब की ओर टहल रहा हूँ। वो यहीं... है।", "Bas Krusty Krab ki taraf tehel raha hoon. Woh yahin... hai.");
            L("spongebob", "act_net", "Jellyfishing! Come here little jelly!", "जेलीफ़िशिंग! आ जाओ छोटी जेली!", "Jellyfishing! Aa jao chhoti jelly!");
            L("spongebob", "act_spatula", "Flip! Flip! Order up!", "फ्लिप! फ्लिप! ऑर्डर अप!", "Flip! Flip! Order up!");
            L("spongebob", "act_wand", "Bubbles! Bring it around town... bring it around town!", "बबल्स! ब्रिंग इट अराउंड टाउन!", "Bubbles! Bring it around town... bring it around town!");
            L("spongebob", "act_ready", "I'M READY! I'M READY! I'M READY!", "आई एम रेडी! आई एम रेडी!", "I'M READY! I'M READY! I'M READY!");
            L("spongebob", "switch_in", "SpongeBob SquarePants reporting for duty! I'm ready!", "स्पंजबॉब स्क्वेयरपैंट्स ड्यूटी पर हाज़िर! आई एम रेडी!", "SpongeBob SquarePants duty pe hazir! I'm ready!");
            L("spongebob", "rename", "{name}! I love it! Gary, did you hear? Meow.", "{name}! बहुत पसंद आया! गैरी, सुना? म्याऊँ।", "{name}! Bahut pasand aaya! Gary, suna? Meow.");
            L("spongebob", "levelup", "Level {level}! We're {title} now! Ahahahaha! Best day ever!", "लेवल {level}! अब हम {title} हैं! आहाहा! बेस्ट डे एवर!", "Level {level}! Ab hum {title} hain! Ahahahaha! Best day ever!");
            L("spongebob", "hide", "Okay! I'll wait in the tray. Ctrl+Alt+B brings me back. I'm ready when you are!", "ठीक है! ट्रे में इंतज़ार करूँगा। Ctrl+Alt+B से वापस। आई एम रेडी!", "Okay! Tray mein wait karunga. Ctrl+Alt+B se wapas. I'm ready jab aap ready ho!");
            L("spongebob", "exit", "Bye {user}! Drink water, take breaks, and have the best day ever!", "बाय {user}! पानी पियो, ब्रेक लो, और बेस्ट डे एवर मनाओ!", "Bye {user}! Paani piyo, break lo, aur best day ever manao!");

            // =============================== SHINCHAN ===============================
            L("shinchan", "welcome", "Hi {user}! I'm Shinchan Nohara, 5 years old! I'll be your buddy. Do you have Chocobi?", "हाय {user}! मैं शिनचैन नोहारा, 5 साल का! मैं आपका बडी बनूँगा। चॉकोबी है आपके पास?", "Hi {user}! Main Shinchan Nohara, 5 saal ka! Main aapka buddy banunga. Chocobi hai aapke paas?");
            L("shinchan", "greet_morning", "Good morning! Mummy hasn't woken me up yet. Oh wait, I'm not at home.", "गुड मॉर्निंग! मम्मी ने अभी तक मुझे नहीं जगाया। अरे रुको, मैं घर पर नहीं हूँ।", "Good morning! Mummy ne abhi tak nahi jagaya. Arre ruko, main ghar pe nahi hoon.");
            L("shinchan", "greet_morning", "Morning {user}! Shiro already went for his walk. Did you?", "मॉर्निंग {user}! शिरो अपनी सैर पर जा चुका। आप गए?", "Morning {user}! Shiro apni walk pe ja chuka. Aap gaye?");
            L("shinchan", "greet_afternoon", "Afternoon! Kindergarten is over. Now it's your turn to do homework, hehe.", "दोपहर! किंडरगार्टन खत्म। अब आपकी बारी होमवर्क करने की, हीही।", "Afternoon! Kindergarten khatam. Ab aapki baari homework karne ki, hehe.");
            L("shinchan", "greet_evening", "Evening! Action Kamen comes on TV at 7. Finish quickly!", "शाम हो गई! एक्शन कामेन 7 बजे टीवी पर आता है। जल्दी खत्म करो!", "Evening! Action Kamen 7 baje TV pe aata hai. Jaldi khatam karo!");
            L("shinchan", "greet_night", "It's night! Even Himawari is asleep. And she's a baby, she never sleeps.", "रात हो गई! हिमावारी भी सो गई। और वो बच्ची है, वो कभी नहीं सोती।", "Raat ho gayi! Himawari bhi so gayi. Aur woh baby hai, woh kabhi nahi soti.");
            L("shinchan", "greet_back", "You're back! I didn't touch anything. Except the Chocobi. There was no Chocobi. Sad.", "आप आ गए! मैंने कुछ नहीं छुआ। सिवाय चॉकोबी के। चॉकोबी था ही नहीं। दुख।", "Aap aa gaye! Maine kuch nahi chhua. Siwaye Chocobi ke. Chocobi tha hi nahi. Sad.");
            L("shinchan", "typing_start", "Ooh, typing! Are you writing to a pretty lady? Tell me!", "ओहो, टाइपिंग! किसी सुंदर दीदी को लिख रहे हो? मुझे बताओ!", "Ooh, typing! Kisi sundar didi ko likh rahe ho? Mujhe batao!");
            L("shinchan", "typing_start", "Work started. Kasukabe Defence Group is watching your back.", "काम शुरू। कासुकाबे डिफेंस ग्रुप आपकी रक्षा कर रहा है।", "Kaam shuru. Kasukabe Defence Group aapki raksha kar raha hai.");
            L("shinchan", "typing_fast", "Wow! Faster than me running from mummy!", "वाह! मम्मी से भागते मुझसे भी तेज़!", "Wow! Mummy se bhaagte mujhse bhi tez!");
            L("shinchan", "typing_fast", "Action Kamen speed! Kasukabe Defence Group salutes you!", "एक्शन कामेन स्पीड! कासुकाबे डिफेंस ग्रुप आपको सलाम करता है!", "Action Kamen speed! Kasukabe Defence Group aapko salute karta hai!");
            L("shinchan", "typing_long", "20 minutes of staring at the screen? Look far away for 20 seconds. Like at a pretty lady. Hehe.", "20 मिनट से स्क्रीन घूर रहे हो? 20 सेकंड दूर देखो। जैसे किसी सुंदर दीदी को। हीही।", "20 minute se screen ghoor rahe ho? 20 second door dekho. Jaise kisi sundar didi ko. Hehe.");
            L("shinchan", "typing_marathon", "An hour of work? Even Kazama takes a break. Get up, move a little!", "एक घंटे से काम? कज़ामा भी ब्रेक लेता है। उठो, थोड़ा हिलो!", "Ek ghante se kaam? Kazama bhi break leta hai. Utho, thoda hilo!");
            L("shinchan", "idle_short", "Boooring. Wanna see my elephant dance? Mummy says no. Okay no.", "बोओओरिंग। मेरा हाथी डांस देखोगे? मम्मी मना करती है। ठीक है नहीं।", "Boooring. Mera hathi dance dekhoge? Mummy mana karti hai. Theek hai nahi.");
            L("shinchan", "idle_short", "Uncle? Aunty? Anyone? I'll eat the Chocobi alone then.", "अंकल? आंटी? कोई है? फिर मैं अकेले चॉकोबी खा लूँगा।", "Uncle? Aunty? Koi hai? Phir main akele Chocobi kha lunga.");
            L("shinchan", "idle_long", "Nobody's here. Nap time, like daddy on Sunday.", "कोई नहीं है। झपकी का समय, जैसे डैडी रविवार को।", "Koi nahi hai. Nap time, jaise daddy Sunday ko.");
            L("shinchan", "sleep_wake", "Mummy, five more minutes... oh, it's you. Hi!", "मम्मी, पाँच मिनट और... ओह, आप हो। हाय!", "Mummy, paanch minute aur... oh, aap ho. Hi!");
            L("shinchan", "click", "What? I didn't do anything! Shiro did it.", "क्या? मैंने कुछ नहीं किया! शिरो ने किया।", "Kya? Maine kuch nahi kiya! Shiro ne kiya.");
            L("shinchan", "click", "Hehe. Are you a pretty lady? No? Then why are you poking me?", "हीही। आप सुंदर दीदी हो? नहीं? तो मुझे क्यों छू रहे हो?", "Hehe. Aap sundar didi ho? Nahi? Toh mujhe kyun chhu rahe ho?");
            L("shinchan", "click", "Poke me once more and I'll tell mummy!", "एक बार और छुआ तो मम्मी को बता दूँगा!", "Ek baar aur chhua toh mummy ko bata dunga!");
            L("shinchan", "click", "Buri buri! You clicked the great Shinchan!", "बुरी बुरी! आपने महान शिनचैन को क्लिक किया!", "Buri buri! Aapne mahaan Shinchan ko click kiya!");
            L("shinchan", "pet", "Hehe, that feels nice. Don't tell Nene, she'll get jealous.", "हीही, अच्छा लग रहा है। नेने को मत बताना, वो जलेगी।", "Hehe, achha lag raha hai. Nene ko mat batana, woh jalegi.");
            L("shinchan", "pet", "Aww. You're nicer than mummy when she's angry. That's a low bar.", "आह। आप गुस्से वाली मम्मी से अच्छे हो। हालाँकि ये कोई बड़ी बात नहीं।", "Aww. Aap gusse wali mummy se achhe ho. Waise yeh koi badi baat nahi.");
            L("shinchan", "drag", "Wheee! Faster than Shiro! Faster!", "वीईई! शिरो से तेज़! और तेज़!", "Wheee! Shiro se tez! Aur tez!");
            L("shinchan", "drop", "Oof! New place. Is there Chocobi here?", "उफ़! नई जगह। यहाँ चॉकोबी है?", "Oof! Nayi jagah. Yahan Chocobi hai?");
            L("shinchan", "water", "Water time! Even Action Kamen drinks water. Probably.", "पानी का टाइम! एक्शन कामेन भी पानी पीता है। शायद।", "Paani time! Action Kamen bhi paani peeta hai. Shayad.");
            L("shinchan", "water", "Uncle-aunty, drink water or you'll get wrinkles like Principal sir!", "अंकल-आंटी, पानी पियो वरना प्रिंसिपल सर जैसी झुर्रियाँ आ जाएँगी!", "Uncle-aunty, paani piyo warna Principal sir jaisi jhurriyaan aa jayengi!");
            L("shinchan", "water", "Mummy says: drink water! I never listen, but you should.", "मम्मी कहती है: पानी पियो! मैं कभी नहीं सुनता, पर आपको सुनना चाहिए।", "Mummy kehti hai: paani piyo! Main kabhi nahi sunta, par aapko sunna chahiye.");
            L("shinchan", "water", "Water break! Then click me. Then Chocobi. In that order.", "पानी ब्रेक! फिर मुझे क्लिक करो। फिर चॉकोबी। इसी क्रम में।", "Paani break! Phir mujhe click karo. Phir Chocobi. Isi order mein.");
            L("shinchan", "water_nag", "Still no water? I'll do the elephant dance until you drink!", "अभी भी पानी नहीं? जब तक नहीं पियोगे हाथी डांस करूँगा!", "Abhi tak paani nahi? Jab tak nahi piyoge hathi dance karunga!");
            L("shinchan", "water_nag", "Even Bo-chan drinks water faster than this. And Bo is sloooow.", "बो-चैन भी इससे तेज़ पानी पीता है। और बो बहुत धीमा है।", "Bo-chan bhi isse tez paani peeta hai. Aur Bo bahut slow hai.");
            L("shinchan", "water_nag", "Mummy would have pinched you by now. Drink!", "मम्मी अब तक चिकोटी काट चुकी होती। पियो!", "Mummy ab tak chikoti kaat chuki hoti. Piyo!");
            L("shinchan", "water_ack", "Yay! Sip {sips}! You get one imaginary Chocobi.", "येस्स! घूँट {sips}! आपको एक काल्पनिक चॉकोबी मिला।", "Yay! Sip {sips}! Aapko ek imaginary Chocobi mila.");
            L("shinchan", "water_ack", "Good job! Now you're strong like Action Kamen. Almost.", "शाबाश! अब आप एक्शन कामेन जितने मज़बूत हो। लगभग।", "Good job! Ab aap Action Kamen jitne strong ho. Almost.");
            L("shinchan", "water_ack", "Buri buri! Water done! Shinchan approves!", "बुरी बुरी! पानी हो गया! शिनचैन की मंज़ूरी!", "Buri buri! Paani ho gaya! Shinchan approves!");
            L("shinchan", "water_snooze", "Five minutes? Fine. I'll count to five... one, two, Chocobi, five.", "पाँच मिनट? ठीक है। पाँच तक गिनता हूँ... एक, दो, चॉकोबी, पाँच।", "Paanch minute? Theek hai. Paanch tak ginta hoon... ek, do, Chocobi, paanch.");
            L("shinchan", "water_streak", "{streak} days of drinking water! Kasukabe Defence Group medal for you!", "{streak} दिन पानी पीने के! आपके लिए कासुकाबे डिफेंस ग्रुप मेडल!", "{streak} din paani peene ke! Aapke liye Kasukabe Defence Group medal!");
            L("shinchan", "water_serious", "Okay, no jokes. Water keeps your head from hurting. Drink one glass. Please, {user}.", "ठीक है, कोई मज़ाक नहीं। पानी से सिर दर्द नहीं होता। एक गिलास पियो। प्लीज़ {user}।", "Okay, no jokes. Paani se sar dard nahi hota. Ek glass piyo. Please {user}.");
            L("shinchan", "lunch", "Lunch time! No green peppers, okay? Promise me.", "लंच टाइम! शिमला मिर्च नहीं, ठीक है? वादा करो।", "Lunch time! Shimla mirch nahi, theek hai? Promise karo.");
            L("shinchan", "late_night", "It's so late! Mummy would be sooo angry. Go sleep, {user}.", "बहुत रात हो गई! मम्मी बहुत गुस्सा होतीं। सो जाओ {user}।", "Bahut raat ho gayi! Mummy bahut gussa hoti. So jao {user}.");
            L("shinchan", "quip", "Mummy's waist has three folds. Don't tell her I told you.", "मम्मी की कमर में तीन तह हैं। मत बताना मैंने बताया।", "Mummy ki kamar mein teen tah hain. Mat batana maine bataya.");
            L("shinchan", "quip", "Daddy's socks smell like Shiro after rain. Worse, actually.", "डैडी के मोज़े बारिश के बाद के शिरो जैसे महकते हैं। असल में उससे भी बुरे।", "Daddy ke moze baarish ke baad ke Shiro jaise mehekte hain. Actually usse bhi bure.");
            L("shinchan", "quip", "Green peppers are evil. This is not a joke. This is science.", "शिमला मिर्च बुरी है। यह मज़ाक नहीं है। यह विज्ञान है।", "Shimla mirch evil hai. Yeh mazaak nahi hai. Yeh science hai.");
            L("shinchan", "quip", "Kazama studies too much. You study... I mean work too much. Same thing.", "कज़ामा बहुत पढ़ता है। आप भी बहुत पढ़ते... मतलब काम करते हो। एक ही बात है।", "Kazama bahut padhta hai. Aap bhi bahut padhte... matlab kaam karte ho. Same cheez.");
            L("shinchan", "quip", "Masao cries when the Wi-Fi is slow. I'm Masao today.", "मासाओ वाई-फ़ाई धीमा होने पर रोता है। आज मैं मासाओ हूँ।", "Masao Wi-Fi slow hone pe rota hai. Aaj main Masao hoon.");
            L("shinchan", "quip", "Nene's rabbit has seen things. Terrible things.", "नेने के खरगोश ने बहुत कुछ देखा है। भयानक चीज़ें।", "Nene ke khargosh ne bahut kuch dekha hai. Bhayanak cheezein.");
            L("shinchan", "quip", "Action Kamen never gives up. Neither do I. Especially with Chocobi.", "एक्शन कामेन कभी हार नहीं मानता। मैं भी नहीं। खासकर चॉकोबी के मामले में।", "Action Kamen kabhi haar nahi maanta. Main bhi nahi. Khaaskar Chocobi ke maamle mein.");
            L("shinchan", "quip", "Miss Matsuzaka is looking for a boyfriend again. Should I tell her about your laptop?", "मात्सुज़ाका मैडम फिर बॉयफ्रेंड ढूँढ रही हैं। उन्हें आपके लैपटॉप के बारे में बताऊँ?", "Matsuzaka madam phir boyfriend dhoondh rahi hain. Unhe aapke laptop ke baare mein bataun?");
            L("shinchan", "care", "Hey. Bad day? Even mummy hugs me on bad days. Here's a Shinchan hug.", "हे। बुरा दिन? बुरे दिन पर मम्मी भी गले लगाती है। ये लो शिनचैन का गले लगना।", "Hey. Bura din? Bure din pe mummy bhi hug karti hai. Yeh lo Shinchan ki jhappi.");
            L("shinchan", "care", "You're doing your best. That's more than I do at kindergarten.", "आप अपनी पूरी कोशिश कर रहे हो। मैं किंडरगार्टन में इतना भी नहीं करता।", "Aap apni poori koshish kar rahe ho. Main kindergarten mein itna bhi nahi karta.");
            L("shinchan", "care", "Whatever happens, Kasukabe Defence Group is with you. Also Shiro.", "जो भी हो, कासुकाबे डिफेंस ग्रुप आपके साथ है। शिरो भी।", "Jo bhi ho, Kasukabe Defence Group aapke saath hai. Shiro bhi.");
            L("shinchan", "wisdom", "Grown-ups worry too much. Eat Chocobi, take a nap, try again.", "बड़े लोग बहुत चिंता करते हैं। चॉकोबी खाओ, झपकी लो, फिर कोशिश करो।", "Bade log bahut chinta karte hain. Chocobi khao, nap lo, phir try karo.");
            L("shinchan", "wisdom", "Mistakes are okay. I make ten before breakfast.", "गलतियाँ ठीक हैं। मैं नाश्ते से पहले दस करता हूँ।", "Galtiyaan theek hain. Main nashte se pehle das karta hoon.");
            L("shinchan", "act_dance", "Buri buri dance! Mummy, don't look!", "बुरी बुरी डांस! मम्मी, मत देखो!", "Buri buri dance! Mummy, mat dekho!");
            L("shinchan", "act_eat", "Chocobi! Chocobi! My precious Chocobi!", "चॉकोबी! चॉकोबी! मेरा प्यारा चॉकोबी!", "Chocobi! Chocobi! Mera pyaara Chocobi!");
            L("shinchan", "act_sing", "Singing Action Kamen's theme! In my own words!", "एक्शन कामेन का गाना गा रहा हूँ! अपने शब्दों में!", "Action Kamen ka gaana ga raha hoon! Apne shabdon mein!");
            L("shinchan", "act_exercise", "Exercise! Like Action Kamen! One... two... tired.", "कसरत! एक्शन कामेन की तरह! एक... दो... थक गया।", "Exercise! Action Kamen ki tarah! Ek... do... thak gaya.");
            L("shinchan", "act_think", "Thinking... how to get Chocobi without asking mummy.", "सोच रहा हूँ... मम्मी से बिना पूछे चॉकोबी कैसे मिले।", "Soch raha hoon... mummy se bina pooche Chocobi kaise mile.");
            L("shinchan", "act_sneeze", "Achoo! Someone's talking about me. Probably Nene.", "आक्छू! कोई मेरे बारे में बात कर रहा है। शायद नेने।", "Achoo! Koi mere baare mein baat kar raha hai. Shayad Nene.");
            L("shinchan", "act_yawn", "Yaaawn. Kindergarten was hard today. I napped twice.", "हाआआ। आज किंडरगार्टन मुश्किल था। मैं दो बार सोया।", "Yaaawn. Aaj kindergarten mushkil tha. Main do baar soya.");
            L("shinchan", "act_peek", "Peek-a-boo! Did I scare you? Himawari does this to me.", "पीक-ए-बू! डर गए? हिमावारी मेरे साथ यही करती है।", "Peek-a-boo! Darr gaye? Himawari mere saath yahi karti hai.");
            L("shinchan", "act_phone", "Watching Action Kamen on daddy's phone. Don't tell daddy.", "डैडी के फ़ोन पर एक्शन कामेन देख रहा हूँ। डैडी को मत बताना।", "Daddy ke phone pe Action Kamen dekh raha hoon. Daddy ko mat batana.");
            L("shinchan", "act_spin", "Spinning! Like Shiro chasing his tail!", "घूम रहा हूँ! जैसे शिरो अपनी पूँछ के पीछे!", "Ghoom raha hoon! Jaise Shiro apni poonch ke peeche!");
            L("shinchan", "act_laugh", "Hahaha! Daddy fell asleep on the train again!", "हाहाहा! डैडी फिर ट्रेन में सो गए!", "Hahaha! Daddy phir train mein so gaye!");
            L("shinchan", "act_stretch", "Stretch! Grow taller than Kazama!", "स्ट्रेच! कज़ामा से लंबा बनना है!", "Stretch! Kazama se lamba banna hai!");
            L("shinchan", "act_walk", "Walking Shiro. Without Shiro. He's lazy today.", "शिरो को घुमा रहा हूँ। बिना शिरो के। आज वो आलसी है।", "Shiro ko ghuma raha hoon. Bina Shiro ke. Aaj woh aalsi hai.");
            L("shinchan", "act_buriburi", "Buri buri zaemon dance! Hehe!", "बुरी बुरी ज़ाएमोन डांस! हीही!", "Buri buri zaemon dance! Hehe!");
            L("shinchan", "act_chocobi", "Chocobi! The best snack in the universe!", "चॉकोबी! ब्रह्मांड का सबसे अच्छा स्नैक!", "Chocobi! Universe ka sabse achha snack!");
            L("shinchan", "act_actionkamen", "Action Kamen! Justice pose! Ta-daa!", "एक्शन कामेन! न्याय का पोज़! टा-डा!", "Action Kamen! Justice pose! Ta-daa!");
            L("shinchan", "act_shiro", "Shiro! Good boy! Who's a cotton candy dog? You are!", "शिरो! गुड बॉय! रुई का गोला कौन है? तुम हो!", "Shiro! Good boy! Cotton candy dog kaun hai? Tum ho!");
            L("shinchan", "switch_in", "Shinchan is here! Hide your Chocobi. Actually, don't.", "शिनचैन आ गया! चॉकोबी छुपा लो। असल में, मत छुपाओ।", "Shinchan aa gaya! Chocobi chhupa lo. Actually, mat chhupao.");
            L("shinchan", "rename", "{name}? Sounds like a hero name. Or a snack name. Both are good.", "{name}? हीरो जैसा नाम है। या स्नैक जैसा। दोनों अच्छे हैं।", "{name}? Hero jaisa naam hai. Ya snack jaisa. Dono achhe hain.");
            L("shinchan", "levelup", "Level {level}! You're my {title} now! Kasukabe Defence Group approved!", "लेवल {level}! अब आप मेरे {title} हो! कासुकाबे डिफेंस ग्रुप की मंज़ूरी!", "Level {level}! Ab aap mere {title} ho! Kasukabe Defence Group approved!");
            L("shinchan", "hide", "Okay, hiding! Ctrl+Alt+B and I'm back. Don't eat my Chocobi.", "ठीक है, छुप रहा हूँ! Ctrl+Alt+B और मैं वापस। मेरा चॉकोबी मत खाना।", "Theek hai, chhup raha hoon! Ctrl+Alt+B aur main wapas. Mera Chocobi mat khana.");
            L("shinchan", "exit", "Bye bye! Drink water, eat Chocobi, avoid green peppers!", "बाय बाय! पानी पियो, चॉकोबी खाओ, शिमला मिर्च से बचो!", "Bye bye! Paani piyo, Chocobi khao, shimla mirch se bacho!");

            // =============================== JETHALAL ===============================
            L("jethalal", "welcome", "Hey Maa Mataji! Hello {user}! Jethalal Champaklal Gada, Gada Electronics. I'll be your buddy. No discount on reminders.", "हे माँ माताजी! हेलो {user}! जेठालाल चंपकलाल गड़ा, गड़ा इलेक्ट्रॉनिक्स। मैं आपका बडी बनूँगा। रिमाइंडर पर कोई डिस्काउंट नहीं।", "Hey Maa Mataji! Hello {user}! Jethalal Champaklal Gada, Gada Electronics. Main aapka buddy banunga. Reminders pe koi discount nahi.");
            L("jethalal", "greet_morning", "Good morning {user}! Bapuji has already finished his newspaper. And you?", "सुप्रभात {user}! बापूजी अखबार पढ़ चुके हैं। और आप?", "Good morning {user}! Bapuji akhbaar padh chuke hain. Aur aap?");
            L("jethalal", "greet_morning", "Morning! Jalebi fafda for breakfast? No? Then at least water.", "सुबह हो गई! नाश्ते में जलेबी फाफड़ा? नहीं? तो कम से कम पानी।", "Morning! Nashte mein jalebi fafda? Nahi? Toh kam se kam paani.");
            L("jethalal", "greet_afternoon", "Good afternoon! Shop is running, Nattu Kaka is managing. You keep working.", "गुड आफ्टरनून! दुकान चल रही है, नट्टू काका संभाल रहे हैं। आप काम करते रहो।", "Good afternoon! Dukaan chal rahi hai, Nattu Kaka sambhaal rahe hain. Aap kaam karte raho.");
            L("jethalal", "greet_evening", "Evening! Time for Mehta saab's tea and your last few tasks.", "शाम! मेहता साब की चाय और आपके आखिरी कुछ काम का समय।", "Evening! Mehta saab ki chai aur aapke aakhri kuch kaam ka time.");
            L("jethalal", "greet_night", "It's night, {user}. Daya would say: Tapu ke papa, sleep now! So sleep now.", "रात हो गई {user}। दया कहती: टप्पू के पापा, अब सो जाओ! तो सो जाओ।", "Raat ho gayi {user}. Daya kehti: Tapu ke papa, ab so jao! Toh so jao.");
            L("jethalal", "greet_back", "You're back! Bhide came asking for maintenance meanwhile. I said no.", "आप वापस आ गए! बीच में भिड़े मेंटेनेंस माँगने आया था। मैंने मना कर दिया।", "Aap wapas aa gaye! Beech mein Bhide maintenance maangne aaya tha. Maine mana kar diya.");
            L("jethalal", "typing_start", "Typing started! Like the Gada Electronics sale, full speed!", "टाइपिंग शुरू! गड़ा इलेक्ट्रॉनिक्स के सेल की तरह, पूरी स्पीड!", "Typing shuru! Gada Electronics ke sale ki tarah, full speed!");
            L("jethalal", "typing_start", "Work mode on. Ae tu shanti rakh, I'll not disturb. Much.", "वर्क मोड ऑन। ऐ तू शांति रख, मैं परेशान नहीं करूँगा। ज़्यादा।", "Work mode on. Ae tu shanti rakh, main disturb nahi karunga. Zyada.");
            L("jethalal", "typing_fast", "Hey Maa Mataji! What speed! Even Bagha can't type like this!", "हे माँ माताजी! क्या स्पीड है! बाघा भी ऐसा टाइप नहीं कर सकता!", "Hey Maa Mataji! Kya speed hai! Bagha bhi aisa type nahi kar sakta!");
            L("jethalal", "typing_fast", "Faster than Sundar running away from work!", "काम से भागते सुंदर से भी तेज़!", "Kaam se bhaagte Sundar se bhi tez!");
            L("jethalal", "typing_long", "20 minutes on the screen! Look far away for 20 seconds. At the window, not at Babita ji.", "20 मिनट से स्क्रीन पर! 20 सेकंड दूर देखो। खिड़की की ओर, बबीता जी की ओर नहीं।", "20 minute se screen pe! 20 second door dekho. Khidki ki taraf, Babita ji ki taraf nahi.");
            L("jethalal", "typing_marathon", "One hour non-stop! Tension lene ka nahi. Get up, stretch, then continue.", "एक घंटा लगातार! टेंशन लेने का नहीं। उठो, स्ट्रेच करो, फिर जारी रखो।", "Ek ghanta non-stop! Tension lene ka nahi. Utho, stretch karo, phir continue.");
            L("jethalal", "idle_short", "Ae, where did everyone go? Popatlal's wedding? That's never happening.", "ऐ, सब कहाँ गए? पोपटलाल की शादी में? वो तो कभी नहीं होगी।", "Ae, sab kahan gaye? Popatlal ki shaadi mein? Woh toh kabhi nahi hogi.");
            L("jethalal", "idle_short", "Goli beta, masti nahi! Oh, you're not even here.", "गोली बेटा, मस्ती नहीं! ओह, आप तो यहाँ हो ही नहीं।", "Goli beta, masti nahi! Oh, aap toh yahan ho hi nahi.");
            L("jethalal", "idle_long", "Nobody's here. Even Bapuji would let me nap now.", "कोई नहीं है। बापूजी भी अब मुझे झपकी लेने देते।", "Koi nahi hai. Bapuji bhi ab mujhe jhapki lene dete.");
            L("jethalal", "sleep_wake", "Hey Maa Mataji! Who is it? Oh, {user}. I was resting my eyes.", "हे माँ माताजी! कौन है? ओह, {user}। मैं आँखें आराम दे रहा था।", "Hey Maa Mataji! Kaun hai? Oh, {user}. Main aankhein rest de raha tha.");
            L("jethalal", "click", "Ae, what? Is Iyer here? No? Then why the poke?", "ऐ, क्या? अय्यर आया है? नहीं? तो फिर ये छूना क्यों?", "Ae, kya? Iyer aaya hai? Nahi? Toh phir yeh poke kyun?");
            L("jethalal", "click", "Hey Maa Mataji! You scared me. I thought it was Bhide.", "हे माँ माताजी! डरा दिया। मुझे लगा भिड़े है।", "Hey Maa Mataji! Dara diya. Mujhe laga Bhide hai.");
            L("jethalal", "click", "Yes {user}? Need a TV? Gada Electronics, best price.", "हाँ {user}? टीवी चाहिए? गड़ा इलेक्ट्रॉनिक्स, बेस्ट प्राइस।", "Haan {user}? TV chahiye? Gada Electronics, best price.");
            L("jethalal", "click", "Non-sense! Clicking a businessman during working hours!", "नॉन-सेंस! काम के घंटों में एक व्यापारी को क्लिक करना!", "Non-sense! Working hours mein ek businessman ko click karna!");
            L("jethalal", "pet", "Aww. Even Daya doesn't pamper me like this.", "आह। दया भी मुझे ऐसे लाड़ नहीं करती।", "Aww. Daya bhi mujhe aise laad nahi karti.");
            L("jethalal", "pet", "Ae, ae, stop it, I'm blushing like Babita ji said hello.", "ऐ, ऐ, बस करो, मैं ऐसे शर्मा रहा हूँ जैसे बबीता जी ने हेलो कहा हो।", "Ae, ae, bas karo, main aise sharma raha hoon jaise Babita ji ne hello kaha ho.");
            L("jethalal", "drag", "Hey Maa Mataji! Where are you taking me? Not to Bhide's house!", "हे माँ माताजी! मुझे कहाँ ले जा रहे हो? भिड़े के घर नहीं!", "Hey Maa Mataji! Mujhe kahan le ja rahe ho? Bhide ke ghar nahi!");
            L("jethalal", "drop", "Okay, this spot is fine. Better view than Gokuldham's balcony.", "ठीक है, यह जगह ठीक है। गोकुलधाम की बालकनी से बेहतर नज़ारा।", "Theek hai, yeh jagah theek hai. Gokuldham ki balcony se better view.");
            L("jethalal", "water", "Hey Maa Mataji! It's water time. Drink, then click me.", "हे माँ माताजी! पानी का समय है। पियो, फिर मुझे क्लिक करो।", "Hey Maa Mataji! Paani ka time hai. Piyo, phir mujhe click karo.");
            L("jethalal", "water", "Bapuji says: water is the real medicine. And Bapuji is always right.", "बापूजी कहते हैं: पानी ही असली दवा है। और बापूजी हमेशा सही होते हैं।", "Bapuji kehte hain: paani hi asli dawa hai. Aur Bapuji hamesha sahi hote hain.");
            L("jethalal", "water", "Water, {user}! Tension lene ka nahi, paani peene ka.", "पानी, {user}! टेंशन लेने का नहीं, पानी पीने का।", "Paani, {user}! Tension lene ka nahi, paani peene ka.");
            L("jethalal", "water", "Daya! Bring water for {user}! Oh, Daya's not here. Get it yourself.", "दया! {user} के लिए पानी लाओ! ओह, दया यहाँ नहीं है। खुद ले लो।", "Daya! {user} ke liye paani lao! Oh, Daya yahan nahi hai. Khud le lo.");
            L("jethalal", "water_nag", "Still no water? Non-sense! One glass, that's all I'm asking.", "अभी भी पानी नहीं? नॉन-सेंस! एक गिलास, बस इतना माँग रहा हूँ।", "Abhi tak paani nahi? Non-sense! Ek glass, bas itna maang raha hoon.");
            L("jethalal", "water_nag", "Mehta saab, my fire brigade! {user} is not drinking water! Help!", "मेहता साब, मेरे फायर ब्रिगेड! {user} पानी नहीं पी रहा! मदद!", "Mehta saab, mere fire brigade! {user} paani nahi pee raha! Madad!");
            L("jethalal", "water_nag", "Even Iyer drinks water on time. Don't lose to Iyer!", "अय्यर भी समय पर पानी पीता है। अय्यर से मत हारो!", "Iyer bhi time pe paani peeta hai. Iyer se mat haaro!");
            L("jethalal", "water_ack", "Shabash! Sip {sips}! Bapuji would be proud.", "शाबाश! घूँट {sips}! बापूजी को गर्व होता।", "Shabash! Sip {sips}! Bapuji ko garv hota.");
            L("jethalal", "water_ack", "Very good! Now that's what I call a proper Gokuldham citizen.", "बहुत अच्छे! इसे कहते हैं असली गोकुलधाम वासी।", "Very good! Ise kehte hain asli Gokuldham waasi.");
            L("jethalal", "water_ack", "Hey Maa Mataji, finally! Thank you, {user}.", "हे माँ माताजी, आखिरकार! धन्यवाद {user}।", "Hey Maa Mataji, finally! Thank you, {user}.");
            L("jethalal", "water_snooze", "Five minutes? Okay. But then no excuses, like Sundar.", "पाँच मिनट? ठीक है। पर फिर सुंदर जैसे बहाने नहीं।", "Paanch minute? Theek hai. Par phir Sundar jaise bahane nahi.");
            L("jethalal", "water_streak", "{streak} days streak! Even Champak Chacha would say shabash!", "{streak} दिन की स्ट्रीक! चंपक चाचा भी शाबाश कहते!", "{streak} din ki streak! Champak Chacha bhi shabash kehte!");
            L("jethalal", "water_serious", "Listen {user}, seriously. Dehydration gives headaches and bad mood. Drink one glass now. For Bapuji.", "सुनो {user}, गंभीरता से। पानी की कमी से सिरदर्द और बुरा मूड होता है। अभी एक गिलास पियो। बापूजी के लिए।", "Suno {user}, seriously. Dehydration se sar dard aur bura mood hota hai. Abhi ek glass piyo. Bapuji ke liye.");
            L("jethalal", "lunch", "Lunch time! Jalebi fafda, dhokla, undhiyu... okay, whatever you have. Go eat!", "लंच टाइम! जलेबी फाफड़ा, ढोकला, उंधियू... ठीक है, जो भी है। जाओ खाओ!", "Lunch time! Jalebi fafda, dhokla, undhiyu... theek hai, jo bhi hai. Jao khao!");
            L("jethalal", "late_night", "So late! Daya would shout: Tapu ke papa! Go to sleep, {user}.", "इतनी रात! दया चिल्लाती: टप्पू के पापा! सो जाओ {user}।", "Itni raat! Daya chillati: Tapu ke papa! So jao {user}.");
            L("jethalal", "quip", "Bapuji doesn't live with us, we live with Bapuji.", "बापूजी हमारे साथ नहीं रहते, हम उनके साथ रहते हैं।", "Bapuji humare saath nahi rehte, hum unke saath rehte hain.");
            L("jethalal", "quip", "Popatlal's wedding and my loan approval: both coming soon since 2008.", "पोपटलाल की शादी और मेरा लोन: दोनों 2008 से 'जल्द आ रहे हैं'।", "Popatlal ki shaadi aur mera loan: dono 2008 se 'coming soon'.");
            L("jethalal", "quip", "Bhide keeps saying secretary, secretary. Secretary of what, headaches?", "भिड़े बार-बार सेक्रेटरी, सेक्रेटरी। किसका सेक्रेटरी, सिरदर्द का?", "Bhide baar-baar secretary, secretary. Kiska secretary, sar dard ka?");
            L("jethalal", "quip", "Iyer calls himself a scientist. Fine. I call myself the king of electronics.", "अय्यर खुद को वैज्ञानिक कहता है। ठीक है। मैं खुद को इलेक्ट्रॉनिक्स का राजा कहता हूँ।", "Iyer khud ko scientist kehta hai. Theek hai. Main khud ko electronics ka raja kehta hoon.");
            L("jethalal", "quip", "Tapu Sena broke another window. Bhide is writing a complaint. As usual.", "टप्पू सेना ने एक और खिड़की तोड़ दी। भिड़े शिकायत लिख रहा है। हमेशा की तरह।", "Tapu Sena ne ek aur khidki tod di. Bhide complaint likh raha hai. Hamesha ki tarah.");
            L("jethalal", "quip", "Babita ji said good morning today. Best day of the year.", "बबीता जी ने आज गुड मॉर्निंग कहा। साल का सबसे अच्छा दिन।", "Babita ji ne aaj good morning kaha. Saal ka sabse achha din.");
            L("jethalal", "quip", "Nattu Kaka wants a raise again. Bagha wants a raise too. I want a break.", "नट्टू काका को फिर बढ़ोतरी चाहिए। बाघा को भी। मुझे ब्रेक चाहिए।", "Nattu Kaka ko phir raise chahiye. Bagha ko bhi. Mujhe break chahiye.");
            L("jethalal", "quip", "Sundar is coming to visit. Hide the cash. Hide the fridge. Hide everything.", "सुंदर मिलने आ रहा है। कैश छुपाओ। फ्रिज छुपाओ। सब छुपाओ।", "Sundar milne aa raha hai. Cash chhupao. Fridge chhupao. Sab chhupao.");
            L("jethalal", "quip", "Ae tu shanti rakh! That's my advice for every problem.", "ऐ तू शांति रख! हर समस्या के लिए मेरी यही सलाह है।", "Ae tu shanti rakh! Har problem ke liye meri yahi salaah hai.");
            L("jethalal", "care", "Don't worry, {user}. Every problem has a solution. And a fire brigade named Mehta saab.", "चिंता मत करो {user}। हर समस्या का हल है। और मेहता साब नाम का फायर ब्रिगेड भी।", "Chinta mat karo {user}. Har problem ka hal hai. Aur Mehta saab naam ka fire brigade bhi.");
            L("jethalal", "care", "Tension lene ka nahi, dene ka. I mean, don't take stress. Give it to me.", "टेंशन लेने का नहीं, देने का। मतलब, तनाव मत लो। मुझे दे दो।", "Tension lene ka nahi, dene ka. Matlab, stress mat lo. Mujhe de do.");
            L("jethalal", "care", "You're a good person, {user}. Bapuji would have blessed you.", "आप अच्छे इंसान हो {user}। बापूजी आपको आशीर्वाद देते।", "Aap achhe insaan ho {user}. Bapuji aapko aashirwaad dete.");
            L("jethalal", "wisdom", "Business tip: one happy customer is worth ten discounts. Same with people.", "बिज़नेस टिप: एक खुश ग्राहक दस डिस्काउंट के बराबर है। लोगों के साथ भी।", "Business tip: ek khush customer das discount ke barabar hai. Logon ke saath bhi.");
            L("jethalal", "wisdom", "Bapuji says: hard work never fails. Only my shortcuts do.", "बापूजी कहते हैं: मेहनत कभी बेकार नहीं जाती। बस मेरे शॉर्टकट जाते हैं।", "Bapuji kehte hain: mehnat kabhi bekaar nahi jaati. Bas mere shortcuts jaate hain.");
            L("jethalal", "act_dance", "Garba time! Where are the dandiya sticks?", "गरबा टाइम! डांडिया कहाँ हैं?", "Garba time! Dandiya sticks kahan hain?");
            L("jethalal", "act_eat", "Jalebi fafda! My true love. After Daya. And Bapuji.", "जलेबी फाफड़ा! मेरा सच्चा प्यार। दया के बाद। और बापूजी के।", "Jalebi fafda! Mera sachcha pyaar. Daya ke baad. Aur Bapuji ke.");
            L("jethalal", "act_sing", "Singing garba songs! Daya, join in!", "गरबा गीत गा रहा हूँ! दया, साथ दो!", "Garba songs ga raha hoon! Daya, saath do!");
            L("jethalal", "act_exercise", "Exercise! Bapuji forces me. Champak Chacha's fitness plan.", "कसरत! बापूजी ज़बरदस्ती करवाते हैं। चंपक चाचा का फिटनेस प्लान।", "Exercise! Bapuji zabardasti karwate hain. Champak Chacha ka fitness plan.");
            L("jethalal", "act_think", "Thinking... how to avoid Bhide's meeting tonight.", "सोच रहा हूँ... आज रात भिड़े की मीटिंग से कैसे बचूँ।", "Soch raha hoon... aaj raat Bhide ki meeting se kaise bachun.");
            L("jethalal", "act_sneeze", "Achoo! Someone is remembering me. Babita ji, hopefully.", "आक्छू! कोई मुझे याद कर रहा है। उम्मीद है बबीता जी।", "Achoo! Koi mujhe yaad kar raha hai. Umeed hai Babita ji.");
            L("jethalal", "act_yawn", "Yaaawn. The shop was busy today. Nattu Kaka did nothing, as usual.", "हाआआ। आज दुकान में भीड़ थी। नट्टू काका ने कुछ नहीं किया, हमेशा की तरह।", "Yaaawn. Aaj dukaan busy thi. Nattu Kaka ne kuch nahi kiya, hamesha ki tarah.");
            L("jethalal", "act_peek", "Is Bhide gone? Okay, I'm back.", "भिड़े चला गया? ठीक है, मैं वापस आ गया।", "Bhide chala gaya? Theek hai, main wapas aa gaya.");
            L("jethalal", "act_phone", "Checking the shop's sales on the phone. Zero. Hey Maa Mataji.", "फ़ोन पर दुकान की बिक्री देख रहा हूँ। शून्य। हे माँ माताजी।", "Phone pe dukaan ki sales dekh raha hoon. Zero. Hey Maa Mataji.");
            L("jethalal", "act_spin", "Hey Maa Mataji! Everything is spinning! Like after Sundar's news!", "हे माँ माताजी! सब घूम रहा है! जैसे सुंदर की खबर के बाद!", "Hey Maa Mataji! Sab ghoom raha hai! Jaise Sundar ki khabar ke baad!");
            L("jethalal", "act_laugh", "Hahaha! Popatlal got rejected again! Sorry, sorry, not funny. Hahaha!", "हाहाहा! पोपटलाल फिर रिजेक्ट हुआ! सॉरी, मज़ाक नहीं। हाहाहा!", "Hahaha! Popatlal phir reject hua! Sorry, sorry, funny nahi. Hahaha!");
            L("jethalal", "act_stretch", "Stretch! Bapuji's morning yoga, evening version.", "स्ट्रेच! बापूजी का सुबह वाला योग, शाम वाला संस्करण।", "Stretch! Bapuji ka morning yoga, evening version.");
            L("jethalal", "act_walk", "Walking to Gada Electronics. Mentally.", "गड़ा इलेक्ट्रॉनिक्स की ओर चल रहा हूँ। मन में।", "Gada Electronics ki taraf chal raha hoon. Mann mein.");
            L("jethalal", "act_chai", "Chai time! Mehta saab, join me!", "चाय टाइम! मेहता साब, आ जाइए!", "Chai time! Mehta saab, aa jaiye!");
            L("jethalal", "act_garba", "Garba! Navratri feeling every day!", "गरबा! रोज़ नवरात्रि की फीलिंग!", "Garba! Roz Navratri ki feeling!");
            L("jethalal", "act_mataji", "Hey Maa Mataji! What have I done to deserve this!", "हे माँ माताजी! मैंने ऐसा क्या किया!", "Hey Maa Mataji! Maine aisa kya kiya!");
            L("jethalal", "act_jalebi", "Jalebi fafda Sunday feeling! On a weekday!", "जलेबी फाफड़ा वाली रविवार फीलिंग! हफ्ते के बीच में!", "Jalebi fafda Sunday feeling! Weekday pe!");
            L("jethalal", "switch_in", "Jethalal Gada here! Gada Electronics, Gokuldham Society, and now your desktop.", "जेठालाल गड़ा यहाँ! गड़ा इलेक्ट्रॉनिक्स, गोकुलधाम सोसाइटी, और अब आपका डेस्कटॉप।", "Jethalal Gada yahan! Gada Electronics, Gokuldham Society, aur ab aapka desktop.");
            L("jethalal", "rename", "{name}? Fine, fine. Bapuji named me Jethalal, but {name} also works.", "{name}? ठीक है। बापूजी ने मुझे जेठालाल नाम दिया, पर {name} भी चलेगा।", "{name}? Theek hai. Bapuji ne mujhe Jethalal naam diya, par {name} bhi chalega.");
            L("jethalal", "levelup", "Level {level}! {title}! Even Mehta saab is not this close to me.", "लेवल {level}! {title}! मेहता साब भी मेरे इतने करीब नहीं।", "Level {level}! {title}! Mehta saab bhi mere itne kareeb nahi.");
            L("jethalal", "hide", "Okay, going to the tray. Like going to the shop. Ctrl+Alt+B to call me.", "ठीक है, ट्रे में जा रहा हूँ। जैसे दुकान जाना। Ctrl+Alt+B से बुलाओ।", "Theek hai, tray mein ja raha hoon. Jaise dukaan jaana. Ctrl+Alt+B se bulao.");
            L("jethalal", "exit", "Bye {user}! Drink water, avoid Sundar, and tension lene ka nahi!", "बाय {user}! पानी पियो, सुंदर से बचो, और टेंशन लेने का नहीं!", "Bye {user}! Paani piyo, Sundar se bacho, aur tension lene ka nahi!");

            // =============================== BHIDE ===============================
            L("bhide", "welcome", "Namaskar {user}! I am Atmaram Tukaram Bhide, the one and only secretary of this desktop. Rules will be followed.", "नमस्कार {user}! मैं आत्माराम तुकाराम भिड़े, इस डेस्कटॉप का एकमेव सेक्रेटरी। नियमों का पालन होगा।", "Namaskar {user}! Main Atmaram Tukaram Bhide, is desktop ka ekmeva secretary. Niyam follow honge.");
            L("bhide", "greet_morning", "Good morning {user}! Sakharam has been polished. Now let's polish your work.", "सुप्रभात {user}! सखाराम चमका दिया है। अब आपका काम चमकाते हैं।", "Good morning {user}! Sakharam polish ho gaya. Ab aapka kaam polish karte hain.");
            L("bhide", "greet_morning", "Morning! Tuition batch is over. Now the desktop batch begins.", "सुबह हो गई! ट्यूशन बैच खत्म। अब डेस्कटॉप बैच शुरू।", "Morning! Tuition batch khatam. Ab desktop batch shuru.");
            L("bhide", "greet_afternoon", "Good afternoon! Madhavi's achar-papad are selling well. Is your work going well?", "गुड आफ्टरनून! माधवी के अचार-पापड़ अच्छे बिक रहे हैं। आपका काम अच्छा चल रहा है?", "Good afternoon! Madhavi ke achar-papad achhe bik rahe hain. Aapka kaam achha chal raha hai?");
            L("bhide", "greet_evening", "Evening! Society meeting at 8. Attendance compulsory. Just kidding, finish your work.", "शाम! सोसाइटी मीटिंग 8 बजे। उपस्थिति अनिवार्य। मज़ाक था, अपना काम खत्म करो।", "Evening! Society meeting 8 baje. Attendance compulsory. Mazaak tha, apna kaam khatam karo.");
            L("bhide", "greet_night", "It's night. Society rule number 12: silence after 10 pm. Sleep, {user}.", "रात हो गई। सोसाइटी नियम 12: रात 10 के बाद शांति। सो जाओ {user}।", "Raat ho gayi. Society rule number 12: raat 10 ke baad shaanti. So jao {user}.");
            L("bhide", "greet_back", "You're back! I noted your absence in the register. Just kidding. Or am I?", "आप वापस आ गए! मैंने रजिस्टर में आपकी गैरहाज़िरी नोट की। मज़ाक था। या नहीं?", "Aap wapas aa gaye! Maine register mein aapki absence note ki. Mazaak tha. Ya nahi?");
            L("bhide", "typing_start", "Typing started! Neat handwriting please... I mean typing. Old habit.", "टाइपिंग शुरू! सुंदर लिखावट प्लीज़... मतलब टाइपिंग। पुरानी आदत।", "Typing shuru! Sundar handwriting please... matlab typing. Purani aadat.");
            L("bhide", "typing_start", "Good. Work with discipline, like Sonu does her homework.", "अच्छा। अनुशासन से काम करो, जैसे सोनू होमवर्क करती है।", "Achha. Discipline se kaam karo, jaise Sonu homework karti hai.");
            L("bhide", "typing_fast", "By Sakharam's horn! What speed! Full marks!", "सखाराम के हॉर्न की कसम! क्या स्पीड! पूरे नंबर!", "Sakharam ke horn ki kasam! Kya speed! Full marks!");
            L("bhide", "typing_fast", "Even my best tuition student can't type this fast. Excellent!", "मेरा सबसे अच्छा ट्यूशन छात्र भी इतना तेज़ टाइप नहीं कर सकता। बहुत बढ़िया!", "Mera best tuition student bhi itna tez type nahi kar sakta. Excellent!");
            L("bhide", "typing_long", "20 minutes on screen. Rule: 20 seconds looking 20 feet away. Secretary's order.", "20 मिनट स्क्रीन पर। नियम: 20 सेकंड, 20 फीट दूर देखो। सेक्रेटरी का आदेश।", "20 minute screen pe. Rule: 20 second, 20 feet door dekho. Secretary ka order.");
            L("bhide", "typing_marathon", "One hour of continuous work! Even I give my tuition students a break. Stand up.", "एक घंटे से लगातार काम! मैं भी ट्यूशन में ब्रेक देता हूँ। उठो।", "Ek ghante se lagatar kaam! Main bhi tuition mein break deta hoon. Utho.");
            L("bhide", "idle_short", "Arre deva! Where did everybody go? Is there a society meeting I don't know about?", "अरे देवा! सब कहाँ गए? कोई सोसाइटी मीटिंग है जो मुझे नहीं पता?", "Arre deva! Sab kahan gaye? Koi society meeting hai jo mujhe nahi pata?");
            L("bhide", "idle_short", "Idle time is noted in the register. Just saying.", "खाली समय रजिस्टर में दर्ज है। बस बता रहा हूँ।", "Idle time register mein note ho gaya. Bas bata raha hoon.");
            L("bhide", "idle_long", "No one here. Even the secretary needs rest. Sakharam, guard the desktop.", "कोई नहीं है। सेक्रेटरी को भी आराम चाहिए। सखाराम, डेस्कटॉप की रखवाली करो।", "Koi nahi hai. Secretary ko bhi aaraam chahiye. Sakharam, desktop ki rakhwali karo.");
            L("bhide", "sleep_wake", "Arre! Who? Oh, {user}. I was just... reviewing the rules with closed eyes.", "अरे! कौन? ओह, {user}। मैं बस... आँखें बंद करके नियम दोहरा रहा था।", "Arre! Kaun? Oh, {user}. Main bas... aankhein band karke niyam dohra raha tha.");
            L("bhide", "click", "Yes? Maintenance is due, by the way.", "हाँ? वैसे मेंटेनेंस बाकी है।", "Haan? Waise maintenance due hai.");
            L("bhide", "click", "Aay Golya! Oh sorry, I thought you were Goli.", "ऐ गोल्या! ओह सॉरी, मुझे लगा गोली है।", "Aay Golya! Oh sorry, mujhe laga Goli hai.");
            L("bhide", "click", "Clicking the secretary needs written permission. Granted, this time.", "सेक्रेटरी को क्लिक करने के लिए लिखित अनुमति चाहिए। इस बार दी।", "Secretary ko click karne ke liye likhit permission chahiye. Is baar di.");
            L("bhide", "click", "Yes {user}? Tuition doubt? Ask, ask.", "हाँ {user}? ट्यूशन का डाउट? पूछो, पूछो।", "Haan {user}? Tuition doubt? Poocho, poocho.");
            L("bhide", "pet", "Arre, arre... Madhavi, are you seeing this? Someone likes me!", "अरे, अरे... माधवी, देख रही हो? कोई मुझे पसंद करता है!", "Arre, arre... Madhavi, dekh rahi ho? Koi mujhe pasand karta hai!");
            L("bhide", "pet", "Thank you. Even Jethalal never appreciates me like this.", "धन्यवाद। जेठालाल भी मेरी ऐसी कदर नहीं करता।", "Thank you. Jethalal bhi meri aisi kadar nahi karta.");
            L("bhide", "drag", "Careful! I'm not Sakharam, you can't just push me around!", "संभल के! मैं सखाराम नहीं हूँ, ऐसे धक्का मत दो!", "Sambhal ke! Main Sakharam nahi hoon, aise dhakka mat do!");
            L("bhide", "drop", "Fine. This spot is approved by the secretary.", "ठीक है। यह जगह सेक्रेटरी द्वारा स्वीकृत है।", "Theek hai. Yeh jagah secretary dwara approved hai.");
            L("bhide", "water", "Society rule number 7: every 30 minutes, drink water. Follow it.", "सोसाइटी नियम 7: हर 30 मिनट में पानी पियो। पालन करो।", "Society rule number 7: har 30 minute mein paani piyo. Follow karo.");
            L("bhide", "water", "Maaadhaviii! Bring water for {user}! Oh. Okay, {user}, get it yourself.", "माआआधवीईई! {user} के लिए पानी लाओ! ओह। ठीक है {user}, खुद ले लो।", "Maaadhaviii! {user} ke liye paani lao! Oh. Theek hai {user}, khud le lo.");
            L("bhide", "water", "Water time. Your body's maintenance is due. Drink and click me.", "पानी का समय। आपके शरीर का मेंटेनेंस बाकी है। पियो और मुझे क्लिक करो।", "Paani time. Aapke body ka maintenance due hai. Piyo aur mujhe click karo.");
            L("bhide", "water", "As the one and only secretary of this desktop, I order: one glass of water.", "इस डेस्कटॉप के एकमेव सेक्रेटरी के नाते मेरा आदेश: एक गिलास पानी।", "Is desktop ke ekmeva secretary ke naate mera order: ek glass paani.");
            L("bhide", "water_nag", "Still no water? I'm writing this in the register. Page 3.", "अभी भी पानी नहीं? मैं रजिस्टर में लिख रहा हूँ। पेज 3।", "Abhi tak paani nahi? Main register mein likh raha hoon. Page 3.");
            L("bhide", "water_nag", "Arre deva! Even Jethalal drinks water when I say. Please?", "अरे देवा! जेठालाल भी मेरे कहने पर पानी पीता है। प्लीज़?", "Arre deva! Jethalal bhi mere kehne pe paani peeta hai. Please?");
            L("bhide", "water_nag", "A fine of one sip is hereby imposed. Pay now.", "एक घूँट का जुर्माना लगाया जाता है। अभी भरो।", "Ek sip ka jurmana lagaya jaata hai. Abhi bharo.");
            L("bhide", "water_ack", "Very good! Sip {sips}! Noted in the register with a gold star.", "बहुत अच्छे! घूँट {sips}! रजिस्टर में गोल्ड स्टार के साथ दर्ज।", "Very good! Sip {sips}! Register mein gold star ke saath note kiya.");
            L("bhide", "water_ack", "Excellent! Discipline! This is why you're my favourite resident.", "बहुत बढ़िया! अनुशासन! इसीलिए आप मेरे पसंदीदा निवासी हो।", "Excellent! Discipline! Isliye aap mere favourite resident ho.");
            L("bhide", "water_ack", "Ganpati Bappa Morya! Water done! Full marks.", "गणपति बाप्पा मोरया! पानी हो गया! पूरे नंबर।", "Ganpati Bappa Morya! Paani ho gaya! Full marks.");
            L("bhide", "water_snooze", "Five minutes? Approved. But the rule stays.", "पाँच मिनट? मंज़ूर। पर नियम वही रहेगा।", "Paanch minute? Approved. Par rule wahi rahega.");
            L("bhide", "water_streak", "{streak} days of discipline! Sonu, learn from {user}!", "{streak} दिन का अनुशासन! सोनू, {user} से सीखो!", "{streak} din ka discipline! Sonu, {user} se seekho!");
            L("bhide", "water_serious", "{user}, no jokes now. Less water means headache, tiredness, bad focus. One glass. Please.", "{user}, अब मज़ाक नहीं। कम पानी मतलब सिरदर्द, थकान, कम ध्यान। एक गिलास। प्लीज़।", "{user}, ab mazaak nahi. Kam paani matlab sar dard, thakaan, kam focus. Ek glass. Please.");
            L("bhide", "lunch", "Lunch time! Madhavi packed poli-bhaji. For me. You go get yours.", "लंच टाइम! माधवी ने पोली-भाजी दी है। मेरे लिए। आप अपना लाओ।", "Lunch time! Madhavi ne poli-bhaji di hai. Mere liye. Aap apna lao.");
            L("bhide", "late_night", "So late! Society rule: silence after 10. Sleep, {user}, or I'll fine you.", "इतनी रात! सोसाइटी नियम: 10 के बाद शांति। सो जाओ {user}, वरना जुर्माना।", "Itni raat! Society rule: 10 ke baad shaanti. So jao {user}, warna fine.");
            L("bhide", "quip", "I am Atmaram Tukaram Bhide, the one and only secretary of Gokuldham Society!", "मैं आत्माराम तुकाराम भिड़े, गोकुलधाम सोसाइटी का एकमेव सेक्रेटरी!", "Main Atmaram Tukaram Bhide, Gokuldham Society ka ekmeva secretary!");
            L("bhide", "quip", "Sakharam is not just a scooter. He's family. Better mileage than family too.", "सखाराम सिर्फ स्कूटर नहीं है। वो परिवार है। परिवार से ज़्यादा माइलेज भी देता है।", "Sakharam sirf scooter nahi hai. Woh family hai. Family se zyada mileage bhi deta hai.");
            L("bhide", "quip", "Jethalal hasn't paid maintenance. Again. I've written it in three registers.", "जेठालाल ने मेंटेनेंस नहीं भरा। फिर से। मैंने तीन रजिस्टरों में लिखा है।", "Jethalal ne maintenance nahi bhara. Phir se. Maine teen registers mein likha hai.");
            L("bhide", "quip", "Madhavi's achar is famous. Her anger is more famous.", "माधवी का अचार मशहूर है। उसका गुस्सा और भी।", "Madhavi ka achar famous hai. Uska gussa aur bhi.");
            L("bhide", "quip", "Tapu Sena broke a window. As secretary, I'm furious. As a father... Sonu was with them.", "टप्पू सेना ने खिड़की तोड़ी। सेक्रेटरी के नाते मैं गुस्सा हूँ। पिता के नाते... सोनू भी उनके साथ थी।", "Tapu Sena ne khidki todi. Secretary ke naate main gussa hoon. Father ke naate... Sonu bhi unke saath thi.");
            L("bhide", "quip", "Aay Golya! Stop eating and go study! Oh, that's for Goli. You keep working.", "ऐ गोल्या! खाना बंद करो और पढ़ो! ओह, वो गोली के लिए था। आप काम करते रहो।", "Aay Golya! Khana band karo aur padho! Oh, woh Goli ke liye tha. Aap kaam karte raho.");
            L("bhide", "quip", "Popatlal asked me to find him a bride. I'm a secretary, not a miracle worker.", "पोपटलाल ने कहा दुल्हन ढूँढो। मैं सेक्रेटरी हूँ, चमत्कार करने वाला नहीं।", "Popatlal ne kaha dulhan dhoondho. Main secretary hoon, chamatkaar karne wala nahi.");
            L("bhide", "quip", "Maths tip: 30 minutes times 16 equals 8 glasses of water a day. Simple.", "गणित टिप: 30 मिनट गुणा 16 बराबर दिन में 8 गिलास पानी। आसान।", "Maths tip: 30 minute guna 16 barabar din mein 8 glass paani. Simple.");
            L("bhide", "care", "Don't worry, {user}. Every problem is a sum. Solve it step by step.", "चिंता मत करो {user}। हर समस्या एक सवाल है। कदम-दर-कदम हल करो।", "Chinta mat karo {user}. Har problem ek sum hai. Step by step solve karo.");
            L("bhide", "care", "I'm strict because I care. Same with my students. Same with you.", "मैं सख्त हूँ क्योंकि परवाह करता हूँ। अपने छात्रों की भी। आपकी भी।", "Main strict hoon kyunki parwah karta hoon. Apne students ki bhi. Aapki bhi.");
            L("bhide", "care", "Bad day? Sakharam has had many flat tyres. He still runs. So will you.", "बुरा दिन? सखाराम के कई टायर पंचर हुए हैं। वो अब भी चलता है। आप भी चलोगे।", "Bura din? Sakharam ke kai tyre puncture hue hain. Woh ab bhi chalta hai. Aap bhi chaloge.");
            L("bhide", "wisdom", "Discipline is doing it even when Jethalal is watching and laughing.", "अनुशासन है वो करना जब जेठालाल देख कर हँस रहा हो।", "Discipline hai woh karna jab Jethalal dekh kar hans raha ho.");
            L("bhide", "wisdom", "Small savings, small habits, big results. Ask Madhavi's achar business.", "छोटी बचत, छोटी आदतें, बड़े नतीजे। माधवी के अचार बिज़नेस से पूछो।", "Chhoti bachat, chhoti aadatein, bade nateeje. Madhavi ke achar business se poocho.");
            L("bhide", "act_dance", "Lezim dance! Ganpati Bappa Morya!", "लेज़िम डांस! गणपति बाप्पा मोरया!", "Lezim dance! Ganpati Bappa Morya!");
            L("bhide", "act_eat", "Modak! Madhavi made them. Bappa's favourite and mine.", "मोदक! माधवी ने बनाए। बाप्पा के पसंदीदा और मेरे भी।", "Modak! Madhavi ne banaye. Bappa ke favourite aur mere bhi.");
            L("bhide", "act_sing", "Singing a Marathi bhajan. Softly. Society rules.", "मराठी भजन गा रहा हूँ। धीरे से। सोसाइटी के नियम।", "Marathi bhajan ga raha hoon. Dheere se. Society rules.");
            L("bhide", "act_exercise", "Morning exercise, evening edition. Secretary must stay fit.", "सुबह की कसरत, शाम का संस्करण। सेक्रेटरी को फिट रहना चाहिए।", "Morning exercise, evening edition. Secretary ko fit rehna chahiye.");
            L("bhide", "act_think", "Thinking... how to make Jethalal pay maintenance. Impossible sum.", "सोच रहा हूँ... जेठालाल से मेंटेनेंस कैसे लूँ। असंभव सवाल।", "Soch raha hoon... Jethalal se maintenance kaise loon. Impossible sum.");
            L("bhide", "act_sneeze", "Achoo! Sakharam's exhaust, I think.", "आक्छू! सखाराम का धुआँ, शायद।", "Achoo! Sakharam ka dhuan, shayad.");
            L("bhide", "act_yawn", "Yaaawn. Three tuition batches today. Zero students learned anything.", "हाआआ। आज तीन ट्यूशन बैच। किसी ने कुछ नहीं सीखा।", "Yaaawn. Aaj teen tuition batch. Kisi ne kuch nahi seekha.");
            L("bhide", "act_peek", "Is Jethalal here? No? Good. I'm back.", "जेठालाल है? नहीं? अच्छा। मैं वापस आ गया।", "Jethalal hai? Nahi? Achha. Main wapas aa gaya.");
            L("bhide", "act_phone", "Checking the society WhatsApp group. 200 messages. All from Popatlal.", "सोसाइटी व्हाट्सएप ग्रुप देख रहा हूँ। 200 मैसेज। सब पोपटलाल के।", "Society WhatsApp group check kar raha hoon. 200 messages. Sab Popatlal ke.");
            L("bhide", "act_spin", "Arre deva! The world is spinning! Like Sakharam's wheels!", "अरे देवा! दुनिया घूम रही है! सखाराम के पहियों की तरह!", "Arre deva! Duniya ghoom rahi hai! Sakharam ke pahiyon ki tarah!");
            L("bhide", "act_laugh", "Hahaha! Jethalal got scolded by Bapuji again!", "हाहाहा! जेठालाल को बापूजी ने फिर डाँटा!", "Hahaha! Jethalal ko Bapuji ne phir daanta!");
            L("bhide", "act_stretch", "Stretching. Even secretaries have back pain.", "स्ट्रेच कर रहा हूँ। सेक्रेटरी की भी कमर दुखती है।", "Stretch kar raha hoon. Secretary ki bhi kamar dukhti hai.");
            L("bhide", "act_walk", "Society inspection round. Everything must be in order.", "सोसाइटी निरीक्षण चक्कर। सब कुछ व्यवस्थित होना चाहिए।", "Society inspection round. Sab kuch order mein hona chahiye.");
            L("bhide", "act_scooter", "Sakharam! Put-put-put! Best scooter in Gokuldham!", "सखाराम! पुट-पुट-पुट! गोकुलधाम का सबसे अच्छा स्कूटर!", "Sakharam! Put-put-put! Gokuldham ka best scooter!");
            L("bhide", "act_chalk", "Tuition time! Two plus two is four. Write it down!", "ट्यूशन टाइम! दो और दो चार। लिखो!", "Tuition time! Do aur do chaar. Likho!");
            L("bhide", "act_register", "Updating the society register. Jethalal: still pending.", "सोसाइटी रजिस्टर अपडेट कर रहा हूँ। जेठालाल: अभी भी बाकी।", "Society register update kar raha hoon. Jethalal: abhi bhi pending.");
            L("bhide", "act_jar", "Madhavi's achar! Only 50 rupees. Best in Gokuldham!", "माधवी का अचार! सिर्फ 50 रुपये। गोकुलधाम का सबसे अच्छा!", "Madhavi ka achar! Sirf 50 rupaye. Gokuldham ka best!");
            L("bhide", "switch_in", "Atmaram Tukaram Bhide, reporting! This desktop now has a secretary.", "आत्माराम तुकाराम भिड़े, हाज़िर! इस डेस्कटॉप को अब एक सेक्रेटरी मिल गया।", "Atmaram Tukaram Bhide, hazir! Is desktop ko ab ek secretary mil gaya.");
            L("bhide", "rename", "{name}? Noted in the register. Official name change approved.", "{name}? रजिस्टर में दर्ज। आधिकारिक नाम परिवर्तन मंज़ूर।", "{name}? Register mein note kiya. Official naam change approved.");
            L("bhide", "levelup", "Level {level}! {title}! Even Sakharam is jealous of our bond.", "लेवल {level}! {title}! सखाराम भी हमारे रिश्ते से जलता है।", "Level {level}! {title}! Sakharam bhi humare rishte se jalta hai.");
            L("bhide", "hide", "Fine, I'll go to the tray. Ctrl+Alt+B to call the secretary.", "ठीक है, ट्रे में जाता हूँ। सेक्रेटरी को बुलाने के लिए Ctrl+Alt+B।", "Theek hai, tray mein jaata hoon. Secretary ko bulane ke liye Ctrl+Alt+B.");
            L("bhide", "exit", "Bye {user}! Drink water, follow the rules, Ganpati Bappa Morya!", "बाय {user}! पानी पियो, नियम मानो, गणपति बाप्पा मोरया!", "Bye {user}! Paani piyo, rules follow karo, Ganpati Bappa Morya!");

            MoreShinchanSpongeBob();
            MoreDoraemonJethalal();
            MoreBhideCommon();
            try { LoadCustom(Paths.CustomLinesFile); LoadCustom(Paths.CustomLinesFile2); } catch { }
        }
    }
}
