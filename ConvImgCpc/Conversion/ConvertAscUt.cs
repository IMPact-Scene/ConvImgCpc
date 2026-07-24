using System;
using System.Collections.Generic;

namespace ConvImgCpc {
	public static partial class Conversion {
		// Conversion avec trames précalculées en mode 1
		static private void ConvertAscUt(DirectBitmap source, Param prm, ImageCpc dest, RvbColor[,] tabCol) {
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
		}

		// Calcul automatique matrice 4x4 en mode 1
		static public void CnvTrame(DirectBitmap source, Param prm, ImageCpc dest, List<TrameM1> lstTrame) {
			prm.modeVirtuel = 1;
			ConvertPasse1(source, prm);
			RechercheCMax(4, prm.lockState, prm);
			for (int y = 0; y < Cpc.TailleY; y += 8) {
				for (int x = 0; x < Cpc.TailleX; x += 8) {
					TrameM1 locTrame = new TrameM1();
					for (int maty = 0; maty < 4; maty++) {
						for (int matx = 0; matx < 4; matx++) {
							RvbColor pix = source.GetPixelColor(x + (matx << 1), y + (maty << 1));
							int oldDist = 0x7FFFFFFF;
							int choix = 0;
							for (int i = 0; i < 4; i++) {
								RvbColor c = dest.BitmapCpc.GetColorPal(i);
								int dist = (c.r - pix.r) * (c.r - pix.r) * prm.coefR + (c.v - pix.v) * (c.v - pix.v) * prm.coefV + (c.b - pix.b) * (c.b - pix.b) * prm.coefB;
								if (dist < oldDist) {
									choix = i;
									oldDist = dist;
									if (dist == 0)
										i = 4;
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
	}
}
