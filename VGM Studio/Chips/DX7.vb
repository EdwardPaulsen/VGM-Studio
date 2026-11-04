Imports System.Text

Namespace DX7

    ''' <summary>
    ''' Operator data for OPSVoice
    ''' </summary>
    Public Structure Op
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

    End Structure


    ''' <summary>
    ''' Voice for Yamaha OPS synthesizers such as the DX7
    ''' </summary>
    Public Structure Voice
        Public ReadOnly Property Index As Long
        Public Property VoiceName As String

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

        Sub New(index As Long, Optional voiceName As String = "Voice")
            Me.Index = index
            Me.VoiceName = voiceName.ToString.PadRight(10).Substring(0, 10)

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

        Sub New(index As Long, voice As OPN.Voice)
            Me.New(index, $"OPN {index:X6}")

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

                    'Rates
                    Dim ar, dr, sr, rr, sl As Integer
                    With voice
                        ar = .AR(ymopIdx)
                        dr = .DR(ymopIdx)
                        SR = .SR(ymopIdx)
                        rr = .RR(ymopIdx)
                        sl = voice.SL(ymopIdx)
                    End With

                    If ar = 31 Then
                        .R1 = 99
                    Else
                        .R1 = Math.Clamp(Math.Round(3.1 * ar + 2), 0, 99) 'OPNR1Table(voice.AR(ymopIdx))
                    End If

                    'Adjust rate based on sustain level
                    Dim slAdj As Single =
                        (15 - sl) / 2

                    .R2 = Math.Clamp(Math.Round(3 * dr + slAdj), 0, 99) 'OPNR2Table(voice.DR(ymopIdx), voice.SL(ymopIdx))
                    .R3 = Math.Clamp(Math.Round(3 * sr + slAdj), 0, 99) 'OPNR3Table(voice.SR(ymopIdx), voice.SL(ymopIdx))
                    .R4 = Math.Clamp(Math.Round(6.2 * rr + 6 - (sl / 2)), 0, 99) 'OPNR4Table(voice.RR(ymopIdx), voice.SL(ymopIdx))

                    '.R1 = Math.Round(voice.AR(ymopIdx) * (99 / 31))
                    '.R2 = Math.Round(voice.DR(ymopIdx) * (99 / 31))
                    '.R3 = Math.Round(voice.SR(ymopIdx) * (99 / 31))
                    '.R4 = Math.Round(((voice.RR(ymopIdx) * 2) + 1) * (99 / 31))

                    .L2 = Math.Clamp(Math.Round(99 - (sl * 6.6)), 0, 99)
                    .L3 = 0

                    .PD = OPNPDTable(voice.DT(ymopIdx))
                    .RS = Math.Clamp(voice.KS(ymopIdx) * 2, 0, 7)

                    .AMS = voice.AMS

                    Dim tl As Integer =
                        voice.TL(ymopIdx)
                    If tl >= 96 Then
                        .TL = 0
                    Else
                        .TL = Math.Round(99 - 0.7795 * tl)
                        '.TL =  (127 - tl) * (99 / 127)
                        '.TL = Math.Round(99 * Math.Pow(1 - (tl / 127), 0.65))
                    End If

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

            ALS = OPNALSTable(voice.Con)

            OPI = 1
            FBL = voice.FB And 7

            LFS = OPNLFSTable(voice.LFOFreq)
            LAMD = OPNAMDTable(voice.AMS)
            LPMS = voice.PMS
        End Sub

        'Phase distortion
        Shared ReadOnly OPNPDTable As Byte() =
            {7, 8, 9, 10, 7, 6, 5, 4}

        'Algorithm select
        Shared ReadOnly OPNALSTable As Byte() =
            {0, 13, 7, 6, 4, 21, 30, 31}

        'Amplitude modulateion depth
        Shared ReadOnly OPNAMDTable As Byte() =
            {0, 35, 75, 99}

        'LFO speed
        Shared ReadOnly OPNLFSTable As Byte() =
            {26, 29, 30, 31, 32, 35, 64, 75}

        Sub New(index As Long, voice As OPM.Voice)
            Me.New(index, $"OPM {index:X6}")

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

                    'Rates
                    Dim ar, dr, sr, rr, sl As Integer
                    With voice
                        ar = .AR(ymopIdx)
                        dr = .DR(ymopIdx)
                        sr = .SR(ymopIdx)
                        rr = .RR(ymopIdx)
                        sl = .SL(ymopIdx)
                    End With

                    If ar = 31 Then
                        .R1 = 99
                    Else
                        .R1 = Math.Clamp(Math.Round(3.1 * ar + 2), 0, 99)
                    End If

                    'Adjust rate based on decay 1 level
                    Dim slAdj As Single =
                        (15 - slAdj) / 2

                    .R2 = Math.Clamp(Math.Round(3 * dr + slAdj), 0, 99)
                    .R3 = Math.Clamp(Math.Round(3 * sr + slAdj), 0, 99)
                    .R4 = Math.Clamp(Math.Round(6.2 * rr + 6 - (sl / 2)), 0, 99)

                    'Levels
                    .L2 = Math.Clamp(Math.Round(99 - (sl * 6.6)), 0, 99)
                    .L3 = 0

                    .PD = OPNPDTable(voice.DT1(ymopIdx))
                    .RS = Math.Clamp(voice.KS(ymopIdx) * 2, 0, 7)

                    .AMS = voice.AMS

                    Dim tl As Integer =
                        voice.TL(ymopIdx)
                    If tl >= 96 Then
                        .TL = 0
                    Else
                        .TL = Math.Round(99 - 0.7795 * tl)
                        '.TL =  (127 - tl) * (99 / 127)
                        '.TL = Math.Round(99 * Math.Pow(1 - (tl / 127), 0.65))
                    End If

                    .PC = voice.Mul(ymopIdx)

                End With
            Next

            ALS = OPNALSTable(voice.Con)

            OPI = 1
            FBL = voice.FL And 7

            LFS = Math.Clamp(Math.Round(99 * Math.Pow(voice.LFOFreq / 255, 0.72)), 0, 99)
            LPMD = Math.Round(99 * Math.Pow(voice.PMD / 125, 0.85))
            LAMD = Math.Round(99 * (voice.AMD / 125))
            LPMS = voice.PMS
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

    End Structure

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

End Namespace


