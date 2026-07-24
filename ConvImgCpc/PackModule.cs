using System;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class PackModule : Form {
		private const int SEEKBACK = 0x1000;
		private const int MAXSTRING = 256;

		private const int MAX_OFFSET_ZX0 = 32640;
		private const int MAX_OFFSET_ZX1 = 32512;
		private const int MAX_SCALE = 50;

		private readonly int[] matches = new int[MAXSTRING];
		private readonly int[,] matchtable = new int[MAXSTRING, SEEKBACK];
		private Block ghostRoot;
		private int outputIndex, bitIndex, bitMask;
		private bool backTrack;

		public PackModule() {
			InitializeComponent();
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

		private int PackStd(byte[] bufIn, int lengthIn, byte[] bufOut, int lengthOut) {
			byte[] codebuffer = new byte[24];
			byte bits = 0;
			int count = 0, bitcount = 0, codecount = 0;
			int matchtablestart = 0, matchtableend = 0, oldmatchtablestart;
			int start, end, max, b, c, d;

			for (int i = 0; i < matches.Length; i++)
				matches[i] = 0;

			while (true) {
				for (c = matchtableend; c < count; c++) {
					b = bufIn[c];
					matchtable[b, matches[b]] = c;
					matches[b]++;
				}
				matchtableend = count;

				if (count >= 2) {
					int stlen = 0;
					int stpos = 0;
					int stlen2 = 0;
					int bb = bufIn[count];
					for (c = matches[bb] - 1; c >= 0; c--) {
						start = matchtable[bb, c];
						end = start + MAXSTRING;
						if (end > count)
							end = count;

						max = end - start;
						if (max >= stlen) {
							for (d = 1; d < max; d++)
								if (start + d >= lengthIn || count + d >= lengthIn || bufIn[start + d] != bufIn[count + d])
									break;

							if ((d >= 2) && (d > stlen)) {
								stlen = d;
								stpos = count - start;
							}
							if ((d == stlen) && (count - start < stpos))
								stpos = count - start;
						}

						if ((stlen == MAXSTRING) && (stpos == stlen))
							break;
					}
					if (count + 1 < lengthIn) {
						bb = bufIn[count + 1];
						for (c = matches[bb] - 1; c >= 0; c--) {
							start = matchtable[bb, c];
							end = start + MAXSTRING;
							if (end > count + 1)
								end = count + 1;

							max = end - start;
							if (max >= stlen2) {
								for (d = 1; d < max; d++)
									if (start + d >= lengthIn || count + d + 1 >= lengthIn || bufIn[start + d] != bufIn[count + d + 1])
										break;

								if ((d >= 2) && (d >= stlen2))
									stlen2 = d;
							}
							if (stlen2 == MAXSTRING)
								break;
						}
						if (stlen2 - 1 > stlen)
							stlen = 0;
					}

					if (stlen > 1) {
						if ((stlen == 2) && (stpos >= MAXSTRING)) {
							codebuffer[codecount++] = bufIn[count++];
							bitcount++;
						}
						else {
							if (stpos == stlen) {
								if (stlen == MAXSTRING)
									codebuffer[codecount++] = 0x1;
								else {
									if (stlen <= 14)
										codebuffer[codecount++] = (byte)stlen;
									else {
										codebuffer[codecount++] = 0x0F;
										codebuffer[codecount++] = (byte)(stlen - 1);
									}
								}
							}
							else {
								if ((stlen == 2) && (stpos < 65))
									codebuffer[codecount++] = (byte)(0x40 + stpos - 1);
								else {
									if ((stlen <= 33) && (stpos < 257)) {
										codebuffer[codecount++] = (byte)(0x20 + stlen - 2);
										codebuffer[codecount++] = (byte)(stpos - 1);
									}
									else {
										if ((stlen >= 3) && (stlen <= 10)) {
											codebuffer[codecount++] = (byte)(0x80 + ((stlen - 3) << 4) + ((stpos - 1) >> 8));
											codebuffer[codecount++] = (byte)(stpos - 1);
										}
										else {
											codebuffer[codecount++] = (byte)(0x10 + ((stpos - 1) >> 8));
											codebuffer[codecount++] = (byte)(stpos - 1);
											codebuffer[codecount++] = (byte)(stlen - 1);
										}
									}
								}
							}
							bits = (byte)(bits | (1 << bitcount));
							bitcount++;
							count += stlen;
						}
					}
					else {
						codebuffer[codecount++] = bufIn[count++];
						bitcount++;
					}
				}
				else {
					codebuffer[codecount++] = bufIn[count++];
					bitcount++;
				}
				if (bitcount == 8) {
					bufOut[lengthOut++] = bits;
					System.Array.Copy(codebuffer, 0, bufOut, lengthOut, codecount);
					lengthOut += codecount;
					bitcount = codecount = 0;
					bits = 0;
				}
				if (count >= lengthIn)
					break;

				oldmatchtablestart = matchtablestart;
				matchtablestart = count - SEEKBACK;
				if (matchtablestart < 0)
					matchtablestart = 0;

				for (c = oldmatchtablestart; c < matchtablestart; c++) {
					b = bufIn[c];
					for (d = 0; d < matches[b]; d++)
						if (matchtable[b, d] >= matchtablestart) {
							System.Buffer.BlockCopy(matchtable, ((b * SEEKBACK) + d) * sizeof(int), matchtable, b * SEEKBACK * sizeof(int), (matches[b] - d) * sizeof(int));
							break;
						}

					matches[b] -= d;
				}
			}
			codebuffer[codecount++] = 0;
			bufOut[lengthOut++] = (byte)(bits | (1 << bitcount));
			System.Array.Copy(codebuffer, 0, bufOut, lengthOut, codecount);
			return (lengthOut + codecount);
		}

		private Block Allocate(int bits, int index, int offset, Block chain) {
			Block ptr;

			if (ghostRoot == null)
				ptr = new Block();
			else {
				ptr = ghostRoot;
				ghostRoot = ptr.ghostChain;
				if (ptr.chain != null && --ptr.chain.references == 0) {
					ptr.chain.ghostChain = ghostRoot;
					ghostRoot = ptr.chain;
				}
			}
			ptr.bits = bits;
			ptr.index = index;
			ptr.offset = offset;
			if (chain != null)
				chain.references++;

			ptr.chain = chain;
			ptr.references = 0;
			return ptr;
		}

		private void Assign(ref Block ptr, Block chain) {
			chain.references++;
			if (ptr != null && --ptr.references == 0) {
				ptr.ghostChain = ghostRoot;
				ghostRoot = ptr;
			}
			ptr = chain;
		}

		private int EliasGammaBits(int value) {
			int bits = 1;
			while ((value >>= 1) != 0)
				bits += 2;
			return bits;
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

		public int PackZX0(byte[] inputData, int inputSize, byte[] outputData, bool v2) {
			Show();
			int bits, length;
			int maxOffset = inputSize - 1 > MAX_OFFSET_ZX0 ? MAX_OFFSET_ZX0 : inputSize - 1 < 1 ? 1 : inputSize - 1;
			int dots = 0;
			Block[] lastLiteral = new Block[maxOffset + 1];
			Block[] lastMatch = new Block[maxOffset + 1];
			Block[] tabOptimal = new Block[inputSize + 1];
			int[] matchLength = new int[maxOffset + 1];
			int[] bestLength = new int[inputSize + 2];

			bestLength[2] = 2;
			Block chain = Allocate(-1, 0 - 1, 1, null);
			chain.references++;
			if (lastMatch[1] != null && --lastMatch[1].references == 0) {
				lastMatch[1].ghostChain = ghostRoot;
				ghostRoot = lastMatch[1];
			}
			lastMatch[1] = chain;
			for (int index = 0; index < inputSize; index++) {
				int bestLengthSize = 2;
				maxOffset = index > MAX_OFFSET_ZX0 ? MAX_OFFSET_ZX0 : index < 1 ? 1 : index;
				for (int offset = 1; offset <= maxOffset; offset++) {
					if (index != 0 && index >= offset && inputData[index] == inputData[index - offset]) {
						if (lastLiteral[offset] != null) {
							length = index - lastLiteral[offset].index;
							bits = lastLiteral[offset].bits + 1 + EliasGammaBits(length);
							chain = Allocate(bits, index, offset, lastLiteral[offset]);
							chain.references++;
							if (lastMatch[offset] != null && --lastMatch[offset].references == 0) {
								lastMatch[offset].ghostChain = ghostRoot;
								ghostRoot = lastMatch[offset];
							}
							lastMatch[offset] = chain;
							if (tabOptimal[index] == null || tabOptimal[index].bits > bits) {
								chain = lastMatch[offset];
								chain.references++;
								if (tabOptimal[index] != null && --tabOptimal[index].references == 0) {
									tabOptimal[index].ghostChain = ghostRoot;
									ghostRoot = tabOptimal[index];
								}
								tabOptimal[index] = chain;
							}
						}
						if (++matchLength[offset] > 1) {
							if (bestLengthSize < matchLength[offset]) {
								bits = tabOptimal[index - bestLength[bestLengthSize]].bits + EliasGammaBits(bestLength[bestLengthSize] - 1);
								do {
									bestLengthSize++;
									int bits2 = tabOptimal[index - bestLengthSize].bits + EliasGammaBits(bestLengthSize - 1);
									if (bits2 <= bits) {
										bestLength[bestLengthSize] = bestLengthSize;
										bits = bits2;
									}
									else
										bestLength[bestLengthSize] = bestLength[bestLengthSize - 1];

								} while (bestLengthSize < matchLength[offset]);
							}
							length = bestLength[matchLength[offset]];
							bits = tabOptimal[index - length].bits + 8 + EliasGammaBits((offset - 1) / 128 + 1) + EliasGammaBits(length - 1);
							if (lastMatch[offset] == null || lastMatch[offset].index != index || lastMatch[offset].bits > bits) {
								chain = Allocate(bits, index, offset, tabOptimal[index - length]);
								chain.references++;
								if (lastMatch[offset] != null && --lastMatch[offset].references == 0) {
									lastMatch[offset].ghostChain = ghostRoot;
									ghostRoot = lastMatch[offset];
								}
								lastMatch[offset] = chain;
								if (tabOptimal[index] == null || tabOptimal[index].bits > bits) {
									chain = lastMatch[offset];
									chain.references++;
									if (tabOptimal[index] != null && --tabOptimal[index].references == 0) {
										tabOptimal[index].ghostChain = ghostRoot;
										ghostRoot = tabOptimal[index];
									}
									tabOptimal[index] = chain;
								}
							}
						}
					}
					else {
						matchLength[offset] = 0;
						if (lastMatch[offset] != null) {
							length = index - lastMatch[offset].index;
							bits = lastMatch[offset].bits + 1 + EliasGammaBits(length) + (length << 3);
							chain = Allocate(bits, index, 0, lastMatch[offset]);
							chain.references++;
							if (lastLiteral[offset] != null && --lastLiteral[offset].references == 0) {
								lastLiteral[offset].ghostChain = ghostRoot;
								ghostRoot = lastLiteral[offset];
							}
							lastLiteral[offset] = chain;
							if (tabOptimal[index] == null || tabOptimal[index].bits > bits) {
								chain = lastLiteral[offset];
								chain.references++;
								if (tabOptimal[index] != null && --tabOptimal[index].references == 0) {
									tabOptimal[index].ghostChain = ghostRoot;
									ghostRoot = tabOptimal[index];
								}
								tabOptimal[index] = chain;
							}
						}
					}
				}
				if (index * MAX_SCALE / inputSize > dots) {
					progressBar1.Value = 100 * ++dots / MAX_SCALE;
					Application.DoEvents();
				}
			}
			Block prev = null, next, optimal = tabOptimal[inputSize - 1];
			int outputSize = (optimal.bits + 25) / 8;
			while (optimal != null) {
				next = optimal.chain;
				optimal.chain = prev;
				prev = optimal;
				optimal = next;
			}
			outputIndex = 0;
			bitMask = 0;
			int lastOffset = 1;
			int inputIndex = 0;
			backTrack = true;
			for (optimal = prev.chain; optimal != null; prev = optimal, optimal = optimal.chain) {
				length = optimal.index - prev.index;

				if (optimal.offset == 0) {
					WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX0(length, outputData, false);
					for (int i = 0; i < length; i++)
						outputData[outputIndex++] = inputData[inputIndex++];
				}
				else if (optimal.offset == lastOffset) {
					WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX0(length, outputData, false);
					inputIndex += length;
				}
				else {
					WriteBit(1, outputData);
					WriteInterlacedEliasGammaZX0((optimal.offset - 1) / 128 + 1, outputData, v2);
					outputData[outputIndex++] = (byte)((127 - (optimal.offset - 1) % 128) << 1);
					backTrack = true;
					WriteInterlacedEliasGammaZX0(length - 1, outputData, false);
					inputIndex += length;
					lastOffset = optimal.offset;
				}
			}
			WriteBit(1, outputData);
			WriteInterlacedEliasGammaZX0(256, outputData, v2);
			Hide();
			return outputSize;
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

		private int PackZX1(byte[] inputData, int inputSize, byte[] outputData) {
			Show();
			int bits, length;
			int maxOffset = inputSize - 1 > MAX_OFFSET_ZX1 ? MAX_OFFSET_ZX1 : inputSize - 1 < 1 ? 1 : inputSize - 1;
			int dots = 0;
			Block[] lastLiteral = new Block[maxOffset + 1];
			Block[] lastMatch = new Block[maxOffset + 1];
			Block[] tabOptimal = new Block[inputSize + 1];
			int[] matchLength = new int[maxOffset + 1];
			int[] bestLength = new int[inputSize + 2];

			bestLength[2] = 2;
			Assign(ref lastMatch[1], Allocate(-1, -1, 1, null));
			for (int index = 0; index < inputSize; index++) {
				int bestLengthSize = 2;
				maxOffset = index > MAX_OFFSET_ZX1 ? MAX_OFFSET_ZX1 : index < 1 ? 1 : index;
				for (int offset = 1; offset <= maxOffset; offset++) {
					if (index != 0 && index >= offset && inputData[index] == inputData[index - offset]) {
						if (lastLiteral[offset] != null) {
							length = index - lastLiteral[offset].index;
							bits = lastLiteral[offset].bits + 1 + EliasGammaBits(length);
							Assign(ref lastMatch[offset], Allocate(bits, index, offset, lastLiteral[offset]));
							if (tabOptimal[index] == null || tabOptimal[index].bits > bits)
								Assign(ref tabOptimal[index], lastMatch[offset]);
						}
						if (++matchLength[offset] > 1) {
							if (bestLengthSize < matchLength[offset]) {
								bits = tabOptimal[index - bestLength[bestLengthSize]].bits + EliasGammaBits(bestLength[bestLengthSize] - 1);
								do {
									bestLengthSize++;
									int bits2 = tabOptimal[index - bestLengthSize].bits + EliasGammaBits(bestLengthSize - 1);
									if (bits2 <= bits) {
										bestLength[bestLengthSize] = bestLengthSize;
										bits = bits2;
									}
									else
										bestLength[bestLengthSize] = bestLength[bestLengthSize - 1];
								}
								while (bestLengthSize < matchLength[offset]);
							}
							length = bestLength[matchLength[offset]];
							bits = tabOptimal[index - length].bits + 1 + (offset > 128 ? 16 : 8) + EliasGammaBits(length - 1);
							if (lastMatch[offset] == null || lastMatch[offset].index != index || lastMatch[offset].bits > bits) {
								Assign(ref lastMatch[offset], Allocate(bits, index, offset, tabOptimal[index - length]));
								if (tabOptimal[index] == null || tabOptimal[index].bits > bits)
									Assign(ref tabOptimal[index], lastMatch[offset]);
							}
						}
					}
					else {
						matchLength[offset] = 0;
						if (lastMatch[offset] != null) {
							length = index - lastMatch[offset].index;
							bits = lastMatch[offset].bits + 1 + EliasGammaBits(length) + length * 8;
							Assign(ref lastLiteral[offset], Allocate(bits, index, 0, lastMatch[offset]));
							if (tabOptimal[index] == null || tabOptimal[index].bits > bits)
								Assign(ref tabOptimal[index], lastLiteral[offset]);
						}
					}
				}
				if (index * MAX_SCALE / inputSize > dots) {
					progressBar1.Value = 100 * ++dots / MAX_SCALE;
					Application.DoEvents();
				}
			}
			Block prev, next = null, optimal = tabOptimal[inputSize - 1];
			int outputSize = (optimal.bits + 24) / 8;
			while (optimal != null) {
				prev = optimal.chain;
				optimal.chain = next;
				next = optimal;
				optimal = prev;
			}
			outputIndex = 0;
			bitMask = 0;
			int lastOffset = 1;
			int inputIndex = 0;
			bool first = true;
			for (optimal = next.chain; optimal != null; optimal = optimal.chain) {
				if (optimal.offset == 0) {
					if (first)
						first = false;
					else
						WriteBit(0, outputData);

					WriteInterlacedEliasGammaZX1(optimal.length, outputData);
					for (int i = 0; i < optimal.length; i++)
						outputData[outputIndex++] = inputData[inputIndex++];
				}
				else
					if (optimal.offset == lastOffset) {
					WriteBit(0, outputData);
					WriteInterlacedEliasGammaZX1(optimal.length, outputData);
					inputIndex += optimal.length;
				}
				else {
					WriteBit(1, outputData);
					if (optimal.offset > 128) {
						outputData[outputIndex++] = (byte)(255 - ((optimal.offset - 1) & 254));
						outputData[outputIndex++] = (byte)(252 - (optimal.offset - 1) / 256 * 2 + optimal.offset % 2);
					}
					else
						outputData[outputIndex++] = (byte)(256 - optimal.offset * 2);

					WriteInterlacedEliasGammaZX1(optimal.length - 1, outputData);
					inputIndex += optimal.length;
					lastOffset = optimal.offset;
				}
			}
			WriteBit(1, outputData);
			outputData[outputIndex++] = 255;
			outputData[outputIndex++] = 255;
			Hide();
			return outputSize;
		}

		public int Depack(byte[] bufIn, int startIn, byte[] bufOut) {
			return DepackStd(bufIn, startIn, bufOut);
		}

		public int Pack(byte[] bufIn, int lengthIn, byte[] bufOut, int lengthOut, Main.PackMethode pkMethode) {
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
			return ret;
		}
	}

	public class Block {
		public Block chain, ghostChain;
		public int bits, index, offset, references, length;
	}
}
