using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ConvImgCpc {
	public partial class EditTrameAscii : Form {
		private readonly BitmapCpc bmpCpc;
		private readonly DirectBitmap bmpTrame;
		private readonly ImageCpc imgCpc;
		private readonly Param param;
		private readonly Main main;
		private int numTrame = 0;
		private byte penLeft = 1, penRight = 0;

		public EditTrameAscii(Main m, ImageCpc i, Param p) {
			InitializeComponent();
			main = m;
			main.ChangeLanguage(Controls, "EditTrameAscii");
			imgCpc = i;
			bmpCpc = i.BitmapCpc;
			param = p;
			bmpTrame = new DirectBitmap(pictEditMatrice.Width, pictEditMatrice.Height);
			pictEditMatrice.Image = bmpTrame.Bitmap;
			DrawMatrice();
			DrawTrame();
			lblPen0.BackColor = Color.FromArgb(bmpCpc.GetColorPal(0).GetColorArgb);
			lblPen1.BackColor = Color.FromArgb(bmpCpc.GetColorPal(1).GetColorArgb);
			lblPen2.BackColor = Color.FromArgb(bmpCpc.GetColorPal(2).GetColorArgb);
			lblPen3.BackColor = Color.FromArgb(bmpCpc.GetColorPal(3).GetColorArgb);
			DrawPens();
			numTabTrame.Maximum = Cpc.TramesAscUt.GetLength(0) - 1;
		}

		private void DrawMatrice() {
			DirectBitmap bmp = new DirectBitmap(pictAllMatrice.Width, pictAllMatrice.Height);
			pictAllMatrice.Image = bmp.Bitmap;
			for (int i = 0; i < 16; i++)
				for (int y = 0; y < 4; y++)
					for (int x = 0; x < 4; x++)
						for (int zx = 0; zx < (x == 3 ? 7 : 8); zx++)
							for (int zy = 0; zy < 8; zy++)
								bmp.SetPixel(zx + ((x + (i << 2)) << 3), zy + (y << 3), bmpCpc.GetColorPal(Cpc.trameM1[i, x, y]));

			pictAllMatrice.Refresh();
		}

		private void DrawTrame() {
			bpPrev.Visible = numTrame > 0;
			bpSuiv.Visible = numTrame < 15;
			for (int y = 0; y < 4; y++)
				for (int x = 0; x < 4; x++)
					for (int zx = 0; zx < 78; zx++)
						for (int zy = 0; zy < 78; zy++)
							bmpTrame.SetPixel(zx + (x * 80), zy + (y * 80), bmpCpc.GetColorPal(Cpc.trameM1[numTrame, x, y]));

			pictEditMatrice.Refresh();
		}

		private void DrawPens() {
			lblPenLeft.BackColor = Color.FromArgb(bmpCpc.GetColorPal(penLeft).GetColorArgb);
			lblPenRight.BackColor = Color.FromArgb(bmpCpc.GetColorPal(penRight).GetColorArgb);
		}

		private void PictAllMatrice_MouseDown(object sender, MouseEventArgs e) {
			int y = e.X / 32;
			if (y >= 0 && y < 16) {
				numTrame = y;
				DrawTrame();
			}
		}

		private void PictEditMatrice_MouseMove(object sender, MouseEventArgs e) {
			int x = e.X / 80;
			int y = e.Y / 80;
			if (x >= 0 && y >= 0 && x < 4 && y < 4) {
				if (e.Button == MouseButtons.Left) {
					Cpc.trameM1[numTrame, x, y] = penLeft;
					DrawMatrice();
					DrawTrame();
				}
				else
					if (e.Button == MouseButtons.Right) {
					Cpc.trameM1[numTrame, x, y] = penRight;
					DrawMatrice();
					DrawTrame();
				}
			}
		}

		private void BpPrev_Click(object sender, EventArgs e) {
			numTrame--;
			DrawTrame();
		}

		private void BpSuiv_Click(object sender, EventArgs e) {
			numTrame++;
			DrawTrame();
		}

		private void LblPen0_Click(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left)
				penLeft = 0;
			else
				penRight = 0;

			DrawPens();
		}

		private void LblPen1_Click(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left)
				penLeft = 1;
			else
				penRight = 1;

			DrawPens();
		}

		private void LblPen2_Click(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left)
				penLeft = 2;
			else
				penRight = 2;

			DrawPens();
		}

		private void LblPen3_Click(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left)
				penLeft = 3;
			else
				penRight = 3;

			DrawPens();
		}

		private void BpRead_Click(object sender, EventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog { Filter = "Modèle trames (.xml)|*.xml" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				FileStream fileParam = File.Open(dlg.FileName, FileMode.Open);
				try {
					byte[] trame = (byte[])new XmlSerializer(typeof(byte[])).Deserialize(fileParam);
					int pos = 0;
					for (int i = 0; i < 16; i++)
						for (int y = 0; y < 4; y++)
							for (int x = 0; x < 4; x++)
								Cpc.trameM1[i, x, y] = trame[pos++];

					DrawMatrice();
					DrawTrame();
				}
				catch {
					main.DisplayErreur("Impossible de lire la trame.");
				}
				fileParam.Close();
			}
		}

		private void BpSave_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog { Filter = "Modèle trames (.xml)|*.xml" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				byte[] trame = new byte[256];
				int pos = 0;
				for (int i = 0; i < 16; i++)
					for (int y = 0; y < 4; y++)
						for (int x = 0; x < 4; x++)
							trame[pos++] = Cpc.trameM1[i, x, y];

				FileStream file = File.Open(dlg.FileName, FileMode.Create);
				try {
					new XmlSerializer(typeof(byte[])).Serialize(file, trame);
				}
				catch {
					main.DisplayErreur("Impossible de sauver la trame.");
				}
				file.Close();
			}
		}

		private void BpAutoGene_Click(object sender, EventArgs e) {
			Enabled = false;
			int nbImages = main.GetMaxImages();
			List<TrameM1> lstTrame = new List<TrameM1>();
			for (int i = 0; i < nbImages; i++) {
				main.SelectImage(i);
				DirectBitmap tmp = main.GetResizeBitmap();
				Conversion.CnvTrame(tmp, param, imgCpc, lstTrame);
				tmp.Dispose();
				lstTrame.Sort((x, y) => y.nbFound - x.nbFound);
			}
			int maxTrame = Math.Min(16, lstTrame.Count);
			for (int i = 0; i < maxTrame; i++)
				for (int y = 0; y < 4; y++)
					for (int x = 0; x < 4; x++)
						Cpc.trameM1[i, x, y] = lstTrame[i].GetPix(x, y);

			DrawMatrice();
			DrawTrame();
			Enabled = true;
		}

		private void BpCopyTrame_Click(object sender, EventArgs e) {
			Cpc.CopyTrame((int)numTabTrame.Value);
			DrawMatrice();
			DrawTrame();
		}

		private void BpForceCalc_Click(object sender, EventArgs e) {
			Enabled = false;
			main.Convert(true);
			Enabled = true;
		}
	}

	public class TrameM1 {
		private readonly byte[,] trame = new byte[4, 4];
		public int nbFound = 0;

		public void SetPix(int x, int y, byte p) {
			trame[x, y] = p;
		}

		public byte GetPix(int x, int y) {
			return trame[x, y];
		}

		public bool IsSame(TrameM1 t, int deltaErr = 0) {
			int nbErr = 0;

			for (int y = 0; y < 4; y++)
				for (int x = 0; x < 4; x++)
					if (trame[x, y] != t.GetPix(x, y)) {
						if (++nbErr > deltaErr)
							return false;
					}
			nbFound++;
			return true;
		}
	}
}
