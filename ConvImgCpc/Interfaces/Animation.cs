using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class Animation : Form {
		private readonly Main main;
		private bool displaySrc = true;
		public int SelImage { get { return (int)numImage.Value; } }
		public int MaxImage { get { return (int)numImage.Maximum; } }
		private int[] tempsAffiche;
		private bool lockTps = false;
		private int tpsStart;

		public Animation(Main m) {
			main = m;
			InitializeComponent();
		}

		public void SetNbImgs(int nbImg, int tps) {
			lblMaxImage.Text = main.multilingue.GetString("Animation.lblMaxImage") + nbImg;
			lblMaxImage.Visible = lblNumImage.Visible = numImage.Visible = bpDel1_2.Visible = nbImg > 1;

			numImage.Maximum = nbImg - 1;
			numImage.Value = hScrollBar1.Value = 0;
			Text = nbImg > 1 ? "Animation" : "Image";
			tempsAffiche = new int[nbImg];
			for (int i = 0; i < nbImg; i++)
				tempsAffiche[i] = tps;

			bpPlay.Enabled = nbImg > 1;
		}

		public void DrawImages(int startImg) {
			lockTps = true;
			PictureBox[] tabPb = new PictureBox[] { pictureBox1, pictureBox2, pictureBox3, pictureBox4, pictureBox5 };
			Button[] tabButton = new Button[] { bpSup1, bpSup2, bpSup3, bpSup4, bpSup5 };
			TextBox[] tabTxt = new TextBox[] { txbTps1, txbTps2, txbTps3, txbTps4, txbTps5 };
			int endImg = Math.Min(startImg + 4, main.imgSrc.NbImg - 1);
			for (int i = startImg; i <= endImg; i++) {
				tabPb[i - startImg].Image = displaySrc ? main.imgSrc.GetBitmap(i) : main.imgCpc.tabBmpLock[i].Bitmap;
				tabPb[i - startImg].Refresh();
				tabButton[i - startImg].Visible = tabTxt[i - startImg].Visible = startImg + i > 0 || endImg - startImg > 2;
				tabTxt[i - startImg].Text = main.imgCpc.tabBmpLock[i].Tps.ToString();
			}
			for (int i = endImg - startImg + 1; i < 5; i++) {
				if (i > 0) {
					tabPb[i].Image = null;
					tabPb[i].Refresh();
					tabButton[i].Visible = false;
					tabTxt[i].Visible = false;
				}
			}
			hScrollBar1.Visible = numImage.Maximum >= 5;
			hScrollBar1.Maximum = (int)(numImage.Maximum);
			lockTps = false;
		}

		private void NumImage_ValueChanged(object sender, EventArgs e) {
			main.SelectImage((int)numImage.Value);
			hScrollBar1.Value = (int)numImage.Value;
			main.imgCpc.SetImgCopy();
			main.Convert(false);
			//		main.imgCpc.Render(true);
		}

		private void HScrollBar1_Scroll(object sender, ScrollEventArgs e) {
			numImage.Value = hScrollBar1.Value;
		}

		private void BpSup_Click(object sender, EventArgs e) {
			int num = (int)numImage.Value;
			int index = Convert.ToInt32(((Button)sender).Tag) + num;
			main.imgSrc.DeleteImage(index);
			numImage.Maximum = main.imgSrc.NbImg - 1;
			main.SetInfo("Suppression image " + index);
			lblMaxImage.Text = main.multilingue.GetString("Animation.lblMaxImage") + main.imgSrc.NbImg;
			main.SelectImage(num > main.imgSrc.NbImg - 1 ? main.imgSrc.NbImg - 1 : num);
		}

		private void PictureBox_Click(object sender, EventArgs e) {
			int num = (int)numImage.Value;
			int index = Convert.ToInt32(((PictureBox)sender).Tag) + num;
			if (index <= numImage.Maximum)
				numImage.Value = index;
		}

		private void RbSource_CheckedChanged(object sender, EventArgs e) {
			bpSaveGif.Visible = false;
			displaySrc = true;
			DrawImages(main.imgCpc.selImage);
		}

		private void RvCalculee_CheckedChanged(object sender, EventArgs e) {
			bpSaveGif.Visible = numImage.Maximum > 0;
			displaySrc = false;
			DrawImages(main.imgCpc.selImage);
		}

		private void BpSaveGif_Click(object sender, EventArgs e) {
			try {
				SaveFileDialog dlg = new SaveFileDialog { Filter = "Gif anim (*.gif)|*.gif" };
				if (dlg.ShowDialog() == DialogResult.OK) {
					byte[] GifAnimation = { 33, 255, 11, 78, 69, 84, 83, 67, 65, 80, 69, 50, 46, 48, 3, 1, 0, 0, 0 };
					MemoryStream ms = new MemoryStream();
					FileStream s = new FileStream(dlg.FileName, FileMode.Create);
					BinaryWriter bWr = new BinaryWriter(s);
					Bitmap b = GetBitmap(main.imgCpc.tabBmpLock[0].Bitmap);
					b.Save(ms, ImageFormat.Gif);
					b.Dispose();
					byte[] tabByte = ms.ToArray();
					tabByte[10] = (byte)(tabByte[10] & 0X78); //No global color table
					bWr.Write(tabByte, 0, 13);
					bWr.Write(GifAnimation);
					WriteGifImg(tabByte, bWr);
					for (int i = 1; i <= numImage.Maximum; i++) {
						ms.SetLength(0);
						b = GetBitmap(main.imgCpc.tabBmpLock[i].Bitmap);
						b.Save(ms, ImageFormat.Gif);
						tabByte = ms.ToArray();
						WriteGifImg(tabByte, bWr);
						b.Dispose();
					}
					bWr.Write(tabByte[tabByte.Length - 1]);
					bWr.Close();
					ms.Dispose();
					s.Close();
				}
			}
			catch (Exception ex) {
				main.DisplayErreur("Erreur lors de la sauvegarde du gif.");
				MessageBox.Show(ex.Message, ex.StackTrace);
			}
		}

		private Bitmap GetBitmap(Bitmap image) {
			Bitmap bitmap = new Bitmap(Cpc.TailleX, Cpc.TailleY, PixelFormat.Format24bppRgb);
			Graphics g = Graphics.FromImage(bitmap);
			g.DrawImage(image, new Rectangle(0, 0, bitmap.Width, bitmap.Height), new Rectangle(0, 0, bitmap.Width, bitmap.Height), GraphicsUnit.Pixel);
			g.Dispose();
			return bitmap;
		}

		private void WriteGifImg(byte[] tabByte, BinaryWriter bWr) {
			tabByte[785] = (byte)(tempsAffiche[0] / 20);
			tabByte[786] = 0;
			tabByte[798] = (byte)(tabByte[798] | 0X87);
			bWr.Write(tabByte, 781, 18);
			bWr.Write(tabByte, 13, 768);
			bWr.Write(tabByte, 799, tabByte.Length - 800);
		}

		private void TxbTps_TextChanged(object sender, EventArgs e) {
			if (!lockTps) {
				TextBox t = (TextBox)sender;
				if (int.TryParse(t.Text, out int v) && v > 0 && v <= 5000) {
					int num = (int)numImage.Value;
					int index = Convert.ToInt32(t.Tag) + num;
					if (index >= 0 && index < main.imgCpc.tabBmpLock.Length)
						main.imgCpc.tabBmpLock[index].Tps = v;
				}
			}
		}

		private void BpPlay_Click(object sender, EventArgs e) {
			main.Enabled = main.imgCpc.Enabled = false;
			bpPlay.Enabled = false;
			bpStop.Enabled = timer1.Enabled = true;
			tpsStart = Environment.TickCount;
			numImage.Value = 0;
		}

		private void BpStop_Click(object sender, EventArgs e) {
			bpPlay.Enabled = true;
			bpStop.Enabled = timer1.Enabled = false;
			main.Enabled = main.imgCpc.Enabled = true;
		}

		private void BpDel1_2_Click(object sender, EventArgs e) {
			for (int i = main.imgSrc.NbImg - 1; i > 0; i -= 2) {
				main.imgSrc.DeleteImage(i);
				numImage.Maximum = main.imgSrc.NbImg - 1;
				main.SetInfo("Suppression image " + i);
			}
			lblMaxImage.Text = main.multilingue.GetString("Animation.lblMaxImage") + main.imgSrc.NbImg;
			main.SelectImage(0);
			lblMaxImage.Visible = lblNumImage.Visible = numImage.Visible = bpDel1_2.Visible = bpPlay.Enabled = main.imgSrc.NbImg > 1;
			numImage.Value = 0;
		}

		private void Animation_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
		}

		private void Timer1_Tick(object sender, EventArgs e) {
			int i = (int)numImage.Value;
			int tpsWait = tempsAffiche[i];
			if (Environment.TickCount - tpsStart > tpsWait) {
				tpsStart = Environment.TickCount;
				if (++i > (int)numImage.Maximum)
					i = 0;

				numImage.Value = i;
			}
		}
	}
}
