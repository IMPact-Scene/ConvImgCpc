using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ConvImgCpc {
	public partial class EditSplit : Form {
		private readonly int offsetX = 0, offsetY = 0;
		private readonly DirectBitmap bmpLock;
		private readonly int taillex = 384; // Résolution image horizontale en pixels mode 1
		private readonly int tailley = 272; // Résolution image verticale en pixels
		private readonly Label[] colors = new Label[16];
		private bool doRender;
		private readonly DirectBitmap bitmapZoom;
		private readonly Main main;
		private LigneSplit curLigneSplit;
		public BitmapCpc bitmapCpc;
		private int numCol = 0;
		//private int mode = 1;

		public EditSplit(Main m, DirectBitmap imgSource) {
			InitializeComponent();
			main = m;
			int tx = pictureBox.Width;
			int ty = pictureBox.Height;
			bitmapZoom = new DirectBitmap(pictureZoom.Width >> 3, pictureZoom.Height >> 3);
			bmpLock = new DirectBitmap(tx, ty);
			pictureBox.Image = bmpLock.Bitmap;
			bitmapCpc = new BitmapCpc(taillex << 1, tailley << 1, Cpc.modeVirtuel); // ###

			retard.Minimum = BitmapCpc.retardMin;
			retard.Maximum = BitmapCpc.retardMin + 32;

			for (int i = 0; i < 16; i++) {
				// Générer les contrôles de "couleurs"
				colors[i] = new Label {
					BorderStyle = BorderStyle.FixedSingle,
					Location = new Point(4 + i * 48, 20),
					Size = new Size(40, 32),
					Tag = i
				};
				colors[i].Click += ClickColor;
				groupPal.Controls.Add(colors[i]);
			}
			Reset();
			UpdateScrSplit(imgSource);
		}

		public void UpdateScrSplit(DirectBitmap img) {
			for (int i = 0; i < 16; i++) {
				bitmapCpc.splitPalette[0, 0, i] = Cpc.Palette[i];
				colors[numCol].BackColor = Color.FromArgb(bitmapCpc.GetPaletteColor(0, 0, numCol).GetColorArgb);
				colors[numCol].Refresh();
			}
			bitmapCpc.CreeBmpCpc(img);
			DisplayLigne();
		}

		private void UpdatePalette() {
			for (int i = 0; i < 16; i++) {
				colors[i].BackColor = Color.FromArgb(bitmapCpc.GetPaletteColor(0, 0, i).GetColorArgb);
				colors[i].Refresh();
			}
		}

		private void DisplayLigne() {
			doRender = false;
			curLigneSplit = bitmapCpc.splitEcran.GetLigne((int)numLigne.Value);
			numPen.Value = curLigneSplit.numPen;
			retard.Value = curLigneSplit.retard;
			lblColor0.Visible = largSplit0.Visible = chkSplit0.Checked = curLigneSplit.GetSplit(0).enable;
			largSplit0.Value = curLigneSplit.GetSplit(0).longueur;
			lblColor0.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(0).couleur].GetColorArgb);
			lblColor1.Visible = largSplit1.Visible = chkSplit1.Checked = curLigneSplit.GetSplit(1).enable && curLigneSplit.GetSplit(0).enable;
			largSplit1.Value = curLigneSplit.GetSplit(1).longueur;
			lblColor1.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(1).couleur].GetColorArgb);
			lblColor2.Visible = largSplit2.Visible = chkSplit2.Checked = curLigneSplit.GetSplit(2).enable && curLigneSplit.GetSplit(1).enable && curLigneSplit.GetSplit(0).enable;
			largSplit2.Value = curLigneSplit.GetSplit(2).longueur;
			lblColor2.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(2).couleur].GetColorArgb);
			lblColor3.Visible = largSplit3.Visible = chkSplit3.Checked = curLigneSplit.GetSplit(3).enable && curLigneSplit.GetSplit(2).enable && curLigneSplit.GetSplit(1).enable && curLigneSplit.GetSplit(0).enable;
			largSplit3.Value = curLigneSplit.GetSplit(3).longueur;
			lblColor3.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(3).couleur].GetColorArgb);
			lblColor4.Visible = largSplit4.Visible = chkSplit4.Checked = curLigneSplit.GetSplit(4).enable && curLigneSplit.GetSplit(3).enable && curLigneSplit.GetSplit(2).enable && curLigneSplit.GetSplit(1).enable && curLigneSplit.GetSplit(0).enable;
			largSplit4.Value = curLigneSplit.GetSplit(4).longueur;
			lblColor4.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(4).couleur].GetColorArgb);
			lblColor5.Visible = largSplit5.Visible = chkSplit5.Checked = curLigneSplit.GetSplit(5).enable && curLigneSplit.GetSplit(4).enable && curLigneSplit.GetSplit(3).enable && curLigneSplit.GetSplit(2).enable && curLigneSplit.GetSplit(1).enable && curLigneSplit.GetSplit(0).enable;
			largSplit5.Value = curLigneSplit.GetSplit(5).longueur;
			lblColor5.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(5).couleur].GetColorArgb);
			lblColor6.Visible = largSplit6.Visible = chkSplit6.Checked = curLigneSplit.GetSplit(6).enable && curLigneSplit.GetSplit(5).enable && curLigneSplit.GetSplit(4).enable && curLigneSplit.GetSplit(3).enable && curLigneSplit.GetSplit(2).enable && curLigneSplit.GetSplit(1).enable && curLigneSplit.GetSplit(0).enable;
			largSplit6.Value = curLigneSplit.GetSplit(6).longueur;
			lblColor6.BackColor = Color.FromArgb(BitmapCpc.RgbCPC[curLigneSplit.GetSplit(6).couleur].GetColorArgb);
			chkChangeMode.Checked = curLigneSplit.changeMode;
			modeCpc.Value = curLigneSplit.newMode;
			doRender = true;
			UpdatePalette();
			Render();
		}

		// Changement de la palette
		private void ClickColor(object sender, System.EventArgs e) {
			Label colorClick = sender as Label;
			numCol = colorClick.Tag != null ? (int)colorClick.Tag : 0;
			EditColor ed = new EditColor(main, 4, numCol, bitmapCpc.GetPaletteColor(0, 0, numCol).GetColorArgb, false);
			ed.ShowDialog(this);
			if (ed.isValide) {
				bitmapCpc.splitPalette[0, 0, numCol] = ed.ValColor;
				colors[numCol].BackColor = Color.FromArgb(bitmapCpc.GetPaletteColor(0, 0, numCol).GetColorArgb);
				colors[numCol].Refresh();
				Render();
			}
		}

		public void Reset() {
			int col = System.Drawing.SystemColors.Control.ToArgb();
			for (int x = 0; x < bmpLock.Width; x++)
				for (int y = 0; y < bmpLock.Height; y++)
					bmpLock.SetPixel(x, y, col);
		}

		public void Render() {
			if (doRender) {
				bitmapCpc.CalcPaletteSplit();
				bitmapCpc.Render(bmpLock, offsetX);
				DrawZoomPicture();
				if (chkChgt.Checked) {
					LigneSplit lSpl = bitmapCpc.splitEcran.GetLigne((int)numLigne.Value);
					Graphics g = Graphics.FromImage(pictureBox.Image);
					numCol = lSpl.numPen;
					int xpos = lSpl.retard >> 2;
					int x = xpos;
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x << 3, 0, x << 3, pictureBox.Image.Height);
					for (int ns = 0; ns < 7; ns++) {
						Split s = lSpl.GetSplit(ns);
						if (s.enable) {
							xpos += s.longueur >> 2;

							if (xpos > 96)
								xpos = 96;

							// de x à xpos => faire palette = curPal
							x = xpos;
							XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x << 3, 0, x << 3, pictureBox.Image.Height);
						}
						else
							break;
					}
				}
				pictureBox.Refresh();
			}
		}

		private void DrawZoomPicture() {
			int posy = Math.Max(0, Math.Min(((int)numLigne.Value << 1) - 7, bmpLock.Height - 14));
			for (int y = 0; y < bitmapZoom.Height; y++)
				for (int x = 0; x < bitmapZoom.Width; x++)
					bitmapZoom.SetPixel(x, y, bmpLock.GetPixelColor(x + hScrollZoom.Value, y + posy).GetColorArgb);

			Bitmap zoomed = (Bitmap)pictureZoom.Image;
			zoomed?.Dispose();
			zoomed = new Bitmap(bitmapZoom.Width << 3, bitmapZoom.Height << 3);
			using (Graphics g = Graphics.FromImage(zoomed)) {
				g.InterpolationMode = System.Drawing.Drawing2D.InterpolationMode.NearestNeighbor;
				g.DrawImage(bitmapZoom.Bitmap, new Rectangle(Point.Empty, zoomed.Size));
			}
			pictureZoom.Image = zoomed;
			pictureZoom.Refresh();
		}

		private void PictureBox_MouseMove(object sender, MouseEventArgs e) {
			int yReel = (offsetY + e.Y) >> 1;
			int xReel = (offsetX + e.X) >> 1;
			lblInfo.Text = "Position X:" + xReel.ToString("000") + ", Y:" + yReel.ToString("000");
		}

		private void PictureBox_MouseLeave(object sender, EventArgs e) {
			lblInfo.Text = "...";
		}

		private void BpSave_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog { Filter = "Fichiers SplitEditor (*.xml)|*.xml" };
			DialogResult result = dlg.ShowDialog();
			if (result == DialogResult.OK) {
				FileStream file = File.Open(dlg.FileName, FileMode.Create);
				try {
					new XmlSerializer(typeof(SplitEcran)).Serialize(file, bitmapCpc.splitEcran);
				}
				catch (Exception ex) {
					MessageBox.Show(ex.StackTrace, ex.Message);
				}
				file.Close();
			}
		}

		private void BpLoad_Click(object sender, EventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog { Filter = "Fichiers SplitEditor (*.xml)|*.xml" };
			DialogResult result = dlg.ShowDialog();
			if (result == DialogResult.OK) {
				FileStream file = File.Open(dlg.FileName, FileMode.Open);
				try {
					SplitEcran spl = (SplitEcran)new XmlSerializer(typeof(SplitEcran)).Deserialize(file);
					bitmapCpc.splitEcran = spl;
				}
				catch (Exception ex) {
					MessageBox.Show(ex.StackTrace, ex.Message);
				}
				file.Close();
				DisplayLigne();
			}
		}

		private void NumLigne_ValueChanged(object sender, EventArgs e) {
			DisplayLigne();
		}

		private void NumPenMode_ValueChanged(object sender, EventArgs e) {
			curLigneSplit.numPen = (int)numPen.Value;
		}

		private void Retard_ValueChanged(object sender, EventArgs e) {
			curLigneSplit.retard = (int)retard.Value;
			DisplayLigne();
		}

		private void ChkChangeMode_CheckedChanged(object sender, EventArgs e) {
			modeCpc.Visible = chkChangeMode.Checked;
			curLigneSplit.changeMode = chkChangeMode.Checked;
		}

		private void ModeCpc_ValueChanged(object sender, EventArgs e) {
			curLigneSplit.newMode = (int)modeCpc.Value;
		}

		private void ChangeLargeur(int index, NumericUpDown val) {
			curLigneSplit.GetSplit(index).longueur = ((int)val.Value) & 0xFFC;
			DisplayLigne();
		}

		private void LargSplit1_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(0, largSplit0);
		}

		private void LargSplit2_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(1, largSplit1);
		}

		private void LargSplit3_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(2, largSplit2);
		}

		private void LargSplit4_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(3, largSplit3);
		}

		private void LargSplit5_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(4, largSplit4);
		}

		private void LargSplit6_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(5, largSplit5);
		}

		private void LargSplit7_ValueChanged(object sender, EventArgs e) {
			if (doRender)
				ChangeLargeur(6, largSplit6);
		}

		private void EnableSplit(int index, CheckBox chk, CheckBox prec) {
			if (prec == null || prec.Checked) {
				curLigneSplit.GetSplit(index).enable = chk.Checked;
				DisplayLigne();
			}
			else
				chk.Checked = false;
		}

		private void ChkSplit1_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(0, chkSplit0, null);
		}

		private void ChkSplit2_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(1, chkSplit1, chkSplit0);
		}

		private void ChkSplit3_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(2, chkSplit2, chkSplit1);
		}

		private void ChkSplit4_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(3, chkSplit3, chkSplit2);
		}

		private void ChkSplit5_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(4, chkSplit4, chkSplit3);
		}

		private void ChkSplit6_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(5, chkSplit5, chkSplit4);
		}

		private void ChkSplit7_CheckedChanged(object sender, EventArgs e) {
			if (doRender)
				EnableSplit(6, chkSplit6, chkSplit5);
		}

		private void ChangeColor(int index) {
			Split curXSplit = curLigneSplit.GetSplit(index);
			EditColor ed = new EditColor(main, 4, curXSplit.couleur, BitmapCpc.RgbCPC[curXSplit.couleur].GetColorArgb, false);
			ed.ShowDialog(this);
			if (ed.isValide) {
				curXSplit.couleur = ed.ValColor;
				DisplayLigne();
			}
		}

		private void LblColor1_Click(object sender, EventArgs e) {
			ChangeColor(0);
			Render();
		}

		private void LblColor2_Click(object sender, EventArgs e) {
			ChangeColor(1);
			Render();
		}

		private void LblColor3_Click(object sender, EventArgs e) {
			ChangeColor(2);
			Render();
		}

		private void LblColor4_Click(object sender, EventArgs e) {
			ChangeColor(3);
			Render();
		}

		private void LblColor5_Click(object sender, EventArgs e) {
			ChangeColor(4);
			Render();
		}

		private void LblColor6_Click(object sender, EventArgs e) {
			ChangeColor(5);
			Render();
		}

		private void LblColor7_Click(object sender, EventArgs e) {
			ChangeColor(6);
			Render();
		}

		private void BpCopieLigne_Click(object sender, EventArgs e) {
			if ((int)numLigne.Value > 0) {
				LigneSplit lignePrec = bitmapCpc.splitEcran.GetLigne((int)numLigne.Value - 1);
				curLigneSplit.numPen = lignePrec.numPen;
				curLigneSplit.retard = lignePrec.retard;
				curLigneSplit.changeMode = lignePrec.changeMode;
				curLigneSplit.newMode = lignePrec.newMode;
				for (int i = 0; i < 7; i++) {
					curLigneSplit.ListeSplit[i].enable = lignePrec.ListeSplit[i].enable;
					curLigneSplit.ListeSplit[i].couleur = lignePrec.ListeSplit[i].couleur;
					curLigneSplit.ListeSplit[i].longueur = lignePrec.ListeSplit[i].longueur;
				}
				DisplayLigne();
			}
		}
		private void BpImportSplit_Click(object sender, EventArgs e) {
			Enabled = false;
			ImportSplit imp = new ImportSplit(bitmapCpc.splitEcran);
			imp.ShowDialog();
			DisplayLigne();
			Enabled = true;
		}

		private void BpGenAsm_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog { Filter = "Source assembleur (*.asm)|*.asm" };
			DialogResult result = dlg.ShowDialog();
			if (result == DialogResult.OK) {
				Enabled = false;
				FileStream fs = new FileStream(dlg.FileName, FileMode.Create, FileAccess.Write);
				StreamWriter sw = new StreamWriter(fs);
				GenSplitAsm.CreeAsm(sw, bitmapCpc);
				sw.Close();
				fs.Close();
				Enabled = true;
			}
		}

		//static public int SauveScr(string fileName, BitmapCpc bitmapCpc, Main main) {
		//	byte[] bufPack = new byte[0x8000];
		//	short startAdr, exec;
		//	CpcAmsdos entete;
		//	int lg = Cpc.BitmapSize;

		//	if (File.Exists(fileName))
		//		main.dsk.Load(fileName);
		//	else
		//		main.dsk.FormatDsk();

		//	MemoryStream ms = new MemoryStream();
		//	entete = Cpc.CreeEntete(fileName, startAdr, (short)lg, exec);
		//	BinaryWriter fpm = new BinaryWriter(ms);
		//	fpm.Write(Cpc.AmsdosToByte(entete));
		//	fpm.Write(compact != Main.PackMethode.None ? bufPack : bitmapCpc.bmpCpc, 0, lg);
		//	fpm.Close();
		//	byte[] fic = ms.ToArray();
		//	GestDSK.DskError err = GestDSK.DskError.ERR_NO_ERR;
		//	for (int i = 0; i < 100; i++) {
		//		string nom = "PICT" + i.ToString("00") + ".BIN";
		//		err = main.dsk.CopieFichier(fic, nom, fic.Length, 178, 0);
		//		if (err == GestDSK.DskError.ERR_NO_ERR) {
		//			main.SetInfo(main.multilingue.GetString("Main.prg.TxtInfo33") + nom);
		//			break;
		//		}
		//	}
		//	if (err == GestDSK.DskError.ERR_NO_ERR)
		//		main.dsk.Save(fileName);
		//	else {
		//		switch (err) {
		//			case GestDSK.DskError.ERR_FILE_EXIST:
		//			case GestDSK.DskError.ERR_NO_DIRENTRY:
		//				MessageBox.Show(main.multilingue.GetString("Main.prg.TxtInfo34"));
		//				break;

		//			case GestDSK.DskError.ERR_NO_BLOCK:
		//				MessageBox.Show(main.multilingue.GetString("Main.prg.TxtInfo35"));
		//				break;
		//		}
		//	}
		//	return (lg);
		//}

		private void PictureBox_MouseClick(object sender, MouseEventArgs e) {
			numLigne.Value = e.Y >> 1;
			hScrollZoom.Value = Math.Max(hScrollZoom.Minimum, Math.Min(e.X - 48, hScrollZoom.Maximum));
			Render();
		}

		private void ChkChgt_CheckedChanged(object sender, EventArgs e) {
			Render();
		}

		private void HScrollZoom_Scroll(object sender, ScrollEventArgs e) {
			Render();
		}

		private void EditSplit_FormClosed(object sender, FormClosedEventArgs e) {
			main.editSplit = null;
		}
	}
}
