Imports System.ComponentModel
Imports VGM_Studio.VGM

Class VGMInfoPage

    Public WithEvents ViewModel As InfoPageViewModel

    Public Sub New(fileName As String)

        ' This call is required by the designer.
        InitializeComponent()

        ' Add any initialization after the InitializeComponent() call.
        ViewModel = New InfoPageViewModel(fileName)
        DataContext = ViewModel

    End Sub

    Private Property HasBeenPopulated As Boolean = False

    Private Sub VGMInfoPage_Loaded(sender As Object, e As RoutedEventArgs) Handles Me.Loaded
        If HasBeenPopulated Then Exit Sub

        ViewModel.BuildTextBlock()

        MainTextBlock.Inlines.AddRange(ViewModel.TextBlock.Inlines.ToArray)

        HasBeenPopulated = True
    End Sub

End Class

Public Class InfoPageViewModel
    Implements INotifyPropertyChanged

    Public Event PropertyChanged(sender As Object, e As PropertyChangedEventArgs) Implements INotifyPropertyChanged.PropertyChanged

    Public Sub NotifyPropertyChanged(<Runtime.CompilerServices.CallerMemberName> Optional ByVal propertyName As String = Nothing)
        RaiseEvent PropertyChanged(Me, New PropertyChangedEventArgs(propertyName))
    End Sub

    Public ReadOnly Property VGMFile As VGMFile

    Public Sub New(fileName As String)
        _VGMFile = New VGMFile(fileName)
    End Sub

    Private _TextBlock As TextBlock
    Public Property TextBlock As TextBlock
        Get
            Return _TextBlock
        End Get
        Private Set(value As TextBlock)
            _TextBlock = value
            NotifyPropertyChanged()
        End Set
    End Property

    Public Sub BuildTextBlock()
        Dim tb As New TextBlock
        Dim inl As InlineCollection =
            tb.Inlines
        Dim nl As Char = ControlChars.NewLine

        With VGMFile
            With .GD3Tag
                inl.Add(New Bold(New Run($"{ .TrackNameInEnglish}{If(.TrackNameInJapanese = "", "", $" ({ .TrackNameInJapanese})")}{nl}")))
                inl.Add(New Run($"from { .GameNameInEnglish}{If(.GameNameInJapanese = "", "", $" ({ .GameNameInJapanese})")} "))
                inl.Add(New Run($"for { .SystemNameInEnglish}{If(.SystemNameInJapanese = "", "", $" ({ .SystemNameInJapanese})")}{nl}"))
                inl.Add(New Run($"by { .OriginalAuthorNameInEnglish}{If(.OriginalAuthorNameInJapanese = "", "", $" ({ .OriginalAuthorNameInJapanese})")}{nl}"))
                inl.Add(New Run($"Released { .GameReleaseDate}{nl}"))
                inl.Add(New Run($"Converted by { .ConversionAuthorName}{nl}"))
                If .Notes <> "" Then
                    inl.Add(New Run($"Notes:{nl} { .Notes}{nl}"))
                End If
            End With

            inl.Add(New Run(nl))
            inl.Add(New Bold(New Run($"Sound chip(s) used:{nl}")))

            If .SN76489Clock <> 0 Then inl.Add(New Run($"{If(.IsDualSN76489, "Dual ", "")}SN76489 @ { .SN76489Clock} Hz{nl}"))
            If .YM2413Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM2413, "Dual ", "")}YM2413 @ { .YM2413Clock} Hz{nl}"))
            If .YM2612Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM2612, "Dual ", "")}YM2612 @ { .YM2612Clock} Hz{nl}"))
            If .YM2151Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM2151, "Dual ", "")}YM2151 @ { .YM2151Clock} Hz{nl}"))
            If .SegaPCMClock <> 0 Then
                Dim bankShift As Integer = .SegaPCMInterface And &HF
                Dim bankMask As Integer = (SegaPCM.BankMaskValue.Mask70 Or ((.SegaPCMInterface >> 16) And &HFC))
                inl.Add(New Run($"{If(.IsDualSegaPCM, "Dual ", "")}Sega PCM @ { .SegaPCMClock} Hz, bank shift {bankShift}, bank mask 0x{bankMask:X2}{nl}"))
            End If
            If .RF5C68Clock <> 0 Then inl.Add(New Run($"{If(.IsDualRF5C68, "Dual ", "")}RF5C68 @ { .RF5C68Clock} Hz{nl}"))
            If .YM2203Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM2203, "Dual ", "")}YM2203 @ { .YM2203Clock} Hz{nl}"))
            If .YM2608Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM2608, "Dual ", "")}YM2608 @ { .YM2608Clock} Hz{nl}"))
            If .YM2610Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM2610, "Dual ", "")}{If(.IsYM2610B, "YM2610B", "YM2610")} @ { .YM2610Clock} Hz{nl}"))
            If .YM3812Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM3812, "Dual ", "")}YM3812 @ { .YM3812Clock} Hz{nl}"))
            If .YM3526Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYM3526, "Dual ", "")}YM3526 @ { .YM3526Clock} Hz{nl}"))
            If .Y8950Clock <> 0 Then inl.Add(New Run($"{If(.IsDualY8950, "Dual ", "")}Y8950 @ { .Y8950Clock} Hz{nl}"))
            If .YMF262Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYMF262, "Dual ", "")}YMF262 @ { .YMF262Clock} Hz{nl}"))
            If .YMF278BClock <> 0 Then inl.Add(New Run($"{If(.IsDualYMF278B, "Dual ", "")}YMF278B @ { .YMF278BClock} Hz{nl}"))
            If .YMF271Clock <> 0 Then inl.Add(New Run($"{If(.IsDualYMF271, "Dual ", "")}YMF271 @ { .YMF271Clock} Hz{nl}"))
            If .YMZ280BClock <> 0 Then inl.Add(New Run($"{If(.IsDualYMZ280B, "Dual ", "")}YMZ280B @ { .YMZ280BClock} Hz{nl}"))
            If .RF5C164Clock <> 0 Then inl.Add(New Run($"{If(.IsDualRFC5164, "Dual ", "")}RF5C164 @ { .RF5C164Clock} Hz{nl}"))
            If .PWMClock <> 0 Then inl.Add(New Run($"{If(.IsDualPWM, "Dual ", "")}PWM @ { .PWMClock} Hz{nl}"))
            If .AY8910Clock <> 0 Then inl.Add(New Run($"{If(.IsDualAY8910, "Dual ", "")}AY-3-8910 type { .AY8910Type} @ { .AY8910Clock} Hz, { .AY8910Flags}{nl}"))

            If .GameBoyDMGClock <> 0 Then inl.Add(New Run($"{If(.IsDualGameBoyDMG, "Dual ", "")}GameBoy DMG @ { .GameBoyDMGClock} Hz{nl}"))
            If .NESAPUClock <> 0 Then inl.Add(New Run($"{If(.IsDualNESAPU, "Dual ", "")}NES APU @ { .NESAPUClock} Hz{nl}"))
            If .MultiPCMClock <> 0 Then inl.Add(New Run($"{If(.IsDualMultiPCM, "Dual ", "")}MultiPCM @ { .MultiPCMClock} Hz{nl}"))
            If .uPD7759Clock <> 0 Then inl.Add(New Run($"{If(.IsDualuPD7759, "Dual ", "")}uPD7759 @ { .uPD7759Clock} Hz{nl}"))
            If .MSM6258Clock <> 0 Then inl.Add(New Run($"{If(.IsDualMSM6258, "Dual ", "")}OKIM6258 @ { .MSM6258Clock} Hz, { .MSM6258Flags}{nl}"))

            If .MSM6295Clock <> 0 Then inl.Add(New Run($"{If(.IsDualMSM6295, "Dual ", "")}MSM6295 @ { .MSM6295Clock} Hz{nl}"))
            If .K051649Clock <> 0 Then inl.Add(New Run($"{If(.IsDualK051649, "Dual ", "")}K051649 @ { .K051649Clock} Hz{nl}"))
            If .K054539Clock <> 0 Then inl.Add(New Run($"{If(.IsDualK054539, "Dual ", "")}K054539 @ { .K054539Clock} Hz, { .K054539Flags}{nl}"))
            If .HuC6280Clock <> 0 Then inl.Add(New Run($"{If(.IsDualHuC6280, "Dual ", "")}HuC6280 @ { .HuC6280Clock} Hz{nl}"))
            If .C140Clock <> 0 Then inl.Add(New Run($"{If(.IsDualC140, "Dual ", "")}C140 ({ .C140Type.GetDescription}) @ { .C140Clock} Hz{nl}"))
            If .K053260Clock <> 0 Then inl.Add(New Run($"{If(.IsDualK053260, "Dual ", "")}K053260 @ { .K053260Clock} Hz{nl}"))
            If .PokeyClock <> 0 Then inl.Add(New Run($"{If(.IsDualPokey, "Dual ", "")}Pokey @ { .PokeyClock} Hz{nl}"))
            If .QSoundClock <> 0 Then inl.Add(New Run($"{If(.IsDualQSound, "Dual ", "")}QSound @ { .QSoundClock} Hz{nl}"))

            inl.Add(New Run(nl))
            inl.Add(New Bold(New Run($"Playback info:{nl}")))

            inl.Add(New Run($"{ .FileInfo.FullName}{nl}"))
            inl.Add(New Run($"VGM v{ .VersionMajor}.{ .VersionMinor}{nl}"))
            If .ExtraHeaderOffset <> 0 Then inl.Add(New Run($"Extra header @ 0x{ .ExtraHeaderOffset:X8}{nl}"))
            inl.Add(New Run($"Sampled @ { .Rate} Hz{nl}"))
            inl.Add(New Run($"{ .TotalSamples} total samples{nl}"))
            If .LoopSamples <> 0 Then inl.Add(New Run($"Loop @ 0x{ .LoopOffset:X8} ({ .LoopSamples} samples){nl}"))
            inl.Add(New Run($"Volume modifier = { .VolumeModifier }{nl}"))
            inl.Add(New Run($"Loop base = { .LoopBase }{nl}"))
            inl.Add(New Run($"Loop modifier = { .LoopModifier }{nl}"))

        End With

        TextBlock = tb

    End Sub

End Class


