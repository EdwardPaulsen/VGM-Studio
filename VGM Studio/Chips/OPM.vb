
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
    Shared Function GetFNote(note As NoteValue, octave As Integer, frac As Integer, mul As Integer, masterClockFreq As Single) As Single
        'Adjust master tuning frequency
        Dim hz As Single =
            440 * (masterClockFreq / 3579545)

        'Calculate difference from standard frequency
        Dim diff As Single = 0

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

        'Calculate final note
        hz *= Math.Pow(2, diff / 12)
        hz *= Math.Pow(2, octave - 4)
        hz *= If(mul = 0, 0.5, mul)

        Return hz
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
        KeyCodePrime = &B_00011111
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
        ''This is different from the apparently incorrect official documentation
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

    Public Structure Voice
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

        Sub New(index As Long)
            Me.Index = index
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
        Public Shadows Function ToString(sep As Char) As String
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
        End Function

    End Structure


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
                            .KeyCodePrime = value And RegDataMask.KeyCodePrime
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