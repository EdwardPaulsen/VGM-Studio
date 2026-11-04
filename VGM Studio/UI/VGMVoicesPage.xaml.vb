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

                              If ViewModel.OPNVoices?.Count > 0 Then
                                  With OPNVoiceComboBox
                                      .ItemsSource = ViewModel.OPNVoices
                                      .SelectedIndex = 0
                                  End With
                                  OPNStackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.YM2612PCMVoices?.Count > 0 Then
                                  With YM2612PCMVoiceComboBox
                                      .ItemsSource = ViewModel.YM2612PCMVoices
                                      .SelectedIndex = 0
                                  End With
                                  YM2612PCMStackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.OPMVoices?.Count > 0 Then
                                  With OPMVoiceComboBox
                                      .ItemsSource = ViewModel.OPMVoices
                                      .SelectedIndex = 0
                                  End With
                                  OPMStackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.RFC5164Voices?.Count > 0 Then
                                  With RFC5164VoiceComboBox
                                      .ItemsSource = ViewModel.RFC5164Voices
                                      .SelectedIndex = 0
                                  End With
                                  RFC5164StackPanel.Visibility = Visibility.Visible
                              End If

                              If ViewModel.SegaPCMVoices?.Count > 0 Then
                                  With SegaPCMVoiceComboBox
                                      .ItemsSource = ViewModel.SegaPCMVoices
                                      .SelectedIndex = 0
                                  End With
                                  SegaPCMStackPanel.Visibility = Visibility.Visible
                              End If

                          End Sub)
    End Sub

    Private Sub OPNVoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OPNVoiceComboBox.SelectionChanged
        ViewModel.OPNVoiceInfo = ViewModel.OPNVoice.ToString(ControlChars.Tab)
    End Sub

    Private Sub YM2612PCMVoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles YM2612PCMVoiceComboBox.SelectionChanged
        With YM2612PCMFreqComboBox
            .ItemsSource = ViewModel.YM2612PCMFreqs
            .SelectedItem = 16000
        End With
    End Sub

    Private Sub ExportOPNButton_Click(sender As Object, e As RoutedEventArgs) Handles ExportOPNButton.Click
        Dim btn As Button = sender
        If btn IsNot Nothing AndAlso btn.ContextMenu IsNot Nothing Then
            With btn.ContextMenu
                .PlacementTarget = btn
                .Placement = Primitives.PlacementMode.Bottom
                .IsOpen = True
            End With
        End If
    End Sub

    Private Sub ExportThisOPNVoiceMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportThisOPNVoiceMenuItem.Click

    End Sub

    Private Sub ExportAllOPNVoicesMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportAllOPNVoicesMenuItem.Click
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
                    For Each key As Long In ViewModel.OPNVoices.Keys
                        dx7Voices.Add(New Voice(key, ViewModel.OPNVoices(key)))
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

    Private Sub OPMVoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles OPMVoiceComboBox.SelectionChanged
        ViewModel.OPMVoiceInfo = ViewModel.OPMVoice.ToString(ControlChars.Tab)
    End Sub

    Private Sub ExportOPMButton_Click(sender As Object, e As RoutedEventArgs) Handles ExportOPMButton.Click
        Dim btn As Button = sender
        If btn IsNot Nothing AndAlso btn.ContextMenu IsNot Nothing Then
            With btn.ContextMenu
                .PlacementTarget = btn
                .Placement = Primitives.PlacementMode.Bottom
                .IsOpen = True
            End With
        End If
    End Sub

    Private Sub ExportThisOPMVoiceMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportThisOPMVoiceMenuItem.Click

    End Sub

    Private Sub ExportAllOPMVoicesMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportAllOPMVoicesMenuItem.Click
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
                    For Each key As Long In ViewModel.OPMVoices.Keys
                        dx7Voices.Add(New Voice(key, ViewModel.OPMVoices(key)))
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

    Private Sub RFC5164VoiceComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles RFC5164VoiceComboBox.SelectionChanged
        If ViewModel.RFC5164Voices?.Count > 0 Then
            With RFC5164VoiceSpeedComboBox
                .ItemsSource = ViewModel.RFC5164Voice.Speeds
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

    Private Sub RFC5164PlayButton_Click(sender As Object, e As RoutedEventArgs) Handles RFC5164PlayButton.Click
        'Get and convert PCM data
        Dim voice As RFC5164.Voice =
            ViewModel.RFC5164Voice

        Dim pcmData(voice.Data.GetUpperBound(0)) As Single

        For i As Integer = 0 To voice.Data.GetUpperBound(0)
            pcmData(i) = RFC5164.GetOutput(New RFC5164.PCMSample(voice.Data(i)), 255, 15)
        Next

        'Play sound
        Dim sampleRate As Integer = (ViewModel.VGMFile.RF5C164Clock / 384) * (ViewModel.RFC5164VoiceSpeed / 100)
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

        If VGMFile.YM2612Clock <> 0 OrElse VGMFile.YM2203Clock <> 0 Then
            OPNVoices = New Dictionary(Of Long, OPN.Voice)
            If VGMFile.YM2612Clock <> 0 Then
                YM2612PCMVoices = New Dictionary(Of Long, YM2612.PCMVoice)
            End If
        End If

        If VGMFile.YM2151Clock <> 0 Then
            OPMVoices = New Dictionary(Of Long, OPM.Voice)
        End If

        If VGMFile.RF5C164Clock <> 0 Then
            RFC5164Voices = New Dictionary(Of Long, RFC5164.Voice)
        End If

        If VGMFile.SegaPCMClock <> 0 Then
            SegaPCMVoices = New Dictionary(Of Long, SegaPCM.Voice)
        End If

    End Sub
    Public ReadOnly Property VGMFile As VGMFile
    Public ReadOnly Property SrcFileInfo As FileInfo
        Get
            Return VGMFile.FileInfo
        End Get
    End Property

    'Yamaha OPN Voices (YM2203, YM2612, etc.)
    Private _OPNVoices As Dictionary(Of Long, OPN.Voice)
    Public Property OPNVoices As Dictionary(Of Long, OPN.Voice)
        Get
            Return _OPNVoices
        End Get
        Set(value As Dictionary(Of Long, OPN.Voice))
            _OPNVoices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _OPNVoice As OPN.Voice
    Public Property OPNVoice As OPN.Voice
        Get
            Return _OPNVoice
        End Get
        Set(value As OPN.Voice)
            _OPNVoice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _OPNVoiceInfo As String
    Public Property OPNVoiceInfo As String
        Get
            Return _OPNVoiceInfo
        End Get
        Set(value As String)
            _OPNVoiceInfo = value
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

    'OPM and OPP Voices (YM2151 and YM2164)
    Private _OPMVoices As Dictionary(Of Long, OPM.Voice)
    Public Property OPMVoices As Dictionary(Of Long, OPM.Voice)
        Get
            Return _OPMVoices
        End Get
        Set(value As Dictionary(Of Long, OPM.Voice))
            _OPMVoices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _OPMVoice As OPM.Voice
    Public Property OPMVoice As OPM.Voice
        Get
            Return _OPMVoice
        End Get
        Set(value As OPM.Voice)
            _OPMVoice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _OPMVoiceInfo As String
    Public Property OPMVoiceInfo As String
        Get
            Return _OPMVoiceInfo
        End Get
        Set(value As String)
            _OPMVoiceInfo = value
            NotifyPropertyChanged()
        End Set
    End Property

    'RF5164 Voices
    Private _RFC5164Voices As Dictionary(Of Long, RFC5164.Voice)
    Public ReadOnly Property RFC5164VoiceChannels As New List(Of SortedSet(Of Integer))
    Public Property RFC5164Voices As Dictionary(Of Long, RFC5164.Voice)
        Get
            Return _RFC5164Voices
        End Get
        Set(value As Dictionary(Of Long, RFC5164.Voice))
            _RFC5164Voices = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _RFC5164Voice As RFC5164.Voice
    Public Property RFC5164Voice As RFC5164.Voice
        Get
            Return _RFC5164Voice
        End Get
        Set(value As RFC5164.Voice)
            _RFC5164Voice = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _RFC5164VoiceAddrInc As UInteger
    Public Property RFC5164VoiceAddrInc As UInteger
        Get
            Return _RFC5164VoiceAddrInc
        End Get
        Set(value As UInteger)
            _RFC5164VoiceAddrInc = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _RFC5164VoiceSpeed As Single
    Public Property RFC5164VoiceSpeed As Single
        Get
            Return _RFC5164VoiceSpeed
        End Get
        Set(value As Single)
            _RFC5164VoiceSpeed = value
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
                                           OPNVoices.Add(key, .OPNVoices(key))
                                       Next
                                       For Each key As Long In .YM2612PCMVoices.Keys
                                           YM2612PCMVoices.Add(key, .YM2612PCMVoices(key))
                                       Next
                                       For Each key As Long In .OPMVoices.Keys
                                           OPMVoices.Add(key, .OPMVoices(key))
                                       Next
                                       For Each key As Long In .RFC5164Voices.Keys
                                           RFC5164Voices.Add(key, .RFC5164Voices(key))
                                       Next
                                       RFC5164VoiceChannels.AddRange(.RFC5164VoiceChannels)
                                       For Each key As Long In .SegaPCMVoices.Keys
                                           SegaPCMVoices.Add(key, .SegaPCMVoices(key))
                                       Next
                                       SegaPCMVoiceChannels.AddRange(.SegaPCMVoiceChannels)
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