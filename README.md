# VGM-Studio
A tool for viewing, extracting, and converting sound data from VGM music log files

This program is the latest iteration of GYM2MID (later called VGM2MID), which I started back in the late 1990s when the developers at Bloodlust Software created the GYM file format and added it to their Genecyst Megadrive/Genesis emulator. I've been working on it off and on (mostly off) ever since then. GYM2MID was my first big project and one I keep coming back to.

The original reason for creating GYM2MID was to convert the soundtrack from Phantasy Star II into MIDI format so that I could hear how it would sound using "real" instruments on my Sound Blaster AWE 32. This is an idea I came back to recently while I was working on VGM Studio. I completed an arranged soundtrack in early 2026. You can listen to it on [YouTube](https://www.youtube.com/playlist?list=PLumw1-sMVdoRH07dYzjm6iJDDN-zEdt6c).

One big thing I learned though is the importance of the original synth sounds themselves. "Real" instruments are great, but those old synthesized sounds really are the flavor of the music. I recall reading a post from Yuzo Koshiro on Ancient's old message board where he mentioned that synth chips are just another type of instrument, and that it's best to use them in their own unique way. That's why it's been cool to see projects like Genny and Dexed appear over the years that let people create using new music using those old sounds. It's kind of wild to extract, say, that bell lead from Star Light Zone (you know the one) and play a tune with it on a keyboard.

Other people such as [ValleyBell](https://github.com/valleybell) and the folks and [VGMRips](https://vgmrips.net/packs/) have done a lot of great work pushing the VGM format forward, way beyond what I did with GYM2MID, but I like to think that I helped get the ball rolling on that front. So, even though VGM Studio isn't as complete as a lot of tools out there, I thought maybe it was time to put my recent work up here so people can check it out.

And yes, most of it is written in Visual Basic .Net. I wrote the original in Visual Basic 6, and never got around to doing a rewrite in C# or whatever. The parts that I ported over from C (the Nuked emulators) are all in C# because it was MUCH easier to do it that way. Anyway, enjoy!

Version 0.9 (Nov 3, 2026)
- Initial public release
- Features:
  - View info and events contained in VGM music log files (full support up to VGM 1.61, partial support for 1.70 and above) 
  - Export FM voices to Yamaha DX-7 compatible .SYX files for use with Dexed or an actual DX-7
  - Preview voices from PCM-based chips
  - Export chip events to .MID and .WAV
  - Mostly-reliable automatic tempo detection for midi files
- Supported chips:
  - Yamaha OPN series: YM2203 and YM2612 (MIDI export; WAV export via Nuked-OPN2)
  - Yamaha OPM series: YM2151 (MIDI export; WIP WAV export vs Nuked-OPM)
  - Yamaha OPL series: YM3526 and YM3812 (MIDI export)
  - Yamaha OPLL series: YM2413 (MIDI export)
  - PSG: TI SN76489 (MIDI export; WAV export via internal emulator or Nuked-PSG)
  - SSG: YM2149 (MIDI export)
  - SegaPCM (WIP MIDI export; WAV export via internal emulator)
  - Ricoh RFC5164 (WIP MIDI support; WAV export via internal emulator)
- Planned features:
  - PCM voice export
  - Custom mapping of PCM voices to MIDI instruments/notes
  - Support for more sound chips
- Issues:
  - Some menu commands are available, but don't actually do anything.
  - Nuked-OPM puts out really buggy sound. I'm sure I made some logic mistakes porting it over from C.
