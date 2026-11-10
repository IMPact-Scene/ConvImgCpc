namespace ConvImgCpc {
	public static partial class Conversion {
		// Conversion "standard"
		static private void ConvertStd(DirectBitmap source, Param prm, ImageCpc dest, int maxPen, RvbColor[,] tabCol) {
			int offsetY = prm.modeImpDraw && Cpc.TailleY == 544 ? 2 : 0;
			double[] pl = new double[16], pa = new double[16], pb = new double[16];
			for (int y = 0; y < Cpc.TailleY; y += 2) {
				int Tx = Cpc.CalcTx(y);
				maxPen = Cpc.MaxPen(y);
				PenLab(tabCol, y >> 1, maxPen, pl, pa, pb);
				for (int x = 0; x < Cpc.TailleX; x += Tx) {
					// Pixel d'origine + réglages + erreur diffusée (tramage avec la palette finale)
					RvbColor pix = ConvReadPixel(source, prm, x, y, Tx);
					int choix = NearestPenLine(prm, pix, tabCol, y >> 1, maxPen, pl, pa, pb, true);
					RvbColor col = tabCol[choix, y >> 1];
					if (col != null) {
						ConvDiffuse(source, prm, x, y, Tx, 2, pix, col);
						if (redoActive)
							source.SetPixel(x, y, col);     // la source contient l'image réduite, comme après la passe 1
					}
					dest.SetPixelCpc(x, y + offsetY, choix, Tx);
				}
			}
		}
	}
}
