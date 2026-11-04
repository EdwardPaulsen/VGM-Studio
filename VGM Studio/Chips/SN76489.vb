'PSG (Programmable Sound Generator) Simulator/Emulator
'for Texas Instruments SN76489 and compatible chips
'1999-2026 by Paul Jensen
'Portions based on:
'- MAME source code by MAME contributors
'- Findings by Maxim and others in the SMSPower! forums

Public Class PSG

#Region "Constants"
    Public Const fNumBitCount As Integer = 10
    Public Const fNumMax As Integer = (1 << fNumBitCount) - 1

    Public Const attenBitCount As Integer = 4
    Public Const attenMax As Integer = (1 << attenBitCount) - 1
#End Region

#Region "Tables"

    Public Shared ReadOnly AttenOutput As Single() =
        {1.0, 0.772, 0.622, 0.485, 0.382, 0.29, 0.229, 0.174, 0.132, 0.096, 0.072, 0.051, 0.034, 0.019, 0.009, 0.0, -1.059}

#End Region

#Region "Shared Methods"
    Shared Function GetFNote(fnum As Integer, sampleClockFreq As Single) As Single
        If fnum = 0 Then
            Return 0
        Else
            Return sampleClockFreq / fnum
        End If
    End Function


#End Region

#Region "Enums"
    Public Enum RegAddr As Byte
        Tone0Freq = 0
        Tone0Atten = 1
        Tone1Freq = 2
        Tone1Atten = 3
        Tone2Freq = 4
        Tone2Atten = 5
        NoiseControl = 6
        NoiseAtten = 7
    End Enum

    Public Enum RegMask As UInteger
        Latch = &B_10000000
        Channel = &B_01100000
        DataType = &B_00010000
        Addr = &B_01110000
        DataLo = &B_00001111
        DataHi = &B_00111111
        RegLo = &B_0000001111
        RegHi = &B_1111110000
        NoiseFB = &B_00000100
        NoiseRate = &B_00000011
    End Enum

    Public Enum ChValue As Byte
        Tone1 = &H0
        Tone2 = &H1
        Tone3 = &H2
        Noise = &H3
    End Enum

    Public Enum DataType As Byte
        FNum = &H0
        Atten = &H1
    End Enum

    Public Enum NoiseFB As Byte
        Periodic = &H0
        White = &H1
    End Enum

    Public Enum NoiseRate As Byte
        High = &H0
        Med = &H1
        Low = &H2
        Tone3 = &H3
    End Enum

#End Region

    Public Class PSGRegisters

        Public NoiseFB As NoiseFB
        Public NoiseRate As NoiseRate

        Public ShiftMask As UShort
        Public TapBits As UShort
        Public ShiftWidth As UInteger
        Public Shift As UShort

        Public ReadOnly Property Chip As Chip
        Sub New(ByRef chip As Chip, Optional mask As UShort = &H8000, Optional tap As UShort = &H9, Optional width As UInteger = 16)
            Me.Chip = chip
            ShiftMask = mask
            TapBits = tap
            ShiftWidth = ShiftWidth
            Shift = mask
        End Sub

    End Class

    Public Class PSGChannel
        Public FNum As UInteger
        Public Atten As UInteger
        Public Counter As Integer
        Public Polarity As Integer
        Public Output As Integer

        Public ReadOnly Property Index As Integer
        Public ReadOnly Property Chip As Chip
        Public Sub New(index As Integer, ByRef chip As Chip)
            Me.Index = index
            Me.Chip = chip
            FNum = 0
            Atten = &HF
            Counter = 0
            Polarity = 1
            Output = 0
        End Sub
    End Class

    Public MustInherit Class Chip

#Region "Constants"

        ' System master clock frequencies
        Public Const System_Master_Clock_NTSC_60Hz As Long = 53693100
        Public Const System_Master_Clock_PAL_50Hz As Long = 44744250

        ' External (master) clock frequencies (fM)
        Public Const Ext_Clock_Divider As Integer = 15
        Public Const Ext_Clock_NTSC_60Hz As Long = System_Master_Clock_NTSC_60Hz \ Ext_Clock_Divider
        Public Const Ext_Clock_PAL_50Hz As Long = System_Master_Clock_PAL_50Hz \ Ext_Clock_Divider

        ' Internal (sample) clock frequencies (fsam)
        Public Const Sample_Clock_Divider As Long = 16
        Public Const Int_Clock_NTSC_60Hz As Long = Ext_Clock_NTSC_60Hz \ Sample_Clock_Divider
        Public Const Int_Clock_PAL_50Hz As Long = Ext_Clock_PAL_50Hz \ Sample_Clock_Divider

#End Region

#Region "Properties"

        ''' <summary>
        ''' Master clock freqency (N)
        ''' </summary>
        ''' <returns>The master clock frequency of the chip in Hz.</returns>
        Public ReadOnly Property MasterClockFreq() As Integer

        ''' <summary>
        ''' Sample clock frequency (FSam)
        ''' </summary>
        ''' <returns>The internal sampling rate of the chip in Hz.</returns>
        Public ReadOnly Property IntSampleClockFreq() As Integer

        ''' <summary>
        ''' External sample clock frequency
        ''' </summary>
        ''' <returns>The external sampling rate of the chip in Hz.</returns>
        Public ReadOnly Property ExtSampleClockFreq() As Integer

        ''' <summary>
        ''' Number of internal clocks per external sample
        ''' </summary>
        ''' <returns>The number of internal clocks per external sample in Hz.</returns>
        Public ReadOnly Property IntClocksPerExtClock As Single

#End Region

        Protected _Registers As PSGRegisters
        Public Property Registers As PSGRegisters
            Get
                Return _Registers
            End Get
            Protected Set(value As PSGRegisters)
                _Registers = value
            End Set
        End Property

        Protected _Channel As PSGChannel()
        Public Property Channel As PSGChannel()
            Get
                Return _Channel
            End Get
            Protected Set(value As PSGChannel())
                _Channel = value
            End Set
        End Property

        Protected _Output(1) As Single
        Public Property Output As Single()
            Get
                Return _Output
            End Get
            Protected Set(value As Single())
                _Output = value
            End Set
        End Property

        Public ReadOnly Property Resample As Boolean
        Public ReadOnly Property IntClocksPerExtClockIntegral As Integer
        Public ReadOnly Property IntClocksPerExtClockFractional As Single
        Public ReadOnly Property ResampleRatio As Single
        Public ReadOnly Property PrevSample As Single()
        Public ReadOnly Property Sample As Single()

        Public ReadOnly Property Filter As Boolean
        Public ReadOnly Property FilterAlpha As Double
        Public ReadOnly Property FilterAlpha_i As Double

#Region "Constructor"

        Sub New(extClockFreq As Integer, extSampleClockFreq As Integer, Optional mask As UShort = &H8000, Optional tap As UShort = &H9, Optional width As UInteger = 16, Optional flags As Byte = 0, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 3390)
            _MasterClockFreq = extClockFreq
            _IntSampleClockFreq = extClockFreq \ Sample_Clock_Divider
            _ExtSampleClockFreq = extSampleClockFreq
            IntClocksPerExtClock = IntSampleClockFreq / extSampleClockFreq

            Registers = New PSGRegisters(Me, mask, tap, width)
            ReDim Channel(3)
            For chIdx As Integer = 0 To 3
                Channel(chIdx) = New PSGChannel(chIdx, Me)
            Next

            'For resampling
            _Resample = resample
            IntClocksPerExtClockIntegral = Math.Floor(IntClocksPerExtClock)
            IntClocksPerExtClockFractional = IntClocksPerExtClock - IntClocksPerExtClockIntegral
            ResampleRatio = extSampleClockFreq / IntSampleClockFreq

            ReDim PrevSample(1)
            ReDim Sample(1)

            'For filtering
            _Filter = filter
            If filter Then
                FilterAlpha = Resampler.GetFirstOrderIIRLowPassFilterAlpha(IntSampleClockFreq, cutoff)
            End If

        End Sub

#End Region

#Region "Data I/O"

        Sub Write(value As Byte)
            'STEP 1: Process data write

            'Get the latch bit (bit 7) from the value
            Dim latch As Boolean =
                (value And RegMask.Latch) <> 0
            Static addr As RegAddr
            Static chIdx As ChValue
            Static dataType As DataType
            Dim data As UInteger

            If latch Then 'If bit 7 (latch bit) is set
                'Get the channel, data type, and data from the value
                '(In this case, data is contained only in the lowest four bits)
                addr = (value And RegMask.Addr) >> 4
                chIdx = (value And RegMask.Channel) >> 5
                dataType = (value And RegMask.DataType) >> 4
                data = value And RegMask.DataLo
            End If

            Dim ch As PSGChannel =
                Channel(chIdx)

            With ch
                Select Case dataType
                    Case DataType.FNum
                        If chIdx = ChValue.Noise Then
                            Registers.NoiseFB = (data And RegMask.NoiseFB) >> 2
                            Registers.NoiseRate = data And RegMask.NoiseRate

                            .FNum =
                                If(Registers.NoiseRate = NoiseRate.Tone3,
                                    Channel(ChValue.Tone3).FNum,
                                    Math.Pow(2, Registers.NoiseRate) << 4)

                            Registers.Shift = Registers.ShiftMask
                            With Registers
                                RaiseEvent NoiseTypeChanged(Me)
                            End With
                        Else
                            Static LastFNumWriteWasLo(2) As Boolean
                            If latch Then 'Write Lo Bits
                                .FNum = (.FNum And RegMask.RegHi) Or data
                                RaiseEvent FNumLoChanged(ch, data)
                                If LastFNumWriteWasLo(chIdx) Then
                                    'RaiseEvent FNumChanged(ch)
                                End If
                                LastFNumWriteWasLo(chIdx) = True
                            Else 'Write Hi Bits
                                data = value And RegMask.DataHi
                                .FNum = (.FNum And RegMask.RegLo) Or (data << 4)
                                LastFNumWriteWasLo(chIdx) = False
                                RaiseEvent FNumHiChanged(ch, data)
                                RaiseEvent FNumChanged(ch)
                            End If
                        End If
                    Case DataType.Atten
                        .Atten = (.Atten And RegMask.RegHi) Or data
                        If chIdx = 3 Then   'Noise
                            RaiseEvent NoiseAttenChanged(ch)
                        Else
                            RaiseEvent ToneAttenChanged(ch)
                        End If
                End Select
            End With

        End Sub

        '9/23/2026: Added support for Game Gear Stereo
        Public ReadOnly LROutput(,) As Boolean = {{True, True}, {True, True}, {True, True}, {True, True}}

        Sub WriteGGStereo(data As Byte)
            For i As Integer = 0 To 7
                LROutput(i Mod 4, If(i < 3, 1, 0)) = ((data >> i) And 1) = 1
            Next
        End Sub

#End Region

#Region "Methods"
        ''' <summary>
        ''' Advances the chip by 16 clock cycles and updates output.
        ''' </summary>
        Sub Advance()

            For i As Integer = 0 To 1
                Output(i) = 0
            Next

            For chIdx As Integer = 0 To 3
                Dim ch As PSGChannel =
                    Channel(chIdx)
                With ch
                    If .FNum <= 1 Then
                        .Output = 1
                    Else
                        .Counter -= 1
                        If .Counter <= 0 Then
                            .Polarity = - .Polarity
                            If chIdx = ChValue.Noise Then
                                'Shift noise output once every full period
                                If .Polarity = 1 Then
                                    With Registers
                                        Dim FB As UShort
                                        Select Case .NoiseFB
                                            Case NoiseFB.Periodic
                                                FB = .Shift And 1
                                            Case NoiseFB.White
                                                FB = Parity(.Shift And .TapBits)
                                        End Select
                                        ch.Output = .Shift And 1
                                        .Shift >>= 1
                                        .Shift = .Shift Or (FB << (.ShiftWidth - 1))
                                    End With
                                End If
                            Else
                                .Output = .Polarity
                            End If

                            'Reset counter
                            If chIdx = ChValue.Noise AndAlso Registers.NoiseRate = NoiseRate.Tone3 Then
                                .Counter += Channel(ChValue.Tone3).FNum
                            Else
                                .Counter += .FNum
                            End If

                        End If
                    End If
                End With

                'Accumulate output
                For spkIdx As Integer = 0 To 1
                    With ch
                        If LROutput(chIdx, spkIdx) Then
                            Output(spkIdx) += .Output * AttenOutput(.Atten) * 0.25
                        End If
                    End With
                Next

            Next

        End Sub

        Private Shared Function Parity(value As UShort) As UShort
            value = value Xor (value >> 8)
            value = value Xor (value >> 4)
            value = value Xor (value >> 2)
            value = value Xor (value >> 1)
            Return value And 1
        End Function

        Sub Reset()

        End Sub

#End Region

#Region "Output"
        Public Sub GenerateSamples(ByRef samples As Single(,))
            Dim outLen As Integer = samples.GetUpperBound(0) + 1
            Dim rawLen As Double = outLen * IntClocksPerExtClock
            Dim rawLenInt As Integer = Math.Floor(rawLen)

            Static rawLenFrac As Double = 0
            rawLenFrac += rawLen - rawLenInt

            If rawLenFrac >= 1 Then
                rawLenInt += Math.Floor(rawLenFrac)
                rawLenFrac -= Math.Floor(rawLenFrac)
            End If

            Dim rawsamples(rawLenInt - 1, 1) As Single

            For i As Integer = 0 To rawsamples.GetUpperBound(0)
                For j As Integer = 0 To 1
                    If Output(j) <> 0 Then
                        Dim foo As Integer = 0
                    End If
                    rawsamples(i, j) = Output(j)
                Next
                Advance()
            Next

            Dim upsample As Boolean =
                rawLen < outLen

            Static prevsample As Single() = {0, 0}

            If upsample Then
                'Resample before applying low pass filter
                Resampler.ApplyCubicResample(rawsamples, samples)
                'Low pass filter
                Resampler.ApplyFirstOrderIIRLowPassFilter(samples, prevsample, FilterAlpha)
            Else
                'Low pass filter
                Resampler.ApplyFirstOrderIIRLowPassFilter(rawsamples, prevsample, FilterAlpha)
                'Resample after applying low pass filter
                Resampler.ApplyCubicResample(rawsamples, samples)
            End If

        End Sub

#End Region

#Region "Events"

        Public Event FNumLoChanged(ByRef ch As PSGChannel, fnumLo As Integer)
        Public Event FNumHiChanged(ByRef ch As PSGChannel, fnumHi As Integer)
        Public Event FNumChanged(ByRef ch As PSGChannel)
        Public Event ToneAttenChanged(ByRef ch As PSGChannel)
        Public Event NoiseTypeChanged(ByRef chip As Chip)
        Public Event NoiseAttenChanged(ByRef ch As PSGChannel)

#End Region

    End Class

End Class

Public Class SN76489

    Public Class Chip
        Inherits PSG.Chip

        Sub New(masterClockFreq As Integer, extSampleClockFreq As Integer, Optional mask As UShort = &H8000, Optional tap As Short = &H9, Optional width As Byte = 16, Optional flags As Byte = 0, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 3390)
            MyBase.New(masterClockFreq, extSampleClockFreq, mask, tap, width, flags, resample, filter, cutoff)
        End Sub
    End Class

End Class
