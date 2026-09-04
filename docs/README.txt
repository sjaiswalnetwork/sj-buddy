SJ BUDDY  -  your desktop toon companion          Made by SJ
============================================================

WHAT IS IT
  A small desktop pet that sits above your taskbar, reacts when you type,
  reminds you to drink water, learns your daily routine and cracks jokes in
  English / Hindi / Hinglish.
  Characters: Shinchan, SpongeBob, Doraemon, Jethalal, Bhide.

HOW TO USE (30 seconds)
  1. Extract the zip anywhere (Desktop is fine).
  2. Double-click "SJ Buddy.exe". No installation, nothing else needed.
     Works on Windows 10 / 11.
     If Windows shows "Windows protected your PC": click "More info" and then
     "Run anyway". The app is new and unsigned, so SmartScreen is cautious.
     It is offline and open: the full source code is in the "source" folder.
  3. Pick your character, name it, choose the water interval and tick
     "Start with Windows" if you want it every day. Done.

CONTROLS
  Left-click the buddy ....... it talks / reacts
                               (during a water reminder: click = "I drank")
  Double-click ............... pet it
  Drag ....................... move it anywhere on the screen
  Right-click ................ full menu: change character, rename, language,
                               water interval, size, settings, stats, hide, exit
  Hover the mouse over it .... it notices you (blushes, leans in)
  Move the mouse around ...... its eyes follow the cursor; zoom past and it flinches
  Tray icon (near the clock) . left-click shows it, right-click opens the menu,
                               double-click hides / shows it
  Ctrl+Alt+B ................. hide / show
  Exit ....................... right-click -> Exit (also from the tray icon)

WATER REMINDER
  Every 30 minutes by default (15 / 20 / 30 / 45 / 60 / 90 minutes, or off).
  It waits if you are away from the PC, nudges you again if you ignore it and
  gets more serious if you keep ignoring it. Click the buddy after you drink.
  It keeps your daily sips, average, best day and streak.

WHAT IT LEARNS (automatically, only on your PC)
  - When your day usually starts and ends, your lunch hour, your peak hours
  - Your normal typing speed (so "fast" means fast for YOU)
  - How chatty you like it: click a line you like and it does more of those;
    hide it right after a line and it learns to talk less
  - How you respond to water reminders (gentle / persistent / serious style)
  - Late nights and long typing sessions -> eye-rest, posture and break nudges
  - XP and levels from sips, clicks, pets, typing minutes and breaks
  Right-click -> "Stats and what I learned" shows everything in plain words.

PRIVACY
  It never records what you type. To know that you are typing, it only checks
  whether typing keys are currently pressed and counts the presses; nothing is
  stored, logged or sent anywhere. No keyboard hook, no internet, no accounts.
  Its only files are settings.txt and brain.txt in %APPDATA%\SJ Buddy.
  Delete that folder to reset everything.

ADD YOUR OWN LINES
  Create a text file named my-lines.txt next to SJ Buddy.exe
  (or in %APPDATA%\SJ Buddy). One entry per line:
      character|category|english|hindi|hinglish
  character: shinchan, spongebob, doraemon, jethalal, bhide, or * for all
  category:  quip, care, wisdom, water, water_ack, water_nag, click, pet,
             greet_morning, greet_afternoon, greet_evening, greet_night,
             typing_start, typing_fast, idle_short, lunch, late_night, ...
  Example:
      jethalal|quip|Bapuji is watching. Work!|बापूजी देख रहे हैं। काम करो!|Bapuji dekh rahe hain. Kaam karo!

EXTRAS
  "SJ Buddy.exe" --settings   opens the Settings window
  "SJ Buddy.exe" --stats      opens the Stats window
  A file named portable.txt next to the exe keeps the settings next to the exe
  (pendrive mode).

REBUILD FROM SOURCE
  Run build.bat inside the "source" folder. It needs only Windows itself
  (the C# compiler that ships with .NET Framework 4.x).

Characters belong to their respective creators. This is a fan-made companion
for personal use.                                              Made by SJ
