using System;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class PackModule : Form {
		private const int SEEKBACK = 0x1000;
		private const int MAXSTRING = 256;
		private const int MAX_OFFSET_ZX0 = 32640;
		private const int MAX_OFFSET_ZX1 = 32512;
		private const int MAX_SCALE = 50;
		private const int MAX_CHAIN = 4096;
		private const int ZX_INF = 1 << 30;
		private int[] zxNBits, zxNIdx, zxNOff, zxNPrev;     // noeuds : bits, index de fin, offset (0 = littéral), noeud précédent
		private int zxNCount, zxMaxOff;
		private int[] zxMBase, zxMIdx, zxMPrev, zxMNode, zxLNode, zxLNodeIdx, zxTabNode, zxTabBits;

		private int outputIndex, bitIndex, bitMask;
		private bool backTrack;

		public PackModule() {
			InitializeComponent();
			Text = "Compress...";
		}

		private int DepackStd(byte[] bufIn, int startIn, byte[] bufOut) {
			byte a, DepackBits = 0;
			int bit, inBytes = startIn, longueur, delta, outBytes = 0;

			while (true) {
				bit = DepackBits & 1;
				DepackBits >>= 1;
				if (DepackBits == 0) {
					DepackBits = bufIn[inBytes++];
					bit = DepackBits & 1;
					DepackBits >>= 1;
					DepackBits |= 0x80;
				}
				if (bit == 0)
					bufOut[outBytes++] = bufIn[inBytes++];
				else {
					if (bufIn[inBytes] == 0)
						break; /* EOF */

					a = bufIn[inBytes];
					if ((a & 0x80) != 0) {
						longueur = 3 + ((bufIn[inBytes] >> 4) & 7);
						delta = (bufIn[inBytes++] & 15) << 8;
						delta |= bufIn[inBytes++];
						delta++;
					}
					else
						if ((a & 0x40) != 0) {
							longueur = 2;
							delta = bufIn[inBytes++] & 0x3f;
							delta++;
						}
						else
							if ((a & 0x20) != 0) {
								longueur = 2 + (bufIn[inBytes++] & 31);
								delta = bufIn[inBytes++];
								delta++;
							}
							else
								if ((a & 0x10) != 0) {
									delta = (bufIn[inBytes++] & 15) << 8;
									delta |= bufIn[inBytes++];
									longueur = bufIn[inBytes++] + 1;
									delta++;
								}
								else {
									if (bufIn[inBytes] == 15) {
										longueur = delta = bufIn[inBytes + 1] + 1;
										inBytes += 2;
									}
									else {
										if (bufIn[inBytes] > 1)
											longueur = delta = bufIn[inBytes];
										else
											longueur = delta = 256;

										inBytes++;
									}
								}
					for (; longueur-- > 0;) {
						bufOut[outBytes] = bufOut[outBytes - delta];
						outBytes++;
					}
				}
			}
			return (outBytes);
		}

		// Taille (en octets) du code d'un match de longueur 'length' à la distance 'offset'
		private static int StdMatchBytes(int length, int offset) {
			if (offset == length)
				return length == MAXSTRING || length <= 14 ? 1 : 2;

			if (length == 2 && offset < 65)
				return 1;

			if ((length <= 33 && offset < 257) || (length >= 3 && length <= 10))
				return 2;
			return 3;
		}

		// Écrit le code d'un match dans 'code' (mêmes formats que l'ancien PackStd), retourne le nombre d'octets
		private static int StdWriteMatch(byte[] code, int pos, int length, int offset) {
			if (offset == length) {
				if (length == MAXSTRING) {
					code[pos] = 0x01;
					return 1;
				}
				if (length <= 14) {
					code[pos] = (byte)length;
					return 1;
				}
				code[pos] = 0x0F;
				code[pos + 1] = (byte)(length - 1);
				return 2;
			}
			if (length == 2 && offset < 65) {
				code[pos] = (byte)(0x40 + offset - 1);
				return 1;
			}
			if (length <= 33 && offset < 257) {
				code[pos] = (byte)(0x20 + length - 2);
				code[pos + 1] = (byte)(offset - 1);
				return 2;
			}
			if (length >= 3 && length <= 10) {
				code[pos] = (byte)(0x80 + ((length - 3) << 4) + ((offset - 1) >> 8));
				code[pos + 1] = (byte)(offset - 1);
				return 2;
			}
			code[pos] = (byte)(0x10 + ((offset - 1) >> 8));
			code[pos + 1] = (byte)(offset - 1);
			code[pos + 2] = (byte)(length - 1);
			return 3;
		}

		private int PackStd(byte[] bufIn, int lengthIn, byte[] bufOut, int lengthOut) {
			int n = lengthIn;

			// Chaînes de hachage sur 2 octets : prev[i] = position précédente (la plus proche) commençant par les mêmes 2 octets
			int[] head = new int[65536];
			for (int i = 0; i < head.Length; i++)
				head[i] = -1;
			int[] prev = new int[n + 1];
			for (int i = 0; i < n; i++)
				prev[i] = -1;
			for (int i = 0; i < n - 1; i++) {
				int key = bufIn[i] | (bufIn[i + 1] << 8);
				prev[i] = head[key];
				head[key] = i;
			}

			// Programmation dynamique, de la fin vers le début : cost[i] = nombre de bits minimal pour coder bufIn[i..]
			// (littéral = 1 + 8 bits ; copie = 1 bit + 8 bits par octet de code)
			int[] cost = new int[n + 1];
			int[] chLen = new int[n + 1];
			int[] chOff = new int[n + 1];
			int[] omin = new int[MAXSTRING + 2];        // plus petit offset permettant une copie de longueur L
			int[] runOff = new int[MAXSTRING + 2];      // offsets utilisables sous la forme "offset == longueur"
			for (int i = n - 1; i >= 0; i--) {
				int best = 9 + cost[i + 1];
				int bestLen = 0, bestOff = 0;
				if (i < n - 1) {
					int maxg = Math.Min(MAXSTRING, n - i);
					int longest = 1, nbRun = 0, steps = 0;
					for (int j = prev[i]; j >= 0; j = prev[j]) {
						int off = i - j;
						if (off > SEEKBACK || ++steps > MAX_CHAIN)
							break;

						if (longest >= maxg && off > MAXSTRING)
							break;

						int maxl = Math.Min(maxg, off);
						if (maxl < 2)
							continue;

						int m = 2;
						while (m < maxl && bufIn[j + m] == bufIn[i + m])
							m++;

						if (m > longest) {
							for (int l = longest + 1; l <= m; l++)
								omin[l] = off;
							longest = m;
						}
						if (off >= 2 && off <= maxg && m >= off)
							runOff[nbRun++] = off;
					}
					for (int l = 2; l <= longest; l++) {
						int cc = 1 + 8 * StdMatchBytes(l, omin[l]) + cost[i + l];
						if (cc < best) {
							best = cc;
							bestLen = l;
							bestOff = omin[l];
						}
					}
					for (int r = 0; r < nbRun; r++) {
						int l = runOff[r];
						int cc = 1 + 8 * StdMatchBytes(l, l) + cost[i + l];
						if (cc < best) {
							best = cc;
							bestLen = l;
							bestOff = l;
						}
					}
				}
				cost[i] = best;
				chLen[i] = bestLen;
				chOff[i] = bestOff;
			}

			// Écriture : 8 codes par octet de drapeaux (bit à 1 = copie), puis code de fin
			byte[] code = new byte[24];
			int bits = 0, bitcount = 0, codecount = 0, pos = 0;
			while (pos < n) {
				int len = chLen[pos];
				if (len == 0)
					code[codecount++] = bufIn[pos++];
				else {
					codecount += StdWriteMatch(code, codecount, len, chOff[pos]);
					bits |= 1 << bitcount;
					pos += len;
				}
				bitcount++;
				if (bitcount == 8) {
					bufOut[lengthOut++] = (byte)bits;
					Array.Copy(code, 0, bufOut, lengthOut, codecount);
					lengthOut += codecount;
					bitcount = codecount = bits = 0;
				}
			}
			code[codecount++] = 0;
			bufOut[lengthOut++] = (byte)(bits | (1 << bitcount));
			Array.Copy(code, 0, bufOut, lengthOut, codecount);
			return lengthOut + codecount;
		}

		private void WriteBit(int value, byte[] outputData) {
			if (backTrack) {
				if (value != 0)
					outputData[outputIndex - 1] |= 1;

				backTrack = false;
			}
			else {
				if (bitMask == 0) {
					bitMask = 128;
					bitIndex = outputIndex;
					outputData[outputIndex++] = 0;
				}
				if (value != 0)
					outputData[bitIndex] |= (byte)bitMask;

				bitMask >>= 1;
			}
		}

		private void WriteInterlacedEliasGammaZX0(int value, byte[] outputData, bool invertMode) {
			int i;

			for (i = 2; i <= value; i <<= 1)
				;
			i >>= 1;
			while ((i >>= 1) != 0) {
				WriteBit(0, outputData);
				WriteBit(invertMode ? (value & i) == 0 ? 1 : 0 : value & i, outputData);
			}
			WriteBit(1, outputData);
		}

		private int ZxNewNode(int bits, int idx, int off, int prev) {
			int id = zxNCount++;
			zxNBits[id] = bits;
			zxNIdx[id] = idx;
			zxNOff[id] = off;
			zxNPrev[id] = prev;
			return id;
		}

		// Noeud correspondant au dernier bloc "copie" de l'offset o (créé seulement si nécessaire)
		private int ZxMatM(int o) {
			if (zxMNode[o] < 0)
				zxMNode[o] = ZxNewNode(zxMBase[o] + 8 * zxMIdx[o], zxMIdx[o], o, zxMPrev[o]);
			return zxMNode[o];
		}

		private void ZxMark(bool[] mark, int id) {
			while (id >= 0 && !mark[id]) {
				mark[id] = true;
				id = zxNPrev[id];
			}
		}

		// Ramasse-miettes : ne conserve que les noeuds accessibles depuis les meilleurs blocs et les états des offsets
		private void ZxCollect(int index) {
			int cnt = zxNCount;
			bool[] mark = new bool[cnt];
			int[] remap = new int[cnt];
			for (int x = 0; x < index; x++)
				ZxMark(mark, zxTabNode[x]);

			for (int o = 1; o <= zxMaxOff; o++) {
				ZxMark(mark, zxMPrev[o]);
				ZxMark(mark, zxMNode[o]);
				ZxMark(mark, zxLNode[o]);
			}
			int k = 0;
			for (int id = 0; id < cnt; id++) {
				if (mark[id]) {
					remap[id] = k;
					int prev = zxNPrev[id];
					zxNBits[k] = zxNBits[id];
					zxNIdx[k] = zxNIdx[id];
					zxNOff[k] = zxNOff[id];
					zxNPrev[k] = prev >= 0 ? remap[prev] : -1;
					k++;
				}
				else
					remap[id] = -1;
			}
			for (int x = 0; x < index; x++)
				if (zxTabNode[x] >= 0)
					zxTabNode[x] = remap[zxTabNode[x]];

			for (int o = 1; o <= zxMaxOff; o++) {
				if (zxMPrev[o] >= 0)
					zxMPrev[o] = remap[zxMPrev[o]];

				if (zxMNode[o] >= 0)
					zxMNode[o] = remap[zxMNode[o]];

				if (zxLNode[o] >= 0)
					zxLNode[o] = remap[zxLNode[o]];
			}
			zxNCount = k;
		}

		private void ZxGrow(int cap) {
			Array.Resize(ref zxNBits, cap);
			Array.Resize(ref zxNIdx, cap);
			Array.Resize(ref zxNOff, cap);
			Array.Resize(ref zxNPrev, cap);
		}

		//
		// Parcours optimal ZX0 / ZX1 (zx1 : coût d'un nouvel offset de ZX1, limite d'offset 'limit'). Retourne le nombre de bits ;
		// parseIdx / parseOff = index de fin et offset (0 = littéral) de chaque bloc, dans l'ordre. État par offset o (valeurs, sans bloc) :
		//   zxMBase[o] = bits - 8 * index du dernier bloc "copie" (ZX_INF = aucun), zxMIdx[o] = son index, zxMPrev[o] = son précédent ;
		//   le bloc littéral qui le suit se calcule par une formule, et n'est créé que si une copie depuis le dernier offset le suit.
		//
		private int ZxOptimize(byte[] inputData, int inputSize, int limit, bool zx1, out int[] parseIdx, out int[] parseOff, out int parseCount) {
			int n = inputSize;
			int mo = n - 1 > limit ? limit : n - 1 < 1 ? 1 : n - 1;
			zxMaxOff = mo;
			int[] mlen = new int[mo + 1];
			int[] eqLast = new int[mo + 1];
			int[] lBitsCur = new int[mo + 1];
			int[] lIdxCur = new int[mo + 1];
			int[] egOff = new int[mo + 1];      // coût en bits d'un nouvel offset (sans la longueur)
			bool[] lFlag = new bool[mo + 1];
			int[] mBase = zxMBase = new int[mo + 1];
			int[] mIdx = zxMIdx = new int[mo + 1];
			zxMPrev = new int[mo + 1];
			zxMNode = new int[mo + 1];
			zxLNode = new int[mo + 1];
			zxLNodeIdx = new int[mo + 1];
			int[] tabNode = zxTabNode = new int[n + 1];
			int[] tabBits = zxTabBits = new int[n + 1];
			int[] bestLength = new int[n + 2];
			int[] egTab = new int[n + 3];
			for (int v = 0; v < egTab.Length; v++) {
				int b = 1, x = v;
				while ((x >>= 1) != 0)
					b += 2;
				egTab[v] = b;
			}
			for (int o = 1; o <= mo; o++) {
				mBase[o] = ZX_INF;
				zxMPrev[o] = -1;
				zxMNode[o] = -1;
				zxLNode[o] = -1;
				zxLNodeIdx[o] = -2;
				eqLast[o] = -2;
				egOff[o] = zx1 ? 1 + (o > 128 ? 16 : 8) : 8 + egTab[(o - 1) / 128 + 1];
			}
			int cap = 1 << 18;
			zxNBits = new int[cap];
			zxNIdx = new int[cap];
			zxNOff = new int[cap];
			zxNPrev = new int[cap];
			zxNCount = 0;

			// bloc fictif de départ (bits = -1, index = -1, offset = 1)
			mBase[1] = 7;
			mIdx[1] = -1;
			bestLength[2] = 2;
			int dots = 0;
			for (int index = 0; index < n; index++) {
				int maxOffset = index > limit ? limit : index < 1 ? 1 : index;
				if (zxNCount + 2 * maxOffset + 16 > zxNBits.Length) {
					ZxCollect(index);
					if (zxNCount + 4 * maxOffset + 64 > zxNBits.Length)
						ZxGrow(Math.Max(2 * zxNBits.Length, zxNCount + 8 * maxOffset + 4096));
				}
				int bestLengthSize = 2, best = ZX_INF, wOff = 0, wKind = 0;
				byte cur = inputData[index];
				int i8 = 8 * index + 1;
				for (int offset = 1; offset <= maxOffset; offset++) {
					if (index != 0 && inputData[index - offset] == cur) {
						int ml;
						if (eqLast[offset] == index - 1)
							ml = ++mlen[offset];
						else {
							ml = mlen[offset] = 1;
							bool has = mBase[offset] < ZX_INF;
							lFlag[offset] = has;
							if (has) {
								// bloc littéral qui précède cette série d'égalités : créé seulement maintenant
								int len = index - 1 - mIdx[offset];
								int lb = mBase[offset] + 8 * mIdx[offset] + 1 + egTab[len] + 8 * len;
								int ln;
								if (zxLNodeIdx[offset] == index - 1)
									ln = zxLNode[offset];
								else {
									int pm = ZxMatM(offset);
									ln = ZxNewNode(lb, index - 1, 0, pm);
								}
								zxLNode[offset] = ln;
								zxLNodeIdx[offset] = index - 1;
								lBitsCur[offset] = lb;
								lIdxCur[offset] = index - 1;
							}
						}
						eqLast[offset] = index;
						if (lFlag[offset]) {
							// copie depuis le dernier offset
							int bitsR = lBitsCur[offset] + 1 + egTab[index - lIdxCur[offset]];
							mBase[offset] = bitsR - 8 * index;
							mIdx[offset] = index;
							zxMPrev[offset] = zxLNode[offset];
							zxMNode[offset] = -1;
							if (bitsR < best) {
								best = bitsR;
								wOff = offset;
								wKind = 0;
							}
						}
						if (ml > 1) {
							// copie depuis un nouvel offset
							if (bestLengthSize < ml) {
								int bits = tabBits[index - bestLength[bestLengthSize]] + egTab[bestLength[bestLengthSize] - 1];
								do {
									bestLengthSize++;
									int bits2 = tabBits[index - bestLengthSize] + egTab[bestLengthSize - 1];
									if (bits2 <= bits) {
										bestLength[bestLengthSize] = bestLengthSize;
										bits = bits2;
									}
									else
										bestLength[bestLengthSize] = bestLength[bestLengthSize - 1];
								} while (bestLengthSize < ml);
							}
							int length = bestLength[ml];
							int bitsN = tabBits[index - length] + egOff[offset] + egTab[length - 1];
							if (mBase[offset] >= ZX_INF || mIdx[offset] != index || mBase[offset] + 8 * index > bitsN) {
								mBase[offset] = bitsN - 8 * index;
								mIdx[offset] = index;
								zxMPrev[offset] = tabNode[index - length];
								zxMNode[offset] = -1;
								if (bitsN < best) {
									best = bitsN;
									wOff = offset;
									wKind = 0;
								}
							}
						}
					}
					else {
						// bloc littéral candidat (offset sans copie : valeur énorme, jamais retenue)
						int c = mBase[offset] + i8 + egTab[index - mIdx[offset]];
						if (c < best) {
							best = c;
							wOff = offset;
							wKind = 1;
						}
					}
				}
				int node;
				if (wKind == 1) {
					int pm = ZxMatM(wOff);
					node = ZxNewNode(best, index, 0, pm);
					zxLNode[wOff] = node;
					zxLNodeIdx[wOff] = index;
				}
				else
					node = ZxMatM(wOff);
				tabNode[index] = node;
				tabBits[index] = best;
				if (index * MAX_SCALE / inputSize > dots) {
					progressBar1.Value = 100 * ++dots / MAX_SCALE;
					Application.DoEvents();
				}
			}

			int total = tabBits[n - 1];
			int cnt = 0;
			for (int id = tabNode[n - 1]; id >= 0 && zxNIdx[id] >= 0; id = zxNPrev[id])
				cnt++;
			parseIdx = new int[cnt];
			parseOff = new int[cnt];
			int p = cnt;
			for (int id = tabNode[n - 1]; id >= 0 && zxNIdx[id] >= 0; id = zxNPrev[id]) {
				p--;
				parseIdx[p] = zxNIdx[id];
				parseOff[p] = zxNOff[id];
			}
			parseCount = cnt;

			// libération de la mémoire
			zxNBits = zxNIdx = zxNOff = zxNPrev = null;
			zxMBase = zxMIdx = zxMPrev = zxMNode = zxLNode = zxLNodeIdx = zxTabNode = zxTabBits = null;
			return total;
		}

		private void WriteInterlacedEliasGammaZX1(int value, byte[] outputData) {
			int i;

			for (i = 2; i <= value; i <<= 1)
				;
			i >>= 1;
			while ((i >>= 1) > 0) {
				WriteBit(1, outputData);
				WriteBit(value & i, outputData);
			}
			WriteBit(0, outputData);
		}

		private int PackZX0(byte[] inputData, int inputSize, byte[] outputData, bool v2) {
			int totalBits = ZxOptimize(inputData, inputSize, MAX_OFFSET_ZX0, false, out int[] parseIdx, out int[] parseOff, out int parseCount);
			int outputSize = (totalBits + 25) / 8;

			outputIndex = 0;
			bitMask = 0;
			int lastOffset = 1;
			int inputIndex = 0;
			int prevIndex = -1;
			backTrack = true;
			for (int b = 0; b < parseCount; b++) {
				int length = parseIdx[b] - prevIndex;
				int offset = parseOff[b];
				prevIndex = parseIdx[b];
				if (offset == 0) {
					WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX0(length, outputData, false);
					for (int i = 0; i < length; i++)
						outputData[outputIndex++] = inputData[inputIndex++];
				}
				else if (offset == lastOffset) {
					WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX0(length, outputData, false);
					inputIndex += length;
				}
				else {
					WriteBit(1, outputData);
					WriteInterlacedEliasGammaZX0((offset - 1) / 128 + 1, outputData, v2);
					outputData[outputIndex++] = (byte)((127 - (offset - 1) % 128) << 1);
					backTrack = true;
					WriteInterlacedEliasGammaZX0(length - 1, outputData, false);
					inputIndex += length;
					lastOffset = offset;
				}
			}
			WriteBit(1, outputData);
			WriteInterlacedEliasGammaZX0(256, outputData, v2);
			return outputSize;
		}

		private int PackZX1(byte[] inputData, int inputSize, byte[] outputData) {
			int totalBits = ZxOptimize(inputData, inputSize, MAX_OFFSET_ZX1, true, out int[] parseIdx, out int[] parseOff, out int parseCount);
			int outputSize = (totalBits + 24) / 8;

			outputIndex = 0;
			bitMask = 0;
			backTrack = false;
			int lastOffset = 1;
			int inputIndex = 0;
			int prevIndex = -1;
			bool first = true;
			for (int b = 0; b < parseCount; b++) {
				int length = parseIdx[b] - prevIndex;
				int offset = parseOff[b];
				prevIndex = parseIdx[b];
				if (offset == 0) {
					if (first)
						first = false;
					else
						WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX1(length, outputData);
					for (int i = 0; i < length; i++)
						outputData[outputIndex++] = inputData[inputIndex++];
				}
				else if (offset == lastOffset) {
					WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX1(length, outputData);
					inputIndex += length;
				}
				else {
					WriteBit(1, outputData);
					if (offset > 128) {
						outputData[outputIndex++] = (byte)(255 - ((offset - 1) & 254));
						outputData[outputIndex++] = (byte)(252 - (offset - 1) / 256 * 2 + offset % 2);
					}
					else
						outputData[outputIndex++] = (byte)(256 - offset * 2);
					WriteInterlacedEliasGammaZX1(length - 1, outputData);
					inputIndex += length;
					lastOffset = offset;
				}
			}
			WriteBit(1, outputData);
			outputData[outputIndex++] = 255;
			outputData[outputIndex++] = 255;
			return outputSize;
		}

		public int Depack(byte[] bufIn, int startIn, byte[] bufOut) {
			return DepackStd(bufIn, startIn, bufOut);
		}

		public int Pack(byte[] bufIn, int lengthIn, byte[] bufOut, int lengthOut, Main.PackMethode pkMethode, bool extShow = false) {
			if (!extShow)
				Show();

			int ret = 0;
			switch (pkMethode) {
				case Main.PackMethode.Standard:
					ret = PackStd(bufIn, lengthIn, bufOut, lengthOut);
					break;

				case Main.PackMethode.ZX0:
					ret = PackZX0(bufIn, lengthIn, bufOut, false);
					break;

				case Main.PackMethode.ZX0_V2:
					ret = PackZX0(bufIn, lengthIn, bufOut, true);
					break;

				case Main.PackMethode.ZX1:
					ret = PackZX1(bufIn, lengthIn, bufOut);
					break;

				case Main.PackMethode.ZX0Ovs:
					byte[] bufTmp = new byte[0x8000];
					int posIn = 0, posOut = 0;
					for (int i = 0; i < 15; i++) {
						//Console.WriteLine("PosIn:" + (posIn + 0x200).ToString("X4") + ", posOut:" + (posOut + 0x200).ToString("X4"));
						int lIn = i < 7 ? 0x600 : i == 7 ? 0xCC0 : 0x6C0;
						Array.Copy(bufIn, posIn, bufTmp, posOut, lIn);
						posIn += i != 7 ? 0x800 : 0xE00;
						posOut += lIn;
					}
					//Console.WriteLine("Final - PosIn:" + (posIn + 0x200).ToString("X4") + ", posOut:" + (posOut + 0x200).ToString("X4"));
					ret = PackZX0(bufTmp, posOut, bufOut, false);
					break;
			}
			if (!extShow)
				Hide();

			return ret;
		}
	}

	public class Block {
		public Block chain, ghostChain;
		public int bits, index, offset, references, length;
	}
}
