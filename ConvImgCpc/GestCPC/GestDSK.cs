using System;
using System.IO;
using System.Runtime.InteropServices;
using System.Text;
using System.Windows.Forms;

namespace ConvImgCpc {
	public class GestDSK {
		public const int MAX_DSK = 2;       // Nbre de fichiers DSK gérés
		const int MAX_TRACKS = 99;          // Nbre maxi de pistes/DSK
		const int SECTSIZE = 512;           // Taille secteur standard
		const byte USER_DELETED = 0xE5;     // Octet pour fichier effacé
		public string NomFic;
		public CPCEMUEnt Infos = new CPCEMUEnt();
		public CPCEMUTrack[][] Tracks;
		public byte FlagWrite;
		public byte[][][] Data = new byte[MAX_TRACKS][][];
		public byte[] Bitmap = new byte[256];
		public enum DskError { ERR_NO_ERR = 0, ERR_NO_DIRENTRY, ERR_NO_BLOCK, ERR_FILE_EXIST };
		private readonly Main main;

		public GestDSK(Main m) {
			main = m; FormatDsk();
		}

		public void FormatDsk() {
			Infos.id = "EXTENDED CPC DSK File\r\nDisk-Info\r\nConvImgCpc    ";
			Infos.NbTracks = 40;
			Infos.NbHeads = 1;
			Tracks = new CPCEMUTrack[MAX_TRACKS][];
			for (int t = 0; t < MAX_TRACKS; t++) {
				if (Tracks[t] == null) {
					Tracks[t] = new CPCEMUTrack[2];
					Tracks[t][0] = new CPCEMUTrack();
					Tracks[t][1] = new CPCEMUTrack();
				}
				if (Data[t] == null)
					Data[t] = new byte[2][];

				Data[t][0] = new byte[0x1800];

				Data[t][1] = new byte[0x1800];

				FormatTrack(t, 0, 0xC1, 9); // Format par défaut = data (face 0)
				FormatTrack(t, 1, 0xC1, 9); // Format par défaut = data (face 1)
				Infos.TrackSizeTable[t] = 0x13;
			}
		}

		//
		// Retourne la position d'un secteur dans le fichier DSK
		//
		private int GetPosData(int track, int sect) {
			// Recherche position secteur
			int Pos = 0;
			CPCEMUTrack tr = Tracks[track][0];

			for (int s = 0; s < tr.NbSect; s++) {
				if (tr.Sect[s].R == sect)
					break;

				if (tr.Sect[s].SectSize != 0)
					Pos += tr.Sect[s].SectSize;
				else
					Pos += (128 << tr.Sect[s].N);
			}
			return (Pos);
		}

		// Retourne l'index (dans le tableau de secteurs) d'un secteur
		private int GetIndexSecteur(int track, int sect) {
			CPCEMUTrack tr = Tracks[track][0];
			for (int s = 0; s < tr.NbSect; s++) {
				if (tr.Sect[s].R == sect)
					return s;
			}
			return -1;
		}

		//
		// Recherche le plus petit secteur d'une piste
		//
		int GetMinSect(bool ForceStd) {
			int Sect = 0x100;
			CPCEMUTrack tr = Tracks[0][0];
			for (int s = 0; s < tr.NbSect; s++) {
				byte CurSect = tr.Sect[s].R;
				if (ForceStd)
					if (CurSect == 0x01 || CurSect == 0x41 || CurSect == 0xC1)
						return (CurSect);

				if (Sect > CurSect)
					Sect = CurSect;
			}
			return (Sect);
		}

		//
		// Retourne une entrée du répertoire
		//
		StDirEntry GetInfoDirEntry(int NumDir) {
			StDirEntry Dir = new StDirEntry();
			int MinSect = GetMinSect(true);
			int s = (NumDir >> 4) + MinSect;
			int t = MinSect == 0x41 ? 2 : MinSect == 1 ? 1 : 0;
			int pos = GetPosData(t, s) + ((NumDir & 15) << 5);
			int length = Marshal.SizeOf(Dir);
			IntPtr ptr = Marshal.AllocHGlobal(length);
			Marshal.Copy(Data[t][0], pos, ptr, length);
			Dir = (StDirEntry)Marshal.PtrToStructure(Marshal.UnsafeAddrOfPinnedArrayElement(Data[t][0], pos), Dir.GetType());
			Marshal.FreeHGlobal(ptr);
			return Dir;
		}

		// Remplir la "bitmap" des blocs utilisés
		private int FillBitmap() {
			int NbKo = 0;
			Array.Clear(Bitmap, 0, Bitmap.Length);
			Bitmap[0] = 1;
			for (int i = 0; i < 64; i++) {
				StDirEntry Dir = GetInfoDirEntry(i);
				if (Dir.User != USER_DELETED) {
					for (int j = 0; j < 16; j++) {
						int b = Dir.Blocks[j];
						if (b > 1 && Bitmap[b] == 0) {
							Bitmap[b] = 1;
							NbKo++;
						}
					}
				}
			}
			return (NbKo);
		}

		// Comparaison nom Amsdos (sans codes spéciaux: protégé, lecture seule...)
		int CompareNomsAmsdos(string n1, string n2) {
			for (int i = 0; i < 11; i++)
				if ((n1[i] & 0x7F) != (n2[i] & 0x7F))
					return 1;

			return 0;
		}

		//
		// Vérifie l'existente d'un fichier, retourne l'indice du fichier si existe,
		// -1 sinon
		//
		int FileExist(string Nom, int User) {
			for (int i = 0; i < 64; i++) {
				StDirEntry Dir = GetInfoDirEntry(i);
				string FullName = Encoding.UTF8.GetString(Dir.Nom, 0, 8) + Encoding.UTF8.GetString(Dir.Ext, 0, 3);
				if (Dir.User == User && CompareNomsAmsdos(Nom, FullName) == 0)
					return i;
			}
			return -1;
		}

		StDirEntry GetNomDir(string NomFic) {
			StDirEntry DirLoc = new StDirEntry();
			string nom, ext;
			int p = NomFic.IndexOf(".");

			if (p > 0) {
				nom = NomFic.Substring(0, p);
				ext = NomFic.Substring(p + 1);
			}
			else {
				nom = NomFic.Length > 8 ? NomFic.Substring(0, 8).ToUpper() : NomFic.ToUpper();
				ext = NomFic.Length > 8 ? NomFic.Substring(8).ToUpper() : "BIN";
			}
			while (nom.Length < 8)
				nom += " ";

			while (ext.Length < 3)
				ext += " ";

			DirLoc.Nom = Encoding.ASCII.GetBytes(nom);
			DirLoc.Ext = Encoding.ASCII.GetBytes(ext);
			return DirLoc;
		}

		//
		// Recherche une entrée de répertoire libre
		//
		private int RechercheDirLibre() {
			for (int i = 0; i < 64; i++) {
				StDirEntry Dir = GetInfoDirEntry(i);
				if (Dir.User == USER_DELETED)
					return i;
			}
			return -1;
		}

		//
		// Recherche un bloc libre et le remplis
		//
		private int RechercheBlocLibre(int MaxBloc) {
			for (int i = 2; i < MaxBloc; i++)
				if (Bitmap[i] == 0) {
					Bitmap[i] = 1;
					return i;
				}
			return 0;
		}

		//
		// Formater une piste
		//
		private void FormatTrack(int t, int h, int MinSect, int NbSect) {
			CPCEMUTrack tr = Tracks[t][h];
			for (int i = 0; i < 0x1800; i++)
				Data[t][h][i] = USER_DELETED;

			tr.id = "Track-Info\r\n    ";
			tr.Track = (byte)t;
			tr.Head = (byte)h;
			tr.SectSize = 2;
			tr.NbSect = (byte)NbSect;
			tr.Gap3 = 0x4E;
			tr.OctRemp = USER_DELETED;
			int readSect = 0;
			//
			// Gestion "entrelacement" des secteurs
			//
			for (int s = 0; s < NbSect; ) {
				tr.Sect[s].C = (byte)t;
				tr.Sect[s].H = (byte)h;
				tr.Sect[s].R = (byte)(readSect + MinSect);
				tr.Sect[s].N = 2;
				tr.Sect[s].SectSize = SECTSIZE;
				readSect++;
				if (++s < NbSect) {
					tr.Sect[s].C = (byte)t;
					tr.Sect[s].H = (byte)h;
					tr.Sect[s].R = (byte)(readSect + MinSect + 4);
					tr.Sect[s].N = 2;
					tr.Sect[s].SectSize = SECTSIZE;
					s++;
				}
			}
		}

		//
		// Ecriture d'un bloc AMSDOS (1 block = 2 secteurs)
		//
		private void WriteBloc(int bloc, byte[] BufBloc, int offset) {
			int track = (bloc << 1) / 9;
			int sect = (bloc << 1) % 9;
			int MinSect = GetMinSect(true);
			if (MinSect == 0x41)
				track += 2;
			else
				if (MinSect == 0x01)
					track++;

			//
			// Ajuste le nombre de pistes si dépassement capacité
			//
			if (track > Infos.NbTracks - 1) {
				Infos.NbTracks = (byte)(track + 1);
				FormatTrack(track, 0, MinSect, 9);
			}

			int Pos = GetPosData(track, sect + MinSect);
			int l = Math.Min(SECTSIZE, BufBloc.Length - offset);
			Array.Copy(BufBloc, offset, Data[track][0], Pos, l);
			if (++sect > 8) {
				track++;
				sect = 0;
			}
			Pos = GetPosData(track, sect + MinSect);
			l = Math.Min(SECTSIZE, BufBloc.Length - offset - SECTSIZE);
			if (l > 0)
				Array.Copy(BufBloc, offset + SECTSIZE, Data[track][0], Pos, l);
		}

		//
		// Positionne une entrée dans le répertoire
		//
		private void SetInfoDirEntry(int NumDir, StDirEntry Dir) {
			int MinSect = GetMinSect(true);
			int s = (NumDir >> 4) + MinSect;
			int t = MinSect == 0x41 ? 2 : MinSect == 1 ? 1 : 0;
			int pos = GetPosData(t, s) + ((NumDir & 15) << 5);
			int len = Marshal.SizeOf(Dir);
			IntPtr ptr = Marshal.AllocHGlobal(len);
			Marshal.StructureToPtr(Dir, ptr, true);
			Marshal.Copy(ptr, Data[t][0], pos, len);
			Marshal.FreeHGlobal(ptr);
		}

		//
		// "Copier" un fichier sur le dsk
		//
		public DskError CopieFichier(byte[] BufFile, string NomFic, int TailleFic, int MaxBloc, int User) {
			int j, l, Bloc, PosFile, NbPages = 0, PosDir, TaillePage;

			FillBitmap();
			StDirEntry DirLoc = GetNomDir(NomFic);
			string FullName = Encoding.UTF8.GetString(DirLoc.Nom, 0, 8) + Encoding.UTF8.GetString(DirLoc.Ext, 0, 3);
			if (FileExist(FullName, User) > -1)
				return (DskError.ERR_FILE_EXIST);

			for (PosFile = 0; PosFile < TailleFic; ) {
				PosDir = RechercheDirLibre();
				if (PosDir != -1) {
					DirLoc.User = (byte)User;
					DirLoc.NumPage = (byte)NbPages++;
					TaillePage = Math.Min(128, ((TailleFic - PosFile) + 127) >> 7);
					DirLoc.NbPages = (byte)TaillePage;
					l = (DirLoc.NbPages + 7) >> 3;
					DirLoc.Blocks = new byte[16];
					for (j = 0; j < l; j++) {
						Bloc = RechercheBlocLibre(MaxBloc);
						if (Bloc != 0) {
							DirLoc.Blocks[j] = (byte)Bloc;
							WriteBloc(Bloc, BufFile, PosFile);
							PosFile += 1024;
						}
						else
							return (DskError.ERR_NO_BLOCK);
					}
					SetInfoDirEntry(PosDir, DirLoc);
				}
				else
					return (DskError.ERR_NO_DIRENTRY);
			}
			return (DskError.ERR_NO_ERR);
		}

		public void Save(string fileName) {
			try {
				FileStream fs = new FileStream(fileName, FileMode.OpenOrCreate);
				BinaryWriter wr = new BinaryWriter(fs);
				wr.Write(Encoding.ASCII.GetBytes(Infos.id));
				wr.Write(Infos.NbTracks);
				wr.Write(Infos.NbHeads);
				wr.Write(Infos.TrackSize);
				wr.Write(Infos.TrackSizeTable);
				for (int t = 0; t < Infos.NbTracks; t++) {
					for (int h = 0; h < Infos.NbHeads; h++) {
						CPCEMUTrack tr = Tracks[t][h];
						wr.Write(Encoding.ASCII.GetBytes(tr.id));
						wr.Write(tr.Track);
						wr.Write(tr.Head);
						wr.Write(tr.Unused);
						wr.Write(tr.SectSize);
						wr.Write(tr.NbSect);
						wr.Write(tr.Gap3);
						wr.Write(tr.OctRemp);
						int tailleData = 0;
						for (int s = 0; s < CPCEMUTrack.MAX_SECTS; s++) {
							CPCEMUSect sect = tr.Sect[s];
							wr.Write(sect.C);
							wr.Write(sect.H);
							wr.Write(sect.R);
							wr.Write(sect.N);
							wr.Write(sect.ST1);
							wr.Write(sect.ST2);
							wr.Write(sect.SectSize);
							if (s < tr.NbSect) {
								int n = sect.SectSize;
								if ((n & 0xFF) > 0)
									n = (n + 0xFF) & 0x1F00;
								else
									if (n == 0) {
										n = sect.N;
										if (n < 6)
											n = 128 << n;
										else
											n = 0x1800;
									}
								tailleData += n > 0x100 ? n : 0x100;
							}
						}
						wr.Write(Data[t][h], 0, tailleData);
					}
				}
				wr.Close();
				fs.Close();
			}
			catch {
				MessageBox.Show(main.multilingue.GetString("GestDSK.TxtInfo1"));
			}
		}

		public void Load(string fileName) {
			try {
				NomFic = fileName;
				FileStream fs = new FileStream(fileName, FileMode.Open);
				BinaryReader br = new BinaryReader(fs);
				Infos.id = Encoding.UTF8.GetString(br.ReadBytes(0x30));
				Infos.NbTracks = br.ReadByte();
				Infos.NbHeads = br.ReadByte();
				Infos.TrackSize = br.ReadInt16();
				Infos.TrackSizeTable = br.ReadBytes(204);
				for (int t = 0; t < Infos.NbTracks; t++)
					for (int h = 0; h < Infos.NbHeads; h++) {
						CPCEMUTrack tr = Tracks[t][h];
						tr.id = Encoding.UTF8.GetString(br.ReadBytes(0x10));
						if (tr.id.Length >= 10 && tr.id.Substring(0, 10) == "Track-Info") {
							tr.Track = br.ReadByte();
							tr.Head = br.ReadByte();
							tr.Unused = br.ReadInt16();
							tr.SectSize = br.ReadByte();
							tr.NbSect = br.ReadByte();
							tr.Gap3 = br.ReadByte();
							tr.OctRemp = br.ReadByte();
							// Si une seule face, alors faire face2=face1 ?
							if (tr.Head == 0 && Infos.NbHeads == 1) {
								Tracks[t][1] = Tracks[t][0];
								Data[t][1] = Data[t][0];
							}
							int tailleData = 0;
							for (int s = 0; s < CPCEMUTrack.MAX_SECTS; s++) {
								CPCEMUSect sect = tr.Sect[s];
								sect.C = br.ReadByte();
								sect.H = br.ReadByte();
								sect.R = br.ReadByte();
								sect.N = br.ReadByte();
								sect.ST1 = br.ReadByte();
								sect.ST2 = br.ReadByte();
								sect.SectSize = br.ReadInt16();
								if (s < tr.NbSect) {
									int n = sect.SectSize;
									if ((n & 0xFF) > 0)
										n = (n + 0xFF) & 0x1F00;
									else
										if (n == 0) {
											n = sect.N;
											if (n < 6)
												n = 128 << n;
											else
												n = 0x1800;
										}
									tailleData += n > 0x100 ? n : 0x100;
								}
							}
							Data[t][h] = br.ReadBytes(tailleData);
						}
						else
							break;
					}
				br.Close();
				fs.Close();
			}
			catch {
				MessageBox.Show(main.multilingue.GetString("GestDSK.TxtInfo2"));
			}
		}
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public struct StDirEntry {
		public byte User;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 8)]
		public byte[] Nom;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 3)]
		public byte[] Ext;
		public byte NumPage;
		public short Unused;
		public byte NbPages;
		[MarshalAs(UnmanagedType.ByValArray, SizeConst = 16)]
		public byte[] Blocks;
	}

	[StructLayout(LayoutKind.Sequential, Pack = 1)]
	public class CPCEMUEnt {
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x30)]
		public string id;           // "MV - CPCEMU Disk-File\r\nDisk-Info\r\n"
		public byte NbTracks;
		public byte NbHeads;
		public short TrackSize;     // 0x1300 = 256 + ( 512 * nbsecteurs )
		public byte[] TrackSizeTable = new byte[204];   // Si "EXTENDED CPC DSK File"
	};

	public class CPCEMUTrack {
		public const int MAX_SECTS = 29;    // Nbre maxi de secteurs/pistes
		[MarshalAs(UnmanagedType.ByValTStr, SizeConst = 0x10)]
		public string id;       // "Track-Info\r\n"
		public byte Track;
		public byte Head;
		public short Unused;
		public byte SectSize;   // 2
		public byte NbSect;     // 9
		public byte Gap3;       // 0x4E
		public byte OctRemp;    // 0xE5
		public CPCEMUSect[] Sect = new CPCEMUSect[MAX_SECTS];

		public CPCEMUTrack() {
			for (int i = 0; i < MAX_SECTS; i++)
				Sect[i] = new CPCEMUSect();
		}
	}

	public class CPCEMUSect {
		public byte C;          // track
		public byte H;          // head
		public byte R;          // sect
		public byte N;          // size
		public byte ST1;        // Valeur ST1
		public byte ST2;        // Valeur ST2
		public short SectSize;  // Taille du secteur en octets
	}
}