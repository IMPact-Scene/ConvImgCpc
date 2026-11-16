using System.Drawing;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class SelectColor : Form {
		public int selColor = -1;

		public SelectColor() {
			InitializeComponent();
			int xStart = (Width / 2) - 24 * Cpc.MaxPen(0);
			for (int i = 0; i < Cpc.MaxPen(0); i++) {
				int col = Cpc.Palette[i];
				int r = (col & 0x0F) * 17;
				int v = ((col & 0xF00) >> 8) * 17;
				int b = ((col & 0xF0) >> 4) * 17;
				Label l = new Label {
					BorderStyle = BorderStyle.FixedSingle,
					Location = new Point(xStart + i * 48, 48),
					Size = new Size(40, 32),
					Tag = i,
					BackColor = Color.FromArgb(r, v, b)
				};
				l.MouseClick += Color_MouseClick;
				Controls.Add(l);
			}
			Application.DoEvents();
		}

		private void Color_MouseClick(object sender, MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				Label colorClick = sender as Label;
				selColor = colorClick.Tag != null ? (int)colorClick.Tag : 0;
				Close();
			}
		}
	}
}
