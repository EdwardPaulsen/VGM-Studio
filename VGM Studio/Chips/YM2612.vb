' Yamaha YM2612 (OPN2) Simulator/Emulator
' Copyright © 1999-2026 Paul Jensen

' Portions based on:
' - Exodus emulator source code by Nemesis
' - MAME source code by MAME contributors
' - Findings by various users in the SpritesMind forums
' - Nuked OPN2 source code

'Imports VGM_Studio.OPN

'Public Class YM2612

'    Public Class Chip
'        Inherits OPN.Chip

'#Region "Constants"

'        'Channel and slot counts
'        Public Const Channel_Count As Integer = 6
'        Public Const Op_Count As Integer = 4

'        ' System master clock frequencies
'        Public Const System_Master_Clock_NTSC_60Hz As Long = 53693100
'        Public Const System_Master_Clock_PAL_50Hz As Long = 44744250

'        ' External (master) clock frequencies
'        Public Const Ext_Clock_Divider As Integer = 7
'        Public Const Ext_Clock_NTSC_60Hz As Long = System_Master_Clock_NTSC_60Hz \ Ext_Clock_Divider
'        Public Const Ext_Clock_PAL_50Hz As Long = System_Master_Clock_PAL_50Hz \ Ext_Clock_Divider

'        ' FM prescaler divider
'        Public Const FM_Prescaler_Divider As Integer = 6

'        ' FM clock frequencies
'        Public Const FM_Clock_NTSC_60Hz As Long = Ext_Clock_NTSC_60Hz \ FM_Prescaler_Divider
'        Public Const FM_Clock_PAL_50Hz As Long = Ext_Clock_PAL_50Hz \ FM_Prescaler_Divider

'        ' Sample clock frequencies 
'        Public Const Sample_Clock_Divider As Long = Channel_Count * Op_Count
'        Public Const Sample_Clock_NTSC_60Hz As Long = FM_Clock_NTSC_60Hz \ Sample_Clock_Divider
'        Public Const Sample_Clock_PAL_50Hz As Long = FM_Clock_PAL_50Hz \ Sample_Clock_Divider

'#End Region

'#Region "Clock Frequencies"

'        ''' <summary>
'        ''' Master clock frequency in Hz.
'        ''' </summary>
'        ''' <returns>The master clock frequency of the chip in Hz.</returns>
'        Public ReadOnly Property MasterClockFreq() As Long

'        ''' <summary>
'        ''' FM generator clock frequency in Hz.
'        ''' </summary>
'        ''' <returns>The FM generator clock in Hz.</returns>
'        Public ReadOnly Property FMClockFreq() As Single

'        ''' <summary>
'        ''' Internal sample clock rate in Hz.
'        ''' </summary>
'        ''' <returns>The internal sampling rate of the chip in Hz.</returns>
'        Public ReadOnly Property IntSampleClockFreq() As Single

'        ''' <summary>
'        ''' External sample clock frequency
'        ''' </summary>
'        ''' <returns>The external sampling rate of the chip in Hz.</returns>
'        Public ReadOnly Property ExtSampleClockFreq() As Integer

'        ''' <summary>
'        ''' Number of internal clocks per external sample
'        ''' </summary>
'        ''' <returns>The number of internal clocks per external sample in Hz.</returns>
'        Public ReadOnly Property IntClocksPerExtClock As Double

'#End Region

'#Region "Constructor"
'        Public ReadOnly Property PG As PhaseGenerator
'        Public ReadOnly Property EG As EnvelopeGenerator
'        Public ReadOnly Property OU As OperatorUnit
'        Public ReadOnly Property DU As DACUnit
'        Public ReadOnly Property Channel() As OPN2Channel()
'        Public ReadOnly Property HighQuality As Boolean

'        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
'            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)

'            'Set clock frequencies
'            _MasterClockFreq = masterClockFreq
'            _FMClockFreq = masterClockFreq / FM_Prescaler_Divider
'            _IntSampleClockFreq = FMClockFreq / Sample_Clock_Divider
'            _ExtSampleClockFreq = extSampleClockFreq
'            _IntClocksPerExtClock = IntSampleClockFreq / extSampleClockFreq

'            'Instantiate channels
'            ReDim _Channel(Channel_Count - 1)
'            For i As Integer = 0 To Channel_Count - 1
'                _Channel(i) = New OPN2Channel(Me, i)
'            Next

'            _PG = New PhaseGenerator(Me)
'            _EG = New EnvelopeGenerator(Me)
'            _OU = New OperatorUnit(Me)
'            _DU = New DACUnit(Me, resample, filter, cutoff)

'            _HighQuality = highQuality

'            'Build tables
'            If _HighQuality = True Then
'                BuildHQSinTable()
'                BuildHQPowTable()
'                BuildHQOutputTable()
'            Else
'                BuildSinTable()
'                BuildPowTable()
'                BuildLinPowTable()
'            End If

'            BuildFNoteTable()
'            BuildPhaseIncTable()
'            BuildKeyCodeTable()

'        End Sub

'        ''' <summary>
'        ''' Advance the chip by one clock cycle.
'        ''' </summary>
'        Sub Advance()
'            'Static LFOClocks As Single 'NOT IMPLEMENTED YET
'            'Static TimerAClocks As Single 'NOT IMPLEMENTED YET
'            'Static TimerBClocks As Single 'NOT IMPLEMENTED YET
'            Static PGClocks As Integer 'Single
'            Static EGClocks As Integer 'Single

'            'Advance phase generator once every internal sample clock
'            PGClocks += 1
'            If PGClocks >= PhaseGenerator.ClockDivider Then
'                PG.Advance()
'                PGClocks = 0
'            End If

'            'Calculate output for all slots on all channels
'            For Each ch As OPN2Channel In _Channel
'                With ch
'                    For Each op As OPN2Channel.OPN2Op In .Op
'                        OU.CalculateOpOutput(op)
'                    Next
'                End With
'            Next

'            DU.CalculateOutput()

'            'Advance envelope generator once every three internal sample clocks
'            EGClocks += 1
'            If EGClocks >= EnvelopeGenerator.ClockDivider Then
'                EG.Advance()
'                EGClocks = 0
'            End If

'        End Sub

'        Sub Reset()
'            'PG.reset
'            'EG.reset
'            'For Each ch As Channel In _Channel
'            '    ch.Reset()
'            'Next
'        End Sub

'#End Region

'#Region "Data I/O"

'        Const Port_Min As Byte = 0
'        Const Port_Max As Byte = 1

'        Const Addr_Min As Byte = &H21
'        Const Addr_Max As Byte = &HB6

'        Private ReadOnly Mem(Port_Max, Addr_Max) As Byte

'#Region "Enums"

'        Public Enum PortIndex As Byte
'            Port0 = 0
'            Port1 = 1
'        End Enum

'        Public Enum ChannelIndex As Byte
'            Ch1 = 0
'            Ch2 = 1
'            Ch3 = 2
'            Ch4 = 3
'            Ch5 = 4
'            Ch6 = 5
'        End Enum

'        Public Enum OpIndex As Byte
'            Op1 = 0
'            Op2 = 1
'            Op3 = 2
'            Op4 = 3
'        End Enum

'        Public Enum RegAddr As Byte
'            'Common registers
'            Test1 = &H21                        'LSI test data
'            LFOEn_LFOFreq = &H22                'LFO enable; frequency set
'            TimerAHi = &H24                     'High bits of timer A
'            TimerALo = &H25                     'Low bits of timer A
'            TimerB = &H26                       'Timer B
'            Ch3Mode_Reset_Enable_Load = &H27    'Set mode of channel 3; reset timer overflow flags; enable timer IRQs; load/start timers
'            KeyOn = &H28                        'Slot on/off; channel selection
'            DAC = &H2A                          'DAC data write
'            DACEn = &H2B                        'DAC enable
'            Test2 = &H2C                        'LSI test data

'            'Operator registers
'            DT_Multi = &H30                     'Frequency detune; phase multiplier
'            TL = &H40                           'Total attenuation level
'            KS_AR = &H50                        'Key scale; attack rate
'            AM_DR = &H60                        'Amplitude modulation; decay rate
'            SR = &H70                           'Sustain rate
'            SL_RR = &H80                        'Sustain level; release rate
'            SSG_EG = &H90                       'Sound source generator-type envelope control

'            'Channel registers
'            FNumLo = &HA0                       'Low bits of of FNum
'            Block_FNumHi = &HA4                 'Octave; high bits of FNum
'            Ch3SpFNumLo = &HA8                  'Low bits of channel 3 special mode FNum
'            Ch3SpBlock_FNumHi = &HAC            'Octave; high bits of channel 3 special mode FNum
'            FB_Con = &HB0                       'Self feedback level; operator connection/algorithm
'            LR_AMS_PMS = &HB4                   'Stereo output control; amplitude and phase modulation sensitivity
'        End Enum

'        Public Enum RegDataMask As Byte
'            'Common registers
'            LFOEn = &B_00001000
'            LFOFreq = &B_00000111

'            TimerAMSBs = &B_11111111

'            TimerALSBs = &B_00000011

'            Ch3Mode = &B_11000000

'            TimerB = &B_11111111

'            TimerBReset = &B_00100000
'            TimerAReset = &B_00010000
'            TimerBEnable = &B_00001000
'            TimerAEnable = &B_00000100
'            TimerBLoad = &B_00000010
'            TimerALoad = &B_00000001

'            Slot = &B_11110000
'            Ch = &B_00000111

'            DACData = &B_11111111

'            DACEn = &B_10000000

'            'Operator registers
'            DT = &B_01110000
'            Multi = &B_00001111

'            TL = &B_1111111
'            KS = &B_11000000

'            AR = &B_00011111
'            AM = &B_10000000

'            DR = &B_00011111

'            SR = &B_00011111

'            SL = &B_11100000
'            RR = &B_00001111

'            SSG_EG = &B_00001111
'            SSG_EGEnabled = &B_00001000
'            SSG_EGAttack = &B_00000100
'            SSG_EGAlternate = &B_00000010
'            SSG_EGHold = &B_00000001

'            'Channel registers
'            FNumLSBs = &B_11111111

'            Block = &B_00111000
'            FNumMSBs = &B_00000111

'            Ch3SpFNumLSBs = &B_11111111

'            Ch3SpBlock = &B_00111000
'            Ch3SpFNumMSBs = &B_00000111

'            FB = &B_00111000
'            Con = &B_00000111

'            L = &B_10000000
'            R = &B_01000000
'            AMS = &B_00110000
'            PMS = &B_00000111
'        End Enum

'        Public Enum Ch3ModeValue As Byte
'            Normal = 0
'            Special = 1
'            CSM = 2
'            Illegal = 3
'        End Enum

'#End Region

'        ''Added 2018/10/1: Descriptive text for last operation performed
'        'Public ReadOnly Property Text As String

'        Public Function Read(bank As PortIndex, addr As RegAddr) As Integer
'            If AccessIsLegal(bank, addr) Then
'                Return Mem(bank, addr)
'            Else
'                Return -1
'            End If
'        End Function

'        ' Added on 8/19/2026: Lower-level support for delayed writes
'        Private _delayedWriteValues As New Dictionary(Of Byte, Byte) From
'            {{RegAddr.Block_FNumHi, 0}, {RegAddr.Block_FNumHi + 1, 0}, {RegAddr.Block_FNumHi + 2, 0}, {RegAddr.Ch3SpBlock_FNumHi, 0}, {RegAddr.Ch3SpBlock_FNumHi + 1, 0}, {RegAddr.Ch3SpBlock_FNumHi + 2, 0}}

'        Public Function Write(port As PortIndex, addr As RegAddr, value As Byte) As Integer
'            If AccessIsLegal(port, addr) Then
'                Mem(port, addr) = value
'                ' Added on 8/19/2026: Lower-level support for delayed writes
'                Select Case addr
'                    Case RegAddr.FNumLo To (RegAddr.FNumLo + 2), RegAddr.Ch3SpFNumLo To (RegAddr.Ch3SpFNumLo + 2)
'                        ' When LSBs are written for frequency values, then MSBs can be written
'                        Mem(0, addr + 4) = _delayedWriteValues(addr + 4)
'                        Mem(1, addr + 4) = _delayedWriteValues(addr + 4)
'                End Select
'                UpdateParameters(port, addr, value)
'                Return 0
'            Else
'                '_Text = "ILLEGAL"
'                Return -1
'            End If
'        End Function

'        Shared Function AccessIsLegal(port As PortIndex, addr As RegAddr) As Boolean

'            If (addr >= Addr_Min) And (addr <= Addr_Max) Then

'                Select Case addr

'                    Case RegAddr.Test1, RegAddr.LFOEn_LFOFreq, RegAddr.TimerAHi,
'                            RegAddr.TimerALo, RegAddr.TimerB, RegAddr.Ch3Mode_Reset_Enable_Load,
'                            RegAddr.KeyOn, RegAddr.DAC, RegAddr.DACEn, RegAddr.Test2

'                        If port = PortIndex.Port1 Then
'                            Return False
'                        Else
'                            Return True
'                        End If

'                    Case RegAddr.DT_Multi To (RegAddr.DT_Multi + &HE),
'                            RegAddr.TL To (RegAddr.TL + &HE),
'                            RegAddr.KS_AR To (RegAddr.KS_AR + &HE),
'                            RegAddr.AM_DR To (RegAddr.AM_DR + &HE),
'                            RegAddr.SR To (RegAddr.SR + &HE),
'                            RegAddr.SL_RR To (RegAddr.SL_RR + &HE),
'                            RegAddr.SSG_EG To (RegAddr.SSG_EG + &HE)

'                        Dim opAddr As Byte = (addr And &HF)
'                        Select Case opAddr
'                            Case 3, 7, &HB, &HF     'These operator addresses are illegal
'                                Return False
'                            Case Else
'                                Return True
'                        End Select

'                    Case RegAddr.FNumLo To (RegAddr.FNumLo + 2),
'                            RegAddr.Block_FNumHi To (RegAddr.Block_FNumHi + 2),
'                            RegAddr.Ch3SpFNumLo To (RegAddr.Ch3SpFNumLo + 2),
'                            RegAddr.Ch3SpBlock_FNumHi To (RegAddr.Ch3SpBlock_FNumHi + 2),
'                            RegAddr.FB_Con To (RegAddr.FB_Con + 2),
'                            RegAddr.LR_AMS_PMS To (RegAddr.LR_AMS_PMS + 2)

'                        Dim opAddr As Byte = (addr And &HF)
'                        Select Case opAddr
'                            Case 3, 7, &HB, &HF     'These operator addresses are illegal
'                                Return False
'                            Case Else
'                                Return True
'                        End Select

'                End Select

'            End If

'            Return False

'        End Function

'#End Region

'#Region "Tables"

'        Public Const TwoPi As Double = 2 * Math.PI
'        Public Const PiOver2 As Double = Math.PI / 2

'        Public Const SinTableBitCount As Integer = 8
'        Public Const SinTableLen As Integer = 1 << SinTableBitCount
'        Public Const SinTableMax As Integer = SinTableLen - 1

'        Public ReadOnly SinTable(SinTableMax) As Integer

'        Public Const AttenFixedBitCount As Integer = 8
'        Public Const AttenFixedLen As Integer = 1 << AttenFixedBitCount
'        Public Const AttenFixedMax As Integer = AttenFixedLen - 1

'        Private Sub BuildSinTable()
'            For i As Integer = 0 To SinTableMax
'                Dim phaseNormalized As Double = ((i * 2) + 1) / (1 << (SinTableBitCount + 1))
'                Dim sinResultNormalized As Double = Math.Sin(phaseNormalized * PiOver2)
'                Dim sinResultAsAttenuation As Double = -Math.Log(sinResultNormalized) / Math.Log(2)
'                Dim sinResult As Integer = sinResultAsAttenuation * (1 << AttenFixedBitCount) + 0.5
'                SinTable(i) = sinResult
'            Next
'        End Sub

'        Public ReadOnly HQSinTable(2047) As Single

'        Private Sub BuildHQSinTable()
'            For phase As Integer = 0 To 2047
'                Dim normPhase As Single = phase / PhaseGenerator.PhaseMax
'                HQSinTable(phase) = Math.Sin(normPhase * TwoPi)
'            Next
'        End Sub

'        Public Const PowTableBitCount As Integer = 8
'        Public Const PowTableLen As Integer = 1 << PowTableBitCount
'        Public Const PowTableMax As Integer = PowTableLen - 1

'        Public Const PowTableOutputBitCount As Integer = 11
'        Public Const PowTableOutputLen As Integer = 1 << PowTableOutputBitCount
'        Public Const PowTableOutputMax As Integer = PowTableOutputLen - 1

'        Public ReadOnly PowTable(PowTableMax) As Integer

'        Private Sub BuildPowTable()
'            For i As Integer = 0 To PowTableMax
'                Dim entryNormalized As Double = (i + 1) / (1 << PowTableBitCount)
'                Dim resultNormalized As Double = 2 ^ -entryNormalized
'                Dim result As Integer = resultNormalized * (1 << PowTableOutputBitCount) + 0.5
'                PowTable(i) = result
'            Next
'        End Sub

'        Public Const LinPowTableOutputBitCount As Integer = 13
'        Public Const LinPowTableOutputLen As Integer = 3328 'This is 01101.00000000, or shift value=13, pow table index=0 in fixed point binary; nothing over this is needed since shift values of 13 or higher always output 0
'        Public Const LinPowTableOutputMax As Integer = LinPowTableOutputLen - 1

'        Public ReadOnly LinPowTable(LinPowTableOutputLen) As Integer

'        Private Sub BuildLinPowTable()
'            For i As Integer = 0 To LinPowTableOutputLen
'                LinPowTable(i) = (PowTable(i And PowTableMax) << 2) >> (i >> PowTableBitCount)
'            Next
'        End Sub

'        Public ReadOnly HQPowTable(2047) As Single

'        Private Sub BuildHQPowTable()
'            Const weight As Single = 0.0046875 '(96 / 2048) / 10
'            For atten As Integer = 0 To 2047
'                HQPowTable(atten) = 10 ^ -(atten * weight)
'            Next
'        End Sub

'        Public ReadOnly HQOutputTable(2047, 2047) As Single

'        Private Sub BuildHQOutputTable()
'            For phase As Integer = 0 To 2047
'                For atten As Integer = 0 To 2047
'                    HQOutputTable(phase, atten) = HQSinTable(phase) * HQPowTable(atten)
'                Next
'            Next
'        End Sub

'        Public ReadOnly DetunePhaseIncTable As Integer(,) =
'            {{0, 0, 1, 2}, {0, 0, 1, 2}, {0, 0, 1, 2}, {0, 0, 1, 2},
'             {0, 1, 2, 2}, {0, 1, 2, 3}, {0, 1, 2, 3}, {0, 1, 2, 3},
'             {0, 1, 2, 4}, {0, 1, 3, 4}, {0, 1, 3, 4}, {0, 1, 3, 5},
'             {0, 2, 4, 5}, {0, 2, 4, 6}, {0, 2, 4, 6}, {0, 2, 5, 7},
'             {0, 2, 5, 8}, {0, 3, 6, 8}, {0, 3, 6, 9}, {0, 3, 7, 10},
'             {0, 4, 8, 11}, {0, 4, 8, 12}, {0, 4, 9, 13}, {0, 5, 10, 14},
'             {0, 5, 11, 16}, {0, 6, 12, 17}, {0, 6, 13, 19}, {0, 7, 14, 20},
'             {0, 8, 16, 22}, {0, 8, 16, 22}, {0, 8, 16, 22}, {0, 8, 16, 22}}

'        Public ReadOnly PhaseModIncTable As Integer(,) =
'            {{0, 0, 0, 0, 0, 0, 0, 0},
'             {0, 0, 0, 0, 1, 1, 1, 1},
'             {0, 0, 0, 1, 1, 1, 2, 2},
'             {0, 0, 1, 1, 2, 2, 3, 3},
'             {0, 0, 1, 2, 2, 2, 3, 4},
'             {0, 0, 2, 3, 4, 4, 5, 6},
'             {0, 0, 4, 6, 8, 8, 10, 12},
'             {0, 0, 8, 12, 16, 16, 20, 24}}

'        Public Const BlockBitCount As Integer = 3
'        Public Const BlockMax As Integer = (1 << BlockBitCount) - 1
'        Public Const FNumBitCount As Integer = 11
'        Public Const FNumMax As Integer = (1 << FNumBitCount) - 1

'        Public Const PhaseIncBitCount As Integer = 20
'        Public Const PhaseIncMax As Integer = (1 << PhaseIncBitCount) - 1

'        Public ReadOnly FNoteTable(PhaseIncMax) As Single

'        Sub BuildFNoteTable()
'            For phaseInc As Integer = 1 To PhaseIncMax
'                FNoteTable(phaseInc) = IntSampleClockFreq * (phaseInc / PhaseIncMax)
'            Next
'        End Sub

'        Shared Function GetFNote(block As Integer, fnum As Integer, sampleClockFreq As Single) As Single
'            Return fnum * sampleClockFreq * Math.Pow(2, block - 21)
'        End Function

'        Public ReadOnly PhaseIncTable(PhaseIncMax) As Single

'        Sub BuildPhaseIncTable()
'            For phaseInc As Integer = 0 To PhaseIncMax
'                PhaseIncTable(phaseInc) = (TwoPi * FNoteTable(phaseInc)) / ExtSampleClockFreq
'            Next
'        End Sub

'        Public ReadOnly KeyCodeTable(BlockMax, FNumMax) As Byte

'        Sub BuildKeyCodeTable()
'            For block As Integer = 0 To BlockMax
'                For fNum As Integer = 0 To FNumMax
'                    KeyCodeTable(block, fNum) = CalculateKeyCode(block, fNum)
'                Next
'            Next
'        End Sub

'        Public Shared Function CalculateKeyCode(block As Integer, fNum As Integer) As Byte
'            Dim F11 As Byte = (fNum >> 10) And 1
'            Dim F10 As Byte = (fNum >> 9) And 1
'            Dim F9 As Byte = (fNum >> 8) And 1
'            Dim F8 As Byte = (fNum >> 7) And 1

'            Dim N4 As Byte = F11
'            Dim N3 As Byte = (F11 And (F10 Or F9 Or F8)) Or ((Not F11) And F10 And F9 And F8)

'            Return (block << 2) Or (N4 << 1) Or N3
'        End Function

'#End Region

'#Region "Common parameters"

'        Private Function GetParameter(addr As RegAddr, mask As RegDataMask) As Byte
'            Return (Read(PortIndex.Port0, addr) And mask) >> GetBitShift(mask)
'        End Function

'        Public ReadOnly Property LFOEn() As Boolean
'            Get
'                Return GetParameter(RegAddr.LFOEn_LFOFreq, RegDataMask.LFOEn) = 1
'            End Get
'        End Property
'        Public ReadOnly Property LFOFreq() As Byte
'            Get
'                Return GetParameter(RegAddr.LFOEn_LFOFreq, RegDataMask.LFOFreq)
'            End Get
'        End Property
'        Public Shared ReadOnly LFOFreqTable As Byte() =
'            {108, 77, 71, 67, 62, 44, 8, 5}
'        Public ReadOnly Property LFOFreqValue() As Integer
'            Get
'                Return LFOFreqTable(LFOFreq)
'            End Get
'        End Property

'        Public ReadOnly Property TimerA() As Integer
'            Get
'                Dim tmrA_MSBs As Integer =
'                    GetParameter(RegAddr.TimerAHi, RegDataMask.TimerAMSBs)
'                Dim tmrA_LSBs As Integer =
'                    GetParameter(RegAddr.TimerALo, RegDataMask.TimerALSBs)
'                Return (tmrA_MSBs << 8) Or tmrA_LSBs
'            End Get
'        End Property
'        Public ReadOnly Property TimerAValue() As Single
'            Get
'                Return 144 * (1024 - TimerA) / (MasterClockFreq / 1000000)
'            End Get
'        End Property

'        Public ReadOnly Property TimerB() As Integer
'            Get
'                Return GetParameter(RegAddr.TimerB, RegDataMask.TimerB)
'            End Get
'        End Property
'        Public ReadOnly Property TimerBValue() As Single
'            Get
'                Return (144 * 16) * (256 - TimerB) / (MasterClockFreq / 1000000)
'            End Get
'        End Property

'        Public ReadOnly Property Ch3Mode() As Ch3ModeValue
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.Ch3Mode)
'            End Get
'        End Property
'        Public ReadOnly Property TimerBReset() As Boolean
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.TimerBReset) = 1
'            End Get
'        End Property
'        Public ReadOnly Property TimerAReset() As Boolean
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.TimerAReset) = 1
'            End Get
'        End Property
'        Public ReadOnly Property TimerBEnable() As Boolean
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.TimerBEnable) = 1
'            End Get
'        End Property
'        Public ReadOnly Property TimerAEnable() As Boolean
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.TimerAEnable) = 1
'            End Get
'        End Property
'        Public ReadOnly Property TimerBLoad() As Boolean
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.TimerBLoad) = 1
'            End Get
'        End Property
'        Public ReadOnly Property TimerALoad() As Boolean
'            Get
'                Return GetParameter(RegAddr.Ch3Mode_Reset_Enable_Load, RegDataMask.TimerALoad) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Slot() As Byte
'            Get
'                Return GetParameter(RegAddr.KeyOn, RegDataMask.Slot)
'            End Get
'        End Property

'        Public Shared ReadOnly ChTable As Byte() =
'            {0, 1, 2, 255, 3, 4, 5, 255}
'        Public ReadOnly Property Ch() As Byte
'            Get
'                Return ChTable(GetParameter(RegAddr.KeyOn, RegDataMask.Ch))
'            End Get
'        End Property


'        'Private _DAC As Byte
'        Public ReadOnly Property DAC() As Byte
'            Get
'                Return GetParameter(RegAddr.DAC, RegDataMask.DACData)
'            End Get
'        End Property

'        'Private _DACEn As Byte
'        Public ReadOnly Property DACEn() As Boolean
'            Get
'                Return GetParameter(RegAddr.DACEn, RegDataMask.DACEn) = 1
'            End Get
'        End Property

'        Private ReadOnly Ch3SpBlockMSBsAddr() As Byte = {&HAD, &HAE, &HAC, &HA2}

'        Public ReadOnly Property Ch3SpBlock(opIdx As Integer) As Integer
'            Get
'                Return GetParameter(Ch3SpBlockMSBsAddr(opIdx), RegDataMask.Ch3SpBlock)
'            End Get
'        End Property
'        Public ReadOnly Property Ch3SpMSBs(opIdx As Integer) As Integer
'            Get
'                Return GetParameter(Ch3SpBlockMSBsAddr(opIdx), RegDataMask.Ch3SpFNumMSBs)
'            End Get
'        End Property

'        Private ReadOnly Ch3SpLSBsAddr() As Byte = {&HA9, &HAA, &HA8, &HA6}

'        Public ReadOnly Property Ch3SpLSBs(opIdx As Integer) As Integer
'            Get
'                Return GetParameter(Ch3SpLSBsAddr(opIdx), RegDataMask.Ch3SpFNumLSBs)
'            End Get
'        End Property

'        Public ReadOnly Property Ch3SpFNum(opIdx As Integer) As Integer
'            Get
'                Return (Ch3SpMSBs(opIdx) << 8) Or Ch3SpLSBs(opIdx)
'            End Get
'        End Property

'#End Region

'        Public Class OPN2Channel
'            Public ReadOnly Property Chip As Chip
'            Public ReadOnly Property Num As Integer
'            Public ReadOnly Property Bank As Integer
'            Public ReadOnly Property MemOffset As Integer
'            Public ReadOnly Property Op As OPN2Op()
'            Public ReadOnly Property FeedbackBuffer As Integer()

'            Public Const OutputBitCount As Integer = 14
'            Public Const OutputLen As Integer = (1 << OutputBitCount)
'            Public Const OutputMax As Integer = (OutputLen + 1) >> 1 '(OutputLen \ 2) - 1
'            Public Const OutputMin As Integer = -OutputMax

'            Public Property Output As Integer

'            Sub New(ByRef chip As Chip, num As Integer)
'                _Chip = chip
'                _Num = num
'                _Bank = If(num < 3, 0, 1)
'                Select Case num
'                    Case 0, 3
'                        _MemOffset = 0
'                    Case 1, 4
'                        _MemOffset = 1
'                    Case 2, 5
'                        _MemOffset = 2
'                End Select

'                ReDim _Op(Op_Count - 1)
'                For i As Integer = 0 To Op_Count - 1
'                    _Op(i) = New OPN2Op(Me, i)
'                Next

'                ReDim _FeedbackBuffer(1)

'                'Enable stereo output by default
'                chip.Write(Bank, RegAddr.LR_AMS_PMS + MemOffset, chip.Read(Bank, RegAddr.LR_AMS_PMS + MemOffset) Or (RegDataMask.L Or RegDataMask.R))
'            End Sub

'            Private Function GetParameter(addr As RegAddr, mask As RegDataMask) As Byte
'                Return (Chip.Read(Bank, addr + MemOffset) And mask) >> GetBitShift(mask)
'            End Function


'            Public ReadOnly Property Block As Integer
'                Get
'                    Return GetParameter(RegAddr.Block_FNumHi, RegDataMask.Block)
'                End Get
'            End Property

'            Public ReadOnly Property FNum() As Integer
'                Get
'                    Dim MSBs As Integer = GetParameter(RegAddr.Block_FNumHi, RegDataMask.FNumMSBs)
'                    Dim LSBs As Integer = GetParameter(RegAddr.FNumLo, RegDataMask.FNumLSBs)
'                    Return (MSBs << 8) Or LSBs
'                End Get
'            End Property

'            Public ReadOnly Property FNote() As Single
'                Get
'                    Return GetFNote(Block, FNum, Chip.IntSampleClockFreq)
'                End Get
'            End Property


'            Public ReadOnly Property FB As Byte
'                Get
'                    Return GetParameter(RegAddr.FB_Con, RegDataMask.FB)
'                End Get
'            End Property

'            Public ReadOnly Property Con() As Byte
'                Get
'                    Return GetParameter(RegAddr.FB_Con, RegDataMask.Con)
'                End Get
'            End Property


'            Public ReadOnly Property L() As Boolean
'                Get
'                    Return GetParameter(RegAddr.LR_AMS_PMS, RegDataMask.L) = 1
'                End Get
'            End Property

'            Public ReadOnly Property R() As Boolean
'                Get
'                    Return GetParameter(RegAddr.LR_AMS_PMS, RegDataMask.R) = 1
'                End Get
'            End Property

'            Public ReadOnly Property AMS() As Byte
'                Get
'                    Return GetParameter(RegAddr.LR_AMS_PMS, RegDataMask.AMS)
'                End Get
'            End Property
'            Shared ReadOnly AMSTable As Single() =
'                {0, 1.4, 5.9, 11.8}
'            Public ReadOnly Property AMSValue() As Single
'                Get
'                    Return AMSTable(AMS)
'                End Get
'            End Property

'            Public ReadOnly Property PMS() As Byte
'                Get
'                    Return GetParameter(RegAddr.LR_AMS_PMS, RegDataMask.PMS)
'                End Get
'            End Property
'            Shared ReadOnly PMSTable As Single() =
'                {0, 3.4, 6.7, 10, 14, 20, 40, 80}
'            Public ReadOnly Property PMSValue() As Single
'                Get
'                    Return PMSTable(PMS)
'                End Get
'            End Property

'            Public Class OPN2Op
'                Public ReadOnly Property Channel As OPN2Channel
'                Public ReadOnly Property Chip As Chip
'                Public ReadOnly Property Num As Integer
'                Public ReadOnly Property MemOffset As Integer

'                Public ReadOnly Property IsCarrier As Boolean
'                    Get
'                        Select Case Num
'                            Case 0
'                                If Channel.Con = 7 Then
'                                    Return True
'                                End If
'                            Case 1
'                                If Channel.Con >= 4 Then
'                                    Return True
'                                End If
'                            Case 2
'                                If Channel.Con >= 6 Then
'                                    Return True
'                                End If
'                            Case 3
'                                Return True
'                        End Select
'                        Return False
'                    End Get
'                End Property

'                Public Const OutputBitCount As Integer = 14
'                Public Const OutputLen As Integer = (1 << OutputBitCount)
'                Public Const OutputMax As Integer = (OutputLen \ 2) - 1
'                Public Const OutputMin As Integer = -OutputMax
'                Public Property Output As Integer

'                Sub New(ByRef ch As OPN2Channel, num As Integer)
'                    _Channel = ch
'                    _Chip = ch.Chip
'                    _Num = num

'                    ' Fix for bit-swapped operator addressing
'                    If num = 1 Then
'                        num = 2
'                    ElseIf num = 2 Then
'                        num = 1
'                    End If
'                    _MemOffset = (num * 4) + (ch.Num Mod 3)
'                End Sub

'                ' Octave and frequency

'                Public ReadOnly Property Block As Integer
'                    Get
'                        If (Channel.Num = 2) And (Channel.Chip.Ch3Mode <> Ch3ModeValue.Normal) And (Num <> 3) Then
'                            Return Chip.Ch3SpBlock(Num)
'                        Else
'                            Return Channel.Block
'                        End If
'                    End Get
'                End Property

'                Public ReadOnly Property FNum As Integer
'                    Get
'                        If (Channel.Num = 2) And (Channel.Chip.Ch3Mode <> Ch3ModeValue.Normal) And (Num <> 3) Then
'                            Return Chip.Ch3SpFNum(Num)
'                        Else
'                            Return Channel.FNum
'                        End If
'                    End Get
'                End Property

'                Public ReadOnly Property KeyCode As Integer
'                    Get
'                        Return Chip.KeyCodeTable(Block, FNum)
'                    End Get
'                End Property

'                Public ReadOnly Property FNote() As Single
'                    Get
'                        Return Chip.FNoteTable(Chip.PG.Phase(Channel.Num, Num).PhaseInc)
'                    End Get
'                End Property

'                Private Function GetParameter(addr As RegAddr, mask As RegDataMask) As Byte
'                    Return (Chip.Read(Channel.Bank, addr + MemOffset) And mask) >> GetBitShift(mask)
'                End Function


'                Public ReadOnly Property DT() As Integer
'                    Get
'                        Return GetParameter(RegAddr.DT_Multi, RegDataMask.DT)
'                    End Get
'                End Property

'                Public ReadOnly Property DTInc() As Integer
'                    Get
'                        Dim sign As Integer =
'                            (DT And &B100) >> 2
'                        Dim mag As Integer =
'                            DT And &B11

'                        Dim value As Integer = Chip.DetunePhaseIncTable(KeyCode, mag)
'                        If sign = 1 Then value = -value

'                        Return value
'                    End Get
'                End Property

'                Public ReadOnly Property Multi() As Single
'                    Get
'                        Return GetParameter(RegAddr.DT_Multi, RegDataMask.Multi)
'                    End Get
'                End Property

'                Public ReadOnly Property TL() As Integer
'                    Get
'                        Return GetParameter(RegAddr.TL, RegDataMask.TL)
'                    End Get
'                End Property

'                Public ReadOnly Property TLtoAtten() As Integer
'                    Get
'                        Return TL << 3
'                    End Get
'                End Property

'                Public ReadOnly Property AttenIndB() As Single
'                    'For MIDI volume calculation
'                    Get
'                        Dim int As Integer =
'                            (TLtoAtten And &B1111000000) >> 6
'                        Dim frac As Integer =
'                            TLtoAtten And &B111111

'                        Dim atten As Single = int

'                        For i As Integer = 0 To 5
'                            If ((frac >> i) And 1) = 1 Then
'                                atten += 2 ^ -(i + 1)
'                            End If
'                        Next

'                        atten *= 3

'                        Return atten
'                    End Get
'                End Property

'                Public ReadOnly Property KS() As Integer
'                    Get
'                        Return GetParameter(RegAddr.KS_AR, RegDataMask.KS)
'                    End Get
'                End Property

'                Public ReadOnly Property AR() As Integer
'                    Get
'                        Return GetParameter(RegAddr.KS_AR, RegDataMask.AR)
'                    End Get
'                End Property


'                Public ReadOnly Property AM() As Integer
'                    Get
'                        Return GetParameter(RegAddr.AM_DR, RegDataMask.AM)
'                    End Get
'                End Property

'                Public ReadOnly Property DR() As Integer
'                    Get
'                        Return GetParameter(RegAddr.AM_DR, RegDataMask.DR)
'                    End Get
'                End Property


'                Public ReadOnly Property SR() As Integer
'                    Get
'                        Return GetParameter(RegAddr.SR, RegDataMask.SR)
'                    End Get
'                End Property


'                Public ReadOnly Property SL() As Integer
'                    Get
'                        Return GetParameter(RegAddr.SL_RR, RegDataMask.SL)
'                    End Get
'                End Property

'                Public ReadOnly Property SLIndB() As Single
'                    Get
'                        Return If(SL = 15, 93, SL * 3)
'                    End Get
'                End Property

'                Public ReadOnly Property SLtoAtten() As Integer
'                    Get
'                        Return If(SL = 15, EnvelopeGenerator.AttenMax, SL << 5)
'                    End Get
'                End Property

'                Public ReadOnly Property RR() As Integer
'                    Get
'                        Return GetParameter(RegAddr.SL_RR, RegDataMask.RR)
'                    End Get
'                End Property

'                Public ReadOnly Property SSG_EG() As Byte
'                    Get
'                        Return GetParameter(RegAddr.SSG_EG, RegDataMask.SSG_EG)
'                    End Get
'                End Property
'                Public ReadOnly Property SSG_EGEnabled() As Boolean
'                    Get
'                        Return GetParameter(RegAddr.SSG_EG, RegDataMask.SSG_EGEnabled) = 1
'                    End Get
'                End Property
'                Public ReadOnly Property SSG_EGAttack() As Boolean
'                    Get
'                        Return GetParameter(RegAddr.SSG_EG, RegDataMask.SSG_EGAttack) = 1
'                    End Get
'                End Property
'                Public ReadOnly Property SSG_EGAlternate() As Boolean
'                    Get
'                        Return GetParameter(RegAddr.SSG_EG, RegDataMask.SSG_EGAlternate) = 1
'                    End Get
'                End Property
'                Public ReadOnly Property SSG_EGHold() As Boolean
'                    Get
'                        Return GetParameter(RegAddr.SSG_EG, RegDataMask.SSG_EGHold) = 1
'                    End Get
'                End Property

'            End Class

'        End Class

'        Public Class PhaseGenerator
'            Private Property Chip As Chip

'            'Number of internal sample clocks per PG clock
'            Public Const ClockDivider As Integer = 1

'            ' Phase Generator clock frequency (in Hz)
'            Public ReadOnly Property ClockFreq() As Single

'            'Number of clock cycles per internal sample clock
'            Public Const ClocksPerIntSample As Single = 1 / ClockDivider

'            'Number of clock cycles per external sample clock
'            Public ReadOnly Property ClocksPerExtSample As Single

'            Public ReadOnly Property Phase As ONP2Phase(,)

'            Sub New(ByRef chip As Chip)
'                _Chip = chip
'                _ClockFreq = chip.FMClockFreq / ClockDivider
'                _ClocksPerExtSample = (ClockFreq / Sample_Clock_Divider) / chip.ExtSampleClockFreq

'                ReDim Phase(Channel_Count - 1, Op_Count - 1)
'                For chIdx As Integer = 0 To Channel_Count - 1
'                    With chip.Channel(chIdx)
'                        For opIdx As Integer = 0 To Op_Count - 1
'                            Phase(chIdx, opIdx) = New ONP2Phase(.Op(opIdx))
'                        Next
'                    End With
'                Next

'            End Sub

'            Public Const PhaseBitCount As Integer = 10
'            Public Const PhaseLen As Integer = (1 << PhaseBitCount)
'            Public Const PhaseMax As Integer = PhaseLen - 1

'            Public Const CounterBitCount As Integer = 20
'            Public Const CounterLen As Integer = (1 << CounterBitCount)
'            Public Const CounterMax As Integer = CounterLen - 1

'            Public Sub Advance()
'                For chIdx As Integer = 0 To Channel_Count - 1
'                    For opIdx As Integer = 0 To Op_Count - 1
'                        Phase(chIdx, opIdx).Advance()
'                    Next
'                Next
'            End Sub

'            Sub RequestPhaseIncUpdate(chIdx As Integer, opIdx As Integer)
'                Phase(chIdx, opIdx).RequestPhaseIncUpdate()
'            End Sub

'            Sub RequestPhaseIncUpdate(chIdx As Integer)
'                For opIdx As Integer = 0 To Op_Count - 1
'                    Phase(chIdx, opIdx).RequestPhaseIncUpdate()
'                Next
'            End Sub

'            Public Class ONP2Phase
'                Public ReadOnly Property Op As OPN2Channel.OPN2Op
'                Public ReadOnly Property Channel As OPN2Channel
'                Public ReadOnly Property Chip As Chip

'                Sub New(ByRef op As OPN2Channel.OPN2Op)
'                    _Op = op
'                    _Channel = op.Channel
'                    _Chip = op.Channel.Chip
'                End Sub

'                Public ReadOnly Property Counter As Integer

'                Public ReadOnly Property Output As Integer
'                    Get
'                        Return (Counter >> 10) And PhaseMax
'                    End Get
'                End Property

'                Private _PhaseIncUpdateRequested As Boolean
'                Sub RequestPhaseIncUpdate()
'                    _PhaseIncUpdateRequested = True
'                End Sub

'                Private _PhaseInc As Integer
'                Public ReadOnly Property PhaseInc As Integer
'                    Get
'                        If _PhaseIncUpdateRequested Then
'                            '_PhaseInc = CalculatePhaseInc(Op)
'                            _PhaseInc = CalculatePhaseInc()
'                        End If
'                        _PhaseIncUpdateRequested = False
'                        Return _PhaseInc
'                        '_PhaseInc = CalculatePhaseInc()
'                        'Return _PhaseInc
'                    End Get
'                End Property

'                Private Function CalculatePhaseInc() As Integer
'                    With Op
'                        'Get FNum
'                        Dim value As Integer = .FNum

'                        'TODO: Add in LFO phase modulation

'                        'Shift by block
'                        value <<= .Block
'                        value >>= 1

'                        'Add detune
'                        value += .DTInc

'                        'Emulate overflow for detune bug
'                        Const OverflowAdjustment As Integer = (1 << 17) - 1
'                        If value < 0 Then
'                            value += OverflowAdjustment
'                        End If

'                        'Multiply
'                        If .Multi = 0 Then
'                            value >>= 1
'                        Else
'                            value *= .Multi
'                        End If

'                        Return value And PhaseIncMax
'                    End With
'                End Function

'                Sub Advance()
'                    _Counter += PhaseInc
'                    If _Counter > CounterMax Then
'                        _Counter -= CounterMax
'                    End If
'                End Sub

'                Sub Reset()
'                    _Counter = 0
'                End Sub

'            End Class

'        End Class

'        Public Class EnvelopeGenerator
'            Private Property Chip As Chip

'            'Number of internal sample clocks per EG clock
'            Public Const ClockDivider As Integer = 3

'            ' Envelope Generator clock frequency in Hz
'            Public ReadOnly Property ClockFreq() As Single

'            'Number of clock cycles per internal sample clock
'            Public Const ClocksPerIntSample As Single = 1 / ClockDivider

'            'Number of clock cycles per external sample clock
'            Public ReadOnly Property ClocksPerExtSample As Single

'            Public ReadOnly Property Envelope As OPN2Envelope(,)

'            Enum Stage As Byte
'                A = 4
'                D = 3
'                S = 2
'                R = 1
'                Off = 0
'            End Enum

'            Sub New(ByRef chip As Chip)
'                _Chip = chip
'                _ClockFreq = chip.FMClockFreq / ClockDivider
'                _ClocksPerExtSample = (ClockFreq / Sample_Clock_Divider) / chip.ExtSampleClockFreq

'                BuildRateShiftTable()
'                BuildKeyScaleRateTable()

'                ReDim Envelope(Channel_Count - 1, Op_Count - 1)
'                For chIdx As Integer = 0 To Channel_Count - 1
'                    With chip.Channel(chIdx)
'                        For opIdx As Integer = 0 To Op_Count - 1
'                            Envelope(chIdx, opIdx) = New OPN2Envelope(.Op(opIdx))
'                        Next
'                    End With
'                Next

'            End Sub

'            Public ReadOnly Property Counter As Integer
'            Const CounterBitCount As Integer = 16
'            Const CounterLen As Integer = 1 << CounterBitCount
'            Const CounterMax As Integer = CounterLen - 1

'            Public ReadOnly Property RateStep As Integer
'                Get
'                    Return Counter And 7
'                End Get
'            End Property

'            Sub Advance()
'                _Counter += 1
'                If Counter > CounterMax Then
'                    _Counter -= CounterMax
'                End If

'                For chIdx As Integer = 0 To Channel_Count - 1
'                    For opIdx As Integer = 0 To Op_Count - 1

'                        Dim env As OPN2Envelope =
'                            Envelope(chIdx, opIdx)

'                        UpdateRate(env)

'                        Dim rs As Integer = RateShiftTable(env.Rate)
'                        Dim mask As Integer = (1 << rs) - 1

'                        If (Counter And mask) = mask Then
'                            UpdateAtten(env)
'                            UpdateStage(env)
'                        End If

'                    Next
'                Next

'            End Sub

'            Sub KeyOnOff(chIdx As Integer, opMask As Byte)
'                For opIdx As Integer = 0 To Op_Count - 1
'                    If ((opMask >> opIdx) And 1) = 1 Then
'                        KeyOn(Envelope(chIdx, opIdx))
'                    Else
'                        KeyOff(Envelope(chIdx, opIdx))
'                    End If
'                Next
'            End Sub

'            Sub KeyOn(ByRef env As OPN2Envelope)
'                With env
'                    If .Key = False Then
'                        Chip.PG.Phase(.Op.Channel.Num, .Op.Num).Reset()

'                        .Stage = Stage.A
'                        UpdateRate(env)
'                        If .Rate >= 62 Then
'                            .Atten = 0
'                        End If
'                        .Key = True
'                    End If
'                End With
'            End Sub

'            Sub KeyOff(ByRef env As OPN2Envelope)
'                With env
'                    If .Key = True Then
'                        If .Stage > Stage.R Then
'                            .Stage = Stage.R
'                            UpdateRate(env)
'                        End If
'                        .Key = False
'                    End If
'                End With
'            End Sub

'            Sub UpdateRate(ByRef env As OPN2Envelope)
'                Dim value As Byte

'                With env.Op
'                    Select Case env.Stage
'                        Case Stage.A
'                            value = .AR
'                        Case Stage.D
'                            value = .DR
'                        Case Stage.S
'                            value = .SR
'                        Case Stage.R
'                            value = (.RR * 2) + 1
'                        Case Else 'Stage.Off
'                            value = 0
'                    End Select

'                    Dim ksr As Byte =
'                        KeyScaleRateTable(.KeyCode, .KS)
'                    value = If(value = 0, 0, (2 * value) + ksr)
'                    value = Math.Min(value, RateMax)
'                End With

'                env.Rate = value
'            End Sub

'            Sub UpdateAtten(ByRef env As OPN2Envelope)
'                With env
'                    If .Stage = Stage.Off Then Exit Sub

'                    Dim newAtten As Integer = .Atten

'                    .AttenInc = AttenIncTable(.Rate, RateStep)

'                    Select Case .Stage
'                        Case Stage.A
'                            If .Rate < 62 Then
'                                newAtten += (-newAtten * .AttenInc) >> 4
'                            End If
'                        Case Else
'                            newAtten += .AttenInc
'                    End Select

'                    newAtten = Math.Clamp(newAtten, 0, AttenMax)

'                    .Atten = newAtten
'                End With
'            End Sub

'            Public Sub UpdateStage(ByRef env As OPN2Envelope)
'                With env
'                    'If .Stage = Stage.Off Then Exit Sub

'                    Select Case .Stage
'                        Case Stage.A
'                            If .Atten <= 0 Then
'                                .Atten = 0
'                                If .Op.SL = 0 Then
'                                    .Stage = Stage.S
'                                    UpdateRate(env)
'                                Else
'                                    .Stage = Stage.D
'                                    UpdateRate(env)
'                                End If
'                            End If
'                        Case Stage.D
'                            If .Atten >= .Op.SLtoAtten Then
'                                .Stage = Stage.S
'                                UpdateRate(env)
'                            End If
'                            'Case Else
'                            '    If .Atten >= AttenMax Then
'                            '        .Stage = Stage.Off
'                            '        UpdateRate(env)
'                            '    End If
'                    End Select
'                End With
'            End Sub

'            Public Const AttenBitCount As Integer = 10
'            Public Const AttenLen As Integer = 1 << AttenBitCount
'            Public Const AttenMax As Integer = (1 << AttenBitCount) - 1
'            Public Const AttenBitWeight As Single = 48 / AttenLen

'            Public Shared ReadOnly AttenIncTable As Integer(,) =
'                {{0, 0, 0, 0, 0, 0, 0, 0}, {0, 0, 0, 0, 0, 0, 0, 0}, {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 0, 1, 0, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 0, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {0, 1, 0, 1, 0, 1, 0, 1}, {0, 1, 0, 1, 1, 1, 0, 1}, {0, 1, 1, 1, 0, 1, 1, 1}, {0, 1, 1, 1, 1, 1, 1, 1},
'                 {1, 1, 1, 1, 1, 1, 1, 1}, {1, 1, 1, 2, 1, 1, 1, 2}, {1, 2, 1, 2, 1, 2, 1, 2}, {1, 2, 2, 2, 1, 2, 2, 2},
'                 {2, 2, 2, 2, 2, 2, 2, 2}, {2, 2, 2, 4, 2, 2, 2, 4}, {2, 4, 2, 4, 2, 4, 2, 4}, {2, 4, 4, 4, 2, 4, 4, 4},
'                 {4, 4, 4, 4, 4, 4, 4, 4}, {4, 4, 4, 8, 4, 4, 4, 8}, {4, 8, 4, 8, 4, 8, 4, 8}, {4, 8, 8, 8, 4, 8, 8, 8},
'                 {8, 8, 8, 8, 8, 8, 8, 8}, {8, 8, 8, 8, 8, 8, 8, 8}, {8, 8, 8, 8, 8, 8, 8, 8}, {8, 8, 8, 8, 8, 8, 8, 8}}

'            Public Const RateBitCount As Integer = 6
'            Public Const RateMax As Integer = (1 << RateBitCount) - 1

'            Public ReadOnly RateShiftTable(RateMax) As Single

'            Sub BuildRateShiftTable()
'                For i As Integer = 0 To RateMax
'                    RateShiftTable(i) = Math.Max(16 - (i \ 4) - 5, 0)
'                Next
'            End Sub

'            Public Const KeyCodeBitCount As Integer = 5
'            Public Const KeyCodeMax As Integer = (1 << KeyCodeBitCount) - 1

'            Public ReadOnly KeyScaleRateTable(KeyCodeMax, 3) As Integer

'            Sub BuildKeyScaleRateTable()
'                For kc As Integer = 0 To KeyCodeMax
'                    For ks As Integer = 0 To 3
'                        KeyScaleRateTable(kc, ks) = kc >> (3 - ks)
'                    Next
'                Next
'            End Sub

'            Public Class OPN2Envelope
'                Public ReadOnly Property Op As OPN2Channel.OPN2Op
'                Public ReadOnly Property Channel As OPN2Channel
'                Public ReadOnly Property Chip As Chip

'                Sub New(ByRef op As OPN2Channel.OPN2Op)
'                    _Op = op
'                    _Channel = op.Channel
'                    _Chip = op.Channel.Chip
'                    Reset()
'                End Sub

'                Public Property Stage As Stage
'                Public Property Rate As Integer
'                Public Property AttenInc As Integer
'                Public Property Atten As Integer
'                Public Property Key As Boolean

'                Public ReadOnly Property Output As Integer
'                    Get
'                        Return Atten + Op.TLtoAtten
'                    End Get
'                End Property

'                Sub Reset()
'                    _Stage = Stage.Off
'                    _Atten = AttenMax
'                    _AttenInc = 0
'                End Sub

'            End Class

'        End Class

'        Public Class OperatorUnit
'            Private Property Chip As Chip

'            Sub New(ByRef chip As Chip)
'                _Chip = chip
'            End Sub

'            Public Sub CalculateOpOutput(ByRef op As OPN2Channel.OPN2Op)

'                With op

'                    'Calculate operator output
'                    Dim ph As Integer = Chip.PG.Phase(.Channel.Num, .Num).Output
'                    Dim at As Integer = Chip.EG.Envelope(.Channel.Num, .Num).Output

'                    'Calculate phase modulation
'                    Dim pm As Integer = CalculatePhaseModulation(op)

'                    If Chip.HighQuality Then
'                        Dim result As Integer =
'                            Chip.HQOutputTable(ph + pm, at) * OPN2Channel.OPN2Op.OutputMax
'                        .Output = result
'                    Else
'                        Dim modPhase As UInteger =
'                            ph + pm

'                        Dim quarterPhase As UInteger =
'                            modPhase And 255

'                        Dim xFlip As Boolean =
'                            ((modPhase >> 8) And 1) = 1
'                        If xFlip Then
'                            quarterPhase = 255 - quarterPhase
'                        End If

'                        Dim atten As Integer =
'                            Chip.SinTable(quarterPhase) + (at << 2)
'                        If atten > LinPowTableOutputLen Then
'                            atten = LinPowTableOutputLen
'                        End If

'                        Dim linPow As Integer = Chip.LinPowTable(atten)

'                        Dim yFlip As Boolean =
'                            ((modPhase >> 9) And 1) = 1
'                        If yFlip Then
'                            linPow = -linPow
'                        End If

'                        .Output = linPow

'                    End If

'                End With

'            End Sub

'            Shared Function CalculatePhaseModulation(ByRef op As OPN2Channel.OPN2Op) As Integer
'                Dim pm As Integer = 0

'                With op.Channel

'                    If op.Num = 0 Then
'                        'Calculate feedback for slot 1
'                        If .FB > 0 Then
'                            pm = .FeedbackBuffer(0) + .FeedbackBuffer(1)
'                            If pm <> 0 Then
'                                pm \= 2 ^ (10 - .FB)
'                            End If
'                        End If
'                    Else
'                        'Calculate modulation from other slots
'                        Select Case op.Num
'                            Case 1
'                                Select Case .Con
'                                    Case 0, 3, 4, 5, 6
'                                        pm = .Op(0).Output
'                                End Select
'                            Case 2
'                                Select Case .Con
'                                    Case 0, 2
'                                        pm = .Op(1).Output
'                                    Case 1
'                                        pm = .Op(0).Output + .Op(1).Output
'                                    Case 5
'                                        pm = .Op(0).Output
'                                End Select
'                            Case 3
'                                Select Case .Con
'                                    Case 0, 1, 4
'                                        pm = .Op(2).Output
'                                    Case 2
'                                        pm = .Op(0).Output + .Op(2).Output
'                                    Case 3
'                                        pm = .Op(1).Output + .Op(2).Output
'                                    Case 5
'                                        pm = .Op(0).Output
'                                End Select
'                        End Select

'                        pm \= 2

'                    End If

'                End With

'                Return pm And PhaseGenerator.PhaseMax
'            End Function

'        End Class

'        Public Class LFOGenerator
'            'Private PhaseCounter As Single
'            'Const CounterMax As Single = &HFFFFF
'            'Private Output As Single
'        End Class

'        Public Class DACUnit
'            Private Property Chip As Chip
'            Public ReadOnly Property Output As Single()

'            Public ReadOnly Property Resample As Boolean
'            Public ReadOnly Property IntClocksPerExtClockIntegral As Integer
'            Public ReadOnly Property IntClocksPerExtClockFractional As Single
'            Public ReadOnly Property ResampleRatio As Single
'            Public ReadOnly Property PrevSample As Single()
'            Public ReadOnly Property Sample As Single()

'            Public ReadOnly Property Filter As Boolean
'            Public ReadOnly Property FilterAlpha As Double
'            Public ReadOnly Property FilterAlpha_i As Double

'            Public Const ChLvlMax As Single = 1 / Channel_Count

'            Public Const OutputBitCount As Integer = 9
'            Public Const OutputLen As Integer = (1 << OutputBitCount)
'            Public Const OutputMax As Integer = (OutputLen \ 2) - 1
'            Public Const OutputMin As Integer = -OutputMax

'            Sub New(ByRef chip As Chip, resample As Boolean, filter As Boolean, cutoff As Single)
'                _Chip = chip
'                ReDim _Output(1)

'                'For resampling
'                _Resample = resample
'                IntClocksPerExtClockIntegral = Math.Floor(chip.IntClocksPerExtClock)
'                IntClocksPerExtClockFractional = chip.IntClocksPerExtClock - IntClocksPerExtClockIntegral
'                ResampleRatio = chip.ExtSampleClockFreq / chip.IntSampleClockFreq

'                ReDim PrevSample(1)
'                ReDim Sample(1)

'                'For filtering
'                _Filter = filter
'                If filter Then
'                    FilterAlpha = Resampler.GetFirstOrderIIRLowPassFilterAlpha(chip.IntSampleClockFreq, cutoff)
'                    'FilterAlpha = Resampler.GetSinglePoleIIRFilterAlpha(chip.IntSampleClockFreq, cutoff)
'                End If

'            End Sub

'            Public Sub GenerateSamples(ByRef samples As Single(,))
'                Dim outLen As Integer = samples.GetUpperBound(0) + 1
'                Dim rawLen As Double = outLen * Chip.IntClocksPerExtClock
'                Dim rawLenInt As Integer = Math.Floor(rawLen)

'                Static rawLenFrac As Double = 0
'                rawLenFrac += rawLen - rawLenInt

'                If rawLenFrac >= 1 Then
'                    rawLenInt += Math.Floor(rawLenFrac)
'                    rawLenFrac -= Math.Floor(rawLenFrac)
'                End If

'                CalculateOutput()

'                Dim rawsamples(rawLenInt - 1, 1) As Single

'                For i As Integer = 0 To rawsamples.GetUpperBound(0)
'                    For j As Integer = 0 To 1
'                        'If Output(j) <> 0 Then
'                        '    Dim foo As Integer = 0
'                        'End If
'                        rawsamples(i, j) = Output(j)
'                    Next
'                    Chip.Advance()
'                Next

'                Dim upsample As Boolean =
'                    rawLen < outLen

'                Static prevsample As Single() = {0, 0}

'                If upsample Then
'                    'Resample before applying low pass filter
'                    Resampler.ApplyCubicResample(rawsamples, samples)

'                    'Low pass filter
'                    Resampler.ApplyFirstOrderIIRLowPassFilter(samples, prevsample, FilterAlpha)
'                    'Resampler.ApplySinglePoleIIRFilter(samples, prevsample, FilterAlpha)
'                Else
'                    'Low pass filter
'                    Resampler.ApplyFirstOrderIIRLowPassFilter(rawsamples, prevsample, FilterAlpha)
'                    'Resampler.ApplySinglePoleIIRFilter(samples, prevsample, FilterAlpha)

'                    'Resample after applying low pass filter
'                    Resampler.ApplyCubicResample(rawsamples, samples)
'                End If

'            End Sub

'            Public Sub CalculateOutput()
'                Dim out(1) As Single

'                'Accumulate output from each channel (NOTE: real chip uses multiplexed output)
'                For Each ch As OPN2Channel In Chip.Channel
'                    CalculateChannelOutput(ch)
'                    With ch
'                        If .L Then
'                            out(0) += .Output + If(.Output >= 0, 4, -3)
'                        Else
'                            out(0) += If(.Output >= 0, 4, -4)
'                        End If
'                        If .R Then
'                            out(1) += .Output + If(.Output >= 0, 4, -3)
'                        Else
'                            out(1) += If(.Output >= 0, 4, -4)
'                        End If
'                    End With
'                Next

'                For i As Integer = 0 To 1
'                    If Chip.HighQuality Then
'                        'Normalize output in HQ
'                        out(i) /= OutputMax
'                    Else
'                        ''Normalize output like "real" DAC
'                        'out(i) \= 128
'                        'out(i) /= 255
'                        out(i) /= OutputMax
'                    End If

'                    _Output(i) = out(i)
'                Next

'            End Sub

'            Private Sub CalculateChannelOutput(ByRef ch As OPN2Channel)
'                Dim out As Integer = 0

'                With ch
'                    If (.Num = 5) And (Chip.DACEn) Then
'                        Dim signBit As Byte =
'                            (Chip.DAC >> 7) And 1
'                        Dim magnitudeBits As Byte =
'                            Chip.DAC And 127
'                        out = If(signBit = 1, -magnitudeBits, 127 - magnitudeBits)
'                        out *= 2
'                    Else
'                        If .Con = 7 Then
'                            out += .Op(0).Output \ 32
'                        End If
'                        If .Con >= 4 Then
'                            out += .Op(1).Output \ 32
'                        End If
'                        If .Con >= 5 Then
'                            out += .Op(2).Output \ 32
'                        End If
'                        out += .Op(3).Output \ 32
'                    End If

'                    .FeedbackBuffer(0) = .FeedbackBuffer(1)
'                    .FeedbackBuffer(1) = .Op(0).Output

'                    'Clamp output
'                    out = Math.Clamp(out, OutputMin, OutputMax)

'                    .Output = out

'                End With

'            End Sub

'        End Class

'#Region "Events"

'        ' ********************************
'        ' *** Common events
'        Public Event Test1Changed(value As Byte)
'        Public Event LFOEnLFOFreqChanged(LFOEn As Boolean, LFOFreq As Integer)
'        Public Event TimerAHiChanged(value As Byte)
'        Public Event TimerALoChanged(value As Byte)
'        Public Event TimerAChanged(value As Single)
'        Public Event TimerBChanged(value As Single)
'        Public Event Ch3ModeResetEnableLoadChanged(ch3mode As Ch3ModeValue, resetB As Boolean, resetA As Boolean, enableB As Boolean, enableA As Boolean, loadB As Boolean, loadA As Boolean)
'        Public Event SlotChChanged(slot As Integer, ch As Integer)
'        Public Event DACChanged(value As Byte)
'        Public Event DACEnChanged(DACEn As Boolean)
'        Public Event Test2Changed(value As Byte)

'        ' ********************************
'        ' *** Channel/Operator events
'        Public Event DTMULTIChanged(chIdx As Integer, opIdx As Integer, dt As Integer, multi As Integer)
'        Public Event TLChanged(chIdx As Integer, opIdx As Integer, tl As Integer)
'        Public Event KSARChanged(chIdx As Integer, opIdx As Integer, ks As Integer, ar As Integer)
'        Public Event AMDRChanged(chIdx As Integer, opIdx As Integer, am As Integer, d1r As Integer)
'        Public Event SRChanged(chIdx As Integer, opIdx As Integer, d2r As Integer)
'        Public Event SLRRChanged(chIdx As Integer, opIdx As Integer, d1l As Integer, rr As Integer)
'        Public Event SSG_EGChanged(chIdx As Integer, opIdx As Integer, enabled As Boolean, attack As Boolean, alternate As Boolean, hold As Boolean)

'        ' ********************************
'        ' *** Channel only events
'        Public Event BlockFNumHiChanged(chIdx As Integer)
'        Public Event FNumLoChanged(chIdx As Integer)
'        Public Event FNumChanged(chIdx As Integer, block As Integer, fNum As Integer, fNote As Single)
'        Public Event Ch3SpBlockFNumHiChanged(chIdx As Integer)
'        Public Event Ch3SpFNumLoChanged(chIdx As Integer)
'        Public Event Ch3SpFNumChanged(opIdx As Integer, block As Integer, fNum As Integer, fNote As Single)
'        Public Event FBConChanged(chIdx As Integer, FB As Integer, con As Integer)
'        Public Event LRAMSPMSChanged(chIdx As Integer, L As Boolean, R As Boolean, AMS As Integer, PMS As Integer)

'        Private Sub UpdateParameters(port As PortIndex, addr As RegAddr, value As Byte)
'            Select Case addr And &HF0
'                Case &H20                                           'Common registers
'                    UpdateCommonParameters(addr, value)
'                Case &H30 To &H90                                   'Operator registers
'                    UpdateOperatorParameters(port, addr, value)
'                Case &HA0 To &HB0                                   'Channel registers
'                    Select Case addr And &HF
'                        Case &H0 To &H6
'                            UpdateChannelParameters(port, addr, value)
'                        Case &H8 To &HE
'                            UpdateCh3SpecialParameters(addr, value)
'                    End Select
'            End Select
'        End Sub

'        Private Sub UpdateCommonParameters(addr As RegAddr, value As Byte)

'            Select Case addr
'                Case RegAddr.Test1
'                    RaiseEvent Test1Changed(value)

'                Case RegAddr.LFOEn_LFOFreq
'                    RaiseEvent LFOEnLFOFreqChanged(LFOEn, LFOFreq)

'                Case RegAddr.TimerAHi
'                    'RaiseEvent TimerAMSBsChanged()

'                Case RegAddr.TimerALo
'                    RaiseEvent TimerAChanged(TimerAValue)

'                Case RegAddr.TimerB
'                    RaiseEvent TimerBChanged(TimerBValue)

'                Case RegAddr.Ch3Mode_Reset_Enable_Load
'                    RaiseEvent Ch3ModeResetEnableLoadChanged(Ch3Mode, TimerBReset, TimerAReset, TimerBEnable, TimerAEnable, TimerBLoad, TimerALoad)

'                Case RegAddr.KeyOn
'                    Dim chIdx As Byte = Ch

'                    ' Exit on illegal Ch value
'                    If chIdx = 255 Then
'                        Exit Sub
'                    End If

'                    ' Update KeyOn
'                    EG.KeyOnOff(chIdx, Slot)

'                    RaiseEvent SlotChChanged(Slot, Ch)

'                Case RegAddr.DAC
'                    RaiseEvent DACChanged(DAC)

'                Case RegAddr.DACEn
'                    RaiseEvent DACEnChanged(DACEn)

'                Case RegAddr.Test2
'                    RaiseEvent Test2Changed(value)

'            End Select

'        End Sub

'        Private Sub UpdateOperatorParameters(port As PortIndex, addr As RegAddr, value As Byte)
'            ' Get channel number
'            Dim chIdx As Byte =
'                addr And 3
'            chIdx += (port * 3)

'            ' Get bitswapped operator number
'            Dim opIdx As Byte =
'                ((addr >> 3) And 1) Or ((addr >> 1) And 2)

'            With Channel(chIdx).Op(opIdx)
'                Select Case (addr And &HF0)
'                    Case RegAddr.DT_Multi
'                        PG.RequestPhaseIncUpdate(chIdx, opIdx)
'                        RaiseEvent DTMULTIChanged(chIdx, opIdx, .DT, .Multi)

'                    Case RegAddr.TL
'                        RaiseEvent TLChanged(chIdx, opIdx, .TL)

'                    Case RegAddr.KS_AR
'                        RaiseEvent KSARChanged(chIdx, opIdx, .KS, .AR)

'                    Case RegAddr.AM_DR
'                        RaiseEvent AMDRChanged(chIdx, opIdx, .AM, .DR)

'                    Case RegAddr.SR
'                        RaiseEvent SRChanged(chIdx, opIdx, .SR)

'                    Case RegAddr.SL_RR
'                        RaiseEvent SLRRChanged(chIdx, opIdx, .SL, .SR)

'                    Case RegAddr.SSG_EG
'                        RaiseEvent SSG_EGChanged(chIdx, opIdx, .SSG_EGEnabled, .SSG_EGAttack, .SSG_EGAlternate, .SSG_EGHold)
'                End Select

'            End With

'        End Sub

'        Private Sub UpdateChannelParameters(port As PortIndex, addr As RegAddr, value As Byte)
'            ' Get channel number
'            Dim chIdx As Byte =
'                addr And 3
'            chIdx += (port * 3)

'            With Channel(chIdx)

'                Select Case addr And &HFC

'                    Case RegAddr.FNumLo
'                        PG.RequestPhaseIncUpdate(chIdx)
'                        RaiseEvent FNumChanged(chIdx, .Block, .FNum, .FNote)

'                    Case RegAddr.Block_FNumHi
'                        ' Delay writes
'                        _delayedWriteValues(addr) = value

'                        RaiseEvent BlockFNumHiChanged(chIdx)

'                    Case RegAddr.FB_Con
'                        RaiseEvent FBConChanged(chIdx, .FB, .Con)

'                    Case RegAddr.LR_AMS_PMS
'                        RaiseEvent LRAMSPMSChanged(chIdx, .L, .R, .AMSValue, .PMSValue)

'                End Select

'            End With

'        End Sub

'        Private Sub UpdateCh3SpecialParameters(addr As RegAddr, value As Byte)
'            ' Channel number (always 3)
'            Dim chIdx As Byte = 2

'            Dim opIdx As Byte
'            Select Case addr And 3
'                Case 1
'                    opIdx = 0
'                Case 2
'                    opIdx = 1
'                Case 0
'                    opIdx = 2
'            End Select

'            Select Case addr And &HFC
'                Case RegAddr.Ch3SpFNumLo
'                    PG.RequestPhaseIncUpdate(chIdx)
'                    RaiseEvent Ch3SpFNumChanged(opIdx, Ch3SpBlock(opIdx), Ch3SpFNum(opIdx), Ch3SpFNum(opIdx))
'                Case RegAddr.Ch3SpBlock_FNumHi
'                    ' Delay writes
'                    _delayedWriteValues(addr) = value
'                    RaiseEvent Ch3SpBlockFNumHiChanged(opIdx)
'            End Select

'        End Sub

'#End Region

'    End Class

'    'Public Structure Voice
'    '    Public ReadOnly Index As Long

'    '    Private _VoiceName As String
'    '    Public Property VoiceName As String
'    '        Get
'    '            Return _VoiceName
'    '        End Get
'    '        Set(value As String)
'    '            _VoiceName = value.PadRight(10).Substring(0, 10)
'    '        End Set
'    '    End Property

'    '    Public ReadOnly LFOFreq As Integer

'    '    Public ReadOnly FB As Integer
'    '    Public ReadOnly Con As Integer
'    '    Public ReadOnly AMS As Integer
'    '    Public ReadOnly PMS As Integer

'    '    Public ReadOnly DT As Integer()
'    '    Public ReadOnly Multi As Integer()
'    '    Public ReadOnly TL As Integer()
'    '    Public ReadOnly IsCarrier As Boolean?()
'    '    Public ReadOnly KS As Integer()
'    '    Public ReadOnly AR As Integer()
'    '    Public ReadOnly AM As Integer()
'    '    Public ReadOnly DR As Integer()
'    '    Public ReadOnly SR As Integer()
'    '    Public ReadOnly SL As Integer()
'    '    Public ReadOnly RR As Integer()
'    '    Public ReadOnly SSG_EG As Integer()

'    '    Public ReadOnly IsCh3Sp As Boolean?
'    '    Public ReadOnly Ch3SpBlock As Integer()
'    '    Public ReadOnly Ch3SpFNum As Integer()
'    '    Public ReadOnly Ch3SpHz As Integer()

'    '    Sub New(index As Long)
'    '        Me.Index = index
'    '        VoiceName = "YM2612"

'    '        LFOFreq = 0

'    '        FB = 0
'    '        Con = 0
'    '        AMS = 0
'    '        PMS = 0

'    '        ReDim DT(Op_Count - 1)
'    '        ReDim Multi(Op_Count - 1)
'    '        ReDim TL(Op_Count - 1)
'    '        ReDim IsCarrier(Op_Count - 1)
'    '        ReDim KS(Op_Count - 1)
'    '        ReDim AR(Op_Count - 1)
'    '        ReDim AM(Op_Count - 1)
'    '        ReDim DR(Op_Count - 1)
'    '        ReDim SR(Op_Count - 1)
'    '        ReDim SL(Op_Count - 1)
'    '        ReDim RR(Op_Count - 1)
'    '        ReDim SSG_EG(Op_Count - 1)

'    '        ReDim Ch3SpBlock(Op_Count - 1)
'    '        ReDim Ch3SpFNum(Op_Count - 1)
'    '        ReDim Ch3SpHz(Op_Count - 1)
'    '    End Sub

'    '    Sub New(index As Long, ByRef ch As OPN2Channel)
'    '        Me.New(index)

'    '        LFOFreq = ch.Chip.LFOFreq

'    '        With ch
'    '            FB = .FB
'    '            Con = .Con
'    '            AMS = .AMSValue
'    '            PMS = .PMSValue
'    '        End With

'    '        For i As Integer = 0 To 3
'    '            With ch.Op(i)
'    '                IsCarrier(i) = .IsCarrier
'    '                DT(i) = .DT
'    '                Multi(i) = .Multi
'    '                TL(i) = .TL
'    '                KS(i) = .KS
'    '                AR(i) = .AR
'    '                AM(i) = .AM
'    '                DR(i) = .DR
'    '                SR(i) = .SR
'    '                SL(i) = .SL
'    '                RR(i) = .RR
'    '                SSG_EG(i) = .SSG_EG
'    '            End With
'    '        Next

'    '        If (ch.Num = ChannelIndex.Ch3) And (ch.Chip.Ch3Mode = Ch3SpModeValue.Special) Then
'    '            IsCh3Sp = True
'    '            For i As Integer = 0 To 3
'    '                With ch.Op(i)
'    '                    Ch3SpBlock(i) = .Block
'    '                    Ch3SpFNum(i) = .FNum
'    '                    Ch3SpHz(i) = (.FNum * Math.Pow(2, .Block - 1) * ch.Chip.MasterClockFreq) / (144 * Math.Pow(2, 20))
'    '                End With
'    '            Next
'    '        Else
'    '            IsCh3Sp = False
'    '        End If

'    '    End Sub
'    '    Public Overrides Function GetHashCode() As Integer
'    '        Return HashCode.Combine(LFOFreq, FB, Con, AMS, PMS)
'    '    End Function

'    '    Public Overrides Function Equals(obj As Object) As Boolean
'    '        If obj Is Nothing Then
'    '            Return False
'    '        End If

'    '        If [GetType]() <> obj.GetType() Then
'    '            Return False
'    '        End If

'    '        Dim b As Voice = obj

'    '        If LFOFreq <> b.LFOFreq OrElse
'    '            FB <> b.FB OrElse
'    '            Con <> b.Con OrElse
'    '            AMS <> b.AMS OrElse
'    '            PMS <> b.PMS OrElse
'    '            IsCh3Sp <> b.IsCh3Sp Then Return False

'    '        'HACK: Return false for uninitialized operators
'    '        'TODO: FIND OUT WHY THIS IS HAPPENING
'    '        If DT Is Nothing OrElse b.DT Is Nothing Then
'    '            Return False
'    '        End If

'    '        For i As Integer = 0 To 3
'    '            If DT?(i) <> b.DT(i) OrElse
'    '                Multi?(i) <> b.Multi(i) OrElse
'    '                TL?(i) <> b.TL(i) AndAlso (Not IsCarrier(i)) OrElse
'    '                KS?(i) <> b.KS(i) OrElse
'    '                AR?(i) <> b.AR(i) OrElse
'    '                AM?(i) <> b.AM(i) OrElse
'    '                DR?(i) <> b.DR(i) OrElse
'    '                SR?(i) <> b.SR(i) OrElse
'    '                SL?(i) <> b.SL(i) OrElse
'    '                RR?(i) <> b.RR(i) Then Return False
'    '            'Removed comparison for SSG-EG because it causes too many duplicates
'    '            'RR?(i) <> b.RR(i) OrElse
'    '            'SSG_EG?(i) <> b.SSG_EG(i) Then Return False
'    '        Next

'    '        Return True
'    '    End Function
'    '    Overloads Shared Operator =(a As Voice, b As Voice)
'    '        Return Equals(a, b)
'    '    End Operator
'    '    Overloads Shared Operator <>(a As Voice, b As Voice)
'    '        Return Not Equals(a, b)
'    '    End Operator
'    '    Public Overrides Function ToString() As String
'    '        Dim sb As New Text.StringBuilder
'    '        Dim sep As Char = ","
'    '        With sb
'    '            If IsCh3Sp Then
'    '                .AppendLine("CHANNEL 3 SPECIAL")
'    '            End If
'    '            .AppendLine("Global parameters:")
'    '            .AppendLine($"LFO{sep}FB{sep}CON{sep}AMS{sep}PMS")
'    '            .AppendLine($"{LFOFreq}{sep}{FB}{sep}{Con}{sep}{AMS}{sep}{PMS}")

'    '            .AppendLine("Operator parameters (Carrier(s) marked with an *):")
'    '            .AppendLine($"No.{sep}DT{sep}Mul{sep}TL{sep}KS{sep}AR{sep}AM{sep}DR{sep}SR{sep}SL{sep}RR{sep}SSG-EG{If(IsCh3Sp, $"{sep}Block{sep}FNum", "")}")
'    '            For i As Integer = 0 To 3
'    '                .AppendLine($"{i + 1}{If(IsCarrier(i), "*", "")}{sep}{DT(i)}{sep}{Multi(i)}{sep}{TL(i)}{sep}{KS(i)}{sep}{AR(i)}{sep}{AM(i)}{sep}{DR(i)}{sep}{SR(i)}{sep}{SL(i)}{sep}{RR(i)}{sep}{SSG_EG(i)}{If(IsCh3Sp, $"{sep}{sep}{Ch3SpBlock(i)}{sep}{sep}{Ch3SpFNum(i)}", "")}")
'    '            Next
'    '        End With
'    '        Return sb.ToString
'    '    End Function

'    'End Structure


'End Class
