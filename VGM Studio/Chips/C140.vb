'C140 emulator
'Ported by Paul Jensen 2020
'from MAME source code
'https://github.com/mamedev/mame/blob/master/src/devices/sound/c140.h
'https://github.com/mamedev/mame/blob/master/src/devices/sound/c140.cpp

Public Class C140

    Public Enum RegAddr As Integer
        Unknown0 = 0
        Unknown1 = 1
        VolL = 2
        VolR = 3
        LoopAddrLo = 4
        LoopAddrHi = 5
        EndAddr = 6
        AddrInc = 7
        Unknown8 = 8
        Unknown9 = 9
        UnknownA = &HA
        UnknownB = &HB
        CurAddrLo = &HC
        CurAddrHi = &HD
        MuteLoopBank = &HE
    End Enum

    Public Enum BankShiftValue As Integer
        Size256K = 11
        Size512K = 12
        Size12M = 13
    End Enum

    Public Enum BankMaskValue As Integer
        Mask7 = &H70
        MaskF = &HF0
        MaskF8 = &HF8
    End Enum

    Public Class Chip
        Const Channel_Count As Integer = 16
        Public ReadOnly Property Channel As Channel()

        Public ReadOnly Property Mem As Byte()

        Public ReadOnly Property Clock As Integer

        Public ReadOnly Property BankShift As BankShiftValue
        Public ReadOnly Property BankMask As BankMaskValue

        Sub New(spcmClock As Integer, spcmInterface As Integer)
            ReDim Channel(Channel_Count - 1)
            For chIdx As Integer = 0 To Channel.GetUpperBound(0)
                Channel(chIdx) = New Channel(Me, chIdx)
            Next
            ReDim Mem(255)

            _Clock = spcmClock

            _BankShift = spcmInterface And &HF
            _BankMask = (BankMaskValue.Mask7 Or ((spcmInterface >> 16) And &HFC))

        End Sub

        Public Sub Write(addr As Integer, value As Byte)
            Dim addrLSB As Byte = addr And &HFF
            Dim addrMSB As Byte = (addr >> 8) And &HFF

            _Mem(addrLSB) = value

            Dim chIdx As Integer =
                 (addrLSB >> 3) And &HF
            Dim reg As RegAddr =
                 ((addrLSB And &H80) >> 4) + (addrLSB And 7)

            With Channel(chIdx)
                Select Case reg
                    Case RegAddr.VolL
                        .VolL = value And &H7F
                        RaiseEvent VolLChanged(chIdx, .VolL)
                    Case RegAddr.VolR
                        .VolR = value And &H7F
                        RaiseEvent VolRChanged(chIdx, .VolR)
                    Case RegAddr.LoopAddrLo
                        .LoopAddrLo = value
                        RaiseEvent LoopAddrLoChanged(chIdx, .LoopAddrLo)
                    Case RegAddr.LoopAddrHi
                        .LoopAddrHi = value
                        RaiseEvent LoopAddrHiChanged(chIdx, .LoopAddrHi)
                    Case RegAddr.EndAddr
                        .EndAddrHi = value
                        RaiseEvent EndAddrChanged(chIdx, .EndAddrHi)
                    Case RegAddr.AddrInc
                        .AddrInc = value
                        RaiseEvent AddrIncChanged(chIdx, .AddrInc)
                    Case RegAddr.CurAddrLo
                        .CurAddrLo = value
                        RaiseEvent CurAddrLoChanged(chIdx, .CurAddrLo)
                    Case RegAddr.CurAddrHi
                        .CurAddrHi = value
                        RaiseEvent CurAddrHiChanged(chIdx, .CurAddrHi)
                    Case RegAddr.MuteLoopBank
                        .IsMuted = If((value And 1) = 1, True, False)
                        .IsLooped = If(((value >> 1) And 1) = 1, True, False)

                        .Bank = value And BankMask
                        .Bank <<= BankShift

                        RaiseEvent MuteLoopBankChanged(chIdx, .IsMuted, .IsLooped, .Bank)
                    Case Else

                End Select

            End With

        End Sub

        Public Event VolLChanged(chIdx As Integer, vol As Integer)
        Public Event VolRChanged(chIdx As Integer, vol As Integer)
        Public Event LoopAddrLoChanged(chIdx As Integer, addrLo As Integer)
        Public Event LoopAddrHiChanged(chIdx As Integer, addrHi As Integer)
        Public Event EndAddrChanged(chIdx As Integer, addr As Integer)
        Public Event AddrIncChanged(chIdx As Integer, inc As Integer)
        Public Event CurAddrLoChanged(chIdx As Integer, addrLo As Integer)
        Public Event CurAddrHiChanged(chIdx As Integer, addrHi As Integer)
        Public Event MuteLoopBankChanged(chIdx As Integer, isMuted As Boolean, isLooped As Boolean, bank As Integer)

    End Class

    Public Class Channel
        Public ReadOnly Property Chip As Chip
        Public ReadOnly Property Num As Integer

        Public Property VolL As Integer
        Public Property VolR As Integer

        Public Property LoopAddrLo As Integer
        Public Property LoopAddrHi As Integer

        Public Property EndAddrHi As Integer

        Public ReadOnly Property EndAddr As Integer
            Get
                Return Bank + (EndAddrHi << 8)
                'Return (Bank << 16) + (EndAddrHi << 8)
            End Get
        End Property

        Public Property CurAddrLo As Integer
        Public Property CurAddrHi As Integer

        Private Property CurAddrLoInternal

        Public ReadOnly Property CurAddr As Integer
            Get
                Return Bank + (CurAddrHi << 8) + CurAddrLo
            End Get
        End Property

        Public Property AddrInc As Integer

        Public Property IsMuted As Boolean
        Public Property IsLooped As Boolean

        Public Property Bank As Integer

        Public ReadOnly Property LoopAddr As Integer
            Get
                Return Bank + (LoopAddrHi << 8) + LoopAddrLo
            End Get
        End Property

        Sub New(ByRef chip As Chip, num As Integer)
            _Chip = chip
            _Num = num
        End Sub

    End Class

    Public Class Voice

        Public ReadOnly Property StartAddr As Integer
        Public ReadOnly Property EndAddr As Integer
        Public ReadOnly Property LoopAddr As Integer

        Public ReadOnly Property Length As Integer
            Get
                Return EndAddr - StartAddr
            End Get
        End Property

        Public ReadOnly Property LoopLength As Integer
            Get
                Return EndAddr - LoopAddr
            End Get
        End Property

        Public Property IsLooped As Boolean = False

        'Public Property Channels As New SortedSet(Of Integer)
        'Public Property Speeds As New SortedSet(Of Integer)

        Sub New(startAddr As Integer, endAddr As Integer, loopAddr As Integer)
            _StartAddr = startAddr
            _EndAddr = endAddr
            _LoopAddr = loopAddr
        End Sub

        Sub New(ByRef ch As Channel)
            Me.New(ch.CurAddr, ch.EndAddr, ch.LoopAddr)
        End Sub

        Public Function IsSameAs(voice As Voice) As Boolean

            With voice
                If (.StartAddr = StartAddr) _
                    And (.EndAddr = EndAddr) _
                    And (.LoopAddr = LoopAddr) Then
                    Return True
                Else
                    Return False
                End If
            End With

        End Function

        Public Overrides Function ToString() As String
            Dim sb As New Text.StringBuilder
            Dim sep As Char = ","
            With sb
                .AppendLine($"Start Addr{sep}End Addr{sep}Loop Addr")
                .AppendLine($"{StartAddr}{sep}{EndAddr}{sep}{LoopAddr}")
            End With
            Return sb.ToString
        End Function

    End Class


End Class


