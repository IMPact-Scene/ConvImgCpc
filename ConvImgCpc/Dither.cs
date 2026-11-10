using System;
using System.Collections.Generic;

namespace ConvImgCpc {
	static public class Dither {
		static readonly double[,] floyd =   {   {7, 3},
										{5, 1}};
		static readonly double[,] bayer1 =  {   {1, 3},
										{4, 2 }};
		static readonly double[,] bayer2 =  {   {0, 12, 3, 15},
										{8, 4, 11, 7},
										{2, 14, 1, 13},
										{10, 6, 9, 5}};
		static readonly double[,] bayer3 =  {   {1, 9, 3, 11 },
										{13, 5, 15, 7},
										{4, 12, 2, 10},
										{16, 8, 14, 6}};
		static readonly double[,] ord1 =        {   {1, 3},
										{2, 4}};
		static readonly double[,] ord2 =        {   {8, 3, 4},
										{6, 1, 2},
										{7, 5, 9}};
		static readonly double[,] ord3 =        {   {0, 8, 2, 10},
										{12, 4, 14, 6},
										{3, 11, 1, 9},
										{15, 7, 13, 5}};
		static readonly double[,] ord4 =        {   {0,48,12,60, 3,51,15,63},
										{32,16,44,28,35,19,47,31},
										{8,56, 4,52,11,59, 7,55},
										{40,24,36,20,43,27,39,23},
										{2,50,14,62, 1,49,13,61},
										{34,18,46,30,33,17,45,29},
										{10,58, 6,54, 9,57, 5,53},
										{42,26,38,22,41,25,37,21 }};
		static readonly double[,] zigzag1 = {   {0, 4, 0},
										{3, 0, 1 },
										{0, 2, 0}};
		static readonly double[,] zigzag2 = {   {0, 4, 2, 0},
										{6, 0, 5, 3 },
										{0, 7, 1, 0}};
		static readonly double[,] zigzag3 = {   {0, 0, 0, 7, 0},
										{0, 2, 6, 9, 8 },
										{3, 0, 1, 5, 0},
										{0, 4, 0, 0, 0}};

		static readonly double[,] test0 =   {   {1.0, 16.0, 1.0, 16.0},
										{16.0, 1.0, 16.0, 1.0},
										{1, 16, 1, 16},
										{16, 1, 16, 1}};

		static readonly double[,] test1 =   {   {1, 4, 1, 4},
										{4, 1, 4, 1},
										{1, 4, 1, 4},
										{4, 1, 4, 1}};

		static readonly double[,] test2 =   {   {8, 1, 8, 1},
										{1, 8, 1, 8},
										{8, 1, 8, 1},
										{1, 8, 1, 8}};

		static readonly double[,] test3 =   {   {8, 16, 16, 8},
										{16, 8, 8, 16}};

		static readonly double[,] test4 =   {   {0, 3 },
										{0, 5 },
										{7, 1 }};

		static readonly double[,] test5 =   {   {0, 0, 7 },
										{3, 5, 1 }};

		static readonly double[,] test6 =   {   {1, 9, 4, 7 },
										{4, 7, 1, 9 },
										{1, 9, 4, 7 },
										{4, 7, 1, 9}};

		static readonly double[,] test7 =   {   {12, 11, 0 },
										{13, 10, 19 },
										{11, 13, 0}};

		static readonly double[,] test8 =   {   {3, 7, 6, 2},
										{5, 4, 1, 0}};

		static readonly double[,] test9 =   {   {1, 5, 10, 14},
										{3, 7, 8, 12},
										{13, 9, 6, 2},
										{15, 11, 4, 0}};

		static double[,] matDither;
		static double matMean;                        // moyenne de la matrice normalisée (tramage ordonné)

		// Erreurs accumulées (en réel, par pixel de la source) : évite les arrondis de l'image 8 bits
		static float[] errR, errV, errB;
		static int errW, errH;
		static bool errValid;
		static readonly float[] tblLin = BuildTblLin();

		static public Dictionary<string, double[,]> dicMat = new Dictionary<string, double[,]>() {
			{ "Floyd-Steinberg (2x2)",  floyd},
			{ "Bayer 1 (2X2)",          bayer1},
			{ "Bayer 2 (4x4)",          bayer2},
			{ "Bayer 3 (4X4)",          bayer3},
			{ "Ordered 1 (2x2)",        ord1},
			{ "Ordered 2 (3x3)",        ord3},
			{ "Ordered 3 (4x4)",        ord4},
			{ "ZigZag1 (3x3)",          zigzag1},
			{ "ZigZag2 (4x3)",          zigzag2},
			{ "ZigZag3 (5x4)",          zigzag3},
			{ "Test0",                  test0},
			{ "Test1",                  test1},
			{ "Test2",                  test2},
			{ "Test3",                  test3},
			{ "Test4",                  test4},
			{ "Test5",                  test5},
			{ "Test6",                  test6},
			{ "Test7",                  test7},
			{ "Test8",                  test8},
			{ "Test9",                  test9},
		};

		static byte MinMaxByte(double value) {
			return value >= 0 ? value <= 255 ? (byte)(value + 0.5) : (byte)255 : (byte)0;   // arrondi (avant : troncature)
		}

		static float[] BuildTblLin() {
			float[] t = new float[256];
			for (int i = 0; i < 256; i++) {
				double c = i / 255.0;
				t[i] = (float)(c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4));
			}
			return t;
		}

		static byte LinearToByte(double x) {
			if (x <= 0)
				return 0;

			if (x >= 1)
				return 255;

			double c = x <= 0.0031308 ? 12.92 * x : 1.055 * Math.Pow(x, 1.0 / 2.4) - 0.055;
			return (byte)(c * 255.0 + 0.5);
		}

		static public int SetMatDither(Param prm) {
			errValid = false;       // les erreurs accumulées sont remises à zéro à chaque nouveau balayage
			int pct = prm.cpcPlus ? prm.pct << 3 : prm.pct << 1;
			if (pct > 0 && dicMat.ContainsKey(prm.methode)) {
				// Copie de travail : les matrices du dictionnaire ne sont plus modifiées
				matDither = (double[,])dicMat[prm.methode].Clone();
				int w = matDither.GetLength(0), h = matDither.GetLength(1);
				bool skipSelf = prm.diffErr;
				double sum = 0;
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++)
						if (!(skipSelf && x == 0 && y == 0))
							sum += matDither[x, y];

				if (sum <= 0)
					return 0;

				double total = 0;
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++) {
						matDither[x, y] = skipSelf && x == 0 && y == 0 ? 0 : (matDither[x, y] * pct) / sum;
						total += matDither[x, y];
					}
				matMean = total / (w * h);
			}
			else
				pct = 0;

			return pct;
		}

		//
		// Ajoute à la couleur p (déjà corrigée : luminosité, contraste...) l'erreur diffusée par les pixels précédents.
		// L'erreur est consommée : elle n'est appliquée qu'une seule fois.
		//
		static public void ApplyError(DirectBitmap source, int xPix, int yPix, RvbColor p) {
			if (!errValid || errW != source.Width || errH != source.Height || xPix >= errW || yPix >= errH)
				return;

			int i = xPix + yPix * errW;
			float er = errR[i], ev = errV[i], eb = errB[i];
			if (er == 0 && ev == 0 && eb == 0)
				return;

			errR[i] = errV[i] = errB[i] = 0;
			p.r = LinearToByte(tblLin[p.r] + er);
			p.v = LinearToByte(tblLin[p.v] + ev);
			p.b = LinearToByte(tblLin[p.b] + eb);
		}

		static public void DoDitherFull(DirectBitmap source, int xPix, int yPix, int Tx, RvbColor p, RvbColor choix, bool diffErr) {
			DoDitherFull(source, xPix, yPix, Tx, 2, p, choix, diffErr);
		}

		// Ty : nombre de lignes de la source pour un pas vertical de la matrice (2 = pixel CPC standard, 8 = blocs ASCII)
		static public void DoDitherFull(DirectBitmap source, int xPix, int yPix, int Tx, int Ty, RvbColor p, RvbColor choix, bool diffErr) {
			int w = matDither.GetLength(0), h = matDither.GetLength(1);
			if (diffErr) {
				// Diffusion de l'erreur (p - choix) vers les pixels suivants, dans le tampon d'erreurs

				if (!errValid || errW != source.Width || errH != source.Height) {
					int n = source.Width * source.Height;
					if (errR == null || errR.Length != n) {
						errR = new float[n];
						errV = new float[n];
						errB = new float[n];
					}
					else {
						Array.Clear(errR, 0, n);
						Array.Clear(errV, 0, n);
						Array.Clear(errB, 0, n);
					}
					errW = source.Width;
					errH = source.Height;
					errValid = true;
				}
				double er, ev, eb;
				er = tblLin[p.r] - tblLin[choix.r];
				ev = tblLin[p.v] - tblLin[choix.v];
				eb = tblLin[p.b] - tblLin[choix.b];
				for (int y = 0; y < h; y++)
					for (int x = 0; x < w; x++) {
						double m = matDither[x, y];
						int px = xPix + Tx * x, py = yPix + Ty * y;
						if (m != 0 && px < source.Width && py < source.Height) {
							int i = px + py * errW;
							double k = m / 256.0;
							errR[i] += (float)(er * k);
							errV[i] += (float)(ev * k);
							errB[i] += (float)(eb * k);
						}
					}
			}
			else {
				int xm = (xPix / Tx) % w;
				int ym = (yPix / Ty) % h;
				double off = matDither[xm, ym] - matMean;
				p.r = MinMaxByte(p.r + off);
				p.v = MinMaxByte(p.v + off);
				p.b = MinMaxByte(p.b + off);
			}
		}
	}
}
