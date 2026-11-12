Imports VGM_Studio.VGM

Public Class WavExportOptions
    Inherits ExportOptions
    Public Property PSGEmu As WAVExporter.PSGEmu = WAVExporter.PSGEmu.NukedYMPSG
    Public Property OPNEmu As WAVExporter.OPNEmu = WAVExporter.OPNEmu.NukedOPN2
    Public Property OPMEmu As WAVExporter.OPMEmu = WAVExporter.OPMEmu.NukedOPM
    Public Property IsSegaPCMTrackExport As Boolean = False
End Class

Public Class WAVExporter
    Inherits Exporter

    Public Overloads Property Options As WavExportOptions

    Const SegaPCM_Channel_Count As Integer =
        SegaPCM.Chip.Ch_Count

    Const Track_Count_Max = 32

    Private ReadOnly tempFileInfo(Track_Count_Max - 1) As IO.FileInfo
    Private ReadOnly tempFileStream(Track_Count_Max - 1) As IO.FileStream
    Private ReadOnly tempBinaryWriter(Track_Count_Max - 1) As IO.BinaryWriter

    Private ReadOnly sampleData As List(Of Single)
    Public Enum PSGEmu As Integer
        Internal = 0
        NukedYMPSG = 1
    End Enum
    Public Enum OPNEmu As Integer
        Internal = 0
        NukedOPN2 = 1
    End Enum
    Public Enum OPMEmu As Integer
        Internal = 0
        NukedOPM = 1
    End Enum

    Protected NukedYMPSGChipState As Nuked.YMPSG.ChipState
    Protected NukedOPN2ChipState As Nuked.OPN2.ChipState
    Protected NukedOPMChipState As Nuked.OPM.ChipState

    Protected NukedYMPSGChipState2 As Nuked.YMPSG.ChipState
    Protected NukedOPN2ChipState2 As Nuked.OPN2.ChipState
    Protected NukedOPMChipState2 As Nuked.OPM.ChipState

    Sub New(ByRef vgmfile As VGMFile, options As WavExportOptions)
        MyBase.New(vgmfile, options)

        Dim extSampleClockFreq As Integer = VGM_Sample_Rate * SampleRateScale

        _Options = options
        sampleData = New List(Of Single)

        If options.PSGEmu = PSGEmu.NukedYMPSG Then
            If vgmfile.SN76489Clock <> 0 Then
                NukedYMPSGChipState = New Nuked.YMPSG.ChipState
                Nuked.YMPSG.Init(NukedYMPSGChipState, extSampleClockFreq, vgmfile.SN76489Clock)
                Nuked.YMPSG.SetFilter(NukedYMPSGChipState, 3390)
            End If
        End If

        If options.OPNEmu = OPNEmu.NukedOPN2 Then
            If vgmfile.YM2612Clock <> 0 Then
                NukedOPN2ChipState = New Nuked.OPN2.ChipState
                Nuked.OPN2.SetChipType(Nuked.OPN2.ChipMode.ym2612)
                Nuked.OPN2.Reset(NukedOPN2ChipState, extSampleClockFreq, vgmfile.YM2612Clock)
                Nuked.OPN2.SetFilter(NukedOPN2ChipState, 5894.6)
                If vgmfile.IsDualYM2612 Then
                    NukedOPN2ChipState2 = New Nuked.OPN2.ChipState
                    Nuked.OPN2.Reset(NukedOPN2ChipState2, extSampleClockFreq, vgmfile.YM2612Clock)
                    Nuked.OPN2.SetFilter(NukedOPN2ChipState2, 5894.6)
                End If
            End If

            If vgmfile.YM2203Clock <> 0 Then
                NukedOPN2ChipState = New Nuked.OPN2.ChipState
                Nuked.OPN2.SetChipType(Nuked.OPN2.ChipMode.ym3438)
                Nuked.OPN2.Reset(NukedOPN2ChipState, extSampleClockFreq, vgmfile.YM2203Clock * 2)
                Nuked.OPN2.SetFilter(NukedOPN2ChipState, 5894.6)
                If vgmfile.IsDualYM2203 Then
                    NukedOPN2ChipState2 = New Nuked.OPN2.ChipState
                    Nuked.OPN2.Reset(NukedOPN2ChipState2, extSampleClockFreq, vgmfile.YM2203Clock * 2)
                    Nuked.OPN2.SetFilter(NukedOPN2ChipState2, 5894.6)
                End If
            End If
        End If

        If vgmfile.YM2151Clock <> 0 Then
            NukedOPMChipState = New Nuked.OPM.ChipState
            Nuked.OPM.Reset(NukedOPMChipState, 0, extSampleClockFreq, vgmfile.YM2151Clock)
            Nuked.OPM.SetFilter(NukedOPMChipState, 5894.6)
        End If

        If vgmfile.RF5C68Clock <> 0 Then
            RF5C68Chip?.Reset()
        End If

        If vgmfile.RF5C164Clock <> 0 Then
            RF5C164Chip?.Reset()
        End If

        If vgmfile.SegaPCMClock <> 0 Then
            SegaPCMChip?.Reset()
        End If

    End Sub

    Public Overrides Sub Save(fileName As String)
        If Options.IsSegaPCMTrackExport Then

            For chIdx As Integer = 0 To SegaPCM_Channel_Count - 1
                Dim fs As IO.FileStream =
                    tempFileStream(chIdx)
                Dim fi As IO.FileInfo =
                    tempFileInfo(chIdx)

                fs.Close()

                fs = New IO.FileStream(fi.FullName, IO.FileMode.Open, IO.FileAccess.Read)
                Using br As New IO.BinaryReader(fs)
                    Dim sampleData((br.BaseStream.Length \ 4) - 1) As Single
                    For sampleNum As Integer = 0 To sampleData.GetUpperBound(0)
                        sampleData(sampleNum) = br.ReadSingle
                    Next

                    Dim wf As New WAV.WAVFile(VGM_Sample_Rate, sampleData, 2)
                    fileName = fileName.Replace(".wav", "")
                    wf.Save($"{fileName} SegaPCM Ch {chIdx:00}.wav")

                End Using
                fs.Close()

                'Delete temp file
                fi.Delete()
            Next

        Else
            Dim wf As New WAV.WAVFile(VGM_Sample_Rate * SampleRateScale, sampleData.ToArray, 2)
            wf.Save(fileName)
        End If

    End Sub


    ' ********************************
    ' *** VGM command processors
    Protected Overrides Sub ProcessPSG(cmd As ChipCommand)
        Dim value As Byte = cmd.Value(0)

        If Options.PSGEmu = PSGEmu.NukedYMPSG Then
            Nuked.YMPSG.WriteBuffered(NukedYMPSGChipState, value)
        Else
            SN76489Chip?.Write(value)
        End If
    End Sub

    Protected Overrides Sub ProcessYM2612(port As OPN.PortIndex, cmd As ChipCommand)
        Dim addr As OPN.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If Options.OPNEmu = OPNEmu.NukedOPN2 Then
            Dim chip As Nuked.OPN2.ChipState =
                If(cmd.ChipIndex = 0, NukedOPN2ChipState, NukedOPN2ChipState2)
            Nuked.OPN2.WriteBuffered(chip, port * 2, cmd.Value(0))
            Nuked.OPN2.WriteBuffered(chip, (port * 2) + 1, cmd.Value(1))

            'Nuked.OPN2.WriteBuffered(NukedOPN2ChipState, (port * 2), cmd.Value(0))
            'Nuked.OPN2.WriteBuffered(NukedOPN2ChipState, (port * 2) + 1, cmd.Value(1))
        Else
            YM2612Chip?.Write(port, addr, value)
        End If
    End Sub

    Protected Overrides Sub ProcessYM2151(cmd As ChipCommand)
        Dim addr As Byte = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If True Then 'Options.OPMEmu = OPMEmu.NukedOPM Then
            'Nuked.OPM.WriteBuffered(NukedOPMChipState, 0, addr)
            'Nuked.OPM.WriteBuffered(NukedOPMChipState, 1, value)
            Nuked.OPM.WriteBuffered(NukedOPMChipState, 0, addr)
            Nuked.OPM.WriteBuffered(NukedOPMChipState, 1, value)
        Else
            'YM2151Chip?.Write(port, addr, value)
        End If
    End Sub

    Protected Overrides Sub ProcessYM2203(cmd As ChipCommand)
        Dim addr As OPN.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        'Don't process commands meant for the SSG part of the chip
        If addr < &H20 Then
            Exit Sub
        End If

        If Options.OPNEmu = OPNEmu.NukedOPN2 Then
            Dim chip As Nuked.OPN2.ChipState =
                If(cmd.ChipIndex = 0, NukedOPN2ChipState, NukedOPN2ChipState2)
            Nuked.OPN2.WriteBuffered(chip, 0, cmd.Value(0))
            Nuked.OPN2.WriteBuffered(chip, 1, cmd.Value(1))

            'If cmd.ChipIndex = 1 Then
            '    Nuked.OPN2.WriteBuffered(NukedOPN2ChipState2, 0, cmd.Value(0))
            '    Nuked.OPN2.WriteBuffered(NukedOPN2ChipState2, 1, cmd.Value(1))
            'Else
            '    Nuked.OPN2.WriteBuffered(NukedOPN2ChipState, 0, cmd.Value(0))
            '    Nuked.OPN2.WriteBuffered(NukedOPN2ChipState, 1, cmd.Value(1))
            'End If
        End If
    End Sub


    Protected Overrides Sub ProcessWriteFromDataBank()
        Dim data As Byte =
            DataBank(DataBankOffset)

        If Options.OPNEmu = OPNEmu.NukedOPN2 Then
            Nuked.OPN2.WriteBuffered(NukedOPN2ChipState, 0, OPN.RegAddr.DACData)
            Nuked.OPN2.WriteBuffered(NukedOPN2ChipState, 1, data)
        Else
            'TODO: Fix this so it works with other types of data
            YM2612Chip?.Write(0, OPN.RegAddr.DACData, data)
        End If

        DataBankOffset += 1
    End Sub


    Protected Overrides Sub ProcessWait(sampleCount As Integer)
        MyBase.ProcessWait(sampleCount)

        If Options.IsSegaPCMTrackExport Then
            'AddSegaPCMSamples(sampleCount)
        Else
            sampleCount *= SampleRateScale
            If sampleCount <= 0 Then
                Exit Sub
            End If

            Dim samples(sampleCount - 1, 1) As Single

            If SN76489Chip IsNot Nothing Then

                If Options.PSGEmu = PSGEmu.NukedYMPSG Then
                    Dim ym7101samples(sampleCount - 1) As Integer
                    Nuked.YMPSG.GenerateStream(NukedYMPSGChipState, ym7101samples, sampleCount)
                    For curSample As Integer = 0 To ym7101samples.GetUpperBound(0)
                        For i As Integer = 0 To 1
                            samples(curSample, i) += ym7101samples(curSample) / 16384
                        Next
                    Next

                Else
                    Dim sn76489samples(sampleCount - 1, 1) As Single
                    SN76489Chip?.GenerateSamples(sn76489samples)
                    For curSample As Integer = 0 To sn76489samples.GetUpperBound(0)
                        For i As Integer = 0 To 1
                            samples(curSample, i) += sn76489samples(curSample, i) * 0.75
                        Next
                    Next
                End If

            End If

            If YM2612Chip IsNot Nothing OrElse YM2203Chip IsNot Nothing Then

                If Options.OPNEmu = OPNEmu.NukedOPN2 Then

                    Dim dual As Boolean =
                        VGMFile.IsDualYM2612 OrElse VGMFile.IsDualYM2203
                    Dim idx As Integer = 0
                    Dim chip As Nuked.OPN2.ChipState
                    Do
                        Dim ym3438samples(sampleCount - 1, 1) As Integer
                        chip = If(idx = 0, NukedOPN2ChipState, NukedOPN2ChipState2)
                        Nuked.OPN2.GenerateStream(chip, ym3438samples, sampleCount)
                        For curSample As Integer = 0 To ym3438samples.GetUpperBound(0)
                            For i As Integer = 0 To 1
                                samples(curSample, i) += ym3438samples(curSample, i) / If(YM2203Chip IsNot Nothing, 32767, 16383)
                            Next
                        Next
                        If dual Then
                            idx += 1
                            dual = False
                        Else
                            Exit Do
                        End If
                    Loop
                Else
                    'Dim ym2612samples(sampleCount - 1, 1) As Single
                    'YM2612Chip?.DU.GenerateSamples(ym2612samples)
                    'For curSample As Integer = 0 To ym2612samples.GetUpperBound(0)
                    '    For i As Integer = 0 To 1
                    '        samples(curSample, i) += ym2612samples(curSample, i) * 0.5
                    '    Next
                    'Next
                End If

            End If

            If YM2151Chip IsNot Nothing Then
                Dim opmSamples(sampleCount - 1, 1) As Integer
                Nuked.OPM.GenerateStream(NukedOPMChipState, opmSamples, sampleCount)
                For curSample As Integer = 0 To opmSamples.GetUpperBound(0)
                    For i As Integer = 0 To 1
                        samples(curSample, i) += opmSamples(curSample, i) / 262143
                    Next
                Next

            End If

            If RF5C68Chip IsNot Nothing Then
                Dim rf5cSamples(sampleCount - 1, 1) As Single
                RF5C68Chip?.GenerateSamples(rf5cSamples)
                For curSample As Integer = 0 To rf5cSamples.GetUpperBound(0)
                    For i As Integer = 0 To 1
                        samples(curSample, i) += rf5cSamples(curSample, i)
                    Next
                Next
            End If

            If RF5C164Chip IsNot Nothing Then
                Dim rf5cSamples(sampleCount - 1, 1) As Single
                RF5C164Chip?.GenerateSamples(rf5cSamples)
                For curSample As Integer = 0 To rf5cSamples.GetUpperBound(0)
                    For i As Integer = 0 To 1
                        samples(curSample, i) += rf5cSamples(curSample, i)
                    Next
                Next
            End If

            If SegaPCMChip IsNot Nothing Then
                Dim spcmsamples(sampleCount - 1, 1) As Single
                SegaPCMChip?.GenerateSamples(spcmsamples)
                For curSample As Integer = 0 To spcmsamples.GetUpperBound(0)
                    For i As Integer = 0 To 1
                        samples(curSample, i) += spcmsamples(curSample, i)
                    Next
                Next

            End If

            'Interleave samples
            With sampleData
                For i As Integer = 0 To sampleCount - 1
                    For j As Integer = 0 To 1
                        .Add(samples(i, j))
                    Next
                Next
            End With

        End If

    End Sub

End Class