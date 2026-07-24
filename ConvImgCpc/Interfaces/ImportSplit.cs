using System;
using System.Drawing;
using System.Drawing.Drawing2D;
using System.Drawing.Imaging;
using System.IO;
using System.Windows.Forms;

namespace ConvImgCpc {
    public partial class ImportSplit : Form {
        private readonly SplitEcran splitEcran;
        private readonly DirectBitmap bmpSplit = new DirectBitmap(768, 544);

        private const int SEUIL_LUM_1 = 85;     // 0x40;
        private const int SEUIL_LUM_2 = 170;    // 0x80;

        public ImportSplit(SplitEcran spl) {
            splitEcran = spl;
            InitializeComponent();
        }

        private void AddErr(string err) {
            listErr.Items.Add(err);
            listErr.SelectedIndex = listErr.Items.Count - 1;
        }

        private void ValideSplit() {
            listErr.Items.Clear();
            int lastLineWrite = 0;
            for (int y = 0; y < 272; y++) {
                int numSplit = 0, longSplit = 0;
                RvbColor p = bmpSplit.GetPixelColor(0, y << 1);
                int curCol = (p.r > SEUIL_LUM_2 ? 6 : p.r > SEUIL_LUM_1 ? 3 : 0) + (p.b > SEUIL_LUM_2 ? 2 : p.b > SEUIL_LUM_1 ? 1 : 0) + (p.v > SEUIL_LUM_2 ? 18 : p.v > SEUIL_LUM_1 ? 9 : 0);
                LigneSplit lSplit = splitEcran.LignesSplit[y];
                for (int i = 0; i < 7; i++) {
                    if (lSplit.ListeSplit.Count <= i)
                        lSplit.ListeSplit.Add(new Split());

                    lSplit.ListeSplit[i].enable = false;
                    lSplit.ListeSplit[i].longueur = 32;
                }

                lSplit.numPen = (int)numPen.Value;
                for (int x = 0; x < 96; x++) {
                    int posY = y << 1;
                    int posX = (x << 3) + (BitmapCpc.retardMin << 1);
                    if (posX < 768 && posX >= 0) {
                        p = bmpSplit.GetPixelColor(posX, posY);
                        // Recherche la couleur cpc la plus proche
                        int indexChoix = (p.r > SEUIL_LUM_2 ? 6 : p.r > SEUIL_LUM_1 ? 3 : 0) + (p.b > SEUIL_LUM_2 ? 2 : p.b > SEUIL_LUM_1 ? 1 : 0) + (p.v > SEUIL_LUM_2 ? 18 : p.v > SEUIL_LUM_1 ? 9 : 0);
                        if (indexChoix != curCol && longSplit >= 32) {
                            lSplit.ListeSplit[numSplit].couleur = curCol;
                            lSplit.ListeSplit[numSplit].longueur = longSplit;
                            lSplit.ListeSplit[numSplit].enable = true;
                            curCol = indexChoix;
                            longSplit = 0;
                            if (numSplit++ >= 6) {
                                AddErr("Plus de 7 splits trouvés sur la ligne " + y + ", seulement 7 seront pris en compte.");
                                x = 96;
                            }
                        }
                        else
                            if (indexChoix != curCol && longSplit > 0)
                            AddErr("Split de longueur inférieure à 32 trouvé sur la ligne " + y + ", la longueur sera ramenée à 32 pixels.");

                        longSplit += 4;
                    }
                }
                if (numSplit < 7) {
                    bool sameLine = y > 0;
                    for (int k = y - 1; k-- > lastLineWrite;) {
                        for (int j = 6; --j >= 0;)
                            if (splitEcran.LignesSplit[k].ListeSplit[j].enable && splitEcran.LignesSplit[k].ListeSplit[j].couleur != curCol) {
                                sameLine = false;
                                break;
                            }
                        if (!sameLine)
                            break;
                    }
                    if (longSplit >= 32 && (y == 0 || numSplit > 0 || !sameLine)) {
                        lSplit.ListeSplit[numSplit].couleur = curCol;
                        lSplit.ListeSplit[numSplit].longueur = longSplit;
                        lSplit.ListeSplit[numSplit].enable = true;
                        lastLineWrite = y;
                    }
                }
            }
        }

        private void BpLire_Click(object sender, EventArgs e) {
            OpenFileDialog dlg = new OpenFileDialog { Filter = "Images (*.bmp, *.gif, *.png, *.jpg)|*.bmp;*.gif;*.png;*.jpg|Tous fichiers|*.*" };
            DialogResult result = dlg.ShowDialog();
            if (result == DialogResult.OK) {
                try {
                    FileStream file = new FileStream(dlg.FileName, FileMode.Open, FileAccess.Read);
                    byte[] tabBytes = new byte[file.Length];
                    file.Read(tabBytes, 0, tabBytes.Length);
                    file.Close();
                    bool bitmapOk = false;
                    try {
                        MemoryStream ms = new MemoryStream(tabBytes);
                        Bitmap bmpRead = new Bitmap(ms);
                        bmpRead = bmpRead.Clone(new Rectangle(0, 0, bmpRead.Width, bmpRead.Height), PixelFormat.Format32bppArgb);
                        if (bmpRead.Width == 384 && bmpRead.Height == 272) {
                            Graphics g = Graphics.FromImage(bmpSplit.Bitmap);
                            g.DrawImage(bmpRead, 0, 0, 768, 544);
                            for (int y = 0; y < 272; y++)
                                for (int x = 0; x < 384; x++) {
                                    RvbColor p = bmpSplit.GetPixelColor(x, y);
                                    int indexChoix = (p.r > SEUIL_LUM_2 ? 6 : p.r > SEUIL_LUM_1 ? 3 : 0) + (p.b > SEUIL_LUM_2 ? 2 : p.b > SEUIL_LUM_1 ? 1 : 0) + (p.v > SEUIL_LUM_2 ? 18 : p.v > SEUIL_LUM_1 ? 9 : 0);
                                    bmpSplit.SetPixel(x, y, BitmapCpc.RgbCPC[indexChoix].GetColor);
                                }
                            g.SmoothingMode = SmoothingMode.None;
                            g.InterpolationMode = InterpolationMode.NearestNeighbor;
                            g.PixelOffsetMode = PixelOffsetMode.HighQuality;
                            g.CompositingQuality = CompositingQuality.AssumeLinear;
                            g.DrawImage(bmpRead, new Rectangle(0, 0, 768, 544));
                            pictureSplit.Image = bmpSplit.Bitmap;
                            bitmapOk = true;
                            ValideSplit();
                        }
                        else
                            MessageBox.Show("L'image doit avoir une dimension de 384 pixels par 272");

                        ms.Dispose();
                    }
                    catch (Exception ex) {
                        MessageBox.Show("Impossible de lire l'image (format inconnu ???)");
						MessageBox.Show(ex.Message, ex.StackTrace);
					}
					if (bitmapOk) {
                    }
                }
                catch (Exception ex) {
                    MessageBox.Show(ex.StackTrace, ex.Message);
                }
            }
        }
    }
}
