using System;
using System.IO;
using System.Windows.Forms;

namespace ConvImgCpc {
	static public class SauveImage {
		/*
		Conversion palette OCP+ :

		static byte CpcVGA[ 28 ] = "TDU\\X]LEMVFW^@_NGORBSZY[JCK";

												W^@_NGORBSZY[JCK = 
												FEDCBA9876543210
		(3 premiers octets /12 de la palette = composante R,B,V)
		*/

		#region Gestion du code asm à ajouter pour le décompactage et l'affichage
		static readonly byte[] CodeStd = {  // Routine à mettre en #C7D0
			0x3A, 0xD0, 0xD7,			//				LD		A,(#D7D0)
			0xCD, 0x1C, 0xBD,			//				CALL	#BD1C
			0x21, 0xD1, 0xD7,			//				LD		HL, #D7D1
			0x46,						//				LD		B,(HL)
			0x48,						//				LD		C,B
			0xCD, 0x38, 0xBC,			//				CALL	#BC38
			0xAF,						//				XOR		A
			0x21, 0xD1, 0xD7,			//				LD		HL,#D7D1
			0x46,						//BCL:			LD		B,(HL)
			0x48,						//				LD		C,B
			0xF5,						//				PUSH	AF
			0xE5,						//				PUSH	HL
			0xCD, 0x32, 0xBC,			//				CALL	#BC32
			0xE1,						//				POP		HL
			0xF1,						//				POP		AF
			0x23,						//				INC		HL
			0x3C,						//				INC		A
			0xFE, 0x10,					//				CP		#10
			0x20, 0xF1,					//				JR		NZ,BCL
			0xC3, 0x18, 0xBB,			//				JP		#BB18
			};

		static readonly byte[] CodeP0 = {
			0xF3,						//				DI
			0x01, 0x11, 0xBC,			//				LD		BC,#BC11
			0x21, 0xD0, 0xDF,			//				LD		HL,#DFD0
			0x7E,						//BCL1:			LD		A,(HL)
			0xED, 0x79,					//				OUT		(C),A
			0x23,						//				INC		HL
			0x0D,						//				DEC		C
			0x20, 0xF9,					//				JR		NZ,BCL1
			0x01, 0xA0, 0x7F,			//				LD		BC,#7FA0
			0x3A, 0xD0, 0xD7,			//				LD		A,(#D7D0)
			0xED, 0x79,					//				OUT		(C),A
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0xB8, 0x7F,			//				LD		BC,#7FB8
			0xED, 0x49,					//				OUT		(C),C
			0x21, 0xD1, 0xD7,			//				LD		HL,#D7D1
			0x11, 0x00, 0x64,			//				LD		DE,#6400
			0x01, 0x22, 0x00,			//				LD		BC,#0022
			0xED, 0xB0,					//				LDIR
			0xCD, 0xD0, 0xCF,			//BCL2:			CALL	WaitKey
			0x38, 0xFB,					//				JR		C,BCL2
			0xFB,						//				EI
			0xC9						//				RET
			};

		static readonly byte[] CodeP1 = {
			0x01, 0x0E, 0xF4,			//WaitKey:		LD		BC,#F40E
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0xC0, 0xF6,			//				LD		BC,#F6C0
			0xED, 0x49,					//				OUT		(C),C
			0xAF,						//				XOR		A
			0xED, 0x79,					//				OUT		(C),A
			0x01, 0x92, 0xF7,			//				LD		BC,#F792
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0x45, 0xF6,			//				LD		BC,#F645
			0xED, 0x49,					//				OUT		(C),C
			0x06, 0xF4,					//				LD		B,#F4
			0xED, 0x78,					//				IN		A,(C)
			0x01, 0x82, 0xF7,			//				LD		BC,#F782
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0x00, 0xF6,			//				LD		BC,#F600
			0xED, 0x49,					//				OUT		(C),C
			0x17,						//				RLA
			0xC9						//				RET
			};

		// Unlock ASIC
		static readonly byte[] CodeP3 = { 0xFF, 0x00, 0xFF, 0x77, 0xB3, 0x51, 0xA8, 0xD4, 0x62, 0x39, 0x9C, 0x46, 0x2B, 0x15, 0x8A, 0xCD, 0xEE };

		static readonly byte[] CodeOv = {
			0x21, 0x47, 0x08,			//				LD		HL,#847
			0xCD, 0x36, 0x08,			//				CALL	SetRegs
			0x3A, 0x00, 0x08,			//				LD		A,(#0800)
			0xCD, 0x1C, 0xBD,			//				CALL	#BD1C
			0x21, 0x01, 0x08,			//				LD		HL,#0801
			0xAF,						//				XOR		A
			0x4E,						//SetPal:			LD		C,(HL)
			0x41,						//				LD		B,C
			0xF5,						//				PUSH	AF
			0xE5,						//				PUSH	HL
			0xCD, 0x32, 0xBC,			//				CALL	#BC32
			0xE1,						//				POP		HL
			0xF1,						//				POP		AF
			0x23,						//				INC		HL
			0x3C,						//				INC		A
			0xFE, 0x10,					//				CP		#10
			0x20, 0xF1,					//				JR		NZ,SetPal
			0xCD, 0x18, 0xBB,			//				CALL	#BB18
			0x21, 0x57, 0x08,			//				LD		HL,#0857 // Remise aux valeurs originales
			0x01, 0x00, 0xBC,			//SetRegs:		LD		BC,#BC00
			0x7E,						//SetRegs1:		LD		A,(HL)
			0xA7,						//				AND		A
			0xC8,						//				RET		Z
			0xED, 0x79,					//				OUT		(C),A
			0x04,						//				INC		B
			0x23,						//				INC		HL
			0x7E,						//				LD		A,(HL)
			0xED, 0x79,					//				OUC		(C),A
			0x23,						//				INC		HL
			0x05,						//				DEC		B
			0x18, 0xF2,					//				JR		SetRegs1
			0x01, 0x30, 0x02, 0x32, 0x03, 0x89, 0x06, 0x22,
			0x07, 0x23, 0x0C, 0x0D, 0x0D, 0x00, 0x00, 0x00,
			0x01, 0x28, 0x02, 0x2E, 0x03, 0x8E, 0x06, 0x19,
			0x07, 0x1E, 0x0C, 0x30, 0x00
			};

		static readonly byte[] CodeOvP = {
			0xF3,						//				DI
			0x01, 0x11, 0xBC,			//				LD		BC,#BC11
			0x21, 0x86, 0x08,			//				LD		HL,#0886
			0x04,						//SetAsic:		INC		B
			0xED, 0xA3,					//				OUTI
			0x0D,						//				DEC		C
			0x20, 0xFA,					//				JR		NZ,SetAsic
			0x21, 0x97, 0x08,			//				LD		HL,#897
			0xCD, 0x75, 0x08,			//				CALL	SetReg
			0x01, 0xB8, 0x7F,			//				LD		BC,#7FB8
			0x3A, 0x00, 0x08,			//				LD		A,(#0800)
			0xED, 0x49,					//				OUT		(C),C
			0xED, 0x79,					//				OUT		(C),A
			0x21, 0x01, 0x08,			//				LD		HL,#0801
			0x11, 0x00, 0x64,			//				LD		DE,#6400
			0x01, 0x20, 0x00,			//				LD		BC,#0020
			0xED, 0xB0,					//				LDIR
			0xAF,						//WaitKey:		XOR		A
			0x01, 0x0E, 0xF4,			//				LD		BC,#F40E
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0xC0, 0xF6,			//				LD		BC,#F6C0
			0xED, 0x49,					//				OUT		(C),C
			0xED, 0x79,					//				OUT		(C),A
			0x01, 0x92, 0xF7,			//				LD		BC,#F792
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0x45, 0xF6,			//				LD		BC,#F645
			0xED, 0x49,					//				OUT		(C),C
			0x06, 0xF4,					//				LD		B,#F4
			0xED, 0x78,					//				IN		A,(C)
			0x01, 0x82, 0xF7,			//				LD		BC,#F782
			0xED, 0x49,					//				OUT		(C),C
			0x17,						//				RLA
			0x38, 0xDD,					//				JR		C,WaitKey
			0x01, 0xA0, 0x7F,			//				LD		BC,#7FA0
			0xED, 0x49,					//				OUT		(C),C
			0xFB,						//				EI
			0x21, 0xA5, 0x08,			//				LD		HL,#08A5
			0x01, 0x00, 0xBC,			//SetReg:		LD		BC,#BC00
			0x7E,						//SetReg1:		LD		A,(HL)
			0xA7,						//				AND		A
			0xC8,						//				RET		Z
			0xED, 0x79,					//				OUT		(C),A
			0x04,						//				INC		B
			0x23,						//				INC		HL
			0x7E,						//				LD		A,(HL)
			0xED, 0x79,					//				OUT		(C),A
			0x23,						//				INC		HL
			0x05,						//				DEC		B
			0x18, 0xF2,					//				JR		SetReg1
			0xFF, 0x00, 0xFF, 0x77, 0xB3, 0x51, 0xA8, 0xD4,
			0x62, 0x39, 0x9C, 0x46, 0x2B, 0x15, 0x8A, 0xCD,
			0xEE, 0x01, 0x30, 0x02, 0x32, 0x06, 0x22, 0x07,
			0x23, 0x0C, 0x0D, 0x0D, 0x00, 0x00, 0x00, 0x01,
			0x28, 0x02, 0x2E, 0x06, 0x19, 0x07, 0x1E, 0x0C,
			0x30
			};

		static readonly byte[] codeEgx0 = {
			0x21, 0x00, 0x20,			//				LD		HL,#2000
			0x2B,						//Wait0:		DEC		HL
			0x7C,						//				LD		A,H
			0xB5,						//				OR		L
			0x20, 0xFB,					//				JR		NZ,Wait0
			0xF3,						//				DI
			0x06, 0xF5,					//WaitVbl:		LD		B,#F5
			0xED, 0x78,					//				IN		A,(C)
			0x1F,						//				RRA
			0x30,0xF9,					//				JR		NC,WaitVbl
			0x21, 0x2F, 0x01,			//				LD		HL,#012F
			0x11, 0x01, 0x8C,			//				LD		DE,#8C01
			0x06, 0x7F,					//SetMode:		LD		B,#7F
			0xED, 0x51,					//				OUT		(C),D
			0x7A,						//				LD		A,D
			0xAB,						//				XOR		E
			0x57,						//				LD		D,A
			0x06, 0x0B,					//				LD		B,#0B
			0x10, 0xFE,					//WaitNextLine:	DJNZ	WaitNextLine
			0xCB, 0x46,					//				BIT		0,(HL)
			0x2B,						//				DEC		HL
			0x7C,						//				LD		A,H
			0xB5,						//				OR		L
			0x20, 0xEE,					//				JR		NZ,SetMode
			0xCD, 0xD0, 0xFF,			//				CALL	WaitKey
			0x28, 0xDC,					//				JR		Z,WaitVbl
			0xFB,						//				EI
			0xC9						//				RET
		};

		static readonly byte[] codeEgx1 = {
			0x16, 0x45,					//				LD		D,#45
			0x01, 0x0E, 0xF4,			//WaitKey1:		LD		BC,#F40E
			0xED, 0x49,					//				OUT		(C),C
			0x01, 0xC0, 0xF6,			//				LD		BC,#F6C0
			0xED, 0x49,					//				OUT		(C),C
			0xAF,						//				XOR		A
			0xED, 0x79,					//				OUT		(C),A
			0x01, 0x92, 0xF7,			//				LD		BC,#F792
			0xED, 0x49,					//				OUT		(C),C
			0x06, 0xF6,					//				LD		B,#F6
			0xED, 0x51,					//				OUT		(C),D
			0x06, 0xF4,					//				LD		B,#F4
			0xED, 0x78,					//				IN		A,(C)
			0x01, 0x82, 0xF7,			//				LD		BC,#F782
			0xED, 0x49,					//				OUT		(C),C
			0x3C,						//				INC		A
			0x20, 0x07,					//				JR		NZ,WaitKey2
			0x7A,						//				LD		A,D
			0x3C,						//				INC		A
			0x57,						//				LD		D,A
			0xFE, 0x4A,					//				CP		#4A
			0x38, 0xD7,					//				JR		C,WaitKey1
			0xC9						//WaitKey2:		RET
		};

		static readonly byte[] codeDepack = {
			0x21, 0x00, 0x00,			//				LD		HL,Source
			0x11, 0x00, 0x00,			//				LD		DE,Dest
			0x7E,						//DepkLzw:		LD		A,(HL)
			0x23,						//				INC		HL
			0x1F,						//				RRA
			0xCB, 0xFF,					//				SET		7,A
			0x32, 0xD3, 0xA5,			//				LD		(BclLzw+1),A
			0x38, 0x0D,					//				JR		C,TstCodeLzw
			0xED, 0xA0,					//				LDI
			0x3E, 0x00,					//BclLzw:		LD		A,0
			0xCB, 0x1F,					//				RRA
			0x32, 0xD3, 0xA5,			//				LD		(BclLzw+1),A
			0x30, 0xF5,					//				JR		NC,CopByteLzw
			0x28, 0xE9,					//				JR		Z,DepkLzw
			0x7E,						//				LD		A,(HL)
			0xA7,						//				AND		A
			0xCA, 0x00, 0x00,			//				JP		Z,AfficheImage
			0x23,						//				INC		HL
			0x47,						//				LD		B,A
			0x07,						//				RLCA
			0x30, 0x1D,					//				JR		NC,TstLzw40
			0x07,						//				RLCA
			0x07,						//				RLCA
			0x07,						//				RLCA
			0xE6, 0x07,					//				AND		#07
			0xC6, 0x03,					//				ADD		A,#03
			0x4F,						//				LD		C,A
			0x78,						//				LD		A,B
			0xE6, 0x0F,					//				AND		#0F
			0x47,						//				LD		B,A
			0x79,						//				LD		A,C
			0x37,						//				SCF
			0x4E,						//CopyBytes0:	LD		C,(HL)
			0x23,						//				INC		HL
			0xE5,						//				PUSH	HL
			0x62,						//				LD		H,D
			0x6B,						//				LD		L,E
			0xED, 0x42,					//				SBC		HL,DE
			0x06, 0x00,					//				LD		B,#00
			0x4F,						//CopyBytes1:	KD		C,A
			0xED, 0xB0,					//CopyBytes2:	LDIR
			0xE1,						//CopyBytes3:	POP		HL
			0x18, 0xCE,					//				JR		BclLzw
			0x07,						//TstLzw40:		RLCA
			0x30, 0x10,					//				JR		NC,TstLzw20
			0x48,						//				LD		C,B
			0xCB, 0xB1,					//				RES		6,C
			0x06, 0x00,					//				LD		B,#00
			0xE5,						//				PUSH	HL
			0x62,						//				LD		H,D
			0x6B,						//				LD		L,E
			0xED, 0x42,					//				SBC		HL,BC
			0xED, 0xA0,					//				LDI
			0xED, 0xA0,					//				LDI
			0x18, 0xEA,					//				JR		CopyBytes3
			0x07,						//TstLzw20:		RLCA
			0x30, 0x29,					//				JR		NC,TstLzw10
			0x78,						//				LD		A,B
			0xC6, 0xE2,					//				ADD		A,#E2
			0x06, 0x00,					//				LD		B,#00
			0x18, 0xD4,					//				JR		CopyBytes0
			0x4E,						//CodeLzw0F:	LD		C,(HL)
			0xE5,						//				PUSH	HL
			0x62,						//				LD		H,D
			0x6B,						//				LD		L,E
			0xFE, 0xF0,					//				CP		#F0
			0x20, 0x0B,					//				JR		NZ,CodeLzw02
			0xAF,						//				XOR		A
			0x47,						//				LD		B,A
			0x03,						//				INC		BC
			0xED, 0x42,					//				SBC		HL,BC
			0xED, 0xB0,					//				LDIR
			0xE1,						//				POP		HL
			0x23,						//				INC		HL
			0x18, 0x9E,					//				JR		BclLzw
			0xFE, 0x20,					//CodeLzw02:	CP		#20
			0x38, 0x07,					//				JR		C,CodeLzw01
			0x48,						//				LD		C,B
			0x06, 0x00,					//				LD		B,#00
			0xED, 0x42,					//				SBC		HL,BC
			0x18, 0xC0,					//				JR		CopyBytes2
			0xAF,						//CodeLzw01:	XOR		A
			0x25,						//				DEC		H
			0x18, 0xBB,					//				JR		CopyBytes1
			0x07,						//TstLzw10:		RLCA
			0x30, 0xDB,					//				JR		NC,CodeLzw0F
			0xCB, 0xA0,					//				RES		4,B
			0x4E,						//				LD		C,(HL)
			0x23,						//				INC		HL
			0x7E,						//				LD		A,(HL)
			0x23,						//				INC		HL
			0xE5,						//				PUSH	HL
			0x62,						//				LD		H,D
			0x6B,						//				LD		L,E
			0xED, 0x42,					//				SBC		HL,BC
			0x06, 0x00,					//				LD		B,#00
			0x4F,						//				LD		C,A
			0x03,						//				INC		BC
			0x18, 0xA8,					//				JR		CopyBytes2
			};

		static readonly byte[] codeDZX0 = {
			0x21, 0x00, 0x00,			//				LD		HL,Source
			0x11, 0x00, 0x00,			//				LD		DE,Dest
			0x01, 0xFF, 0xFF,			//				LD		BC,#FFFF
			0xC5,						//				PUSH	BC
			0x03,						//				INC		BC
			0x3E, 0x80,					//				LD		A,#80
			0xCD, 0x3F, 0xA0,			//				CALL	dzx0s_elias
			0xED, 0xB0,					//				LDIR
			0x87,						//				ADD		A,A
			0x38, 0x0D,					//				JR		C,dzx0s_new_offset
			0xCD, 0x3F, 0xA0,			//				CALL	dzx0s_elias
			0xE3,						//				EX		(SP),HL
			0xE5,						//				PUSH	HL
			0x19,						//				ADD		HL,DE
			0xED, 0xB0,					//				LDIR
			0xE1,						//				POP		HL
			0xE3,						//				EX		(SP),HL
			0x87,						//				ADD		A,A
			0x30, 0xEB,					//				JR		NC,dxz0s_litterals
			0xCD, 0x3F, 0xA0,			//				CALL	dzx0_elias
			0x47,						//				LD		B,A
			0xF1,						//				POP		AF
			0xAF,						//				XOR		A
			0x91,						//				SUB		C
			0xCA, 0x00, 0x00,			//				JP		Z,AfficheImage
			0x4F,						//				LD		C,A
			0x78,						//				LD		A,B
			0x41,						//				LD		B,C
			0x4E,						//				LD		C,(HL)
			0x23,						//				INC		HL
			0xCB, 0x18,					//				RR		B
			0xCB, 0x19,					//				RR		C
			0xC5,						//				PUSH	BC
			0x01, 0x01, 0x00,			//				LD		BC,#0001
			0xD4, 0x47, 0xA0,			//				CALL	NC,dzx0s_elias_backtrack
			0x03,						//				INC		BC
			0x18, 0xD9,					//				JR		dzx0s_copy
			0x0C,						//dzx0s_elias:	INC		C
			0x87,						//dzx0s_elias_loop:	ADD		A,A
			0x20, 0x03,					//				JR		NZ,dzx0s_elias_skip
			0x7E,						//				LD		A,(HL)
			0x23,						//				INC		HL
			0x17,						//				RLA
			0xD8,						//				RET		C
			0x87,						//				ADD		A,A
			0xCB, 0x11,					//				RL		C
			0xCB, 0x10,					//				RL		B
			0x18, 0xF2					//				JR	dzx0s_elias_loop
		};

		static readonly byte[] codeDZX0_V2 = {
			0x21, 0x00, 0x00,			//				LD		HL,Source
			0x11, 0x00, 0x00,			//				LD		DE,Dest
			0x01, 0xFF, 0xFF,			//				LD		BC,#FFFF
			0xC5,						//				PUSH	BC
			0x03,						//				INC		BC
			0x3E, 0x80,					//				LD		A,#80
			0xCD, 0x3D, 0xA0,			//				CALL	dzx0s_elias
			0xED, 0xB0,					//				LDIR
			0x87,						//				ADD		A,A
			0x38, 0x0D,					//				JR		C,dzx0s_new_offset
			0xCD, 0x3D, 0xA0,			//				CALL	dzx0s_elias
			0xE3,						//				EX		(SP),HL
			0xE5,						//				PUSH	HL
			0x19,						//				ADD		HL,DE
			0xED, 0xB0,					//				LDIR
			0xE1,						//				POP		HL
			0xE3,						//				EX		(SP),HL
			0x87,						//				ADD		A,A
			0x30, 0xEB,					//				JR		NC,dxz0s_litterals
			0xC1,						//				POP		BC
			0x0E,0xFE,					//				LD		C,#FE
			0xCD, 0x3E, 0xA0,			//				CALL	dzx0_elias_loop
			0x0C,						//				INC		C
			0xCA, 0x00, 0x00,			//				JP		Z,AfficheImage
			0x41,						//				LD		B,C
			0x4E,						//				LD		C,(HL)
			0x23,						//				INC		HL
			0xCB, 0x18,					//				RR		B
			0xCB, 0x19,					//				RR		C
			0xC5,						//				PUSH	BC
			0x01, 0x01, 0x00,			//				LD		BC,#0001
			0xD4, 0x45, 0xA0,			//				CALL	NC,dzx0s_elias_backtrack
			0x03,						//				INC		BC
			0x18, 0xDB,					//				JR		dzx0s_copy
			0x0C,						//dzx0s_elias:	INC		C
			0x87,						//dzx0s_elias_loop:	ADD		A,A
			0x20, 0x03,					//				JR		NZ,dzx0s_elias_skip
			0x7E,						//				LD		A,(HL)
			0x23,						//				INC		HL
			0x17,						//				RLA
			0xD8,						//				RET		C
			0x87,						//				ADD		A,A
			0xCB, 0x11,					//				RL		C
			0xCB, 0x10,					//				RL		B
			0x18, 0xF2					//				JR	dzx0s_elias_loop
		};

		static readonly byte[] codeDZX1 = {
			0x21, 0x00, 0x00,			//				LD		HL,Source
			0x11, 0x00, 0x00,			//				LD		DE,Dest
			0x01, 0xFF, 0xFF,			//				LD		BC,#FFFF
			0xC5,						//				PUSH	BC
			0x3E, 0x80,					//				LD		A,#80
			0xCD, 0x3D, 0xA0,			//dzx1s_litteral:	CALL	dzx1s_elias
			0xED, 0xB0,					//				LDIR
			0x87,						//				ADD		A,A
			0x38, 0x0D,					//				JR		C,dzx1s_new_offset
			0xCD, 0x3D, 0xA0,			//				CALL	dzx1s_elias
			0xE3,						//dzx1s_copy:	EX		(SP),HL
			0xE5,						//				PUSH	HL
			0x19,						//				ADD		HL,DE
			0xED, 0xB0,					//				LDIR
			0xE1,						//				POP		HL
			0xE3,						//				EX		(SP),HL
			0x87,						//				ADD		A,A
			0x30, 0xEB,					//				JR		C,dzx1s_literals
			0x33,						//				INC		SP
			0x33,						//				INC		SP
			0x05,						//				DEC		B
			0x4E,						//				LD		C,(HL)
			0X23,						//				INC		HL
			0xCB, 0x19,					//				RR		C
			0x30, 0x0A,					//				JR		NC,dzx1s_msb_skip
			0x46,						//				LD		B,(HL)
			0x23,						//				INC		HL
			0xCB, 0x18,					//				RR		B
			0x04,						//				INC		B
			0xCA, 0x00, 0x00,			//				JP		Z,AfficheImage
			0xCB, 0x11,					//				RL		C
			0xC5,						//				PUSH	BC
			0xCD, 0x3B, 0xA0,			//				CALL	dzx1s_elias
			0x03,						//				INC		BC
			0x18, 0xDC,					//				JR		dzx1s_copy
			0x01, 0x01, 0x00,			//dzx1s_elias:	LD		BC,1
			0x87,						//dzx1s_elias_loop:	ADD		A,A
			0x20, 0x03,					//				JR		NZ,dzx1s_elias_skip
			0x7E,						//				LD		A,(HL)
			0x23,						//				INC		HL
			0x17,						//				RLA
			0xD0,						//dzx1s_elias_skip:	RET		NC
			0x87,						//				ADD		A,A
			0xCB, 0x11,					//				RL		C
			0xCB, 0x10,					//				RL		B
			0x18, 0xF2					//				JR	dzx1s_elias_loop
		};
		#endregion

		static private readonly byte[] ModePal = new byte[48];

		static private void Poke16(byte[] tabMem, int offset, short value) {
			tabMem[offset] = (byte)(value & 0xFF);
			tabMem[offset + 1] = (byte)(value >> 8);
		}

		static private void SaveImgAsm(Main main, Main.PackMethode compact, Param param, string fileName, string version, byte[] bufPack, int lg, int[,] colMode5) {
			bool overscan = (Cpc.NbLig * Cpc.NbCol > 0x3F00);
			StreamWriter sw;
			bool with2file = main.chk17K.Checked && compact != Main.PackMethode.None && !param.withCode && lg > 16384;
			string fileName2 = null;
			SaveMedia dlgSave = new SaveMedia("Image", Path.GetFileNameWithoutExtension(fileName), param.withPalette);
			dlgSave.ShowDialog();
			if (dlgSave.saveMediaOk) {
				if (with2file) {
					string dir = Path.GetDirectoryName(fileName);
					string name = Path.GetFileNameWithoutExtension(fileName);
					string ext = Path.GetExtension(fileName);
					fileName = dir + "\\" + name + "_01" + ext;
					fileName2 = dir + "\\" + name + "_02" + ext;
				}
				sw = SaveAsm.OpenAsm(fileName, version, true);

				string labelMedia = string.IsNullOrEmpty(dlgSave.LabelMedia) && param.withCode ? "ImageCmp" : dlgSave.LabelMedia;
				string labelPalette = string.IsNullOrEmpty(dlgSave.LabelPal) ? "Palette" : dlgSave.LabelPal;
				if (param.withCode) {
					int org = 0xA500 - lg - (Cpc.modeVirtuel == 5 ? 600 : 0);
					sw.WriteLine("	ORG	#" + org.ToString("X4"));
					sw.WriteLine("	Nolist");
				}
				if (with2file) {
					SaveAsm.GenereDatas(sw, bufPack, 16384, 16);
					sw.Close();
					byte[] rest = new byte[lg - 16384];
					Array.Copy(bufPack, 16384, rest, 0, lg - 16384);
					sw = SaveAsm.OpenAsm(fileName2, version, true);
					SaveAsm.GenereDatas(sw, rest, lg - 16384, 16);
				}
				else
					SaveAsm.GenereDatas(sw, bufPack, lg, 16, 0, labelMedia);

				if (param.withCode) {
					sw.WriteLine("	List");
					sw.WriteLine("	RUN	$");
					sw.WriteLine("_StartDepack:");
					if (Cpc.modeVirtuel == 3 || Cpc.modeVirtuel == 4)
						SaveAsm.GenereAfficheModeEgx(sw, Cpc.Palette, overscan, compact, labelMedia, labelPalette);
					else {
						if (Cpc.modeVirtuel == 5)
							SaveAsm.GenereAfficheModeX(sw, colMode5, overscan, compact, labelMedia);
						else
							SaveAsm.GenereAfficheStd(sw, overscan, compact, labelMedia, labelPalette);
					}
				}
				if (param.withPalette && (Cpc.modeVirtuel < 3 || Cpc.modeVirtuel > 5))
					SaveAsm.GenerePalette(sw, param, param.withCode, param.withCode, dlgSave.LabelPal);

				SaveAsm.CloseAsm(sw);
			}
		}

		static public int SauveScr(string fileName, BitmapCpc bitmapCpc, Main main, Main.PackMethode compact, Main.OutputFormat format, Param param, string version = null, int[,] colMode5 = null) {
			byte[] bufPack = new byte[0x8000];
			bool overscan = (Cpc.NbLig * Cpc.NbCol > 0x3F00);
			if (param.withPalette && format != Main.OutputFormat.Assembler) {
				if (param.cpcPlus) {
					ModePal[0] = (byte)(Cpc.modeVirtuel | 0x8C);
					int k = 1;
					for (int i = 0; i < 16; i++) {
						ModePal[k++] = (byte)(((Cpc.Palette[i] >> 4) & 0x0F) | (Cpc.Palette[i] << 4));
						ModePal[k++] = (byte)(Cpc.Palette[i] >> 8);
					}
				}
				else {
					ModePal[0] = (byte)Cpc.modeVirtuel;
					for (int i = 0; i < 16; i++)
						ModePal[1 + i] = (byte)Cpc.Palette[i];
				}
			}

			if (Cpc.modeVirtuel == 3 || Cpc.modeVirtuel == 4) {
				int mode = 0x8C01;
				if (Cpc.modeVirtuel == 4)
					mode = 0x8D03;

				if (Cpc.yEgx == 2)
					mode += 0x100;

				codeEgx0[20] = (byte)(mode & 0xFF);
				codeEgx0[21] = (byte)(mode >> 8);
			}
			byte[] imgCpc = bitmapCpc.bmpCpc;
			if (!overscan) {
				if (param.withPalette && format != Main.OutputFormat.Assembler)
					Buffer.BlockCopy(ModePal, 0, imgCpc, 0x17D0, ModePal.Length);

				if (param.withCode && format != Main.OutputFormat.Assembler) {
					if (param.cpcPlus) {
						Buffer.BlockCopy(CodeP0, 0, imgCpc, 0x07D0, CodeP0.Length);
						Buffer.BlockCopy(CodeP1, 0, imgCpc, 0x0FD0, CodeP1.Length);
						Buffer.BlockCopy(CodeP3, 0, imgCpc, 0x1FD0, CodeP3.Length);
					}
					else
						Buffer.BlockCopy(CodeStd, 0, imgCpc, 0x07D0, CodeStd.Length);

					if (Cpc.modeVirtuel == 3 || Cpc.modeVirtuel == 4) {
						Buffer.BlockCopy(codeEgx0, 0, imgCpc, 0x37D0, codeEgx0.Length);
						Buffer.BlockCopy(codeEgx1, 0, imgCpc, 0x2FD0, codeEgx1.Length);
						imgCpc[0x07F2] = 0xD0;
						imgCpc[0x07F3] = 0xF7;  //	CALL 0xF7D0
						imgCpc[0x37FA] = 0xEF;  //	Call 0xEFD0
					}
				}
			}
			else {
				if (Cpc.NbLig * Cpc.NbCol > 0x4000) {
					Buffer.BlockCopy(ModePal, 0, imgCpc, 0x600, ModePal.Length);
					if (param.withCode && format != Main.OutputFormat.Assembler) {
						if (param.cpcPlus)
							Buffer.BlockCopy(CodeOvP, 0, imgCpc, 0x621, CodeOvP.Length);
						else
							Buffer.BlockCopy(CodeOv, 0, imgCpc, 0x611, CodeOv.Length);

						if (Cpc.modeVirtuel == 3 || Cpc.modeVirtuel == 4) {
							Buffer.BlockCopy(codeEgx0, 0, imgCpc, 0x1600, codeEgx0.Length);
							Buffer.BlockCopy(codeEgx1, 0, imgCpc, 0x1640, codeEgx1.Length);
							if (param.cpcPlus) {
								imgCpc[0x669] = 0xCD;
								Poke16(imgCpc, 0x66A, 0x1800);  // CALL	#1800
							}
							else
								Poke16(imgCpc, 0x631, 0x1800);  // CALL	#1800

							Poke16(imgCpc, 0x1629, 0x1840); //	CALL	#1840
						}
					}
				}
			}

			short startAdr = (short)(overscan ? 0x200 : 0xC000);
			short exec = (short)(overscan ? param.cpcPlus ? 0x821 : 0x811 : 0xC7D0);
			CpcAmsdos entete;
			int lg = Cpc.BitmapSize;
			if (compact == Main.PackMethode.ZX0Ovs && (Cpc.NbLig != 272 || Cpc.NbCol != 96))
				compact = Main.PackMethode.ZX0;

			if (compact != Main.PackMethode.None) {
				lg = new PackModule().Pack(bitmapCpc.bmpCpc, lg, bufPack, 0, compact);
				if (param.withCode && format != Main.OutputFormat.Assembler) {
					short newExec;
					switch (compact) {
						case Main.PackMethode.Standard:
							Buffer.BlockCopy(codeDepack, 0, bufPack, lg, codeDepack.Length);
							Poke16(bufPack, lg + 0x04, startAdr);
							startAdr = (short)(0xA657 - (lg + codeDepack.Length));
							Poke16(bufPack, lg + 0x01, startAdr);
							Poke16(bufPack, lg + 0x20, exec);
							lg += codeDepack.Length;
							exec = (short)(0xA657 - codeDepack.Length);
							break;

						case Main.PackMethode.ZX0:
							newExec = (short)(0xA657 - codeDZX0.Length);
							Buffer.BlockCopy(codeDZX0, 0, bufPack, lg, codeDZX0.Length);
							Poke16(bufPack, lg + 0x04, startAdr);
							Poke16(bufPack, lg + 0x0E, (short)(newExec + 0x3F));
							Poke16(bufPack, lg + 0x16, (short)(newExec + 0x3F));
							Poke16(bufPack, lg + 0x23, (short)(newExec + 0x3F));
							Poke16(bufPack, lg + 0x3A, (short)(newExec + 0x47));
							startAdr = (short)(0xA657 - (lg + codeDZX0.Length));
							Poke16(bufPack, lg + 0x01, startAdr);
							Poke16(bufPack, lg + 0x2A, exec);
							lg += codeDZX0.Length;
							exec = newExec;
							break;

						case Main.PackMethode.ZX0_V2:
							newExec = (short)(0xA657 - codeDZX0_V2.Length);
							Buffer.BlockCopy(codeDZX0_V2, 0, bufPack, lg, codeDZX0_V2.Length);
							Poke16(bufPack, lg + 0x04, startAdr);
							Poke16(bufPack, lg + 0x0E, (short)(newExec + 0x3D));
							Poke16(bufPack, lg + 0x16, (short)(newExec + 0x3D));
							Poke16(bufPack, lg + 0x26, (short)(newExec + 0x3E));
							Poke16(bufPack, lg + 0x38, (short)(newExec + 0x45));
							startAdr = (short)(0xA657 - (lg + codeDZX0_V2.Length));
							Poke16(bufPack, lg + 0x01, startAdr);
							Poke16(bufPack, lg + 0x2A, exec);
							lg += codeDZX0_V2.Length;
							exec = newExec;
							break;

						case Main.PackMethode.ZX1:
							newExec = (short)(0xA657 - codeDZX1.Length);
							Buffer.BlockCopy(codeDZX1, 0, bufPack, lg, codeDZX1.Length);
							Poke16(bufPack, lg + 0x04, startAdr);
							Poke16(bufPack, lg + 0x0D, (short)(newExec + 0x3B));
							Poke16(bufPack, lg + 0x15, (short)(newExec + 0x3B));
							Poke16(bufPack, lg + 0x36, (short)(newExec + 0x3B));
							startAdr = (short)(0xA657 - (lg + codeDZX1.Length));
							Poke16(bufPack, lg + 0x01, startAdr);
							Poke16(bufPack, lg + 0x30, exec);
							lg += codeDZX1.Length;
							exec = newExec;
							break;
					}
				}
				else {
					startAdr = (short)(0xA657 - lg);
					exec = 0;
				}
			}
			switch (format) {
				case Main.OutputFormat.Binary:
					entete = Cpc.CreeEntete(fileName, startAdr, (short)lg, exec);
					FileStream s = new FileStream(fileName, FileMode.Create);
					BinaryWriter fp = new BinaryWriter(s);
					fp.Write(Cpc.AmsdosToByte(entete));
					fp.Write(compact != Main.PackMethode.None ? bufPack : bitmapCpc.bmpCpc, 0, lg);
					fp.Close();
					s.Close();
					break;

				case Main.OutputFormat.Assembler:
					SaveImgAsm(main, compact, param, fileName, version, bufPack, lg, colMode5);
					break;

				case Main.OutputFormat.DSK:
					if (File.Exists(fileName))
						main.dsk.Load(fileName);
					else
						main.dsk.FormatDsk();

					MemoryStream ms = new MemoryStream();
					entete = Cpc.CreeEntete(fileName, startAdr, (short)lg, exec);
					BinaryWriter fpm = new BinaryWriter(ms);
					fpm.Write(Cpc.AmsdosToByte(entete));
					fpm.Write(compact != Main.PackMethode.None ? bufPack : bitmapCpc.bmpCpc, 0, lg);
					fpm.Close();
					byte[] fic = ms.ToArray();
					GestDSK.DskError err = GestDSK.DskError.ERR_NO_ERR;
					for (int i = 0; i < 100; i++) {
						string nom = "CNVIMG" + i.ToString("00") + (compact == Main.PackMethode.None ? ".SCR" : ".CMP");
						err = main.dsk.CopieFichier(fic, nom, fic.Length, 178, 0);
						if (err == GestDSK.DskError.ERR_NO_ERR) {
							main.SetInfo(main.multilingue.GetString("Main.prg.TxtInfo33") + nom);
							break;
						}
					}
					if (err == GestDSK.DskError.ERR_NO_ERR)
						main.dsk.Save(fileName);
					else {
						switch (err) {
							case GestDSK.DskError.ERR_FILE_EXIST:
							case GestDSK.DskError.ERR_NO_DIRENTRY:
								MessageBox.Show(main.multilingue.GetString("Main.prg.TxtInfo34"));
								break;

							case GestDSK.DskError.ERR_NO_BLOCK:
								MessageBox.Show(main.multilingue.GetString("Main.prg.TxtInfo35"));
								break;
						}
					}
					break;
			}
			return (lg);
		}

		static public void SauvePalette(string NomFic, Param param) {
			int i;
			byte[] pal = new byte[239];

			pal[0] = (byte)param.modeVirtuel;
			int indexPal = 3;
			if (param.cpcPlus) {
				for (i = 0; i < 16; i++)
					for (int j = 0; j < 4; j++) {
						pal[indexPal++] = (byte)Cpc.CpcVGA[26 - ((Cpc.Palette[i] >> 4) & 0x0F)];
						pal[indexPal++] = (byte)Cpc.CpcVGA[26 - (Cpc.Palette[i] & 0x0F)];
						pal[indexPal++] = (byte)Cpc.CpcVGA[26 - ((Cpc.Palette[i] >> 8) & 0x0F)];
					}
				pal[195] = pal[3];
				pal[196] = pal[4];
				pal[197] = pal[5];
			}
			else {
				for (i = 0; i < 16; i++)
					for (int j = 0; j < 12; j++)
						pal[indexPal++] = (byte)Cpc.CpcVGA[Cpc.Palette[i] < Cpc.CpcVGA.Length ? Cpc.Palette[i] : 0];

				for (i = 0; i < 12; i++)
					pal[indexPal++] = pal[i + 3];
			}
			CpcAmsdos entete = Cpc.CreeEntete(NomFic, (short)-30711, (short)pal.Length, (short)-30711);
			FileStream s = new FileStream(NomFic, FileMode.Create);
			BinaryWriter fp = new BinaryWriter(s);
			fp.Write(Cpc.AmsdosToByte(entete));
			fp.Write(pal, 0, pal.Length);
			fp.Close();
			s.Close();
		}

		static public bool LirePalette(string NomFic, Param param) {
			byte[] entete = new byte[0x80];
			byte[] pal = new byte[239];
			bool ret = false;

			FileStream s = new FileStream(NomFic, FileMode.Open);
			BinaryReader fp = new BinaryReader(s);
			if (fp != null) {
				fp.Read(entete, 0, entete.Length);
				fp.Read(pal, 0, pal.Length);
				fp.Close();
				if (Cpc.CheckAmsdos(entete) && pal[0] < 11) {
					for (int i = 0; i < 16; i++)
						for (int j = 0; j < 27; j++)
							if (pal[3 + i * 12] == (byte)Cpc.CpcVGA[j]) {
								if (param.cpcPlus) {
									RvbColor col = Cpc.RgbCPC[j];
									Cpc.Palette[i] = ((col.b >> 4) << 4) + ((col.v >> 4) << 8) + (col.r >> 4);
								}
								else
									Cpc.Palette[i] = j;
							}
					ret = true;
				}
			}
			s.Close();
			return ret;
		}

		static public bool LirePaletteKit(string NomFic) {
			if (File.Exists(NomFic)) {
				FileStream fileScr = new FileStream(NomFic, FileMode.Open, FileAccess.Read);
				byte[] tabBytes = new byte[fileScr.Length];
				fileScr.Read(tabBytes, 0, tabBytes.Length);
				fileScr.Close();
				if (Cpc.CheckAmsdos(tabBytes) && (tabBytes.Length == 158 || tabBytes.Length == 160)) {
					int start = 128;
					for (int i = tabBytes.Length == 160 ? 0 : 1; i < 16; i++) {
						int kit = tabBytes[start] + (tabBytes[start + 1] << 8);
						int col = (kit & 0xF00) + ((kit & 0x0F) << 4) + ((kit & 0xF0) >> 4);
						Cpc.Palette[i] = col;
						start += 2;
					}
					return true;
				}
			}
			return false;
		}
	}
}
