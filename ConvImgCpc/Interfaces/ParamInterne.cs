using System;
using System.Windows.Forms;

namespace ConvImgCpc {
	public partial class ParamInterne : Form {
		private bool stopModif;
		private readonly Main main;

		public ParamInterne(Main m) {
			InitializeComponent();
			main = m;
		}

		public void InitValues() {
			stopModif = true;
			numericLumR.Value = trackLumR.Value = main.param.coefR;
			numericLumV.Value = trackLumV.Value = main.param.coefV;
			numericLumB.Value = trackLumB.Value = main.param.coefB;
			numericR1.Value = trackR1.Value = main.param.cstR1;
			numericR2.Value = trackR2.Value = main.param.cstR2;
			numericR3.Value = trackR3.Value = main.param.cstR3;
			numericR4.Value = trackR4.Value = main.param.cstR4;
			numericV1.Value = trackV1.Value = main.param.cstV1;
			numericV2.Value = trackV2.Value = main.param.cstV2;
			numericV3.Value = trackV3.Value = main.param.cstV3;
			numericV4.Value = trackV4.Value = main.param.cstV4;
			numericB1.Value = trackB1.Value = main.param.cstB1;
			numericB2.Value = trackB2.Value = main.param.cstB2;
			numericB3.Value = trackB3.Value = main.param.cstB3;
			numericB4.Value = trackB4.Value = main.param.cstB4;
			rbDistanceSup.Checked = main.param.kMeansDist == 0;
			rbDistanceEuclide.Checked = main.param.kMeansDist == 1;
			rbDistanceManhattan.Checked = main.param.kMeansDist == 2;
			numColor.Value = main.param.kMeansColor >= 2 ? main.param.kMeansColor : 16;
			numColor.Minimum = 2;
			numPass.Value = main.param.kMeansPass >= 1 ? main.param.kMeansPass : 1;
			numPass.Minimum = 1;
			stopModif = false;
		}

		private void TrackLumR_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericLumR.Value = main.param.coefR = trackLumR.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackLumV_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericLumV.Value = main.param.coefV = trackLumV.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackLumB_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericLumB.Value = main.param.coefB = trackLumB.Value;
				main.Convert(false);
				stopModif = false;
			}

		}

		private void NumericLumR_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				trackLumR.Value = main.param.coefR = (int)numericLumR.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericLumV_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				trackLumV.Value = main.param.coefV = (int)numericLumV.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericLumB_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				trackLumB.Value = main.param.coefB = (int)numericLumB.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackR1_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericR1.Value = main.param.cstR1 = trackR1.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackR2_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericR2.Value = main.param.cstR2 = trackR2.Value;
				main.Convert(false);
				stopModif = false;
			}

		}

		private void TrackR3_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericR3.Value = main.param.cstR3 = trackR3.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackR4_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericR4.Value = main.param.cstR4 = trackR4.Value;
				main.Convert(false);
				stopModif = false;
			}

		}

		private void NumericR1_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstR1 = trackR1.Value = (int)numericR1.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericR2_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstR2 = trackR2.Value = (int)numericR2.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericR3_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstR3 = trackR3.Value = (int)numericR3.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericR4_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstR4 = trackR4.Value = (int)numericR4.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackV1_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericV1.Value = main.param.cstV1 = trackV1.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackV2_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericV2.Value = main.param.cstV2 = trackV2.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackV3_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericV3.Value = main.param.cstV3 = trackV3.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackV4_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericV4.Value = main.param.cstV4 = trackV4.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericV1_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstV1 = trackV1.Value = (int)numericV1.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericV2_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstV2 = trackV2.Value = (int)numericV2.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericV3_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstV3 = trackV3.Value = (int)numericV3.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericV4_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstV4 = trackV4.Value = (int)numericV4.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackB1_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericB1.Value = main.param.cstB1 = trackB1.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackB2_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericB2.Value = main.param.cstB2 = trackB2.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackB3_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericB3.Value = main.param.cstB3 = trackB3.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void TrackB4_Scroll(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				numericB4.Value = main.param.cstB4 = trackB4.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericB1_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstB1 = trackB1.Value = (int)numericB1.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericB2_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstB2 = trackB2.Value = (int)numericB2.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericB3_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstB3 = trackB3.Value = (int)numericB3.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumericB4_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				stopModif = true;
				main.param.cstB4 = trackB4.Value = (int)numericB4.Value;
				main.Convert(false);
				stopModif = false;
			}
		}

		private void NumColor_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				Enabled = false;
				stopModif = true;
				main.param.kMeansColor = (int)numColor.Value;
				main.Convert(false);
				stopModif = false;
				Enabled = true;
			}
		}

		private void RbDistanceSup_CheckedChanged(object sender, EventArgs e) {
			if (!stopModif) {
				Enabled = false;
				stopModif = true;
				main.param.kMeansDist = 0;
				main.Convert(false);
				stopModif = false;
				Enabled = true;
			}
		}

		private void RbDistanceEuclide_CheckedChanged(object sender, EventArgs e) {
			if (!stopModif) {
				Enabled = false;
				stopModif = true;
				main.param.kMeansDist = 1;
				main.Convert(false);
				stopModif = false;
				Enabled = true;
			}
		}

		private void RbDistanceManhattan_CheckedChanged(object sender, EventArgs e) {
			if (!stopModif) {
				Enabled = false;
				stopModif = true;
				main.param.kMeansDist = 2;
				main.Convert(false);
				stopModif = false;
				Enabled = true;
			}
		}

		private void NumPass_ValueChanged(object sender, EventArgs e) {
			if (!stopModif) {
				Enabled = false;
				stopModif = true;
				main.param.kMeansPass = (int)numPass.Value;
				main.Convert(false);
				stopModif = false;
				Enabled = true;
			}
		}
	}
}
