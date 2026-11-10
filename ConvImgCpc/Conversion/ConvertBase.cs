using System;
using System.Collections.Generic;

namespace ConvImgCpc {
	public static partial class Conversion {
		public enum Distance { DISTANCE_SUP = 0, DISTANCE_EUCLIDE = 1, DISTANCE_MANHATTAN = 2 };

		static private readonly int[,] coulTrouvee = new int[4096, 272];
		static private readonly byte[] tblContrast = new byte[256];
		static public Niveau[] niveaux;

		static public double OptCountExponent = 0.8;    // poids des couleurs peu fréquentes (1 = proportionnel au nombre de pixels)

		static private uint[] backupBits = null;
		static private bool perceptualReady = false;
		static private readonly float[] tblLinear = BuildTblLinear();
		static private readonly double[] cpcLabL = new double[27];
		static private readonly double[] cpcLabA = new double[27];
		static private readonly double[] cpcLabB = new double[27];
		static private double[] hwL, hwA, hwB;          // OKLab de toutes les couleurs matérielles (27 ou 4096)
		static private bool redoActive = false;         // les convertisseurs (X, split, ASCII) font eux-mêmes le tramage final
		static private int redoPct = 0;
		static private bool inConvert = false;          // vrai uniquement pendant Convert() : CnvTrame appelle ConvertPasse1 seule
		static private bool blkValid = false;           // mode ASC-UT : couleurs moyennes des blocs mémorisées
		private static readonly List<double> blkL = new List<double>();
		static private readonly List<double> blkA = new List<double>();
		static private readonly List<double> blkB = new List<double>();

		static private float[] BuildTblLinear() {
			float[] t = new float[256];
			for (int i = 0; i < 256; i++) {
				double c = i / 255.0;
				t[i] = (float)(c <= 0.04045 ? c / 12.92 : Math.Pow((c + 0.055) / 1.055, 2.4));
			}
			return t;
		}

		static private byte LinearToByte(double x) {
			if (x <= 0)
				return 0;

			if (x >= 1)
				return 255;

			double c = x <= 0.0031308 ? 12.92 * x : 1.055 * Math.Pow(x, 1.0 / 2.4) - 0.055;
			return (byte)(c * 255.0 + 0.5);
		}

		// Couleur RVB (sRGB 0-255) vers OKLab
		static private void LabFromRvb(int r, int v, int b, out double oL, out double oA, out double oB) {
			double lr = tblLinear[r], lg = tblLinear[v], lb = tblLinear[b];
			double l = Math.Pow(0.4122214708 * lr + 0.5363325363 * lg + 0.0514459929 * lb, 1.0 / 3.0);
			double m = Math.Pow(0.2119034982 * lr + 0.6806995451 * lg + 0.1073969566 * lb, 1.0 / 3.0);
			double s = Math.Pow(0.0883024619 * lr + 0.2817188376 * lg + 0.6299787005 * lb, 1.0 / 3.0);
			oL = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
			oA = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
			oB = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
		}

		static private void InitPerceptual() {
			for (int i = 0; i < 27; i++) {
				RvbColor c = Cpc.RgbCPC[i];
				LabFromRvb(c.r, c.v, c.b, out cpcLabL[i], out cpcLabA[i], out cpcLabB[i]);
			}
			int nb = Cpc.cpcPlus ? 4096 : 27;
			if (hwL == null || hwL.Length != nb) {
				hwL = new double[nb];
				hwA = new double[nb];
				hwB = new double[nb];
			}
			for (int i = 0; i < nb; i++) {
				RvbColor c = Cpc.GetColor(i);
				LabFromRvb(c.r, c.v, c.b, out hwL[i], out hwA[i], out hwB[i]);
			}
			perceptualReady = true;
		}

		// Quantifie une composante sur 'bits' bits (arrondi au plus proche, ou tronqué comme avant si OptRoundQuant = false)
		static private byte QuantComp(int v, int bits) {
			int n = (1 << bits) - 1;
			return (byte)Math.Round(Math.Round(v * n / 255.0) * 255.0 / n);
		}

		static byte MinMaxByte(double value) {
			return value >= 0 ? value <= 255 ? (byte)value : (byte)255 : (byte)0;
		}

		// Modification luminosité / saturation
		static private void SetLumiSat(float lumi, float satur, ref float r, ref float v, ref float b) {
			float min = Math.Min(r, Math.Min(v, b));
			float max = Math.Max(r, Math.Max(v, b));
			float dif = max - min;
			float hue = 0;
			if (max > min) {
				hue = v == max ? (b - r) / dif * 60f + 120f : b == max ? (r - v) / dif * 60f + 240f : (v - b) / dif * 60f + (b > v ? 360f : 0);
				if (hue < 0)
					hue += 360f;
			}
			hue *= 255f / 360f;
			float sat = satur * (dif / max) * 255f;
			float bri = lumi * max;
			r = v = b = bri;
			if (sat != 0) {
				max = bri;
				dif = bri * sat / 255f;
				min = bri - dif;
				float h = hue * 360f / 255f;
				if (h < 60f) {
					r = max;
					v = h * dif / 60f + min;
					b = min;
				}
				else
					if (h < 120f) {
						r = -(h - 120f) * dif / 60f + min;
						v = max;
						b = min;
					}
					else
						if (h < 180f) {
							r = min;
							v = max;
							b = (h - 120f) * dif / 60f + min;
						}
						else
							if (h < 240f) {
								r = min;
								v = -(h - 240f) * dif / 60f + min;
								b = max;
							}
							else
								if (h < 300f) {
									r = (h - 240f) * dif / 60f + min;
									v = min;
									b = max;
								}
								else
									if (h <= 360f) {
										r = max;
										v = min;
										b = -(h - 360f) * dif / 60 + min;
									}
									else
										r = v = b = 0;
			}
		}

		// Retourne la couleur CPC (0-26) la plus proche
		static private int GetNumColorPixelCpc(Param prm, RvbColor p) {
			int indexChoix = 0;

			if (!prm.newMethode)
				indexChoix = (p.r > prm.cstR2 ? 6 : p.r > prm.cstR1 ? 3 : 0) + (p.b > prm.cstB2 ? 2 : p.b > prm.cstB1 ? 1 : 0) + (p.v > prm.cstV2 ? 18 : p.v > prm.cstV1 ? 9 : 0);
			else {
				if (!perceptualReady)
					InitPerceptual();

				LabFromRvb(p.r, p.v, p.b, out double pL, out double pA, out double pB);
				double oldDistL = double.MaxValue;
				for (int i = 0; i < 27; i++) {
					double dl = pL - cpcLabL[i], da = pA - cpcLabA[i], db = pB - cpcLabB[i];
					double d = dl * dl + da * da + db * db;
					if (d < oldDistL) {
						oldDistL = d;
						indexChoix = i;
						if (d == 0)
							break;
					}
				}
			}
			return indexChoix;
		}

		// Lecture d'un pixel de la source avec tous les réglages (sans tramage)
		static private RvbColor GetPixelAdjRaw(DirectBitmap source, int xPix, int yPix, int Tx, Param prm) {
			RvbColor p = new RvbColor(0);
			if (prm.lissage) {
				double lr = 0, lv = 0, lb = 0;
				for (int i = 0; i < Tx; i++) {
					p = source.GetPixelColor(xPix + i, yPix);
					lr += tblLinear[p.r];
					lv += tblLinear[p.v];
					lb += tblLinear[p.b];
				}
				p.r = LinearToByte(lr / Tx);
				p.v = LinearToByte(lv / Tx);
				p.b = LinearToByte(lb / Tx);
			}
			else
				p = source.GetPixelColor(xPix, yPix);

			switch (prm.bitsRVB) {
				//12 bits (4 bits par composantes)
				case 12:
					p.r = QuantComp(p.r, 4);
					p.v = QuantComp(p.v, 4);
					p.b = QuantComp(p.b, 4);
					break;

				// 9 bits (3 bits par composantes)
				case 9:
					p.r = QuantComp(p.r, 3);
					p.v = QuantComp(p.v, 3);
					p.b = QuantComp(p.b, 3);
					break;

				// 6 bits (2 bits par composantes)
				case 6:
					p.r = QuantComp(p.r, 2);
					p.v = QuantComp(p.v, 2);
					p.b = QuantComp(p.b, 2);
					break;
			}

			int m1 = (prm.reductPal1 ? 0x11 : 0x00) | (prm.reductPal3 ? 0x22 : 0x00);
			int m2 = (prm.reductPal2 ? 0xEE : 0xFF) & (prm.reductPal4 ? 0xDD : 0xFF);
			p.r = MinMaxByte(((p.r | m1) & m2) * prm.pctRed / 100.0);
			p.v = MinMaxByte(((p.v | m1) & m2) * prm.pctGreen / 100.0);
			p.b = MinMaxByte(((p.b | m1) & m2) * prm.pctBlue / 100.0);
			if (p.r != 0 || p.v != 0 || p.b != 0) {
				float r = tblContrast[p.r];
				float v = tblContrast[p.v];
				float b = tblContrast[p.b];
				if (prm.pctLumi != 100 || prm.pctSat != 100)
					SetLumiSat(prm.pctLumi > 100 ? (100 + (prm.pctLumi - 100) * 2) / 100.0F : prm.pctLumi / 100.0F, prm.pctSat / 100.0F, ref r, ref v, ref b);

				p.r = MinMaxByte(r);
				p.v = MinMaxByte(v);
				p.b = MinMaxByte(b);
			}

			return p;
		}

		// Idem, avec en plus l'erreur de tramage diffusée par les pixels précédents (appliquée après les réglages)
		static private RvbColor GetPixelAdj(DirectBitmap source, int xPix, int yPix, int Tx, Param prm) {
			RvbColor p = GetPixelAdjRaw(source, xPix, yPix, Tx, prm);
			Dither.ApplyError(source, xPix, yPix, p);
			return p;
		}

		static private RvbColor GetPixel(DirectBitmap source, int xPix, int yPix, int Tx, Param prm, int pct) {
			RvbColor p = GetPixelAdj(source, xPix, yPix, Tx, prm);

			// Appliquer la matrice de tramage
			if (pct > 0) {
				RvbColor choix = prm.cpcPlus ? new RvbColor(QuantComp(p.r, 4), QuantComp(p.v, 4), QuantComp(p.b, 4)) : Cpc.RgbCPC[GetNumColorPixelCpc(prm, p)];
				Dither.DoDitherFull(source, xPix, yPix, Tx, p, choix, prm.diffErr);
			}
			return p;
		}

		//
		// Passe 1 : Réduit la palette aux x couleurs de la palette du CPC.
		// Remplit le tableau coulTrouvee avec ces couleurs
		// Effectue également un traitement de l'erreur (tramage) si demandé.
		//
		static private void ConvertPasse1(DirectBitmap source, Param prm) {
			Array.Clear(coulTrouvee, 0, coulTrouvee.Length);
			int pct = Dither.SetMatDither(prm);
			bool converterRead = OptUseConverterRead(prm);
			if (converterRead)
				pct = 0;    // le tramage sera calculé plus tard, avec la palette finale

			bool blockHist = converterRead && Cpc.modeVirtuel >= 7;     // modes ASC-UT et ASCII : histogramme des couleurs moyennes des blocs
			blkL.Clear();
			blkA.Clear();
			blkB.Clear();
			blkValid = blockHist && Cpc.modeVirtuel == 7;
			RvbColor p, choix, n1, n2;
			int indexChoix;
			int incY = prm.trameTc ? 4 : 2;
			for (int yPix = 0; yPix < Cpc.TailleY; yPix += incY) {
				if (blockHist && (yPix & 7) != 0)
					continue;
				int Tx = Cpc.CalcTx(yPix);
				for (int xPix = 0; xPix < Cpc.TailleX; xPix += Tx) {
					if (blockHist) {
						int bw = Tx;
						if (Cpc.modeVirtuel == 7) {     // ASC-UT : blocs de 8 pixels de large
							if ((xPix & 7) != 0)
								continue;
							bw = 8;
						}
						p = BlockAverageRaw(source, prm, xPix, yPix, bw, 8);
						if (prm.cpcPlus) {
							choix = new RvbColor(QuantComp(p.r, 4), QuantComp(p.v, 4), QuantComp(p.b, 4));
							indexChoix = ((choix.v << 4) & 0xF00) + ((choix.b) & 0xF0) + ((choix.r) >> 4);
						}
						else
							indexChoix = GetNumColorPixelCpc(prm, p);
						coulTrouvee[indexChoix, yPix >> 1] += 4;
						if (blkValid) {
							LabFromRvb(p.r, p.v, p.b, out double bl, out double ba, out double bb);
							blkL.Add(bl);
							blkA.Add(ba);
							blkB.Add(bb);
						}
						continue;
					}
					for (int yy = 0; yy < incY; yy += 2) {
						p = GetPixel(source, xPix, yy + yPix, Tx, prm, pct);
						// Recherche le point dans la couleur cpc la plus proche
						if (prm.cpcPlus) {
							choix = new RvbColor(QuantComp(p.r, 4), QuantComp(p.v, 4), QuantComp(p.b, 4));
							indexChoix = ((choix.v << 4) & 0xF00) + ((choix.b) & 0xF0) + ((choix.r) >> 4);
						}
						else {
							indexChoix = GetNumColorPixelCpc(prm, p);
							choix = Cpc.RgbCPC[indexChoix];
						}
						coulTrouvee[indexChoix, yPix >> 1]++;
						if (!prm.trameTc)
							source.SetPixel(xPix, yy + yPix, prm.setPalCpc ? choix : p);
					}
					if (prm.trameTc) {
						// Moyenne des 2 pixels
						RvbColor p0 = GetPixel(source, xPix, yPix, Tx, prm, pct);
						RvbColor p1 = GetPixel(source, xPix, yPix + 2, Tx, prm, pct);
						int r = (p0.r + p1.r);
						int v = (p0.v + p1.v);
						int b = (p0.b + p1.b);
						if (prm.cpcPlus) {
							n1 = new RvbColor((byte)(r >> 1), (byte)(v >> 1), (byte)(b >> 1));
							n2 = new RvbColor((byte)(Math.Min(255, (r >> 3) * 8)), (byte)(Math.Min(255, (v >> 3) * 8)), (byte)(Math.Min(255, (b >> 3) * 8)));
						}
						else {
							n1 = Cpc.RgbCPC[(r > prm.cstR3 ? 6 : r > prm.cstR1 ? 3 : 0) + (v > prm.cstV3 ? 18 : v > prm.cstV1 ? 9 : 0) + (b > prm.cstB3 ? 2 : b > prm.cstB1 ? 1 : 0)];
							n2 = Cpc.RgbCPC[(r > prm.cstR4 ? 6 : r > prm.cstR2 ? 3 : 0) + (v > prm.cstV4 ? 18 : v > prm.cstV2 ? 9 : 0) + (b > prm.cstB4 ? 2 : b > prm.cstB2 ? 1 : 0)];
						}
						source.SetPixel(xPix, yPix, (xPix & Tx) == 0 ? n1 : n2);
						source.SetPixel(xPix, yPix + 2, (xPix & Tx) == 0 ? n2 : n1);
					}
				}
			}
		}

		//
		// Choix optimisé de la palette : minimise l'erreur de quantification perceptuelle pondérée par le
		// nombre de pixels, au lieu de prendre les couleurs les plus fréquentes. Respecte les crayons
		// verrouillés et désactivés. Les crayons sont numérotés par fréquence décroissante, comme avant.
		//
		static private void RechercheOptimisee(int maxPen, int[] lockState, Param prm, int findMax) {
			double[] count = new double[findMax];
			double total = 0;
			for (int i = 0; i < findMax; i++) {
				for (int y = 0; y < 272; y++)
					count[i] += coulTrouvee[i, y];
				total += count[i];
			}
			if (total == 0)
				return;

			// Couleurs verrouillées
			int[] locked = new int[maxPen];
			bool[] seen = new bool[findMax];
			int nLocked = 0, nbFree = 0;
			for (int x = 0; x < maxPen; x++) {
				if (lockState[x] > 0) {
					int col = Cpc.Palette[x];
					if (col >= 0 && col < findMax && !seen[col]) {
						seen[col] = true;
						locked[nLocked++] = col;
					}
				}
				else if (prm.disableState[x] == 0)
					nbFree++;
			}
			if (nbFree == 0)
				return;

			int[] chosen = ChooseColors(count, findMax, locked, nLocked, nbFree, 400, 4);

			int next = 0;
			for (int x = 0; x < maxPen && next < chosen.Length; x++)
				if (lockState[x] == 0 && prm.disableState[x] == 0)
					Cpc.Palette[x] = chosen[next++];

			// Même effet de bord que l'ancienne méthode : les couleurs choisies sont retirées de l'histogramme
			for (int j = 0; j < chosen.Length; j++)
				for (int y = 0; y < 272; y++)
					coulTrouvee[chosen[j], y] = 0;
		}

		//
		// Choisit jusqu'à nFree couleurs matérielles (en plus des nFixed couleurs imposées) qui minimisent
		// l'erreur perceptuelle pondérée sur l'histogramme 'count' (sélection gloutonne puis échanges).
		// Retourne les couleurs choisies, de la plus fréquente à la moins fréquente.
		//
		static private int[] ChooseColors(double[] count, int findMax, int[] fixedIdx, int nFixed, int nFree, int candMax, int maxPass) {
			if (hwL == null || hwL.Length < findMax)
				InitPerceptual();

			int n = 0;
			for (int i = 0; i < findMax; i++)
				if (count[i] > 0)
					n++;
			if (n == 0 || nFree <= 0)
				return new int[0];

			int[] ptIdx = new int[n];
			double[] w = new double[n], pl = new double[n], pa = new double[n], pb = new double[n], keyCount = new double[n];
			int k = 0;
			for (int i = 0; i < findMax; i++) {
				if (count[i] > 0) {
					ptIdx[k] = i;
					w[k] = Math.Pow(count[i], OptCountExponent);
					keyCount[k] = -count[i];
					pl[k] = hwL[i];
					pa[k] = hwA[i];
					pb[k] = hwB[i];
					k++;
				}
			}

			// Couleurs candidates : les 27 couleurs CPC, ou les plus utilisées en CPC+
			int nbCand;
			int[] candIdx;
			if (findMax <= 27) {
				nbCand = findMax;
				candIdx = new int[nbCand];
				for (int i = 0; i < nbCand; i++)
					candIdx[i] = i;
			}
			else {
				int[] sorted = (int[])ptIdx.Clone();
				double[] keys = (double[])keyCount.Clone();
				Array.Sort(keys, sorted);
				nbCand = Math.Min(n, candMax);
				candIdx = new int[nbCand];
				Array.Copy(sorted, candIdx, nbCand);
			}
			double[] cl = new double[nbCand], ca = new double[nbCand], cb = new double[nbCand];
			for (int c = 0; c < nbCand; c++) {
				cl[c] = hwL[candIdx[c]];
				ca[c] = hwA[candIdx[c]];
				cb[c] = hwB[candIdx[c]];
			}

			// Membres : couleurs imposées, puis couleurs choisies
			int maxMem = nFixed + nFree;
			int[] mIdx = new int[maxMem];
			double[] ml = new double[maxMem], ma = new double[maxMem], mb = new double[maxMem];
			bool[] isMem = new bool[findMax];
			int nMem = 0;
			for (int f = 0; f < nFixed; f++) {
				int col = fixedIdx[f];
				if (col >= 0 && col < findMax && !isMem[col]) {
					mIdx[nMem] = col;
					ml[nMem] = hwL[col];
					ma[nMem] = hwA[col];
					mb[nMem] = hwB[col];
					isMem[col] = true;
					nMem++;
				}
			}
			int nLocked = nMem;

			// Distance de chaque point à la couleur la plus proche déjà choisie
			double[] curD = new double[n];
			for (int i = 0; i < n; i++) {
				curD[i] = 1e30;
				for (int m = 0; m < nMem; m++) {
					double dl = pl[i] - ml[m], da = pa[i] - ma[m], db = pb[i] - mb[m];
					double d = dl * dl + da * da + db * db;
					if (d < curD[i])
						curD[i] = d;
				}
			}

			// Sélection gloutonne : on ajoute à chaque étape la couleur qui réduit le plus l'erreur totale
			for (int f = 0; f < nFree; f++) {
				if (nMem > 0) {
					double curCost = 0;
					for (int i = 0; i < n; i++)
						curCost += w[i] * curD[i];
					if (curCost < 1e-9)
						break;      // tout est déjà parfaitement représenté
				}
				int bestC = -1;
				double bestCost = double.MaxValue;
				for (int c = 0; c < nbCand; c++) {
					if (isMem[candIdx[c]])
						continue;
					double cost = 0;
					for (int i = 0; i < n; i++) {
						double dl = pl[i] - cl[c], da = pa[i] - ca[c], db = pb[i] - cb[c];
						double d = dl * dl + da * da + db * db;
						cost += w[i] * (d < curD[i] ? d : curD[i]);
						if (cost >= bestCost)
							break;
					}
					if (cost < bestCost) {
						bestCost = cost;
						bestC = c;
					}
				}
				if (bestC < 0)
					break;
				mIdx[nMem] = candIdx[bestC];
				ml[nMem] = cl[bestC];
				ma[nMem] = ca[bestC];
				mb[nMem] = cb[bestC];
				isMem[mIdx[nMem]] = true;
				nMem++;
				for (int i = 0; i < n; i++) {
					double dl = pl[i] - cl[bestC], da = pa[i] - ca[bestC], db = pb[i] - cb[bestC];
					double d = dl * dl + da * da + db * db;
					if (d < curD[i])
						curD[i] = d;
				}
			}

			// Amélioration : chaque couleur choisie est remplacée par la meilleure alternative, tant que l'erreur diminue
			if (nMem > nLocked) {
				double[] d1 = new double[n], d2 = new double[n];
				int[] b1 = new int[n];
				for (int pass = 0; pass < maxPass; pass++) {
					bool improved = false;
					for (int slot = nLocked; slot < nMem; slot++) {
						double cur = 0;
						for (int i = 0; i < n; i++) {
							double a = 1e30, b = 1e30;
							int ba = 0;
							for (int m = 0; m < nMem; m++) {
								double dl = pl[i] - ml[m], da = pa[i] - ma[m], db = pb[i] - mb[m];
								double d = dl * dl + da * da + db * db;
								if (d < a) {
									b = a;
									a = d;
									ba = m;
								}
								else if (d < b)
									b = d;
							}
							d1[i] = a;
							d2[i] = b;
							b1[i] = ba;
							cur += w[i] * a;
						}
						double bestCost = cur;
						int bestC = -1;
						for (int c = 0; c < nbCand; c++) {
							if (isMem[candIdx[c]])
								continue;
							double cost = 0;
							for (int i = 0; i < n; i++) {
								double dl = pl[i] - cl[c], da = pa[i] - ca[c], db = pb[i] - cb[c];
								double d = dl * dl + da * da + db * db;
								double e = b1[i] == slot ? d2[i] : d1[i];
								cost += w[i] * (d < e ? d : e);
								if (cost >= bestCost)
									break;
							}
							if (cost < bestCost) {
								bestCost = cost;
								bestC = c;
							}
						}
						if (bestC >= 0 && bestCost < cur * 0.9999) {
							isMem[mIdx[slot]] = false;
							mIdx[slot] = candIdx[bestC];
							ml[slot] = cl[bestC];
							ma[slot] = ca[bestC];
							mb[slot] = cb[bestC];
							isMem[mIdx[slot]] = true;
							improved = true;
						}
					}
					if (!improved)
						break;
				}
			}

			// Tri par fréquence décroissante
			int nb = nMem - nLocked;
			int[] order = new int[nb];
			double[] key = new double[nb];
			for (int j = 0; j < nb; j++) {
				order[j] = mIdx[nLocked + j];
				key[j] = -count[order[j]];
			}
			Array.Sort(key, order);
			return order;
		}

		//
		// Palettes "par ligne" (modes X et split) : nFixed couleurs communes à toutes les lignes (crayons 0 à nFixed-1),
		// puis nVar couleurs propres à chaque ligne. Choix par minimisation de l'erreur ; les lignes voisines
		// (trackModeX) sont prises en compte avec un poids décroissant pour que les couleurs restent stables.
		// Remplit colMode5 et retourne le nombre de couleurs (comme RechercheCMaxModeX / RechercheCMaxModeSplit).
		//
		static private int ChoosePensPerLine(int[,] colMode5, int[] lockState, int yMax, Param prm, int nFixed, int nVar) {
			int findMax = Cpc.cpcPlus ? 4096 : 27;
			int lines = Math.Min(272, yMax >> 1);

			// 1) couleurs communes à toutes les lignes
			double[] total = new double[findMax];
			for (int i = 0; i < findMax; i++)
				for (int y = 0; y < lines; y++)
					total[i] += coulTrouvee[i, y];

			int[] fixedCol = new int[nFixed];
			int[] lockedList = new int[nFixed];
			int nLocked = 0, nFreeFixed = 0;
			for (int c = 0; c < nFixed; c++) {
				if (lockState[c] > 0) {
					int col = Cpc.Palette[c];
					if (col < 0 || col >= findMax)
						col = 0;
					fixedCol[c] = col;
					lockedList[nLocked++] = col;
				}
				else
					nFreeFixed++;
			}
			int[] chosen = ChooseColors(total, findMax, lockedList, nLocked, nFreeFixed, 400, 4);
			int next = 0;
			for (int c = 0; c < nFixed; c++) {
				if (lockState[c] <= 0) {
					int col;
					if (next < chosen.Length) {
						col = chosen[next++];
						Cpc.Palette[c] = col;
					}
					else {
						col = Cpc.Palette[c];
						if (col < 0 || col >= findMax)
							col = 0;
					}
					fixedCol[c] = col;
				}
				for (int y = 0; y < 272; y++) {
					coulTrouvee[fixedCol[c], y] = 0;
					colMode5[y, c] = fixedCol[c];
				}
			}

			// 2) couleurs de chaque ligne
			int half = prm.trackModeX << 1;
			double[] h = new double[findMax];
			for (int y = 0; y < lines; y++) {
				Array.Clear(h, 0, findMax);
				for (int dy = -half; dy <= half; dy++) {
					int yy = y + dy;
					if (yy < 0 || yy >= lines)
						continue;
					double wgt = 1.0 - Math.Abs(dy) / (double)(half + 1);
					for (int i = 0; i < findMax; i++)
						if (coulTrouvee[i, yy] > 0)
							h[i] += wgt * coulTrouvee[i, yy];
				}
				int[] sel = ChooseColors(h, findMax, fixedCol, nFixed, nVar, 60, 2);
				for (int k = 0; k < nVar; k++)
					colMode5[y, nFixed + k] = k < sel.Length ? sel[k] : fixedCol[0];
			}

			int[] cFound = new int[4096];
			int nbCol = 0;
			for (int i = 0; i < 16; i++) {
				for (int y = 0; y < 272; y++) {
					if (cFound[colMode5[y, i]] == 0) {
						cFound[colMode5[y, i]] = 1;
						nbCol++;
					}
				}
			}
			return nbCol;
		}

		// Tous les modes sauf 11 (capture de sprites) : les convertisseurs lisent la source d'origine et font eux-mêmes
		// le tramage avec la palette finale
		static private bool OptUseConverterRead(Param prm) {
			if (!inConvert || prm.trameTc || backupBits == null)
				return false;
			int m = Cpc.modeVirtuel;
			return m >= 0 && m <= 10;
		}

		// Pixel à convertir : source d'origine + réglages + erreur diffusée (+ trame ordonnée). Sans tramage final : pixel de la passe 1.
		static private RvbColor ConvReadPixel(DirectBitmap source, Param prm, int x, int y, int Tx) {
			if (!redoActive)
				return source.GetPixelColor(x, y);
			RvbColor p = GetPixelAdj(source, x, y, Tx, prm);
			if (redoPct > 0 && !prm.diffErr)
				Dither.DoDitherFull(source, x, y, Tx, p, p, false);
			return p;
		}

		// Diffusion de l'erreur (pixel lu - couleur choisie) vers les pixels suivants
		static private void ConvDiffuse(DirectBitmap source, Param prm, int x, int y, int Tx, int Ty, RvbColor p, RvbColor choix) {
			if (redoActive && redoPct > 0 && prm.diffErr)
				Dither.DoDitherFull(source, x, y, Tx, Ty, p, choix, true);
		}

		// Couleur moyenne (en lumière linéaire) d'un bloc de Tx pixels sur incY lignes de la source, réglages appliqués
		static private RvbColor BlockAverageRaw(DirectBitmap source, Param prm, int x, int y, int Tx, int incY) {
			double r = 0, v = 0, b = 0;
			int n = 0;
			for (int j = 0; j < incY; j += 2) {
				for (int i = 0; i < Tx; i++) {
					RvbColor q = GetPixelAdjRaw(source, x + i, y + j, 1, prm);
					r += tblLinear[q.r];
					v += tblLinear[q.v];
					b += tblLinear[q.b];
					n++;
				}
			}
			return new RvbColor(LinearToByte(r / n), LinearToByte(v / n), LinearToByte(b / n));
		}

		static private RvbColor ConvReadBlock(DirectBitmap source, Param prm, int x, int y, int Tx, int incY) {
			RvbColor p = BlockAverageRaw(source, prm, x, y, Tx, incY);
			Dither.ApplyError(source, x, y, p);
			if (redoPct > 0 && !prm.diffErr)
				Dither.DoDitherFull(source, x, y, Tx, incY, p, p, false);
			return p;
		}

		// OKLab des crayons d'une ligne (palettes par ligne)
		static private void PenLab(RvbColor[,] tabCol, int line, int nPens, double[] pl, double[] pa, double[] pb) {
			for (int i = 0; i < nPens; i++) {
				RvbColor c = tabCol[i, line];
				if (c != null)
					LabFromRvb(c.r, c.v, c.b, out pl[i], out pa[i], out pb[i]);
			}
		}

		private static double PenDistance(double pL, double pA, double pB, double cL, double cA, double cB) {
			double dl = pL - cL, da = pA - cA, db = pB - cB;
			return dl * dl + da * da + db * db;
		}

		// Crayon le plus proche parmi les nPens premiers crayons de la ligne (null = crayon inutilisé)
		static private int NearestPenLine(Param prm, RvbColor pix, RvbColor[,] tabCol, int line, int nPens, double[] pl, double[] pa, double[] pb) {
			return NearestPenLine(prm, pix, tabCol, line, nPens, pl, pa, pb, false);
		}

		static private int NearestPenLine(Param prm, RvbColor pix, RvbColor[,] tabCol, int line, int nPens, double[] pl, double[] pa, double[] pb, bool checkDisable) {
			LabFromRvb(pix.r, pix.v, pix.b, out double pL, out double pA, out double pB);
			int choix = 0;
			double oldDist = double.MaxValue;
			for (int i = 0; i < nPens; i++) {
				RvbColor c = tabCol[i, line];
				if (c == null || (checkDisable && prm.disableState[i] != 0))
					continue;

				double d = PenDistance(pL, pA, pB, pl[i], pa[i], pb[i]);
				if (d < oldDist) {
					choix = i;
					oldDist = d;
					if (d == 0)
						break;
				}
			}
			return choix;
		}

		static private int GetValColor(int c, Param prm) {
			if (prm.cpcPlus) {
				int v = c >> 8;
				int b = (c >> 4) & 0x0F;
				int r = c & 0x0F;
				return r * r * prm.coefR + v * v * prm.coefV + b * b * prm.coefB;
			}
			else
				return c;
		}

		//
		// Recherche les x meilleurs couleurs parmis les n possibles (remplit le tableau Cpc.Palette)
		//
		static void RechercheCMax(int maxPen, int[] lockState, Param prm) {
			for (int i = 0; i < 16; i++)
				if (prm.lockState[i] == 0 && lockState[i] == 0)
					Cpc.Palette[i] = 0xFFFF;

			try {
				int cUtil, x, FindMax = Cpc.cpcPlus ? 4096 : 27, valMax = 0;
				for (x = 0; x < maxPen; x++)
					if (lockState[x] > 0)
						for (int y = 0; y < 272; y++)
							if (Cpc.Palette[x] < 4096)
								coulTrouvee[Cpc.Palette[x], y] = 0;

				if (!prm.newReduc) {
					if (inConvert && Cpc.modeVirtuel == 7 && blkValid)
						RechercheAscUt(maxPen, lockState, prm, FindMax);
					else
						RechercheOptimisee(maxPen, lockState, prm, FindMax);
				}
				else {
					// Recherche la couleur la plus utilisée
					for (cUtil = 0; cUtil < maxPen; cUtil++) {
						valMax = 0;
						if (lockState[cUtil] == 0 && prm.disableState[cUtil] == 0) {
							for (int i = 0; i < FindMax; i++) {
								int nbc = 0;
								for (int y = 0; y < 272; y++)
									nbc += coulTrouvee[i, y];

								if (valMax < nbc) {
									valMax = nbc;
									Cpc.Palette[cUtil] = i;
								}
							}
							for (int y = 0; y < 272; y++)
								if (Cpc.Palette[cUtil] < 0xFFFF)
									coulTrouvee[Cpc.Palette[cUtil], y] = 0;

							if (prm.newReduc)
								break; // Première couleur trouvée => sortie
						}
					}
					if (prm.newReduc) {   // Méthode altenative recherche de couleurs : les plus différentes parmis les plus utilisées
						RvbColor colFirst = Cpc.GetColor(Cpc.Palette[cUtil]);
						bool takeDist = false;
						for (x = 0; x < maxPen; x++) {
							if (takeDist) {
								valMax = 0;
								if (lockState[x] == 0 /*&& prm.disableState[x] == 0*/) {
									for (int i = 0; i < FindMax; i++) {
										int nbc = 0;
										for (int y = 0; y < 272; y++)
											nbc += coulTrouvee[i, y];

										if (valMax < nbc) {
											valMax = nbc;
											Cpc.Palette[x] = i;
										}
									}
								}
							}
							else {
								if (lockState[x] == 0 /*&& prm.disableState[x] == 0*/ && x != cUtil) {
									int dist, oldDist = 0;
									for (int rech = 4; rech-- > 0;) {
										for (int i = 0; i < FindMax; i++) {
											int nbc = 0;
											for (int y = 0; y < 272; y++)
												nbc += coulTrouvee[i, y];

											if (nbc > valMax >> rech) {
												RvbColor c = Cpc.GetColor(i);
												dist = (c.r - colFirst.r) * (c.r - colFirst.r) * prm.coefR + (c.v - colFirst.v) * (c.v - colFirst.v) * prm.coefV + (c.b - colFirst.b) * (c.b - colFirst.b) * prm.coefB;
												if (dist > oldDist) {
													oldDist = dist;
													Cpc.Palette[x] = i;
												}
											}
										}
									}
									if (oldDist == 0)
										x--;
								}
							}
							for (int y = 0; y < 272; y++)
								if (Cpc.Palette[x] < 0xFFFF)
									coulTrouvee[Cpc.Palette[x], y] = 0;

							takeDist = !takeDist;
							cUtil = x;
						}
					}
				}
				if ((prm.newSortPal & 1) == 1) {
					bool sensPal = (prm.newSortPal & 2) == 0;
					for (x = 0; x < maxPen - 1; x++) {
						if (lockState[x] == 0 && prm.disableState[x] == 0 && Cpc.Palette[x] != 0xFFFF) {
							for (int c = x + 1; c < maxPen; c++) {
								if (lockState[c] == 0 && prm.disableState[c] == 0 && Cpc.Palette[c] != 0xFFFF) {
									if ((GetValColor(Cpc.Palette[x], prm) > GetValColor(Cpc.Palette[c], prm) && sensPal)
										|| (GetValColor(Cpc.Palette[x], prm) < GetValColor(Cpc.Palette[c], prm) && !sensPal)) {
										int tmp = Cpc.Palette[x];
										Cpc.Palette[x] = Cpc.Palette[c];
										Cpc.Palette[c] = tmp;
									}
								}
							}
						}
					}
				}
			}
			catch (Exception ex) {
				System.Windows.Forms.MessageBox.Show(ex.StackTrace, ex.Message);
			}
		}

		//
		// Passe 2 : réduit l'image au nombre de couleurs max du mode CPC choisi.
		//
		static private void Passe2(DirectBitmap source, ImageCpc dest, Param prm, ref int colSplit) {
			RvbColor[,] tabCol = new RvbColor[16, 272];
			int[] MemoLockState = new int[16];
			int i;
			int maxPen = Cpc.MaxPen(Cpc.yEgx ^ 2);
			for (i = 0; i < 16; i++)
				MemoLockState[i] = prm.lockState[i];

			// Modes EGX ?
			if (Cpc.modeVirtuel == 3 || Cpc.modeVirtuel == 4) {
				int newMax = Cpc.MaxPen(Cpc.yEgx);
				RechercheCMax(newMax, MemoLockState, prm);
				for (i = 0; i < newMax; i++)
					MemoLockState[i] = 1;
			}
			// Mode X ou Mode Split ?
			if (Cpc.modeVirtuel == 5 || Cpc.modeVirtuel == 6) {
				if (Cpc.modeVirtuel == 5)
					colSplit = RechercheCMaxModeX(Cpc.colMode5, MemoLockState, Cpc.TailleY, prm);
				else {
					colSplit = RechercheCMaxModeSplit(Cpc.colMode5, MemoLockState, Cpc.TailleY, prm);
					maxPen = 9;
				}
				// réduit l'image à maxPen couleurs.
				for (int y = 0; y < Cpc.TailleY >> 1; y++)
					for (i = 0; i < maxPen; i++)
						tabCol[i, y] = Cpc.GetColor(Cpc.colMode5[y, i]);
			}
			else {  // Mode standard CPC ou utilisation de gros pixels trames
				RechercheCMax(maxPen, MemoLockState, prm);
				// réduit l'image à MaxPen couleurs.
				for (int y = 0; y < Cpc.TailleY; y += 2) {
					maxPen = Cpc.MaxPen(y);
					for (i = 0; i < maxPen; i++)
						tabCol[i, y >> 1] = dest.BitmapCpc.GetColorPal(i);
				}
			}
			redoActive = false;
			if (OptUseConverterRead(prm)) {
				if (backupBits != null && backupBits.Length == source.TabBits.Length) {
					Array.Copy(backupBits, source.TabBits, backupBits.Length);
					redoPct = Dither.SetMatDither(prm);
					redoActive = true;
				}
			}
			switch (Cpc.modeVirtuel) {
				case 5:
					ConvertModeX(source, prm, dest, tabCol);
					break;

				case 6:
					ConvertSplit(source, prm, dest, tabCol);
					break;

				case 7:
					ConvertAscUt(source, prm, dest, tabCol);
					break;

				case 8:
				case 9:
				case 10:
					ConvertAscii(source, prm, dest, maxPen, tabCol);
					break;

				default:
					ConvertStd(source, prm, dest, maxPen, tabCol);
					break;
			}
			redoActive = false;
		}

		static public int Convert(DirectBitmap source, ImageCpc dest, Param prm, bool noInfo = false) {
			inConvert = true;
			try {
				return ConvertMain(source, dest, prm, noInfo);
			}
			finally {
				inConvert = false;
				redoActive = false;
				backupBits = null;
			}
		}

		static private int ConvertMain(DirectBitmap source, ImageCpc dest, Param prm, bool noInfo) {
			Array.Clear(coulTrouvee, 0, coulTrouvee.GetLength(0) * coulTrouvee.GetLength(1));
			double c = prm.pctContrast / 100.0;
			for (int i = 0; i < 256; i++)
				tblContrast[i] = MinMaxByte(((((i / 255.0) - 0.5) * c) + 0.5) * 255.0);

			if (prm.filtre)
				Palettiser(source, prm);

			InitPerceptual();
			backupBits = (uint[])source.TabBits.Clone();
			ConvertPasse1(source, prm);

			// Calculer nbre de couleurs dans l'image
			int nbCol = 0;
			for (int i = 0; i < coulTrouvee.GetLength(0); i++) {
				bool memoCol = false;
				for (int y = 0; y < 272; y++)
					if (!memoCol && coulTrouvee[i, y] > 0) {
						nbCol++;
						memoCol = true;
					}
			}

			int colSplit = 0;
			Passe2(source, dest, prm, ref colSplit);
			if (!noInfo) {
				dest.main.SetInfo("Conversion terminée, nombre de couleurs dans l'image:" + nbCol);
				if (colSplit > 0)
					dest.main.SetInfo("Couleurs générées avec split : " + colSplit);
			}

			dest.BitmapCpc.isCalc = true;
			return nbCol;
		}

		static public void Palettiser(DirectBitmap img, Param prm) {
			int nbNiv = prm.kMeansColor < 2 ? 2 : prm.kMeansColor;
			int incY = prm.trameTc ? 4 : 2;

			// Pixels analysés, triés par luminosité
			List<int> ech = new List<int>();
			List<double> lum = new List<double>();
			for (int y = 0; y < Cpc.TailleY; y += incY) {
				int Tx = Cpc.CalcTx(y);
				for (int x = 0; x < Cpc.TailleX; x += Tx) {
					RvbColor c = img.GetPixelColor(x, y);
					ech.Add((c.r << 16) | (c.v << 8) | c.b);
					lum.Add(0.299 * c.r + 0.587 * c.v + 0.114 * c.b);
				}
			}
			int[] sample = ech.ToArray();
			Array.Sort(lum.ToArray(), sample);

			// Niveaux initiaux : vraies couleurs de l'image prises à intervalles réguliers de luminosité
			// (et non des gris, qui laissaient des niveaux inutilisés)
			niveaux = new Niveau[nbNiv];
			for (int n = 0; n < nbNiv; n++) {
				int s = sample.Length > 0 ? sample[Math.Min(sample.Length - 1, (int)((n + 0.5) * sample.Length / nbNiv))] : 0;
				niveaux[n] = new Niveau((byte)(s >> 16), (byte)(s >> 8), (byte)s);
			}

			for (int p = 0; p < prm.kMeansPass; p++) {
				for (int n = 0; n < nbNiv; n++)
					niveaux[n].Reset();

				for (int i = 0; i < sample.Length; i++)
					NiveauAdequat(prm, new RvbColor(sample[i]));

				for (int n = 0; n < nbNiv; n++)
					niveaux[n].SetNiveauMoyen();
			}

			for (int y = 0; y < Cpc.TailleY; y += incY)
				for (int x = 0; x < Cpc.TailleX; x++)
					img.SetPixel(x, y, NiveauAdequat(prm, img.GetPixelColor(x, y)).couleurNiveau.GetColorArgb);
		}

		static public Niveau NiveauAdequat(Param prm, RvbColor coul) {
			int minDist = 0x7FFFFFFF;
			int ret = 0;
			byte r = coul.r;
			byte v = coul.v;
			byte b = coul.b;
			for (int n = 0; n < niveaux.Length; n++) {
				RvbColor cNiv = niveaux[n].couleurNiveau;
				int dist;
				switch ((Distance)prm.kMeansDist) {
					case Distance.DISTANCE_EUCLIDE:
						dist = (r - cNiv.r) * (r - cNiv.r) * prm.coefR + (v - cNiv.v) * (v - cNiv.v) * prm.coefV + (b - cNiv.b) * (b - cNiv.b) * prm.coefB;
						break;
					case Distance.DISTANCE_SUP:
						dist = Math.Max(Math.Abs(r - cNiv.r), Math.Max(Math.Abs(v - cNiv.v), Math.Abs(b - cNiv.b)));
						break;
					default:
						dist = Math.Abs(r - cNiv.r) * prm.coefR + Math.Abs(v - cNiv.v) * prm.coefV + Math.Abs(b - cNiv.b) * prm.coefB;
						break;
				}
				if (dist < minDist) {
					ret = n;
					minDist = dist;
				}
			}
			niveaux[ret].nbR += r;
			niveaux[ret].nbV += v;
			niveaux[ret].nbB += b;
			niveaux[ret].nbPixels++;
			return niveaux[ret];
		}
	}

	public class Niveau {
		public RvbColor couleurNiveau;
		public int nbR, nbV, nbB, nbPixels;

		public Niveau(byte r, byte v, byte b) {
			couleurNiveau = new RvbColor(r, v, b);
			Reset();
		}

		public void Reset() {
			nbR = nbV = nbB = nbPixels = 0;
		}

		public void SetNiveauMoyen() {
			if (nbPixels > 0) {     // un niveau sans pixel conserve sa couleur précédente
				couleurNiveau.r = (byte)(nbR / nbPixels);
				couleurNiveau.v = (byte)(nbV / nbPixels);
				couleurNiveau.b = (byte)(nbB / nbPixels);
			}
		}
	}
}
