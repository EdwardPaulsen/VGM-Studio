Public Class ZipArchiveEntrySelectionWindow

    Private _SelectedEntryName As String
    Public Property SelectedEntryName As String
        Private Set(ByVal value As String)
            _SelectedEntryName = value
        End Set
        Get
            Return _SelectedEntryName
        End Get
    End Property

    Public Sub New(fileName As String, entryNames As String())

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        FileNameLabel.Content = fileName

        With MainListBox
            .ItemsSource = (entryNames)
            .SelectedIndex = 0
        End With

    End Sub

    Private Sub OKButton_Click(sender As Object, e As RoutedEventArgs) Handles OKButton.Click
        DialogResult = True
    End Sub

    Private Sub CancelButton_Click(sender As Object, e As RoutedEventArgs) Handles CancelButton.Click

    End Sub

    Private Sub MainListBox_SelectionChanged(sender As Object, e As SelectionChangedEventArgs) Handles MainListBox.SelectionChanged
        SelectedEntryName = MainListBox.SelectedItem.ToString
    End Sub

    Private Sub MainListBox_MouseDoubleClick(sender As Object, e As MouseButtonEventArgs) Handles MainListBox.MouseDoubleClick
        DialogResult = True
    End Sub

End Class
