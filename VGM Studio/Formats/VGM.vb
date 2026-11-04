Imports System.ComponentModel
Imports System.IO

Public Class VGM

    'VGM Specification v1.72
    '=============================================================================
    'VGM (Video Game Music) is a sample-accurate sound logging format for the Sega
    'Master System, the Sega Game Gear and possibly many other machines (e.g. Sega
    'Genesis).

    'The normal file extension is .vgm but files can also be GZip compressed into
    '.vgz files. However, a VGM player should attempt to support compressed and
    'uncompressed files with either extension. (ZLib's GZIO library makes this
    'trivial to implement.)

    'The format starts with a 256 byte header:

    '      00  01  02  03    04  05  06  07   08  09  0A  0B  0C  0D  0E  0F
    '0x00 ["Vgm " ident    ][EoF offset     ][Version        ][SN76489 clock  ]
    '0x10 [YM2413 clock    ][GD3 offset     ][Total # samples][Loop offset    ]
    '0x20 [Loop # samples  ][Rate           ][SN FB ][SNW][SF][YM2612 clock   ]
    '0x30 [YM2151 clock    ][VGM data offset][Sega PCM clock ][SPCM Interface ]
    '0x40 [RF5C68 clock    ][YM2203 clock   ][YM2608 clock   ][YM2610/B clock ]
    '0x50 [YM3812 clock    ][YM3526 clock   ][Y8950 clock    ][YMF262 clock   ]
    '0x60 [YMF278B clock   ][YMF271 clock   ][YMZ280B clock  ][RF5C164 clock  ]
    '0x70 [PWM clock       ][AY8910 clock   ][AYT][AY Flags  ][VM] *** [LB][LM]
    '0x80 [GB DMG clock    ][NES APU clock  ][MultiPCM clock ][uPD7759 clock  ]
    '0x90 [OKIM6258 clock  ][OF][KF][CF][***][OKIM6295 clock ][K051649 clock  ]
    '0xA0 [K054539 clock   ][HuC6280 clock  ][C140 clock     ][K053260 clock  ]
    '0xB0 [Pokey clock     ][QSound clock   ][SCSP clock     ][Extra Hdr ofs  ]
    '0xC0 [WonderSwan clock][VSU clock      ][SAA1099 clock  ][ES5503 clock   ]
    '0xD0 [ES5506 clock    ][ES chns][CD][**][X1-010 clock   ][C352 clock     ]
    '0xE0 [GA20 clock      ][Mikey clock    ] *** *** *** ***  *** *** *** ***
    '0xF0  *** *** *** ***   *** *** *** ***  *** *** *** ***  *** *** *** ***

    '- Unused space (marked with *) is reserved for future expansion, and must be
    '  zero.
    '- All integer values are *unsigned* and written in "Intel" byte order (Little
    '  Endian), so for example 0x12345678 is written as 0x78 0x56 0x34 0x12.
    '- All pointer offsets are written as relative to the current position in the
    '  file, so for example the GD3 offset at 0x14 in the header is the file
    '  position of the GD3 tag minus 0x14.
    '- All header sizes are valid for all versions from 1.50 on, as long as header
    '  has at least 64 bytes. If the VGM data starts at an offset that is lower
    '  than 0x100, all overlapping header bytes have to be handled as they were
    '  zero.
    '- VGMs run with a rate of 44100 samples per second. All sample values use this
    '  unit.

    '0x00:  "Vgm " (0x56 0x67 0x6d 0x20) file identification (32 bits)
    '0x04:  Eof offset (32 bits)
    '       Relative offset to end of file (i.e. file length - 4).
    '       This is mainly used to find the next track when concatanating
    '       player stubs and multiple files.
    '0x08:  Version number (32 bits)
    '       Version number in BCD-Code. e.g. Version 1.70 is stored as 0x00000170.
    '       This is used for backwards compatibility in players, and defines which
    '       header values are valid.
    '0x0C:  SN76489 clock (32 bits)
    '       Input clock rate in Hz for the SN76489 PSG chip. A typical value is
    '       3579545. It should be 0 if there is no PSG chip used.
    '       Note: Bit 31 (0x80000000) is used on combination with the dual-chip-bit
    '       to indicate that this is a T6W28. (PSG variant used in NeoGeo Pocket)
    '0x10:  YM2413 clock (32 bits)
    '       Input clock rate in Hz for the YM2413 chip. A typical value is 3579545.
    '       It should be 0 if there is no YM2413 chip used.
    '0x14:  GD3 offset (32 bits)
    '       Relative offset to GD3 tag. 0 if no GD3 tag.
    '       GD3 tags are descriptive tags similar in use to ID3 tags in MP3 files.
    '       See the GD3 specification for more details. The GD3 tag is usually
    '       stored immediately after the VGM data.
    '0x18:  Total # samples (32 bits)
    '       Total of all wait values in the file.
    '0x1C:  Loop offset (32 bits)
    '       Relative offset to loop point, or 0 if no loop.
    '       For example, if the data for the one-off intro to a song was in bytes
    '       0x0040-0x3FFF of the file, but the main looping section started at
    '       0x4000, this would contain the value 0x4000-0x1C = 0x00003FE4.
    '0x20:  Loop # samples (32 bits)
    '       Number of samples in one loop, or 0 if there is no loop.
    '       Total of all wait values between the loop point and the end of
    '       the file.
    '[VGM 1.01 additions:]
    '0x24:  Rate (32 bits)
    '       "Rate" of recording in Hz, used for rate scaling on playback. It is
    '       typically 50 for PAL systems and 60 for NTSC systems. It should be set
    '       to zero if rate scaling is not appropriate - for example, if the game
    '       adjusts its music engine for the system's speed.
    '       VGM 1.00 files will have a value of 0.
    '[VGM 1.10 additions:]
    '0x28:  SN76489 feedback (16 bits)
    '       The white noise feedback pattern for the SN76489 PSG. Known values are:
    '       0x0009  Sega Master System 2/Game Gear/Mega Drive
    '               (SN76489/SN76496 integrated into Sega VDP chip)
    '       0x0003  Sega Computer 3000H, BBC Micro
    '               (SN76489AN)
    '       0x0006  SN76494, SN76496
    '       For version 1.01 and earlier files, the feedback pattern should be
    '       assumed to be 0x0009. If the PSG is not used then this may be omitted
    '       (left at zero).
    '0x2A:  SN76489 shift register width (8 bits)
    '       The noise feedback shift register width, in bits. Known values are:
    '       16  Sega Master System 2/Game Gear/Mega Drive
    '           (SN76489/SN76496 integrated into Sega VDP chip)
    '       15  Sega Computer 3000H, BBC Micro
    '           (SN76489AN)
    '       For version 1.01 and earlier files, the shift register width should be
    '       assumed to be 16. If the PSG is not used then this may be omitted (left
    '       at zero).
    '[VGM 1.51 additions:]
    '0x2B:  SN76489 Flags (8 bits)
    '       Misc flags for the SN76489. Most of them don't make audible changes and
    '       can be ignored, if the SN76489 emulator lacks the features.
    '       bit 0   frequency 0 is 0x400
    '       bit 1   output negate flag
    '       bit 2   stereo on/off (on when bit clear)
    '       bit 3   /8 Clock Divider on/off (on when bit clear)
    '       bit 4-7 reserved (must be zero)
    '       For version 1.51 and earlier files, all the flags should not be set.
    '       If the PSG is not used then this may be omitted (left at zero).
    '[VGM 1.10 additions:]
    '0x2C:  YM2612 clock (32 bits)
    '       Input clock rate in Hz for the YM2612 chip. A typical value is 7670454.
    '       It should be 0 if there us no YM2612 chip used.
    '       For version 1.01 and earlier files, the YM2413 clock rate should be
    '       used for the clock rate of the YM2612.
    '0x30:  YM2151 clock (32 bits)
    '       Input clock rate in Hz for the YM2151 chip. A typical value is 3579545.
    '       It should be 0 if there us no YM2151 chip used.
    '       For version 1.01 and earlier files, the YM2413 clock rate should be
    '       used for the clock rate of the YM2151.
    '[VGM 1.50 additions:]
    '0x34:  VGM data offset (32 bits)
    '       Relative offset to VGM data stream.
    '       If the VGM data starts at absolute offset 0x40, this will contain 
    '       value 0x0000000C. For versions prior to 1.50, it should be 0 and the
    '       VGM data must start at offset 0x40.
    '[VGM 1.51 additions:]
    '0x38: Sega PCM clock (32 bits)
    '       Input clock rate in Hz for the Sega PCM chip. A typical value is
    '       4000000. It should be 0 if there is no Sega PCM chip used.
    '0x3C:  Sega PCM interface register (32 bits)
    '       The interface register for the Sega PCM chip. It should be 0 if there
    '       is no Sega PCM chip used.
    '0x40:  RF5C68 clock (32 bits)
    '       Input clock rate in Hz for the RF5C68 PCM chip. A typical value is
    '       12500000. It should be 0 if there is no RF5C68 chip used.
    '0x44:  YM2203 clock (32 bits)
    '       Input clock rate in Hz for the YM2203 chip. A typical value is 3000000.
    '       It should be 0 if there is no YM2203 chip used.
    '0x48:  YM2608 clock (32 bits)
    '       Input clock rate in Hz for the YM2608 chip. A typical value is 8000000.
    '       It should be 0 if there is no YM2608 chip used.
    '0x4C:  YM2610/YM2610B clock (32 bits)
    '       Input clock rate in Hz for the YM2610/B chip. A typical value is
    '       8000000. It should be 0 if there is no YM2610/B chip used.
    '       Note: Bit 31 is used to set whether it is an YM2610 or an YM2610B chip.
    '       If bit 31 is set it is an YM2610B, if bit 31 is clear it is an YM2610.
    '0x50:  YM3812 clock (32 bits)
    '       Input clock rate in Hz for the YM3812 chip. A typical value is 3579545.
    '       It should be 0 if there is no YM3812 chip used.
    '0x54:  YM3526 clock (32 bits)
    '       Input clock rate in Hz for the YM3526 chip. A typical value is 3579545.
    '       It should be 0 if there is no YM3526 chip used.
    '0x58:  Y8950 clock (32 bits)
    '       Input clock rate in Hz for the Y8950 chip. A typical value is 3579545.
    '       It should be 0 if there is no Y8950 chip used.
    '0x5C:  YMF262 clock (32 bits)
    '       Input clock rate in Hz for the YMF262 chip. A typical value is
    '       14318180. It should be 0 if there is no YMF262 chip used.
    '0x60:  YMF278B clock (32 bits)
    '       Input clock rate in Hz for the YMF278B chip. A typical value is
    '       33868800. It should be 0 if there is no YMF278B chip used.
    '0x64:   YMF271 clock (32 bits)
    '       Input clock rate in Hz for the YMF271 chip. A typical value is
    '       16934400. It should be 0 if there is no YMF271 chip used.
    '0x68:  YMZ280B clock (32 bits)
    '       Input clock rate in Hz for the YMZ280B chip. A typical value is
    '       16934400. It should be 0 if there is no YMZ280B chip used.
    '0x6C:  RF5C164 clock (32 bits)
    '       Input clock rate in Hz for the RF5C164 PCM chip. A typical value is
    '       12500000. It should be 0 if there is no RF5C164 chip used.
    '0x70:  PWM clock (32 bits)
    '       Input clock rate in Hz for the PWM chip. A typical value is
    '       23011361. It should be 0 if there is no PWM chip used.
    '0x74:  AY8910 clock (32 bits)
    '       Input clock rate in Hz for the AY8910 chip. A typical value is 1789750.
    '       It should be 0 if there is no AY8910 chip used.
    '0x78:  AY8910 Chip Type (8 bits)
    '       Defines the exact type of AY8910. The values are:
    '       0x00 - AY8910
    '       0x01 - AY8912
    '       0x02 - AY8913
    '       0x03 - AY8930
    '       0x10 - YM2149
    '       0x11 - YM3439
    '       0x12 - YMZ284
    '       0x13 - YMZ294
    '       If the AY8910 is not used then this may be omitted (left at zero).
    '0x79:  AY8910 Flags (8 bits)
    '       Misc flags for the AY8910. Default is 0x01.
    '       For additional description see ay8910.h in MAME source code.
    '       bit 0   Legacy Output
    '       bit 1   Single Output
    '       bit 2   Discrete Output
    '       bit 3   RAW Output
    '       bit 4-7 reserved (must be zero)
    '       If the AY8910 is not used then this may be omitted (left at zero).
    '0x7A:  YM2203/AY8910 Flags (8 bits)
    '       Misc flags for the AY8910. This one is specific for the AY8910 that's
    '       connected with/part of the YM2203.
    '0x7B:  YM2608/AY8910 Flags (8 bits)
    '       Misc flags for the AY8910. This one is specific for the AY8910 that's
    '       connected with/part of the YM2608.
    '[VGM 1.60 additions:]
    '0x7C:  Volume Modifier (8 bits)
    '       Volume = 2 ^ (VolumeModifier / 0x20) where VolumeModifier is a number
    '       from -63 to 192 (-63 = 0xC1, 0 = 0x00, 192 = 0xC0). Also the value -63
    '       gets replaces with -64 in order to get make factor of 0.25 possible.
    '       Therefore the volume can reach levels between 0.25 and 64.
    '       Default is 0, which is equal to a factor of 1 or 100%.
    '       Note: Players should support the Volume Modifier in v1.50 files and
    '       higher. This way MegaDrive VGMs can use the Volume Modifier without
    '       breaking compatibility with old players.
    '0x7D:  RESERVED
    '       Reserved byte for future use. It must be 0.
    '0x7E:  Loop Base (8 bits)
    '       Modifies the number of loops that are played before the playback ends.
    '       Set this value to eg. 1 to reduce the number of played loops by one.
    '       This is useful, if the song is looped twice in the vgm, because there
    '       are minor differences between the first and second loop and the song
    '       repeats just the second loop.
    '       The resulting number of loops that are played is calculated as
    '       following: NumLoops = NumLoopsModified - LoopBase
    '       Default is 0. Negative numbers are possible (80h...FFh = -128...-1)
    '[VGM 1.51 additions:]
    '0x7F:  Loop Modifier (8 bits)
    '       Modifies the number of loops that are played before the playback ends.
    '       You may want to use this, e.g. if a tune has a very short, but non-
    '       repetive loop (then set it to 0x20 double the loop number).
    '       The resulting number of loops that are played is calculated as
    '       following: NumLoops = ProgramNumLoops * LoopModifier / 0x10
    '       Default is 0, which is equal to 0x10.
    '[VGM 1.61 additions:]
    '0x80:  GameBoy DMG clock (32 bits)
    '       Input clock rate in Hz for the GameBoy DMG chip, LR35902. A typical
    '       value is 4194304. It should be 0 if there is no GB DMG chip used.
    '0x84:  NES APU clock (32 bits)
    '       Input clock rate in Hz for the NES APU chip, N2A03. A typical value is
    '       1789772. It should be 0 if there is no NES APU chip used.
    '0x88:  MultiPCM clock (32 bits)
    '       Input clock rate in Hz for the MultiPCM chip. A typical value is
    '       8053975. It should be 0 if there is no MultiPCM chip used.
    '0x8C:  uPD7759 clock (32 bits)
    '       Input clock rate in Hz for the uPD7759 chip. A typical value is 640000.
    '       It should be 0 if there is no uPD7759 chip used.
    '0x90:  OKIM6258 clock (32 bits)
    '       Input clock rate in Hz for the OKIM6258 chip. A typical value is
    '       4000000. It should be 0 if there is no OKIM6258 chip used.
    '0x94:  OKIM6258 Flags (8 bits)
    '       Misc flags for the OKIM6258. Default is 0x00.
    '       bit 0-1 Clock Divider (clock dividers are 1024, 768, 512, 512)
    '       bit 2   3/4-bit ADPCM select (default is 4-bit, doesn't work currently)
    '       bit 3   10/12-bit Output (default is 10-bit)
    '       bit 4-7 reserved (must be zero)
    '       If the OKIM6258 is not used then this may be omitted (left at zero).
    '0x95:  K054539 Flags (8 bits)
    '       Misc flags for the K054539. Default is 0x01.
    '       See also k054539.h in MAME source code.
    '       bit 0   Reverse Stereo
    '       bit 1   Disable Reverb
    '       bit 2   Update at KeyOn
    '       bit 3-7 reserved (must be zero)
    '       If the K054539 is not used then this may be omitted (left at zero).
    '0x96:  C140 Chip Type (8 bits)
    '       Defines the exact type of C140 and its banking method. The values are:
    '       0x00 - C140, Namco System 2
    '       0x01 - C140, Namco System 21a
    '       0x02 - C140, Namco System 21b
    '       0x03 - 219 ASIC, Namco NA-1/2
    '       If the C140 is not used then this may be omitted (left at zero).
    '0x97:  RESERVED
    '       Reserved byte for future use. It must be 0.
    '0x98:  OKIM6295 clock (32 bits)
    '       Input clock rate in Hz for the OKIM6295 chip. A typical value is
    '       4000000. It should be 0 if there is no OKIM6295 chip used.
    '0x9C:  K051649 clock (32 bits)
    '       Input clock rate in Hz for the K051649 chip. A typical value is
    '       1500000. It should be 0 if there is no K051649 chip used.
    '0xA0:  K054539 clock (32 bits)
    '       Input clock rate in Hz for the K054539 chip. A typical value is 48000.
    '       It should be 0 if there is no K054539 chip used.
    '0xA4:  HuC6280 clock (32 bits)
    '       Input clock rate in Hz for the HuC6280 chip. A typical value is
    '       3579545. It should be 0 if there is no HuC6280 chip used.
    '0xA8:  C140 clock (32 bits)
    '       Input clock rate in Hz for the C140 chip. A typical value is 21390.
    '       It should be 0 if there is no C140 chip used.
    '0xAC:  K053260 clock (32 bits)
    '       Input clock rate in Hz for the K053260 chip. A typical value is 
    '       3579545. It should be 0 if there is no K053260 chip used.
    '0xB0:  Pokey clock (32 bits)
    '       Input clock rate in Hz for the Pokey chip. A typical value is 1789772.
    '       It should be 0 if there is no Pokey chip used.
    '0xB4:  QSound clock (32 bits)
    '       Input clock rate in Hz for the QSound chip. A typical value is 4000000.
    '       It should be 0 if there is no QSound chip used.
    '0xB8:  RESERVED
    '       Reserved bytes for future use. They must be 0.
    '[VGM 1.70 additions:]
    '0xBC: Extra Header Offset
    '       Relative offset to the extra header or 0 if no extra header is present.
    '[VGM 1.71 additions:]
    '0xC0:  WonderSwan clock (32 bits)
    '       Input clock rate in Hz for the WonderSwan chip. A typical value is 3072000.
    '       It should be 0 if there is no WonderSwan chip used.
    'OxC4:  VSU clock (32 bits)
    '       Input clock rate in Hz for the VSU chip. A typical value is 5000000.
    '       It should be 0 if there is no VSU chip used.
    '0xC8:  SAA1099 clock (32 bits)
    '       Input clock rate in Hz for the SAA1099 chip. A typical value is 8000000 (or 7159000/7159090).
    '       It should be 0 if there is no SAA1099 chip used.
    '0xCC:  ES5503 clock (32 bits)
    '       Input clock rate in Hz for the ES5503 chip. A typical value is 7159090.
    '       It should be 0 if there is no ES5503 chip used.
    '0xD0:  ES5505/ES5506 clock (32 bits)
    '       Input clock rate in Hz for the ES5505/ES5506 chip. A typical value is 16000000.
    '       It should be 0 if there is no ES5505/ES5506 chip used.
    '       Note: Bit 31 is used to set whether it is an ES5505 or an ES5506 chip. If bit 31 is set it is an ES5506, if bit 31 is clear it is an ES5505.
    '0xD4:  ES5503 amount of output channels (8 bits)
    '       Defines the internal number of output channels for the ES5503.
    '       Possible values are 1 to 8. A typical value is 2.
    '       If the ES5503 is not used then this may be omitted (left at zero).
    '0xD5:  ES5505/ES5506 amount of output channels (8 bits)
    '       Defines the internal number of output channels for the ES5506.
    '       Possible values are 1 to 4 for the ES5505 and 1 to 8 for the ES5506. A typical value is 1.
    '       If the ES5505/ES5506 is not used then this may be omitted (left at zero).
    '0xD6:  C352 clock divider (8 bits)
    '       Defines the clock divider For the C352 chip, divided by 4 In order To achieve a divider range Of 0 To 1020. A typical value Is 288.
    '       If the C352 is not used then this may be omitted (left at zero).
    '0xD8:  X1-010 clock
    '       Input clock rate in Hz for the X1-010 chip. A typical value is 16000000.
    '       It should be 0 if there is no X1-010 chip used.
    '0xDC:  C352 clock (32 bits)
    '       Input clock rate in Hz for the C352 chip. A typical value is 24192000.
    '       It should be 0 if there is no C352 chip used.
    '0xE0:  GA20 clock (32 bits)
    '       Input clock rate in Hz for the GA20 chip. A typical value is 3579545.
    '       It should be 0 if there is no GA20 chip used.
    '[VGM 1.72 additions:]
    '0xE4:  Mikey clock
    '       Input clock rate in Hz for the Mikey (Atari Lynx) chip. A typical value is 16000000.
    '       It should be 0 if there is no Mikey chip used.


    'Starting at the location specified by the VGM data offset (or, offset 0x40 for
    'file versions below 1.50) is found a sequence of commands containing data
    'written to the chips or timing information. A command is one of:

    '0x4F dd            Game Gear PSG stereo, write dd to port 0x06
    '0x50 dd            PSG (SN76489/SN76496) write value dd
    '0x51 aa dd         YM2413, write value dd to register aa
    '0x52 aa dd         YM2612 port 0, write value dd to register aa
    '0x53 aa dd         YM2612 port 1, write value dd to register aa
    '0x54 aa dd         YM2151, write value dd to register aa
    '0x55 aa dd         YM2203, write value dd to register aa
    '0x56 aa dd         YM2608 port 0, write value dd to register aa
    '0x57 aa dd         YM2608 port 1, write value dd to register aa
    '0x58 aa dd         YM2610 port 0, write value dd to register aa
    '0x59 aa dd         YM2610 port 1, write value dd to register aa
    '0x5A aa dd         YM3812, write value dd to register aa
    '0x5B aa dd         YM3526, write value dd to register aa
    '0x5C aa dd         Y8950, write value dd to register aa
    '0x5D aa dd         YMZ280B, write value dd to register aa
    '0x5E aa dd         YMF262 port 0, write value dd to register aa
    '0x5F aa dd         YMF262 port 1, write value dd to register aa
    '0x61 nn nn         Wait n samples, n can range from 0 to 65535 (approx 1.49
    '                   seconds). Longer pauses than this are represented by multiple
    '                   wait commands.
    '0x62               wait 735 samples (60th of a second), a shortcut for
    '                   0x61 0xdf 0x02
    '0x63               wait 882 samples (50th of a second), a shortcut for
    '                   0x61 0x72 0x03
    '0x64 cc nn nn      override length of 0x62/0x63
    '                   cc - command (0x62/0x63)
    '                   nn - delay in samples
    '                   [Note  Not yet implemented. Am I really sure about this?]
    '0x66               end of sound data
    '0x67 ...           data block  see below
    '0x68 ...           PCM RAM write  see below
    '0x7n               wait n+1 samples, n can range from 0 to 15.
    '0x8n               YM2612 port 0 address 2A write from the data bank, then wait 
    '                   n samples; n can range from 0 to 15. Note that the wait is n,
    '                   NOT n+1. (Note  Written to first chip instance only.)
    '0x90-0x95          DAC Stream Control Write  see below
    '0xA0 aa dd         AY8910, write value dd to register aa
    '0xB0 aa dd         RF5C68, write value dd to register aa
    '0xB1 aa dd         RF5C164, write value dd to register aa
    '0xB2 ad dd         PWM, write value ddd to register a (d is MSB, dd is LSB)
    '0xB3 aa dd         GameBoy DMG, write value dd to register aa
    '0xB4 aa dd         NES APU, write value dd to register aa
    '0xB5 aa dd         MultiPCM, write value dd to register aa
    '0xB6 aa dd         uPD7759, write value dd to register aa
    '0xB7 aa dd         OKIM6258, write value dd to register aa
    '0xB8 aa dd         OKIM6295, write value dd to register aa
    '0xB9 aa dd         HuC6280, write value dd to register aa
    '0xBA aa dd         K053260, write value dd to register aa
    '0xBB aa dd         Pokey, write value dd to register aa
    '0xBC aa dd         WonderSwan, write value dd to register aa
    '0xBD aa dd         SAA1099, write value dd to register aa
    '0xBE aa dd         ES5506, write value dd to register aa
    '                   Note: this command writes 8-bit data. For 16-bit data, use command 0xD6
    '0xBF aa dd         GA20, write value dd to register aa
    '0xC0 aaaa dd       Sega PCM, write value dd to memory offset aaaa
    '0xC1 aaaa dd       RF5C68, write value dd to memory offset aaaa
    '0xC2 aaaa dd       RF5C164, write value dd to memory offset aaaa
    '0xC3 cc aaaa       MultiPCM, write set bank offset aaaa to channel cc
    '0xC4 mmll rr       QSound, write value mmll to register rr
    '                   (mm - data MSB, ll - data LSB)
    '0xC5 mmll rr       SCSP, write value mmll to register rr
    '0xC6 mmll rr       WonderSwan, write value mmll to register rr
    '0xC7 mmll rr       VSU, write value mmll to register rr
    '0xC8 mmll rr       X1-010, write value mmll to register rr
    '0xD0 pp aa dd      YMF278B port pp, write value dd to register aa
    '0xD1 pp aa dd      YMF271 port pp, write value dd to register aa
    '0xD2 pp aa dd      SCC1 port pp, write value dd to register aa
    '0xD3 pp aa dd      K054539 write value dd to register ppaa
    '0xD4 pp aa dd      C140 write value dd to register ppaa
    '0xD5 pp aa dd      ES5503 write value dd to register ppaa
    '0xD6 pp aa dd      ES5506 write value aadd to register pp
    '                   Note: This command writes 16-bit data. For 8-bit data, use command 0xBE
    '0xE0 dddddddd      seek to offset dddddddd (Intel byte order) in PCM data bank
    '                   Note: This command is used tiwh 8x commands. The usual order is:
    '                   1. Data block 00 (0x67, 0x66, 0x00 ... )
    '                   2. Use E to set the read pointer
    '                   3. Use 8x to play a sample and advance the pointer by 1
    '0xE1 mmll aadd     C352, write value aadd to register mmll

    'Some ranges are reserved for future use, with different numbers of operands:

    '0x30..0x3F dd            one operand, reserved for future use
    '                         Note: used for dual-chip support (see below)
    '0x40..0x4E dd dd         two operands, reserved for future use
    '                         Note: was one operand only til v1.60
    '0xA1..0xAF dd dd         two operands, reserved for future use
    '                         Note: used for dual-chip support (see below)
    '0xBC..0xBF dd dd         two operands, reserved for future use
    '0xC5..0xCF dd dd dd      three operands, reserved for future use
    '0xD5..0xDF dd dd dd      three operands, reserved for future use
    '0xE1..0xFF dd dd dd dd   four operands, reserved for future use

    'On encountering these, the correct number of bytes should be skipped.


    'Data blocks
    '-----------
    'VGM command 0x67 specifies a data block. These are used to store large amounts
    'of data, which can be used in parallel with the normal VGM data stream. The
    'data block format is:

    '  0x67 0x66 tt ss ss ss ss (data)

    'where:
    '  0x67 = VGM command
    '  0x66 = compatibility command to make older players stop parsing the stream
    '  tt   = data type
    '  ss ss ss ss (32 bits) = size of data, in bytes
    '  (data) = data, of size previously specified

    'Data blocks of recorded streams, if present, should be at the very start of the
    'VGM data. Multiple data blocks expand the data bank. (The start offset and
    'length of the block in the data bank should be saved for command 0x95.)
    'Because data blocks can happen anywhere in the stream, players must be able to
    'parse data blocks anywhere in the stream. 

    'The data block type specifies what type of data it contains. Currently defined
    'types are:

    '00..3F   data of recorded streams (uncompressed)
    '40..7E   data of recorded streams (compressed)
    '  data block format for compressed streams:
    '    tt (8 bits) = compression type
    '                    00 - n-Bit-Compression
    '                    01 - DPCM-Compression
    '    ss ss ss ss (32 bits) = size of uncompressed data
    '    (attr) = attribute bytes used by the decompression-algorithm
    '    n-Bit-Compression:
    '        bd (8 bits) = Bits decompressed
    '        bc (8 bits) = Bits compressed
    '        st (8 bits) = compression sub-type
    '                        00 - copy (high bits aren't used)
    '                        01 - shift left (low bits aren't used)
    '                        02 - use table (uses a decompression table)
    '        aa aa (16 bits) = value that is added (ignored if table is used)
    '    DPCM-Compression:
    '        bd (8 bits) = Bits decompressed
    '        bc (8 bits) = Bits compressed
    '        st (8 bits) = [reserved for future use]
    '        aa aa (16 bits) = start value
    '    (data) = compressed data, of size (block size - 0x0A - attr size)
    '7F       Compression Table
    '    tt (8 bits) = compression type (see above)
    '    st (8 bits) = compression sub-type (see above)
    '    bd (8 bits) = Bits decompressed
    '    bc (8 bits) = Bits compressed
    '    cc cc (16 bits) = number of following values (with each of size
    '                       ceil(bd / 8))
    '    (data) = table data, of size (block size - 0x06)
    '80..BF   ROM/RAM Image dumps (contain usually samples)
    '  data block format for ROM dumps:
    '    rr rr rr rr (32 bits) = size of the entire ROM
    '    ss ss ss ss (32 bits) = start address of data
    '    (data) = ROM data, of size (block size - 0x08)
    '  The size of the VGM can be decreased a lot by saving only the used parts
    '  of the ROM. This is done by saving multiple small ROM data blocks.
    '  The start address is the ROM offset where the data will be written, the
    '  ROM size is used to allocate space for the ROM (and some chips rely on it).
    'C0..FF   RAM writes
    '  data block format for direct RAM writes:
    '    ss ss (16 bits) = start address of data (affected by a chip's registers)
    '    (data) = ROM data, of size (block size - 0x02)

    '00 = YM2612 PCM data for use with associated commands
    '01 = RF5C68 PCM data for use with associated commands
    '02 = RF5C164 PCM data for use with associated commands
    '03 = PWM PCM data for use with associated commands
    '04 = MSM6258 ADPCM data for use with associated commands
    '05 = HuC6280 PCM data for use with associated commands
    '06 = SCSP PCM data for use with associated commands
    '07 = NES APU DPCM data for use with associated commands
    '08 = Mikey PCM data for use with associated commands
    '40..7E = same as 00..3E, just compressed
    '80 = Sega PCM ROM data
    '81 = YM2608 DELTA-T ROM data
    '82 = YM2610 ADPCM ROM data
    '83 = YM2610 DELTA-T ROM data
    '84 = YMF278B ROM data
    '85 = YMF271 ROM data
    '86 = YMZ280B ROM data
    '87 = YMF278B RAM data
    '88 = Y8950 DELTA-T ROM data
    '89 = MultiPCM ROM data
    '8A = uPD7759 ROM data
    '8B = OKIM6295 ROM data
    '8C = K054539 ROM data
    '8D = C140 ROM data
    '8E = K053260 ROM data
    '8F = Q-Sound ROM data
    '90 = ES5505/ES5506 ROM data
    '91 = X1-010 ROM data
    '92 = C352 ROM data
    '93 = GA20 ROM data
    'C0 = RF5C68 RAM write
    'C1 = RF5C164 RAM write
    'C2 = NES APU RAM write
    'E0 = SCSP RAM write
    'E1 = ES5503 RAM write

    'All unknown types must be skipped by the player.

    'PCM RAM writes
    '--------------
    'VGM command 0x68 specifies a PCM RAM write. These are used to write data from
    'data blocks to the RAM of a PCM chip. The data block format is:

    '  0x68 0x66 cc oo oo oo dd dd dd ss ss ss

    'where:
    '  0x68 = VGM command
    '  0x66 = compatibility command to make older players stop parsing the stream
    '  cc   = chip type (see data block types 00..3F)
    '  oo oo oo (24 bits) = read offset in data block
    '  dd dd dd (24 bits) = write offset in chip's ram (affected by chip's
    '                        registers)
    '  ss ss ss (24 bits) = size of data, in bytes
    '    Since size can't be zero, a size of 0 bytes means 0x0100 0000 bytes.

    'All unknown types must be skipped by the player.

    'DAC Stream Control Write
    '------------------------
    'VGM commands 0x90 to 0x95 specify writes to the DAC Stream Control Driver.
    'These are used to stream data from data blocks to the chips via chip writes.
    'To use it you must:
    '1. Setup the Stream (set chip type and command) - this activates the stream
    '2. Set the Stream Data Bank
    '3. Set the Stream Frequency
    '4. Now you can start the stream, change its frequency, start it again, stop it,
    '    etc ...

    'There are the following commands:

    'Note: Stream ID 0xFF is reserved and ignored unless noted otherwise.

    'Setup Stream Control:
    '  0x90 ss tt pp cc
    '      ss = Stream ID
    '      tt = Chip Type (see clock-order in header, e.g. YM2612 = 0x02)
    '            bit 7 is used to select the 2nd chip
    '      pp cc = write command/register cc at port pp
    '      Note: For chips that use Channel Select Registers (like the RF5C-family
    '            and the HuC6280), the format is pp cd where pp is the channel
    '            number, c is the channel register and d is the data register.
    '            If you set pp to FF, the channel select write is skipped.

    'Set Stream Data:
    '  0x91 ss dd ll bb
    '      ss = Stream ID
    '      dd = Data Bank ID (see data block types 0x00..0x3f)
    '      ll = Step Size (how many data is skipped after every write, usually 1)
    '            Set to 2, if you're using an interleaved stream (e.g. for
    '             left/right channel).
    '      bb = Step Base (data offset added to the Start Offset when starting
    '            stream playback, usually 0)
    '            If you're using an interleaved stream, set it to 0 in one stream
    '            and to 1 in the other one.
    '      Note: Step Size/Step Step are given in command-data-size
    '             (i.e. 1 for YM2612, 2 for PWM), not bytes

    'Set Stream Frequency:
    '  0x92 ss ff ff ff ff
    '      ss = Stream ID
    '      ff = Frequency (or Sample Rate, in Hz) at which the writes are done

    'Start Stream:
    '  0x93 ss aa aa aa aa mm ll ll ll ll
    '      ss = Stream ID
    '      aa = Data Start offset in data bank (byte offset in data bank)
    '            Note: if set to -1, the Data Start offset is ignored
    '      mm = Length Mode (how the Data Length is calculated)
    '            00 - ignore (just change current data position)
    '            01 - length = number of commands
    '            02 - length in msec
    '            03 - play until end of data
    '            1? - (bit 4) Reverse Mode
    '            8? - (bit 7) Loop (automatically restarts when finished)
    '      ll = Data Length

    'Stop Stream:
    '  0x94 ss
    '      ss = Stream ID
    '            Note: 0xFF stops all streams

    'Start Stream (fast call):
    '  0x95 ss bb bb ff
    '      ss = Stream ID
    '      bb = Block ID (number of the data block that is part of the data bank set
    '            with command 0x91)
    '      ff = Flags
    '            bit 0 - Loop (see command 0x93, mm bit 7)
    '            bit 4 - Reverse Mode (see command 0x93)

    'General Note to the DAC Stream Control:
    'Although it may be quite hard to press already streamed data into these
    'commands, it makes it very easy to write vgm-creation tools that need to stream
    'something. (like YM2612 DAC drums/voices/etc.)
    'The DAC Stream Control can use with almost all chips and is NOT limited to
    'chips such as YM2612 and PWM.

    'Dual Chip Support
    '-----------------
    'These chips support two instances of a chip in one vgm:
    'PSG, YM2413, YM2612, YM2151, YM2203, YM2608, YM2610, YM3812, YM3526, Y8950,
    'YMZ280B, YMF262, YMF278B, YMF271, AY8910, GameBoy DMG, NES APU, MultiPCM,
    'uPD7759, OKIM6258, OKIM6295, K051649, K054539, HuC6280, C140, K053260, Pokey

    'Dual chip support is activated by setting bit 30 (0x40000000) in the chip's
    'clock value. (Note: The PSG needs this bit for T6W28 mode.)

    'Dual Chip Supprt #1:
    'The second chip instance is controlled via seperate commands.

    'The second PSG uses 0x30 (0x3F for GG Stereo).
    'All chips of the YM-family use 0xAn. n is the last digit of the main command.
    'e.g. 0x52 (1st chip) -> 0xA2 (2nd chip)

    'Dual Chip Supprt #2:
    'All other chips use bit 7 (0x80) of the first parameter byte to distinguish
    'between the 1st and 2nd chip. (0x00-7F = Chip 1, 0x80-0xFF = chip 2)

    'Extra Header
    '------------
    'With VGM v1.70, there was an extra header added. This one has to be placed
    'between the usual header and the actual VGM data.

    'This is the format of the extra header:

    '      00  01  02  03   04  05  06  07   08  09  0A  0B  0C  0D  0E  0F
    '0x00 [Header Size    ][ChpClock Offset][ChpVol Offset  ]

    'Header Size is the size of the extra header. It has to be 4 or larger,
    'depending in the needed offsets.

    'Then there are two offsets that point to extra header data for:
    '- additional Chip Clocks for second chips
    '- user-defined chip volumes

    'Chip Clock Header
    '-----------------
    '1 byte   - Entry Count (chips with extra clocks)
    '[5 bytes - List Entry 1]
    '[5 bytes - List Entry 2]
    '...

    'Each list entry has the format:
    '1 byte  - Chip ID (chip order follows the header)
    '4 bytes - clock for second chip of the type above

    'Chip Volume Header
    '------------------
    '1 byte   - Entry Count (chips with user-defined volumes)
    '[4 bytes - List Entry 1]
    '[4 bytes - List Entry 2]
    '...

    'Each list entry has the format:
    '1 byte  - Chip ID (chip order follows the header)
    '          Note: If bit 7 is set, it's the volume for a paired chip.
    '                (e.g. the AY-part of the YM2203)
    '1 byte  - Flags
    '          Note: If bit 0 is set, it's the volume for the second chip.
    '2 bytes - volume for the chip
    '          Note: If Bit 15 is 0, this is an absolute volume setting.
    '                If Bit 15 is 1, it's relative and the chip volume gets
    '                multiplied by ((Value & 0x7FFF) / 0x0100).

    'History
    '-------
    '[1.00]
    'Initial public release by Dave

    '[1.01]
    'Rate value added by Maxim; 1.00 files are fully compatible

    '[1.10]
    'PSG white noise feedback and shift register width parameters added by Maxim,
    'with note on how to handle earlier version files.
    'Additional wait command added by Maxim with thanks to Steve Snake for the 
    'suggestion.
    '1.01 files are fully compatible but 1.01 players might have problems
    'with 1.10 files, hence the 0.1 version change.

    '[1.50]
    'VGM data offset added to header by Maxim.
    'Data block support added by blargg, to allow for better handling of YM2612 PCM
    'data.
    'Both of these changes have the potential to cause problems, but are really good
    'changes, so the version number has been increased all the way to 1.50.

    '[1.51]
    'Sega PCM, RF5C68, YM2203, YM2608, YM2610/B, YM3812, YM3526, Y8950, YMF262,
    'YMF278B, YMF271, YMZ280B, RF5C164, PWM and AY8910 chips and commands added.
    'Additional data block types RF5C68 RAM write, RF5C164 RAM write, Sega PCM ROM,
    'YM2608 DELTA-T ROM, YM2610 ADPCM ROM, YM2610 DELTA-T ROM, YMF278B ROM,
    'YMF271 ROM, YMF271 RAM, YMZ280B ROM and Y8950 DELTA-T ROM Data added.
    'Data Block Types splitted into 4 categories. (PCM Stream, compressed PCM
    'Stream, ROM/RAM Dump, RAM write)
    'SN76489 Flags and Loop Modifier added.
    'It is the first time the header size exceeds 0x40 bytes.
    '1.51 files are fully compatible to 1.50 players, but there may be problems with
    'the new commands.
    'Note: Dual chip support was added too, but as a "cheat"-feature. The dual-chip
    '-bits in the clock values are not compatible to 1.50, but the rest is.
    'All changes done by Valley Bell.

    '[1.60]
    'RF5C68, RF5C164 and PWM PCM blocks and compressed data blocks added.
    'A whole bunch of new commands (PCM RAM write and DAC Stream Control) added.
    'Volume Modifier and Loop Base added.
    'The new commands (especially 0x9?) may cause problems with older players.
    'All changes done by Valley Bell.

    '[1.61]
    'GameBoy DMG, NES APU, MultiPCM, uPD7759, OKIM6258, OKIM6295, K051649, K051649,
    'HuC6280, C140, K053260, Pokey and Q-Sound chips added.
    '(including necessary data blocks)

    'Changed number of operands from 1 to 2 for reserved commands 0x40-0x4E.
    'Although they're still unused, old players might handle future vgm versions
    'wrongly.
    'All changes done by Valley Bell.

    '[1.70]
    'Added extra header with seperate chip clocks for the second one of dual chips
    'and chip volume adjustments.
    'All changes done by Valley Bell.

    '[1.71]
    'SCSP, WonderSwan, Virtual Boy VSU, SAA1099, ES5503, ES5506, Seta X1-010, Namco C352, Irem GA20 added. (including necessary ROM data blocks)
    'Data blocks (type 0x) for MSM6258, HuC6280, SCSP and NES added. VGM v1.61 players should support the data block of their respective chips despite their late addition. Documented command 0x31 added by NewRisingSun.
    'All changes done by Valley Bell.

    '[1.72]
    'Mikey chip clock and command 0x40 added.

#Region "Constants"

    Public Const VGM_Sample_Rate As Integer = 44100
    Public Const VGM_Samples_Per_Minute As Integer = VGM_Sample_Rate * 60
    Public Const VGM_Samples_Per_Hour As Long = VGM_Samples_Per_Minute * 60
    Public Const VGM_Samples_Per_Day As Long = VGM_Samples_Per_Hour * 24

#End Region

#Region "Enums"

    Public Enum CommandType As Byte
        'Supports up to v1.70
        GG_Stereo = &H4F
        PSG = &H50

        YM2413 = &H51
        YM2612_Port_0 = &H52
        YM2612_Port_1 = &H53
        YM2151 = &H54
        YM2203 = &H55
        YM2608_Port_0 = &H56
        YM2608_Port_1 = &H57
        YM2610_Port_0 = &H58
        YM2610_Port_1 = &H59
        YM3812 = &H5A
        YM3526 = &H5B
        Y8950 = &H5C
        YMZ280B = &H5D
        YMF262_Port_0 = &H5E
        YMF262_Port_1 = &H5F

        Wait_n_Samples = &H61
        Wait_735_Samples = &H62
        Wait_882_Samples = &H63
        End_Of_Sound_Data = &H66

        Data_Block = &H67
        PCM_RAM_Write = &H68

        Wait_n_Plus_1_Sample = &H70
        Wait_n_Plus_2_Samples = &H71
        Wait_n_Plus_3_Samples = &H72
        Wait_n_Plus_4_Samples = &H73
        Wait_n_Plus_5_Samples = &H74
        Wait_n_Plus_6_Samples = &H75
        Wait_n_Plus_7_Samples = &H76
        Wait_n_Plus_8_Samples = &H77
        Wait_n_Plus_9_Samples = &H78
        Wait_n_Plus_10_Samples = &H79
        Wait_n_Plus_11_Samples = &H7A
        Wait_n_Plus_12_Samples = &H7B
        Wait_n_Plus_13_Samples = &H7C
        Wait_n_Plus_14_Samples = &H7D
        Wait_n_Plus_15_Samples = &H7E
        Wait_n_Plus_16_Samples = &H7F

        Write_from_Data_Bank_and_Wait_0_Samples = &H80
        Write_from_Data_Bank_and_Wait_1_Samples = &H81
        Write_from_Data_Bank_and_Wait_2_Samples = &H82
        Write_from_Data_Bank_and_Wait_3_Samples = &H83
        Write_from_Data_Bank_and_Wait_4_Samples = &H84
        Write_from_Data_Bank_and_Wait_5_Samples = &H85
        Write_from_Data_Bank_and_Wait_6_Samples = &H86
        Write_from_Data_Bank_and_Wait_7_Samples = &H87
        Write_from_Data_Bank_and_Wait_8_Samples = &H88
        Write_from_Data_Bank_and_Wait_9_Samples = &H89
        Write_from_Data_Bank_and_Wait_10_Samples = &H8A
        Write_from_Data_Bank_and_Wait_11_Samples = &H8B
        Write_from_Data_Bank_and_Wait_12_Samples = &H8C
        Write_from_Data_Bank_and_Wait_13_Samples = &H8D
        Write_from_Data_Bank_and_Wait_14_Samples = &H8E
        Write_from_Data_Bank_and_Wait_15_Samples = &H8F

        DAC_Stream_ControlWrite_0 = &H90
        DAC_Stream_ControlWrite_1 = &H91
        DAC_Stream_ControlWrite_2 = &H92
        DAC_Stream_ControlWrite_3 = &H93
        DAC_Stream_ControlWrite_4 = &H94
        DAC_Stream_ControlWrite_5 = &H95

        AY8910 = &HA0

        RF5C68_8bit = &HB0
        RF5C164_8bit = &HB1
        PWM = &HB2
        GameBoy_DMG = &HB3
        NES_APU = &HB4
        MultiPCM = &HB5
        uPD7759 = &HB6
        OKIM6258 = &HB7
        OKIM6295 = &HB8
        HuC6280 = &HB9
        K053260 = &HBA
        Pokey = &HBB

        SegaPCM = &HC0
        RF5C68_16bit = &HC1
        RF5C164_16bit = &HC2
        MultiPCM_Set_Bank_Offset = &HC3
        QSound = &HC4

        YMF278B = &HD0
        YMF271 = &HD1
        SCC1 = &HD2
        K054539 = &HD3
        C140 = &HD4

        Seek_to_Data_Offset = &HE0
    End Enum

    Public Enum DataBlockType As Byte
        'Supports up to v1.70
        YM2612_PCM_Data = &H0
        RF5C68_PCM_Data = &H1
        RF5C164_PCM_Data = &H2
        PWM_PCM_Data = &H3

        YM2612_Compressed_PCM_Data = &H40
        RF5C68_Compressed_PCM_Data = &H41
        RF5C164_Compressed_PCM_Data = &H42
        PWM_Compressed_PCM_Data = &H43

        SegaPCM_ROM_Data = &H80
        YM2608_DeltaT_ROM_Data = &H81
        YM2610_ADPCM_ROM_Data = &H82
        YM2610_DeltaT_ROM_Data = &H83
        YM278B_ROM_Data = &H84
        YM271_ROM_Data = &H85
        YMZ280B_ROM_Data = &H86
        YM278B_RAM_Data = &H87
        Y8950_DeltaT_ROM_Data = &H88
        MultiPCM_ROM_Data = &H89
        uPD7759_ROM_Data = &H8A
        OKIM6495_ROM_Data = &H8B
        K054539_ROM_Data = &H8C
        C140_ROM_Data = &H8D
        K053260_ROM_Data = &H8E
        QSound_ROM_Data = &H8F

        RF5C68_RAM_Write = &HC0
        RF5C164_RAM_Write = &HC1
        NESAPU_RAM_Write = &HC2
    End Enum

#End Region

#Region "Commands"
    Public MustInherit Class Command
        Public ReadOnly Property Offset As Long

        Public ReadOnly Property CommandType As CommandType

        Public ReadOnly Property Sample As Long

        Public ReadOnly Property Hours As Integer
            Get
                Return (Sample \ VGM_Samples_Per_Hour) Mod 24
            End Get
        End Property

        Public ReadOnly Property Minutes As Integer
            Get
                Return (Sample \ VGM_Samples_Per_Minute) Mod 60
            End Get
        End Property

        Public ReadOnly Property Seconds As Integer
            Get
                Return (Sample \ VGM_Sample_Rate) Mod 60
            End Get
        End Property

        Public ReadOnly Property Samples As Integer
            Get
                Return Sample Mod VGM_Sample_Rate
            End Get
        End Property

        Public Overridable ReadOnly Property Description As String
        Public Overridable ReadOnly Property Details As String = ""

        Sub New(offset As Long, type As CommandType, sample As Long)
            MyBase.New
            _Offset = offset
            _CommandType = type
            _Sample = sample
        End Sub

    End Class

    Public Class ChipCommand
        Inherits Command

        Public ReadOnly Property Value As Byte()

        Public Overrides ReadOnly Property Description As String
            Get
                Select Case CommandType
                    Case CommandType.GG_Stereo, CommandType.PSG
                        Return $"Write 0x{Value(0):X2}"

                    Case CommandType.YM2413 To CommandType.YMF262_Port_1, CommandType.AY8910 To CommandType.Pokey
                        Return $"Write 0x{Value(1):X2} to addr 0x{Value(0):X2}"

                    Case CommandType.SegaPCM To CommandType.RF5C164_16bit
                        Return $"Write 0x{Value(2):X2} to addr 0x{Value(1):X2}{Value(0):X2}"

                    Case CommandType.MultiPCM_Set_Bank_Offset
                        Return $"Set ch 0x{Value(0):X2} offset to addr 0x{Value(1):X2}{Value(2):X2}"

                    Case CommandType.QSound
                        Return $"Write 0x{Value(0):X2}{Value(1):X2} to addr 0x{Value(2):X2}"

                    Case CommandType.YMF278B To CommandType.SCC1
                        Return $"Write 0x{Value(2):X2} to port {Value(0)} addr 0x{Value(1):X2}"

                    Case CommandType.K054539, CommandType.C140
                        Return $"Write 0x{Value(2):X2} to addr 0x{Value(1):X2}{Value(0):X2}"

                    Case Else
                        Return $"Unknown command"

                End Select

            End Get
        End Property

        Public Overrides ReadOnly Property Details As String

        Sub New(offset As Long, type As CommandType, sample As Long, value As Byte(), details As String)
            MyBase.New(offset, type, sample)
            _Value = value
            _Details = details
        End Sub

        Sub New(cmd As ChipCommand, details As String)
            Me.New(cmd.Offset, cmd.CommandType, cmd.Sample, cmd.Value, details)
        End Sub

    End Class

    Public Class WaitCommand
        Inherits Command

        Public ReadOnly Property SampleCount As Integer

        Public Overrides ReadOnly Property Description As String
            Get
                Return $"Wait {SampleCount} samples"
            End Get
        End Property

        Sub New(offset As Long, type As CommandType, sample As Long, sampleCount As Integer)
            MyBase.New(offset, type, sample)
            _SampleCount = sampleCount
        End Sub

    End Class

    Public Class DataBlockCommand
        Inherits Command
        Public ReadOnly Property DataType As DataBlockType
        Public ReadOnly Property Data As Byte()
        Public Overrides ReadOnly Property Description As String
            Get
                Return $"{DataType}"
            End Get
        End Property
        Public Overrides ReadOnly Property Details As String
            Get
                Return $"Data length {Data.Length}"
            End Get
        End Property
        Sub New(offset As Long, type As CommandType, sample As Long, dataType As DataBlockType, data As Byte())
            MyBase.New(offset, type, sample)
            _DataType = dataType
            _Data = data
        End Sub

    End Class

    Public Class RAMWriteDataBlockCommand
        Inherits DataBlockCommand
        Public ReadOnly Property WriteAddr As UShort
            Get
                Return BitConverter.ToUInt16(MyBase.Data, 0)
            End Get
        End Property
        Public Shadows ReadOnly Property Data As Byte()
            Get
                Dim d(MyBase.Data.GetUpperBound(0) - 2) As Byte
                Array.Copy(MyBase.Data, 2, d, 0, MyBase.Data.Length - 2)
                Return d
            End Get
        End Property

        Public Overrides ReadOnly Property Details As String
            Get
                Return $"Write address 0x{WriteAddr:X4}; Data length {Data.Length} bytes"
            End Get
        End Property
        Sub New(offset As Long, type As CommandType, sample As Long, dataType As DataBlockType, data As Byte())
            MyBase.New(offset, type, sample, dataType, data)
        End Sub

    End Class

    Public Class ROMDataBlockCommand
        Inherits DataBlockCommand

        Public ReadOnly Property ROMSize As UInteger
            Get
                Return BitConverter.ToUInt32(MyBase.Data, 0)
            End Get
        End Property
        Public ReadOnly Property WriteAddr As UInteger
            Get
                Return BitConverter.ToUInt32(MyBase.Data, 4)
            End Get
        End Property
        Public Shadows ReadOnly Property Data As Byte()
            Get
                Dim d(MyBase.Data.GetUpperBound(0) - 8) As Byte
                Array.Copy(MyBase.Data, 8, d, 0, MyBase.Data.Length - 8)
                Return d
            End Get
        End Property
        Public Overrides ReadOnly Property Details As String
            Get
                Return $"ROM size {ROMSize} bytes; Write addr 0x{WriteAddr:X8}"
            End Get
        End Property
        Sub New(offset As Long, type As CommandType, sample As Long, dataType As DataBlockType, data As Byte())
            MyBase.New(offset, type, sample, dataType, data)
        End Sub

    End Class

    Public Class PCMRAMWriteCommand
        Inherits Command
        Public ReadOnly Property DataType As DataBlockType
        Public ReadOnly Property ReadAddr As UInteger
        Public ReadOnly Property WriteAddr As UInteger
        Public ReadOnly Property Length As UInteger
        Public Overrides ReadOnly Property Details As String
            Get
                Return $"Read address 0x{ReadAddr:X8}; Write address 0x{WriteAddr:X8}; Data length {Length} bytes"
            End Get
        End Property

        Sub New(offset As Long, type As CommandType, sample As Long, chipType As DataBlockType, readAddr As UInteger, writeAddr As UInteger, length As UInteger)
            MyBase.New(offset, type, sample)
            _DataType = chipType
            _ReadAddr = readAddr
            _WriteAddr = writeAddr
            _Length = length
        End Sub

    End Class

    Public Class SeekCommand
        Inherits Command

        Public ReadOnly Property SeekOffset As Long

        Public Overrides ReadOnly Property Description As String
            Get
                Return $"Seek to offset 0x{SeekOffset:X8}"
            End Get
        End Property

        Sub New(offset As Long, type As CommandType, sample As Long, seekOffset As Long)
            MyBase.New(offset, type, sample)
            _SeekOffset = seekOffset
        End Sub

    End Class

    Public Class EndOfSoundDataCommand
        Inherits Command

        Public Overrides ReadOnly Property Description As String
            Get
                Return $"End of sound data"
            End Get
        End Property

        Sub New(offset As Long, sample As Long)
            MyBase.New(offset, CommandType.End_Of_Sound_Data, sample)
        End Sub

    End Class

    Public Class CommandList
        Inherits List(Of Command)

        Sub New()
            MyBase.New
        End Sub

    End Class

#End Region

    Public Class VGMFile
        Implements IDisposable

#Region "Properties"

        Public ReadOnly Property FileInfo As FileInfo

        Private ReadOnly Property TempFileInfo As FileInfo

        Public ReadOnly Property HasValidExtension As Boolean
            Get
                Select Case FileInfo.Extension.ToLower
                    Case ".vgm", ".vgz"
                        Return True
                    Case Else
                        Return False
                End Select
            End Get
        End Property

        Public ReadOnly Property IsCompressed As Boolean

        Public ReadOnly Property ID As String
        Public Const IDCheckString As String = "Vgm "

        Public Property EOFOffset As Integer

        Public Enum VGMVersion As Integer
            <Description("v1.00")> v1_00 = &H100
            <Description("v1.01")> v1_01 = &H101
            <Description("v1.10")> v1_10 = &H110
            <Description("v1.50")> v1_50 = &H150
            <Description("v1.51")> v1_51 = &H151
            <Description("v1.60")> v1_60 = &H160
            <Description("v1.61")> v1_61 = &H161
            <Description("v1.70")> v1_70 = &H170
        End Enum
        Public Property Version As VGMVersion

        Public ReadOnly Property VersionMajor As String
            Get
                Return Version >> 8
            End Get
        End Property

        Public ReadOnly Property VersionMinor As String
            Get
                Return $"{Version And 255:X2}"
            End Get
        End Property

        Public Const MaxSupportedVersion As VGMVersion = VGMVersion.v1_70

        Public ReadOnly Property IsVersionSupported As Boolean
            Get
                If Version <= MaxSupportedVersion Then
                    Return True
                Else
                    Return False
                End If
            End Get
        End Property

        Const Clock_Mask As UInteger =
            &H3FFFFFFFUI
        Const Dual_Chip_Mask As UInteger =
            &H40000000UI
        Const Bit_31_Mask As UInteger =
            &H80000000UI
        Public Property SN76489Clock As UInteger
        Public Property IsDualSN76489 As Boolean
        Public Property YM2413Clock As UInteger
        Public Property IsDualYM2413 As Boolean
        Public Property GD3Offset As UInteger
        Public Property TotalSamples As UInteger
        Public Property LoopOffset As UInteger
        Public Property LoopSamples As UInteger
        Public Property Rate As UInteger

        Public Enum SN76489FeedbackValue As Short
            <Description("SN76489 or SN76496")> SN76489_or_SN76496 = &H9
            SN76489AN = &H3
            <Description("Default")> DefaultValue = SN76489_or_SN76496
        End Enum
        Public Property SN76489Feedback As SN76489FeedbackValue

        Public Enum SN76489ShiftRegisterWidthValue As Byte
            <Description("SN76489 or SN76496")> SN76489_or_SN76496 = 16
            SN76489AN = 15
            DefaultValue = SN76489_or_SN76496
        End Enum
        Public Property SN76489ShiftRegisterWidth As SN76489ShiftRegisterWidthValue

        <Flags> Public Enum SN76489FlagValue As Byte
            <Description("Freq 0 Is 0x400")> Freq0Is0x400 = 1
            <Description("Output Negate Flag")> OutputNegateFlag = 2
            <Description("Stereo OFF")> StereoOff = 4
            <Description("Divide Clock by 8 OFF")> DivClockBy8Off = 8
        End Enum
        Public Property SN76489Flags As SN76489FlagValue
        Public Property YM2612Clock As UInteger
        Public Property IsDualYM2612 As Boolean
        Public Property YM2151Clock As UInteger
        Public Property IsDualYM2151 As Boolean
        Public Property SegaPCMClock As UInteger
        Public Property IsDualSegaPCM As Boolean
        Public Property SegaPCMInterface As UInteger
        Public Property RF5C68Clock As UInteger
        Public Property IsDualRF5C68 As Boolean
        Public Property YM2203Clock As UInteger
        Public Property IsDualYM2203 As Boolean
        Public Property YM2608Clock As UInteger
        Public Property IsDualYM2608 As Boolean
        Public Property YM2610Clock As UInteger
        Public Property IsDualYM2610 As Boolean
        Public Property IsYM2610B As Boolean
        Public Property YM3812Clock As UInteger
        Public Property IsDualYM3812 As Boolean
        Public Property YM3526Clock As UInteger
        Public Property IsDualYM3526 As Boolean
        Public Property Y8950Clock As UInteger
        Public Property IsDualY8950 As Boolean
        Public Property YMF262Clock As UInteger
        Public Property IsDualYMF262 As Boolean
        Public Property YMF278BClock As UInteger
        Public Property IsDualYMF278B As Boolean
        Public Property YMF271Clock As UInteger
        Public Property IsDualYMF271 As Boolean
        Public Property YMZ280BClock As UInteger
        Public Property IsDualYMZ280B As Boolean
        Public Property RF5C164Clock As UInteger
        Public Property IsDualRFC5164 As Boolean
        Public Property PWMClock As UInteger
        Public Property IsDualPWM As Boolean
        Public Property AY8910Clock As UInteger
        Public Property IsDualAY8910 As Boolean
        Public Property AY8910Type As AYTypeValue
        Public Enum AYTypeValue As Byte
            <Description("AY-8910")> AY8910 = &H0
            <Description("AY-8912")> AY8912 = &H1
            <Description("AY-8913")> AY8913 = &H2
            <Description("AY-8930")> AY8930 = &H3
            YM2149 = &H10
            YM3439 = &H11
            YMZ284 = &H12
            YMZ294 = &H13
        End Enum

        Public Property AY8910Flags As AYFlagValue
        <Flags> Public Enum AYFlagValue As Byte
            <Description("Legacy Output")> LegacyOutput = 1
            <Description("Single Output")> SingleOutput = 2
            <Description("Discrete Output")> DiscreteOutput = 4
            <Description("Raw Output")> RawOutput = 8
            <Description("YMxxxx Pin 26 (Clock Divider) Low")> YMxxxxPin26Low = 16
        End Enum

        Public Property YM2203_AY8910Flags As AYFlagValue
        Public Property YM2608_AY8910Flags As AYFlagValue
        Public Property VolumeModifier As Byte
        Public Property LoopBase As Byte
        Public Property LoopModifier As UInteger
        Public Property GameBoyDMGClock As UInteger
        Public Property IsDualGameBoyDMG As Boolean
        Public Property NESAPUClock As UInteger
        Public Property IsDualNESAPU As Boolean
        Public Property MultiPCMClock As UInteger
        Public Property IsDualMultiPCM As Boolean
        Public Property uPD7759Clock As UInteger
        Public Property IsDualuPD7759 As Boolean
        Public Property MSM6258Clock As UInteger
        Public Property IsDualMSM6258 As Boolean
        Public Property MSM6258Flags As OKIFlagValue
        <Flags> Public Enum OKIFlagValue As Byte
            <Description("Clock Divider Lo Bit")> ClockDividerLoBit = 1
            <Description("Clock Divider Hi Bit")> ClockDividerHiBit = 2
            <Description("ADPCM 3 or 4 Bit Select")> ADPCM3or4BitSelect = 4
            <Description("Output 10 or 12 Bit")> Output10or12bit = 8
        End Enum

        Public Property K054539Flags As K0FlagValue
        <Flags> Public Enum K0FlagValue As Byte
            <Description("Reverse Stereo")> ReverseStereo = 1
            <Description("Disable Reverb")> DisableReverb = 2
            <Description("Update at Key On")> UpdateAtKeyOn = 4
        End Enum

        Public Property C140Type As C140TypeValue
        Public Enum C140TypeValue As Byte
            <Description("Namco System 2")> NamcoSystem2 = 0
            <Description("Namco System 21a")> NamcoSystem21a = 1
            <Description("Namco System 21b")> NamcoSystem21b = 2
            <Description("Namco NA 1 or 2")> NamcoNA1or2 = 3
        End Enum

        Public Property MSM6295Clock As UInteger
        Public Property IsDualMSM6295 As Boolean
        Public Property K051649Clock As UInteger
        Public Property IsDualK051649 As Boolean
        Public Property K054539Clock As UInteger
        Public Property IsK052539 As Boolean
        Public Property IsDualK054539 As Boolean
        Public Property HuC6280Clock As UInteger
        Public Property IsDualHuC6280 As Boolean
        Public Property C140Clock As UInteger
        Public Property IsDualC140 As Boolean
        Public Property K053260Clock As UInteger
        Public Property IsDualK053260 As Boolean
        Public Property PokeyClock As UInteger
        Public Property IsDualPokey As Boolean
        Public Property QSoundClock As UInteger
        Public Property IsDualQSound As Boolean
        Public Property ExtraHeaderOffset As UInteger
        Public Property DataOffset As UInteger
        Public Property Data As Byte()
        Public Property GD3Tag As GD3Tag

#End Region

#Region "Methods"

        Sub New()
            MyBase.New()
        End Sub

        Sub New(fileName As String)
            MyBase.New()

            _FileInfo = New FileInfo(fileName)

            If HasValidExtension Then
                'ADDED on 2026/7/12: Fix for compressed files with .vgm extension
                Using fs As New FileStream(FileInfo.FullName, FileMode.Open, FileAccess.Read)
                    Using br As New BinaryReader(fs)
                        Dim id As Byte() = br.ReadBytes(2)
                        If id(0) = &H1F And id(1) = &H8B Then
                            _IsCompressed = True
                        End If
                    End Using
                End Using

                _TempFileInfo = New FileInfo(Path.ChangeExtension(Path.GetTempFileName, "vgm"))

                If IsCompressed Then
                    Decompress()
                Else
                    File.Copy(FileInfo.FullName, TempFileInfo.FullName)
                End If

                ReadMetadata()

                If GD3Offset <> 0 Then
                    _GD3Tag = New GD3Tag(Me)
                End If

            Else
                'Dim prompt As String =
                '    $"The file {fi.FullName} is not a .vgm or .vgz file.{ControlChars.NewLine}The file will not be opened."
                'Throw New Exception(prompt)
            End If

        End Sub

        Private Sub Decompress()
            Using fsIn As FileStream = FileInfo.OpenRead
                Using fsOut As FileStream = TempFileInfo.Create
                    Using gzsOut As New Compression.GZipStream(fsIn, Compression.CompressionMode.Decompress)
                        gzsOut.CopyTo(fsOut)
                    End Using
                End Using
            End Using
        End Sub

        Public Function GetReader(Optional ByVal share As FileShare = FileShare.Read) As BinaryReader
            Dim fs As New FileStream(TempFileInfo.FullName, FileMode.Open, FileAccess.Read, share)
            Dim br As New BinaryReader(fs)
            Return br
        End Function

        Private Sub ReadMetadata()
            'Added on 10/17/2026: Support for dual chips
            Dim clock As UInteger

            Using br As BinaryReader = GetReader()
                With br
                    _ID = Text.Encoding.ASCII.GetChars(.ReadBytes(4))
                    If ID <> IDCheckString Then
                        Throw New Exception("This file is not a valid VGM file. If the file extension is .vgm, try changing it to .vgz.")
                        End
                    End If

                    EOFOffset = .ReadInt32 + &H4
                    Version = .ReadInt32

                    clock = .ReadUInt32
                    SN76489Clock = clock And Clock_Mask
                    IsDualSN76489 = (clock And Dual_Chip_Mask) <> 0

                    clock = .ReadUInt32
                    YM2413Clock = clock And Clock_Mask
                    IsDualYM2413 = (clock And Dual_Chip_Mask) <> 0

                    GD3Offset = .ReadUInt32 : If GD3Offset <> 0 Then GD3Offset += &H14

                    TotalSamples = .ReadUInt32
                    LoopOffset = .ReadUInt32 : If LoopOffset <> 0 Then LoopOffset += &H1C
                    LoopSamples = .ReadUInt32

                    If Version >= VGMVersion.v1_01 Then
                        Rate = .ReadUInt32
                        'HACK: Fix dumps that have a rate of 0
                        If Rate = 0 Then Rate = 60
                    Else
                        Rate = 60
                    End If

                    If Version >= VGMVersion.v1_10 Then
                        SN76489Feedback = .ReadUInt16
                        SN76489ShiftRegisterWidth = .ReadByte
                        If Version >= VGMVersion.v1_51 Then
                            SN76489Flags = .ReadByte
                        Else
                            SN76489Flags = 0
                            .BaseStream.Seek(1, SeekOrigin.Current) 'skip reserved byte
                        End If

                        clock = .ReadUInt32
                        YM2612Clock = clock And Clock_Mask
                        IsDualYM2612 = (clock And Dual_Chip_Mask) <> 0

                        clock = .ReadUInt32
                        YM2151Clock = clock And Clock_Mask
                        IsDualYM2151 = (clock And Dual_Chip_Mask) <> 0

                    Else
                        SN76489Feedback = SN76489FeedbackValue.DefaultValue
                        SN76489ShiftRegisterWidth = SN76489ShiftRegisterWidthValue.DefaultValue
                        SN76489Flags = 0
                        YM2612Clock = YM2413Clock
                        YM2151Clock = YM2413Clock
                    End If

                    If Version >= VGMVersion.v1_50 Then
                        DataOffset = .ReadUInt32 + &H34
                    Else
                        DataOffset = &H40
                    End If

                    If Version >= VGMVersion.v1_51 Then
                        SegaPCMClock = .ReadUInt32
                        SegaPCMInterface = .ReadUInt32

                        If .BaseStream.Position < DataOffset Then
                            clock = .ReadUInt32
                            RF5C68Clock = clock And Clock_Mask
                            IsDualRF5C68 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YM2203Clock = clock And Clock_Mask
                            IsDualYM2203 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YM2608Clock = clock And Clock_Mask
                            IsDualYM2608 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YM2610Clock = clock And Clock_Mask
                            IsDualYM2610 = (clock And Dual_Chip_Mask) <> 0
                            IsYM2610B = (clock And Bit_31_Mask) <> 0

                            clock = .ReadUInt32
                            YM3812Clock = clock And Clock_Mask
                            IsDualYM3812 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YM3526Clock = clock And Clock_Mask
                            IsDualYM3526 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            Y8950Clock = clock And Clock_Mask
                            IsDualY8950 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YMF262Clock = clock And Clock_Mask
                            IsDualYMF262 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YMF278BClock = clock And Clock_Mask
                            IsDualYMF278B = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YMF271Clock = clock And Clock_Mask
                            IsDualYMF271 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            YMZ280BClock = clock And Clock_Mask
                            IsDualYMZ280B = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            RF5C164Clock = clock And Clock_Mask
                            IsDualRFC5164 = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            PWMClock = clock And Clock_Mask
                            IsDualPWM = (clock And Dual_Chip_Mask) <> 0

                            clock = .ReadUInt32
                            AY8910Clock = clock And Clock_Mask
                            IsDualAY8910 = (clock And Dual_Chip_Mask) <> 0
                            AY8910Type = .ReadByte
                            AY8910Flags = .ReadByte
                            YM2203_AY8910Flags = .ReadByte
                            YM2608_AY8910Flags = .ReadByte

                            If Version >= VGMVersion.v1_60 Then
                                VolumeModifier = .ReadByte
                                .BaseStream.Seek(1, SeekOrigin.Current) 'skip reserved byte
                                LoopBase = .ReadByte
                            End If

                            LoopModifier = .ReadByte

                            If Version >= VGMVersion.v1_61 Then
                                clock = .ReadUInt32
                                GameBoyDMGClock = clock And Clock_Mask
                                IsDualGameBoyDMG = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                NESAPUClock = clock And Clock_Mask
                                IsDualNESAPU = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                MultiPCMClock = clock And Clock_Mask
                                IsDualMultiPCM = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                uPD7759Clock = clock And Clock_Mask
                                IsDualuPD7759 = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                MSM6258Clock = clock And Clock_Mask
                                IsDualMSM6258 = (clock And Dual_Chip_Mask) <> 0
                                MSM6258Flags = .ReadByte

                                K054539Flags = .ReadByte

                                C140Type = .ReadByte
                                .BaseStream.Seek(1, SeekOrigin.Current) 'skip reserved byte

                                clock = .ReadUInt32
                                MSM6295Clock = clock And Clock_Mask
                                IsDualMSM6295 = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                K051649Clock = clock And Clock_Mask
                                IsDualK051649 = (clock And Dual_Chip_Mask) <> 0
                                IsK052539 = (clock And Bit_31_Mask) <> 0

                                clock = .ReadUInt32
                                K054539Clock = clock And Clock_Mask
                                IsDualK054539 = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                HuC6280Clock = clock And Clock_Mask
                                IsDualHuC6280 = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                C140Clock = clock And Clock_Mask
                                IsDualC140 = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                K053260Clock = clock And Clock_Mask
                                IsDualK053260 = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                PokeyClock = clock And Clock_Mask
                                IsDualPokey = (clock And Dual_Chip_Mask) <> 0

                                clock = .ReadUInt32
                                QSoundClock = clock And Clock_Mask
                                IsDualQSound = (clock And Dual_Chip_Mask) <> 0

                                .BaseStream.Seek(4, SeekOrigin.Current) 'skip reserved bytes

                                If Version >= VGMVersion.v1_70 Then
                                    ExtraHeaderOffset = .ReadUInt32
                                End If

                            End If

                        End If

                    End If

                End With

            End Using

        End Sub

        Private Function GetVGMData() As Byte()
            Using br As BinaryReader = GetReader()
                With br
                    ReDim Data(.BaseStream.Length - 64 - (EOFOffset - GD3Offset))
                    .BaseStream.Seek(DataOffset, SeekOrigin.Begin)
                    Return .ReadBytes(Data.Length)
                End With
            End Using

        End Function

        Public Function GetCommandList()
            Dim cmdList As New CommandList
            Dim curSample As Long = 0

            Using br As BinaryReader = GetReader()

                With br
                    .BaseStream.Seek(DataOffset, SeekOrigin.Begin)

                    While .BaseStream.Position < EOFOffset '.BaseStream.Length

                        Dim offset As Long = .BaseStream.Position

                        Dim cmdType As CommandType =
                            .ReadByte

                        Select Case cmdType
                            Case CommandType.GG_Stereo, CommandType.PSG
                                '0x4f dd    : Game Gear PSG stereo, write dd to port 0x06
                                '0x50 dd    : PSG (SN76489/SN76496) write value dd

                                Dim value As Byte = .ReadByte

                                cmdList.Add(New ChipCommand(offset, cmdType, curSample, {value}, ""))

                            Case CommandType.YM2413 To CommandType.YMF262_Port_1, CommandType.AY8910 To CommandType.Pokey
                                '0xnn aa dd : Chip write value dd to register aa

                                Dim addr As Byte = .ReadByte
                                Dim value As Byte = .ReadByte

                                cmdList.Add(New ChipCommand(offset, cmdType, curSample, {addr, value}, ""))

                            Case CommandType.SegaPCM To &HCF, CommandType.YMF278B To &HDF
                                'Three bytes of operands

                                Dim operands As Byte() =
                                    .ReadBytes(3)

                                cmdList.Add(New ChipCommand(offset, cmdType, curSample, operands, ""))

                            Case &HE1 To &HFF
                                'Four bytes of operands

                                Dim operands As Byte() =
                                    .ReadBytes(4)

                                cmdList.Add(New ChipCommand(offset, cmdType, curSample, operands, ""))

                            Case CommandType.Wait_n_Samples
                                '0x61 nn nn : Wait n samples, n can range from 0 to 65535 (approx 1.49
                                '             seconds). Longer pauses than this are represented by multiple
                                '             wait commands.
                                '             NOTE: low byte comes first

                                Dim sampleCount As Integer =
                                    .ReadUInt16 '.ReadInt16

                                cmdList.Add(New WaitCommand(offset, cmdType, curSample, sampleCount))

                                curSample += sampleCount

                            Case CommandType.Wait_735_Samples, CommandType.Wait_882_Samples
                                '0x62       : wait 735 samples (60th of a second), a shortcut for
                                '             0x61 0xdf 0x02
                                '0x63       : wait 882 samples (50th of a second), a shortcut for
                                '             0x61 0x72 0x03

                                Dim sampleCount As Integer
                                Select Case cmdType
                                    Case CommandType.Wait_735_Samples
                                        sampleCount = 735
                                    Case CommandType.Wait_882_Samples
                                        sampleCount = 882
                                End Select

                                cmdList.Add(New WaitCommand(offset, cmdType, curSample, sampleCount))

                                curSample += sampleCount

                            Case CommandType.Data_Block
                                '0x67 ...   : data block: see below
                                'The data block format is:
                                '0x67 0x66 tt ss ss ss ss (data)
                                'where:
                                '  0x67 = VGM command
                                '  0x66 = compatibility command to make older players stop parsing the stream
                                '  tt   = data type
                                '  ss ss ss ss (32 bits) = size of data, in bytes
                                '  (data) = data, of size previously specified

                                Dim eosd As Byte = .ReadByte

                                Dim dataType As DataBlockType =
                                    .ReadByte

                                Dim size As Long =
                                    .ReadUInt32

                                Dim data As Byte() =
                                    .ReadBytes(size)

                                Select Case dataType
                                    Case DataBlockType.YM2612_PCM_Data To DataBlockType.PWM_PCM_Data
                                        cmdList.Add(New DataBlockCommand(offset, cmdType, curSample, dataType, data))
                                    Case DataBlockType.YM2612_Compressed_PCM_Data To DataBlockType.PWM_Compressed_PCM_Data
                                        cmdList.Add(New DataBlockCommand(offset, cmdType, curSample, dataType, data))
                                    Case &H7F
                                        'TODO: Implement decompression table
                                    Case DataBlockType.SegaPCM_ROM_Data To DataBlockType.QSound_ROM_Data
                                        cmdList.Add(New ROMDataBlockCommand(offset, cmdType, curSample, dataType, data))
                                    Case DataBlockType.RF5C68_RAM_Write To DataBlockType.NESAPU_RAM_Write
                                        cmdList.Add(New RAMWriteDataBlockCommand(offset, cmdType, curSample, dataType, data))
                                End Select

                            Case CommandType.PCM_RAM_Write
                                '0x68 = VGM command
                                '0x66 = compatibility command to make older players stop parsing the stream
                                '  cc = chip type (see data block types 00..3F)
                                '  oo oo oo (24 bits) = read offset in data block
                                '  dd dd dd (24 bits) = write offset in chip's ram (affected by chip's
                                '                        registers)
                                '  ss ss ss (24 bits) = size of data, in bytes
                                '    Since Size can't be zero, a size of 0 bytes means 0x0100 0000 bytes.

                                'Skip compatibility command
                                .BaseStream.Seek(1, SeekOrigin.Current)

                                Dim chipType As DataBlockType =
                                    .ReadByte

                                Dim bytes As Byte() =
                                    { .ReadByte, .ReadByte, .ReadByte, 0}
                                Dim readOffset As Integer =
                                    BitConverter.ToInt32(bytes)

                                bytes = { .ReadByte, .ReadByte, .ReadByte, 0}
                                Dim writeOffset As Integer =
                                    BitConverter.ToInt32(bytes)

                                bytes = { .ReadByte, .ReadByte, .ReadByte, 0}
                                Dim length As Integer =
                                    BitConverter.ToInt32(bytes)

                                cmdList.Add(New PCMRAMWriteCommand(offset, cmdType, curSample, chipType, readOffset, writeOffset, length))

                            Case CommandType.Wait_n_Plus_1_Sample To CommandType.Wait_n_Plus_16_Samples
                                '0x7n       : wait n+1 samples, n can range from 0 to 15.

                                Dim sampleCount As Integer =
                                    (cmdType And 15) + 1

                                cmdList.Add(New WaitCommand(offset, cmdType, curSample, sampleCount))

                                curSample += sampleCount

                            Case CommandType.Write_from_Data_Bank_and_Wait_0_Samples To CommandType.Write_from_Data_Bank_and_Wait_15_Samples
                                '0x8n       : YM2612 port 0 address 2A write from the data bank, then wait 
                                '             n samples; n can range from 0 to 15. Note that the wait is n,
                                '             NOT n+1.

                                Dim sampleCount As Integer =
                                    cmdType And 15

                                cmdList.Add(New WaitCommand(offset, cmdType, curSample, sampleCount))

                                curSample += sampleCount

                            Case CommandType.Seek_to_Data_Offset
                                '0xe0 dddddddd : seek to offset dddddddd (Intel byte order) in PCM data bank

                                Dim seekOffset As Long =
                                    .ReadUInt32

                                cmdList.Add(New SeekCommand(offset, cmdType, curSample, seekOffset))

                            Case CommandType.End_Of_Sound_Data
                                '0x66       : end of sound data

                                cmdList.Add(New EndOfSoundDataCommand(offset, curSample))

                                Exit While

                            Case Else

                        End Select

                    End While

                End With

            End Using

            Return cmdList

        End Function


#End Region

#Region "IDisposable Support"
        Private disposedValue As Boolean ' To detect redundant calls

        ' IDisposable
        Protected Overridable Sub Dispose(disposing As Boolean)
            If Not disposedValue Then
                If disposing Then
                    ' TODO: dispose managed state (managed objects).
                    If TempFileInfo IsNot Nothing Then
                        With TempFileInfo
                            If .Exists Then
                                .Delete()
                            End If
                        End With
                    End If
                End If

                ' TODO: free unmanaged resources (unmanaged objects) and override Finalize() below.
                ' TODO: set large fields to null.
            End If
            disposedValue = True
        End Sub

        ' TODO: override Finalize() only if Dispose(disposing As Boolean) above has code to free unmanaged resources.
        'Protected Overrides Sub Finalize()
        '    ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
        '    Dispose(False)
        '    MyBase.Finalize()
        'End Sub

        ' This code added by Visual Basic to correctly implement the disposable pattern.
        Public Sub Dispose() Implements IDisposable.Dispose
            ' Do not change this code.  Put cleanup code in Dispose(disposing As Boolean) above.
            Dispose(True)
            ' TODO: uncomment the following line if Finalize() is overridden above.
            ' GC.SuppressFinalize(Me)
        End Sub

#End Region

    End Class

End Class