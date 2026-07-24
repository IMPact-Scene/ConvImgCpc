using System;
using System.IO;
using System.Runtime.InteropServices;

namespace ConvImgCpc {
	public partial class Cpc {
		static public int[] Palette = { 1, 24, 20, 6, 26, 0, 2, 7, 10, 12, 14, 16, 18, 22, 1, 14, 1 };
		static public int[] tabOctetMode = { 0x00, 0x80, 0x08, 0x88, 0x20, 0xA0, 0x28, 0xA8, 0x02, 0x82, 0x0A, 0x8A, 0x22, 0xA2, 0x2A, 0xAA };
		public const int Lum0 = 0x00;
		public const int Lum1 = 0x66;
		public const int Lum2 = 0xFF;
		static public RvbColor[] RgbCPC = {
							new RvbColor( Lum0, Lum0, Lum0),
							new RvbColor( Lum0, Lum0, Lum1),
							new RvbColor( Lum0, Lum0, Lum2),
							new RvbColor( Lum1, Lum0, Lum0),
							new RvbColor( Lum1, Lum0, Lum1),
							new RvbColor( Lum1, Lum0, Lum2),
							new RvbColor( Lum2, Lum0, Lum0),
							new RvbColor( Lum2, Lum0, Lum1),
							new RvbColor( Lum2, Lum0, Lum2),
							new RvbColor( Lum0, Lum1, Lum0),
							new RvbColor( Lum0, Lum1, Lum1),
							new RvbColor( Lum0, Lum1, Lum2),
							new RvbColor( Lum1, Lum1, Lum0),
							new RvbColor( Lum1, Lum1, Lum1),
							new RvbColor( Lum1, Lum1, Lum2),
							new RvbColor( Lum2, Lum1, Lum0),
							new RvbColor( Lum2, Lum1, Lum1),
							new RvbColor( Lum2, Lum1, Lum2),
							new RvbColor( Lum0, Lum2, Lum0),
							new RvbColor( Lum0, Lum2, Lum1),
							new RvbColor( Lum0, Lum2, Lum2),
							new RvbColor( Lum1, Lum2, Lum0),
							new RvbColor( Lum1, Lum2, Lum1),
							new RvbColor( Lum1, Lum2, Lum2),
							new RvbColor( Lum2, Lum2, Lum0),
							new RvbColor( Lum2, Lum2, Lum1),
							new RvbColor( Lum2, Lum2, Lum2)
							};
		static public int[,] colMode5 = new int[272, 16];
		static public string[] modesVirtuels = {
										"Mode 0",				// 0
										"Mode 1",				// 1
										"Mode 2",				// 2
										"Mode EGX1",			// 3
										"Mode EGX2",			// 4
										"Mode X",				// 5
										"Mode <Split>",			// 6
										"Mode ASC-UT",			// 7
										"Mode ASC0",			// 8
										"Mode ASC1",			// 9
										"Mode ASC2",			// 10
										"Capture Sprites"		// 11
										};
		static public string CpcVGA = "TDU\\X]LEMVFW^@_NGORBSZY[JCK";

		static public byte[,,] trameM1 = new byte[16, 4, 4];            // NumTrame,X,Y
		static public byte[,,,] spritesHard = new byte[8, 16, 16, 16];  // NumBank,NumSprite,X,Y
		static public int[] paletteSprite = new int[16];

		static public int modeVirtuel = 1;
		static public bool cpcPlus = false;
		static protected int nbCol = 80;
		static public int NbCol { get { return nbCol; } }
		static public int TailleX {
			get { return nbCol << 3; }
			set { nbCol = value >> 3; }
		}
		static protected int nbLig = 200;
		static public int NbLig { get { return nbLig; } }
		static public int TailleY {
			get { return nbLig << 1; }
			set { nbLig = value >> 1; }
		}
		static public int BitmapSize { get { return nbCol + GetAdrCpc((TailleY & 0x3F8) - 2); } }
		static public int yEgx = 0;

		static public int GetAdrCpc(int y) {
			int adrCPC = (y >> 4) * nbCol + (y & 14) * 0x400;
			if (y > 255 && (nbCol * nbLig > 0x4000))
				adrCPC += 0x3800;

			return adrCPC;
		}

		static public int CalcTx(int y = 0) {
			return 8 >> (modeVirtuel == 11 ? 2 : modeVirtuel >= 8 ? modeVirtuel - 8 : modeVirtuel >= 5 ? 2 : modeVirtuel > 2 ? ((y & 2) == yEgx ? modeVirtuel - 1 : modeVirtuel - 2) : modeVirtuel + 1);
		}

		static public int MaxPen(int y = 0) {
			switch (modeVirtuel) {
				case 0:
				case 1:
				case 2:
					return 1 << (4 >> modeVirtuel);
				case 3:
				case 4:
					return 1 << (4 >> ((y & 2) == yEgx ? modeVirtuel - 2 : modeVirtuel - 3));
				case 5:
					return 4;
				case 6:
					return 16;
				case 7:
					return 4;
				case 8:
					return 16;
				case 9:
					return 4;
				case 10:
					return 2;
			}
			return 16;
		}

		static public int GetPalCPC(int c) {
			return cpcPlus ? (((c & 0xF0) >> 4) * 17) + ((((c & 0xF00) >> 8) * 17) << 8) + (((c & 0x0F) * 17) << 16) : RgbCPC[c < 27 ? c : 0].GetColor;
		}

		static public CpcAmsdos CreeEntete(string nomFic, short start, short length, short entry) {
			CpcAmsdos entete = new CpcAmsdos();
			string nom = Path.GetFileName(nomFic);
			string ext = Path.GetExtension(nomFic);
			// Supprimer exension du nom
			if (ext != "") {
				nom = nom.Substring(0, nom.Length - ext.Length).ToUpper();
				ext = ext.Substring(1).ToUpper();
			}

			// Convertir le nom du fichier au format "AMSDOS" 8.3
			string result = "";
			for (int i = 0; i < 8; i++)
				result += i < nom.Length ? nom[i] : ' ';

			for (int i = 0; i < 3; i++)
				result += i < ext.Length ? ext[i] : ' ';

			// Initialisation de l'en-tête avec les valeurs passées en paramètre
			entete.Adress = start;
			entete.Length = entete.RealLength = entete.LogicalLength = length;
			entete.FileType = 2;
			entete.EntryAdress = entry;
			entete.FileName = result;
			// Calcul du checksum
			entete.CheckSum = (short)CalcCheckSum(entete);
			return entete;
		}

		static public int CalcCheckSum(byte[] arr) {
			int checkSum = 0;
			for (int i = 0; i < 67; i++) // Checksum = somme des 67 premiers octets
				checkSum += arr[i];

			return checkSum;
		}

		static public int CalcCheckSum(CpcAmsdos str) {
			int size = Marshal.SizeOf(str);
			byte[] arr = new byte[size];
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.StructureToPtr(str, ptr, true);
			Marshal.Copy(ptr, arr, 0, size);
			Marshal.FreeHGlobal(ptr);
			return CalcCheckSum(arr);
		}

		static public CpcAmsdos GetAmsdos(byte[] arr) {
			CpcAmsdos str = new CpcAmsdos();
			int size = Marshal.SizeOf(str);
			IntPtr ptr = Marshal.AllocHGlobal(size);
			Marshal.Copy(arr, 0, ptr, size);
			str = (CpcAmsdos)Marshal.PtrToStructure(ptr, typeof(CpcAmsdos));
			Marshal.FreeHGlobal(ptr);
			return str;
		}

		static public bool CheckAmsdos(byte[] entete) {
			if (entete.Length >= 128) {
				CpcAmsdos enteteAms = GetAmsdos(entete);
				return CalcCheckSum(entete) == enteteAms.CheckSum;
			}
			return false;
		}

		static public byte[] AmsdosToByte(CpcAmsdos obj) {
			int len = Marshal.SizeOf(obj);
			byte[] arr = new byte[len];
			IntPtr ptr = Marshal.AllocHGlobal(len);
			Marshal.StructureToPtr(obj, ptr, true);
			Marshal.Copy(ptr, arr, 0, len);
			Marshal.FreeHGlobal(ptr);
			return arr;
		}

		static public int GetPenColor(DirectBitmap bmpLock, int x, int y) {
			int pen;
			RvbColor col = bmpLock.GetPixelColor(x, y);
			if (cpcPlus) {
				for (pen = 0; pen < 16; pen++) {
					if ((col.v >> 4) == (Palette[pen] >> 8) && (col.b >> 4) == ((Palette[pen] >> 4) & 0x0F) && (col.r >> 4) == (Palette[pen] & 0x0F))
						break;
				}
			}
			else {
				for (pen = 0; pen < 16; pen++) {
					int c = modeVirtuel == 5 ? colMode5[y >> 1, pen] : Palette[pen] != 0xFFFF && Palette[pen] < 27 ? Palette[pen] : 0;
					RvbColor fixedCol = RgbCPC[c < 27 ? c : 0];
					if (fixedCol.r == col.r && fixedCol.b == col.b && fixedCol.v == col.v)
						break;
				}
			}
			return pen < 16 ? pen : 0;
		}

		static public RvbColor GetColor(int c) {
			if (c < 0xFFFF)
				return cpcPlus ? new RvbColor((byte)((c & 0x0F) * 17), (byte)(((c & 0xF00) >> 8) * 17), (byte)(((c & 0xF0) >> 4) * 17)) : RgbCPC[c >= 0 && c < 27 ? c : 0];

			return new RvbColor(0);
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct CpcAmsdos {
		//
		// Structure d'une entrée AMSDOS
		//
		public byte UserNumber;             // User
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x0F)]
		public string FileName;             // Nom + extension
		public byte BlockNum;               // Numéro du premier bloc (disquette)
		public byte LastBlock;              // Numéro du dernier bloc (disquette)
		public byte FileType;               // Type de fichier
		public short Length;                // Longueur
		public short Adress;                // Adresse
		public byte FirstBlock;             // Premier bloc de fichier (disquette)
		public short LogicalLength;         // Longueur logique
		public short EntryAdress;           // Point d'entrée
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x24)]
		public string Unused;
		public short RealLength;            // Longueur réelle
		public byte BigLength;              // Longueur réelle (3 octets)
		public short CheckSum;              // CheckSum Amsdos
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x3B)]
		public string Unused2;
	}
}

