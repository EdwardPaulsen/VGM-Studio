Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports Microsoft.Win32
Imports VGM_Studio.VGM

Class VGMEventsPage

    Public WithEvents ViewModel As EventsPageViewModel

    Public Sub New(fileName As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ViewModel = New EventsPageViewModel(fileName)
        DataContext = ViewModel

    End Sub

    Private Property HasBeenPopulated As Boolean = False

    Private Sub VGMEventsPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If HasBeenPopulated Then Exit Sub
        ViewModel.StartExport()
        HasBeenPopulated = True
    End Sub

    Private Sub OnCompleted(sender As Object, e As EventArgs) Handles ViewModel.Completed
        Dispatcher.Invoke(Sub()
                              MainListBox.ItemsSource = ViewModel.EventListItems

                              'Filter wait commands by default
                              Dim view As ICollectionView = CollectionViewSource.GetDefaultView(MainListBox.ItemsSource)
                              view.Filter = Function(item As EventsPageViewModel.EventListItem) As Boolean
                                                If TypeOf (item.Command) Is WaitCommand Then
                                                    Return False
                                                Else
                                                    Return True
                                                End If

                                                'If item.chIdx = "1" Then
                                                '    Return True
                                                'Else
                                                '    Return False
                                                'End If
                                            End Function

                              MainProgressBar.Visibility = Visibility.Collapsed
                              MainListBox.Visibility = Visibility.Visible

                              'ExportLabel.Visibility = Visibility.Visible
                              'ExportEventsButton.Visibility = Visibility.Visible
                              'ExportDataBlocksButton.Visibility = Visibility.Visible
                              'ExportPCMSamplesButton.Visibility = Visibility.Visible
                              'ExportNoteLengthsButton.Visibility = Visibility.Visible
                              'ExportVoiceListButton.Visibility = Visibility.Visible
                          End Sub)
    End Sub

    Private Sub ExportButton_Click(sender As Object, e As RoutedEventArgs) 'Handles ExportEventsButton.Click

        Dim sfd As New SaveFileDialog

        With sfd
            .AddExtension = True
            .CheckPathExists = True
            .DefaultExt = ".csv"
            .InitialDirectory = My.Settings.LastExportDirectory
            .Filter = "Comma-separated value files (*.csv)|*.csv"
            .FileName = $"{ViewModel.VGMFile.GD3Tag.GameNameInEnglish} - {ViewModel.VGMFile.GD3Tag.TrackNameInEnglish} Events"  '$"{Path.GetFileNameWithoutExtension(ViewModel.SrcFileInfo.FullName)} Events"

            Select Case .ShowDialog
                Case True

                    Dim dstFileInfo As New FileInfo(.FileName)

                    Using fsOut As New FileStream(dstFileInfo.FullName, FileMode.Create, FileAccess.Write)
                        Using swOut As New StreamWriter(fsOut, Text.Encoding.UTF8)

                            Dim nl As Char = ControlChars.NewLine
                            Dim sep As Char = "," 'ControlChars.Tab

                            With ViewModel.VGMFile.GD3Tag
                                swOut.WriteLine($"{ .TrackNameInEnglish}{If(.TrackNameInJapanese = "", "", $" ({ .TrackNameInJapanese})")}")
                                swOut.WriteLine($"from { .GameNameInEnglish}{If(.GameNameInJapanese = "", "", $" ({ .GameNameInJapanese})")} ")
                                swOut.WriteLine($"for { .SystemNameInEnglish}{If(.SystemNameInJapanese = "", "", $" ({ .SystemNameInJapanese})")}")
                                swOut.WriteLine($"by { .OriginalAuthorNameInEnglish}{If(.OriginalAuthorNameInJapanese = "", "", $" ({ .OriginalAuthorNameInJapanese})")}")
                                swOut.WriteLine($"Released { .GameReleaseDate}")
                                swOut.WriteLine($"Converted by { .ConversionAuthorName}")
                                If .Notes <> "" Then
                                    swOut.WriteLine($"Notes:{nl} { .Notes}")
                                End If
                                swOut.WriteLine()
                            End With

                            'swOut.WriteLine($"Offset{sep}Timecode{sep}Type{sep}Description{sep}Ch{sep}Op{sep}Details")
                            swOut.WriteLine($"Offset{sep}Timecode{sep}Type{sep}Description{sep}Details")

                            For Each item As EventsPageViewModel.EventListItem In ViewModel.EventListItems
                                With item
                                    If TypeOf (item.Command) IsNot WaitCommand Then
                                        swOut.WriteLine($"{item.Offset.Replace("$", "0x")}{sep}{item.Timecode}{sep}{item.Type}{sep}{item.Description}{sep}{item.Details.Replace(",", "")}")
                                        'swOut.WriteLine($"{item.Offset.Replace("$", "0x")}{sep}{item.Timecode}{sep}{item.Type}{sep}{item.Description}{sep}{item.chIdx}{sep}{item.opIdx}{sep}{item.Details.Replace(",", "")}")
                                    End If
                                End With
                            Next

                        End Using
                    End Using

                Case False

                Case Else

            End Select

        End With

    End Sub

    Private Sub ExportDataBlocksButton_Click(sender As Object, e As RoutedEventArgs) 'Handles ExportDataBlocksButton.Click

        Dim ofd As New OpenFolderDialog With {
                   .Title = "Choose a folder to output the data blocks as .wav files.",
                    .InitialDirectory = My.Settings.LastExportDirectory
        }

        With ofd

            Select Case .ShowDialog
                Case True

                    Dim vgmFile As New VGMFile(ViewModel.SrcFileInfo.FullName)
                    Dim conv As New NullExporter(vgmFile)

                    Static blockID As Integer = 0

                    For Each cmd As Command In conv.CommandList
                        If cmd.CommandType = CommandType.Data_Block Then

                            Dim dbCmd As DataBlockCommand =
                                cmd

                            Dim data As Byte() = {0}

                            Dim sampleRate As Integer = 16000
                            Dim numChannels As Integer = 1

                            Select Case dbCmd.DataType
                                Case DataBlockType.YM2612_PCM_Data
                                    data = dbCmd.Data.Clone
                                    sampleRate = 16000
                                    numChannels = 1
                                Case DataBlockType.SegaPCM_ROM_Data
                                    Dim rdbCmd As ROMDataBlockCommand =
                                        dbCmd
                                    ReDim data(rdbCmd.Data.Length)
                                    Array.Copy(rdbCmd.Data, 0, data, 0, rdbCmd.Data.Length)
                                    sampleRate = 31250 '15625
                                    numChannels = 1
                            End Select

                            Dim wf As New WAV.WAVFile(sampleRate, data, numChannels)
                            Dim fileName As String =
                                $"{ .FolderName}\{vgmFile.FileInfo.Name.Replace(".", "_")} Data Block {blockID:000}.wav"
                            wf.Save(fileName)

                            blockID += 1

                        End If
                    Next

                Case Else

            End Select

        End With

    End Sub

    Private Sub ExportPCMSamplesButton_Click(sender As Object, e As RoutedEventArgs) 'Handles ExportPCMSamplesButton.Click

        Dim ofd As New OpenFolderDialog With {
            .Title = "Choose a folder to export the PCM samples as .wav files.",
            .InitialDirectory = My.Settings.LastExportDirectory
        }

        With ofd
            Select Case .ShowDialog
                Case True

                    Dim vgmFile As New VGMFile(ViewModel.SrcFileInfo.FullName)
                    Dim conv As New NullExporter(vgmFile)

                    For Each cmd As Command In conv.CommandList
                        If cmd.CommandType = CommandType.Data_Block Then

                            Dim dbCmd As DataBlockCommand =
                                cmd

                            Select Case dbCmd.DataType
                                Case DataBlockType.YM2612_PCM_Data
                                    Dim sampleRate As Integer = 16000
                                    Dim data As Byte() = dbCmd.Data.Clone
                                    Dim numChannels As Integer = 1

                                    Dim wf As New WAV.WAVFile(sampleRate, data, numChannels)
                                    Dim fileName As String =
                                        $"{ .FolderName}\{vgmFile.FileInfo.Name.Replace(".", "_")} YM2612 PCM Samples.wav"
                                    wf.Save(fileName)

                                Case DataBlockType.SegaPCM_ROM_Data
                                    'For i As Integer = 0 To ViewModel.SegaPCMVoices.Count - 1
                                    '    Dim voice As SegaPCM.Voice =
                                    '        ViewModel.SegaPCMVoices(i)
                                    '    Dim channels As SortedSet(Of Integer) =
                                    '        ViewModel.SegaPCMVoiceChannels(i)
                                    '    Dim speeds As SortedSet(Of Integer) =
                                    '        ViewModel.SegaPCMVoiceSpeeds(i)

                                    '    'Export Sega PCM voices
                                    '    Dim voiceData(voice.Length - 1) As Byte
                                    '    Array.Copy(ViewModel.DataBank, voice.StartAddr, voiceData, 0, voice.Length)

                                    '    For Each speed As Integer In speeds
                                    '        Dim pos As Integer = 0
                                    '        Dim frac As Integer = 0
                                    '        Dim inc As Integer = speed

                                    '        Dim wavDataList As New List(Of Byte)

                                    '        Do
                                    '            wavDataList.Add(voiceData(pos))

                                    '            frac += inc
                                    '            If frac >= 256 Then
                                    '                pos += 1
                                    '                frac -= 256
                                    '            End If

                                    '        Loop Until pos > voiceData.GetUpperBound(0)

                                    '        Dim sampleRate As Integer = 31250
                                    '        Dim numChannels As Integer = 1
                                    '        Dim wavData As Byte() = wavDataList.ToArray

                                    '        Dim wf As New WAVFile(sampleRate, wavData, numChannels)
                                    '        Dim fileName As String =
                                    '            $"{ .FolderName}\{vgmFile.FileInfo.Name.Replace(".", "_")} Sega PCM Sample 0x{voice.StartAddr:X8} Speed 0x{speed:X2}.wav"
                                    '        wf.Save(fileName)
                                    '    Next

                                    'Next

                            End Select

                        End If
                    Next

                Case Else

            End Select

        End With
    End Sub

    Private Sub ExportNoteLengthsButton_Click(sender As Object, e As RoutedEventArgs) 'Handles ExportNoteLengthsButton.Click

        Dim sfd As New SaveFileDialog

        With sfd
            .AddExtension = True
            .CheckPathExists = True
            .DefaultExt = ".csv"
            .InitialDirectory = My.Settings.LastExportDirectory
            .Filter = "Comma-separated value files (*.csv)|*.csv"
            .FileName = $"{ViewModel.VGMFile.GD3Tag.GameNameInEnglish} - {ViewModel.VGMFile.GD3Tag.TrackNameInEnglish} Note Lengths" 'Path.GetFileNameWithoutExtension(ViewModel.SrcFileInfo.FullName) & " Note Lengths.csv"

            Select Case .ShowDialog
                Case True

                    Dim NoteLengthsInMillisecs As New SortedList(Of Long, Long)

                    For Each key As Long In ViewModel.NoteGapLengths.Keys
                        Dim length As Long =
                            (key / 44100) * 1000
                        Dim count As Long =
                            ViewModel.NoteGapLengths(key)
                        If NoteLengthsInMillisecs.ContainsKey(length) Then
                            NoteLengthsInMillisecs(length) += count
                        Else
                            NoteLengthsInMillisecs.Add(length, count)
                        End If
                    Next

                    Using fsOut As New FileStream(.FileName, FileMode.Create, FileAccess.Write)
                        Using swOut As New StreamWriter(fsOut, Text.Encoding.UTF8)

                            swOut.WriteLine("Length (ms),Count,BPM")

                            For Each key As Long In NoteLengthsInMillisecs.Keys
                                Dim count As Long =
                                    NoteLengthsInMillisecs(key)
                                swOut.WriteLine($"{key:0.00},{count},{60000 / key:0.00}")
                            Next

                        End Using
                    End Using

                Case False

                Case Else

            End Select

        End With

    End Sub

    Private Sub ExportVoiceListButton_Click(sender As Object, e As RoutedEventArgs) 'Handles ExportVoiceListButton.Click

        'Dim sfd As New SaveFileDialog

        'With sfd
        '    .AddExtension = True
        '    .CheckPathExists = True
        '    .DefaultExt = ".csv"
        '    .InitialDirectory = My.Settings.LastExportDirectory
        '    .Filter = "Comma-separated value files (*.csv)|*.csv"
        '    .FileName = $"{ViewModel.VGMFile.GD3Tag.GameNameInEnglish} - {ViewModel.VGMFile.GD3Tag.TrackNameInEnglish} Voices" '$"{Path.GetFileNameWithoutExtension(ViewModel.SrcFileInfo.FullName)} Voices"

        '    Select Case .ShowDialog
        '        Case True

        '            Dim dstFileInfo As New FileInfo(.FileName)

        '            Using fsOut As New FileStream(dstFileInfo.FullName, FileMode.Create, FileAccess.Write)
        '                Using swOut As New StreamWriter(fsOut, Text.Encoding.UTF8)

        '                    Dim nl As Char = ControlChars.NewLine
        '                    Dim sep As Char = ","

        '                    With swOut
        '                        .WriteLine($"Data Bank")
        '                        .WriteLine()
        '                        .WriteLine($"Start Addr{sep}End Addr")
        '                    End With

        '                    Dim uniqueDataBankOffsets As New SortedSet(Of Long)

        '                    For i As Integer = 0 To ViewModel.DataBankVoices.Count - 1
        '                        Dim voice As Exporter.DataBankVoice =
        '                            ViewModel.DataBankVoices(i)

        '                        With voice
        '                            Dim s As String = $"0x{ .StartOffset:X8}{sep}0x{ .EndOffset:X8}"
        '                            swOut.WriteLine(s)
        '                        End With

        '                        With uniqueDataBankOffsets
        '                            If .Contains(voice.StartOffset) = False Then
        '                                .Add(voice.StartOffset)
        '                            End If
        '                            If .Contains(voice.EndOffset) = False Then
        '                                .Add(voice.EndOffset)
        '                            End If
        '                        End With

        '                    Next

        '                    With swOut
        '                        .WriteLine()
        '                        .WriteLine($"Unique offsets")
        '                        .WriteLine()
        '                        .WriteLine($"Hex{sep}Dec{sep}Time (@ 8000 Hz){sep}Time (@ 11025 Hz){sep}Time (@ 16000 Hz){sep}Time (@ 22050 Hz)")
        '                    End With

        '                    For i As Integer = 0 To uniqueDataBankOffsets.Count - 1
        '                        Dim offset As Integer =
        '                            uniqueDataBankOffsets(i)

        '                        Dim s As String = $"0x{ offset:X8}{sep}{offset}"

        '                        Dim startTime = Function(addr As Integer, sampleRate As Integer) As Single
        '                                            Return addr / sampleRate
        '                                        End Function

        '                        s &= $"{sep}{startTime(offset, 8000):0.000}"
        '                        s &= $"{sep}{startTime(offset, 11025):0.000}"
        '                        s &= $"{sep}{startTime(offset, 16000):0.000}"
        '                        s &= $"{sep}{startTime(offset, 22050):0.000}"

        '                        swOut.WriteLine(s)
        '                    Next


        '                    If ViewModel.YM2612Voices.Count > 0 Then
        '                        swOut.WriteLine()
        '                        swOut.WriteLine("YM2612 Voices")
        '                        swOut.WriteLine()
        '                    End If

        '                    For i As Integer = 0 To ViewModel.YM2612Voices.Count - 1
        '                        Dim voice As YM2612.Voice =
        '                            ViewModel.YM2612Voices(i)

        '                        swOut.WriteLine($"Voice {(i + 1):000}")

        '                        With voice
        '                            swOut.WriteLine($"LFOFreq{sep}FB{sep}Con{sep}AMS{sep}PMS")
        '                            swOut.WriteLine($"{ .LFOFreq}{sep}{ .FB}{sep}{ .Con}{sep}{ .AMS}{sep}{ .PMS}")
        '                            swOut.WriteLine($"Op{sep}DT{sep}Multi{sep}TL{sep}KS{sep}AR{sep}AM{sep}DR{sep}SR{sep}SL{sep}RR{sep}SSG_EG{sep}{If(.IsCh3Sp, $"ch3SpBlock{sep}ch3SpFNum", "")}")
        '                            For j As Integer = 0 To 3
        '                                swOut.WriteLine($"{j}{sep}{ .DT(j)}{sep}{ .Multi(j)}{sep}{ .TL(j)}{sep}{ .KS(j)}{sep}{ .AR(j)}{sep}{ .AM(j)}{sep}{ .DR(j)}{sep}{ .SR(j)}{sep}{ .SL(j)}{sep}{ .RR(j)}{sep}{ .SSG_EG(j)}{If(.IsCh3Sp, $"{sep}{ .Ch3SpBlock(j)}{sep}{ .Ch3SpFNum(j)}", "")}")
        '                            Next
        '                        End With

        '                        swOut.WriteLine()

        '                    Next

        '                    'For i As Integer = 0 To ViewModel.YM2612DACVoices.Count - 1
        '                    '    Dim voice As Long =
        '                    '        ViewModel.YM2612DACVoices(i)
        '                    '    Dim s As String = $"{voice}"
        '                    '    swOut.WriteLine(s)
        '                    'Next

        '                    With swOut
        '                        .WriteLine()
        '                        .WriteLine($"Sega PCM")
        '                        .WriteLine()
        '                        .WriteLine($"Start Addr{sep}End Addr{sep}Loop Addr{sep}Looped{sep}Channel(s){sep}Speed(s)")
        '                    End With

        '                    For i As Integer = 0 To ViewModel.SegaPCMVoices.Count - 1
        '                        Dim voice As SegaPCM.Voice =
        '                            ViewModel.SegaPCMVoices(i)
        '                        Dim channels As SortedSet(Of Integer) =
        '                            ViewModel.SegaPCMVoiceChannels(i)
        '                        Dim speeds As SortedSet(Of Integer) =
        '                            ViewModel.SegaPCMVoiceSpeeds(i)

        '                        Dim s As String = ""

        '                        With voice
        '                            s &= $"0x{ .StartAddr:X8}{sep}0x{ .EndAddr:X8}{sep}0x{ .LoopAddr:X8}{sep}{ If(.Looped = True, "Y", "N")}{sep}"
        '                        End With

        '                        With channels
        '                            s &= "{"
        '                            For j As Integer = 0 To .Count - 1
        '                                s &= $"{ channels(j) + 1}"
        '                                If j < .Count - 1 Then
        '                                    s &= "; "
        '                                End If
        '                            Next
        '                            s &= "}"
        '                        End With

        '                        s &= sep

        '                        With speeds
        '                            s &= "{"
        '                            For j As Integer = 0 To .Count - 1
        '                                s &= $"0x{ speeds(j):X2}"
        '                                If j < .Count - 1 Then
        '                                    s &= "; "
        '                                End If
        '                            Next
        '                            s &= "}"
        '                        End With

        '                        swOut.WriteLine(s)

        '                    Next

        '                End Using

        '            End Using

        '        Case False

        '        Case Else

        '    End Select

        'End With

    End Sub

End Class

Public Class EventsPageViewModel
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(<Runtime.CompilerServices.CallerMemberName> Optional ByVal propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Private _ProgressState As Shell.TaskbarItemProgressState = Shell.TaskbarItemProgressState.None
    Public Property ProgressState As Shell.TaskbarItemProgressState
        Get
            Return _ProgressState
        End Get
        Set(value As Shell.TaskbarItemProgressState)
            If value <> _ProgressState Then
                _ProgressState = value
                NotifyPropertyChanged()
            End If
        End Set
    End Property

    Private _ProgressValue As Double = 0
    Public Property ProgressValue As Double
        Get
            Return _ProgressValue
        End Get
        Set(value As Double)
            If value <> _ProgressValue Then
                _ProgressValue = value
                NotifyPropertyChanged()
            End If
        End Set
    End Property

    Public ReadOnly Property VGMFile As VGMFile
    Public ReadOnly Property SrcFileInfo As FileInfo
        Get
            Return VGMFile.FileInfo
        End Get
    End Property

    Public Structure EventListItem
        Public ReadOnly Property Command As Command

        Sub New(command As Command)
            _Command = command
        End Sub

        Public ReadOnly Property Offset As String
            Get
                Return $"0x{Command.Offset:X8}"
            End Get
        End Property

        Public ReadOnly Property Timecode As String
            Get
                With Command
                    Return $"{ .Hours:0}:{ .Minutes:00}:{ .Seconds:00}.{ .Samples:00000}"
                End With
            End Get
        End Property

        Public ReadOnly Property Type As String
            Get
                Return Command.CommandType.ToString.Replace("_", " ")
            End Get
        End Property

        Public ReadOnly Property Description As String
            Get
                Return Command.Description?.Replace("_", " ")
            End Get
        End Property

        'Public ReadOnly Property chIdx As String
        '    Get
        '        If TypeOf (Command) Is ChipCommand Then
        '            Dim chipCmd As ChipCommand = Command
        '            Return If(chipCmd.chIdx > -1, chipCmd.chIdx + 1, "")
        '        End If
        '        Return ""
        '    End Get
        'End Property

        'Public ReadOnly Property opIdx As String
        '    Get
        '        If TypeOf (Command) Is ChipCommand Then
        '            Dim chipCmd As ChipCommand = Command
        '            Return If(chipCmd.opIdx > -1, chipCmd.opIdx + 1, "")
        '        End If
        '        Return ""
        '    End Get
        'End Property

        Public ReadOnly Property Details As String
            Get
                Return Command.Details
            End Get
        End Property

    End Structure

    Public EventListItems As New List(Of EventListItem)

    Public NoteGapLengths As New SortedDictionary(Of Long, Long)

    'Public DataBank As Byte()
    'Public DataBankVoices As New List(Of Exporter.DataBankVoice)
    'Public YM2612Voices As New List(Of YM2612.Voice)
    'Public YM2612DACVoices As New List(Of Long)
    'Public SegaPCMVoices As New List(Of SegaPCM.Voice)
    'Public SegaPCMVoiceChannels As New List(Of SortedSet(Of Integer))
    'Public SegaPCMVoiceSpeeds As New List(Of SortedSet(Of Integer))

    Public Sub New(fileName As String)
        _VGMFile = New VGMFile(fileName)
    End Sub

    Public Property TokenSource As New CancellationTokenSource

    Public Event Completed(sender As Object, e As EventArgs)

    Dim Progress As IProgress(Of Double) =
        New Progress(Of Double)(Sub(value) ProgressValue = value)

    Public Sub StartExport()
        Dim token As CancellationToken = TokenSource.Token

        Dim conv As New NullExporter(VGMFile)
        ''Uncomment the following line to make the convert process synchronous (for easier debugging)
        'conv.Convert(Progress, token) : RaiseEvent Completed(Me, New EventArgs) : Exit Sub
        Dim t As New Task(Sub() conv.Convert(Progress, token), token)

        Dim tCompleted As Task =
                t.ContinueWith(Sub()

                                   Dim cmdList As CommandList =
                                        conv.CommandList

                                   Dim progVal As Integer = 0
                                   Dim progMax As Integer = cmdList.Count
                                   Dim progInc As Integer = progMax \ 100
                                   Dim progPct As Integer = 0

                                   Progress.Report(0)

                                   For cmdIdx As Integer = 0 To cmdList.Count - 1
                                       Dim cmd As Command =
                                            cmdList(cmdIdx)

                                       EventListItems.Add(New EventListItem(cmd))

                                       progVal += 1

                                       If (progVal Mod progInc) = 0 Then
                                           progPct += 1
                                           Progress.Report(progPct / 100)
                                       End If

                                   Next

                                   With conv
                                       For Each key As Long In .NoteGapLengths.Keys
                                           NoteGapLengths.Add(key, .NoteGapLengths(key))
                                       Next

                                       'DataBank = .DataBank.ToArray
                                       'DataBankVoices.AddRange(.DataBankVoices)
                                       'YM2612Voices.AddRange(.YM2612Voices)
                                       'SegaPCMVoices.AddRange(.SegaPCMVoices)
                                       'SegaPCMVoiceChannels.AddRange(.SegaPCMVoiceChannels)
                                       'SegaPCMVoiceSpeeds.AddRange(.SegaPCMVoiceSpeeds)
                                   End With

                                   RaiseEvent Completed(Me, New EventArgs)

                               End Sub,
                                 TaskContinuationOptions.NotOnCanceled Or TaskContinuationOptions.NotOnFaulted Or TaskContinuationOptions.ExecuteSynchronously
                                 )

        Dim tFaulted As Task =
                    t.ContinueWith(Sub()
                                       MessageBox.Show("Faulted")
                                   End Sub,
                                 TaskContinuationOptions.OnlyOnFaulted Or TaskContinuationOptions.ExecuteSynchronously
                                 )

        t.Start()

    End Sub

End Class
