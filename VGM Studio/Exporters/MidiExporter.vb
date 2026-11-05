Imports VGM_Studio.VGM
Imports VGM_Studio.Midi
Imports System.Runtime.Serialization

Public Class MidiExportOptions
    Inherits ExportOptions

    Public Property TimeDivisionType As TimeDivisionType = TimeDivisionType.Ticks

    Public Property Tempo As Single = 120 'i.e. BPM (quarter notes per minute)
    'NOTE: Calculate BPM by using 60000 / quarterNoteLengthInMillisecs          

    Public Property Numerator As Integer = 4
    Public Property Denominator As Integer = 4
    Public Property PitchBendRange As Integer = 2
    Public Property Volume As Integer = 100
    Public Property Velocity As Integer = 100 'Mezzo-forte
End Class

Public Class MidiExporter
    Inherits Exporter

    Private MidiSequence As Sequence
    Private MidiTrack As Track

    'Added 2021/02/12: Full SegaPCM Midi Output
    Private SegaPCMMidiSequence As Sequence
    Private SegaPCMMidiTrack As Track

    Public Overloads Property Options As MidiExportOptions

    Public ReadOnly Property BeatsPerSecond As Single
        Get
            Return Options.Tempo / 60 'quarter notes per second
        End Get
    End Property

    Public ReadOnly Property TicksPerQuarterNote As Integer
        Get
            ''Changed on 3/5/2026: Ticks per quarter note is fixed at 960
            Return 960
            'Return VGM_Sample_Rate / BeatsPerSecond
        End Get
    End Property

    Public ReadOnly Property MillisecsPerTick As Single
        Get
            Return 60000 / (Options.Tempo * TicksPerQuarterNote)
        End Get
    End Property

    Const Frames_Per_Tick As Integer = 1

    Private ReadOnly Property SamplesPerTick As Single

    Sub New(ByRef vgmFile As VGM.VGMFile, options As MidiExportOptions)
        MyBase.New(vgmFile, options)

        _Options = options

        Select Case options.TimeDivisionType 'AccuracyLevel
            Case TimeDivisionType.SMTPE
                _SamplesPerTick = VGM_Sample_Rate / (FrameRate * Frames_Per_Tick)
            Case TimeDivisionType.Ticks
                _SamplesPerTick = VGM_Sample_Rate / (TicksPerQuarterNote * BeatsPerSecond)
        End Select

        If SegaPCMChip IsNot Nothing Then
            BuildPCMVoiceDict()
        End If
    End Sub

    'Const Pitchbend_Range_in_Semitones = 24

    Protected Overrides Sub Start()
        MidiSequence = New Sequence
        MidiTrack = New Track(MidiSequence)

        With MidiTrack
            .AddSetTempoEvent(Options.Tempo)
            .AddTimeSignatureEvent(Options.Numerator, Math.Log(Options.Denominator, 2), 24, 8)

            'Set up channels 1–9 for FM
            For chIdx As Integer = 0 To 8
                With .Channel(chIdx)
                    .ChangeProgram(ProgramValue.Lead8BassLead) 'Bass & Lead is a good all-rounder
                    .ChangeVolume(Options.Volume)
                End With
            Next

            'Set up channel 10 for percussion
            With .Channel(9)
                .ChangeVolume(Options.Volume)
            End With

            'Set up channels 11–13 to square waves for PSG and SSG tones
            For chIdx As Integer = 10 To 12
                With .Channel(chIdx)
                    .ChangeProgram(ProgramValue.Lead1Square)
                    .ChangeVolume(0)
                End With
            Next

            'Set panning for PSG channels for better stereo separation
            'Tones
            .Channel(10).ChangePan(ControllerValue.PanLeft75Right25)
            .Channel(11).ChangePan(ControllerValue.PanCenter)
            .Channel(12).ChangePan(ControllerValue.PanLeft25Right75)

            'Noise
            .Channel(13).ChangePan(ControllerValue.PanCenter)

            'Set pitch bend range for all channels
            For chIdx As Integer = 0 To MidiTrack.Channel.GetUpperBound(0)
                With .Channel(chIdx)
                    .ChangePitchBendRange(Options.PitchBendRange, 0)
                End With
            Next

        End With

        'Added 2021/02/12: Full SegaPCM Midi Output
        If SegaPCMChip IsNot Nothing Then
            SegaPCMMidiSequence = New Sequence
            SegaPCMMidiTrack = New Track(MidiSequence)

            With SegaPCMMidiTrack
                .AddSetTempoEvent(Options.Tempo)
                .AddTimeSignatureEvent(Options.Numerator, Math.Log(Options.Denominator, 2), 24, 8)
            End With
        End If

    End Sub

    Protected Overrides Sub Finish()
        MidiTrack.Close()

        'Added 2021/02/12: Full SegaPCM Midi Output
        If SegaPCMChip IsNot Nothing Then
            SegaPCMMidiTrack.Close()
        End If
    End Sub

    Public Overrides Sub Save(fileName As String)
        'If CancellationToken.IsCancellationRequested Then Exit Sub

        Dim td As New TimeDivision(960)

        Select Case Options.TimeDivisionType
            Case TimeDivisionType.SMTPE
                Dim frames As Byte
                Select Case FrameRate
                    Case 50
                        frames = 25
                    Case 60
                        frames = 30
                End Select
                td = New TimeDivision(frames, 2 * Frames_Per_Tick)

            Case TimeDivisionType.Ticks
                td = New TimeDivision(TicksPerQuarterNote)

        End Select

        Dim mf As New MidiFormat0File(MidiTrack, td)
        mf.Save(fileName)

        'Added 2021/02/12: Full SegaPCM Midi Output
        If SegaPCMChip IsNot Nothing Then
            Dim spcmMf As New MidiFormat0File(SegaPCMMidiTrack, td)
            With spcmMf
                .Save(fileName.Replace(".mid", " SegaPCM.mid"))
            End With
        End If

    End Sub

#Region "VGM Command Processors"

    Protected Overrides Sub ProcessWait(sampleCount As Integer)
        MyBase.ProcessWait(sampleCount)

        'Carry over any fractional ticks to next wait command
        Static ticks As Single
        ticks += sampleCount / SamplesPerTick

        Dim wholeTicks As Integer =
            Math.Floor(ticks)

        If wholeTicks > 0 Then
            MidiTrack.Tick(wholeTicks)

            'Added 2021/02/12: Full SegaPCM Midi Output
            If SegaPCMChip IsNot Nothing Then
                SegaPCMMidiTrack.Tick(wholeTicks)

                For curSample As Integer = 0 To sampleCount
                    SegaPCMChip.Advance()
                Next

            End If

            ticks -= wholeTicks
        End If
    End Sub

    Protected Overrides Sub ProcessWriteFromDataBank()
        MyBase.ProcessWriteFromDataBank()

        'TEST: Convert data bank offsets to MIDI percussion
        With MidiTrack.Channel(9)
            Dim offset As Long =
                DataBankOffset - 1
            Select Case offset
                ''Phantasy from Phantasy Star II
                'Case 0
                '    .PlayNote(New Note(Percussion.ClosedHiHat, 0), 127, 1)
                'Case &H201
                '    .PlayNote(New Note(Percussion.AcousticBassDrum, 0), 127, 1)
                'Case &H382
                '    .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)
                'Case &HB81
                '    .PlayNote(New Note(Percussion.OpenHiHat, 0), 127, 1)
                'Case &H1183
                '    .PlayNote(New Note(Percussion.LowMidTom, 0), 127, 1)
                'Case &H1984
                '    .PlayNote(New Note(Percussion.HiMidTom, 0), 127, 1)
                'Case &H1D00
                '    .PlayNote(New Note(Percussion.ClosedHiHat, 0), 127, 1)


                ''Green Hill Zone from Sonic the Hedgehog
                'Case 0
                '    .PlayNote(New Note(Percussion.AcousticBassDrum, 0), 127, 1)
                'Case &H61F, &H1505, &H23E5, &H3349
                '    .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)

                ''The Shinobi from Revenge of Shinobi
                'Case 0
                '    .PlayNote(New Note(Percussion.AcousticBassDrum, 0), 127, 1)
                'Case &H710
                '    .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)
                'Case &H16B0
                '    .PlayNote(New Note(Percussion.OpenHiHat, 0), 127, 1)
                '    'Case &H1AE0
                '    '    .PlayNote(New Note(Percussion.PedalHiHat, 0), 127, 1)

                ''The Street of Rage from Streets of Rage
                'Case 0
                '    .PlayNote(New Note(Percussion.AcousticBassDrum, 0), 127, 1)
                'Case &H862
                '    .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)
                'Case &H1236
                '    .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)

                ''Fighting in the Street from Streets of Rage
                'Case 0
                '    .PlayNote(New Note(Percussion.AcousticBassDrum, 0), 127, 1)
                'Case &H862
                '    .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)
                'Case &H1236
                '    .PlayNote(New Note(Percussion.RideBell, 0), 127, 1)

                ''Moon Beach from Streets of Rage
                ' Case 0, &H862
                '     .PlayNote(New Note(Percussion.AcousticBassDrum, 0), 127, 1)
                ' Case &HE3D
                '     .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)
                ' Case &H1811
                '     .PlayNote(New Note(Percussion.AcousticSnare, 0), 127, 1)

            End Select
        End With

    End Sub

#End Region

#Region "PSG (SN76489 and compatibles) Event Handlers"

    Const PSG_SSG_Base_Midi_Ch_Num As Integer = 10
    Protected Const PSG_Atten_Max As Single = 15
    Protected Const PSG_SSG_Midi_Ref_Vol As Byte = 127

    Protected Class PSGStatus
        Public NoteOn(2) As Boolean
        Public OldAtten(2) As UInteger
        Public OldFNum(2) As UInteger
        Public OldFNote(2) As Single
        Public NoteChanged(2) As Boolean
    End Class

    Protected thisPSGStatus As New PSGStatus
    Protected Overrides Sub PSG_FNumLoChanged(ByRef ch As PSG.PSGChannel, fnumLo As Byte)
    End Sub

    Protected Overrides Sub PSG_FNumHiChanged(ByRef ch As PSG.PSGChannel, fnumHi As Byte)
    End Sub

    Protected Overrides Sub PSG_FNumChanged(ByRef ch As PSG.PSGChannel)
        With ch
            'Don't play notes on tone channel 2 if the noise rate source is tone 2
            If (.Index = PSG.ChValue.Tone3) And (.Chip.Registers.NoiseRate = PSG.NoiseRate.Tone3) Then
                Exit Sub
            End If

            Dim midichIdx As Byte =
                PSG_SSG_Base_Midi_Ch_Num + .Index

            Dim fNote As Single =
                PSG.GetFNote(.FNum, .Chip.IntSampleClockFreq)

            If .FNum < (.Chip.IntSampleClockFreq / (.Chip.ExtSampleClockFreq / 2)) Then
                MidiTrack.Channel(midichIdx).StopNote(0)
                thisPSGStatus.NoteOn(.Index) = False
            Else
                Dim note As New Note(fNote)

                If thisPSGStatus.NoteOn(.Index) Then
                    MidiTrack.Channel(midichIdx).BendPitch(note, 0)
                Else
                    MidiTrack.Channel(midichIdx).PlayNote(note, Options.Velocity, 0)
                    thisPSGStatus.NoteOn(.Index) = True
                End If

            End If

            thisPSGStatus.OldFNum(.Index) = .FNum
            thisPSGStatus.OldFNote(.Index) = fNote
        End With
    End Sub

    Protected PSGLevelToMidiVol As Byte() =
        {127, 101, 80, 64, 50, 40, 32, 25, 20, 16, 13, 10, 8, 6, 4, 0}
    Protected Overrides Sub PSG_ToneAttenChanged(ByRef ch As PSG.PSGChannel)
        With ch
            Dim midichIdx As Byte =
                PSG_SSG_Base_Midi_Ch_Num + .Index

            If .Atten = PSG_Atten_Max Then
                MidiTrack.Channel(midichIdx).StopNote(0)
                thisPSGStatus.NoteOn(.Index) = False
            Else
                Dim vol As Byte =
                    If(.Atten = 15, 0, PSG_SSG_Midi_Ref_Vol - (.Atten * 8))
                'Dim vol As Byte =
                '    PSGLevelToMidiVol(atten)
                MidiTrack.Channel(midichIdx).ChangeVolume(vol)

                'If attenuation decreases by the specified amount or more then play a new note
                Dim attenDelta As Integer =
                    CInt(thisPSGStatus.OldAtten(.Index)) - CInt(.Atten)
                If Not thisPSGStatus.NoteOn(.Index) AndAlso
                    thisPSGStatus.OldAtten(.Index) = PSG_Atten_Max AndAlso
                    .Atten <> PSG_Atten_Max OrElse
                    attenDelta >= 2 Then

                    Dim fnote As Single =
                        PSG.GetFNote(.FNum, .Chip.IntSampleClockFreq)
                    Dim note As New Note(fnote)
                    MidiTrack.Channel(midichIdx).PlayNote(note, Options.Velocity, 0)
                    thisPSGStatus.NoteOn(.Index) = True

                    thisPSGStatus.OldFNum(.Index) = .FNum
                    thisPSGStatus.OldFNote(.Index) = fnote
                End If
            End If
            thisPSGStatus.OldAtten(.Index) = .Atten
        End With
    End Sub

    Protected Overrides Sub PSG_NoiseTypeChanged(ByRef chip As PSG.Chip)
        If chip.Registers.NoiseRate = PSG.NoiseRate.Tone3 Then
            ' Stop Tone3
            Dim midichIdx As Byte =
                PSG_SSG_Base_Midi_Ch_Num + 2
            MidiTrack.Channel(midichIdx).StopNote(0)
            thisPSGStatus.NoteOn(2) = False
        End If
    End Sub

    Protected Overrides Sub PSG_NoiseAttenChanged(ByRef ch As PSG.PSGChannel)
        With ch
            Dim midichIdx As Byte = 9
            'Play Noise
            If .Atten = PSG_Atten_Max Then
                MidiTrack.Channel(midichIdx).StopNote(0)
            Else
                Dim vel As Byte =
                If(.Atten = 15, 0, PSG_SSG_Midi_Ref_Vol - (.Atten * 8))
                MidiTrack.Channel(midichIdx).PlayNote(New Note(Percussion.Cabasa, 0), vel, 0)
            End If
        End With
    End Sub

#End Region

#Region "Yamaha OPLL (YM2413/B (OPLL), YM2420, YM2423, YM2425, YMF281/B, Konami VRC7) Event Handlers"

    Const OPL_AttenIndB_Max As Single = 48
    Const OPL_Midi_Ref_Vol As Byte = 127

    Protected Class OPLStatus
        Public NoteOn(8) As Boolean
        Public OldVol(8) As UInteger
        Public OldFNum(8) As UInteger
        Public OldFNote(8) As Single
        Public WasPrevFNumWriteHi(8) As Boolean
        Public NoteChanged(8) As Boolean
    End Class

    Protected thisOPLStatus As New OPLStatus

    Protected Overrides Sub OPLL_RhythmChanged(ByRef chip As OPLL.Chip)
        Dim regs As OPLL.OPLLRegisters =
            chip.Registers

        Dim BDVel, HHVel, SDVel, TOMVel, TCYMVel As Byte
        With regs
            BDVel = If(.BDVol = 15, 0, 127 - (.BDVol * 8))
            SDVel = If(.SDVol = 15, 0, 127 - (.SDVol * 8))
            TOMVel = If(.TOMVol = 15, 0, 127 - (.TOMVol * 8))
            TCYMVel = If(.TCYMVol = 15, 0, 127 - (.TCYMVol * 8))
            HHVel = If(.HHVol = 15, 0, 127 - (.HHVol * 8))
        End With

        Dim midichIdx As Byte = 9

        With MidiTrack.Channel(midichIdx)
            If regs.BD AndAlso BDVel <> 0 Then
                .PlayNote(New Note(Percussion.BassDrum1, 0), BDVel, 0)
            Else
                .StopNote(0)
            End If
            If regs.SD AndAlso SDVel <> 0 Then
                .PlayNote(New Note(Percussion.AcousticSnare, 0), SDVel, 1)
            Else
                .StopNote(1)
            End If
            If regs.TOM AndAlso TOMVel <> 0 Then
                .PlayNote(New Note(Percussion.LowTom, 0), TOMVel, 2)
            Else
                .StopNote(2)
            End If
            If regs.TCYM AndAlso TCYMVel <> 0 Then
                .PlayNote(New Note(Percussion.OpenHiHat, 0), TCYMVel, 3)
            Else
                .StopNote(3)
            End If
            If regs.HH AndAlso HHVel <> 0 Then
                .PlayNote(New Note(Percussion.ClosedHiHat, 0), HHVel, 4)
            Else
                .StopNote(4)
            End If
        End With

    End Sub

    Protected Overrides Sub OPLL_FNumLoChanged(ByRef ch As OPLL.OPLLChannel)
        With ch
            If thisOPLLStatus.NoteOn(.Index) AndAlso
            Not thisOPLLStatus.WasPrevFNumWriteHi(.Index) Then
                Dim fNote As Single =
                    OPLL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
                'Changed on 2026/02/17: Transpose note up two octaves
                fNote *= 4
                Dim note As New Note(fNote)

                Dim midichIdx As Byte =
                    .Index
                MidiTrack.Channel(midichIdx).BendPitch(note, 0)
            End If
            thisOPLLStatus.WasPrevFNumWriteHi(.Index) = False
        End With
    End Sub

    Protected Overrides Sub OPLL_SusKeyBlockFNumHiChanged(ByRef ch As OPLL.OPLLChannel)
        With ch
            Dim midichIdx As Byte =
                .Index

            Select Case .Sus
                Case True
                    MidiTrack.AddControllerChangeEvent(midichIdx, ControllerType.SoundReleaseTime, 64)
                Case False
                    MidiTrack.AddControllerChangeEvent(midichIdx, ControllerType.SoundReleaseTime, 0)
            End Select

            Select Case .Key
                Case True
                    Dim fNote As Single =
                        OPLL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
                    'Changed on 2026/02/17: Transpose note up two octaves
                    fNote *= 4
                    Dim note As New Note(fNote)

                    If thisOPLLStatus.NoteOn(.Index) Then
                        MidiTrack.Channel(midichIdx).BendPitch(note, 0)
                    Else
                        Dim vel As Integer =
                            If(.Vol = 15, 0, 127 - (.Vol * 8))

                        MidiTrack.Channel(midichIdx).PlayNote(note, vel, 0)
                        thisOPLLStatus.NoteOn(.Index) = True
                    End If

                Case False
                    MidiTrack.Channel(midichIdx).StopNote(0)
                    thisOPLLStatus.NoteOn(.Index) = False
            End Select
            thisOPLLStatus.WasPrevFNumWriteHi(.Index) = True
        End With
    End Sub

    Protected Overrides Sub OPLL_InstVolChanged(ByRef ch As OPLL.OPLLChannel)
        Dim midichIdx As Byte
        Dim midiProg As ProgramValue
        Dim midiVol As Single

        With ch
            midichIdx = .Index

            Select Case .Inst
                Case OPLL.InstType.User
                    midiProg = ProgramValue.Lead8BassLead
                Case OPLL.InstType.Violin
                    midiProg = ProgramValue.StringEnsemble1
                Case OPLL.InstType.Guitar
                    midiProg = ProgramValue.AcousticGuitarSteel
                Case OPLL.InstType.Piano
                    midiProg = ProgramValue.AcousticGrandPiano
                Case OPLL.InstType.Flute
                    midiProg = ProgramValue.Flute
                Case OPLL.InstType.Clarinet
                    midiProg = ProgramValue.Clarinet
                Case OPLL.InstType.Oboe
                    midiProg = ProgramValue.Oboe
                Case OPLL.InstType.Trumpet
                    midiProg = ProgramValue.Trumpet
                Case OPLL.InstType.Organ
                    midiProg = ProgramValue.HammondOrgan
                Case OPLL.InstType.Horn
                    midiProg = ProgramValue.Trombone
                Case OPLL.InstType.Synthesizer
                    midiProg = ProgramValue.Pad3Polysynth
                Case OPLL.InstType.Harpsichord
                    midiProg = ProgramValue.Harpsichord
                Case OPLL.InstType.Vibraphone
                    midiProg = ProgramValue.Vibraphone
                Case OPLL.InstType.SynthBass
                    midiProg = ProgramValue.SynthBass1
                Case OPLL.InstType.AcousticBass
                    midiProg = ProgramValue.AcousticBass
                Case OPLL.InstType.ElectricGuitar
                    midiProg = ProgramValue.ElectricGuitarJazz
            End Select

            midiVol =
                If(ch.Vol = 15, 0, 127 - (ch.Vol * 8))
        End With

        'Add to midi track
        With MidiTrack.Channel(midichIdx)
            .ChangeProgram(midiProg)
            .ChangeVolume(midiVol)
        End With

    End Sub

#End Region

#Region "Yamaha OPM (YM2151) and OPP (YM2164) Event Handlers"

    Const OPM_Base_Midi_Ch_Num As Integer = 0
    Const OPM_AttenIndB_Max As Single = 96
    Const OPM_Midi_Ref_Vol As Byte = 127

    Protected Class OPMStatus
        Public YM2151KeyOn(7) As Boolean
    End Class

    Protected thisOPMStatus As New OPMStatus

    Protected Overrides Sub OPM_KeyOnChanged(ByRef chip As OPM.Chip)
        MyBase.OPM_KeyOnChanged(chip)

        With chip.Registers
            Dim ch As OPM.OPMChannel =
                chip.Channel(.Ch)
            Dim midiCh As Channel =
                MidiTrack.Channel(.Ch)
            If .KeyOn = 0 Then
                midiCh.StopNote(0)
            Else
                With ch
                    Dim fnote As Single =
                        OPM.GetFNote(.Note, .Octave, .KeyFrac, .Op(3).Mul, chip.MasterClockFreq)
                    Dim note As New Note(fnote)
                    midiCh.PlayNote(note, Options.Velocity, 0)
                End With
            End If
        End With
    End Sub

    Protected Overrides Sub OPM_TLChanged(ByRef op As OPM.OPMOp)
        'Get average TL from all operators
        If op.Index = 3 Then
            Dim ch As OPM.OPMChannel =
                op.Channel
            Dim tl As Integer =
                ch.Op(3).TL
            For i As Integer = 0 To 2
                With ch.Op(i)
                    If .IsCarrier Then
                        tl += .TL
                        tl \= 2
                    End If
                End With
            Next
            Dim midiCh As Channel =
                MidiTrack.Channel(OPN_Base_Midi_Ch_Num + ch.Index)
            midiCh.ChangeVolume(OPM_Midi_Ref_Vol - tl)
        End If
    End Sub

    Protected Overrides Sub OPM_KeyCodeChanged(ByRef ch As OPM.OPMChannel)
        OPM_KeyCodeOrFractionChanged(ch)
    End Sub

    Protected Overrides Sub OPM_KeyFractionChanged(ByRef ch As OPM.OPMChannel)
        OPM_KeyCodeOrFractionChanged(ch)
    End Sub

    Private Sub OPM_KeyCodeOrFractionChanged(ByRef ch As OPM.OPMChannel)
        With ch
            Dim midiCh As Channel =
                MidiTrack.Channel(OPM_Base_Midi_Ch_Num + .Index)
            If midiCh.NoteOn(0) = True Then
                Dim fnote As Single =
                    OPM.GetFNote(.Note, .Octave, .KeyFrac, .Op(3).Mul, .Chip.MasterClockFreq)
                Dim note As New Note(fnote)
                midiCh.BendPitch(note, 0)
                'midiCh.BendPitch(OPMKeyCodeToMidiNote(.Note, .Octave, .KeyFrac, .Op(3).Mul), 0)
            End If
        End With

    End Sub

    Protected Overrides Sub OPM_RLFLConChanged(ByRef ch As OPM.OPMChannel)
        With ch
            Dim pan As ControllerValue

            If .L = True AndAlso .R = False Then
                pan = ControllerValue.PanLeft
            ElseIf .L = True AndAlso .R = True Then
                pan = ControllerValue.PanCenter
            ElseIf .L = False AndAlso .R = True Then
                pan = ControllerValue.PanRight
            End If

            Dim midiChIdx As Integer =
                OPN_Base_Midi_Ch_Num + .Index
            MidiTrack.AddPanChangeEvent(midiChIdx, pan)
        End With

    End Sub

#End Region

#Region "Yamaha OPN (YM2203 (OPN), YM2608 (OPNA), YM2010/B (OPNB), YM2612 (OPN2), YM3438 (OPN2C), YMF276 (OPN2L), and YMF288 (OPN3)) Event Handlers"

    Const OPN_Base_Midi_Ch_Num As Integer = 0
    Const OPN_Ch3Sp_Base_Midi_Ch_Num As Integer = 6
    Const OPN_AttenIndB_Max As Single = 48
    Const OPN_Midi_Ref_Vol As Byte = 127

    Protected Class OPNStatus
        Public PrevKeyOn(7) As Boolean
        Public KeyOn(7) As Boolean
        Public PrevOpOn(7, 3) As Boolean
        Public OpOn(7, 3) As Boolean
    End Class

    Dim thisOPNStatus As New OPNStatus

    Protected Overrides Sub OPN_KeyOnChanged(ByRef chip As OPN.Chip)
        MyBase.OPN_KeyOnChanged(chip)

        'Set current channel
        Dim chIdx As Integer
        With chip.Registers
            'Reject invalid Ch value
            If .Ch = 3 Then
                Exit Sub
            End If
            chIdx = (.Port * 3) + .Ch
        End With

        'TEST: Check for patch changes
        Static oldVoice As OPN.Voice() =
            {New OPN.Voice(0), New OPN.Voice(0), New OPN.Voice(0), New OPN.Voice(0), New OPN.Voice(0), New OPN.Voice(0)}

        Dim keyOn As Byte =
            chip.Registers.KeyOn

        If keyOn <> 0 Then
            If Not ThisOPNVoice = oldVoice(chIdx) Then
                'TODO: Add midi program change or metadata event
            End If
            oldVoice(chIdx) = ThisOPNVoice
        End If
        'END TEST

        'Update key on data
        For opIdx As Integer = 0 To 3
            thisOPNStatus.PrevOpOn(chIdx, opIdx) = thisOPNStatus.OpOn(chIdx, opIdx)
            thisOPNStatus.OpOn(chIdx, opIdx) = ((keyOn >> opIdx) And 1) = 1
        Next

        Dim ch As OPN.OPNChannel =
            chip.Channel(chIdx)
        Dim isCh3SpMode As Boolean =
            chIdx = 2 AndAlso chip.Registers.Ch3Mode = OPN.Ch3Mode.Special

        'Use separate fNotes for each operator
        Dim fNum, block, dt, multi As Integer
        Dim fNote(3) As Single

        For opIdx As Integer = 0 To 3
            If isCh3SpMode AndAlso opIdx < 3 Then
                fNum = chip.Registers.Ch3SpFNum(opIdx)
                block = chip.Registers.Ch3SpBlock(opIdx)
            Else
                fNum = ch.FNum
                block = ch.Block
            End If
            dt = ch.Op(opIdx).DT
            multi = ch.Op(opIdx).Multi
            'Get FNote (with detune and multiple)
            fNote(opIdx) = OPN.GetFNote(block, fNum, dt, multi, chip.IntSampleClockFreq)
        Next

        'Special handing for Channel 3 Special Mode
        If isCh3SpMode Then
            For opIdx As Integer = 0 To 2
                With MidiTrack.Channel(OPN_Ch3Sp_Base_Midi_Ch_Num + opIdx)
                    If thisOPNStatus.OpOn(chIdx, opIdx) Then
                        If thisOPNStatus.PrevOpOn(chIdx, opIdx) = False Then
                            Dim op As OPN.OPNOp =
                                ch.Op(opIdx)
                            If op.IsCarrier Then
                                .PlayNote(New Note(fNote(opIdx)), 127 - op.TL, 0)
                            End If
                        End If
                    Else
                        .StopNote(0)
                    End If
                End With
            Next

        End If

        Dim midiCh As Channel =
            MidiTrack.Channel(OPN_Base_Midi_Ch_Num + chIdx)

        If thisOPNStatus.OpOn(chIdx, 3) Then
            'midiCh.PlayNote(New Note(fNote(3)), 127 - ch.Op(3).TL, 0)
            midiCh.PlayNote(New Note(fNote(3)), Options.Velocity, 0)
        Else
            midiCh.StopNote(0)
        End If

        If (keyOn And &HF) <> 0 Then
            thisOPNStatus.KeyOn(chIdx) = True
        Else
            thisOPNStatus.KeyOn(chIdx) = False
        End If

    End Sub

    Protected Overrides Sub OPN_BlockFNumChanged(ByRef ch As OPN.OPNChannel)
        With ch
            If thisOPNStatus.KeyOn(.Index) Then
                Dim midiCh As Channel =
                    MidiTrack.Channel(OPN_Base_Midi_Ch_Num + .Index)
                Dim fNote As Single =
                    OPN.GetFNote(.Block, .FNum, .Op(3).DT, .Op(3).Multi, .Chip.IntSampleClockFreq)
                midiCh.BendPitch(fNote, 0)
            End If
        End With
    End Sub

    Protected Overrides Sub OPN_Ch3ModeResetEnableLoadChanged(ByRef chip As OPN.Chip)
        Static oldCh3Mode As OPN.Ch3Mode =
            OPN.Ch3Mode.Normal

        With chip.Registers
            If (.Ch3Mode = OPN.Ch3Mode.Normal) And (oldCh3Mode <> OPN.Ch3Mode.Normal) Then
                Dim midiCh As Channel =
                    MidiTrack.Channel(OPN_Base_Midi_Ch_Num + 2)
                midiCh.StopNote(0)
                For i As Integer = 0 To 2
                    midiCh = MidiTrack.Channel(OPN_Ch3Sp_Base_Midi_Ch_Num + i)
                    midiCh.StopNote(0)
                Next
            End If
            oldCh3Mode = .Ch3Mode
        End With

    End Sub

    Protected Overrides Sub OPN_Ch3SpBlockFNumChanged(ByRef chip As OPN.Chip, opIdx As Integer)

        With chip.Channel(2).Op(opIdx)
            If thisOPNStatus.OpOn(2, opIdx) Then
                Dim block As Integer =
                    chip.Registers.Ch3SpBlock(opIdx)
                Dim fnum As Integer =
                    chip.Registers.Ch3SpFNum(opIdx)
                Dim fNote As Single =
                    OPN.GetFNote(block, fnum, .DT, .Multi, chip.IntSampleClockFreq)

                Dim midiCh As Channel =
                    MidiTrack.Channel(OPN_Ch3Sp_Base_Midi_Ch_Num + opIdx)
                midiCh.BendPitch(fNote, 0)
            End If
        End With

    End Sub

    Protected Overrides Sub OPN_TLChanged(ByRef op As OPN.OPNOp)
        'Get average TL from all operators
        If op.Index = 3 Then
            Dim ch As OPN.OPNChannel =
                op.Channel
            Dim tl As Integer =
                ch.Op(3).TL
            'For i As Integer = 0 To 2
            '    With ch.Op(i)
            '        If .IsCarrier Then
            '            tl += .TL
            '            tl \= 2
            '        End If
            '    End With
            'Next

            Dim dB As Single =
                -(tl * OPN.dB_Per_TL_Step)
            Dim midiVol As Byte =
                GetMidiVolume(dB)
            Dim midiCh As Channel =
                MidiTrack.Channel(OPN_Base_Midi_Ch_Num + op.Channel.Index)
            midiCh.ChangeVolume(midiVol)
        End If

    End Sub

    Protected Overrides Sub OPN_KSARChanged(ByRef op As OPN.OPNOp)
        With op
            Dim midichIdx As Integer =
                OPN_Base_Midi_Ch_Num + .Channel.Index
            MidiTrack.AddControllerChangeEvent(midichIdx, ControllerType.SoundAttackTime, (31 - .AR) * 8)
        End With
    End Sub

    Protected Overrides Sub OPN_SLRRChanged(ByRef op As OPN.OPNOp)
        With op
            Dim midichIdx As Integer =
                OPN_Base_Midi_Ch_Num + .Channel.Index
            MidiTrack.AddControllerChangeEvent(midichIdx, ControllerType.SoundReleaseTime, .RR * 8)
        End With
    End Sub

    Protected Overrides Sub OPN_LRAMSPMSChanged(ByRef ch As OPN.OPNChannel)
        With ch
            Dim pan As ControllerValue

            If .L = True AndAlso .R = False Then
                pan = ControllerValue.PanLeft
            ElseIf .L = True AndAlso .R = True Then
                pan = ControllerValue.PanCenter
            ElseIf .L = False AndAlso .R = True Then
                pan = ControllerValue.PanRight
            End If

            Dim midiCh As Channel =
                MidiTrack.Channel(OPN_Base_Midi_Ch_Num + .Index)
            midiCh.ChangePan(pan)
        End With
    End Sub

#End Region

#Region "Yamaha OPL YM3526 (OPL), Y8950 (MSX-Audio), YM3812 (OPL2), YMF262 (OPL3), YMF278B (OPL4), and YMF289 (OPL3-L) Event Handlers"

    Const OPLL_AttenIndB_Max As Single = 48
    Const OPLL_Midi_Ref_Vol As Byte = 127

    Protected Class OPLLStatus
        Public NoteOn(8) As Boolean
        Public OldVol(8) As UInteger
        Public OldFNum(8) As UInteger
        Public OldFNote(8) As Single
        Public WasPrevFNumWriteHi(8) As Boolean
        Public NoteChanged(8) As Boolean
    End Class

    Protected thisOPLLStatus As New OPLLStatus

    Protected Overrides Sub OPL_AMD_PMD_RhythmChanged(ByRef chip As OPL.Chip)
        Dim regs As OPL.OPLRegisters =
            chip.Registers

        Dim BDVel, HHVel, SDVel, TOMVel, TCVel As Byte
        With chip
            BDVel = 127 - (.Channel(6).Op(1).TL * 2)
            HHVel = 127 - (.Channel(7).Op(0).TL * 2)
            SDVel = 127 - (.Channel(7).Op(1).TL * 2)
            TOMVel = 127 - (.Channel(8).Op(0).TL * 2)
            TCVel = 127 - (.Channel(8).Op(1).TL * 2)
        End With

        Dim midichIdx As Byte = 9

        With MidiTrack.Channel(midichIdx)
            If regs.BD Then
                .PlayNote(New Note(Percussion.BassDrum1, 0), BDVel, 0)
            Else
                .StopNote(0)
            End If
            If regs.SD Then
                .PlayNote(New Note(Percussion.AcousticSnare, 0), SDVel, 1)
            Else
                .StopNote(1)
            End If
            If regs.TOM Then
                .PlayNote(New Note(Percussion.LowTom, 0), TOMVel, 2)
            Else
                .StopNote(2)
            End If
            If regs.TC Then
                .PlayNote(New Note(Percussion.OpenHiHat, 0), TCVel, 3)
            Else
                .StopNote(3)
            End If
            If regs.HH Then
                .PlayNote(New Note(Percussion.ClosedHiHat, 0), HHVel, 4)
            Else
                .StopNote(4)
            End If
        End With

    End Sub

    Protected Overrides Sub OPL_KSL_TLChanged(ByRef op As OPL.OPLOp)
        'Only change volume for carriers
        If Not op.IsCarrier Then
            Exit Sub
        End If

        Dim midiVol As Single

        With op
            midiVol =
                If(.TL = 63, 0, 127 - (.TL * 2))
        End With

        Dim midiCh As Channel =
            MidiTrack.Channel(op.Channel.Index)

        'Add to midi track
        With midiCh
            .ChangeVolume(midiVol)
        End With

    End Sub

    Protected Overrides Sub OPL_FNumLoChanged(ByRef ch As OPL.OPLChannel)
        With ch
            If thisOPLStatus.NoteOn(.Index) AndAlso
            Not thisOPLStatus.WasPrevFNumWriteHi(.Index) Then
                Dim fNote As Single =
                    OPL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
                'Changed on 2026/02/17: Transpose note up two octaves
                fNote *= 4
                Dim note As New Note(fNote)

                Dim midiCh As Channel =
                    MidiTrack.Channel(.Index)
                midiCh.BendPitch(note, 0)
            End If
            thisOPLStatus.WasPrevFNumWriteHi(.Index) = False
        End With
    End Sub

    Protected Overrides Sub OPL_KeyOnBlockFNumHiChanged(ByRef ch As OPL.OPLChannel)
        With ch
            Dim midiCh As Channel =
                MidiTrack.Channel(.Index)

            Select Case .KeyOn
                Case True
                    Dim fNote As Single =
                        OPL.GetFNote(.Block, .FNum, .Op(1).Multi, .Chip.IntSampleClockFreq)
                    ''Changed on 2026/02/17: Transpose note up one octave
                    'fNote *= 2
                    Dim note As New Note(fNote)

                    If thisOPLStatus.NoteOn(.Index) Then
                        midiCh.BendPitch(note, 0)
                    Else
                        Dim vel As Integer =
                            If(.Op(1).TL = 63, 0, 127 - (.Op(1).TL * 2))

                        midiCh.PlayNote(note, vel, 0)
                        thisOPLStatus.NoteOn(.Index) = True
                    End If

                Case False
                    midiCh.StopNote(0)
                    thisOPLStatus.NoteOn(.Index) = False
            End Select
            thisOPLStatus.WasPrevFNumWriteHi(.Index) = True
        End With
    End Sub

#End Region

#Region "SSG (AY-3-8910, YM2149, compatibles) Event Handlers"

    Protected Class SSGStatus
        Public NoteOn(2) As Boolean
        Public Level(2) As UInteger
        Public OldLevel(2) As UInteger
        Public OldFNum(2) As UInteger
        Public OldFNote(2) As Single
        Public NoteChanged(2) As Boolean
    End Class

    Protected thisSSGStatus As New SSGStatus
    Protected Overrides Sub SSG_IONoiseToneCtrlChanged(chip As SSG.Chip, ioEn() As Boolean, noiseEn() As Boolean, toneEn() As Boolean)
        For chIdx = 0 To 2
            Dim midichIdx As Byte =
                PSG_SSG_Base_Midi_Ch_Num + chIdx
            If toneEn(chIdx) = False Then
                If thisSSGStatus.NoteOn(chIdx) Then
                    MidiTrack.Channel(midichIdx).StopNote(0)
                    thisSSGStatus.NoteOn(chIdx) = False
                End If
            End If
        Next
    End Sub

    Protected SSGLevelToMidiVol As Byte() =
        {0, 16, 26, 36, 46, 56, 66, 75, 84, 92, 100, 107, 114, 120, 124, 127}
    Protected Overrides Sub SSG_ModeLevelChanged(chip As SSG.Chip, chIdx As Integer, mode As Boolean, level As UInteger)
        Dim midichIdx As Byte =
            PSG_SSG_Base_Midi_Ch_Num + chIdx

        If level = 0 Then
            If thisSSGStatus.NoteOn(chIdx) Then
                MidiTrack.Channel(midichIdx).StopNote(0)
                thisSSGStatus.NoteOn(chIdx) = False
            End If
        Else
            Dim vol As Single =
                SSGLevelToMidiVol(level)
            MidiTrack.Channel(midichIdx).ChangeVolume(vol)

            If (thisSSGStatus.OldLevel(chIdx) = 0) And (level <> 0) Then
                With chip.Registers
                    If .FNum(chIdx) <> 0 Then
                        Dim fNote As Single =
                            SSG.GetFNote(.FNum(chIdx), chip.IntSampleClockFreq)
                        Dim note As New Note(fNote)
                        MidiTrack.Channel(midichIdx).PlayNote(note, Options.Velocity, 0)
                        thisSSGStatus.NoteOn(chIdx) = True
                    End If
                End With
            End If
        End If

        thisSSGStatus.OldLevel(chIdx) = level

    End Sub

    Protected Overrides Sub SSG_NoiseFNumChanged(chip As SSG.Chip, fnum As UInteger)
        'Dim hz As Single =
        '    SSG.GetFNote(fnum, chip.IntSampleClockFreq)
    End Sub

    Protected Overrides Sub SSG_FNumLoChanged(chip As SSG.Chip, chIdx As Integer, fnumLo As UInteger, fnum As UInteger)
        'If Not thisSSGStatus.NoteChanged(chIdx) Then
        '    'Bend pitch
        '    Dim midichIdx As Byte =
        '        PSG_SSG_Base_Midi_Ch_Num + chIdx
        '    Dim fNote As Single =
        '        SSG.GetFNote(fnum, chip.IntSampleClockFreq)
        '    Dim note As New Note(fNote)

        '    MidiTrack.Channel(midichIdx).BendPitch(note, 0)
        '    thisSSGStatus.OldFNote(chIdx) = fNote
        'End If
        'thisSSGStatus.NoteChanged(chIdx) = False
    End Sub
    Protected Overrides Sub SSG_FNumHiChanged(chip As SSG.Chip, chIdx As Integer, fnumHi As UInteger, fnum As UInteger)
        ''Play new note
        'Dim midichIdx As Byte =
        '    PSG_SSG_Base_Midi_Ch_Num + chIdx
        'Dim fNote As Single =
        '    SSG.GetFNote(fnum, chip.IntSampleClockFreq)
        'Dim note As New Note(fNote)

        'MidiTrack.Channel(midichIdx).PlayNote(note, Options.Velocity, 0)
        'thisSSGStatus.NoteOn(chIdx) = True
        'thisSSGStatus.OldFNote(chIdx) = fNote
        'thisSSGStatus.NoteChanged(chIdx) = True
    End Sub

    Protected Overrides Sub SSG_FNumChanged(chip As SSG.Chip, chIdx As Integer, fnum As UInteger)
        Dim midichIdx As Byte =
            PSG_SSG_Base_Midi_Ch_Num + chIdx

        Dim fnote As Single =
            SSG.GetFNote(fnum, chip.IntSampleClockFreq)

        If fnum < (chip.IntSampleClockFreq / (chip.ExtSampleClockFreq / 2)) Then
            MidiTrack.Channel(midichIdx).StopNote(0)
            thisSSGStatus.NoteOn(chIdx) = False
        Else
            Dim note As New Note(fnote)

            If thisSSGStatus.NoteOn(chIdx) Then
                MidiTrack.Channel(midichIdx).BendPitch(note, 0)
            Else
                MidiTrack.Channel(midichIdx).PlayNote(note, Options.Velocity, 0)
                thisSSGStatus.NoteOn(chIdx) = True
            End If

        End If

        thisSSGStatus.OldFNum(chIdx) = fnum
        thisSSGStatus.OldFNote(chIdx) = fnote
    End Sub

#End Region

#Region "SegaPCM Event Handlers"

    Protected Class SegaPCMStatus
        Public SegaPCMNoteOnSample(15) As Long
        Public SegaPCMVoiceKey(15) As Integer
    End Class

    Protected thisSegaPCMStatus As SegaPCMStatus

    Protected Overrides Sub SegaPCMChip_VolLChanged(ByRef ch As SegaPCM.Channel)
        SegaPCMChip_VolChanged(ch)
    End Sub

    Protected Overrides Sub SegaPCMChip_VolRChanged(ByRef ch As SegaPCM.Channel)
        SegaPCMChip_VolChanged(ch)
    End Sub

    Private Sub SegaPCMChip_VolChanged(ByRef ch As SegaPCM.Channel)
        With ch

            Dim key As Integer =
                thisSegaPCMStatus.SegaPCMVoiceKey(.Index)

            If PCMVoiceDict.ContainsKey(key) Then
                Dim voi As MidiVoice =
                    PCMVoiceDict(key)

                If TypeOf (voi) Is MelodicMidiVoice Then
                    'Change pan
                    Dim panL As Integer =
                        .Vol(0) \ 2
                    Dim panR As Integer =
                        .Vol(1) \ 2
                    Dim pan As Integer =
                        ControllerValue.PanCenter - panL + panR
                    Dim midiCh As Channel =
                        SegaPCMMidiTrack.Channel(.Index)
                    midiCh.ChangePan(pan)
                End If

            End If

        End With

    End Sub

    Protected Overrides Sub SegaPCMChip_MuteLoopOffsetChanged(ByRef ch As SegaPCM.Channel)
        With ch
            Dim status As SegaPCMStatus =
                thisSegaPCMStatus

            If Not .Enabled Then

                'With MidiTrack.Channel(9)
                '    .StopNote(chIdx)
                'End With

            Else

                Dim vol As Integer =
                    (.Vol(0) + .Vol(1)) \ 2

                If vol > 0 Then

                    If status.SegaPCMNoteOnSample(.Index) < CurrentSample Then 'avoid duplicate key ons

                        Dim key As Integer =
                            (.LoopAddr << 8) + .AddrInc
                        Dim oldKey As Integer =
                            status.SegaPCMVoiceKey(.Index)
                        status.SegaPCMVoiceKey(.Index) = key

                        If PCMVoiceDict.ContainsKey(key) Then
                            Dim voi As MidiVoice =
                                PCMVoiceDict(key)

                            If TypeOf (voi) Is MelodicMidiVoice Then
                                Dim mv As MelodicMidiVoice =
                                    PCMVoiceDict(key)

                                Dim midiCh As Channel =
                                    SegaPCMMidiTrack.Channel(.Index)

                                '    midiCh.ChangeProgram(mv.Program)

                                Dim noteIndex As Integer = 0
                                For Each note As Note In mv.Note
                                    midiCh.PlayNote(New Note(note.Number, note.Fraction), 100, noteIndex)

                                    ''Scale note(s) to correct pitch
                                    'Dim scaledNoteValue As Single =
                                    '    note.Number + 12 * (Math.Log(.AddrInc / &H80, 2))
                                    'Dim scaledNoteWhole As NoteNumber =
                                    '    Math.Floor(scaledNoteValue)
                                    'Dim scaledNoteFrac As Single =
                                    '    scaledNoteValue - scaledNoteWhole

                                    'With SegaPCMMidiTrack.Channel(chIdx)
                                    '    .PlayNote(New Note(scaledNoteWhole, scaledNoteFrac), 100, noteIndex)
                                    'End With
                                    noteIndex += 1
                                Next

                            ElseIf TypeOf (voi) Is PercussiveMidiVoice Then
                                Dim pv As PercussiveMidiVoice =
                                    PCMVoiceDict(key)

                                Dim perc As Percussion =
                                    pv.Note

                                Dim vel As Integer =
                                    63 + vol
                                vel = Math.Min(vel, VelocityValue.Max)

                                Dim midiCh As Channel =
                                    SegaPCMMidiTrack.Channel(9)

                                midiCh.StopNote(.Index)
                                midiCh.PlayNote(New Note(perc, 0), vel, .Index)

                            End If

                        End If

                        status.SegaPCMNoteOnSample(.Index) = CurrentSample

                    End If

                End If

            End If

        End With

    End Sub

    Protected Overrides Sub SegaPCMChip_EndAddrReached(ByRef ch As SegaPCM.Channel)
        MyBase.SegaPCMChip_EndAddrReached(ch)

        With ch

            Dim status As SegaPCMStatus =
                thisSegaPCMStatus

            Dim key As Integer =
                status.SegaPCMVoiceKey(.Index)

            If PCMVoiceDict.ContainsKey(key) Then
                Dim voi As MidiVoice =
                    PCMVoiceDict(key)

                If TypeOf (voi) Is MelodicMidiVoice Then
                    Dim mv As MelodicMidiVoice =
                        PCMVoiceDict(key)
                    Dim noteIndex As Integer = 0
                    For Each note As Note In mv.Note
                        Dim midiCh As Channel =
                            SegaPCMMidiTrack.Channel(.Index)
                        midiCh.StopNote(noteIndex)
                        noteIndex += 1
                    Next

                ElseIf TypeOf (voi) Is PercussiveMidiVoice Then

                    Dim midiCh As Channel =
                        SegaPCMMidiTrack.Channel(9)

                    midiCh.StopNote(.Index)

                End If

            End If

        End With

    End Sub

    Private ReadOnly Property PCMVoiceDict As Dictionary(Of Integer, MidiVoice)

    Private Sub BuildPCMVoiceDict()
        _PCMVoiceDict = New Dictionary(Of Integer, MidiVoice)

        With PCMVoiceDict
            '*** After Burner II
            'PERCUSSION
            .Add(&H70, New PercussiveMidiVoice(Percussion.BassDrum1))
            .Add(&H2C0060, New PercussiveMidiVoice(Percussion.AcousticSnare))
            .Add(&H2C0070, New PercussiveMidiVoice(Percussion.AcousticSnare))
            .Add(&H490070, New PercussiveMidiVoice(Percussion.HiMidTom))
            .Add(&H590070, New PercussiveMidiVoice(Percussion.LowMidTom))
            .Add(&H800070, New PercussiveMidiVoice(Percussion.OpenHiHat))
            .Add(&H980050, New PercussiveMidiVoice(Percussion.CrashCymbal1))
            .Add(&H980060, New PercussiveMidiVoice(Percussion.CrashCymbal2))
            .Add(&H980070, New PercussiveMidiVoice(Percussion.SplashCymbal))
            .Add(&HB60070, New PercussiveMidiVoice(Percussion.Cowbell))
            .Add(&HC30070, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC30080, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC30090, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC300A0, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC300B0, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC300C0, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC300D0, New PercussiveMidiVoice(Percussion.Cabasa))
            .Add(&HC80070, New PercussiveMidiVoice(Percussion.RideBell))
            .Add(&HEE0070, New PercussiveMidiVoice(Percussion.HighTom))
            .Add(&H4200070, New PercussiveMidiVoice(Percussion.ClosedHiHat))

            'DIST. GUITAR
            '0x2C000: Fm power(?) chord
            .Add(&H2C0005A, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.B4, 0)})) 'Bm chord

            '0x30000: D#M chord
            .Add(&H3000044, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.E3, 0)})) 'EM chord
            .Add(&H3000048, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.F3, 0)})) 'FM chord
            .Add(&H3000050, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.G3, 0)})) 'GM chord
            .Add(&H300005A, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.A3, 0)})) 'AM chord

            '0x34000: D# single string
            .Add(&H3400044, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.E3, 0)})) 'E
            .Add(&H340004C, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.Fsharp3, 0)})) 'F#
            .Add(&H3400051, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.G3, 0)})) 'G
            .Add(&H340005B, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.A3, 0)})) 'A
            .Add(&H3400066, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.B3, 0)})) 'B
            .Add(&H340006C, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.C4, 0)})) 'C
            .Add(&H3400073, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.Csharp4, 0)})) 'C#
            .Add(&H3400079, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.D4, 0)})) 'D

            '0x38000: High G#M (power?) chord
            .Add(&H3800051, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.C3, 0)})) 'CM chord
            .Add(&H380005B, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.D3, 0)})) 'DM chord

            '0x3C000: High G#(?) fifth chord
            .Add(&H3C00043, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.D4, 0)}))
            .Add(&H3C0004B, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.E4, 0)}))
            .Add(&H3C0005B, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.Fsharp4, 0)}))

            '0x50000: Muted guitar stroke
            .Add(&H500004A, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.Gsharp5, 0)})) 'G# fifth

            '0x59600: D#M chord (more treble than 0x2C000)
            .Add(&H5960043, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.E3, 0)})) 'EM chord
            .Add(&H5960050, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.G3, 0)})) 'GM chord
            .Add(&H596005A, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.A3, 0)})) 'AM chord
            .Add(&H596005B, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.A3, 0)})) 'AM chord
            .Add(&H596006B, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.C4, 0)})) 'CM chord
            .Add(&H5960078, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.D4, 0)})) 'DM chord
            .Add(&H5960087, New MelodicMidiVoice(ProgramValue.DistortionGuitar, {New Note(NoteNumber.E4, 0)})) 'EM chord

            '*** Galaxy Force II
            'PERCUSSION
            .Add(&H80, New PercussiveMidiVoice(Percussion.AcousticBassDrum))

            .Add(&HD0060, New PercussiveMidiVoice(Percussion.LowTom))
            .Add(&HD0080, New PercussiveMidiVoice(Percussion.LowMidTom))
            .Add(&HD00B4, New PercussiveMidiVoice(Percussion.HiMidTom))
            .Add(&HD00FE, New PercussiveMidiVoice(Percussion.HighTom))

            .Add(&H2500A0, New PercussiveMidiVoice(Percussion.OpenHiHat))

            .Add(&H540068, New PercussiveMidiVoice(Percussion.SplashCymbal))

            .Add(&HB80080, New PercussiveMidiVoice(Percussion.SideStick))

            .Add(&HD90080, New PercussiveMidiVoice(Percussion.HandClap))

            .Add(&HEA00A0, New PercussiveMidiVoice(Percussion.ClosedHiHat))

            .Add(&H1000080, New PercussiveMidiVoice(Percussion.RideCymbal2))
            .Add(&H10000C0, New PercussiveMidiVoice(Percussion.RideCymbal1))

            .Add(&H1370080, New PercussiveMidiVoice(Percussion.Cowbell))

            .Add(&H1490080, New PercussiveMidiVoice(Percussion.Tambourine))

            .Add(&H17100A0, New PercussiveMidiVoice(Percussion.AcousticSnare))

            .Add(&H2000060, New PercussiveMidiVoice(Percussion.ChineseCymbal))
            .Add(&H20000A0, New PercussiveMidiVoice(Percussion.CrashCymbal1))
            .Add(&H20000C0, New PercussiveMidiVoice(Percussion.CrashCymbal2))

            'SLAP BASS
            .Add(&H11A5006E, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.D2, 0)}))
            .Add(&H11A50075, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Dsharp2, 0)}))
            .Add(&H11A5007D, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.E2, 0)}))
            .Add(&H11A50085, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.F2, 0)}))

            .Add(&H113A007D, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.E2, 0)}))
            .Add(&H113A0085, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.F2, 0)}))
            .Add(&H113A008E, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.Fsharp2, 0)}))
            .Add(&H113A0092, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.G2, 0)}))
            .Add(&H113A009C, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.G2, 0)}))
            .Add(&H113A00A6, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.Gsharp2, 0)}))
            .Add(&H113A00B8, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.Gsharp2, 0)}))
            .Add(&H113A00D2, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.A2, 0)}))
            .Add(&H113A00DC, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.A2, 0)}))

            .Add(&H1153006E, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.C4, 0)}))
            .Add(&H1153007A, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.D4, 0)}))
            .Add(&H11530081, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.Dsharp4, 0)}))

            .Add(&H1164006E, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.D2, 0)}))
            .Add(&H1164007D, New MelodicMidiVoice(ProgramValue.SlapBass2, {New Note(NoteNumber.E2, 0)}))

            .Add(&H1184006E, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.C2, 0)}))
            .Add(&H11840075, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Csharp2, 0)}))
            .Add(&H1184007D, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.D2, 0)}))
            .Add(&H11840085, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Dsharp2, 0)}))

            .Add(&H11BB006E, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.G2, 0)}))
            .Add(&H11BB0075, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Gsharp2, 0)}))
            .Add(&H11BB007D, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.A2, 0)}))
            .Add(&H11BB0085, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Asharp2, 0)}))
            .Add(&H11BB008A, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.B2, 0)}))

            .Add(&H11D1006E, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.F3, 0)}))
            .Add(&H11D10075, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Fsharp3, 0)}))
            .Add(&H11D1007D, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.G3, 0)}))
            .Add(&H11D10085, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Gsharp3, 0)}))
            .Add(&H11D1008B, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.A3, 0)}))
            .Add(&H11D10093, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Asharp3, 0)}))

            .Add(&H11E2006E, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.G4, 0)}))
            .Add(&H11E20075, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Gsharp4, 0)}))
            .Add(&H11E2007D, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.A4, 0)}))
            .Add(&H11E20085, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.Asharp4, 0)}))
            .Add(&H11E2008A, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.B4, 0)}))

            .Add(&H11EE006E, New MelodicMidiVoice(ProgramValue.SlapBass1, {New Note(NoteNumber.A2, 0)}))


            '*** Out Run
            .Add(&H3260050, New PercussiveMidiVoice(Percussion.ShortWhistle)) 'Seashore FX
            .Add(&H4009084, New PercussiveMidiVoice(Percussion.AcousticSnare))
            .Add(&H40C1D78, New PercussiveMidiVoice(Percussion.ElectricSnare))
            .Add(&H41C0384, New PercussiveMidiVoice(Percussion.HiMidTom))
            .Add(&H429AB84, New PercussiveMidiVoice(Percussion.AcousticBassDrum))
            .Add(&H4302F84, New PercussiveMidiVoice(Percussion.ClosedHiHat))
            .Add(&H43BE690, New PercussiveMidiVoice(Percussion.CrashCymbal1))
            .Add(&H500F084, New PercussiveMidiVoice(Percussion.LowMidTom))
            .Add(&H5095184, New PercussiveMidiVoice(Percussion.HighFloorTom))
            .Add(&H517C084, New PercussiveMidiVoice(Percussion.OpenHiHat))
            .Add(&H5437C84, New PercussiveMidiVoice(Percussion.Cowbell))
            .Add(&H549DE84, New PercussiveMidiVoice(Percussion.SideStick))
            .Add(&H54DA084, New PercussiveMidiVoice(Percussion.Maracas))
            .Add(&H5583084, New PercussiveMidiVoice(Percussion.HandClap))
            .Add(&H5600284, New PercussiveMidiVoice(Percussion.ShortWhistle)) 'Splash Wave only?

        End With
    End Sub

#End Region

End Class

Public MustInherit Class MidiVoice
    Public Overrides Function ToString() As String
        Dim value As String = MyBase.ToString
        Dim sb As New Text.StringBuilder(value)
        With sb
            .AppendLine($"Midi Voice")
        End With
        Return sb.ToString
    End Function
End Class

Public Class MelodicMidiVoice
    Inherits MidiVoice
    Public ReadOnly Property Program As ProgramValue
    Public ReadOnly Property Note As Note()
    Sub New(program As ProgramValue, note As Note())
        _Note = note
    End Sub
    Public Overrides Function ToString() As String
        Return MyBase.ToString & $" (Melodic)"
    End Function
End Class

Public Class PercussiveMidiVoice
    Inherits MidiVoice
    Public ReadOnly Property Note As Percussion
    Sub New(note As Percussion)
        _Note = note
    End Sub
    Public Overrides Function ToString() As String
        Return MyBase.ToString & $" (Percussive)"
    End Function
End Class

