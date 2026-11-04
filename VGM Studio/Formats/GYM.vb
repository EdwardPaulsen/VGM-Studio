'Imports System
'Imports System.IO

'Public Class GYM

'    Public Enum Command As Byte
'        Delay166ms = 0
'        YM2612Port0 = 1
'        YM2612Port1 = 2
'        PSG = 3
'    End Enum

'    Public Shared Function ToVGM(ByVal GYMFile As GYMFile) As VGMFile
'        Dim GYMCommand As Byte
'        Dim bytPort, bytRegister, bytData As Byte
'        Dim TotalSamples As Integer

'        Dim alVGMData As ArrayList =
'            New ArrayList

'        Dim IEnumGYMFile As System.Collections.IEnumerator =
'            GYMFile.Data.GetEnumerator

'        Dim WaitsOK As Boolean = False

'        While ienumGYMFile.MoveNext
'            GYMCommand = CByte(IEnumGYMFile.Current)
'            Select Case GYMCommand
'                Case GYM.Command.Delay166ms
'                    If WaitsOK Then
'                        alVGMData.Add(CByte(VGM.Command.Wait735Samples))
'                        TotalSamples += 735
'                    End If
'                Case GYM.Command.YM2612Port0, GYM.Command.YM2612Port1
'                    bytPort = IIf( _
'                        GYMCommand = GYM.Command.YM2612Port0, _
'                        VGM.Command.YM2612Port0, _
'                        VGM.Command.YM2612Port1 _
'                        )

'                    With IEnumGYMFile
'                        .MoveNext()
'                        bytRegister = CByte(.Current)
'                        .MoveNext()
'                        bytData = CByte(.Current)
'                    End With

'                    With alVGMData
'                        .Add(bytPort)
'                        .Add(bytRegister)
'                        .Add(bytData)
'                    End With

'                    WaitsOK = True
'                Case GYM.Command.PSG
'                    bytPort = VGM.Command.PSG
'                    With IEnumGYMFile
'                        .MoveNext()
'                        bytData = CByte(.Current)
'                    End With

'                    With alVGMData
'                        .Add(bytPort)
'                        .Add(bytData)
'                    End With

'                    WaitsOK = True
'                Case Else
'                    'Exit While
'            End Select
'            Application.DoEvents()
'        End While

'        alVGMData.Add(CByte(VGM.Command.EndOfSoundData))

'        Dim VGMFile As New VGMFile
'        With VGMFile
'            .Vgm_ = "Vgm "
'            .Offset = 0
'            .Version = "1.1.0.0"
'            .HzPSG = 3595740
'            .HzFM = 7670442
'            .GD3Offset = 0
'            .TotalSamples = TotalSamples
'            .LoopOffset = 0
'            .LoopSamples = 0
'            .Rate = 60
'            .Data = alVGMData.ToArray(GetType(Byte))
'        End With

'        Return VGMFile

'    End Function

'    Private Function ToMIDIFile(ByVal GYMFile As GYMFile) As MIDIFile
'        Return Nothing
'    End Function

'End Class

'Public Class GYMFile

'#Region " GYMX Header Specification #"
'    '+------------------+------------------------------------------------+
'    '| width (in bytes) |            description                         |
'    '|------------------|------------------------------------------------|
'    '|        4         | Identifier: the characters 'G' 'Y' 'M' 'X'     |
'    '|------------------|------------------------------------------------|
'    '|       32         | Song title                                     |
'    '|------------------|------------------------------------------------|
'    '|       32         | Game title                                     |
'    '|------------------|------------------------------------------------|
'    '|       32         | Game publisher                                 |
'    '|------------------|------------------------------------------------|
'    '|       32         | Name of emulator used to create the gym file   |
'    '|------------------|------------------------------------------------|
'    '|       32         | Name of person who created the gym file        |
'    '|------------------|------------------------------------------------|
'    '|      256         | Miscelaneous comments                          |
'    '|------------------|------------------------------------------------|
'    '|        4         | Loop integer (unsigned little endian). Zero if |
'    '|                  | the gym should not loop. Otherwise it specifies|
'    '|                  | the point (1 being the very beginning) at which|
'    '|                  | looping begins in 1/60 seconds.                |
'    '|------------------|------------------------------------------------|
'    '|        4         | Specifies the uncompresed size (unsigned little|
'    '|                  | endian) of the raw gym data if the gym data has|
'    '|                  | been compressed with zlib (deflate algorythm). |
'    '|                  | Zero if not compressed.                        |
'    '|------------------|------------------------------------------------|
'    '|        X         | raw gym data                                   |
'    '+------------------+------------------------------------------------+
'#End Region
'#Region " GYM Variables "
'    Public Class FileHeader
'        Public GYMX() As Byte
'        Public SongTitle() As Byte
'        Public GameTitle() As Byte
'        Public GamePublisher() As Byte
'        Public EmulatorName() As Byte
'        Public GYMCreator() As Byte
'        Public Comments() As Byte
'        Public LoopInteger As UInteger
'        Public UncompressedSize As UInteger

'        Sub New()
'        End Sub

'        Public Shared Function FromFile(ByVal fileName As String) As FileHeader
'            Return Nothing
'        End Function
'    End Class

'    Dim mbytGYMData As Byte()
'#End Region
'#Region " GYM Properties "
'    Public ReadOnly Property Data() As Byte()
'        Get
'            Return mbytGYMData
'        End Get
'    End Property

'#End Region

'    Sub New()
'    End Sub

'    Sub New(ByVal fileName As String)
'        Dim fsIn As New System.IO.FileStream(fileName, FileMode.Open, FileAccess.Read, FileShare.Read)
'        Dim br As New BinaryReader(fsIn)

'        Dim fh As New FileHeader
'        Try
'            fh.GYMX = br.ReadBytes(4)
'            Dim aenc As New System.Text.ASCIIEncoding()
'            Dim chrGYMX() As Char = aenc.GetChars(fh.GYMX)
'            Dim strVGM_ As String = CStr(chrGYMX)
'            If strVGM_ = "GYMX" Then
'                With br
'                    fh.SongTitle = .ReadBytes(32)
'                    fh.GameTitle = .ReadBytes(32)
'                    fh.GamePublisher = .ReadBytes(32)
'                    fh.EmulatorName = .ReadBytes(32)
'                    fh.GYMCreator = .ReadBytes(32)
'                    fh.Comments = .ReadBytes(256)
'                    fh.LoopInteger = .ReadUInt32
'                    fh.UncompressedSize = .ReadUInt32
'                End With
'            Else
'                fh = Nothing
'                br.BaseStream.Seek(0, SeekOrigin.Begin)
'            End If
'        Catch ex As Exception
'            Dim title As String = "Error"
'            MsgBox(ex.Message, MsgBoxStyle.OKOnly + MsgBoxStyle.Exclamation, title)
'            br.Close()
'            br = Nothing
'            Exit Sub
'        End Try

'        Try
'            With br
'                mbytGYMData = .ReadBytes(.BaseStream.Length - .BaseStream.Position)
'            End With
'        Catch ex As Exception
'            Dim title As String = "Error"
'            MsgBox(ex.Message, MsgBoxStyle.OKOnly + MsgBoxStyle.Exclamation, title)
'            Exit Sub
'        Finally
'            br.Close()
'            br = Nothing
'        End Try
'    End Sub

'    Protected Overrides Sub Finalize()
'        mbytGYMData = Nothing
'    End Sub

'End Class