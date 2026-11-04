

Imports VGM_Studio.VGM

Public Class CSVExporter
    Inherits Exporter

    Public Enum ExportTypeValue As Integer
        Voices
        NoteLengths
    End Enum

    Public ReadOnly Property ExportType As ExportTypeValue

    Sub New(ByRef vgmFile As VGMFile, exportType As Integer)
        MyBase.New(vgmFile, New ExportOptions)
        _ExportType = exportType
    End Sub

    Public Overrides Sub Save(fileName As String)

        Select Case ExportType
            Case ExportTypeValue.Voices

            Case ExportTypeValue.NoteLengths
                SaveNoteLengths(fileName)
        End Select

    End Sub

    Private Sub SaveNoteLengths(fileName As String)

        Dim NoteLengthsInMillisecs As New SortedList(Of Long, Long)

        For Each key As Long In NoteGapLengths.Keys
            Dim length As Long =
        (key / VGM_Sample_Rate) * 1000
            Dim count As Long =
                NoteGapLengths(key)
            If NoteLengthsInMillisecs.ContainsKey(length) Then
                NoteLengthsInMillisecs(length) += count
            Else
                NoteLengthsInMillisecs.Add(length, count)
            End If
        Next

        Using fsOut As New IO.FileStream(fileName, IO.FileMode.Create, IO.FileAccess.Write)
            Using swOut As New IO.StreamWriter(fsOut)

                swOut.WriteLine("Length (ms),Count")

                For Each key As Long In NoteLengthsInMillisecs.Keys
                    Dim count As Long =
                NoteLengthsInMillisecs(key)
                    swOut.WriteLine($"{key:0.00},{count}")
                Next

            End Using
        End Using

    End Sub

End Class


