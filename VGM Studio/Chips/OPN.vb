'Yamaha Operator Type-N (OPN) Simulator/Emulator
'for YM2203 (OPN), YM2608 (OPNA), YM2010/B (OPNB), YM2612 (OPN2), YM3438 (OPN2C), YMF276 (OPN2L), and YMF288 (OPN3)
'Copyright © 2026 Paul Jensen
'Portions based on:
'- Nuked-OPN2 by Nukeyt
'- YMFM by Aaron Giles

Imports VGM_Studio.OPN
Public Class OPN

#Region "Constants"

    ' Operator count per voice
    Public Const Op_Count = 4

    ' Clock cycles per slot
    Public Const Cycles_Per_Slot As Integer = 1

    ' Decibels per TL step
    Public Const dB_Per_TL_Step As Single = 0.75

#End Region

#Region "Shared Methods"
    'Shared Function GetFNote(block As Integer, fnum As Integer, sampleClockFreq As Single) As Single
    '    Return fnum * sampleClockFreq * Math.Pow(2, block - 21)
    'End Function

    Const PhaseIncMax As UInteger = (1 << 20) - 1
    Shared Function GetFNote(block As UInteger, fnum As UInteger, dt As UInteger, multi As UInteger, sampleClockFreq As Single) As Single
        Dim phaseInc As Integer =
            GetPhaseInc(block, fnum, dt, multi)
        Return sampleClockFreq * (phaseInc / PhaseIncMax)
    End Function
    Shared Function GetFNote(block As UInteger, fnum As UInteger, sampleClockFreq As Single) As Single
        Return GetFNote(block, fnum, 0, 1, sampleClockFreq)
    End Function

    Const DetuneOverflowMask As UInteger = (1 << 17) - 1

    Shared Function GetPhaseInc(block As UInteger, fnum As UInteger, dt As UInteger, multi As UInteger) As UInteger
        'Get basic increment
        Dim inc As Integer =
            (fnum << block) >> 1

        'Apply Detune
        Dim keyCode As UInteger =
            GetKeyCode(block, fnum)
        Dim dtIdx As UInteger =
            dt And &B_0011
        Dim dtMag As Integer =
            DetuneTable(keyCode, dtIdx)
        Dim dtSign As Boolean =
            (dt And &B_0100) <> 0
        If dtSign Then
            dtMag = -dtMag
        End If
        inc += dtMag

        'Emulate detune subtraction overflow
        If inc < 0 Then
            inc += DetuneOverflowMask
        End If

        'Apply Multi
        inc = If(multi = 0, inc >> 1, inc * multi)

        Return inc
    End Function

    Shared Function GetKeyCode(block As UInteger, fnum As UInteger) As UInteger
        Dim F11 As Byte = (fnum >> 10) And 1
        Dim F10 As Byte = (fnum >> 9) And 1
        Dim F9 As Byte = (fnum >> 8) And 1
        Dim F8 As Byte = (fnum >> 7) And 1

        Dim N4 As Byte = F11
        Dim N3 As Byte = (F11 And (F10 Or F9 Or F8)) Or ((Not F11) And F10 And F9 And F8)

        Return (block << 2) Or (N4 << 1) Or N3
    End Function

    Public Shared ReadOnly DetuneTable As Byte(,) = {
        {0, 0, 1, 2}, {0, 0, 1, 2}, {0, 0, 1, 2}, {0, 0, 1, 2},  'Block 0
        {0, 1, 2, 2}, {0, 1, 2, 3}, {0, 1, 2, 3}, {0, 1, 2, 3},  'Block 1
        {0, 1, 2, 4}, {0, 1, 3, 4}, {0, 1, 3, 4}, {0, 1, 3, 5},  'Block 2
        {0, 2, 4, 5}, {0, 2, 4, 6}, {0, 2, 4, 6}, {0, 2, 5, 7},  'Block 3
        {0, 2, 5, 8}, {0, 3, 6, 8}, {0, 3, 6, 9}, {0, 3, 7, 10},  'Block 4
        {0, 4, 8, 11}, {0, 4, 8, 12}, {0, 4, 9, 13}, {0, 5, 10, 14},  'Block 5
        {0, 5, 11, 16}, {0, 6, 12, 17}, {0, 6, 13, 19}, {0, 7, 14, 20},  'Block 6
        {0, 8, 16, 22}, {0, 8, 16, 22}, {0, 8, 16, 22}, {0, 8, 16, 22}  'Block 7
    }

#End Region

#Region "Enums"
    Public Enum ChipType As Integer
        YM2203
        YM2608
        YM2010
        YM2010B
        YM2612
        YM3438
        YMF276
        YMF288
    End Enum

    Public Enum RegAddr As Byte
        'Global registers
        Test21 = &H21                       'LSI test data
        LFOEn_LFOFreq = &H22                'LFO enable; frequency set
        TimerAHi = &H24                     'High bits of timer A
        TimerALo = &H25                     'Low bits of timer A
        TimerB = &H26                       'Timer B
        Ch3Mode_Reset_Enable_Load = &H27    'Set mode of channel 3; reset timer overflow flags; enable timer IRQs; load/start timers
        KeyOn = &H28                        'Slot on/off; channel selection
        DACData = &H2A                      'DAC data write (YM2612 ONLY)
        DACEn = &H2B                        'DAC enable (YM2612 ONLY)
        Test2C = &H2C                       'LSI test data (YM2612 ONLY?)
        ClockDividerOneSixth = &H2D         'Set FM clock divider to 1/6 and SSG clock divider to 1/4 (YM2203 and YM2608 ONLY)
        ClockDividerOneThird = &H2E         'Set FM clock divider to 1/3 and SSG clock divider to 1/2 (YM2203 and YM2608 ONLY)
        ClockDividerOneHalf = &H2F          'Set FM clock divider to 1/2 and SSG clock divider to 1/1 (YM2203 and YM2608 ONLY)

        'Operator registers
        DT_Multi = &H30                     'Frequency detune; phase multiplier
        TL = &H40                           'Total attenuation level
        KS_AR = &H50                        'Key scale; attack rate
        AM_DR = &H60                        'Amplitude modulation; decay rate
        SR = &H70                           'Sustain rate
        SL_RR = &H80                        'Sustain level; release rate
        SSG_EG = &H90                       'Sound source generator-type envelope control

        'Channel registers
        FNumLo = &HA0                       'Low bits of of FNum
        Block_FNumHi = &HA4                 'Octave; high bits of FNum
        Ch3SpFNumLo = &HA8                  'Low bits of channel 3 special mode FNum
        Ch3SpBlock_FNumHi = &HAC            'Octave; high bits of channel 3 special mode FNum
        FB_Con = &HB0                       'Self feedback level; operator connection/algorithm
        LR_AMS_PMS = &HB4                   'Stereo output control; amplitude and phase modulation sensitivity
    End Enum

    Public Enum RegDataMask As Byte
        'Global registers
        Test21SerialDataSelect = &B_10000000
        Test21ReadStatusMode = &B_01000000
        Test21EGMode = &B_00100000
        Test21InvertDACOutput = &B_00010000
        Test21MuteAll = &B_00001000
        Test21TimerTurbo = &B_00000100

        LFOEn = &B_00001000
        LFOFreq = &B_00000111

        TimerAHi = &B_11111111

        TimerALo = &B_00000011

        Ch3Mode = &B_11000000

        TimerB = &B_11111111

        TimerBReset = &B_00100000
        TimerAReset = &B_00010000
        TimerBEnable = &B_00001000
        TimerAEnable = &B_00000100
        TimerBLoad = &B_00000010
        TimerALoad = &B_00000001

        KeyOn = &B_11110000
        Port = &B_00000100
        Ch = &B_00000011

        DACData = &B_11111111
        DACEn = &B_10000000

        'Operator registers
        DT = &B_01110000
        Multi = &B_00001111

        TL = &B_1111111
        KS = &B_11000000

        AR = &B_00011111
        AM = &B_10000000

        DR = &B_00011111

        SR = &B_00011111

        SL = &B_11100000
        RR = &B_00001111

        SSG_EG_Cnt = &B_00001000
        SSG_EG_Att = &B_00000100
        SSG_EG_Alt = &B_00000010
        SSG_EG_Hld = &B_00000001

        'Channel registers
        FNumLo = &B_11111111

        Block = &B_00111000
        FNumHi = &B_00000111

        Ch3SpFNumLo = &B_11111111

        Ch3SpBlock = &B_00111000
        Ch3SpFNumHi = &B_00000111

        FB = &B_00111000
        Con = &B_00000111

        L = &B_10000000
        R = &B_01000000
        AMS = &B_00110000
        PMS = &B_00000111
    End Enum

    Public Enum PortIndex As Integer
        Port0 = 0
        Port1 = 1
    End Enum

    Public Enum ChannelIndex As Integer
        Ch1 = 0
        Ch2 = 1
        Ch3 = 2
        Ch4 = 3
        Ch5 = 4
        Ch6 = 5
    End Enum

    Public Enum OpIndex As Byte
        Op1 = 0
        Op2 = 1
        Op3 = 2
        Op4 = 3
    End Enum

    Public Enum OpOffset As Byte
        Op1 = &H0
        Op2 = &H8
        Op3 = &H4
        Op4 = &HC
    End Enum

    Public Enum Ch3Mode As Byte
        Normal = 0
        Special = 1
        CSM = 2         'Composite sinusoidal modeling mode for speech synthesis
        Illegal = 3
    End Enum

#End Region

#Region "Tables"

    '/* logsin table */
    Public Shared ReadOnly LogSinROM As UShort() =
    {
        &H_859, &H_6C3, &H_607, &H_58B, &H_52E, &H_4E4, &H_4A6, &H_471,
        &H_443, &H_41A, &H_3F5, &H_3D3, &H_3B5, &H_398, &H_37E, &H_365,
        &H_34E, &H_339, &H_324, &H_311, &H_2FF, &H_2ED, &H_2DC, &H_2CD,
        &H_2BD, &H_2AF, &H_2A0, &H_293, &H_286, &H_279, &H_26D, &H_261,
        &H_256, &H_24B, &H_240, &H_236, &H_22C, &H_222, &H_218, &H_20F,
        &H_206, &H_1FD, &H_1F5, &H_1EC, &H_1E4, &H_1DC, &H_1D4, &H_1CD,
        &H_1C5, &H_1BE, &H_1B7, &H_1B0, &H_1A9, &H_1A2, &H_19B, &H_195,
        &H_18F, &H_188, &H_182, &H_17C, &H_177, &H_171, &H_16B, &H_166,
        &H_160, &H_15B, &H_155, &H_150, &H_14B, &H_146, &H_141, &H_13C,
        &H_137, &H_133, &H_12E, &H_129, &H_125, &H_121, &H_11C, &H_118,
        &H_114, &H_10F, &H_10B, &H_107, &H_103, &H_0FF, &H_0FB, &H_0F8,
        &H_0F4, &H_0F0, &H_0EC, &H_0E9, &H_0E5, &H_0E2, &H_0DE, &H_0DB,
        &H_0D7, &H_0D4, &H_0D1, &H_0CD, &H_0CA, &H_0C7, &H_0C4, &H_0C1,
        &H_0BE, &H_0BB, &H_0B8, &H_0B5, &H_0B2, &H_0AF, &H_0AC, &H_0A9,
        &H_0A7, &H_0A4, &H_0A1, &H_09F, &H_09C, &H_099, &H_097, &H_094,
        &H_092, &H_08F, &H_08D, &H_08A, &H_088, &H_086, &H_083, &H_081,
        &H_07F, &H_07D, &H_07A, &H_078, &H_076, &H_074, &H_072, &H_070,
        &H_06E, &H_06C, &H_06A, &H_068, &H_066, &H_064, &H_062, &H_060,
        &H_05E, &H_05C, &H_05B, &H_059, &H_057, &H_055, &H_053, &H_052,
        &H_050, &H_04E, &H_04D, &H_04B, &H_04A, &H_048, &H_046, &H_045,
        &H_043, &H_042, &H_040, &H_03F, &H_03E, &H_03C, &H_03B, &H_039,
        &H_038, &H_037, &H_035, &H_034, &H_033, &H_031, &H_030, &H_02F,
        &H_02E, &H_02D, &H_02B, &H_02A, &H_029, &H_028, &H_027, &H_026,
        &H_025, &H_024, &H_023, &H_022, &H_021, &H_020, &H_01F, &H_01E,
        &H_01D, &H_01C, &H_01B, &H_01A, &H_019, &H_018, &H_017, &H_017,
        &H_016, &H_015, &H_014, &H_014, &H_013, &H_012, &H_011, &H_011,
        &H_010, &H_00F, &H_00F, &H_00E, &H_00D, &H_00D, &H_00C, &H_00C,
        &H_00B, &H_00A, &H_00A, &H_009, &H_009, &H_008, &H_008, &H_007,
        &H_007, &H_007, &H_006, &H_006, &H_005, &H_005, &H_005, &H_004,
        &H_004, &H_004, &H_003, &H_003, &H_003, &H_002, &H_002, &H_002,
        &H_002, &H_001, &H_001, &H_001, &H_001, &H_001, &H_001, &H_001,
        &H_000, &H_000, &H_000, &H_000, &H_000, &H_000, &H_000, &H_000
    }

    '/* exp table */
    Public Shared ReadOnly ExpROM As UShort() =
    {
        &H_000, &H_003, &H_006, &H_008, &H_00B, &H_00E, &H_011, &H_014,
        &H_016, &H_019, &H_01C, &H_01F, &H_022, &H_025, &H_028, &H_02A,
        &H_02D, &H_030, &H_033, &H_036, &H_039, &H_03C, &H_03F, &H_042,
        &H_045, &H_048, &H_04B, &H_04E, &H_051, &H_054, &H_057, &H_05A,
        &H_05D, &H_060, &H_063, &H_066, &H_069, &H_06C, &H_06F, &H_072,
        &H_075, &H_078, &H_07B, &H_07E, &H_082, &H_085, &H_088, &H_08B,
        &H_08E, &H_091, &H_094, &H_098, &H_09B, &H_09E, &H_0A1, &H_0A4,
        &H_0A8, &H_0AB, &H_0AE, &H_0B1, &H_0B5, &H_0B8, &H_0BB, &H_0BE,
        &H_0C2, &H_0C5, &H_0C8, &H_0CC, &H_0CF, &H_0D2, &H_0D6, &H_0D9,
        &H_0DC, &H_0E0, &H_0E3, &H_0E7, &H_0EA, &H_0ED, &H_0F1, &H_0F4,
        &H_0F8, &H_0FB, &H_0FF, &H_102, &H_106, &H_109, &H_10C, &H_110,
        &H_114, &H_117, &H_11B, &H_11E, &H_122, &H_125, &H_129, &H_12C,
        &H_130, &H_134, &H_137, &H_13B, &H_13E, &H_142, &H_146, &H_149,
        &H_14D, &H_151, &H_154, &H_158, &H_15C, &H_160, &H_163, &H_167,
        &H_16B, &H_16F, &H_172, &H_176, &H_17A, &H_17E, &H_181, &H_185,
        &H_189, &H_18D, &H_191, &H_195, &H_199, &H_19C, &H_1A0, &H_1A4,
        &H_1A8, &H_1AC, &H_1B0, &H_1B4, &H_1B8, &H_1BC, &H_1C0, &H_1C4,
        &H_1C8, &H_1CC, &H_1D0, &H_1D4, &H_1D8, &H_1DC, &H_1E0, &H_1E4,
        &H_1E8, &H_1EC, &H_1F0, &H_1F5, &H_1F9, &H_1FD, &H_201, &H_205,
        &H_209, &H_20E, &H_212, &H_216, &H_21A, &H_21E, &H_223, &H_227,
        &H_22B, &H_230, &H_234, &H_238, &H_23C, &H_241, &H_245, &H_249,
        &H_24E, &H_252, &H_257, &H_25B, &H_25F, &H_264, &H_268, &H_26D,
        &H_271, &H_276, &H_27A, &H_27F, &H_283, &H_288, &H_28C, &H_291,
        &H_295, &H_29A, &H_29E, &H_2A3, &H_2A8, &H_2AC, &H_2B1, &H_2B5,
        &H_2BA, &H_2BF, &H_2C4, &H_2C8, &H_2CD, &H_2D2, &H_2D6, &H_2DB,
        &H_2E0, &H_2E5, &H_2E9, &H_2EE, &H_2F3, &H_2F8, &H_2FD, &H_302,
        &H_306, &H_30B, &H_310, &H_315, &H_31A, &H_31F, &H_324, &H_329,
        &H_32E, &H_333, &H_338, &H_33D, &H_342, &H_347, &H_34C, &H_351,
        &H_356, &H_35B, &H_360, &H_365, &H_36A, &H_370, &H_375, &H_37A,
        &H_37F, &H_384, &H_38A, &H_38F, &H_394, &H_399, &H_39F, &H_3A4,
        &H_3A9, &H_3AE, &H_3B4, &H_3B9, &H_3BF, &H_3C4, &H_3C9, &H_3CF,
        &H_3D4, &H_3DA, &H_3DF, &H_3E4, &H_3EA, &H_3EF, &H_3F5, &H_3FA
    }

    '/* Note table */
    Public Shared ReadOnly fNNoteTable As UInteger() =
    {
        0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 3, 3, 3
    }

    '/* Envelope generator */
    Public Shared ReadOnly EGStepHiTable As UInteger(,) =
    {
        {0, 0, 0, 0},
        {1, 0, 0, 0},
        {1, 0, 1, 0},
        {1, 1, 1, 0}
    }

    Public Shared ReadOnly EGAMShiftTable As Byte() =
    {
        7, 3, 1, 0
    }

    '/* Phase generator */
    Public Shared ReadOnly PGDetuneTable As UInteger() =
    {
        16, 17, 19, 20, 22, 24, 27, 29
    }

    Public Shared ReadOnly PGLFOSh1Table As UInteger(,) =
    {
        {7, 7, 7, 7, 7, 7, 7, 7},
        {7, 7, 7, 7, 7, 7, 7, 7},
        {7, 7, 7, 7, 7, 7, 1, 1},
        {7, 7, 7, 7, 1, 1, 1, 1},
        {7, 7, 7, 1, 1, 1, 1, 0},
        {7, 7, 1, 1, 0, 0, 0, 0},
        {7, 7, 1, 1, 0, 0, 0, 0},
        {7, 7, 1, 1, 0, 0, 0, 0}
    }

    Public Shared ReadOnly PGLFOSh2Table As UInteger(,) =
    {
        {7, 7, 7, 7, 7, 7, 7, 7},
        {7, 7, 7, 7, 2, 2, 2, 2},
        {7, 7, 7, 2, 2, 2, 7, 7},
        {7, 7, 2, 2, 7, 7, 2, 2},
        {7, 7, 2, 7, 7, 7, 2, 7},
        {7, 7, 7, 2, 7, 7, 2, 1},
        {7, 7, 7, 2, 7, 7, 2, 1},
        {7, 7, 7, 2, 7, 7, 2, 1}
    }

    '/* LFO */
    Public Shared ReadOnly LFOCyclesTable As UInteger() =
    {
        108, 77, 71, 67, 62, 44, 8, 5
    }

    '/* FM algorithm */
    Public Shared ReadOnly FMAlgorithmTable As UInteger(,,) =
    {
        {
            {1, 1, 1, 1, 1, 1, 1, 1}, '/* OP1_0 */
            {1, 1, 1, 1, 1, 1, 1, 1}, '/* OP1_1 */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* OP2 */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* Last operator */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* Last operator */
            {0, 0, 0, 0, 0, 0, 0, 1}  '/* Out           */
        },
        {
            {0, 1, 0, 0, 0, 1, 0, 0}, '/* OP1_0         */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* OP1_1         */
            {1, 1, 1, 0, 0, 0, 0, 0}, '/* OP2           */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* Last operator */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* Last operator */
            {0, 0, 0, 0, 0, 1, 1, 1}  '/* Out           */
        },
        {
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* OP1_0         */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* OP1_1         */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* OP2           */
            {1, 0, 0, 1, 1, 1, 1, 0}, '/* Last operator */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* Last operator */
            {0, 0, 0, 0, 1, 1, 1, 1}  '/* Out           */
        },
        {
            {0, 0, 1, 0, 0, 1, 0, 0}, '/* OP1_0         */
            {0, 0, 0, 0, 0, 0, 0, 0}, '/* OP1_1         */
            {0, 0, 0, 1, 0, 0, 0, 0}, '/* OP2           */
            {1, 1, 0, 1, 1, 0, 0, 0}, '/* Last operator */
            {0, 0, 1, 0, 0, 0, 0, 0}, '/* Last operator */
            {1, 1, 1, 1, 1, 1, 1, 1}  '/* Out           */
        }
    }

#End Region

#Region "Classes"
    Public Class OPNRegisters
        Public Test21SerialDataSelect As Boolean
        Public Test21ReadStatusMode As Boolean
        Public Test21EGMode As Boolean
        Public Test21InvertDACOutput As Boolean
        Public Test21MuteAll As Boolean
        Public Test21TimerTurbo As Boolean
        Public Test21Bit1 As Boolean
        Public Test21Bit0 As Boolean
        Public LFOEn As Boolean
        Public LFOFreq As Byte
        Public TimerAHi As Byte
        Public TimerALo As Byte
        Public TimerA As UInteger
        Public TimerB As Byte
        Public Ch3Mode As Ch3Mode
        Public TimerBReset As Boolean
        Public TimerAReset As Boolean
        Public TimerBEnable As Boolean
        Public TimerAEnable As Boolean
        Public TimerBLoad As Boolean
        Public TimerALoad As Boolean
        Public KeyOn As Byte
        Public Port As Byte
        Public Ch As Byte
        Public DACData As UShort       '(YM2612 ONLY)
        Public DACEn As Boolean        '(YM2612 ONLY)
        Public Test2CBit7 As Boolean   '(YM2612 ONLY)
        Public Test2CBit6 As Boolean   '(YM2612 ONLY)
        Public Test2CBit5 As Boolean   '(YM2612 ONLY)
        Public Test2CBit4 As Boolean   '(YM2612 ONLY)
        Public Test2CBit3 As Boolean   '(YM2612 ONLY)
        Public Test2CBit2 As Boolean   '(YM2612 ONLY)
        Public Test2CBit1 As Boolean   '(YM2612 ONLY)
        Public Test2CBit0 As Boolean   '(YM2612 ONLY)

        Public Ch3SpFNumLo(2) As Byte
        Public Ch3SpBlockFnumHiDelayed(2) As Byte
        Public Ch3SpBlock(2) As Byte
        Public Ch3SpFNumHi(2) As Byte
        Public Ch3SpFNum(2) As UInteger

        Public ReadOnly chip As Chip
        Sub New(ByRef chip As Chip)
            Me.chip = chip
        End Sub

    End Class

    Public Class OPNChannel
        Public FNumLo As Byte
        Public BlockFnumHiDelayed As Byte
        Public Block As Byte
        Public FNumHi As Byte
        Public FNum As UInteger
        Public FB As Byte
        Public Con As Byte
        Public L As Boolean
        Public R As Boolean
        Public AMS As Byte
        Public PMS As Byte

        Public Output As UInteger

        Public ReadOnly Op(Op_Count - 1) As OPNOp
        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Chip As Chip

        Public Sub New(index As Integer, ByRef chip As Chip)
            Me.Index = index
            Me.Chip = chip
            L = True
            R = True
            For i As Integer = 0 To Op.Length - 1
                Op(i) = New OPNOp(i, Me)
            Next
        End Sub

        Sub Clock()

        End Sub

        Public Sub CalculateOutput()
            For i As Integer = 0 To 3
                Op(i).CalculateOutput()
            Next
        End Sub

    End Class

    Public Class OPNOp
        Public DT As Byte
        Public Multi As Byte
        Public TL As Byte
        Public SL As Byte
        Public KS As Byte
        Public AR As Byte
        Public DR As Byte
        Public SR As Byte
        Public RR As Byte
        Public AM As Boolean
        Public SSG_EG_Cnt As Boolean
        Public SSG_EG_Att As Boolean
        Public SSG_EG_Alt As Boolean
        Public SSG_EG_Hld As Boolean

        Public PhaseInc As UInteger
        Public PhaseCounter As UInteger

        Public Output As UInteger
        Public ReadOnly Property IsCarrier() As Boolean
            Get
                Dim con As Byte =
                    Channel.Con
                Select Case Index
                    Case 0
                        If con = 7 Then
                            Return True
                        End If
                    Case 1
                        If con >= 4 Then
                            Return True
                        End If
                    Case 2
                        If con >= 5 Then
                            Return True
                        End If
                    Case 3
                        Return True
                End Select
                Return False
            End Get
        End Property

        Public ReadOnly Property Index As Integer
        Public ReadOnly Channel As OPNChannel
        Public Sub New(index As Integer, ByRef channel As OPNChannel)
            Me.Index = index
            Me.Channel = channel
        End Sub

        Sub Clock()

        End Sub

        Public Sub CalculateOutput()

        End Sub

    End Class

    Public Class Voice
        Inherits FM.Voice

        'Public ReadOnly Index As Long

        'Protected _VoiceName As String
        'Public Property VoiceName As String
        '    Get
        '        Return _VoiceName
        '    End Get
        '    Set(value As String)
        '        _VoiceName = value.PadRight(10).Substring(0, 10)
        '    End Set
        'End Property

        'Public MasterClockFreq As UInteger

        Public LFOFreq As UInteger

        Public FB As UInteger
        Public Con As UInteger
        Public AMS As UInteger
        Public PMS As UInteger

        Public DT As UInteger()
        Public Multi As UInteger()
        Public TL As UInteger()
        Public ReadOnly Property IsSilent As Boolean
            Get
                For opIdx = 0 To Op_Count - 1
                    If IsCarrier(opIdx) AndAlso TL(opIdx) <> 127 Then
                        Return False
                    End If
                Next
                Return True
            End Get
        End Property
        Public IsCarrier As Boolean?()
        Public SL As UInteger()
        Public KS As UInteger()
        Public AR As UInteger()
        Public DR As UInteger()
        Public SR As UInteger()
        Public RR As UInteger()
        Public AM As Boolean()
        Public SSG_EG_Cnt As Boolean()
        Public SSG_EG_Att As Boolean()
        Public SSG_EG_Alt As Boolean()
        Public SSG_EG_Hld As Boolean()

        Public IsCh3Sp As Boolean?
        Public Ch3SpBlock As UInteger()
        Public Ch3SpFNum As UInteger()
        Public Ch3SpHz As UInteger()

        Public Overrides ReadOnly Property InfoString(sep As Char) As String
            Get
                Dim sb As New Text.StringBuilder
                'Dim sep As Char = ","
                With sb
                    .AppendLine($"{sep}LFOFreq{sep}FB{sep}CON{sep}AMS")
                    .AppendLine($"Ch{sep}{ LFOFreq}{sep}{ FB}{sep}{ Con}{sep}{ AMS}{sep}{sep}{sep}{sep}{sep}{sep}{sep}SSG-EG{sep}{sep}{sep}{sep}Ch 3 Special")
                    .AppendLine($"{sep}DT{sep}Multi{sep}TL{sep}SL{sep}KS{sep}AR{sep}DR{sep}SR{sep}RR{sep}AM{sep}Cnt{sep}Att{sep}Alt{sep}Hld{sep}Block{sep}FNum")
                    For i As Integer = 0 To 3
                        .AppendLine($"Op {i}{If(IsCarrier(i), "*", "")}{sep}{ DT(i)}{sep}{ Multi(i)}{sep}{TL(i)}{sep}{ SL(i)}{sep}{ KS(i)}{sep}{ AR(i)}{sep}{ DR(i)}{sep}{ SR(i)}{sep}{ RR(i)}{sep}{ AM(i)}{sep}{ SSG_EG_Cnt(i)}{sep}{ SSG_EG_Att(i)}{sep}{ SSG_EG_Alt(i)}{sep}{ SSG_EG_Hld(i)}{sep}{ Ch3SpBlock(i)}{sep}{ Ch3SpFNum(i)}")
                    Next
                End With
                Return sb.ToString
            End Get
        End Property

        Sub New(index As Long)
            MyBase.New(index)

            VoiceName = "OPNVOICE"
            MasterClockFreq = 0

            LFOFreq = 0

            FB = 0
            Con = 0
            AMS = 0
            PMS = 0

            Dim size As Integer =
                Op_Count - 1

            ReDim DT(size)
            ReDim Multi(size)
            ReDim TL(size)
            ReDim IsCarrier(size)
            ReDim SL(size)
            ReDim KS(size)
            ReDim AR(size)
            ReDim DR(size)
            ReDim SR(size)
            ReDim RR(size)
            ReDim AM(size)
            ReDim SSG_EG_Cnt(size)
            ReDim SSG_EG_Att(size)
            ReDim SSG_EG_Alt(size)
            ReDim SSG_EG_Hld(size)

            ReDim Ch3SpBlock(3)
            ReDim Ch3SpFNum(3)
            ReDim Ch3SpHz(3)
        End Sub

        Sub New(index As Long, ByRef chip As Chip, ByRef ch As OPNChannel)
            Me.New(index)

            MasterClockFreq = chip.MasterClockFreq

            Dim regs As OPNRegisters =
                chip.Registers

            LFOFreq = regs.LFOFreq

            With ch
                FB = .FB
                Con = .Con
                AMS = .AMS
                PMS = .PMS
            End With

            For i As Integer = 0 To Op_Count - 1
                With ch.Op(i)
                    DT(i) = .DT
                    Multi(i) = .Multi
                    TL(i) = .TL
                    IsCarrier(i) = .IsCarrier
                    SL(i) = .SL
                    KS(i) = .KS
                    AR(i) = .AR
                    DR(i) = .DR
                    SR(i) = .SR
                    RR(i) = .RR
                    AM(i) = .AM
                    SSG_EG_Cnt(i) = .SSG_EG_Cnt
                    SSG_EG_Att(i) = .SSG_EG_Att
                    SSG_EG_Alt(i) = .SSG_EG_Alt
                    SSG_EG_Hld(i) = .SSG_EG_Hld
                End With
            Next

            If (ch.Index = 2) And (regs.Ch3Mode = Ch3Mode.Special) Then
                IsCh3Sp = True
                For opIdx As Integer = 0 To 2
                    With ch.Op(opIdx)
                        Ch3SpBlock(opIdx) = regs.Ch3SpBlock(opIdx)
                        Ch3SpFNum(opIdx) = regs.Ch3SpFNum(opIdx)
                        Ch3SpHz(opIdx) = GetFNote(Ch3SpBlock(opIdx), Ch3SpFNum(opIdx), .DT, .Multi, chip.IntSampleClockFreq)
                    End With
                Next
                With ch
                    Ch3SpBlock(3) = .Block
                    Ch3SpFNum(3) = .FNum
                    Ch3SpHz(3) = GetFNote(.Block, .FNum, .Op(3).DT, .Op(3).Multi, chip.IntSampleClockFreq)
                End With
            Else
                IsCh3Sp = False
            End If

        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(LFOFreq, FB, Con, AMS, PMS)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            If [GetType]() <> obj.GetType() Then
                Return False
            End If

            Dim b As Voice = obj

            If LFOFreq <> b.LFOFreq OrElse
                FB <> b.FB OrElse
                Con <> b.Con OrElse
                AMS <> b.AMS OrElse
                PMS <> b.PMS OrElse
                IsCh3Sp <> b.IsCh3Sp Then Return False

            'HACK: Return false for uninitialized operators
            'TODO: FIND OUT WHY THIS IS HAPPENING
            If DT Is Nothing OrElse b.DT Is Nothing Then
                Return False
            End If

            For i As Integer = 0 To 3
                If DT?(i) <> b.DT(i) OrElse
                    Multi?(i) <> b.Multi(i) OrElse
                    TL?(i) <> b.TL(i) AndAlso (Not IsCarrier(i)) OrElse
                    SL?(i) <> b.SL(i) OrElse
                    KS?(i) <> b.KS(i) OrElse
                    AR?(i) <> b.AR(i) OrElse
                    DR?(i) <> b.DR(i) OrElse
                    SR?(i) <> b.SR(i) OrElse
                    RR?(i) <> b.RR(i) OrElse
                    AM?(i) <> b.AM(i) Then Return False
                'Removed comparison for SSG-EG because it causes too many duplicates
                'RR?(i) <> b.RR(i) OrElse
                'SSG_EG?(i) <> b.SSG_EG(i) Then Return False
            Next

            Return True
        End Function
        Overloads Shared Operator =(a As Voice, b As Voice)
            Return Equals(a, b)
        End Operator
        Overloads Shared Operator <>(a As Voice, b As Voice)
            Return Not Equals(a, b)
        End Operator

    End Class

#End Region
    Public MustInherit Class Chip

#Region "Properties"
        Public MustOverride ReadOnly Property ChipType As ChipType

        Protected _MasterClockFreq As Long
        ''' <summary>
        ''' Master clock frequency in Hz.
        ''' </summary>
        ''' <returns>The master clock frequency of the chip in Hz.</returns>
        Public Property MasterClockFreq() As Long
            Get
                Return _MasterClockFreq
            End Get
            Protected Set(value As Long)
                _MasterClockFreq = value
            End Set
        End Property

        Protected _FMClockFreq As Single
        ''' <summary>
        ''' FM generator clock frequency in Hz.
        ''' </summary>
        ''' <returns>The FM generator clock frequency in Hz.</returns>
        Public Property FMClockFreq() As Single
            Get
                Return _FMClockFreq
            End Get
            Protected Set(value As Single)
                _FMClockFreq = value
            End Set
        End Property

        Protected _IntSampleClockFreq As Single
        ''' <summary>
        ''' Internal sample clock rate in Hz.
        ''' </summary>
        ''' <returns>The internal sampling rate of the chip in Hz.</returns>
        Public Property IntSampleClockFreq() As Single
            Get
                Return _IntSampleClockFreq
            End Get
            Protected Set(value As Single)
                _IntSampleClockFreq = value
            End Set
        End Property

        Protected _ExtSampleClockFreq As Integer
        ''' <summary>
        ''' External sample clock frequency
        ''' </summary>
        ''' <returns>The external sampling rate of the chip in Hz.</returns>
        Public Property ExtSampleClockFreq() As Integer
            Get
                Return _ExtSampleClockFreq
            End Get
            Protected Set(value As Integer)
                _ExtSampleClockFreq = value
            End Set
        End Property

        Protected _IntClocksPerExtClock As Double
        ''' <summary>
        ''' Number of internal clocks per external sample
        ''' </summary>
        ''' <returns>The number of internal clocks per external sample in Hz.</returns>
        Public Property IntClocksPerExtClock As Double
            Get
                Return _IntClocksPerExtClock
            End Get
            Protected Set(value As Double)
                _IntClocksPerExtClock = value
            End Set
        End Property

        Protected _FMPrescalerDivider As Integer = 6
        ''' <summary>
        ''' Divides the master clock.
        ''' </summary>
        Public Property FMPrescalerDivider As Integer
            Get
                Return _FMPrescalerDivider
            End Get
            Protected Set(value As Integer)
                _FMPrescalerDivider = value
            End Set
        End Property

        Protected _ChannelCount As Integer
        Public MustOverride ReadOnly Property ChannelCount As Integer

        Protected _TotalOpSlots As Integer
        ''' <summary>
        ''' The number of operator time slots.
        ''' </summary>
        ''' <returns>An <c>Integer</c> value that gives the number of slots.</returns>
        Public ReadOnly Property TotalOpSlots As Integer
            Get
                Return ChannelCount * Op_Count
            End Get
        End Property

        Protected _FMClockDivider As Integer
        ''' <summary>
        ''' Number by which to divide the prescaled FM clock.
        ''' </summary>
        ''' <returns>An <c>Integer</c> divisor. 12 for YM2203, 24 for YM2612, etc.</returns>
        Public ReadOnly Property FMClockDivider As Integer
            Get
                Return TotalOpSlots * Cycles_Per_Slot
            End Get
        End Property

        Protected _Registers As OPNRegisters
        ''' <summary>
        ''' Global registers for OPN chips.
        ''' </summary>
        ''' <returns>An <c>OPNRegisters</c> structure containing chip parameters.</returns>
        Public Property Registers As OPNRegisters
            Get
                Return _Registers
            End Get
            Protected Set(value As OPNRegisters)
                _Registers = value
            End Set
        End Property

        Protected _Channel As OPNChannel()
        ''' <summary>
        ''' Parameters for each channel. 
        ''' </summary>
        ''' <returns>A <c>OPNChannel</c> object containing channel parameters./returns>
        Public Property Channel As OPNChannel()
            Get
                Return _Channel
            End Get
            Protected Set(value As OPNChannel())
                _Channel = value
            End Set
        End Property

#End Region

#Region "Methods"
        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            'Set clock frequencies
            Me.MasterClockFreq = masterClockFreq
            Me.ExtSampleClockFreq = extSampleClockFreq
            InitializeFrequencies()

            Registers = New OPNRegisters(Me)

            ReDim Channel(ChannelCount - 1)
            For chIdx As Integer = 0 To Channel.Length - 1
                Channel(chIdx) = New OPNChannel(chIdx, Me)
            Next
        End Sub

        'Protected MustOverride Sub InitializeFrequencies()
        Protected Overridable Sub InitializeFrequencies()
            FMClockFreq = MasterClockFreq / FMPrescalerDivider
            IntSampleClockFreq = FMClockFreq / FMClockDivider
            IntClocksPerExtClock = IntSampleClockFreq / ExtSampleClockFreq
        End Sub

        Public Function Read(addr As RegAddr) As Byte
            Return 0
        End Function

        ''' <summary>
        ''' Writes a byte of data to a specified address.
        ''' </summary>
        ''' <param name="addr">The address to write to.</param>
        ''' <param name="value">The data byte to write.</param>
        ''' <param name="port">The port to write to.</param>
        ''' <returns>Returns <c>True</c> upon success and <c>False</c> upon failure.</returns>
        Public Overridable Function Write(addr As RegAddr, value As Byte, Optional port As PortIndex = 0) As Boolean

            If addr < &H_30 Then 'Common parameter
                With _Registers
                    Select Case addr
                        Case RegAddr.Test21
                            .Test21SerialDataSelect = (value And RegDataMask.Test21SerialDataSelect) <> 0
                            .Test21ReadStatusMode = (value And RegDataMask.Test21ReadStatusMode) <> 0
                            .Test21EGMode = (value And RegDataMask.Test21EGMode) <> 0
                            .Test21InvertDACOutput = (value And RegDataMask.Test21InvertDACOutput) <> 0
                            .Test21MuteAll = (value And RegDataMask.Test21MuteAll) <> 0
                            .Test21TimerTurbo = (value And RegDataMask.Test21TimerTurbo) <> 0
                            .Test21Bit1 = (value And &B_00000010) <> 0
                            .Test21Bit0 = (value And &B_00000001) <> 0
                            RaiseEvent Test21Changed(Me)
                        Case RegAddr.LFOEn_LFOFreq
                            .LFOEn = (value And RegDataMask.LFOEn) <> 0
                            .LFOFreq = value And RegDataMask.LFOFreq
                            RaiseEvent LFOEnLFOFreqChanged(Me)
                        Case RegAddr.TimerAHi
                            .TimerAHi = value And RegDataMask.TimerAHi
                            .TimerA = (.TimerA And RegDataMask.TimerALo) Or (CUInt(.TimerAHi) << 2)
                            RaiseEvent TimerAChanged(Me)
                        Case RegAddr.TimerALo
                            .TimerALo = value And RegDataMask.TimerALo
                            .TimerA = (.TimerA And (RegDataMask.TimerAHi << 2)) Or .TimerALo
                            RaiseEvent TimerAChanged(Me)
                        Case RegAddr.TimerB
                            .TimerB = value And RegDataMask.TimerB
                            RaiseEvent TimerBChanged(Me)
                        Case RegAddr.Ch3Mode_Reset_Enable_Load
                            .Ch3Mode = (value And RegDataMask.Ch3Mode) >> 6
                            .TimerBReset = (value And RegDataMask.TimerBReset) <> 0
                            .TimerAReset = (value And RegDataMask.TimerAReset) <> 0
                            .TimerBEnable = (value And RegDataMask.TimerBEnable) <> 0
                            .TimerAEnable = (value And RegDataMask.TimerAEnable) <> 0
                            .TimerBLoad = (value And RegDataMask.TimerBLoad) <> 0
                            .TimerALoad = (value And RegDataMask.TimerALoad) <> 0
                            RaiseEvent Ch3ModeResetEnableLoadChanged(Me)
                        Case RegAddr.KeyOn
                            .KeyOn = (value And RegDataMask.KeyOn) >> 4
                            .Port = (value And RegDataMask.Port) >> 2
                            .Ch = value And RegDataMask.Ch

                            'Reject illegal Ch values
                            If .Ch = 3 Then
                                Return False
                            End If

                            RaiseEvent KeyOnChanged(Me)
                        Case RegAddr.ClockDividerOneSixth, RegAddr.ClockDividerOneThird, RegAddr.ClockDividerOneHalf '(YM2203 and YM2608 ONLY)
                            If ChipType = ChipType.YM2203 Or ChipType = ChipType.YM2608 Then
                                Select Case addr
                                    Case RegAddr.ClockDividerOneSixth
                                        _FMPrescalerDivider = 6
                                    Case RegAddr.ClockDividerOneThird
                                        _FMPrescalerDivider = 3
                                    Case RegAddr.ClockDividerOneHalf
                                        _FMPrescalerDivider = 2
                                End Select
                                InitializeFrequencies()
                                RaiseEvent FMClockDividerChanged(Me)
                            Else
                                Return False
                            End If
                        Case RegAddr.DACData '(YM2612 ONLY)
                            If ChipType = ChipType.YM2612 Then
                                .DACData = (.DACData And &B_00000000_1) Or (CUShort(value) << 1)
                                RaiseEvent DACDataChanged(Me)
                            Else
                                Return False
                            End If
                        Case RegAddr.DACEn '(YM2612 ONLY)
                            If ChipType = ChipType.YM2612 Then
                                .DACEn = (value And RegDataMask.DACEn) <> 0
                                RaiseEvent DACEnChanged(Me)
                            Else
                                Return False
                            End If
                        Case RegAddr.Test2C '(YM2612 ONLY?)
                            .Test2CBit7 = (value And &B_10000000) <> 0
                            .Test2CBit6 = (value And &B_01000000) <> 0

                            'Alters panning behavior of all channels except 5
                            .Test2CBit5 = (value And &B_00100000) <> 0

                            .Test2CBit4 = (value And &B_00010000) <> 0

                            'Lowest bit of DAC data
                            .Test2CBit3 = (value And &B_00001000) <> 0
                            .DACData = (.DACData And &B_11111111_0) Or If(.Test2CBit3, 1, 0)
                            RaiseEvent DACDataChanged(Me)

                            .Test2CBit2 = (value And &B_00000100) <> 0
                            .Test2CBit1 = (value And &B_00000010) <> 0
                            .Test2CBit0 = (value And &B_00000001) <> 0
                            RaiseEvent Test2CChanged(Me)

                        Case Else
                            Return False
                    End Select
                End With

            ElseIf addr >= &H_30 And addr <= &H_B6 Then 'Operator or channel parameter
                Dim chIdx As Integer =
                    (addr And &B_00000011)

                'Reject invalid addresses
                If chIdx = 3 Then
                    Return False
                End If

                chIdx +=
                    If(port = PortIndex.Port0, 0, 3)

                Dim ch As OPNChannel =
                    Channel(chIdx)

                If addr < &H_A0 Then 'Operator parameter
                    'Get bitswapped operator index
                    Dim opIdx As Byte =
                       ((addr >> 3) And 1) Or ((addr >> 1) And 2)

                    Dim op As OPNOp =
                        ch.Op(opIdx)

                    With op
                        Select Case addr And &H_F0
                            Case RegAddr.DT_Multi
                                .DT = (value And RegDataMask.DT) >> 4
                                .Multi = value And RegDataMask.Multi
                                RaiseEvent DTMULTIChanged(op)
                            Case RegAddr.TL
                                .TL = value And RegDataMask.TL
                                RaiseEvent TLChanged(op)
                            Case RegAddr.KS_AR
                                .KS = (value And RegDataMask.KS) >> 6
                                .AR = value And RegDataMask.AR
                                RaiseEvent KSARChanged(op)
                            Case RegAddr.AM_DR
                                .AM = (value And RegDataMask.AM) <> 0
                                .DR = value And RegDataMask.DR
                                RaiseEvent AMDRChanged(op)
                            Case RegAddr.SR
                                .SR = value And RegDataMask.SR
                                RaiseEvent SRChanged(op)
                            Case RegAddr.SL_RR
                                .SL = (value And RegDataMask.SL) >> 5
                                .RR = value And RegDataMask.RR
                                RaiseEvent SLRRChanged(op)
                            Case RegAddr.SSG_EG
                                .SSG_EG_Cnt = (value And RegDataMask.SSG_EG_Cnt) <> 0
                                .SSG_EG_Att = (value And RegDataMask.SSG_EG_Att) <> 0
                                .SSG_EG_Alt = (value And RegDataMask.SSG_EG_Alt) <> 0
                                .SSG_EG_Hld = (value And RegDataMask.SSG_EG_Hld) <> 0
                                RaiseEvent SSG_EGChanged(op)
                            Case Else
                                Return False
                        End Select
                    End With

                ElseIf addr >= &H_A0 And addr <= &H_B6 Then 'Channel parameter

                    With Channel(chIdx)
                        Select Case addr And &HFC
                            Case RegAddr.FNumLo
                                .Block = (.BlockFnumHiDelayed And RegDataMask.Block) >> 3
                                .FNumHi = .BlockFnumHiDelayed And RegDataMask.FNumHi
                                .FNumLo = value And RegDataMask.FNumLo
                                .FNum = (CUInt(.FNumHi) << 8) Or .FNumLo
                                RaiseEvent BlockFNumChanged(ch)
                            Case RegAddr.Block_FNumHi
                                .BlockFnumHiDelayed = value
                                RaiseEvent BlockFNumHiChanged(ch)
                            Case RegAddr.Ch3SpFNumLo, RegAddr.Ch3SpBlock_FNumHi
                                Dim opIdx As Integer
                                Select Case chIdx
                                    Case 0
                                        opIdx = 2
                                    Case 1
                                        opIdx = 0
                                    Case 2
                                        opIdx = 1
                                End Select
                                With _Registers
                                    Select Case addr And &HFC
                                        Case RegAddr.Ch3SpFNumLo
                                            .Ch3SpBlock(opIdx) = (.Ch3SpBlockFnumHiDelayed(opIdx) And RegDataMask.Block) >> 3
                                            .Ch3SpFNumHi(opIdx) = .Ch3SpBlockFnumHiDelayed(opIdx) And RegDataMask.FNumHi
                                            .Ch3SpFNumLo(opIdx) = value And RegDataMask.FNumLo
                                            .Ch3SpFNum(opIdx) = (CUInt(.Ch3SpFNumHi(opIdx)) << 8) Or .Ch3SpFNumLo(opIdx)
                                            RaiseEvent Ch3SpBlockFNumChanged(Me, opIdx)
                                        Case RegAddr.Ch3SpBlock_FNumHi
                                            _Registers.Ch3SpBlockFnumHiDelayed(opIdx) = value
                                            RaiseEvent Ch3SpBlockFNumHiChanged(Me, opIdx)
                                    End Select
                                End With
                            Case RegAddr.FB_Con
                                .FB = (value And RegDataMask.FB) >> 3
                                .Con = value And RegDataMask.Con
                                RaiseEvent FBConChanged(ch)
                            Case RegAddr.LR_AMS_PMS
                                .L = (value And RegDataMask.L) <> 0
                                .R = (value And RegDataMask.R) <> 0
                                .AMS = (value And RegDataMask.AMS) >> 4
                                .PMS = value And RegDataMask.PMS
                                RaiseEvent LRAMSPMSChanged(ch)
                            Case Else
                                Return False
                        End Select
                    End With

                End If

            Else
                Return False
            End If

            Return True
        End Function

#End Region

#Region "Events"

        ' ********************************
        ' *** Global events
        Public Event Test21Changed(ByRef chip As Chip)
        Public Event LFOEnLFOFreqChanged(ByRef chip As Chip)
        Public Event TimerAHiChanged(ByRef chip As Chip)
        Public Event TimerALoChanged(ByRef chip As Chip)
        Public Event TimerAChanged(ByRef chip As Chip)
        Public Event TimerBChanged(ByRef chip As Chip)
        Public Event Ch3ModeResetEnableLoadChanged(ByRef chip As Chip)
        Public Event KeyOnChanged(ByRef chip As Chip)
        Public Event DACDataChanged(ByRef chip As Chip) '(YM2612 ONLY)
        Public Event DACEnChanged(ByRef chip As Chip) '(YM2612 ONLY)
        Public Event Test2CChanged(ByRef chip As Chip) '(YM2612 ONLY)
        Public Event FMClockDividerChanged(ByRef chip As Chip) '(YM2203 ONLY?)

        ' ********************************
        ' *** Operator events
        Public Event DTMULTIChanged(ByRef op As OPNOp)
        Public Event TLChanged(ByRef op As OPNOp)
        Public Event KSARChanged(ByRef op As OPNOp)
        Public Event AMDRChanged(ByRef op As OPNOp)
        Public Event SRChanged(ByRef op As OPNOp)
        Public Event SLRRChanged(ByRef op As OPNOp)
        Public Event SSG_EGChanged(ByRef op As OPNOp)

        ' ********************************
        ' *** Channel events
        Public Event BlockFNumHiChanged(ByRef ch As OPNChannel)
        Public Event FNumLoChanged(ByRef ch As OPNChannel)
        Public Event BlockFNumChanged(ByRef ch As OPNChannel)
        Public Event Ch3SpBlockFNumHiChanged(ByRef chip As Chip, opIdx As Integer)
        Public Event Ch3SpFNumLoChanged(ByRef chip As Chip, opIdx As Integer)
        Public Event Ch3SpBlockFNumChanged(ByRef chip As Chip, opIdx As Integer)
        Public Event FBConChanged(ByRef ch As OPNChannel)
        Public Event LRAMSPMSChanged(ByRef ch As OPNChannel)

#End Region

    End Class

End Class

Public Class YM2203

    Public Class Chip
        Inherits OPN.Chip
        ' The Yamaha YM2203 includes:
        ' 1) 12 FM-Operator Type N (OPN) Units (3 channels, 4 operators each).
        ' 2) A YM2149 Sound Source Generator (SSG) / Programmable Sound Generator (PSG) compatible with General Instrument AY-3-8910

        Public Overrides ReadOnly Property ChipType As ChipType = ChipType.YM2203

        Public Overrides ReadOnly Property ChannelCount As Integer
            Get
                Return 3
            End Get
        End Property

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)

            'ReDim Channel(ChannelCount - 1)
            'For chIdx As Integer = 0 To Channel.Length - 1
            '    Channel(chIdx) = New OPNChannel(chIdx, Me)
            'Next
        End Sub

        'Protected Overrides Sub InitializeFrequencies()
        '    FMClockFreq = MasterClockFreq / FMPrescalerDivider
        '    IntSampleClockFreq = FMClockFreq / FMClockDivider
        '    IntClocksPerExtClock = IntSampleClockFreq / ExtSampleClockFreq
        'End Sub

    End Class

End Class

Public Class YM2612

#Region "Structures"

    Public Class PCMVoice
        Public ReadOnly Property Index As Long
        Public ReadOnly Property StartAddr As UInteger
        Public ReadOnly Property EndAddr As UInteger
        Public ReadOnly Property Data As Byte()
        Public ReadOnly Property DataHash As String
            Get
                If Data Is Nothing Then
                    Return String.Empty
                Else
                    Dim dataHashBytes As Byte() =
                        Security.Cryptography.SHA1.HashData(Data)
                    Return Convert.ToHexString(dataHashBytes)
                End If
            End Get
        End Property

        Sub New(Index As Long, startAddr As UInteger, endAddr As UInteger, ByRef pcmdata As Byte())
            _Index = Index
            _StartAddr = startAddr
            _EndAddr = endAddr
            'Get PCM data
            Dim length As UInteger =
                endAddr - startAddr
            If length > 0 Then
                ReDim _Data(length - 1)
                Array.Copy(pcmdata, startAddr, _Data, 0, length)
            End If
        End Sub

        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(StartAddr, EndAddr, DataHash)
        End Function

        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            If [GetType]() <> obj.GetType() Then
                Return False
            End If

            Dim b As PCMVoice = obj

            If StartAddr = b.StartAddr AndAlso
               EndAddr = b.EndAddr Then Return True
            Return False
        End Function
        Overloads Shared Operator =(a As PCMVoice, b As PCMVoice)
            Return Equals(a, b)
        End Operator
        Overloads Shared Operator <>(a As PCMVoice, b As PCMVoice)
            Return Not Equals(a, b)
        End Operator

    End Class

#End Region
    Public Class Chip
        Inherits OPN.Chip
        ' The Yamaha YM2612 includes:
        ' 1) 24 FM-Operator Type N (OPN) Units (6 channels, 4 operators each).
        ' 2) A DAC that can replace the output of channel 6.
        Public Overrides ReadOnly Property ChipType As ChipType = ChipType.YM2612
        Public Overrides ReadOnly Property ChannelCount As Integer
            Get
                Return 6
            End Get
        End Property

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)
        End Sub

        ''' <summary>
        ''' Writes a byte of data to a specified address
        ''' </summary>
        ''' <param name="addr">The address to write to.</param>
        ''' <param name="value">The data byte to write.</param>
        ''' <param name="port">The port to write to.</param>
        ''' <returns>Returns <c>True</c> upon success and <c>False</c> upon failure.</returns>
        Public Shadows Function Write(addr As RegAddr, value As Byte, port As PortIndex) As Boolean
            Return MyBase.Write(addr, value, If(port = PortIndex.Port0, 0, 3))
        End Function

    End Class

End Class