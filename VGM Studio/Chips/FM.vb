Public Class FM

    Public MustInherit Class Voice

        Protected _Index As UInteger
        Public Property Index As UInteger
            Get
                Return _Index
            End Get
            Protected Set(value As UInteger)

            End Set
        End Property

        Protected _VoiceName As String
        Public Property VoiceName As String
            Get
                Return _VoiceName
            End Get
            Set(value As String)
                _VoiceName = value.PadRight(10).Substring(0, 10)
            End Set
        End Property

        Protected _MasterClockFreq As UInteger
        Public Property MasterClockFreq As UInteger
            Get
                Return _MasterClockFreq
            End Get
            Protected Set(value As UInteger)
                _MasterClockFreq = value
            End Set
        End Property

        Public MustOverride ReadOnly Property InfoString(sep As Char) As String

        Sub New(index As Long)
            Me.Index = index
            VoiceName = "FMVOICE"
            MasterClockFreq = 0
        End Sub

    End Class

End Class