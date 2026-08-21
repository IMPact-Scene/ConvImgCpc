using System;

namespace ConvImgCpc {
	public static partial class Conversion {
		public enum Distance { DISTANCE_SUP = 0, DISTANCE_EUCLIDE = 1, DISTANCE_MANHATTAN = 2 };

		static private readonly int[,] coulTrouvee = new int[4096, 272];
		static private readonly byte[] tblContrast = new byte[256];
		static public Niveau[] niveaux;

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
				int oldDist = 0x7FFFFFFF;
				for (int i = 0; i < 27; i++) {
					RvbColor s = Cpc.RgbCPC[i];
					int dist = (s.r - p.r) * (s.r - p.r) * prm.coefR + (s.v - p.v) * (s.v - p.v) * prm.coefV + (s.b - p.b) * (s.b - p.b) * prm.coefB;
					if (dist < oldDist) {
						oldDist = dist;
						indexChoix = i;
						if (dist == 0)
							break;
					}
				}
			}
			return indexChoix;
		}

		static private RvbColor GetPixel(DirectBitmap source, int xPix, int yPix, int Tx, Param prm, int pct) {
			RvbColor p = new RvbColor(0);
			if (prm.lissage) {
				float r = 0, v = 0, b = 0;
				for (int i = 0; i < Tx; i++) {
					p = source.GetPixelColor(xPix + i, yPix);
					r += p.r;
					b += p.b;
					v += p.v;
				}
				p.r = (byte)(r / Tx);
				p.v = (byte)(v / Tx);
				p.b = (byte)(b / Tx);
			}
			else
				p = source.GetPixelColor(xPix, yPix);

			switch (prm.bitsRVB) {
				//12 bits (4 bits par composantes)
				case 12:
					p.r = (byte)((p.r >> 4) * 17);
					p.v = (byte)((p.v >> 4) * 17);
					p.b = (byte)((p.b >> 4) * 17);
					break;

				// 9 bits (3 bits par composantes)
				case 9:
					p.r = (byte)((p.r >> 5) * 36);
					p.v = (byte)((p.v >> 5) * 36);
					p.b = (byte)((p.b >> 5) * 36);
					break;

				// 6 bits (2 bits par composantes)
				case 6:
					p.r = (byte)((p.r >> 6) * 85);
					p.v = (byte)((p.v >> 6) * 85);
					p.b = (byte)((p.b >> 6) * 85);
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

			// Appliquer la matrice de tramage
			if (pct > 0) {
				RvbColor choix = prm.cpcPlus ? new RvbColor((byte)((p.r >> 4) * 17), (byte)((p.v >> 4) * 17), (byte)((p.b >> 4) * 17)) : Cpc.RgbCPC[GetNumColorPixelCpc(prm, p)];
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
			RvbColor p, choix, n1, n2;
			int indexChoix;
			int incY = prm.trameTc ? 4 : 2;
			for (int yPix = 0; yPix < Cpc.TailleY; yPix += incY) {
				int Tx = Cpc.CalcTx(yPix);
				for (int xPix = 0; xPix < Cpc.TailleX; xPix += Tx) {
					for (int yy = 0; yy < incY; yy += 2) {
						p = GetPixel(source, xPix, yy + yPix, Tx, prm, pct);
						// Recherche le point dans la couleur cpc la plus proche
						if (prm.cpcPlus) {
							choix = new RvbColor((byte)((p.r >> 4) * 17), (byte)((p.v >> 4) * 17), (byte)((p.b >> 4) * 17));
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

		static private int GetValColor(int c, Param prm) {
			if (prm.cpcPlus) {
				int v = c >> 8;
				int r = (c >> 4) & 0x0F;
				int b = c & 0x0F;
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
		}

		static public int Convert(DirectBitmap source, ImageCpc dest, Param prm, bool noInfo = false) {
			Array.Clear(coulTrouvee, 0, coulTrouvee.GetLength(0) * coulTrouvee.GetLength(1));
			double c = prm.pctContrast / 100.0;
			for (int i = 0; i < 256; i++)
				tblContrast[i] = MinMaxByte(((((i / 255.0) - 0.5) * c) + 0.5) * 255.0);

			if (prm.filtre)
				Palettiser(source, prm);

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
			niveaux = new Niveau[prm.kMeansColor];
			for (int n = 0; n < prm.kMeansColor; n++) {
				byte gris = (byte)(255 * n / (prm.kMeansColor - 1));
				niveaux[n] = new Niveau(gris, gris, gris);
			}
			int incY = prm.trameTc ? 4 : 2;
			for (int p = 0; p < prm.kMeansPass; p++) {
				for (int n = 0; n < prm.kMeansColor; n++)
					niveaux[n].Reset();

				for (int y = 0; y < Cpc.TailleY; y += incY) {
					int Tx = Cpc.CalcTx(y);
					for (int x = 0; x < Cpc.TailleX; x += Tx) {
						RvbColor coul = img.GetPixelColor(x, y);
						NiveauAdequat(prm, coul);
					}
				}
				for (int n = 0; n < prm.kMeansColor; n++)
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
			couleurNiveau.r = nbPixels > 0 ? (byte)(nbR / nbPixels) : (byte)0;
			couleurNiveau.v = nbPixels > 0 ? (byte)(nbV / nbPixels) : (byte)0;
			couleurNiveau.b = nbPixels > 0 ? (byte)(nbB / nbPixels) : (byte)0;
		}
	}
}
