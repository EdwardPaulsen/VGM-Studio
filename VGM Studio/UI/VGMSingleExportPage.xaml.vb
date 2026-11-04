Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports Microsoft.Win32
Imports VGM_Studio.My
Imports VGM_Studio.VGM

Class VGMSingleExportPage

    Public WithEvents ViewModel As ExportPageViewModel
    Private ReadOnly Property HomePage As HomePage
    Public Sub New(fileName As String, homePage As HomePage)
        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ViewModel = New ExportPageViewModel(fileName)
        DataContext = ViewModel

        Me.HomePage = homePage
    End Sub

    Private Property HasBeenPopulated As Boolean = False

    Private Sub VGMSingleExportPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If HasBeenPopulated Then Exit Sub
        ViewModel.StartExport()
        HasBeenPopulated = True
        ViewModel.MidiTempos = CreateSortedBPMList()
        If ViewModel.MidiTempos.Count > 0 Then
            ViewModel.MidiTempo = ViewModel.MidiTempos(0)
            BpmComboBox.SelectedValue = ViewModel.MidiTempo
        End If
        ViewModel.SampleRateScale = Settings.SampleRateScale
        ViewModel.PSGEmu = Settings.PSGEmu
        ViewModel.YM2612Emu = Settings.YM2612Emu

        If ViewModel.VGMFile.SN76489Clock = 0 Then
            PSGStackPanel.Visibility = Visibility.Collapsed
        End If

        If ViewModel.VGMFile.YM2612Clock = 0 Then
            YM2612StackPanel.Visibility = Visibility.Collapsed
        End If
    End Sub


    Private Sub ExportButton_Click(sender As Object, e As RoutedEventArgs) Handles ExportButton.Click
        Dim btn As Button = sender
        If btn IsNot Nothing AndAlso btn.ContextMenu IsNot Nothing Then
            With btn.ContextMenu
                .PlacementTarget = btn
                .Placement = Primitives.PlacementMode.Bottom
                .IsOpen = True
            End With
        End If
    End Sub
    Private Sub ExportAsMIDIFileMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportAsMIDIFileMenuItem.Click
        Dim fileName As String
        Dim options As New ExportOptions

        Dim sfd As New SaveFileDialog

        With sfd
            .AddExtension = True
            .CheckPathExists = True
            .DefaultExt = ".mid"
            .InitialDirectory = Settings.LastExportDirectory
            .Filter = "Midi files (*.mid)|*.mid"
            .FileName = RemoveIllegalFileNameChars($"{ViewModel.VGMFile.GD3Tag.GameNameInEnglish} - {ViewModel.VGMFile.GD3Tag.TrackNameInEnglish}", "-")

            Select Case .ShowDialog
                Case True
                    If RetryIfFileInUse(.FileName) = False Then
                        Exit Sub
                    End If
                    Dim dstFileInfo As New FileInfo(.FileName)
                    options = New MidiExportOptions
                    Dim midiOpts As MidiExportOptions = options
                    With midiOpts
                        .TimeDivisionType = ViewModel.MidiTimeDivisionType
                        .Tempo = ViewModel.MidiTempo
                        .Numerator = ViewModel.MidiNumerator
                        .Denominator = ViewModel.MidiDenominator
                        .PitchBendRange = ViewModel.MidiPitchBendRange
                    End With
                    fileName = .FileName
                    Export(fileName, options)
                Case Else
                    Exit Sub
            End Select
        End With

    End Sub
    Private Sub ExportAsWAVFileMenuItem_Click(sender As Object, e As RoutedEventArgs) Handles ExportAsWAVFileMenuItem.Click
        Dim fileName As String
        Dim options As New ExportOptions

        Dim sfd As New SaveFileDialog

        With sfd
            .AddExtension = True
            .CheckPathExists = True
            .DefaultExt = ".mid"
            .InitialDirectory = Settings.LastExportDirectory
            .Filter = "Wave files (*.wav)|*.wav"
            .FileName = RemoveIllegalFileNameChars($"{ViewModel.VGMFile.GD3Tag.GameNameInEnglish} - {ViewModel.VGMFile.GD3Tag.TrackNameInEnglish}", "-")

            Select Case .ShowDialog
                Case True
                    If RetryIfFileInUse(.FileName) = False Then
                        Exit Sub
                    End If
                    Dim dstFileInfo As New FileInfo(.FileName)
                    options = New WavExportOptions With {
                        .HighQualityYM2612 = False,
                        .SampleRateScale = ViewModel.SampleRateScale,
                        .PSGEmu = ViewModel.PSGEmu,
                        .OPNEmu = ViewModel.YM2612Emu
                    }
                    fileName = .FileName
                    Export(fileName, options)
                Case Else
                    Exit Sub
            End Select

        End With

    End Sub

    Private Function RetryIfFileInUse(path As String) As Boolean
        If IsFileInUse(path) Then
            Dim result As MessageBoxResult =
                MessageBox.Show($"{path} is in use.{ControlChars.CrLf}Close the file and retry, or cancel.", "File in use", MessageBoxButton.RetryCancel, MessageBoxImage.Error)
            Select Case result
                Case MessageBoxResult.Retry
                    If RetryIfFileInUse(path) Then Return True
                Case MessageBoxResult.Cancel
                    Return False
            End Select
        End If
        Return True
    End Function

    Private Sub Export(filename As String, options As ExportOptions)
        Try
            Dim exportWindow As New SingleExportWindow(ViewModel.SrcFileInfo.FullName, filename, options) With {
                .Title = $"Exporting {Path.GetFileName(filename)}",
                .Owner = System.Windows.Application.Current.MainWindow
            }
            Dim result As Boolean =
                exportWindow.ShowDialog()
        Catch ex As Exception
            With ex
                Dim result As MessageBoxResult =
                    MessageBox.Show(.Message, "Error", MessageBoxButton.OK, MessageBoxImage.Error)
            End With
        End Try

        With Settings
            .LastExportDirectory = Path.GetDirectoryName(filename)
            .Save()
        End With
    End Sub


    Private Sub AutodetectButton_Click(sender As Object, e As RoutedEventArgs) Handles AutodetectButton.Click
        ViewModel.MidiTempo = ViewModel.MidiTempos(0)
        BpmComboBox.SelectedValue = ViewModel.MidiTempo
    End Sub

    Private Sub BpmComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles BpmComboBox.SelectionChanged
        ViewModel.MidiTempo = BpmComboBox.SelectedValue
    End Sub

    Private Const BPM_Min As Single = 60
    Private Const BPM_Max As Single = 200

    Private Function CreateSortedBPMList() As List(Of Single)
        'Create temporary bpm list
        Dim roughBPMList As New SortedList(Of Single, Long)

        Const MillisecsPerSample As Single = 1000 / 44100

        For Each lengthInSamples As Long In ViewModel.NoteGapLengths.Keys
            Dim lengthInMillisecs As Single _
                = lengthInSamples * MillisecsPerSample

            Dim bpm As Single = 60000 / lengthInMillisecs
            If Single.IsInfinity(bpm) Then Continue For

            'Fit bpm into accepted range
            If bpm > 0 Then
                If bpm < BPM_Min Then
                    'Double until it fits in range
                    While bpm <= BPM_Min
                        bpm *= 2
                    End While
                ElseIf bpm > BPM_Max Then
                    'Halve until it fits in range
                    While bpm >= BPM_Max
                        bpm /= 2
                    End While
                End If
            End If

            Dim count As Long =
                ViewModel.NoteGapLengths(lengthInSamples)
            If roughBPMList.Keys.Contains(bpm) Then
                roughBPMList(bpm) += count
            Else
                roughBPMList.Add(bpm, count)
            End If
        Next

        'Average closely related values into buckets
        Dim bucketBPM As Single = 0
        Dim bucketCount As Single = 0

        Dim bucketedBPMList As New SortedList(Of Single, Long)

        For Each bpm As Single In roughBPMList.Keys
            If (bpm - bucketBPM) > 1 Then
                If bucketCount = 0 Then
                    bucketBPM = bpm
                    bucketCount = roughBPMList(bpm)
                Else
                    bucketedBPMList.Add(Math.Round(bucketBPM, 2), bucketCount)
                    bucketBPM = bpm
                    bucketCount = 0
                End If
            Else
                'TEST: Give a higher weight to whole number tempos
                If (bpm Mod 1) = 0 Then
                    'bucketedBPMList.Add(bpm, Long.MaxValue)
                    Dim weightedCount As Long =
                        If(roughBPMList(bpm) > Long.MaxValue \ 100, Long.MaxValue, roughBPMList(bpm) * 100)
                    bucketedBPMList.Add(bpm, weightedCount)
                    bucketBPM = bpm
                    bucketCount = 0
                Else
                    bucketBPM += bpm
                    If bucketCount > 0 Then
                        bucketBPM /= 2
                    End If
                    bucketCount += roughBPMList(bpm)
                End If
            End If
        Next

        'Sort BPMs in descending order of frequency
        Dim sortedByValue =
            bucketedBPMList.OrderByDescending(Function(kvp) kvp.Value).ToList()
        Dim bpmList As New List(Of Single)
        For Each kvp In sortedByValue
            bpmList.Add(kvp.Key)
        Next
        Return bpmList
    End Function

    Private Sub SampleRateScaleComboBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles SampleRateScaleComboBox.SelectionChanged
        SampleRateScaleTextRun.Text = $"({VGM_Sample_Rate * ViewModel.SampleRateScale} Hz)"
    End Sub

End Class

Public Class ExportPageViewModel
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(<Runtime.CompilerServices.CallerMemberName> Optional ByVal propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public Sub New()
    End Sub

    Private _MidiTimeDivisionType As Midi.TimeDivisionType = Midi.TimeDivisionType.Ticks
    Public Property MidiTimeDivisionType As Midi.TimeDivisionType
        Get
            Return _MidiTimeDivisionType
        End Get
        Set(value As Midi.TimeDivisionType)
            _MidiTimeDivisionType = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _MidiTempo As Single = 120
    Public Property MidiTempo As Single
        Get
            Return _MidiTempo
        End Get
        Set(value As Single)
            _MidiTempo = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _MidiTempos As List(Of Single)
    Public Property MidiTempos As List(Of Single)
        Get
            Return _MidiTempos
        End Get
        Set(value As List(Of Single))
            _MidiTempos = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property MidiNumerators As Integer() =
        {1, 2, 3, 4, 5, 6, 7, 8, 9, 10, 11, 12}

    Private _MidiNumerator As Integer = 4
    Public Property MidiNumerator As Integer
        Get
            Return _MidiNumerator
        End Get
        Set(value As Integer)
            _MidiNumerator = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property MidiDenominators As Integer() =
        {1, 2, 4, 8, 16}

    Private _MidiDenominator As Integer = 4
    Public Property MidiDenominator As Integer
        Get
            Return _MidiDenominator
        End Get
        Set(value As Integer)
            _MidiDenominator = value
            NotifyPropertyChanged()
        End Set
    End Property

    Private _MidiPitchBendRange As Integer = 2
    Public Property MidiPitchBendRange As Integer
        Get
            Return _MidiPitchBendRange
        End Get
        Set(value As Integer)
            _MidiPitchBendRange = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property SampleRateScales As Integer() =
        {1, 2, 4}
    Private _SampleRateScale As Integer = 1
    Public Property SampleRateScale As Integer
        Get
            Return _SampleRateScale
        End Get
        Set(value As Integer)
            _SampleRateScale = value
            Settings.SampleRateScale = value
            Settings.Save()
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property PSGEmus As WAVExporter.PSGEmu() =
        {WAVExporter.PSGEmu.Internal, WAVExporter.PSGEmu.NukedYMPSG}
    Private _PSGEmu As WAVExporter.PSGEmu = WAVExporter.PSGEmu.NukedYMPSG
    Public Property PSGEmu As WAVExporter.PSGEmu
        Get
            Return _PSGEmu
        End Get
        Set(value As WAVExporter.PSGEmu)
            _PSGEmu = value
            Settings.PSGEmu = value
            Settings.Save()
            NotifyPropertyChanged()
        End Set
    End Property

    Public ReadOnly Property YM2612Emus As WAVExporter.OPNEmu() =
        {WAVExporter.OPNEmu.Internal, WAVExporter.OPNEmu.NukedOPN2}
    Private _YM2612Emu As WAVExporter.OPNEmu = WAVExporter.OPNEmu.NukedOPN2
    Public Property YM2612Emu As WAVExporter.OPNEmu
        Get
            Return _YM2612Emu
        End Get
        Set(value As WAVExporter.OPNEmu)
            _YM2612Emu = value
            Settings.YM2612Emu = value
            Settings.Save()
            NotifyPropertyChanged()
        End Set
    End Property


    '' Added on 2026/02/17 : Automatic tempo detection
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

    Public Sub New(fileName As String)
        _VGMFile = New VGMFile(fileName)
    End Sub

    Public NoteGapLengths As New SortedDictionary(Of Long, Long)

    Public Property TokenSource As New CancellationTokenSource

    Public Event Completed(sender As Object, e As EventArgs)

    Dim Progress As IProgress(Of Double) =
        New Progress(Of Double)(Sub(value) ProgressValue = value)

    Public Sub StartExport()
        Dim token As CancellationToken = TokenSource.Token

        Dim conv As New NullExporter(VGMFile)
        'Uncomment the following line to make the convert process synchronous (for easier debugging)
        conv.Convert(Progress, token) : RaiseEvent Completed(Me, New EventArgs)
        Dim t As New Task(Sub() conv.Convert(Progress, token), token)
        With conv
            For Each key As Long In .NoteGapLengths.Keys
                NoteGapLengths.Add(key, .NoteGapLengths(key))
            Next
        End With

    End Sub

End Class