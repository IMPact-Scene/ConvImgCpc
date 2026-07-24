using System;
using System.Drawing;
using System.IO;
using System.Windows.Forms;
using System.Xml.Serialization;

namespace ConvImgCpc {
	public partial class ImageCpc : Form {
		private int numSpriteEdit = 0;
		private PosSprite posSprite = new PosSprite();
		Bitmap tmp;

		public Bitmap DrawSpritesHard(PictureBox pict, Bitmap source = null) {
			if (pict != null)
				pict.Image = tmp;

			Bitmap bmp = new Bitmap(source ?? pict.Image);
			int numBank = 0;
			for (int spr = 0; spr < 16; spr++) {
				if (posSprite.zoomXSprite[spr] != 0 && posSprite.zoomYSprite[spr] != 0) {
					int taillex = posSprite.zoomXSprite[spr];
					int tailley = posSprite.zoomYSprite[spr] << 1;
					int ofsx = posSprite.posXSprite[spr];
					int ofsy = posSprite.posYSprite[spr] << 1;
					for (int y = 0; y < 16; y++) {
						for (int x = 0; x < 16; x++) {
							for (int zx = 0; zx < taillex; zx++)
								for (int zy = 0; zy < tailley; zy++) {
									if (zx + (x * taillex) + ofsx < Cpc.TailleX && zy + (y * tailley) + ofsy < Cpc.TailleY) {
										byte col = Cpc.spritesHard[numBank, spr, x, y];
										if (col != 0) {
											RvbColor c = Cpc.GetColor(Cpc.paletteSprite[col & 0x0F]);
											bmp.SetPixel(zx + (x * taillex) + ofsx, zy + (y * tailley) + ofsy, Color.FromArgb(c.GetColorArgb));
										}
									}
								}
						}
					}
				}
			}
			if (source == null)
				pict.Image = bmp;

			return bmp;
		}

		private void ChkAfficheSH_CheckedChanged(object sender, EventArgs e) {
			if (chkAfficheSH.Checked)
				tmp = new Bitmap(pictureBox.Image);
			else
				tmp.Dispose();

			Render(true);
		}

		private void NumSprite_ValueChanged(object sender, EventArgs e) {
			numSpriteEdit = (int)numSprite.Value;
			numZoomX.Value = posSprite.zoomXSprite[numSpriteEdit];
			numZoomY.Value = posSprite.zoomYSprite[numSpriteEdit];
			numPosX.Value = posSprite.posXSprite[numSpriteEdit];
			numPosY.Value = posSprite.posYSprite[numSpriteEdit];
		}

		private void NumZoomX_ValueChanged(object sender, EventArgs e) {
			posSprite.zoomXSprite[numSpriteEdit] = (byte)numZoomX.Value;
			if (chkCopyZoom.Checked)
				for (int i = numSpriteEdit + 1; i < 16; i++)
					posSprite.zoomXSprite[i] = posSprite.zoomXSprite[numSpriteEdit];

			Render(true);
		}

		private void NumZoomY_ValueChanged(object sender, EventArgs e) {
			posSprite.zoomYSprite[numSpriteEdit] = (byte)numZoomY.Value;
			if (chkCopyZoom.Checked)
				for (int i = numSpriteEdit + 1; i < 16; i++)
					posSprite.zoomYSprite[i] = posSprite.zoomYSprite[numSpriteEdit];

			Render(true);
		}

		private void NumPosX_ValueChanged(object sender, EventArgs e) {
			posSprite.posXSprite[numSpriteEdit] = (int)numPosX.Value;
			Render(true);
		}

		private void NumPosY_ValueChanged(object sender, EventArgs e) {
			posSprite.posYSprite[numSpriteEdit] = (int)numPosY.Value;
			Render(true);
		}

		private void BpCopiePosSprites_Click(object sender, EventArgs e) {
			string posTxt = "";
			for (int i = 0; i < 16; i++) {
				posTxt += ";\tSprite " + i.ToString() + Environment.NewLine;
				posTxt += "\tDW\t" + posSprite.posXSprite[i].ToString() + "," + posSprite.posYSprite[i].ToString() + "\t\t; position" + Environment.NewLine;
				string z = (posSprite.zoomXSprite[i] * 4 + posSprite.zoomYSprite[i]).ToString();
				posTxt += "\tDB\t" + z + "," + z + "," + z + "," + z + "\t\t; zoom" + Environment.NewLine;
			}
			Clipboard.SetText(posTxt);
			MessageBox.Show("Position des sprites copié(es) dans le presse-papier");
		}

		private void BpReadCoord_Click(object sender, EventArgs e) {
			var dlg = new OpenFileDialog { Filter = "Fichier XML (*.xml)|*.xml" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				FileStream fileParam = File.Open(dlg.FileName, FileMode.Open);
				try {
					posSprite = (PosSprite)new XmlSerializer(typeof(PosSprite)).Deserialize(fileParam);
					numZoomX.Value = posSprite.zoomXSprite[numSpriteEdit];
					numZoomY.Value = posSprite.zoomYSprite[numSpriteEdit];
					numPosX.Value = posSprite.posXSprite[numSpriteEdit];
					numPosY.Value = posSprite.posYSprite[numSpriteEdit];
				}
				catch (Exception ex) {
					MessageBox.Show("Erreur lors de la lecture des positions...");
					MessageBox.Show(ex.Message, ex.StackTrace);
				}
				fileParam.Close();
			}
		}

		private void BpSaveCoord_Click(object sender, EventArgs e) {
			SaveFileDialog dlg = new SaveFileDialog { Filter = "Fichier XML (*.xml)|*.xml" };
			if (dlg.ShowDialog() == DialogResult.OK) {
				FileStream file = File.Open(dlg.FileName, FileMode.Create);
				try {
					new XmlSerializer(typeof(PosSprite)).Serialize(file, posSprite);
				}
				catch {
					MessageBox.Show("Erreur lors de la sauvegarde des positions...");
				}
				file.Close();
			}
		}
	}

	[System.Serializable]
	public class PosSprite {
		public int[] posXSprite = new int[16];
		public int[] posYSprite = new int[16];
		public byte[] zoomXSprite = new byte[16];
		public byte[] zoomYSprite = new byte[16];

	}
}
