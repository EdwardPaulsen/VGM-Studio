'Namespace YM3812

'#Region "Enums"

'    Public Enum RegAddr As Byte
'        IRO_TimerFlags_Test = &H1
'        Timer1 = &H2
'        Timer2 = &H3
'        IRQReset_TimerControl = &H4
'        CSM_NoteSelect = &H8
'        AM_Vib_EGType_KSR_Multi = &H20
'        KSL_TL = &H40
'        AR_DR = &H60
'        SL_RR = &H80
'        FNumLSBs = &HA0
'        KeyOn_Block_FNumMSBs = &HB0
'        AMDepth_VibDepth_Rhythm = &HBD
'        FB_Con = &HC0
'        WaveSelect = &HE0
'    End Enum

'    Public Enum RegDataMask As Byte
'        IRQ = 1
'        Timer1Flag = 1
'        Timer2Flag = 1
'        Test = 31
'        Timer1 = 255
'        Timer2 = 255
'        IRQReset = 1
'        Timer1Mask = 1
'        Timer2Mask = 1
'        Timer1State = 1
'        Timer2State = 1
'        CSM = 1
'        NoteSelect = 1
'        AM = 1
'        Vib = 1
'        EGType = 1
'        KSR = 1
'        Multi = 15
'        KSL = 3
'        TL = 63
'        AR = 15
'        DR = 15
'        SL = 15
'        RR = 15
'        FNumLSBs = 255
'        KeyOn = 1
'        Block = 7
'        FNumMSBs = 3
'        AMDepth = 1
'        VibDepth = 1
'        RhythmEn = 1
'        BD = 1
'        SD = 1
'        TOM = 1
'        TC = 1
'        HH = 1
'        FB = 7
'        Con = 1
'        WaveSelect = 3
'    End Enum

'    Public Enum RegDataBitShiftR As Byte
'        IRQ = 7
'        Timer1Flag = 6
'        Timer2Flag = 5
'        Test = 0
'        Timer1 = 0
'        Timer2 = 0
'        IRQReset = 7
'        Timer1Mask = 6
'        Timer2Mask = 5
'        Timer1State = 1
'        Timer2State = 0
'        CSM = 7
'        NoteSelect = 6
'        AM = 7
'        Vib = 6
'        EGType = 5
'        KSR = 4
'        Multi = 0
'        KSL = 6
'        TL = 0
'        AR = 4
'        DR = 0
'        SL = 4
'        RR = 0
'        FNumLSBs = 0
'        KeyOn = 5
'        Block = 2
'        FNumMSBs = 0
'        AMDepth = 7
'        VibDepth = 6
'        RhythmEn = 5
'        BD = 4
'        SD = 3
'        TOM = 2
'        TC = 1
'        HH = 0
'        FB = 1
'        Con = 0
'        WaveSelect = 0
'    End Enum

'    Public Enum WaveSelectType As Byte
'        Sine = 0
'        HalfSine = 1
'        AbsoluteSine = 2
'        PseudoSawtooth = 3
'    End Enum

'#End Region

'    'Public Class FNum
'    '    Public Shared Function ToHz( FNum As Double,  fsam As Double, Optional  Block As Byte = 0) As Double
'    '        Return (FNum * fsam * 2 ^ (Block - 19))
'    '    End Function
'    'End Class

'    Public Class Chip

'#Region "Properties"

'        ' Base oscillator frequency constants
'        Const Master_Clock_NTSC_60Hz As Long = 53693100
'        Const Master_Clock_PAL_50Hz As Long = 44744250

'        ' Chip frequencies (fM)
'        Const Chip_Clock_NTSC_60Hz As Long = Master_Clock_NTSC_60Hz \ 14
'        Const Chip_Clock_PAL_50Hz As Long = Master_Clock_PAL_50Hz \ 14

'        ' Chip frequency (fM)
'        Private _fM As Long
'        Public Property fM() As Single
'            Get
'                Return _fM
'            End Get
'            Set(value As Single)
'                _fM = value
'            End Set
'        End Property

'        ' Sample frequency (fsam)
'        Public ReadOnly Property fsam() As Long
'            Get
'                Return fM \ 72
'            End Get
'        End Property

'        Public ReadOnly Property IRQ() As Boolean
'            Get
'                Return (Read(RegAddr.IRO_TimerFlags_Test >> RegDataBitShiftR.IRQ) And RegDataMask.IRQ) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Timer1Flag() As Boolean
'            Get
'                Return (Read(RegAddr.IRO_TimerFlags_Test >> RegDataBitShiftR.Timer1Flag) And RegDataMask.Timer1Flag) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Timer2Flag() As Boolean
'            Get
'                Return (Read(RegAddr.IRO_TimerFlags_Test >> RegDataBitShiftR.Timer2Flag) And RegDataMask.Timer2Flag) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Test() As Byte
'            Get
'                Return Read(RegAddr.IRO_TimerFlags_Test >> RegDataBitShiftR.Test) And RegDataMask.Test
'            End Get
'        End Property

'        Public ReadOnly Property Timer1() As Byte
'            Get
'                Return Read(RegAddr.Timer1 >> RegDataBitShiftR.Timer1) And RegDataMask.Timer1
'            End Get
'        End Property

'        Public ReadOnly Property Timer2() As Byte
'            Get
'                Return Read(RegAddr.Timer2 >> RegDataBitShiftR.Timer2) And RegDataMask.Timer2
'            End Get
'        End Property

'        Public ReadOnly Property IRQReset() As Boolean
'            Get
'                Return (Read(RegAddr.IRQReset_TimerControl >> RegDataBitShiftR.IRQReset) And RegDataMask.IRQReset) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Timer1Mask() As Byte
'            Get
'                Return Read(RegAddr.IRQReset_TimerControl >> RegDataBitShiftR.Timer1Mask) And RegDataMask.Timer1Mask
'            End Get
'        End Property

'        Public ReadOnly Property Timer2Mask() As Byte
'            Get
'                Return Read(RegAddr.IRQReset_TimerControl >> RegDataBitShiftR.Timer2Mask) And RegDataMask.Timer2Mask
'            End Get
'        End Property

'        Public ReadOnly Property Timer1State() As Boolean
'            Get
'                Return (Read(RegAddr.IRQReset_TimerControl >> RegDataBitShiftR.Timer1State) And RegDataMask.Timer1State) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Timer2State() As Boolean
'            Get
'                Return (Read(RegAddr.IRQReset_TimerControl >> RegDataBitShiftR.Timer2State) And RegDataMask.Timer2State) = 1
'            End Get
'        End Property

'        Public ReadOnly Property CSM() As Boolean
'            Get
'                Return (Read(RegAddr.CSM_NoteSelect >> RegDataBitShiftR.CSM) And RegDataMask.CSM) = 1
'            End Get
'        End Property

'        Public ReadOnly Property NoteSelect() As Boolean
'            Get
'                Return (Read(RegAddr.CSM_NoteSelect >> RegDataBitShiftR.NoteSelect) And RegDataMask.NoteSelect) = 1
'            End Get
'        End Property

'        Public ReadOnly Property AM(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AM_Vib_EGType_KSR_Multi + GetAddr(ch, op)) >> RegDataBitShiftR.AM) And RegDataMask.AM
'            End Get
'        End Property

'        Public ReadOnly Property Vib(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AM_Vib_EGType_KSR_Multi + GetAddr(ch, op)) >> RegDataBitShiftR.Vib) And RegDataMask.Vib
'            End Get
'        End Property

'        Public ReadOnly Property EGType(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AM_Vib_EGType_KSR_Multi + GetAddr(ch, op)) >> RegDataBitShiftR.EGType) And RegDataMask.EGType
'            End Get
'        End Property

'        Public ReadOnly Property KSR(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AM_Vib_EGType_KSR_Multi + GetAddr(ch, op)) >> RegDataBitShiftR.KSR) And RegDataMask.KSR
'            End Get
'        End Property

'        Public ReadOnly Property Multi(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AM_Vib_EGType_KSR_Multi + GetAddr(ch, op)) >> RegDataBitShiftR.Multi) And RegDataMask.Multi
'            End Get
'        End Property

'        Public ReadOnly Property KSL(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.KSL_TL + GetAddr(ch, op)) >> RegDataBitShiftR.KSL) And RegDataMask.KSL
'            End Get
'        End Property

'        Public ReadOnly Property TL(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.KSL_TL + GetAddr(ch, op)) >> RegDataBitShiftR.TL) And RegDataMask.TL
'            End Get
'        End Property

'        Public ReadOnly Property AR(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AR_DR + GetAddr(ch, op)) >> RegDataBitShiftR.AR) And RegDataMask.AR
'            End Get
'        End Property

'        Public ReadOnly Property DR(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.AR_DR + GetAddr(ch, op)) >> RegDataBitShiftR.DR) And RegDataMask.DR
'            End Get
'        End Property

'        Public ReadOnly Property SL(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.SL_RR + GetAddr(ch, op)) >> RegDataBitShiftR.SL) And RegDataMask.SL
'            End Get
'        End Property

'        Public ReadOnly Property RR(ch As Byte, op As Byte) As Byte
'            Get
'                Return (Read(RegAddr.SL_RR + GetAddr(ch, op)) >> RegDataBitShiftR.RR) And RegDataMask.RR
'            End Get
'        End Property

'        Public ReadOnly Property FNumLSBs(ch As Byte) As Byte
'            Get
'                Return Read(RegAddr.FNumLSBs + ch) And RegDataMask.FNumLSBs
'            End Get
'        End Property

'        Public ReadOnly Property KeyOn(ch As Byte) As Boolean
'            Get
'                Return ((Read(RegAddr.KeyOn_Block_FNumMSBs + ch) >> RegDataBitShiftR.KeyOn) And RegDataMask.KeyOn) = 1
'            End Get
'        End Property

'        Public ReadOnly Property Block(ch As Byte) As Byte
'            Get
'                Return (Read(RegAddr.KeyOn_Block_FNumMSBs + ch) >> RegDataBitShiftR.Block) And RegDataMask.Block
'            End Get
'        End Property

'        Public ReadOnly Property FNumMSBs(ch As Byte) As Byte
'            Get
'                Return (Read(RegAddr.KeyOn_Block_FNumMSBs + ch) >> RegDataBitShiftR.FNumMSBs) And RegDataMask.FNumMSBs
'            End Get
'        End Property

'        Public ReadOnly Property FNum(ch As Byte) As Integer
'            Get
'                Dim intFNum As Integer =
'                    FNumMSBs(ch)
'                intFNum <<= 8
'                intFNum += FNumLSBs(ch)
'                Return intFNum
'            End Get
'        End Property

'        Public ReadOnly Property FNote(ch As Byte) As Single
'            Get
'                Return GetFNote(FNum(ch), Block(ch), fsam)
'            End Get
'        End Property

'        Public Shared Function GetFNote(fnum As Integer, block As Byte, fsam As Long) As Single
'            'F = (fmus * (2 ^ 19) / fsam) / 2 ^ (b - 1)
'            Return fnum * (2 ^ (block - 1)) / ((2 ^ 19) / fsam)
'        End Function

'        Public ReadOnly Property AMDepth() As Byte
'            Get
'                Return (Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.AMDepth) And RegDataMask.AMDepth
'            End Get
'        End Property

'        Public ReadOnly Property VibDepth() As Byte
'            Get
'                Return (Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.VibDepth) And RegDataMask.VibDepth
'            End Get
'        End Property

'        Public ReadOnly Property RhythmEn() As Boolean
'            Get
'                Return ((Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.RhythmEn) And RegDataMask.RhythmEn) = 1
'            End Get
'        End Property

'        Public ReadOnly Property BD() As Boolean
'            Get
'                Return ((Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.BD) And RegDataMask.BD) = 1
'            End Get
'        End Property

'        Public ReadOnly Property BDTL() As Byte
'            Get
'                'Bass drum volume(s) at offsets &H10 and &H13 from KSL_TL address
'                Return (Read(RegAddr.KSL_TL + &H10) >> RegDataBitShiftR.TL) And RegDataMask.TL
'            End Get
'        End Property

'        Public ReadOnly Property SD() As Boolean
'            Get
'                Return ((Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.SD) And RegDataMask.SD) = 1
'            End Get
'        End Property

'        Public ReadOnly Property SDTL() As Byte
'            Get
'                'Snare drum volume at offset &H14 from KSL_TL address
'                Return (Read(RegAddr.KSL_TL + &H14) >> RegDataBitShiftR.TL) And RegDataMask.TL
'            End Get
'        End Property

'        Public ReadOnly Property TOM() As Boolean
'            Get
'                Return ((Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.TOM) And RegDataMask.TOM) = 1
'            End Get
'        End Property

'        Public ReadOnly Property TOMTL() As Byte
'            Get
'                'Tom volume at offset &H12 from KSL_TL address
'                Return (Read(RegAddr.KSL_TL + &H12) >> RegDataBitShiftR.TL) And RegDataMask.TL
'            End Get
'        End Property

'        Public ReadOnly Property TC() As Boolean
'            Get
'                Return ((Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.TC) And RegDataMask.TC) = 1
'            End Get
'        End Property

'        Public ReadOnly Property TCTL() As Byte
'            Get
'                'Top cymbal volume at offset &H15 from KSL_TL address
'                Return (Read(RegAddr.KSL_TL + &H15) >> RegDataBitShiftR.TL) And RegDataMask.TL
'            End Get
'        End Property

'        Public ReadOnly Property HH() As Boolean
'            Get
'                Return ((Read(RegAddr.AMDepth_VibDepth_Rhythm) >> RegDataBitShiftR.HH) And RegDataMask.HH) = 1
'            End Get
'        End Property

'        Public ReadOnly Property HHTL() As Byte
'            Get
'                'Hi-hat volume at offset &H11 from KSL_TL address
'                Return (Read(RegAddr.KSL_TL + &H11) >> RegDataBitShiftR.TL) And RegDataMask.TL
'            End Get
'        End Property

'        Public ReadOnly Property FB(ch As Byte) As Byte
'            Get
'                Return Read((RegAddr.FB_Con + ch) >> RegDataBitShiftR.FB) And RegDataMask.FB
'            End Get
'        End Property

'        Public ReadOnly Property Con(ch As Byte) As Byte
'            Get
'                Return Read((RegAddr.FB_Con + ch) >> RegDataBitShiftR.Con) And RegDataMask.Con
'            End Get
'        End Property

'        Public ReadOnly Property WaveSelect(ch As Byte) As Byte
'            Get
'                Return Read((RegAddr.WaveSelect + ch) >> RegDataBitShiftR.WaveSelect) And RegDataMask.WaveSelect
'            End Get
'        End Property


'#End Region

'        Sub New(clockfreq As Integer)
'            MyBase.New()
'            With Me
'                .fM = clockfreq
'            End With
'        End Sub

'        Dim Mem(&HF5) As Byte   'Stores register data

'        Public Function Read(reg As Byte) As Byte
'            Return Mem(reg)
'        End Function

'        Public Sub Write(addr As RegAddr, value As Byte)
'            Mem(addr) = value
'            Select Case addr
'                Case &H1 To &H8, &HBD
'                    RaiseCommonEvent(addr)
'                    Exit Sub
'            End Select
'            Select Case addr And &HF0
'                Case &H20 To &H90
'                    RaiseChannelEvent(addr)
'                Case &HA0 To &HF0
'                    RaiseKeyEvent(addr)
'            End Select
'        End Sub

'        Public Shared Function GetAddr(ch As Byte, op As Byte) As Integer
'            Return (8 * (ch \ 3)) + (ch Mod 3) + (2 * op)
'        End Function

'        Public Shared Function GetchIdx(addr As Byte) As Integer
'            addr = addr And &H1F
'            Return (3 * (addr \ 8)) + ((addr Mod 8) Mod 3)
'        End Function

'        Public Shared Function GetOpIdx(addr As Byte) As Integer
'            addr = addr And &H1F
'            Return (addr Mod 8) \ 3
'        End Function


'#Region "Events"

'        ' ********************************
'        ' *** Common events

'        Public Event TestChanged(value As Byte)
'        Public Event Timer1Changed(value As Byte)
'        Public Event Timer2Changed(value As Byte)
'        Public Event IRQReset_TimerControlChanged(irqReset As Boolean, timer1Mask As Byte, timer2Mask As Byte, timer1State As Boolean, timer2State As Boolean)
'        Public Event CSM_NoteSelectChanged(csm As Byte, noteSelect As Byte)
'        Public Event AMDepth_VibDepth_RhythmChanged(AMDepth As Byte, VibDepth As Byte, rhythmEnabled As Boolean, bd As Boolean, sd As Boolean, tom As Boolean, tc As Boolean, hh As Boolean)

'        Private Sub RaiseCommonEvent(addr As RegAddr)
'            Select Case addr
'                Case RegAddr.IRO_TimerFlags_Test
'                    RaiseEvent TestChanged(Test)
'                Case RegAddr.Timer1
'                    RaiseEvent Timer1Changed(Timer1)
'                Case RegAddr.Timer2
'                    RaiseEvent Timer2Changed(Timer2)
'                Case RegAddr.IRQReset_TimerControl
'                    RaiseEvent IRQReset_TimerControlChanged(IRQReset, Timer1Mask, Timer2Mask, Timer1State, Timer2State)
'                Case RegAddr.CSM_NoteSelect
'                    RaiseEvent CSM_NoteSelectChanged(CSM, NoteSelect)
'                Case RegAddr.AMDepth_VibDepth_Rhythm
'                    RaiseEvent AMDepth_VibDepth_RhythmChanged(AMDepth, VibDepth, RhythmEn, BD, SD, TOM, TC, HH)
'            End Select
'        End Sub

'        ' ********************************
'        ' *** Channel events

'        Public Event AM_Vib_EGType_KSR_MultiChanged(chIdx As Byte, opIdx As Byte, AM As Byte, Vib As Byte, egType As Byte, KSR As Byte, multi As Byte)
'        Public Event KSL_TLChanged(chIdx As Byte, opIdx As Byte, KSL As Byte, TL As Byte)
'        Public Event AR_DRChanged(chIdx As Byte, opIdx As Byte, AR As Byte, DR As Byte)
'        Public Event SL_RRChanged(chIdx As Byte, opIdx As Byte, SL As Byte, RR As Byte)

'        Private Sub RaiseChannelEvent(addr As RegAddr)
'            Dim ch As Byte = GetchIdx(addr)
'            Dim op As Byte = GetOpIdx(addr)

'            Select Case addr And &HE0
'                Case RegAddr.AM_Vib_EGType_KSR_Multi
'                    RaiseEvent AM_Vib_EGType_KSR_MultiChanged(ch, op, AM(ch, op), Vib(ch, op), EGType(ch, op), KSR(ch, op), Multi(ch, op))
'                Case RegAddr.KSL_TL
'                    RaiseEvent KSL_TLChanged(ch, op, KSL(ch, op), TL(ch, op))
'                Case RegAddr.AR_DR
'                    RaiseEvent AR_DRChanged(ch, op, AR(ch, op), DR(ch, op))
'                Case RegAddr.SL_RR
'                    RaiseEvent SL_RRChanged(ch, op, SL(ch, op), RR(ch, op))
'            End Select

'        End Sub


'        ' ********************************
'        ' *** Key events

'        Public Event FNumLoChanged(chIdx As Integer, LSBs As Byte)
'        Public Event KeyOnBlockFNumHiChanged(chIdx As Integer, keyOn As Boolean, block As Byte, MSBs As Byte)
'        Public Event FB_ConChanged(chIdx As Integer, FB As Byte, Con As Byte)
'        Public Event WaveSelectChanged(chIdx As Integer, type As WaveSelectType)

'        Private Sub RaiseKeyEvent(addr As RegAddr)
'            Dim ch As Byte = addr And &HF

'            Select Case addr And &HF0
'                Case RegAddr.FNumLSBs
'                    RaiseEvent FNumLoChanged(ch, FNumLSBs(ch))
'                Case RegAddr.KeyOn_Block_FNumMSBs
'                    RaiseEvent KeyOnBlockFNumHiChanged(ch, KeyOn(ch), Block(ch), FNumMSBs(ch))
'                Case RegAddr.FB_Con
'                    RaiseEvent FB_ConChanged(ch, FB(ch), Con(ch))
'                Case RegAddr.WaveSelect
'                    RaiseEvent WaveSelectChanged(ch, WaveSelect(ch))
'            End Select

'        End Sub

'#End Region

'    End Class

'    Public Class Channel
'        Public ReadOnly Property Chip As Chip
'        Public ReadOnly Property Num As Integer

'        Sub New(ByRef chip As Chip, num As Integer)
'            _Chip = chip
'            _Num = num
'        End Sub

'        Public ReadOnly Property FNumLSBs As Byte
'        Public ReadOnly Property KeyOn As Boolean
'        Public ReadOnly Property Block As Byte
'        Public ReadOnly Property FNumMSB As Byte

'        Public ReadOnly Property FNum As Integer
'            Get
'                Dim intFNum As Integer =
'                    FNumMSB
'                intFNum <<= 8
'                intFNum += FNumLSBs
'                Return intFNum
'            End Get
'        End Property

'        Public Shared Function GetFNote(fnum As Integer, block As Byte, fsam As Long) As Single
'            Return fnum * (2 ^ (block - 1)) / ((2 ^ 18) / fsam)
'        End Function

'        Public ReadOnly Property TL As Byte

'        Public ReadOnly Property AttenIndB(ch As Byte) As Single
'            Get
'                Return GetAttenIndB(TL)
'            End Get
'        End Property

'        Public Shared Function GetAttenIndB(tl As Byte) As Single
'            Return tl * 0.75
'        End Function

'    End Class

'End Namespace