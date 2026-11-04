namespace MAME
{
    // license:BSD-3-Clause
    // copyright-holders:Nicola Salmoria
    /***************************************************************************

      sn76496.cpp
      by Nicola Salmoria
      with contributions by others

      Routines to emulate the:
      Texas Instruments SN76489, SN76489A, SN76494/SN76496
      ( Also known as, or at least compatible with, the TMS9919 and SN94624.)
      and the Sega 'PSG' used on the Master System, Game Gear, and Megadrive/Genesis
      This chip is known as the Programmable Sound Generator, or PSG, and is a 4
      channel sound generator, with three squarewave channels and a noise/arbitrary
      duty cycle channel.

      Noise emulation for all verified chips should be accurate:

      ** SN76489 uses a 15-bit shift register with taps on bits D and E, output on E,
      XOR function.
      It uses a 15-bit ring buffer for periodic noise/arbitrary duty cycle.
      Its output is inverted.
      ** SN94624 is the same as SN76489 but lacks the /8 divider on its clock input.
      ** SN76489A uses a 15-bit shift register with taps on bits D and E, output on F,
      XOR function.
      It uses a 15-bit ring buffer for periodic noise/arbitrary duty cycle.
      Its output is not inverted.
      ** SN76494 is the same as SN76489A but lacks the /8 divider on its clock input.
      ** SN76496 is identical in operation to the SN76489A, but the audio input on pin 9 is
      documented.
      All the TI-made PSG chips have an audio input line which is mixed with the 4 channels
      of output. (It is undocumented and may not function properly on the sn76489, 76489a
      and 76494; the sn76489a input is mentioned in datasheets for the tms5200)
      All the TI-made PSG chips act as if the frequency was set to 0x400 if 0 is
      written to the frequency register.
      ** Sega Master System III/MD/Genesis PSG uses a 16-bit shift register with taps
      on bits C and F, output on F
      It uses a 16-bit ring buffer for periodic noise/arbitrary duty cycle.
      (whether it uses an XOR or XNOR needs to be verified, assumed XOR)
      (whether output is inverted or not needs to be verified, assumed to be inverted)
      ** Sega Game Gear PSG is identical to the SMS3/MD/Genesis one except it has an
      extra register for mapping which channels go to which speaker.
      The register, connected to a z80 port, means:
      for bits 7  6  5  4  3  2  1  0
               L3 L2 L1 L0 R3 R2 R1 R0
      Noise is an XOR function, and audio output is negated before being output.
      All the Sega-made PSG chips act as if the frequency was set to 0 if 0 is written
      to the frequency register.
      ** NCR8496 (as used on the Tandy 1000TX) is similar to the SN76489 but with a
      different noise LFSR pattern: taps on bits A and E, output on E, XNOR function
      It uses a 15-bit ring buffer for periodic noise/arbitrary duty cycle.
      Its output is inverted.
      ** PSSJ-3 (as used on the later Tandy 1000 series computers) is the same as the
      NCR8496 with the exception that its output is not inverted.

      28/03/2005 : Sebastien Chevalier
      Update th SN76496Write func, according to SN76489 doc found on SMSPower.
       - On write with 0x80 set to 0, when LastRegister is other then TONE,
       the function is similar than update with 0x80 set to 1

      23/04/2007 : Lord Nightmare
      Major update, implement all three different noise generation algorithms and a
      set_variant call to discern among them.

      28/04/2009 : Lord Nightmare
      Add READY line readback; cleaned up struct a bit. Cleaned up comments.
      Add more TODOs. Fixed some unsaved savestate related stuff.

      04/11/2009 : Lord Nightmare
      Changed the way that the invert works (it now selects between XOR and XNOR
      for the taps), and added R->OldNoise to simulate the extra 0 that is always
      output before the noise LFSR contents are after an LFSR reset.
      This fixes SN76489/A to match chips. Added SN94624.

      14/11/2009 : Lord Nightmare
      Removed STEP mess, vastly simplifying the code. Made output bipolar rather
      than always above the 0 line, but disabled that code due to pending issues.

      16/11/2009 : Lord Nightmare
      Fix screeching in regulus: When summing together four equal channels, the
      size of the max amplitude per channel should be 1/4 of the max range, not
      1/3. Added NCR8496.

      18/11/2009 : Lord Nightmare
      Modify Init functions to support negating the audio output. The gamegear
      psg does this. Change gamegear and sega psgs to use XOR rather than XNOR
      based on testing. Got rid of R->OldNoise and fixed taps accordingly.
      Added stereo support for game gear.

      15/01/2010 : Lord Nightmare
      Fix an issue with SN76489 and SN76489A having the wrong periodic noise periods.
      Note that properly emulating the noise cycle bit timing accurately may require
      extensive rewriting.

      24/01/2010: Lord Nightmare
      Implement periodic noise as forcing one of the XNOR or XOR taps to 1 or 0 respectively.
      Thanks to PlgDavid for providing samples which helped immensely here.
      Added true clock divider emulation, so sn94624 and sn76494 run 8x faster than
      the others, as in real life.

      15/02/2010: Lord Nightmare & Michael Zapf (additional testing by PlgDavid)
      Fix noise period when set to mirror channel 3 and channel 3 period is set to 0
      (tested on hardware for noise, wave needs tests) - MZ
      Fix phase of noise on sn94624 and sn76489; all chips use a standard XOR, the only
      inversion is the output itself - LN, Plgdavid
      Thanks to PlgDavid and Michael Zapf for providing samples which helped immensely here.

      23/02/2011: Lord Nightmare & Enik
      Made it so the Sega PSG chips have a frequency of 0 if 0 is written to the
      frequency register, while the others have 0x400 as before. Should fix a bug
      or two on sega games, particularly Vigilante on Sega Master System. Verified
      on SMS hardware.

      27/06/2012: Michael Zapf
      Converted to modern device, legacy devices were gradually removed afterwards.

      16/09/2015: Lord Nightmare
      Fix PSG chips to have volume reg inited on reset to 0x0 based on tests by
      ValleyBell. Made Sega PSG chips start up with register 0x3 selected (volume
      for channel 2) based on hardware tests by Nemesis.

      03/09/2018: Lord Nightmare, Qbix, ValleyBell, NewRisingSun
      * renamed the NCR8496 to its correct name, based on chip pictures on VGMPF
      * fixed NCR8496's noise LFSR behavior so it is only reset if the mode bit in
      register 6 is changed.
      * NCR8496's LFSR feedback function is an XNOR, which is now supported.
      * add PSSJ-3 support for the later Tandy 1000 series computers.
      * NCR8496's output is inverted, PSSJ-3's output is not.

      10/12/2019: Michael Zapf
      * READY line handling by own emu_timer, not depending on sound_stream_update


      TODO:
      * Implement the TMS9919 - any difference to sn94624?
      * Implement the T6W28; has registers in a weird order, needs writes to be
        'sanitized' first. Also is stereo, similar to game gear.
      * Factor out common code so that the SAA1099 can share some code.
      * verify NCR8496/PSSJ-3 behavior on write to mirrored registers; unlike the
        other variants, the NCR-derived variants are implied to ignore writes to
        regs 1,3,5,6,7 if 0x80 is not set. This needs to be verified on real hardware.

      BTANB:
      * Unless the registers are quickly initialized by software, non-Sega chips
        output a low-pitch beep at cold boot, eg. bbcm, bankp, exedexes, docastle.

    ***************************************************************************/

    // pinout reference

    /*
                 ___   ___
           D2 1 |*  \_/   | 16 Vcc
           D1 2 |         | 15 D3
           D0 3 |         | 14 CLOCK
        READY 4 | SN76496 | 13 D4
          _WE 5 |         | 12 D5
          _CE 6 |         | 11 D6
    AUDIO OUT 7 |         | 10 D7
          GND 8 |_________| 9  AUDIO IN

        on SN76489, pin 9 is N/C

    */

public abstract class SN76496_base_device
{
        public enum device_type
        {
            sn74696 = 0,
            y2404 = 1,
            sn76489 = 2,
            sn76489a = 3,
            sn76494 = 4,
            sn94624 = 5,
            ncr8496 = 6,
            pssj3 = 7,
            gamegear = 8,
            segapsg = 9
        }

        static bool BIT(int value, int bit) { return (((value) >> (bit)) & 1) == 1; }

        //devcb_write_line m_ready_handler;
        //emu_timer* m_ready_timer;
        //sound_stream* m_sound;

        // internal state
        bool m_ready_state;      // READY pin state
        public bool ready_r() { return m_ready_state; }
        int[] m_vol_table;    // volume table (for 4-bit to db conversion)
        int[] m_register;      // registers
        int m_last_register;    // last register written
        int[] m_volume;        // db volume of voice 0-2 and noise
        int m_RNG;              // noise generator LFSR
        int m_current_clock;
        int m_stereo_mask;      // the stereo output mask
        int[] m_period;        // length of 1/2 of waveform
        int[] m_count;         // position within the waveform
        int[] m_output;        // 1-bit output of each channel, pre-volume

        // configuration
        readonly int m_feedback_mask;      // mask for feedback
        readonly int m_whitenoise_tap1;    // mask for white noise tap 1 (higher one, usually bit 14)
        readonly int m_whitenoise_tap2;    // mask for white noise tap 2 (lower one, usually bit 13)
        readonly bool m_negate;            // output negate flag
        readonly bool m_stereo;            // whether we're dealing with stereo or not
        readonly int m_clock;              // clock frequency in Hz
        readonly int m_clock_divider;      // clock divider
        readonly int m_ext_clock;          // external clock frequency (i.e. sample rate) in Hz
        readonly bool m_ncr_style_psg;     // flag to ignore writes to regs 1,3,5,6,7 with bit 7 low
        readonly bool m_sega_style_psg;    // flag to make frequency zero acts as if it is one more than max (0x3ff+1) or if it acts like 0; the initial register is pointing to 0x3 instead of 0x0; the volume reg is preloaded with 0xF instead of 0x0

        /// <summary>
        /// Creates a new SN76489 base device.
        /// </summary>
        /// <param name="fbmask">Feedback mask.</param>
        /// <param name="tap1">Feedback tap bits.</param>
        /// <param name="tap2">Feedback tap bits.</param>
        /// <param name="negate">Negate output signal.</param>
        /// <param name="stereo">Stereo output.</param>
        /// <param name="clock">Clock frequency in Hz.</param>
        /// <param name="divider">Clock divider.</param>
        /// <param name="ncrpsg">NCR style PSG.</param>
        /// <param name="segapsg">Sega sytle PSG.</param>
        public SN76496_base_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg)
        {
            m_vol_table = new int[16];
            m_register = new int[8];
            m_volume = new int[4];
            m_period = new int[4];
            m_count = new int[4];
            m_output = new int[4];

            m_feedback_mask = fbmask;
            m_whitenoise_tap1 = tap1;
            m_whitenoise_tap2 = tap2;
            m_negate = negate;
            m_stereo = stereo;

            m_clock = clock;
            m_clock_divider = divider;
            m_ext_clock = extclock;
            m_ncr_style_psg = ncrpsg;
            m_sega_style_psg = segapsg;
        }

        public void Device_Start()
        {
            const int MAX_OUTPUT = 0x7fff;
            int sample_rate = m_clock / 2;

            //m_sound = stream_alloc(0, (m_stereo ? 2 : 1), sample_rate);

            m_last_register = m_sega_style_psg ? 3 : 0; // Sega VDP PSG defaults to selected period reg for 2nd channel
            for (int i = 0; i < 8; i += 2)
            {
                m_register[i] = 0;
                m_register[i + 1] = m_sega_style_psg ? 0xf : 0; // volume = 0x0 (max volume) on reset; this needs testing on chips other than SN76489A and Sega VDP PSG
            }

            for (int i = 0; i < 4; i++)
            {
                m_output[i] = 0;
                m_count[i] = 0;
                m_period[i] = (m_sega_style_psg || i == 3) ? 0 : 0x400;
            }

            m_RNG = m_feedback_mask;
            m_output[3] = m_RNG & 1;

            m_stereo_mask = 0xff; // all channels enabled
            m_current_clock = m_clock_divider - 1;

            // set gain
            int gain = 0;
            gain &= 0xff;

            // increase max output basing on gain (0.2 dB per step)
            double out1 = MAX_OUTPUT / 4; // four channels, each gets 1/4 of the total range
            
            while (gain-- > 0)
		        out1 *= 1.023292992; // = (10 ^ (0.2/20))

            // build volume table (2dB per step)
            for (int i = 0; i < 15; i++)
            {
                // limit volume to avoid clipping
                if (out1 > MAX_OUTPUT / 4)
                    m_vol_table[i] = MAX_OUTPUT / 4;
                else
                    m_vol_table[i] = (int)out1;

		    out1 /= 1.258925412; /* = 10 ^ (2/20) = 2dB */
            }
        
            m_vol_table[15] = 0;

	        for (int i = 0; i< 4; i++)
		        m_volume[i] = m_vol_table[m_register[i * 2 + 1]];
            	m_ready_state = false;
            	//m_ready_timer = timer_alloc(FUNC(sn76496_base_device::delayed_ready), this);
                //m_ready_timer->adjust(attotime::zero);
        }

        public void Device_Clock_Changed()
        {
            //m_sound->set_sample_rate(clock() / 2);
        }

        public void Stereo_W(byte data)
        {
            //m_sound->update();

            if (m_stereo)
                m_stereo_mask = data;
            //else
                //fatalerror("sn76496_base_device: Call to stereo write with mono chip!\n");
        }

        public void Set_Ready_State(bool state)
        {
            if (state != m_ready_state)
            {
                m_ready_state = state;

                //if (!m_ready_handler.isunset())
                //    m_ready_handler(state ? ASSERT_LINE : CLEAR_LINE);
            }
        }

        public void Write(byte data)
        {
            // update the output buffer before changing the registers
            //m_sound->update();

            int r;
            if ((data & 0x80) != 0)
            {
                r = (data & 0x70) >> 4;
                m_last_register = r;
                if ((m_ncr_style_psg && (r == 6)) && ((data & 0x04) != (m_register[6] & 0x04)))
                    m_RNG = m_feedback_mask; // NCR-style PSG resets the LFSR only on a mode write which actually changes the state of bit 2 of register 6
                m_register[r] = (m_register[r] & 0x3f0) | (data & 0x0f);
            }
            else
            {
                r = m_last_register;
                //if (m_ncr_style_psg && ((r & 1) || (r == 6)))
                //  return; // NCR-style PSG ignores writes to regs 1, 3, 5, 6 and 7 with bit 7 clear; this behavior is not verified on hardware yet, uncomment it once verified.
            }

            int c = r >> 1;
            switch (r)
            {
                case 0: // tone 0: frequency
                case 2: // tone 1: frequency
                case 4: // tone 2: frequency
                    if ((data & 0x80) == 0)
                        m_register[r] = (m_register[r] & 0x0f) | ((data & 0x3f) << 4);
                    if ((m_register[r] != 0) || m_sega_style_psg)
                        m_period[c] = m_register[r];
                    else m_period[c] = 0x400;

                    if (r == 4)
                    {
                        // update noise shift frequency
                        if ((m_register[6] & 0x03) == 0x03)
                            m_period[3] = m_period[2] << 1;
                    }
                    break;
                case 1: // tone 0: volume
                case 3: // tone 1: volume
                case 5: // tone 2: volume
                case 7: // noise: volume
                    m_volume[c] = m_vol_table[data & 0x0f];
                    if ((data & 0x80) == 0)
                        m_register[r] = (m_register[r] & 0x3f0) | (data & 0x0f);
                    break;
                case 6: // noise: frequency, mode
                    {
                        if ((data & 0x80) == 0)
                            //logerror("sn76496_base_device: write to reg 6 with bit 7 clear; data was %03x, new write is %02x!\n", m_register[6], data);
                        if ((data & 0x80) == 0)
                            m_register[r] = (m_register[r] & 0x3f0) | (data & 0x0f);

                        int n = m_register[6];
                        // N/512,N/1024,N/2048,Tone #3 output
                        m_period[3] = ((n & 3) == 3) ? (m_period[2] << 1) : (1 << (5 + (n & 3)));
                        if (!m_ncr_style_psg)
                            m_RNG = m_feedback_mask;
                    }
                    break;
            }

            //set_ready_state(0);
            //m_ready_timer->adjust(attotime::from_hz(clock() / (4 * m_clock_divider)));
        }

        public void Sound_Stream_Update(int[] samples) //(sound_stream &stream)
        {
            short out1;
            short out2 = 0;

            //for (int sampindex = 0; sampindex < stream.samples(); sampindex++)
            for (int sampindex = 0; sampindex <= samples.GetUpperBound(0); sampindex++)
            {
                // clock chip once
                if (m_current_clock > 0) // not ready for new divided clock
                {
                    m_current_clock--;
                }
                else // ready for new divided clock, make a new sample
                {
                    m_current_clock = m_clock_divider - 1;

                    // handle channels 0,1,2
                    for (int i = 0; i < 3; i++)
                    {
                        m_count[i]--;
                        if (m_count[i] <= 0)
                        {
                            m_output[i] ^= 1;
                            m_count[i] = m_period[i];
                        }
                    }

                    // handle channel 3
                    m_count[3]--;
                    if (m_count[3] <= 0)
                    {
                        // if noisemode is 1, both taps are enabled
                        // if noisemode is 0, the lower tap, whitenoisetap2, is held at 0
                        // The != was a bit-XOR (^) before
                        if (((m_RNG & m_whitenoise_tap1) != 0) != (((m_RNG & m_whitenoise_tap2) != (m_ncr_style_psg ? m_whitenoise_tap2 : 0)) && BIT(m_register[6], 2)))
                        {
                            m_RNG >>= 1;
                            m_RNG |= m_feedback_mask;
                        }
                        else
                        {
                            m_RNG >>= 1;
                        }
                        m_output[3] = m_RNG & 1;
                        m_count[3] = m_period[3];
                    }
                }

                if (m_stereo)
                {
			        out1 = (short)(((((m_stereo_mask & 0x10) != 0) && (m_output[0] != 0)) ? m_volume[0] : 0) +
                        ((((m_stereo_mask & 0x20) != 0) && (m_output[1] != 0)) ? m_volume[1] : 0) +
                        ((((m_stereo_mask & 0x40) != 0) && (m_output[2] != 0)) ? m_volume[2] : 0) +
                        ((((m_stereo_mask & 0x80) != 0) && (m_output[3] != 0)) ? m_volume[3] : 0));

                    out2 = (short)(((((m_stereo_mask & 0x1) != 0) && (m_output[0] != 0)) ? m_volume[0] : 0) +
                        ((((m_stereo_mask & 0x2) != 0) && (m_output[1] != 0)) ? m_volume[1] : 0) +
                        ((((m_stereo_mask & 0x4) != 0) && (m_output[2] != 0)) ? m_volume[2] : 0) +
                        ((((m_stereo_mask & 0x8) != 0) && (m_output[3] != 0)) ? m_volume[3] : 0));
                }
                else
                {
			        out1 = (short)(((m_output[0] != 0) ? m_volume[0] : 0) +
                        ((m_output[1] != 0) ? m_volume[1] : 0) +
                        ((m_output[2] != 0) ? m_volume[2] : 0) +
                        ((m_output[3] != 0) ? m_volume[3] : 0));
                }

                if (m_negate) { out1 = (short)-out1; out2 = (short)-out2; }

                //stream.put_int(0, sampindex, out, 32768);
                //if (m_stereo)
                    //stream.put_int(1, sampindex, out2, 32768);
            }
        }


    };

    // SN76496: Whitenoise verified, phase verified, periodic verified (by Michael Zapf)
    public class SN76496_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) : 
            SN76496_base_device(clock, 8, extclock, 0x10000, 0x04, 0x08, false, false, false, false)
    {
    };

    // Y2404: not verified yet. todo: verify; (don't be fooled by the Y, it's a TI chip, not Yamaha)
    public class Y2404_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x10000, 0x04, 0x08, false, false, false, false)
        {
    };

    // SN76489: not verified yet. todo: verify;
    public class SN76489_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x4000, 0x01, 0x02, true, false, false, false)
        {
    };

    // SN76489A: whitenoise verified, phase verified, periodic verified (by plgdavid)
    public class sn76489a_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x10000, 0x04, 0x08, false, false,  false, false)
        {
    };

    // SN76494: not verified, (according to datasheet: same as sn76489a but without the /8 divider)
    public class sn76494_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 1, extclock, 0x10000, 0x04, 0x08, false, false, false, false)
        {
    };

    // SN94624: whitenoise verified, phase verified, period verified; verified by PlgDavid
    class sn94624_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 1, extclock, 0x4000, 0x01, 0x02, true, false, false, false)
        {
    };

    // NCR8496: whitenoise verified, phase verified; verified by ValleyBell & NewRisingSun
    class ncr8496_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x8000, 0x02, 0x20, true, false, true, false)
        {
    };

    // PSSJ-3: whitenoise verified, phase verified; verified by ValleyBell & NewRisingSun
    class pssj3_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x8000, 0x02, 0x20, false, false, true, false)
        {
    };

    // Game Gear PSG: Verified by Justin Kerk
    class gamegear_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x8000, 0x01, 0x08, true, true, false, true)
        {
    };

    // Sega VDP PSG: todo: verify; from smspower wiki, assumed to have same invert as gamegear
    class segapsg_device(int clock, int divider, int extclock, int fbmask, int tap1, int tap2, bool negate, bool stereo, bool ncrpsg, bool segapsg) :
            SN76496_base_device(clock, 8, extclock, 0x8000, 0x01, 0x08, true, false, false, true)
        {
    };

}