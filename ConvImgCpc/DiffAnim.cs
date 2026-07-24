using System;
using System.Collections.Generic;
using System.IO;

namespace ConvImgCpc {
	public class DiffAnim {
		private readonly List<DeltaDiff> delta = new List<DeltaDiff>();
		int lastAdr;

		public DiffAnim() {
			lastAdr = 0x10000;
		}

		public void AddDiff(int adr, byte d) {
			if (adr != lastAdr + 1)
				delta.Add(new DeltaDiff(adr));

			delta[delta.Count - 1].AddValue(d);
			lastAdr = adr;
		}

		public void Save(StreamWriter sw, int nbOctetsLigne) {
			foreach (DeltaDiff d in delta) {
				int nbOctets = 0, length = d.lstDelta.Count;
				sw.WriteLine("\tDW\t#" + d.startAddress.ToString("X4") + "\t;adresse");
				sw.WriteLine("\tDB\t#" + length.ToString("X2") + "\t;nbre octets");
				string line = "\tDB\t";
				for (int i = 0; i < length; i++) {
					line += "#" + d.lstDelta[i].ToString("X2") + ",";
					if (++nbOctets >= Math.Min(nbOctetsLigne, Cpc.NbCol)) {
						sw.WriteLine(line.Substring(0, line.Length - 1));
						line = "\tDB\t";
						nbOctets = 0;
						if (i < length - 1) {
							line += "\r\n";
						}
					}
				}
				if (nbOctets > 0)
					sw.WriteLine(line.Substring(0, line.Length - 1));
			}
			if (delta.Count > 0)
				sw.WriteLine("\tDW\t#0000\t;fin de table");
		}
	}

	/*
	LD	HL,DiffImage_1
Next
	CALL	Loop
	LD	A,(HL)
	INC	HL
	OR	(HL)
	DEC	HL
	RET	Z
	JR	Next

Loop:
	LD	E,(HL)
	INC	HL
	LD	D,(HL)
	INC	HL
	LD	A,D
	OR	E
	RET	Z
	LD	A,(HL)
	INC	HL
	EX	DE,HL
	LD	BC,#C000
	ADD	HL,BC
	EX	DE,HL
	LD	B,0
	LD	C,A
	LDIR
	JR	Loop
 * 	 */

	public class DeltaDiff {
		public int startAddress;
		public List<byte> lstDelta;

		public DeltaDiff(int sa) {
			startAddress = sa;
			lstDelta = new List<byte>();
		}

		public void AddValue(byte v) {
			lstDelta.Add(v);
		}
	}
}
