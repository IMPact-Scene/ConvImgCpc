namespace ConvImgCpc {
	public static partial class Conversion {
		//
		//
		// Recherche les couleurs pour le mode "split"
		//
		static int RechercheCMaxModeSplit(int[,] colMode5, int[] lockState, int yMax, Param prm) {
			int c, FindMax = Cpc.cpcPlus ? 4096 : 27;

			// Les trois premières couleurs sont "fixes"
			for (c = 0; c < 3; c++) {
				if (lockState[c] > 0) {
					for (int y = 0; y < 272; y++) {
						coulTrouvee[Cpc.Palette[c], y] = 0;
						colMode5[y, c] = Cpc.Palette[c];
					}
				}
				else {
					int valMax = 0;
					for (int i = 0; i < FindMax; i++) {
						int valFound = 0;
						for (int y = 0; y < yMax >> 1; y++)
							valFound += coulTrouvee[i, y];

						if (valMax < valFound) {
							valMax = valFound;
							Cpc.Palette[c] = i;
						}
					}
					for (int y = 0; y < 272; y++) {
						int col = Cpc.Palette[c];
						if (col > 4095)
							col = 0;

						coulTrouvee[col, y] = 0;
						colMode5[y, c] = col;
					}
				}
			}

			// Recherche les couleurs par ligne
			for (c = 3; c < 9; c++) {
				for (int y = 0; y < yMax >> 1; y++) {
					int valMax = 0;
					for (int i = 0; i < FindMax; i++) {
						for (int deltay = -(prm.trackModeX << 1); deltay <= (prm.trackModeX << 1); deltay++) {
							if (y + deltay >= 0 && y + deltay < (yMax >> 1) && valMax < coulTrouvee[i, y + deltay]) {
								valMax = coulTrouvee[i, y + deltay];
								colMode5[y, c] = i;
							}
						}
					}
					coulTrouvee[colMode5[y, c], y] = 0;
				}
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

		// Conversion avec des "splits-rasters"
		static private void ConvertSplit(DirectBitmap source, Param prm, ImageCpc dest, RvbColor[,] tabCol) {
			for (int y = 0; y < Cpc.TailleY; y += 2) {
				int tailleSplit = 0, colSplit = -1;
				for (int x = 0; x < Cpc.TailleX; x += 2) {
					int oldDist = 0x7FFFFFFF;
					RvbColor pix = source.GetPixelColor(x, y);
					int choix = 0, memoPen = 0;
					for (int i = 0; i < 9; i++) {
						memoPen = i;
						if (colSplit != -1 && tailleSplit < 32 && i > 2)
							memoPen = colSplit;

						RvbColor c = tabCol[memoPen, y >> 1];
						int dist = (c.r - pix.r) * (c.r - pix.r) * prm.coefR + (c.v - pix.v) * (c.v - pix.v) * prm.coefV + (c.b - pix.b) * (c.b - pix.b) * prm.coefB;
						if (dist < oldDist) {
							choix = memoPen;
							oldDist = dist;
							if (dist == 0 || memoPen == colSplit)
								i = 9;
						}
					}
					if (choix > 2) {
						if (colSplit != choix) {
							colSplit = choix;
							tailleSplit = 0;
						}
					}
					//if (choix == colSplit)
					tailleSplit++;

					dest.SetPixelCpc(x, y, choix, 2);
				}
			}
		}
	}
}
