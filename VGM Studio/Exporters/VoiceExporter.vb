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
                            Dim newKey As Long = CurrentSample
                            While YM2612PCMVoices.ContainsKey(newKey)
                                newKey += 1
                            End While
                            YM2612PCMVoices.Add(newKey, thisVoice)
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
        If keyOn <> 0 AndAlso Not ThisOPNVoice.IsSilent Then
            Dim add As Boolean = True
            For Each key As Long In OPNVoices.Keys
                If ThisOPNVoice = OPNVoices(key) Then
                    add = False
                    Exit For
                End If
            Next
            If add Then
                Dim newKey As Long = CurrentSample
                While OPNVoices.ContainsKey(newKey)
                    newKey += 1
                End While
                OPNVoices.Add(newKey, ThisOPNVoice)
            End If
        End If
    End Sub

    Public ReadOnly Property OPMVoices As New Dictionary(Of Long, OPM.Voice)
    Protected Overrides Sub OPM_KeyOnChanged(ByRef chip As OPM.Chip)
        MyBase.OPM_KeyOnChanged(chip)

        Dim keyOn As Byte =
                chip.Registers.KeyOn

        'Add current OPN voice if it is unique
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
                Dim newKey As Long = CurrentSample
                While OPMVoices.ContainsKey(newKey)
                    newKey += 1
                End While
                OPMVoices.Add(newKey, ThisOPMVoice)
            End If
        End If
    End Sub


    Public ReadOnly Property SegaPCMVoices As New Dictionary(Of Long, SegaPCM.Voice)
    Public ReadOnly Property SegaPCMVoiceChannels As New List(Of SortedSet(Of Integer))
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
                    SegaPCMVoices.Add(CurrentSample, thisVoice)
                    SegaPCMVoiceChannels.Add(New SortedSet(Of Integer))
                End If

                Dim index As Integer = 0

                For Each key As Long In SegaPCMVoices.Keys

                    If SegaPCMVoices(key) = thisVoice Then
                        With SegaPCMVoiceChannels(index)
                            If Not .Contains(ch.Index) Then
                                .Add(ch.Index)
                            End If
                        End With
                        Exit For
                    End If

                    index += 1

                Next

            End If

        End With

    End Sub

    Public ReadOnly Property RFC5164Voices As New Dictionary(Of Long, RFC5164.Voice)
    Public ReadOnly Property RFC5164VoiceChannels As New List(Of SortedSet(Of Integer))
    Protected Overrides Sub RFC5164Chip_ChannelOutputEnabled(value As Byte)
        MyBase.RFC5164Chip_ChannelOutputEnabled(value)

        For chIdx As Integer = 0 To 7
            With RFC5164Chip.Channel(chIdx)
                'Added on 2026/10/07: Logging of unique addresses
                If .AddrInc > 0 Then

                    Dim thisVoice As New RFC5164.Voice(CurrentSample, RFC5164Chip.Channel(chIdx), RFC5164Chip.PCMRAM)
                    Dim voiceExists As Boolean = False

                    For Each key As Long In RFC5164Voices.Keys
                        Dim addrInc As UInteger = .AddrInc
                        If thisVoice = RFC5164Voices(key) Then
                            With RFC5164Voices(key).AddrIncs
                                If Not .Contains(addrInc) Then
                                    .Add(addrInc)
                                    'Also add speed as a percentage
                                    Dim speed As Single =
                                        (addrInc / RFC5164.ADDR_INC_MAX) * 100
                                    RFC5164Voices(key).Speeds.Add(speed)
                                End If
                            End With
                            voiceExists = True
                            Exit For
                        End If
                    Next

                    If Not voiceExists Then
                        RFC5164Voices.Add((CurrentSample << 8) Or chIdx, thisVoice)
                        RFC5164VoiceChannels.Add(New SortedSet(Of Integer))
                    End If

                    Dim index As Integer = 0

                    For Each key As Long In RFC5164Voices.Keys
                        If RFC5164Voices(key) = thisVoice Then
                            With RFC5164VoiceChannels(index)
                                If Not .Contains(chIdx) Then
                                    .Add(chIdx)
                                End If
                            End With
                            Exit For
                        End If

                        index += 1

                    Next

                End If
            End With
        Next
    End Sub

End Class