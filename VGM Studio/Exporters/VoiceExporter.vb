Imports VGM_Studio.VGM

Public Class VoiceExportOptions
    Inherits ExportOptions
End Class

Public Class VoiceExporter
    Inherits Exporter
    Public Overloads Property Options As VoiceExportOptions

    Sub New(ByRef vgmFile As VGMFile, options As VoiceExportOptions)
        MyBase.New(vgmFile, options)
        _Options = options
    End Sub
    Public Overrides Sub Save(fileName As String)

    End Sub

    'Store list of unique voices for FM chips using abstract FM.Voice class
    Public ReadOnly Property FMVoices As New Dictionary(Of UInteger, FM.Voice)


    'Store list of unique voices for OPN chips (YM2203, YM2612, YM2608, YM2610)
    Public ReadOnly Property OPNVoices As New Dictionary(Of Long, OPN.Voice)
    Public ReadOnly Property YM2612PCMVoices As New Dictionary(Of Long, YM2612.PCMVoice)
    Protected Overrides Sub ProcessSeekToDataOffset(seekOffset As Long)
        Static prevSeekOffset As Long = 0

        If DataBankOffset > 0 Then
            If YM2612Chip IsNot Nothing Then
                Dim thisVoice As New YM2612.PCMVoice(CurrentSample, prevSeekOffset, DataBankOffset, DataBank.ToArray)
                With thisVoice
                    If .StartAddr < .EndAddr Then
                        Dim add As Boolean = True
                        If thisVoice.Data Is Nothing Then
                            add = False
                        End If
                        If add Then
                            For Each key As Long In YM2612PCMVoices.Keys
                                If thisVoice = YM2612PCMVoices(key) Then
                                    add = False
                                    Exit For
                                End If
                            Next
                        End If
                        If add Then
                            Dim key As Long =
                                YM2612PCMVoices.Count 'CurrentSample
                            While YM2612PCMVoices.ContainsKey(key)
                                key += 1
                            End While
                            YM2612PCMVoices.Add(key, thisVoice)
                        End If
                    End If
                End With
            End If
        End If

        prevSeekOffset = seekOffset
        DataBankOffset = seekOffset
    End Sub

    Protected Overrides Sub OPN_KeyOnChanged(ByRef chip As OPN.Chip)
        MyBase.OPN_KeyOnChanged(chip)

        Dim keyOn As Byte =
            chip.Registers.KeyOn

        'Add current OPN voice if it is unique
        'Reject voices that are silenced
        If keyOn <> 0 AndAlso Not ThisOPNVoice.IsSilent Then
            Dim add As Boolean = True
            For Each key As Long In OPNVoices.Keys
                If OPNVoices(key) = ThisOPNVoice Then
                    add = False
                    Exit For
                End If
            Next
            If add Then
                Dim key As Long =
                    OPNVoices.Count 'CurrentSample
                While OPNVoices.ContainsKey(key)
                    key += 1
                End While
                OPNVoices.Add(key, ThisOPNVoice)
            End If
        End If
    End Sub

    'Store list of unique voices for OPM and OPP chips (YM2151 and YM2164)
    Public ReadOnly Property OPMVoices As New Dictionary(Of Long, OPM.Voice)
    Protected Overrides Sub OPM_KeyOnChanged(ByRef chip As OPM.Chip)
        MyBase.OPM_KeyOnChanged(chip)

        Dim keyOn As Byte =
            chip.Registers.KeyOn

        'Add current OPM voice if it is unique
        'Reject voices that are silenced
        If keyOn <> 0 AndAlso Not ThisOPMVoice.IsSilent Then
            Dim add As Boolean = True
            For Each key As Long In OPMVoices.Keys
                If ThisOPMVoice = OPMVoices(key) Then
                    add = False
                    Exit For
                End If
            Next
            If add Then
                Dim key As Long =
                    OPMVoices.Count 'CurrentSample
                While OPMVoices.ContainsKey(key)
                    key += 1
                End While
                OPMVoices.Add(key, ThisOPMVoice)
            End If
        End If
    End Sub


    Public ReadOnly Property SegaPCMVoices As New Dictionary(Of Long, SegaPCM.Voice)
    'Public ReadOnly Property SegaPCMVoiceChannels As New List(Of SortedSet(Of Integer))
    Protected Overrides Sub SegaPCMChip_MuteLoopOffsetChanged(ByRef ch As SegaPCM.Channel)
        MyBase.SegaPCMChip_MuteLoopOffsetChanged(ch)

        With ch
            'Added on 2019/01/28: Logging of unique addresses
            If .AddrInc > 0 Then

                Dim thisVoice As New SegaPCM.Voice(CurrentSample, ch, SegaPCMChip.ROMData)
                Dim voiceExists As Boolean = False

                For Each key As Long In SegaPCMVoices.Keys
                    If thisVoice = SegaPCMVoices(key) Then
                        Dim addrInc As UInteger = .AddrInc
                        With SegaPCMVoices(key).AddrIncs
                            If Not .Contains(addrInc) Then
                                .Add(addrInc)
                                'Also add speed as a percentage
                                Dim speed As Single =
                                    (addrInc / SegaPCM.ADDR_INC_MAX) * 100
                                SegaPCMVoices(key).Speeds.Add(speed)
                            End If
                        End With
                        voiceExists = True
                        Exit For
                    End If
                Next

                If Not voiceExists Then
                    SegaPCMVoices.Add(SegaPCMVoices.Count, thisVoice)
                    'SegaPCMVoiceChannels.Add(New SortedSet(Of Integer))
                End If

                'Dim index As Integer = 0

                'For Each key As Long In SegaPCMVoices.Keys

                '    If SegaPCMVoices(key) = thisVoice Then
                '        With SegaPCMVoiceChannels(index)
                '            If Not .Contains(ch.Index) Then
                '                .Add(ch.Index)
                '            End If
                '        End With
                '        Exit For
                '    End If

                '    index += 1

                'Next

            End If

        End With

    End Sub

    Public ReadOnly Property RF5CVoices As New Dictionary(Of Long, RF5C.Voice)
    Protected Overrides Sub RF5CChip_ChannelOutputEnabled(ByRef chip As RF5C.Chip)
        MyBase.RF5CChip_ChannelOutputEnabled(chip)

        For chIdx As Integer = 0 To 7
            Dim ch As RF5C.Channel =
                chip.Channel(chIdx)
            If ch.AddrInc = 0 Then Continue For

            ProcessRF5CVoice(ch)
        Next

    End Sub

    Protected Overrides Sub ProcessRF5CPCMRAMWrite(ByRef chip As RF5C.Chip, readAddr As UInteger, writeAddr As UInteger, data As Byte())
        MyBase.ProcessRF5CPCMRAMWrite(chip, readAddr, writeAddr, data)

        Static readAddrs As New List(Of UInteger)
        If readAddrs.Contains(readAddr) Then
            Exit Sub
        Else
            readAddrs.Add(readAddr)
        End If

        Dim ch As RF5C.Channel =
            chip.Channel(chip.ChannelLatch)
        If ch.AddrInc = 0 Then Exit Sub

        ProcessRF5CVoice(ch)
    End Sub

    Protected Sub ProcessRF5CVoice(ByRef ch As RF5C.Channel)
        If ch.AddrInc = 0 Then Exit Sub

        Dim voice As New RF5C.Voice(RF5CVoices.Count, ch, ch.Chip.PCMRAM)
        If voice.Data Is Nothing Then
            Exit Sub
        End If

        'Check if voice exists
        Dim voiceExists As Boolean = False
        For Each kvp As KeyValuePair(Of Long, RF5C.Voice) In RF5CVoices
            If kvp.Value = voice Then
                voice = kvp.Value
                voiceExists = True
                Exit For
            End If
        Next

        With voice
            .AddrIncs.Add(ch.AddrInc)
            'Add speed as a percentage
            Dim speed As Single =
                (ch.AddrInc / RF5C.ADDR_INC_MAX) * 100
            .Speeds.Add(speed)
        End With

        If Not voiceExists Then
            Dim key As Long =
                voice.Index '(CurrentSample << 8) Or ch.Index
            While RF5CVoices.ContainsKey(key)
                key += 1
            End While
            RF5CVoices.Add(key, voice)
        End If

    End Sub

End Class