Imports System.ComponentModel
Imports System.IO
Imports System.Media
Imports System.Threading
Imports Microsoft.Win32
Imports VGM_Studio.VGM
Imports VGM_Studio.My
Imports VGM_Studio.DX7

Class VGMVoicesPage

    Public WithEvents ViewModel As VoicesPageViewModel
    Public Sub New(fileName As String)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ViewModel = New VoicesPageViewModel(fileName)
        DataContext = ViewModel
    End Sub
    Private Property HasBeenPopulated As Boolean = False
    Private Sub VGMVoicesPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If HasBeenPopulated Then Exit Sub
        ViewModel.StartExport()
        HasBeenPopulated = True
    End Sub

    Private Sub OnCompleted(sender As Object, e As EventArgs) Handles ViewModel.Completed
        Dispatcher.Invoke(Sub()
                              MainProgressBar.Visibility = Visibility.Collapsed

                              If ViewModel.FMVoices?.Count > 0 Then
                                  With FMVoiceComboBox
                                      .ItemsSource = ViewModel.FMVoices
                                      .SelectedIndex = 0
                                  End With
                                  FMStackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.YM2612PCMVoices?.Count > 0 Then
                                  With YM2612PCMVoiceComboBox
                                      .ItemsSource = ViewModel.YM2612PCMVoices
                                      .SelectedIndex = 0
                                  End With
                                  YM2612PCMStackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.SegaPCMVoices?.Count > 0 Then
                                  With SegaPCMVoiceComboBox
                                      .ItemsSource = ViewModel.SegaPCMVoices
                                      .SelectedIndex = 0
                                  End With
                                  SegaPCMStackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.RF5CVoices?.Count > 0 Then
                                  With RF5CVoiceComboBox
                                      .ItemsSource = ViewModel.RF5CVoices
                                      .SelectedIndex = 0
                                  End With
                                  RF5CStackPanel.Visibility = Visibility.Visible
                              End If

                          End Sub)
    End Sub

    Private Sub FMVoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles FMVoiceComboBox.SelectionChanged
        ViewModel.FMVoiceInfo = ViewModel.FMVoice.InfoString(ControlChars.Tab)
    End Sub

    Private Sub ExportFMButton_Click(sender As Object, e As RoutedEventArgs) Handles ExportFMButton.Click
        Dim btn As Button = sender
        If btn IsNot Nothing AndAlso btn.ContextMenu IsNot Nothing Then
            With btn.ContextMenu
                .PlacementTarget = btn
                .Placement = Primitives.PlacementMode.Bottom
                .IsOpen = True
            End With
        End If
    End Sub

    Private Sub ExportThisFMVoiceMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportThisFMVoiceMenuItem.Click

    End Sub

    Private Sub ExportAllFMVoicesMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportAllFMVoicesMenuItem.Click
        'Save file
        Dim sfd As New SaveFileDialog

        With sfd
            .AddExtension = True
            .CheckPathExists = True
            .DefaultExt = ".syx"
            .InitialDirectory = Settings.LastVoiceExportDirectory
            .Filter = "Yamaha DX7 SYX files (*.syx)|*.syx"
            .FileName = RemoveIllegalFileNameChars($"{ViewModel.VGMFile.GD3Tag.GameNameInEnglish} - {ViewModel.VGMFile.GD3Tag.TrackNameInEnglish}", "-")

            Select Case .ShowDialog
                Case True
                    Dim dx7Voices As New List(Of Voice)
                    For Each key As Long In ViewModel.FMVoices.Keys
                        dx7Voices.Add(New Voice(key, ViewModel.FMVoices(key)))
                    Next

                    'Create SYX data
                    Dim syxFile As New SYXFile(dx7Voices.ToArray)

                    Using s As New FileStream(.FileName, FileMode.OpenOrCreate)
                        Using bw As New BinaryWriter(s)
                            bw.Write(syxFile.Data)
                        End Using
                    End Using

                    Settings.LastVoiceExportDirectory = Path.GetDirectoryName(.FileName)
                    Settings.Save()
            End Select

        End With

    End Sub

    Private Sub YM2612PCMVoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles YM2612PCMVoiceComboBox.SelectionChanged
        With YM2612PCMFreqComboBox
            .ItemsSource = ViewModel.YM2612PCMFreqs
            .SelectedItem = 16000
        End With
    End Sub

    Private Sub RF5C164VoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles RF5CVoiceComboBox.SelectionChanged
        If ViewModel.RF5CVoices?.Count > 0 Then
            With RF5CVoiceSpeedComboBox
                .ItemsSource = ViewModel.RF5CVoice.Speeds
                .SelectedIndex = 0
            End With
        End If
    End Sub

    Private Sub SegaPCMVoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles SegaPCMVoiceComboBox.SelectionChanged
        If ViewModel.SegaPCMVoices?.Count > 0 Then
            With SegaPCMVoiceSpeedComboBox
                .ItemsSource = ViewModel.SegaPCMVoice.Speeds
                .SelectedIndex = 0
            End With
        End If
    End Sub

    Private Sub YM2612PCMPlayButton_Click(sender As Object, e As RoutedEventArgs) Handles YM2612PCMPlayButton.Click
        'Get and convert PCM data
        Dim voice As YM2612.PCMVoice =
            ViewModel.YM2612PCMVoice

        Dim pcmData As Byte() =
            voice.Data

        If pcmData Is Nothing Then Exit Sub

        'Play sound
        Dim sampleRate As Integer = YM2612PCMFreqComboBox.SelectedValue
        Dim numChannels As Integer = 1
        Dim wf As New WAV.WAVFile(sampleRate, pcmData, numChannels)
        Using stream As New MemoryStream
            wf.Save(stream, True)
            stream.Seek(0, SeekOrigin.Begin)
            Using sp As New SoundPlayer(stream)
                sp.Play()
            End Using
        End Using
    End Sub

    Private Sub RFCPlayButton_Click(sender As Object, e As RoutedEventArgs) Handles RF5CPlayButton.Click
        'Get and convert PCM data
        Dim voice As RF5C.Voice =
            ViewModel.RF5CVoice

        Dim pcmData(voice.Data.GetUpperBound(0)) As Single

        For i As Integer = 0 To voice.Data.GetUpperBound(0)
            pcmData(i) = RF5C.GetOutput(New RF5C.PCMSample(voice.Data(i)), 255, 15, voice.ChipType)
        Next

        'Play sound
        Dim clock As Integer
        With ViewModel.VGMFile
            If .RF5C68Clock <> 0 Then
                clock = .RF5C68Clock
            ElseIf .RF5C164Clock <> 0 Then
                clock = .RF5C164Clock
            End If
        End With
        Dim sampleRate As Integer = (clock / 384) * (ViewModel.RF5CVoiceSpeed / 100)
        Dim numChannels As Integer = 1
        Dim wf As New WAV.WAVFile(sampleRate, pcmData, numChannels)
        Using stream As New MemoryStream
            wf.Save(stream, True)
            stream.Seek(0, SeekOrigin.Begin)
            Using sp As New SoundPlayer(stream)
                sp.Play()
            End Using
        End Using
    End Sub

    Private Sub SegaPCMPlayButton_Click(sender As Object, e As RoutedEventArgs) Handles SegaPCMPlayButton.Click
        'Get and convert PCM data
        Dim voice As SegaPCM.Voice =
            ViewModel.SegaPCMVoice

        Dim pcmData(voice.Data.GetUpperBound(0)) As Single

        For i As Integer = 0 To voice.Data.GetUpperBound(0)
            pcmData(i) = SegaPCM.GetOutput(voice.Data(i), 127)
        Next

        'Play sound
        Dim sampleRate As Integer = (ViewModel.VGMFile.SegaPCMClock / 128) * (ViewModel.SegaPCMVoiceSpeed / 100)
        Dim numChannels As Integer = 1
        Dim wf As New WAV.WAVFile(sampleRate, pcmData, numChannels)
        Using stream As New MemoryStream
            wf.Save(stream, True)
            stream.Seek(0, SeekOrigin.Begin)
            Using sp As New SoundPlayer(stream)
                sp.Play()
            End Using
        End Using
    End Sub

    'Private Sub YM2612VoiceNameTextBox_TextChanged(sender As Object, e As TextChangedEventArgs) Handles YM2612VoiceNameTextBox.TextChanged
    '    Dim tb As TextBox = sender
    '    Dim voice As YM2612.Voice =
    '        ViewModel.YM2612Voice
    '    voice.VoiceName = tb.Text
    'End Sub

End Class

Public Class VoicesPageViewModel
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(<Runtime.CompilerServices.CallerMemberName> Optional propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public Sub New()
    End Sub
    Public Sub New(fileName As String)
        _VGMFile = New VGMFile(fileName)

        FMVoices = New Dictionary(Of Long, FM.Voice)
        YM2612PCMVoices = New Dictionary(Of Long, YM2612.PCMVoice)
        RF5CVoices = New Dictionary(Of Long, RF5C.Voice)
        SegaPCMVoices = New Dictionary(Of Long, SegaPCM.Voice)

    End Sub
    Public ReadOnly Property VGMFile As VGMFile
    Public ReadOnly Property SrcFileInfo As FileInfo
        Get
            Return VGMFile.FileInfo
        End Get
    End Property

    'FM Voices
    Private _FMVoices As Dictionary(Of Long, FM.Voice)
    Public Property FMVoices As Dictionary(Of Long, FM.Voice)
        Get
            Return _FMVoices
        End Get
        Set(value As Dictionary(Of Long, FM.Voice))
            _FMVoices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _FMVoice As FM.Voice
    Public Property FMVoice As FM.Voice
        Get
            Return _FMVoice
        End Get
        Set(value As FM.Voice)
            _FMVoice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _FMVoiceInfo As String
    Public Property FMVoiceInfo As String
        Get
            Return _FMVoiceInfo
        End Get
        Set(value As String)
            _FMVoiceInfo = value
            NotifyPropertyChanged()
        End Set
    End Property

    'YM2612 PCM Voices
    Private _YM2612PCMVoices As Dictionary(Of Long, YM2612.PCMVoice)
    Public Property YM2612PCMVoices As Dictionary(Of Long, YM2612.PCMVoice)
        Get
            Return _YM2612PCMVoices
        End Get
        Set(value As Dictionary(Of Long, YM2612.PCMVoice))
            _YM2612PCMVoices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _YM2612PCMVoice As YM2612.PCMVoice
    Public Property YM2612PCMVoice As YM2612.PCMVoice
        Get
            Return _YM2612PCMVoice
        End Get
        Set(value As YM2612.PCMVoice)
            _YM2612PCMVoice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly YM2612PCMFreqs As Integer() =
        {2000, 4000, 8000, 16000, 32000}
    Private _YM2612PCMFreq
    Public Property YM2612PCMFreq As Integer
        Get
            Return _YM2612PCMFreq
        End Get
        Set(value As Integer)
            _YM2612PCMFreq = value
            NotifyPropertyChanged()
        End Set
    End Property

    'RF5164 Voices
    Private _RF5CVoices As Dictionary(Of Long, RF5C.Voice)
    Public ReadOnly Property RF5CVoiceChannels As New List(Of SortedSet(Of Integer))
    Public Property RF5CVoices As Dictionary(Of Long, RF5C.Voice)
        Get
            Return _RF5CVoices
        End Get
        Set(value As Dictionary(Of Long, RF5C.Voice))
            _RF5CVoices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _RF5CVoice As RF5C.Voice
    Public Property RF5CVoice As RF5C.Voice
        Get
            Return _RF5CVoice
        End Get
        Set(value As RF5C.Voice)
            _RF5CVoice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _RF5CVoiceAddrInc As UInteger
    Public Property RF5CVoiceAddrInc As UInteger
        Get
            Return _RF5CVoiceAddrInc
        End Get
        Set(value As UInteger)
            _RF5CVoiceAddrInc = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _RF5CVoiceSpeed As Single
    Public Property RF5CVoiceSpeed As Single
        Get
            Return _RF5CVoiceSpeed
        End Get
        Set(value As Single)
            _RF5CVoiceSpeed = value
            NotifyPropertyChanged()
        End Set
    End Property

    'Sega PCM Voices
    Private _SegaPCMVoices As Dictionary(Of Long, SegaPCM.Voice)
    Public ReadOnly Property SegaPCMVoiceChannels As New List(Of SortedSet(Of Integer))
    Public Property SegaPCMVoices As Dictionary(Of Long, SegaPCM.Voice)
        Get
            Return _SegaPCMVoices
        End Get
        Set(value As Dictionary(Of Long, SegaPCM.Voice))
            _SegaPCMVoices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _SegaPCMVoice As SegaPCM.Voice
    Public Property SegaPCMVoice As SegaPCM.Voice
        Get
            Return _SegaPCMVoice
        End Get
        Set(value As SegaPCM.Voice)
            _SegaPCMVoice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _SegaPCMVoiceAddrInc As UInteger
    Public Property SegaPCMVoiceAddrInc As UInteger
        Get
            Return _SegaPCMVoiceAddrInc
        End Get
        Set(value As UInteger)
            _SegaPCMVoiceAddrInc = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _SegaPCMVoiceSpeed As Single
    Public Property SegaPCMVoiceSpeed As Single
        Get
            Return _SegaPCMVoiceSpeed
        End Get
        Set(value As Single)
            _SegaPCMVoiceSpeed = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property Exporter As VoiceExporter
    Public Property TokenSource As New CancellationTokenSource

    Public Event Completed(sender As Object, e As EventArgs)

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

    Dim Progress As IProgress(Of Double) =
        New Progress(Of Double)(Sub(value) ProgressValue = value)

    Public Sub StartExport()
        Dim token As CancellationToken = TokenSource.Token

        _Exporter = New VoiceExporter(VGMFile, New VoiceExportOptions)
        ''Uncomment the following line to make the convert process synchronous (for easier debugging)
        'conv.Convert(Progress, token) : RaiseEvent Completed(Me, New EventArgs) : Exit Sub
        Dim t As New Task(Sub() Exporter.Convert(Progress, token), token)

        Dim tCompleted As Task =
                t.ContinueWith(Sub()

                                   Dim cmdList As CommandList =
                                        Exporter.CommandList

                                   Dim progVal As Integer = 0
                                   Dim progMax As Integer = cmdList.Count
                                   Dim progInc As Integer = progMax \ 100
                                   Dim progPct As Integer = 0

                                   Progress.Report(0)

                                   For cmdIdx As Integer = 0 To cmdList.Count - 1
                                       Dim cmd As Command =
                                            cmdList(cmdIdx)

                                       progVal += 1

                                       If (progVal Mod progInc) = 0 Then
                                           progPct += 1
                                           Progress.Report(progPct / 100)
                                       End If

                                   Next

                                   With Exporter
                                       For Each key As Long In .OPNVoices.Keys
                                           FMVoices.Add(key, .OPNVoices(key))
                                       Next
                                       For Each key As Long In .YM2612PCMVoices.Keys
                                           YM2612PCMVoices.Add(key, .YM2612PCMVoices(key))
                                       Next
                                       For Each key As Long In .OPMVoices.Keys
                                           FMVoices.Add(key, .OPMVoices(key))
                                       Next
                                       For Each key As Long In .RF5CVoices.Keys
                                           RF5CVoices.Add(key, .RF5CVoices(key))
                                       Next
                                       For Each key As Long In .SegaPCMVoices.Keys
                                           SegaPCMVoices.Add(key, .SegaPCMVoices(key))
                                       Next
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