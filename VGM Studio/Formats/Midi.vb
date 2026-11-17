'MIDI Class
'Copyright © 1999–2026 Paul Jensen
'Reference(s):
'- Birch, Dr. Sebastian Anthony
'  "The MIDI File Format"
'  <https://www.personal.kent.edu/~sbirch/Music_Production/MP-II/MIDI/midi_file_format.htm>


Imports System.IO
Imports System.Math

Public Class Midi

#Region "Shared Methods"

    Shared Function GetMidiVolume(dB As Single) As Byte
        'Dim db As Single =
        '    Log2(Clamp(pct, 0.01, 100) / 100) * 6
        Return Pow(10, dB / 40) * 127
    End Function

    Shared Function GetVariableLength(value As ULong) As Byte()
        Dim list As New List(Of Byte)
        With list
            .Add(value And &H7F)
            Do
                value >>= 7
                If value <= 0 Then Exit Do
                .Insert(0, (value And &H7F) Or &H80)
            Loop
            Return .ToArray
        End With
    End Function

#End Region

#Region "Enums"

    Public Enum TimeDivisionType As Byte
        SMTPE = 0
        Ticks = 1
    End Enum

    Public Enum ChannelValue As Byte
        Min = &H0
        Max = &HF '15
    End Enum

    Public Enum NoteNumber As Byte
        Min = &H0
        Max = &H7F '127

        C0 = 0
        Csharp0 = 1
        D0 = 2
        Dsharp0 = 3
        E0 = 4
        F0 = 5
        Fsharp0 = 6
        G0 = 7
        Gsharp0 = 8
        A0 = 9
        Asharp0 = 10
        B0 = 11

        C1 = 12
        Csharp1 = 13
        D1 = 14
        Dsharp1 = 15
        E1 = 16
        F1 = 17
        Fsharp1 = 18
        G1 = 19
        Gsharp1 = 20
        A1 = 21
        Asharp1 = 22
        B1 = 23

        C2 = 24
        Csharp2 = 25
        D2 = 26
        Dsharp2 = 27
        E2 = 28
        F2 = 29
        Fsharp2 = 30
        G2 = 31
        Gsharp2 = 32
        A2 = 33
        Asharp2 = 34
        B2 = 35

        C3 = 36
        Csharp3 = 37
        D3 = 38
        Dsharp3 = 39
        E3 = 40
        F3 = 41
        Fsharp3 = 42
        G3 = 43
        Gsharp3 = 44
        A3 = 45
        Asharp3 = 46
        B3 = 47

        C4 = 48
        Csharp4 = 49
        D4 = 50
        Dsharp4 = 51
        E4 = 52
        F4 = 53
        Fsharp4 = 54
        G4 = 55
        Gsharp4 = 56
        A4 = 57
        Asharp4 = 58
        B4 = 59

        C5 = 60
        Csharp5 = 61
        D5 = 62
        Dsharp5 = 63
        E5 = 64
        F5 = 65
        Fsharp5 = 66
        G5 = 67
        Gsharp5 = 68
        A5 = 69
        Asharp5 = 70
        B5 = 71

        C6 = 72
        Csharp6 = 73
        D6 = 74
        Dsharp6 = 75
        E6 = 76
        F6 = 77
        Fsharp6 = 78
        G6 = 79
        Gsharp6 = 80
        A6 = 81
        Asharp6 = 82
        B6 = 83

        C7 = 84
        Csharp7 = 85
        D7 = 86
        Dsharp7 = 87
        E7 = 88
        F7 = 89
        Fsharp7 = 90
        G7 = 91
        Gsharp7 = 92
        A7 = 93
        Asharp7 = 94
        B7 = 95

        C8 = 96
        Csharp8 = 97
        D8 = 98
        Dsharp8 = 99
        E8 = 100
        F8 = 101
        Fsharp8 = 102
        G8 = 103
        Gsharp8 = 104
        A8 = 105
        Asharp8 = 106
        B8 = 107

        C9 = 108
        Csharp9 = 109
        D9 = 110
        Dsharp9 = 111
        E9 = 112
        F9 = 113
        Fsharp9 = 114
        G9 = 115
        Gsharp9 = 116
        A9 = 117
        Asharp9 = 118
        B9 = 119

        C10 = 120
        Csharp10 = 121
        D10 = 122
        Dsharp10 = 123
        E10 = 124
        F10 = 125
        Fsharp10 = 126
        G10 = 127
    End Enum

    Public Enum VelocityValue As Byte
        Min = &H0
        Max = &H7F '127
        Pianissimo = 15
        MezzoPiano = 31
        Piano = 47
        Normal = 63
        Forte = 79
        MezzoForte = 95
        Fortissimo = 111
    End Enum

    Public Enum EventType As Byte
        NoteOff = &H80
        NoteOn = &H90
        NoteAftertouch = &HA0
        ControllerChange = &HB0
        ProgramChange = &HC0
        ChannelAftertouch = &HD0
        PitchWheel = &HE0
        Sysex = &HF0
        SysexEscape = &HF7
        MetaEvent = &HFF
    End Enum

    Public Enum MetaEventType As Byte
        TrackNumber = &H0
        TextEvent = &H1
        CopyrightNotice = &H2
        TrackName = &H3
        InstrumentName = &H4
        Lyric = &H5
        Marker = &H6
        CuePoint = &H7
        ChannelPrefix = &H20
        EndOfTrack = &H2F
        SetTempo = &H51
        SMTPEOffset = &H54
        TimeSignature = &H58
        KeySignature = &H59
        SequencerSpecific = &H7F
        'TODO: Add more types of Meta events
    End Enum

    Public Enum ControllerType As Byte
        ModulatorWheel = 1
        DataEntry = 6
        Volume = 7
        Pan = 10
        Sustain = 64
        LegatoPedal = 68
        Hold2Pedal = 69
        SoundTimbre = 71
        SoundReleaseTime = 72
        SoundAttackTime = 73
        SoundBrightness = 74
        RPNFine = 100
        RPNCoarse = 101
        AllControllersOff = 123
        AllNotesOff = 123
    End Enum

    Public Enum ControllerValue As Byte
        VolumeMin = &H0
        VolumeMax = &H7F

        '11/4/2026: Fixed bug in Volume values.  Volume0Percent was &H1, but should be &H0.  Volume100Percent was &H7E, but should be &H7F. Thanks, ValleyBell!
        Volume0Percent = 0
        Volume25Percent = 64
        Volume50Percent = 90
        Volume75Percent = 110
        Volume100Percent = 127

        '11/4/2026: Fixed bug in Pan values.  PanLeft was &H0, but should be &H1.  PanCenter was &H3F, but should be &H40. Thanks, ValleyBell!
        PanLeft = &H1 '&H0
        PanRight = &H7F
        PanCenter = &H40 '&H3F

        PanLeft100Right0 = 1
        PanLeft75Right25 = 32
        PanLeft50Right50 = 64
        PanLeft25Right75 = 96
        PanLeft0Right100 = 127

        PitchBendRangeDefault = 2
    End Enum

    Public Enum ProgramValue As Byte
        AcousticGrandPiano = 0
        BrightAcousticPiano = 1
        ElectricGrandPiano = 2
        HonkyTonkPiano = 3
        RhodesPiano = 4
        ChorusedPiano = 5
        Harpsichord = 6
        Clavinet = 7
        Celesta = 8
        Glockenspiel = 9
        MusicBox = 10
        Vibraphone = 11
        Marimba = 12
        Xylophone = 13
        TubularBells = 14
        Dulcimer = 15
        HammondOrgan = 16
        PercussiveOrgan = 17
        RockOrgan = 18
        ChurchOrgan = 19
        ReedOrgan = 20
        Accordion = 21
        Harmonica = 22
        TangoAccordion = 23
        AcousticGuitarNylon = 24
        AcousticGuitarSteel = 25
        ElectricGuitarJazz = 26
        ElectricGuitarClean = 27
        ElectricGuitarMuted = 28
        OverdrivenGuitar = 29
        DistortionGuitar = 30
        GuitarHarmonics = 31
        AcousticBass = 32
        ElectricBassFinger = 33
        ElectricBassPick = 34
        FretlessBass = 35
        SlapBass1 = 36
        SlapBass2 = 37
        SynthBass1 = 38
        SynthBass2 = 39
        Violin = 40
        Viola = 41
        Cello = 42
        Contrabass = 43
        TremoloStrings = 44
        PizzicatoStrings = 45
        OrchestralHarp = 46
        Timpani = 47
        StringEnsemble1 = 48
        StringEnsemble2 = 49
        SynthStrings1 = 50
        SynthStrings2 = 51
        ChoirAhhs = 52
        VoiceOohs = 53
        SynthVoice = 54
        OrchestraHit = 55
        Trumpet = 56
        Trombone = 57
        Tuba = 58
        MutedTrumpet = 59
        FrenchHorn = 60
        BrassSection = 61
        SynthBrass1 = 62
        SynthBrass2 = 63
        SopranoSax = 64
        AltoSax = 65
        TenorSax = 66
        BaritoneSax = 67
        Oboe = 68
        EnglishHorn = 69
        Bassoon = 70
        Clarinet = 71
        Piccolo = 72
        Flute = 73
        Recorder = 74
        PanFlute = 75
        BottleBlow = 76
        Shakuhachi = 77
        Whistle = 78
        Ocarina = 79
        Lead1Square = 80
        Lead2Sawtooth = 81
        Lead3iope = 82
        Lead4Chiff = 83
        Lead5Changarang = 84
        Lead6Voice = 85
        Lead7Fifths = 86
        Lead8BassLead = 87
        Pad1NewAge = 88
        Pad2Warm = 89
        Pad3Polysynth = 90
        Pad4Choir = 91
        Pad5Bowed = 92
        Pad6Metallic = 93
        Pad7Halo = 94
        Pad8Sweep = 95
        FX1Rain = 96
        FX2Soundtrack = 97
        FX3Crystal = 98
        FX4Atmosphere = 99
        FX5Brightness = 100
        FX6Goblins = 101
        FX7Echoes = 102
        FX8SciFi = 103
        Sitar = 104
        Banjo = 105
        Shamisen = 106
        Koto = 107
        Kalimba = 108
        Bagpipe = 109
        Fiddle = 110
        Shanai = 111
        TinkleBell = 112
        Agogo = 113
        SteelDrums = 114
        Woodblock = 115
        TaikoDrum = 116
        MelodicTom = 117
        SynthDrum = 118
        ReverseCymbal = 119
        GuitarFretNoise = 120
        BreathNoise = 121
        Seashore = 122
        BirdTweet = 123
        TelephoneRing = 124
        Helicopter = 125
        Applause = 126
        Gunshot = 127
    End Enum

    Public Enum PitchWheelValue As UShort
        Min = &H0US
        Max = &H3FFFUS
        Center = &H2000US
    End Enum

    Public Enum RPN As UShort
        PitchBendRange = 0
    End Enum

    Public Enum Percussion As Byte
        AcousticBassDrum = 35
        BassDrum1 = 36
        SideStick = 37
        AcousticSnare = 38
        HandClap = 39
        ElectricSnare = 40
        LowFloorTom = 41
        ClosedHiHat = 42
        HighFloorTom = 43
        PedalHiHat = 44
        LowTom = 45
        OpenHiHat = 46
        LowMidTom = 47
        HiMidTom = 48
        CrashCymbal1 = 49
        HighTom = 50
        RideCymbal1 = 51
        ChineseCymbal = 52
        RideBell = 53
        Tambourine = 54
        SplashCymbal = 55
        Cowbell = 56
        CrashCymbal2 = 57
        Vibraslap = 58
        RideCymbal2 = 59
        HiBongo = 60
        LowBongo = 61
        MuteHiConga = 62
        OpenHiConga = 63
        LowConga = 64
        HiTimbale = 65
        LowTimbale = 66
        HighAgogo = 67
        LowAgogo = 68
        Cabasa = 69
        Maracas = 70
        ShortWhistle = 71
        LongWhistle = 72
        ShortGuiro = 73
        LongGuiro = 74
        Claves = 75
        HiWoodBlock = 76
        LowWoodBlock = 77
        MuteCuica = 78
        OpenCuica = 79
        MuteTriangle = 80
        OpenTriangle = 81
    End Enum

#End Region

    Public Class DeltaTime
        'Delta times are variable length, in big-endian byte order
        'Public ReadOnly Property Data() As List(Of Byte)
        Public ReadOnly Property Data As Byte()

        Sub New(ticks As ULong)
            'Data = New List(Of Byte)
            'With Data
            '    .Add(ticks And &H7F)
            '    Do
            '        ticks >>= 7
            '        If ticks <= 0 Then Exit Do
            '        .Insert(0, (ticks And &H7F) Or &H80)
            '    Loop
            'End With
            Data = GetVariableLength(ticks)
        End Sub

    End Class

    Public MustInherit Class MidiEvent
        Protected ReadOnly Property DataList As List(Of Byte)

        Public ReadOnly Property Data As Byte()
            Get
                Return DataList.ToArray
            End Get
        End Property

        Sub New()
            DataList = New List(Of Byte)
        End Sub

    End Class

    Public MustInherit Class TrackEvent : Inherits MidiEvent
        Public ReadOnly Property Type() As EventType
            Get
                Return DataList(0) And &HF0
            End Get
        End Property

        Public ReadOnly Property Channel() As ChannelValue
            Get
                Return DataList(0) And &HF
            End Get
        End Property

        Sub New(type As EventType, channel As ChannelValue)
            MyBase.New()
            With DataList
                .Add(type Or (channel And &HF))
            End With
        End Sub
    End Class

    Public Class NoteOffEvent : Inherits TrackEvent
        Public ReadOnly Property Note() As NoteNumber
            Get
                Return DataList(1)
            End Get
        End Property

        Public ReadOnly Property Velocity() As VelocityValue
            Get
                Return DataList(2)
            End Get
        End Property

        Sub New(channel As ChannelValue, note As NoteNumber, velocity As VelocityValue)
            MyBase.New(EventType.NoteOff, channel)

            With DataList
                .Add(note And NoteNumber.Max)
                .Add(velocity)
            End With
        End Sub

    End Class

    Public Class NoteOnEvent : Inherits TrackEvent
        Public ReadOnly Property Note() As NoteNumber
            Get
                Return DataList(1)
            End Get
        End Property

        Public ReadOnly Property Velocity() As VelocityValue
            Get
                Return DataList(2)
            End Get
        End Property

        Sub New(channel As ChannelValue, note As NoteNumber, velocity As VelocityValue)
            MyBase.New(EventType.NoteOn, channel)

            With DataList
                .Add(note And NoteNumber.Max)
                .Add(velocity)
            End With
        End Sub

    End Class

    Public Class NoteAftertouchEvent : Inherits TrackEvent
        Public ReadOnly Property Note() As NoteNumber
            Get
                Return DataList(1)
            End Get
        End Property

        Public ReadOnly Property Aftertouch() As VelocityValue
            Get
                Return DataList(2)
            End Get
        End Property

        Sub New(channel As ChannelValue, note As NoteNumber, aftertouch As VelocityValue)
            MyBase.New(EventType.NoteAftertouch, channel)

            With DataList
                .Add(note And NoteNumber.Max)
                .Add(aftertouch)
            End With
        End Sub

    End Class

    Public Class ControllerChangeEvent : Inherits TrackEvent
        Public ReadOnly Property Controller() As ControllerType
            Get
                Return DataList(1)
            End Get
        End Property

        Public ReadOnly Property Value() As ControllerValue
            Get
                Return DataList(2)
            End Get
        End Property

        Sub New(channel As ChannelValue, controller As ControllerType, value As ControllerValue)
            MyBase.New(EventType.ControllerChange, channel)

            With DataList
                .Add(controller)
                .Add(value)
            End With
        End Sub

    End Class

    Public Class ProgramChangeEvent : Inherits TrackEvent
        Public ReadOnly Property Program() As ProgramValue
            Get
                Return DataList(1)
            End Get
        End Property

        Sub New(channel As ChannelValue, program As ProgramValue)
            MyBase.New(EventType.ProgramChange, channel)

            With DataList
                .Add(program)
            End With
        End Sub

    End Class

    Public Class ChannelAftertouchEvent : Inherits TrackEvent
        Public ReadOnly Property Aftertouch() As VelocityValue
            Get
                Return DataList(1)
            End Get
        End Property

        Sub New(channel As ChannelValue, aftertouch As ProgramValue)
            MyBase.New(EventType.ChannelAftertouch, channel)

            With DataList
                .Add(aftertouch)
            End With
        End Sub

    End Class

    Public Class PitchWheelEvent : Inherits TrackEvent
        Public ReadOnly Property Value As PitchWheelValue
            Get
                Return BytesToValue(DataList.ToArray)
            End Get
        End Property

        Sub New(channel As ChannelValue, value As PitchWheelValue)
            MyBase.New(EventType.PitchWheel, channel)

            With DataList
                .AddRange(ValueToBytes(value))
            End With
        End Sub

        'Sub New(channel As ChannelValue, semitones As Single, range As Integer)
        '    MyBase.New(EventType.PitchWheel, New PitchWheel(semitones).Value(range))
        'End Sub

        Private Function ValueToBytes(value As PitchWheelValue) As Byte()
            'Value is in LSB, MSB (fine, coarse) order
            Return {value And &H7F, (value >> 7) And &H7F}
        End Function

        Private Function BytesToValue(bytes As Byte()) As PitchWheelValue
            Dim value As PitchWheelValue =
                (bytes(1) << 7) Or CUShort(bytes(0))
            'value += bytes(0)
            Return value And PitchWheelValue.Max
        End Function

    End Class

    Public MustInherit Class MetaEvent : Inherits MidiEvent
        Public ReadOnly Property Type() As EventType
            Get
                Return DataList(1)
            End Get
        End Property

        Sub New(type As MetaEventType, length As Byte)
            MyBase.New()
            With DataList
                .Add(&HFF)
                .Add(type)
                .Add(length)
            End With
        End Sub
    End Class

    Public Class EndOfTrackEvent : Inherits MetaEvent
        Sub New()
            MyBase.New(MetaEventType.EndOfTrack, 0)
        End Sub
    End Class

    Public Class SetTempoEvent : Inherits MetaEvent
        Const Microsecs_Per_Minute As ULong = 60000000

        Sub New(tempo As Single)
            MyBase.New(MetaEventType.SetTempo, 3)

            Dim microsecsPerQuarterNote As Long =
                Microsecs_Per_Minute / tempo

            With DataList
                .Add((microsecsPerQuarterNote >> 16) And &HFF)
                .Add((microsecsPerQuarterNote >> 8) And &HFF)
                .Add(microsecsPerQuarterNote And &HFF)
            End With
        End Sub

    End Class

    Public Class TimeSignatureEvent : Inherits MetaEvent
        Public ReadOnly Property Numerator As Byte
        Public ReadOnly Property Denominator As Byte
        Public ReadOnly Property Metronome As Byte
        Public ReadOnly Property n32ndsPerQuarterNote As Byte

        Sub New(numerator As Byte, denominator As Byte, metronome As Byte, n32ndsPerQuarterNote As Byte)
            MyBase.New(MetaEventType.TimeSignature, 4)

            _Numerator = numerator
            _Denominator = denominator
            _Metronome = metronome
            _n32ndsPerQuarterNote = n32ndsPerQuarterNote

            With DataList
                .Add(numerator)
                .Add(denominator)
                .Add(metronome)
                .Add(n32ndsPerQuarterNote)
            End With
        End Sub

    End Class


    Public Class Sequence

        Private _Tracks As Dictionary(Of String, Track)
        Public Property Tracks As Dictionary(Of String, Track)
            Get
                Return _Tracks
            End Get
            Private Set(value As Dictionary(Of String, Track))
                _Tracks = value
            End Set
        End Property

        Sub New()
            'Initalize tracks
            Tracks = New Dictionary(Of String, Track)
        End Sub

        Public Sub AddTrack(key As String, track As Track)
            With track
                Tracks.Add(key, track)
            End With
        End Sub

        Public Sub Tick(ticks As ULong)
            'For Each track As Track In Tracks
            '    track.Tick(ticks)
            'Next
            For Each kvp As KeyValuePair(Of String, Track) In Tracks
                kvp.Value.Tick(ticks)
            Next
        End Sub

        Public Sub Tick(ticks As Single)
            'For Each track As Track In Tracks
            '    track.Tick(ticks)
            'Next
            For Each kvp As KeyValuePair(Of String, Track) In Tracks
                kvp.Value.Tick(ticks)
            Next
        End Sub

    End Class

    Public Class Track

        'Public ReadOnly Property Index As Integer
        '    Get
        '        Dim idx As Integer = 0
        '        For Each track As Track In Sequence.Tracks
        '            If track Is Me Then
        '                Return idx
        '            End If
        '            idx += 1
        '        Next
        '        Return -1
        '    End Get
        'End Property

        'Private _Sequence As Sequence
        'Public Property Sequence As Sequence
        '    Get
        '        Return Sequence
        '    End Get
        '    Protected Set(value As Sequence)
        '        _Sequence = value
        '    End Set
        'End Property

        Private _Channel As Channel()
        Public Property Channel As Channel()
            Get
                Return _Channel
            End Get
            Protected Set(value As Channel())
                _Channel = value
            End Set
        End Property

        Private _TrackData As List(Of Byte)
        Public Property Data As List(Of Byte)
            Get
                Return _TrackData
            End Get
            Protected Set(value As List(Of Byte))
                _TrackData = value
            End Set
        End Property

        Public Property Tempo As Single

        Private _IsOpen As Boolean
        Public Property IsOpen() As Boolean
            Get
                Return _IsOpen
            End Get
            Private Set(value As Boolean)
                If _IsOpen <> value Then
                    _IsOpen = value
                    IsClosed = Not value
                End If
            End Set
        End Property

        Private _IsClosed As Boolean
        Public Property IsClosed() As Boolean
            Get
                Return _IsClosed
            End Get
            Private Set(value As Boolean)
                If _IsClosed <> value Then
                    _IsClosed = value
                    IsOpen = Not value
                End If
            End Set
        End Property

        Private _Ticks As ULong
        Private Property Ticks As ULong
            Set(value As ULong)
                _Ticks = value
            End Set
            Get
                Return _Ticks
            End Get
        End Property

        'Sub New(ByRef sequence As Sequence)
        '    Me.Sequence = sequence
        '    sequence.Tracks.Add(Me)

        '    TrackData = New List(Of Byte)

        '    ReDim Channel(ChannelValue.Max)
        '    For chIdx As Integer = 0 To Channel.GetUpperBound(0)
        '        Channel(chIdx) = New Channel(Me, chIdx, Midi.Channel.StatusType.Ready)
        '    Next

        '    IsOpen = True
        'End Sub

        Sub New()
            ReDim Channel(ChannelValue.Max)
            For chIdx As Integer = 0 To Channel.GetUpperBound(0)
                Channel(chIdx) = New Channel(Me, chIdx, Midi.Channel.StatusType.Ready)
            Next

            Data = New List(Of Byte)

            IsOpen = True
        End Sub

        Public Sub Tick(ticks As Long)
            Me.Ticks += ticks
        End Sub

        ' ********************************
        ' *** Methods for adding events

        Private Sub AddDeltaTime(dt As DeltaTime)
            Data.AddRange(dt.Data)
        End Sub

        Public Sub AddEvent(midievent As MidiEvent)
            If IsClosed Then Exit Sub

            AddDeltaTime(New DeltaTime(Ticks))
            Ticks = 0
            Data.AddRange(midievent.Data)

            If TypeOf (midievent) Is EndOfTrackEvent Then
                IsClosed = True
            End If
        End Sub

        Public Sub AddNoteOffEvent(channel As ChannelValue, note As Note, velocity As VelocityValue, noteIndex As Integer)
            AddEvent(New NoteOffEvent(channel, note.Number, velocity))

            With Me.Channel(channel)
                .NoteOn(noteIndex) = False
                .Note(noteIndex) = note
                .Velocity(noteIndex) = velocity
            End With
        End Sub

        Public Sub AddNoteOnEvent(channel As ChannelValue, note As Note, velocity As VelocityValue, noteIndex As Integer)
            AddEvent(New NoteOnEvent(channel, note.Number, velocity))

            With Me.Channel(channel)
                .NoteOn(noteIndex) = True
                .Note(noteIndex) = note
                .Velocity(noteIndex) = velocity
            End With
        End Sub

        Public Sub AddNoteAftertouchEvent(channel As ChannelValue, note As NoteNumber, aftertouch As VelocityValue)
            AddEvent(New NoteAftertouchEvent(channel, note, aftertouch))
        End Sub

        Public Sub AddControllerChangeEvent(channel As ChannelValue, controller As ControllerType, value As ControllerValue)
            AddEvent(New ControllerChangeEvent(channel, controller, value))
        End Sub

        Public Sub AddVolumeChangeEvent(channel As ChannelValue, volume As ControllerValue)
            AddEvent(New ControllerChangeEvent(channel, ControllerType.Volume, volume))
            With Me.Channel(channel)
                .Volume = volume
            End With
        End Sub

        Public Sub AddPanChangeEvent(channel As ChannelValue, pan As ControllerValue)
            If Me.Channel(channel).Pan <> pan Then
                AddEvent(New ControllerChangeEvent(channel, ControllerType.Pan, pan))
                With Me.Channel(channel)
                    .Pan = pan
                End With
            End If
        End Sub

        Public Sub AddRPNPitchBendRangeEvent(channel As ChannelValue, semitones As Byte, cents As Byte)
            AddEvent(New ControllerChangeEvent(channel, 101, 0))
            AddEvent(New ControllerChangeEvent(channel, 100, 0))

            AddEvent(New ControllerChangeEvent(channel, 6, semitones))
            AddEvent(New ControllerChangeEvent(channel, 38, cents))

            Me.Channel(channel).PitchBendRange = semitones
        End Sub

        Public Sub AddProgramChangeEvent(channel As ChannelValue, program As ProgramValue)
            AddEvent(New ProgramChangeEvent(channel, program))
            With Me.Channel(channel)
                .Program = program
            End With
        End Sub

        Public Sub AddChannelAftertouchEvent(channel As ChannelValue, aftertouch As VelocityValue)
            AddEvent(New ChannelAftertouchEvent(channel, aftertouch))
        End Sub

        Public Sub AddPitchWheelEvent(channel As ChannelValue, pitchWheel As PitchWheel)
            AddEvent(New PitchWheelEvent(channel, pitchWheel.Value))
            With Me.Channel(channel)
                .PitchWheel = pitchWheel
            End With
        End Sub

        Public Sub AddSetTempoEvent(tempo As Single)
            If tempo <= 0 Then Exit Sub

            AddEvent(New SetTempoEvent(tempo))
            Me.Tempo = tempo
        End Sub

        Public Sub AddTimeSignatureEvent(num As Byte, den As Byte, met As Byte, n32nds As Byte)
            If num = 0 Or met = 0 Or n32nds = 0 Then
                Exit Sub
            End If
            AddEvent(New TimeSignatureEvent(num, den, met, n32nds))
        End Sub

        Public Sub Close()
            Dim eotEvent As New EndOfTrackEvent
            AddEvent(eotEvent)
            _IsClosed = True
        End Sub

    End Class

    Public Class Channel

        Protected _Track As Track
        Public Property Track As Track
            Get
                Return _Track
            End Get
            Protected Set(value As Track)
                _Track = value
            End Set
        End Property

        Protected _Index As Integer
        Public Property Index As Integer
            Get
                Return _Index
            End Get
            Set(value As Integer)
                _Index = value
            End Set
        End Property

        Const Max_Note_Index As Integer = 127
        Public NoteOn(Max_Note_Index) As Boolean
        Public Note(Max_Note_Index) As Note
        Public Velocity(Max_Note_Index) As VelocityValue

        Public PitchWheel As PitchWheel
        Public PitchWheelValue As Integer
        Public Property PitchBendRange As Integer = 2

        Public Volume As ControllerValue
        Public Pan As ControllerValue
        Public Program As ProgramValue

        Enum StatusType As Byte
            Ready = 0
            Busy = 255
        End Enum

        Public Status As StatusType

        Sub New(ByRef track As Track, index As Integer, initialStatus As StatusType)
            Me.Track = track
            Me.Index = index
            Reset(initialStatus)
        End Sub

        Public Sub Reset(initialStatus As StatusType)
            Status = initialStatus
            For noteNum As Integer = 0 To Max_Note_Index
                NoteOn(noteNum) = False
            Next
            For noteNum As Integer = 0 To Max_Note_Index
                Note(noteNum) = New Note(0, VelocityValue.Normal)
            Next
            PitchWheel = New PitchWheel(0, PitchBendRange)
            Volume = ControllerValue.VolumeMin
            Pan = ControllerValue.PanCenter
            Program = ProgramValue.AcousticGrandPiano
        End Sub

        ' ********************************
        ' *** Methods for creating Midi notes and changing pitch

        Public Sub PlayNote(note As Note, velocity As VelocityValue, noteIndex As Integer)
            If NoteOn(noteIndex) = True Then
                StopNote(noteIndex)
            End If

            Track.AddNoteOnEvent(Index, note, velocity, noteIndex)

            SetPitch(note)
        End Sub

        Public Sub StopNote(noteIndex As Integer)
            If NoteOn(noteIndex) = True Then
                Track.AddNoteOffEvent(Index, Note(noteIndex), 0, noteIndex)
                'ResetPitch()
            End If
        End Sub

        Public Sub SetPitch(note As Note)
            If PitchWheel.Pitch <> note.Fraction Then
                PitchWheel.Pitch = note.Fraction
                Track.AddPitchWheelEvent(Index, PitchWheel)
            End If
        End Sub

        Public Sub BendPitch(newNote As Note, noteIndex As Integer)
            Dim pitchDelta As Single =
                newNote.Pitch - Note(noteIndex).Pitch

            'If new note is different from old note
            If pitchDelta <> 0 Then

                Dim basePitchBend As Single =
                    Note(noteIndex).Fraction

                Dim newPitchBend As Single =
                    basePitchBend + pitchDelta

                If Abs(newPitchBend) > PitchBendRange Then
                    PlayNote(newNote, Velocity(noteIndex), noteIndex)
                Else
                    'Avoid redundant pitch wheel events
                    If newPitchBend <> PitchWheel.Pitch Then
                        PitchWheel.Pitch = newPitchBend
                        Track.AddPitchWheelEvent(Index, PitchWheel)
                    End If
                End If

            End If

        End Sub

        Public Sub BendPitch(fNote As Single, noteIndex As Integer)
            Dim note As New Note(fNote)
            BendPitch(note, noteIndex)
        End Sub

        Public Sub ResetPitch()
            If PitchWheel.Pitch <> 0 Then
                PitchWheel.Pitch = 0
                Track.AddPitchWheelEvent(Index, PitchWheel)
            End If
        End Sub

        Public Sub ChangeVolume(volume As ControllerValue)
            If Me.Volume <> volume Then
                Track.AddVolumeChangeEvent(Index, volume)
            End If
        End Sub

        Public Sub ChangeVolume(attIndB As Single, attMax As Single, refVol As Byte)
            Dim midiVol As Integer =
                Midi.Volume.Value(attIndB, attMax, refVol)
            If midiVol <> Volume Then
                Track.AddVolumeChangeEvent(Index, midiVol)
            End If
        End Sub

        Public Sub ChangeProgram(program As ProgramValue)
            If Me.Program <> program Then
                Track.AddProgramChangeEvent(Index, program)
            End If
        End Sub

        Public Sub ChangePan(pan As ControllerValue)
            If Me.Pan <> pan Then
                Track.AddPanChangeEvent(Index, pan)
            End If
        End Sub

        Public Sub ChangePitchBendRange(semitones As Byte, cents As Byte)
            PitchWheel.Range = semitones
            Track.AddRPNPitchBendRangeEvent(Index, semitones, cents)
        End Sub

    End Class

    Public Class Note
        Public ReadOnly Property Hz As Single
        Public ReadOnly Property A4 As Single = 440

        Public ReadOnly Property Pitch As Single

        Public ReadOnly Property Number() As NoteNumber
            Get
                Dim num As Integer =
                    Floor(Pitch)
                If Sign(Fraction) = -1 Then
                    num += 1
                End If
                Return Math.Min(num, NoteNumber.Max)
            End Get
        End Property

        Public ReadOnly Property Fraction() As Single
            Get
                Dim frac As Single =
                    Pitch - Floor(Pitch)
                If frac >= 0.5 Then
                    frac -= 1
                End If
                Return frac
            End Get
        End Property

        Public Shadows ReadOnly Property ToString As String
            Get
                Static noteNames As String() = {"C", "C#", "D", "D#", "E", "F", "F#", "G", "G#", "A", "A#", "B"}

                Dim num As Single = Number
                Dim note As Integer = Floor(num)
                Dim octave As Integer =
                    (note \ 12) - 2
                note = note Mod 12

                Return $"Note {noteNames(note)}{octave}{If(Fraction <> 0, $" {If(Fraction > 0, "+", "")}{Fraction * 100:0} cents", "")}"
            End Get
        End Property

        Sub New(num As NoteNumber, frac As Single)
            'limit frac to < 1
            Dim sign As Integer =
                Math.Sign(frac)
            frac = Abs(frac)
            frac -= Floor(frac)
            frac *= sign

            _Pitch = Max(num + frac, 0)
            _Hz = Max(ValueToHz(Pitch), 0)
        End Sub

        Sub New(hz As Single)
            _Hz = Max(hz, 0)
            _Pitch = Max(HzToValue(hz), 0)
        End Sub

        Private Function ValueToHz(value As Single) As Single
            Return A4 * 2 ^ ((value - NoteNumber.A4) / 12)
        End Function

        Private Function HzToValue(hz As Single) As Single
            Return NoteNumber.A4 + 12 * Log(hz / A4, 2)
        End Function

    End Class

    Public Class PitchWheel
        'Pitch is stored internally as semitones
        Protected _Pitch As Single
        Public Property Pitch As Single
            Get
                Return _Pitch
            End Get
            Set(value As Single)
                _Pitch = value
            End Set
        End Property

        Protected _Range As Integer
        Public Property Range As Integer
            Get
                Return _Range
            End Get
            Set(value As Integer)
                _Range = Abs(value)
            End Set
        End Property

        Public ReadOnly Property Value As PitchWheelValue
            Get
                Dim semitoneIncrement As Integer =
                    PitchWheelValue.Center \ Range
                Dim val As Integer =
                    PitchWheelValue.Center + (Pitch * semitoneIncrement)
                'Clamp the output value to the maximum supported by the Midi standard
                val = Clamp(val, PitchWheelValue.Min, PitchWheelValue.Max)
                Return val
            End Get
        End Property

        Sub New(pitch As Single, range As Integer)
            Me.Pitch = pitch
            Me.Range = range
        End Sub

    End Class

    Public Class Volume
        Public Shared Function Value(attIndB As Single, attMax As Single, refVol As Byte) As Byte
            Dim midiVol As Integer

            If attIndB >= attMax Then
                midiVol = 0
            Else
                midiVol = refVol * 2 ^ (-attIndB / 6.020599913) '3)
                'midiVol = refVol - (refVol * (attIndB / attMax))
                'midiVol = refVol * (10 ^ (-attIndB / 20))
            End If

            'Clamp volume
            midiVol = Clamp(midiVol, ControllerValue.VolumeMin, ControllerValue.VolumeMax)

            Return midiVol
        End Function
    End Class

    Public Class TimeDivision

        Public ReadOnly Property Data As Byte()
            Get
                Dim bytes(1) As Byte

                Select Case Format
                    Case DivisionFormat.TicksPerQuarterNote
                        bytes(0) = (TicksPerQuarterNote >> 8) And &H7F
                        bytes(1) = TicksPerQuarterNote And &HFF
                    Case DivisionFormat.SMTPEFrames
                        bytes(0) = FramesPerSecond
                        bytes(1) = TicksPerFrame
                End Select

                Return bytes
            End Get
        End Property

        Enum DivisionFormat As Byte
            TicksPerQuarterNote
            SMTPEFrames
        End Enum

        Public ReadOnly Property Format As DivisionFormat

        Public ReadOnly Property TicksPerQuarterNote As UShort

        Enum FramesPerSecondValue As Byte
            'All values are two's compliment negative numbers
            Film24Hz = &HE8 '24
            PAL25Hz = &HE7
            NTSC29Hz = &HE3
            NTSC30Hz = &HE2
        End Enum

        Public ReadOnly Property FramesPerSecond As FramesPerSecondValue
        Public ReadOnly Property TicksPerFrame As Byte

        Sub New(ticksPerQuarterNote As UShort)
            Format = DivisionFormat.TicksPerQuarterNote
            _TicksPerQuarterNote = ticksPerQuarterNote
        End Sub

        Sub New(framesPerSecond As FramesPerSecondValue, ticksPerFrame As Byte)
            Format = DivisionFormat.SMTPEFrames
            _FramesPerSecond = framesPerSecond
            _TicksPerFrame = ticksPerFrame
        End Sub

    End Class

    Public MustInherit Class MidiFile

        Public Const HeaderChunkID As String = "MThd"
        Public Const HeaderChunkSize As UInteger = 6

        Public Const TrackChunkID As String = "MTrk"
        Public Const TrackChunkSize As UInteger = 0

        Public Enum MidiFormat As UShort
            Format0 = 0
            Format1 = 1
            Format2 = 2
        End Enum
        Public ReadOnly Property Format As MidiFormat
        Public ReadOnly Property TrackCount As UShort
            Get
                Return Tracks.Count
            End Get
        End Property
        Public ReadOnly Property TimeDivision As TimeDivision
        Public ReadOnly Property Tracks As List(Of Track)
        Public ReadOnly Property Data As List(Of Byte)

        Sub New(format As MidiFormat, td As TimeDivision)
            _Format = format
            _TimeDivision = td
            _Tracks = New List(Of Track)
            _Data = New List(Of Byte)
        End Sub

        Public Sub Save(path As String)
            If Format = MidiFormat.Format0 And Tracks.Count > 1 Then
                'TODO: If there is more than one track, save to format 1 instead.'
            End If

            'Add file header
            Dim fileHeader As Byte() =
                GetFileHeader(Me)
            Data.AddRange(fileHeader)

            'Add tracks
            For Each track As Track In Tracks
                'Add track header
                With track
                    If .IsOpen Then
                        .Close()
                    End If
                End With
                Dim trackHeader As Byte() =
                GetTrackHeader(track)
                Data.AddRange(trackHeader)

                'Add track data
                Data.AddRange(track.Data)
            Next

            'Write file
            Try
                Using fsOut As New FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)
                    Using bw As New BinaryWriter(fsOut)
                        bw.Write(Data.ToArray)
                    End Using
                End Using
            Catch ex As Exception
                MsgBox(ex.Message.ToString)
            End Try

        End Sub

        Shared Function GetFileHeader(ByRef file As MidiFile) As Byte()
            Dim header As New List(Of Byte)

            With header
                Dim bytes As Byte()

                bytes = Text.Encoding.ASCII.GetBytes(HeaderChunkID)
                .AddRange(bytes)

                bytes = BitConverter.GetBytes(HeaderChunkSize)
                Array.Reverse(bytes)
                .AddRange(bytes)

                bytes = BitConverter.GetBytes(file.Format)
                Array.Reverse(bytes)
                .AddRange(bytes)

                bytes = BitConverter.GetBytes(file.TrackCount)
                Array.Reverse(bytes)
                .AddRange(bytes)

                bytes = BitConverter.GetBytes(file.TimeDivision.TicksPerQuarterNote)
                Array.Reverse(bytes)
                .AddRange(bytes)

                Return .ToArray
            End With

        End Function

        Shared Function GetTrackHeader(ByRef track As Track) As Byte()
            Dim header As New List(Of Byte)

            With header
                Dim bytes As Byte()

                bytes = Text.Encoding.ASCII.GetBytes(TrackChunkID)
                .AddRange(bytes)

                bytes = BitConverter.GetBytes(track.Data.Count)
                Array.Reverse(bytes)
                .AddRange(bytes)

                Return .ToArray
            End With

        End Function

    End Class

    Public Class MidiFormat0File : Inherits MidiFile

        Sub New(ByRef track As Track, td As TimeDivision)
            MyBase.New(MidiFormat.Format0, td)
            Tracks.Add(track)
        End Sub

        '    Public Sub Save(path As String)
        '        'Add file header
        '        Dim fileHeader As Byte() =
        '            GetFileHeader(Me)
        '        Data.AddRange(fileHeader)

        '        'Add track header
        '        With Tracks(0)
        '            If .IsOpen Then
        '                .Close()
        '            End If
        '        End With
        '        Dim trackHeader As Byte() =
        '            GetTrackHeader(Tracks(0))
        '        Data.AddRange(trackHeader)

        '        'Add track data
        '        Data.AddRange(Tracks(0).Data)

        '        'Write file
        '        Try
        '            Using fsOut As New FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)
        '                Using bw As New BinaryWriter(fsOut)
        '                    bw.Write(Data.ToArray)
        '                End Using
        '            End Using
        '        Catch ex As Exception
        '            MsgBox(ex.Message.ToString)
        '        End Try

        '    End Sub

    End Class

    Public Class MidiFormat1File : Inherits MidiFile

        Sub New(td As TimeDivision)
            MyBase.New(MidiFormat.Format1, td)
        End Sub

    End Class

End Class