namespace Nuked
{
    /*
     * Copyright (C) 2017-2021 Alexey Khokholov (Nuke.YKT)
     *
     * This file is part of Nuked OPN2.
     *
     * This library is free software; you can redistribute it and/or
     * modify it under the terms of the GNU Lesser General Public
     * License as published by the Free Software Foundation; either
     * version 2.1 of the License, or (at your option) any later version.
     *
     * This library is distributed in the hope that it will be useful,
     * but WITHOUT ANY WARRANTY; without even the implied warranty of
     * MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the GNU
     * Lesser General Public License for more details.
     *
     * You should have received a copy of the GNU Lesser General Public
     * License along with this library; if not, write to the Free Software
     * Foundation, Inc., 51 Franklin Street, Fifth Floor, Boston, MA 02110-1301 USA
     *
     *  Nuked OPN2 (Yamaha YM3438) emulator.
     *  Thanks:
     *      Silicon Pr0n:
     *          Yamaha YM3438 decap and die shot(digshadow).
     *      OPLx decapsulated(Matthew Gambrell, Olli Niemitalo):
     *          OPL2 ROMs.
     *
     * version: 1.0.9
     *
     * C# Conversion and modifications Copyright (C) 2026 Paul Jensen
     * 
     * Modifications:
     * - Added function YMPSG_SetFilter to support custom low pass filter cutoff frequencies.
     * 
     */

    public class OPN2
    {
        public static uint SIGN_EXTEND(int bit_index, uint value)
        {
            return (value & ((1u << bit_index) - 1u)) - (value & (1u << bit_index));
        }

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
            0x000, 0x003, 0x006, 0x008, 0x00b, 0x00e, 0x011, 0x014,
            0x016, 0x019, 0x01c, 0x01f, 0x022, 0x025, 0x028, 0x02a,
            0x02d, 0x030, 0x033, 0x036, 0x039, 0x03c, 0x03f, 0x042,
            0x045, 0x048, 0x04b, 0x04e, 0x051, 0x054, 0x057, 0x05a,
            0x05d, 0x060, 0x063, 0x066, 0x069, 0x06c, 0x06f, 0x072,
            0x075, 0x078, 0x07b, 0x07e, 0x082, 0x085, 0x088, 0x08b,
            0x08e, 0x091, 0x094, 0x098, 0x09b, 0x09e, 0x0a1, 0x0a4,
            0x0a8, 0x0ab, 0x0ae, 0x0b1, 0x0b5, 0x0b8, 0x0bb, 0x0be,
            0x0c2, 0x0c5, 0x0c8, 0x0cc, 0x0cf, 0x0d2, 0x0d6, 0x0d9,
            0x0dc, 0x0e0, 0x0e3, 0x0e7, 0x0ea, 0x0ed, 0x0f1, 0x0f4,
            0x0f8, 0x0fb, 0x0ff, 0x102, 0x106, 0x109, 0x10c, 0x110,
            0x114, 0x117, 0x11b, 0x11e, 0x122, 0x125, 0x129, 0x12c,
            0x130, 0x134, 0x137, 0x13b, 0x13e, 0x142, 0x146, 0x149,
            0x14d, 0x151, 0x154, 0x158, 0x15c, 0x160, 0x163, 0x167,
            0x16b, 0x16f, 0x172, 0x176, 0x17a, 0x17e, 0x181, 0x185,
            0x189, 0x18d, 0x191, 0x195, 0x199, 0x19c, 0x1a0, 0x1a4,
            0x1a8, 0x1ac, 0x1b0, 0x1b4, 0x1b8, 0x1bc, 0x1c0, 0x1c4,
            0x1c8, 0x1cc, 0x1d0, 0x1d4, 0x1d8, 0x1dc, 0x1e0, 0x1e4,
            0x1e8, 0x1ec, 0x1f0, 0x1f5, 0x1f9, 0x1fd, 0x201, 0x205,
            0x209, 0x20e, 0x212, 0x216, 0x21a, 0x21e, 0x223, 0x227,
            0x22b, 0x230, 0x234, 0x238, 0x23c, 0x241, 0x245, 0x249,
            0x24e, 0x252, 0x257, 0x25b, 0x25f, 0x264, 0x268, 0x26d,
            0x271, 0x276, 0x27a, 0x27f, 0x283, 0x288, 0x28c, 0x291,
            0x295, 0x29a, 0x29e, 0x2a3, 0x2a8, 0x2ac, 0x2b1, 0x2b5,
            0x2ba, 0x2bf, 0x2c4, 0x2c8, 0x2cd, 0x2d2, 0x2d6, 0x2db,
            0x2e0, 0x2e5, 0x2e9, 0x2ee, 0x2f3, 0x2f8, 0x2fd, 0x302,
            0x306, 0x30b, 0x310, 0x315, 0x31a, 0x31f, 0x324, 0x329,
            0x32e, 0x333, 0x338, 0x33d, 0x342, 0x347, 0x34c, 0x351,
            0x356, 0x35b, 0x360, 0x365, 0x36a, 0x370, 0x375, 0x37a,
            0x37f, 0x384, 0x38a, 0x38f, 0x394, 0x399, 0x39f, 0x3a4,
            0x3a9, 0x3ae, 0x3b4, 0x3b9, 0x3bf, 0x3c4, 0x3c9, 0x3cf,
            0x3d4, 0x3da, 0x3df, 0x3e4, 0x3ea, 0x3ef, 0x3f5, 0x3fa
        ];

        /* Note table */
        public static readonly uint[] fn_note = [
            0, 0, 0, 0, 0, 0, 0, 1, 2, 3, 3, 3, 3, 3, 3, 3
        ];

        /* Envelope generator */
        public static readonly uint[,] eg_stephi = {
            { 0, 0, 0, 0 },
            { 1, 0, 0, 0 },
            { 1, 0, 1, 0 },
            { 1, 1, 1, 0 }
        };

        public static readonly byte[] eg_am_shift = [
            7, 3, 1, 0
        ];

        /* Phase generator */
        public static readonly uint[] pg_detune = [
            16, 17, 19, 20, 22, 24, 27, 29 ];

        public static readonly uint[,] pg_lfo_sh1 = {
            { 7, 7, 7, 7, 7, 7, 7, 7 },
            { 7, 7, 7, 7, 7, 7, 7, 7 },
            { 7, 7, 7, 7, 7, 7, 1, 1 },
            { 7, 7, 7, 7, 1, 1, 1, 1 },
            { 7, 7, 7, 1, 1, 1, 1, 0 },
            { 7, 7, 1, 1, 0, 0, 0, 0 },
            { 7, 7, 1, 1, 0, 0, 0, 0 },
            { 7, 7, 1, 1, 0, 0, 0, 0 }
        };

        public static readonly uint[,] pg_lfo_sh2 = {
            { 7, 7, 7, 7, 7, 7, 7, 7 },
            { 7, 7, 7, 7, 2, 2, 2, 2 },
            { 7, 7, 7, 2, 2, 2, 7, 7 },
            { 7, 7, 2, 2, 7, 7, 2, 2 },
            { 7, 7, 2, 7, 7, 7, 2, 7 },
            { 7, 7, 7, 2, 7, 7, 2, 1 },
            { 7, 7, 7, 2, 7, 7, 2, 1 },
            { 7, 7, 7, 2, 7, 7, 2, 1 }
        };

        /* Address decoder */
        public static readonly uint[] op_offset = [
            0x000, /* Ch1 OP1/OP2 */
            0x001, /* Ch2 OP1/OP2 */
            0x002, /* Ch3 OP1/OP2 */
            0x100, /* Ch4 OP1/OP2 */
            0x101, /* Ch5 OP1/OP2 */
            0x102, /* Ch6 OP1/OP2 */
            0x004, /* Ch1 OP3/OP4 */
            0x005, /* Ch2 OP3/OP4 */
            0x006, /* Ch3 OP3/OP4 */
            0x104, /* Ch4 OP3/OP4 */
            0x105, /* Ch5 OP3/OP4 */
            0x106  /* Ch6 OP3/OP4 */
        ];

        public static readonly uint[] ch_offset = [
            0x000, /* Ch1 */
            0x001, /* Ch2 */
            0x002, /* Ch3 */
            0x100, /* Ch4 */
            0x101, /* Ch5 */
            0x102  /* Ch6 */
        ];

        /* LFO */
        public static readonly uint[] lfo_cycles = [
            108, 77, 71, 67, 62, 44, 8, 5
        ];

        /* FM algorithm */
        public static readonly uint[,,] fm_algorithm = {
            {
                { 1, 1, 1, 1, 1, 1, 1, 1 }, /* OP1_0         */
                { 1, 1, 1, 1, 1, 1, 1, 1 }, /* OP1_1         */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* OP2           */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 1 }  /* Out           */
            },
            {
                { 0, 1, 0, 0, 0, 1, 0, 0 }, /* OP1_0         */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* OP1_1         */
                { 1, 1, 1, 0, 0, 0, 0, 0 }, /* OP2           */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 1, 1, 1 }  /* Out           */
            },
            {
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* OP1_0         */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* OP1_1         */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* OP2           */
                { 1, 0, 0, 1, 1, 1, 1, 0 }, /* Last operator */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* Last operator */
                { 0, 0, 0, 0, 1, 1, 1, 1 }  /* Out           */
            },
            {
                { 0, 0, 1, 0, 0, 1, 0, 0 }, /* OP1_0         */
                { 0, 0, 0, 0, 0, 0, 0, 0 }, /* OP1_1         */
                { 0, 0, 0, 1, 0, 0, 0, 0 }, /* OP2           */
                { 1, 1, 0, 1, 1, 0, 0, 0 }, /* Last operator */
                { 0, 0, 1, 0, 0, 0, 0, 0 }, /* Last operator */
                { 1, 1, 1, 1, 1, 1, 1, 1 }  /* Out           */
            }
        };

        public enum EGNum : byte
        {
            attack = 0,
            decay = 1,
            sustain = 2,
            release = 3
        };

        public class ChipState
        {
            public uint cycles;
            public uint channel;
            public short mol, mor;

            /* IO */
            public ushort write_data;
            public byte write_a;
            public byte write_d;
            public bool write_a_en;
            public bool write_d_en;
            public bool write_busy;
            public byte write_busy_cnt;
            public bool write_fm_address;
            public bool write_fm_data;
            public ushort write_fm_mode_a;
            public ushort address;
            public byte data;
            public bool pin_test_in;
            public byte pin_irq;
            public bool busy;

            /* LFO */
            public byte lfo_en;
            public byte lfo_freq;
            public byte lfo_pm;
            public byte lfo_am;
            public byte lfo_cnt;
            public byte lfo_inc;
            public byte lfo_quotient;

            /* Phase generator */
            public ushort pg_fnum;
            public byte pg_block;
            public byte pg_kcode;
            public uint[] pg_inc;
            public uint[] pg_phase;
            public bool[] pg_reset;
            public uint pg_read;

            /* Envelope generator */
            public byte eg_cycle;
            public bool eg_cycle_stop;
            public byte eg_shift;
            public byte eg_shift_lock;
            public byte eg_timer_low_lock;
            public ushort eg_timer;
            public byte eg_timer_inc;
            public ushort eg_quotient;
            public bool eg_custom_timer;
            public byte eg_rate;
            public byte eg_ksv;
            public byte eg_inc;
            public bool eg_ratemax;
            public byte[] eg_sl;
            public byte eg_lfo_am;
            public byte[] eg_tl;
            public EGNum[] eg_state;
            public ushort[] eg_level;
            public ushort[] eg_out;
            public bool[] eg_kon;
            public bool[] eg_kon_csm;
            public bool[] eg_kon_latch;
            public byte[] eg_csm_mode;
            public bool[] eg_ssg_enable;
            public bool[] eg_ssg_pgrst_latch;
            public bool[] eg_ssg_repeat_latch;
            public bool[] eg_ssg_hold_up_latch;
            public bool[] eg_ssg_dir;
            public bool[] eg_ssg_inv;
            public uint[] eg_read;
            public byte eg_read_inc;

            /* FM */
            public short[,] fm_op1;
            public short[] fm_op2;
            public short[] fm_out;
            public ushort[] fm_mod;

            /* Channel */
            public short[] ch_acc;
            public short[] ch_out;
            public short ch_lock;
            public bool ch_lock_l;
            public bool ch_lock_r;
            public short ch_read;

            /* Timer */
            public ushort timer_a_cnt;
            public ushort timer_a_reg;
            public bool timer_a_load_lock;
            public bool timer_a_load;
            public bool timer_a_enable;
            public bool timer_a_reset;
            public bool timer_a_load_latch;
            public bool timer_a_overflow_flag;
            public bool timer_a_overflow;
            public ushort timer_b_cnt;
            public byte timer_b_subcnt;
            public ushort timer_b_reg;
            public bool timer_b_load_lock;
            public bool timer_b_load;
            public bool timer_b_enable;
            public bool timer_b_reset;
            public bool timer_b_load_latch;
            public bool timer_b_overflow_flag;
            public bool timer_b_overflow;

            /* Register set */
            public bool[] mode_test_21;
            public bool[] mode_test_2c;
            public byte mode_ch3;
            public byte mode_kon_channel;
            public bool[] mode_kon_operator;
            public bool[] mode_kon;
            public bool mode_csm;
            public bool mode_kon_csm;
            public bool dacen;
            public short dacdata;

            public byte[] ks;
            public byte[] ar;
            public byte[] sr;
            public byte[] dt;
            public byte[] multi;
            public byte[] sl;
            public byte[] rr;
            public byte[] dr;
            public bool[] am;
            public byte[] tl;
            public byte[] ssg_eg;

            public ushort[] fnum;
            public byte[] block;
            public byte[] kcode;
            public ushort[] fnum_3ch;
            public byte[] block_3ch;
            public byte[] kcode_3ch;
            public byte reg_a4;
            public byte reg_ac;
            public byte[] connect;
            public byte[] fb;
            public bool[] pan_l, pan_r;
            public byte[] ams;
            public byte[] pms;
            public byte status;
            public uint status_time;


            // For buffered writes
            public ulong writebuf_samplecnt;
            public uint writebuf_cur;
            public uint writebuf_last;
            public ulong writebuf_lasttime;
            public readonly ulong WRITEBUF_SIZE = 2048;
            public readonly ulong WRITEBUF_DELAY = 15;
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
                /* Phase generator */
                pg_inc = new uint[24];
                pg_phase = new uint[24];
                pg_reset = new bool[24];

                /* Envelope generator */
                eg_sl = new byte[2];
                eg_tl = new byte[2];
                eg_state = new EGNum[24];
                eg_level = new ushort[24];
                eg_out = new ushort[24];
                eg_kon = new bool[24];
                eg_kon_csm = new bool[24];
                eg_kon_latch = new bool[24];
                eg_csm_mode = new byte[24];
                eg_ssg_enable = new bool[24];
                eg_ssg_pgrst_latch = new bool[24];
                eg_ssg_repeat_latch = new bool[24];
                eg_ssg_hold_up_latch = new bool[24];
                eg_ssg_dir = new bool[24];
                eg_ssg_inv = new bool[24];
                eg_read = new uint[2];

                /* FM */
                fm_op1 = new short[6, 2];
                fm_op2 = new short[6];
                fm_out = new short[24];
                fm_mod = new ushort[24];

                /* Channel */
                ch_acc = new short[6];
                ch_out = new short[6];

                /* Register set */
                mode_test_21 = new bool[8];
                mode_test_2c = new bool[8];
                mode_kon_operator = new bool[4];
                mode_kon = new bool[24];

                ks = new byte[24];
                ar = new byte[24];
                sr = new byte[24];
                dt = new byte[24];
                multi = new byte[24];
                sl = new byte[24];
                rr = new byte[24];
                dr = new byte[24];
                am = new bool[24];
                tl = new byte[24];
                ssg_eg = new byte[24];

                fnum = new ushort[6];
                block = new byte[6];
                kcode = new byte[6];
                fnum_3ch = new ushort[6];
                block_3ch = new byte[6];
                kcode_3ch = new byte[6];
                connect = new byte[6];
                fb = new byte[6];
                pan_l = new bool[6];
                pan_r = new bool[6];
                ams = new byte[6];
                pms = new byte[6];

                mute = new bool[7];
                oldsamples = new int[2];
                samples = new int[2];

                writebuf = new WriteBuf[WRITEBUF_SIZE];

                FILTER_CUTOFF = 0.51229881578701075; // 5894.6 Hz single pole IIR low pass (NTSC)
                FILTER_CUTOFF_I = (1 - FILTER_CUTOFF);
            }
        }

        public struct WriteBuf
        {
            public ulong time;
            public byte port;
            public byte data;
        }

        public enum ChipMode : uint
        {
            ym3438 = 0x00,      /* Normal mode */
            ym2612 = 0x01,      /* Enables YM2612 emulation (MD1, MD2 VA2) */
            readmode = 0x02,    /* Enables status read on any port (TeraDrive, MD1 VA7, MD2, etc) */
        };

        static ChipMode chip_type = ChipMode.readmode;
        private static readonly bool use_filter = true;

        static void DoIO(ref ChipState chip)
        {
            /* Write signal check */
            chip.write_a_en = (chip.write_a & 0x03) == 0x01;
            chip.write_d_en = (chip.write_d & 0x03) == 0x01;
            chip.write_a <<= 1;
            chip.write_d <<= 1;

            /* Busy counter */
            chip.busy = chip.write_busy;
            chip.write_busy_cnt += (byte)(chip.write_busy ? 1 : 0);
            chip.write_busy = (chip.write_busy && ((chip.write_busy_cnt >> 5) == 0)) || chip.write_d_en;
            chip.write_busy_cnt &= 0x1f;
        }

        static void DoRegWrite(ref ChipState chip)
        {
            uint i;
            uint slot = chip.cycles % 12;
            uint address;
            uint channel = chip.channel;

            /* Update registers */
            if (chip.write_fm_data)
            {
                /* Slot */
                if (op_offset[slot] == (chip.address & 0x107))
                {
                    if ((chip.address & 0x08) != 0)
                    {
                        /* OP2, OP4 */
                        slot += 12;
                    }
                    address = (uint)chip.address & 0xf0;
                    switch (address)
                    {
                        case 0x30: /* DT, MULTI */
                            chip.multi[slot] = (byte)(chip.data & 0x0f);
                            if (chip.multi[slot] == 0)
                            {
                                chip.multi[slot] = 1;
                            }
                            else
                            {
                                chip.multi[slot] <<= 1;
                            }
                            chip.dt[slot] = (byte)((chip.data >> 4) & 0x07);
                            break;
                        case 0x40: /* TL */
                            chip.tl[slot] = (byte)(chip.data & 0x7f);
                            break;
                        case 0x50: /* KS, AR */
                            chip.ar[slot] = (byte)(chip.data & 0x1f);
                            chip.ks[slot] = (byte)((chip.data >> 6) & 0x03);
                            break;
                        case 0x60: /* AM, DR */
                            chip.dr[slot] = (byte)(chip.data & 0x1f);
                            chip.am[slot] = ((chip.data >> 7) & 0x01) == 1;
                            break;
                        case 0x70: /* SR */
                            chip.sr[slot] = (byte)(chip.data & 0x1f);
                            break;
                        case 0x80: /* SL, RR */
                            chip.rr[slot] = (byte)(chip.data & 0x0f);
                            chip.sl[slot] = (byte)((chip.data >> 4) & 0x0f);
                            chip.sl[slot] |= (byte)((chip.sl[slot] + 1) & 0x10);
                            break;
                        case 0x90: /* SSG-EG */
                            chip.ssg_eg[slot] = (byte)(chip.data & 0x0f);
                            break;
                        default:
                            break;
                    }
                }

                /* Channel */
                if (ch_offset[channel] == (chip.address & 0x103))
                {
                    address = (uint)(chip.address & 0xfc);
                    switch (address)
                    {
                        case 0xa0:
                            chip.fnum[channel] = (ushort)((chip.data & 0xff) | ((chip.reg_a4 & 0x07) << 8));
                            chip.block[channel] = (byte)((chip.reg_a4 >> 3) & 0x07);
                            chip.kcode[channel] = (byte)(chip.block[channel] << 2 | (byte)fn_note[chip.fnum[channel] >> 7]);
                            break;
                        case 0xa4:
                            chip.reg_a4 = (byte)(chip.data & 0xff);
                            break;
                        case 0xa8:
                            chip.fnum_3ch[channel] = (ushort)((chip.data & 0xff) | ((chip.reg_ac & 0x07) << 8));
                            chip.block_3ch[channel] = (byte)((chip.reg_ac >> 3) & 0x07);
                            chip.kcode_3ch[channel] = (byte)((chip.block_3ch[channel] << 2) | (byte)fn_note[chip.fnum_3ch[channel] >> 7]);
                            break;
                        case 0xac:
                            chip.reg_ac = (byte)(chip.data & 0xff);
                            break;
                        case 0xb0:
                            chip.connect[channel] = (byte)(chip.data & 0x07);
                            chip.fb[channel] = (byte)((chip.data >> 3) & 0x07);
                            break;
                        case 0xb4:
                            chip.pms[channel] = (byte)(chip.data & 0x07);
                            chip.ams[channel] = (byte)((chip.data >> 4) & 0x03);
                            chip.pan_l[channel] = ((chip.data >> 7) & 0x01) == 1;
                            chip.pan_r[channel] = ((chip.data >> 6) & 0x01) == 1;
                            break;
                        default:
                            break;
                    }
                }
            }

            if (chip.write_a_en || chip.write_d_en)
            {
                /* Data */
                if (chip.write_a_en)
                {
                    chip.write_fm_data = false;
                }

                if (chip.write_fm_address && chip.write_d_en)
                {
                    chip.write_fm_data = true;
                }

                /* Address */
                if (chip.write_a_en)
                {
                    if ((chip.write_data & 0xf0) != 0x00)
                    {
                        /* FM Write */
                        chip.address = chip.write_data;
                        chip.write_fm_address = true;
                    }
                    else
                    {
                        /* SSG write */
                        chip.write_fm_address = false;
                    }
                }

                /* FM Mode */
                /* Data */
                if (chip.write_d_en && ((chip.write_data & 0x100) == 0))
                {
                    switch (chip.write_fm_mode_a)
                    {
                        case 0x21: /* LSI test 1 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_21[i] = ((chip.write_data >> (int)i) & 0x01) == 1;
                            }
                            break;
                        case 0x22: /* LFO control */
                            if (((chip.write_data >> 3) & 0x01) != 0)
                            {
                                chip.lfo_en = 0x7f;
                            }
                            else
                            {
                                chip.lfo_en = 0;
                            }
                            chip.lfo_freq = (byte)(chip.write_data & 0x07);
                            break;
                        case 0x24: /* Timer A */
                            chip.timer_a_reg &= 0x03;
                            chip.timer_a_reg |= (ushort)((chip.write_data & 0xff) << 2);
                            break;
                        case 0x25:
                            chip.timer_a_reg &= 0x3fc;
                            chip.timer_a_reg |= (ushort)(chip.write_data & 0x03);
                            break;
                        case 0x26: /* Timer B */
                            chip.timer_b_reg = (ushort)(chip.write_data & 0xff);
                            break;
                        case 0x27: /* CSM, Timer control */
                            chip.mode_ch3 = (byte)((chip.write_data & 0xc0) >> 6);
                            chip.mode_csm = chip.mode_ch3 == 2;
                            chip.timer_a_load = (chip.write_data & 0x01) == 1;
                            chip.timer_a_enable = ((chip.write_data >> 2) & 0x01) == 1;
                            chip.timer_a_reset = ((chip.write_data >> 4) & 0x01) == 1;
                            chip.timer_b_load = ((chip.write_data >> 1) & 0x01) == 1;
                            chip.timer_b_enable = ((chip.write_data >> 3) & 0x01) == 1;
                            chip.timer_b_reset = ((chip.write_data >> 5) & 0x01) == 1;
                            break;
                        case 0x28: /* Key on/off */
                            for (i = 0; i < 4; i++)
                            {
                                chip.mode_kon_operator[i] = ((chip.write_data >> (4 + (int)i)) & 0x01) == 1;
                            }
                            if ((chip.write_data & 0x03) == 0x03)
                            {
                                /* Invalid address */
                                chip.mode_kon_channel = 0xff;
                            }
                            else
                            {
                                chip.mode_kon_channel = (byte)((chip.write_data & 0x03) + ((chip.write_data >> 2) & 1) * 3);
                            }
                            break;
                        case 0x2a: /* DAC data */
                            chip.dacdata &= 0x01;
                            chip.dacdata |= (short)((chip.write_data ^ 0x80) << 1);
                            break;
                        case 0x2b: /* DAC enable */
                            chip.dacen = (chip.write_data >> 7) == 1;
                            break;
                        case 0x2c: /* LSI test 2 */
                            for (i = 0; i < 8; i++)
                            {
                                chip.mode_test_2c[i] = ((chip.write_data >> (int)i) & 0x01) == 1;
                            }
                            chip.dacdata &= 0x1fe;
                            chip.dacdata |= (short)(chip.mode_test_2c[3] ? 1 : 0);
                            chip.eg_custom_timer = !chip.mode_test_2c[7] && chip.mode_test_2c[6];
                            break;
                        default:
                            break;
                    }
                }

                /* Address */
                if (chip.write_a_en)
                {
                    chip.write_fm_mode_a = (ushort)(chip.write_data & 0x1ff);
                }
            }

            if (chip.write_fm_data)
            {
                chip.data = (byte)(chip.write_data & 0xff);
            }
        }

        static void PhaseCalcIncrement(ref ChipState chip)
        {
            uint chan = chip.channel;
            uint slot = chip.cycles;
            uint fnum = chip.pg_fnum;
            uint fnum_h = fnum >> 4;
            uint fm;
            uint basefreq;
            byte lfo = chip.lfo_pm;
            byte lfo_l = (byte)(lfo & 0x0f);
            byte pms = chip.pms[chan];
            byte dt = chip.dt[slot];
            byte dt_l = (byte)(dt & 0x03);
            byte detune = 0;
            byte block, note;
            byte sum, sum_h, sum_l;
            byte kcode = chip.pg_kcode;

            fnum <<= 1;

            /* Apply LFO */
            if ((lfo_l & 0x08) != 0)
            {
                lfo_l ^= 0x0f;
            }
            fm = (fnum_h >> (int)pg_lfo_sh1[pms, lfo_l]) + (fnum_h >> (int)pg_lfo_sh2[pms, lfo_l]);
            if (pms > 5)
            {
                fm <<= pms - 5;
            }
            fm >>= 2;
            if ((lfo & 0x10) != 0)
            {
                fnum -= fm;
            }
            else
            {
                fnum += fm;
            }
            fnum &= 0xfff;

            basefreq = (fnum << chip.pg_block) >> 2;

            /* Apply detune */
            if (dt_l != 0)
            {
                if (kcode > 0x1c)
                {
                    kcode = 0x1c;
                }
                block = (byte)(kcode >> 2);
                note = (byte)(kcode & 0x03);
                sum = (byte)(block + 9 + ((dt_l == 3) ? 1 : 0 | (dt_l & 0x02)));
                sum_h = (byte)(sum >> 1);
                sum_l = (byte)(sum & 0x01);
                detune = (byte)(pg_detune[(sum_l << 2) | note] >> (9 - sum_h));
            }
            if ((dt & 0x04) == 0x04)
            {
                basefreq -= detune;
            }
            else
            {
                basefreq += detune;
            }
            basefreq &= 0x1ffff;
            chip.pg_inc[slot] = (basefreq * chip.multi[slot]) >> 1;
            chip.pg_inc[slot] &= 0xfffff;
        }

        static void PhaseGenerate(ref ChipState chip)
        {
            uint slot;

            /* Mask increment */
            slot = (chip.cycles + 20) % 24;
            if (chip.pg_reset[slot])
            {
                chip.pg_inc[slot] = 0;
            }

            /* Phase step */
            slot = (chip.cycles + 19) % 24;
            if (chip.pg_reset[slot] || chip.mode_test_21[3])
            {
                chip.pg_phase[slot] = 0;
            }
            chip.pg_phase[slot] += chip.pg_inc[slot];
            chip.pg_phase[slot] &= 0xfffff;
        }

        static void EnvelopeSSGEG(ref ChipState chip)
        {
            uint slot = chip.cycles;

            bool direction = false;
            chip.eg_ssg_pgrst_latch[slot] = false;
            chip.eg_ssg_repeat_latch[slot] = false;
            chip.eg_ssg_hold_up_latch[slot] = false;

            if ((chip.ssg_eg[slot] & 0x08) != 0)
            {
                direction = chip.eg_ssg_dir[slot];
                if ((chip.eg_level[slot] & 0x200) != 0)
                {
                    /* Reset */
                    if ((chip.ssg_eg[slot] & 0x03) == 0)
                    {
                        chip.eg_ssg_pgrst_latch[slot] = true;
                    }
                    /* Repeat */
                    if ((chip.ssg_eg[slot] & 0x01) == 0)
                    {
                        chip.eg_ssg_repeat_latch[slot] = true;
                    }
                    /* Inverse */
                    if ((chip.ssg_eg[slot] & 0x03) == 0x02)
                    {
                        direction ^= true;
                    }
                    if ((chip.ssg_eg[slot] & 0x03) == 0x03)
                    {
                        direction = true;
                    }
                }
                /* Hold up */
                if (chip.eg_kon_latch[slot] && ((chip.ssg_eg[slot] & 0x07) == 0x05 || (chip.ssg_eg[slot] & 0x07) == 0x03))
                {
                    chip.eg_ssg_hold_up_latch[slot] = true;
                }
                direction &= chip.eg_kon[slot];
            }
            chip.eg_ssg_dir[slot] = direction;
            chip.eg_ssg_enable[slot] = ((chip.ssg_eg[slot] >> 3) & 0x01) == 1;
            bool ssg1 = ((chip.ssg_eg[slot] >> 2) & 0x01) == 1;
            bool ssg2 = ((chip.ssg_eg[slot] >> 3) & 0x01) == 1;
            chip.eg_ssg_inv[slot] = (chip.eg_ssg_dir[slot] ^ (ssg1 & ssg2)) & chip.eg_kon[slot];
            //chip.eg_ssg_inv[slot] = (chip.eg_ssg_dir[slot] ^ (((chip.ssg_eg[slot] >> 2) & 0x01) & ((chip.ssg_eg[slot] >> 3) & 0x01))) & chip.eg_kon[slot];
        }

        static void EnvelopeADSR(ref ChipState chip)
        {
            uint slot = (chip.cycles + 22) % 24;

            bool nkon = chip.eg_kon_latch[slot];
            bool okon = chip.eg_kon[slot];
            bool kon_event;
            bool koff_event;
            bool eg_off;
            short level;
            short nextlevel = 0;
            short ssg_level;
            EGNum nextstate = chip.eg_state[slot];
            short inc = 0;
            chip.eg_read[0] = chip.eg_read_inc;
            chip.eg_read_inc = (byte)(chip.eg_inc > 0 ? 1 : 0);

            /* Reset phase generator */
            chip.pg_reset[slot] = (nkon && !okon) || chip.eg_ssg_pgrst_latch[slot];

            /* KeyOn/Off */
            kon_event = (nkon && !okon) || (okon && chip.eg_ssg_repeat_latch[slot]);
            koff_event = okon && !nkon;

            ssg_level = level = (short)chip.eg_level[slot];

            if (chip.eg_ssg_inv[slot])
            {
                /* Inverse */
                ssg_level = (short)(512 - level);
                ssg_level &= 0x3ff;
            }

            if (koff_event)
            {
                level = ssg_level;
            }

            if (chip.eg_ssg_enable[slot])
            {
                eg_off = (level >> 9) != 0;
            }
            else
            {
                eg_off = (level & 0x3f0) == 0x3f0;
            }

            nextlevel = level;

            if (kon_event)
            {
                nextstate = EGNum.attack;
                /* Instant attack */
                if (chip.eg_ratemax)
                {
                    nextlevel = 0;
                }
                else if (chip.eg_state[slot] == EGNum.attack && level != 0 && (chip.eg_inc != 0) && nkon)
                {
                    inc = (short)((~level << chip.eg_inc) >> 5);
                }
            }
            else
            {
                switch (chip.eg_state[slot])
                {
                    case EGNum.attack:
                        if (level == 0)
                        {
                            nextstate = EGNum.decay;
                        }
                        else if ((chip.eg_inc != 0) && !chip.eg_ratemax && nkon)
                        {
                            inc = (short)((~level << chip.eg_inc) >> 5);
                        }
                        break;
                    case EGNum.decay:
                        if ((level >> 4) == (chip.eg_sl[1] << 1))
                        {
                            nextstate = EGNum.sustain;
                        }
                        else if (!eg_off && (chip.eg_inc != 0))
                        {
                            inc = (short)(1 << (chip.eg_inc - 1));
                            if (chip.eg_ssg_enable[slot])
                            {
                                inc <<= 2;
                            }
                        }
                        break;
                    case EGNum.sustain:
                    case EGNum.release:
                        if (!eg_off && (chip.eg_inc != 0))
                        {
                            inc = (short)(1 << (chip.eg_inc - 1));
                            if (chip.eg_ssg_enable[slot])
                            {
                                inc <<= 2;
                            }
                        }
                        break;
                    default:
                        break;
                }
                if (!nkon)
                {
                    nextstate = EGNum.release;
                }
            }
            if (chip.eg_kon_csm[slot])
            {
                nextlevel |= (short)(chip.eg_tl[1] << 3);
            }

            /* Envelope off */
            if (!kon_event && !chip.eg_ssg_hold_up_latch[slot] && chip.eg_state[slot] != EGNum.attack && eg_off)
            {
                nextstate = EGNum.release;
                nextlevel = 0x3ff;
            }

            nextlevel += inc;

            chip.eg_kon[slot] = chip.eg_kon_latch[slot];
            chip.eg_level[slot] = (ushort)(nextlevel & 0x3ff);
            chip.eg_state[slot] = nextstate;
        }

        static void EnvelopePrepare(ref ChipState chip)
        {
            byte rate;
            byte sum;
            byte inc = 0;
            uint slot = chip.cycles;
            EGNum rate_sel;

            /* Prepare increment */
            rate = (byte)((chip.eg_rate << 1) + chip.eg_ksv);

            if (rate > 0x3f)
            {
                rate = 0x3f;
            }

            sum = (byte)(((rate >> 2) + chip.eg_shift_lock) & 0x0f);
            if (chip.eg_rate != 0 && chip.eg_quotient == 2)
            {
                if (rate < 48)
                {
                    switch (sum)
                    {
                        case 12:
                            inc = 1;
                            break;
                        case 13:
                            inc = (byte)((rate >> 1) & 0x01);
                            break;
                        case 14:
                            inc = (byte)(rate & 0x01);
                            break;
                        default:
                            break;
                    }
                }
                else
                {
                    inc = (byte)(eg_stephi[rate & 0x03, chip.eg_timer_low_lock] + (rate >> 2) - 11);
                    if (inc > 4)
                    {
                        inc = 4;
                    }
                }
            }
            chip.eg_inc = inc;
            chip.eg_ratemax = (rate >> 1) == 0x1f;

            /* Prepare rate & ksv */
            rate_sel = chip.eg_state[slot];
            if ((chip.eg_kon[slot] && chip.eg_ssg_repeat_latch[slot])
             || (!chip.eg_kon[slot] && chip.eg_kon_latch[slot]))
            {
                rate_sel = EGNum.attack;
            }
            switch (rate_sel)
            {
                case EGNum.attack:
                    chip.eg_rate = chip.ar[slot];
                    break;
                case EGNum.decay:
                    chip.eg_rate = chip.dr[slot];
                    break;
                case EGNum.sustain:
                    chip.eg_rate = chip.sr[slot];
                    break;
                case EGNum.release:
                    chip.eg_rate = (byte)((chip.rr[slot] << 1) | 0x01);
                    break;
                default:
                    break;
            }
            chip.eg_ksv = (byte)(chip.pg_kcode >> (chip.ks[slot] ^ 0x03));
            if (chip.am[slot])
            {
                chip.eg_lfo_am = (byte)(chip.lfo_am >> eg_am_shift[chip.ams[chip.channel]]);
            }
            else
            {
                chip.eg_lfo_am = 0;
            }
            /* Delay TL & SL value */
            chip.eg_tl[1] = chip.eg_tl[0];
            chip.eg_tl[0] = chip.tl[slot];
            chip.eg_sl[1] = chip.eg_sl[0];
            chip.eg_sl[0] = chip.sl[slot];
        }

        static void EnvelopeGenerate(ref ChipState chip)
        {
            uint slot = (chip.cycles + 23) % 24;
            ushort level;

            level = chip.eg_level[slot];

            if (chip.eg_ssg_inv[slot])
            {
                /* Inverse */
                level = (ushort)(512 - level);
            }
            if (chip.mode_test_21[5])
            {
                level = 0;
            }
            level &= 0x3ff;

            /* Apply AM LFO */
            level += chip.eg_lfo_am;

            /* Apply TL */
            if (!(chip.mode_csm && chip.channel == 2 + 1))
            {
                level += (ushort)(chip.eg_tl[0] << 3);
            }
            if (level > 0x3ff)
            {
                level = 0x3ff;
            }
            chip.eg_out[slot] = level;
        }

        static void UpdateLFO(ref ChipState chip)
        {
            if ((chip.lfo_quotient & lfo_cycles[chip.lfo_freq]) == lfo_cycles[chip.lfo_freq])
            {
                chip.lfo_quotient = 0;
                chip.lfo_cnt++;
            }
            else
            {
                chip.lfo_quotient += chip.lfo_inc;
            }
            chip.lfo_cnt &= chip.lfo_en;
        }

        static void FMPrepare(ref ChipState chip)
        {
            uint slot = (chip.cycles + 6) % 24;
            uint channel = chip.channel;
            short mod, mod1, mod2;
            uint op = slot / 6;
            byte connect = chip.connect[channel];
            uint prevslot = (chip.cycles + 18) % 24;

            /* Calculate modulation */
            mod1 = mod2 = 0;

            if (fm_algorithm[op, 0, connect] == 1)
            {
                mod2 |= chip.fm_op1[channel, 0];
            }
            if (fm_algorithm[op, 1, connect] == 1)
            {
                mod1 |= chip.fm_op1[channel, 1];
            }
            if (fm_algorithm[op, 2, connect] == 1)
            {
                mod1 |= chip.fm_op2[channel];
            }
            if (fm_algorithm[op, 3, connect] == 1)
            {
                mod2 |= chip.fm_out[prevslot];
            }
            if (fm_algorithm[op, 4, connect] == 1)
            {
                mod1 |= chip.fm_out[prevslot];
            }
            mod = (short)(mod1 + mod2);

            if (op == 0)
            {
                /* Feedback */
                mod = (short)(mod >> (10 - chip.fb[channel]));
                if (chip.fb[channel] == 0)
                {
                    mod = 0;
                }
            }
            else
            {
                mod >>= 1;
            }

            chip.fm_mod[slot] = (ushort)mod;
            slot = (chip.cycles + 18) % 24;

            /* OP1 */
            if (slot / 6 == 0)
            {
                chip.fm_op1[channel, 1] = chip.fm_op1[channel, 0];
                chip.fm_op1[channel, 0] = chip.fm_out[slot];
            }

            /* OP2 */
            if (slot / 6 == 2)
            {
                chip.fm_op2[channel] = chip.fm_out[slot];
            }
        }

        static void ChGenerate(ref ChipState chip)
        {
            uint slot = (chip.cycles + 18) % 24;
            uint channel = chip.channel;
            uint op = slot / 6;
            bool test_dac = chip.mode_test_2c[5];
            short acc = chip.ch_acc[channel];
            short add = (short)(test_dac ? 1 : 0);
            short sum = 0;

            if (op == 0 && !test_dac)
            {
                acc = 0;
            }

            if ((fm_algorithm[op, 5, chip.connect[channel]] == 1) && !test_dac)
            {
                add += (short)(chip.fm_out[slot] >> 5);
            }

            sum = (short)(acc + add);

            /* Clamp */
            if (sum > 255)
            {
                sum = 255;
            }
            else if (sum < -256)
            {
                sum = -256;
            }

            if (op == 0 || test_dac)
            {
                chip.ch_out[channel] = chip.ch_acc[channel];
            }

            chip.ch_acc[channel] = sum;
        }

        static void ChOutput(ref ChipState chip)
        {
            uint cycles = chip.cycles;
            uint slot = chip.cycles;
            uint channel = chip.channel;
            bool test_dac = chip.mode_test_2c[5];
            short out0;
            short sign;
            bool out_en;

            chip.ch_read = chip.ch_lock;

            if (slot < 12)
            {
                /* Ch 4,5,6 */
                channel++;
            }
            if ((cycles & 3) == 0)
            {
                if (!test_dac)
                {
                    /* Lock value */
                    chip.ch_lock = chip.ch_out[channel];
                }
                chip.ch_lock_l = chip.pan_l[channel];
                chip.ch_lock_r = chip.pan_r[channel];
            }
            /* Ch 6 */
            if (((cycles >> 2) == 1 && chip.dacen) || test_dac)
            {
                out0 = chip.dacdata;
                out0 = (short)SIGN_EXTEND(8, (uint)out0);
            }
            else
            {
                out0 = chip.ch_lock;
            }

            chip.mol = 0;
            chip.mor = 0;

            if ((chip_type & ChipMode.ym2612) != 0)
            {
                out_en = ((cycles & 3) == 3) || test_dac;

                /* YM2612 DAC emulation(not verified) */
                sign = (short)(out0 >> 8);
                if (out0 >= 0)
                {
                    out0++;
                    sign++;
                }
                if (chip.ch_lock_l && out_en)
                {
                    chip.mol = out0;
                }
                else
                {
                    chip.mol = sign;
                }
                if (chip.ch_lock_r && out_en)
                {
                    chip.mor = out0;
                }
                else
                {
                    chip.mor = sign;
                }

                /* Amplify signal */
                chip.mol *= 3;
                chip.mor *= 3;
            }
            else
            {
                out_en = ((cycles & 3) != 0) || test_dac;
                if (chip.ch_lock_l && out_en)
                {
                    chip.mol = out0;
                }
                if (chip.ch_lock_r && out_en)
                {
                    chip.mor = out0;
                }
            }
        }

        static void FMGenerate(ref ChipState chip)
        {
            uint slot = (chip.cycles + 19) % 24;

            /* Calculate phase */
            ushort phase = (ushort)((chip.fm_mod[slot] + (chip.pg_phase[slot] >> 10)) & 0x3ff);
            ushort quarter;
            ushort level;
            short output;

            if ((phase & 0x100) != 0)
            {
                quarter = (ushort)((phase ^ 0xff) & 0xff);
            }
            else
            {
                quarter = (ushort)(phase & 0xff);
            }
            level = logsinrom[quarter];

            /* Apply envelope */
            level += (ushort)(chip.eg_out[slot] << 2);

            /* Transform */
            if (level > 0x1fff)
            {
                level = 0x1fff;
            }
            output = (short)(((exprom[(level & 0xff) ^ 0xff] | 0x400) << 2) >> (level >> 8));
            if ((phase & 0x200) != 0)
            {
                output = (short)(((~output) ^ ((chip.mode_test_21[4] ? 1 : 0) << 13)) + 1);
            }
            else
            {
                output = (short)(output ^ ((chip.mode_test_21[4] ? 1 : 0) << 13));
            }
            output = (short)SIGN_EXTEND(13, (uint)output);
            chip.fm_out[slot] = output;
        }

        static void DoTimerA(ref ChipState chip)
        {
            ushort time;
            bool load;
            load = chip.timer_a_overflow;

            if (chip.cycles == 2)
            {
                /* Lock load value */
                load |= (!chip.timer_a_load_lock && chip.timer_a_load);
                chip.timer_a_load_lock = chip.timer_a_load;
                if (chip.mode_csm)
                {
                    /* CSM KeyOn */
                    chip.mode_kon_csm = load;
                }
                else
                {
                    chip.mode_kon_csm = false;
                }
            }

            /* Load counter */
            if (chip.timer_a_load_latch)
            {
                time = chip.timer_a_reg;
            }
            else
            {
                time = chip.timer_a_cnt;
            }
            chip.timer_a_load_latch = load;

            /* Increase counter */
            if ((chip.cycles == 1 && chip.timer_a_load_lock) || chip.mode_test_21[2])
            {
                time++;
            }

            /* Set overflow flag */
            if (chip.timer_a_reset)
            {
                chip.timer_a_reset = false;
                chip.timer_a_overflow_flag = false;
            }
            else
            {
                chip.timer_a_overflow_flag |= chip.timer_a_overflow & chip.timer_a_enable;
            }
            chip.timer_a_overflow = (time >> 10) != 0;
            chip.timer_a_cnt = (ushort)(time & 0x3ff);
        }

        static void DoTimerB(ref ChipState chip)
        {
            ushort time;
            bool load;
            load = chip.timer_b_overflow;

            if (chip.cycles == 2)
            {
                /* Lock load value */
                load |= (!chip.timer_b_load_lock && chip.timer_b_load);
                chip.timer_b_load_lock = chip.timer_b_load;
            }
            /* Load counter */
            if (chip.timer_b_load_latch)
            {
                time = chip.timer_b_reg;
            }
            else
            {
                time = chip.timer_b_cnt;
            }
            chip.timer_b_load_latch = load;
            /* Increase counter */
            if (chip.cycles == 1)
            {
                chip.timer_b_subcnt++;
            }
            if ((chip.timer_b_subcnt == 0x10 && chip.timer_b_load_lock) || chip.mode_test_21[2])
            {
                time++;
            }
            chip.timer_b_subcnt &= 0x0f;
            /* Set overflow flag */
            if (chip.timer_b_reset)
            {
                chip.timer_b_reset = false;
                chip.timer_b_overflow_flag = false;
            }
            else
            {
                chip.timer_b_overflow_flag |= chip.timer_b_overflow & chip.timer_b_enable;
            }
            chip.timer_b_overflow = (time >> 8) != 0;
            chip.timer_b_cnt = (ushort)(time & 0xff);
        }

        static void KeyOn(ref ChipState chip)
        {
            uint slot = chip.cycles;
            uint chan = chip.channel;

            /* Key On */
            chip.eg_kon_latch[slot] = chip.mode_kon[slot];
            chip.eg_kon_csm[slot] = false;
            if (chip.channel == 2 && chip.mode_kon_csm)
            {
                /* CSM Key On */
                chip.eg_kon_latch[slot] = true;
                chip.eg_kon_csm[slot] = true;
            }
            if (chip.cycles == chip.mode_kon_channel)
            {
                /* OP1 */
                chip.mode_kon[chan] = chip.mode_kon_operator[0];
                /* OP2 */
                chip.mode_kon[chan + 12] = chip.mode_kon_operator[1];
                /* OP3 */
                chip.mode_kon[chan + 6] = chip.mode_kon_operator[2];
                /* OP4 */
                chip.mode_kon[chan + 18] = chip.mode_kon_operator[3];
            }
        }
        public static void Reset(ref ChipState chip, uint rate, uint clock)
        {
            uint i;
            uint rateratio = chip.rateratio;

            for (i = 0; i < 24; i++)
            {
                chip.eg_out[i] = 0x3ff;
                chip.eg_level[i] = 0x3ff;
                chip.eg_state[i] = EGNum.release;
                chip.multi[i] = 1;
            }

            for (i = 0; i < 6; i++)
            {
                chip.pan_l[i] = true;
                chip.pan_r[i] = true;
            }

            if (rate != 0)
            {
                chip.rate = rate;
                chip.clock = clock;
                chip.rateratio = (uint)((((ulong)144 * rate) << chip.RSM_FRAC) / clock);
            }
            else
            {
                chip.rateratio = rateratio;
            }
        }

        public static void SetChipType(ChipMode type)
        {
            chip_type = type;
        }

        public static void Clock(ref ChipState chip, ref int[] buffer)
        {
            uint slot = chip.cycles;
            chip.lfo_inc = (byte)(chip.mode_test_21[1] ? 1 : 0);
            chip.pg_read >>= 1;
            chip.eg_read[1] >>= 1;
            chip.eg_cycle++;

            /* Lock envelope generator timer value */
            if (chip.cycles == 1 && chip.eg_quotient == 2)
            {
                if (chip.eg_cycle_stop)
                {
                    chip.eg_shift_lock = 0;
                }
                else
                {
                    chip.eg_shift_lock = (byte)(chip.eg_shift + 1);
                }
                chip.eg_timer_low_lock = (byte)(chip.eg_timer & 0x03);
            }
            /* Cycle specific functions */
            switch (chip.cycles)
            {
                case 0:
                    chip.lfo_pm = (byte)(chip.lfo_cnt >> 2);
                    if ((chip.lfo_cnt & 0x40) != 0)
                    {
                        chip.lfo_am = (byte)(chip.lfo_cnt & 0x3f);
                    }
                    else
                    {
                        chip.lfo_am = (byte)(chip.lfo_cnt ^ 0x3f);
                    }
                    chip.lfo_am <<= 1;
                    break;
                case 1:
                    chip.eg_quotient++;
                    chip.eg_quotient %= 3;
                    chip.eg_cycle = 0;
                    chip.eg_cycle_stop = true;
                    chip.eg_shift = 0;
                    chip.eg_timer_inc |= (byte)(chip.eg_quotient >> 1);
                    chip.eg_timer = (ushort)(chip.eg_timer + chip.eg_timer_inc);
                    chip.eg_timer_inc = (byte)(chip.eg_timer >> 12);
                    chip.eg_timer &= 0xfff;
                    break;
                case 2:
                    chip.pg_read = chip.pg_phase[21] & 0x3ff;
                    chip.eg_read[1] = chip.eg_out[0];
                    break;
                case 13:
                    chip.eg_cycle = 0;
                    chip.eg_cycle_stop = true;
                    chip.eg_shift = 0;
                    chip.eg_timer = (ushort)(chip.eg_timer + chip.eg_timer_inc);
                    chip.eg_timer_inc = (byte)(chip.eg_timer >> 12);
                    chip.eg_timer &= 0xfff;
                    break;
                case 23:
                    chip.lfo_inc |= 1;
                    break;
            }
            chip.eg_timer &= (ushort)~((chip.mode_test_21[5] ? 1 : 0) << chip.eg_cycle);
            if ((((chip.eg_timer >> chip.eg_cycle) | (chip.pin_test_in && chip.eg_custom_timer ? 1 : 0)) & (chip.eg_cycle_stop ? 1 : 0)) != 0)
            {
                chip.eg_shift = chip.eg_cycle;
                chip.eg_cycle_stop = false;
            }

            DoIO(ref chip);

            DoTimerA(ref chip);
            DoTimerB(ref chip);
            KeyOn(ref chip);

            ChOutput(ref chip);
            ChGenerate(ref chip);

            FMPrepare(ref chip);
            FMGenerate(ref chip);

            PhaseGenerate(ref chip);
            PhaseCalcIncrement(ref chip);

            EnvelopeADSR(ref chip);
            EnvelopeGenerate(ref chip);
            EnvelopeSSGEG(ref chip);
            EnvelopePrepare(ref chip);

            /* Prepare fnum & block */
            if (chip.mode_ch3 != 0)
            {
                /* Channel 3 special mode */
                switch (slot)
                {
                    case 1: /* OP1 */
                        chip.pg_fnum = chip.fnum_3ch[1];
                        chip.pg_block = chip.block_3ch[1];
                        chip.pg_kcode = chip.kcode_3ch[1];
                        break;
                    case 7: /* OP3 */
                        chip.pg_fnum = chip.fnum_3ch[0];
                        chip.pg_block = chip.block_3ch[0];
                        chip.pg_kcode = chip.kcode_3ch[0];
                        break;
                    case 13: /* OP2 */
                        chip.pg_fnum = chip.fnum_3ch[2];
                        chip.pg_block = chip.block_3ch[2];
                        chip.pg_kcode = chip.kcode_3ch[2];
                        break;
                    case 19: /* OP4 */
                    default:
                        chip.pg_fnum = chip.fnum[(chip.channel + 1) % 6];
                        chip.pg_block = chip.block[(chip.channel + 1) % 6];
                        chip.pg_kcode = chip.kcode[(chip.channel + 1) % 6];
                        break;
                }
            }
            else
            {
                chip.pg_fnum = chip.fnum[(chip.channel + 1) % 6];
                chip.pg_block = chip.block[(chip.channel + 1) % 6];
                chip.pg_kcode = chip.kcode[(chip.channel + 1) % 6];
            }

            UpdateLFO(ref chip);
            DoRegWrite(ref chip);

            chip.cycles = (chip.cycles + 1) % 24;
            chip.channel = chip.cycles % 6;

            buffer[0] = chip.mol;
            buffer[1] = chip.mor;

            if (chip.status_time > 0)
                chip.status_time--;
        }

        public static void Write(ref ChipState chip, uint port, byte data)
        {
            port &= 3;
            chip.write_data = (ushort)(((port << 7) & 0x100) | data);
            if ((port & 1) == 1)
            {
                /* Data */
                chip.write_d |= 1;
            }
            else
            {
                /* Address */
                chip.write_a |= 1;
            }
        }

        /// <summary>
        /// Writes a single data byte to a buffer.
        /// </summary>
        /// <param name="chip">The chip to write to.</param>
        /// <param name="port">The port to write to. Use 0 and 1 for Part 1 address and data, and 2 and 3 for Part 2 address and data.</param>
        /// <param name="data">The data to write.</param>
        /// <remarks>Use Write to make an immediate write.</remarks>
        public static void WriteBuffered(ref ChipState chip, uint port, byte data)
        {
            ulong time1, time2;
            ulong skip;

            int[] buffer = new int[2];

            if ((chip.writebuf[chip.writebuf_last].port & 0x04) != 0)
            {
                Write(ref chip, (uint)(chip.writebuf[chip.writebuf_last].port & 0x03), chip.writebuf[chip.writebuf_last].data);

                chip.writebuf_cur = (uint)((chip.writebuf_last + 1) % chip.WRITEBUF_SIZE);
                skip = chip.writebuf[chip.writebuf_last].time - chip.writebuf_samplecnt;
                chip.writebuf_samplecnt = chip.writebuf[chip.writebuf_last].time;

                while (skip-- > 0)
                {
                    Clock(ref chip, ref buffer);
                }
            }

            chip.writebuf[chip.writebuf_last].port = (byte)((port & 0x03) | 0x04);
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

        public static void SetTestPin(ref ChipState chip, uint value)
        {
            chip.pin_test_in = (value & 1) == 1;
        }

        public static bool ReadTestPin(ref ChipState chip)
        {
            if (!chip.mode_test_2c[7])
            {
                return false;
            }
            return chip.cycles == 23;
        }

        public static bool ReadIRQPin(ref ChipState chip)
        {
            return chip.timer_a_overflow_flag | chip.timer_b_overflow_flag;
        }

        public static byte Read(ref ChipState chip, uint port)
        {
            if ((port & 3) == 0 || ((chip_type & ChipMode.readmode) != 0))
            {
                if (chip.mode_test_21[6])
                {
                    /* Read test data */
                    uint slot = (chip.cycles + 18) % 24;
                    ushort testdata = (ushort)(((chip.pg_read & 0x01) << 15) | ((chip.eg_read[(chip.mode_test_21[0] ? 1 : 0)] & 0x01) << 14));

                    if (chip.mode_test_2c[4])
                    {
                        testdata |= (ushort)(chip.ch_read & 0x1ff);
                    }
                    else
                    {
                        testdata |= (ushort)(chip.fm_out[slot] & 0x3fff);
                    }
                    if (chip.mode_test_21[7])
                    {
                        chip.status = (byte)(testdata & 0xff);
                    }
                    else
                    {
                        chip.status = (byte)(testdata >> 8);
                    }
                }
                else
                {
                    chip.status = (byte)(((chip.busy ? 1 : 0) << 7) | ((chip.timer_b_overflow_flag ? 1 : 0) << 1) | (chip.timer_a_overflow_flag ? 1 : 0));
                }
                if ((chip_type & ChipMode.ym2612) != 0)
                {
                    chip.status_time = 300000;
                }
                else
                {
                    chip.status_time = 40000000;
                }
            }
            if (chip.status_time != 0)
            {
                return chip.status;
            }
            return 0;
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

        static void GenerateResampled(ref ChipState chip, ref int[] buf)
        {
            uint i, j;
            int[] buffer = new int[2];
            bool mute;

            while (chip.samplecnt >= chip.rateratio)
            {

                for (i = 0; i < 2; i++)
                {
                    chip.oldsamples[i] = chip.samples[i];
                }
                chip.samples[0] = chip.samples[1] = 0;

                for (i = 0; i < 24; i++)
                {
                    mute = (chip.cycles >> 2) switch
                    {
                        // Ch 2
                        0 => chip.mute[1],
                        // Ch 6, DAC
                        1 => chip.mute[5 + (chip.dacen ? 1 : 0)],
                        // Ch 4
                        2 => chip.mute[3],
                        // Ch 1
                        3 => chip.mute[0],
                        // Ch 5
                        4 => chip.mute[4],
                        // Ch 3
                        5 => chip.mute[2],
                        _ => false,
                    };
                    Clock(ref chip, ref buffer);

                    if (!mute)
                    {
                        for (j = 0; j < 2; j++)
                        {
                            chip.samples[j] += buffer[j];
                        }
                    }

                    while (chip.writebuf[chip.writebuf_cur].time <= chip.writebuf_samplecnt)
                    {
                        if ((chip.writebuf[chip.writebuf_cur].port & 0x04) == 0)
                        {
                            break;
                        }
                        chip.writebuf[chip.writebuf_cur].port &= 0x03;

                        Write(ref chip, chip.writebuf[chip.writebuf_cur].port, chip.writebuf[chip.writebuf_cur].data);

                        chip.writebuf_cur = (uint)((chip.writebuf_cur + 1) % chip.WRITEBUF_SIZE);
                    }
                    chip.writebuf_samplecnt++;
                }

                // Low pass filter
                if (!use_filter)
                {
                    for (i = 0; i < 2; i++)
                    {
                        chip.samples[i] *= chip.OUTPUT_FACTOR;
                    }
                }
                else
                {
                    for (i = 0; i < 2; i++)
                    {
                        chip.samples[i] = (int)(chip.oldsamples[i] + chip.FILTER_CUTOFF_I * (chip.samples[i] * chip.OUTPUT_FACTOR_F - chip.oldsamples[i]));
                    }
                }

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

        /// <summary>
        /// Sets a mask for muting individual channels.
        /// </summary>
        /// <param name="chip">The chip to set the mask for.</param>
        /// <param name="mute">A bitmask for muting channels.</param>
        /// <remarks>A value of 0x00 enables all channels, and 0x3F mutes all channels.</remarks>
        public static void SetMute(ref ChipState chip, uint mute)
        {
            int i;
            for (i = 0; i < 7; i++)
            {
                chip.mute[i] = ((mute >> i) & 1) == 1;
            }
        }

    }

    public class YM3438
    {

    }

}