using System;
using System.Collections.Generic;

namespace ConvImgCpc {
	public static partial class Conversion {
		// Conversion avec trames précalculées en mode 1
		//
		// Chaque bloc de 4x4 pixels CPC est rendu par la trame dont la couleur moyenne est la plus proche de la couleur
		// moyenne du bloc. Ici : moyennes calculées en lumière linéaire (mélange réel des couleurs), distance perceptuelle,
		// et erreur diffusée d'un bloc à l'autre (tramage final, avec la palette définitive).
		static private void ConvertAscUt(DirectBitmap source, Param prm, ImageCpc dest, RvbColor[,] tabCol) {
			if (!redoActive) {
				for (int y = 0; y <= Cpc.TailleY - 8; y += 8) {
					for (int x = 0; x < Cpc.TailleX; x += 8) {
						int choix = 0, oldDist = 0x7FFFFFFF;
						for (int i = 0; i < 16; i++) {
							int dist, r1 = 0, v1 = 0, b1 = 0, r2 = 0, v2 = 0, b2 = 0;
							for (int ym = 0; ym < 4; ym++) {
								for (int xm = 0; xm < 4; xm++) {
									RvbColor pix = source.GetPixelColor(x + (xm << 1), y + (ym << 1));
									RvbColor c = tabCol[Cpc.trameM1[i, xm, ym], ym + (y >> 1)];
									r1 += pix.r;
									v1 += pix.v;
									b1 += pix.b;
									r2 += c.r;
									v2 += c.v;
									b2 += c.b;
								}
							}
							dist = Math.Abs(r1 - r2) * prm.coefR + Math.Abs(v1 - v2) * prm.coefV + Math.Abs(b1 - b2) * prm.coefB;
							if (dist < oldDist) {
								choix = i;
								oldDist = dist;
								if (dist == 0)
									i = 16;
							}
						}
						for (int ym = 0; ym < 4; ym++)
							for (int xm = 0; xm < 4; xm++)
								dest.SetPixelCpc(x + (xm << 1), y + (ym << 1), Cpc.trameM1[choix, xm, ym], 2);
					}
				}
				return;
			}
			double[] pl = new double[16], pa = new double[16], pb = new double[16];
			RvbColor[] patColor = new RvbColor[16];
			for (int y = 0; y <= Cpc.TailleY - 8; y += 8) {
				// Couleur moyenne de chaque trame pour ce groupe de 4 lignes
				for (int i = 0; i < 16; i++) {
					double r = 0, v = 0, b = 0;
					for (int ym = 0; ym < 4; ym++) {
						for (int xm = 0; xm < 4; xm++) {
							RvbColor c = tabCol[Cpc.trameM1[i, xm, ym], ym + (y >> 1)] ?? tabCol[0, ym + (y >> 1)];
							r += tblLinear[c.r];
							v += tblLinear[c.v];
							b += tblLinear[c.b];
						}
					}
					patColor[i] = new RvbColor(LinearToByte(r / 16), LinearToByte(v / 16), LinearToByte(b / 16));
					LabFromRvb(patColor[i].r, patColor[i].v, patColor[i].b, out pl[i], out pa[i], out pb[i]);
				}
				for (int x = 0; x < Cpc.TailleX; x += 8) {
					RvbColor pix = ConvReadBlock(source, prm, x, y, 8, 8);
					LabFromRvb(pix.r, pix.v, pix.b, out double pL, out double pA, out double pB);
					int choix = 0;
					double oldDist = double.MaxValue;
					for (int i = 0; i < 16; i++) {
						double dist = PenDistance(pL, pA, pB, pl[i], pa[i], pb[i]);
						if (dist < oldDist) {
							choix = i;
							oldDist = dist;
							if (dist == 0)
								break;
						}
					}
					ConvDiffuse(source, prm, x, y, 8, 8, pix, patColor[choix]);
					for (int ym = 0; ym < 4; ym++)
						for (int xm = 0; xm < 4; xm++)
							dest.SetPixelCpc(x + (xm << 1), y + (ym << 1), Cpc.trameM1[choix, xm, ym], 2);
				}
			}
		}

		// Calcul automatique matrice 4x4 en mode 1
		static public void CnvTrame(DirectBitmap source, Param prm, ImageCpc dest, List<TrameM1> lstTrame) {
			prm.modeVirtuel = 1;
			ConvertPasse1(source, prm);
			RechercheCMax(4, prm.lockState, prm);
			RvbColor[] pal4 = new RvbColor[4];
			double[] pl4 = new double[4], pa4 = new double[4], pb4 = new double[4];
			for (int i = 0; i < 4; i++) {
				pal4[i] = dest.BitmapCpc.GetColorPal(i);
				LabFromRvb(pal4[i].r, pal4[i].v, pal4[i].b, out pl4[i], out pa4[i], out pb4[i]);
			}
			for (int y = 0; y < Cpc.TailleY; y += 8) {
				for (int x = 0; x < Cpc.TailleX; x += 8) {
					TrameM1 locTrame = new TrameM1();
					for (int maty = 0; maty < 4; maty++) {
						for (int matx = 0; matx < 4; matx++) {
							RvbColor pix = source.GetPixelColor(x + (matx << 1), y + (maty << 1));
							LabFromRvb(pix.r, pix.v, pix.b, out double pL, out double pA, out double pB);
							int choix = 0;
							int n = 4;  // 4 couleurs
							double oldDist = double.MaxValue;
							for (int i = 0; i < n; i++) {
								double d = PenDistance(pL, pA, pB, pl4[i], pa4[i], pb4[i]);
								if (d < oldDist) {
									choix = i;
									oldDist = d;
									if (d == 0)
										break;
								}
							}
							locTrame.SetPix(matx, maty, (byte)choix);
						}
					}
					bool found = false;
					foreach (TrameM1 t in lstTrame) {
						if (t.IsSame(locTrame, 4)) {
							found = true;
							break;
						}
					}
					if (!found)
						lstTrame.Add(locTrame);
				}
			}
		}

		// Coût d'une palette de 4 crayons : somme, sur tous les blocs de l'image, de l'écart entre la couleur moyenne
		// du bloc et la couleur moyenne de la meilleure des 16 trames.
		static private double AscUtCost(int[] pal, int n, double[,] frac, double[] lr, double[] lg, double[] lb,
										double[] tl, double[] ta, double[] tb, int nb, double[] ml, double[] ma, double[] mb) {
			for (int i = 0; i < 16; i++) {
				double r = 0, g = 0, b = 0;
				for (int p = 0; p < n; p++) {
					double f = frac[i, p];
					if (f != 0) {
						r += f * lr[pal[p]];
						g += f * lg[pal[p]];
						b += f * lb[pal[p]];
					}
				}
				double l = Math.Pow(0.4122214708 * r + 0.5363325363 * g + 0.0514459929 * b, 1.0 / 3.0);
				double m = Math.Pow(0.2119034982 * r + 0.6806995451 * g + 0.1073969566 * b, 1.0 / 3.0);
				double s = Math.Pow(0.0883024619 * r + 0.2817188376 * g + 0.6299787005 * b, 1.0 / 3.0);
				ml[i] = 0.2104542553 * l + 0.7936177850 * m - 0.0040720468 * s;
				ma[i] = 1.9779984951 * l - 2.4285922050 * m + 0.4505937099 * s;
				mb[i] = 0.0259040371 * l + 0.7827717662 * m - 0.8086757660 * s;
			}
			double total = 0;
			for (int k = 0; k < nb; k++) {
				double best = double.MaxValue;
				for (int i = 0; i < 16; i++) {
					double dl = tl[k] - ml[i], da = ta[k] - ma[i], db = tb[k] - mb[i];
					double d = dl * dl + da * da + db * db;
					if (d < best)
						best = d;
				}
				total += best;
			}
			return total;
		}

		//
		// Palette pour ASC-UT. Une trame mélange des crayons : il faut donc des couleurs dont les mélanges (donnés par
		// les 16 trames) couvrent au mieux les couleurs des blocs, et non des couleurs proches de chaque pixel.
		// Départ : palette standard, puis remplacement successif de chaque crayon par la couleur qui réduit le plus l'erreur.
		//
		static private void RechercheAscUt(int maxPen, int[] lockState, Param prm, int findMax) {
			int nb = blkL.Count;
			int n = Math.Min(4, maxPen);

			// Histogramme (avant que la méthode standard ne le remette à zéro), pour choisir les couleurs candidates
			double[] count = new double[findMax];
			for (int i = 0; i < findMax; i++)
				for (int y = 0; y < 272; y++)
					count[i] += coulTrouvee[i, y];

			RechercheOptimisee(maxPen, lockState, prm, findMax);
			if (nb == 0 || n < 2)
				return;

			// Candidates : les 27 couleurs CPC ; en CPC+ les plus utilisées et une grille de 4 niveaux par composante
			int[] cand;
			if (findMax <= 27) {
				cand = new int[findMax];
				for (int i = 0; i < findMax; i++)
					cand[i] = i;
			}
			else {
				bool[] seen = new bool[findMax];
				List<int> lst = new List<int>();
				List<int> used = new List<int>();
				List<double> key = new List<double>();
				for (int i = 0; i < findMax; i++) {
					if (count[i] > 0) {
						used.Add(i);
						key.Add(-count[i]);
					}
				}
				int[] usedArr = used.ToArray();
				Array.Sort(key.ToArray(), usedArr);
				for (int i = 0; i < usedArr.Length && i < 60; i++) {
					lst.Add(usedArr[i]);
					seen[usedArr[i]] = true;
				}
				int[] lv = { 0, 5, 10, 15 };
				for (int g = 0; g < 4; g++)
					for (int b = 0; b < 4; b++)
						for (int r = 0; r < 4; r++) {
							int col = (lv[g] << 8) | (lv[b] << 4) | lv[r];     // format &GBR de Cpc.GetColor
							if (!seen[col]) {
								seen[col] = true;
								lst.Add(col);
							}
						}
				cand = lst.ToArray();
			}

			// Couleurs matérielles en lumière linéaire
			double[] lr = new double[findMax], lg = new double[findMax], lb = new double[findMax];
			for (int i = 0; i < findMax; i++) {
				RvbColor c = Cpc.GetColor(i);
				lr[i] = tblLinear[c.r];
				lg[i] = tblLinear[c.v];
				lb[i] = tblLinear[c.b];
			}

			// Proportion de chaque crayon dans chacune des 16 trames
			double[,] frac = new double[16, 4];
			for (int i = 0; i < 16; i++)
				for (int ym = 0; ym < 4; ym++)
					for (int xm = 0; xm < 4; xm++)
						frac[i, Cpc.trameM1[i, xm, ym] & 3] += 1.0 / 16.0;

			double[] tl = blkL.ToArray(), ta = blkA.ToArray(), tb = blkB.ToArray();
			double[] ml = new double[16], ma = new double[16], mb = new double[16];
			int[] pal = new int[4];
			for (int p = 0; p < 4; p++) {
				pal[p] = Cpc.Palette[p];
				if (pal[p] < 0 || pal[p] >= findMax)
					pal[p] = 0;
			}

			double cur = AscUtCost(pal, n, frac, lr, lg, lb, tl, ta, tb, nb, ml, ma, mb);
			int maxPass = findMax <= 27 ? 3 : 2;
			for (int pass = 0; pass < maxPass; pass++) {
				bool improved = false;
				for (int slot = 0; slot < n; slot++) {
					if (lockState[slot] > 0 || prm.disableState[slot] != 0)
						continue;
					int keep = pal[slot];
					int bestC = keep;
					double bestCost = cur;
					for (int ci = 0; ci < cand.Length; ci++) {
						int cc = cand[ci];
						bool dup = false;
						for (int q = 0; q < n; q++)
							if (pal[q] == cc)
								dup = true;
						if (dup)
							continue;
						pal[slot] = cc;
						double cost = AscUtCost(pal, n, frac, lr, lg, lb, tl, ta, tb, nb, ml, ma, mb);
						if (cost < bestCost) {
							bestCost = cost;
							bestC = cc;
						}
					}
					pal[slot] = bestC;
					if (bestC != keep) {
						cur = bestCost;
						improved = true;
					}
				}
				if (!improved)
					break;
			}

			for (int s = 0; s < n; s++)
				if (lockState[s] <= 0 && prm.disableState[s] == 0)
					Cpc.Palette[s] = pal[s];
		}
	}
}
