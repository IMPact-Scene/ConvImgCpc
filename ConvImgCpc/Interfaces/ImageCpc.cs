using System;
using System.Collections.Generic;
using System.Drawing;
using System.IO;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using static ConvImgCpc.Main;

namespace ConvImgCpc {
	public partial class ImageCpc : Form {
		public DirectBitmap[] tabBmpLock;
		public DirectBitmap BmpLock { get { return tabBmpLock[selImage]; } }
		private DirectBitmap tmpLock;
		private Label[] lblColors = new Label[16];
		private Label[] lblUsedColors = new Label[16];
		private CheckBox[] lockColors = new CheckBox[16];
		private CheckBox[] disableColors = new CheckBox[16];
		private Label lblBorder;
		public int[] lockState = new int[16];
		private Image imgOrigine;
		public delegate void ConvertDelegate(bool doConvert, bool noInfo = false);
		private BitmapCpc[] TabBitmapCpc;
		public BitmapCpc BitmapCpc { get { return TabBitmapCpc[selImage]; } }
		public int selImage = 0, maxImage = 0;
		public Main main;
		public ConvertDelegate Convert;
		private int startGrille, tailleGrille;
		private bool drawGrille = false;

		const int WM_LBUTTONUP = 0x0202;
		const int PM_REMOVE = 0x0001;

		[StructLayout(LayoutKind.Sequential)]
		public struct NativeMessage {
			public IntPtr handle;
			public uint msg;
			public IntPtr wParam;
			public IntPtr lParam;
			public uint time;
			public System.Drawing.Point p;
		}

		[DllImport("user32.dll")]
		public static extern int PeekMessage(out NativeMessage lpMsg, IntPtr window, uint wMsgFilterMin, uint wMsgFilterMax, uint wRemoveMsg);

		public ImageCpc(Main m, ConvertDelegate fctConvert) {
			InitializeComponent();
			main = m;
			lblBorder = new Label {
				BorderStyle = BorderStyle.FixedSingle,
				Location = new Point(168, 58),
				Size = new Size(1024, 4),
				BackColor = Color.Black
			};
			Controls.Add(lblBorder);
			for (int i = 0; i < 16; i++) {
				// Générer les contrôles de "couleurs"
				lblColors[i] = new Label {
					BorderStyle = BorderStyle.FixedSingle,
					Location = new Point(168 + i * 48, 18),
					Size = new Size(40, 27),
					Tag = i,
					TextAlign = ContentAlignment.MiddleCenter,
					Text = i.ToString()
				};
				lblColors[i].MouseDown += ClickColor;
				Controls.Add(lblColors[i]);
				lblUsedColors[i] = new Label {
					Location = new Point(168 + i * 48, 18),
					Size = new Size(8, 8)
				};
				Controls.Add(lblUsedColors[i]);
				lblUsedColors[i].BringToFront();
				// Générer les contrôles de "bloquage couleur"
				lockColors[i] = new CheckBox {
					Location = new Point(180 + i * 48, 42),
					Size = new Size(20, 20),
					Tag = i
				};
				lockColors[i].Click += ClickLock;
				Controls.Add(lockColors[i]);
				lockColors[i].Update();
				// Générer les contrôles de désactivation couleur
				disableColors[i] = new CheckBox {
					Location = new Point(180 + i * 48, 0),
					Size = new Size(20, 20),
					Tag = i
				};
				disableColors[i].Click += DisableColor;
				Controls.Add(disableColors[i]);
			}
			InitBitmapCpc(1, 100);
			Reset();
			tailleCrayon.SelectedItem = "1";
			Convert = fctConvert;
			pictureBox.Image = imgOrigine = BmpLock.Bitmap;
		}

		public void InitBitmapCpc(int nbImage, int tps) {
			if (nbImage == 0)
				nbImage++;

			selImage = 0;
			maxImage = nbImage;
			TabBitmapCpc = new BitmapCpc[nbImage];
			tabBmpLock = new DirectBitmap[nbImage];
			for (int i = 0; i < nbImage; i++) {
				TabBitmapCpc[i] = new BitmapCpc();
				tabBmpLock[i] = new DirectBitmap(pictureBox.Width, pictureBox.Height) { Tps = tps };
			}
		}

		public void Reset(bool force = false) {
			if (!BitmapCpc.isCalc || force) {
				int startImg = force ? 0 : selImage;
				int endImg = force ? maxImage : selImage;
				for (int i = startImg; i < endImg; i++) {
					selImage = i;
					int col = SystemColors.Control.ToArgb();
					for (int y = 0; y < tabBmpLock[i].Height; y += 2) {
						int startX = y < Cpc.TailleY ? Cpc.TailleX : 0;
						tabBmpLock[i].SetHorLineDouble(0, y, startX, Cpc.GetPalCPC(Cpc.Palette[0]));
						tabBmpLock[i].SetHorLineDouble(startX, y, tabBmpLock[i].Width - startX, col);
					}
				}
			}
			UpdatePalette();
			ToolModeDraw(null);
		}

		public void SetImgCopy() {
			if (imgMotif != null && imgCopy != null)
				imgCopy.CopyBits(BmpLock);
		}

		public void SetPixelCpc(int xPos, int yPos, int col, int tx) {
			BmpLock.SetHorLineDouble(xPos, yPos, tx, Cpc.GetPalCPC(Cpc.modeVirtuel == 5 || Cpc.modeVirtuel == 6 ? Cpc.colMode5[Math.Min(271, yPos >> 1), col] : Cpc.Palette[col]));
		}

		public void SetImpDrawMode(bool impDrawMode) {
			modeImpDraw = pictImpDraw.Visible = Cpc.TailleY == 544 && Cpc.TailleX == 768 && impDrawMode;
		}

		private void RefreshUsedColor() {
			byte[] col = new byte[16];
			for (int i = 0; i < 16; i++)
				col[i] = 0;

			for (int y = 0; y < imgOrigine.Height; y += 2) {
				int tx = Cpc.CalcTx(y);
				for (int x = 0; x < imgOrigine.Width; x += tx) {
					int pen = Cpc.GetPenColor(BmpLock, x, y);
					col[pen & 0x0F] = 1;
				}
			}
			for (int i = 0; i < 16; i++)
				lblUsedColors[i].BackColor = col[i] == 0 ? Color.Red : Color.Green;
		}

		public void Render(bool forceDrawZoom = false, bool withSpriteGrid = false, bool withTilesGrid = false) {
			groupBoxPosSprite.Visible = Cpc.cpcPlus && Cpc.modeVirtuel <= 2;
			bpGenPal.Visible = Cpc.cpcPlus;
			UpdatePalette();
			modeCaptureSprites.Visible = chkGrilleSprite.Visible = Cpc.modeVirtuel == 11;
			chkGrille.Visible = /*modeEdition.Visible =*/ Cpc.modeVirtuel != 11;
			if (chkDoRedo.Checked && modeEdition.Checked) {
				Enabled = false;
				List<MemoPoint> lst = undo.lstUndoRedo;
				foreach (MemoPoint p in lst) {
					int Tx = Cpc.CalcTx(p.posy);
					BmpLock.SetHorLineDouble(p.posx, p.posy, Tx, p.newColor);
				}
				forceDrawZoom = true;
				bpUndo.Enabled = undo.CanUndo;
				bpRedo.Enabled = undo.CanRedo;
				Enabled = true;
			}

			if (zoom != 1) {
				if (tmpLock == null)
					tmpLock = new DirectBitmap(imgOrigine.Width, imgOrigine.Height);

				if (forceDrawZoom) {
					for (int y = 0; y < imgOrigine.Height; y += 2) {
						int ySrc = offsetY + (y / zoom);
						for (int x = 0; x < imgOrigine.Width; x += zoom)
							tmpLock.SetHorLineDouble(x, y, Math.Min(zoom, imgOrigine.Width - x - 1), ySrc > Cpc.TailleY - 1 ? SystemColors.Control.ToArgb() : BmpLock.GetPixel(offsetX + (x / zoom), ySrc));
					}
				}
				pictureBox.Image = tmpLock.Bitmap;
			}
			else {
				pictureBox.Image = imgCopy != null ? imgCopy.Bitmap : BmpLock.Bitmap;
				tmpLock = null;
			}
			if (drawGrille) {
				int x = startGrille * 16;
				Graphics g = Graphics.FromImage(pictureBox.Image);
				while (x < Cpc.TailleX) {
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x, 0, x, Cpc.TailleY, false);
					x += tailleGrille * 16;
				}
			}
			if (chkGrilleSprite.Checked || withSpriteGrid) {
				Graphics g = Graphics.FromImage(pictureBox.Image);
				for (int x = 0; x < Cpc.TailleX; x += 32)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x, 0, x, Cpc.TailleY, false);

				for (int y = 0; y < Cpc.TailleY; y += 32)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, 0, y, Cpc.TailleX, y, false);
			}

			if (chkTilesGrid.Checked || withTilesGrid) {
				Graphics g = Graphics.FromImage(pictureBox.Image);
				int tx = Cpc.CalcTx(0);
				int sizex = (int)tilesSizeX.Value * tx;
				int sizey = (int)tilesSizeY.Value * 2;
				for (int x = 0; x < Cpc.TailleX; x += sizex)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x, 0, x, Cpc.TailleY, false);

				for (int y = 0; y < Cpc.TailleY; y += sizey)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, 0, y, Cpc.TailleX, y, false);
			}

			if (chkAfficheSH.Checked)
				DrawSpritesHard(pictureBox);

			pictureBox.Refresh();
			if (fenetreRendu != null) {
				fenetreRendu.SetImage(BmpLock.Bitmap);
				fenetreRendu.Picture.Refresh();
			}
			RefreshUsedColor();
			if (chkAutoCopy.Checked)
				CopyToClipBoard();

			main.rasterPlus?.DrawLines();
			main.editSplit?.UpdateScrSplit(BmpLock);
		}

		public void CaptureSprite(int captSizeX, int captSizeY, int posx, int posy, DirectBitmap bmp) {
			Graphics g = Graphics.FromImage(pictureBox.Image);
			int x, y;
			if (chkGrilleSprite.Checked) {
				for (x = 0; x < Cpc.TailleX; x += 32)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x, 0, x, Cpc.TailleY, false);

				for (y = 0; y < Cpc.TailleY; y += 32)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, 0, y, Cpc.TailleX, y, false);
			}
			int sprSizeX = captSizeX << 5;
			int sprSizeY = captSizeY << 5;
			for (x = 0; x < sprSizeX; x += 2)
				for (y = 0; y < sprSizeY; y += 2) {
					int c = BmpLock.GetPixel(posx + x, posy + y);
					for (int zx = 0; zx < 8; zx++)
						for (int zy = 0; zy < 8; zy++)
							bmp.SetPixel(zx + x * 4, zy + y * 4, c);
				}
			if (chkGrilleSprite.Checked) {
				for (x = 0; x < Cpc.TailleX; x += 32)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, x, 0, x, Cpc.TailleY, false);

				for (y = 0; y < Cpc.TailleY; y += 32)
					XorDrawing.DrawXorLine(g, (Bitmap)pictureBox.Image, 0, y, Cpc.TailleX, y, false);
			}
		}

		// Déplacement/Zoom image
		private void MoveOrSize(MouseEventArgs e) {
			if (e.X >= 0 && e.Y >= 0 && e.Y < Cpc.TailleY && e.X < Cpc.TailleX) {
				if (e.Button == MouseButtons.Left) {
					if (!movePos) {
						main.GetSizePos(ref posx, ref posy, ref sizex, ref sizey);
						movePos = true;
						memoMouseX = e.X;
						memoMouseY = e.Y;
					}
					else 
						main.SetSizePos(posx + memoMouseX - e.X, posy + memoMouseY - e.Y, sizex, sizey, true);
				}
				else {
					if (e.Button == MouseButtons.Right) {
						if (chkAfficheSH.Checked && e.X / (chkX2.Checked ? 2 : 1) < Cpc.TailleX && e.Y / (chkX2.Checked ? 4 : 2) < Cpc.TailleY) {
							numPosX.Value = e.X / (chkX2.Checked ? 2 : 1);
							numPosY.Value = e.Y / (chkX2.Checked ? 4 : 2);
						}
						else {
							if (!moveSize) {
								main.GetSizePos(ref posx, ref posy, ref sizex, ref sizey);
								moveSize = true;
								memoMouseX = e.X;
								memoMouseY = e.Y;
							}
							else
								main.SetSizePos(posx, posy, sizex - memoMouseX + e.X, sizey - memoMouseY + e.Y, true);
						}
					}
					else
						movePos = moveSize = false;
				}
			}
		}

		#region Lecture/Sauvegarde
		public void SauvePng(string fileName, Param param) {
			if (Cpc.modeVirtuel == 6) {
				string singleName = Path.GetDirectoryName(fileName) + "\\" + Path.GetFileNameWithoutExtension(fileName);
				DirectBitmap bmpRaster = new DirectBitmap(BmpLock.Bitmap.Width >> 1, BmpLock.Bitmap.Height >> 1);
				RvbColor c2 = new RvbColor(0);
				int posx = 0;
				for (int y = 0; y < bmpRaster.Height; y++) {
					bool memoC = false;
					for (int x = 0; x < bmpRaster.Width; x++) {
						RvbColor c = BmpLock.GetPixelColor(x << 1, y << 1);
						for (int i = 0; i < 16; i++) {
							RvbColor p = Cpc.RgbCPC[Cpc.colMode5[y, i]];
							if (p.r == c.r && p.v == c.v && p.b == c.b) {
								if (i > 2) {
									c2 = p;
									int start = memoC ? x & 0xFF8 : 0;
									memoC = true;
									for (int r = start; r < x; r++)
										bmpRaster.SetPixel(r, y, c2);
								}
								break;
							}
						}
						bmpRaster.SetPixel(x, y, c2);
						posx++;
					}
				}
				bmpRaster.Bitmap.Save(singleName + "_Rasters" + ".png", System.Drawing.Imaging.ImageFormat.Png);
				BitmapCpc.CreeBmpCpcForceMode1(BmpLock);
				SauveImage.SauveScr(singleName + ".scr", BitmapCpc, main, Main.PackMethode.None, Main.OutputFormat.Binary, param);
			}
			else {
				DirectBitmap bmpResize = new DirectBitmap(Cpc.TailleX, Cpc.TailleY);
				for (int y = 0; y < Cpc.TailleY; y++)
					for (int x = 0; x < Cpc.TailleX; x++)
						bmpResize.SetPixel(x, y, BmpLock.GetPixel(x, y));

				bmpResize.Bitmap.Save(fileName, System.Drawing.Imaging.ImageFormat.Png);
			}

			main.SetInfo("Sauvegarde image PNG ok.");
		}

		public void SauveScr(string fileName, Main.OutputFormat format, Param param) {
			BitmapCpc.CreeBmpCpc(BmpLock);
			SauveImage.SauveScr(fileName, BitmapCpc, main, Main.PackMethode.None, format, param);
			main.SetInfo("Sauvegarde image CPC ok.");
		}

		public void SauveCmp(string fileName, Main.PackMethode pkMethode, Main.OutputFormat format, Param param, string version = null) {
			BitmapCpc.CreeBmpCpc(BmpLock);
			if (Cpc.modeVirtuel >= 7 && version != null) {
				SaveAnim sa = new SaveAnim(main, fileName, version, pkMethode);
				string labelPalette = "Palette";
				sa.DoSave(true, pkMethode, labelPalette);
				sa.Dispose();
			}
			else
				SauveImage.SauveScr(fileName, BitmapCpc, main, pkMethode, format, param, version, Cpc.colMode5);

			main.SetInfo("Sauvegarde image compactée ok.");
		}

		private byte[] MakeSprite() {
			return MakeSprite(0, 0, Cpc.TailleX, Cpc.TailleY);
		}

		private byte[] MakeSprite(int posX, int posY, int tailleX, int tailleY) {
			byte[] ret = new byte[(tailleX * tailleY) >> 4];
			Array.Clear(ret, 0, ret.Length);
			for (int y = 0; y < tailleY; y += 2) {
				int tx = Cpc.CalcTx(y);
				for (int x = 0; x < tailleX; x += 8) {
					byte octet = 0;
					for (int p = 0; p < 8; p++)
						if ((p % tx) == 0) {
							int pen = Cpc.GetPenColor(BmpLock, x + p + posX, y + posY);
							octet |= (byte)(Cpc.tabOctetMode[pen] >> (p / tx));
						}
					ret[(x >> 3) + (y >> 1) * (tailleX >> 3)] = octet;
				}
			}
			return ret;
		}

		private int MakeCell(StreamWriter sw, int x, int y, int sizex, int sizey, byte[] tempArray, int totSize) {
			byte[] ret = MakeSprite(x, y, sizex, sizey);
			if (chkPack.Checked)
				Array.Copy(ret, 0, tempArray, totSize, ret.Length);
			else {
				SaveAsm.GenereDatas(sw, ret, ret.Length, sizex >> 3, 0, null, true);
				sw.WriteLine(";");
			}
			return ret.Length;
		}

		public void SauveFont(string fileName) {
			SaveMedia dlgSave = new SaveMedia("Bitmap tiles", Path.GetFileNameWithoutExtension(fileName), main.param.withPalette);
			dlgSave.ShowDialog();
			byte[] tempArray = new byte[0x10000];
			byte[] packArray = new byte[0x10000];
			if (dlgSave.saveMediaOk) {
				int tx = Cpc.CalcTx(0);
				int sizex = (int)tilesSizeX.Value * tx;
				int sizey = (int)tilesSizeY.Value * 2;
				StreamWriter sw = SaveAsm.OpenAsm(fileName);
				int totSize = 0;
				sw.WriteLine(dlgSave.LabelMedia);
				if (rbRows.Checked) {
					for (int y = 0; y < Cpc.TailleY; y += sizey)
						for (int x = 0; x < Cpc.TailleX; x += sizex)
							totSize += MakeCell(sw, x, y, sizex, sizey, tempArray, totSize);
				}
				else {
					for (int x = 0; x < Cpc.TailleX; x += sizex)
						for (int y = 0; y < Cpc.TailleY; y += sizey)
							totSize += MakeCell(sw, x, y, sizex, sizey, tempArray, totSize);
				}
				if (chkPack.Checked) {
					int l = new PackModule().Pack(tempArray, totSize, packArray, totSize, main.pkMethode);
					SaveAsm.GenereDatas(sw, packArray, l, 16);
				}
				else
					sw.WriteLine("; Taille totale " + totSize.ToString() + " octets");

				if (main.param.withPalette)
					SaveAsm.GenerePalette(sw, main.param, false, false, dlgSave.LabelPal);

				SaveAsm.CloseAsm(sw);
				main.SetInfo("Sauvegarde tileset assembleur ok.");
			}
		}

		public void SauveSprite(string fileName, string version, Param param) {
			byte[] ret = MakeSprite();
			SaveMedia dlgSave = new SaveMedia("Soft sprite", Path.GetFileNameWithoutExtension(fileName), param.withPalette);
			dlgSave.ShowDialog();
			if (dlgSave.saveMediaOk) {
				StreamWriter sw = SaveAsm.OpenAsm(fileName, version, true);
				SaveAsm.GenereDatas(sw, ret, ret.Length, Cpc.TailleX >> (Cpc.TailleX <= 320 ? 3 : Cpc.TailleX <= 640 ? 4 : 5), 0, dlgSave.LabelMedia);
				if (param.withPalette)
					SaveAsm.GenerePalette(sw, param, false, false, dlgSave.LabelPal);

				SaveAsm.CloseAsm(sw);
				main.SetInfo("Sauvegarde sprite assembleur ok.");
			}
		}

		public void SauveSpriteCmp(string fileName, string version, Param param, Main.PackMethode pkMethode) {
			byte[] ret = MakeSprite();
			SaveMedia dlgSave = new SaveMedia("Soft sprite", Path.GetFileNameWithoutExtension(fileName), param.withPalette);
			dlgSave.ShowDialog();
			if (dlgSave.saveMediaOk) {
				byte[] sprCmp = new byte[ret.Length];
				StreamWriter sw = SaveAsm.OpenAsm(fileName, version, true);
				SaveAsm.GenereDatas(sw, sprCmp, new PackModule().Pack(ret, ret.Length, sprCmp, 0, pkMethode), 16, 0, dlgSave.LabelMedia);
				if (param.withPalette)
					SaveAsm.GenerePalette(sw, param, false, false, dlgSave.LabelPal);

				SaveAsm.CloseAsm(sw);
				main.SetInfo("Sauvegarde sprite assembleur compacté ok.");
			}
		}

		public void SauveImp(string fileName) {
			int nbImages = main.GetMaxImages();
			int l = 0;
			for (int i = 0; i < nbImages; i++) {
				main.SelectImage(i, true);
				Convert(!BitmapCpc.isCalc, true);
				l += MakeSprite().Length;
			}
			l += 3; // 3 octets de fin de fichier
			CpcAmsdos entete = Cpc.CreeEntete(fileName, 0x4000, (short)l, -13622);
			FileStream s = new FileStream(fileName, FileMode.Create);
			BinaryWriter fp = new BinaryWriter(s);
			fp.Write(Cpc.AmsdosToByte(entete));
			for (int i = 0; i < nbImages; i++) {
				main.SelectImage(i, true);
				byte[] sprite = MakeSprite();
				fp.Write(sprite, 0, sprite.Length);
			}
			byte[] endData = { (byte)(Cpc.TailleX >> 3), (byte)(Cpc.TailleY >> 1), (byte)nbImages };
			fp.Write(endData, 0, endData.Length);
			fp.Close();
			s.Close();
		}

		public void SauveTiles(string fileName, int tailleX, int tailleY, Param param) {
			List<byte[]> lstTiles = new List<byte[]>();
			int l = 0;
			byte[] tabTiles = new byte[2048];
			int nbTiles = 0;
			for (int y = 0; y < Cpc.TailleY; y += tailleY)
				for (int x = 0; x < Cpc.TailleX; x += tailleX) {
					byte[] tile = MakeSprite(x, y, tailleX, tailleY);
					// Vérifier tile n'existe déjà pas dans la liste
					bool foundOne = false;
					for (int t = 0; t < lstTiles.Count; t++) {
						bool found = true;
						byte[] tileFound = lstTiles[t];
						for (int i = 0; i < tileFound.Length; i++)
							if (tileFound[i] != tile[i]) {
								found = false;
								break;
							}
						if (found) {
							tabTiles[nbTiles++] = (byte)t;
							foundOne = true;
							break;
						}
					}
					if (!foundOne) {
						int t = lstTiles.Count;
						if (t < 255) {
							tabTiles[nbTiles++] = (byte)t;
							lstTiles.Add(tile);
							l += tile.Length;
						}
						else {
							break;
						}
					}
				}
			int nbImages = lstTiles.Count;
			if (nbImages < 255) {
				main.SetInfo("Nbre de tiles:" + nbImages);
				l += 3; // 3 octets de fin de fichier
				CpcAmsdos entete = Cpc.CreeEntete(fileName, 0x4000, (short)l, -13622);
				FileStream fs = new FileStream(fileName, FileMode.Create);
				BinaryWriter fp = new BinaryWriter(fs);
				fp.Write(Cpc.AmsdosToByte(entete));
				for (int i = 0; i < nbImages; i++) {
					byte[] sprite = lstTiles[i];
					fp.Write(sprite, 0, sprite.Length);
				}
				byte[] endData = { (byte)(tailleX >> 3), (byte)(tailleY >> 1), (byte)nbImages };
				fp.Write(endData, 0, endData.Length);
				fp.Close();
				fs.Close();

				// Ecriture fichier .TIL
				string s = fileName.Substring(0, fileName.Length - 3) + "TIL";
				entete = Cpc.CreeEntete(s, 0x4000, (short)l, -13622);
				fs = new FileStream(s, FileMode.Create);
				fp = new BinaryWriter(fs);
				fp.Write(Cpc.AmsdosToByte(entete));
				fp.Write(tabTiles, 0, nbTiles);
				fp.Close();
				fs.Close();

				// Ecriture fichier palette
				if (param.cpcPlus)
					main.SavePaletteKit(Path.ChangeExtension(fileName, "KIT"), true);
				else
					SauvePalette(Path.ChangeExtension(fileName, "PAL"), param);
			}
			else
				main.DisplayErreur("Trop de tiles...");
		}

		/*
				public byte[] GetCpcScr(Param param, bool spriteMode = false) {
					int maxSize = (Cpc.TailleX >> 3) + ((Cpc.TailleY - 2) >> 4) * (Cpc.TailleX >> 3) + ((Cpc.TailleY - 2) & 14) * 0x400;
					if (spriteMode)
						maxSize = (Cpc.TailleX * Cpc.TailleY) >> 4;
					else
						if (maxSize >= 0x4000)
							maxSize += 0x3800;

					byte[] ret = new byte[maxSize];
					Array.Clear(ret, 0, ret.Length);
					int posRet = 0;
					for (int y = 0; y < Cpc.TailleY; y += 2) {
						int adrCPC = Cpc.GetAdrCpc(y);
						int tx = Cpc.CalcTx(y);
						for (int x = 0; x < Cpc.TailleX; x += 8) {
							byte pen = 0, octet = 0;
							for (int p = 0; p < 8; p++)
								if ((p % tx) == 0) {
									RvbColor col = BmpLock.GetPixelColor(x + p, y);
									if (Cpc.cpcPlus) {
										for (pen = 0; pen < 16; pen++) {
											if ((col.v >> 4) == (Cpc.Palette[pen] >> 8) && (col.b >> 4) == ((Cpc.Palette[pen] >> 4) & 0x0F) && (col.r >> 4) == (Cpc.Palette[pen] & 0x0F))
												break;
										}
									}
									else {
										for (pen = 0; pen < 16; pen++) {
											RvbColor fixedCol = Cpc.RgbCPC[Cpc.Palette[pen]];
											if (fixedCol.r == col.r && fixedCol.b == col.b && fixedCol.v == col.v)
												break;
										}
									}
									octet |= (byte)(Cpc.tabOctetMode[pen] >> (p / tx));
								}
							if (!spriteMode)
								posRet = Cpc.GetAdrCpc(y) + (x >> 3);

							ret[posRet++] = octet;
						}
					}
					return ret;
				}
		*/

		public void SauveMatrice(string fileName, string version, Param param, Main.PackMethode pkMethode) {
			int maxSize = (Cpc.TailleX * Cpc.TailleY) >> 6;
			byte[] ret = new byte[maxSize];
			Array.Clear(ret, 0, ret.Length);
			int posRet = 0;
			for (int y = 0; y < Cpc.TailleY; y += 8) {
				//int adrCPC = Cpc.GetAdrCpc(y);
				//int tx = Cpc.CalcTx(y);
				for (int x = 0; x < Cpc.TailleX; x += 8) {
					byte pen = (byte)Cpc.GetPenColor(BmpLock, x, y);
					ret[posRet++] = (byte)(Cpc.tabOctetMode[pen] + (Cpc.tabOctetMode[pen] >> 1));
				}
			}
			byte[] retCmp = new byte[maxSize];
			StreamWriter sw = SaveAsm.OpenAsm(fileName, version);
			SaveAsm.GenerePalette(sw, param, false, false, "Palette");
			sw.WriteLine("Mat64x64Cmp");
			SaveAsm.GenereDatas(sw, retCmp, new PackModule().Pack(ret, ret.Length, retCmp, 0, pkMethode), 16);
			SaveAsm.CloseAsm(sw);
			main.SetInfo("Sauvegarde matrice assembleur ok.");
		}

		public void SauveDeltaPack(string fileName, string version, bool reboucle, Main.PackMethode pkMethode) {
			if (Cpc.NbCol * Cpc.NbLig > 0x4000)
				MessageBox.Show("Les animations avec des écrans de plus de 16ko ne sont pas supportés...");
			else
				new SaveAnim(main, fileName, version, pkMethode).ShowDialog();
		}

		public void SauveDiffImage(string fileName, string version, bool reboucle, Main.PackMethode pkMethode) {
			main.SetInfo("Début sauvegarde DiffImage assembleur...");
			main.Enabled = main.anim.Enabled = Enabled = false;
			int nbImages = main.GetMaxImages();
			byte[] bufOut = new byte[0x10000];
			StreamWriter sw = SaveAsm.OpenAsm(fileName, version, true);
			sw.WriteLine("\tnolist");
			for (int i = 0; i < nbImages; i++) {
				main.SelectImage(i, true);
				Convert(!BitmapCpc.isCalc, true);
				Application.DoEvents();
				BitmapCpc.CreeBmpCpc(BmpLock);
				byte[] src = BitmapCpc.bmpCpc;
				if (i == 0)
					Buffer.BlockCopy(src, 0, bufOut, 0, src.Length);
				else {
					sw.WriteLine("DiffImage_" + i.ToString() + ":");
					DiffAnim dif = new DiffAnim();
					for (int m = 0; m < src.Length; m++) {
						if (src[m] != bufOut[m]) {
							dif.AddDiff(m, src[m]);
							bufOut[m] = src[m];
						}
					}
					dif.Save(sw, 32);
				}
			}
			sw.WriteLine("\tlist");
			sw.WriteLine("\tDW\t#0000;\tFin anim");
			SaveAsm.CloseAsm(sw);
		}

		public void SauvBump(string fileName, string version) {
			//RvbColor c2 = new RvbColor(0);
			int pos = 0;
			byte[] bump = new byte[Cpc.TailleY * Cpc.TailleX / 64];
			for (int y = 0; y < Cpc.TailleY; y += 16) {
				for (int x = 0; x < Cpc.TailleX; x += 8) {
					for (int yy = 0; yy < 2; yy++) {
						bump[pos++] = (byte)Cpc.GetPenColor(BmpLock, x, y + (yy << 3));
					}
				}
			}
			StreamWriter sw = SaveAsm.OpenAsm(fileName, version, true);
			SaveAsm.GenereDatas(sw, bump, bump.Length, 16);
			SaveAsm.CloseAsm(sw);
			main.SetInfo("Sauvegarde bump ok.");
		}

		public void SauveTunnel(string fileName) {
			bool withGreen = true;
			int incy = BitmapCpc.TailleY / 12;
			int incx = BitmapCpc.TailleX / 16;
			int nbImages = main.GetMaxImages();
			StreamWriter sw = File.CreateText(fileName);
			for (int i = 0; i < nbImages; i++) {
				main.SelectImage(i, true);
				Convert(!BitmapCpc.isCalc, true);
				sw.WriteLine("DataImage" + i.ToString("000"));
				for (int y = 0; y < BitmapCpc.TailleY; y += incy) {
					int totv = 0, divv = 0;
					string s = "";
					for (int x = 0; x < BitmapCpc.TailleX; x += incx) {
						int div = 0, totr = 0, totb = 0;
						for (int yy = 0; yy < incy; yy += 2) {
							for (int xx = 0; xx < incx; xx += 2) {
								RvbColor col = BmpLock.GetPixelColor(x + xx, y + yy);
								totr += col.r;
								totv += col.v;
								totb += col.b;
								div += 17;
								divv += 17;
							}
						}
						totr = Math.Min(15, (totr / div) + (totv / (div * 2)));
						totb = Math.Min(15, (totb / div) + (totv / (div * 8)));
						s += "#" + totr.ToString("X1") + totb.ToString("X1");
						if (x < BitmapCpc.TailleX - incx)
							s += ",";
					}
					if (withGreen)
						s = "#" + (totv / divv).ToString("X2") + "," + s;

					sw.WriteLine("\tDB\t" + s);
				}
			}
			sw.WriteLine("");
			sw.WriteLine("Anim");
			for (int i = 0; i < nbImages; i++)
				sw.WriteLine("	DW	DataImage" + i.ToString("000"));

			sw.WriteLine("	DW	0");
			sw.Close();
		}

		private void CheckLockedPalette(Param param) {
			bool oneLocked = false;
			for (int i = 0; i < Cpc.MaxPen(Cpc.yEgx ^ 2); i++)
				if (lockState[i] != 0)
					oneLocked = true;

			if (!oneLocked)
				lockAllPal.Checked = true;

			UpdatePalette();
		}

		public void LirePalette(string fileName, Param param) {
			if (SauveImage.LirePalette(fileName, param)) {
				CheckLockedPalette(param);
				main.SetInfo("Lecture palette ok.");
			}
			else {
				main.SetInfo("Erreur lecture palette...");
				main.DisplayErreur("Erreur lecture palette...");
			}
		}

		public void LirePaletteKit(string fileName, Param param) {
			if (SauveImage.LirePaletteKit(fileName)) {
				CheckLockedPalette(param);
				main.SetInfo("Lecture palette ok.");
			}
			else {
				main.SetInfo("Erreur lecture palette...");
				main.DisplayErreur("Erreur lecture palette...");
			}
		}

		public void SauvePalette(string fileName, Param param) {
			SauveImage.SauvePalette(fileName, param);
			main.SetInfo("Sauvegarde palette ok.");
		}

		public void SauveEgx(string fileName, Param param) {
			int mode = Cpc.modeVirtuel;
			int model1 = mode == 3 ? 1 : 2;
			int model2 = mode == 3 ? 0 : 1;
			Cpc.modeVirtuel = model1;
			if (fileName.ToUpper().EndsWith(".SCR"))
				fileName = fileName.Substring(0, fileName.Length - 4);

			BitmapCpc.CreeBmpCpc(BmpLock, true, 0);
			SauveImage.SauveScr(fileName + "0.SCR", BitmapCpc, main, Main.PackMethode.None, Main.OutputFormat.Binary, param);
			Cpc.modeVirtuel = model2;
			BitmapCpc.CreeBmpCpc(BmpLock, true, 1);
			SauveImage.SauveScr(fileName + "1.SCR", BitmapCpc, main, Main.PackMethode.None, Main.OutputFormat.Binary, param);
			main.SetInfo("Sauvegarde des deux images CPC ok.");
			Cpc.modeVirtuel = mode;
		}
		#endregion

		#region Gestion palette
		// Click sur un "lock"
		private void ClickLock(object sender, EventArgs e) {
			CheckBox colorLock = sender as CheckBox;
			int numLock = colorLock.Tag != null ? (int)colorLock.Tag : 0;
			lockState[numLock] = colorLock.Checked ? 1 : 0;
			Convert(false);
		}

		// Changement de la palette
		private void ClickColor(object sender, MouseEventArgs e) {
			Label colorClick = sender as Label;
			int pen = colorClick.Tag != null ? (int)colorClick.Tag : 0;
			if (!modeEdition.Checked) {
				EditColor ed = new EditColor(main, pen, Cpc.Palette[pen], BitmapCpc.GetColorPal(pen).GetColorArgb, Cpc.cpcPlus);
				ed.ShowDialog(this);
				if (ed.isValide) {
					Cpc.Palette[pen] = ed.ValColor;
					lockColors[pen].Checked = true;
					lockState[pen] = 1;
					UpdatePalette();
					Convert(false);
				}
			}
			else {
				if (editToolMode != EditTool.Draw && editToolMode != EditTool.Fill)
					rbDraw.Checked = true;

				RvbColor col = BitmapCpc.GetColorPal(pen);
				if (e.Button == MouseButtons.Left) {
					drawCol = pen;
					drawColor.BackColor = Color.FromArgb(col.r, col.v, col.b);
					drawColor.Text = pen.ToString();
					drawColor.ForeColor = (col.r * 9798 + col.v * 19235 + col.b * 3735) > 0x400000 ? Color.Black : Color.White;
				}
				else {
					undrawCol = pen;
					undrawColor.BackColor = Color.FromArgb(col.r, col.v, col.b);
					undrawColor.Text = pen.ToString();
					undrawColor.ForeColor = (col.r * 9798 + col.v * 19235 + col.b * 3735) > 0x400000 ? Color.Black : Color.White;
				}
			}
		}

		private void DisableColor(object sender, EventArgs e) {
			CheckBox chkDisable = sender as CheckBox;
			int numLock = chkDisable.Tag != null ? (int)chkDisable.Tag : 0;
			main.param.disableState[numLock] = chkDisable.Checked ? 1 : 0;
			Convert(false);
		}

		private void UpdatePalette() {
			int maxPen = Cpc.MaxPen(Cpc.yEgx ^ 2);
			for (int i = 0; i < 16; i++) {
				RvbColor col = BitmapCpc.GetColorPal(i);
				lblColors[i].BackColor = Color.FromArgb(col.GetColorArgb);
				lblColors[i].ForeColor = (col.r * 9798 + col.v * 19235 + col.b * 3735) > 0x400000 ? Color.Black : Color.White;
				lockColors[i].Visible = lblColors[i].Visible = lblUsedColors[i].Visible = main.param.disableState[i] == 0 && i < maxPen;
				disableColors[i].Visible = i < maxPen;
				lblColors[i].Refresh();
			}
		}

		public void LockAllPal_CheckedChanged(object sender, EventArgs e) {
			for (int i = 0; i < 16; i++) {
				lockColors[i].Checked = lockAllPal.Checked;
				lockState[i] = lockAllPal.Checked ? 1 : 0;
			}
			Convert(false);
		}

		public void SetLockPalette() {
			for (int i = 0; i < 16; i++)
				lockColors[i].Checked = lockState[i] != 0;

			Convert(false);
		}

		// Copier la palette dans le presse-papier
		private void BpCopyPal_Click(object sender, EventArgs e) {
			string palTxt = "";
			int i, maxPen = Cpc.MaxPen(Cpc.yEgx ^ 2);
			for (i = 0; i < maxPen; i++) {
				int val = Cpc.Palette[i];
				if (Cpc.cpcPlus)
					val = (val & 0xF00) + ((val & 0x0F) << 4) + ((val & 0xF0) >> 4);

				string valStr = Cpc.cpcPlus ? ("&" + val.ToString("X3")) : val.ToString();
				palTxt += valStr + (i < maxPen - 1 ? "," : "");
			}
			palTxt += "\r\n";
			if (Cpc.cpcPlus)
				palTxt += ";ASIC Values :\r\n	DW	";
			else
				palTxt += ";Gate Array Values :\r\n	DB	";

			for (i = 0; i < maxPen; i++) {
				int val = Cpc.Palette[i];
				if (Cpc.cpcPlus)
					val = (val & 0xF00) + ((val & 0x0F) << 4) + ((val & 0xF0) >> 4);
				if (val == 0xFFFF)
					val = 0;

				string valStr = "#" + (Cpc.cpcPlus ? val.ToString("X3") : ((byte)(Cpc.CpcVGA[val])).ToString("X2"));
				palTxt += valStr + (i < maxPen - 1 ? "," : "");

			}
			Clipboard.SetText(palTxt);
			MessageBox.Show("Palette copiée dans le presse-papier");
		}
		#endregion

		private void ChkX2_CheckedChanged(object sender, EventArgs e) {
			if (chkX2.Checked) {
				Width = 1232 + 1024;
				Height = 668 + 544;
				lblBorder.Width = pictureBox.Width = 2048;
				pictureBox.Height = 1088;
				vScrollBar.Left = 1195 + 1024;
				hScrollBar.Top = 608 + 544;
			}
			else {
				Width = 1232;
				Height = 715;
				lblBorder.Width = pictureBox.Width = 1024;
				pictureBox.Height = 544;
				vScrollBar.Left = 1195;
				hScrollBar.Top = 608;
			}
			pictureBox.Refresh();
		}

		public void ResetX2() {
			chkX2.Checked = false;
		}

		public void ResetGrille() {
			chkGrille.Checked = false;
		}

		private void ImageCpc_FormClosing(object sender, FormClosingEventArgs e) {
			e.Cancel = true;
		}

		private void ChkGrille_CheckedChanged(object sender, EventArgs e) {
			if (chkGrille.Checked) {
				if (int.TryParse(txbStartNop.Text, out startGrille) && int.TryParse(txbTailleNop.Text, out tailleGrille)) {
					if (tailleGrille > 0 && tailleGrille <= 16 && startGrille + tailleGrille < 64) {
						txbStartNop.Enabled = txbTailleNop.Enabled = false;
						drawGrille = true;
						Render(true);
					}
					else
						chkGrille.Checked = false;
				}
				else
					chkGrille.Checked = false;
			}
			else {
				Render(true);
				drawGrille = false;
				txbStartNop.Enabled = txbTailleNop.Enabled = true;
			}
		}

		private void DoGenPal() {
			for (int c = 0; c < 16; c++) {
				int col = Cpc.Palette[c];
				int r = ((col & 0x0F) * 17);
				int v = (((col & 0xF00) >> 8) * 17);
				int b = (((col & 0xF0) >> 4) * 17);
				lblColors[c].BackColor = Color.FromArgb(r, v, b);
				lblColors[c].ForeColor = (r * 9798 + v * 19235 + b * 3735) > 0x400000 ? Color.Black : Color.White;
				lblColors[c].Refresh();
				lockColors[c].Checked = true;
				lockState[c] = 1;
			}
			Convert(false);
		}

		private void BpGenPal_Click(object sender, EventArgs e) {
			GenPalette g = new GenPalette(Cpc.Palette, 0, DoGenPal);
			g.ShowDialog();
		}

		private byte CalcOctet(int x, int y, int maxLen) {
			byte octet = 0;
			int tx = Cpc.CalcTx(y);
			for (int p = 0; p < Math.Min(8, maxLen); p++)
				if ((p % tx) == 0) {
					int pen = Cpc.GetPenColor(imgMotif, (x << 3) + p, y);
					octet |= (byte)(Cpc.tabOctetMode[pen] >> (p / tx));
				}
			return octet;
		}

		private byte[] MakeWin(bool rowMode = false) {
			int realWidth = (imgMotif.Width >> 3) + ((imgMotif.Width % 8) > 0 ? 1 : 0);
			byte[] ret = new byte[(realWidth * imgMotif.Height) >> 1];
			int pos = 0;
			if (rowMode) {
				for (int x = 0; x < realWidth; x++)
					for (int y = 0; y < imgMotif.Height; y += 2)
						ret[pos++] = CalcOctet(x, y, imgMotif.Width);
			}
			else {
				for (int y = 0; y < imgMotif.Height; y += 2)
					for (int x = 0; x < realWidth; x++)
						ret[pos++] = CalcOctet(x, y, imgMotif.Width - (x << 3));
			}
			return ret;
		}

		private void BpSaveWin_Click(object sender, EventArgs e) {
			Enabled = false;
			SaveFileDialog dlg = new SaveFileDialog { Filter = " (.win)|*.win| Raw assembler data (.asm)|*.asm|Raw assembler data in column order (.asm)|*.asm" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				byte[] bWin;
				string fileName = dlg.FileName;
				switch (dlg.FilterIndex) {
					case 1:
						bWin = MakeWin();
						FileStream fileScr = new FileStream(fileName, FileMode.Create, FileAccess.Write);
						CpcAmsdos entete = Cpc.CreeEntete(fileName, 0x4000, (short)(bWin.Length + 4), -13622);
						fileScr.Write(Cpc.AmsdosToByte(entete), 0, 128);
						fileScr.Write(bWin, 0, bWin.Length);
						byte[] endFile = new byte[4];
						endFile[0] = (byte)(imgMotif.Width & 0xFF);
						endFile[1] = (byte)(imgMotif.Width >> 8);
						endFile[2] = (byte)((imgMotif.Height >> 1) & 0xFF);
						endFile[3] = (byte)(imgMotif.Height >> 9);
						fileScr.Write(endFile, 0, 4);
						fileScr.Close();
						break;

					case 2:
						bWin = MakeWin();
						StreamWriter sw1 = SaveAsm.OpenAsm(fileName, main.lblInfoVersion.Text);
						SaveAsm.GenereDatas(sw1, bWin, bWin.Length, (imgMotif.Width + 7) >> 3);
						SaveAsm.CloseAsm(sw1);
						break;

					case 3:
						bWin = MakeWin(true);
						StreamWriter sw2 = SaveAsm.OpenAsm(fileName, main.lblInfoVersion.Text);
						SaveAsm.GenereDatas(sw2, bWin, bWin.Length, imgMotif.Height >> 1);
						SaveAsm.CloseAsm(sw2);
						break;
				}
			}
			Enabled = true;
		}

		private void BpLoadWin_Click(object sender, EventArgs e) {
			Enabled = false;
			OpenFileDialog dlg = new OpenFileDialog { Filter = " (*.win)|*.win" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				FileStream fileScr = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
				int l = (int)fileScr.Length;
				byte[] tabRead = new byte[l];
				fileScr.Read(tabRead, 0, l);
				fileScr.Close();
				if (Cpc.CheckAmsdos(tabRead)) {
					int width = tabRead[l - 4] + (tabRead[l - 3] << 8);
					int realWidth = (width >> 3) + ((width % 8) > 0 ? 1 : 0);
					imgMotif = new BitmapCpc(tabRead, 128).DrawBitmap(realWidth, tabRead[l - 2], width, true);
					if (zoom != 1) {
						zoom = 1;
						DoZoom();
					}
					imgCopy = new DirectBitmap(BmpLock.Width, BmpLock.Height);
					imgCopy.CopyBits(BmpLock);
					pictureBox.Image = imgCopy.Bitmap;
					pictureBox.Refresh();
				}
			}
			Enabled = true;
			bpSaveWin.Enabled = imgMotif != null;
			while (PeekMessage(out NativeMessage msg, (System.IntPtr)0, WM_LBUTTONUP, WM_LBUTTONUP, PM_REMOVE) != 0)
				;
		}

		private void CopyToClipBoard() {
			DirectBitmap bmpTmp = new DirectBitmap(Cpc.TailleX, Cpc.TailleY);
			for (int x = 0; x < Cpc.TailleX; x++)
				for (int y = 0; y < Cpc.TailleY; y++)
					bmpTmp.SetPixel(x, y, BmpLock.GetPixel(x, y));

			if (chkAfficheSH.Checked) {
				Bitmap newBmp = DrawSpritesHard(pictureBox, bmpTmp.Bitmap);
				Clipboard.SetImage(newBmp);
				newBmp.Dispose();
			}
			else
				Clipboard.SetImage(bmpTmp.Bitmap);

			bmpTmp.Dispose();
		}

		private void ChkTilesGrid_CheckedChanged(object sender, EventArgs e) {
			bpSaveTiles.Enabled = tilesSizeX.Enabled = tilesSizeY.Enabled = main.Enabled = !chkTilesGrid.Checked;
			Render(true, false, !chkTilesGrid.Checked);
		}

		private void BpCopyImage_Click(object sender, EventArgs e) {
			CopyToClipBoard();
			Render();
		}

		private void BpSaveFnt_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog { Filter = "Bitmapfont (*.asm)|*.asm" };
			if (dlg.ShowDialog() == DialogResult.OK)
				SauveFont(dlg.FileName);
		}
	}
}
