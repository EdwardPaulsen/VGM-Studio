Imports System.ComponentModel
Imports System.IO
Imports System.Runtime.CompilerServices
Imports System.Text.RegularExpressions

Public Module Helpers
    Public Function GetBitShift(mask As Integer) As Integer
        For i As Integer = 0 To 31
            If (mask >> i) And 1 Then
                Return i
            End If
        Next
        Return 0
    End Function

    Public Function ReverseBits8(b As Byte) As Byte
        Dim x As Integer = b
        ' Swap 4-bit nibbles
        x = ((x And &B_11110000) >> 4) Or ((x And &B_00001111) << 4)
        ' Swap 2-bit pairs
        x = ((x And &B_11001100) >> 2) Or ((x And &B_00110011) << 2)
        ' Swap single bits
        x = ((x And &B_10101010) >> 1) Or ((x And &B_01010101) << 1)
        Return x
    End Function

    Public Function ReverseBitsLoop(n As UInteger, bitCount As Integer) As UInteger
        Dim result As UInteger = 0

        For bit As Integer = 0 To bitCount - 1
            ' Shift result left to make room, then extract and OR the lowest bit of n
            result = (result << 1) Or (n And 1UI)
            ' Shift n right to process the next bit
            n >>= 1
        Next

        Return result
    End Function

    Public Function IsFileInUse(path As String) As Boolean
        ' First, verify if the file actually exists
        If Not File.Exists(path) Then
            Return False
        End If

        Try
            ' Attempt to open the file exclusively with no sharing
            Using fs As New FileStream(path, FileMode.Open, FileAccess.ReadWrite, FileShare.None)
                ' If successful, the file is not in use
                Return False
            End Using
        Catch ex As IOException
            ' An IOException means another process has locked the file
            Return True
        Catch ex As Exception
            ' Handle other potential errors (like security/permission issues)
            Return True
        End Try
    End Function

    Public Function RemoveIllegalFileNameChars(input As String, Optional replacement As String = "") As String
        Dim regexSearch = New String(Path.GetInvalidFileNameChars()) & New String(Path.GetInvalidPathChars())
        Dim r = New Regex(String.Format("[{0}]", Regex.Escape(regexSearch)))
        Return r.Replace(input, replacement)
    End Function


    <Extension()>
    Public Function GetDescription(value As [Enum]) As String
        Dim fieldType = value.[GetType]()
        Dim fieldInfo = fieldType.GetField(value.ToString())

        If fieldInfo Is Nothing Then
            Return value.ToString()
        End If

        Dim attributes = DirectCast(fieldInfo.GetCustomAttributes(GetType(DescriptionAttribute), False), DescriptionAttribute())

        If attributes IsNot Nothing AndAlso attributes.Length > 0 Then
            Return attributes(0).Description
        End If

        Return value.ToString()
    End Function

End Module
