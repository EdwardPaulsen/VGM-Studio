Imports System.IO
Imports System.Math

Public Class Midi

#Region "Enums"

    Public Enum TimeDivisionType As Integer
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
        MetaEvent = &HFF
    End Enum

    Public Enum MetaEventType
        TrackNumber = &H0
        TextEvent = &H1
        CopyrightNotice = &H2
        TrackName = &H3
        InstrumentName = &H4
        Lyric = &H5
        Marker = &H6
        CuePoint = &H7
        EndOfTrack = &H2F
        SetTempo = &H51
        TimeSignature = &H58
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

        Volume0Percent = 0
        Volume10Percent = 11
        Volume20Percent = 24
        Volume30Percent = 38
        Volume40Percent = 50
        Volume50Percent = 63
        Volume60Percent = 76
        Volume70Percent = 88
        Volume80Percent = 101
        Volume90Percent = 114
        Volume100Percent = 127

        PanLeft = &H0
        PanRight = &H7F
        PanCenter = &H3F

        PanLeft100Right0 = 0
        PanLeft75Right25 = 31
        PanLeft50Right50 = 63
        PanLeft25Right75 = 95
        PanLeft0Right100 = 127

        PitchBendRanageDefault = 2
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

    Public Enum PitchWheelValue As Short
        Min = &H0S
        Max = &H3FFFS
        Center = &H2000S
    End Enum

    Public Enum RPN As Short
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
        Public ReadOnly Property Data() As List(Of Byte)

        Sub New(ticks As Long)
            Data = New List(Of Byte)
            With Data
                .Add(ticks And &H7F)
                Do
                    ticks >>= 7
                    If ticks = 0 Then Exit Do
                    .Insert(0, (ticks And &H7F) Or &H80)
                Loop
            End With
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
            'Value is in LSB,MSB (fine, coarse) order
            Return {value And &H7F, (value >> 7) And &H7F}
        End Function

        Private Function BytesToValue(bytes As Byte()) As PitchWheelValue
            Dim value As PitchWheelValue =
                bytes(1) << 7
            value += bytes(0)
            Return value
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
            'With EventDataList
            '    .Add(CByte(&H0))
            'End With
        End Sub
    End Class

    Public Class SetTempoEvent : Inherits MetaEvent
        Const Microsecs_Per_Minute As Long = 60000000

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
        Public Property Tracks As List(Of Track)

        Sub New()
            _Tracks = New List(Of Track)
        End Sub

        Public Sub Tick(ticks As Long)
            For Each track As Track In Tracks
                track.Tick(ticks)
            Next
        End Sub

        Public Sub Tick(ticks As Single)
            For Each track As Track In Tracks
                track.Tick(ticks)
            Next
        End Sub

    End Class

    Public Class Track
        Public ReadOnly Property Sequence As Sequence

        Public ReadOnly Property Num As Integer
            Get
                Dim index As Integer = 0
                For Each track As Track In Sequence.Tracks
                    If track Is Me Then
                        Return index
                    End If
                    index += 1
                Next
                Return -1
            End Get
        End Property

        Public Channel(ChannelValue.Max) As Channel

        Public ReadOnly Property TrackData As List(Of Byte)
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

        Private ReadOnly Property Ticks As Long 'Single

        Sub New(ByRef sequence As Sequence)
            _Sequence = sequence
            sequence.Tracks.Add(Me)

            TrackData = New List(Of Byte)

            For chIdx As Integer = 0 To Channel.GetUpperBound(0)
                Channel(chIdx) = New Channel(Me, chIdx, Midi.Channel.StatusType.Ready)
            Next

            IsOpen = True
        End Sub

        Public Sub Tick(ticks As Long)
            _Ticks += ticks
        End Sub

        ' ********************************
        ' *** Methods for adding events

        Private Sub AddDeltaTime(dt As DeltaTime)
            TrackData.AddRange(dt.Data)
        End Sub

        Public Sub AddEvent(midievent As MidiEvent)
            If _IsClosed = True Then Exit Sub

            AddDeltaTime(New DeltaTime(_Ticks))
            _Ticks = 0
            TrackData.AddRange(midievent.Data)

            If TypeOf (midievent) Is EndOfTrackEvent Then
                _IsClosed = True
            End If
        End Sub

        'NoteOff = &H80
        Public Sub AddNoteOffEvent(channel As ChannelValue, note As Note, velocity As VelocityValue, noteIndex As Integer)
            AddEvent(New NoteOffEvent(channel, note.Number, velocity))

            With Me.Channel(channel)
                .NoteOn(noteIndex) = False
                .Note(noteIndex) = note
                .Velocity(noteIndex) = velocity
            End With
        End Sub

        'NoteOn = &H90
        Public Sub AddNoteOnEvent(channel As ChannelValue, note As Note, velocity As VelocityValue, noteIndex As Integer)
            AddEvent(New NoteOnEvent(channel, note.Number, velocity))

            With Me.Channel(channel)
                .NoteOn(noteIndex) = True
                .Note(noteIndex) = note
                .Velocity(noteIndex) = velocity
            End With
        End Sub

        'NoteAftertouch = &HA0
        Public Sub AddNoteAftertouchEvent(channel As ChannelValue, note As NoteNumber, aftertouch As VelocityValue)
            AddEvent(New NoteAftertouchEvent(channel, note, aftertouch))
        End Sub

        'ControllerChange = &HB0
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

        'ProgramChange = &HC0
        Public Sub AddProgramChangeEvent(channel As ChannelValue, program As ProgramValue)
            AddEvent(New ProgramChangeEvent(channel, program))
            With Me.Channel(channel)
                .Program = program
            End With
        End Sub

        'ChannelAftertouch = &HD0
        Public Sub AddChannelAftertouchEvent(channel As ChannelValue, aftertouch As VelocityValue)
            AddEvent(New ChannelAftertouchEvent(channel, aftertouch))
        End Sub

        'PitchWheel = &HE0
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

        'MetaEvent = &HFF
        Public Sub Close()
            Dim eotEvent As New EndOfTrackEvent
            AddEvent(eotEvent)
            _IsClosed = True
        End Sub

        Public Sub InsertTrackHeader()
            ' Initialize Track header ("MTrk")
            Dim MTrk As Byte() = {Asc("M"), Asc("T"), Asc("r"), Asc("k")}
            Dim TrackLen As UInteger = Convert.ToUInt32(TrackData.Count)

            With TrackData
                .InsertRange(0, MTrk)
            End With

            With TrackData
                .Insert(4, (TrackLen >> 24) And &HFF)
                .Insert(5, (TrackLen >> 16) And &HFF)
                .Insert(6, (TrackLen >> 8) And &HFF)
                .Insert(7, TrackLen And &HFF)
            End With
        End Sub

    End Class

    Public Class Channel
        Public ReadOnly Property Track As Track
        Public ReadOnly Property Num As Integer

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

        Sub New(ByRef track As Track, num As Integer, initialStatus As StatusType)
            _Track = track
            _Num = num
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
            'For noteNum As Integer = 0 To Max_Note_Index
            '    Velocity(noteNum) = VelocityValue.Normal
            'Next
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

            Track.AddNoteOnEvent(Num, note, velocity, noteIndex)

            SetPitch(note)
            'PitchWheel.Pitch = note.Fraction
            'Track.AddPitchWheelEvent(Num, pitchWheel)
        End Sub

        'Public Sub PlayNote(fNote As Single, velocity As VelocityValue, noteIndex As Integer)
        '    Dim note As New Note(fNote)
        '    PlayNote(note, velocity, noteIndex)
        'End Sub

        Public Sub StopNote(noteIndex As Integer)
            If NoteOn(noteIndex) = True Then
                Track.AddNoteOffEvent(Num, Note(noteIndex), 0, noteIndex)
                'ResetPitch()
            End If
        End Sub

        Public Sub SetPitch(note As Note)
            If PitchWheel.Pitch <> note.Fraction Then
                PitchWheel.Pitch = note.Fraction
                Track.AddPitchWheelEvent(Num, PitchWheel)
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
                    If newPitchBend <> PitchWheel.Pitch Then 'avoid redundant pitch wheel events
                        PitchWheel.Pitch = newPitchBend
                        Track.AddPitchWheelEvent(Num, PitchWheel)
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
                Track.AddPitchWheelEvent(Num, PitchWheel)
            End If
        End Sub

        Public Sub ChangeVolume(volume As ControllerValue)
            If Me.Volume <> volume Then
                Track.AddVolumeChangeEvent(Num, volume)
            End If
        End Sub

        Public Sub ChangeVolume(attIndB As Single, attMax As Single, refVol As Byte)
            Dim midiVol As Integer =
                Midi.Volume.Value(attIndB, attMax, refVol)
            If midiVol <> Volume Then
                Track.AddVolumeChangeEvent(Num, midiVol)
            End If
        End Sub

        Public Sub ChangeProgram(program As ProgramValue)
            If Me.Program <> program Then
                Track.AddProgramChangeEvent(Num, program)
            End If
        End Sub

        Public Sub ChangePan(pan As ControllerValue)
            If Me.Pan <> pan Then
                Track.AddPanChangeEvent(Num, pan)
            End If
        End Sub

        Public Sub ChangePitchBendRange(semitones As Byte, cents As Byte)
            PitchWheel.Range = semitones
            Track.AddRPNPitchBendRangeEvent(Num, semitones, cents)
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
                Dim note As Integer = Math.Floor(num)
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

            ''Keep initial pitch bend between -0.5 and +0.5
            'If frac >= 0.5 Then
            '    num = Min(num + 1, NoteNumber.Max)
            '    frac -= 1
            'ElseIf frac <= -0.5 Then
            '    num = Max(num - 1, NoteNumber.Min)
            '    frac += 1
            'End If

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
        Public Property Pitch As Single

        Private _Range As Integer
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
                'Clip the output value to the maximum supported by the Midi standard
                val = Min(Max(val, PitchWheelValue.Min), PitchWheelValue.Max)
                Return val
            End Get
        End Property

        Sub New(pitch As Single, range As Integer)
            _Pitch = pitch
            Me.Range = range
        End Sub

        ''Pitch is stored internally as semitones
        'Public ReadOnly Property Pitch As Single

        'Public ReadOnly Property Value(range As Integer) As PitchWheelValue
        '    Get
        '        Dim semitoneIncrement As Single =
        '            8192 / range 'PitchWheelValue.Max / (range * 2)
        '        Dim val As Integer =
        '            PitchWheelValue.Center + (Pitch * semitoneIncrement)
        '        'Clip the output value to the maximum supported by the Midi standard
        '        val = Min(Max(val, PitchWheelValue.Min), PitchWheelValue.Max)
        '        Return val
        '    End Get
        'End Property

        'Sub New(semitones As Single)
        '    _Pitch = semitones
        'End Sub

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

            'Limit volume
            midiVol = Min(Max(midiVol, ControllerValue.VolumeMin), ControllerValue.VolumeMax)

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
                        Dim negFrames As Short =
                            FramesPerSecond
                        negFrames = -negFrames
                        Dim frames As Byte() =
                            BitConverter.GetBytes(negFrames)
                        bytes(0) = frames(0)
                        bytes(1) = TicksPerFrame

                End Select

                Return bytes

            End Get
        End Property

        Enum DivisionFormat As Integer
            TicksPerQuarterNote
            SMTPEFrames
        End Enum

        Public ReadOnly Property Format As DivisionFormat

        Public ReadOnly Property TicksPerQuarterNote As Short

        Enum FramesPerSecondValue As Byte
            Film24Hz = 24
            PAL25Hz = 25
            NTSC29Hz = 29
            NTSC30Hz = 30
        End Enum

        Public ReadOnly Property FramesPerSecond As FramesPerSecondValue
        Public ReadOnly Property TicksPerFrame As Byte

        Sub New(ticksPerQuarterNote As Short)
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
        Public ReadOnly Property Header As FileHeader
        Public MustOverride ReadOnly Property FileData As List(Of Byte)
        Public ReadOnly Property Format As Integer
        Public Property TrackCount As Integer

        Public Property TimeDivision As TimeDivision

        Structure FileHeader
            Shared ReadOnly MThd As String = "MThd"
            Shared ReadOnly Length As Integer = 6
            Public Property Format As Short
            Public Property TrackCount As Short

            Public ReadOnly Property Data As Byte()
                Get
                    Dim byteList As New List(Of Byte)
                    Dim bytes As Byte()

                    bytes = Text.Encoding.ASCII.GetBytes(MThd.ToCharArray)
                    byteList.AddRange(bytes)

                    bytes = BitConverter.GetBytes(Length)
                    Array.Reverse(bytes)
                    byteList.AddRange(bytes)

                    bytes = BitConverter.GetBytes(Format)
                    Array.Reverse(bytes)
                    byteList.AddRange(bytes)

                    bytes = BitConverter.GetBytes(TrackCount)
                    Array.Reverse(bytes)
                    byteList.AddRange(bytes)

                    Return byteList.ToArray
                End Get
            End Property

            Sub New(format As Integer, trackCount As Integer)
                _Format = format
                _TrackCount = trackCount
            End Sub

        End Structure

        Sub New(format As Integer, trackCount As Integer, td As TimeDivision)
            _Header = New FileHeader(format, trackCount)
            _TimeDivision = td
        End Sub

        Protected Sub AddFileHeader()
            'Dim MThd As Byte() = {Asc("M"), Asc("T"), Asc("h"), Asc("d")}   'Midi Track header ("MThd")
            ''***All of the following values are high byte first in the file
            'Dim HeaderLen As Byte() = {0, 0, 0, 6}      'header length (=6) (4 bytes)
            'Dim MidiFormat As Byte() = {0, 0}           'Midi format (=0) (2 bytes)
            'Dim NumTracks As Byte() = {0, 1}            'track count (=1) (2 bytes)

            With FileData
                .AddRange(Header.Data)
                .AddRange(TimeDivision.Data)
            End With

            'With FileData
            '    .AddRange(MThd)
            '    .AddRange(HeaderLen)
            '    .AddRange(MidiFormat)
            '    .AddRange(NumTracks)
            '    .AddRange(TimeDivision.Data)
            'End With

        End Sub

    End Class

    Public Class MidiFormat0File
        Inherits MidiFile

        Public Overrides ReadOnly Property FileData As List(Of Byte)

        Public ReadOnly Property Track As Track

        Sub New(ByRef track As Track, td As TimeDivision)
            MyBase.New(0, 1, td)
            _FileData = New List(Of Byte)
            _Track = track
        End Sub

        Public Sub Save(path As String)
            AddFileHeader()

            With Track
                If .IsOpen Then
                    .Close()
                End If
                .InsertTrackHeader()
            End With

            FileData.AddRange(Track.TrackData)

            Try
                Using fsOut As New FileStream(path, FileMode.Create, FileAccess.Write, FileShare.None)
                    Using bw As New BinaryWriter(fsOut)
                        bw.Write(FileData.ToArray)
                    End Using
                End Using
            Catch ex As Exception
                MsgBox(ex.Message.ToString)
            End Try

        End Sub

    End Class

End Class