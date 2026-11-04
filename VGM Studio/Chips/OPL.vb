'Yamaha Operator Type-L (OPL) Simulator/Emulator
'for YM3526 (OPL), Y8950 (MSX-Audio), YM3812 (OPL2), YMF262 (OPL3), YMF278B (OPL4), and YMF289 (OPL3-L)
'Copyright © 2026 Paul Jensen
'Portions based on:
'- Nuked-OPN2 by Nukeyt
'- YMFM by Aaron Giles

Imports VGM_Studio.OPL

Public Class OPL

#Region "Constants"

    ' Operator count per voice
    Public Const Op_Count = 2

    ' Clock cycles per slot
    Public Const Cycles_Per_Slot As Integer = 4

#End Region

#Region "Shared Methods"

    Const PhaseIncMax As UInteger = (1 << 20) - 1
    Shared Function GetFNote(block As UInteger, fnum As UInteger, multi As UInteger, sampleClockFreq As Single) As Single
        Dim phaseInc As UInteger =
            GetPhaseInc(block, fnum, multi)
        Return sampleClockFreq * (phaseInc / PhaseIncMax)
    End Function
    Shared Function GetFNote(block As UInteger, fnum As UInteger, sampleClockFreq As Single) As Single
        Return GetFNote(block, fnum, 1, sampleClockFreq)
    End Function

    Protected Shared ReadOnly multiTable As Integer() =
        {0.5, 1, 2, 3, 4, 5, 6, 7, 8, 10, 10, 12, 12, 15, 15, 15}
    Shared Function GetPhaseInc(block As UInteger, fnum As UInteger, Optional multi As UInteger = 1) As Integer
        'Get basic increment
        Dim inc As Integer =
            (fnum << block) >> 1

        'Apply Multi
        inc *= multiTable(multi) 'If(multi = 0, inc >> 1, inc * multi)

        Return inc
    End Function

#End Region

#Region "Enums"
    Public Enum ChipType As Integer
        YM3526
        Y8950
        YM3812
        YMF262
        YMF278B
        YMF289
    End Enum

    Public Enum RegAddr As Byte
        'Global
        Status = &H_00                      'Status register
        Test = &H_01                        'TEST information (usually 0)
        Timer1 = &H_02                      'Timer 1
        Timer2 = &H_03                      'Timer 2
        IRQ_TimerCtrl = &H_04               'IRQ and timer control
        CSMEn_NoteSel = &H_08               'CSM is for the CSM speech synthesis mode; NOTE SEL is for switching the keyboard split by using the F-Number

        'Operator
        AM_PM_EGType_KSR_Multi = &H_20      'Amplitude modulation; phase modulation; envelope generator type; key scaling rate; phase multiplier
        KSL_TL = &H_40                      'Key scaling level; total attenuation level (for carrier waveform)
        AR_DR = &H_60                       'Attack rate; decay rate
        SL_RR = &H_80                       'Sustain level; release rate

        'Channel
        FNumLo = &H_A0                      'Low bits of FNum
        KeyOn_Block_FNumHi = &H_B0          'Sustain on/off; Key on/off; octave; high bit of FNum
        FB_Con = &H_C0                      'Self-feedback; connection (algorithm)
        WS = &H_E0                          'Waveform select

        'Global / Rhythm
        AMD_PMD_Rhythm = &H_BD              'Amplitude Modulation (tremolo); Phase Modulation (vibrato); rhythm control
    End Enum

    Public Enum RegDataMask As Byte
        'Global
        IRQFlag = &B_10000000
        Timer1Flag = &B_01000000
        Timer2Flag = &B_00100000
        WSEn = &B_0010000                   'Wave select enable (OPL2)
        Test = &B_11111111
        Timer1 = &B_11111111
        Timer2 = &B_11111111
        IRQReset = &B_10000000
        Timer1Mask = &B_01000000
        Timer2Mask = &B_00100000
        Timer2Start = &B_00000010
        Timer1Start = &B_00000001
        CSMEn = &B_10000000
        NoteSel = &B_01000000

        'Operator
        AM = &B_10000000
        PM = &B_01000000
        EGType = &B_00100000
        KSR = &B_00010000
        Multi = &B_00001111
        KSL = &B_11000000
        TL = &B_00111111
        AR = &B_11110000
        DR = &B_00001111
        SL = &B_11110000
        RR = &B_00001111

        'Channel
        FNumLo = &B_11111111
        KeyOn = &B_00100000
        Block = &B_00011100
        FNumHi = &B_00000011
        FB = &B_00001110
        Con = &B_00000001
        WS = &B_00000011

        'Global / Rhythm
        AMD = &B_10000000
        PMD = &B_01000000
        RhythmEn = &B_00100000
        BD = &B_00010000
        SD = &B_00001000
        TOM = &B_00000100
        TC = &B_00000010
        HH = &B_00000001
    End Enum

    Public Enum EGType As Byte
        Percussive = 0
        Sustained = 1
    End Enum

    Public Enum WaveformType As Byte
        Normal
        Half
        Absolute
        Quarter
    End Enum

#End Region

#Region "Classes and Structures"

    Public Class OPLRegisters
        Public WSEn As Boolean          'OPL2
        Public Test As Byte
        Public Timer1 As Byte
        Public Timer2 As Byte
        Public IRQReset As Boolean
        Public Timer1Mask As Boolean
        Public Timer2Mask As Boolean
        Public Timer2Start As Boolean
        Public Timer1Start As Boolean
        Public CSMEn As Boolean
        Public NoteSel As Boolean

        Public AMD As Boolean
        Public PMD As Boolean

        Public RhythmEn As Boolean
        Public BD As Boolean
        Public SD As Boolean
        Public TOM As Boolean
        Public TC As Boolean
        Public HH As Boolean
        Public ReadOnly Property Chip As Chip

        Sub New(ByRef chip As Chip)
            Me.Chip = chip
        End Sub


    End Class

    Public Class OPLChannel
        Public FNumLo As Byte
        Public FNumHi As Byte
        Public FNum As UInteger
        Public KeyOn As Boolean
        Public Block As Byte
        Public FB As Byte
        Public Con As Byte
        Public Waveform As WaveformType
        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Chip As Chip

        Protected _Op As OPLOp()
        Public Property Op As OPLOp()
            Get
                Return _Op
            End Get
            Protected Set(value As OPLOp())
                _Op = value
            End Set
        End Property

        Sub New(index As Integer, ByRef chip As Chip, Optional opCount As Integer = Op_Count)
            Me.Index = index
            Me.Chip = chip

            ReDim Op(opCount - 1)
            For opIdx = 0 To Op.Length - 1
                Op(opIdx) = New OPLOp(opIdx, Me)
            Next
        End Sub

    End Class

    Public Class OPLOp
        Public AM As Boolean
        Public PM As Boolean
        Public EGType As EGType
        Public KSR As Byte
        Public Multi As Single
        Public KSL As Byte
        Public TL As Byte
        Public DC As Byte
        Public DM As Byte
        Public AR As Byte
        Public DR As Byte
        Public SL As Byte
        Public RR As Byte

        Public ReadOnly Property IsCarrier As Boolean
            Get
                Return Index = 1
            End Get
        End Property

        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Channel As OPLChannel

        Sub New(index As Integer, ByRef ch As OPLChannel)
            Me.Index = index
            Me.Channel = ch
        End Sub

    End Class

    'Public Class OPLInstrument
    '    Public ReadOnly Property Chip As Chip
    '    Public ReadOnly Property Op As OPLOp()

    '    Sub New(ByRef chip As Chip)
    '        _Chip = chip
    '        ReDim _Op(Op_Count - 1)
    '        For i As Integer = 0 To Op_Count - 1
    '            _Op(i) = New OPLOp(i, chip)
    '        Next
    '    End Sub

    'End Class

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

        Protected _MasterClockDivider As Integer
        ''' <summary>
        ''' Number by which to divide the master clock.
        ''' </summary>
        ''' <returns>An <c>Integer</c> divisor. 72 for YM2413.</returns>
        Public ReadOnly Property MasterClockDivider As Integer
            Get
                Return TotalOpSlots * Cycles_Per_Slot
            End Get
        End Property

        Protected _Registers As OPLRegisters
        ''' <summary>
        ''' Global registers common to all OPL chips.
        ''' </summary>
        ''' <returns>An <c>OPLRegisters</c> structure.</returns>
        Public Property Registers As OPLRegisters
            Get
                Return _Registers
            End Get
            Protected Set(value As OPLRegisters)
                _Registers = value
            End Set
        End Property

        Protected _Channel As OPLChannel()
        ''' <summary>
        ''' Parameters for each channel. 
        ''' </summary>
        ''' <returns></returns>
        Public Property Channel As OPLChannel()
            Get
                Return _Channel
            End Get
            Protected Set(value As OPLChannel())
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
            Registers = New OPLRegisters(Me)

            ReDim Channel(ChannelCount - 1)
            For i As Integer = 0 To Channel.Length - 1
                Channel(i) = New OPLChannel(i, Me)
            Next

        End Sub
        Protected Overridable Sub InitializeFrequencies()
            IntSampleClockFreq = MasterClockFreq / MasterClockDivider
            IntClocksPerExtClock = IntSampleClockFreq / ExtSampleClockFreq
        End Sub

        Public Function Read(addr As Byte) As Byte
            Return 0
        End Function

        Public Function Write(addr As OPLL.RegAddr, value As Byte) As Boolean

            If addr < &H20 OrElse addr = &HBD Then 'Global parameter / rhythm

                With Registers
                    Select Case addr
                        Case RegAddr.Test
                            Select Case ChipType
                                Case ChipType.YM3526
                                    .Test = value And &HFF
                                Case ChipType.YM3812
                                    .WSEn = (value And RegDataMask.WSEn) <> 0
                                    .Test = value And &HF
                            End Select
                            RaiseEvent TESTChanged(Me)
                        Case RegAddr.Timer1
                            .Timer1 = value
                            RaiseEvent Timer1Changed(Me)
                        Case RegAddr.Timer2
                            .Timer2 = value
                            RaiseEvent Timer2Changed(Me)
                        Case RegAddr.IRQ_TimerCtrl
                            .IRQReset = (value And RegDataMask.IRQReset) <> 0
                            .Timer1Mask = (value And RegDataMask.Timer1Mask) <> 0
                            .Timer2Mask = (value And RegDataMask.Timer2Mask) <> 0
                            .Timer2Start = (value And RegDataMask.Timer2Start) <> 0
                            .Timer1Start = (value And RegDataMask.Timer1Start) <> 0
                            RaiseEvent IRQ_TimerCtrlChanged(Me)
                        Case RegAddr.CSMEn_NoteSel
                            .CSMEn = (value And RegDataMask.CSMEn) <> 0
                            .NoteSel = (value And RegDataMask.NoteSel) <> 0
                            RaiseEvent CSMEn_NoteSelChanged(Me)
                        Case RegAddr.AMD_PMD_Rhythm
                            .AMD = (value And RegDataMask.AMD) <> 0
                            .PMD = (value And RegDataMask.PMD) <> 0
                            .RhythmEn = (value And RegDataMask.RhythmEn) <> 0
                            .BD = (value And RegDataMask.BD) <> 0
                            .SD = (value And RegDataMask.SD) <> 0
                            .TOM = (value And RegDataMask.TOM) <> 0
                            .TC = (value And RegDataMask.TC) <> 0
                            .HH = (value And RegDataMask.HH) <> 0
                            RaiseEvent AMD_PMD_RhythmChanged(Me)
                        Case Else
                            Return False
                    End Select
                End With

            ElseIf addr >= &H20 AndAlso addr < &HA0 Then ' Operator Parameter
                'Get op sub index (0 to 5)
                Dim idx2 As Byte =
                    addr And &B_00000111

                'Reject invalid addresses
                If idx2 > 5 Then
                    Return False
                End If

                'Get op group index (0 to 2)
                Dim idx1 As Byte =
                    (addr And &B_00011000) >> 3

                'Get channel index
                Dim chIdx As Byte =
                    (idx1 * 3) + (idx2 Mod 3)

                'Get op index
                Dim opIdx As Byte =
                    idx2 \ 3

                'Set channel and op
                Dim ch As OPLChannel =
                    Channel(chIdx)
                Dim op As OPLOp =
                    ch.Op(opIdx)

                With op
                    Select Case addr And &HE0
                        Case RegAddr.AM_PM_EGType_KSR_Multi
                            .AM = (value And RegDataMask.AM) <> 0
                            .PM = (value And RegDataMask.PM) <> 0
                            .EGType = (value And RegDataMask.EGType) >> 5
                            .KSR = (value And RegDataMask.KSR) >> 4
                            .Multi = value And RegDataMask.Multi
                            RaiseEvent AM_PM_EGType_KSR_MultiChanged(op)
                        Case RegAddr.KSL_TL
                            .KSL = (value And RegDataMask.KSL) >> 6
                            .TL = (value And RegDataMask.TL)
                            RaiseEvent KSL_TLChanged(op)
                        Case RegAddr.AR_DR
                            .AR = (value And RegDataMask.AR) >> 4
                            .DR = value And RegDataMask.DR
                            RaiseEvent AR_DRChanged(op)
                        Case RegAddr.SL_RR
                            .SL = (value And RegDataMask.SL) >> 4
                            .RR = value And RegDataMask.RR
                            RaiseEvent SL_RRChanged(op)
                        Case Else
                            Return False
                    End Select
                End With

            ElseIf addr >= &HA0 AndAlso addr < &H100 Then

                Static WasPrevFNumWriteHi(ChannelCount - 1) As Boolean

                'Get channel index
                Dim chIdx As Byte =
                    addr And &HF

                'Reject invalid addresses
                If chIdx > 8 Then
                    Return False
                End If

                'Set channel
                Dim ch As OPLChannel =
                    Channel(chIdx)

                With ch
                    Select Case addr And &HF0
                        Case RegAddr.FNumLo
                            .FNumLo = value And RegDataMask.FNumLo
                            .FNum = (.FNum And &B_1100000000) Or .FNumLo
                            RaiseEvent FNumLoChanged(ch)

                            'If Not WasPrevFNumWriteHi(chIdx) Then
                            '    RaiseEvent FNumChanged(ch)
                            'End If
                            WasPrevFNumWriteHi(chIdx) = False
                        Case RegAddr.KeyOn_Block_FNumHi
                            .KeyOn = (value And RegDataMask.KeyOn) <> 0
                            .Block = (value And RegDataMask.Block) >> 2

                            .FNumHi = value And RegDataMask.FNumHi
                            WasPrevFNumWriteHi(chIdx) = True
                            .FNum = (.FNum And &B_0011111111) Or (CUInt(.FNumHi) << 8)

                            RaiseEvent KeyOnBlockFNumHiChanged(ch)
                            'RaiseEvent FNumChanged(Me, ch, .FNum)
                        Case RegAddr.FB_Con
                            .FB = (value And RegDataMask.FB) >> 1
                            .Con = value And RegDataMask.Con
                            RaiseEvent FB_ConChanged(ch)
                        Case RegAddr.WS
                            .Waveform = value And RegDataMask.WS
                            RaiseEvent WaveformChanged(ch)
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

        ' Global events / rhythm
        Public Event TESTChanged(ByRef chip As Chip)
        Public Event Timer1Changed(ByRef chip As Chip)
        Public Event Timer2Changed(ByRef chip As Chip)
        Public Event IRQ_TimerCtrlChanged(ByRef chip As Chip)
        Public Event CSMEn_NoteSelChanged(ByRef chip As Chip)
        Public Event AMD_PMD_RhythmChanged(ByRef chip As Chip)

        ' Operator events
        Public Event AM_PM_EGType_KSR_MultiChanged(ByRef op As OPLOp)
        Public Event KSL_TLChanged(ByRef op As OPLOp)
        Public Event AR_DRChanged(ByRef op As OPLOp)
        Public Event SL_RRChanged(ByRef op As OPLOp)


        ' Channel events
        Public Event FNumLoChanged(ByRef ch As OPLChannel)
        Public Event KeyOnBlockFNumHiChanged(ByRef ch As OPLChannel)
        Public Event FNumChanged(ByRef ch As OPLChannel)
        Public Event FB_ConChanged(ByRef ch As OPLChannel)
        Public Event WaveformChanged(ByRef ch As OPLChannel)        'OPL2

#End Region

    End Class

End Class

Public Class YM3526

    Public Class Chip
        Inherits OPL.Chip
        Public Overrides ReadOnly Property ChipType As ChipType = ChipType.YM3526

        Public Overrides ReadOnly Property ChannelCount As Integer
            Get
                Return 9
            End Get
        End Property

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)
        End Sub
    End Class

End Class

Public Class YM3812

    Public Class Chip
        Inherits OPL.Chip
        Public Overrides ReadOnly Property ChipType As ChipType = ChipType.YM3812

        Public Overrides ReadOnly Property ChannelCount As Integer
            Get
                Return 9
            End Get
        End Property

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)
        End Sub

    End Class

End Class