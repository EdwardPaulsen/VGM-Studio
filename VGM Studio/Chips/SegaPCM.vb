
'SegaPCM Simulator / Emulator
'Copyright © 2017–2026 Paul Jensen
'Portions based on:
'- MAME sources by Hiromitsu Shioya, Olivier Galibert

Public Class SegaPCM

#Region "Constants"

#End Region

#Region "Shared Methods"

    Shared Function GetOutput(samp As Byte, vol As Byte) As Single
        Return (samp - &H80) * vol / 16256 '128 * 127
    End Function

#End Region

#Region "Enums"
    Public Enum RegAddr As Integer
        Unknown0 = 0
        Unknown1 = 1
        VolL = 2
        VolR = 3
        LoopAddrLo = 4
        LoopAddrHi = 5
        EndAddrHi = 6
        AddrInc = 7
        Unknown80 = &H80
        Unknown81 = &H81
        Unknown82 = &H82
        Unknown83 = &H83
        StartAddrLo = &H84
        StartAddrHi = &H85
        Ctrl = &H86
        Unknown87 = &H87
    End Enum

    Public Enum BankShiftValue As UInteger
        Size256K = 11
        Size512K = 12
        Size12M = 13
    End Enum

    Public Enum BankMaskValue As UInteger
        Mask70 = &H70
        MaskF0 = &HF0
        MaskF8 = &HF8
    End Enum

    <Flags> Public Enum GameID As Long
        Space_Harrier = 1
        OutRun = 2
        After_Burner = 4
        Thunder_Blade = 8
        Last_Survivor = &H10 '16
        Super_Monaco_GP = &H20 '32
        Line_of_Fire = &H40 '64
        Bakudan_Yarou = &H80 '128
        Racing_Hero = &H100 '256
        GP_Rider = &H200 '512
        A_B_Cop = &H400 '
        Royal_Ascot = &H800 '2048
        Carribean_Boule = &H1000 '4096
        Galaxy_Force_II = &H2000 '8192
        Power_Drift = &H4000 '16384
        G_LOC_Air_Battle = &H8000 '32768
        Strike_Fighter = &H10000 '65536
        Rail_Chase = &H20000 '131072
    End Enum

#End Region

#Region "Classes and Structures"

    Public Class Channel

        Public Bank As UInteger

        Public LoopAddrLo As UInteger
        Public LoopAddrHi As UInteger
        Public ReadOnly Property LoopAddr As UInteger
            Get
                Return (LoopAddrHi << 8) Or LoopAddrLo
            End Get
        End Property

        Public EndAddrHi As UInteger
        Public ReadOnly Property EndAddr As UInteger
            Get
                Return EndAddrHi << 8
            End Get
        End Property

        Public StartAddrLo As UInteger
        Public StartAddrHi As UInteger
        Public ReadOnly Property StartAddr As UInteger '16.8 fixed point
            Get
                Return (StartAddrHi << 8) Or StartAddrLo
            End Get
        End Property

        Public AddrInc As UInteger

        Public Addr As UInteger
        Public AddrFrac As UInteger

        Public Vol As Byte()
        Public Property Output As Single()

        Public Enabled As Boolean
        Public Looped As Boolean

        Public ReadOnly Index As Integer
        Public ReadOnly Chip As Chip
        Sub New(index As Integer, ByRef chip As Chip) 'bankShift As BankShiftValue, bankMask As BankMaskValue)
            Me.Index = index
            Me.Chip = chip
            ReDim Vol(1)
            ReDim Output(1)
        End Sub

        Public Sub CalculateOutput()
            For spkIdx As Integer = 0 To 1
                If (Not Enabled) Or (StartAddr >= EndAddr) Then
                    Output(spkIdx) = 0
                Else
                    Dim samp As Byte =
                        Chip.ROMData(Bank + (Addr >> 8))
                    Output(spkIdx) = Chip.OutputLookupTable(samp, Vol(spkIdx))
                End If
            Next
        End Sub

        Sub Advance()
            If Not Enabled Then
                Exit Sub
            End If

            Const maxValue As UInteger =
                16777215 '((1 << 24) - 1)

            Addr = (Addr + AddrInc) And maxValue

            If (Addr >> 8) >= EndAddr Then
                Addr = (LoopAddrHi << 16) Or (LoopAddrLo << 8)
                Enabled = Looped
            End If

        End Sub

    End Class

    Public Structure Voice
        Public ReadOnly Property Index As Long
        Public ReadOnly Property StartAddr As UInteger
        Public ReadOnly Property EndAddr As UInteger
        Public ReadOnly Property LoopAddr As UInteger
        Public ReadOnly Property Data As Byte()
        Public ReadOnly Property AddrIncs As SortedSet(Of UInteger)
        Public ReadOnly Property Length As UInteger
            Get
                Return EndAddr - StartAddr
            End Get
        End Property
        Public ReadOnly Property LoopLength As UInteger
            Get
                Return EndAddr - LoopAddr
            End Get
        End Property
        Public Property Looped As Boolean
        Sub New(index As Long, startAddr As UInteger, endAddr As UInteger, loopAddr As UInteger, ByRef pcmData As Byte())
            _Index = index
            _StartAddr = startAddr
            _EndAddr = endAddr
            _LoopAddr = loopAddr
            'Get PCM data
            Dim length As UInteger =
                endAddr - startAddr
            If length > 0 Then
                ReDim _Data(length - 1)
                Array.Copy(pcmData, startAddr, _Data, 0, length)
            End If
            AddrIncs = New SortedSet(Of UInteger)
        End Sub
        Sub New(sample As Long, ByRef ch As Channel, ByRef pcmdata As Byte())
            Me.New(sample, ch.Bank + ch.StartAddr, ch.Bank + ch.EndAddr, ch.Bank + ch.LoopAddr, pcmdata)
        End Sub
        Public Overrides Function GetHashCode() As Integer
            Return HashCode.Combine(StartAddr, EndAddr, LoopAddr)
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
               EndAddr = b.EndAddr AndAlso
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
                .AppendLine($"Start Addr{sep}End Addr{sep}Loop Addr")
                .AppendLine($"{StartAddr}{sep}{EndAddr}{sep}{LoopAddr}")
            End With
            Return sb.ToString
        End Function
    End Structure

#End Region

    Public Class Chip

        Public Const Ch_Count As Integer = 16
        Public Const Std_Ext_Clock As Integer = 4000000
        Public Const Ext_Clock_Divider_Slow As Integer = 256 'for early SegaPCM games (e.g. Space Harrier)
        Public Const Ext_Clock_Divider_Fast As Integer = 128 'for later SegaPCM games
        Public Const Std_Sample_Clock As Integer = 31250
        Public Const Std_Clocks_Per_Sample As Integer = Std_Ext_Clock \ Std_Sample_Clock

#Region "Clock Frequencies"

        ''' <summary>
        ''' Master clock frequency in Hz (4000000 is standard).
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property MasterClockFreq() As Long

        ''' <summary>
        ''' Internal sample rate in Hz (31250).
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property IntSampleClockFreq() As Single

        ''' <summary>
        ''' External sample rate (e.g. 44100, 48000, etc.).
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property ExtSampleClockFreq() As Long

        ''' <summary>
        ''' Number of samples per external sample.
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property IntClocksPerExtClock() As Single

#End Region

#Region "Lookup Tables"

        Private _OutputLookupTable(255, 127) As Single
        Public ReadOnly Property OutputLookupTable As Single(,)
            Get
                Return _OutputLookupTable
            End Get
        End Property

        Protected Sub GenerateOutputLookupTable()
            'Pre-calculate all possible 12-bit output values for speed boost
            For samp As Integer = 0 To 255
                For vol As Integer = 0 To 127
                    OutputLookupTable(samp, vol) = GetOutput(samp, vol)
                Next
            Next
        End Sub

#End Region

        ''' <summary>
        ''' External sample ROM data
        ''' </summary>
        ''' <returns></returns>
        Public ReadOnly Property ROMData As Byte() 'List(Of Byte)
        Public ReadOnly Property BankShift As BankShiftValue
        Public ReadOnly Property BankMask As BankMaskValue
        Public ReadOnly Property Channel As Channel()

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

        ''' <summary>
        ''' Creates a new instance of a SegaPCM chip
        ''' </summary>
        ''' <param name="masterClockFreq">The external clock freqency in Hz (typically 4000000).</param>
        ''' <param name="spcmInterface">Interface settings that determine bank shift and bank mask.</param>
        ''' <param name="extSampleClockFreq">The external sample frequency for output in Hz (e.g. 44100).</param>
        ''' <param name="romData">The ROM data containing samples for playback.</param>
        Sub New(masterClockFreq As UInteger, extSampleClockFreq As UInteger, spcmInterface As UInteger, Optional resample As Boolean = True, Optional filter As Boolean = True, Optional cutoff As Single = 15625)
            'Set clock frequencies
            _MasterClockFreq = masterClockFreq
            _IntSampleClockFreq = Me.MasterClockFreq / Ext_Clock_Divider_Fast
            _ExtSampleClockFreq = extSampleClockFreq
            _IntClocksPerExtClock = IntSampleClockFreq / Me.ExtSampleClockFreq

            _BankShift = spcmInterface And &HF
            Select Case BankShift
                Case BankShiftValue.Size256K
                    ReDim _ROMData(262143)
                Case BankShiftValue.Size512K
                    ReDim _ROMData(524287)
                Case BankShiftValue.Size12M
                    ReDim _ROMData(1677215)
            End Select

            _BankMask = BankMaskValue.Mask70 Or ((spcmInterface >> 16) And &HFC)

            'Instantiate channels
            ReDim Channel(Ch_Count - 1)
            For chIdx As Integer = 0 To Channel.Length - 1
                Channel(chIdx) = New Channel(chIdx, Me)
            Next

            Output = {0, 0}

            GenerateOutputLookupTable()

            'For resampling
            _Resample = resample
            IntClocksPerExtClockIntegral = Math.Floor(IntClocksPerExtClock)
            IntClocksPerExtClockFractional = IntClocksPerExtClock - IntClocksPerExtClockIntegral
            ResampleRatio = Me.ExtSampleClockFreq / IntSampleClockFreq

            ReDim PrevSample(1)
            ReDim Sample(1)

            'For filtering
            _Filter = filter
            If filter Then
                FilterAlpha = Resampler.GetFirstOrderIIRLowPassFilterAlpha(IntSampleClockFreq, cutoff)
                'FilterAlpha = Resampler.GetSinglePoleIIRFilterAlpha(chip.IntSampleClockFreq, cutoff)
            End If

        End Sub
        Sub Reset()
        End Sub

        Public Sub WriteROMData(data As Byte(), addr As Integer)
            Array.Copy(data, 0, ROMData, addr, data.Length)
        End Sub

        Public Sub Write(addr As UShort, value As Byte)
            Dim addrLo As Byte =
                addr And &HFF
            Dim addrHi As Byte =
                (addr >> 8) And &HFF

            ' How to decode addresses from the LSB
            ' r cccc rrr
            ' r = register address
            ' c = channel number

            Dim chIdx As Integer =
                 (addrLo >> 3) And &HF
            Dim reg As RegAddr =
                 addrLo And &H87 '((addrLo And &H80) >> 4) + (addrLo And 7)

            Dim ch As Channel =
                Channel(chIdx)

            With ch
                Select Case reg
                    Case RegAddr.VolL
                        .Vol(0) = value And &H7F
                        RaiseEvent VolLChanged(ch)
                    Case RegAddr.VolR
                        .Vol(1) = value And &H7F
                        RaiseEvent VolRChanged(ch)
                    Case RegAddr.LoopAddrLo
                        .LoopAddrLo = value
                        RaiseEvent LoopAddrLoChanged(ch)
                    Case RegAddr.LoopAddrHi
                        .LoopAddrHi = value
                        RaiseEvent LoopAddrHiChanged(ch)
                    Case RegAddr.EndAddrHi
                        .EndAddrHi = value
                        RaiseEvent EndAddrChanged(ch)
                    Case RegAddr.AddrInc
                        .AddrInc = value
                        RaiseEvent AddrIncChanged(ch)
                    Case RegAddr.StartAddrLo
                        .StartAddrLo = value
                        RaiseEvent StartAddrLoChanged(ch)
                    Case RegAddr.StartAddrHi
                        .StartAddrHi = value
                        RaiseEvent StartAddrHiChanged(ch)
                    Case RegAddr.Ctrl
                        'The chip uses active low logic for enabling and looping channels
                        .Enabled = (value And 1) = 0
                        If .Enabled Then .Addr = .StartAddr << 8
                        .Looped = (value And 2) = 0
                        .Bank = (value And BankMask) << BankShift
                        RaiseEvent MuteLoopBankChanged(ch)
                    Case Else

                End Select

            End With

        End Sub

        Sub Advance()
            CalculateOutput()

            'Advance all channels
            For chIdx As Integer = 0 To Channel.Length - 1
                Dim ch As Channel =
                    Channel(chIdx)
                With ch
                    .Advance()
                    'Check for finished samples
                    If .StartAddr >= .EndAddr Then
                        RaiseEvent EndAddrReached(ch)
                    End If
                End With
            Next
        End Sub

        Private Sub CalculateOutput()
            _Output = {0, 0}
            For chIdx As Integer = 0 To Channel.Length - 1
                'CalculateChannelOutput(chIdx)
                With Channel(chIdx)
                    .CalculateOutput()
                    For spkIdx As Integer = 0 To 1
                        _Output(spkIdx) += .Output(spkIdx)
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

            For i As Integer = 0 To rawsamples.GetUpperBound(0)
                For j As Integer = 0 To 1
                    'Output is 12-bit unsigned
                    'In this implementation, the output is already converted to 32-bit float
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

        Public Event VolLChanged(ByRef ch As Channel) '(chIdx As Integer, vol As Integer)
        Public Event VolRChanged(ByRef ch As Channel) '(chIdx As Integer, vol As Integer)
        Public Event LoopAddrLoChanged(ByRef ch As Channel) '(chIdx As Integer, addrLo As Integer)
        Public Event LoopAddrHiChanged(ByRef ch As Channel) '(chIdx As Integer, addrHi As Integer)
        Public Event EndAddrChanged(ByRef ch As Channel) '(chIdx As Integer, addr As Integer)
        Public Event AddrIncChanged(ByRef ch As Channel) '(chIdx As Integer, inc As Integer)
        Public Event StartAddrLoChanged(ByRef ch As Channel) '(chIdx As Integer, addrLo As Integer)
        Public Event StartAddrHiChanged(ByRef ch As Channel) '(chIdx As Integer, addrHi As Integer)
        Public Event MuteLoopBankChanged(ByRef ch As Channel) '(chIdx As Integer, enabled As Boolean, looped As Boolean, bank As Integer)
        Public Event EndAddrReached(ByRef ch As Channel) '(chIdx As Integer)

    End Class

End Class