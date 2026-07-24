using System.Collections.Generic;

namespace ConvImgCpc {
	static public class Dither {
		static readonly double[,] floyd =	{	{7, 3},
										{5, 1}};
		static readonly double[,] bayer1 =	{	{1, 3},
										{4, 2 }};
		static readonly double[,] bayer2 =	{	{0, 12, 3, 15},
										{8, 4, 11, 7},
										{2, 14, 1, 13},
										{10, 6, 9, 5}};
		static readonly double[,] bayer3 =	{	{1, 9, 3, 11 },
										{13, 5, 15, 7},
										{4, 12, 2, 10},
										{16, 8, 14, 6}};
		static readonly double[,] ord1 =		{	{1, 3},
										{2, 4}};
		static readonly double[,] ord2 =		{	{8, 3, 4},
										{6, 1, 2},
										{7, 5, 9}};
		static readonly double[,] ord3 =		{	{0, 8, 2, 10},
										{12, 4, 14, 6},
										{3, 11, 1, 9},
										{15, 7, 13, 5}};
		static readonly double[,] ord4 =		{	{0,48,12,60, 3,51,15,63},
										{32,16,44,28,35,19,47,31},
										{8,56, 4,52,11,59, 7,55},
										{40,24,36,20,43,27,39,23},
										{2,50,14,62, 1,49,13,61},
										{34,18,46,30,33,17,45,29},
										{10,58, 6,54, 9,57, 5,53},
										{42,26,38,22,41,25,37,21 }};
		static readonly double[,] zigzag1 = {	{0, 4, 0},
										{3, 0, 1 },
										{0, 2, 0}};
		static readonly double[,] zigzag2 = {	{0, 4, 2, 0},
										{6, 0, 5, 3 },
										{0, 7, 1, 0}};
		static readonly double[,] zigzag3 = {	{0, 0, 0, 7, 0},
										{0, 2, 6, 9, 8 },
										{3, 0, 1, 5, 0},
										{0, 4, 0, 0, 0}};

		static readonly double[,] test0 =	{	{1.0, 16.0, 1.0, 16.0},
										{16.0, 1.0, 16.0, 1.0},
										{1, 16, 1, 16},
										{16, 1, 16, 1}};

		static readonly double[,] test1 =	{	{1, 4, 1, 4},
										{4, 1, 4, 1},
										{1, 4, 1, 4},
										{4, 1, 4, 1}};

		static readonly double[,] test2 =	{	{8, 1, 8, 1},
										{1, 8, 1, 8},
										{8, 1, 8, 1},
										{1, 8, 1, 8}};

		static readonly double[,] test3 =	{	{8, 16, 16, 8},
										{16, 8, 8, 16}};

		static readonly double[,] test4 =	{	{0, 3 },
										{0, 5 },
										{7, 1 }};

		static readonly double[,] test5 =	{	{0, 0, 7 },
										{3, 5, 1 }};

		static readonly double[,] test6 =	{	{1, 9, 4, 7 },
										{4, 7, 1, 9 },
										{1, 9, 4, 7 },
										{4, 7, 1, 9}};

		static readonly double[,] test7 =	{	{12, 11, 0 },
										{13, 10, 19 },
										{11, 13, 0}};

		static readonly double[,] test8 =	{	{3, 7, 6, 2},
										{5, 4, 1, 0}};

		static readonly double[,] test9 =	{	{1, 5, 10, 14},
										{3, 7, 8, 12},
										{13, 9, 6, 2},
										{15, 11, 4, 0}};

		static double[,] matDither;

		static public Dictionary<string, double[,]> dicMat = new Dictionary<string, double[,]>() {
			{ "Floyd-Steinberg (2x2)",	floyd},
			{ "Bayer 1 (2X2)",			bayer1},
			{ "Bayer 2 (4x4)",			bayer2},
			{ "Bayer 3 (4X4)",			bayer3},
			{ "Ordered 1 (2x2)",		ord1},
			{ "Ordered 2 (3x3)",		ord3},
			{ "Ordered 3 (4x4)",		ord4},
			{ "ZigZag1 (3x3)",			zigzag1},
			{ "ZigZag2 (4x3)",			zigzag2},
			{ "ZigZag3 (5x4)",			zigzag3},
			{ "Test0",					test0},
			{ "Test1",					test1},
			{ "Test2",					test2},
			{ "Test3",					test3},
			{ "Test4",					test4},
			{ "Test5",					test5},
			{ "Test6",					test6},
			{ "Test7",					test7},
			{ "Test8",					test8},
			{ "Test9",					test9},
		};

		static byte MinMaxByte(double value) {
			return value >= 0 ? value <= 255 ? (byte)value : (byte)255 : (byte)0;
		}

		static public int SetMatDither(Param prm) {
			int pct = prm.cpcPlus ? prm.pct << 3 : prm.pct << 1;
			if (pct > 0 && dicMat.ContainsKey(prm.methode)) {
				matDither = dicMat[prm.methode];
				double sum = 0;
				for (int y = 0; y < matDither.GetLength(1); y++)
					for (int x = 0; x < matDither.GetLength(0); x++)
						sum += matDither[x, y];

				for (int y = 0; y < matDither.GetLength(1); y++)
					for (int x = 0; x < matDither.GetLength(0); x++)
						matDither[x, y] = (matDither[x, y] * pct) / sum;
			}
			else
				pct = 0;

			return pct;
		}

		static public void DoDitherFull(DirectBitmap source, int xPix, int yPix, int Tx, RvbColor p, RvbColor choix, bool diffErr) {
			if (diffErr) {
				for (int y = 0; y < matDither.GetLength(1); y++)
					for (int x = 0; x < matDither.GetLength(0); x++)
						if (xPix + Tx * x < source.Width && yPix + 2 * y < source.Height) {
							RvbColor pix = source.GetPixelColor(xPix + Tx * x, yPix + (y << 1));
							pix.r = MinMaxByte((double)pix.r + (p.r - choix.r) * matDither[x, y] / 256.0);
							pix.v = MinMaxByte((double)pix.v + (p.v - choix.v) * matDither[x, y] / 256.0);
							pix.b = MinMaxByte((double)pix.b + (p.b - choix.b) * matDither[x, y] / 256.0);
							source.SetPixel(xPix + Tx * x, yPix + (y << 1), pix);
						}
			}
			else {
				int xm = (xPix / Tx) % matDither.GetLength(0);
				int ym = ((yPix) >> 1) % matDither.GetLength(1);
				p.r = MinMaxByte((double)p.r + matDither[xm, ym]);
				p.v = MinMaxByte((double)p.v + matDither[xm, ym]);
				p.b = MinMaxByte((double)p.b + matDither[xm, ym]);
			}
		}
	}
}
