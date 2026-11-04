Imports VGM_Studio.VGM

Public Class NullExporter
    Inherits Exporter

    Sub New(ByRef vgmfile As VGMFile)
        MyBase.New(vgmfile, New ExportOptions)
    End Sub

    Public Overrides Sub Save(fileName As String)
    End Sub

End Class