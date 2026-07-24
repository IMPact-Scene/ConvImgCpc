using System;
using System.IO;

namespace ConvImgCpc {
	public class SaveAsm {
		#region Code assembleur général
		static public StreamWriter OpenAsm(string fileName, string version = null, bool withTaille = false) {
			StreamWriter sw = File.CreateText(fileName);
			if (version != null)
				sw.WriteLine("; Généré par ConvImgCpc" + version.Replace('\n', ' ') + " - le " + DateTime.Now.ToString("dd/MM/yyyy (HH mm ss)"));

			sw.WriteLine("; Mode écran - " + Cpc.modesVirtuels[Cpc.modeVirtuel]);
			if (withTaille)
				sw.WriteLine("; Taille (nbColsxNbLignes) " + Cpc.NbCol.ToString() + "x" + Cpc.NbLig.ToString());

			return sw;
		}

		static public void CloseAsm(StreamWriter sw) {
			sw.Close();
			sw.Dispose();
		}

		static public void WriteDatas(StreamWriter sw, int[] tabCol, int start, int end, int nbMotsLigne, int ligneSepa = 0, string labelSepa = null) {
			sw.Write(GenereDatas(tabCol, start, end, nbMotsLigne, ligneSepa, labelSepa));
		}

		static public string GenereDatas(int[] tabCol, int start, int end, int nbMotsLigne, int ligneSepa = 0, string labelSepa = null, bool exclTaille = false) {
			string ret = "";
			string line = "\tDW\t";
			int nbOctets = 0, nbLigne = 0, indiceLabel = 0;
			if (labelSepa != null) {
				ret += labelSepa + indiceLabel.ToString("00") + "\r\n";
				indiceLabel++;
			}
			for (int i = start; i <= end; i++) {
				line += "#" + tabCol[i].ToString("X3") + ",";
				if (++nbOctets >= Math.Min(nbMotsLigne, 16)) {
					ret += line.Substring(0, line.Length - 1) + "\r\n";
					line = "\tDW\t";
					nbOctets = 0;
					if (i < end - 1 && ++nbLigne >= ligneSepa && ligneSepa > 0) {
						nbLigne = 0;
						if (labelSepa != null) {
							ret += labelSepa + indiceLabel.ToString("00") + "\r\n";
							indiceLabel++;
						}
						else
							line += "\r\n";
					}
				}
			}
			if (nbOctets > 0)
				ret += line.Substring(0, line.Length - 1) + "\r\n";

			if (!exclTaille)
				ret += "; Taille totale " + (end - start + 1).ToString() + " mots\r\n";

			return ret;
		}

		static public void GenereDatas(StreamWriter sw, byte[] tabByte, int length, int nbOctetsLigne, int ligneSepa = 0, string labelSepa = null, bool exclTaille = false) {
			string line = "\tDB\t";
			int nbOctets = 0, nbLigne = 0, indiceLabel = 0;
			if (!string.IsNullOrEmpty(labelSepa)) {
				sw.WriteLine(labelSepa + (ligneSepa != 0 ? indiceLabel.ToString("00") : ""));
				indiceLabel++;
			}
			// Vérifier si tableau contient la même valeur
			byte firstVal = tabByte[0];
			bool sameVal = true;
			for (int i = 1; i < length; i++)
				if (tabByte[i] != firstVal) {
					sameVal = false;
					break;
				}
			if (sameVal)
				sw.WriteLine("\tDS\t" + length.ToString() + ",#" + firstVal.ToString("X2"));
			else {
				for (int i = 0; i < length; i++) {
					line += "#" + tabByte[i].ToString("X2") + ",";
					if (++nbOctets >= Math.Min(nbOctetsLigne, Cpc.NbCol)) {
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
			}
			if (nbOctets > 0)
				sw.WriteLine(line.Substring(0, line.Length - 1));

			if (!exclTaille)
				sw.WriteLine("; Taille totale " + length.ToString() + " octets");
		}

		static private void GenereDepack(StreamWriter sw, Main.PackMethode pkMethode, string jumpLabel = null) {
			switch (pkMethode) {
				case Main.PackMethode.Standard:
					GenereDStd(sw, jumpLabel);
					break;

				case Main.PackMethode.ZX0:
				case Main.PackMethode.ZX0Ovs:
				case Main.PackMethode.ZX0_V2:
					GenereDZX0(sw, pkMethode != Main.PackMethode.ZX0, jumpLabel);
					break;

				case Main.PackMethode.ZX1:
					GenereDZX1(sw, jumpLabel);
					break;
			}
		}

		static private void GenereDZX0(StreamWriter sw, bool v2, string jumpLabel) {
			sw.WriteLine("; Decompactage");
			sw.WriteLine("Depack");
			sw.WriteLine("	ld	bc,#ffff			; preserve default offset 1");
			sw.WriteLine("	push	bc");
			sw.WriteLine("	inc	bc");
			sw.WriteLine("	ld	a,#80");
			sw.WriteLine("dzx0s_literals");
			sw.WriteLine("	call	dzx0s_elias		; obtain length");
			sw.WriteLine("	ldir					; copy literals");
			sw.WriteLine("	add	a,a					; copy from last offset or new offset?");
			sw.WriteLine("	jr	c,dzx0s_new_offset");
			sw.WriteLine("	call	dzx0s_elias		; obtain length");
			sw.WriteLine("dzx0s_copy");
			sw.WriteLine("	ex	(sp),hl				; preserve source,restore offset");
			sw.WriteLine("	push	hl				; preserve offset");
			sw.WriteLine("	add	hl,de				; calculate destination - offset");
			sw.WriteLine("	ldir					; copy from offset");
			sw.WriteLine("	pop	hl					; restore offset");
			sw.WriteLine("	ex	(sp),hl				; preserve offset,restore source");
			sw.WriteLine("	add	a,a					; copy from literals or new offset?");
			sw.WriteLine("	jr	nc,dzx0s_literals");
			sw.WriteLine("dzx0s_new_offset");
			if (v2) {
				sw.WriteLine("	pop	bc					;discard last offset");
				sw.WriteLine("	ld	c,#fe				; prepare negative offset");
				sw.WriteLine("	call dzx0s_elias_loop	;obtain offset MSB");
				sw.WriteLine("	inc	c");
			}
			else {
				sw.WriteLine("	call	dzx0s_elias		; obtain offset MSB");
				sw.WriteLine("	ld b,a");
				sw.WriteLine("	pop	af					; discard last offset");
				sw.WriteLine("	xor	a					; adjust for negative offset");
				sw.WriteLine("	sub	c");
			}
			sw.WriteLine((jumpLabel != null ? ("	JP	Z," + jumpLabel) : "	RET	Z") + "		; Plus d'octets à traiter = fini" + Environment.NewLine);
			if (!v2) {
				sw.WriteLine("	ld	c,a");
				sw.WriteLine("	ld	a,b");
			}
			sw.WriteLine("	ld	b,c");
			sw.WriteLine("	ld	c,(hl)				; obtain offset LSB");
			sw.WriteLine("	inc	hl");
			sw.WriteLine("	rr	b					; last offset bit becomes first length bit");
			sw.WriteLine("	rr	c");
			sw.WriteLine("	push	bc				; preserve new offset");
			sw.WriteLine("	ld	bc,1				; obtain length");
			sw.WriteLine("	call	nc,dzx0s_elias_backtrack");
			sw.WriteLine("	inc	bc");
			sw.WriteLine("	jr	dzx0s_copy");
			sw.WriteLine("dzx0s_elias");
			sw.WriteLine("	inc	c					; interlaced Elias gamma coding");
			sw.WriteLine("dzx0s_elias_loop");
			sw.WriteLine("	add	a,a");
			sw.WriteLine("	jr	nz,dzx0s_elias_skip");
			sw.WriteLine("	ld	a,(hl)				; load another group of 8 bits");
			sw.WriteLine("	inc	hl");
			sw.WriteLine("	rla");
			sw.WriteLine("dzx0s_elias_skip");
			sw.WriteLine("	ret 	c");
			sw.WriteLine("dzx0s_elias_backtrack");
			sw.WriteLine("	add	a,a");
			sw.WriteLine("	rl	c");
			sw.WriteLine("	rl	b");
			sw.WriteLine("	jr	dzx0s_elias_loop");
		}

		static private void GenereDZX1(StreamWriter sw, string jumpLabel) {
			sw.WriteLine("; Decompactage");
			sw.WriteLine("Depack:");
			sw.WriteLine("	ld	bc,#ffff			; preserve default offset 1");
			sw.WriteLine("	push	bc");
			sw.WriteLine("	ld	a,#80");
			sw.WriteLine("dzx1s_literals:");
			sw.WriteLine("	call	dzx1s_elias		; obtain length");
			sw.WriteLine("	ldir					; copy literals");
			sw.WriteLine("	add	a,a					; copy from last offset or new offset?");
			sw.WriteLine("	jr	c,dzx1s_new_offset");
			sw.WriteLine("	call	dzx1s_elias		; obtain length");
			sw.WriteLine("dzx1s_copy:");
			sw.WriteLine("	ex	(sp),hl				; preserve source,restore offset");
			sw.WriteLine("	push	hl				; preserve offset");
			sw.WriteLine("	add	hl,de				; calculate destination - offset");
			sw.WriteLine("	ldir					; copy from offset");
			sw.WriteLine("	pop	hl					; restore offset");
			sw.WriteLine("	ex	(sp),hl				; preserve offset,restore source");
			sw.WriteLine("	add	a,a					; copy from literals or new offset?");
			sw.WriteLine("	jr	nc,dzx1s_literals");
			sw.WriteLine("dzx1s_new_offset:");
			sw.WriteLine("	inc	sp					; discard last offset");
			sw.WriteLine("	inc	sp");
			sw.WriteLine("	dec	b");
			sw.WriteLine("	ld	c,(hl)				; obtain offset LSB");
			sw.WriteLine("	inc	hl");
			sw.WriteLine("	rr	c					; single byte offset?");
			sw.WriteLine("	jr	nc,dzx1s_msb_skip");
			sw.WriteLine("	ld	b,(hl)				; obtain offset MSB");
			sw.WriteLine("	inc	hl");
			sw.WriteLine("	rr	b					; replace last LSB bit with last MSB bit");
			sw.WriteLine("	inc	b");
			sw.WriteLine((jumpLabel != null ? ("	JP	Z," + jumpLabel) : "	RET	Z") + "		; Plus d'octets à traiter = fini" + Environment.NewLine);
			sw.WriteLine("	rl	c");
			sw.WriteLine("dzx1s_msb_skip:");
			sw.WriteLine("	push	bc				; preserve new offset");
			sw.WriteLine("	call	dzx1s_elias		; obtain length");
			sw.WriteLine("	inc	bc");
			sw.WriteLine("	jr	dzx1s_copy");
			sw.WriteLine("dzx1s_elias:");
			sw.WriteLine("	ld	bc,1				; interlaced Elias gamma coding");
			sw.WriteLine("dzx1s_elias_loop:");
			sw.WriteLine("	add	a,a");
			sw.WriteLine("	jr	nz,dzx1s_elias_skip");
			sw.WriteLine("	ld	a,(hl)				; load another group of 8 bits");
			sw.WriteLine("	inc	hl");
			sw.WriteLine("	rla");
			sw.WriteLine("dzx1s_elias_skip:");
			sw.WriteLine("	ret	nc");
			sw.WriteLine("	add	a,a");
			sw.WriteLine("	rl	c");
			sw.WriteLine("	rl	b");
			sw.WriteLine("	jr	dzx1s_elias_loop");
		}

		static private void GenereDStd(StreamWriter sw, string jumpLabel = null) {
			sw.WriteLine("; Decompactage");
			sw.WriteLine("Depack:");
			sw.WriteLine("	LD	A,(HL)			; DepackBits = InBuf[ InBytes++ ]");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	RRA				; Rotation rapide calcul seulement flag C");
			sw.WriteLine("	SET	7,A			; Positionne bit 7 en gardant flag C");
			sw.WriteLine("	LD	(BclLzw+1),A");
			sw.WriteLine("	JR	C,TstCodeLzw");
			sw.WriteLine("CopByteLzw:");
			sw.WriteLine("	LDI				; OutBuf[ OutBytes++ ] = InBuf[ InBytes++ ]" + Environment.NewLine);
			sw.WriteLine("BclLzw:");
			sw.WriteLine("	LD	A,0");
			sw.WriteLine("	RR	A			; Rotation avec calcul Flags C et Z");
			sw.WriteLine("	LD	(BclLzw+1),A");
			sw.WriteLine("	JR	NC,CopByteLzw");
			sw.WriteLine("	JR	Z,Depack" + Environment.NewLine);
			sw.WriteLine("TstCodeLzw:");
			sw.WriteLine("	LD	A,(HL)			; A = InBuf[ InBytes ];");
			sw.WriteLine("	AND	A");
			sw.WriteLine((jumpLabel != null ? ("	JP	Z," + jumpLabel) : "	RET	Z") + "		; Plus d'octets à traiter = fini" + Environment.NewLine);
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	B,A			; B = InBuf[ InBytes ]");
			sw.WriteLine("	RLCA				; A & #80 ?");
			sw.WriteLine("	JR	NC,TstLzw40" + Environment.NewLine);
			sw.WriteLine("	RLCA");
			sw.WriteLine("	RLCA");
			sw.WriteLine("	RLCA");
			sw.WriteLine("	AND	7");
			sw.WriteLine("	ADD	A,3			; Longueur = 3 + ( ( InBuf[ InBytes ] >> 4 ) & 7 );");
			sw.WriteLine("	LD	C,A			; C = Longueur");
			sw.WriteLine("	LD	A,B			; B = InBuf[InBytes]");
			sw.WriteLine("	AND	#0F			; Delta = ( InBuf[ InBytes++ ] & 15 ) << 8");
			sw.WriteLine("	LD	B,A			; B = poids fort Delta");
			sw.WriteLine("	LD	A,C			; A = Length");
			sw.WriteLine("	SCF				; Repositionner flag C (pour Delta++)");
			sw.WriteLine("CopyBytes0:");
			sw.WriteLine("	LD	C,(HL)			; C = poids faible Delta (Delta |= InBuf[ InBytes++ ]);");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	PUSH	HL");
			sw.WriteLine("	LD	H,D");
			sw.WriteLine("	LD	L,E");
			sw.WriteLine("	SBC	HL,BC			; HL=HL-(BC+1)");
			sw.WriteLine("	LD	B,0");
			sw.WriteLine("CopyBytes1:");
			sw.WriteLine("	LD	C,A");
			sw.WriteLine("CopyBytes2:");
			sw.WriteLine("	LDIR");
			sw.WriteLine("CopyBytes3:");
			sw.WriteLine("	POP	HL");
			sw.WriteLine("	JR	BclLzw" + Environment.NewLine);
			sw.WriteLine("TstLzw40:");
			sw.WriteLine("	RLCA				; A & #40 ?");
			sw.WriteLine("	JR	NC,TstLzw20" + Environment.NewLine);
			sw.WriteLine("	LD	C,B");
			sw.WriteLine("	RES	6,C			; Delta = 1 + InBuf[ InBytes++ ] & #3f;");
			sw.WriteLine("	LD	B,0			; BC = Delta + 1 car flag C = 1");
			sw.WriteLine("	PUSH	HL");
			sw.WriteLine("	LD	H,D");
			sw.WriteLine("	LD	L,E");
			sw.WriteLine("	SBC	HL,BC");
			sw.WriteLine("	LDI");
			sw.WriteLine("	LDI				; Longueur = 2");
			sw.WriteLine("	JR	CopyBytes3" + Environment.NewLine);
			sw.WriteLine("TstLzw20:");
			sw.WriteLine("	RLCA				; A & #20 ?");
			sw.WriteLine("	JR	NC,TstLzw10" + Environment.NewLine);
			sw.WriteLine("	LD	A,B			; B compris entre #20 et #3F");
			sw.WriteLine("	ADD	A,#E2			; = ( A AND #1F ) + 2,et positionne carry");
			sw.WriteLine("	LD	B,0			; Longueur = 2 + ( InBuf[ InBytes++ ] & 31 );");
			sw.WriteLine("	JR	CopyBytes0" + Environment.NewLine);
			sw.WriteLine("CodeLzw0F:");
			sw.WriteLine("	LD	C,(HL)");
			sw.WriteLine("	PUSH	HL");
			sw.WriteLine("	LD	H,D");
			sw.WriteLine("	LD	L,E");
			sw.WriteLine("	CP	#F0");
			sw.WriteLine("	JR	NZ,CodeLzw02" + Environment.NewLine);
			sw.WriteLine("	XOR	A");
			sw.WriteLine("	LD	B,A");
			sw.WriteLine("	INC	BC			; Longueur = Delta = InBuf[ InBytes + 1 ] + 1;");
			sw.WriteLine("	SBC	HL,BC");
			sw.WriteLine("	LDIR");
			sw.WriteLine("	POP	HL");
			sw.WriteLine("	INC	HL			; Inbytes += 2");
			sw.WriteLine("	JR	BclLzw" + Environment.NewLine);
			sw.WriteLine("CodeLzw02:");
			sw.WriteLine("	CP	#20");
			sw.WriteLine("	JR	C,CodeLzw01" + Environment.NewLine);
			sw.WriteLine("	LD	C,B			; Longueur = Delta = InBuf[ InBytes ];");
			sw.WriteLine("	LD	B,0");
			sw.WriteLine("	SBC	HL,BC");
			sw.WriteLine("	JR	CopyBytes2" + Environment.NewLine);
			sw.WriteLine("CodeLzw01:				; Ici,B = 1");
			sw.WriteLine("	XOR	A			; Carry a zéro");
			sw.WriteLine("	DEC	H			; Longueur = Delta = 256");
			sw.WriteLine("	JR	CopyBytes1" + Environment.NewLine);
			sw.WriteLine("TstLzw10:");
			sw.WriteLine("	RLCA				; A & #10 ?");
			sw.WriteLine("	JR	NC,CodeLzw0F" + Environment.NewLine);
			sw.WriteLine("	RES	4,B			; B = Delta(high) -> ( InBuf[ InBytes++ ] & 15 ) << 8;");
			sw.WriteLine("	LD	C,(HL)			; C = Delta(low)  -> InBuf[ InBytes++ ];");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	A,(HL)			; A = Longueur - 1");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	PUSH	HL");
			sw.WriteLine("	LD	H,D");
			sw.WriteLine("	LD	L,E");
			sw.WriteLine("	SBC	HL,BC			; Flag C=1 -> hl=hl-(bc+1) (Delta+1)");
			sw.WriteLine("	LD	B,0");
			sw.WriteLine("	LD	C,A");
			sw.WriteLine("	INC	BC			; BC =  Longueur = InBuf[ InBytes++ ] + 1;");
			sw.WriteLine("	JR	CopyBytes2" + Environment.NewLine);
		}

		static private void GenereFormatEcran(StreamWriter sw, bool noChangeAdr = false) {
			if (Cpc.NbCol != 80) {
				sw.WriteLine("	LD	HL,#" + ((Cpc.NbCol + 1) >> 1).ToString("X2") + (26 + (Cpc.NbCol >> 2)).ToString("X2"));
				sw.WriteLine("	LD	BC,#BC01");
				sw.WriteLine("	OUT	(C),C");
				sw.WriteLine("	INC	B");
				sw.WriteLine("	OUT	(C),H");
				sw.WriteLine("	DEC	B");
				sw.WriteLine("	INC	C");
				sw.WriteLine("	OUT	(C),C");
				sw.WriteLine("	INC	B");
				sw.WriteLine("	OUT	(C),L");
			}
			if (Cpc.NbLig != 200) {
				sw.WriteLine("	LD	HL,#" + ((Cpc.NbLig + 7) >> 3).ToString("X2") + (18 + (Cpc.NbLig >> 4)).ToString("X2"));
				sw.WriteLine("	LD	BC,#BC06");
				sw.WriteLine("	OUT	(C),C");
				sw.WriteLine("	INC	B");
				sw.WriteLine("	OUT	(C),H");
				sw.WriteLine("	DEC	B");
				sw.WriteLine("	INC	C");
				sw.WriteLine("	OUT	(C),C");
				sw.WriteLine("	INC	B");
				sw.WriteLine("	OUT	(C),L");
			}
			if (Cpc.NbLig * Cpc.NbCol > 0x4000 && !noChangeAdr) {
				sw.WriteLine("	LD	BC,#BC0C");
				sw.WriteLine("	OUT	(C),C");
				sw.WriteLine("	INC	B");
				sw.WriteLine("	INC	C");
				sw.WriteLine("	OUT	(C),C");
			}
		}

		static public void GenereInitOld(StreamWriter sw, string labelPalette) {
			sw.WriteLine("	LD	HL," + labelPalette);
			sw.WriteLine("	LD	B,#7F");
			sw.WriteLine("	XOR	A");
			sw.WriteLine("SetPal:");
			sw.WriteLine("	OUT	(C),A");
			sw.WriteLine("	INC	B");
			sw.WriteLine("	OUTI");
			sw.WriteLine("	INC	A");
			sw.WriteLine("	CP	18");
			sw.WriteLine("	JR	C,SetPal");
		}

		static public void GenereInitPlus(StreamWriter sw, string labelPalette) {
			sw.WriteLine("	LD	BC,#BC11");
			sw.WriteLine("	LD	HL,UnlockAsic");
			sw.WriteLine("Unlock:");
			sw.WriteLine("	LD	A,(HL)");
			sw.WriteLine("	OUT	(C),A");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	DEC	C");
			sw.WriteLine("	JR	NZ,Unlock");
			sw.WriteLine("	LD	BC,#7FB8");
			sw.WriteLine("	LD	A,(" + labelPalette + ")");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	OUT	(C),A");
			sw.WriteLine("	LD	HL," + labelPalette + "+1");
			sw.WriteLine("	LD	DE,#6400");
			sw.WriteLine("	LD	BC,#0022");
			sw.WriteLine("	LDIR");
			sw.WriteLine("	LD	BC,#7FA0");
			sw.WriteLine("	OUT	(C),C");
		}

		static public void GenerePalette(StreamWriter sw, Param p, bool withUnlockAsic, bool withMode, string label) {
			if (Cpc.cpcPlus) {
				if (withUnlockAsic) {
					sw.WriteLine("UnlockAsic:");
					sw.WriteLine("	DB	#FF,#00,#FF,#77,#B3,#51,#A8,#D4,#62,#39,#9C,#46,#2B,#15,#8A,#CD,#EE");
				}
				if (!string.IsNullOrEmpty(label))
					sw.WriteLine(label);

				if (withMode)
					sw.WriteLine("\tDB\t#" + ((Cpc.modeVirtuel == 7 ? 1 : Cpc.modeVirtuel & 3) | 0x8C).ToString("X2"));

				string line = "	DW	";
				for (int i = 0; i < 17; i++) {
					int c = i >= 16 || p.disableState[i] == 0 ? Cpc.Palette[i < Cpc.MaxPen() ? i : 0] : 0xFFFF;
					line += "#" + ((byte)(c >> 8)).ToString("X1") + ((byte)(((c >> 4) & 0x0F) | (c << 4))).ToString("X2") + ",";
				}
				sw.WriteLine(line.Substring(0, line.Length - 1));
			}
			else {
				if (!string.IsNullOrEmpty(label))
					sw.WriteLine(label);

				string line = "\tDB\t";
				for (int i = 0; i < 17; i++) {
					int k = i >= 16 || p.disableState[i] == 0 ? Cpc.Palette[i < Cpc.MaxPen() ? i : 0] : -1;
					line += "#" + (k == -1 ? "FF" : ((int)Cpc.CpcVGA[k < 27 ? k : 0]).ToString("X2")) + ",";
				}
				if (withMode)
					line += "#" + ((Cpc.modeVirtuel == 7 ? 1 : Cpc.modeVirtuel & 3) | 0x8C).ToString("X2");
				else
					line = line.Substring(0, line.Length - 1);

				sw.WriteLine(line);
			}
		}
		#endregion

		#region Code assembleur pour affichage animations
		static public void GenereEntete(StreamWriter sw, int adr) {
			if (Cpc.NbLig * Cpc.NbCol > 0x4000)
				adr = 0x8000;

			sw.WriteLine("	ORG	#" + adr.ToString("X4"));
			sw.WriteLine("	RUN	$" + Environment.NewLine);
			sw.WriteLine("	DI");
			GenereFormatEcran(sw);
		}

		static public void GenereAffichage(StreamWriter sw, bool delai, bool reboucle, bool gest128K, bool imageMode, Main.PackMethode pkMethode) {
			if (delai) {
				sw.WriteLine("	LD	HL,NewIrq");
				sw.WriteLine("	LD	(#39),HL");
				sw.WriteLine("	EI");
			}
			if (Cpc.modeVirtuel > 7) {
				sw.WriteLine("	LD	BC,#F00");
				sw.WriteLine("	LD	DE,Fonte");
				sw.WriteLine("SetFonte:");
				sw.WriteLine("	LD	HL,DataFnt");
				sw.WriteLine("	LD	A,C");
				sw.WriteLine("	AND	B");
				sw.WriteLine("	ADD	A,L");
				sw.WriteLine("	LD	L,A");
				sw.WriteLine("	LD	A,(HL)");
				sw.WriteLine("	LD	(DE),A");
				sw.WriteLine("	INC	DE");
				sw.WriteLine("	LD	HL,DataFnt");
				sw.WriteLine("	LD	A,C");
				sw.WriteLine("	RRCA");
				sw.WriteLine("	RRCA");
				sw.WriteLine("	RRCA");
				sw.WriteLine("	RRCA");
				sw.WriteLine("	AND	B");
				sw.WriteLine("	ADD	A,L");
				sw.WriteLine("	LD	L,A");
				sw.WriteLine("	LD	A,(HL)");
				sw.WriteLine("	LD	(DE),A");
				sw.WriteLine("	INC	DE");
				sw.WriteLine("	INC	C");
				sw.WriteLine("	JR	NZ,SetFonte");
			}
			sw.WriteLine("Debut");
			if (imageMode)
				sw.WriteLine("LD	HL,Delta0");
			else {
				sw.WriteLine("	LD	IX,AnimDelta");
				sw.WriteLine("Boucle:");
				if (gest128K) {
					sw.WriteLine("	LD	BC,#7FC0");
					sw.WriteLine("	OUT	(C),C");
				}
				sw.WriteLine("	LD	H,(IX+1)");
				sw.WriteLine("	LD	L,(IX+0)");
				sw.WriteLine("	LD	A,H");
				sw.WriteLine("	OR	H");
				if (reboucle) {
					sw.WriteLine("	JR	NZ,Boucle2");
					sw.WriteLine("	LD	IX,AnimDelta+" + (2 + (gest128K ? 1 : 0) + (delai ? 1 : 0)).ToString());
					sw.WriteLine("	JR	Boucle");

					sw.WriteLine("Boucle2:");
				}
				else
					sw.WriteLine("	RET	Z");
			}
			if (delai) {
				sw.WriteLine("	LD	A,(IX+2)");
				sw.WriteLine("	LD	(SyncFrame+3),A");
			}
			if (gest128K) {
				sw.WriteLine("	LD	B,#7F");
				sw.WriteLine("	LD	A,(IX+" + (delai ? "3" : "2") + ")");
				sw.WriteLine("	OUT	(C),A");
			}
			sw.WriteLine("	LD	DE,Buffer");
			GenereDepack(sw, pkMethode, "InitDraw");
			if (delai) {
				sw.WriteLine("NewIrq:");
				sw.WriteLine("	PUSH	AF");
				sw.WriteLine("	LD	A,(SyncFrame+1)");
				sw.WriteLine("	INC	A");
				sw.WriteLine("	LD	(SyncFrame+1),A");
				sw.WriteLine("	POP	AF");
				sw.WriteLine("	EI");
				sw.WriteLine("	RET");
			}
			sw.WriteLine("InitDraw:");
			if (delai) {
				sw.WriteLine("SyncFrame:");
				sw.WriteLine("	LD	A,0");
				sw.WriteLine("	CP	1");
				sw.WriteLine("	JR	C,SyncFrame");
				sw.WriteLine("	XOR	A");
				sw.WriteLine("	LD	(SyncFrame+1),A");
			}
		}

		static public void GenereDrawDC(StreamWriter sw, bool delai, bool modeCol, bool gest128K, int addBC26, bool optimSpeed) {
			sw.WriteLine("	LD	HL,buffer");
			if (modeCol) {
				sw.WriteLine("	LD	B,(HL)		; Taille en X");
				sw.WriteLine("	INC	HL");
				sw.WriteLine("	LD	A,(HL)");
				sw.WriteLine("	INC	HL");
				sw.WriteLine("	LD	(NbLig+1),A		; Taille en Y");
				sw.WriteLine("	LD	E,(HL)");
				sw.WriteLine("	INC	HL");
				sw.WriteLine("	LD	D,(HL)		; Adresse écran");
				sw.WriteLine("	INC	HL");
				sw.WriteLine("	EX	DE,HL");
				if (delai)
					sw.WriteLine("	DI");

				sw.WriteLine("	LD	(SauvSp+1),SP");
				sw.WriteLine("	LD	SP,#C0" + Cpc.NbCol.ToString("X2"));
				sw.WriteLine("Draw1:");
				sw.WriteLine("	LD	(RecupHL+1),HL");
				sw.WriteLine("NbLig:");
				sw.WriteLine("	LD	C,0");
				sw.WriteLine("Draw2:");
				sw.WriteLine("	LD	A,(DE)");
				sw.WriteLine("	INC	DE");
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	LD	A,H");
				sw.WriteLine("	ADD	A,#" + (addBC26 + 1).ToString("X2"));
				sw.WriteLine("	LD	H,A");
				sw.WriteLine("	JR	NC,Draw3");
				sw.WriteLine("	ADD	HL,SP");
				sw.WriteLine("Draw3:");
				sw.WriteLine("	DEC	C");
				sw.WriteLine("	JR	NZ,Draw2");
				sw.WriteLine("RecupHL:");
				sw.WriteLine("	LD	HL,0");
				sw.WriteLine("	INC	HL");
				sw.WriteLine("	DJNZ	Draw1");
				sw.WriteLine("SauvSp:");
				sw.WriteLine("	LD	SP,0");
				if (delai)
					sw.WriteLine("	EI");
			}
			else {
				sw.WriteLine("	LD	A,(HL)");
				sw.WriteLine("	INC	HL");
				sw.WriteLine("	LD	(Nbx+1),A		; Taille en X");
				if (!optimSpeed) {
					sw.WriteLine("	NEG");
					sw.WriteLine("	LD	(NbDec+1),A		; Ajustement pour 'BC26'");
					sw.WriteLine("	LD	A,(HL)			; Taille en Y");
					sw.WriteLine("	INC	HL");
					sw.WriteLine("	LD	E,(HL)");
					sw.WriteLine("	INC	HL");
					sw.WriteLine("	LD	D,(HL)			; Adresse ecran");
					sw.WriteLine("	INC	HL");
				}
				else {
					sw.WriteLine("	LD	A,(HL)			; Taille en Y");
					sw.WriteLine("	INC	HL");
				}
				sw.WriteLine("Nbx:");
				sw.WriteLine("	LD	BC,0");
				if (optimSpeed) {
					sw.WriteLine("	LD	E,(HL)");
					sw.WriteLine("	INC	HL");
					sw.WriteLine("	LD	D,(HL)			; Adresse ecran");
					sw.WriteLine("	INC	HL");
				}
				sw.WriteLine("	LDIR");
				if (!optimSpeed) {
					sw.WriteLine("	EX	DE,HL");
					sw.WriteLine("NbDec:");
					sw.WriteLine("	LD	BC,#" + addBC26.ToString("X2") + "FF			; 'BC26'");
					sw.WriteLine("	ADD	HL,BC");
					sw.WriteLine("	JR	NC,Suite");
					sw.WriteLine("	LD	BC,#C0" + Cpc.NbCol.ToString("X2"));
					sw.WriteLine("	ADD	HL,BC");
					sw.WriteLine("Suite:");
					sw.WriteLine("	EX	DE,HL");
				}
				sw.WriteLine("	DEC	A");
				sw.WriteLine("	JR	NZ,Nbx");
			}
			sw.WriteLine("	INC	IX");
			sw.WriteLine("	INC	IX");
			if (delai)
				sw.WriteLine("	INC	IX");

			if (gest128K)
				sw.WriteLine("	INC	IX");

			sw.WriteLine("	JP	Boucle" + Environment.NewLine);
			sw.WriteLine("	Nolist");
		}

		static public void GenereDrawDirect(StreamWriter sw, bool gest128K) {
			sw.WriteLine("	LD	HL,buffer");
			sw.WriteLine("	LD	DE,#C000");
			sw.WriteLine("	LD	C,(HL)");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	B,(HL)");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("DrawImgD1:");
			sw.WriteLine("	LD	A,(HL)			; Deplacement");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	BIT	7,A");
			sw.WriteLine("	JR	NZ,DrawImgD4");
			sw.WriteLine("	ADD	A,E");
			sw.WriteLine("	JR	NC,DrawImgD2");
			sw.WriteLine("	INC	D");
			sw.WriteLine("DrawImgD2:");
			sw.WriteLine("	LD	E,A");
			sw.WriteLine("	LDI				; Octet a copier");
			sw.WriteLine("DrawImgD3:");
			sw.WriteLine("	JP	PE,DrawImgD1");
			sw.WriteLine("	INC	IX");
			sw.WriteLine("	INC	IX");
			sw.WriteLine("	INC	IX");
			if (gest128K)
				sw.WriteLine("	INC	IX");

			sw.WriteLine("	JP	Boucle" + Environment.NewLine);
			sw.WriteLine("DrawImgD4:");
			sw.WriteLine("	AND	#7F");
			sw.WriteLine("	ADD	A,E");
			sw.WriteLine("	JR	NC,DrawImgD5");
			sw.WriteLine("	INC	D");
			sw.WriteLine("DrawImgD5:");
			sw.WriteLine("	LD	E,A");
			sw.WriteLine("	PUSH	BC");
			sw.WriteLine("	LD	B,(HL)");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	A,(HL)");
			sw.WriteLine("DrawImgD6:");
			sw.WriteLine("	LD	(DE),A");
			sw.WriteLine("	INC	DE");
			sw.WriteLine("	DJNZ	DrawImgD6");
			sw.WriteLine("	POP	BC");
			sw.WriteLine("	CPI");
			sw.WriteLine("	JR	DrawImgD3" + Environment.NewLine);
			sw.WriteLine("	Nolist");
		}

		static public void GenereDrawBlock(StreamWriter sw, int height, bool delai, bool gest128K) {
			sw.WriteLine("	LD	HL,buffer");
			sw.WriteLine("	LD	A,(HL)");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	(TailleBlock1+1),A");
			sw.WriteLine("	LD	C,A");
			sw.WriteLine("	XOR	A");
			sw.WriteLine("	SUB	C");
			sw.WriteLine("	LD	(TailleBlock2+1),A");
			sw.WriteLine("LoopBlock:");
			sw.WriteLine("	LD	A,(HL)");
			sw.WriteLine("	AND	A");
			sw.WriteLine("	JR	Z,FinBlock");
			sw.WriteLine("	LD	D,A");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	E,(HL)		; Adresse écran");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	LD	A," + height.ToString());
			sw.WriteLine("TailleBlock1:");
			sw.WriteLine("	LD	BC,4");
			sw.WriteLine("	LDIR");
			sw.WriteLine("	EX	DE,HL");
			sw.WriteLine("TailleBlock2:");
			sw.WriteLine("	LD	BC,#7FF");
			sw.WriteLine("	ADD	HL,BC");
			if (height != 8) {
				sw.WriteLine("	JR	NC,BlockOK");
				sw.WriteLine("	LD	BC,#C0" + Cpc.NbCol.ToString("X2"));
				sw.WriteLine("	ADD	HL,BC");
				sw.WriteLine("BlockOk:");
			}
			sw.WriteLine("	EX	DE,HL");
			sw.WriteLine("	DEC	A");
			sw.WriteLine("	JR	NZ,TailleBlock1");
			sw.WriteLine("	JR	LoopBlock");
			sw.WriteLine("FinBlock:");
			sw.WriteLine("	INC	IX");
			sw.WriteLine("	INC	IX");
			if (delai)
				sw.WriteLine("	INC	IX");

			if (gest128K)
				sw.WriteLine("	INC	IX");

			sw.WriteLine("	JP	Boucle" + Environment.NewLine);
			sw.WriteLine("	Nolist");
		}

		static public void GenereDrawAscii(StreamWriter sw, bool frameFull, bool frameO, bool frameD, bool gest128K, bool imageMode, bool withSpeed) {
			bool overscan = Cpc.NbLig * Cpc.NbCol > 0x4000;
			if (frameFull && !imageMode) {
				sw.WriteLine("	LD	HL,Buffer");
				sw.WriteLine("	LD	A,(HL)");
				sw.WriteLine("	CP	'D'");
				sw.WriteLine("	JR	Z,DrawImgD");
				sw.WriteLine("	CP	'I'");
				sw.WriteLine("	JR	Z,DrawImgI");
			}
			else
				if (frameO)
					sw.WriteLine("	LD	HL,Buffer");

			if (!frameD) {
				sw.WriteLine("	LD	BC," + (overscan ? "#0200" : "#C000"));
				if (Cpc.modeVirtuel == 7)
					sw.WriteLine("	LD	D,Fonte/256");

				sw.WriteLine("DrawImgO:");
				if (Cpc.modeVirtuel > 7) {
					sw.WriteLine("	LD	D,Fonte/512");
					if (!frameO)
						sw.WriteLine("	INC	HL");

					sw.WriteLine("	LD	E,(HL)			; Code ASCII");
					sw.WriteLine("	EX	DE,HL");
					sw.WriteLine("	ADD	HL,HL");
				}
				else {
					if (!frameO)
						sw.WriteLine("	INC	HL");

					sw.WriteLine("	LD	A,(HL)			; Code ASCII");
					sw.WriteLine("	AND	#0F");
					sw.WriteLine("	RLCA");
					sw.WriteLine("	RLCA");
					sw.WriteLine("	LD	E,A");
					sw.WriteLine("	EX	DE,HL");
				}
				sw.WriteLine("	LD	A,(HL)");
				sw.WriteLine("	INC	L");
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	SET	3,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(HL)");
					sw.WriteLine("	INC	L");
				}
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	SET	4,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(HL)");
					sw.WriteLine("	INC	L");
				}
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	RES	3,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(HL)");
					sw.WriteLine("	INC	L");
				}
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	SET	5,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)			; Code ASCII");
					sw.WriteLine("	AND	#F0");
					sw.WriteLine("	RRCA");
					sw.WriteLine("	RRCA");
					sw.WriteLine("	ADD	A,3");
					sw.WriteLine("	LD	L,A");
				}
				sw.WriteLine("	LD	A,(HL)");
				if (Cpc.modeVirtuel == 7)
					sw.WriteLine("	DEC	L");
				else
					sw.WriteLine("	INC	HL");

				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	SET	3,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(HL)");
					sw.WriteLine("	DEC	L");
				}
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	RES	4,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(HL)");
					sw.WriteLine("	DEC	L");
				}
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	RES	3,B");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(HL)");
				}
				sw.WriteLine("	LD	(BC),A");
				sw.WriteLine("	RES	5,B");
				sw.WriteLine("	EX	DE,HL");
				if (frameO)
					sw.WriteLine("	INC	HL");

				sw.WriteLine("	INC	BC");
				sw.WriteLine("	BIT	3,B");
				sw.WriteLine("	JR	Z,DrawImgO");
				if (overscan) {
					sw.WriteLine("	LD	A,B");
					sw.WriteLine("	ADD	A,#40");
					sw.WriteLine("	LD	B,A");
					sw.WriteLine("	JP	P,DrawImgO");
				}
			}
			if (!frameD) {
				if (!imageMode) {
					sw.WriteLine("DrawImgI:");
					sw.WriteLine("	INC	IX");
					sw.WriteLine("	INC	IX");
					if (withSpeed)
						sw.WriteLine("	INC	IX");

					if (gest128K)
						sw.WriteLine("	INC	IX");

					sw.WriteLine("	JP	Boucle");
				}
				else
					GenereTspace(sw, true);
			}
			if (!frameO) {
				if (!frameD)
					sw.WriteLine("DrawImgD:");

				sw.WriteLine("	LD	HL,#C000");
				if (frameD)
					sw.WriteLine("	LD	IY,Buffer");
				else
					sw.WriteLine("	LD	IY,Buffer+1");

				sw.WriteLine("	LD	C,(IY+0)");
				sw.WriteLine("	LD	B,(IY+1)");
				if (frameD) {
					sw.WriteLine("	LD	A,B");
					sw.WriteLine("	OR	C");
					sw.WriteLine("	JR	Z,EndDraw");
				}
				sw.WriteLine("DrawImgD1:");
				sw.WriteLine("	LD	D,0");
				sw.WriteLine("	LD	E,(IY+2)			; Déplacement");
				sw.WriteLine("	ADD	HL,DE			; Ajouter à aresse écran");
				if (Cpc.modeVirtuel > 7) {
					sw.WriteLine("	EX	DE,HL");
					sw.WriteLine("	LD	H,Fonte/512");
					sw.WriteLine("	LD	L,(IY+3)			; Code ASCII");
					sw.WriteLine("	ADD	HL,HL");
					sw.WriteLine("	EX	DE,HL");
				}
				else {
					sw.WriteLine("	LD	D,Fonte/256");
					sw.WriteLine("	LD	A,(IY+3)			; Code ASCII");
					sw.WriteLine("	AND	#0F");
					sw.WriteLine("	RLCA");
					sw.WriteLine("	RLCA");
					sw.WriteLine("	LD	E,A");
				}
				sw.WriteLine("	LD	A,(DE)");
				sw.WriteLine("	INC	E");
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	SET	3,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)");
					sw.WriteLine("	INC	E");
				}
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	SET	4,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)");
					sw.WriteLine("	INC	E");
				}
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	RES	3,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)");
					sw.WriteLine("	INC	E");
				}
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	SET	5,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(IY+3)			; Code ASCII");
					sw.WriteLine("	AND	#F0");
					sw.WriteLine("	RRCA");
					sw.WriteLine("	RRCA");
					sw.WriteLine("	ADD	A,3");
					sw.WriteLine("	LD	E,A");
				}
				sw.WriteLine("	LD	A,(DE)");
				if (Cpc.modeVirtuel == 7)
					sw.WriteLine("	DEC	E");

				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	SET	3,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)");
					sw.WriteLine("	DEC E");
				}
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	RES	4,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)");
					sw.WriteLine("	DEC	E");
				}
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	RES	3,H");
				if (Cpc.modeVirtuel == 7) {
					sw.WriteLine("	LD	A,(DE)");
				}
				sw.WriteLine("	LD	(HL),A");
				sw.WriteLine("	RES	5,H");
				sw.WriteLine("	INC	IY");
				sw.WriteLine("	INC	IY");
				sw.WriteLine("	CPI");
				sw.WriteLine("	JP	PE,DrawImgD1");
				if (frameD) {
					if (!imageMode) {
						sw.WriteLine("EndDraw");
						sw.WriteLine("	INC	IX");
						sw.WriteLine("	INC	IX");
						if (withSpeed)
							sw.WriteLine("	INC	IX");

						if (gest128K)
							sw.WriteLine("	INC	IX");

						sw.WriteLine("	JP	Boucle");
					}
					else
						GenereTspace(sw, true);
				}
				else
					sw.WriteLine("	JR	DrawImgI");
			}
			sw.WriteLine("	Nolist");
		}

		static public void GenereFin(StreamWriter sw, int ltot, bool force8000, bool withCode) {
			sw.WriteLine("; Taille totale animation = " + ltot.ToString() + " (#" + ltot.ToString("X4") + ")");
			sw.WriteLine("_EndCode:");
			sw.WriteLine("	List");
			if (Cpc.modeVirtuel >= 7) {
				int align = Cpc.modeVirtuel == 7 ? 256 : 512;
				sw.WriteLine("	Align	" + align.ToString() + "		; Doit être un multiple de " + align.ToString());
				sw.WriteLine("Fonte:");
				if (Cpc.modeVirtuel == 7) {
					int[] ordreAdr = new int[4] { 0, 1, 3, 2 };
					for (int i = 0; i < 16; i++) {
						string str = "	DB	";
						for (int ym = 0; ym < 4; ym++) {
							byte octet = 0;
							for (int xm = 0; xm < 4; xm++)
								octet |= (byte)(Cpc.tabOctetMode[Cpc.trameM1[i, xm, ordreAdr[ym]]] >> xm);

							str += "#" + octet.ToString("X2") + ",";
						}
						sw.WriteLine(str.Substring(0, str.Length - 1));
					}
				}
				else
					sw.WriteLine("	DS	" + align.ToString() + Environment.NewLine);
			}

			if (withCode && Cpc.modeVirtuel >= 7 && force8000)
				sw.WriteLine("	Org	#8000");

			sw.WriteLine("buffer" + (force8000 ? "	equ	#8000" : ":"));
			if (withCode && Cpc.modeVirtuel > 7) {
				sw.WriteLine("DataFnt:");
				sw.WriteLine("	DB	#00,#C0,#0C,#CC,#30,#F0,#3C,#FC,#03,#C3,#0F,#CF,#33,#F3,#3F,#FF");
			}
		}

		static public void GenerePointeurs(StreamWriter sw, int nbImages, int[] bank, int[] speed, bool gest128K) {
			if (gest128K) {
				sw.WriteLine("	ORG EndBank0");
				sw.WriteLine("	Write Direct -1,-1,#C0");
			}
			sw.WriteLine("AnimDelta:");
			for (int i = 0; i < nbImages; i++) {
				sw.WriteLine("\tDW\tDelta" + i.ToString());
				if (speed != null)
					sw.WriteLine("\tDB\t#" + Convert.ToInt32(Math.Max(1, Math.Min(255, speed[i] / 3.333333))).ToString("X2"));

				if (gest128K)
					sw.WriteLine("\tDB\t#" + bank[i].ToString("X2"));
			}
			sw.WriteLine("\tDW\t#0");
		}
		#endregion

		#region Code assembleur pour affichage images
		static private void GenereTspace(StreamWriter sw, bool wait, bool withoutRet = false) {
			sw.WriteLine("TstSpace:");
			sw.WriteLine("	LD	BC,#F40E");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	LD	BC,#F6C0");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	XOR	A");
			sw.WriteLine("	OUT	(C),A");
			sw.WriteLine("	LD	BC,#F792");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	LD	BC,#F645");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	LD	B,#F4");
			sw.WriteLine("	IN	A,(C)");
			sw.WriteLine("	LD	BC,#F782");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	LD	BC,#F600");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	INC	A");
			if (wait)
				sw.WriteLine("	JR	Z,TstSpace");

			if (!withoutRet)
				sw.WriteLine("	RET");
		}

		// #### A revoir...
		static public void GenereAfficheStd(StreamWriter sw, bool overscan, Main.PackMethode pkMethode, string labelMedia, string labelPalette) {
			sw.WriteLine("	DI");
			if (Cpc.cpcPlus)
				GenereInitPlus(sw, labelPalette);
			else
				GenereInitOld(sw, labelPalette);

			GenereFormatEcran(sw);
			if (pkMethode != Main.PackMethode.None) {
				sw.WriteLine("	LD	HL," + labelMedia);
				sw.WriteLine("	LD	DE,#" + (overscan ? "0200" : "C000"));
				sw.WriteLine("	CALL	Depack");
			}
			if (pkMethode == Main.PackMethode.ZX0Ovs) {
				sw.WriteLine("	EX	DE,HL");
				sw.WriteLine("	DEC	HL");
				sw.WriteLine("	LD	IX,Zones");
				sw.WriteLine("LoopDepack:");
				sw.WriteLine("	LD	A,(IX+1)");
				sw.WriteLine("	AND	A");
				sw.WriteLine("	JR	Z,TstSpace");
				sw.WriteLine("	LD	D,A");
				sw.WriteLine("	LD	E,(IX+0)");
				sw.WriteLine("	LD	B,(IX+3)");
				sw.WriteLine("	LD	C,(IX+2)");
				sw.WriteLine("	LDDR");
				sw.WriteLine("	INC	IX");
				sw.WriteLine("	INC	IX");
				sw.WriteLine("	INC	IX");
				sw.WriteLine("	INC	IX");
				sw.WriteLine("	JR	LoopDepack");
			}
			GenereTspace(sw, true, true);
			sw.WriteLine("	LD	BC,#BC0C");
			sw.WriteLine("	LD	A,#30");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	INC	B");
			sw.WriteLine("	OUT	(C),A");
			sw.WriteLine("	EI");
			sw.WriteLine("	RET");
			GenereDepack(sw, pkMethode);
			if (pkMethode == Main.PackMethode.ZX0Ovs) {
				sw.WriteLine("Zones:");
				sw.WriteLine("	DW	#7EBF,#6C0,#76BF,#6C0,#6EBF,#6C0,#66BF,#6C0,#5EBF,#6C0,#56BF,#6C0,#4EBF,#6C0,#46BF,#CC0");
				sw.WriteLine("	DW	#37FF,#600,#2FFF,#600,#27FF,#600,#1FFF,#600,#17FF,#600,#0FFF,#600 ;,#07FF,#600	; Si decompactage en #200, pas necessaire");
				sw.WriteLine("	DW	#0");
			}
		}

		static public void GenereAfficheModeX(StreamWriter sw, int[,] colMode5, bool isOverscan, Main.PackMethode pkMethode, string labelMedia) {
			GenereFormatEcran(sw, true);
			sw.WriteLine("	LD	HL," + labelMedia);
			sw.WriteLine("	LD	DE,#" + (isOverscan ? "0200" : "C000"));
			sw.WriteLine("	CALL	Depack");
			sw.WriteLine("	DI");
			sw.WriteLine("	LD	HL,#C9FB");
			sw.WriteLine("	LD	(#38),HL");
			sw.WriteLine("	EI");
			sw.WriteLine("WaitVbl:");
			sw.WriteLine("	LD	B,#F5");
			sw.WriteLine("	IN	A,(C)");
			sw.WriteLine("	RRA");
			sw.WriteLine("	JR	NC,WaitVbl");
			sw.WriteLine("WaitEnd:");
			sw.WriteLine("	IN	A,(C)");
			sw.WriteLine("	RRA");
			sw.WriteLine("	JR	C,WaitEnd");
			sw.WriteLine("	LD	HL,Color01			; Initialiser les couleurs fixes de l'image (0 et 1)");
			sw.WriteLine("	LD	BC,#7F00");
			sw.WriteLine("	OUT	(C),C			; Sélection pen 0");
			sw.WriteLine("	OUTI				; Mise à jour couleur");
			sw.WriteLine("	INC	C");
			sw.WriteLine("	OUT	(C),C			; Sélection pen 1");
			sw.WriteLine("	OUTI				; Mise à jour couleur");
			if (isOverscan) {
				sw.WriteLine("	LD	BC,#BC0C");
				sw.WriteLine("	OUT	(C),C");
				sw.WriteLine("	INC	B");
				sw.WriteLine("	INC	C");
				sw.WriteLine("	OUT	(C),C");
			}
			sw.WriteLine("	HALT");
			sw.WriteLine("	HALT");
			sw.WriteLine("	HALT");
			sw.WriteLine("	HALT");
			sw.WriteLine("	HALT");
			sw.WriteLine("	HALT");
			sw.WriteLine("	DI");
			sw.WriteLine("	LD	BC," + (Cpc.NbLig == 200 ? 634 : 268).ToString());
			sw.WriteLine("	DEC	BC");
			sw.WriteLine("	LD	A,B");
			sw.WriteLine("	OR	C");
			sw.WriteLine("	JR	NZ,$-3			; Attendre première ligne visible de l'écran");
			sw.WriteLine("Boucle:");
			sw.WriteLine("	LD	HL,ColorModeX		; Initaliser les couleurs variables");
			sw.WriteLine("	LD	DE," + Cpc.NbLig + "			; par lignes de l'image (2 et 3)");
			sw.WriteLine("LoopLineX:");
			sw.WriteLine("	LD	BC,#7F02");
			sw.WriteLine("	OUT	(C),C			; Sélection pen 2");
			sw.WriteLine("	OUTI				; Mise à jour couleur");
			sw.WriteLine("	INC C");
			sw.WriteLine("	OUT	(C),C			; Sélection pen 3");
			sw.WriteLine("	OUTI				; Mise à jour couleur");
			sw.WriteLine("	LD	B,8");
			sw.WriteLine("	DJNZ	$");
			sw.WriteLine("	CP	(HL)");
			sw.WriteLine("	DEC	DE");
			sw.WriteLine("	LD	A,D");
			sw.WriteLine("	OR	E");
			sw.WriteLine("	JR	NZ,LoopLineX");

			sw.WriteLine("	LD	BC," + (Cpc.NbLig == 200 ? 1022 : 364).ToString());
			sw.WriteLine("	DEC	BC");
			sw.WriteLine("	LD	A,B");
			sw.WriteLine("	OR	C");
			sw.WriteLine("	JR	NZ,$-3");
			sw.WriteLine("	CP	(HL)");
			if (Cpc.NbLig == 200)
				sw.WriteLine("	CP	(HL)");

			sw.WriteLine("	JR	Boucle");

			// Code du décompacteur avant les datas
			GenereDepack(sw, pkMethode);
			sw.WriteLine("Color01:");
			sw.WriteLine("	DB	'" + Cpc.CpcVGA[colMode5[0, 0]].ToString() + Cpc.CpcVGA[colMode5[0, 1]].ToString() + "'");
			sw.WriteLine("ColorModeX:");
			string line = "\tDB\t'";
			int nbOctets = 0;
			for (int y = 0; y < 272; y++) {
				line += Cpc.CpcVGA[colMode5[y, 2]].ToString() + Cpc.CpcVGA[colMode5[y, 3]].ToString();
				if (++nbOctets >= 16) {
					sw.WriteLine(line + "'");
					line = "\tDB\t'";
					nbOctets = 0;
				}
			}
			if (nbOctets > 0)
				sw.WriteLine(line + "'");
		}

		static public void GenereAfficheModeEgx(StreamWriter sw, int[] palette, bool overscan, Main.PackMethode pkMethode, string labelMedia, string labelPalette) {
			sw.WriteLine("	LD	HL," + labelPalette);
			sw.WriteLine("	LD	B,(HL)");
			sw.WriteLine("	LD	C,B");
			sw.WriteLine("	CALL	#BC38");
			sw.WriteLine("	XOR	A");
			sw.WriteLine("	LD	HL," + labelPalette);
			sw.WriteLine("SetPalette:");
			sw.WriteLine("	LD	B,(HL)");
			sw.WriteLine("	LD	C,B");
			sw.WriteLine("	PUSH	AF");
			sw.WriteLine("	PUSH	HL");
			sw.WriteLine("	CALL	#BC32");
			sw.WriteLine("	POP	HL");
			sw.WriteLine("	POP	AF");
			sw.WriteLine("	INC	HL");
			sw.WriteLine("	INC	A");
			sw.WriteLine("	CP	#10");
			sw.WriteLine("	JR	NZ,SetPalette");

			GenereFormatEcran(sw);
			sw.WriteLine("	LD	HL," + labelMedia);
			sw.WriteLine("	LD	DE,#" + (overscan ? "0200" : "C000"));
			sw.WriteLine("	CALL	Depack");

			sw.WriteLine("	DI");
			sw.WriteLine("WaitVbl:");
			sw.WriteLine("	LD	B,#F5");
			sw.WriteLine("	IN	A,(C)");
			sw.WriteLine("	RRA");
			sw.WriteLine("	JR	NC,WaitVbl");
			sw.WriteLine("WaitEnd:");
			sw.WriteLine("	IN	A,(C)");
			sw.WriteLine("	RRA");
			sw.WriteLine("	JR	C,WaitEnd");
			sw.WriteLine("	LD	HL,#012F");
			int mode = 0x8C01;
			if (Cpc.modeVirtuel == 4)
				mode = 0x8D03;

			if (Cpc.yEgx == 2)
				mode += 0x100;

			sw.WriteLine("	LD	DE,#" + mode.ToString("X4"));
			sw.WriteLine("SetMode:");
			sw.WriteLine("	LD	B,#7F");
			sw.WriteLine("	OUT	(C),D			; Premier mode pour les lignes paires");
			sw.WriteLine("	LD	A,D");
			sw.WriteLine("	XOR	E				; Inversion du mode pour la prochaine ligne");
			sw.WriteLine("	LD	D,A");
			sw.WriteLine("	LD	B,11");
			sw.WriteLine("WaitNextLine:");
			sw.WriteLine("	DJNZ	WaitNextLine");
			sw.WriteLine("	BIT	0,(HL)			; + 4 Nops");
			sw.WriteLine("	DEC	HL");
			sw.WriteLine("	LD	A,H");
			sw.WriteLine("	OR	L");
			sw.WriteLine("	JR	NZ,SetMode");
			sw.WriteLine("	CALL	TstSpace");
			sw.WriteLine("	JR	Z,WaitVbl");

			sw.WriteLine("	LD	BC,#BC0C");
			sw.WriteLine("	LD	A,#30");
			sw.WriteLine("	OUT	(C),C");
			sw.WriteLine("	INC	B");
			sw.WriteLine("	OUT	(C),A");
			sw.WriteLine("	EI");
			sw.WriteLine("	RET");

			GenereTspace(sw, false);
			GenereDepack(sw, pkMethode);

			string line = "\tDB\t";
			for (int y = 0; y < 16; y++)
				line += palette[y].ToString() + ",";

			sw.WriteLine(labelPalette);
			sw.WriteLine(line.Substring(0, line.Length - 1));
		}
		#endregion
	}
}
