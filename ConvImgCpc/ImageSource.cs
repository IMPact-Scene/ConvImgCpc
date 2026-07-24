using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;

namespace ConvImgCpc {
	public partial class ImageSource {
		private readonly List<Bitmap> tabImage = new List<Bitmap>();
		public Bitmap GetImage { get { return tabImage.Count == 0 ? null : tabImage[imageSel < tabImage.Count ? imageSel : tabImage.Count - 1]; } }
		public int NbImg { get { return tabImage.Count; } }
		private int imageSel = 0;
		public int tpsFrame = 0;

		public ImageSource() {
		}

		public void Init() {
			tabImage.Add(new Bitmap(768, 544));
		}

		private void ClearAll() {
			foreach (Bitmap b in tabImage)
				b.Dispose();

			tabImage.Clear();
		}

		public void InitBitmap(Bitmap b) {
			ClearAll();
			tabImage.Add(new Bitmap(b));
		}

		public void InitBitmap(MemoryStream imageStream) {
			ClearAll();
			Image selImage = new Bitmap(imageStream);
			FrameDimension dimension = new FrameDimension(selImage.FrameDimensionsList[0]);
			int nbImg = selImage.GetFrameCount(dimension);
			tpsFrame = nbImg > 1 ? BitConverter.ToInt32(selImage.GetPropertyItem(20736).Value, 0) * 10 : 0;
			for (int n = 0; n < nbImg; n++) {
				selImage.SelectActiveFrame(dimension, n);
				tabImage.Add(new Bitmap(selImage));
			}
		}

		public void InitBitmap(int nbImg) {
			ClearAll();
			for (int n = 0; n < nbImg; n++)
				tabImage.Add(new Bitmap(768, 544));
		}

		public void ImportBitmap(Bitmap bmp, int numImage) {
			tabImage[numImage] = new Bitmap(bmp);
		}

		public Bitmap GetBitmap(int numImage) {
			return tabImage[numImage < tabImage.Count ? numImage : tabImage.Count - 1];
		}

		public void DeleteImage(int numImage) {
			tabImage.RemoveAt(numImage);
		}

		public void SelectBitmap(int num) {
			imageSel = num;
		}
	}
}
