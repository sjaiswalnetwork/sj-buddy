# LinkedIn post kit for SJ Buddy

**Images:** attach `linkedin-1.png` and `linkedin-2.png` (LinkedIn shows them as a two-image carousel).
**Links go in the FIRST COMMENT**, not the post body. LinkedIn shows posts containing outside links to fewer people.
**Optional follow-up post:** upload `SJ Buddy - Guide.pdf` as a *Document* post a few days later. LinkedIn turns PDFs into a swipeable carousel.

GitHub: https://github.com/sjaiswalnetwork/sj-buddy/releases/latest/download/SJ-Buddy-v1.0.zip
Google Drive: https://drive.google.com/file/d/1SGSwMQ-sH4FaVpN3f-oPuL_y9j58v6hq/view?usp=sharing
Project page: https://github.com/sjaiswalnetwork/sj-buddy

Both links verified working for people who are not signed in.

---

## MAIN POST (use this one)

I gave Claude Fable 5.1 one prompt and 7 screenshots.

Two hours later, Shinchan was living on my taskbar. 🤖

SJ Buddy is a free desktop pet for Windows. It types along when you type, reminds you to drink water every 30 minutes, and jokes with you in Hindi, English and Hinglish.

Five characters: Shinchan, SpongeBob, Doraemon, Jethalal, Bhide.
Bhide rides Sakharam across your taskbar. Jethalal yells "Hey Maa Mataji!" when you skip your water.

The build is the part that got me:

→ It researched all five characters so the lines actually sound like them
→ It drew every character in code. Zero image files.
→ It built a brain that learns my start time, lunch hour and typing speed
→ It ran the app, screenshotted itself, found DPI bugs, fixed them
→ It killed an antivirus false positive on its own
→ It shipped a 320 KB exe, a PDF guide and the full source

~4,000 lines of C#. 990 dialogue lines. 3 languages. One session.

Then I said "give every character 50 more lines" and it came back in minutes.

Free for everyone. Links in the comments 👇

Made by SJ.

#Claude #ClaudeCode #Anthropic #AI #BuildInPublic #Windows #DesktopPet

---

## FIRST COMMENT (paste right after posting)

Free download, Windows 10/11, no installation:

GitHub: https://github.com/sjaiswalnetwork/sj-buddy/releases/latest/download/SJ-Buddy-v1.0.zip
Google Drive: https://drive.google.com/file/d/1SGSwMQ-sH4FaVpN3f-oPuL_y9j58v6hq/view?usp=sharing

How to run it:
1. Download the zip from either link
2. Right-click it, choose Extract All
3. Double-click SJ Buddy.exe
4. If Windows says "protected your PC", click More info, then Run anyway (the app is unsigned, the full source is on GitHub)
5. Pick your character and go

Source code: https://github.com/sjaiswalnetwork/sj-buddy

---

## EVEN SHORTER (if you want a one-screen post)

One prompt. Seven screenshots. Two hours.

Claude Fable 5.1 built me a desktop pet from scratch. 🤖

Shinchan, SpongeBob, Doraemon, Jethalal or Bhide sits on your taskbar, types along when you type, reminds you to drink water, and jokes in Hindi, English and Hinglish.

Fable researched the characters, drew all five in code, built the learning logic, tested itself with its own screenshots, and shipped a 320 KB exe with a PDF guide.

~4,000 lines of C#. 990 dialogue lines. One session.

Free for everyone. Links in the comments 👇 Made by SJ.

#Claude #ClaudeCode #AI #BuildInPublic #Windows

---

## REPLY TEMPLATES for common comments

- **"Is it safe?"** → Fully offline, no keyboard hook, never records what you type. The complete source is on GitHub so anyone can read it or rebuild the exe with build.bat. SmartScreen warns only because it is unsigned.
- **"Mac / Linux?"** → Windows only for now, it is a WinForms app. Source is open if someone wants to port it.
- **"Can I add my own jokes?"** → Yes, create my-lines.txt next to the exe. Format and categories are in the README.
- **"Which model / tool?"** → Claude Fable 5.1 inside Claude Code, running on my own Windows laptop. It wrote, built, ran and screenshot-tested the app itself.
- **"Can you add <character>?"** → The characters are drawn in code, so new ones are doable. Drop a name and I will look at it.
