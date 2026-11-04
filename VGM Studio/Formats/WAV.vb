Imports System.IO
Imports System.Text

Namespace WAV

#Region "Enums"
    Public Enum WaveFormatType As Short
        Unknown = &H0S          'Unknown Format
        PCM = &H1S              'PCM
        ADPCM = &H2S            'Microsoft ADPCM Format
        IEEEFloat = &H3S        'IEEE Float
        Extensible = &HFFFES    'WAVE_FORMAT_EXTENSIBLE
    End Enum

    <Flags()>
    Public Enum SpeakerFlags As Integer
        FrontLeft = &H1I
        FrontRight = &H2I
        FrontCenter = &H4I
        LowFrequency = &H8I
        BackLeft = &H10I
        BackRight = &H20I
        FrontLeftOfCenter = &H40I
        FrontRightOfCenter = &H80I
        BackCenter = &H100I
        SideLeft = &H200I
        SideRight = &H400I
        TopCenter = &H800I
        TopFrontLeft = &H1000I
        TopFrontCenter = &H2000I
        TopFrontRight = &H4000I
        TopBackLeft = &H8000I
        TopBackCenter = &H10000I
        TopBackRight = &H20000I
        'Reserved = &H80000000I
    End Enum

    Structure RIFFFILEHEADER 'Len = 8
        Dim RiffID As String
        Dim FileLength As Integer
    End Structure

    Structure RIFFFILEFORMATHEADER 'Len = 4
        Dim FormatID As String
    End Structure

    Structure RIFFCHUNKHEADER 'Len = 8
        Dim ChunkID As String
        Dim ChunkLength As Integer
    End Structure

    Structure WAVEFORMATEX 'Len = 18
        Dim FormatTag As Short
        Dim Channels As Short
        Dim SamplesPerSec As Integer
        Dim AvgBytesPerSec As Integer
        Dim BlockAlign As Short
        Dim BitsPerSample As Short
        Dim Size As Short
    End Structure

    Structure WAVEFORMATEXTENSIBLE 'Len = 22
        Dim ValidBitsPerSample As Short
        Dim ChannelMask As Integer
        Dim SubFormat As Guid
    End Structure

#End Region

    Public Class WAVFile

        Dim fileHeader As New RIFFFILEHEADER()
        Dim fileFormatHeader As New RIFFFILEFORMATHEADER()
        Dim formatChunkHeader As New RIFFCHUNKHEADER()
        Dim dataChunkHeader As New RIFFCHUNKHEADER()
        Dim wfEx As New WAVEFORMATEX()
        Dim wfExt As New WAVEFORMATEXTENSIBLE()

        Private SampleData8BitPCM As List(Of Byte)
        Private SampleData16BitPCM As List(Of Short)
        Private SampleDataIEEEFloat As List(Of Single)

        Public ReadOnly Property MetadataLength As Long
            Get
                Return 38
            End Get
        End Property

        Public ReadOnly Property SampleDataLength As Long
            Get
                Dim value As Long
                Select Case wfEx.FormatTag
                    Case WaveFormatType.PCM
                        Select Case wfEx.BitsPerSample'mBitsPerSample
                            Case 8
                                value = SampleData8BitPCM.LongCount
                            Case 16
                                value = SampleData16BitPCM.LongCount
                        End Select
                    Case WaveFormatType.IEEEFloat
                        value = SampleDataIEEEFloat.LongCount
                End Select
                Return value * wfEx.BlockAlign
            End Get
        End Property

        Sub New(bitsPerSample As Short, validBitsPerSample As Short, samplesPerSecond As Integer, numChannels As Short)
            With fileHeader
                .RiffID = "RIFF" '&H46464952I
            End With

            With fileFormatHeader
                .FormatID = "WAVE" '&H45564157I 
            End With

            With formatChunkHeader
                .ChunkID = "fmt " '&H20746D66I 
                '.ChunkLength = Len(wfEx) + Len(wfExt)
                .ChunkLength = Len(wfEx)
            End With

            With wfEx
                '.FormatTag = WaveFormatType.Extensible
                .FormatTag = WaveFormatType.PCM
                .BitsPerSample = bitsPerSample
                .SamplesPerSec = samplesPerSecond
                .Channels = numChannels
                .BlockAlign = (.BitsPerSample * .Channels) \ 8
                .AvgBytesPerSec = .BlockAlign * .SamplesPerSec
                .Size = 0 'Len(wfExt)
            End With

            'With wfExt
            '    .ValidBitsPerSample = validBitsPerSample
            '    .ChannelMask = SpeakerFlags.FrontLeft + SpeakerFlags.FrontRight
            '    .SubFormat = New Guid("00000001-0000-0010-8000-00aa00389b71")
            'End With

            With dataChunkHeader
                .ChunkID = "data" '&H61746164I
                '.ChunkLength = SampleDataLength
            End With
        End Sub

        Sub New(samplesPerSecond As Integer, ByRef sampleData As Byte(), numChannels As Integer)
            Me.New(8, 8, samplesPerSecond, numChannels)
            SampleData8BitPCM = New List(Of Byte)
            SampleData8BitPCM.AddRange(sampleData)
        End Sub

        Sub New(samplesPerSecond As Integer, ByRef sampleData As Short(), numChannels As Integer)
            Me.New(16, 16, samplesPerSecond, numChannels)
            SampleData16BitPCM = New List(Of Short)
            SampleData16BitPCM.AddRange(sampleData)
        End Sub

        Sub New(samplesPerSecond As Integer, ByRef sampleData As Single(), numChannels As Integer)
            Me.New(32, 32, samplesPerSecond, numChannels)
            wfEx.FormatTag = WaveFormatType.IEEEFloat
            SampleDataIEEEFloat = New List(Of Single)
            SampleDataIEEEFloat.AddRange(sampleData)
        End Sub

        Public Sub Save(ByRef stream As Stream, leaveOpen As Boolean)
            dataChunkHeader.ChunkLength = SampleDataLength

            Dim bw As New BinaryWriter(stream)

            With fileHeader
                bw.Write(Encoding.ASCII.GetBytes(.RiffID))
                bw.Write(.FileLength)
            End With
            With fileFormatHeader
                bw.Write(Encoding.ASCII.GetBytes(.FormatID))
            End With
            With formatChunkHeader
                bw.Write(Encoding.ASCII.GetBytes(.ChunkID))
                bw.Write(.ChunkLength)
            End With
            With wfEx
                bw.Write(.FormatTag)
                bw.Write(.Channels)
                bw.Write(.SamplesPerSec)
                bw.Write(.AvgBytesPerSec)
                bw.Write(.BlockAlign)
                bw.Write(.BitsPerSample)
                bw.Write(.Size)
            End With
            'With wfxtWaveFormatExtensible
            '    bw.Write(.ValidBitsPerSample)
            '    bw.Write(.ChannelMask)
            '    Dim sf As Byte() = .SubFormat.ToByteArray
            '    bw.Write(sf)
            'End With
            With dataChunkHeader
                bw.Write(Encoding.ASCII.GetBytes(.ChunkID))
                bw.Write(.ChunkLength)
            End With

            If SampleData8BitPCM IsNot Nothing Then
                bw.Write(SampleData8BitPCM.ToArray)
            ElseIf SampleData16BitPCM IsNot Nothing Then
                For i As Long = 0 To SampleData16BitPCM.LongCount - 1
                    'bw.Write(BitConverter.GetBytes(SampleData16BitPCM(i)))
                    bw.Write(SampleData16BitPCM(i))
                Next
            ElseIf SampleDataIEEEFloat IsNot Nothing Then
                For i As Long = 0 To SampleDataIEEEFloat.LongCount - 1
                    'bw.Write(BitConverter.GetBytes(SampleDataIEEEFloat(i)))
                    bw.Write(SampleDataIEEEFloat(i))
                Next
            End If

            'Write data length to header
            bw.BaseStream.Seek(4, SeekOrigin.Begin)
            bw.Write(CUInt(bw.BaseStream.Length - 8))

            'Close BinaryWriter if necessary
            If leaveOpen = False Then
                bw.Close()
            End If

        End Sub

        Public Sub Save(path As String)
            Try
                Using fsOut As New FileStream(path, FileMode.Create)
                    Save(fsOut, False)
                End Using
            Catch ex As Exception
                MsgBox(ex.Message.ToString)
            End Try
        End Sub

    End Class

End Namespace