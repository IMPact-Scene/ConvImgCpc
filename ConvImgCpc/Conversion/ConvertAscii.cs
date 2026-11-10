namespace ConvImgCpc {
	public static partial class Conversion {
		// Conversion ASC0 à ASC2
		static private void ConvertAscii(DirectBitmap source, Param prm, ImageCpc dest, int maxPen, RvbColor[,] tabCol) {
			int incY = 8;
			RvbColor pix;
			double[] pl = new double[16], pa = new double[16], pb = new double[16];
			for (int y = 0; y < Cpc.TailleY; y += incY) {
				int Tx = Cpc.CalcTx(y);
				//maxPen = Cpc.MaxPen(y);
				PenLab(tabCol, y >> 1, maxPen, pl, pa, pb);
				for (int x = 0; x < Cpc.TailleX; x += Tx) {
					if (redoActive)
						pix = ConvReadBlock(source, prm, x, y, Tx, incY);     // moyenne du bloc, puis tramage avec la palette finale
					else {
						int r = 0, v = 0, b = 0;
						for (int j = 0; j < incY; j += 2) {
							pix = source.GetPixelColor(x, y + j);
							r += pix.r;
							v += pix.v;
							b += pix.b;
						}
						pix = new RvbColor((byte)(r >> 2), (byte)(v >> 2), (byte)(b >> 2));
					}
					int choix = NearestPenLine(prm, pix, tabCol, y >> 1, maxPen, pl, pa, pb);
					ConvDiffuse(source, prm, x, y, Tx, incY, pix, tabCol[choix, y >> 1]);
					int offsetY = prm.modeImpDraw && Cpc.TailleY == 544 ? 2 : 0;
					for (int j = 0; j < incY; j += 2)
						dest.SetPixelCpc(x, y + offsetY + j, choix, Tx);
				}
			}
		}
	}
}
