Imports System.ComponentModel
Imports System.IO
Imports System.Threading
Imports System.Windows.Threading
Imports VGM_Studio.VGM

Public Class SingleExportWindow

    Public WithEvents ViewModel As SingleExportWindowViewModel

    Public Sub New(srcFileName As String, dstFileName As String, options As ExportOptions) 'exportType As SingleExportViewModel.ExportTypeValue)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ViewModel = New SingleExportWindowViewModel(srcFileName, dstFileName, options) 'exportType)
        DataContext = ViewModel

    End Sub

    Private Sub OnCompleted(sender As Object, e As EventArgs) Handles ViewModel.Completed
        Dispatcher.Invoke(New Action(Sub() Close()))
    End Sub

    Private Sub OnCanceled(sender As Object, e As EventArgs) Handles ViewModel.Canceled
        Dispatcher.Invoke(New Action(Sub() Close()))
    End Sub

    Private Sub SingleExportWindow_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        ViewModel.StartExport()
    End Sub

    Private Sub SingleExportWindow_Closing(sender As Object, e As CancelEventArgs) Handles Me.Closing
        ViewModel.Cancel()
    End Sub

End Class

'Public Class VoiceExportOptions
'    Inherits ExportOptions

'End Class

Public Class NoteLengthExportOptions
    Inherits ExportOptions

End Class

Public Class SingleExportWindowViewModel
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

    Public ReadOnly Property SrcFileInfo As FileInfo
    Public ReadOnly Property DstFileInfo As FileInfo

    Public ReadOnly Property Options As ExportOptions

    Public Sub New(srcFileName As String, dstFileName As String, options As ExportOptions) 'exportType As ExportTypeValue)
        _SrcFileInfo = New FileInfo(srcFileName)
        _DstFileInfo = New FileInfo(dstFileName)
        _Options = options
    End Sub

    Public Property TokenSource As New CancellationTokenSource

    Public Event Completed(sender As Object, e As EventArgs)

    Public Sub Cancel()
        TokenSource.Cancel()
    End Sub

    Public Event Canceled(sender As Object, e As EventArgs)

    Dim Progress As IProgress(Of Double) =
        New Progress(Of Double)(Sub(value) ProgressValue = value)

    Public Sub StartExport()
        Dim token As CancellationToken = TokenSource.Token

        Dim srcVGMFile As New VGMFile(SrcFileInfo.FullName)

        Dim conv As Exporter =
            New NullExporter(srcVGMFile)

        If TypeOf (Options) Is MidiExportOptions Then
            conv = New MidiExporter(srcVGMFile, Options)
        ElseIf TypeOf (Options) Is WavExportOptions Then
            conv = New WAVExporter(srcVGMFile, Options)
        ElseIf TypeOf (Options) Is VoiceExportOptions Then
            conv = New CSVExporter(srcVGMFile, CSVExporter.ExportTypeValue.Voices)
        ElseIf TypeOf (Options) Is NoteLengthExportOptions Then
            conv = New CSVExporter(srcVGMFile, CSVExporter.ExportTypeValue.NoteLengths)
        End If

        If conv IsNot Nothing Then
            ProgressState = Shell.TaskbarItemProgressState.Normal

            Dim t As New Task(Sub() conv.Convert(Progress, token), token)

            Dim tCompleted As Task =
                t.ContinueWith(Sub()
                                   conv.Save(DstFileInfo.FullName)
                                   RaiseEvent Completed(Me, New EventArgs)
                               End Sub,
                                TaskContinuationOptions.NotOnCanceled Or TaskContinuationOptions.NotOnFaulted Or TaskContinuationOptions.ExecuteSynchronously
                                )

            Dim tCancelled As Task =
                t.ContinueWith(Sub()
                                   RaiseEvent Canceled(Me, New EventArgs)
                               End Sub,
                                 TaskContinuationOptions.OnlyOnCanceled Or TaskContinuationOptions.ExecuteSynchronously
                                 )

            Dim tFaulted As Task =
                    t.ContinueWith(Sub()
                                       MessageBox.Show("Faulted")
                                   End Sub,
                                 TaskContinuationOptions.OnlyOnFaulted Or TaskContinuationOptions.ExecuteSynchronously
                                 )

            t.Start()

        End If

    End Sub

End Class
