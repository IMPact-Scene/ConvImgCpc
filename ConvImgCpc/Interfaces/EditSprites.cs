using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;



namespace ConvImgCpc {
	public partial class EditSprites : Form {
		private int numSprite = 0, numBank = 0, selSprite = -1;
		private byte penLeft = 1;
		private readonly DirectBitmap bmpSprite, bmpAllSprites, bmpTest;
		private readonly Main main;
		private readonly Label[] lblColors = new Label[16];
		private readonly Label[] lblUsedColors = new Label[16];
		private readonly Main.PackMethode pkMethod;
		private readonly byte[,] tempSprite = new byte[256, 16];
		private readonly Label lblRectSelColor = new Label();
		private readonly Label lblRectSelSprite = new Label();
		private readonly int[,] posSprites = new int[16, 2];
		private readonly bool[] spriteEnableTest = new bool[16];
		private int tickTimer;
		private int lineStartX = -1, lineStartY = -1;
		private int oldPosx = -1, oldPosy = -1;

		const int WM_LBUTTONUP = 0x0202;
		const int PM_REMOVE = 0x0001;

		[StructLayout(LayoutKind.Sequential)]
		public struct NativeMessage {
			public IntPtr handle;
			public uint msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public Point p;
		}

		[DllImport("user32.dll")]
		public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr window, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);



		public EditSprites(Main m, Main.PackMethode pk) {
			InitializeComponent();
			main = m;
			pkMethod = pk;
			main.ChangeLanguage(Controls, "EditSprites");
			bool paletteSpriteOk = false;
			lblRectSelColor.Size = new Size(48, 40);
			lblRectSelColor.BackColor = Color.Black;
			lblRectSelColor.Location = new Point(1200, 1200);
			Controls.Add(lblRectSelColor);
			lblRectSelSprite.Size = new Size(64, 4);
			Controls.Add(lblRectSelSprite);

			for (int c = 0; c < 16; c++)
				if (Cpc.paletteSprite[c] != 0)
					paletteSpriteOk = true;

			for (int c = 0; c < 16; c++) {
				// Générer les contrôles de "couleurs"
				lblColors[c] = new Label {
					BorderStyle = BorderStyle.FixedSingle,
					Location = new Point(730, 74 + c * 40),
					Size = new Size(40, 32),
					Tag = c,
					TextAlign = ContentAlignment.MiddleCenter,
					Text = c.ToString()
				};
				lblColors[c].MouseDown += ClickColor;
				if (!paletteSpriteOk)
					Cpc.paletteSprite[c] = Cpc.Palette[c];

				SetLblColor(c, Cpc.paletteSprite[c]);
				Controls.Add(lblColors[c]);
				lblColors[c].BringToFront();

				lblUsedColors[c] = new Label {
					Location = new Point(730, 74 + c * 40),
					Size = new Size(8, 8)
				};
				Controls.Add(lblUsedColors[c]);
				lblUsedColors[c].BringToFront();
			}

			bmpSprite = new DirectBitmap(pictEditSprite.Width, pictEditSprite.Height);
			pictEditSprite.Image = bmpSprite.Bitmap;
			bmpAllSprites = new DirectBitmap(pictAllSprites.Width, pictAllSprites.Height);
			pictAllSprites.Image = bmpAllSprites.Bitmap;
			bmpTest = new DirectBitmap(pictTest.Width, pictTest.Height);
			pictTest.Image = bmpTest.Bitmap;
			comboBanque.SelectedIndex = 0;
			DrawPens();
			DrawSprite();
		}

		private void SetLblColor(int i, int col) {
			byte r = (byte)((col & 0x0F) * 17);
			byte v = (byte)(((col & 0xF00) >> 8) * 17);
			byte b = (byte)(((col & 0xF0) >> 4) * 17);
			lblColors[i].BackColor = Color.FromArgb(r, v, b);
			lblColors[i].ForeColor = (r * 9798 + v * 19235 + b * 3735) > 0x400000 ? Color.Black : Color.White;
		}

		// Touches du clavier
		protected override bool ProcessCmdKey(ref Message msg, Keys keyData) {
			switch (keyData) {
				case Keys.Left:
					if (bpPrev.Visible)
						BpPrev_Click(null, null);
					break;

				case Keys.Right:
					if (bpSuiv.Visible)
						BpSuiv_Click(null, null);
					break;
			}
			return base.ProcessCmdKey(ref msg, keyData);
		}

		// Changement de la palette
		private void ClickColor(object sender, MouseEventArgs e) {
			Label colorClick = sender as Label;
			int pen = colorClick.Tag != null ? (int)colorClick.Tag : 0;
			if (e.Button == MouseButtons.Right) {
				int col = Cpc.paletteSprite[pen];
				RvbColor colRvb = Cpc.GetColor(col);
				EditColor ed = new EditColor(main, pen, col, colRvb.GetColor, true);
				ed.ShowDialog(this);
				if (ed.isValide) {
					Cpc.paletteSprite[pen] = ed.ValColor;
					SetLblColor(pen, ed.ValColor);
					lblColors[pen].Refresh();
					DrawMatrice();
				}
			}
			else {
				penLeft = (byte)pen;
				DrawPens();
			}
		}

		private void RefreshUsedColor() {
			byte[] col = new byte[16];
			for (int i = 0; i < 16; i++)
				col[i] = 0;

			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					col[Cpc.spritesHard[numBank, numSprite, x, y] & 0x0F] = 1;

			for (int i = 0; i < 16; i++)
				lblUsedColors[i].BackColor = col[i] == 0 ? Color.Red : Color.Green;
		}

		private void SetBmpSprite(int x, int y, int pixCol) {
			pixCol &= 0x0F;
			RvbColor c = Cpc.GetColor(Cpc.paletteSprite[pixCol]);
			RvbColor damier = Cpc.GetColor(Cpc.paletteSprite[0]);
			for (int zx = 0; zx < 38; zx++) {
				//					damier.r ^= 255;
				//					damier.v ^= 255;
				//					damier.b ^= 255;
				for (int zy = 0; zy < 38; zy++) {
					//				damier.r ^= 255;
					//				damier.v ^= 255;
					//				damier.b ^= 255;
					bmpSprite.SetPixel(zx + (x * 40), zy + (y * 40), pixCol == 0 ? damier : c);
				}
			}
		}

		private void DrawMatrice() {
			for (int spr = 0; spr < 16; spr++) {
				for (int y = 0; y < 16; y++) {
					for (int x = 0; x < 16; x++) {
						int pixCol = Cpc.spritesHard[numBank, spr, x, y] & 0x0F;
						RvbColor c = Cpc.GetColor(Cpc.paletteSprite[pixCol]);
						for (int zx = 0; zx < (x == 15 ? 3 : 4); zx++)
							for (int zy = 0; zy < 4; zy++)
								bmpAllSprites.SetPixel(zx + ((x + (spr << 4)) << 2), zy + (y << 2), c);

						if (spr == numSprite)
							SetBmpSprite(x, y, pixCol);
					}
				}
			}
			RefreshUsedColor();
			pictAllSprites.Refresh();
			pictEditSprite.Refresh();
		}

		private void DrawSprite() {
			lblSelSprite.Text = "Sprite n° : " + numSprite.ToString();
			lblRectSelSprite.Location = new Point(3 + 64 * numSprite, 63);
			bpPrev.Visible = numSprite > 0 || numBank > 0;
			bpSuiv.Visible = numSprite < 15 || numBank < 7;

			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					SetBmpSprite(x, y, Cpc.spritesHard[numBank, numSprite, x, y]);

			RefreshUsedColor();
			pictEditSprite.Refresh();
		}

		private void SetPixelSprite(int x, int y) {
			int pixCol = Cpc.spritesHard[numBank, numSprite, x, y] & 0x0F;
			RvbColor c = Cpc.GetColor(Cpc.paletteSprite[pixCol]);
			SetBmpSprite(x, y, pixCol);

			for (int zx = 0; zx < (x == 15 ? 3 : 4); zx++)
				for (int zy = 0; zy < 4; zy++)
					bmpAllSprites.SetPixel(zx + ((x + (numSprite << 4)) << 2), zy + (y << 2), c);

			RefreshUsedColor();
			pictEditSprite.Refresh();
			pictAllSprites.Refresh();
		}

		private void DrawPens() {
			int cl = Cpc.paletteSprite[penLeft];
			int cr = Cpc.paletteSprite[0];
			lblPenLeft.BackColor = Color.FromArgb((byte)((cl & 0x0F) * 17), (byte)(((cl & 0xF00) >> 8) * 17), (byte)(((cl & 0xF0) >> 4) * 17));
			lblPenRight.BackColor = Color.FromArgb((byte)((cr & 0x0F) * 17), (byte)(((cr & 0xF00) >> 8) * 17), (byte)(((cr & 0xF0) >> 4) * 17));
			lblColSelR.Text = "R:" + (cl & 0x0F).ToString();
			lblColSelV.Text = "V:" + (cl >> 8).ToString();
			lblColSelB.Text = "B:" + ((cl & 0xF0) >> 4).ToString();
		}

		private void DrawLine(int x1, int y1, int x2, int y2, bool drawSprite = false) {
			if (x2 < x1) {
				int a = x2;
				x2 = x1;
				x1 = a;
				a = y2;
				y2 = y1;
				y1 = a;
			}
			int dx = x2 - x1;
			if (dx == 0 && y1 > y2) {
				int a = y2;
				y2 = y1;
				y1 = a;
			}
			int dy = y2 - y1;
			int e;
			if (dx != 0) {
				if (dy != 0) {
					if (dy > 0) {
						if (dx >= dy) {
							e = dx;
							dx <<= 1;
							dy <<= 1;
							do {
								SetPixelSprite(x1, y1);
								Cpc.spritesHard[numBank, numSprite, x1, y1] = penLeft;
								e -= dy;
								if (e < 0) {
									y1++;
									e += dx;
								}
								;
							}
							while (x1++ != x2);
						}
						else {
							e = dy;
							dy <<= 1;
							dx <<= 1;
							do {
								SetPixelSprite(x1, y1);
								Cpc.spritesHard[numBank, numSprite, x1, y1] = penLeft;
								e -= dx;
								if (e < 0) {
									x1++;
									e += dy;
								}
							} while (y1++ != y2);
						}
					}
					else {
						if (dx >= -dy) {
							e = dx;
							dy <<= 1;
							dx <<= 1;
							do {
								SetPixelSprite(x1, y1);
								Cpc.spritesHard[numBank, numSprite, x1, y1] = penLeft;
								e += dy;
								if (e < 0) {
									y1--;
									e += dx;
								}
							} while (x1++ != x2);
						}
						else {
							e = dy;
							dy <<= 1;
							dx <<= 1;
							do {
								SetPixelSprite(x1, y1);
								Cpc.spritesHard[numBank, numSprite, x1, y1] = penLeft;
								e += dx;
								if (e > 0) {
									x1++;
									e += dy;
								}
							} while (y1-- != y2);
						}
					}
				}
				else {  // dy = 0 (et dx > 0)
					do {
						SetPixelSprite(x1, y1);
						Cpc.spritesHard[numBank, numSprite, x1, y1] = penLeft;
					}
					while (x1++ != x2);
				}
			}
			else {  // dx = 0
				if (dy != 0) {
					do {
						SetPixelSprite(x1, y1);
						Cpc.spritesHard[numBank, numSprite, x1, y1] = penLeft;
					}
					while (y1++ != y2);
				}
			}
			DrawSprite();
			if (drawSprite)
				DrawMatrice();
		}

		private int GetLastSprite(bool allBank = false) {
			int startBank = allBank ? 0 : numBank;
			int endBank = allBank ? 3 : numBank;
			for (int bank = endBank; bank >= startBank; bank--)
				for (int i = 15; i >= 0; i--)
					for (int y = 0; y < 16; y++)
						for (int x = 0; x < 16; x++)
							if (Cpc.spritesHard[bank, i, x, y] != 0)
								return i + 16 * bank;

			return 0;
		}

		private void DoGenPal() {
			for (int c = 0; c < 16; c++) {
				SetLblColor(c, Cpc.paletteSprite[c]);
				lblColors[c].Refresh();
			}
			DrawMatrice();
			DrawSprite();
		}

		private void DrawSpriteTest(DirectBitmap bmp, int spr, int ofsx, int ofsy) {
			int taillex = 1 << (int)zoomX.Value;
			int tailley = 2 << (int)zoomY.Value;
			for (int y = 0; y < 16; y++) {
				for (int x = 0; x < 16; x++) {
					for (int zx = 0; zx < taillex; zx++)
						for (int zy = 0; zy < tailley; zy++) {
							if (zx + (x * taillex) + ofsx < 512 && zy + (y * tailley) + ofsy < 512) {
								int col = Cpc.paletteSprite[Cpc.spritesHard[numBank, spr, x, y] & 0x0F];
								bmp.SetPixel(zx + (x * taillex) + ofsx, zy + (y * tailley) + ofsy, Cpc.GetColor(col));
							}
						}
				}
			}
		}

		private void PictAllMatrice_MouseDown(object sender, MouseEventArgs e) {
			int y = e.X / 64;
			if (y >= 0 && y < 16) {
				if (e.Button == MouseButtons.Left && y != numSprite) {
					numSprite = y;
					DrawSprite();
				}
				if (e.Button == MouseButtons.Right) {
					if (selSprite == -1) {
						selSprite = y;
						for (int i = 0; i < 64; i++) {
							bmpAllSprites.SetPixel(0 + (selSprite << 6), i, 0xFFFFFF);
							bmpAllSprites.SetPixel(63 + (selSprite << 6), i, 0xFFFFFF);
							bmpAllSprites.SetPixel(i + (selSprite << 6), 0, 0xFFFFFF);
							bmpAllSprites.SetPixel(i + (selSprite << 6), 63, 0xFFFFFF);
						}
					}
					else {
						for (int i = 0; i < 64; i++) {
							bmpAllSprites.SetPixel(0 + (selSprite << 6), i, 0);
							bmpAllSprites.SetPixel(63 + (selSprite << 6), i, 0);
							bmpAllSprites.SetPixel(i + (selSprite << 6), 0, 0);
							bmpAllSprites.SetPixel(i + (selSprite << 6), 63, 0);
						}
						int swapSprite = y;
						if (swapSprite != selSprite) {
							for (y = 0; y < 16; y++)
								for (int x = 0; x < 16; x++) {
									byte tmp = Cpc.spritesHard[numBank, selSprite, x, y];
									Cpc.spritesHard[numBank, selSprite, x, y] = Cpc.spritesHard[numBank, swapSprite, x, y];
									Cpc.spritesHard[numBank, swapSprite, x, y] = tmp;
								}
						}
						selSprite = -1;
						DrawMatrice();
					}
					pictAllSprites.Refresh();
				}
			}
		}

		private void PictEditMatrice_MouseMove(object sender, MouseEventArgs e) {
			int x = e.X / 40;
			int y = e.Y / 40;
			if (x >= 0 && y >= 0 && x < 16 && y < 16) {
				byte col = Cpc.spritesHard[numBank, numSprite, x, y];
				if (e.Button == MouseButtons.Left) {
					if (rbFill.Checked) {
						Stack<Point> pixels = new Stack<Point>();
						int old = Cpc.spritesHard[numBank, numSprite, x, y];
						if (old != penLeft) {
							pixels.Push(new Point(x, y));
							while (pixels.Count > 0) {
								Point a = pixels.Pop();
								if (a.X < 16 && a.X >= 0 && a.Y < 16 && a.Y >= 0 && Cpc.spritesHard[numBank, numSprite, a.X, a.Y] == old) {
									Cpc.spritesHard[numBank, numSprite, a.X, a.Y] = penLeft;
									SetPixelSprite(a.X, a.Y);
									pixels.Push(new Point(a.X - 1, a.Y));
									pixels.Push(new Point(a.X + 1, a.Y));
									pixels.Push(new Point(a.X, a.Y - 1));
									pixels.Push(new Point(a.X, a.Y + 1));
								}
							}
						}
					}
					else
						if (rbPt.Checked) {
						Cpc.spritesHard[numBank, numSprite, x, y] = penLeft;
						SetPixelSprite(x, y);
					}
					else
							if (lineStartX == -1 && lineStartY == -1) {
						lineStartX = x;
						lineStartY = y;
						for (x = 0; x < 16; x++)
							for (y = 0; y < 16; y++)
								tempSprite[x + 16 * y, 0] = Cpc.spritesHard[numBank, numSprite, x, y];
					}
					else {
						for (int x1 = 0; x1 < 16; x1++)
							for (int y1 = 0; y1 < 16; y1++)
								Cpc.spritesHard[numBank, numSprite, x1, y1] = tempSprite[x1 + 16 * y1, 0];

						DrawLine(lineStartX, lineStartY, x, y);
					}
				}
				else
					if (e.Button == MouseButtons.Right) {
					Cpc.spritesHard[numBank, numSprite, x, y] = 0;
					SetPixelSprite(x, y);
				}
				else
						if (lineStartX != -1 && lineStartY != -1) {
					for (int x1 = 0; x1 < 16; x1++)
						for (int y1 = 0; y1 < 16; y1++)
							Cpc.spritesHard[numBank, numSprite, x1, y1] = tempSprite[x1 + 16 * y1, 0];

					DrawLine(lineStartX, lineStartY, x, y, true);
					lineStartX = lineStartY = -1;
				}
				lblRectSelColor.Location = new Point(726, 70 + col * 40);       // Mise en évidence couleur sous la souris
			}
			else
				lblRectSelColor.Location = new Point(1200, 1200);
		}

		private void PictEditSprite_MouseLeave(object sender, EventArgs e) {
			lblRectSelColor.Location = new Point(1200, 1200);
		}

		private void BpPrev_Click(object sender, EventArgs e) {
			numSprite--;
			if (numSprite < 0) {
				numBank--;
				numSprite = 15;
				comboBanque.SelectedIndex = numBank;
			}
			DrawSprite();
		}

		private void BpSuiv_Click(object sender, EventArgs e) {
			numSprite++;
			if (numSprite > 15) {
				numBank++;
				numSprite = 0;
				comboBanque.SelectedIndex = numBank;
			}
			DrawSprite();
		}

		private void BpCopySprite_Click(object sender, EventArgs e) {
			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					tempSprite[x + y * 16, 0] = Cpc.spritesHard[numBank, numSprite, x, y];
		}

		private void BpPasteSprite_Click(object sender, EventArgs e) {
			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, x, y] = tempSprite[x + y * 16, 0];

			DrawMatrice();
			DrawSprite();
		}

		private void BpHorizontalFlip_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[256];
			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					tmp[x + y * 16] = Cpc.spritesHard[numBank, numSprite, x, y];

			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, x, 15 - y] = tmp[x + y * 16];

			DrawMatrice();
		}

		private void BpVerticalFlip_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[256];
			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					tmp[x + y * 16] = Cpc.spritesHard[numBank, numSprite, x, y];

			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, 15 - x, y] = tmp[x + y * 16];

			DrawMatrice();
		}

		private void BpRotateSprite_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[256];
			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					tmp[x + y * 16] = Cpc.spritesHard[numBank, numSprite, x, y];

			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, 15 - y, x] = tmp[x + y * 16];

			DrawMatrice();
		}

		private void BpClearSprite_Click(object sender, EventArgs e) {
			for (int y = 0; y < 16; y++)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, x, y] = 0;

			DrawMatrice();
		}

		private void BpClearBank_Click(object sender, EventArgs e) {
			for (int i = 0; i < 16; i++)
				for (int y = 0; y < 16; y++)
					for (int x = 0; x < 16; x++)
						Cpc.spritesHard[numBank, i, x, y] = 0;

			DrawMatrice();
		}

		private void ComboBanque_SelectedIndexChanged(object sender, EventArgs e) {
			numBank = comboBanque.SelectedIndex;
			DrawMatrice();
		}

		private void BpTest_Click(object sender, EventArgs e) {
			int nb = rb1Sprite.Checked ? 1 : rb2Sprite.Checked ? 2 : 4;
			int mask = rb1Sprite.Checked ? 0xF : rb2Sprite.Checked ? 0x0c : 0x00;
			int taillex = (1 << (int)zoomX.Value) << 4;
			int tailley = (2 << (int)zoomY.Value) << 4;
			int start = numSprite & mask;

			for (int y = 0; y < 512; y++)
				for (int x = 0; x < 512; x++)
					bmpTest.SetPixel(x, y, 0);

			if (rbPosSpr.Checked) {
				for (int s = 0; s < 16; s++)
					if (spriteEnableTest[s])
						DrawSpriteTest(bmpTest, s, posSprites[s, 0] * taillex, posSprites[s, 1] * tailley);
			}
			else
				if (rb28sprite.Checked) {
				for (int y = 0; y < 2; y++)
					for (int x = 0; x < 4; x++)
						DrawSpriteTest(bmpTest, start++, x * taillex, y * tailley);

				for (int y = 0; y < 2; y++)
					for (int x = 4; x < 8; x++)
						DrawSpriteTest(bmpTest, start++, x * taillex, y * tailley);
			}
			else
					if (rb42Sprite.Checked) {
				for (int y = 0; y < 4; y++)
					for (int x = 0; x < 2; x++)
						DrawSpriteTest(bmpTest, start++, x * taillex, y * tailley);

				for (int y = 0; y < 4; y++)
					for (int x = 2; x < 4; x++)
						DrawSpriteTest(bmpTest, start++, x * taillex, y * tailley);
			}
			else {
				for (int y = 0; y < nb; y++)
					for (int x = 0; x < nb; x++)
						DrawSpriteTest(bmpTest, start++, x * taillex, y * tailley);
			}
			pictTest.Refresh();
		}

		private void BpGenPal_Click(object sender, EventArgs e) {
			GenPalette g = new GenPalette(Cpc.paletteSprite, 1, DoGenPal);
			g.ShowDialog();
		}

		private void PictTest_MouseMove(object sender, MouseEventArgs e) {
			if (rbPosSpr.Checked) {
				int taillex = (1 << (int)zoomX.Value) << 4;
				int tailley = (2 << (int)zoomY.Value) << 4;
				int posx = (e.X / taillex) * taillex;
				int posy = (e.Y / tailley) * tailley;
				Graphics g = Graphics.FromImage(pictTest.Image);
				if (oldPosx > -1 && oldPosy > -1) {
					XorDrawing.DrawXorRectangle(g, (Bitmap)pictTest.Image, oldPosx, oldPosy, oldPosx + taillex, oldPosy + tailley);
				}
				oldPosx = posx;
				oldPosy = posy;
				XorDrawing.DrawXorRectangle(g, (Bitmap)pictTest.Image, oldPosx, oldPosy, oldPosx + taillex, oldPosy + tailley);
				pictTest.Refresh();
			}
		}

		private void PictTest_MouseLeave(object sender, EventArgs e) {
			if (rbPosSpr.Checked) {
				Graphics g = Graphics.FromImage(pictTest.Image);
				int taillex = (1 << (int)zoomX.Value) << 4;
				int tailley = (2 << (int)zoomY.Value) << 4;
				if (oldPosx > -1 && oldPosy > -1) {
					XorDrawing.DrawXorRectangle(g, (Bitmap)pictTest.Image, oldPosx, oldPosy, oldPosx + taillex, oldPosy + tailley);
					pictTest.Refresh();
					oldPosx = oldPosy = -1;
				}
			}
		}

		private void PictTest_MouseDown(object sender, MouseEventArgs e) {
			if (rbPosSpr.Checked) {
				Graphics g = Graphics.FromImage(pictTest.Image);
				int taillex = (1 << (int)zoomX.Value) << 4;
				int tailley = (2 << (int)zoomY.Value) << 4;
				XorDrawing.DrawXorRectangle(g, (Bitmap)pictTest.Image, oldPosx, oldPosy, oldPosx + taillex, oldPosy + tailley);
				pictTest.Refresh();
				DrawSpriteTest(bmpTest, numSprite, oldPosx, oldPosy);
				posSprites[numSprite, 0] = oldPosx / taillex;
				posSprites[numSprite, 1] = oldPosy / tailley;
				spriteEnableTest[numSprite] = true;
				oldPosx = oldPosy = -1;
			}
		}

		private void BpInversePalette_Click(object sender, EventArgs e) {
			int[] tempPalette = new int[16];
			for (int i = 1; i < 16; i++)
				tempPalette[i] = Cpc.paletteSprite[i];

			for (int i = 1; i < 16; i++) {
				Cpc.paletteSprite[i] = tempPalette[16 - i];
				SetLblColor(i, Cpc.paletteSprite[i]);
			}
			DrawMatrice();
			DrawSprite();
		}

		private void BpCopyBank_Click(object sender, EventArgs e) {
			for (int s = 0; s < 16; s++)
				for (int y = 0; y < 16; y++)
					for (int x = 0; x < 16; x++)
						tempSprite[x + y * 16, s] = Cpc.spritesHard[numBank, s, x, y];
		}

		private void BpPasteBank_Click(object sender, EventArgs e) {
			for (int s = 0; s < 16; s++)
				for (int y = 0; y < 16; y++)
					for (int x = 0; x < 16; x++)
						Cpc.spritesHard[numBank, s, x, y] = tempSprite[x + y * 16, s];

			DrawMatrice();
			DrawSprite();
		}

		// Clignottement rectangle autour du sprite selectionné
		private void Timer1_Tick(object sender, EventArgs e) {
			if (tickTimer++ == 1) {
				tickTimer = 0;
				lblRectSelSprite.BackColor = Color.Black;
			}
			else
				lblRectSelSprite.BackColor = Color.Red;
		}

		#region Gestion Lecture/Sauvegarde
		private void SauveSprites(bool allBank = false) {
			SaveFileDialog dlg = new SaveFileDialog {
				Filter = main.multilingue.GetString("EditSprites.TxtInfo1") + " (.spr)|*.spr|"
						+ main.multilingue.GetString("EditSprites.TxtInfo3") + " (.asm)|*.asm|"
						+ main.multilingue.GetString("EditSprites.TxtInfo4") + " (.asm)|*.asm|"
						+ main.multilingue.GetString("EditSprites.TxtInfo8") + " (.asm)|*.asm"
			};
			if (dlg.ShowDialog() == DialogResult.OK) {
				Enabled = false;
				try {
					int maxSprite = GetLastSprite(allBank);
					int size = allBank ? ((maxSprite + 15) >> 4) * 0x1000 : 0x1000;
					byte[] buffer = new byte[size];
					byte[] sprPk = new byte[512 * (maxSprite + 1)];
					int pos = 0;
					int startBank = allBank ? 0 : numBank;
					int endBank = allBank ? ((maxSprite + 15) >> 4) : numBank + 1;
					for (int bank = startBank; bank < endBank; bank++)
						for (int i = 0; i < 16; i++) {
							for (int y = 0; y < 16; y++) {
								for (int x = 0; x < 16; x++)
									buffer[pos++] = Cpc.spritesHard[bank, i, x, y];
							}
						}
					bool withNbSpr = dlg.FilterIndex == 4 && !allBank;
					string fileName = Path.GetFileNameWithoutExtension(dlg.FileName);
					SaveMedia dlgSave = new SaveMedia("Sprites", fileName, true, dlg.FilterIndex == 3, withNbSpr ? "Number of sprites to save" : null, maxSprite + 1);
					switch (dlg.FilterIndex) {
						case 1:
							CpcAmsdos entete = Cpc.CreeEntete(Path.GetFileName(dlg.FileName), 0x4000, (short)size, 0);
							FileStream fs = new FileStream(dlg.FileName, FileMode.Create);
							BinaryWriter fp = new BinaryWriter(fs);
							fp.Write(Cpc.AmsdosToByte(entete));
							fp.Write(buffer);
							fp.Close();
							fs.Close();
							// Sauvegarde palette au format .KIT
							if (chkWithPal.Checked)
								main.SavePaletteKit(Path.ChangeExtension(dlg.FileName, "kit"));

							break;

						case 2:
							// Sauvegarde assembleur
							dlgSave.ShowDialog();
							if (dlgSave.saveMediaOk) {
								StreamWriter sw = SaveAsm.OpenAsm(dlg.FileName, "");
								SaveAsm.GenereDatas(sw, buffer, (maxSprite + 1) * 256, 16, 16, dlgSave.LabelMedia);
								if (chkWithPal.Checked)
									SavePaletteKitAsm(sw, dlgSave.LabelPal);

								SaveAsm.CloseAsm(sw);
							}
							break;

						case 3:
							// Sauvegarde assembleur compacté 
							dlgSave.ShowDialog();
							if (dlgSave.saveMediaOk) {
								int numSpr = 0;
								StreamWriter sw2 = SaveAsm.OpenAsm(dlg.FileName, "");
								byte[] spr = new byte[256];
								for (int bank = startBank; bank < endBank; bank++)
									for (int i = 0; i < 16; i++) {
										Array.Copy(buffer, numSpr * 256, spr, 0, 256);
										sw2.WriteLine(dlgSave.LabelMedia + numSpr.ToString("00"));
										int l = new PackModule().Pack(spr, 256, sprPk, 0, pkMethod);
										SaveAsm.GenereDatas(sw2, sprPk, l, 16);
										if (numSpr++ >= maxSprite) {
											bank = endBank;
											i = 16;
										}
									}
								// Ajout table des pointeurs
								sw2.WriteLine("\r\n" + dlgSave.LabelPtr);
								string s = "	DW	";
								int nbWord = 0;
								for (int i = 0; i < numSpr; i++) {
									s += dlgSave.LabelMedia + i.ToString("00");
									if (++nbWord > 3) {
										nbWord = 0;
										sw2.WriteLine(s);
										s = "	DW	";
									}
									else
										s += ",";
								}
								if (s != "	DW	")
									sw2.WriteLine(s.Substring(0, s.Length - 1));

								if (dlgSave.ZeroPtr)
									sw2.WriteLine("	DW	0");

								if (chkWithPal.Checked) {
									sw2.WriteLine("");
									SavePaletteKitAsm(sw2, dlgSave.LabelPal);
								}

								SaveAsm.CloseAsm(sw2);
							}
							break;

						case 4:
							// Sauvegarde assembleur compacté full
							dlgSave.ShowDialog();
							if (dlgSave.saveMediaOk) {
								StreamWriter sw3 = SaveAsm.OpenAsm(dlg.FileName, "");
								int nbSpt = allBank ? maxSprite + 1 - (startBank << 4) : dlgSave.NbMedia;
								sw3.WriteLine("; " + nbSpt.ToString() + " sprites");
								sw3.WriteLine(dlgSave.LabelMedia);
								int lt = new PackModule().Pack(buffer, nbSpt << 8, sprPk, 0, pkMethod);
								SaveAsm.GenereDatas(sw3, sprPk, lt, 16);

								if (chkWithPal.Checked)
									SavePaletteKitAsm(sw3, dlgSave.LabelPal);

								SaveAsm.CloseAsm(sw3);
							}
							break;
					}
				}
				catch (Exception ex) {
					main.DisplayErreur(main.multilingue.GetString("EditSprites.TxtInfo5"));
					MessageBox.Show(ex.Message, ex.StackTrace);
				}
				Enabled = true;
			}
		}

		private void SavePaletteKitAsm(StreamWriter sw, string label) {
			if (!string.IsNullOrEmpty(label))
				sw.WriteLine(label);

			string s = "	DW	";
			for (int i = 1; i < 16; i++) {
				int kit = Cpc.paletteSprite[i];
				int col = (kit & 0xF00) + ((kit & 0x0F) << 4) + ((kit & 0xF0) >> 4);
				s += "#" + col.ToString("X3");
				if (i < 15)
					s += ",";
			}
			sw.WriteLine(s);
		}

		private void BpRead_Click(object sender, EventArgs e) {
			Enabled = false;
			OpenFileDialog dlg = new OpenFileDialog { Filter = main.multilingue.GetString("EditSprites.TxtInfo1") + " (.spr)|*.spr" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				try {
					FileStream fileScr = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
					byte[] tabBytes = new byte[fileScr.Length];
					fileScr.Read(tabBytes, 0, tabBytes.Length);
					fileScr.Close();
					if (Cpc.CheckAmsdos(tabBytes)) {
						CpcAmsdos enteteAms = Cpc.GetAmsdos(tabBytes);
						// Décodage sprite
						int pos = 0;
						int startBank = enteteAms.RealLength == 0x1000 ? numBank : 0;
						for (int bank = startBank; bank < 8; bank++)
							for (int i = 0; i < 16; i++) {
								for (int y = 0; y < 16; y++) {
									for (int x = 0; x < 16; x++) {
										Cpc.spritesHard[bank, i, x, y] = tabBytes[128 + pos++];
										if (pos >= tabBytes.Length - 128) {
											x = y = i = bank = 16;  // Forcer sortie de toutes les boucles
										}
									}
								}
							}
					}
					string filePalette = Path.ChangeExtension(dlg.FileName, "kit");
					if (File.Exists(filePalette)) {
						main.ReadPaletteSprite(filePalette, lblColors);
						DoGenPal();
					}
				}
				catch {
					main.DisplayErreur(main.multilingue.GetString("EditSprites.TxtInfo2"));
				}
				DrawMatrice();
			}
			Enabled = true;
			while (PeekMessage(out NativeMessage msg, (System.IntPtr)0, WM_LBUTTONUP, WM_LBUTTONUP, PM_REMOVE) != 0)
				;
		}

		private void BpSave_Click(object sender, EventArgs e) {
			SauveSprites();
		}

		private void BpReadPal_Click(object sender, EventArgs e) {
			OpenFileDialog dlg = new OpenFileDialog { Filter = "Palette CPC+ (.kit)|*.kit" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				try {
					main.ReadPaletteSprite(dlg.FileName, lblColors);
					DrawMatrice();
				}
				catch {
					main.DisplayErreur(main.multilingue.GetString("EditSprites.TxtInfo6"));
				}
			}
		}

		private void BpSavePal_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog { Filter = "Palette CPC+ (.kit)|*.kit" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				try {
					main.SavePaletteKit(dlg.FileName);
				}
				catch {
					main.DisplayErreur(main.multilingue.GetString("EditSprites.TxtInfo7"));
				}
			}
		}

		private void BpSaveAll_Click(object sender, EventArgs e) {
			SauveSprites(true);
		}
		#endregion

		private void BpScrUp_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[16];
			for (int x = 0; x < 16; x++)
				tmp[x] = Cpc.spritesHard[numBank, numSprite, x, 0];

			for (int y = 1; y < 16; y++)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, x, y - 1] = Cpc.spritesHard[numBank, numSprite, x, y];

			for (int x = 0; x < 16; x++)
				Cpc.spritesHard[numBank, numSprite, x, 15] = tmp[x];

			DrawMatrice();
		}

		private void BpScrDown_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[16];
			for (int x = 0; x < 16; x++)
				tmp[x] = Cpc.spritesHard[numBank, numSprite, x, 15];

			for (int y = 15; y > 0; y--)
				for (int x = 0; x < 16; x++)
					Cpc.spritesHard[numBank, numSprite, x, y] = Cpc.spritesHard[numBank, numSprite, x, y - 1];

			for (int x = 0; x < 16; x++)
				Cpc.spritesHard[numBank, numSprite, x, 0] = tmp[x];

			DrawMatrice();
		}

		private void BpScrLeft_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[16];
			for (int y = 0; y < 16; y++)
				tmp[y] = Cpc.spritesHard[numBank, numSprite, 0, y];

			for (int x = 1; x < 16; x++)
				for (int y = 0; y < 16; y++)
					Cpc.spritesHard[numBank, numSprite, x - 1, y] = Cpc.spritesHard[numBank, numSprite, x, y];

			for (int y = 0; y < 16; y++)
				Cpc.spritesHard[numBank, numSprite, 15, y] = tmp[y];

			DrawMatrice();

		}

		private void BpScrRight_Click(object sender, EventArgs e) {
			byte[] tmp = new byte[16];
			for (int y = 0; y < 16; y++)
				tmp[y] = Cpc.spritesHard[numBank, numSprite, 15, y];

			for (int x = 15; x > 0; x--)
				for (int y = 0; y < 16; y++)
					Cpc.spritesHard[numBank, numSprite, x, y] = Cpc.spritesHard[numBank, numSprite, x - 1, y];

			for (int y = 0; y < 16; y++)
				Cpc.spritesHard[numBank, numSprite, 0, y] = tmp[y];

			DrawMatrice();
		}
	}
}