Imports System.IO
Imports VGM_Studio.VGM

Public Class GD3Tag

#Region "GD3 Tag Specification v1.00"

    'GD3 v1.00 tag spec:
    'The GD3 tag Is a bit Like an ID3 tag only used For games.
    'All values are written In Intel Byte order.
    'The GD3 tag begins With a "Gd3 " String.
    'Next four bytes are the version number (current Is 0x00,0x01,0x00,0x00)
    'Next four bytes are a 32-bit length Of the following data In bytes.
    'The following data Is a series Of null-terminated strings Of this form:

    '"Track name (in English characters)\0"
    '"Track name (in Japanese characters)\0"
    '"Game name (in English characters)\0"
    '"Game name (in Japanese characters)\0"
    '"System name (in English characters)\0"
    '"System name (in Japanese characters)\0"
    '"Name of Original Track Author (in English characters)\0"
    '"Name of Original Track Author (in Japanese characters)\0"
    '"Date of game's release written in the form yyyy/mm/dd, or just yyyy/mm or yyyy if month and day is not known\0"
    '"Name of person who converted it to a VGM file.\0"
    '"Notes\0"

    'NOTE that two bytes are used To store every character, even English characters.
    'The null terminators are two zeros.
    'e.g. 0x54 0x00 0x72 0x00 0x61 0x00 0x63 0x00 ... 0x00 0x00
    'New-line characters should only be in the notes field,
    'where they are represented by 0x00 0x0a only.
    'You can write 16-bit strings In Visual C++ Like this:
    'unsigned short *MyString=L"Track name";

    'If a field In Not used Or unknown, it should be a empty String (i.e. two zero bytes
    'For the null terminator).

    'Be careful To include the system name In a standard form.
    'Here are some standard system names:
    '"Sega Master System"
    '"Sega Game Gear"
    '"Sega Master System / Game Gear"
    '"Sega Mega Drive / Genesis"
    '"Sega Game 1000"
    '"Sega Computer 3000"
    '"Sega System 16"
    '"Capcom Play System 1"
    '"Colecovision"
    '"BBC Model B"
    '"BBC Model B+"
    '"BBC Master 128"

    'Keep spelling, spacing And capitalization the same.

    'Note that the "Original Track Author" might be the composer, arranger, programmer
    'Or any combination of these.
#End Region

    Public ReadOnly Property ID As String
    Public Const IDCheckString As String = "Gd3 "

    Public Enum GD3Version As Integer
        Version_1_00 = &H100
    End Enum
    Public ReadOnly Property Version As GD3Version
    Public Const MaxSupportedVersion As GD3Version = GD3Version.Version_1_00

    Public ReadOnly Property IsVersionSupported As Boolean
        Get
            If Version <= MaxSupportedVersion Then
                Return True
            Else
                Return False
            End If
        End Get
    End Property

    Public ReadOnly Property Length As Integer

    Public Property TrackNameInEnglish As String
    Public Property TrackNameInJapanese As String
    Public Property GameNameInEnglish As String
    Public Property GameNameInJapanese As String
    Public Property SystemNameInEnglish As String
    Public Property SystemNameInJapanese As String
    Public Property OriginalAuthorNameInEnglish As String
    Public Property OriginalAuthorNameInJapanese As String
    Public Property GameReleaseDate As String
    Public Property ConversionAuthorName As String
    Public Property Notes As String

    Sub New(ByRef vgmFile As VGMFile)
        Using br As BinaryReader = vgmFile.GetReader()
            With br
                .BaseStream.Seek(vgmFile.GD3Offset, SeekOrigin.Begin)
                _ID = Text.Encoding.ASCII.GetChars(.ReadBytes(4))
                If ID <> IDCheckString Then
                    MessageBox.Show("Error: Invalid GD3 ID.")
                    Exit Sub
                End If

                _Version = .ReadInt32

                If IsVersionSupported = False Then
                    MessageBox.Show("Error: Unsupported GD3 version.")
                    Exit Sub
                End If

                _Length = .ReadInt32

                TrackNameInEnglish = ReadGD3String(br)
                TrackNameInJapanese = ReadGD3String(br)
                GameNameInEnglish = ReadGD3String(br)
                GameNameInJapanese = ReadGD3String(br)
                SystemNameInEnglish = ReadGD3String(br)
                SystemNameInJapanese = ReadGD3String(br)
                OriginalAuthorNameInEnglish = ReadGD3String(br)
                OriginalAuthorNameInJapanese = ReadGD3String(br)
                GameReleaseDate = ReadGD3String(br)
                ConversionAuthorName = ReadGD3String(br)
                Notes = ReadGD3String(br)

            End With

        End Using

    End Sub

    Private Function ReadGD3String(ByRef br As BinaryReader) As String
        Dim chr As String, str As String = ""
        Do
            chr = Text.Encoding.Unicode.GetChars(br.ReadBytes(2))
            If chr = ControlChars.NullChar Then
                Exit Do
            Else
                str &= chr
            End If
        Loop
        Return str
    End Function

End Class