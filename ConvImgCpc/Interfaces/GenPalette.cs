using System;
using System.Drawing;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class GenPalette : Form {
		private readonly int[] palette;
		private readonly int minStart;
		private readonly Action FctToDo = null;
		private readonly bool modeRaster = false;
		public bool done = false;
		public int start = 0, end = 0;

		public GenPalette(int[] p, int ms, Action a) {
			InitializeComponent();
			palette = p;
			minStart = ms;
			txbFrom.Text = minStart.ToString();
			int cs = palette[minStart];
			txbStartR.Text = (cs & 0x0F).ToString();
			txbStartV.Text = (cs >> 8).ToString();
			txbStartB.Text = ((cs & 0xF0) >> 4).ToString();
			int ce = palette[15];
			txbEndR.Text = (ce & 0x0F).ToString();
			txbEndV.Text = (ce >> 8).ToString();
			txbEndB.Text = ((ce & 0xF0) >> 4).ToString();
			FctToDo = a;
			lblError.Text = "";
		}

		public GenPalette(ref int[] p, int ms, int me, RvbColor oldCol) {
			InitializeComponent();
			palette = p;
			txbFrom.Text = ms.ToString();
			txbTo.Text = me.ToString();
			txbStartR.Text = (oldCol.r & 0x0F).ToString();
			txbStartV.Text = (oldCol.v >> 4).ToString();
			txbStartB.Text = ((oldCol.b & 0xF0) >> 4).ToString();
			modeRaster = true;
		}

		public static void SetCompValue(string lblComp, ref TrackBar modComp, TrackBar r, TrackBar v, TrackBar b, Label lblColor) {
			if (int.TryParse(lblComp, out int i) && i >= 0 && i <= 15) {
				modComp.Value = i;
				lblColor.BackColor = Color.FromArgb(r.Value * 17, v.Value * 17, b.Value * 17);
			}
		}

		private void TrkStartR_Scroll(object sender, EventArgs e) {
			txbStartR.Text = trkStartR.Value.ToString();
			CalcPalette();
		}

		private void TrkStartV_Scroll(object sender, EventArgs e) {
			txbStartV.Text = trkStartV.Value.ToString();
			CalcPalette();
		}

		private void TrkStartB_Scroll(object sender, EventArgs e) {
			txbStartB.Text = trkStartB.Value.ToString();
			CalcPalette();
		}

		private void TxbStartR_TextChanged(object sender, EventArgs e) {
			if (int.TryParse(txbStartR.Text, out int i) && i >= 0 && i <= 15) {
				trkStartR.Value = i;
				lblStartColor.BackColor = Color.FromArgb(trkStartR.Value * 17, trkStartV.Value * 17, trkStartB.Value * 17);
			}
		}

		private void TxbStartV_TextChanged(object sender, EventArgs e) {
			if (int.TryParse(txbStartV.Text, out int i) && i >= 0 && i <= 15) {
				trkStartV.Value = i;
				lblStartColor.BackColor = Color.FromArgb(trkStartR.Value * 17, trkStartV.Value * 17, trkStartB.Value * 17);
			}
		}

		private void TxbStartB_TextChanged(object sender, EventArgs e) {
			if (int.TryParse(txbStartB.Text, out int i) && i >= 0 && i <= 15) {
				trkStartB.Value = i;
				lblStartColor.BackColor = Color.FromArgb(trkStartR.Value * 17, trkStartV.Value * 17, trkStartB.Value * 17);
			}
		}

		private void TrkEndR_Scroll(object sender, EventArgs e) {
			txbEndR.Text = trkEndR.Value.ToString();
			CalcPalette();
		}

		private void TrkEndV_Scroll(object sender, EventArgs e) {
			txbEndV.Text = trkEndV.Value.ToString();
			CalcPalette();
		}

		private void TrkEndB_Scroll(object sender, EventArgs e) {
			txbEndB.Text = trkEndB.Value.ToString();
			CalcPalette();
		}

		private void TxbEndR_TextChanged(object sender, EventArgs e) {
			if (int.TryParse(txbEndR.Text, out int i) && i >= 0 && i <= 15) {
				trkEndR.Value = i;
				lblEndColor.BackColor = Color.FromArgb(trkEndR.Value * 17, trkEndV.Value * 17, trkEndB.Value * 17);
			}
		}

		private void TxbEndV_TextChanged(object sender, EventArgs e) {
			if (int.TryParse(txbEndV.Text, out int i) && i >= 0 && i <= 15) {
				trkEndV.Value = i;
				lblEndColor.BackColor = Color.FromArgb(trkEndR.Value * 17, trkEndV.Value * 17, trkEndB.Value * 17);
			}
		}

		private void TxbEndB_TextChanged(object sender, EventArgs e) {
			if (int.TryParse(txbEndB.Text, out int i) && i >= 0 && i <= 15) {
				trkEndB.Value = i;
				lblEndColor.BackColor = Color.FromArgb(trkEndR.Value * 17, trkEndV.Value * 17, trkEndB.Value * 17);
			}
		}

		private void BpGenerate_Click(object sender, EventArgs e) {
			CalcPalette();

			if (FctToDo == null)
				Close();

			done = true;
		}

		private void CalcPalette() {
			lblError.Text = "";
			if (modeRaster)
				CalcPaletteRaster();
			else
				CalcPaletteStd();

			// Vérifier pas 2 fois la même couleur
			for (int i = 0; i < 16; i++)
				for (int j = i + 1; j < 16; j++)
					if (palette[i] == palette[j]) {
						lblError.Text = "Color " + i.ToString() + " is the same of color " + j.ToString();
						break;
					}
		}

		private void CalcPaletteStd() {
			double rs = 0, vs = 0, bs = 0, re = 0, ve = 0, be = 0;
			if (int.TryParse(txbFrom.Text, out start) && int.TryParse(txbTo.Text, out end) && start >= minStart && start < end && end < 272) {
				if (double.TryParse(txbStartR.Text, out rs) && double.TryParse(txbStartV.Text, out vs) && double.TryParse(txbStartB.Text, out bs)
					&& double.TryParse(txbEndR.Text, out re) && double.TryParse(txbEndV.Text, out ve) && double.TryParse(txbEndB.Text, out be)) {
					if (rs >= 0 && vs >= 0 && bs >= 0 && re >= 0 && ve >= 0 && be >= 0 && rs < 16 && vs < 16 && bs < 16 && re < 16 && ve < 16 && be < 16) {
						rs *= 17;
						vs *= 17;
						bs *= 17;
						re *= 17;
						ve *= 17;
						be *= 17;
						double kr = (re - rs) / (end - start);
						double kv = (ve - vs) / (end - start);
						double kb = (be - bs) / (end - start);
						for (int i = start; i < end; i++) {
							palette[i] = (int)((rs / 17) + ((int)(bs / 17) << 4) + ((int)(vs / 17) << 8));
							rs += kr;
							bs += kb;
							vs += kv;
						}
						palette[end] = (int)((re / 17) + ((int)(be / 17) << 4) + ((int)(ve / 17) << 8));
					}
				}
			}
			FctToDo?.Invoke();
		}

		private void CalcPaletteRaster() {
			if (int.TryParse(txbFrom.Text, out start) && int.TryParse(txbTo.Text, out end) && start >= 0 && start < 272 && end >= 0 && end < 272 && start != end) {
				if (double.TryParse(txbStartR.Text, out double rs) && double.TryParse(txbStartV.Text, out double vs) && double.TryParse(txbStartB.Text, out double bs)
					&& double.TryParse(txbEndR.Text, out double re) && double.TryParse(txbEndV.Text, out double ve) && double.TryParse(txbEndB.Text, out double be)) {
					if (rs >= 0 && vs >= 0 && bs >= 0 && re >= 0 && ve >= 0 && be >= 0 && rs < 16 && vs < 16 && bs < 16 && re < 16 && ve < 16 && be < 16) {
						int sens = start < end ? 1 : -1;
						double kr = (re - rs) / (end - start) * sens;
						double kv = (ve - vs) / (end - start) * sens;
						double kb = (be - bs) / (end - start) * sens;
						for (int i = start; i != end + sens; i += sens) {
							palette[i] = ((int)(bs) + ((int)(rs) << 4) + ((int)(vs) << 8));
							rs += kr;
							bs += kb;
							vs += kv;
						}
						palette[end] = ((int)(be) + ((int)(re) << 4) + ((int)(ve) << 8));
						done = true;
					}
				}
			}
		}

		private void BpGetCol_Click(object sender, EventArgs e) {
			if (int.TryParse(txbFrom.Text, out int start)) {
				int cs = palette[start];
				txbStartR.Text = (cs & 0x0F).ToString();
				txbStartV.Text = (cs >> 8).ToString();
				txbStartB.Text = ((cs & 0xF0) >> 4).ToString();
			}
		}
	}
}
