using System;
using System.IO;

namespace ConvImgCpc {
	static public class GenSplitAsm {
		private readonly static string CpcVGA = "TDU\\X]LEMVFW^@_NGORBSZY[JCK";
		private readonly static byte[] bufPack = new byte[0x8000];

		// Code d'initialisation
		static private void WriteDebFile(StreamWriter wr) {
			wr.WriteLine("	RUN $");
			wr.WriteLine("");
			wr.WriteLine("	nolist");
			wr.WriteLine("	DI");
			wr.WriteLine("	LD	HL,ImageCmp");
			wr.WriteLine("	LD	DE,#0200");
			wr.WriteLine("	CALL	Depack");
			wr.WriteLine("	LD	HL,#C9FB");
			wr.WriteLine("	LD	(#38),HL");
			wr.WriteLine("	LD	HL,Overscan");
			wr.WriteLine("	LD	B,#BC");
			wr.WriteLine("BclCrtc");
			wr.WriteLine("	LD	A,(HL)");
			wr.WriteLine("	INC	HL");
			wr.WriteLine("	AND	A");
			wr.WriteLine("	JR	Z,SetPalette");
			wr.WriteLine("	OUT	(C),A");
			wr.WriteLine("	INC	B");
			wr.WriteLine("	INC	B");
			wr.WriteLine("	OUTI");
			wr.WriteLine("	DEC	B");
			wr.WriteLine("	JR	BclCrtc");
			wr.WriteLine("SetPalette");
			wr.WriteLine("	LD	B,#7F");
			wr.WriteLine("BclPalette");
			wr.WriteLine("	OUT	(C),A");
			wr.WriteLine("	INC	B");
			wr.WriteLine("	OUTI");
			wr.WriteLine("	INC	A");
			wr.WriteLine("	CP	16");
			wr.WriteLine("	JR	NZ,BclPalette");
			wr.WriteLine("Boucle");
			wr.WriteLine("	EI");
			wr.WriteLine("	LD	B,#F5");
			wr.WriteLine("WaitVbl");
			wr.WriteLine("	IN	A,(C)");
			wr.WriteLine("	RRA");
			wr.WriteLine("	JR	NC,WaitVbl");
			wr.WriteLine("	HALT");
			wr.WriteLine("	DI");
		}

		// Générer des 'NOPS' en fonction du nbre de pixels à passer. Retourne si HL a été modifié (par ADD HL,BC)
		static private bool GenereRetard(StreamWriter wr, int nbPixels, bool crashBC = false, bool canUseHL = false) {
			bool ret = false;
			int nbNops = nbPixels >> 3; // 1 Nop = 8 pixels
			if (nbNops >= 1024) {
				int bc = (nbNops - (crashBC ? 2 : 4)) / 7;
				int delai = ((bc * 7) + (crashBC ? 2 : 4));
				wr.WriteLine("	LD	BC," + bc.ToString() + "			; Attendre " + delai.ToString() + " NOPs (7*" + bc.ToString() + "+" + (crashBC ? 2 : 4).ToString() + ")");
				wr.WriteLine("	DEC	BC");
				wr.WriteLine("	LD	A,B");
				wr.WriteLine("	OR	C");
				wr.WriteLine("	JR	NZ,$-3");
				if (!crashBC)
					wr.WriteLine("	LD	B,#7F");

				nbNops -= ((bc * 7) + (crashBC ? 2 : 4));
			}
			if (nbNops > 11 && nbNops < 17) {
				wr.WriteLine("	EX	(SP),HL			; Attendre 6 NOPs");
				wr.WriteLine("	EX	(SP),HL			; Attendre 6 NOPs");
				nbNops -= 12;
			}
			if (nbNops > 8 && nbNops < 17) {
				wr.WriteLine("	PUSH	IX			; Attendre 5 NOPs");
				wr.WriteLine("	POP	IX			; Attendre 4 NOPs");
				nbNops -= 9;
			}
			if (nbNops > 7 && nbNops < 1024) {
				int b = (nbNops - (crashBC ? 1 : 3)) >> 2;
				int delai = ((b << 2) + (crashBC ? 1 : 3));
				wr.WriteLine("	LD	B," + b.ToString() + "			; Attendre " + delai.ToString() + " NOPs (4*" + b.ToString() + "+" + (crashBC ? 1 : 3).ToString() + ")");
				wr.WriteLine("	DJNZ	$-0");
				if (!crashBC)
					wr.WriteLine("	LD	B,#7F");

				nbNops -= delai;
			}
			if (nbNops >= 4) {
				wr.WriteLine("	ADD	IX,BC			; Attendre 4 NOPs");
				nbNops -= 4;
			}
			if (nbNops >= 3 && canUseHL) {
				wr.WriteLine("	ADD	HL,BC			; Attendre 3 NOPs");
				nbNops -= 3;
				ret = true;
			}
			if (nbNops >= 2) {
				wr.WriteLine("	CP	(HL)			; Attendre 2 NOPs");
				nbNops -= 2;
			}
			for (; nbNops-- > 0;)
				wr.WriteLine("	NOP				; Attendre 1 NOP");

			return ret;
		}

		static public void GenereDatas(StreamWriter sw, byte[] tabByte, int length, int nbOctetsLigne, int ligneSepa = 0, string labelSepa = null) {
			string line = "\tDB\t";
			int nbOctets = 0, nbLigne = 0, indiceLabel = 0;
			if (labelSepa != null) {
				sw.WriteLine(labelSepa + indiceLabel.ToString("00"));
				indiceLabel++;
			}
			for (int i = 0; i < length; i++) {
				line += "#" + tabByte[i].ToString("X2") + ",";
				if (++nbOctets >= nbOctetsLigne) {
					sw.WriteLine(line.Substring(0, line.Length - 1));
					line = "\tDB\t";
					nbOctets = 0;
					if (i < length - 1 && ++nbLigne >= ligneSepa && ligneSepa > 0) {
						nbLigne = 0;
						if (labelSepa != null) {
							sw.WriteLine(labelSepa + indiceLabel.ToString("00"));
							indiceLabel++;
						}
						else
							line += "\r\n";
					}
				}
			}
			if (nbOctets > 0)
				sw.WriteLine(line.Substring(0, line.Length - 1));

			sw.WriteLine("; Taille totale " + length.ToString() + " octets");
		}

		static private void WriteEndFile(StreamWriter wr, int[,,] palette, Main.PackMethode pkMethode) {
			wr.WriteLine("	JP	Boucle");
			wr.WriteLine("");
			SaveAsm.GenereDepack(wr, pkMethode);
			wr.WriteLine("");
			wr.WriteLine("	List");
			wr.WriteLine("");
			wr.WriteLine("Overscan");
			wr.WriteLine("	DB	1,48,2,50,3,#8E,6,34,7,35,12,13,13,0,0");
			wr.WriteLine("Palette");
			wr.Write("	DB	");
			for (int i = 0; i < 16; i++) {
				int col = CpcVGA[palette[0, 0, i] != 0xFFFF ? palette[0, 0, i] : 0];
				wr.Write("#" + col.ToString("X2"));
				if (i < 15)
					wr.Write(",");
			}
			wr.WriteLine("");
		}

		static public void CreeAsm(StreamWriter wr, BitmapCpc bmpCpc, Main.PackMethode pkMethode) {
			int nbLigneVide = 0;
			int tpsImage = 3;
			int reste = 0, oldc1 = 0, oldc2 = 0, oldc3 = 0, oldc4 = 0, oldc5 = 0, oldPen = 0;
			int oldRetPrec = BitmapCpc.retardMin + 15136;

			// Compactage de l'image 
			byte[] bmp = bmpCpc.bmpCpc;
			for (int i = 0x600; i < 0x800; i++)
				bmp[i] = bmp[i + 0x800] = bmp[i + 0x1000] = bmp[i + 0x1800] = bmp[i + 0x2000] = bmp[i + 0x2800] = bmp[i + 0x3000] = 0;

			for (int i = 0x44C0; i < 0x4600; i++)
				bmp[i] = bmp[i + 0x800] = bmp[i + 0x1000] = bmp[i + 0x1800] = bmp[i + 0x2000] = bmp[i + 0x2800] = bmp[i + 0x3000] = bmp[i + 0x3800] = 0;

			int lgPack = new PackModule().Pack(bmp, 0x7CC0, bufPack, 0, pkMethode);

			wr.WriteLine("	ORG	#" + (0x8400 - lgPack).ToString("X4"));
			wr.WriteLine("	Nolist");
			wr.WriteLine("ImageCmp");
			GenereDatas(wr, bufPack, lgPack, 16);
			WriteDebFile(wr);
			for (int y = 0; y < 262; y++) {
				LigneSplit lSpl = bmpCpc.splitEcran.LignesSplit[y];
				// Split 1
				if (lSpl.ListeSplit[0].enable) {
					bool hlMod = false;
					int retPrec = oldRetPrec;
					oldRetPrec = 0;
					if (nbLigneVide > 0 || reste > 0) {
						retPrec += (reste << 3) + (nbLigneVide * 512);
						nbLigneVide = 0;
					}
					int retard = lSpl.retard - BitmapCpc.retardMin;
					hlMod |= GenereRetard(wr, retard + retPrec, false, true);
					int retSameCol = 0;
					wr.WriteLine("; ---- Ligne " + y.ToString() + " ----");
					if (reste < 0 && lSpl.numPen == oldPen)
						hlMod |= GenereRetard(wr, (6 + reste) << 3, false, true);
					else {
						wr.WriteLine("	LD	C," + lSpl.numPen.ToString() + "			; (2 NOPs)");
						wr.WriteLine("	OUT	(C),C			; (4 NOPs)");
						oldPen = lSpl.numPen;
					}
					int c0 = CpcVGA[lSpl.ListeSplit[0].couleur];
					wr.WriteLine("	LD	C,#" + c0.ToString("X2") + "			; (2 NOPs)");
					int c1 = CpcVGA[lSpl.ListeSplit[1].couleur];
					int c2 = CpcVGA[lSpl.ListeSplit[2].couleur];
					if ((c1 != oldc1 || c2 != oldc2) && (lSpl.ListeSplit[1].enable || lSpl.ListeSplit[2].enable))
						wr.WriteLine("	LD	DE,#" + c1.ToString("X2") + c2.ToString("X2") + "		; (3 NOPs)");
					else
						retSameCol += 24;

					int c3 = CpcVGA[lSpl.ListeSplit[3].couleur];
					int c4 = CpcVGA[lSpl.ListeSplit[4].couleur];
					if (hlMod || ((c3 != oldc3 || c4 != oldc4) && (lSpl.ListeSplit[3].enable || lSpl.ListeSplit[4].enable))) {
						wr.WriteLine("	LD	HL,#" + c3.ToString("X2") + c4.ToString("X2") + "		; (3 NOPs)");
					}
					else
						retSameCol += 24;

					int c5 = CpcVGA[lSpl.ListeSplit[5].couleur];
					if (c5 != oldc5 && lSpl.ListeSplit[5].enable)
						wr.WriteLine("	LD	A,#" + c5.ToString("X2") + "			; (2 NOPs)");
					else
						retSameCol += 16;

					if (retSameCol > 0)
						GenereRetard(wr, retSameCol, false, !lSpl.ListeSplit[3].enable && !lSpl.ListeSplit[4].enable);

					wr.WriteLine("	OUT	(C),C			; (4 NOPs)");
					int tpsLine = (retard >> 3) + 20;
					int lg = lSpl.ListeSplit[0].longueur - 32;
					tpsLine += lg >> 3;
					// Split 2
					if (lSpl.ListeSplit[1].enable) {
						int lgs = lSpl.ListeSplit[0].longueur - 32;
						GenereRetard(wr, lgs);
						wr.WriteLine("	OUT	(C),D			; (4 NOPs)");
						tpsLine += 4 + (lgs >> 3);
						// Split 3
						if (lSpl.ListeSplit[2].enable) {
							lgs = lSpl.ListeSplit[1].longueur - 32;
							GenereRetard(wr, lgs, false, !lSpl.ListeSplit[3].enable && !lSpl.ListeSplit[4].enable);
							wr.WriteLine("	OUT	(C),E			; (4 NOPs)");
							tpsLine += 4 + (lgs >> 3);
							// Split 4
							if (lSpl.ListeSplit[3].enable) {
								lgs = lSpl.ListeSplit[2].longueur - 32;
								GenereRetard(wr, lgs);
								wr.WriteLine("	OUT	(C),H			; (4 NOPs)");
								tpsLine += 4 + (lgs >> 3);
								// Split 5
								if (lSpl.ListeSplit[4].enable) {
									lgs = lSpl.ListeSplit[3].longueur - 32;
									GenereRetard(wr, lgs);
									wr.WriteLine("	OUT	(C),L			; (4 NOPs)");
									tpsLine += 4 + (lgs >> 3);
									// Split 6
									if (lSpl.ListeSplit[5].enable) {
										lgs = lSpl.ListeSplit[4].longueur - 32;
										GenereRetard(wr, lgs);
										wr.WriteLine("	OUT	(C),A			; (4 NOPs)");
										tpsLine += 4 + (lgs >> 3);
									}
									if (lSpl.ListeSplit[6].enable) {
										int c6 = CpcVGA[lSpl.ListeSplit[6].couleur];
										int sameColor = 6;
										string tabRegs = "CDEHLAA";
										for (int i = 0; i < 6; i++)
											if (c6 == CpcVGA[lSpl.ListeSplit[i].couleur])
												sameColor = i;

										char reg = tabRegs[sameColor]; // Registre ayant la couleur déja définie
										if (sameColor == 6) {
											wr.WriteLine("	LD	A,#" + c6.ToString("X2") + "			; (2 NOPs)");
											lgs = lSpl.ListeSplit[5].longueur - 48;
											tpsLine += 2;
											c5 = c6;
										}
										else
											lgs = lSpl.ListeSplit[5].longueur - 32;

										GenereRetard(wr, lgs);
										wr.WriteLine("	OUT	(C)," + reg.ToString() + "			; (4 NOPs)");
										tpsLine += 4 + (lgs >> 3);
									}
								}
							}
						}
					}
					oldc1 = c1;
					oldc2 = c2;
					oldc3 = c3;
					oldc4 = c4;
					oldc5 = c5;
					reste = 64 - tpsLine + (lg >> 3);
				}
				else {
					nbLigneVide++;
					wr.WriteLine("; ---- Ligne " + y.ToString() + " (vide) ----");
				}
				tpsImage += 64;
			}
			WriteEndFile(wr, bmpCpc.splitPalette, pkMethode);
		}
	}
}
