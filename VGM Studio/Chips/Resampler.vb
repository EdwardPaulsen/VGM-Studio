Public Class Resampler

    Shared Sub ApplyFirstOrderIIRLowPassFilter(ByRef samples As Single(,), ByRef prevSample As Single(), alpha As Double)
        For i As Integer = 0 To samples.GetUpperBound(0)
            For j As Integer = 0 To 1
                samples(i, j) = (alpha * samples(i, j)) + ((1 - alpha) * prevSample(j))
                prevSample(j) = samples(i, j)
            Next
        Next
    End Sub

    Shared Sub ApplyFirstOrderIIRLowPassFilter(ByRef samples As Single(,), ByRef prevsample As Single(), samplerate As Double, cutoff As Double)
        Dim alpha As Double =
            GetFirstOrderIIRLowPassFilterAlpha(samplerate, cutoff)
        ApplyFirstOrderIIRLowPassFilter(samples, prevsample, alpha)
    End Sub

    Shared Function GetFirstOrderIIRLowPassFilterAlpha(samplerate As Double, cutoff As Double) As Double
        'Dim dt As Double = 1.0R / samplerate
        'Dim rc As Double = 1.0R / (2.0R * Math.PI * cutoff)
        'Return dt / (rc + dt)

        ' Formula is ɑ = 2πfc / (fs+2πfc) or ɑ = tan(π(fc/fs)) / 1+tan(π(fc/fs)) 
        ' Cutoff must be below the Nyquist frequency (samplerate / 2)
        If cutoff >= samplerate / 2 Then Return 1
        Dim tanVal As Double =
            Math.Tan(Math.PI * cutoff / samplerate)
        Return tanVal / (1 + tanVal)
    End Function

    Shared Sub ApplyInvertedFirstOrderIIRFilter(ByRef samples As Single(,), ByRef prevsample As Single(), alpha As Double)
        For i As Integer = 0 To samples.GetUpperBound(0)
            For j As Integer = 0 To 1
                samples(i, j) = (alpha * prevsample(j)) * ((1 - alpha) * samples(i, j))
                prevsample(j) = samples(i, j)
            Next
        Next
    End Sub
    Shared Sub ApplyInvertedFirstOrderIIRFilter(ByRef samples As Single(,), ByRef prevsample As Single(), samplerate As Double, cutoff As Double)
        Dim alpha As Double =
            GetInvertedFirstOrderIIRFilterAlpha(samplerate, cutoff)
        ApplyInvertedFirstOrderIIRFilter(samples, prevsample, samplerate, cutoff)
    End Sub

    Shared Function GetInvertedFirstOrderIIRFilterAlpha(samplerate As Double, cutoff As Double) As Double
        ' Formula is:
        ' ɑ = 2 - cos(2π(fc/fs)) - √((2-cos(2π(fc/fs))²-1)
        Dim omega_c As Double = 2 * Math.PI * (cutoff / samplerate)
        Dim cos_omega As Double = Math.Cos(omega_c)
        Return 2 - cos_omega - Math.Sqrt(Math.Pow(2 - cos_omega, 2) - 1)
    End Function

    Shared Sub ApplyResample(ByRef input As Single(,), ByRef output As Single(,))
        Dim idxRatio As Double = input.Length / output.Length
        For i As Integer = 0 To output.GetUpperBound(0)
            For j As Integer = 0 To 1
                output(i, j) = input(Math.Floor(i * idxRatio), j)
            Next
        Next
    End Sub

    Shared Sub ApplyCubicResample(ByRef input As Single(,), ByRef output As Single(,))
        Dim idxRatio As Double = input.Length / output.Length
        Dim inPos As Double, inIdx As Integer, t As Integer

        For outIdx As Integer = 0 To output.GetUpperBound(0)
            inPos = outIdx * idxRatio
            inIdx = Math.Floor(inPos)
            t = inPos - inIdx

            For ch As Integer = 0 To 1
                Dim y0 As Double = input(Math.Max(0, inIdx - 1), ch)
                Dim y1 As Double = input(inIdx, ch)
                Dim y2 As Double = input(Math.Min(input.GetUpperBound(0), inIdx + 1), ch)
                Dim y3 As Double = input(Math.Min(input.GetUpperBound(0), inIdx + 2), ch)
                output(outIdx, ch) = CubicResample(y0, y1, y2, y3, t)
            Next
        Next
    End Sub

    Shared Function CubicResample(y0 As Double, y1 As Double, y2 As Double, y3 As Double, t As Double) As Double
        Dim a As Double = -0.5 * y0 + 1.5 * y1 - 1.5 * y2 + 0.5 * y3
        Dim b As Double = y0 - 2.5 * y1 + 2.0 * y2 - 0.5 * y3
        Dim c As Double = -0.5 * y0 + 0.5 * y2
        Dim d As Double = y1
        Return (a * Math.Pow(t, 3)) + (b * Math.Pow(t, 2)) + (c * t) + d
    End Function

    Shared Function CubicHermite6PoleResample(samples As Double(), n As Integer, x As Integer)

    End Function

End Class
