Imports System.IO
Imports System.IO.Compression
Imports System.Reflection
Imports Microsoft.Win32

Class HomePage

    'Public ReadOnly Property VGMFIle As VGMFile
    Public ReadOnly Property VGMFileInfo As FileInfo

    Public Sub New()

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.

    End Sub

    Private Sub VGMOptionsPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        Dim name As String =
            Assembly.GetExecutingAssembly().GetName().Name
        WindowTitle = name
    End Sub

    Private Sub OpenButton_Click(sender As Object, e As RoutedEventArgs) Handles OpenButton.Click
        Dim ofd As New OpenFileDialog

        With ofd
            .AddExtension = True
            .CheckFileExists = True
            .CheckPathExists = True
            .DefaultExt = ".vgz"
            .Filter = "All supported files|*.vgm;*.vgz;*.zip|VGM files|*.vgm;*.vgz|ZIP archives|*.zip"
            .Multiselect = False
            .InitialDirectory = My.Settings.LastImportDirectory
            .FileName = My.Settings.LastOpenedFilename

            Select Case .ShowDialog
                Case True

                    'ADDED on 2026/7/12: Support for ZIP archives
                    Dim fileName As String = .FileName
                    Dim info As New FileInfo(fileName)
                    Dim ext As String = info.Extension.ToLower

                    With My.Settings
                        .LastImportDirectory = Path.GetDirectoryName(fileName)
                        .LastOpenedFilename = fileName
                        .Save()
                    End With

                    'If a ZIP archive is chosen
                    If ext = ".zip" Then

                        Using archive As New ZipArchive(ofd.OpenFile)
                            'Get a list of vgm files from the archive
                            Dim VGMFileList As New List(Of String)
                            For Each entry As ZipArchiveEntry In archive.Entries
                                Dim entryExt As String = IO.Path.GetExtension(entry.Name)
                                If (entryExt = ".vgm") Or (entryExt = ".vgz") Then
                                    VGMFileList.Add(entry.Name)
                                End If
                            Next

                            'If the list contains at least one item ...
                            If VGMFileList.Count > 0 Then
                                Dim zaWindow As New ZipArchiveEntrySelectionWindow(IO.Path.GetFileName(fileName), VGMFileList.ToArray) With {
                                    .Owner = Application.Current.MainWindow}

                                Select Case zaWindow.ShowDialog()
                                    Case True
                                        If zaWindow.SelectedEntryName <> "" Then
                                            Dim entry As ZipArchiveEntry = archive.GetEntry(zaWindow.SelectedEntryName)
                                            Dim tempFileName As String = IO.Path.Combine(IO.Path.GetTempPath, entry.Name)
                                            entry.ExtractToFile(tempFileName, True)
                                            fileName = tempFileName
                                        End If
                                    Case False
                                        Exit Sub
                                End Select
                            Else
                                MessageBox.Show($"No VGM files found", IO.Path.GetFileName(fileName), MessageBoxButton.OK, MessageBoxImage.Information)
                                Exit Sub
                            End If
                        End Using

                    End If

                    _VGMFileInfo = New FileInfo(fileName)

                    'Dim vgmOptionsPage As New VGMOptionsPage(fileName)
                    'NavigationService.Navigate(vgmOptionsPage)

                    infoPage = Nothing
                    eventsPage = Nothing
                    voicesPage = Nothing
                    exportPage = Nothing

                    InfoButton.IsEnabled = True
                    EventsButton.IsEnabled = True
                    VoicesButton.IsEnabled = True
                    ExportButton.IsEnabled = True

                    With System.Windows.Application.Current.MainWindow
                        .Title = $"{VGMFileInfo.Name} - { Assembly.GetEntryAssembly().GetName().Name }"
                    End With

                    InfoButton_Click(sender, e)

                Case False

            End Select

        End With

    End Sub

    Dim infoPage As VGMInfoPage

    Private Sub InfoButton_Click(sender As Object, e As RoutedEventArgs) Handles InfoButton.Click
        If infoPage Is Nothing Then infoPage = New VGMInfoPage(VGMFileInfo.FullName)
        MainFrame.Navigate(infoPage)
    End Sub

    Dim eventsPage As VGMEventsPage

    Private Sub EventsButton_Click(sender As Object, e As RoutedEventArgs) Handles EventsButton.Click
        If eventsPage Is Nothing Then eventsPage = New VGMEventsPage(VGMFileInfo.FullName)
        MainFrame.Navigate(eventsPage)
    End Sub

    Dim voicesPage As VGMVoicesPage

    Private Sub VoicesButton_Click(sender As Object, e As RoutedEventArgs) Handles VoicesButton.Click
        If voicesPage Is Nothing Then voicesPage = New VGMVoicesPage(VGMFileInfo.FullName)
        MainFrame.Navigate(voicesPage)
    End Sub

    Dim exportPage As VGMSingleExportPage

    Private Sub ExportButton_Click(sender As Object, e As RoutedEventArgs) Handles ExportButton.Click
        'If TypeOf (MainFrame.Content) Is VGMSingleExportPage Then Exit Sub
        If exportPage Is Nothing Then exportPage = New VGMSingleExportPage(VGMFileInfo.FullName, Me)
        MainFrame.Navigate(exportPage)
    End Sub

End Class