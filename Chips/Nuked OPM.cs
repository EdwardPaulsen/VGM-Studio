namespace Nuked
{
    /* Nuked OPM
     * Copyright (C) 2020, 2026 Nuke.YKT
     *
     * This file is part of Nuked OPM.
     *
     * Nuked OPM is free software: you can redistribute it and/or modify
     * it under the terms of the GNU Lesser General Public License as
     * published by the Free Software Foundation, either version 2.1
     * of the License, or (at your option) any later version.
     *
     * Nuked OPM is distributed in the hope that it will be useful,
     * but WITHOUT ANY WARRANTY; without even the implied warranty of
     * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
     * GNU Lesser General Public License for more details.
     *
     * You should have received a copy of the GNU Lesser General Public License
     * along with Nuked OPM. If not, see <https://www.gnu.org/licenses/>.
     *
     *  Nuked OPM emulator.
     *  Thanks:
     *      John McMaster(siliconpr0n.org):
     *          YM2151 and other FM chip decaps and die shots.
     *      gtr3qq (https://github.com/gtr3qq):
     *          YM2164 decap
     *
     * version: 1.0
     */

    public class OPM
    {
        /* logsin table */
        public static readonly ushort[] logsinrom = [
            0x859, 0x6c3, 0x607, 0x58b, 0x52e, 0x4e4, 0x4a6, 0x471,
            0x443, 0x41a, 0x3f5, 0x3d3, 0x3b5, 0x398, 0x37e, 0x365,
            0x34e, 0x339, 0x324, 0x311, 0x2ff, 0x2ed, 0x2dc, 0x2cd,
            0x2bd, 0x2af, 0x2a0, 0x293, 0x286, 0x279, 0x26d, 0x261,
            0x256, 0x24b, 0x240, 0x236, 0x22c, 0x222, 0x218, 0x20f,
            0x206, 0x1fd, 0x1f5, 0x1ec, 0x1e4, 0x1dc, 0x1d4, 0x1cd,
            0x1c5, 0x1be, 0x1b7, 0x1b0, 0x1a9, 0x1a2, 0x19b, 0x195,
            0x18f, 0x188, 0x182, 0x17c, 0x177, 0x171, 0x16b, 0x166,
            0x160, 0x15b, 0x155, 0x150, 0x14b, 0x146, 0x141, 0x13c,
            0x137, 0x133, 0x12e, 0x129, 0x125, 0x121, 0x11c, 0x118,
            0x114, 0x10f, 0x10b, 0x107, 0x103, 0x0ff, 0x0fb, 0x0f8,
            0x0f4, 0x0f0, 0x0ec, 0x0e9, 0x0e5, 0x0e2, 0x0de, 0x0db,
            0x0d7, 0x0d4, 0x0d1, 0x0cd, 0x0ca, 0x0c7, 0x0c4, 0x0c1,
            0x0be, 0x0bb, 0x0b8, 0x0b5, 0x0b2, 0x0af, 0x0ac, 0x0a9,
            0x0a7, 0x0a4, 0x0a1, 0x09f, 0x09c, 0x099, 0x097, 0x094,
            0x092, 0x08f, 0x08d, 0x08a, 0x088, 0x086, 0x083, 0x081,
            0x07f, 0x07d, 0x07a, 0x078, 0x076, 0x074, 0x072, 0x070,
            0x06e, 0x06c, 0x06a, 0x068, 0x066, 0x064, 0x062, 0x060,
            0x05e, 0x05c, 0x05b, 0x059, 0x057, 0x055, 0x053, 0x052,
            0x050, 0x04e, 0x04d, 0x04b, 0x04a, 0x048, 0x046, 0x045,
            0x043, 0x042, 0x040, 0x03f, 0x03e, 0x03c, 0x03b, 0x039,
            0x038, 0x037, 0x035, 0x034, 0x033, 0x031, 0x030, 0x02f,
            0x02e, 0x02d, 0x02b, 0x02a, 0x029, 0x028, 0x027, 0x026,
            0x025, 0x024, 0x023, 0x022, 0x021, 0x020, 0x01f, 0x01e,
            0x01d, 0x01c, 0x01b, 0x01a, 0x019, 0x018, 0x017, 0x017,
            0x016, 0x015, 0x014, 0x014, 0x013, 0x012, 0x011, 0x011,
            0x010, 0x00f, 0x00f, 0x00e, 0x00d, 0x00d, 0x00c, 0x00c,
            0x00b, 0x00a, 0x00a, 0x009, 0x009, 0x008, 0x008, 0x007,
            0x007, 0x007, 0x006, 0x006, 0x005, 0x005, 0x005, 0x004,
            0x004, 0x004, 0x003, 0x003, 0x003, 0x002, 0x002, 0x002,
            0x002, 0x001, 0x001, 0x001, 0x001, 0x001, 0x001, 0x001,
            0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000, 0x000
        ];

        /* exp table */
        public static readonly ushort[] exprom = [
            0x7fa, 0x7f5, 0x7ef, 0x7ea, 0x7e4, 0x7df, 0x7da, 0x7d4,
            0x7cf, 0x7c9, 0x7c4, 0x7bf, 0x7b9, 0x7b4, 0x7ae, 0x7a9,
            0x7a4, 0x79f, 0x799, 0x794, 0x78f, 0x78a, 0x784, 0x77f,
            0x77a, 0x775, 0x770, 0x76a, 0x765, 0x760, 0x75b, 0x756,
            0x751, 0x74c, 0x747, 0x742, 0x73d, 0x738, 0x733, 0x72e,
            0x729, 0x724, 0x71f, 0x71a, 0x715, 0x710, 0x70b, 0x706,
            0x702, 0x6fd, 0x6f8, 0x6f3, 0x6ee, 0x6e9, 0x6e5, 0x6e0,
            0x6db, 0x6d6, 0x6d2, 0x6cd, 0x6c8, 0x6c4, 0x6bf, 0x6ba,
            0x6b5, 0x6b1, 0x6ac, 0x6a8, 0x6a3, 0x69e, 0x69a, 0x695,
            0x691, 0x68c, 0x688, 0x683, 0x67f, 0x67a, 0x676, 0x671,
            0x66d, 0x668, 0x664, 0x65f, 0x65b, 0x657, 0x652, 0x64e,
            0x649, 0x645, 0x641, 0x63c, 0x638, 0x634, 0x630, 0x62b,
            0x627, 0x623, 0x61e, 0x61a, 0x616, 0x612, 0x60e, 0x609,
            0x605, 0x601, 0x5fd, 0x5f9, 0x5f5, 0x5f0, 0x5ec, 0x5e8,
            0x5e4, 0x5e0, 0x5dc, 0x5d8, 0x5d4, 0x5d0, 0x5cc, 0x5c8,
            0x5c4, 0x5c0, 0x5bc, 0x5b8, 0x5b4, 0x5b0, 0x5ac, 0x5a8,
            0x5a4, 0x5a0, 0x59c, 0x599, 0x595, 0x591, 0x58d, 0x589,
            0x585, 0x581, 0x57e, 0x57a, 0x576, 0x572, 0x56f, 0x56b,
            0x567, 0x563, 0x560, 0x55c, 0x558, 0x554, 0x551, 0x54d,
            0x549, 0x546, 0x542, 0x53e, 0x53b, 0x537, 0x534, 0x530,
            0x52c, 0x529, 0x525, 0x522, 0x51e, 0x51b, 0x517, 0x514,
            0x510, 0x50c, 0x509, 0x506, 0x502, 0x4ff, 0x4fb, 0x4f8,
            0x4f4, 0x4f1, 0x4ed, 0x4ea, 0x4e7, 0x4e3, 0x4e0, 0x4dc,
            0x4d9, 0x4d6, 0x4d2, 0x4cf, 0x4cc, 0x4c8, 0x4c5, 0x4c2,
            0x4be, 0x4bb, 0x4b8, 0x4b5, 0x4b1, 0x4ae, 0x4ab, 0x4a8,
            0x4a4, 0x4a1, 0x49e, 0x49b, 0x498, 0x494, 0x491, 0x48e,
            0x48b, 0x488, 0x485, 0x482, 0x47e, 0x47b, 0x478, 0x475,
            0x472, 0x46f, 0x46c, 0x469, 0x466, 0x463, 0x460, 0x45d,
            0x45a, 0x457, 0x454, 0x451, 0x44e, 0x44b, 0x448, 0x445,
            0x442, 0x43f, 0x43c, 0x439, 0x436, 0x433, 0x430, 0x42d,
            0x42a, 0x428, 0x425, 0x422, 0x41f, 0x41c, 0x419, 0x416,
            0x414, 0x411, 0x40e, 0x40b, 0x408, 0x406, 0x403, 0x400
        ];

        /* Envelope generator */
        public static readonly uint[,] eg_stephi = {
            { 0, 0, 0, 0 },
            { 1, 0, 0, 0 },
            { 1, 0, 1, 0 },
            { 1, 1, 1, 0 }
        };

        /* Phase generator */
        public static readonly uint[] pg_detune = [
            16, 17, 19, 20, 22, 24, 27, 29
        ];

        public struct FreqTable(int b, bool a, int s)
        {
            public int basefreq = b;
            public bool approxtype = a;
            public int slope = s;
        }

        public static readonly FreqTable[] pg_freqtable = [
            new FreqTable( 1299, true, 19),
            new FreqTable( 1318, true, 19 ),
            new FreqTable( 1337, true, 19 ),
            new FreqTable( 1356, true, 20 ),
            new FreqTable( 1376, true, 20 ),
            new FreqTable( 1396, true, 20 ),
            new FreqTable( 1416, true, 21 ),
            new FreqTable( 1437, true, 20 ),
            new FreqTable( 1458, true, 21 ),
            new FreqTable( 1479, true, 21 ),
            new FreqTable( 1501, true, 22 ),
            new FreqTable( 1523, true, 22 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 1545, true, 22 ),
            new FreqTable( 1567, true, 22 ),
            new FreqTable( 1590, true, 23 ),
            new FreqTable( 1613, true, 23 ),
            new FreqTable( 1637, true, 23 ),
            new FreqTable( 1660, true, 24 ),
            new FreqTable( 1685, true, 24 ),
            new FreqTable( 1709, true, 24 ),
            new FreqTable( 1734, true, 25 ),
            new FreqTable( 1759, true, 25 ),
            new FreqTable( 1785, true, 26 ),
            new FreqTable( 1811, true, 26 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 1837, true, 26 ),
            new FreqTable( 1864, true, 27 ),
            new FreqTable( 1891, true, 27 ),
            new FreqTable( 1918, true, 28 ),
            new FreqTable( 1946, true, 28 ),
            new FreqTable( 1975, true, 28 ),
            new FreqTable( 2003, true, 29 ),
            new FreqTable( 2032, true, 30 ),
            new FreqTable( 2062, true, 30 ),
            new FreqTable( 2092, true, 30 ),
            new FreqTable( 2122, true, 31 ),
            new FreqTable( 2153, true, 31 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 2185, true, 31 ),
            new FreqTable( 2216, false, 31 ),
            new FreqTable( 2249, false, 31 ),
            new FreqTable( 2281, false, 31 ),
            new FreqTable( 2315, false, 31 ),
            new FreqTable( 2348, false, 31 ),
            new FreqTable( 2382, false, 30 ),
            new FreqTable( 2417, false, 30 ),
            new FreqTable( 2452, false, 30 ),
            new FreqTable( 2488, false, 30 ),
            new FreqTable( 2524, false, 30 ),
            new FreqTable( 2561, false, 30 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 ),
            new FreqTable( 0,    false, 16 )
        ];


        /* FM algorithm */
        public static readonly uint[,,] fm_algorithm = {
            {
                { 1, 1, 1, 1, 1, 1, 1, 1 }, /* M1_0          */
                { 1, 1, 1, 1, 1, 1, 1, 1 }, /* M1_1          */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* C1            */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 1 }  /* Out           */
            },
            {
                { 0, 1, 0, 0, 0, 1, 0, 0 }, /* M1_0          */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* M1_1          */
                { 1, 1, 1, 0, 0, 0, 0, 0 }, /* C1            */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 1, 1, 1 }  /* Out           */
            },
            {
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* M1_0          */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* M1_1          */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* C1            */
                { 1, 0, 0, 1, 1, 1, 1, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 1, 1, 1, 1 }  /* Out           */
            },
            {
                { 0, 0, 1, 0, 0, 1, 0, 0 }, /* M1_0          */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* M1_1          */
                { 0, 0, 0, 1, 0, 0, 0, 0 }, /* C1            */
                { 1, 1, 0, 1, 1, 0, 0, 0 }, /* Last operator */
                { 0, 0, 1, 0, 0, 0, 0, 0 }, /* Last operator */
                { 1, 1, 1, 1, 1, 1, 1, 1 }  /* Out           */
            }
        };

        public static readonly ushort[] lfo_counter2_table = [
            0x0000, 0x4000, 0x6000, 0x7000,
            0x7800, 0x7c00, 0x7e00, 0x7f00,
            0x7f80, 0x7fc0, 0x7fe0, 0x7ff0,
            0x7ff8, 0x7ffc, 0x7ffe, 0x7fff
        ];

        public enum OPMFlags : uint
        {
            none = 0x00,
            ym2164 = 0x01,   /* YM2164(OPP) */
        };

        public struct ChipState
        {
            public uint cycles;
            public bool ic;
            public bool ic2;
            public bool opp;

            // IO
            public byte write_data;
            public bool write_a;
            public bool write_a_en;
            public bool write_d;
            public bool write_d_en;
            public bool write_busy;
            public byte write_busy_cnt;
            public byte mode_address;
            public bool io_ct1;
            public bool io_ct2;

            // LFO
            public byte lfo_am_lock;
            public byte lfo_pm_lock;
            public byte lfo_counter1;
            public byte lfo_counter1_of1;
            public bool lfo_counter1_of2;
            public ushort lfo_counter2;
            public bool lfo_counter2_load;
            public bool lfo_counter2_of;
            public bool lfo_counter2_of_lock;
            public bool lfo_counter2_of_lock2;
            public bool lfo_counter3_clock;
            public ushort lfo_counter3;
            public bool lfo_counter3_step;
            public bool lfo_frq_update;
            public bool lfo_clock;
            public bool lfo_clock_lock;
            public bool lfo_clock_test;
            public bool lfo_test;
            public uint lfo_val;
            public byte lfo_val_carry;
            public uint lfo_out1;
            public uint lfo_out2;
            public uint lfo_out2_b;
            public bool lfo_mult_carry;
            public bool lfo_trig_sign;
            public bool lfo_saw_sign;
            public byte lfo_bit_counter;

            // Env Gen
            public EGNum[] eg_state;
            public ushort[] eg_level;
            public byte[] eg_rate;
            public byte[] eg_sl;
            public byte[] eg_tl;
            public ushort eg_tl_opp;
            public bool[] eg_zr;
            public byte eg_timershift_lock;
            public byte eg_timer_lock;
            public byte eg_inchi;
            public byte eg_shift;
            public byte eg_clock;
            public byte eg_clockcnt;
            public byte eg_clockquotinent;
            public byte eg_inc;
            public bool[] eg_ratemax;
            public bool eg_instantattack;
            public bool eg_inclinear;
            public bool eg_incattack;
            public bool eg_mute;
            public ushort[] eg_outtemp;
            public ushort[] eg_out;
            public ushort eg_am;
            public byte[] eg_ams;
            public bool eg_timercarry;
            public uint eg_timer;
            public uint eg_timer2;
            public bool eg_timerbstop;
            public uint eg_serial;
            public bool eg_serial_bit;
            public bool eg_test;


            // Phase Gen
            public ushort[] pg_fnum;
            public byte[] pg_kcode;
            public uint[] pg_inc;
            public uint[] pg_phase;
            public bool[] pg_reset;
            public bool[] pg_reset_latch;
            public uint pg_serial;
            public byte pg_opp_pms;
            public byte[] pg_opp_dt2;

            // Operator
            public ushort op_phase_in;
            public ushort op_mod_in;
            public ushort op_phase;
            public ushort[] op_logsin;
            public ushort op_atten;
            public ushort[] op_exp;
            public byte[] op_pow;
            public uint op_sign;
            public short[] op_out;
            public uint op_connect;
            public byte op_counter;
            public bool op_fbupdate;
            public byte op_fbshift;
            public bool op_c1update;
            public bool[] op_modtable;
            public short[,] op_m1;
            public short[] op_c1;
            public short[] op_mod;
            public short[] op_fb;
            public byte op_mixl;
            public byte op_mixr;
            public byte op_opp_rl;
            public byte[] op_opp_fb;

            // Mixer

            public int[] mix;
            public int[] mix2;
            public int mix_op;
            public uint[] mix_serial;
            public uint mix_bits;
            public uint mix_top_bits_lock;
            public bool mix_sign_lock;
            public bool mix_sign_lock2;
            public byte mix_exp_lock;
            public bool[] mix_clamp_low;
            public bool[] mix_clamp_high;
            public byte mix_out_bit;

            // Output
            public bool smp_so;
            public bool smp_sh1;
            public bool smp_sh2;

            // Noise
            public uint noise_lfsr;
            public uint noise_timer;
            public bool noise_timer_of;
            public bool noise_update;
            public byte noise_bit;

            // Register set
            public bool[] mode_test;
            public bool[] mode_kon_operator;
            public byte mode_kon_channel;

            public byte reg_address;
            public bool reg_address_ready;
            public byte reg_data;
            public bool reg_data_ready;

            public byte[] ch_rl;
            public byte[] ch_fb;
            public byte[] ch_connect;
            public byte[] ch_kc;
            public byte[] ch_kf;
            public byte[] ch_pms;
            public byte[] ch_ams;

            public byte[] sl_dt1;
            public byte[] sl_mul;
            public byte[] sl_tl;
            public byte[] sl_ks;
            public byte[] sl_ar;
            public bool[] sl_am_e;
            public byte[] sl_d1r;
            public byte[] sl_dt2;
            public byte[] sl_d2r;
            public byte[] sl_d1l;
            public byte[] sl_rr;

            public bool noise_en;
            public byte noise_freq;

            // OPP
            public byte[] ch_ramp_div;
            public byte reg_20_delay;
            public bool reg_28_delay;
            public bool reg_30_delay;
            public byte[] opp_tl_cnt;
            public ushort[] opp_tl;

            // Timer
            public ushort timer_a_reg;
            public byte timer_b_reg;
            public bool timer_a_temp;
            public bool timer_a_do_reset, timer_a_do_load;
            public bool timer_a_inc;
            public ushort timer_a_val;
            public bool timer_a_of; // "_of" means "overflow"
            public bool timer_a_load;
            public bool timer_a_status;

            public byte timer_b_sub;
            public bool timer_b_sub_of;
            public bool timer_b_inc;
            public ushort timer_b_val;
            public bool timer_b_of;
            public bool timer_b_do_reset, timer_b_do_load;
            public bool timer_b_temp;
            public bool timer_b_status;
            public bool timer_irq;

            public byte lfo_freq_hi;
            public byte lfo_freq_lo;
            public byte lfo_pmd;
            public byte lfo_amd;
            public byte lfo_wave;

            public bool timer_irqa, timer_irqb;
            public bool timer_loada, timer_loadb;
            public bool timer_reseta, timer_resetb;
            public bool mode_csm;

            public bool nc_active, nc_active_lock, nc_sign, nc_sign_lock, nc_sign_lock2;
            public byte nc_bit;
            public ushort nc_out;
            public short op_mix;

            public bool kon_csm, kon_csm_lock;
            public byte kon_do;
            public bool kon_chanmatch;
            public bool[] kon;
            public bool[] kon2;
            public bool[] mode_kon;

            // DAC
            public bool dac_osh1, dac_osh2;
            public ushort dac_bits;
            public int[] dac_output;


            // For buffered writes
            public ulong writebuf_samplecnt;
            public uint writebuf_cur;
            public uint writebuf_last;
            public ulong writebuf_lasttime;
            public readonly ulong WRITEBUF_SIZE = 2048;
            public readonly ulong WRITEBUF_DELAY = 36;
            public WriteBuf[] writebuf;


            // For resampling
            public readonly int RSM_FRAC = 10;
            public uint rate, clock;
            public uint rateratio;
            public int samplecnt;
            public int[] oldsamples;
            public int[] samples;
            public readonly int OUTPUT_FACTOR = 11;
            public readonly int OUTPUT_FACTOR_F = 12;
            public double FILTER_CUTOFF, FILTER_CUTOFF_I;
            public bool[] mute;

            public ChipState()
            {
                // Env Gen
                eg_state = new EGNum[32];
                eg_level = new ushort[32];
                eg_rate = new byte[2];
                eg_sl = new byte[2];
                eg_tl = new byte[3];
                eg_zr = new bool[2];
                eg_ratemax = new bool[2];
                eg_outtemp = new ushort[2];
                eg_out = new ushort[2];
                eg_ams = new byte[2];

                // Phase Gen
                pg_fnum = new ushort[32];
                pg_kcode = new byte[32];
                pg_inc = new uint[32];
                pg_phase = new uint[32];
                pg_reset = new bool[32];
                pg_reset_latch = new bool[32];
                pg_opp_dt2 = new byte[32];

                // Operator
                op_logsin = new ushort[3];
                op_exp = new ushort[2];
                op_pow = new byte[2];
                op_out = new short[6];
                op_modtable = new bool[5];
                op_m1 = new short[8, 2];
                op_c1 = new short[8];
                op_mod = new short[3];
                op_fb = new short[2];
                op_opp_fb = new byte[3];

                // Mixer
                mix = new int[2];
                mix2 = new int[2];
                mix_serial = new uint[2];
                mix_clamp_low = new bool[2];
                mix_clamp_high = new bool[2];

                // Register set
                mode_test = new bool[8];
                mode_kon_operator = new bool[4];

                ch_rl = new byte[8];
                ch_fb = new byte[8];
                ch_connect = new byte[8];
                ch_kc = new byte[8];
                ch_kf = new byte[8];
                ch_pms = new byte[8];
                ch_ams = new byte[8];

                sl_dt1 = new byte[32];
                sl_mul = new byte[32];
                sl_tl = new byte[32];
                sl_ks = new byte[32];
                sl_ar = new byte[32];
                sl_am_e = new bool[32];
                sl_d1r = new byte[32];
                sl_dt2 = new byte[32];
                sl_d2r = new byte[32];
                sl_d1l = new byte[32];
                sl_rr = new byte[32];

                // OPP
                ch_ramp_div = new byte[8];
                opp_tl_cnt = new byte[8];
                opp_tl = new ushort[32];

                // Timer
                kon = new bool[32];
                kon2 = new bool[32];
                mode_kon = new bool[32];

                // DAC
                dac_output = new int[2];

                writebuf = new WriteBuf[WRITEBUF_SIZE];

                oldsamples = new int[2];
                samples = new int[2];
                mute = new bool[8];


            FILTER_CUTOFF = 0.51229881578701075; // 5894.6 Hz single pole IIR low pass (NTSC)
                FILTER_CUTOFF_I = (1 - FILTER_CUTOFF);
            }
        };

        public enum EGNum
        {
            attack = 0,
            decay = 1,
            sustain = 2,
            release = 3
        };

        public struct WriteBuf
        {
            public ulong time;
            public byte port;
            public byte data;
        }

        private static readonly bool use_filter = false;

        protected static int KCToFNum(int kcode)
        {
            int kcode_h = (kcode >> 4) & 63;
            int kcode_l = kcode & 15;
            int i, slope, sum = 0;

            if (pg_freqtable[kcode_h].approxtype)
            {
                for (i = 0; i < 4; i++)
                {
                    if ((kcode_l & (1 << i)) != 0)
                    {
                        sum += (pg_freqtable[kcode_h].slope >> (3 - i));
                    }
                }
            }
            else
            {
                slope = pg_freqtable[kcode_h].slope | 1;
                if ((kcode_l & 1) != 0)
                {
                    sum += (slope >> 3) + 2;
                }
                if ((kcode_l & 2) != 0)
                {
                    sum += 8;
                }
                if ((kcode_l & 4) != 0)
                {
                    sum += slope >> 1;
                }
                if ((kcode_l & 8) != 0)
                {
                    sum += slope;
                    sum++;
                }
                if ((kcode_l & 12) == 12 && (pg_freqtable[kcode_h].slope & 1) == 0)
                {
                    sum += 4;
                }
            }
            return pg_freqtable[kcode_h].basefreq + (sum >> 1);
        }

        protected static int LFOApplyPMS(int lfo, int pms)
        {
            bool t;
            int out0;
            int top = (lfo >> 4) & 7;

            if (pms != 7)
            {
                top >>= 1;
            }
            t = ((top & 6) == 6 && pms == 7) || ((top & 3) == 3 && pms >= 6);

            out0 = top + ((top >> 2) & 1) + (t ? 1 : 0);
            out0 = out0 * 2 + ((lfo >> 4) & 1);

            if (pms == 7)
            {
                out0 >>= 1;
            }
            out0 &= 15;
            out0 = (lfo & 15) + out0 * 16;
            out0 = pms switch
            {
                1 => (out0 >> 5) & 3,
                2 => (out0 >> 4) & 7,
                3 => (out0 >> 3) & 15,
                4 => (out0 >> 2) & 31,
                5 => (out0 >> 1) & 63,
                6 => (out0 & 255) << 1,
                7 => (out0 & 255) << 2,
                _ => 0,
            };
            return out0;
        }

        protected static int CalcKCode(int kcf, int lfo, bool lfo_sign, int dt)
        {
            int t2, t3; //, b0, b1, b2, b3, w2, w3, w6;
            bool b0, b1, b2, b3, w2, w3, w6;
            bool overflow1 = false;
            bool overflow2 = false;
            bool negoverflow = false;
            int sum, cr;
            
            if (!lfo_sign)
            {
                lfo = ~lfo;
            }
            sum = (kcf & 8191) + (lfo & 8191) + (lfo_sign ? 0 : 1); //(!lfo_sign);
            cr = ((kcf & 255) + (lfo & 255) + (lfo_sign ? 0 : 1)) >> 8; //(!lfo_sign)) >> 8;
            if ((sum & (1 << 13)) != 0)
            {
                overflow1 = true;
            }
            sum &= 8191;
            if (lfo_sign && ((((sum >> 6) & 3) == 3) || (cr != 0)))
            {
                sum += 64;
            }
            if (!lfo_sign && (cr == 0) && (lfo & 192) != 0)
            {
                sum += (-64) & 8191;
                negoverflow = true;
            }
            if ((sum & (1 << 13)) != 0)
            {
                overflow2 = true;
            }
            sum &= 8191;
            if ((!lfo_sign && !overflow1) || (negoverflow && !overflow2))
            {
                sum = 0;
            }
            if (lfo_sign && (overflow1 || overflow2))
            {
                sum = 8127;
            }

            t2 = sum & 63;
            if (dt == 2)
                t2 += 20;
            if (dt == 2 || dt == 3)
                t2 += 32;

            b0 = ((t2 >> 6) & 1) != 0;
            b1 = dt == 2;
            b2 = ((sum >> 6) & 1) != 0;
            b3 = ((sum >> 7) & 1) != 0;

            w2 = b0 && b1 && b2;
            w3 = b0 && b3;
            w6 = (b0 && !w2 && w3) || (b3 && !b0 && b1);

            t2 &= 63;

            t3 = (sum >> 6) + (w6 ? 1 : 0) + (b1 ? 1 : 0) + ((w2 || w3) ? 1 : 0) * 2 + ((dt == 3) ? 1 : 0) * 4 + ((dt != 0) ? 1 : 0) * 8;
            if ((t3 & 128) != 0)
            {
                t2 = 63;
                t3 = 126;
            }
            sum = t3 * 64 + t2;
            return sum;
        }

        protected static void PhaseCalcFNumBlock(ref ChipState chip)
        {
            uint slot = (chip.cycles + 7) & 31;
            uint channel = slot & 7;
            uint kcf = (uint)((chip.ch_kc[channel] << 6) + chip.ch_kf[channel]);
            uint lfo = (uint)((chip.lfo_pmd != 0) ? chip.lfo_pm_lock : 0);
            uint pms = chip.opp ? chip.pg_opp_pms : chip.ch_pms[channel];
            uint dt = chip.opp ? chip.pg_opp_dt2[slot] : chip.sl_dt2[slot];
            int lfo_pm = LFOApplyPMS((int)(lfo & 127), (int)pms);
            uint kcode = (uint)CalcKCode((int)kcf, lfo_pm, (lfo & 0x80) != 0 && pms != 0, (int)dt);
            uint fnum = (uint)KCToFNum((int)kcode);
            uint kcode_h = kcode >> 8;

            chip.pg_fnum[slot] = (ushort)fnum;
            chip.pg_kcode[slot] = (byte)kcode_h;

            if (chip.opp)
            {
                slot = chip.cycles & 31;
                channel = slot & 7;
                chip.pg_opp_pms = chip.ch_pms[channel];
                chip.pg_opp_dt2[slot] = chip.sl_dt2[slot];
            }
        }

        protected static void PhaseCalcIncrement(ref ChipState chip)
        {
            uint slot = chip.cycles;
            uint channel = slot & 7;
            uint dt = chip.sl_dt1[slot];
            uint dt_l = dt & 3;
            uint detune = 0;
            uint multi = chip.sl_mul[slot];
            uint kcode = chip.pg_kcode[slot];
            uint fnum = chip.pg_fnum[slot];
            uint block = kcode >> 2;
            uint basefreq = (fnum << (int)block) >> 2;
            uint note, sum, sum_h, sum_l, inc;
            
            /* Apply detune */
            if (dt_l != 0)
            {
                if (kcode > 0x1c)
                {
                    kcode = 0x1c;
                }
                block = kcode >> 2;
                note = kcode & 0x03;
                sum = block + 9 + (((dt_l == 3) ? 1u : 0) | (dt_l & 0x02));
                sum_h = sum >> 1;
                sum_l = sum & 0x01;
                detune = pg_detune[(sum_l << 2) | note] >> (9 - (int)sum_h);
            }
            if ((dt & 0x04) != 0)
            {
                basefreq -= detune;
            }
            else
            {
                basefreq += detune;
            }
            basefreq &= 0x1ffff;
            if (multi != 0)
            {
                inc = basefreq * multi;
            }
            else
            {
                inc = basefreq >> 1;
            }
            inc &= 0xfffff;
            chip.pg_inc[slot] = inc;
        }

        protected static void PhaseGenerate(ref ChipState chip)
        {
            uint slot = (chip.cycles + 27) & 31;

            chip.pg_reset_latch[slot] = chip.pg_reset[slot];
            slot = (chip.cycles + 25) & 31;
            
            /* Mask increment */
            if (chip.pg_reset_latch[slot])
            {
                chip.pg_inc[slot] = 0;
            }
            
            /* Phase step */
            slot = (chip.cycles + 24) & 31;
            if (chip.pg_reset_latch[slot] || chip.mode_test[3])
            {
                chip.pg_phase[slot] = 0;
            }
            chip.pg_phase[slot] += chip.pg_inc[slot];
            chip.pg_phase[slot] &= 0xfffff;
        }

        protected static void PhaseDebug(ref ChipState chip)
        {
            chip.pg_serial >>= 1;
            if (chip.cycles == 5)
            {
                chip.pg_serial |= (chip.pg_phase[29] & 0x3ff);
            }
        }

        protected static void KeyOn1(ref ChipState chip)
        {
            uint cycles = (chip.cycles + 1) & 31;

            chip.kon_chanmatch = false;
            if (chip.mode_kon_channel + 24 == cycles)
            {
                chip.kon_chanmatch = true;
            }
        }

        protected static void KeyOn2(ref ChipState chip)
        {
            uint slot = (chip.cycles + 8) & 31;

            if (chip.kon_chanmatch)
            {
                chip.mode_kon[(slot + 0) & 31] = chip.mode_kon_operator[0];
                chip.mode_kon[(slot + 8) & 31] = chip.mode_kon_operator[2];
                chip.mode_kon[(slot + 16) & 31] = chip.mode_kon_operator[1];
                chip.mode_kon[(slot + 24) & 31] = chip.mode_kon_operator[3];
            }
        }

        protected static void EnvelopePhase1(ref ChipState chip)
        {
            uint slot = (chip.cycles + 2) & 31;
            bool kon = chip.mode_kon[slot] || chip.kon_csm;

            chip.kon2[slot] = chip.kon[slot];
            chip.kon[slot] = kon;
        }

        protected static void EnvelopePhase2(ref ChipState chip)
        {
            uint slot = chip.cycles;
            uint chan = slot & 7;
            byte rate = 0, ksv, ams;
            bool zr; 

            EGNum sel = chip.eg_state[slot];
            if (chip.kon[slot] && chip.kon2[slot])
                sel = EGNum.attack;
            switch (sel)
            {
                case EGNum.attack:
                    rate = chip.sl_ar[slot];
                    break;
                case EGNum.decay:
                    rate = chip.sl_d1r[slot];
                    break;
                case EGNum.sustain:
                    rate = chip.sl_d2r[slot];
                    break;
                case EGNum.release:
                    rate = (byte)(chip.sl_rr[slot] * 2 + 1);
                    break;
                default:
                    break;
            }
            if (chip.ic)
            {
                rate = 31;
            }

            zr = rate == 0;

            ksv = (byte)(chip.pg_kcode[slot] >> (chip.sl_ks[slot] ^ 3));
            if (chip.sl_ks[slot] == 0 && zr)
            {
                ksv = (byte)(ksv & ~3);
            }
            rate = (byte)(rate * 2 + ksv);
            if ((rate & 64) != 0)
            {
                rate = 63;
            }

            if (chip.opp)
            {
                slot = (chip.cycles + 30) & 31;
                chip.eg_tl_opp = chip.opp_tl[slot];
            }
            else
            {
                chip.eg_tl[2] = chip.eg_tl[1];
                chip.eg_tl[1] = chip.eg_tl[0];
                chip.eg_tl[0] = chip.sl_tl[slot];
            }
            chip.eg_sl[1] = chip.eg_sl[0];
            chip.eg_sl[0] = chip.sl_d1l[slot];
            if (chip.eg_sl[0] == 15)
            {
                chip.eg_sl[0] = 31;
            }
            chip.eg_zr[1] = chip.eg_zr[0];
            chip.eg_zr[0] = zr;
            chip.eg_rate[1] = chip.eg_rate[0];
            chip.eg_rate[0] = rate;
            chip.eg_ratemax[1] = chip.eg_ratemax[0];
            chip.eg_ratemax[0] = (rate >> 1) == 31;
            ams = (byte)(chip.sl_am_e[slot] ? chip.ch_ams[chan] : 0);
            chip.eg_am = ams switch
            {
                1 => (ushort)(chip.lfo_am_lock << 0),
                2 => (ushort)(chip.lfo_am_lock << 1),
                3 => (ushort)(chip.lfo_am_lock << 2),
                _ => 0,
            };
        }

        protected static void EnvelopePhase3(ref ChipState chip)
        {
            uint slot = (chip.cycles + 31) & 31;

            chip.eg_shift = (byte)((chip.eg_timershift_lock + (chip.eg_rate[0] >> 2)) & 15);
            chip.eg_inchi = (byte)eg_stephi[chip.eg_rate[0] & 3, chip.eg_timer_lock & 3];

            chip.eg_outtemp[1] = chip.eg_outtemp[0];
            chip.eg_outtemp[0] = (ushort)(chip.eg_level[slot] + chip.eg_am);
            if ((chip.eg_outtemp[0] & 1024) != 0)
            {
                chip.eg_outtemp[0] = 1023;
            }
        }

        protected static void EnvelopePhase4(ref ChipState chip)
        {
            uint slot = (chip.cycles + 30) & 31;
            byte inc = 0;
            bool kon, eg_off, eg_zero, slreach;
            
            if ((chip.eg_clock & 2) != 0)
            {
                if (chip.eg_rate[1] >= 48)
                {
                    inc = (byte)(chip.eg_inchi + (chip.eg_rate[1] >> 2) - 11);
                    if (inc > 4)
                    {
                        inc = 4;
                    }
                }
                else if (!chip.eg_zr[1])
                {
                    switch (chip.eg_shift)
                    {
                        case 12:
                            inc = (byte)(chip.eg_rate[1] != 0 ? 1u : 0);
                            break;
                        case 13:
                            inc = (byte)((chip.eg_rate[1] >> 1) & 1);
                            break;
                        case 14:
                            inc = (byte)(chip.eg_rate[1] & 1);
                            break;
                    }
                }
            }
            chip.eg_inc = inc;

            kon = chip.kon[slot] && !chip.kon2[slot];
            chip.pg_reset[slot] = kon;
            chip.eg_instantattack = chip.eg_ratemax[1] && (kon || !chip.eg_ratemax[1]);

            eg_off = (chip.eg_level[slot] & 0x3f0) == 0x3f0;
            slreach = (chip.eg_level[slot] >> 4) == (chip.eg_sl[1] << 1);
            eg_zero = chip.eg_level[slot] == 0;

            chip.eg_mute = eg_off && chip.eg_state[slot] != EGNum.attack && !kon;
            chip.eg_inclinear = false;

            if (!kon && !eg_off)
            {
                switch (chip.eg_state[slot])
                {
                    case EGNum.decay:
                        if (!slreach)
                            chip.eg_inclinear = true;
                        break;
                    case EGNum.sustain:
                    case EGNum.release:
                        chip.eg_inclinear = true;
                        break;
                }
            }
            chip.eg_incattack = chip.eg_state[slot] == EGNum.attack && !chip.eg_ratemax[1] && chip.kon[slot] && !eg_zero;


            // Update state
            if (kon)
            {
                chip.eg_state[slot] = EGNum.attack;
            }
            else if (!chip.kon[slot])
            {
                chip.eg_state[slot] = EGNum.release;
            }
            else
            {
                switch (chip.eg_state[slot])
                {
                    case EGNum.attack:
                        if (eg_zero)
                        {
                            chip.eg_state[slot] = EGNum.decay;
                        }
                        break;
                    case EGNum.decay:
                        if (eg_off)
                        {
                            chip.eg_state[slot] = EGNum.release;
                        }
                        else if (slreach)
                        {
                            chip.eg_state[slot] = EGNum.sustain;
                        }
                        break;
                    case EGNum.sustain:
                        if (eg_off)
                        {
                            chip.eg_state[slot] = EGNum.release;
                        }
                        break;
                    case EGNum.release:
                        break;
                }
            }

            if (chip.ic)
            {
                chip.eg_state[slot] = EGNum.release;
            }
        }

        protected static void EnvelopePhase5(ref ChipState chip)
        {
            uint slot = (chip.cycles + 29) & 31;
            uint level = chip.eg_level[slot];
            uint step = 0;

            if (chip.eg_instantattack)
            {
                level = 0;
            }
            if (chip.eg_mute || chip.ic)
            {
                level = 0x3ff;
            }
            if (chip.eg_inc != 0)
            {
                if (chip.eg_inclinear)
                {
                    step |= (uint)(1 << (chip.eg_inc - 1));
                }
                if (chip.eg_incattack)
                {
                    step |= (uint)(((~chip.eg_level[slot]) << chip.eg_inc) >> 5);
                }
            }
            level += step;
            chip.eg_level[slot] = (ushort)level;

            chip.eg_out[0] = chip.eg_outtemp[1];
            if (chip.opp)
                chip.eg_out[0] += chip.eg_tl_opp;
            else
                chip.eg_out[0] += (ushort)(chip.eg_tl[2] << 3);
            if ((chip.eg_out[0] & 1024) != 0)
            {
                chip.eg_out[0] = 1023;
            }

            if (chip.eg_test)
            {
                chip.eg_out[0] = 0;
            }

            chip.eg_test = chip.mode_test[5];
        }

        protected static void EnvelopePhase6(ref ChipState chip)
        {
            uint slot = (chip.cycles + 28) & 31;

            chip.eg_serial_bit = ((chip.eg_serial >> 9) & 1) != 0;
            if (chip.cycles == 3)
            {
                chip.eg_serial = (uint)(chip.eg_out[0] ^ 1023);
            }
            else
            {
                chip.eg_serial <<= 1;
            }

            chip.eg_out[1] = chip.eg_out[0];
        }

        protected static void EnvelopeClock(ref ChipState chip)
        {
            chip.eg_clock <<= 1;
            if ((chip.eg_clockcnt & 2) != 0 || chip.mode_test[0])
            {
                chip.eg_clock |= 1;
            }
            if (chip.ic || (chip.cycles == 31 && (chip.eg_clockcnt & 2) != 0))
            {
                chip.eg_clockcnt = 0;
            }
            else if (chip.cycles == 31)
            {
                chip.eg_clockcnt++;
            }
        }

        protected static void EnvelopeTimer(ref ChipState chip)
        {
            uint cycle = (chip.cycles + 31) & 15;
            uint cycle2;
            bool inc = ((chip.cycles + 31) & 31) < 16 && (chip.eg_clock & 1) != 0 && (cycle == 0 || chip.eg_timercarry);
            byte timerbit = (byte)((chip.eg_timer >> (int)cycle) & 1);
            byte sum = (byte)(timerbit + (inc ? 1u : 0));
            byte sum0 = (byte)(((sum & 1) == 1) && !chip.ic ? 1u : 0);

            chip.eg_timercarry = (sum >> 1) != 0;
            //chip.eg_timer = (uint)((chip.eg_timer & (~(1 << (int)cycle))) | (sum0 << (int)cycle));
            chip.eg_timer = (chip.eg_timer & (~(1u << (int)cycle))) | ((uint)sum0 << (int)cycle);

            cycle2 = (chip.cycles + 30) & 15;

            chip.eg_timer2 <<= 1;
            if ((chip.eg_timer & (1 << (int)cycle2)) != 0 && !chip.eg_timerbstop)
            {
                chip.eg_timer2 |= 1;
            }

            if ((chip.eg_timer & (1 << (int)cycle2)) != 0)
            {
                chip.eg_timerbstop = true;
            }

            if (cycle == 0 || chip.ic2)
            {
                chip.eg_timerbstop = false;
            }

            if (chip.cycles == 1 && (chip.eg_clock & 1) != 0)
            {
                chip.eg_timershift_lock = 0;
                if ((chip.eg_timer2 & (8 + 32 + 128 + 512 + 2048 + 8192 + 32768)) != 0)
                {
                    chip.eg_timershift_lock |= 1;
                }
                if ((chip.eg_timer2 & (4 + 32 + 64 + 512 + 1024 + 8192 + 16384)) != 0)
                {
                    chip.eg_timershift_lock |= 2;
                }
                if ((chip.eg_timer2 & (4 + 8 + 16 + 512 + 1024 + 2048 + 4096)) != 0)
                {
                    chip.eg_timershift_lock |= 4;
                }
                if ((chip.eg_timer2 & (4 + 8 + 16 + 32 + 64 + 128 + 256)) != 0)
                {
                    chip.eg_timershift_lock |= 8;
                }
                chip.eg_timer_lock = (byte)chip.eg_timer;
            }
        }

        protected static void OperatorPhase1(ref ChipState chip)
        {
            uint slot = chip.cycles;
            short mod = chip.op_mod[2];

            chip.op_phase_in = (ushort)(chip.pg_phase[slot] >> 10);
            if ((chip.op_fbshift & 8) != 0)
            {
                if (chip.op_fb[1] == 0)
                {
                    mod = 0;
                }
                else
                {
                    mod = (short)(mod >> (9 - chip.op_fb[1]));
                }
            }
            chip.op_mod_in = (ushort)mod;
        }

        protected static void OperatorPhase2(ref ChipState chip)
        {
            uint slot = (chip.cycles + 31) & 31;

            chip.op_phase = (ushort)((chip.op_phase_in + chip.op_mod_in) & 1023);
        }

        protected static void OperatorPhase3(ref ChipState chip)
        {
            uint slot = (chip.cycles + 30) & 31;
            ushort phase = (ushort)(chip.op_phase & 255);

            if ((chip.op_phase & 256) != 0)
            {
                phase ^= 255;
            }
            chip.op_logsin[0] = logsinrom[phase];
            chip.op_sign <<= 1;
            chip.op_sign |= (uint)((chip.op_phase >> 9) & 1);
        }

        protected static void OperatorPhase4(ref ChipState chip)
        {
            uint slot = (chip.cycles + 29) & 31;

            chip.op_logsin[1] = chip.op_logsin[0];
        }

        protected static void OperatorPhase5(ref ChipState chip)
        {
            uint slot = (chip.cycles + 28) & 31;

            chip.op_logsin[2] = chip.op_logsin[1];
        }

        protected static void OperatorPhase6(ref ChipState chip)
        {
            uint slot = (chip.cycles + 27) & 31;

            chip.op_atten = (ushort)(chip.op_logsin[2] + (chip.eg_out[1] << 2));
            if ((chip.op_atten & 4096) != 0)
            {
                chip.op_atten = 4095;
            }
        }

        protected static void OperatorPhase7(ref ChipState chip)
        {
            uint slot = (chip.cycles + 26) & 31;

            chip.op_exp[0] = exprom[chip.op_atten & 255];
            chip.op_pow[0] = (byte)(chip.op_atten >> 8);
        }

        protected static void OperatorPhase8(ref ChipState chip)
        {
            uint slot = (chip.cycles + 25) & 31;

            chip.op_exp[1] = chip.op_exp[0];
            chip.op_pow[1] = chip.op_pow[0];
        }

        protected static void OperatorPhase9(ref ChipState chip)
        {
            uint slot = (chip.cycles + 24) & 31;
            short out0 = (short)((chip.op_exp[1] << 2) >> chip.op_pow[1]);

            if (!chip.opp && chip.mode_test[4])
            {
                out0 |= 0x2000;
            }
            chip.op_out[0] = out0;
        }

        protected static void OperatorPhase10(ref ChipState chip)
        {
            uint slot = (chip.cycles + 23) & 31;
            short out0 = chip.op_out[0];

            if ((chip.op_sign & 64) != 0)
            {
                out0 ^= 0x3fff;
                out0 = (short)((out0 + 1) & 0x3fff);
            }
            out0 <<= 2; out0 >>= 2;
            chip.op_out[1] = out0;
        }

        protected static void OperatorPhase11(ref ChipState chip)
        {
            uint slot = (chip.cycles + 22) & 31;
            chip.op_out[2] = chip.op_out[1];
        }

        protected static void OperatorPhase12(ref ChipState chip)
        {
            uint slot = (chip.cycles + 21) & 31;
            chip.op_out[3] = chip.op_out[2];
        }

        protected static void OperatorPhase13(ref ChipState chip)
        {
            uint slot = (chip.cycles + 20) & 31;
            uint channel = slot & 7;

            chip.op_out[4] = chip.op_out[3];
            chip.op_connect = chip.ch_connect[channel];
            
            if (chip.opp)
            {
                chip.op_opp_rl = chip.ch_rl[channel];
                chip.op_opp_fb[2] = chip.op_opp_fb[1];
                chip.op_opp_fb[1] = chip.op_opp_fb[0];
                chip.op_opp_fb[0] = chip.ch_fb[channel];
            }
        }

        protected static void OperatorPhase14(ref ChipState chip)
        {
            uint slot = (chip.cycles + 19) & 31;
            uint channel = slot & 7;
            byte rl;

            chip.op_mix = chip.op_out[5] = chip.op_out[4];
            chip.op_fbupdate = (chip.op_counter == 0);
            chip.op_c1update = (chip.op_counter == 2);
            chip.op_fbshift <<= 1;
            chip.op_fbshift |= (byte)((chip.op_counter == 2) ? 1u : 0);

            chip.op_modtable[0] = fm_algorithm[(chip.op_counter + 2) & 3, 0, chip.op_connect] == 1;
            chip.op_modtable[1] = fm_algorithm[(chip.op_counter + 2) & 3, 1, chip.op_connect] == 1;
            chip.op_modtable[2] = fm_algorithm[(chip.op_counter + 2) & 3, 2, chip.op_connect] == 1;
            chip.op_modtable[3] = fm_algorithm[(chip.op_counter + 2) & 3, 3, chip.op_connect] == 1;
            chip.op_modtable[4] = fm_algorithm[(chip.op_counter + 2) & 3, 4, chip.op_connect] == 1;
            
            rl = chip.opp ? chip.op_opp_rl : chip.ch_rl[channel];
            chip.op_mixl = (byte)((fm_algorithm[chip.op_counter, 5, chip.op_connect] != 0) && (rl & 1) != 0 ? 1u : 0);
            chip.op_mixr = (byte)((fm_algorithm[chip.op_counter, 5, chip.op_connect] != 0) && (rl & 2) != 0 ? 1u : 0);

            // Muting
            if (chip.mute[channel])
            {
                chip.op_mixl = chip.op_mixr = 0;
            }
        }

        protected static void OperatorPhase15(ref ChipState chip)
        {
            uint slot = (chip.cycles + 18) & 31;
            short mod, mod1 = 0, mod2 = 0;

            if (chip.op_modtable[0])
            {
                mod2 |= chip.op_m1[slot & 7, 0];
            }
            if (chip.op_modtable[1])
            {
                mod1 |= chip.op_m1[slot & 7, 1];
            }
            if (chip.op_modtable[2])
            {
                mod1 |= chip.op_c1[slot & 7];
            }
            if (chip.op_modtable[3])
            {
                mod2 |= chip.op_out[5];
            }
            if (chip.op_modtable[4])
            {
                mod1 |= chip.op_out[5];
            }
            
            mod = (short)((mod1 + mod2) >> 1);
            chip.op_mod[0] = mod;
            
            if (chip.op_fbupdate)
            {
                chip.op_m1[slot & 7, 1] = chip.op_m1[slot & 7, 0];
                chip.op_m1[slot & 7, 0] = chip.op_out[5];
            }
            if (chip.op_c1update)
            {
                chip.op_c1[slot & 7] = chip.op_out[5];
            }
        }

        protected static void OperatorPhase16(ref ChipState chip)
        {
            uint slot = (chip.cycles + 17) & 31;
            // hack
            chip.op_mod[2] = chip.op_mod[1];
            chip.op_fb[1] = chip.op_fb[0];

            chip.op_mod[1] = chip.op_mod[0];
            chip.op_fb[0] = chip.opp ? chip.op_opp_fb[2] : chip.ch_fb[slot & 7];
        }

        protected static void OperatorCounter(ref ChipState chip)
        {
            if ((chip.cycles & 7) == 4)
            {
                chip.op_counter++;
            }
            if (chip.cycles == 12)
            {
                chip.op_counter = 0;
            }
        }

        protected static void Mixer2(ref ChipState chip)
        {
            uint cycles = (chip.cycles + 30) & 31;
            bool bit;
            byte top, ex;

            if (cycles < 16)
            {
                bit = (chip.mix_serial[0] & 1) != 0;
            }
            else
            {
                bit = (chip.mix_serial[1] & 1) != 0;
            }
            if ((chip.cycles & 15) == 1)
            {
                chip.mix_sign_lock = bit ^ true;
                chip.mix_top_bits_lock = (chip.mix_bits >> 15) & 63;
            }
            if ((chip.cycles & 15) == 7)
            {
                top = (byte)chip.mix_top_bits_lock;
                if (chip.mix_sign_lock)
                {
                    top ^= 63;
                }
                if ((top & 32) != 0)
                {
                    ex = 7;
                }
                else if ((top & 16) != 0)
                {
                    ex = 6;
                }
                else if ((top & 8) != 0)
                {
                    ex = 5;
                }
                else if ((top & 4) != 0)
                {
                    ex = 4;
                }
                else if ((top & 2) != 0)
                {
                    ex = 3;
                }
                else if ((top & 1) != 0)
                {
                    ex = 2;
                }
                else
                {
                    ex = 1;
                }
                chip.mix_sign_lock2 = chip.mix_sign_lock;
                chip.mix_exp_lock = ex;
            }
            chip.mix_out_bit <<= 1;
            switch (chip.cycles & 15)
            {
                case 0:
                    chip.mix_out_bit |= (byte)((chip.mix_sign_lock2 ^ true) ? 1u : 0);
                    break;
                case 1:
                    chip.mix_out_bit |= (byte)((chip.mix_exp_lock >> 0) & 1);
                    break;
                case 2:
                    chip.mix_out_bit |= (byte)((chip.mix_exp_lock >> 1) & 1);
                    break;
                case 3:
                    chip.mix_out_bit |= (byte)((chip.mix_exp_lock >> 2) & 1);
                    break;
                default:
                    if ((chip.mix_exp_lock != 0))
                    {
                        chip.mix_out_bit |= (byte)((chip.mix_bits >> (chip.mix_exp_lock - 1)) & 1);
                    }
                    break;
            }
            chip.mix_bits >>= 1;
            chip.mix_bits |= (bit ? 1u : 0) << 20;
        }

        protected static void Output(ref ChipState chip)
        {
            uint slot = (chip.cycles + 27) & 31;

            chip.smp_so = (chip.mix_out_bit & 1) != 0;
            chip.smp_sh1 = (slot & 24) == 8 && !chip.ic;
            chip.smp_sh2 = (slot & 24) == 24 && !chip.ic;
        }

        protected static void DAC(ref ChipState chip)
        {
            int exp, mant;

            if (chip.dac_osh1 && !chip.smp_sh1)
            {
                exp = (chip.dac_bits >> 10) & 7;
                mant = (chip.dac_bits >> 0) & 1023;
                mant -= 512;
                chip.dac_output[1] = (mant << exp) >> 1;
            }
            if (chip.dac_osh2 && !chip.smp_sh2)
            {
                exp = (chip.dac_bits >> 10) & 7;
                mant = (chip.dac_bits >> 0) & 1023;
                mant -= 512;
                chip.dac_output[0] = (mant << exp) >> 1;
            }
            chip.dac_bits >>= 1;
            chip.dac_bits |= (ushort)((chip.smp_so ? 1u : 0) << 12);
            chip.dac_osh1 = chip.smp_sh1;
            chip.dac_osh2 = chip.smp_sh2;
        }

        protected static void Mixer(ref ChipState chip)
        {
            uint slot = (chip.cycles + 18) & 31;
            uint channel = (slot & 7);
            
            // Right channel
            chip.mix_serial[1] >>= 1;
            if (chip.cycles == 13)
            {
                chip.mix_serial[1] |= (uint)((chip.mix[1] & 1023) << 4);
            }
            if (chip.cycles == 14)
            {
                chip.mix_serial[1] |= (uint)(((chip.mix2[1] >> 10) & 31) << 13);
                chip.mix_serial[1] |= (uint)((((chip.mix2[1] >> 17) & 1) ^ 1) << 18);
                chip.mix_clamp_low[1] = false;
                chip.mix_clamp_high[1] = false;
                
                switch ((chip.mix2[1] >> 15) & 7)
                {
                    case 0:
                    default:
                        break;
                    case 1:
                        chip.mix_clamp_high[1] = true;
                        break;
                    case 2:
                        chip.mix_clamp_high[1] = true;
                        break;
                    case 3:
                        chip.mix_clamp_high[1] = true;
                        break;
                    case 4:
                        chip.mix_clamp_low[1] = true;
                        break;
                    case 5:
                        chip.mix_clamp_low[1] = true;
                        break;
                    case 6:
                        chip.mix_clamp_low[1] = true;
                        break;
                    case 7:
                        break;
                }
            }
            if (chip.mix_clamp_low[1])
            {
                chip.mix_serial[1] = (uint)(chip.mix_serial[1] & ~2);
            }
            if (chip.mix_clamp_high[1])
            {
                chip.mix_serial[1] |= 2;
            }
            
            // Left channel
            chip.mix_serial[0] >>= 1;
            if (chip.cycles == 29)
            {
                chip.mix_serial[0] |= (uint)((chip.mix[0] & 1023) << 4);
            }
            
            if (chip.cycles == 30)
            {
                chip.mix_serial[0] |= (uint)(((chip.mix2[0] >> 10) & 31) << 13);
                chip.mix_serial[0] |= (uint)((((chip.mix2[0] >> 17) & 1) ^ 1) << 18);
                chip.mix_clamp_low[0] = false;
                chip.mix_clamp_high[0] = false;

                switch ((chip.mix2[0] >> 15) & 7)
                {
                    case 0:
                    default:
                        break;
                    case 1:
                        chip.mix_clamp_high[0] = true;
                        break;
                    case 2:
                        chip.mix_clamp_high[0] = true;
                        break;
                    case 3:
                        chip.mix_clamp_high[0] = true;
                        break;
                    case 4:
                        chip.mix_clamp_low[0] = true;
                        break;
                    case 5:
                        chip.mix_clamp_low[0] = true;
                        break;
                    case 6:
                        chip.mix_clamp_low[0] = true;
                        break;
                    case 7:
                        break;
                }
            }
            
            if (chip.mix_clamp_low[0])
            {
                chip.mix_serial[0] = (uint)(chip.mix_serial[0] & ~2);
            }
            if (chip.mix_clamp_high[0])
            {
                chip.mix_serial[0] |= 2;
            }
            
            chip.mix2[0] = chip.mix[0];
            chip.mix2[1] = chip.mix[1];
            
            if (chip.cycles == 13)
            {
                chip.mix[1] = 0;
            }
            if (chip.cycles == 29)
            {
                chip.mix[0] = 0;
            }
            
            chip.mix[0] += chip.op_mix * chip.op_mixl;
            chip.mix[1] += chip.op_mix * chip.op_mixr;
        }

        protected static void Noise(ref ChipState chip)
        {
            bool noise_step = chip.ic || chip.noise_update;
            uint bit = 0;

            if (noise_step)
            {
                if (!chip.ic)
                {
                    bool rst = (chip.noise_lfsr & 0xffff) == 0 && chip.noise_bit == 0;
                    bool xr = (((chip.noise_lfsr >> 2) & 1) ^ chip.noise_bit) != 0;
                    bit = (rst | xr) ? 1u : 0;
                }
                chip.noise_bit = (byte)(chip.noise_lfsr & 1);
            }
            else
                bit = chip.noise_lfsr & 1;
                chip.noise_lfsr >>= 1;
                chip.noise_lfsr |= bit << 15;
        }

        protected static void NoiseTimer(ref ChipState chip)
        {
            uint timer = chip.noise_timer;

            chip.noise_update = chip.noise_timer_of;

            if ((chip.cycles & 15) == 15)
            {
                timer++;
                timer &= 31;
            }
            if (chip.ic || (chip.noise_timer_of && ((chip.cycles & 15) == 15)))
            {
                timer = 0;
            }

            chip.noise_timer_of = chip.noise_timer == (chip.noise_freq ^ 31);
            chip.noise_timer = timer;
        }

        protected static void DoTimerA(ref ChipState chip)
        {
            ushort value = chip.timer_a_val;

            value += (ushort)(chip.timer_a_inc ? 1u: 0);
            chip.timer_a_of = ((value >> 10) & 1) != 0;

            if (chip.timer_a_do_reset)
            {
                value = 0;
            }
            if (chip.timer_a_do_load)
            {
                value = chip.timer_a_reg;
            }

            chip.timer_a_val = (ushort)(value & 1023);
        }

        protected static void DoTimerA2(ref ChipState chip)
        {
            if (chip.cycles == 1)
            {
                chip.timer_a_load = chip.timer_loada;
            }
            
            chip.timer_a_inc = chip.mode_test[2] || (chip.timer_a_load && chip.cycles == 0);
            chip.timer_a_do_load = chip.timer_a_of || (chip.timer_a_load && chip.timer_a_temp);
            chip.timer_a_do_reset = chip.timer_a_temp;
            chip.timer_a_temp = !chip.timer_a_load;
            
            if (chip.timer_reseta || chip.ic)
            {
                chip.timer_a_status = false;
            }
            else
            {
                chip.timer_a_status = chip.timer_a_status || (chip.timer_irqa && chip.timer_a_of);
            }
            chip.timer_reseta = false;
        }

        protected static void DoTimerB(ref ChipState chip)
        {
            ushort value = chip.timer_b_val;

            value += (ushort)(chip.timer_b_inc ? 1u : 0);
            chip.timer_b_of = ((value >> 8) & 1) != 0;
            
            if (chip.timer_b_do_reset)
            {
                value = 0;
            }
            if (chip.timer_b_do_load)
            {
                value = chip.timer_b_reg;
            }

            chip.timer_b_val = (ushort)(value & 255);

            if (chip.cycles == 0)
            {
                chip.timer_b_sub++;
            }

            if (chip.opp)
            {
                chip.timer_b_sub_of = ((chip.timer_b_sub >> 5) & 1) != 0;
                chip.timer_b_sub &= 31;
            }
            else
            {
                chip.timer_b_sub_of = ((chip.timer_b_sub >> 4) & 1) != 0;
                chip.timer_b_sub &= 15;
            }
            if (chip.ic)
            {
                chip.timer_b_sub = 0;
            }
        }

        protected static void DoTimerB2(ref ChipState chip)
        {
            chip.timer_b_inc = chip.mode_test[2] || (chip.timer_loadb && chip.timer_b_sub_of);
            chip.timer_b_do_load = chip.timer_b_of || (chip.timer_loadb && chip.timer_b_temp);
            chip.timer_b_do_reset = chip.timer_b_temp;
            chip.timer_b_temp = !chip.timer_loadb;
            
            if (chip.timer_resetb || chip.ic)
            {
                chip.timer_b_status = false;
            }
            else
            {
                chip.timer_b_status |= chip.timer_irqb && chip.timer_b_of;
            }
            chip.timer_resetb = false;
        }

        protected static void DoTimerIRQ(ref ChipState chip)
        {
            chip.timer_irq = chip.timer_a_status || chip.timer_b_status;
        }

        protected static void DoLFOMult(ref ChipState chip)
        {
            bool ampm_sel = (chip.lfo_bit_counter & 8) != 0;
            byte dp = ampm_sel ? chip.lfo_pmd : chip.lfo_amd;
            bool bit = false, b1, b2;
            byte sum;

            chip.lfo_out2_b = chip.lfo_out2;

            switch (chip.lfo_bit_counter & 7)
            {
                case 0:
                    bit = (dp & 64) != 0 && (chip.lfo_out1 & 64) != 0;
                    break;
                case 1:
                    bit = (dp & 32) != 0 && (chip.lfo_out1 & 32) != 0;
                    break;
                case 2:
                    bit = (dp & 16) != 0 && (chip.lfo_out1 & 16) != 0;
                    break;
                case 3:
                    bit = (dp & 8) != 0 && (chip.lfo_out1 & 8) != 0;
                    break;
                case 4:
                    bit = (dp & 4) != 0 && (chip.lfo_out1 & 4) != 0;
                    break;
                case 5:
                    bit = (dp & 2) != 0 && (chip.lfo_out1 & 2) != 0;
                    break;
                case 6:
                    bit = (dp & 1) != 0 && (chip.lfo_out1 & 1) != 0;
                    break;
            }

            b1 = (chip.lfo_out2 & 1) != 0;
            if ((chip.lfo_bit_counter & 7) == 0)
            {
                b1 = false;
            }
            b2 = chip.lfo_mult_carry;
            if ((chip.cycles & 15) == 15)
            {
                b2 = false;
            }
            
            sum = (byte)((bit ? 1u : 0) + (b1 ? 1u : 0) + (b2 ? 1u : 0));
            chip.lfo_out2 >>= 1;
            chip.lfo_out2 |= (uint)((sum & 1) << 15);
            chip.lfo_mult_carry = (sum >> 1) != 0;
        }

        protected static void DoLFO1(ref ChipState chip)
        {
            ushort counter2 = chip.lfo_counter2;
            bool of_old = chip.lfo_counter2_of;
            bool lfo_bit, carry, noise;
            byte sum;
            bool bb, sb, x, w2, w3, mulm, mb;
            bool lfo_pm_sign;
            bool ampm_sel = (chip.lfo_bit_counter & 8) != 0;

            counter2 += (ushort)((chip.lfo_counter1_of1 & 2) != 0 || chip.mode_test[3] ? 1u : 0);
            chip.lfo_counter2_of = ((counter2 >> 15) & 1) != 0;

            if (chip.ic)
            {
                counter2 = 0;
            }
            if (chip.lfo_counter2_load)
            {
                counter2 = lfo_counter2_table[chip.lfo_freq_hi];
            }
            
            chip.lfo_counter2 = (ushort)(counter2 & 32767);
            chip.lfo_counter2_load = chip.lfo_frq_update || of_old;
            chip.lfo_frq_update = false;
            
            if ((chip.cycles & 15) == 12)
            {
                chip.lfo_counter1++;
            }
            
            chip.lfo_counter1_of1 <<= 1;
            chip.lfo_counter1_of1 |= (byte)((chip.lfo_counter1 >> 4) & 1);
            chip.lfo_counter1 &= 15;
            
            if (chip.ic)
            {
                chip.lfo_counter1 = 0;
            }

            if ((chip.cycles & 15) == 5)
            {
                chip.lfo_counter2_of_lock2 = chip.lfo_counter2_of_lock;
            }

            chip.lfo_counter3 += (ushort)(chip.lfo_counter3_clock ? 1u : 0);
            if (chip.ic)
            {
                chip.lfo_counter3 = 0;
            }

            chip.lfo_counter3_clock = (chip.cycles & 15) == 13 && chip.lfo_counter2_of_lock2;

            if ((chip.cycles & 15) == 15 && (chip.lfo_bit_counter & 7) == 0)
            {
                chip.lfo_trig_sign = (chip.lfo_val & 0x80) != 0;
                chip.lfo_saw_sign = (chip.lfo_val & 0x100) != 0;
            }

            lfo_pm_sign = chip.lfo_wave == 2 ? chip.lfo_trig_sign : chip.lfo_saw_sign;


            x = chip.lfo_clock && chip.lfo_wave != 3 && (chip.cycles & 15) == 15;
            w2 = chip.lfo_wave == 2 && x;
            w3 = !chip.ic && !chip.mode_test[1] && (!chip.lfo_clock_lock || chip.lfo_wave != 3) && (chip.lfo_val & 0x8000) != 0;

            mulm = ((chip.cycles + 1) & 15) < 8;

            bb = ampm_sel ? chip.lfo_saw_sign : (chip.lfo_wave != 2 || !chip.lfo_trig_sign);
            bb ^= w3;

            sb = ampm_sel ? ((chip.cycles & 15) == 6) : !chip.lfo_saw_sign;

            mb = mulm && (chip.lfo_wave == 1 ? sb : bb);

            chip.lfo_out1 <<= 1;
            chip.lfo_out1 |= mb ? 1u : 0;

            carry = x || ((chip.cycles & 15) != 15 && chip.lfo_val_carry != 0 && chip.lfo_wave != 3);
            sum = (byte)((carry ? 1u : 0) + (w2 ? 1u : 0) + (w3 ? 1u : 0));
            lfo_bit = (sum & 1) != 0;
            
            if (chip.lfo_wave == 3 && chip.lfo_clock_lock)
            {
                noise = (chip.noise_lfsr & 1) != 0;
                lfo_bit |= noise;
            }
            chip.lfo_val_carry = (byte)(sum >> 1);
            chip.lfo_val <<= 1;
            chip.lfo_val |= lfo_bit ? 1u : 0;


            if ((chip.cycles & 15) == 15 && (chip.lfo_bit_counter & 7) == 7)
            {
                if (ampm_sel)
                {
                    chip.lfo_pm_lock = (byte)((chip.lfo_out2_b >> 8) & 255);
                    chip.lfo_pm_lock ^= (byte)((lfo_pm_sign ? 1u : 0) << 7);
                }
                else
                {
                    chip.lfo_am_lock = (byte)((chip.lfo_out2_b >> 8) & 255);
                }
            }

            if ((chip.cycles & 15) == 14)
            {
                chip.lfo_bit_counter++;
            }
            if ((chip.cycles & 15) != 12 && chip.lfo_counter1_of2)
            {
                chip.lfo_bit_counter = 0;
            }
            chip.lfo_counter1_of2 = chip.lfo_counter1 == 2;
        }

        protected static void DoLFO2(ref ChipState chip)
        {
            chip.lfo_clock_test = chip.lfo_clock;
            chip.lfo_clock = (chip.lfo_counter2_of || chip.lfo_test || chip.lfo_counter3_step);
            
            if ((chip.cycles & 15) == 14)
            {
                chip.lfo_counter2_of_lock = chip.lfo_counter2_of;
                chip.lfo_clock_lock = chip.lfo_clock;
            }
            
            chip.lfo_counter3_step = false;
            
            if (chip.lfo_counter3_clock)
            {
                if ((chip.lfo_counter3 & 1) == 0)
                {
                    chip.lfo_counter3_step = (chip.lfo_freq_lo & 8) != 0;
                }
                else if ((chip.lfo_counter3 & 2) == 0)
                {
                    chip.lfo_counter3_step = (chip.lfo_freq_lo & 4) != 0;
                }
                else if ((chip.lfo_counter3 & 4) == 0)
                {
                    chip.lfo_counter3_step = (chip.lfo_freq_lo & 2) != 0;
                }
                else if ((chip.lfo_counter3 & 8) == 0)
                {
                    chip.lfo_counter3_step = (chip.lfo_freq_lo & 1) != 0;
                }
            }
            chip.lfo_test = chip.mode_test[2];
        }

        protected static void CSM(ref ChipState chip)
        {
            chip.kon_csm = chip.kon_csm_lock;
            if (chip.cycles == 1)
            {
                chip.kon_csm_lock = chip.timer_a_do_load && chip.mode_csm;
            }
        }

        protected static void NoiseChannel(ref ChipState chip)
        {
            chip.nc_active |= chip.eg_serial_bit;
            if (chip.cycles == 13)
            {
                chip.nc_active = false;
            }
            chip.nc_out <<= 1;
            chip.nc_out |= (ushort)((chip.nc_sign ^ chip.eg_serial_bit) ? 1u : 0);
            chip.nc_sign = !chip.nc_sign_lock;
            
            if (chip.cycles == 12)
            {
                chip.nc_active_lock = chip.nc_active;
                chip.nc_sign_lock2 = chip.nc_active_lock && !chip.nc_sign_lock;
                chip.nc_sign_lock = (chip.noise_lfsr & 1) != 0;

                if (chip.noise_en)
                {
                    if (chip.nc_sign_lock2)
                    {
                        chip.op_mix = (short)(((chip.nc_out & ~1) << 2) | -4089);
                    }
                    else
                    {
                        chip.op_mix = (short)((chip.nc_out & ~1) << 2);
                    }
                }
            }
        }

        protected static void DoIO(ref ChipState chip)
        {
            // Busy
            chip.write_busy_cnt += (byte)(chip.write_busy ? 1u : 0);
            chip.write_busy = ((chip.write_busy_cnt >> 5) == 0 && chip.write_busy && !chip.ic) | chip.write_d_en;
            chip.write_busy_cnt &= 0x1f;
            if (chip.ic)
            {
                chip.write_busy_cnt = 0;
            }
            // Write signal check
            chip.write_a_en = chip.write_a;
            chip.write_d_en = chip.write_d;
            chip.write_a = false;
            chip.write_d = false;
        }

        protected static void DoRegWrite(ref ChipState chip)
        {
            int i;
            uint cycles = chip.opp ? (chip.cycles + 1) & 31 : chip.cycles;
            uint channel = cycles & 7;
            uint slot = cycles;

            if (chip.opp)
            {
                uint channel_d1 = (cycles + 7) & 7;
                uint channel_d4 = (cycles + 4) & 7;
                if (chip.mode_test[4])
                {
                    // Clear registers
                    chip.ch_ramp_div[channel] = 0;
                    chip.ch_rl[channel_d4] = 0;
                    chip.ch_fb[channel_d4] = 0;
                    chip.ch_connect[channel_d4] = 0;
                    chip.ch_kc[channel_d1] = 0;
                    chip.ch_kf[channel_d1] = 0;
                    chip.ch_pms[channel] = 0;
                    chip.ch_ams[channel] = 0;

                    chip.sl_dt1[slot] = 0;
                    chip.sl_mul[slot] = 0;
                    chip.sl_tl[slot] = 0;
                    chip.sl_ks[slot] = 0;
                    chip.sl_ar[slot] = 0;
                    chip.sl_am_e[slot] = false;
                    chip.sl_d1r[slot] = 0;
                    chip.sl_dt2[slot] = 0;
                    chip.sl_d2r[slot] = 0;
                    chip.sl_d1l[slot] = 0;
                    chip.sl_rr[slot] = 0;
                }
                else
                {
                    if ((chip.reg_20_delay & 8) != 0) // RL, FB, CONNECT
                    {
                        chip.ch_rl[channel_d4] = (byte)(chip.reg_data >> 6);
                        chip.ch_fb[channel_d4] = (byte)((chip.reg_data >> 3) & 0x07);
                        chip.ch_connect[channel_d4] = (byte)(chip.reg_data & 0x07);
                    }
                    if (chip.reg_28_delay) // KC
                    {
                        chip.ch_kc[channel_d1] = (byte)(chip.reg_data & 0x7f);
                    }
                    if (chip.reg_30_delay) // KF
                    {
                        chip.ch_kf[channel_d1] = (byte)(chip.reg_data >> 2);
                    }
                    // Register write
                    if (chip.reg_data_ready)
                    {
                        // Channel
                        if (chip.reg_address == channel) // Ramp div
                        {
                            chip.ch_ramp_div[channel] = chip.reg_data;
                        }
                        if (chip.reg_address == (0x38 | channel)) // PMS, AMS
                        {
                            chip.ch_pms[channel] = (byte)((chip.reg_data >> 4) & 0x07);
                            chip.ch_ams[channel] = (byte)(chip.reg_data & 0x03);
                        }
                        // Slot
                        if ((chip.reg_address & 0x1f) == slot)
                        {
                            switch (chip.reg_address & 0xe0)
                            {
                                case 0x40: // DT1, MUL
                                    chip.sl_dt1[slot] = (byte)((chip.reg_data >> 4) & 0x07);
                                    chip.sl_mul[slot] = (byte)(chip.reg_data & 0x0f);
                                    break;
                                case 0x60: // TL
                                    chip.sl_tl[slot] = chip.reg_data;
                                    break;
                                case 0x80: // KS, AR
                                    chip.sl_ks[slot] = (byte)(chip.reg_data >> 6);
                                    chip.sl_ar[slot] = (byte)(chip.reg_data & 0x1f);
                                    break;
                                case 0xa0: // AMS-EN, D1R
                                    chip.sl_am_e[slot] = (chip.reg_data >> 7) != 0;
                                    chip.sl_d1r[slot] = (byte)(chip.reg_data & 0x1f);
                                    break;
                                case 0xc0: // DT2, D2R
                                    chip.sl_dt2[slot] = (byte)(chip.reg_data >> 6);
                                    chip.sl_d2r[slot] = (byte)(chip.reg_data & 0x1f);
                                    break;
                                case 0xe0: // D1L, RR
                                    chip.sl_d1l[slot] = (byte)(chip.reg_data >> 4);
                                    chip.sl_rr[slot] = (byte)(chip.reg_data & 0x0f);
                                    break;
                                default:
                                    break;
                            }
                        }
                    }
                }

                chip.reg_20_delay <<= 1;
                chip.reg_20_delay |= (byte)(chip.reg_data_ready && chip.reg_address == (0x20 | channel) ? 1u : 0);
                chip.reg_28_delay = chip.reg_data_ready && chip.reg_address == (0x28 | channel);
                chip.reg_30_delay = chip.reg_data_ready && chip.reg_address == (0x30 | channel);
            }
            else
            {
                // Register write
                if (chip.reg_data_ready)
                {
                    // Channel
                    if ((chip.reg_address & 0xe7) == (0x20 | channel))
                    {
                        switch (chip.reg_address & 0x18)
                        {
                            case 0x00: // RL, FB, CONNECT
                                chip.ch_rl[channel] = (byte)(chip.reg_data >> 6);
                                chip.ch_fb[channel] = (byte)((chip.reg_data >> 3) & 0x07);
                                chip.ch_connect[channel] = (byte)(chip.reg_data & 0x07);
                                break;
                            case 0x08: // KC
                                chip.ch_kc[channel] = (byte)(chip.reg_data & 0x7f);
                                break;
                            case 0x10: // KF
                                chip.ch_kf[channel] = (byte)(chip.reg_data >> 2);
                                break;
                            case 0x18: // PMS, AMS
                                chip.ch_pms[channel] = (byte)((chip.reg_data >> 4) & 0x07);
                                chip.ch_ams[channel] = (byte)(chip.reg_data & 0x03);
                                break;
                            default:
                                break;
                        }
                    }
                    // Slot
                    if ((chip.reg_address & 0x1f) == slot)
                    {
                        switch (chip.reg_address & 0xe0)
                        {
                            case 0x40: // DT1, MUL
                                chip.sl_dt1[slot] = (byte)((chip.reg_data >> 4) & 0x07);
                                chip.sl_mul[slot] = (byte)(chip.reg_data & 0x0f);
                                break;
                            case 0x60: // TL
                                chip.sl_tl[slot] = (byte)(chip.reg_data & 0x7f);
                                break;
                            case 0x80: // KS, AR
                                chip.sl_ks[slot] = (byte)(chip.reg_data >> 6);
                                chip.sl_ar[slot] = (byte)(chip.reg_data & 0x1f);
                                break;
                            case 0xa0: // AMS-EN, D1R
                                chip.sl_am_e[slot] = (chip.reg_data >> 7) != 0;
                                chip.sl_d1r[slot] = (byte)(chip.reg_data & 0x1f);
                                break;
                            case 0xc0: // DT2, D2R
                                chip.sl_dt2[slot] = (byte)(chip.reg_data >> 6);
                                chip.sl_d2r[slot] = (byte)(chip.reg_data & 0x1f);
                                break;
                            case 0xe0: // D1L, RR
                                chip.sl_d1l[slot] = (byte)(chip.reg_data >> 4);
                                chip.sl_rr[slot] = (byte)(chip.reg_data & 0x0f);
                                break;
                            default:
                                break;
                        }
                    }
                }
            }


            // Mode write
            if (chip.write_d_en)
            {
                if (chip.mode_address == (chip.opp ? 9 : 1))
                {
                    for (i = 0; i < 8; i++)
                    {
                        chip.mode_test[i] = ((chip.write_data >> i) & 0x01) != 0;
                    }
                }
                switch (chip.mode_address)
                {
                    case 0x08:
                        for (i = 0; i < 4; i++)
                        {
                            chip.mode_kon_operator[i] = ((chip.write_data >> (i + 3)) & 0x01) != 0;
                        }
                        chip.mode_kon_channel = (byte)(chip.write_data & 0x07);
                        break;
                    case 0x0f:
                        chip.noise_en = (chip.write_data >> 7) != 0;
                        chip.noise_freq = (byte)(chip.write_data & 0x1f);
                        break;
                    case 0x10:
                        chip.timer_a_reg &= 0x03;
                        chip.timer_a_reg |= (ushort)(chip.write_data << 2);
                        break;
                    case 0x11:
                        chip.timer_a_reg &= 0x3fc;
                        chip.timer_a_reg |= (ushort)(chip.write_data & 0x03);
                        break;
                    case 0x12:
                        chip.timer_b_reg = chip.write_data;
                        break;
                    case 0x14:
                        chip.mode_csm = ((chip.write_data >> 7) & 1) != 0;
                        chip.timer_irqb = ((chip.write_data >> 3) & 1) != 0;
                        chip.timer_irqa = ((chip.write_data >> 2) & 1) != 0;
                        chip.timer_resetb = ((chip.write_data >> 5) & 1) != 0;
                        chip.timer_reseta = ((chip.write_data >> 4) & 1) != 0;
                        chip.timer_loadb = ((chip.write_data >> 1) & 1) != 0;
                        chip.timer_loada = ((chip.write_data >> 0) & 1) != 0;
                        break;
                    case 0x18:
                        chip.lfo_freq_hi = (byte)(chip.write_data >> 4);
                        chip.lfo_freq_lo = (byte)(chip.write_data & 0x0f);
                        chip.lfo_frq_update = true;
                        break;
                    case 0x19:
                        if ((chip.write_data & 0x80) != 0)
                        {
                            chip.lfo_pmd = (byte)(chip.write_data & 0x7f);
                        }
                        else
                        {
                            chip.lfo_amd = chip.write_data;
                        }
                        break;
                    case 0x1b:
                        chip.lfo_wave = (byte)(chip.write_data & 0x03);
                        chip.io_ct1 = ((chip.write_data >> 6) & 0x01) != 0;
                        chip.io_ct2 = (chip.write_data >> 7) != 0;
                        break;
                }
            }

            // Register data write
            chip.reg_data_ready = chip.reg_data_ready && !chip.write_a_en;
            if (chip.reg_address_ready && chip.write_d_en)
            {
                chip.reg_data = chip.write_data;
                chip.reg_data_ready = true;
            }

            // Register address write
            chip.reg_address_ready = chip.reg_address_ready && !chip.write_a_en;
            if (chip.write_a_en && ((chip.write_data & 0xe0) != 0 || (chip.opp && (chip.write_data & 0xf8) == 0)))
            {
                chip.reg_address = chip.write_data;
                chip.reg_address_ready = true;
            }
            if (chip.write_a_en)
            {
                chip.mode_address = chip.write_data;
            }
        }

        protected static void DoIC(ref ChipState chip)
        {
            uint channel = chip.cycles & 7;
            uint slot = chip.cycles;
            
            if (chip.ic)
            {
                if (chip.opp)
                {
                    uint i;
                    for (i = 0; i < 2; i++)
                    {
                        byte ch = (byte)((channel + 4 * i) & 7);
                        chip.ch_ramp_div[ch] = 0;
                        chip.ch_rl[ch] = 0;
                        chip.ch_fb[ch] = 0;
                        chip.ch_connect[ch] = 0;
                        chip.ch_kc[ch] = 0;
                        chip.ch_kf[ch] = 0;
                        chip.ch_pms[ch] = 0;
                        chip.ch_ams[ch] = 0;
                    }
                    for (i = 0; i < 8; i++)
                    {
                        byte sl = (byte)((slot + 4 * i) & 31);
                        chip.sl_dt1[sl] = 0;
                        chip.sl_mul[sl] = 0;
                        chip.sl_tl[sl] = 0;
                        chip.sl_ks[sl] = 0;
                        chip.sl_ar[sl] = 0;
                        chip.sl_am_e[sl] = false;
                        chip.sl_d1r[sl] = 0;
                        chip.sl_dt2[sl] = 0;
                        chip.sl_d2r[sl] = 0;
                        chip.sl_d1l[sl] = 0;
                        chip.sl_rr[sl] = 0;
                    }
                }
                else
                {
                    chip.ch_rl[channel] = 0;
                    chip.ch_fb[channel] = 0;
                    chip.ch_connect[channel] = 0;
                    chip.ch_kc[channel] = 0;
                    chip.ch_kf[channel] = 0;
                    chip.ch_pms[channel] = 0;
                    chip.ch_ams[channel] = 0;

                    chip.sl_dt1[slot] = 0;
                    chip.sl_mul[slot] = 0;
                    chip.sl_tl[slot] = 0;
                    chip.sl_ks[slot] = 0;
                    chip.sl_ar[slot] = 0;
                    chip.sl_am_e[slot] = false;
                    chip.sl_d1r[slot] = 0;
                    chip.sl_dt2[slot] = 0;
                    chip.sl_d2r[slot] = 0;
                    chip.sl_d1l[slot] = 0;
                    chip.sl_rr[slot] = 0;
                }

                chip.timer_a_reg = 0;
                chip.timer_b_reg = 0;
                chip.timer_irqa = false;
                chip.timer_irqb = false;
                chip.timer_loada = false;
                chip.timer_loadb = false;
                chip.mode_csm = false;

                chip.mode_test[0] = false;
                chip.mode_test[1] = false;
                chip.mode_test[2] = false;
                chip.mode_test[3] = false;
                chip.mode_test[4] = false;
                chip.mode_test[5] = false;
                chip.mode_test[6] = false;
                chip.mode_test[7] = false;
                chip.noise_en = false;
                chip.noise_freq = 0;

                chip.mode_kon_channel = 0;
                chip.mode_kon_operator[0] = false;
                chip.mode_kon_operator[1] = false;
                chip.mode_kon_operator[2] = false;
                chip.mode_kon_operator[3] = false;
                chip.mode_kon[(slot + 8) & 31] = false;

                chip.lfo_pmd = 0;
                chip.lfo_amd = 0;
                chip.lfo_wave = 0;
                chip.lfo_freq_hi = 0;
                chip.lfo_freq_lo = 0;

                chip.io_ct1 = false;
                chip.io_ct2 = false;

                chip.reg_address = 0;
                chip.reg_data = 0;
            }
            chip.ic2 = chip.ic;
        }

        protected static void OPP_TLRamp(ref ChipState chip)
        {
            uint slot = chip.cycles;
            uint channel = slot & 7;
            bool step = ((chip.cycles + 1) & 31) < 8;
            uint ramp = chip.ch_ramp_div[channel];

            bool match = ramp == chip.opp_tl_cnt[channel];
            uint tl = chip.sl_tl[slot];

            if (chip.ic || (step && match))
                chip.opp_tl_cnt[channel] = 0;
            else
                chip.opp_tl_cnt[channel] += (byte)(step ? 1u : 0);

            if ((tl & 128) != 0)
            {
                if (match)
                {
                    uint val = (uint)(chip.opp_tl[slot] >> 3);
                    tl &= 127;
                    if (val < tl)
                        chip.opp_tl[slot]++;
                    else if (val > tl)
                        chip.opp_tl[slot]--;
                }
            }
            else
                chip.opp_tl[slot] = (ushort)(tl << 3);
        }

        public static void Clock(ref ChipState chip, ref int?[] output, ref bool? sh1, ref bool? sh2, ref bool? so)
        {
            Output(ref chip);
            DAC(ref chip);
            Mixer2(ref chip);
            Mixer(ref chip);

            OperatorPhase16(ref chip);
            OperatorPhase15(ref chip);
            OperatorPhase14(ref chip);
            OperatorPhase13(ref chip);
            OperatorPhase12(ref chip);
            OperatorPhase11(ref chip);
            OperatorPhase10(ref chip);
            OperatorPhase9(ref chip);
            OperatorPhase8(ref chip);
            OperatorPhase7(ref chip);
            OperatorPhase6(ref chip);
            OperatorPhase5(ref chip);
            OperatorPhase4(ref chip);
            OperatorPhase3(ref chip);
            OperatorPhase2(ref chip);
            OperatorPhase1(ref chip);
            OperatorCounter(ref chip);

            EnvelopeTimer(ref chip);
            EnvelopePhase6(ref chip);
            EnvelopePhase5(ref chip);
            EnvelopePhase4(ref chip);
            EnvelopePhase3(ref chip);
            EnvelopePhase2(ref chip);
            EnvelopePhase1(ref chip);

            if (chip.opp)
                OPP_TLRamp(ref chip);

            PhaseDebug(ref chip);
            PhaseGenerate(ref chip);
            PhaseCalcIncrement(ref chip);
            PhaseCalcFNumBlock(ref chip);

            DoTimerIRQ(ref chip);
            DoTimerA(ref chip);
            DoTimerB(ref chip);
            DoLFOMult(ref chip);
            DoLFO1(ref chip);
            Noise(ref chip);
            KeyOn2(ref chip);
            DoRegWrite(ref chip);
            EnvelopeClock(ref chip);
            NoiseTimer(ref chip);
            KeyOn1(ref chip);
            DoIO(ref chip);
            DoTimerA2(ref chip);
            DoTimerB2(ref chip);
            DoLFO2(ref chip);
            CSM(ref chip);
            NoiseChannel(ref chip);
            DoIC(ref chip);
            
            if (sh1 == true)
            {
                sh1 = chip.smp_sh1;
            }
            if (sh2 == true)
            {
                sh2 = chip.smp_sh2;
            }
            if (so == true)
            {
                so = chip.smp_so;
            }
            if (!(output == null))
            {
                output[0] = chip.dac_output[0];
                output[1] = chip.dac_output[1];
            }
            chip.cycles = (chip.cycles + 1) & 31;
        }

        public static void Write(ref ChipState chip, uint port, byte data)
        {
            chip.write_data = data;
            
            if (chip.ic)
            {
                return;
            }
            if ((port & 0x01) != 0)
            {
                chip.write_d = true;
            }
            else
            {
                chip.write_a = true;
            }
        }

        public static byte Read(ref ChipState chip, uint port)
        {
            ushort testdata;
            if ((port & 1) == 0)
                return 0xff;
            if (chip.mode_test[6])
            {
                //testdata = (ushort)(chip.op_out[5] | (((chip.eg_serial_bit ^ true) ? 1u : 0) << 14) | ((chip.pg_serial & 1) << 15));
                ushort p, q, r;
                p = (ushort)chip.op_out[5];
                q = (ushort)(((chip.eg_serial_bit ^ true) ? 1u : 0) << 14);
                r = (ushort)((chip.pg_serial & 1) << 15);
                testdata = (ushort)(p | q | r);
                
                if (chip.mode_test[7])
                {
                    return (byte)(testdata & 255);
                }
                else
                {
                    return (byte)(testdata >> 8);
                }
            }

            //return (chip.write_busy << 7) | (chip.timer_b_status << 1) | chip.timer_a_status;
            uint a, b, c;
            a = (chip.write_busy ? 1u : 0) << 7;
            b = (chip.timer_b_status ? 1u : 0) << 1;
            c = chip.timer_a_status ? 1u : 0;
            return (byte)(a | b | c);
        }

        public static bool ReadIRQ(ref ChipState chip)
        {
            return chip.timer_irq;
        }

        public static bool ReadCT1(ref ChipState chip)
        {
            if (chip.opp)
            {
                return chip.io_ct2;
            }
            if (chip.mode_test[3])
            {
                return chip.lfo_clock_test;
            }
            return chip.io_ct1;
        }

        public static bool ReadCT2(ref ChipState chip)
        {
            if (chip.opp)
            {
                if (chip.mode_test[3])
                {
                    return chip.lfo_clock_test;
                }
                return chip.io_ct1;
            }
            return chip.io_ct2;
        }

        public static void SetIC(ref ChipState chip, bool ic)
        {
            if (chip.ic != ic)
            {
                chip.ic = ic;
                if (!ic)
                {
                    chip.cycles = 0;
                }
            }
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
            double omega_c = 2 * Math.PI * (cutoffFreq / (chip.clock / 144));
            double cos_omega = Math.Cos(omega_c);
            chip.FILTER_CUTOFF = 2 - cos_omega - Math.Sqrt(Math.Pow(2 - cos_omega, 2) - 1);
            chip.FILTER_CUTOFF_I = 1 - chip.FILTER_CUTOFF;
        }

        /// <summary>
        /// Writes a single data byte to a buffer.
        /// </summary>
        /// <param name="chip">The chip to write to.</param>
        /// <param name="port">The port to write to. Use 0 for an address write and 1 for a data byte write.</param>
        /// <param name="data">The data to write.</param>
        /// <remarks>Use Write to make an immediate write.</remarks>
        public static void WriteBuffered(ref ChipState chip, uint port, byte data)
        {
            ulong time1, time2;
            int?[] buffer = new int?[2];
            bool? sh1 = null, sh2 = null, so = null;
            long skip;

            if ((chip.writebuf[chip.writebuf_last].port & 0x02) != 0)
            {
                Write(ref chip, (uint)(chip.writebuf[chip.writebuf_last].port & 0x01), chip.writebuf[chip.writebuf_last].data);

                chip.writebuf[chip.writebuf_last].port &= 0x01;
                chip.writebuf_cur = (uint)((chip.writebuf_last + 1) % chip.WRITEBUF_SIZE);
                skip = (long)(chip.writebuf[chip.writebuf_last].time - chip.writebuf_samplecnt);
                if (skip < 0)
                    skip = 0;
                chip.writebuf_samplecnt = chip.writebuf[chip.writebuf_last].time;

                while (skip-- > 0)
                {
                    Clock(ref chip, ref buffer, ref sh1, ref sh2, ref so);
                }
            }

            chip.writebuf[chip.writebuf_last].port = (byte)((port & 0x01) | 0x02);
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

        static void GenerateResampled(ref ChipState chip, ref int[] buf)
        {
            uint i, j;
            int?[] buffer = new int?[2];
            //bool mute;
            bool? sh1 = null, sh2 = null, so = null;

            while (chip.samplecnt >= chip.rateratio)
            {
                for (i = 0; i < 2; i++)
                {
                    chip.oldsamples[i] = chip.samples[i];
                }
                chip.samples[0] = chip.samples[1] = 0;

                for (i = 0; i < 32; i++)
                {
                    Clock(ref chip, ref buffer, ref sh1, ref sh2, ref so);

                    if (i == 0)
                    {
                        for (j = 0; j < 2; j++)
                        {
                            if (buffer[j] != 0) {
                                int foo = 0;
                        }
                            chip.samples[j] += (int)buffer[j];
                        }
                    }

                    while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
                    {
                        if ((chip.writebuf[chip.writebuf_cur].port & 0x02) == 0)
                        {
                            break;
                        }
                        chip.writebuf[chip.writebuf_cur].port &= 0x01;

                        Write(ref chip, 
                            chip.writebuf[chip.writebuf_cur].port, 
                            chip.writebuf[chip.writebuf_cur].data);

                        chip.writebuf_cur = (uint)((chip.writebuf_cur + 1) % chip.WRITEBUF_SIZE);
                    }
                    chip.writebuf_samplecnt++;
                }

                //// Low pass filter
                //if (!use_filter)
                //{
                //    for (i = 0; i < 2; i++)
                //    {
                //        chip.samples[i] *= chip.OUTPUT_FACTOR;
                //    }
                //}
                //else
                //{
                //    for (i = 0; i < 2; i++)
                //    {
                //        chip.samples[i] = (int)(chip.oldsamples[i] + chip.FILTER_CUTOFF_I * (chip.samples[i] * chip.OUTPUT_FACTOR_F - chip.oldsamples[i]));
                //    }
                //}

                chip.samplecnt -= (int)chip.rateratio;
            }

            // Resample
            for (i = 0; i < 2; i++)
            {
                buf[i] = (int)((chip.oldsamples[i] * (chip.rateratio - chip.samplecnt) + chip.samples[i] * chip.samplecnt) / chip.rateratio);
            }

            chip.samplecnt += 1 << chip.RSM_FRAC;
        }

        /// <summary>
        /// Generates a stream of signed 32-bit samples from the chip.
        /// </summary>
        /// <param name="chip">The chip to generate samples from</param>
        /// <param name="samples">A 2-dimensional array for holding samples. The first dimension holds the samples, and the second dimension specifies the channel (0 = left and 1 = right).</param>
        /// <param name="numsamples">The number of samples to generate.</param>
        /// <remarks>This routine advances the clock automatically.</remarks>
        public static void GenerateStream(ref ChipState chip, ref int[,] samples, uint numsamples)
        {
            uint i, j;
            int[] buffer = new int[2];

            for (i = 0; i < numsamples; i++)
            {
                GenerateResampled(ref chip, ref buffer);

                for (j = 0; j < 2; j++)
                {
                    samples[i, j] = buffer[j];
                }
            }
        }

        public static void Reset(ref ChipState chip, uint flags, uint rate, uint clock)
        {
            uint i;
            uint rateratio = chip.rateratio;
            chip.opp = (flags & (uint)OPMFlags.ym2164) != 0;
            int?[] output = null;
            bool? sh1 = null, sh2 = null, so = null;

            SetIC(ref chip, true);
            for (i = 0; i < 32 * 64; i++)
            {
                Clock(ref chip, ref output, ref sh1, ref sh2, ref so);
            }
            SetIC(ref chip, false);

            if (rate != 0)
            {
                chip.rate = rate;
                chip.clock = clock;
                chip.rateratio = (uint)((((ulong)64 * rate) << chip.RSM_FRAC) / clock);
            }
            else
            {
                chip.rateratio = rateratio;
            }
        }

        /// <summary>
        /// Sets a mask for muting individual channels.
        /// </summary>
        /// <param name="chip">The chip to set the mask for.</param>
        /// <param name="mute">A bitmask for muting channels.</param>
        /// <remarks>A value of 0x00 enables all channels, and 0xFF mutes all channels.</remarks>
        public static void SetMute(ref ChipState chip, uint mute)
        {
            int i;
            for (i = 0; i < 8; i++)
            {
                chip.mute[i] = ((mute >> i) & 1) == 1;
            }
        }
 
    
    }


    public class YM2151
    {


    }


}