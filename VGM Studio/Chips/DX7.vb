Imports System.Text

Public Class DX7

    ''' <summary>
    ''' Operator data for OPSVoice
    ''' </summary>
    Public Class Op
        Public ReadOnly Property Index As Long

        Public R1 As Byte   'Attack
        Public R2 As Byte   'Decay
        Public R3 As Byte   'Sustain
        Public R4 As Byte   'Release

        Public L1 As Byte   'Attack
        Public L2 As Byte   'Decay
        Public L3 As Byte   'Sustain
        Public L4 As Byte   'Release

        Public BP As Byte   'Breakpoint
        Public LD As Byte   'Left Depth
        Public RD As Byte   'Right Depth
        Public LC As Byte   'Left Curve
        Public RC As Byte   'Right Curve

        Public PD As Byte   'Phase Detune
        Public RS As Byte   'Rate Scaling

        Public TS As Byte   'Touch Sensitivity
        Public AMS As Byte  'Amplitude Modulation Sensitivity

        Public TL As Byte   'Total Level

        Public PC As Byte   'Phase Coarse
        Public PM As Byte   'Phase Mode
        Public PF As Byte   'Phase PF

        Sub New(index As Long)
            Me.Index = index

            R1 = 99
            R2 = 99
            R3 = 99
            R4 = 99

            L1 = 99
            L2 = 99
            L3 = 99
            L4 = 0

            BP = 39    'Note C3
            LD = 0
            RD = 0
            LC = 0
            RC = 0

            PD = 0
            RS = 0

            TS = 0
            AMS = 0

            TL = 99

            PC = 1
            PM = 0
            PF = 0

        End Sub

        Public Function ToVmemFormat() As Byte()
            Dim ba(16) As Byte
            ba(0) = R1
            ba(1) = R2
            ba(2) = R3
            ba(3) = R4
            ba(4) = L1
            ba(5) = L2
            ba(6) = L3
            ba(7) = L4
            ba(8) = BP
            ba(9) = LD
            ba(10) = RD
            ba(11) = (RC << 2) Or LC
            ba(12) = (PD << 3) Or RS
            ba(13) = (TS << 2) Or AMS
            ba(14) = TL
            ba(15) = (PC << 1) Or PM
            ba(16) = PF
            Return ba
        End Function

    End Class


    ''' <summary>
    ''' Voice for Yamaha OPS synthesizers such as the DX7
    ''' </summary>
    Public Class Voice
        Public ReadOnly Property Index As Long

        Protected _VoiceName As String
        Public Property VoiceName As String
            Get
                Return _VoiceName
            End Get
            Protected Set(value As String)
                _VoiceName = value.ToString.PadRight(10).Substring(0, 10)
            End Set
        End Property

        Dim Op As Op()

        Public PR1 As Byte  'Pitch Rate (Attack)
        Public PR2 As Byte  'Pitch Rate (Decay)
        Public PR3 As Byte  'Pitch Rate (Sustain)
        Public PR4 As Byte  'Pitch Rate (Release)

        Public PL1 As Byte  'Pitch Level (Attack)
        Public PL2 As Byte  'Pitch Level (Decay)
        Public PL3 As Byte  'Pitch Level (Sustain)
        Public PL4 As Byte  'Pitch Level (Release)

        Public ALS As Byte  'Algorithm Select

        Public OPI As Byte  'Oscillator Phase Init
        Public FBL As Byte  'Feedback Level

        Public LFS As Byte  'LFO Speed
        Public LFD As Byte  'LFO Delay
        Public LPMD As Byte 'Pitch Modulation Depth
        Public LAMD As Byte 'Amplitude Modulation Depth
        Public LPMS As Byte 'LFO Phase Mosulation Sensitivity

        Public Enum WaveformType As Byte
            Triangle = 0
            SawDown = 1
            SawUp = 2
            Square = 3
            Since = 4
            SampleAndHold = 5
        End Enum

        Public LFW As WaveformType  'LFO Waveform
        Public LFKS As Byte         'LFO Key Sync
        Public TRNS As Byte         'Transpose

        'Tables for converting YM operator values to DX7 operator values

        'Algorithm select
        Shared ReadOnly YMALSTable As Byte() =
            {0, 13, 7, 6, 4, 21, 30, 31}

        Public ReadOnly YMTLTable() As Byte = {
            99, 98, 97, 96, 95, 94, 93, 92, 91, 90, 89, 88, 87, 86, 85, 84,
            83, 82, 81, 80, 79, 78, 77, 76, 75, 74, 73, 72, 71, 70, 69, 68,
            67, 66, 65, 64, 63, 62, 61, 60, 59, 58, 57, 56, 55, 54, 53, 52,
            51, 50, 49, 48, 47, 46, 45, 44, 43, 42, 41, 40, 39, 38, 37, 36,
            35, 34, 33, 32, 31, 30, 29, 28, 27, 26, 25, 24, 23, 22, 21, 20,
            19, 19, 18, 18, 17, 17, 16, 16, 15, 15, 14, 14, 13, 13, 12, 12,
            11, 11, 10, 10, 9, 9, 8, 8, 7, 7, 6, 6, 5, 5, 4, 4,
            3, 3, 2, 2, 1, 1, 0, 0, 0, 0, 0, 0, 0, 0, 0, 0
        }

        Public ReadOnly YMARTable() As Byte = {
            0, 5, 9, 13, 17, 21, 24, 28,
            31, 35, 38, 41, 45, 48, 51, 54,
            58, 61, 64, 67, 70, 73, 76, 79,
            82, 85, 88, 91, 93, 96, 98, 99
        }

        Public ReadOnly YMDRSRTable() As Byte = {
            0, 3, 6, 10, 13, 16, 19, 22,
            26, 29, 32, 35, 38, 42, 45, 48,
            51, 54, 58, 61, 64, 67, 70, 73,
            77, 80, 83, 86, 89, 93, 96, 99
        }

        Public ReadOnly YMRRTable() As Byte = {
            0, 7, 13, 20, 26, 33, 40, 46,
            53, 59, 66, 73, 79, 86, 92, 99
        }

        'Phase detune
        Shared ReadOnly YMDTTable As Byte() =
            {7, 8, 9, 10, 7, 6, 5, 4}

        'Amplitude modulation depth
        Shared ReadOnly OPNAMDTable As Byte() =
            {0, 35, 75, 99}

        'LFO speed
        Shared ReadOnly OPNLFSTable As Byte() =
            {26, 29, 30, 31, 32, 35, 64, 75}

        Sub New(index As Long, Optional voiceName As String = "Voice")
            Me.Index = index
            Me.VoiceName = $"{voiceName} {index:00}"

            ReDim Op(5)
            For opIdx As Integer = 0 To 5
                Op(opIdx) = New Op(opIdx)
            Next

            'Pitch Rate (PR)
            PR1 = 99
            PR2 = 99
            PR3 = 99
            PR4 = 99

            'Pitch level (PL)
            PL1 = 50
            PL2 = 50
            PL3 = 50
            PL4 = 50

            'Algorithm Select (ALS)
            ALS = 0

            'Oscillator Phase Init (OPI) Feedback Level (FBL)
            OPI = 1
            FBL = 0

            'Low Frequency Oscillator (LFO)
            LFS = 0
            LFD = 0
            LPMD = 0
            LAMD = 0
            LPMS = 0
            LFW = WaveformType.Triangle
            LFKS = 1 'Key Sync On

            'Transpose
            TRNS = 24
        End Sub

        Sub New(index As Long, voice As FM.Voice)
            Me.New(index)

            If TypeOf (voice) Is OPM.Voice Then
                Dim opmVoice As OPM.Voice =
                    voice
                NewOPMVoice(index, opmVoice)
            ElseIf TypeOf voice Is OPN.Voice Then
                Dim opnVoice As OPN.Voice =
                    voice
                NewOPNVoice(index, opnVoice)
            End If

        End Sub

        Sub NewOPMVoice(index As Long, voice As OPM.Voice)
            VoiceName = $"OPM {index:00}"

            'DX7 operators are in the reverse order of YM operators
            For dx7opIdx As Integer = 5 To 0 Step -1
                With Op(dx7opIdx)
                    'Set ops 1 and 2 to silence
                    If dx7opIdx < 2 Then
                        .TL = 0
                        Continue For
                    End If

                    Dim ymopIdx As Integer =
                        5 - dx7opIdx

                    'Remap operators for ym algo 2
                    If voice.Con = 2 Then
                        Select Case dx7opIdx
                            Case 5
                                ymopIdx = 1
                            Case 4
                                ymopIdx = 2
                            Case 3
                                ymopIdx = 0
                            Case 2
                                ymopIdx = 3
                        End Select
                    End If

                    'Get YM rates and levels
                    Dim ar, dr, sr, rr, sl As Integer
                    Dim ks As Integer
                    With voice
                        ar = .AR(ymopIdx)
                        dr = .DR(ymopIdx)
                        sr = .SR(ymopIdx)
                        rr = .RR(ymopIdx)
                        sl = .SL(ymopIdx)
                        ks = .KS(ymopIdx)
                    End With

                    'Set DX7 rates
                    If ar = 0 Then
                        .R1 = 0
                        .L1 = 0
                        .TL = 0
                    ElseIf ar = 30 AndAlso ks > 0 OrElse ar = 31 Then
                        .R1 = 99
                    Else
                        .R1 = YMARTable(ar)
                    End If

                    .R2 = YMDRSRTable(dr)
                    .R3 = YMDRSRTable(sr)
                    .R4 = YMRRTable(rr)

                    'Set DX7 rates
                    .L2 = Math.Clamp(Math.Round(99 - (sl * 6.6)), 0, 99)
                    .L3 = 0

                    .PD = YMDTTable(voice.DT1(ymopIdx))
                    .RS = Math.Clamp(voice.KS(ymopIdx) * 2, 0, 7)

                    .AMS = voice.AMS

                    'Get YM TL and set DX TL
                    Dim tl As Integer =
                        voice.TL(ymopIdx)
                    .TL = YMTLTable(tl)

                    'OPM ONLY: Set coarse and fine pitch
                    Dim mul As Single =
                        voice.Mul(ymopIdx)
                    mul = If(mul = 0, 0.5, mul)

                    Dim tune As Single =
                        mul * OPM.GetDT2Multiplier(voice.DT2(ymopIdx))
                    Dim coarse As Integer =
                        Math.Floor(tune)
                    Dim fine As Integer =
                        (tune - coarse) * 99

                    .PC = coarse
                    .PF = fine

                End With
            Next

            'OPM and OPN
            ALS = YMALSTable(voice.Con)
            OPI = 1
            FBL = voice.FL And 7

            'OPM ONLY
            LFS = Math.Clamp(Math.Round(99 * Math.Pow(voice.LFOFreq / 255, 0.72)), 0, 99)
            LPMD = Math.Round(99 * Math.Pow(voice.PMD / 125, 0.85))
            LAMD = Math.Round(99 * (voice.AMD / 125))

            'OPN and OPM
            LPMS = voice.PMS

            'OPM ONLY
            Select Case voice.Waveform
                Case OPM.WaveformType.Sawtooth
                    LFW = WaveformType.SawDown
                Case OPM.WaveformType.Square
                    LFW = WaveformType.Square
                Case OPM.WaveformType.Triangle
                    LFW = WaveformType.Triangle
                Case OPM.WaveformType.Noise
                    LFW = WaveformType.SampleAndHold
            End Select
        End Sub

        Sub NewOPNVoice(index As Long, voice As OPN.Voice)
            VoiceName = $"OPN {index:00}"

            'DX7 operators are in the reverse order of YM operators
            For dx7opIdx As Integer = 5 To 0 Step -1
                With Op(dx7opIdx)
                    'Set ops 1 and 2 to silence
                    If dx7opIdx < 2 Then
                        .TL = 0
                        Continue For
                    End If

                    Dim ymopIdx As Integer =
                        5 - dx7opIdx

                    'Remap operators for ym algo 2
                    If voice.Con = 2 Then
                        Select Case dx7opIdx
                            Case 5
                                ymopIdx = 1
                            Case 4
                                ymopIdx = 2
                            Case 3
                                ymopIdx = 0
                            Case 2
                                ymopIdx = 3
                        End Select
                    End If

                    'Get YM rates and levels
                    Dim ar, dr, sr, rr, sl As Integer
                    Dim ks As Integer
                    With voice
                        ar = .AR(ymopIdx)
                        dr = .DR(ymopIdx)
                        sr = .SR(ymopIdx)
                        rr = .RR(ymopIdx)
                        sl = .SL(ymopIdx)
                        ks = .KS(ymopIdx)
                    End With

                    'Set DX7 rates
                    If ar = 0 Then
                        .R1 = 0
                        .L1 = 0
                        .TL = 0
                    ElseIf ar = 30 AndAlso ks > 0 OrElse ar = 31 Then
                        .R1 = 99
                    Else
                        .R1 = YMARTable(ar)
                    End If
                    .R2 = YMDRSRTable(dr)
                    .R3 = YMDRSRTable(sr)
                    .R4 = YMRRTable(rr)

                    'Set DX7 levels
                    .L2 = Math.Clamp(Math.Round(99 - (sl * 6.6)), 0, 99)
                    .L3 = 0

                    .PD = YMDTTable(voice.DT(ymopIdx))
                    .RS = Math.Clamp(voice.KS(ymopIdx) * 2, 0, 7)

                    .AMS = voice.AMS

                    'Get YM TL and set DX TL
                    Dim tl As Integer =
                        voice.TL(ymopIdx)
                    .TL = YMTLTable(tl)

                    'OPN ONLY: Set coarse and fine tune for special mode channel 3
                    If voice.IsCh3Sp Then
                        Dim hz As Integer =
                            voice.Ch3SpHz(ymopIdx)
                        Dim coarse As Integer = 0
                        If (hz >= 1) And (hz < 10) Then
                            coarse = 0
                        ElseIf (hz >= 10) And (hz < 100) Then
                            coarse = 1
                        ElseIf (hz >= 100) And (hz < 1000) Then
                            coarse = 2
                        ElseIf hz >= 1000 Then
                            coarse = 3
                        End If
                        Dim fine As Integer =
                            Math.Round(((hz / Math.Pow(10, coarse)) - 1) / 8.772 * 100)
                        .PC = coarse
                        .PM = 1
                        .PF = fine
                    Else
                        .PC = voice.Multi(ymopIdx)
                    End If

                End With
            Next

            'OPM and OPN
            ALS = YMALSTable(voice.Con)
            OPI = 1
            FBL = voice.FB And 7

            'OPN ONLY
            LFS = OPNLFSTable(voice.LFOFreq)
            LAMD = OPNAMDTable(voice.AMS)

            'OPM and OPN
            LPMS = voice.PMS
        End Sub

        Public Function ToVmemBytes() As Byte()
            Dim vmem(127) As Byte

            For opIdx = 5 To 0 Step -1
                Dim offset As Integer =
                    (5 - opIdx) * 17
                Dim ba As Byte() =
                    Op(opIdx).ToVmemFormat
                Array.Copy(ba, 0, vmem, offset, ba.Length)
            Next

            vmem(102) = PR1
            vmem(103) = PR2
            vmem(104) = PR3
            vmem(105) = PR4

            vmem(106) = PL1
            vmem(107) = PL2
            vmem(108) = PL3
            vmem(109) = PL4

            vmem(110) = ALS

            vmem(111) = (OPI << 3) Or FBL

            vmem(112) = LFS
            vmem(113) = LFD
            vmem(114) = LPMD
            vmem(115) = LAMD
            vmem(116) = (LPMS << 4) Or (LFW << 1) Or LFKS

            vmem(117) = TRNS

            Dim name As String =
                VoiceName.PadRight(10).Substring(0, 10)
            Dim nameBytes As Byte() =
                Encoding.ASCII.GetBytes(name)
            Array.Copy(nameBytes, 0, vmem, 118, 10)

            Return vmem

        End Function

    End Class

    Public Structure SYXFile
        Public ReadOnly Data As Byte()
        Public Enum FormatCode As Byte
            VMEMPacked = 9
        End Enum

        Sub New(voices As Voice())
            'Create SYX data
            ReDim Data(4103)

            Data(0) = &H_F0 'SysEx start
            Data(1) = &H_43 'Manufacturer ID: Yamaha
            Data(2) = &H_00 'Device MIDI channel 1
            Data(3) = &H_09 'Format code: 32 voices VMEM packed bulk data
            Data(4) = &H_20 'Byte count MSB
            Data(5) = &H_00 'Byte count LSB

            'Add a maximum of 32 voices
            Dim offset As Integer = 6
            For i As Integer = 0 To Math.Min(voices.Count - 1, 31)
                Dim vmemBytes As Byte() =
                    voices(i).ToVmemBytes
                Array.Copy(vmemBytes, 0, Data, offset, vmemBytes.Length)
                offset += 128
            Next

            'Calculate checksum
            Dim sum As Integer = 0
            For i As Integer = 6 To 4101
                sum += Data(i)
            Next
            sum = -sum
            sum = sum And &H_7F
            Dim checksum As Byte =
                        CByte(sum)

            Data(4102) = checksum 'Checksum

            Data(4103) = &H_0F7 'End of data
        End Sub

    End Structure
    Public Class Chip

    End Class

End Class


