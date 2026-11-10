namespace ConvImgCpc {
	public static partial class Conversion {
		//
		// Recherche les couleurs pour le mode "X"
		//
		static int RechercheCMaxModeX(int[,] colMode5, int[] lockState, int yMax, Param prm) {
			return ChoosePensPerLine(colMode5, lockState, yMax, prm, 2, 2);
		}

		// Conversion "Mode X"
		static private void ConvertModeX(DirectBitmap source, Param prm, ImageCpc dest, RvbColor[,] tabCol) {
			double[] pl = new double[4], pa = new double[4], pb = new double[4];
			int offsetY = prm.modeImpDraw && Cpc.TailleY == 544 ? 2 : 0;
			for (int y = 0; y < Cpc.TailleY; y += 2) {
				PenLab(tabCol, y >> 1, 4, pl, pa, pb);
				for (int x = 0; x < Cpc.TailleX; x += 2) {
					RvbColor pix = ConvReadPixel(source, prm, x, y, 2);
					int choix = NearestPenLine(prm, pix, tabCol, y >> 1, 4, pl, pa, pb);
					ConvDiffuse(source, prm, x, y, 2, 2, pix, tabCol[choix, y >> 1]);
					dest.SetPixelCpc(x, y + offsetY, choix, 2);
				}
			}
		}
	}
}
