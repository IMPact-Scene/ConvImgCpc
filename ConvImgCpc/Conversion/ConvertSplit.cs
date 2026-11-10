namespace ConvImgCpc {
	public static partial class Conversion {
		//
		//
		// Recherche les couleurs pour le mode "split"
		//
		static int RechercheCMaxModeSplit(int[,] colMode5, int[] lockState, int yMax, Param prm) {
			return ChoosePensPerLine(colMode5, lockState, yMax, prm, 3, 6);   // méthode d'origine ci-dessous si OptOptimizePalette = false
		}

		// Conversion avec des "splits-rasters"
		static private void ConvertSplit(DirectBitmap source, Param prm, ImageCpc dest, RvbColor[,] tabCol) {
			double[] pl = new double[9], pa = new double[9], pb = new double[9];
			for (int y = 0; y < Cpc.TailleY; y += 2) {
				PenLab(tabCol, y >> 1, 9, pl, pa, pb);
				int tailleSplit = 0, colSplit = -1;
				for (int x = 0; x < Cpc.TailleX; x += 2) {
					RvbColor pix = ConvReadPixel(source, prm, x, y, 2);
					LabFromRvb(pix.r, pix.v, pix.b, out double pL, out double pA, out double pB);
					double oldDist = double.MaxValue;
					int choix = 0, memoPen;
					for (int i = 0; i < 9; i++) {
						memoPen = i;
						if (colSplit != -1 && tailleSplit < 32 && i > 2)
							memoPen = colSplit;

						double dist = PenDistance(pL, pA, pB, pl[memoPen], pa[memoPen], pb[memoPen]);
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

					ConvDiffuse(source, prm, x, y, 2, 2, pix, tabCol[choix, y >> 1]);
					dest.SetPixelCpc(x, y, choix, 2);
				}
			}
		}
	}
}
