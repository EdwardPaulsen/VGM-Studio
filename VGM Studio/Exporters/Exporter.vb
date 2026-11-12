Imports System.Threading
Imports VGM_Studio.VGM
Imports VGM_Studio.Midi

Public Class ExportOptions
    Public Property SampleRateScale As Integer = 1
    Public Property HighQualityYM2612 As Boolean = False
End Class

Public MustInherit Class Exporter
    Protected ReadOnly Property VGMFile As VGMFile
    'Public ReadOnly Property CancellationToken As CancellationToken

    Public ReadOnly Property FrameRate As Single
    Public ReadOnly Property CommandList As CommandList
    Public ReadOnly Property DataBank As New List(Of Byte)
    Protected Property DataBankOffset As Long
    Public ReadOnly Property SampleRateScale As Single
    Public Property Options As ExportOptions

    Protected WithEvents SN76489Chip As SN76489.Chip
    Protected WithEvents SN76489Chip2 As SN76489.Chip

    Protected WithEvents YM2413Chip As YM2413.Chip
    Protected WithEvents YM2413Chip2 As YM2413.Chip

    Protected WithEvents YM2612Chip As YM2612.Chip
    Protected WithEvents YM2612Chip2 As YM2612.Chip

    Protected WithEvents YM2151Chip As YM2151.Chip
    Protected WithEvents YM2151Chip2 As YM2151.Chip

    Protected WithEvents YM3526Chip As YM3526.Chip
    Protected WithEvents YM3526Chip2 As YM3526.Chip

    Protected WithEvents YM3812Chip As YM3812.Chip
    Protected WithEvents YM3812Chip2 As YM3812.Chip

    Protected WithEvents SegaPCMChip As SegaPCM.Chip
    Protected WithEvents SegaPCMChip2 As SegaPCM.Chip

    Protected WithEvents YM2203Chip As YM2203.Chip
    Protected WithEvents YM2203Chip2 As YM2203.Chip

    Protected WithEvents YM2149Chip As YM2149.Chip
    Protected WithEvents YM2149Chip2 As YM2149.Chip

    Protected WithEvents RF5C68Chip As RF5C68.Chip
    Protected WithEvents RF5C164Chip As RF5C164.Chip

    Protected WithEvents C140Chip As C140.Chip
    Protected WithEvents C140Chip2 As C140.Chip

    Sub New(ByRef vgmFile As VGMFile, options As ExportOptions) ', ByRef token As CancellationToken)
        MyBase.New()

        _VGMFile = vgmFile

        '_CancellationToken = token

        _FrameRate = vgmFile.Rate
        'If FrameRate = 60 Then _FrameRate = 59.94

        _CommandList = vgmFile.GetCommandList

        _SampleRateScale = options.SampleRateScale

        _Options = options

        InitializeChips()

    End Sub

    Protected Overridable Sub InitializeChips()

        Dim extSampleClockFreq As Integer = VGM_Sample_Rate * SampleRateScale

        If VGMFile.SN76489Clock <> 0 Then
            SN76489Chip = New SN76489.Chip(VGMFile.SN76489Clock, extSampleClockFreq, tap:=VGMFile.SN76489Feedback, width:=VGMFile.SN76489ShiftRegisterWidth, flags:=VGMFile.SN76489Flags)
        End If

        If VGMFile.YM2413Clock <> 0 Then
            YM2413Chip = New YM2413.Chip(VGMFile.YM2413Clock, extSampleClockFreq)
        End If

        If VGMFile.YM3526Clock <> 0 Then
            YM3526Chip = New YM3526.Chip(VGMFile.YM3526Clock, extSampleClockFreq)
        End If

        If VGMFile.YM3812Clock <> 0 Then
            YM3812Chip = New YM3812.Chip(VGMFile.YM3812Clock, extSampleClockFreq)
        End If

        If VGMFile.YM2612Clock <> 0 Then
            YM2612Chip = New YM2612.Chip(VGMFile.YM2612Clock, extSampleClockFreq, Options.HighQualityYM2612)
        End If

        If VGMFile.YM2151Clock <> 0 Then
            YM2151Chip = New YM2151.Chip(VGMFile.YM2151Clock, extSampleClockFreq)
        End If

        If VGMFile.SegaPCMClock <> 0 Then
            SegaPCMChip = New SegaPCM.Chip(VGMFile.SegaPCMClock, extSampleClockFreq, VGMFile.SegaPCMInterface)
        End If

        If VGMFile.YM2203Clock <> 0 Then
            YM2203Chip = New YM2203.Chip(VGMFile.YM2203Clock, extSampleClockFreq)
            YM2149Chip = New YM2149.Chip(VGMFile.YM2203Clock / 2, extSampleClockFreq)
            If VGMFile.IsDualYM2203 Then
                YM2203Chip2 = New YM2203.Chip(VGMFile.YM2203Clock, extSampleClockFreq)
                YM2149Chip2 = New YM2149.Chip(VGMFile.YM2203Clock / 2, extSampleClockFreq)
            End If
        End If

        If VGMFile.RF5C68Clock <> 0 Then
            RF5C68Chip = New RF5C68.Chip(VGMFile.RF5C68Clock, extSampleClockFreq)
        End If

        If VGMFile.RF5C164Clock <> 0 Then
            RF5C164Chip = New RF5C164.Chip(VGMFile.RF5C164Clock, extSampleClockFreq)
        End If

        If VGMFile.C140Clock <> 0 Then
            C140Chip = New C140.Chip(VGMFile.C140Clock, 0)
        End If

    End Sub

    Public Sub Convert(ByRef progress As IProgress(Of Double), token As CancellationToken)
        Start()

        Dim progVal As Integer = 0
        Dim progMax As Integer = CommandList.Count
        Dim progInc As Integer = progMax \ 100
        Dim progPct As Integer = 0

        For cmdIdx As Integer = 0 To CommandList.Count - 1
            'CancellationToken.ThrowIfCancellationRequested()
            If token.IsCancellationRequested Then 'CancellationToken.IsCancellationRequested Then
                Exit For
            End If

            Dim cmd As Command =
                CommandList(cmdIdx)

            Dim cmdType As CommandType =
                cmd.CommandType

            Select Case cmdType
                Case CommandType.GG_Stereo To CommandType.YMF262_Port_1, CommandType.AY8910 To CommandType.C140
                    _Details = ""

                    Select Case cmdType
                        Case CommandType.GG_Stereo
                            ProcessGGStereo(cmd)

                        Case CommandType.PSG
                            ProcessPSG(cmd)

                        Case CommandType.YM2413
                            ProcessYM2413(cmd)

                        Case CommandType.YM2612_Port_0
                            ProcessYM2612(OPN.PortIndex.Port0, cmd)

                        Case CommandType.YM2612_Port_1
                            ProcessYM2612(OPN.PortIndex.Port1, cmd)

                        Case CommandType.YM2151
                            ProcessYM2151(cmd)

                        Case CommandType.SegaPCM
                            ProcessSegaPCM(cmd)

                        Case CommandType.YM2203, CommandType.YM2203_2
                            ProcessYM2203(cmd)

                        Case CommandType.YM3812
                            ProcessYM3812(cmd)

                        Case CommandType.YM3526
                            ProcessYM3526(cmd)

                        Case CommandType.RF5C68_8bit
                            ProcessRF5C68_8bit(cmd)

                        Case CommandType.RF5C164_8bit
                            ProcessRF5C164_8bit(cmd)

                        Case CommandType.C140
                            ProcessC140(cmd)

                        Case Else
                            _Details = "This chip is not supported (yet)."
                    End Select

                    'Update command details with results from processing
                    CommandList(cmdIdx) = New ChipCommand(cmd, Details)

                Case CommandType.Wait_n_Samples, CommandType.Wait_735_Samples, CommandType.Wait_882_Samples, CommandType.Wait_n_Plus_1_Sample To CommandType.Wait_n_Plus_16_Samples, CommandType.Write_from_Data_Bank_and_Wait_0_Samples To CommandType.Write_from_Data_Bank_and_Wait_15_Samples
                    Select Case cmdType
                        Case CommandType.Write_from_Data_Bank_and_Wait_0_Samples To CommandType.Write_from_Data_Bank_and_Wait_15_Samples
                            ProcessWriteFromDataBank()
                    End Select

                    Dim waitCmd As WaitCommand = cmd
                    ProcessWait(waitCmd.SampleCount)

                Case CommandType.Data_Block
                    ProcessDataBlock(cmd)

                Case CommandType.PCM_RAM_Write
                    ProcessPCMRAMWrite(cmd)

                Case CommandType.Seek_to_Data_Offset
                    Dim seekCmd As SeekCommand = cmd
                    ProcessSeekToDataOffset(seekCmd.SeekOffset)

                Case CommandType.End_Of_Sound_Data
                    ProcessEndOfSoundData()

                    progVal = 100
                    progress.Report(progVal)

                    Exit For

                Case Else

            End Select

            progVal += 1

            If (progVal Mod progInc) = 0 Then
                progPct += 1
                progress.Report(progPct / 100)
            End If

        Next

        Finish()

    End Sub

    Protected Overridable Sub Start()
    End Sub

    Protected Overridable Sub Finish()
    End Sub

    Public MustOverride Sub Save(fileName As String)

#Region "VGM Command Processors"

    Protected _Details As String
    Public Property Details As String
        Get
            Return _Details
        End Get
        Protected Set(value As String)
            _Details = value
        End Set
    End Property

    Private Function IsChipAvailable(ByRef chip As Object) As Boolean
        If chip Is Nothing Then
            Details = "ERROR: Chip unsupported or unavailable"
            Return False
        End If
        Return True
    End Function

    Protected Overridable Sub ProcessGGStereo(cmd As ChipCommand)
        Dim value As Byte = cmd.Value(0)

        If IsChipAvailable(SN76489Chip) Then
            SN76489Chip.WriteGGStereo(value)
            Dim pan(3, 1) As Boolean
            For ch As Integer = 0 To 3
                For spk As Integer = 0 To 1
                    pan(ch, spk) = SN76489Chip.LROutput(ch, spk)
                Next
            Next
            Details = $"Pan"
            For ch As Integer = 0 To 3
                Details &= $"{If(pan(ch, 0) = True And pan(ch, 1) = False, " R", "")}{If(pan(ch, 0) = False And pan(ch, 1) = True, " L", "")}{If(pan(ch, 0) = True And pan(ch, 1) = True, " C", "")}{If(pan(ch, 0) = False And pan(ch, 1) = False, " ?", "")}"
            Next
        End If
    End Sub

    Protected Overridable Sub ProcessPSG(cmd As ChipCommand)
        Dim value As Byte = cmd.Value(0)

        If IsChipAvailable(SN76489Chip) Then
            SN76489Chip.Write(value)
        End If
    End Sub

    Protected Overridable Sub ProcessYM2413(cmd As ChipCommand)
        Dim addr As OPLL.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If IsChipAvailable(YM2413Chip) Then
            YM2413Chip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessYM2612(port As OPN.PortIndex, cmd As ChipCommand)
        Dim addr As OPN.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If IsChipAvailable(YM2612Chip) Then
            YM2612Chip.Write(addr, value, port)
        End If

    End Sub

    Protected Overridable Sub ProcessYM2151(cmd As ChipCommand)
        Dim addr As OPM.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If IsChipAvailable(YM2151Chip) Then
            YM2151Chip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessYM3812(cmd As ChipCommand)
        Dim addr As OPL.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If IsChipAvailable(YM3812Chip) Then
            YM3812Chip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessYM3526(cmd As ChipCommand)
        Dim addr As OPL.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        If IsChipAvailable(YM3526Chip) Then
            YM3526Chip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessSegaPCM(cmd As ChipCommand)
        Dim addr As SegaPCM.RegAddr =
            (cmd.Value(1) << 8) + cmd.Value(0)
        Dim value As Integer =
            cmd.Value(2)

        If IsChipAvailable(SegaPCMChip) Then
            SegaPCMChip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessYM2203(cmd As ChipCommand)
        Dim addr As OPN.RegAddr = cmd.Value(0)
        Dim value As Byte = cmd.Value(1)

        Select Case cmd.CommandType
            Case CommandType.YM2203
                If IsChipAvailable(YM2203Chip) Then
                    If addr < &H20 Then
                        YM2149Chip.Write(addr, value)
                    End If
                    YM2203Chip.Write(addr, value)
                End If
            Case CommandType.YM2203_2
                If IsChipAvailable(YM2203Chip2) Then
                    If addr < &H20 Then
                        YM2149Chip2.Write(addr, value)
                    End If
                    YM2203Chip2.Write(addr, value)
                End If
        End Select

    End Sub

    Protected Overridable Sub ProcessRF5C68_8bit(cmd As ChipCommand)
        Dim addr As Byte =
            cmd.Value(0)
        Dim value As Integer =
            cmd.Value(1)

        If IsChipAvailable(RF5C68Chip) Then
            RF5C68Chip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessRF5C164_8bit(cmd As ChipCommand)
        Dim addr As Byte =
            cmd.Value(0)
        Dim value As Integer =
            cmd.Value(1)

        If IsChipAvailable(RF5C164Chip) Then
            RF5C164Chip.Write(addr, value)
        End If
    End Sub

    Protected Overridable Sub ProcessC140(cmd As ChipCommand) 'addr As Integer, value As Byte)
        Dim addr As UShort =
            (cmd.Value(1) << 8) + cmd.Value(0)
        Dim value As Integer =
            cmd.Value(2)

        If IsChipAvailable(C140Chip) Then
            'C140Chip.Write(addr, value)
        End If
    End Sub

    'Added on 2019/01/30: Keep track of current sample
    Public ReadOnly Property CurrentSample As Long

    Protected Overridable Sub ProcessWait(sampleCount As Integer)
        _CurrentSample += sampleCount
    End Sub

    Protected Overridable Sub ProcessEndOfSoundData()
    End Sub

    Protected Overridable Sub ProcessWriteFromDataBank()
        Dim data As Byte =
            DataBank(DataBankOffset)

        'TODO: Fix this so it works with other types of data
        YM2612Chip?.Write(0, OPN.RegAddr.DACData, data)

        DataBankOffset += 1
    End Sub

    Protected Overridable Sub ProcessDataBlock(cmd As DataBlockCommand)
        Select Case cmd.DataType
            Case DataBlockType.YM2612_PCM_Data To DataBlockType.PWM_PCM_Data
                DataBank.AddRange(cmd.Data)
            Case DataBlockType.YM2612_Compressed_PCM_Data To DataBlockType.PWM_Compressed_PCM_Data
                'TODO: Implement this

            Case DataBlockType.SegaPCM_ROM_Data To DataBlockType.QSound_ROM_Data
                Dim rdbCmd As ROMDataBlockCommand = cmd

                'Get the rom size and start address from the command data
                Dim romSize As Integer =
                    rdbCmd.ROMSize
                Dim startAddr As Integer =
                    rdbCmd.WriteAddr
                Dim romData As Byte() =
                    rdbCmd.Data

                Select Case cmd.DataType
                    Case DataBlockType.SegaPCM_ROM_Data
                        SegaPCMChip?.WriteROMData(romData, startAddr)
                End Select

                'Create new data bank and copy current data bank contents into it
                Dim newData(romSize - 1) As Byte
                Array.Copy(DataBank.ToArray, 0, newData, 0, DataBank.Count)
                'Copy rom data into new data bank at start address
                Array.Copy(romData, 0, newData, startAddr, romData.Length)

                'Clear the data bank and add the new data to it
                With DataBank
                    .Clear()
                    .AddRange(newData)
                End With

            Case DataBlockType.RF5C164_RAM_Write To DataBlockType.NESAPU_RAM_Write
                Dim rwCmd As RAMWriteDataBlockCommand = cmd

                Select Case cmd.DataType
                    Case DataBlockType.RF5C68_RAM_Write
                        RF5C68Chip?.WriteToPCMRAM(rwCmd.Data, rwCmd.WriteAddr)
                    Case DataBlockType.RF5C164_RAM_Write
                        RF5C164Chip?.WriteToPCMRAM(rwCmd.Data, rwCmd.WriteAddr)
                    Case DataBlockType.NESAPU_RAM_Write
                        'TODO: Implement this
                End Select

        End Select
    End Sub

    Protected Overridable Sub ProcessPCMRAMWrite(cmd As PCMRAMWriteCommand)
        With cmd
            Dim data(cmd.Length - 1) As Byte
            Array.Copy(DataBank.ToArray, .ReadAddr, data, 0, .Length)
            Select Case cmd.DataType
                Case DataBlockType.RF5C68_PCM_Data
                    RF5C68Chip?.WriteToPCMRAM(data, .WriteAddr)
                    ProcessRF5CPCMRAMWrite(RF5C68Chip, .ReadAddr, .WriteAddr, data)
                Case DataBlockType.RF5C164_PCM_Data
                    RF5C164Chip?.WriteToPCMRAM(data, .WriteAddr)
                    ProcessRF5CPCMRAMWrite(RF5C164Chip, .ReadAddr, .WriteAddr, data)
            End Select
        End With
    End Sub

    Protected Overridable Sub ProcessSeekToDataOffset(seekOffset As Long)
        Static prevSeekOffset As Long = 0

        _DataBankOffset = seekOffset

        prevSeekOffset = seekOffset
    End Sub

#End Region
    Private Function ChIdxPrefix(chIdx As Byte) As String
        Return $"Ch {chIdx + 1}: "
    End Function

    Private Function ChOpIdxPrefix(chIdx As Byte, opIdx As Byte) As String
        Return $"Ch {chIdx + 1} Op {opIdx + 1}: "
    End Function

    'Added on 2019/01/30: Keep track of note lengths
    Protected noteOn(255) As Boolean
    Protected noteOnSample(255) As Long
    Public ReadOnly Property NoteGapLengths As New SortedDictionary(Of Long, Long)

    Protected Sub ProcessNoteOn(chIdx As Integer)
        noteOn(chIdx) = True

        Dim length As Long =
            CurrentSample - noteOnSample(chIdx)

        With NoteGapLengths
            If .ContainsKey(length) Then
                NoteGapLengths(length) += 1
            Else
                .Add(length, 1)
            End If
        End With

        noteOnSample(chIdx) = CurrentSample
    End Sub

    Protected Sub ProcessNoteOff(chIdx As Integer)
        noteOn(chIdx) = False
    End Sub


#Region "PSG (SN76489 and compatibles) Event Handlers"

    Protected Overridable Sub PSG_FNumLoChanged(ByRef ch As PSG.PSGChannel, fnumLo As Byte) Handles SN76489Chip.FNumLoChanged
        With ch
            Dim fNote As Single =
                PSG.GetFNote(.FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fNote)
            Details = $"Ch { .Index + 1}: FNum Lo Bits = 0x{fnumLo:X2}"
        End With
    End Sub

    Protected Overridable Sub PSG_FNumHiChanged(ByRef ch As PSG.PSGChannel, fnumHi As Byte) Handles SN76489Chip.FNumHiChanged
        With ch
            Dim fNote As Single =
                PSG.GetFNote(.FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fNote)
            Details = $"Ch { .Index + 1}: FNum Hi Bits = 0x{fnumHi:X2}"
        End With
    End Sub

    Protected Overridable Sub PSG_FNumChanged(ByRef ch As PSG.PSGChannel) Handles SN76489Chip.FNumChanged
        With ch
            Dim fNote As Single =
                PSG.GetFNote(.FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fNote)
            Details &= $", FNum = { .FNum} ({fNote:0.000} Hz, {note.ToString})"
        End With
    End Sub

    Protected Overridable Sub PSG_ToneAttenChanged(ByRef ch As PSG.PSGChannel) Handles SN76489Chip.ToneAttenChanged
        With ch
            Details = $"Ch { .Index + 1}: Attenuation = { .Atten}"
        End With
    End Sub

    Protected Overridable Sub PSG_NoiseAttenChanged(ByRef ch As PSG.PSGChannel) Handles SN76489Chip.NoiseAttenChanged
        With ch
            Details = $"Noise Attenuation = { .Atten}"
        End With
    End Sub

    Protected Overridable Sub PSG_NoiseTypeChanged(ByRef chip As PSG.Chip) Handles SN76489Chip.NoiseTypeChanged
        With chip.Registers
            Details = $"Noise FB = { .NoiseFB}, Shift Rate = { .NoiseRate}"
        End With
    End Sub

#End Region

#Region "OPLL (YM2413/B, YM2420, YM2423, YM2425, YMF281/B, Konami VRC7) Event Handlers"

    Protected Overridable Sub OPLL_AM_PM_EGType_KSR_MultiChanged(ByRef op As OPLL.OPLLOp) Handles YM2413Chip.AM_PM_EGType_KSR_MultiChanged
        With op
            Details = $"User Instrument Op { .Index}: AM {If(.AM, "ON", "OFF")}, PM {If(.PM, "ON", "OFF")}, { .EGType} EG, KSR = { .KSR}, Multi = { .Multi}"
        End With
    End Sub
    Protected Overridable Sub OPLL_KSL_TLChanged(ByRef op As OPLL.OPLLOp) Handles YM2413Chip.KSL_TLChanged
        With op
            Details = $"User Instrument Op { .Index}: KSL = { .KSL}, TL = { .TL}"
        End With
    End Sub
    Protected Overridable Sub OPLL_KSL_DC_DMChanged(ByRef op As OPLL.OPLLOp) Handles YM2413Chip.KSL_DC_DMChanged
        With op
            Details = $"User Instrument Op { .Index}: KSL = { .KSL}, DC = { .DC}; DM = { .DM}"
        End With
    End Sub
    Protected Overridable Sub OPLL_AR_DRChanged(ByRef op As OPLL.OPLLOp) Handles YM2413Chip.AR_DRChanged
        With op
            Details = $"User Instrument Op { .Index}: AR = { .AR}, DR = { .DR}"
        End With
    End Sub
    Protected Overridable Sub OPLL_SL_RRChanged(ByRef op As OPLL.OPLLOp) Handles YM2413Chip.SL_RRChanged
        With op
            Details = $"User Instrument Op { .Index}: SL = { .SL}, RR = { .RR}"
        End With
    End Sub

    Protected Overridable Sub OPLL_RhythmChanged(ByRef chip As OPLL.Chip) Handles YM2413Chip.RhythmChanged
        With chip.Registers
            Details = $"Rhythm {If(.RhythmEn, "ON", "OFF")}{If(.BD, ", BD", "")}{If(.SD, ", SD", "")}{If(.TOM, ", TOM", "")}{If(.TCYM, ", TCYM", "")}{If(.BD, ", BD", "")}{If(.HH, ", HH", "")}"
        End With
    End Sub

    Protected Overridable Sub OPLL_TESTChanged(ByRef chip As OPLL.Chip) Handles YM2413Chip.TESTChanged
        With chip.Registers
            Details = $"TEST = 0x{ .Test:X2}"
        End With
    End Sub

    Protected Overridable Sub OPLL_FNumLoChanged(ByRef ch As OPLL.OPLLChannel) Handles YM2413Chip.FNumLoChanged
        With ch
            Dim fnote As Single =
                OPLL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fnote)
            Details = $"Ch { .Index + 1}: FNum Lo Bits = { .FNumLo} (FNum = { .FNum}); ({fnote:0.000} Hz, {note.ToString})"
        End With
    End Sub

    Protected Overridable Sub OPLL_SusKeyBlockFNumHiChanged(ByRef ch As OPLL.OPLLChannel) Handles YM2413Chip.SusKeyBlockFNumHiChanged
        With ch
            Dim fnote As Single =
                OPLL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fnote)
            Details = $"Ch { .Index + 1}: Sus {If(.Sus, "ON", "OFF")}; Key {If(.Key, "ON", "OFF")}; Block = { .Block}, FNum Hi Bit = { .FNumHi} (FNum = { .FNum}); ({fnote:0.000} Hz, {note.ToString})"

            'Added on 2019/01/30: Keep track of note lengths
            If .Key Then
                ProcessNoteOn(ch.Index)
            Else
                ProcessNoteOff(ch.Index)
            End If
        End With
    End Sub

    Protected Overridable Sub OPLL_FNumChanged(ByRef ch As OPLL.OPLLChannel) Handles YM2413Chip.FNumChanged
        With ch
            Dim fnote As Single =
                OPLL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fnote)
            Details = $"Ch { .Index + 1}: FNum = { .FNum}; ({fnote:0.000} Hz, {note.ToString})"
        End With
    End Sub

    Protected Overridable Sub OPLL_InstVolChanged(ByRef ch As OPLL.OPLLChannel) Handles YM2413Chip.InstVolChanged
        With ch
            Details = $"Ch { .Index + 1}: Inst = { .Inst}; Vol = { .Vol}"
        End With
    End Sub

    Protected Overridable Sub OPLL_BDVolChanged(ByRef chip As OPLL.Chip) Handles YM2413Chip.BDVolChanged
        With chip.Registers
            Details = $"Bass Drum Vol = { .BDVol}"
        End With
    End Sub

    Protected Overridable Sub OPLL_HHVol_SDVolChanged(ByRef chip As OPLL.Chip) Handles YM2413Chip.HHVol_SDVolChanged
        With chip.Registers
            Details = $"Hi Hat Vol = { .HHVol}; Snare Drum Vol = { .SDVol}"
        End With
    End Sub

    Protected Overridable Sub OPLL_TOMVol_TCYMVolChanged(ByRef chip As OPLL.Chip) Handles YM2413Chip.TOMVol_TCYMVolChanged
        With chip.Registers
            Details = $"Tom Vol = { .TOMVol}; Cymbal Vol = { .TCYMVol}"
        End With
    End Sub

#End Region

#Region "Yamaha OPN YM2203 (OPN), YM2608 (OPNA), YM2010/B (OPNB), YM2612 (OPN2), YM3438 (OPN2C), YMF276 (OPN2L), and YMF288 (OPN3) Event Handlers"

    Protected Overridable Sub OPN_Test1Changed(ByRef chip As OPN.Chip) Handles YM2612Chip.Test21Changed, YM2203Chip.Test21Changed
        With chip.Registers
            Details = $"Test 0x21: Serial Data {If(.Test21SerialDataSelect, " MSB", " LSB")}; {If(.Test21ReadStatusMode, "Serial Data", "Status Flag")} Read Mode; {If(.Test21EGMode, "Special", "Normal")} EG Mode; {If(.Test21InvertDACOutput, "Inverted", "Normal")} DAC Output; Audio Output {If(.Test21MuteAll, "OFF", "ON")}, {If(.Test21TimerTurbo, "Turbo", "Normal")} Timer Speed"
        End With
    End Sub

    Protected Overridable Sub OPN_LFOEnLFOFreqChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.LFOEnLFOFreqChanged, YM2203Chip.LFOEnLFOFreqChanged
        With chip.Registers
            Details = $"LFO {If(.LFOEn, "ON", "OFF")}; Freq = { .LFOFreq}"
        End With
    End Sub

    Protected Overridable Sub OPN_TimerAChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.TimerAChanged, YM2203Chip.TimerAChanged
        With chip.Registers
            Details = $"Timer A = { .TimerA}"
        End With
    End Sub

    Protected Overridable Sub OPN_TimerBChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.TimerBChanged, YM2203Chip.TimerBChanged
        With chip.Registers
            Details = $"Timer B = { .TimerB}"
        End With
    End Sub

    Protected Overridable Sub OPN_Ch3ModeResetEnableLoadChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.Ch3ModeResetEnableLoadChanged, YM2203Chip.Ch3ModeResetEnableLoadChanged
        With chip.Registers
            Details = $"Ch 3 { .Ch3Mode.ToString.ToLower} mode{If(.TimerBReset, "; Reset Timer B", "")}{If(.TimerAReset, "; Reset Timer A", "")}{If(.TimerBEnable, "; Enable Timer B", "")}{If(.TimerAEnable, "; Enable Timer A", "")}{If(.TimerBLoad, "; Load Timer B", "")}{If(.TimerBReset, "; Load Timer A", "")}"
        End With
    End Sub

    'Track current voice
    Protected ThisOPNVoice As OPN.Voice

    Protected Overridable Sub OPN_KeyOnChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.KeyOnChanged, YM2203Chip.KeyOnChanged
        With chip.Registers
            'Reject invalid Ch value
            If .Ch = 3 Then
                Exit Sub
            End If

            Dim chIdx As Integer =
                (.Port * 3) + .Ch

            Details = $"Ch { chIdx + 1}: "

            If .KeyOn = 15 Then
                Details &= "Key ON"
                'Added on 2019/01/30: Keep track of note lengths
                ProcessNoteOn(chIdx)
            ElseIf .KeyOn = 0 Then
                Details &= "Key OFF"
                'Added on 2019/01/30: Keep track of note lengths
                ProcessNoteOff(chIdx)
            Else
                Details &= $"Slot = {System.Convert.ToString(chip.Registers.Ch, 2):0000}"
                'Added on 2019/01/30: Keep track of note lengths
                ProcessNoteOn(chIdx)
            End If

            'Update current OPN voice
            If .KeyOn <> 0 Then
                ThisOPNVoice = New OPN.Voice(CurrentSample, chip, chip.Channel(chIdx))
            End If

        End With

    End Sub

    Protected Overridable Sub OPN_DACChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.DACDataChanged
        With chip.Registers
            Details = $"DAC Data = { .DACData}"
        End With
    End Sub

    Protected Overridable Sub OPN_DACEnChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.DACEnChanged
        With chip.Registers
            Details = $"DAC {If(.DACEn, "ON", "OFF")}"
        End With
    End Sub

    Protected Overridable Sub OPN_Test2CChanged(ByRef chip As OPN.Chip) Handles YM2612Chip.Test2CChanged
        With chip.Registers
            Details = $"Test 2 = { .Test2CBit7},{ .Test2CBit6},{ .Test2CBit5},{ .Test2CBit4},{ .Test2CBit3},{ .Test2CBit2},{ .Test2CBit1}, { .Test2CBit0}"
        End With
    End Sub
    Private Sub YM2203Chip_FMClockDividerChanged(ByRef chip As OPN.Chip) Handles YM2203Chip.FMClockDividerChanged
        Details = $"FM Clock Divider = {chip.FMClockDivider}"
    End Sub

    Protected Overridable Sub OPN_DTMULTIChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.DTMULTIChanged, YM2203Chip.DTMULTIChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: DT = { .DT}; Multi = { .Multi}"
        End With
    End Sub

    Protected Overridable Sub OPN_TLChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.TLChanged, YM2203Chip.TLChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: TL = { .TL}"
        End With
    End Sub

    Protected Overridable Sub OPN_KSARChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.KSARChanged, YM2203Chip.KSARChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: KS = { .KS}; AR = { .AR}"
        End With
    End Sub

    Protected Overridable Sub OPN_AMDRChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.AMDRChanged, YM2203Chip.AMDRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: AM { If(op.AM, "ON", "OFF")}; DR = { op.DR}"
        End With
    End Sub

    Protected Overridable Sub OPN_SRChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.SRChanged, YM2203Chip.SRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: SR = { .SR}"
        End With
    End Sub

    Protected Overridable Sub OPN_SLRRChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.SLRRChanged, YM2203Chip.SLRRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: SL = { .SL}; RR = { .RR}"
        End With
    End Sub

    Protected Overridable Sub OPN_SSG_EGChanged(ByRef op As OPN.OPNOp) Handles YM2612Chip.SSG_EGChanged, YM2203Chip.SSG_EGChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: SSG-EG Shape: {If(.SSG_EG_Cnt, "Cnt", "OFF")}{If(.SSG_EG_Att, ", Att", "")}{If(.SSG_EG_Alt, ", Alt", "")}{If(.SSG_EG_Hld, ", Hld", "")}"
        End With
    End Sub

    Protected Overridable Sub OPN_BlockFNumHiChanged(ByRef ch As OPN.OPNChannel) Handles YM2612Chip.BlockFNumHiChanged, YM2203Chip.BlockFNumHiChanged
        Details = $"Ch {ch.Index + 1}: Block and FNum Hi bits (DELAYED)"
    End Sub

    Protected Overridable Sub OPN_BlockFNumChanged(ByRef ch As OPN.OPNChannel) Handles YM2612Chip.BlockFNumChanged, YM2203Chip.BlockFNumChanged
        With ch
            Dim fNote As Single =
                OPN.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fNote)
            Details = $"Ch { .Index + 1}: Block = { .Block}, FNum = { .FNum} ({fNote:0.000} Hz, {note.ToString})"
        End With
    End Sub

    Protected Overridable Sub OPN_Ch3SpBlockFNumHiChanged(ByRef chip As OPN.Chip, opIdx As Integer) Handles YM2612Chip.Ch3SpBlockFNumHiChanged, YM2203Chip.Ch3SpBlockFNumHiChanged
        Details = $"Ch 3 Op {opIdx + 1}: SP Mode Block and FNum Hi bits (DELAYED)"
    End Sub

    Protected Overridable Sub OPN_Ch3SpBlockFNumChanged(ByRef chip As OPN.Chip, opIdx As Integer) Handles YM2612Chip.Ch3SpBlockFNumChanged, YM2203Chip.Ch3SpBlockFNumChanged
        Dim block As UInteger =
            chip.Registers.Ch3SpBlock(opIdx)
        Dim fnum As UInteger =
            chip.Registers.Ch3SpFNum(opIdx)
        Dim fNote As Single =
            OPN.GetFNote(block, fnum, chip.IntSampleClockFreq)
        Dim note As New Note(fNote)
        Details = $"Ch 3 Op {opIdx + 1}: SP Mode Block = { block}, FNum = { fnum} ({fNote:0.000} Hz, {note.ToString})"
    End Sub

    Protected Overridable Sub OPN_FBConChanged(ByRef ch As OPN.OPNChannel) Handles YM2612Chip.FBConChanged, YM2203Chip.FBConChanged
        With ch
            Details = $"Ch { .Index + 1}: FB = { .FB}; Con = { .Con}"
        End With
    End Sub

    Protected Overridable Sub OPN_LRAMSPMSChanged(ByRef ch As OPN.OPNChannel) Handles YM2612Chip.LRAMSPMSChanged, YM2203Chip.LRAMSPMSChanged
        With ch
            Details = $"Ch { .Index + 1}: Speaker L {If(.L, "ON", "OFF")}, R {If(.R, "ON", "OFF")}; AMS = { .AMS}; PMS = { .PMS}"
        End With
    End Sub

#End Region

#Region "SSG (AY-3-8910, YM2149, and compatibles) Event Handlers"

    Protected ReadOnly SSGChString As String() =
{"A", "B", "C"}

    Protected Overridable Sub SSG_FNumLoChanged(chip As SSG.Chip, chIdx As Integer, fnumLo As UInteger, fnum As UInteger) Handles YM2149Chip.FNumLoChanged
        Dim fnote As Single =
            SSG.GetFNote(fnum, chip.IntSampleClockFreq)
        Dim note As New Note(fnote)
        Details = $"SSG Ch {SSGChString(chIdx)}: FNum Lo = {fnumLo}; FNum = {fnum}; ({fnote:0.000} Hz, {note.ToString})"
    End Sub

    Protected Overridable Sub SSG_FNumHiChanged(chip As SSG.Chip, chIdx As Integer, fnumHi As UInteger, fnum As UInteger) Handles YM2149Chip.FNumHiChanged
        Dim fnote As Single =
            SSG.GetFNote(fnum, chip.IntSampleClockFreq)
        Dim note As New Note(fnote)
        Details = $"SSG Ch {SSGChString(chIdx)}: FNum Hi = {fnumHi}; FNum = {fnum}; ({fnote:0.000} Hz, {note.ToString})"
    End Sub
    Protected Overridable Sub SSG_FNumChanged(chip As SSG.Chip, chIdx As Integer, fnum As UInteger) Handles YM2149Chip.FNumChanged

    End Sub

    Protected Overridable Sub SSG_NoiseFNumChanged(chip As SSG.Chip, fnum As UInteger) Handles YM2149Chip.NoiseFNumChanged
        Dim hz As Single =
            SSG.GetFNote(fnum, chip.IntSampleClockFreq)
        Details = $"SSG Noise FNum = {fnum} ({hz}) Hz"
    End Sub

    Protected Overridable Sub SSG_IONoiseToneCtrlChanged(chip As SSG.Chip, ioEn() As Boolean, noiseEn() As Boolean, toneEn() As Boolean) Handles YM2149Chip.IONoiseToneCtrlChanged
        'TODO: Rewrite this to use loops
        Details = $"SSG Ctrl: I/O A {If(ioEn(0), "ON", "OFF")}; I/O B {If(ioEn(1), "ON", "OFF")}; Noise A {If(noiseEn(0), "ON", "OFF")}; Noise B {If(noiseEn(1), "ON", "OFF")}; Noise C {If(noiseEn(2), "ON", "OFF")}; Tone A {If(toneEn(0), "ON", "OFF")}; Tone B {If(toneEn(1), "ON", "OFF")}; Tone C {If(toneEn(2), "ON", "OFF")}; "
    End Sub

    Protected Overridable Sub SSG_ModeLevelChanged(chip As SSG.Chip, chIdx As Integer, mode As Boolean, level As UInteger) Handles YM2149Chip.ModeLevelChanged
        Details = $"SSG Ch {SSGChString(chIdx)}: Env Mode = {If(mode, "EG", "Fixed")}; Level = {level}"
    End Sub

    Protected Overridable Sub SSG_EnvFNumLoChanged(chip As SSG.Chip, periodLo As UInteger, period As UInteger) Handles YM2149Chip.EnvFNumLoChanged
        Dim hz As Single =
            SSG.GetEnvHz(period, chip.IntSampleClockFreq)
        Details = $"SSG Env FNum Hi = {periodLo}; Period = {period} ({hz} Hz)"
    End Sub

    Protected Overridable Sub SSG_EnvFNumHiChanged(chip As SSG.Chip, periodHi As UInteger, period As UInteger) Handles YM2149Chip.EnvFNumHiChanged
        Dim hz As Single =
            SSG.GetEnvHz(period, chip.IntSampleClockFreq)
        Details = $"SSG Env FNum Hi = {periodHi}; Period = {period} ({hz} Hz)"
    End Sub

    Protected Overridable Sub SSG_EnvShapeChanged(chip As SSG.Chip, cnt As Boolean, att As Boolean, alt As Boolean, hld As Boolean) Handles YM2149Chip.EnvShapeChanged
        Details = $"SSG Env Shape = {If(cnt, "Cnt", "")}{If(att, ", Att", "")}{If(alt, ", Alt", "")}{If(hld, ", Hld", "")}"
    End Sub

    Protected Overridable Sub SSG_IOADataChanged(chip As SSG.Chip, data As UInteger) Handles YM2149Chip.IOADataChanged
        Details = $"SSG I/O A Data = 0x{data:X2}"
    End Sub

    Protected Overridable Sub SSG_IOBDataChanged(chip As SSG.Chip, data As UInteger) Handles YM2149Chip.IOBDataChanged
        Details = $"SSG I/O B Data = 0x{data:X2}"
    End Sub

#End Region

#Region "Yamaha OPM (YM2151) and OPP (YM2164) Event Handlers"
    Protected Overridable Sub OPM_TestChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.TestChanged
        With chip.Registers
            Details = $"Test = 0x{ .Test}:X2"
        End With
    End Sub

    'Track current voice
    Protected ThisOPMVoice As OPM.Voice

    Protected Overridable Sub OPM_KeyOnChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.KeyOnChanged
        With chip.Registers
            Details = $"Ch { .Ch + 1}: "

            If .KeyOn = 15 Then
                Details &= "Key ON"
                'Added on 2019/01/30: Keep track of note lengths
                ProcessNoteOn(.Ch)
            ElseIf .KeyOn = 0 Then
                Details &= "Key OFF"
                'Added on 2019/01/30: Keep track of note lengths
                ProcessNoteOff(.Ch)
            Else
                Details &= $"KeyOn = {System.Convert.ToString(.KeyOn, 2):0000}"
                'Added on 2019/01/30: Keep track of note lengths
                ProcessNoteOn(.Ch)
            End If

            'Update current OPN voice
            If .KeyOn <> 0 Then
                ThisOPMVoice = New OPM.Voice(CurrentSample, chip, chip.Channel(.Ch))
            End If

        End With
    End Sub

    Protected Overridable Sub OPM_NoiseEnFreqChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.NoiseEnFreqChanged
        With chip.Registers
            Details = $"Noise {If(.NoiseEn, "ON", "OFF")}, Freq = { .NoiseFreq}"
        End With
    End Sub

    Protected Overridable Sub OPM_TimerAHihanged(ByRef chip As OPM.Chip) Handles YM2151Chip.TimerAHiChanged
        With chip.Registers
            Details = $"Timer A Hi Bits = { .TimerAHi} (Timer A = { .TimerA})"
        End With
    End Sub

    Protected Overridable Sub OPM_TimerALoChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.TimerALoChanged
        With chip.Registers
            Details = $"Timer A Lo Bits = { .TimerALo} (Timer A = { .TimerA})"
        End With
    End Sub

    Protected Overridable Sub OPM_TimerBChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.TimerBChanged
        With chip.Registers
            Details = $"Timer B = { .TimerB}"
        End With
    End Sub

    Protected Overridable Sub OPM_CSMEnTimerResetEnLoadChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.CSMEnTimerResetEnLoadChanged
        With chip.Registers
            Details = $"CSM {If(.CSMEn, "ON", "OFF")}{If(.TimerBReset, "; Reset Timer B", "")}{If(.TimerAReset, "; Reset Timer A", "")}{If(.TimerBEn, "; Enable Timer B", "")}{If(.TimerAEn, "; Enable Timer A", "")}{If(.TimerBLoad, "; Load Timer B", "")}{If(.TimerALoad, "; Load Timer A", "")}"
        End With
    End Sub

    Protected Overridable Sub OPM_LFOFreqChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.LFOFreqChanged
        With chip.Registers
            Details = $"LFOFreq = { .LFOFreq}"
        End With
    End Sub

    Protected Overridable Sub OPM_PMDAMDChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.PMDAMDChanged
        With chip.Registers
            Details = $"{ .PMDAMDSelect} = {If(.PMDAMDSelect = OPM.PMDAMDSelectValue.PMD, .PMD, .AMD)}"
        End With
    End Sub

    Protected Overridable Sub OPM_CtrlWaveformChanged(ByRef chip As OPM.Chip) Handles YM2151Chip.CtrlWaveformChanged
        With chip.Registers
            Details = $"Control1 = { .Ctrl1}; Control2 = { .Ctrl2}; Waveform = { .Waveform}"
        End With
    End Sub

    Protected Overridable Sub OPM_RLFLConChanged(ByRef ch As OPM.OPMChannel) Handles YM2151Chip.RLFLConChanged
        With ch
            Details = $"Ch { .Index + 1}: Speaker L {If(.L, "ON", "OFF")}, R {If(.R, "ON", "OFF")}; FL = { .FL}; Con = { .Con}"
        End With
    End Sub

    Protected Overridable Sub OPM_KeyCodeChanged(ByRef ch As OPM.OPMChannel) Handles YM2151Chip.KeyCodeChanged
        With ch
            Details = $"Ch { .Index + 1}: Note = { ch.NoteName}{ ch.Octave}"
        End With
    End Sub

    Protected Overridable Sub OPM_KeyFractionChanged(ByRef ch As OPM.OPMChannel) Handles YM2151Chip.KeyFracChanged
        With ch
            Details = $"Ch { .Index + 1}: Fraction = { .KeyFrac} ({(.KeyFrac / 63) * 100:0} cents)"
        End With
    End Sub

    Protected Overridable Sub OPM_PMSAMSChanged(ByRef ch As OPM.OPMChannel) Handles YM2151Chip.PMSAMSChanged
        With ch
            Details = $"Ch { .Index + 1}: PMS = { .PMS}; AMS = { .AMS}"
        End With
    End Sub

    Protected Overridable Sub OPM_DT1MULChanged(ByRef op As OPM.OPMOp) Handles YM2151Chip.DT1MULChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: DT1 = { .DT1}; MUL = { .Mul}"
        End With
    End Sub

    Protected Overridable Sub OPM_TLChanged(ByRef op As OPM.OPMOp) Handles YM2151Chip.TLChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: TL = { .TL}"
        End With
    End Sub

    Protected Overridable Sub OPM_KSARChanged(ByRef op As OPM.OPMOp) Handles YM2151Chip.KSARChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: KS = { .KS}; AR = { .AR}"
        End With
    End Sub

    Protected Overridable Sub OPM_AMSEnableDRChanged(ByRef op As OPM.OPMOp) Handles YM2151Chip.AMSEnDRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: AMS {If(.AMSEn, "ON", "OFF")}; DR = { .DR}"
        End With
    End Sub

    Protected Overridable Sub OPM_DT2SRChanged(ByRef op As OPM.OPMOp) Handles YM2151Chip.DT2SRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: DT2 = { .DT2 }; SR = { .SR}"
        End With
    End Sub

    Protected Overridable Sub OPM_SLRRChanged(ByRef op As OPM.OPMOp) Handles YM2151Chip.SLRRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: SL = { .SL}; RR = { .RR}"
        End With
    End Sub

#End Region

#Region "Yamaha OPL (YM3526 (OPL), YM3812 (OPL2), YMF262 (OPL3), YMF278 (OPL4), and YMF289 (OPL3-L)) Event Handlers"
    Protected Overridable Sub OPL_TestChanged(ByRef chip As OPL.Chip) Handles YM3526Chip.TESTChanged, YM3812Chip.TESTChanged
        With chip.Registers
            Select Case .Chip.ChipType
                Case OPL.ChipType.YM3526
                    Details = $"Test = { .Test:B8}"
                Case OPL.ChipType.YM3812
                    Details = $"Waveform select {If(.WSEn, "ON", "OFF")}; Test = { .Test:B4}"
            End Select
        End With
    End Sub

    Protected Overridable Sub OPL_Timer1Changed(ByRef chip As OPL.Chip) Handles YM3526Chip.Timer1Changed, YM3812Chip.Timer1Changed
        With chip.Registers
            Details = $"Timer 1 = { .Timer1}"
        End With
    End Sub

    Protected Overridable Sub OPL_Timer2Changed(ByRef chip As OPL.Chip) Handles YM3526Chip.Timer2Changed, YM3812Chip.Timer2Changed
        With chip.Registers
            Details = $"Timer 2 = { .Timer2}"
        End With
    End Sub

    Protected Overridable Sub OPL_IRQ_TimerCtrlChanged(ByRef chip As OPL.Chip) Handles YM3526Chip.IRQ_TimerCtrlChanged, YM3812Chip.IRQ_TimerCtrlChanged
        With chip.Registers
            Details = $"IRQ Reset = { .IRQReset}; Timer 1 Mask { .Timer1Mask}; Timer 2 Mask = { .Timer2Mask}; Timer 2 Start = { .Timer2Start}; Timer 1 Start = { .Timer1Start};"
        End With
    End Sub

    Protected Overridable Sub OPL_CSMEn_NoteSelChanged(ByRef chip As OPL.Chip) Handles YM3526Chip.CSMEn_NoteSelChanged, YM3812Chip.CSMEn_NoteSelChanged
        With chip.Registers
            Details = $"CSM = {If(.CSMEn, "ON", "OFF")}; Note Select = { .NoteSel}"
        End With
    End Sub

    Protected Overridable Sub OPL_AMD_PMD_RhythmChanged(ByRef chip As OPL.Chip) Handles YM3526Chip.AMD_PMD_RhythmChanged, YM3812Chip.AMD_PMD_RhythmChanged
        With chip.Registers
            Details = $"AM depth { .AMD}; PM Depth { .PMD}; Rhythm {If(.RhythmEn, "ON", "OFF")}{If(.BD, "; BD", "")}{If(.SD, "; SD", "")}{If(.TOM, "; TOM", "")}{If(.TC, "; TC", "")}{If(.BD, "; BD", "")}{If(.HH, "; HH", "")}"
        End With
    End Sub

    Protected Overridable Sub OPL_AM_PM_EGType_KSR_MultiChanged(ByRef op As OPL.OPLOp) Handles YM3526Chip.AM_PM_EGType_KSR_MultiChanged, YM3812Chip.AM_PM_EGType_KSR_MultiChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: AM {If(.AM, "ON", "OFF")}; PM {If(.PM, "ON", "OFF")}; EG Type = { .EGType}; KSR = { .KSR}; Multi = { .Multi}"
        End With
    End Sub

    Protected Overridable Sub OPL_KSL_TLChanged(ByRef op As OPL.OPLOp) Handles YM3526Chip.KSL_TLChanged, YM3812Chip.KSL_TLChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: KSL = { .KSL}; TL = { .TL}"
        End With
    End Sub

    Protected Overridable Sub OPL_AR_DRChanged(ByRef op As OPL.OPLOp) Handles YM3526Chip.AR_DRChanged, YM3812Chip.AR_DRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: AR ={ .AR}; DR = { .DR}"
        End With
    End Sub

    Protected Overridable Sub OPL_SL_RRChanged(ByRef op As OPL.OPLOp) Handles YM3526Chip.SL_RRChanged, YM3812Chip.SL_RRChanged
        With op
            Details = $"Ch { .Channel.Index + 1} Op { .Index + 1}: SL ={ .SL}; RR = { .RR}"
        End With
    End Sub

    Protected Overridable Sub OPL_FNumLoChanged(ByRef ch As OPL.OPLChannel) Handles YM3526Chip.FNumLoChanged, YM3812Chip.FNumLoChanged
        With ch
            Dim fNote As Single =
                OPL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fNote)
            Details = $"Ch { .Index + 1}: FNum Lo Bits = { .FNumLo}; FNum = { .FNum} ({fNote:0.000} Hz, {note.ToString})"
        End With
    End Sub

    Protected Overridable Sub OPL_KeyOnBlockFNumHiChanged(ByRef ch As OPL.OPLChannel) Handles YM3526Chip.KeyOnBlockFNumHiChanged, YM3812Chip.KeyOnBlockFNumHiChanged
        With ch
            Dim fNote As Single =
                OPL.GetFNote(.Block, .FNum, .Chip.IntSampleClockFreq)
            Dim note As New Note(fNote)
            Details = $"Ch { .Index + 1}: Key {If(.KeyOn, "ON", "OFF")}; Block = { .Block}; FNum Hi Bits = { .FNumHi}; FNum = { .FNum} ({fNote:0.000} Hz, {note.ToString})"

            'Added on 2019/01/30: Keep track of note lengths
            If .KeyOn = True Then
                ProcessNoteOn(.Index)
            Else
                ProcessNoteOff(.Index)
            End If
        End With
    End Sub

    Protected Overridable Sub OPL_FNumChanged(ByRef ch As OPL.OPLChannel) Handles YM3526Chip.FNumChanged, YM3812Chip.FNumChanged

    End Sub

    Protected Overridable Sub OPL_FB_ConChanged(ByRef ch As OPL.OPLChannel) Handles YM3526Chip.FB_ConChanged, YM3812Chip.FB_ConChanged
        With ch
            Details = $"Ch { .Index + 1}: FB = { .FB}, Con = { .Con}"
        End With
    End Sub

    Protected Overridable Sub OPL_WaveformChanged(ByRef ch As OPL.OPLChannel) Handles YM3812Chip.WaveformChanged
        With ch
            Details = $"Ch { .Index + 1}: Waveform = { .Waveform}"
        End With
    End Sub

#End Region

#Region "SegaPCM Event Handlers"
    Protected Overridable Sub SegaPCMChip_VolLChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.VolLChanged
        With ch
            Details = $"Ch { .Index + 1}: Volume L = { .Vol(0)}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_VolRChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.VolRChanged
        With ch
            Details = $"Ch { .Index + 1}: Volume R = { .Vol(1)}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_LoopAddrLoChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.LoopAddrLoChanged
        With ch
            Details = $"Ch { .Index + 1}: Loop Address Lo = 0x{ .LoopAddrHi:X2}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_LoopAddrHiChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.LoopAddrHiChanged
        With ch
            Details = $"Ch { .Index + 1}: Loop Address Hi = 0x{ .LoopAddrHi:X2}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_EndAddrChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.EndAddrChanged
        With ch
            Details = $"Ch { .Index + 1}: End Address = 0x{ .EndAddr:X2}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_AddrIncChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.AddrIncChanged
        With ch
            Details = $"Ch { .Index + 1}: Address Increment = 0x{ .AddrInc:X2} ({(.AddrInc / 255) * 100:0.000}% speed)"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_StartAddrLoChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.StartAddrLoChanged
        With ch
            Details = $"Ch { .Index + 1}: Start Address Lo = 0x{ .StartAddrLo:X2}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_StartAddrHiChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.StartAddrHiChanged
        With ch
            Details = $"Ch { .Index + 1}: Start Address Hi = 0x{ .StartAddrHi:X2}"
        End With
    End Sub

    Protected Overridable Sub SegaPCMChip_MuteLoopOffsetChanged(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.MuteLoopBankChanged
        With ch
            Details = $"Ch { .Index + 1}: {If(.Enabled, "Enabled", "Disabled")}, {If(.Looped, "", "Not ")}Looped, Bank 0x{ .Bank:X2}"

            Const SegaPCM_Sample_Freq As Integer = 31250
            Dim lengthInMillisecs As Integer = 0

            If .AddrInc > 0 Then
                Dim sampleCount As Integer =
                    (.EndAddr - .StartAddr) * (255 / .AddrInc)
                lengthInMillisecs = 1000 * (sampleCount / SegaPCM_Sample_Freq)
            End If

            Details &= $" (StartAddr = 0x{ .Bank + .StartAddr:X5}, LoopAddr = 0x{ .Bank + .LoopAddr:X5}, EndAddr = 0x{ .Bank + .EndAddr:X5}, AddrInc = 0x{ .AddrInc:X2} [{(.AddrInc / 255) * 100:0.000}% speed] [{lengthInMillisecs:0} ms] )"
        End With
    End Sub
    Protected Overridable Sub SegaPCMChip_EndAddrReached(ByRef ch As SegaPCM.Channel) Handles SegaPCMChip.EndAddrReached
        'Dim foo As Integer = 0
    End Sub

#End Region

#Region "Ricoh RFC Series (RF5C68 and RF5C164) (8-bit) Event Handlers"
    Protected Overridable Sub RF5CChip_EnvelopeChanged(ByRef ch As RF5C.Channel) Handles RF5C68Chip.EnvelopeChanged, RF5C164Chip.EnvelopeChanged
        With ch
            Details = $"Ch { .Index + 1} Envelope = { .Envelope}"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_PanChanged(ByRef ch As RF5C.Channel) Handles RF5C68Chip.PanChanged, RF5C164Chip.PanChanged
        With ch
            Details = $"Ch { .Index + 1} Pan: L { .Pan(0)}, R { .Pan(1)}"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_AddressIncrementChanged(ByRef ch As RF5C.Channel) Handles RF5C68Chip.AddressIncrementChanged, RF5C164Chip.AddressIncrementChanged
        With ch
            Dim Hz As Single =
                    RF5C.GetSampleRateInHz(.AddrInc, .Chip.MasterClockFreq)
            Dim pct As Single =
                    Hz / .Chip.IntSampleClockFreq
            Details = $"Ch { .Index + 1} Sample Rate = 0x{ .AddrInc:X4} ({ Hz:F3} Hz) ({pct * 100:F3}% speed)"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_LoopAddressChanged(ByRef ch As RF5C.Channel) Handles RF5C68Chip.LoopAddressChanged, RF5C164Chip.LoopAddressChanged
        With ch
            Details = $"Ch { .Index + 1} Loop Address 0x{ .LoopAddr:X4}"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_StartAddressChanged(ByRef ch As RF5C.Channel) Handles RF5C68Chip.StartAddressChanged, RF5C164Chip.StartAddressChanged
        With ch
            Details = $"Ch { .Index + 1} Start Address 0x{ .StartAddr:X4}"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_ChannelLatchChanged(ByRef chip As RF5C.Chip) Handles RF5C68Chip.ChannelLatchChanged, RF5C164Chip.ChannelLatchChanged
        With chip
            Details = $"Ch { .ChannelLatch + 1} Latched"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_RAMWriteAddressChanged(ByRef chip As RF5C.Chip) Handles RF5C68Chip.RAMWriteAddressChanged, RF5C164Chip.RAMWriteAddressChanged
        With chip
            Details = $"PCM RAM Address 0x{ .RAMWriteAddr:X8}"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_ChipOutputEnabled(ByRef chip As RF5C.Chip) Handles RF5C68Chip.ChipOutputEnabledChanged, RF5C164Chip.ChipOutputEnabledChanged
        With chip
            Details = $"Chip Output {If(.Enabled, "Enabled", "Disabled")}"
        End With
    End Sub
    Protected Overridable Sub RF5CChip_ChannelOutputEnabled(ByRef chip As RF5C.Chip) Handles RF5C68Chip.ChannelOutputEnabledChanged, RF5C164Chip.ChannelOutputEnabledChanged
        Details = $"Channel Output: "
        For chIdx As Integer = 0 To 7
            With chip.Channel(chIdx)
                Details &= $"Ch { .Index + 1} {If(.Enabled, "ON", "OFF")}{If(chIdx < 7, ", ", "")}"
            End With
        Next
    End Sub

    Protected Overridable Sub ProcessRF5CPCMRAMWrite(ByRef chip As RF5C.Chip, readAddr As UInteger, writeAddr As UInteger, data As Byte())
    End Sub

#End Region

End Class