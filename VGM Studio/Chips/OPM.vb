
'Yamaha Operator Type-M and Type-P (OPM, OPP) Simulator/Emulator
'for YM2151 (OPM) and YM2164 (OPP)
'Copyright © 2018–2026 Paul Jensen
'Portions based on:
'- Nuked-OPM by Nukeyt
'- YMFM by Aaron Giles

Imports VGM_Studio.OPM
Public Class OPM

#Region "Constants"

    ' Operator count per voice
    Public Const Op_Count = 4

    'Channel count
    Public Const Ch_Count As Integer = 8

    ' Total slots cycles per slot
    Public Const Total_Op_Slots As Integer = Ch_Count * Op_Count

    ' Clock cycles per slot
    Public Const Cycles_Per_Slot As Integer = 2

    ' Sample clock frequencies 
    Public Const Sample_Clock_Divider As Long = Total_Op_Slots * Cycles_Per_Slot

#End Region

#Region "Shared Methods"

    Const PhaseIncMax As UInteger = (1 << 20) - 1
    Shared Function GetFNote(kc As Integer, kf As Integer, pm As Integer, dt1 As Integer, dt2 As Integer, mul As Integer, sampleClockFreq As Single) As Single
        Dim phaseInc As Integer =
            GetPhaseInc(kc, kf, pm, dt1, dt2, mul)
        Return sampleClockFreq * (phaseInc / PhaseIncMax)
    End Function

    Shared Function GetPhaseInc(kc As Integer, kf As Integer, pm As Integer, dt1 As Integer, dt2 As Integer, mul As Integer) As UInteger
        'Get basic increment
        Dim oct As Integer =
            (kc >> 4) And &H7
        Dim note As Integer =
            kc And &HF
        Dim semi As Integer =
            note - (note >> 2)  'Collapse 0..14 to 0..11 (skip input values 3, 7, 11, and 15) 
        Dim inc As Integer =
            (oct * 768) + (semi * 64) + (kf And &H3F)

        'Add phase modulation
        'NOT IMPLEMENTED YET

        'Apply DT2 (Coarse Detune)
        dt2 = Math.Min(dt2, 3)
        inc += DT2ToPMTable(dt2)

        'Calculate lookup index and retrieve from table
        inc = Math.Clamp(inc, 0, 6143)
        oct = inc \ 768
        Dim fnumIdx As Integer =
            inc Mod 768
        inc = FNumTable(fnumIdx)
        inc = (inc << oct) >> 1

        'Apply DT1 (fine detune)
        Dim kcPrime As UInteger =
            kc And (RegDataMask.KeyCodePrime) >> 2
        Dim dtIdx As UInteger =
            dt1 And &B_0011
        Dim dtMag As Integer =
            DT1ToPMTable(kcPrime, dtIdx)
        Dim dtSign As Boolean =
            (dt1 And &B_0100) <> 0
        If dtSign Then
            dtMag = -dtMag
        End If
        inc += dtMag

        'Apply Multi
        inc *= If(mul = 0, 0.5, mul)

        Return inc
    End Function

    Shared ReadOnly FNumTable As UShort() = { 'Values calculated as Math.Round(2048 * Math.Pow(i / 768))
        2048, 2050, 2052, 2054, 2055, 2057, 2059, 2061, 2063, 2065, 2067, 2068, 2070, 2072, 2074, 2076,
        2078, 2080, 2081, 2083, 2085, 2087, 2089, 2091, 2093, 2095, 2097, 2098, 2100, 2102, 2104, 2106,
        2108, 2110, 2112, 2114, 2116, 2117, 2119, 2121, 2123, 2125, 2127, 2129, 2131, 2133, 2135, 2137,
        2139, 2141, 2142, 2144, 2146, 2148, 2150, 2152, 2154, 2156, 2158, 2160, 2162, 2164, 2166, 2168,
        2170, 2172, 2174, 2176, 2178, 2180, 2182, 2184, 2185, 2187, 2189, 2191, 2193, 2195, 2197, 2199,
        2201, 2203, 2205, 2207, 2209, 2211, 2213, 2215, 2217, 2219, 2221, 2223, 2225, 2227, 2229, 2231,
        2233, 2235, 2237, 2239, 2241, 2243, 2245, 2248, 2250, 2252, 2254, 2256, 2258, 2260, 2262, 2264,
        2266, 2268, 2270, 2272, 2274, 2276, 2278, 2280, 2282, 2284, 2286, 2288, 2291, 2293, 2295, 2297,
        2299, 2301, 2303, 2305, 2307, 2309, 2311, 2313, 2315, 2318, 2320, 2322, 2324, 2326, 2328, 2330,
        2332, 2334, 2336, 2339, 2341, 2343, 2345, 2347, 2349, 2351, 2353, 2355, 2358, 2360, 2362, 2364,
        2366, 2368, 2370, 2373, 2375, 2377, 2379, 2381, 2383, 2385, 2388, 2390, 2392, 2394, 2396, 2398,
        2401, 2403, 2405, 2407, 2409, 2411, 2414, 2416, 2418, 2420, 2422, 2425, 2427, 2429, 2431, 2433,
        2435, 2438, 2440, 2442, 2444, 2447, 2449, 2451, 2453, 2455, 2458, 2460, 2462, 2464, 2467, 2469,
        2471, 2473, 2475, 2478, 2480, 2482, 2484, 2487, 2489, 2491, 2493, 2496, 2498, 2500, 2502, 2505,
        2507, 2509, 2512, 2514, 2516, 2518, 2521, 2523, 2525, 2527, 2530, 2532, 2534, 2537, 2539, 2541,
        2544, 2546, 2548, 2550, 2553, 2555, 2557, 2560, 2562, 2564, 2567, 2569, 2571, 2574, 2576, 2578,
        2581, 2583, 2585, 2588, 2590, 2592, 2595, 2597, 2599, 2602, 2604, 2607, 2609, 2611, 2614, 2616,
        2618, 2621, 2623, 2625, 2628, 2630, 2633, 2635, 2637, 2640, 2642, 2645, 2647, 2649, 2652, 2654,
        2657, 2659, 2661, 2664, 2666, 2669, 2671, 2673, 2676, 2678, 2681, 2683, 2686, 2688, 2690, 2693,
        2695, 2698, 2700, 2703, 2705, 2707, 2710, 2712, 2715, 2717, 2720, 2722, 2725, 2727, 2730, 2732,
        2735, 2737, 2740, 2742, 2745, 2747, 2749, 2752, 2754, 2757, 2759, 2762, 2764, 2767, 2769, 2772,
        2774, 2777, 2780, 2782, 2785, 2787, 2790, 2792, 2795, 2797, 2800, 2802, 2805, 2807, 2810, 2812,
        2815, 2818, 2820, 2823, 2825, 2828, 2830, 2833, 2835, 2838, 2841, 2843, 2846, 2848, 2851, 2853,
        2856, 2859, 2861, 2864, 2866, 2869, 2872, 2874, 2877, 2879, 2882, 2885, 2887, 2890, 2893, 2895,
        2898, 2900, 2903, 2906, 2908, 2911, 2914, 2916, 2919, 2922, 2924, 2927, 2930, 2932, 2935, 2938,
        2940, 2943, 2946, 2948, 2951, 2954, 2956, 2959, 2962, 2964, 2967, 2970, 2972, 2975, 2978, 2980,
        2983, 2986, 2989, 2991, 2994, 2997, 2999, 3002, 3005, 3008, 3010, 3013, 3016, 3019, 3021, 3024,
        3027, 3030, 3032, 3035, 3038, 3041, 3043, 3046, 3049, 3052, 3054, 3057, 3060, 3063, 3065, 3068,
        3071, 3074, 3077, 3079, 3082, 3085, 3088, 3091, 3093, 3096, 3099, 3102, 3105, 3107, 3110, 3113,
        3116, 3119, 3122, 3124, 3127, 3130, 3133, 3136, 3139, 3141, 3144, 3147, 3150, 3153, 3156, 3159,
        3161, 3164, 3167, 3170, 3173, 3176, 3179, 3182, 3184, 3187, 3190, 3193, 3196, 3199, 3202, 3205,
        3208, 3211, 3213, 3216, 3219, 3222, 3225, 3228, 3231, 3234, 3237, 3240, 3243, 3246, 3249, 3252,
        3254, 3257, 3260, 3263, 3266, 3269, 3272, 3275, 3278, 3281, 3284, 3287, 3290, 3293, 3296, 3299,
        3302, 3305, 3308, 3311, 3314, 3317, 3320, 3323, 3326, 3329, 3332, 3335, 3338, 3341, 3344, 3347,
        3350, 3353, 3356, 3359, 3362, 3365, 3368, 3371, 3374, 3377, 3380, 3383, 3386, 3390, 3393, 3396,
        3399, 3402, 3405, 3408, 3411, 3414, 3417, 3420, 3423, 3426, 3430, 3433, 3436, 3439, 3442, 3445,
        3448, 3451, 3454, 3458, 3461, 3464, 3467, 3470, 3473, 3476, 3480, 3483, 3486, 3489, 3492, 3495,
        3499, 3502, 3505, 3508, 3511, 3514, 3518, 3521, 3524, 3527, 3530, 3534, 3537, 3540, 3543, 3546,
        3550, 3553, 3556, 3559, 3562, 3566, 3569, 3572, 3575, 3579, 3582, 3585, 3588, 3592, 3595, 3598,
        3601, 3605, 3608, 3611, 3615, 3618, 3621, 3624, 3628, 3631, 3634, 3638, 3641, 3644, 3647, 3651,
        3654, 3657, 3661, 3664, 3667, 3671, 3674, 3677, 3681, 3684, 3687, 3691, 3694, 3697, 3701, 3704,
        3707, 3711, 3714, 3718, 3721, 3724, 3728, 3731, 3734, 3738, 3741, 3745, 3748, 3751, 3755, 3758,
        3762, 3765, 3768, 3772, 3775, 3779, 3782, 3785, 3789, 3792, 3796, 3799, 3803, 3806, 3809, 3813,
        3816, 3820, 3823, 3827, 3830, 3834, 3837, 3841, 3844, 3848, 3851, 3854, 3858, 3861, 3865, 3868,
        3872, 3875, 3879, 3882, 3886, 3889, 3893, 3897, 3900, 3904, 3907, 3911, 3914, 3918, 3921, 3925,
        3928, 3932, 3935, 3939, 3943, 3946, 3950, 3953, 3957, 3960, 3964, 3968, 3971, 3975, 3978, 3982,
        3986, 3989, 3993, 3996, 4000, 4004, 4007, 4011, 4015, 4018, 4022, 4025, 4029, 4033, 4036, 4040,
        4044, 4047, 4051, 4055, 4058, 4062, 4066, 4069, 4073, 4077, 4080, 4084, 4088, 4092, 4095, 4099
    }

    Shared Function GetFNote(note As NoteValue, oct As Integer, frac As Integer, kcPrime As Integer, dt1 As Integer, dt2 As Integer, mul As Integer, masterClockFreq As Single) As Single
        'Adjust master tuning frequency
        'Dim hz As Single =
        '    440 * (masterClockFreq / 3579545)
        Dim hz As Single =
            27.5 * (masterClockFreq / 3579545)

        'Calculate difference from standard frequency
        Dim diff As Single

        Select Case note
            Case NoteValue.Csharp
                diff = -8
            Case NoteValue.D
                diff = -7
            Case NoteValue.Dsharp
                diff = -6
            Case NoteValue.E
                diff = -5
            Case NoteValue.F
                diff = -4
            Case NoteValue.Fsharp
                diff = -3
            Case NoteValue.G
                diff = -2
            Case NoteValue.Gsharp
                diff = -1
            Case NoteValue.A
                diff = 0
            Case NoteValue.Asharp
                diff = 1
            Case NoteValue.B
                diff = 2
            Case NoteValue.C
                diff = 3
            Case Else
                Return 0
        End Select

        'Add normalized fractional note
        diff += CSng(frac) / 63

        'Calculate note
        hz *= Math.Pow(2, diff / 12)
        'hz *= Math.Pow(2, oct - 4)
        hz *= Math.Pow(2, oct)

        'Apply DT2 (coarse detune)
        hz *= GetDT2Multiplier(dt2)

        'Apply DT1 (coarse detune)
        hz *= GetDT1Multiplier(kcPrime, dt1)

        'Apply phase multiple
        hz *= If(mul = 0, 0.5, mul)

        Return hz
    End Function

    Shared ReadOnly DT2ToPMTable As Single() =
        {0, 384, 500, 608}
    Shared ReadOnly DT2ToCentTable As Single() =
        {0, 600, 781, 950}
    Shared ReadOnly DT2ToMultiplierTable As Single() =
        {1, Math.Sqrt(2), Math.PI / 2, Math.Sqrt(3)}
    Shared Function GetDT2Multiplier(dt2 As Byte) As Single
        dt2 = Math.Min(dt2, DT2ToMultiplierTable.GetUpperBound(0))
        Return DT2ToMultiplierTable(dt2)
    End Function

    Public Shared ReadOnly DT1ToPMTable As Byte(,) = {
        {0, 0, 1, 2}, {0, 0, 1, 2}, {0, 0, 1, 2}, {0, 0, 1, 2},  'Octave 0
        {0, 1, 2, 2}, {0, 1, 2, 3}, {0, 1, 2, 3}, {0, 1, 2, 3},  'Octave 1
        {0, 1, 2, 4}, {0, 1, 3, 4}, {0, 1, 3, 4}, {0, 1, 3, 5},  'Octave 2
        {0, 2, 4, 5}, {0, 2, 4, 6}, {0, 2, 4, 6}, {0, 2, 5, 7},  'Octave 3
        {0, 2, 5, 8}, {0, 3, 6, 8}, {0, 3, 6, 9}, {0, 3, 7, 10},  'Octave 4
        {0, 4, 8, 11}, {0, 4, 8, 12}, {0, 4, 9, 13}, {0, 5, 10, 14},  'Octave 5
        {0, 5, 11, 16}, {0, 6, 12, 17}, {0, 6, 13, 19}, {0, 7, 14, 20},  'Octave 6
        {0, 8, 16, 22}, {0, 8, 16, 22}, {0, 8, 16, 22}, {0, 8, 16, 22}  'Octave 7
    }
    Shared Function GetDT1Multiplier(kcPrime As Integer, dt1 As Integer) As Single
        dt1 = Math.Min(dt1, DT1ToPMTable.GetUpperBound(1))
        Dim dt1pm As Integer =
            DT1ToPMTable(kcPrime, dt1)
        Dim oct As Integer =
            (kcPrime And &B_11100) >> 2
        Return 1 + 1 / (1024 * Math.Pow(2, oct))
    End Function

    Shared Function GetAttenIndB(tl As Byte) As Single
        Dim int As Integer =
            (tl And 31) >> 2
        Dim frac As Integer =
            tl And 3

        Dim atten As Single = int

        For i As Integer = 0 To 1
            If ((frac >> i) And 1) = 1 Then
                atten += 2 ^ -(i + 1)
            End If
        Next

        atten *= 3

        Return atten
    End Function

#End Region

#Region "Enums"
    Public Enum ChipType As Integer
        YM2151
        YM2164
    End Enum

    Public Enum RegAddr As Byte
        'Global
        Test_LFOReset = &H1                         'LSI test data; low frequency oscillator reset
        KeyOn = &H8                                 'Slot on/off; channel selection
        NoiseEn_Freq = &HF                      'Noise generator enable and frequency
        TimerAHi = &H10                           'High bits of Timer A
        TimerALo = &H11                           'Low bits of Timer A
        TimerB = &H12                               'Timer B
        CSMEn_TimerReset_En_Load = &H14          'Enable timer-based key on in all slots (composite sinusoidal modeling); reset timer overflow flags; enable timer IRQs; load/start timers 
        LFOFreq = &H18                              'Frequency of low frequency oscillator
        PMD_AMD = &H19                              'Phase and amplitude modulation depth 
        Ctrl_Waveform = &H1B                     'External control output; waveform for phase and amplitude modulation

        'Channel
        RL_FL_Con = &H20                            'Stereo output control; self feedback level; slot connection/algorithm 
        KeyCode = &H28                              'Key code for note selection
        KeyFrac = &H30                              'Fractional value for note selection
        PMS_AMS = &H38                              'Phase and amplitude modulation sensitivity

        'Slot
        DT1_MUL = &H40                              'Fine detune; phase multiply
        TL = &H60                                   'Total attentuation level
        KS_AR = &H80                                'Key scaling; attack rate
        AMSEn_DR = &HA0                        'Enable amplitude modulation sensitivity; decay 1 level 
        DT2_SR = &HC0                              'Coarse detune; decay 2 rate
        D1L_RR = &HE0                               'Decay 1 level; release rate
    End Enum

    Public Enum RegDataMask As Byte
        'Global
        Test = &B_11111101
        LFOReset = &B_00000010

        KeyOn = &B_01111000
        Ch = &B_00000111

        NoiseEn = &B_10000000
        NoiseFreq = &B_00011111

        TimerAHi = &B_11111111
        TimerALo = &B_00000011

        TimerB = &B_11111111

        CSMEn = &B_10000000
        TimerBReset = &B_00100000
        TimerAReset = &B_00010000
        TimerBEn = &B_00001000
        TimerAEn = &B_00000100
        TimerBLoad = &B_00000010
        TimerALoad = &B_00000001

        LFOFreq = &B_11111111

        PMD_AMDSelect = &B_10000000
        PMD_AMD = &B_01111111

        Ctrl1 = &B_10000000
        Ctrl2 = &B_01000000
        Waveform = &B_00000011

        'Channel
        R = &B_10000000
        L = &B_01000000
        FL = &B_00111000
        Con = &B_00000111

        KeyCode = &B_01111111
        KeyCodePrime = &B_01111100
        Octave = &B_01110000
        Note = &B_00001111

        KeyFrac = &B_11111100

        PMS = &B_01110000
        AMS = &B_00000011

        'Slot
        DT1 = &B_01110000
        MUL = &B_00001111

        TL = &B_01111111

        KS = &B_11000000
        AR = &B_00011111

        AMSEn = &B_10000000
        DR = &B_00011111

        DT2 = &B_11000000
        SR = &B_00011111

        SL = &B_11110000
        RR = &B_00001111
    End Enum

    Public Enum ChannelIndex As Byte
        Ch1 = 0
        Ch2 = 1
        Ch3 = 2
        Ch4 = 3
        Ch5 = 4
        Ch6 = 5
        Ch7 = 6
        Ch8 = 7
    End Enum

    Public Enum OpIndex As Byte
        Op1 = 0
        Op2 = 1
        Op3 = 2
        Op4 = 3
    End Enum

    Public Enum ChOffsetValue As Byte
        Ch1 = &H0
        Ch2 = &H1
        Ch3 = &H2
        Ch4 = &H3
        Ch5 = &H4
        Ch6 = &H5
        Ch7 = &H6
        Ch8 = &H7
    End Enum

    Public Enum SlotOffsetValue As Byte
        M1 = &H0
        M2 = &H8
        C1 = &H10
        C2 = &H18
    End Enum

    Public Enum NoteValue As Integer
        Csharp = 0
        D = 1
        Dsharp = 2
        E = 4
        F = 5
        Fsharp = 6
        G = 8
        Gsharp = 9
        A = 10
        Asharp = 12
        B = 13
        C = 14
    End Enum

    Public Enum PMDAMDSelectValue As Byte
        PMD = 1
        AMD = 0
    End Enum

    Public Enum WaveformType As Byte
        Sawtooth = 0
        Square = 1
        Triangle = 2
        Noise = 3
    End Enum

#End Region

#Region "Classes and Structures"
    Public Class OPMRegisters
        Public Test As Byte
        Public LFOReset As Boolean
        Public KeyOn As Byte
        Public Ch As Byte
        Public NoiseEn As Boolean
        Public NoiseFreq As Byte
        Public TimerAHi As Byte
        Public TimerALo As Byte
        Public TimerA As UShort
        Public TimerB As Byte
        Public CSMEn As Boolean
        Public TimerBReset As Boolean
        Public TimerAReset As Boolean
        Public TimerBEn As Boolean
        Public TimerAEn As Boolean
        Public TimerBLoad As Boolean
        Public TimerALoad As Boolean
        Public LFOFreq As Byte
        Public PMDAMDSelect As PMDAMDSelectValue
        Public PMD As Byte
        Public AMD As Byte
        Public Ctrl1 As Boolean
        Public Ctrl2 As Boolean
        Public Waveform As WaveformType

        Public ReadOnly chip As Chip
        Sub New(ByRef chip As Chip)
            Me.chip = chip
        End Sub

    End Class

    Public Class OPMChannel
        Public R As Boolean
        Public L As Boolean
        Public FL As Byte
        Public Con As Byte

        Public KeyCode As Byte
        Public KeyCodePrime As Byte
        Public Octave As Byte
        Public Note As Byte

        Shared ReadOnly NoteNameTable As String() =
            {"C", "C#", "D", "UNDEFINED", "D#", "E", "F", "UNDEFINED", "F#", "G", "G#", "UNDEFINED", "A", "A#", "B", "UNDEFINED"}
        Public ReadOnly Property NoteName As String
            Get
                Return NoteNameTable(Note)
            End Get
        End Property

        Public KeyFrac As Byte

        Public PMS As Byte
        'Phase modulation sensitivity in cents
        Shared ReadOnly PMSTable As Single() =
            {0, 5, 10, 20, 50, 100, 400, 700}
        Public ReadOnly Property PMSValue() As Single
            Get
                Return PMSTable(PMS)
            End Get
        End Property

        Public AMS As Byte
        'Amplitude modulation sensitivity in dB
        Shared ReadOnly AMSTable As Single() =
            {0, 23.90625, 47.8125, 95.625}
        Public ReadOnly Property AMSValue() As Single
            Get
                Return AMSTable(AMS)
            End Get
        End Property

        Public Output As UInteger

        Public ReadOnly Op(Op_Count - 1) As OPMOp
        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Chip As Chip
        Sub New(index As Integer, ByRef chip As Chip)
            Me.Chip = chip
            Me.Index = index
            L = True
            R = True
            For i As Integer = 0 To Op.Length - 1
                Op(i) = New OPMOp(i, Me)
            Next
        End Sub

    End Class

    Public Class OPMOp
        Public KS As Byte
        Public DT1 As Byte
        Public DT2 As Byte
        Public Mul As Byte
        Public TL As Byte
        Public SL As Byte
        Public AR As Byte
        Public DR As Byte
        Public SR As Byte
        Public RR As Byte
        Public AMSEn As Boolean

        Public Property Output As Integer
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
        Public ReadOnly Property Channel As OPMChannel

        Sub New(index As Integer, ByRef ch As OPMChannel)
            Me.Index = index
            Me.Channel = ch
        End Sub

    End Class

    Public Class Voice
        Inherits FM.Voice

        Public ReadOnly Index As Long

        Private _VoiceName As String
        Public Property VoiceName As String
            Get
                Return _VoiceName
            End Get
            Set(value As String)
                _VoiceName = value.PadRight(10).Substring(0, 10)
            End Set
        End Property

        Public MasterClockFreq As UInteger

        Public LFOFreq As UInteger
        Public Waveform As WaveformType
        Public PMD As UInteger
        Public AMD As UInteger

        Public FL As UInteger
        Public Con As UInteger
        Public AMS As UInteger
        Public PMS As UInteger

        Public DT1 As UInteger()
        Public DT2 As UInteger()
        Public Mul As UInteger()
        Public TL As UInteger()
        Public IsCarrier As Boolean?()
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
        Public SL As UInteger()
        Public KS As UInteger()
        Public AR As UInteger()
        Public DR As UInteger()
        Public SR As UInteger()
        Public RR As UInteger()
        Public AMSEn As Boolean()

        Public Overrides ReadOnly Property InfoString(sep As Char) As String
            Get
                Dim sb As New Text.StringBuilder
                With sb
                    .AppendLine($"{sep}LFO{sep}Waveform{sep}FL{sep}CON{sep}AMS{sep}PMS")
                    .AppendLine($"Ch{sep}{LFOFreq}{sep}{Waveform}{sep}{FL}{sep}{Con}{sep}{AMS}{sep}{PMS}")
                    .AppendLine($"{sep}DT1{sep}DT2{sep}Mul{sep}TL{sep}SL{sep}KS{sep}AR{sep}DR{sep}SR{sep}RR{sep}AMSEn")
                    For i As Integer = 0 To 3
                        .AppendLine($"Op {i}{If(IsCarrier(i), "*", "")}{sep}{DT1(i)}{sep}{DT2(i)}{sep}{Mul(i)}{sep}{TL(i)}{sep}{SL(i)}{sep}{KS(i)}{sep}{AR(i)}{sep}{DR(i)}{sep}{SR(i)}{sep}{RR(i)}{sep}{AMSEn(i)}")
                    Next
                End With
                Return sb.ToString
            End Get
        End Property

        Sub New(index As Long)
            MyBase.New(index)

            VoiceName = "OPMVOICE"

            MasterClockFreq = 0

            LFOFreq = 0
            Waveform = WaveformType.Triangle
            PMD = 0
            AMD = 0

            FL = 0
            Con = 0
            AMS = 0
            PMS = 0

            Dim size As Integer =
                Op_Count - 1

            ReDim DT1(size)
            ReDim DT2(size)
            ReDim Mul(size)
            ReDim TL(size)
            ReDim IsCarrier(size)
            ReDim SL(size)
            ReDim KS(size)
            ReDim AR(size)
            ReDim DR(size)
            ReDim SR(size)
            ReDim RR(size)
            ReDim AMSEn(size)
        End Sub

        Sub New(index As Long, ByRef chip As Chip, ByRef ch As OPMChannel)
            Me.New(index)

            MasterClockFreq = chip.MasterClockFreq

            Dim regs As OPMRegisters =
                chip.Registers

            LFOFreq = regs.LFOFreq
            Waveform = regs.Waveform
            PMD = regs.PMD
            AMD = regs.AMD

            With ch
                FL = .FL
                Con = .Con
                AMS = .AMS
                PMS = .PMS
            End With

            For i As Integer = 0 To Op_Count - 1
                With ch.Op(i)
                    DT1(i) = .DT1
                    DT2(i) = .DT2
                    Mul(i) = .Mul
                    TL(i) = .TL
                    IsCarrier(i) = .IsCarrier
                    SL(i) = .SL
                    KS(i) = .KS
                    AR(i) = .AR
                    DR(i) = .DR
                    SR(i) = .SR
                    RR(i) = .RR
                    AMSEn(i) = .AMSEn
                End With
            Next

        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(LFOFreq, FL, Waveform, PMD, AMD, Con, AMS, PMS)
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
                Waveform <> b.Waveform OrElse
                PMD <> b.PMD OrElse
                AMD <> b.AMD OrElse
                FL <> b.FL OrElse
                Con <> b.Con OrElse
                AMS <> b.AMS OrElse
                PMS <> b.PMS Then Return False

            'HACK: Return false for uninitialized operators
            'TODO: FIND OUT WHY THIS IS HAPPENING
            If DT1 Is Nothing OrElse b.DT1 Is Nothing Then
                Return False
            End If

            For i As Integer = 0 To 3
                If DT1?(i) <> b.DT1(i) OrElse
                    DT2?(i) <> b.DT2(i) OrElse
                    Mul?(i) <> b.Mul(i) OrElse
                    TL?(i) <> b.TL(i) AndAlso (Not IsCarrier(i)) OrElse
                    SL?(i) <> b.SL(i) OrElse
                    KS?(i) <> b.KS(i) OrElse
                    AR?(i) <> b.AR(i) OrElse
                    DR?(i) <> b.DR(i) OrElse
                    SR?(i) <> b.SR(i) OrElse
                    RR?(i) <> b.RR(i) OrElse
                    AMSEn?(i) <> b.AMSEn(i) Then Return False
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

#End Region

#Region "Methods"

        Protected _Registers As OPMRegisters
        ''' <summary>
        ''' Global registers for OPM chips.
        ''' </summary>
        ''' <returns>An <c>OPMRegisters</c> structure containing chip parameters.</returns>
        Public Property Registers As OPMRegisters
            Get
                Return _Registers
            End Get
            Protected Set(value As OPMRegisters)
                _Registers = value
            End Set
        End Property

        Protected _Channel As OPMChannel()
        ''' <summary>
        ''' Parameters for each channel. 
        ''' </summary>
        ''' <returns>A <c>OPMChannel</c> object containing channel parameters./returns>
        Public Property Channel As OPMChannel()
            Get
                Return _Channel
            End Get
            Protected Set(value As OPMChannel())
                _Channel = value
            End Set
        End Property

        Public ReadOnly Property HighQuality As Boolean

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            'Set clock frequencies
            Me.MasterClockFreq = masterClockFreq
            Me.IntSampleClockFreq = masterClockFreq / Sample_Clock_Divider
            Me.ExtSampleClockFreq = extSampleClockFreq
            IntClocksPerExtClock = IntSampleClockFreq / extSampleClockFreq

            Registers = New OPMRegisters(Me)

            'Instantiate channels
            ReDim _Channel(Ch_Count - 1)
            For i As Integer = 0 To Ch_Count - 1
                _Channel(i) = New OPMChannel(i, Me)
            Next

            Me.HighQuality = highQuality
        End Sub

        Sub Advance()

        End Sub

        Sub Reset()

        End Sub

        Const Addr_Min As Byte = &H1
        Const Addr_Max As Byte = &HFF

        Public Function Read(addr As RegAddr) As Byte
            Return 0
        End Function

        ''' <summary>
        ''' Writes a byte of data to a specified address.
        ''' </summary>
        ''' <param name="addr">The address to write to.</param>
        ''' <param name="value">The data byte to write.</param>
        ''' <returns>Returns <c>True</c> upon success and <c>False</c> upon failure.</returns>
        Public Function Write(addr As RegAddr, value As Byte) As Boolean
            If addr < &H20 Then 'Global parameter

                With Registers
                    Select Case addr
                        Case RegAddr.Test_LFOReset
                            .Test = value And RegDataMask.Test
                            .LFOReset = (value And RegDataMask.LFOReset) <> 0
                            RaiseEvent TestChanged(Me)
                        Case RegAddr.KeyOn
                            .KeyOn = (value And RegDataMask.KeyOn) >> 3
                            .Ch = (value And RegDataMask.Ch)
                            RaiseEvent KeyOnChanged(Me)
                        Case RegAddr.NoiseEn_Freq
                            .NoiseEn = (value And RegDataMask.NoiseEn) <> 0
                            .NoiseFreq = (value And RegDataMask.NoiseFreq)
                            RaiseEvent NoiseEnFreqChanged(Me)
                        Case RegAddr.TimerAHi
                            .TimerAHi = value And RegDataMask.TimerAHi
                            .TimerA = (.TimerA And &B_00_00000011) Or (CUInt(.TimerAHi) << 2)
                            RaiseEvent TimerAHiChanged(Me)
                        Case RegAddr.TimerALo
                            .TimerALo = value And RegDataMask.TimerALo
                            .TimerA = (.TimerA And &B_11_11111100) Or .TimerALo
                            RaiseEvent TimerALoChanged(Me)
                        Case RegAddr.TimerB
                            .TimerB = value And RegDataMask.TimerB
                            RaiseEvent TimerBChanged(Me)
                        Case RegAddr.CSMEn_TimerReset_En_Load
                            .CSMEn = (value And RegDataMask.CSMEn) <> 0
                            .TimerBEn = (value And RegDataMask.TimerBEn) <> 0
                            .TimerAEn = (value And RegDataMask.TimerAEn) <> 0
                            .TimerBReset = (value And RegDataMask.TimerBReset) <> 0
                            .TimerAReset = (value And RegDataMask.TimerAReset) <> 0
                            .TimerBLoad = (value And RegDataMask.TimerBLoad) <> 0
                            .TimerALoad = (value And RegDataMask.TimerALoad) <> 0
                            RaiseEvent CSMEnTimerResetEnLoadChanged(Me)
                        Case RegAddr.LFOFreq
                            .LFOFreq = value And RegDataMask.LFOFreq
                            RaiseEvent LFOFreqChanged(Me)
                        Case RegAddr.PMD_AMD
                            .PMDAMDSelect = (value And RegDataMask.PMD_AMDSelect) >> 7
                            RaiseEvent PMDAMDChanged(Me)
                        Case RegAddr.Ctrl_Waveform
                            .Ctrl1 = (value And RegDataMask.Ctrl1) <> 0
                            .Ctrl2 = (value And RegDataMask.Ctrl2) <> 0
                            .Waveform = (value And RegDataMask.Waveform)
                            RaiseEvent CtrlWaveformChanged(Me)
                        Case Else
                            Return False
                    End Select
                End With

            ElseIf addr >= &H20 AndAlso addr < &H40 Then 'Channel parameter
                Dim chIdx As Byte =
                    addr And 7
                Dim ch As OPMChannel =
                    Channel(chIdx)

                With ch
                    Select Case addr And &H38
                        Case RegAddr.RL_FL_Con
                            .R = (value And RegDataMask.R) <> 0
                            .L = (value And RegDataMask.L) <> 0
                            .FL = (value And RegDataMask.FL) >> 3
                            .Con = value And RegDataMask.Con
                            RaiseEvent RLFLConChanged(ch)
                        Case RegAddr.KeyCode
                            .KeyCode = value And RegDataMask.KeyCode
                            .KeyCodePrime = (value And RegDataMask.KeyCodePrime) >> 2
                            .Octave = (value And RegDataMask.Octave) >> 4
                            .Note = value And RegDataMask.Note
                            RaiseEvent KeyCodeChanged(ch)
                        Case RegAddr.KeyFrac
                            .KeyFrac = (value And RegDataMask.KeyFrac) >> 2
                            RaiseEvent KeyFracChanged(ch)
                        Case RegAddr.PMS_AMS
                            .PMS = (value And RegDataMask.PMS) >> 4
                            .AMS = value And RegDataMask.AMS
                            RaiseEvent PMSAMSChanged(ch)
                        Case Else
                            Return False
                    End Select
                End With

            ElseIf addr >= &H40 AndAlso addr <= &HFF Then 'Operator parameter
                Dim chIdx As Byte =
                    addr And 7
                Dim opIdx As Byte =
                    (addr And 31) \ 8
                Dim op As OPMOp =
                    Channel(chIdx).Op(opIdx)

                With op
                    Select Case (addr And &HE0)
                        Case RegAddr.DT1_MUL
                            .DT1 = (value And RegDataMask.DT1) >> 4
                            .Mul = value And RegDataMask.MUL
                            RaiseEvent DT1MULChanged(op)
                        Case RegAddr.TL
                            .TL = value And RegDataMask.TL
                            RaiseEvent TLChanged(op)
                        Case RegAddr.KS_AR
                            .KS = (value And RegDataMask.KS) >> 6
                            .AR = value And RegDataMask.AR
                            RaiseEvent KSARChanged(op)
                        Case RegAddr.AMSEn_DR
                            .AMSEn = (value And RegDataMask.AMSEn) <> 0
                            .DR = value And RegDataMask.DR
                            RaiseEvent AMSEnDRChanged(op)
                        Case RegAddr.DT2_SR
                            .DT2 = (value And RegDataMask.DT2) >> 6
                            .SR = value And RegDataMask.SR
                            RaiseEvent DT2SRChanged(op)
                        Case RegAddr.D1L_RR
                            .SL = (value And RegDataMask.SL) >> 4
                            .RR = value And RegDataMask.RR
                            RaiseEvent SLRRChanged(op)
                        Case Else
                            Return False
                    End Select
                End With

            Else
                Return False
            End If

            Return True
        End Function

#End Region


#Region "Events"

        '' ********************************
        '' *** Common events
        Public Event TestChanged(ByRef chip As Chip)
        Public Event KeyOnChanged(ByRef chip As Chip)
        Public Event NoiseEnFreqChanged(ByRef chip As Chip)
        Public Event TimerAHiChanged(ByRef chip As Chip)
        Public Event TimerALoChanged(ByRef chip As Chip)
        Public Event TimerAChanged(ByRef chip As Chip)
        Public Event TimerBChanged(ByRef chip As Chip)
        Public Event CSMEnTimerResetEnLoadChanged(ByRef chip As Chip)
        Public Event LFOFreqChanged(ByRef chip As Chip)
        Public Event PMDAMDChanged(ByRef chip As Chip)
        Public Event CtrlWaveformChanged(ByRef chip As Chip)

        ' ********************************
        ' *** Channel only events
        Public Event RLFLConChanged(ByRef ch As OPMChannel)
        Public Event KeyCodeChanged(ByRef ch As OPMChannel)
        Public Event KeyFracChanged(ByRef ch As OPMChannel)
        Public Event PMSAMSChanged(ByRef ch As OPMChannel)

        ' ********************************
        ' *** Channel/Operator events
        Public Event DT1MULChanged(ByRef op As OPMOp)
        Public Event TLChanged(ByRef op As OPMOp)
        Public Event KSARChanged(ByRef op As OPMOp)
        Public Event AMSEnDRChanged(ByRef op As OPMOp)
        Public Event DT2SRChanged(ByRef op As OPMOp)
        Public Event SLRRChanged(ByRef op As OPMOp)

#End Region

    End Class

End Class

Public Class YM2151

    Public Class Chip
        Inherits OPM.Chip

        Public Overrides ReadOnly Property ChipType As ChipType = ChipType.YM2151

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)
        End Sub

    End Class

End Class