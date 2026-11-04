' Ricoh RFC5164 PCM Sound Chip Simulator / Emulator
' Copyright © 2026 Paul Jensen

' Portions based on:
' - Information from jsgroth's blog at https://jsgroth.dev/blog/posts/sega-cd-pcm-overview/
' - rf5c68.cpp MAME source code by MAME contributors
' - pcm.cpp from Genesis GX Plus by Eke-Eke

Public Class RFC5164

#Region "Constants"

    Public Const Ch_Count As Integer = 8

    'Current address is 16.11 fixed-point decimal
    Const INT_BITS As UInteger = 16
    Const FRAC_BITS As UInteger = 11
    Const TOTAL_BITS As UInteger = INT_BITS + FRAC_BITS
    Const LOOP_END = &HFF

    Public Const ADDR_INC_MAX As UInteger = (1 << FRAC_BITS) - 1

#End Region

#Region "Shared Methods"

    Shared Function GetOutput(samp As PCMSample, env As Byte, pan As Byte) As Single
        'DCA block algorithm (p. 3-60 of the RF5C68A manual)

        '(1) Get sign bit (top bit) and magnitude bits (bottom 7 bits) from input sample
        'True = positive, False = negative
        Dim sign As Boolean =
            samp.Sign
        Dim magn As Integer =
            samp.Magn

        '(2) Multiply magnitude bits with envelope data
        Dim out0 As UInteger =
            magn * env
        '(3) Multiply output from (2) with the pan data for the L and R channels
        out0 *= pan And &HF
        '(4) Multiply the top 14 bits from (3) with the sign bit from (1)
        out0 >>= 5

        Dim out As Integer =
            If(sign, -out0, out0)

        '(5) Clamp the output to 16-bit
        out = Math.Clamp(out, Short.MinValue, Short.MaxValue)

        Return out / Short.MaxValue
    End Function

    Shared Function GetSampleRateInHz(addrInc As UInteger, masterClockFreq As Single) As Single
        Return masterClockFreq / 384 * (addrInc / 2047) '(1 << 11) - 1
    End Function

#End Region

#Region "Enums"
    Public Enum RegAddr As Byte
        Envelope = 0
        Pan = 1
        AddrIncLo = 2
        AddrIncHi = 3
        LoopAddrLo = 4
        LoopAddrHi = 5
        StartAddr = 6
        Ctrl = 7
        Enable = 8
    End Enum

#End Region

#Region "Classes and Structures"

    Public Structure PCMSample
        Private Samp As Byte
        Property Sign As Boolean
            Get
                'Top bit set = positive; unset = negative
                Return (Samp And &H80) = 0
            End Get
            Set(value As Boolean)
                If value Then
                    Samp = Samp And &H7F
                Else
                    Samp = Samp Or &H80
                End If
            End Set
        End Property

        Property Magn As Byte
            Get
                Return Samp And &H7F
            End Get
            Set(value As Byte)
                Samp = (Samp And &H80) Or (value And &H7F)
            End Set
        End Property

        ReadOnly Property Value As Integer
            Get
                If Sign Then
                    Return -Magn
                Else
                    Return Magn
                End If
            End Get
        End Property

        Sub New(samp As Byte)
            Me.Samp = samp
        End Sub
    End Structure

    Public Class PCMChannel
        Public Enabled As Boolean          'Channel is active
        Public PrevSample As Byte          'Prevous sample
        Public Sample As Byte              'Current sample
        Public PrevAddr As UInteger
        Public Addr As UInteger            'Current address in 16.11 fixed-point decimal
        Public AddrInc As UInteger         'Address increment in 5.11 fixed-point decimal

        Public StartAddr As UInteger       'Start address as a 16-bit integer
        Public LoopAddr As UInteger        'Loop address as a 16-bit integer
        Public Envelope As UInteger
        Public Pan(1) As UInteger

        Public PrevOutput(1) As Single
        Public Output(1) As Single

        Public ReadOnly Index As Integer
        Public ReadOnly Property Chip As Chip
        Public Sub New(index As Integer, ByRef chip As Chip)
            Me.Index = index
            Me.Chip = chip
            Pan = {&HF, &HF}
        End Sub

        Sub CalculateOutput()
            For spkIdx As Integer = 0 To 1
                PrevOutput(spkIdx) = Output(spkIdx)
                '.Output(spkIdx) = GetOutput(New PCMSample(.Sample), .Envelope, .Pan(spkIdx))
                Dim samp As New PCMSample(Sample)
                Output(spkIdx) = Chip.OutputLookupTable(samp.Magn, Envelope, Pan(spkIdx))
                If samp.Sign Then
                    Output(spkIdx) *= -1
                End If
            Next
        End Sub

        Sub Advance()
            If Not Enabled Then
                'Reload PCM RAM address pointers when channels are OFF
                If Not Enabled Then
                    Addr = StartAddr << (8 + FRAC_BITS)
                End If
                Sample = 0
                Exit Sub
            End If

            'Store previous address and sample
            PrevAddr = Addr
            PrevSample = Sample

            Dim nextAddr As UInteger

            'Get current sample
            Dim thisSample As Integer =
                Chip.PCMRAM(Addr >> FRAC_BITS)
            'Check for loop end marker
            If thisSample = LOOP_END Then
                'Preview next sample
                Dim nextSample As Byte =
                        Chip.PCMRAM(LoopAddr >> FRAC_BITS)
                Sample = (If(nextSample = LOOP_END, 0, nextSample))
                nextAddr = (LoopAddr << FRAC_BITS) And ((1 << TOTAL_BITS) - 1)
            Else
                'Get current sample
                Sample = thisSample
                nextAddr = (Addr + AddrInc) And ((1 << TOTAL_BITS) - 1)
            End If
            'Increment address
            Addr = nextAddr
        End Sub

    End Class

    Public Structure Voice
        Public ReadOnly Property Index As Long
        Public ReadOnly Property MasterClockFreq As UInteger
        Public ReadOnly Property StartAddr As UInteger
        Public ReadOnly Property EndAddr As UInteger
        Public ReadOnly Property LoopAddr As UInteger
        Public ReadOnly Property Data As Byte()
        Public ReadOnly Property AddrIncs As SortedSet(Of UInteger)
        Public ReadOnly Property Speeds As SortedSet(Of Single)

        Sub New(index As Long, ByRef chip As Chip, startAddr As UInteger, loopAddr As UInteger, ByRef pcmData As Byte())
            _Index = index
            _MasterClockFreq = chip.MasterClockFreq
            _StartAddr = startAddr << 8
            _LoopAddr = loopAddr
            'Get PCM data
            Dim list As New List(Of Byte)
            Dim loopCount As Integer = 0
            For i As Integer = startAddr << 8 To pcmData.GetUpperBound(0)
                Dim thisSamp As Byte =
                    pcmData(i)
                If thisSamp = &HFF Then
                    If loopCount >= 2 Then
                        _EndAddr = i
                        Exit For
                    Else
                        If loopAddr = startAddr << 8 Then
                            _EndAddr = i
                            Exit For
                        Else
                            loopCount += 1
                            i = loopAddr - 1
                        End If
                    End If
                Else
                    list.Add(pcmData(i))
                End If
            Next
            Dim length As Integer =
                list.Count
            If length > 0 Then
                ReDim _Data(length - 1)
                Array.Copy(list.ToArray, _Data, length)
            End If
            AddrIncs = New SortedSet(Of UInteger)
            Speeds = New SortedSet(Of Single)
        End Sub
        Sub New(sample As Long, ByRef ch As PCMChannel, ByRef pcmData As Byte())
            Me.New(sample, ch.Chip, ch.StartAddr, ch.LoopAddr, pcmData)
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(StartAddr, LoopAddr)
        End Function
        Public Overrides Function Equals(obj As Object) As Boolean
            If obj Is Nothing Then
                Return False
            End If

            If [GetType]() <> obj.GetType() Then
                Return False
            End If

            Dim b As Voice = obj

            If StartAddr = b.StartAddr AndAlso
               LoopAddr = b.LoopAddr Then
                Return True
            End If
            Return False
        End Function
        Overloads Shared Operator =(a As Voice, b As Voice)
            Return Equals(a, b)
        End Operator
        Overloads Shared Operator <>(a As Voice, b As Voice)
            Return Not Equals(a, b)
        End Operator
        Public Overrides Function ToString() As String
            Dim sb As New Text.StringBuilder
            Dim sep As Char = ","
            With sb
                .AppendLine($"Start Addr{sep}Loop Addr")
                .AppendLine($"{StartAddr}{sep}{LoopAddr}")
            End With
            Return sb.ToString
        End Function
    End Structure

#End Region

    Public Class Chip

#Region "Clock Frequencies"

        ''' <summary>
        ''' Master clock frequency in Hz.
        ''' </summary>
        ''' <returns>The master clock frequency of the chip in Hz.</returns>
        Public ReadOnly Property MasterClockFreq() As Long

        ''' <summary>
        ''' FM generator clock frequency in Hz.
        ''' </summary>
        ''' <returns>The FM generator clock in Hz.</returns>
        Public ReadOnly Property FMClockFreq() As Single

        ''' <summary>
        ''' Internal sample clock rate in Hz.
        ''' </summary>
        ''' <returns>The internal sampling rate of the chip in Hz.</returns>
        Public ReadOnly Property IntSampleClockFreq() As Single

        ''' <summary>
        ''' External sample clock frequency
        ''' </summary>
        ''' <returns>The external sampling rate of the chip in Hz.</returns>
        Public ReadOnly Property ExtSampleClockFreq() As Integer

        ''' <summary>
        ''' Number of internal clocks per external sample
        ''' </summary>
        ''' <returns>The number of internal clocks per external sample in Hz.</returns>
        Public ReadOnly Property IntClocksPerExtClock As Double

#End Region

#Region "Lookup Tables"

        'Sample magnitude, envelope, pan
        Private _OutputLookupTable(127, 255, 15) As Single
        Public ReadOnly Property OutputLookupTable As Single(,,)
            Get
                Return _OutputLookupTable
            End Get
        End Property

        Protected Sub GenerateOutputLookupTable()
            'Pre-calculate all possible 12-bit output values for speed boost
            For magn As Integer = 0 To 127
                For env As Integer = 0 To 255
                    For pan As Integer = 0 To 15
                        OutputLookupTable(magn, env, pan) =
                            GetOutput(New PCMSample(magn + 128), env, pan)
                    Next
                Next
            Next
        End Sub

#End Region

        Dim Enabled As Boolean
        Public PCMRAM(65535) As Byte
        Public RAMWriteAddr As UInteger
        Public Channel As PCMChannel()
        Public PrevOutput(1) As Single
        Public Output(1) As Single

        ''Current address is 16.11 fixed-point decimal
        'Const INT_BITS As UInteger = 16
        'Const FRAC_BITS As UInteger = 11
        'Const TOTAL_BITS As UInteger = INT_BITS + FRAC_BITS

        ''0xFF is a special sample value that means "loop end"
        'Const LOOP_END = &HFF

        Private _ChannelLatch As Integer
        Public Property ChannelLatch As Integer
            Private Set(value As Integer)
                _ChannelLatch = value
            End Set
            Get
                Return _ChannelLatch
            End Get
        End Property

        'For resampling
        Public ReadOnly Property Resample As Boolean
        Public ReadOnly Property IntClocksPerExtClockIntegral As Integer
        Public ReadOnly Property IntClocksPerExtClockFractional As Single
        Public ReadOnly Property ResampleRatio As Single
        Public ReadOnly Property PrevSample As Single()
        Public ReadOnly Property Sample As Single()

        'For filtering
        Public ReadOnly Property Filter As Boolean
        Public ReadOnly Property FilterAlpha As Double

        Sub New(freqMasterClock As Long, freqExtSampleClock As Long, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 16276.041666666666) '7973)
            'Set clock frequencies
            _MasterClockFreq = freqMasterClock
            _IntSampleClockFreq = _MasterClockFreq / 384 '48 cycles per channel * 8 channels
            _ExtSampleClockFreq = freqExtSampleClock
            _IntClocksPerExtClock = IntSampleClockFreq / ExtSampleClockFreq

            'Instantiate channels
            ReDim Channel(Ch_Count - 1)
            For chIdx As Integer = 0 To Channel.Length - 1
                Channel(chIdx) = New PCMChannel(chIdx, Me)
            Next

            Output = {0, 0}

            GenerateOutputLookupTable()

            'For resampling
            _Resample = resample
            IntClocksPerExtClockIntegral = Math.Floor(IntClocksPerExtClock)
            IntClocksPerExtClockFractional = IntClocksPerExtClock - IntClocksPerExtClockIntegral
            ResampleRatio = ExtSampleClockFreq / IntSampleClockFreq

            'For filtering
            _Filter = filter
            If filter Then
                FilterAlpha = Resampler.GetFirstOrderIIRLowPassFilterAlpha(IntSampleClockFreq, cutoff)
            End If

        End Sub

        Sub Reset()
            Enabled = False
            ChannelLatch = 0
            For i As Integer = 0 To Ch_Count - 1
                Channel(i) = New PCMChannel(i, Me)
            Next
        End Sub

        Sub WriteToPCMRAM(ByRef data As Byte(), startAddr As UShort)
            Array.Copy(data, 0, PCMRAM, startAddr + RAMWriteAddr, data.Length)
        End Sub

        Function Write(addr As Byte, value As Byte) As Boolean
            'Advance()
            If addr < &H7 OrElse (addr >= &H20) AndAlso (addr < &H40) Then 'Channel event
                Dim chIdx As Integer =
                    ChannelLatch
                Dim ch As PCMChannel =
                    Channel(chIdx)

                Select Case addr
                    Case &H0 'ENV Envelope
                        With ch
                            .Envelope = value
                        End With
                        RaiseEvent EnvelopeChanged(ch)
                    Case &H1 'PAN Pan
                        With ch
                            .Pan(0) = value And &H_0F
                            .Pan(1) = (value And &H_F0) >> 4
                        End With
                        RaiseEvent PanChanged(ch)
                    Case &H2 'FDL Frequency Data Lo
                        With ch
                            .AddrInc = (.AddrInc And &H_FF00) Or value
                        End With
                        RaiseEvent AddressIncrementChanged(ch)
                    Case &H3 'FDH Frequency Data Hi
                        With ch
                            .AddrInc = (.AddrInc And &H_00FF) Or (CUInt(value) << 8)
                        End With
                        RaiseEvent AddressIncrementChanged(ch)
                    Case &H4 'LSL Loop Start Lo
                        With ch
                            .LoopAddr = ((.LoopAddr And &H_FF00) Or value)
                        End With
                        RaiseEvent LoopAddressChanged(ch)
                    Case &H5 'LSH Loop Start Hi
                        With ch
                            .LoopAddr = ((.LoopAddr And &H_00FF) Or (CUInt(value) << 8))
                        End With
                        RaiseEvent LoopAddressChanged(ch)
                    Case &H6 'ST  Start
                        With ch
                            .StartAddr = value << 8 '16.11 fixed point
                            'Reload PCM RAM address if channel is OFF
                            If Not .Enabled Then
                                .Addr = .StartAddr << (8 + FRAC_BITS)
                            End If
                        End With
                        RaiseEvent StartAddressChanged(ch)
                    Case &H20 To &H3F 'Direct control of panning volume
                        chIdx =
                            (addr And &HF) \ 2
                        Dim lrIdx As Byte =
                            addr Mod 2
                        ch =
                            Channel(chIdx)
                        With ch
                            .Pan(lrIdx) = value And &HF
                        End With
                        RaiseEvent PanChanged(ch)
                    Case Else
                        Return False
                End Select

            Else    'Chip event
                Select Case addr
                    Case &H7 'Control Register
                        Dim en As Boolean =
                            (value And &B_10000000) <> 0
                        Enabled = en
                        RaiseEvent ChipOutputEnabledChanged(Enabled)

                        'Check MOD (Mode?) bit and process data accordingly
                        If (value And &B_01000000) <> 0 Then
                            'Latch channel
                            'Lowest 3 bits control selection of channels when the microcomputer accesses internal memories
                            '(ENV, PAN, FDL, FDH, LSL, LSH, and ST).
                            ChannelLatch = value And &B_00000111
                            RaiseEvent ChannelLatchChanged(ChannelLatch)
                        Else
                            'Select PCM RAM bank and address
                            'Lowest 4 bits control higher addresses when the microcomputer accesses external waveform memories.
                            RAMWriteAddr = (value And &HF) << 12
                            RaiseEvent RAMWriteAddressChanged(RAMWriteAddr)
                        End If
                    Case &H8 'Channel ON/OFF Register
                        'The chip uses active low logic here. So, instead of 1 = ON and 0 = OFF, its more like 1 = MUTED and 0 = NOT MUTED
                        For i As Integer = 0 To 7
                            With Channel(i)
                                .Enabled = ((value >> i) And 1) = 0
                                'Reload PCM RAM address if channel is OFF
                                If Not .Enabled Then
                                    .Addr = .StartAddr << (8 + FRAC_BITS)
                                End If
                            End With
                        Next
                        RaiseEvent ChannelOutputEnabledChanged(value)
                    Case Else
                        Return False
                End Select

            End If

            Return True

        End Function

        Sub Advance()
            If Not Enabled Then
                Exit Sub
            End If

            CalculateOutput()

            For chIdx As Integer = 0 To Channel.Length - 1
                'With Channel(i)
                With Channel(chIdx)
                    Channel(chIdx).Advance()
                End With
            Next

        End Sub

        Public Sub CalculateOutput()
            'Save previous output for each speaker
            For spkIdx As Integer = 0 To 1
                PrevOutput(spkIdx) = Output(spkIdx)
            Next

            Output = {0, 0}

            'Calculate output for each channel (chIdx = Channel Index)
            For chIdx As Integer = 0 To Ch_Count - 1
                With Channel(chIdx)
                    .CalculateOutput()
                    'Sum final chip output for each speaker (spkIdx = Spekaer Index)
                    For spkIdx As Integer = 0 To 1
                        Output(spkIdx) += .Output(spkIdx)
                    Next
                End With
            Next

        End Sub

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

            If rawLenInt = 0 Then
                Exit Sub
            End If

            Dim rawsamples(rawLenInt - 1, 1) As Single

            CalculateOutput()

            For sampIdx As Integer = 0 To rawsamples.GetUpperBound(0)
                For spkIdx As Integer = 0 To 1
                    rawsamples(sampIdx, spkIdx) = Output(spkIdx) '/ Short.MaxValue
                Next
                Advance()
            Next

            Dim upsample As Boolean =
                rawLen < outLen

            Static prevsample As Single() = {0, 0}

            If upsample Then
                'Resample before applying low pass filter
                Resampler.ApplyCubicResample(rawsamples, samples)

                ''Low pass filter
                Resampler.ApplyFirstOrderIIRLowPassFilter(samples, prevsample, FilterAlpha)
            Else
                ''Low pass filter
                Resampler.ApplyFirstOrderIIRLowPassFilter(rawsamples, prevsample, FilterAlpha)

                'Resample after applying low pass filter
                Resampler.ApplyCubicResample(rawsamples, samples)
            End If

        End Sub

#Region "Events"

        Public Event EnvelopeChanged(ByRef ch As PCMChannel)
        Public Event PanChanged(ByRef ch As PCMChannel)
        Public Event AddressIncrementChanged(ByRef ch As PCMChannel)
        Public Event LoopAddressChanged(ByRef ch As PCMChannel)
        Public Event StartAddressChanged(ByRef ch As PCMChannel)
        Public Event ChannelLatchChanged(chIdx As Integer)
        Public Event RAMWriteAddressChanged(addr As UInteger)
        Public Event ChipOutputEnabledChanged(enabled As Boolean)
        Public Event ChannelOutputEnabledChanged(enabled As Byte)

#End Region

    End Class


End Class
