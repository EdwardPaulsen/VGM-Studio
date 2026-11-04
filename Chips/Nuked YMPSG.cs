namespace Nuked
{
    /* Copyright (C) 2021 Nuke.YKT
     * License: GPLv2+
     * 
     * Nuked PSG (YM7101) emulator.
     * Version 1.0.1
     *
     * C# Conversion and modifications Copyright (C) 2026 Paul Jensen
     * 
     * Modifications:
     * - Added YMPSG_WriteBuffered, YMPSG_GenerateStream, and YMPSG_GeenerateResampled functions (based on Nuked OPN2) to increase ease of use.
     * - Added function YMPSG_SetFilter to support custom low pass filter cutoff frequencies.
     * 
     */

    public class YMPSG
    {
        public struct WriteBuf
        {
            public ulong time;
            public bool stat;
            public byte data;
        }

        public struct ChipState
        {
            // IO
            public byte data;
            public byte latch;
            public bool write_flag;
            public bool write_flag_l;

            public byte prescaler_1;
            public byte prescaler_2;
            public byte prescaler_2_l;
            public byte reset_latch;
            public byte ic;
            public byte ic_latch1;
            public byte ic_latch2;
            public byte data_mask;
            public bool reg_reset;
            public byte[] volume;
            public ushort[] freq;
            public byte noise_data;
            public bool noise_of;
            public bool noise_trig;
            public bool noise_trig_l;
            public byte rot;

            public byte chan_sel;

            public ushort[] counter;
            public byte counter_of;
            public byte sign;
            public byte sign_l;
            public byte noise_sign_l;
            public ushort noise;
            public byte test;
            public byte[] volume_out;
            public byte mute;


            // For buffered writes
            public ulong writebuf_samplecnt;
            public uint writebuf_cur;
            public uint writebuf_last;
            public ulong writebuf_lasttime;
            public readonly ulong WRITEBUF_SIZE = 2048;
            public readonly ulong WRITEBUF_DELAY = 8;
            public WriteBuf[] writebuf;


            // For resampling
            public readonly int RSM_FRAC = 10;
            public uint rate, clock;
            public uint rateratio;
            public int samplecnt;
            public int oldsample;
            public int sample;
            public double FILTER_CUTOFF, FILTER_CUTOFF_I;

            public ChipState()
            {
                volume = new byte[4];
                freq = new ushort[3];

                counter = new ushort[4];
                volume_out = new byte[4];

                writebuf = new WriteBuf[WRITEBUF_SIZE];

                FILTER_CUTOFF = 0.9092492456124146; // 3390 Hz single pole IIR low pass (NTSC)
                FILTER_CUTOFF_I = (1 - FILTER_CUTOFF);
            }

        }

        public static readonly float[] YMPSGVol = [
            1.0F, 0.772F, 0.622F, 0.485F, 0.382F, 0.29F, 0.229F, 0.174F, 0.132F, 0.096F, 0.072F, 0.051F, 0.034F, 0.019F, 0.009F, 0.0F, -1.059F
        ];

        private static readonly bool use_filter = true;

        /// <summary>
        /// Initializes the chip.
        /// </summary>
        /// <param name="chip">The chip to initialize.</param>
        public static void Init(ref ChipState chip, uint rate, uint clock)
        {
            uint i;
            uint rateratio = chip.rateratio;

            SetIC(ref chip, 1);

            for (i = 0; i < 16; i++)
            {
                Clock(ref chip);
            }
            SetIC(ref chip, 0);

            if (rate != 0 && clock != 0)
            {
                chip.rate = rate;
                chip.clock = clock;
                chip.rateratio = (uint)((((ulong)16 * rate) << chip.RSM_FRAC) / clock);
            }
            else
            {
                chip.rateratio = rateratio;
            }

        }

        /// <summary>
        /// Sets the IC value for the chip.
        /// </summary>
        /// <param name="chip">The chip to set the value for.</param>
        /// <param name="ic">The IC value.</param>
        static void SetIC(ref ChipState chip, uint ic)
        {
            chip.ic = (byte)ic;
        }

        /// <summary>
        /// Sets the value for the low pass filter.
        /// </summary>
        /// <param name="chip">The chip to set the value for.</param>
        /// <param name="cutoffFreq">The frequency at which to begin attenuation.</param>
        public static void SetFilter(ref ChipState chip, float cutoffFreq)
        {
            // Equation is:
            // ɑ = 2 - cos(2π*(fc/fs)) - √((2-cos(2π*(fc/fs))² - 1)
            double omega_c = 2 * Math.PI * (cutoffFreq / (chip.clock / 16));
            double cos_omega = Math.Cos(omega_c);
            chip.FILTER_CUTOFF = 2 - cos_omega - Math.Sqrt(Math.Pow(2 - cos_omega, 2) - 1);
            chip.FILTER_CUTOFF_I = 1 - chip.FILTER_CUTOFF;
        }

        /// <summary>
        /// Writes a single data byte to a buffer.
        /// </summary>
        /// <param name="chip">The chip to write to.</param>
        /// <param name="data">The data to write.</param>
        /// <remarks>Use Write to make an immediate write.</remarks>
        public static void WriteBuffered(ref ChipState chip, byte data)
        {
            ulong time1, time2;
            ulong skip;

            if (chip.writebuf[chip.writebuf_last].stat)
            {
                Write(ref chip, chip.writebuf[chip.writebuf_last].data);

                chip.writebuf_cur = (uint)((chip.writebuf_last + 1) % chip.WRITEBUF_SIZE);
                skip = chip.writebuf[chip.writebuf_last].time - chip.writebuf_samplecnt;
                chip.writebuf_samplecnt = chip.writebuf[chip.writebuf_last].time;
                while (skip-- > 0)
                {
                    Clock(ref chip);
                }
            }

            chip.writebuf[chip.writebuf_last].stat = true;
            chip.writebuf[chip.writebuf_last].data = data;
            time1 = chip.writebuf_lasttime + chip.WRITEBUF_DELAY;
            time2 = chip.writebuf_samplecnt;

            if (time1 < time2)
            {
                time1 = time2;
            }

            chip.writebuf[chip.writebuf_last].time = time1;
            chip.writebuf_lasttime = time1;
            chip.writebuf_last = (uint)((chip.writebuf_last + 1) % chip.WRITEBUF_SIZE);
        }

        /// <summary>
        /// Writes a single data byte immediately to the chip.
        /// </summary>
        /// <param name="chip">The chip to write to.</param>
        /// <param name="data">The data to write.</param>
        /// <remarks>Use WriteBuffered to make a buffered write.</remarks>
        static void Write(ref ChipState chip, byte data)
        {
            chip.data = data;
            chip.write_flag = true;
        }

        /// <summary>
        /// Reads the current raw sample data from the chip.
        /// </summary>
        /// <param name="chip">The chip to read from</param>
        /// <returns>A raw sample byte</returns>
        /// <remarks>This routine does not advance the clock.</remarks>
        public static ushort Read(ref ChipState chip)
        {
            ushort data = 0;
            uint i;

            UpdateSample(ref chip);

            for (i = 0; i < 4; i++)
            {
                data |= (ushort)(chip.volume_out[i] << ((3 - (int)i) * 4));
            }
            return data;
        }

        /// <summary>
        /// Runs the chip for one clock cycle.
        /// </summary>
        /// <param name="chip">The chip to run.</param>
        public static void Clock(ref ChipState chip)
        {
            byte prescaler2_latch;
            prescaler2_latch = chip.prescaler_2;
            chip.prescaler_2 += chip.prescaler_1;
            chip.prescaler_2 &= 1;
            chip.prescaler_1 ^= 1;

            if ((chip.ic_latch1 & 3) == 2)
            {
                chip.prescaler_1 = 0;
                chip.prescaler_2 = 0;
            }
            chip.ic_latch1 <<= 1;
            chip.ic_latch1 |= (byte)(chip.ic & 1);

            UpdateRegisters(ref chip);

            chip.write_flag_l = false;

            if (chip.write_flag)
            {
                WriteLatch(ref chip);
                chip.write_flag = false;
                chip.write_flag_l = true;
            }
            if ((chip.prescaler_1) != 0)
            {
                if (prescaler2_latch == 0)
                {
                    ClockInternal1(ref chip);
                }
                else
                {
                    ClockInternal2(ref chip);
                }
            }
        }

        static void UpdateRegisters(ref ChipState chip)
        {
            byte data = chip.data;
            if (chip.data_mask != 0)
            {
                data = 0;
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 1))
            {
                chip.volume[0] = (byte)(data & 15);
                if (chip.data_mask != 0)
                {
                    chip.volume[0] = 15;
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 3))
            {
                chip.volume[1] = (byte)(data & 15);
                if (chip.data_mask != 0)
                {
                    chip.volume[1] = 15;
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 4))
            {
                if (((data & 128) != 0) || chip.reg_reset)
                {
                    chip.freq[2] &= 1008;
                    chip.freq[2] |= (ushort)(data & 15);
                }
                if ((data & 128) == 0)
                {
                    chip.freq[2] &= 15;
                    chip.freq[2] |= (ushort)((data << 4) & 1008);
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 2))
            {
                if (((data & 128) != 0) || chip.reg_reset)
                {
                    chip.freq[1] &= 1008;
                    chip.freq[1] |= (ushort)(data & 15);
                }
                if ((data & 128) == 0)
                {
                    chip.freq[1] &= 15;
                    chip.freq[1] |= (ushort)((data << 4) & 1008);
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 5))
            {
                chip.volume[2] = (byte)(data & 15);
                if (chip.data_mask != 0)
                {
                    chip.volume[2] = 15;
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 0))
            {
                if (((data & 128) != 0) || chip.reg_reset)
                {
                    chip.freq[0] &= 1008;
                    chip.freq[0] |= (ushort)(data & 15);
                }
                if ((data & 128) == 0)
                {
                    chip.freq[0] &= 15;
                    chip.freq[0] |= (ushort)((data << 4) & 1008);
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 7))
            {
                chip.volume[3] = (byte)(data & 15);
                if (chip.data_mask != 0)
                {
                    chip.volume[3] = 15;
                }
            }
            if (chip.reg_reset || (chip.write_flag_l && chip.latch == 6))
            {
                chip.noise_data = (byte)(data & 7);
                chip.noise_trig = true;
            }
        }

        static void WriteLatch(ref ChipState chip)
        {
            byte data = chip.data;
            if (chip.data_mask != 0)
            {
                data = 0;
            }
            if ((data & 128) != 0)
            {
                chip.latch = (byte)((data >> 4) & 7);
            }
        }

        static void ClockInternal1(ref ChipState chip)
        {
            ushort freq = 0;
            byte chan_sel = chip.chan_sel;
            bool noise_of;
            byte noise_bit1, noise_bit2, noise_next;

            if ((chip.noise_data & 3) == 3)
            {
                noise_of = ((chip.sign >> 1) & 1) == 1;
            }
            else
            {
                noise_of = ((chip.sign & 1) == 1);
            }
            if (chip.noise_trig_l || ((chip.ic_latch2 & 1) == 1))
            {
                chip.noise = 0;
            }
            else if (noise_of && !chip.noise_of)
            {
                noise_bit1 = (byte)((chip.noise >> 15) & 1);
                noise_bit2 = (byte)((chip.noise >> 12) & 1);
                noise_bit1 ^= noise_bit2;
                noise_next = (byte)((((noise_bit1 != 0) && (((chip.noise_data >> 2) & 1) == 1)) || ((chip.noise & 32767) == 0)) ? 1 : 0);
                chip.noise <<= 1;
                chip.noise |= noise_next;
            }
            chip.noise_of = noise_of;
            if ((chip.ic_latch2 & 2) != 0)
            {
                chan_sel = 0;
            }
            if ((chan_sel & 1) != 0)
            {
                freq |= chip.freq[0];
            }
            if ((chan_sel & 2) != 0)
            {
                freq |= chip.freq[1];
            }
            if ((chan_sel & 4) != 0)
            {
                freq |= chip.freq[2];
            }
            if ((chan_sel & 8) != 0)
            {
                if ((chip.noise_data & 3) == 0)
                {
                    freq |= 16;
                }
                if ((chip.noise_data & 3) == 1)
                {
                    freq |= 32;
                }
                if ((chip.noise_data & 3) == 2)
                {
                    freq |= 64;
                }
            }
            if ((chip.chan_sel & 1) != 0)
            {
                chip.sign ^= (byte)(chip.counter_of & 15);
            }
            if ((chip.ic_latch2 & 2) != 0)
            {
                chip.sign = 0;
            }
            chip.counter_of <<= 1;
            if (chip.counter[chip.rot] >= freq)
            {
                chip.counter_of |= 1;
                chip.counter[chip.rot] = 0;
            }
            if ((chip.ic_latch2 & 2) != 0)
            {
                chip.counter[chip.rot] = 0;
            }
            chip.counter[chip.rot]++;
            chip.counter[chip.rot] &= 1023;
            if (((chip.ic_latch2 & 1) == 1) || (chip.chan_sel & 7) != 0)
            {
                chip.chan_sel <<= 1;
            }
            else
            {
                chip.chan_sel <<= 1;
                chip.chan_sel |= 1;
            }
            chip.ic_latch2 <<= 1;
            chip.ic_latch2 |= (byte)(chip.ic & 1);
            chip.noise_trig_l = chip.noise_trig;
        }

        static void ClockInternal2(ref ChipState chip)
        {
            chip.data_mask = (byte)((chip.ic_latch2 >> 1) & 1);
            chip.reg_reset = ((chip.ic_latch2 >> 0) & 1) == 1;
            if (chip.noise_trig_l)
            {
                chip.noise_trig = false;
            }
            UpdateRegisters(ref chip);

            chip.rot = (byte)((chip.rot + 1) & 3);
            chip.sign_l = chip.sign;
            chip.noise_sign_l = (byte)((chip.noise >> 14) & 1);
        }

        static void UpdateSample(ref ChipState chip)
        {
            uint i;
            byte sign = (byte)(chip.sign & 14);
            sign |= chip.noise_sign_l;

            if ((chip.test & 1) != 0)
            {
                sign |= 15;
            }

            for (i = 0; i < 4; i++)
            {
                if (((sign >> (3 - (int)i)) & 1) != 0)
                {
                    chip.volume_out[i] = chip.volume[i];
                }
                else
                {
                    chip.volume_out[i] = 15;
                }
            }
        }

        /// <summary>
        /// Gets the current audio output of the chip.
        /// </summary>
        /// <param name="chip">The chip to get output from.</param>
        /// <returns>A 32-bit floating point sample.</returns>
        /// <remarks>This routine does not advance the clock.</remarks>
        public static float GetOutput(ref ChipState chip)
        {
            float sample = 0.0F;
            uint i;

            UpdateSample(ref chip);

            if ((chip.test & 1) != 0)
            {
                sample += YMPSGVol[chip.volume_out[chip.test >> 1]];
                sample += YMPSGVol[16] * 3.0F;
            }
            else if (chip.mute == 0)
            {
                for (i = 0; i < 4; i++)
                {
                    sample += YMPSGVol[chip.volume_out[i]];
                }
            }
            else
            {
                for (i = 0; i < 4; i++)
                {
                    if (((chip.mute >> (int)i) & 1) == 0)
                        sample += YMPSGVol[chip.volume_out[i]];
                }
            }
            return sample;
        }

        /// <summary>
        /// Generates a stream of signed 32-bit samples from the chip.
        /// </summary>
        /// <param name="chip">The chip to generate samples from</param>
        /// <param name="samples">A 2-dimensional array for holding samples. The first dimension holds the samples, and the second dimension specifies the channel (0 = left and 1 = right).</param>
        /// <param name="numsamples">The number of samples to generate.</param>
        /// <remarks>This routine advances the clock automatically.</remarks>
        public static void GenerateStream(ref ChipState chip, ref int[] samples, uint numsamples)
        {
            uint i, j;
            int buffer = 0;

            for (i = 0; i < numsamples; i++)
            {
                GenerateResampled(ref chip, ref buffer);
                samples[i] = buffer;
            }
        }

        protected static void GenerateResampled(ref ChipState chip, ref int buf)
        {
            uint i;
            float out0;

            while (chip.samplecnt >= chip.rateratio)
            {
                chip.oldsample = chip.sample;
                chip.sample = 0;

                for (i = 0; i < 16; i++)
                {
                    Clock(ref chip);

                    while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
                    {
                        if (!chip.writebuf[chip.writebuf_cur].stat)
                        {
                            break;
                        }
                        chip.writebuf[chip.writebuf_cur].stat = false;

                        Write(ref chip, chip.writebuf[chip.writebuf_cur].data);

                        chip.writebuf_cur = (uint)((chip.writebuf_cur + 1) % chip.WRITEBUF_SIZE);
                    }
                    chip.writebuf_samplecnt++;
                }
                
                out0 = GetOutput(ref chip);

                buf = (int)(out0 * 8192.0F);

                chip.sample = buf;

                // Low pass filter
                if (use_filter)
                {
                    chip.sample = (int)(chip.oldsample + chip.FILTER_CUTOFF_I * (chip.sample - chip.oldsample));
                }

                chip.samplecnt -= (int)chip.rateratio;
            }

            // Resample
            buf = (int)((chip.oldsample * (chip.rateratio - chip.samplecnt) + chip.sample * chip.samplecnt) / chip.rateratio);

            chip.samplecnt += 1 << chip.RSM_FRAC;
        }

        /// <summary>
        /// Generates the next 32-bit signed sample from the chip.
        /// </summary>
        /// <param name="chip">The chip to get the sample from.</param>
        /// <param name="buf">A 32-bit signed buffer for the sample.</param>
        /// <remarks>This routine advances the clock by 16 cycles.</remarks>
        protected static void Generate(ref ChipState chip, ref int buf)
        {
            uint i;
            float out0;

            for (i = 0; i < 16; i++)
            {
                Clock(ref chip);

                while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
                {
                    if (!chip.writebuf[chip.writebuf_cur].stat)
                    {
                        break;
                    }
                    chip.writebuf[chip.writebuf_cur].stat = false;

                    Write(ref chip, chip.writebuf[chip.writebuf_cur].data);

                    chip.writebuf_cur = (uint)((chip.writebuf_cur + 1) % chip.WRITEBUF_SIZE);
                }
                chip.writebuf_samplecnt++;
            }
            out0 = GetOutput(ref chip);
            buf = (int)(out0 * 8192.0F);
        }

        /// <summary>
        /// Sets a mask for muting individual channels.
        /// </summary>
        /// <param name="chip">The chip to set the mask for.</param>
        /// <param name="mute">A bitmask for muting channels.</param>
        /// <remarks>A value of 0x0 enables all channels, and 0xF mutes all channels.</remarks>
        public static void SetMute(ref ChipState chip, byte mute)
        {
            chip.mute = mute;
        }

        /// <summary>
        /// Sets a test value from the chip.
        /// </summary>
        /// <param name="chip">The chip to set the value for.</param>
        /// <param name="test">The test value.</param>
        public static void Test(ref ChipState chip, ushort test)
        {
            chip.test = (byte)((test >> 9) & 7);
        }

    }

    public class YM7101
    {


    }

}
