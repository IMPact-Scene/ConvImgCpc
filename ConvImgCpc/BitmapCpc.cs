using System;
using System.Windows.Forms;

namespace ConvImgCpc {
	public class BitmapCpc : Cpc {
		private const int maxColsCpc = 96;
		private const int maxLignesCpc = 272;

		static private readonly byte[] bufTmp = new byte[0x10000];
		public byte[] bmpCpc = new byte[0x10000];
		public byte[] imgAscii = new byte[0x1000];
		public int[] tabMode = new int[272];

		public SplitEcran splitEcran = new SplitEcran();
		public int[,,] splitPalette = new int[96, 272, 17];
		public const int retardMin = -4; // ### 4;

		public bool isCalc = false;

		public BitmapCpc() {
		}

		public BitmapCpc(byte[] source, int offset) {
			Array.Copy(source, offset, bmpCpc, 0, source.Length - offset);
		}

		public BitmapCpc(byte[] source, int tx, int ty) {
			Array.Copy(source, bmpCpc, source.Length);
			TailleX = tx;
			TailleY = ty;
		}

		public BitmapCpc(int tx, int ty, int mode) {
			nbCol = tx >> 3;
			nbLig = ty >> 1;
			for (int y = 0; y < 272; y++) {
				tabMode[y] = mode;
				LigneSplit l = new LigneSplit { retard = retardMin };
				for (int j = 0; j < 7; j++)
					l.ListeSplit.Add(new Split());

				splitEcran.LignesSplit.Add(l);
				for (int x = 0; x < 96; x++)
					for (int i = 0; i < 16; i++)
						splitPalette[x, y, i] = Palette[i]; // ### a reconstruire avec les splits ?
			}
		}

		private int AppliquePalette(int x, int y, int xpos, int[] curPal) {
			for (; x < xpos; x++)
				for (int i = 0; i < 16; i++)
					splitPalette[x, y, i] = curPal[i];

			return x;
		}

		public void CalcPaletteSplit() {
			int[] curPal = new int[16];
			for (int i = 0; i < 16; i++)
				curPal[i] = splitPalette[0, 0, i];

			// Raz splits
			for (int y = 0; y < 272; y++)
				for (int x = 0; x < 96; x++)
					for (int i = 0; i < 16; i++)
						splitPalette[x, y, i] = curPal[i];

			for (int y = 0; y < 272; y++) {
				LigneSplit lSpl = splitEcran.LignesSplit[y];
				int numCol = lSpl.numPen;
				int xpos = lSpl.retard >> 2;
				// de x à xpos => faire palette = curPal
				int x = AppliquePalette(0, y, xpos, curPal);
				for (int ns = 0; ns < 7; ns++) {
					Split s = lSpl.GetSplit(ns);
					if (s.enable) {
						xpos += s.longueur >> 2;
						curPal[numCol] = s.couleur;
						if (xpos > 96)
							xpos = 96;

						// de x à xpos => faire palette = curPal
						x = AppliquePalette(x, y, xpos, curPal);
					}
					else
						break;
				}
				// Terminer ligne
				AppliquePalette(x, y, 96, curPal);
				if (y < 271 && lSpl.changeMode)
					tabMode[y + 1] = lSpl.newMode;
			}
		}

		public RvbColor GetColorPal(int palEntry) {
			return GetColor(Palette[palEntry]);
		}

		private void SetPalette(byte[] palStart, int startAdr, bool plus) {
			modeVirtuel = palStart[startAdr] & 0x03;
			for (int i = 0; i < 16; i++)
				Palette[i] = plus ? ((palStart[startAdr + 1 + (i << 1)] << 4) & 0xF0) + (palStart[startAdr + 1 + (i << 1)] >> 4) + (palStart[startAdr + 2 + (i << 1)] << 8) : palStart[startAdr + i + 1];
		}

		public RvbColor GetPaletteColor(int x, int y, int col) {
			return RgbCPC[splitPalette[x, y, col] < 27 ? splitPalette[x, y, col] : 0];
		}


		private bool InitDatas() {
			bool Ret = false;
			// Si sauvegardé avec ConvImgCpc, alors la palette se trouve dans l'image...
			// CPC OLD, écran standard
			if (bmpCpc[0x7D0] == 0x3A && bmpCpc[0x7D1] == 0xD0 && bmpCpc[0x7D2] == 0xD7 && bmpCpc[0x7D3] == 0xCD) {
				cpcPlus = false;
				nbCol = 80;
				nbLig = 200;
				SetPalette(bmpCpc, 0x17D0, cpcPlus);
				Ret = true;
			}
			else
				// CPC +, écran standard
				if (bmpCpc[0x7D0] == 0xF3 && bmpCpc[0x7D1] == 0x01 && bmpCpc[0x7D2] == 0x11 && bmpCpc[0x7D3] == 0xBC) {
					cpcPlus = true;
					nbCol = 80;
					nbLig = 200;
					SetPalette(bmpCpc, 0x17D0, cpcPlus);
					Ret = true;
				}
				else
					// CPC OLD, écran overscan
					if (bmpCpc[0x611] == 0x21 && bmpCpc[0x612] == 0x47 && bmpCpc[0x613] == 0x08 && bmpCpc[0x614] == 0xCD) {
						cpcPlus = false;
						nbCol = maxColsCpc;
						nbLig = maxLignesCpc;
						SetPalette(bmpCpc, 0x600, cpcPlus);
						Ret = true;
					}
					else
						// CPC +, écran overscan
						if (bmpCpc[0x621] == 0xF3 && bmpCpc[0x622] == 0x01 && bmpCpc[0x623] == 0x11 && bmpCpc[0x624] == 0xBC) {
							cpcPlus = true;
							nbCol = maxColsCpc;
							nbLig = maxLignesCpc;
							SetPalette(bmpCpc, 0x600, cpcPlus);
							Ret = true;
						}
			return (Ret);
		}

		private void DepactOCP() {
			int PosIn = 0, PosOut = 0;

			bmpCpc.CopyTo(bufTmp, 0x10000);
			while (PosOut < 0x4000) {
				if (bufTmp[PosIn] == 'M' && bufTmp[PosIn + 1] == 'J' && bufTmp[PosIn + 2] == 'H') {
					PosIn += 3;
					int LgOut = bufTmp[PosIn++];
					LgOut += (bufTmp[PosIn++] << 8);
					int CntBlock = 0;
					while (CntBlock < LgOut) {
						if (bufTmp[PosIn] == 'M' && bufTmp[PosIn + 1] == 'J' && bufTmp[PosIn + 2] == 'H')
							break;

						byte a = bufTmp[PosIn++];
						if (a == 1) { // MARKER_OCP
							int c = bufTmp[PosIn++];
							a = bufTmp[PosIn++];
							if (c == 0)
								c = 0x100;

							for (int i = 0; i < c && CntBlock < LgOut; i++) {
								bmpCpc[PosOut++] = a;
								CntBlock++;
							}
						}
						else {
							bmpCpc[PosOut++] = a;
							CntBlock++;
						}
					}
				}
				else
					PosOut = 0x4000;
			}
		}

		private void DepactPK() {
			byte[] Palette = new byte[0x100];

			// Valeurs par défaut
			cpcPlus = false;
			nbCol = 80;
			nbLig = 200;

			//
			//PKSL -> 320x200 STD
			//PKS3 -> 320x200 Mode 3
			//PKSP -> 320x200 PLUS
			//PKVL -> Overscan STD
			//PKVP -> Overscan PLUS
			//

			cpcPlus = (bmpCpc[3] == 'P') || (bmpCpc[2] == 'O');
			bool Overscan = (bmpCpc[2] == 'V') || (bmpCpc[3] == 'V');
			bool Std = (bmpCpc[2] == 'S' && bmpCpc[3] == 'L');
			if (Std)
				for (int i = 0; i < 17; i++)
					Palette[i] = bmpCpc[i + 4];

			int l = new PackModule().Depack(bmpCpc, Std ? 21 : 4, bufTmp);
			if (Std) {
				int i = 0;
				for (int x = 0; x < 80; x++)
					for (int y = 0; y < 200; y++)
						bmpCpc[x + GetAdrCpc(y << 1)] = bufTmp[i++];
			}
			else
				Array.Copy(bufTmp, bmpCpc, l);
			if (Overscan) {
				nbCol = maxColsCpc;
				nbLig = maxLignesCpc;
				SetPalette(bmpCpc, 0x600, cpcPlus);
			}
			else {
				if (Std)
					SetPalette(Palette, 0, cpcPlus);
				else
					SetPalette(bmpCpc, 0x17D0, cpcPlus);
			}
		}

		public byte GetByteCpc(DirectBitmap bmpLock, int x, int y, int tx) {
			byte octet = 0, decal = 0;
			for (int p = 0; p < 8; p += tx) {
				int pen = GetPenColor(bmpLock, x + p, y);
				octet |= (byte)(tabOctetMode[pen] >> (decal++));
			}
			return octet;
		}

		public void CreeBmpCpc(DirectBitmap bmpLock, bool egx = false, int lignestart = 0) {
			Array.Clear(bmpCpc, 0, bmpCpc.Length);
			for (int y = 0; y < TailleY; y += 2) {
				int adrCPC = GetAdrCpc(y);
				int tx = CalcTx(y);
				for (int x = 0; x < TailleX; x += 8) {
					if (!egx || ((y >> 1) & 1) == lignestart) {
						bmpCpc[adrCPC + (x >> 3)] = GetByteCpc(bmpLock, x, y, tx);
						//byte octet = 0, decal = 0;
						//for (int p = 0; p < 8; p += tx) {
						//	int pen = GetPenColor(bmpLock, x + p, y);
						//	octet |= (byte)(tabOctetMode[pen] >> (decal++));
						//}
						//bmpCpc[adrCPC + (x >> 3)] = octet;
					}
				}
			}
		}

		public void CreeBmpCpcForceMode1(DirectBitmap bmpLock) {
			Array.Clear(bmpCpc, 0, bmpCpc.Length);
			for (int y = 0; y < TailleY; y += 2) {
				int adrCPC = GetAdrCpc(y);
				int tx = CalcTx(y);
				int maxPen = MaxPen(y);
				for (int x = 0; x < TailleX; x += 8) {
					byte pen, octet = 0, decal = 0;
					for (int p = 0; p < 8; p += tx) {
						RvbColor col = bmpLock.GetPixelColor(x + p, y);
						for (pen = 0; pen < maxPen; pen++) {
							if (cpcPlus) {
								if ((col.v >> 4) == (Palette[pen] >> 8) && (col.r >> 4) == ((Palette[pen] >> 4) & 0x0F) && (col.b >> 4) == (Palette[pen] & 0x0F))
									break;
							}
							else {
								RvbColor fixedCol = RgbCPC[Palette[pen]];
								if (fixedCol.r == col.r && fixedCol.b == col.b && fixedCol.v == col.v)
									break;
							}
						}
						octet |= (byte)(tabOctetMode[Math.Min(3, (int)pen)] >> (decal++));
					}
					bmpCpc[adrCPC + (x >> 3)] = octet;
				}
			}
		}

		private void CreeImgAscii(DirectBitmap bmpLock) {
			int l = 0;
			for (int y = 0; y < NbLig << 1; y += 16)
				for (int x = 0; x < NbCol; x++) {
					switch (modeVirtuel) {
						case 8: // ASC0
							imgAscii[l++] = (byte)(GetPenColor(bmpLock, x << 3, y) | (GetPenColor(bmpLock, x << 3, y + 8) << 4));
							break;

						case 9: // ASC1
							imgAscii[l++] = (byte)(GetPenColor(bmpLock, x << 3, y) | (GetPenColor(bmpLock, (x << 3) + 4, y) << 2)
												| (GetPenColor(bmpLock, x << 3, y + 8) << 4) | (GetPenColor(bmpLock, (x << 3) + 4, y + 8) << 6));
							break;

						case 10: // ASC2
							imgAscii[l++] = (byte)(GetPenColor(bmpLock, x << 3, y) | (GetPenColor(bmpLock, (x << 3) + 2, y) << 2)
												| (GetPenColor(bmpLock, (x << 3) + 4, y) << 1) | (GetPenColor(bmpLock, (x << 3) + 6, y) << 3)
												| (GetPenColor(bmpLock, x << 3, y + 8) << 4) | (GetPenColor(bmpLock, (x << 3) + 2, y + 8) << 6)
												| (GetPenColor(bmpLock, (x << 3) + 4, y + 8) << 5) | (GetPenColor(bmpLock, (x << 3) + 6, y + 8) << 7));
							break;
					}
				}
		}

		private int GetAsciiMat(DirectBitmap bmpLock, int x, int y) {
			for (int i = 0; i < 16; i++) {
				bool found = true;
				for (int ym = 0; ym < 4; ym++) {
					for (int xm = 0; xm < 4; xm++) {
						RvbColor pix = bmpLock.GetPixelColor(x + (xm << 1), y + (ym << 1));
						RvbColor c = GetColorPal(trameM1[i, xm, ym]);
						if (c.r != pix.r || c.v != pix.v || c.b != pix.b) {
							found = false;
							break;
						}
					}
				}
				if (found)
					return i;
			}
			return 0;
		}

		private void CreeImgAsciiMat(DirectBitmap bmpLock) {
			int l = 0;
			for (int y = 0; y < NbLig << 1; y += 16)
				for (int x = 0; x < NbCol; x++)
					imgAscii[l++] = (byte)(GetAsciiMat(bmpLock, x << 3, y) | (GetAsciiMat(bmpLock, x << 3, y + 8) << 4));
		}

		public void ConvertAscii(DirectBitmap bmpLock) {
			if (modeVirtuel == 7)
				CreeImgAsciiMat(bmpLock);
			else
				CreeImgAscii(bmpLock);
		}

		// Rendu d'une image CPC dans un bitmap PC
		public DirectBitmap DrawBitmap(int nbCol, int nbLig, int realWidth = 0, bool isSprite = false, ImageCpc imgCpc = null) {
			int width = realWidth != 0 ? realWidth : nbCol << 3;
			DirectBitmap loc = new DirectBitmap(width, nbLig << 1);
			for (int y = 0; y < nbLig << 1; y += 2) {
				int mode = (modeVirtuel >= 5 ? 1 : modeVirtuel >= 3 ? (y & 2) == yEgx ? modeVirtuel - 2 : modeVirtuel - 3 : modeVirtuel);
				int adrCPC = isSprite ? nbCol * (y >> 1) : GetAdrCpc(y);
				int xBitmap = 0;
				int p0, p1, p2, p3;
				for (int x = 0; x < nbCol; x++) {
					byte octet = bmpCpc[adrCPC + x];
					switch (mode) {
						case 0:
							p0 = (octet >> 7) + ((octet & 0x20) >> 3) + ((octet & 0x08) >> 2) + ((octet & 0x02) << 2);
							p1 = ((octet & 0x40) >> 6) + ((octet & 0x10) >> 2) + ((octet & 0x04) >> 1) + ((octet & 0x01) << 3);
							loc.SetHorLineDouble(xBitmap, y, 4, GetPalCPC(Palette[p0]));
							loc.SetHorLineDouble(xBitmap + 4, y, 4, GetPalCPC(Palette[p1]));
							if (imgCpc != null) {
								imgCpc.SetPixelCpc(xBitmap, y, p0, 4);
								imgCpc.SetPixelCpc(xBitmap + 4, y, p1, 4);
							}
							xBitmap += 8;
							break;

						case 1:
							p0 = ((octet >> 7) & 1) + ((octet >> 2) & 2);
							p1 = ((octet >> 6) & 1) + ((octet >> 1) & 2);
							p2 = ((octet >> 5) & 1) + ((octet >> 0) & 2);
							p3 = ((octet >> 4) & 1) + ((octet << 1) & 2);
							loc.SetHorLineDouble(xBitmap, y, 2, GetPalCPC(Palette[p0]));
							loc.SetHorLineDouble(xBitmap + 2, y, 2, GetPalCPC(Palette[p1]));
							loc.SetHorLineDouble(xBitmap + 4, y, 2, GetPalCPC(Palette[p2]));
							loc.SetHorLineDouble(xBitmap + 6, y, 2, GetPalCPC(Palette[p3]));
							if (imgCpc != null) {
								imgCpc.SetPixelCpc(xBitmap, y, p0, 2);
								imgCpc.SetPixelCpc(xBitmap + 2, y, p1, 2);
								imgCpc.SetPixelCpc(xBitmap + 4, y, p2, 2);
								imgCpc.SetPixelCpc(xBitmap + 6, y, p3, 2);
							}
							xBitmap += 8;
							break;

						case 2:
							for (int i = 8; i-- > 0;) {
								p0 = (octet >> i) & 1;
								loc.SetPixel(xBitmap, y, GetPalCPC(Palette[p0]));
								loc.SetPixel(xBitmap++, y + 1, GetPalCPC(Palette[p0]));
								imgCpc?.SetPixelCpc(xBitmap, y, p0, 1);
							}
							break;
					}
				}
			}
			if (imgCpc != null) {
				imgCpc.BitmapCpc.isCalc = true;
				imgCpc.Render();
			}
			return (loc);
		}

		public DirectBitmap Render(DirectBitmap bmp, int offsetX) {
			for (int y = 0; y < (nbLig << 1); y += 2) {
				int adrCPC = (y >> 4) * nbCol + (y & 14) * 0x400;
				if (y > 255 && (nbCol * nbLig > 0x3FFF))
					adrCPC += 0x3800;

				adrCPC += offsetX >> 3;
				int xBitmap = 0;
				for (int x = 0; x < nbCol; x++) {
					byte octet = bmpCpc[adrCPC + x];
					switch (tabMode[y >> 1]) {
						case 0:
							bmp.SetHorLineDouble(xBitmap, y, 4, GetPalCPC(splitPalette[x, y >> 1, (octet >> 7) + ((octet & 0x20) >> 3) + ((octet & 0x08) >> 2) + ((octet & 0x02) << 2)]));
							bmp.SetHorLineDouble(xBitmap + 4, y, 4, GetPalCPC(splitPalette[x, y >> 1, ((octet & 0x40) >> 6) + ((octet & 0x10) >> 2) + ((octet & 0x04) >> 1) + ((octet & 0x01) << 3)]));
							xBitmap += 8;
							break;

						case 1:
						case 3:
							bmp.SetHorLineDouble(xBitmap, y, 2, GetPalCPC(splitPalette[x, y >> 1, ((octet >> 7) & 1) + ((octet >> 2) & 2)]));
							bmp.SetHorLineDouble(xBitmap + 2, y, 2, GetPalCPC(splitPalette[x, y >> 1, ((octet >> 6) & 1) + ((octet >> 1) & 2)]));
							bmp.SetHorLineDouble(xBitmap + 4, y, 2, GetPalCPC(splitPalette[x, y >> 1, ((octet >> 5) & 1) + ((octet >> 0) & 2)]));
							bmp.SetHorLineDouble(xBitmap + 6, y, 2, GetPalCPC(splitPalette[x, y >> 1, ((octet >> 4) & 1) + ((octet << 1) & 2)]));
							xBitmap += 8;
							break;

						case 2:
							for (int i = 8; i-- > 0;) {
								bmp.SetPixel(xBitmap, y, GetPalCPC(splitPalette[x, y >> 1, (octet >> i) & 1]));
								bmp.SetPixel(xBitmap++, y + 1, GetPalCPC(splitPalette[x, y >> 1, (octet >> i) & 1]));
							}
							break;
					}
				}
			}
			return bmp;
		}

		public DirectBitmap CreateImageFromCpc(int length, Param par, bool isSprite = false, ImageCpc imgCpc = null) {
			if (!isSprite) {
				if (bmpCpc[0] == 'M' && bmpCpc[1] == 'J' && bmpCpc[2] == 'H')
					DepactOCP();
				else
					if (bmpCpc[0] == 'P' && bmpCpc[1] == 'K' && (bmpCpc[2] == 'O' || bmpCpc[2] == 'V' || bmpCpc[2] == 'S'))
						DepactPK();
					else {
						if (!InitDatas()) {
							if (length == 16384 || length == 16336) {
								nbCol = 80;
								nbLig = 200;
							}
							else
								if (length < 31000) {
									try {
										int l = new PackModule().Depack(bmpCpc, 0, bufTmp);
										Array.Copy(bufTmp, bmpCpc, l);
										if (!InitDatas()) {
											cpcPlus = false;
											nbCol = maxColsCpc;
											nbLig = maxLignesCpc;
											SetPalette(bmpCpc, 0x600, cpcPlus);
										}
									}
									catch (Exception ex) {
										MessageBox.Show(ex.Message, ex.StackTrace);
									}
								}
								else {
									if (length > 0x4000) {
										nbCol = maxColsCpc;
										nbLig = maxLignesCpc;
									}
								}
						}
					}
			}
			par.cpcPlus = cpcPlus;
			return DrawBitmap(nbCol, nbLig, 0, isSprite, imgCpc);
		}
	}
}
