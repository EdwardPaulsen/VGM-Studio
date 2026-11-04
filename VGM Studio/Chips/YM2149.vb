'Software-Controlled Sound Generator (SSG) Simulator/Emulator
'for General Instruments AY-3-8910, Yamaha YM2149 and compatible chips
'Copyright © 2026 Paul Jensen

Public Class SSG

#Region "Constants"
    Public Const fNumBitCount As Integer = 12
    Public Const fNumMax As Integer = (1 << fNumBitCount) - 1
#End Region

#Region "Shared Methods"
    Shared Function GetFNote(fnum As Integer, sampleClockFreq As Single) As Single
        If fnum = 0 Then
            Return 0
        Else
            Return sampleClockFreq / (fnum / 2)
        End If
    End Function

    Shared Function GetEnvHz(fnum As Integer, sampleClockFreq As Single) As Single
        If fnum = 0 Then
            Return 0
        Else
            Return sampleClockFreq / (16 * fnum)
        End If
    End Function
    Shared Function GetNormLevel(level As Integer) As Single
        Return NormLevels(level * 2)
    End Function

#End Region

#Region "Enums"
    Public Enum RegAddr As Byte
        FNumALo = &H0                   'Fine tune for melodic tone channel A
        FNumAHi = &H1                   'Coarse tune for melodic tone channel A
        FNumBLo = &H2                   'Fine tune for melodic tone channel B
        FNumBHi = &H3                   'Coarse tune for melodic tone channel B
        FNumCLo = &H4                   'Fine tune for melodic tone channel C
        FNumCHi = &H5                   'Coarse tune for melodic tone channel C
        NoiseFNum = &H6                 'Period for noise channel
        IO_Noise_Tone_Ctrl = &H7        'Enable I/O port A and/or B, noise and/or tone channel    
        Mode_LevelA = &H8               'Set attentuation mode (envelope or fixed) and level for tone channel A
        Mode_LevelB = &H9               'Set attentuation mode (envelope or fixed) and level for tone channel B
        Mode_LevelC = &HA               'Set attentuation mode (envelope or fixed) and level for tone channel A
        EnvFNumLo = &HB                 'Low bits of envelope period
        EnvFNumHi = &HC                 'High bits of envelope period
        EnvShape = &HD                  'Envelope shape
        IOAData = &HE                   'I/O port A data
        IOBData = &HF                   'I/O port B data
    End Enum

    Public Enum RegDataMask As Byte
        FNumLo = &B_11111111
        FNumHi = &B_00001111
        FNumNoise = &B_00011111

        IOCtrl = &B_11000000
        IOB = &B_10000000
        IOA = &B_01000000
        NoiseCtrl = &B_00111000
        NoiseC = &B_00100000
        NoiseB = &B_00010000
        NoiseA = &B_00001000
        ToneCtrl = &B_00000111
        ToneC = &B_00000100
        ToneB = &B_00000010
        ToneA = &B_00000001

        EnvMode = &B_00010000
        Level = &B_00001111

        EnvFNum = &B_11111111

        Cont = &B_00001000
        Att = &B_00000100
        Alt = &B_00000010
        Hold = &B_00000001

        IOData = &B_11111111
    End Enum

#End Region

#Region "Tables"

    Public Shared ReadOnly Levels As UShort() =
    {
        0, 44, 53, 62, 74, 88, 105, 125,
        149, 177, 210, 250, 297, 354, 420, 500,
        595, 707, 841, 1000, 1189, 1414, 1682, 2000,
        2378, 2828, 3364, 4000, 4757, 5657, 6727, 8000
    }

    Public Shared ReadOnly NormLevels As Single() =
    {
        0, 0.0055, 0.006625, 0.00775, 0.00925, 0.011, 0.013125, 0.015625,
        0.018625, 0.022125, 0.02625, 0.03125, 0.037125, 0.04425, 0.0525, 0.0625,
        0.074375, 0.088375, 0.105125, 0.125, 0.148625, 0.17675, 0.21025, 0.25,
        0.29725, 0.3535, 0.4205, 0.5, 0.594625, 0.707125, 0.840875, 1.0
    }

#End Region

#Region "Classes"
    Public Class SSGRegisters
        Public FNum(2) As UInteger
        Public NoiseFNum As UInteger
        Public IOEnabled(1) As Boolean      'Active-low signal
        Public NoiseEn(2) As Boolean        'Active-low signal
        Public ToneEn(2) As Boolean         'Active-low signal
        Public EnvMode(2) As Boolean        'Active-high signal
        Public Level(2) As Byte
        Public EnvFNum As UInteger
        Public EnvCnt As Boolean
        Public EnvAtt As Boolean
        Public EnvAlt As Boolean
        Public EnvHld As Boolean
        Public IOData(1) As Byte

        Public ReadOnly chip As Chip
        Sub New(ByRef chip As Chip)
            Me.chip = chip
        End Sub

    End Class

#End Region

    Public MustInherit Class Chip

#Region "Clock Frequencies"

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

        Protected _Registers As SSGRegisters
        ''' <summary>
        ''' Global registers common to all SSG chips.
        ''' </summary>
        ''' <returns>An <c>SSGRegisters</c> structure.</returns>
        Public Property Registers As SSGRegisters
            Get
                Return _Registers
            End Get
            Protected Set(value As SSGRegisters)
                _Registers = value
            End Set
        End Property

#Region "Methods"
        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            'Set clock frequencies
            Me.MasterClockFreq = masterClockFreq
            Me.ExtSampleClockFreq = extSampleClockFreq
            InitializeFrequencies()
            Registers = New SSGRegisters(Me)
        End Sub

        Protected MustOverride Sub InitializeFrequencies()

        ''' <summary>
        ''' Writes a byte of data to a specified address
        ''' </summary>
        ''' <param name="addr">The address to write to.</param>
        ''' <param name="value">The data byte to write.</param>
        ''' <returns>Returns <c>True</c> upon success and <c>False</c> upon failure.</returns>
        Public Overridable Function Write(addr As RegAddr, value As UInteger) As Boolean
            Static IsPrevFNumWriteHi(2) As Boolean

            With _Registers
                Select Case addr
                    Case RegAddr.FNumALo, RegAddr.FNumBLo, RegAddr.FNumCLo
                        Dim idx As Integer =
                            addr \ 2
                        .FNum(idx) =
                            (.FNum(idx) And &B_111100000000) Or value
                        RaiseEvent FNumLoChanged(Me, idx, value, .FNum(idx))
                        If Not IsPrevFNumWriteHi(idx) Then
                            RaiseEvent FNumChanged(Me, idx, .FNum(idx))
                        End If
                        IsPrevFNumWriteHi(idx) = False
                    Case RegAddr.FNumAHi, RegAddr.FNumBHi, RegAddr.FNumCHi
                        Dim idx As Integer =
                            (addr - 1) \ 2
                        value =
                            value And RegDataMask.FNumHi
                        .FNum(idx) =
                            (.FNum(idx) And &B_000011111111) Or (value << 8)
                        RaiseEvent FNumHiChanged(Me, idx, value, .FNum(idx))
                        RaiseEvent FNumChanged(Me, idx, .FNum(idx))
                        IsPrevFNumWriteHi(idx) = True
                    Case RegAddr.NoiseFNum
                        .NoiseFNum = (value And RegDataMask.FNumNoise)
                        RaiseEvent NoiseFNumChanged(Me, .NoiseFNum)
                    Case RegAddr.IO_Noise_Tone_Ctrl
                        Dim io As Byte =
                            value And RegDataMask.IOCtrl
                        For i As Integer = 0 To 1
                            .IOEnabled(i) = (io >> i) = 0
                        Next
                        Dim noise As Byte =
                            value And RegDataMask.NoiseCtrl
                        For i As Integer = 0 To 2
                            .NoiseEn(i) = (noise >> i) = 0
                        Next
                        Dim tone As Byte =
                            value And RegDataMask.ToneCtrl
                        For i As Integer = 0 To 2
                            .ToneEn(i) = (tone >> i) = 0
                        Next
                        RaiseEvent IONoiseToneCtrlChanged(Me, .IOEnabled, .NoiseEn, .ToneEn)
                    Case RegAddr.Mode_LevelA, RegAddr.Mode_LevelB, RegAddr.Mode_LevelC
                        Dim idx As Integer =
                            addr And 3
                        .EnvMode(idx) = (value And RegDataMask.EnvMode) <> 0
                        .Level(idx) = value And RegDataMask.Level
                        RaiseEvent ModeLevelChanged(Me, idx, .EnvMode(idx), .Level(idx))
                    Case RegAddr.EnvFNumLo
                        .EnvFNum =
                            (.EnvFNum And &H_FF00) Or value
                        RaiseEvent EnvFNumLoChanged(Me, value, .EnvFNum)
                    Case RegAddr.EnvFNumHi
                        .EnvFNum =
                            (.EnvFNum And &H_00FF) Or (value << 8)
                        RaiseEvent EnvFNumHiChanged(Me, value, .EnvFNum)
                    Case RegAddr.EnvShape
                        .EnvCnt = (value And RegDataMask.Cont) <> 0
                        .EnvAtt = (value And RegDataMask.Att) <> 0
                        .EnvAlt = (value And RegDataMask.Alt) <> 0
                        .EnvHld = (value And RegDataMask.Hold) <> 0
                        RaiseEvent EnvShapeChanged(Me, .EnvCnt, .EnvAtt, .EnvAlt, .EnvHld)
                    Case RegAddr.IOAData
                        .IOData(0) = value
                        RaiseEvent IOADataChanged(Me, .IOData(0))
                    Case RegAddr.IOBData
                        .IOData(1) = value
                        RaiseEvent IOBDataChanged(Me, .IOData(1))
                    Case Else
                        Return False
                End Select
            End With

            Return True
        End Function

#End Region

#Region "Events"

        Public Event FNumLoChanged(chip As Chip, chIdx As Integer, fnumLo As UInteger, fnum As UInteger)
        Public Event FNumHiChanged(chip As Chip, chIdx As Integer, fnumHi As UInteger, fnum As UInteger)
        Public Event FNumChanged(chip As Chip, chIdx As Integer, fnum As UInteger)
        Public Event NoiseFNumChanged(chip As Chip, fnum As UInteger)
        Public Event IONoiseToneCtrlChanged(chip As Chip, ioEn As Boolean(), noiseEn As Boolean(), toneEn As Boolean())
        Public Event ModeLevelChanged(chip As Chip, chIdx As Integer, mode As Boolean, level As UInteger)
        Public Event EnvFNumLoChanged(chip As Chip, fnumLo As UInteger, period As UInteger)
        Public Event EnvFNumHiChanged(chip As Chip, fnumHi As UInteger, period As UInteger)
        Public Event EnvShapeChanged(chip As Chip, cnt As Boolean, att As Boolean, alt As Boolean, hld As Boolean)
        Public Event IOADataChanged(chip As Chip, data As UInteger)
        Public Event IOBDataChanged(chip As Chip, data As UInteger)

#End Region

    End Class

End Class

Public Class YM2149

    Public Class Chip
        Inherits SSG.Chip

        Sub New(masterClockFreq As Long, extSampleClockFreq As Long, Optional highQuality As Boolean = False, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 5894.6)
            MyBase.New(masterClockFreq, extSampleClockFreq, highQuality, resample, filter, cutoff)
        End Sub

        Protected Overrides Sub InitializeFrequencies()
            'TODO: Add code to handle changes in clock divider from YM2203
            IntSampleClockFreq = MasterClockFreq / 16
            IntClocksPerExtClock = IntSampleClockFreq / ExtSampleClockFreq
        End Sub
    End Class

End Class
