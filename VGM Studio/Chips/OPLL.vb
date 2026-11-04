'Yamaha Operator Type-LL (OPLL) Simulator/Emulator
'for YM2413/B (OPLL), YM2420, YM2423, YM2425, YMF281/B, Konami VRC7
'Copyright © 2014-2026 Paul Jensen
'Portions based on:
'- Nuked-OPN2 by Nukeyt
'- YMFM by Aaron Giles

Imports VGM_Studio.OPLL
Public Class OPLL

#Region "Constants"

    ' Operator count per voice
    Public Const Op_Count As Integer = 2

    ' Clock cycles per slot
    Public Const Cycles_Per_Slot As Integer = 4

#End Region

#Region "Shared Methods"

    Const PhaseIncMax As UInteger = (1 << 19) - 1
    Shared Function GetFNote(block As UInteger, fnum As UInteger, multi As UInteger, sampleClockFreq As Single) As Single
        Dim phaseInc As UInteger =
            GetPhaseInc(block, fnum, multi)
        Return sampleClockFreq * (phaseInc / PhaseIncMax)
    End Function
    Shared Function GetFNote(block As UInteger, fnum As UInteger, sampleClockFreq As Single) As Single
        Return GetFNote(block, fnum, 1, sampleClockFreq)
    End Function

    'Shared Function GetFNote(block As Integer, fnum As Integer, sampleClockFreq As Single) As Single
    '    Return fnum * sampleClockFreq * Math.Pow(2, block - 19)
    'End Function

    'Shared Function GetFNote(fnum As Integer, block As Byte, fsam As Long) As Single
    '    'F = (fmus × 2^18/fsam) / 2b^-1
    '    Return fnum * (2 ^ (block - 1)) / ((2 ^ 18) / fsam)
    'End Function

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
        YM2413
        KonamiVRC7
        YM2423
        YMF281
    End Enum

    Public Enum RegAddr As Byte
        'User instrument
        AM_PM_EGType_KSR_Multi = &H0        'Amplitude modulation; phase modulation; envelope generator type; key scaling rate; phase multiplier
        KSL_TL = &H2                        'Key scaling level; total attenuation level (for carrier waveform)
        KSL_DC_DM = &H3                     'Key scaling level; rectify carrier and/or modulator to half-wave
        AR_DR = &H4                         'Attack rate; decay rate
        SL_RR = &H6                         'Sustain level; release rate

        'Common
        Rhythm = &HE                        'Rhythm control
        Test = &HF                          'LSI test data

        'Channel
        FNumLo = &H10                     'Low bits of FNum
        Sus_Key_Block_FNumHi = &H20        'Sustain on/off; Key on/off; octave; high bit of FNum
        Inst_Vol = &H30                     'Instrument selection; instrument volume control

        'Rhythm
        BDVol = &H36                        'Bass drum volume
        HHVol_SDVol = &H37                  'High hat volume; snare drum volume
        TOMVol_TCYMVol = &H38               'Tom volume; top/ride cymbal volume
    End Enum

    Public Enum RegDataMask As Byte
        'User instrument
        AM = &B_10000000
        PM = &B_01000000
        EGType = &B_00100000
        KSR = &B_00010000
        Multi = &B_00001111
        KSL = &B_11000000
        TL = &B_00111111
        DC = &B_00010000
        DM = &B_00001000
        FB = &B_00000111
        AR = &B_11110000
        DR = &B_00001111
        SL = &B_11110000
        RR = &B_00001111
        RhythmEn = &B_00100000
        BD = &B_00010000
        SD = &B_00001000
        TOM = &B_00000100
        TCYM = &B_00000010
        HH = &B_00000001
        Test = &B_11111111
        FNumLo = &B_11111111
        Sus = &B_00100000
        Key = &B_000010000
        Block = &B_00001110
        FNumHi = &B_00000001
        Inst = &B_11110000
        Vol = &B_00001111
        BDVol = &B_00001111
        HHVol = &B_11110000
        SDVol = &B_00001111
        TOMVol = &B_11110000
        TCYMVol = &B_00001111
    End Enum

    Public Enum InstType As Byte
        User = 0
        Violin = 1
        Guitar = 2
        Piano = 3
        Flute = 4
        Clarinet = 5
        Oboe = 6
        Trumpet = 7
        Organ = 8
        Horn = 9
        Synthesizer = 10
        Harpsichord = 11
        Vibraphone = 12
        SynthBass = 13
        AcousticBass = 14
        ElectricGuitar = 15
    End Enum

    Public Enum EGType As Byte
        Percussive = 0
        Sustained = 1
    End Enum

#End Region

#Region "Classes and Structures"

    Public Class OPLLRegisters
        Public RhythmEn As Boolean
        Public BD As Boolean
        Public SD As Boolean
        Public TOM As Boolean
        Public TCYM As Boolean
        Public HH As Boolean
        Public Test As Byte
        Public BDVol As Byte
        Public HHVol As Byte
        Public SDVol As Byte
        Public TOMVol As Byte
        Public TCYMVol As Byte
        Public ReadOnly Property Chip As Chip

        Sub New(ByRef chip As Chip)
            Me.Chip = chip
        End Sub


    End Class

    Public Class OPLLOp
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

        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Chip As Chip

        Sub New(index As Integer, ByRef chip As Chip)
            Me.Index = index
            Me.Chip = chip
        End Sub

    End Class

    Public Class OPLLInstrument
        Public ReadOnly Property Chip As Chip

        Protected _Op As OPLLOp()
        Public Property Op As OPLLOp()
            Get
                Return _Op
            End Get
            Protected Set(value As OPLLOp())
                _Op = value
            End Set
        End Property

        Sub New(ByRef chip As Chip, Optional opCount As Integer = Op_Count)
            Me.Chip = chip
            ReDim Op(Op_Count - 1)
            For i As Integer = 0 To Op.Length - 1
                Op(i) = New OPLLOp(i, chip)
            Next

        End Sub

    End Class

    Public Class OPLLChannel
        Public FNumLo As Byte
        Public FNumHi As Byte
        Public FNum As UInteger
        Public Sus As Boolean
        Public Key As Boolean
        Public Block As Byte
        Public Inst As InstType
        Public Vol As Byte

        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Chip As Chip
        Sub New(index As Integer, ByRef chip As Chip)
            Me.Index = index
            Me.Chip = chip
        End Sub

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

        Protected _Registers As OPLLRegisters
        ''' <summary>
        ''' Global registers common to all OPLL chips.
        ''' </summary>
        ''' <returns>An <c>OPLLRegisters</c> structure.</returns>
        Public Property Registers As OPLLRegisters
            Get
                Return _Registers
            End Get
            Protected Set(value As OPLLRegisters)
                _Registers = value
            End Set
        End Property

        Protected _Channel As OPLLChannel()
        ''' <summary>
        ''' Parameters for each channel. 
        ''' </summary>
        ''' <returns></returns>
        Public Property Channel As OPLLChannel()
            Get
                Return _Channel
            End Get
            Protected Set(value As OPLLChannel())
                _Channel = value
            End Set
        End Property

        Public ReadOnly Property UserInst As OPLLInstrument

#End Region

#Region "Methods"

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            'Set clock frequencies
            Me.MasterClockFreq = masterClockFreq
            Me.ExtSampleClockFreq = extSampleClockFreq
            InitializeFrequencies()
            Registers = New OPLLRegisters(Me)

            ReDim _Channel(ChannelCount - 1)
            For i As Integer = 0 To ChannelCount - 1
                _Channel(i) = New OPLLChannel(i, Me)
            Next

            UserInst = New OPLLInstrument(Me)
        End Sub
        Protected Overridable Sub InitializeFrequencies()
            IntSampleClockFreq = MasterClockFreq / MasterClockDivider
            IntClocksPerExtClock = IntSampleClockFreq / ExtSampleClockFreq
        End Sub

        Public Function Read(addr As Byte) As Byte
            Return 0
        End Function

        Public Function Write(addr As OPLL.RegAddr, value As Byte) As Boolean

            If addr < &H10 Then

                If addr < &HE Then 'User Channel parameter

                    Dim opIdx As Byte =
                        addr And 1

                    Dim op As OPLLOp =
                        UserInst.Op(opIdx)

                    With op
                        Select Case addr And &H6
                            Case RegAddr.AM_PM_EGType_KSR_Multi
                                .AM = (value And RegDataMask.AM) <> 0
                                .PM = (value And RegDataMask.PM) <> 0
                                .EGType = (value And RegDataMask.EGType) >> 5
                                .KSR = (value And RegDataMask.KSR) >> 4
                                .Multi = value And RegDataMask.Multi
                                RaiseEvent AM_PM_EGType_KSR_MultiChanged(op)
                            Case RegAddr.KSL_TL
                                .KSL = (value And RegDataMask.KSL) >> 6
                                If opIdx = 0 Then
                                    .TL = (value And RegDataMask.TL)
                                    RaiseEvent KSL_TLChanged(op)
                                Else
                                    .DC = (value And RegDataMask.DC) >> 4
                                    .DM = (value And RegDataMask.DM) >> 3
                                    RaiseEvent KSL_DC_DMChanged(op)
                                End If
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

                Else 'Common parameter

                    With Registers
                        Select Case addr
                            Case RegAddr.Rhythm
                                .RhythmEn = (value And RegDataMask.RhythmEn) <> 0
                                .BD = (value And RegDataMask.BD) <> 0
                                .SD = (value And RegDataMask.SD) <> 0
                                .TOM = (value And RegDataMask.TOM) <> 0
                                .TCYM = (value And RegDataMask.TCYM) <> 0
                                .HH = (value And RegDataMask.HH) <> 0
                                RaiseEvent RhythmChanged(Me)
                            Case RegAddr.Test
                                .Test = value
                                RaiseEvent TESTChanged(Me)
                            Case Else
                                Return False
                        End Select
                    End With

                End If

            ElseIf addr >= &H10 And addr < &H40 Then ' Channel Parameter

                Dim chIdx As Byte =
                    addr And &HF

                'Reject invalid addresses
                If chIdx > 8 Then
                    Return False
                End If

                Static WasPrevFNumWriteHi(ChannelCount - 1) As Boolean

                Dim ch As OPLLChannel =
                    Channel(chIdx)

                With ch
                    Select Case addr And &HF0
                        Case RegAddr.FNumLo
                            .FNumLo = value And RegDataMask.FNumLo
                            .FNum = (.FNum And &B_100000000) Or .FNumLo
                            RaiseEvent FNumLoChanged(ch)

                            If Not WasPrevFNumWriteHi(chIdx) Then
                                RaiseEvent FNumChanged(ch)
                            End If
                            WasPrevFNumWriteHi(chIdx) = False
                        Case RegAddr.Sus_Key_Block_FNumHi
                            .Sus = (value And RegDataMask.Sus) <> 0
                            .Key = (value And RegDataMask.Key) <> 0
                            .Block = (value And RegDataMask.Block) >> 1

                            .FNumHi = value And RegDataMask.FNumHi
                            WasPrevFNumWriteHi(chIdx) = True
                            .FNum = (.FNum And &B_011111111) Or (CUInt(.FNumHi) << 8)

                            RaiseEvent SusKeyBlockFNumHiChanged(ch)
                            'RaiseEvent FNumChanged(Me, ch, .FNum)
                        Case RegAddr.Inst_Vol
                            .Inst = (value And RegDataMask.Inst) >> 4
                            .Vol = value And RegDataMask.Vol
                            RaiseEvent InstVolChanged(ch)
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

        ' ********************************
        ' *** User channel events

        Public Event AM_PM_EGType_KSR_MultiChanged(ByRef op As OPLLOp)
        Public Event KSL_TLChanged(ByRef op As OPLLOp)
        Public Event KSL_DC_DMChanged(ByRef op As OPLLOp)
        Public Event AR_DRChanged(ByRef op As OPLLOp)
        Public Event SL_RRChanged(ByRef op As OPLLOp)

        ' ********************************
        ' *** Common events

        Public Event TESTChanged(ByRef chip As Chip)
        Public Event RhythmChanged(ByRef chip As Chip)
        Public Event BDVolChanged(ByRef chip As Chip)
        Public Event HHVol_SDVolChanged(ByRef chip As Chip)
        Public Event TOMVol_TCYMVolChanged(ByRef chip As Chip)

        ' ********************************
        ' *** Channel events

        Public Event FNumLoChanged(ByRef ch As OPLLChannel)
        Public Event SusKeyBlockFNumHiChanged(ByRef ch As OPLLChannel)
        Public Event FNumChanged(ByRef ch As OPLLChannel)
        Public Event InstVolChanged(ByRef ch As OPLLChannel)

#End Region

    End Class

End Class

Public Class YM2413

    Public Class Chip
        Inherits OPLL.Chip
        Public Overrides ReadOnly Property ChipType As ChipType = ChipType.YM2413

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
