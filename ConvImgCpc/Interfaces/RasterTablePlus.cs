using System;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ConvImgCpc {
	public partial class RasterTablePlus : Form {
		private readonly DirectBitmap bmpImage;
		private readonly DirectBitmap bmpFond;
		private readonly int selColor = -1;
		private readonly Main main;
		private RvbColor oldCol = new RvbColor(0);
		private int newStart = 0, newEnd = 15, lastAddConstat = 0;
		private Raster raster = new Raster();

		public RasterTablePlus(Main m, DirectBitmap bmp) {
			InitializeComponent();
			bmpFond = bmp;
			main = m;
			bmpImage = new DirectBitmap(pictureBox.Width, pictureBox.Height);
			pictureBox.Image = bmpImage.Bitmap;
			SelectColor sel = new SelectColor();
			sel.ShowDialog();
			if (sel.selColor != -1) {
				int col = Cpc.Palette[sel.selColor];
				int r = ((col & 0x0F) * 17);
				int v = (((col & 0xF00) >> 8) * 17);
				int b = (((col & 0xF0) >> 4) * 17);
				selColor = (r << 16) + (v << 8) + b;
			}
			DrawLines();
		}

		public void DrawLines() {
			pictureBox.Image = bmpImage.Bitmap;

			txbStart.Text = newStart.ToString();
			for (int i = 0; i < 272; i++)
				for (int x = 0; x < 768; x += 2) {
					int c = bmpFond.GetPixel(x, i * 2);
					if (selColor == c || selColor == -1)
						c = ((raster.line[i] & 0x0F) * 17) + (((raster.line[i] >> 8) * 17) << 8) + ((((raster.line[i] & 0xF0) >> 4) * 17) << 16);

					bmpImage.SetHorLineDouble(x, i * 2, 2, c);
				}
			txbLineStart.Text = raster.minLine.ToString();
			txbLineEnd.Text = raster.maxLine.ToString();
			bpGenerate.Enabled = raster.minLine < raster.maxLine;

			if (chkSprites.Checked)
				pictureBox.Image = main.imgCpc.DrawSpritesHard(null, bmpImage.Bitmap);

			pictureBox.Refresh();
		}

		private void TrkStartR_Scroll(object sender, EventArgs e) {
			txbStartR.Text = trkStartR.Value.ToString();
		}

		private void TrkStartV_Scroll(object sender, EventArgs e) {
			txbStartV.Text = trkStartV.Value.ToString();
		}

		private void TrkStartB_Scroll(object sender, EventArgs e) {
			txbStartB.Text = trkStartB.Value.ToString();
		}

		private void TxbStartR_TextChanged(object sender, EventArgs e) {
			GenPalette.SetCompValue(txbStartR.Text, ref trkStartR, trkStartR, trkStartV, trkStartB, lblStartColor);
		}

		private void TxbStartV_TextChanged(object sender, EventArgs e) {
			GenPalette.SetCompValue(txbStartV.Text, ref trkStartV, trkStartR, trkStartV, trkStartB, lblStartColor);
		}

		private void TxbStartB_TextChanged(object sender, EventArgs e) {
			GenPalette.SetCompValue(txbStartB.Text, ref trkStartB, trkStartR, trkStartV, trkStartB, lblStartColor);
		}

		private void BpRMoins_Click(object sender, EventArgs e) {
			if (trkStartR.Value > 0)
				trkStartR.Value--;
		}

		private void BpRPlus_Click(object sender, EventArgs e) {
			if (trkStartR.Value < 15)
				trkStartR.Value++;
		}

		private void BpVMoins_Click(object sender, EventArgs e) {
			if (trkStartV.Value > 0)
				trkStartV.Value--;
		}

		private void BpVPlus_Click(object sender, EventArgs e) {
			if (trkStartV.Value < 15)
				trkStartV.Value++;
		}

		private void BpBMoins_Click(object sender, EventArgs e) {
			if (trkStartB.Value > 0)
				trkStartB.Value--;
		}

		private void BpBPlus_Click(object sender, EventArgs e) {
			if (trkStartB.Value < 15)
				trkStartB.Value++;
		}

		private void BpClearAll_Click(object sender, EventArgs e) {
			for (int i = 0; i < 272; i++)
				raster.line[i] = 0;

			newStart = 0;
			newEnd = 15;
			raster.minLine = raster.maxLine = 0;
			oldCol = new RvbColor(0);
			DrawLines();
		}

		private void BpAddConstant_Click(object sender, EventArgs e) {
			int sens = rbPlus.Checked ? 1 : -1;
			if (int.TryParse(txbConstant.Text, out lastAddConstat) && int.TryParse(txbStart.Text, out newStart)
				&& lastAddConstat >= 0 && lastAddConstat < 272 && lastAddConstat + sens * newStart <= 272 && lastAddConstat * sens + newStart >= 0) {
				if (double.TryParse(txbStartR.Text, out double rs) && double.TryParse(txbStartV.Text, out double vs) && double.TryParse(txbStartB.Text, out double bs)) {
					if (rs >= 0 && vs >= 0 && bs >= 0 && rs < 16 && vs < 16 && bs < 16) {
						for (int i = 0; i < lastAddConstat; i++)
							raster.line[newStart + i * sens] = (int)(bs + ((int)rs << 4) + ((int)vs << 8));
					}
					newStart += sens * lastAddConstat;
					newEnd += sens * lastAddConstat;
					raster.maxLine = newStart - 1;
					DrawLines();
					bpUndo.Enabled = true;
				}
			}
		}

		private void PictureBox_MouseMove(object sender, MouseEventArgs e) {
			lblLine.Text = "line " + (e.Y / 2).ToString();
		}

		private void BpUndo_Click(object sender, EventArgs e) {
			newStart -= lastAddConstat;
			newEnd -= lastAddConstat;
			raster.maxLine = newStart - 1;
			for (int i = 0; i < lastAddConstat; i++)
				raster.line[i + newStart] = 0;

			DrawLines();
			bpUndo.Enabled = false;
		}

		private void BpAddLine_Click(object sender, EventArgs e) {
			GenPalette g = new GenPalette(ref raster.line, newStart, newEnd, oldCol);
			g.ShowDialog();
			if (g.done) {
				newStart = g.end + 1;
				newEnd = g.end + (g.end - g.start) + 1;
				raster.minLine = raster.minLine > g.start ? g.start : raster.minLine;
				raster.maxLine = raster.maxLine < g.end ? g.end : raster.maxLine;
				oldCol = new RvbColor((byte)(((raster.line[g.end] & 0xF0) >> 4) * 17), (byte)((raster.line[g.end] >> 8) * 17), (byte)((raster.line[g.end] & 0x0F) * 17));
				DrawLines();
			}
			bpUndo.Enabled = false;
		}

		private void BpCopyToClipboard_Click(object sender, EventArgs e) {
			if (int.TryParse(txbLineStart.Text, out int start) && int.TryParse(txbLineEnd.Text, out int end) && start < end && start >= 0 && end < 272) {
				raster.minLine = start;
				raster.maxLine = end;
				Clipboard.SetText(SaveAsm.GenereDatas(raster.line, start, end, 16));
			}
		}

		private void BpLoad_Click(object sender, EventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog { Filter = "Fichiers xml (*.xml)|*.xml" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				FileStream fileParam = File.Open(dlg.FileName, FileMode.Open);
				try {
					raster = (Raster)new XmlSerializer(typeof(Raster)).Deserialize(fileParam);
					DrawLines();
				}
				catch {
					MessageBox.Show("Erreur lecture rasters ...");
				}
				fileParam.Close();
			}
		}

		private void BpSave_Click(object sender, EventArgs e) {
			if (int.TryParse(txbLineStart.Text, out int start) && int.TryParse(txbLineEnd.Text, out int end) && start < end && start >= 0 && end < 272) {
				raster.minLine = start;
				raster.maxLine = end;
				SaveFileDialog dlg = new SaveFileDialog { Filter = "Fichiers xml (*.xml)|*.xml" };
				if (dlg.ShowDialog() == DialogResult.OK) {
					FileStream file = File.Open(dlg.FileName, FileMode.Create);
					try {
						new XmlSerializer(typeof(Raster)).Serialize(file, raster);
					}
					catch {
						MessageBox.Show("Erreur sauvegarde rasters...");
					}
					file.Close();
				}
			}
		}

		private void BpScrollUp_Click(object sender, EventArgs e) {
			int.TryParse(txbScroll.Text, out int scrLine);
			if (scrLine > 0 && scrLine < 272) {
				for (int i = scrLine; i < 272; i++)
					raster.line[i - scrLine] = raster.line[i];

				for (int i = 272 - scrLine; i < 272; i++)
					raster.line[i] = 0;
			}
			DrawLines();
		}

		private void BpScrollDown_Click(object sender, EventArgs e) {
			int.TryParse(txbScroll.Text, out int scrLine);
			if (scrLine > 0 && scrLine < 272) {
				for (int i = 271; i >= scrLine; i--)
					raster.line[i] = raster.line[i - scrLine];

				for (int i = 0; i < scrLine; i++)
					raster.line[i] = 0;
			}
			DrawLines();
		}

		private void BpImportKit_Click(object sender, EventArgs e) {
			if (int.TryParse(txbStart.Text, out newStart) && newStart + 16 < 272) {
				Enabled = false;
				OpenFileDialog of = new OpenFileDialog { Filter = "Fichiers palette (*.kit)|*.kit" };
				if (of.ShowDialog() == DialogResult.OK) {
					FileStream fileScr = new FileStream(of.FileName, FileMode.Open, FileAccess.Read);
					byte[] entete = new byte[0x80];
					byte[] tabBytes = new byte[fileScr.Length - 0x80];
					fileScr.Read(entete, 0, entete.Length);
					fileScr.Read(tabBytes, 0, tabBytes.Length);
					fileScr.Close();
					if (Cpc.CheckAmsdos(entete)) {
						if ((tabBytes.Length == 30 || tabBytes.Length == 32)) {
							int start = 0;
							for (int i = tabBytes.Length == 32 ? 0 : 1; i < 16; i++) {
								int kit = tabBytes[start] + (tabBytes[start + 1] << 8);
								raster.line[newStart + i] = kit & 0xFFF;
								start += 2;
							}
						}
					}
				}
				DrawLines();
				Enabled = true;
			}
		}

		private void BpGenerate_Click(object sender, EventArgs e) {
			if (int.TryParse(txbLineStart.Text, out int start) && int.TryParse(txbLineEnd.Text, out int end) && start < end && start >= 0 && end < 272) {
				raster.minLine = start;
				raster.maxLine = end;
				SaveFileDialog dlg = new SaveFileDialog { Filter = "Assembler file (.asm)|*.asm" };
				if (dlg.ShowDialog() == DialogResult.OK) {
					StreamWriter sw = SaveAsm.OpenAsm(dlg.FileName);
					SaveAsm.WriteDatas(sw, raster.line, start, end, 16);
					SaveAsm.CloseAsm(sw);
				}
			}
		}

		private void BpGenFade_Click(object sender, EventArgs e) {
			if (int.TryParse(txbLineStart.Text, out int start) && int.TryParse(txbLineEnd.Text, out int end) && start < end && start >= 0 && end < 272) {
				raster.minLine = start;
				raster.maxLine = end;
				SaveFileDialog dlg = new SaveFileDialog { Filter = "Assembler file (.asm)|*.asm" };
				if (dlg.ShowDialog() == DialogResult.OK) {
					int[][] tabFade = new int[16][];
					for (int i = 0; i < 16; i++)
						tabFade[i] = new int[272];

					int maxR = 0, maxV = 0, maxB = 0;
					for (int c = start; c <= end; c++) {
						tabFade[0][c] = raster.line[c];
						maxR = Math.Max(maxR, (tabFade[0][c] & 0xF0) >> 4);
						maxV = Math.Max(maxV, tabFade[0][c] >> 8);
						maxB = Math.Max(maxB, tabFade[0][c] & 0x0F);
					}
					StreamWriter sw = SaveAsm.OpenAsm(dlg.FileName);
					for (int i = 0; i < 15; i++) {
						for (int c = start; c <= end; c++) {
							int r = ((tabFade[i][c] & 0xF0) >> 4) * maxR / 16;
							int v = ((tabFade[i][c]) >> 8) * maxV / 16;
							int b = (tabFade[i][c] & 0x0F) * maxB / 16;
							tabFade[i + 1][c] = (r << 4) + (v << 8) + b;
						}
					}
					for (int i = 15; i >= 0; i--) {
						sw.WriteLine("Fade_" + (15 - i).ToString());
						SaveAsm.WriteDatas(sw, tabFade[i], start, end, 16);
					}
					SaveAsm.CloseAsm(sw);
				}
			}
		}

		private void RasterTablePlus_FormClosed(object sender, FormClosedEventArgs e) {
			main.rasterPlus = null;
		}

		private void ChkSprites_CheckedChanged(object sender, EventArgs e) {
			DrawLines();
		}
	}

	[Serializable]
	public class Raster {
		public int[] line = new int[272];
		public int minLine = 0, maxLine = 0;
	}
}
