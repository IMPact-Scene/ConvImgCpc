using System.Drawing;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class EditColor : Form {
		private readonly Label[] colors = new Label[27];
		private readonly Label lblValColor = new Label();
		private readonly TextBox[] tabVal = new TextBox[3];
		private readonly TrackBar[] tabTrack = new TrackBar[3];
		private readonly Label[] tabLabel = new Label[3];
		private readonly int numColor;
		private int valColor;
		public int ValColor { get { return valColor; } }
		public bool isValide;

		public EditColor(Main m, int numC, int val, int rgbColor, bool cpcPlus) {
			InitializeComponent();
			selColor.BackColor = Color.FromArgb(rgbColor);
			m.ChangeLanguage(Controls, "EditColor");
			numColor = numC;
			lblNumColor.Text += numColor;
			if (cpcPlus) {
				for (int i = 0; i < 3; i++) {
					tabLabel[i] = new Label();
					tabVal[i] = new TextBox();
					tabTrack[i] = new TrackBar();
					tabLabel[i].Location = new Point(277, 8 + 40 * i);
					tabVal[i].Location = new Point(298, 5 + 40 * i);
					tabTrack[i].Location = new Point(331, 5 + 40 * i);
					tabLabel[i].AutoSize = true;
					tabVal[i].Size = new Size(27, 20);
					tabTrack[i].Size = new Size(104, 42);
					tabVal[i].TextChanged += Val_TextChanged;
					tabTrack[i].Scroll += Track_Scroll;
					tabTrack[i].Maximum = 15;
					tabVal[i].Tag = tabTrack[i].Tag = i;
					tabLabel[i].Text = "VRB".Substring(i, 1);
					switch (i) {
						case 0:
							tabVal[i].Text = (Color.FromArgb(rgbColor).G / 17).ToString();
							break;

						case 1:
							tabVal[i].Text = (Color.FromArgb(rgbColor).R / 17).ToString();
							break;

						case 2:
							tabVal[i].Text = (Color.FromArgb(rgbColor).B / 17).ToString();
							break;
					}
					tabVal[i].MaxLength = 2;
					Controls.Add(tabLabel[i]);
					Controls.Add(tabTrack[i]);
					Controls.Add(tabVal[i]);
				}
			}
			else {
				valColor = val;
				int i = 0;
				for (int y = 0; y < 3; y++)
					for (int x = 0; x < 9; x++) {
						colors[i] = new Label {
							BorderStyle = BorderStyle.FixedSingle,
							Location = new Point(4 + x * 48, 80 + y * 40),
							Size = new Size(40, 32),
							Tag = i,
							BackColor = Color.FromArgb(Cpc.RgbCPC[i].GetColorArgb)
						};
						colors[i].Click += ClickColor;
						colors[i].DoubleClick += DblClickColor;
						Controls.Add(colors[i++]);
					}
				lblValColor.AutoSize = true;
				lblValColor.Location = new Point(271, 34);
				lblValColor.Text = "=" + val;
				Controls.Add(lblValColor);
			}
		}

		private void ClickColor(object sender, System.EventArgs e) {
			Label colorClick = sender as Label;
			valColor = colorClick.Tag != null ? (int)colorClick.Tag : 0;
			lblValColor.Text = "=" + valColor;
			selColor.BackColor = colorClick.BackColor;
		}

		private void DblClickColor(object sender, System.EventArgs e) {
			ClickColor(sender, e);
			BpValide_Click(sender, e);
		}

		private int GetCompValue(string txt) {
			if (!int.TryParse(txt, out int v))
				try {
					v = int.Parse(txt, System.Globalization.NumberStyles.HexNumber);
				}
				catch { }
			return v;
		}

		private void NewColor() {
			try {
				int v = tabVal[0] != null ? GetCompValue(tabVal[0].Text) : 0;
				int r = tabVal[1] != null ? GetCompValue(tabVal[1].Text) : 0;
				int b = tabVal[2] != null ? GetCompValue(tabVal[2].Text) : 0;
				valColor = (v << 8) + (b << 4) + r;
				selColor.BackColor = Color.FromArgb(r * 17, v * 17, b * 17);
			}
			catch { }
		}

		private void Track_Scroll(object sender, System.EventArgs e) {
			int i = (int)((TrackBar)sender).Tag;
			tabVal[i].Text = tabTrack[i].Value.ToString();
		}

		private void Val_TextChanged(object sender, System.EventArgs e) {
			try {
				int i = (int)((TextBox)sender).Tag;
				tabTrack[i].Value = GetCompValue(tabVal[i].Text);
				NewColor();
			}
			catch { }
		}

		private void BpValide_Click(object sender, System.EventArgs e) {
			isValide = true;
			Close();
		}

		private void BpAnnule_Click(object sender, System.EventArgs e) {
			Close();
		}
	}
}
