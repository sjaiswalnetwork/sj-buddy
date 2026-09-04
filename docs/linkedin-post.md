# LinkedIn post kit for SJ Buddy

Attach **linkedin-1.png** and **linkedin-2.png** as the post images (LinkedIn shows them as a two-image carousel).
Put the download link in the **first comment** (LinkedIn shows link posts to fewer people) and edit the post to say "link in comments" once the comment is up.
Optional second post a few days later: upload **SJ Buddy - Guide.pdf** as a *Document* post; LinkedIn turns PDFs into a swipeable carousel.

**LIVE download link:** https://github.com/sjaiswalnetwork/sj-buddy/releases/latest/download/SJ-Buddy-v1.0.zip
**Project page:** https://github.com/sjaiswalnetwork/sj-buddy

---

## Main post (about 2,200 characters)

I gave an AI one prompt and 7 screenshots. Two hours later, this little guy was sitting on my taskbar. 🤖💧

Meet SJ Buddy: a free desktop pet for Windows that reacts when you type, reminds you to drink water every 30 minutes, learns your routine, and cracks jokes in Hindi, English and Hinglish.

Five characters: Shinchan, SpongeBob, Doraemon, Jethalal and Bhide. Yes, Bhide rides Sakharam across your taskbar. Yes, Jethalal says "Hey Maa Mataji!" when you ignore your water.

What amazed me was HOW it got built, with Claude Fable 5.1 (Anthropic's newest model) inside Claude Code:

🔍 It researched all five characters first (catchphrases, running gags, rivalries) so the lines actually sound like them.
🎨 No image files anywhere. It DREW every character in code: eyes, mouths, arms, a laptop, a water glass, a bamboo copter, a scooter, a chalkboard.
🧠 It designed a "brain" that learns your day: usual start time, lunch hour, typing speed, how chatty you like it, how fast you answer reminders. Rule-based, fully offline, nothing leaves the PC.
🧪 It ran the app, took its own screenshots, spotted layout bugs on my 125% DPI screen and fixed them.
🛡️ When McAfee flagged a tiny helper file, it rebuilt the pipeline so no helper exe exists and removed the keyboard hook entirely.
📦 It packaged everything: one 320 KB exe, a PDF guide, and the full source with a one-click build script.

The numbers: about 4,000 lines of C#, 990 dialogue entries (each in 3 languages), 5 characters, 40+ animations, compiled with the compiler that ships inside Windows, so it runs on any Windows 10/11 PC with zero installation.

Then I asked for changes ("remove the logo", "it treats my mouse like typing", "give every character 50+ lines") and each came back working in minutes.

It's free and anyone can use it. Download link in the first comment 👇
Extract the zip, double-click SJ Buddy.exe, pick your character. If Windows says "protected your PC", click More info, then Run anyway (it's unsigned; the source is included).

Made by SJ.

#Claude #Anthropic #ClaudeCode #AI #BuildInPublic #DesktopPet #Productivity #CSharp #Windows #Doraemon #Shinchan #SpongeBob #TMKOC

---

## First comment (paste right after posting)

Download SJ Buddy, free, Windows 10/11, no install:
https://github.com/sjaiswalnetwork/sj-buddy/releases/latest/download/SJ-Buddy-v1.0.zip

Source code and guide: https://github.com/sjaiswalnetwork/sj-buddy

Extract the zip, double-click SJ Buddy.exe, pick your character. If Windows shows "protected your PC": More info, then Run anyway (the app is unsigned; the full source is in the repo).

---

## Short version (about 700 characters)

One prompt, 7 screenshots, 2 hours. Claude Fable 5.1 built me a desktop pet from scratch. 🤖💧

SJ Buddy sits above your taskbar as Shinchan, SpongeBob, Doraemon, Jethalal or Bhide. It types along when you type, reminds you to drink water, learns your routine, and jokes in Hindi, English and Hinglish.

Fable researched the characters, drew all five in code (no image files), designed the learning logic, tested it with its own screenshots, fixed an antivirus false positive, and packaged a single 320 KB exe with a PDF guide. About 4,000 lines of C# and 990 dialogue lines.

Free for anyone. Link in the first comment. Made by SJ.

#Claude #ClaudeCode #AI #BuildInPublic #Windows

---

## Reply templates for common comments

- "Is it safe?" -> It's fully offline, has no keyboard hook, never records what you type, and the complete source code is on GitHub so anyone can read it or rebuild the exe with build.bat. SmartScreen warns only because it's unsigned.
- "Mac / Linux?" -> Windows only for now (it's a WinForms app). The source is open if someone wants to port it.
- "Can I add my own jokes?" -> Yes: create my-lines.txt next to the exe. Format and categories are in the README.
- "Which model / tool?" -> Claude Fable 5.1 inside Claude Code, on my own Windows laptop. It wrote, built, ran and screenshot-tested the app itself.
