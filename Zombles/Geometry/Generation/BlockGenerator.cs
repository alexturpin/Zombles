﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Reflection;
using Zombles.Entities;
using OpenTK;
using ResourceLibrary;

namespace Zombles.Geometry.Generation
{
    public abstract class BlockGenerator
    {
        private static BlockGenerator[] _sGenerators;

        public static BlockGenerator[] GetAll()
        {
            if (_sGenerators == null)
                FindGenerators();

            return _sGenerators;
        }

        public static bool WillAnyFit(int width, int height, bool acceptLarger = false)
        {
            if (_sGenerators == null)
                FindGenerators();

            foreach (BlockGenerator gen in _sGenerators)
                if (gen.Frequency > 0.0 && gen.WillFit(width, height, acceptLarger))
                    return true;

            return false;
        }

        public static double FitnessScore(int width, int height)
        {
            if (_sGenerators == null)
                FindGenerators();

            double total = 0.0;
            double score = 0.0;

            foreach (BlockGenerator gen in _sGenerators) {
                total += gen.Frequency;
                if (gen.WillFit(width, height, false))
                    score += gen.Frequency;
                else if (gen.WillFit(width, height, true))
                    score += gen.Frequency / 2.0f;
            }

            return score / total;
        }

        public static BlockGenerator GetRandom(int width, int height, Random rand, bool acceptLarger = false)
        {
            if (_sGenerators == null)
                FindGenerators();

            BlockGenerator cur = null;
            double freq = double.MaxValue;
            foreach (BlockGenerator gen in _sGenerators) {
                if (gen.WillFit(width, height, acceptLarger)) {
                    double val = gen.Frequency;
                    if (val <= 0.0)
                        val = double.MaxValue / 2.0;
                    else
                        val = rand.NextDouble() / val;

                    if (val < freq) {
                        cur = gen;
                        freq = val;
                    }
                }
            }

            return cur;
        }

        public static BlockGenerator[] GetAllRandom(int width, int height, Random rand)
        {
            if (_sGenerators == null)
                FindGenerators();

            List<BlockGenerator> gens = new List<BlockGenerator>();
            foreach (BlockGenerator gen in _sGenerators)
                if (gen.WillFit(width, height))
                    gens.Add(gen);

            return gens.OrderBy(x => x.Frequency <= 0.0
                ? double.MaxValue : rand.NextDouble() / x.Frequency).ToArray();
        }

        public static void FindGenerators()
        {
            Type[] types = ScriptManager.GetTypes(typeof(BlockGenerator));
            List<BlockGenerator> valid = new List<BlockGenerator>();

            for (int i = 0; i < types.Length; ++i) {
                ConstructorInfo cns = types[i].GetConstructor(new Type[0]);
                if (cns != null)
                    valid.Add(cns.Invoke(new object[0]) as BlockGenerator);
            }

            _sGenerators = valid.ToArray();
        }

        public double Frequency { get; private set; }

        public int MinShortSide { get; private set; }
        public int MinLongSide { get; private set; }
        public int MaxShortSide { get; private set; }
        public int MaxLongSide { get; private set; }

        protected BlockGenerator(double frequency, int minSide, int maxSide)
        {
            Frequency = frequency;

            MinShortSide = minSide;
            MinLongSide = minSide;
            MaxShortSide = maxSide;
            MaxLongSide = maxSide;
        }

        protected BlockGenerator(double frequency, int minShortSide, int minLongSide, int maxShortSide, int maxLongSide)
        {
            Frequency = frequency;

            MinShortSide = minShortSide;
            MinLongSide = minLongSide;
            MaxShortSide = maxShortSide;
            MaxLongSide = maxLongSide;
        }

        public bool WillFit(int width, int height, bool acceptLarger = false)
        {
            int sh = Math.Min(width, height);
            int ln = Math.Max(width, height);

            return sh >= MinShortSide && ln >= MinLongSide &&
                (acceptLarger || (ln <= MaxLongSide && sh <= MaxShortSide));
        }

        public Block Generate(District district, int borderLeft, int borderTop,
            int borderRight, int borderBottom, Random rand)
        {
            int width = district.Width;
            int height = district.Height;

            TileBuilder[,] tiles = new TileBuilder[width, height];

            for (int tx = 0; tx < width; ++tx) for (int ty = 0; ty < height; ++ty)
                    tiles[tx, ty] = new TileBuilder();

            int innerLeft = borderLeft;
            int innerTop = borderTop;
            int innerRight = width - borderRight;
            int innerBottom = height - borderBottom;
            int innerWidth = width - borderLeft - borderRight;
            int innerHeight = height - borderTop - borderBottom;

            Func<int, int, ResourceLocator> roadFunc = delegate(int tx, int ty) {
                return rand.NextTexture("floor/road/", 0, 4);
            };

            GenHelper.BuildFloor(tiles, 0, 0, width, height, 0, roadFunc);
            GenHelper.BuildFloor(tiles, innerLeft - 1, innerTop - 1, innerWidth + 2, innerHeight + 2, 0,
                (x, y) => {
                    if (Tools.Random.NextDouble() < 0.75) {
                        return "floor/concrete/0";
                    } else {
                        return Tools.Random.NextTexture("floor/concrete", 1, 4);
                    }
                });

            int texOffset = 0; bool horz = false;
            Func<int, int, ResourceLocator> pavementFunc = delegate(int tx, int ty) {
                if ((horz && tx % 8 == 4) || (!horz && ty % 8 == 4))
                    return "floor/pavement/" + (texOffset + 2).ToString("X").ToLower();

                return rand.NextTexture("floor/pavement/", texOffset, texOffset + 2);
            };

            if (borderBottom > 1) {
                texOffset = 0; horz = true;
                GenHelper.BuildFloor(tiles, innerLeft - 1, innerBottom, innerWidth + 2, 1, 0, pavementFunc);
            }
            if (borderRight > 1) {
                texOffset = 4; horz = false;
                GenHelper.BuildFloor(tiles, innerRight, innerTop - 1, 1, innerHeight + 2, 0, pavementFunc);
            }
            if (borderTop > 1) {
                texOffset = 8; horz = true;
                GenHelper.BuildFloor(tiles, innerLeft - 1, innerTop - 1, innerWidth + 2, 1, 0, pavementFunc);
            }
            if (borderLeft > 1) {
                texOffset = 12; horz = false;
                GenHelper.BuildFloor(tiles, innerLeft - 1, innerTop - 1, 1, innerHeight + 2, 0, pavementFunc);
            }

            if (borderBottom > 1 && borderRight > 1)
                GenHelper.BuildFloor(tiles, innerRight, innerBottom, 1, 1, 0, "floor/pavement/3");
            if (borderTop > 1 && borderRight > 1)
                GenHelper.BuildFloor(tiles, innerRight, innerTop - 1, 1, 1, 0, "floor/pavement/7");
            if (borderTop > 1 && borderLeft > 1)
                GenHelper.BuildFloor(tiles, innerLeft - 1, innerTop - 1, 1, 1, 0, "floor/pavement/b");
            if (borderBottom > 1 && borderLeft > 1)
                GenHelper.BuildFloor(tiles, innerLeft - 1, innerBottom, 1, 1, 0, "floor/pavement/f");

            Generate(district, tiles, borderLeft, borderTop, borderRight, borderBottom, rand);

            Block block = new Block(district);
            block.BuildTiles(tiles);
            return block;
        }

        protected abstract void Generate(District district, TileBuilder[,] tiles, int borderLeft, int borderTop,
            int borderRight, int borderBottom, Random rand);
    }
}
