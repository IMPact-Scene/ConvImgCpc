using System;
using System.Collections.Generic;
using System.Drawing;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class ImageCpc : Form {
		private readonly UndoRedo undo = new UndoRedo();
		private Rendu fenetreRendu;
		private int offsetX = 0, offsetY = 0;
		private int zoom = 1, oldZoom = 1;
		private bool setZoomRect = false, unZoom = false;
		private int zoomRectx, zoomRecty, zoomRectw, zoomRecth;
		private int drawCol = 1, undrawCol = 0;
		private int penWidth = 1;
		private int posx = 0, posy = 0, sizex = 0, sizey = 0;
		private int memoMouseX = 0, memoMouseY = 0;
		private bool movePos = false, moveSize = false;
		private bool doDraw = false;
		private bool setCopyRect = false;
		private int copyRectx, copyRecty, copyRectw, copyRecth;
		private enum EditTool { Draw, Zoom, Copy, Pick, Fill };
		private EditTool editToolMode = EditTool.Draw;
		private DirectBitmap imgMotif;
		private DirectBitmap imgCopy;
		private bool modeImpDraw;

		private void ToolModeDraw(MouseEventArgs e) {
			int incY = Cpc.modeVirtuel >= 8 && Cpc.modeVirtuel < 11 ? 8 : 2;
			int yReel = e != null ? (offsetY + (e.Y / (zoom * (chkX2.Checked ? 2 : 1)))) & -incY : 0;
			int tx = Cpc.CalcTx(yReel);
			int penDraw = drawCol % (Cpc.modeVirtuel == 6 || Cpc.modeVirtuel == 11 ? 16 : 1 << tx);
			RvbColor colDraw = BitmapCpc.GetColorPal(penDraw);
			int penUndraw = undrawCol % (Cpc.modeVirtuel == 6 || Cpc.modeVirtuel == 11 ? 16 : 1 << tx);
			RvbColor colUndraw = BitmapCpc.GetColorPal(penUndraw);
			drawColor.BackColor = Color.FromArgb(colDraw.GetColorArgb);
			drawColor.Text = penDraw.ToString();
			drawColor.ForeColor = (colDraw.r * 9798 + colDraw.v * 19235 + colDraw.b * 3735) > 0x400000 ? Color.Black : Color.White;
			undrawColor.BackColor = Color.FromArgb(colUndraw.GetColorArgb);
			drawColor.Width = undrawColor.Width = 35 * Math.Min(tx, 4);
			undrawColor.Text = penUndraw.ToString();
			undrawColor.ForeColor = (colUndraw.r * 9798 + colUndraw.v * 19235 + colUndraw.b * 3735) > 0x400000 ? Color.Black : Color.White;
			drawColor.Refresh();
			undrawColor.Refresh();
			if (e == null || e.Button == MouseButtons.None) {
				if (doDraw) {
					doDraw = false;
					undo.EndUndoRedo();
				}
			}
			else {
				int pen = e.Button == MouseButtons.Left ? drawCol : undrawCol;
				for (int y = 0; y < penWidth * incY; y += 2) {
					tx = Cpc.CalcTx(yReel);
					int nbCol = Cpc.modeVirtuel == 6 || Cpc.modeVirtuel == 11 ? 16 : 1 << tx;
					int realColor = Cpc.GetPalCPC(Cpc.Palette[pen % nbCol]);
					int yStart = zoom * (yReel - offsetY);
					for (int x = 0; x < penWidth * tx; x += tx) {
						int xReel = (x + offsetX + (e.X / (zoom * (chkX2.Checked ? 2 : 1)))) & -tx;
						if (xReel >= 0 && yReel >= 0 && xReel < Cpc.TailleX && yReel < Cpc.TailleY) {
							undo.MemoUndoRedo(xReel, yReel, BmpLock.GetPixel(xReel, yReel), realColor, doDraw);
							doDraw = true;
							BmpLock.SetHorLineDouble(xReel, yReel, tx, realColor);
							if (zoom != 1)
								for (int yz = yStart; yz < Math.Min(tmpLock.Height, yStart + (zoom << 1)); yz += 2)
									if (yz >= 0)
										tmpLock.SetHorLineDouble(zoom * (xReel - offsetX), yz, zoom * tx, realColor);
						}
					}
					yReel += 2;
				}
				Render();
			}
		}

		private void DoZoom() {
			vScrollBar.Visible = Cpc.TailleY * zoom > pictureBox.Image.Height;
			hScrollBar.Visible = Cpc.TailleX * zoom > pictureBox.Image.Width;
			hScrollBar.Maximum = hScrollBar.LargeChange + Cpc.TailleX - (Cpc.TailleX / zoom);
			vScrollBar.Maximum = vScrollBar.LargeChange + Cpc.TailleY - (Cpc.TailleY / zoom);
			hScrollBar.Value = Math.Max(0, Math.Min(Math.Min(zoomRectx, zoomRectx + zoomRectw), Cpc.TailleX - ((imgOrigine.Width + zoom) / zoom)));
			vScrollBar.Minimum = modeImpDraw ? 2 : 0;
			vScrollBar.Value = Math.Max(modeImpDraw ? 2 : 0, Math.Min(Math.Min(zoomRecty, zoomRecty + zoomRecth), Cpc.TailleY - ((imgOrigine.Height + zoom) / zoom)));
			offsetX = (hScrollBar.Value >> 3) << 3;
			offsetY = (vScrollBar.Value >> 1) << 1;
			lblZoom.Text = zoom.ToString() + ":1";
			oldZoom = zoom;
			Render(true);
		}

		private void ToolModeZoom(MouseEventArgs e) {
			if (e.Button == MouseButtons.Left) {
				if (zoom == 1) {
					if (!setZoomRect) {
						setZoomRect = true;
						zoomRectx = e.X / (chkX2.Checked ? 2 : 1);
						zoomRecty = e.Y / (chkX2.Checked ? 2 : 1);
						zoomRecth = zoomRectw = 0;
					}
					else {
						Graphics g = Graphics.FromImage(pictureBox.Image);
						if (zoomRecth != 0 || zoomRectw != 0) {
							XorDrawing.DrawXorRectangle(g, (Bitmap)pictureBox.Image, zoomRectx, zoomRecty, zoomRectx + zoomRectw, zoomRecty + zoomRecth);
						}
						zoomRectw = (e.X / (chkX2.Checked ? 2 : 1)) - zoomRectx;
						zoomRecth = (e.Y / (chkX2.Checked ? 2 : 1)) - zoomRecty;
						XorDrawing.DrawXorRectangle(g, (Bitmap)pictureBox.Image, zoomRectx, zoomRecty, zoomRectx + zoomRectw, zoomRecty + zoomRecth);
						pictureBox.Refresh();
					}
				}
			}
			else
				if (e.Button == MouseButtons.Right) {
				if (!unZoom) {
					unZoom = true;
					if (zoom >= 2)
						zoom >>= 1;

					DoZoom();
				}
			}
			else {
				unZoom = false;
				if (setZoomRect) {
					setZoomRect = false;
					if (zoomRectw != 0 && zoomRecth != 0) {
						Graphics g = Graphics.FromImage(pictureBox.Image);
						XorDrawing.DrawXorRectangle(g, (Bitmap)pictureBox.Image, zoomRectx, zoomRecty, zoomRectx + zoomRectw, zoomRecty + zoomRecth);
						zoom = Math.Max(1, Math.Min(Math.Abs(768 / zoomRectw), Math.Abs(544 / zoomRecth)) & 0x7E);
						DoZoom();
					}
				}
			}
		}

		private void DrawCopy(MouseEventArgs e) {
			if (imgMotif != null && imgCopy != null) {
				imgCopy.CopyBits(BmpLock);
				int yReel = ((e.Y / (zoom * (chkX2.Checked ? 2 : 1))) & 0xFFE) * zoom;
				int tx = Cpc.CalcTx(yReel);
				int xReel = ((e.X / (zoom * (chkX2.Checked ? 2 : 1))) & -tx) * zoom;
				if (xReel < Cpc.NbCol << 3 && yReel < Cpc.NbLig << 1) {
					Graphics g = Graphics.FromImage(pictureBox.Image);
					g.DrawImage(imgMotif.Bitmap, xReel, yReel);
					pictureBox.Refresh();
				}
			}
		}

		private void ToolModeCopy(MouseEventArgs e) {
			int yReel = ((e.Y / (zoom * (chkX2.Checked ? 2 : 1))) & 0xFFE) * zoom;
			int tx = Cpc.CalcTx(yReel);
			int xReel = ((e.X / (zoom * (chkX2.Checked ? 2 : 1))) & -tx) * zoom;
			if (e.Button == MouseButtons.Right && imgMotif != null)
				ReleaseMotif();

			if (e.Button == MouseButtons.Left && xReel >= 0 && yReel >= 0) {
				if (imgMotif != null) {
					for (int y = 0; y < imgMotif.Height; y += 2) {
						tx = Cpc.CalcTx(y + yReel);
						for (int x = 0; x < imgMotif.Width; x += tx) {
							if (x + xReel < Cpc.NbCol << 3 && y + yReel < Cpc.NbLig << 1) {
								int c = imgMotif.GetPixel(x, y);
								undo.MemoUndoRedo(x + xReel, y + yReel, BmpLock.GetPixel(x + xReel, y + yReel), c, doDraw);
								BmpLock.SetHorLineDouble(x + xReel, y + yReel, tx, c);
								doDraw = true;
							}
						}
					}
					imgCopy.CopyBits(BmpLock);
					Render();
				}
				else {
					if (!setCopyRect) {
						setCopyRect = true;
						copyRectx = xReel;
						copyRecty = yReel;
						copyRecth = copyRectw = 0;
					}
					else {
						Graphics g = Graphics.FromImage(pictureBox.Image);
						if (copyRecth != 0 || copyRectw != 0) {
							XorDrawing.DrawXorRectangle(g, (Bitmap)pictureBox.Image, copyRectx, copyRecty, copyRectx + copyRectw, copyRecty + copyRecth);
						}
						copyRectw = xReel - copyRectx;
						copyRecth = yReel - copyRecty;
						XorDrawing.DrawXorRectangle(g, (Bitmap)pictureBox.Image, copyRectx, copyRecty, copyRectx + copyRectw, copyRecty + copyRecth);
						pictureBox.Refresh();
					}
				}
			}
			else {
				if (setCopyRect && e.Button == MouseButtons.None) {
					setCopyRect = false;
					if (copyRectw != 0 && copyRecth != 0) {
						Graphics g = Graphics.FromImage(pictureBox.Image);
						XorDrawing.DrawXorRectangle(g, (Bitmap)pictureBox.Image, copyRectx, copyRecty, copyRectx + copyRectw, copyRecty + copyRecth);
						imgMotif = new DirectBitmap(Math.Abs(copyRectw / zoom), Math.Abs(copyRecth / zoom));
						bpSaveWin.Enabled = imgMotif != null;
						if (copyRectw < 0)
							copyRectx += copyRectw;

						if (copyRecth < 0)
							copyRecty += copyRecth;

						for (int x = 0; x < imgMotif.Width; x++)
							for (int y = 0; y < imgMotif.Height; y++)
								imgMotif.SetPixel(x, y, BmpLock.GetPixel(offsetX + x + copyRectx / zoom, offsetY + y + copyRecty / zoom));

						if (zoom != 1) {
							zoom = 1;
							DoZoom();
						}
						imgCopy = new DirectBitmap(BmpLock.Width, BmpLock.Height);
						pictureBox.Image = imgCopy.Bitmap;
					}
				}
				if (doDraw) {
					doDraw = false;
					undo.EndUndoRedo();
				}
				DrawCopy(e);
				pictureBox.Refresh();
			}
		}

		private void ToolModePick(MouseEventArgs e) {
			int incY = Cpc.modeVirtuel >= 8 ? 8 : 2;
			int yReel = e != null ? (offsetY + (e.Y / (zoom * (chkX2.Checked ? 2 : 1)))) & -incY : 0;
			int tx = Cpc.CalcTx(yReel);
			int xReel = (offsetX + (e.X / (zoom * (chkX2.Checked ? 2 : 1)))) & -tx;
			RvbColor col = BmpLock.GetPixelColor(xReel, yReel);
			int pen = Cpc.GetPenColor(BmpLock, xReel, yReel);
			if (e.Button == MouseButtons.Left) {
				drawCol = pen;
				drawColor.BackColor = Color.FromArgb(col.r, col.v, col.b);
				drawColor.Text = pen.ToString();
				drawColor.ForeColor = (col.r * 9798 + col.v * 19235 + col.b * 3735) > 0x400000 ? Color.Black : Color.White;
			}
			if (e.Button == MouseButtons.Right) {
				undrawCol = pen;
				undrawColor.BackColor = Color.FromArgb(col.r, col.v, col.b);
				undrawColor.Text = pen.ToString();
				undrawColor.ForeColor = (col.r * 9798 + col.v * 19235 + col.b * 3735) > 0x400000 ? Color.Black : Color.White;
			}
		}

		private void DoFill(int xReel, int yReel, int tx, int fill) {
			Stack<Point> pixels = new Stack<Point>();
			int old = BmpLock.GetPixelColor(xReel, yReel).GetColorArgb;
			if (old != fill) {
				pixels.Push(new Point(xReel, yReel));
				while (pixels.Count > 0) {
					Point a = pixels.Pop();
					if (a.X < Cpc.TailleX && a.X >= 0 && a.Y < Cpc.TailleY && a.Y >= 0 && BmpLock.GetPixelColor(a.X, a.Y).GetColorArgb == old) {
						undo.MemoUndoRedo(a.X, a.Y, BmpLock.GetPixel(a.X, a.Y), fill, doDraw);
						doDraw = true;
						BmpLock.SetHorLineDouble(a.X, a.Y, tx, fill);
						pixels.Push(new Point(a.X - tx, a.Y));
						pixels.Push(new Point(a.X + tx, a.Y));
						pixels.Push(new Point(a.X, a.Y - 2));
						pixels.Push(new Point(a.X, a.Y + 2));
					}
				}
			}
		}

		private void ToolModeFill(MouseEventArgs e) {
			if (e == null || e.Button == MouseButtons.None) {
				if (doDraw) {
					doDraw = false;
					undo.EndUndoRedo();
				}
			}
			else {
				int incY = Cpc.modeVirtuel >= 8 ? 8 : 2;
				int yReel = e != null ? (offsetY + (e.Y / (zoom * (chkX2.Checked ? 2 : 1)))) & -incY : 0;
				int tx = Cpc.CalcTx(yReel);
				int xReel = (offsetX + (e.X / (zoom * (chkX2.Checked ? 2 : 1)))) & -tx;
				if (xReel >= 0 && yReel >= 0 && xReel < Cpc.TailleX && yReel < Cpc.TailleY) {
					int fill = BitmapCpc.GetColorPal(e.Button == MouseButtons.Left ? drawCol : undrawCol % (Cpc.modeVirtuel == 6 || Cpc.modeVirtuel == 11 ? 16 : 1 << tx)).GetColorArgb;
					DoFill(xReel, yReel, tx, fill);
					Render(true);
				}
			}
		}

		private void ReleaseMotif() {
			imgCopy?.Dispose();
			imgCopy = null;
			if (imgMotif != null) {
				imgMotif.Dispose();
				imgMotif = null;
				bpSaveWin.Enabled = false;
			}
			pictureBox.Image = tmpLock != null ? tmpLock.Bitmap : BmpLock.Bitmap;
		}

		// Sur déplacement de la souris...
		private void TrtMouseMove(object sender, MouseEventArgs e) {
			if (modeCaptureSprites.Checked)
				CaptureSprites(e);      // Capture de sprites hard
			else
				if (modeEdition.Checked) {
					int incY = Cpc.modeVirtuel >= 8 && Cpc.modeVirtuel < 11 ? 8 : 2;
					int yReel = (((offsetY + (e.Y / (zoom * (chkX2.Checked ? 2 : 1)))) & -incY) >> 1) - (modeImpDraw ? 1 : 0);
					int tx = Cpc.CalcTx(yReel);
					int xReel = (offsetX + (e.X / (zoom * (chkX2.Checked ? 2 : 1)))) & -tx;
					lblInfoPos.Text = "x:" + xReel.ToString("000") + " y:" + yReel.ToString("000") + " - @:" + ((xReel >> 3) + Cpc.GetAdrCpc(yReel * 2)).ToString("X4");
					if (xReel >= 0 && yReel >= 0) {
						switch (editToolMode) {
							case EditTool.Draw:
								ToolModeDraw(e);
								break;

							case EditTool.Zoom:
								ToolModeZoom(e);
								break;

							case EditTool.Copy:
								ToolModeCopy(e);
								break;

							case EditTool.Pick:
								ToolModePick(e);
								break;

							case EditTool.Fill:
								ToolModeFill(e);
								break;
						}
					}
					if (e.Button == MouseButtons.None) {
						bpUndo.Enabled = undo.CanUndo;
						bpRedo.Enabled = undo.CanRedo;
					}
			}
			else
				MoveOrSize(e);      // Déplacement/Zoom image
		}

		private void VScrollBar_Scroll(object sender, ScrollEventArgs e) {
			offsetY = (vScrollBar.Value >> 1) << 1;
			Render(true);
		}

		private void HScrollBar_Scroll(object sender, ScrollEventArgs e) {
			offsetX = (hScrollBar.Value >> 3) << 3;
			Render(true);
		}

		private void TailleCrayon_SelectedIndexChanged(object sender, EventArgs e) {
			penWidth = int.Parse(tailleCrayon.SelectedItem.ToString());
		}

		private void ModeEdition_CheckedChanged(object sender, System.EventArgs e) {
			ReleaseMotif();
			zoom = 1;
			chkRendu.Checked = false;
			offsetX = offsetY = 0;
			vScrollBar.Visible = hScrollBar.Visible = setZoomRect = false;
			if (modeEdition.Checked) {
				undo.Reset();
				grpEdition.Visible = tailleCrayon.Enabled = true;
				bpUndo.Enabled = bpRedo.Enabled = false;
				TailleCrayon_SelectedIndexChanged(null, null);
				main.param.autoRecalc = main.autoRecalc.Checked = false;
			}
			else {
				CloseRendu();
				grpEdition.Visible = tailleCrayon.Enabled = false;
				Render();
			}
		}

		private void CloseRendu() {
			if (fenetreRendu != null) {
				fenetreRendu.Hide();
				fenetreRendu.Close();
				fenetreRendu.Dispose();
				fenetreRendu = null;
			}
		}

		private void ChkRendu_CheckedChanged(object sender, System.EventArgs e) {
			if (chkRendu.Checked) {
				fenetreRendu = new Rendu(BmpLock.Bitmap);
				fenetreRendu.Show();
				Render();
			}
			else
				CloseRendu();
		}

		private void RbDraw_CheckedChanged(object sender, EventArgs e) {
			bpLoadWin.Enabled = bpSaveWin.Enabled = false;
			ReleaseMotif();
			editToolMode = EditTool.Draw;
		}

		private void RbZoom_CheckedChanged(object sender, EventArgs e) {
			bpLoadWin.Enabled = bpSaveWin.Enabled = false;
			ReleaseMotif();
			editToolMode = EditTool.Zoom;
		}

		private void RbCopy_CheckedChanged(object sender, EventArgs e) {
			bpLoadWin.Enabled = true;
			bpSaveWin.Enabled = imgMotif != null;
			editToolMode = EditTool.Copy;
		}

		private void RbPickColor_CheckedChanged(object sender, EventArgs e) {
			bpLoadWin.Enabled = bpSaveWin.Enabled = false;
			editToolMode = EditTool.Pick;
		}

		private void RbFill_CheckedChanged(object sender, EventArgs e) {
			bpLoadWin.Enabled = bpSaveWin.Enabled = false;
			editToolMode = EditTool.Fill;
		}

		private void BpUndo_Click(object sender, System.EventArgs e) {
			Enabled = false;
			List<MemoPoint> lst = undo.Undo();
			foreach (MemoPoint p in lst) {
				int tx = Cpc.CalcTx(p.posy);
				BmpLock.SetHorLineDouble(p.posx, p.posy, tx, p.oldColor);
			}
			imgCopy?.CopyBits(BmpLock);
			Render(true);
			bpUndo.Enabled = undo.CanUndo;
			bpRedo.Enabled = undo.CanRedo;
			Enabled = true;
		}

		private void BpRedo_Click(object sender, System.EventArgs e) {
			Enabled = false;
			List<MemoPoint> lst = undo.Redo();
			foreach (MemoPoint p in lst) {
				int tx = Cpc.CalcTx(p.posy);
				BmpLock.SetHorLineDouble(p.posx, p.posy, tx, p.newColor);
			}
			imgCopy?.CopyBits(BmpLock);
			Render(true);
			bpUndo.Enabled = undo.CanUndo;
			bpRedo.Enabled = undo.CanRedo;
			Enabled = true;
		}

		// Flip horizontal
		private void BpHorFlip_Click(object sender, EventArgs e) {
			int maxY = Cpc.TailleY >> 1;
			for (int y = 0; y < maxY; y++) {
				int zy = Cpc.TailleY - 1 - y;
				for (int x = 0; x < Cpc.TailleX; x++) {
					int p0 = BmpLock.GetPixel(x, y);
					BmpLock.SetPixel(x, y, BmpLock.GetPixel(x, zy));
					BmpLock.SetPixel(x, zy, p0);
				}
			}

			List<MemoPoint> lst = undo.lstUndoRedo;
			foreach (MemoPoint p in lst)
				p.posy = Cpc.TailleY - 1 - p.posy;

			Render(true);
		}

		// Flip Vertical
		private void BpVerFlip_Click(object sender, EventArgs e) {
			int maxX = Cpc.TailleX >> 1;
			for (int x = 0; x < maxX; x++) {
				int zx = Cpc.TailleX - 1 - x;
				for (int y = 0; y < Cpc.TailleY; y++) {
					int p0 = BmpLock.GetPixel(x, y);
					BmpLock.SetPixel(x, y, BmpLock.GetPixel(zx, y));
					BmpLock.SetPixel(zx, y, p0);
				}
			}

			List<MemoPoint> lst = undo.lstUndoRedo;
			foreach (MemoPoint p in lst)
				p.posx = Cpc.TailleX - 1 - p.posx;

			Render(true);
		}
	}
}
