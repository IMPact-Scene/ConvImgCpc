namespace ConvImgCpc {
	partial class EditSplit {
		/// <summary>
		/// Variable nécessaire au concepteur.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Nettoyage des ressources utilisées.
		/// </summary>
		/// <param name="disposing">true si les ressources managées doivent être supprimées ; sinon, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Code généré par le Concepteur Windows Form

		/// <summary>
		/// Méthode requise pour la prise en charge du concepteur - ne modifiez pas
		/// le contenu de cette méthode avec l'éditeur de code.
		/// </summary>
		private void InitializeComponent() {
			this.label1 = new System.Windows.Forms.Label();
			this.numLigne = new System.Windows.Forms.NumericUpDown();
			this.chkSplit0 = new System.Windows.Forms.CheckBox();
			this.chkSplit1 = new System.Windows.Forms.CheckBox();
			this.chkSplit2 = new System.Windows.Forms.CheckBox();
			this.chkSplit3 = new System.Windows.Forms.CheckBox();
			this.chkSplit4 = new System.Windows.Forms.CheckBox();
			this.chkSplit5 = new System.Windows.Forms.CheckBox();
			this.label2 = new System.Windows.Forms.Label();
			this.label3 = new System.Windows.Forms.Label();
			this.label4 = new System.Windows.Forms.Label();
			this.label5 = new System.Windows.Forms.Label();
			this.label6 = new System.Windows.Forms.Label();
			this.label7 = new System.Windows.Forms.Label();
			this.largSplit0 = new System.Windows.Forms.NumericUpDown();
			this.largSplit1 = new System.Windows.Forms.NumericUpDown();
			this.largSplit2 = new System.Windows.Forms.NumericUpDown();
			this.largSplit3 = new System.Windows.Forms.NumericUpDown();
			this.largSplit4 = new System.Windows.Forms.NumericUpDown();
			this.largSplit5 = new System.Windows.Forms.NumericUpDown();
			this.grpSplit = new System.Windows.Forms.GroupBox();
			this.modeCpc = new System.Windows.Forms.NumericUpDown();
			this.chkChangeMode = new System.Windows.Forms.CheckBox();
			this.label18 = new System.Windows.Forms.Label();
			this.label8 = new System.Windows.Forms.Label();
			this.label9 = new System.Windows.Forms.Label();
			this.label10 = new System.Windows.Forms.Label();
			this.label11 = new System.Windows.Forms.Label();
			this.label12 = new System.Windows.Forms.Label();
			this.label13 = new System.Windows.Forms.Label();
			this.lblColor6 = new System.Windows.Forms.Label();
			this.lblColor5 = new System.Windows.Forms.Label();
			this.lblColor4 = new System.Windows.Forms.Label();
			this.lblColor3 = new System.Windows.Forms.Label();
			this.lblColor2 = new System.Windows.Forms.Label();
			this.lblColor1 = new System.Windows.Forms.Label();
			this.label16 = new System.Windows.Forms.Label();
			this.lblColor0 = new System.Windows.Forms.Label();
			this.chkSplit6 = new System.Windows.Forms.CheckBox();
			this.largSplit6 = new System.Windows.Forms.NumericUpDown();
			this.numPen = new System.Windows.Forms.NumericUpDown();
			this.bpLoad = new System.Windows.Forms.Button();
			this.bpSave = new System.Windows.Forms.Button();
			this.label14 = new System.Windows.Forms.Label();
			this.retard = new System.Windows.Forms.NumericUpDown();
			this.label15 = new System.Windows.Forms.Label();
			this.lblInfo = new System.Windows.Forms.Label();
			this.bpCopieLigne = new System.Windows.Forms.Button();
			this.groupPal = new System.Windows.Forms.GroupBox();
			this.bpImportSplit = new System.Windows.Forms.Button();
			this.bpGenAsm = new System.Windows.Forms.Button();
			this.chkChgt = new System.Windows.Forms.CheckBox();
			this.hScrollZoom = new System.Windows.Forms.HScrollBar();
			this.pictureZoom = new System.Windows.Forms.PictureBox();
			this.pictureBox = new System.Windows.Forms.PictureBox();
			((System.ComponentModel.ISupportInitialize)(this.numLigne)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit0)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit1)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit2)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit3)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit4)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit5)).BeginInit();
			this.grpSplit.SuspendLayout();
			((System.ComponentModel.ISupportInitialize)(this.modeCpc)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit6)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.numPen)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.retard)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureZoom)).BeginInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).BeginInit();
			this.SuspendLayout();
			// 
			// label1
			// 
			this.label1.AutoSize = true;
			this.label1.Location = new System.Drawing.Point(22, 39);
			this.label1.Name = "label1";
			this.label1.Size = new System.Drawing.Size(33, 13);
			this.label1.TabIndex = 1;
			this.label1.Text = "Ligne";
			// 
			// numLigne
			// 
			this.numLigne.Location = new System.Drawing.Point(60, 36);
			this.numLigne.Maximum = new decimal(new int[] {
            261,
            0,
            0,
            0});
			this.numLigne.Name = "numLigne";
			this.numLigne.Size = new System.Drawing.Size(51, 20);
			this.numLigne.TabIndex = 2;
			this.numLigne.ValueChanged += new System.EventHandler(this.NumLigne_ValueChanged);
			// 
			// chkSplit0
			// 
			this.chkSplit0.AutoSize = true;
			this.chkSplit0.Location = new System.Drawing.Point(18, 24);
			this.chkSplit0.Name = "chkSplit0";
			this.chkSplit0.Size = new System.Drawing.Size(55, 17);
			this.chkSplit0.TabIndex = 3;
			this.chkSplit0.Text = "Split 1";
			this.chkSplit0.UseVisualStyleBackColor = true;
			this.chkSplit0.CheckedChanged += new System.EventHandler(this.ChkSplit1_CheckedChanged);
			// 
			// chkSplit1
			// 
			this.chkSplit1.AutoSize = true;
			this.chkSplit1.Location = new System.Drawing.Point(18, 103);
			this.chkSplit1.Name = "chkSplit1";
			this.chkSplit1.Size = new System.Drawing.Size(55, 17);
			this.chkSplit1.TabIndex = 3;
			this.chkSplit1.Text = "Split 2";
			this.chkSplit1.UseVisualStyleBackColor = true;
			this.chkSplit1.CheckedChanged += new System.EventHandler(this.ChkSplit2_CheckedChanged);
			// 
			// chkSplit2
			// 
			this.chkSplit2.AutoSize = true;
			this.chkSplit2.Location = new System.Drawing.Point(18, 182);
			this.chkSplit2.Name = "chkSplit2";
			this.chkSplit2.Size = new System.Drawing.Size(55, 17);
			this.chkSplit2.TabIndex = 3;
			this.chkSplit2.Text = "Split 3";
			this.chkSplit2.UseVisualStyleBackColor = true;
			this.chkSplit2.CheckedChanged += new System.EventHandler(this.ChkSplit3_CheckedChanged);
			// 
			// chkSplit3
			// 
			this.chkSplit3.AutoSize = true;
			this.chkSplit3.Location = new System.Drawing.Point(18, 261);
			this.chkSplit3.Name = "chkSplit3";
			this.chkSplit3.Size = new System.Drawing.Size(55, 17);
			this.chkSplit3.TabIndex = 3;
			this.chkSplit3.Text = "Split 4";
			this.chkSplit3.UseVisualStyleBackColor = true;
			this.chkSplit3.CheckedChanged += new System.EventHandler(this.ChkSplit4_CheckedChanged);
			// 
			// chkSplit4
			// 
			this.chkSplit4.AutoSize = true;
			this.chkSplit4.Location = new System.Drawing.Point(18, 340);
			this.chkSplit4.Name = "chkSplit4";
			this.chkSplit4.Size = new System.Drawing.Size(55, 17);
			this.chkSplit4.TabIndex = 3;
			this.chkSplit4.Text = "Split 5";
			this.chkSplit4.UseVisualStyleBackColor = true;
			this.chkSplit4.CheckedChanged += new System.EventHandler(this.ChkSplit5_CheckedChanged);
			// 
			// chkSplit5
			// 
			this.chkSplit5.AutoSize = true;
			this.chkSplit5.Location = new System.Drawing.Point(18, 419);
			this.chkSplit5.Name = "chkSplit5";
			this.chkSplit5.Size = new System.Drawing.Size(55, 17);
			this.chkSplit5.TabIndex = 3;
			this.chkSplit5.Text = "Split 6";
			this.chkSplit5.UseVisualStyleBackColor = true;
			this.chkSplit5.CheckedChanged += new System.EventHandler(this.ChkSplit6_CheckedChanged);
			// 
			// label2
			// 
			this.label2.AutoSize = true;
			this.label2.Location = new System.Drawing.Point(84, 26);
			this.label2.Name = "label2";
			this.label2.Size = new System.Drawing.Size(43, 13);
			this.label2.TabIndex = 4;
			this.label2.Text = "Largeur";
			// 
			// label3
			// 
			this.label3.AutoSize = true;
			this.label3.Location = new System.Drawing.Point(84, 105);
			this.label3.Name = "label3";
			this.label3.Size = new System.Drawing.Size(43, 13);
			this.label3.TabIndex = 4;
			this.label3.Text = "Largeur";
			// 
			// label4
			// 
			this.label4.AutoSize = true;
			this.label4.Location = new System.Drawing.Point(84, 184);
			this.label4.Name = "label4";
			this.label4.Size = new System.Drawing.Size(43, 13);
			this.label4.TabIndex = 4;
			this.label4.Text = "Largeur";
			// 
			// label5
			// 
			this.label5.AutoSize = true;
			this.label5.Location = new System.Drawing.Point(84, 263);
			this.label5.Name = "label5";
			this.label5.Size = new System.Drawing.Size(43, 13);
			this.label5.TabIndex = 4;
			this.label5.Text = "Largeur";
			// 
			// label6
			// 
			this.label6.AutoSize = true;
			this.label6.Location = new System.Drawing.Point(84, 342);
			this.label6.Name = "label6";
			this.label6.Size = new System.Drawing.Size(43, 13);
			this.label6.TabIndex = 4;
			this.label6.Text = "Largeur";
			// 
			// label7
			// 
			this.label7.AutoSize = true;
			this.label7.Location = new System.Drawing.Point(84, 421);
			this.label7.Name = "label7";
			this.label7.Size = new System.Drawing.Size(43, 13);
			this.label7.TabIndex = 4;
			this.label7.Text = "Largeur";
			// 
			// largSplit0
			// 
			this.largSplit0.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit0.Location = new System.Drawing.Point(140, 24);
			this.largSplit0.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit0.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit0.Name = "largSplit0";
			this.largSplit0.Size = new System.Drawing.Size(51, 20);
			this.largSplit0.TabIndex = 2;
			this.largSplit0.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit0.ValueChanged += new System.EventHandler(this.LargSplit1_ValueChanged);
			// 
			// largSplit1
			// 
			this.largSplit1.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit1.Location = new System.Drawing.Point(140, 103);
			this.largSplit1.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit1.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit1.Name = "largSplit1";
			this.largSplit1.Size = new System.Drawing.Size(51, 20);
			this.largSplit1.TabIndex = 2;
			this.largSplit1.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit1.ValueChanged += new System.EventHandler(this.LargSplit2_ValueChanged);
			// 
			// largSplit2
			// 
			this.largSplit2.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit2.Location = new System.Drawing.Point(140, 182);
			this.largSplit2.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit2.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit2.Name = "largSplit2";
			this.largSplit2.Size = new System.Drawing.Size(51, 20);
			this.largSplit2.TabIndex = 2;
			this.largSplit2.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit2.ValueChanged += new System.EventHandler(this.LargSplit3_ValueChanged);
			// 
			// largSplit3
			// 
			this.largSplit3.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit3.Location = new System.Drawing.Point(140, 261);
			this.largSplit3.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit3.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit3.Name = "largSplit3";
			this.largSplit3.Size = new System.Drawing.Size(51, 20);
			this.largSplit3.TabIndex = 2;
			this.largSplit3.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit3.ValueChanged += new System.EventHandler(this.LargSplit4_ValueChanged);
			// 
			// largSplit4
			// 
			this.largSplit4.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit4.Location = new System.Drawing.Point(140, 340);
			this.largSplit4.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit4.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit4.Name = "largSplit4";
			this.largSplit4.Size = new System.Drawing.Size(51, 20);
			this.largSplit4.TabIndex = 2;
			this.largSplit4.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit4.ValueChanged += new System.EventHandler(this.LargSplit5_ValueChanged);
			// 
			// largSplit5
			// 
			this.largSplit5.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit5.Location = new System.Drawing.Point(140, 419);
			this.largSplit5.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit5.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit5.Name = "largSplit5";
			this.largSplit5.Size = new System.Drawing.Size(51, 20);
			this.largSplit5.TabIndex = 2;
			this.largSplit5.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit5.ValueChanged += new System.EventHandler(this.LargSplit6_ValueChanged);
			// 
			// grpSplit
			// 
			this.grpSplit.Controls.Add(this.modeCpc);
			this.grpSplit.Controls.Add(this.chkChangeMode);
			this.grpSplit.Controls.Add(this.label18);
			this.grpSplit.Controls.Add(this.label8);
			this.grpSplit.Controls.Add(this.label9);
			this.grpSplit.Controls.Add(this.label10);
			this.grpSplit.Controls.Add(this.label11);
			this.grpSplit.Controls.Add(this.label12);
			this.grpSplit.Controls.Add(this.label13);
			this.grpSplit.Controls.Add(this.lblColor6);
			this.grpSplit.Controls.Add(this.lblColor5);
			this.grpSplit.Controls.Add(this.lblColor4);
			this.grpSplit.Controls.Add(this.lblColor3);
			this.grpSplit.Controls.Add(this.lblColor2);
			this.grpSplit.Controls.Add(this.lblColor1);
			this.grpSplit.Controls.Add(this.label16);
			this.grpSplit.Controls.Add(this.lblColor0);
			this.grpSplit.Controls.Add(this.label7);
			this.grpSplit.Controls.Add(this.label6);
			this.grpSplit.Controls.Add(this.label5);
			this.grpSplit.Controls.Add(this.label4);
			this.grpSplit.Controls.Add(this.label3);
			this.grpSplit.Controls.Add(this.chkSplit6);
			this.grpSplit.Controls.Add(this.label2);
			this.grpSplit.Controls.Add(this.chkSplit5);
			this.grpSplit.Controls.Add(this.chkSplit4);
			this.grpSplit.Controls.Add(this.chkSplit3);
			this.grpSplit.Controls.Add(this.chkSplit2);
			this.grpSplit.Controls.Add(this.chkSplit1);
			this.grpSplit.Controls.Add(this.largSplit6);
			this.grpSplit.Controls.Add(this.chkSplit0);
			this.grpSplit.Controls.Add(this.largSplit5);
			this.grpSplit.Controls.Add(this.largSplit4);
			this.grpSplit.Controls.Add(this.largSplit3);
			this.grpSplit.Controls.Add(this.largSplit2);
			this.grpSplit.Controls.Add(this.largSplit1);
			this.grpSplit.Controls.Add(this.largSplit0);
			this.grpSplit.Location = new System.Drawing.Point(12, 88);
			this.grpSplit.Name = "grpSplit";
			this.grpSplit.Size = new System.Drawing.Size(329, 647);
			this.grpSplit.TabIndex = 6;
			this.grpSplit.TabStop = false;
			this.grpSplit.Text = "Splits";
			// 
			// modeCpc
			// 
			this.modeCpc.Location = new System.Drawing.Point(256, 539);
			this.modeCpc.Maximum = new decimal(new int[] {
            2,
            0,
            0,
            0});
			this.modeCpc.Name = "modeCpc";
			this.modeCpc.Size = new System.Drawing.Size(53, 20);
			this.modeCpc.TabIndex = 13;
			this.modeCpc.Visible = false;
			this.modeCpc.ValueChanged += new System.EventHandler(this.ModeCpc_ValueChanged);
			// 
			// chkChangeMode
			// 
			this.chkChangeMode.AutoSize = true;
			this.chkChangeMode.Location = new System.Drawing.Point(18, 539);
			this.chkChangeMode.Name = "chkChangeMode";
			this.chkChangeMode.Size = new System.Drawing.Size(198, 17);
			this.chkChangeMode.TabIndex = 12;
			this.chkChangeMode.Text = "Changement de mode ligne suivante";
			this.chkChangeMode.UseVisualStyleBackColor = true;
			this.chkChangeMode.CheckedChanged += new System.EventHandler(this.ChkChangeMode_CheckedChanged);
			// 
			// label18
			// 
			this.label18.AutoSize = true;
			this.label18.Location = new System.Drawing.Point(209, 491);
			this.label18.Name = "label18";
			this.label18.Size = new System.Drawing.Size(43, 13);
			this.label18.TabIndex = 6;
			this.label18.Text = "Couleur";
			// 
			// label8
			// 
			this.label8.AutoSize = true;
			this.label8.Location = new System.Drawing.Point(209, 421);
			this.label8.Name = "label8";
			this.label8.Size = new System.Drawing.Size(43, 13);
			this.label8.TabIndex = 6;
			this.label8.Text = "Couleur";
			// 
			// label9
			// 
			this.label9.AutoSize = true;
			this.label9.Location = new System.Drawing.Point(209, 342);
			this.label9.Name = "label9";
			this.label9.Size = new System.Drawing.Size(43, 13);
			this.label9.TabIndex = 7;
			this.label9.Text = "Couleur";
			// 
			// label10
			// 
			this.label10.AutoSize = true;
			this.label10.Location = new System.Drawing.Point(209, 263);
			this.label10.Name = "label10";
			this.label10.Size = new System.Drawing.Size(43, 13);
			this.label10.TabIndex = 8;
			this.label10.Text = "Couleur";
			// 
			// label11
			// 
			this.label11.AutoSize = true;
			this.label11.Location = new System.Drawing.Point(209, 184);
			this.label11.Name = "label11";
			this.label11.Size = new System.Drawing.Size(43, 13);
			this.label11.TabIndex = 9;
			this.label11.Text = "Couleur";
			// 
			// label12
			// 
			this.label12.AutoSize = true;
			this.label12.Location = new System.Drawing.Point(209, 105);
			this.label12.Name = "label12";
			this.label12.Size = new System.Drawing.Size(43, 13);
			this.label12.TabIndex = 10;
			this.label12.Text = "Couleur";
			// 
			// label13
			// 
			this.label13.AutoSize = true;
			this.label13.Location = new System.Drawing.Point(209, 26);
			this.label13.Name = "label13";
			this.label13.Size = new System.Drawing.Size(43, 13);
			this.label13.TabIndex = 11;
			this.label13.Text = "Couleur";
			// 
			// lblColor6
			// 
			this.lblColor6.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor6.Location = new System.Drawing.Point(279, 474);
			this.lblColor6.Name = "lblColor6";
			this.lblColor6.Size = new System.Drawing.Size(35, 35);
			this.lblColor6.TabIndex = 5;
			this.lblColor6.Click += new System.EventHandler(this.LblColor6_Click);
			// 
			// lblColor5
			// 
			this.lblColor5.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor5.Location = new System.Drawing.Point(279, 404);
			this.lblColor5.Name = "lblColor5";
			this.lblColor5.Size = new System.Drawing.Size(35, 35);
			this.lblColor5.TabIndex = 5;
			this.lblColor5.Click += new System.EventHandler(this.LblColor5_Click);
			// 
			// lblColor4
			// 
			this.lblColor4.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor4.Location = new System.Drawing.Point(279, 326);
			this.lblColor4.Name = "lblColor4";
			this.lblColor4.Size = new System.Drawing.Size(35, 35);
			this.lblColor4.TabIndex = 5;
			this.lblColor4.Click += new System.EventHandler(this.LblColor4_Click);
			// 
			// lblColor3
			// 
			this.lblColor3.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor3.Location = new System.Drawing.Point(279, 248);
			this.lblColor3.Name = "lblColor3";
			this.lblColor3.Size = new System.Drawing.Size(35, 35);
			this.lblColor3.TabIndex = 5;
			this.lblColor3.Click += new System.EventHandler(this.LblColor3_Click);
			// 
			// lblColor2
			// 
			this.lblColor2.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor2.Location = new System.Drawing.Point(279, 170);
			this.lblColor2.Name = "lblColor2";
			this.lblColor2.Size = new System.Drawing.Size(35, 35);
			this.lblColor2.TabIndex = 5;
			this.lblColor2.Click += new System.EventHandler(this.LblColor2_Click);
			// 
			// lblColor1
			// 
			this.lblColor1.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor1.Location = new System.Drawing.Point(279, 92);
			this.lblColor1.Name = "lblColor1";
			this.lblColor1.Size = new System.Drawing.Size(35, 35);
			this.lblColor1.TabIndex = 5;
			this.lblColor1.Click += new System.EventHandler(this.LblColor1_Click);
			// 
			// label16
			// 
			this.label16.AutoSize = true;
			this.label16.Location = new System.Drawing.Point(84, 491);
			this.label16.Name = "label16";
			this.label16.Size = new System.Drawing.Size(43, 13);
			this.label16.TabIndex = 4;
			this.label16.Text = "Largeur";
			// 
			// lblColor0
			// 
			this.lblColor0.BorderStyle = System.Windows.Forms.BorderStyle.FixedSingle;
			this.lblColor0.Location = new System.Drawing.Point(279, 14);
			this.lblColor0.Name = "lblColor0";
			this.lblColor0.Size = new System.Drawing.Size(35, 35);
			this.lblColor0.TabIndex = 5;
			this.lblColor0.Click += new System.EventHandler(this.LblColor0_Click);
			// 
			// chkSplit6
			// 
			this.chkSplit6.AutoSize = true;
			this.chkSplit6.Location = new System.Drawing.Point(18, 489);
			this.chkSplit6.Name = "chkSplit6";
			this.chkSplit6.Size = new System.Drawing.Size(55, 17);
			this.chkSplit6.TabIndex = 3;
			this.chkSplit6.Text = "Split 7";
			this.chkSplit6.UseVisualStyleBackColor = true;
			this.chkSplit6.CheckedChanged += new System.EventHandler(this.ChkSplit7_CheckedChanged);
			// 
			// largSplit6
			// 
			this.largSplit6.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.largSplit6.Location = new System.Drawing.Point(140, 489);
			this.largSplit6.Maximum = new decimal(new int[] {
            384,
            0,
            0,
            0});
			this.largSplit6.Minimum = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit6.Name = "largSplit6";
			this.largSplit6.Size = new System.Drawing.Size(51, 20);
			this.largSplit6.TabIndex = 2;
			this.largSplit6.Value = new decimal(new int[] {
            32,
            0,
            0,
            0});
			this.largSplit6.ValueChanged += new System.EventHandler(this.LargSplit7_ValueChanged);
			// 
			// numPen
			// 
			this.numPen.Location = new System.Drawing.Point(270, 36);
			this.numPen.Maximum = new decimal(new int[] {
            15,
            0,
            0,
            0});
			this.numPen.Name = "numPen";
			this.numPen.Size = new System.Drawing.Size(51, 20);
			this.numPen.TabIndex = 2;
			this.numPen.ValueChanged += new System.EventHandler(this.NumPenMode_ValueChanged);
			// 
			// bpLoad
			// 
			this.bpLoad.Location = new System.Drawing.Point(12, 9);
			this.bpLoad.Name = "bpLoad";
			this.bpLoad.Size = new System.Drawing.Size(82, 23);
			this.bpLoad.TabIndex = 8;
			this.bpLoad.Text = "Lire Splits";
			this.bpLoad.UseVisualStyleBackColor = true;
			this.bpLoad.Click += new System.EventHandler(this.BpLoad_Click);
			// 
			// bpSave
			// 
			this.bpSave.Location = new System.Drawing.Point(110, 9);
			this.bpSave.Name = "bpSave";
			this.bpSave.Size = new System.Drawing.Size(82, 23);
			this.bpSave.TabIndex = 8;
			this.bpSave.Text = "Sauver Splits";
			this.bpSave.UseVisualStyleBackColor = true;
			this.bpSave.Click += new System.EventHandler(this.BpSave_Click);
			// 
			// label14
			// 
			this.label14.AutoSize = true;
			this.label14.Location = new System.Drawing.Point(15, 64);
			this.label14.Name = "label14";
			this.label14.Size = new System.Drawing.Size(39, 13);
			this.label14.TabIndex = 9;
			this.label14.Text = "Retard";
			// 
			// retard
			// 
			this.retard.Increment = new decimal(new int[] {
            8,
            0,
            0,
            0});
			this.retard.Location = new System.Drawing.Point(60, 62);
			this.retard.Maximum = new decimal(new int[] {
            36,
            0,
            0,
            0});
			this.retard.Minimum = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.retard.Name = "retard";
			this.retard.Size = new System.Drawing.Size(51, 20);
			this.retard.TabIndex = 2;
			this.retard.Value = new decimal(new int[] {
            4,
            0,
            0,
            0});
			this.retard.ValueChanged += new System.EventHandler(this.Retard_ValueChanged);
			// 
			// label15
			// 
			this.label15.AutoSize = true;
			this.label15.Location = new System.Drawing.Point(130, 39);
			this.label15.Name = "label15";
			this.label15.Size = new System.Drawing.Size(134, 13);
			this.label15.TabIndex = 11;
			this.label15.Text = "Numéro de stylo à changer";
			// 
			// lblInfo
			// 
			this.lblInfo.AutoSize = true;
			this.lblInfo.Location = new System.Drawing.Point(357, 609);
			this.lblInfo.Name = "lblInfo";
			this.lblInfo.Size = new System.Drawing.Size(16, 13);
			this.lblInfo.TabIndex = 13;
			this.lblInfo.Text = "...";
			// 
			// bpCopieLigne
			// 
			this.bpCopieLigne.Location = new System.Drawing.Point(349, 24);
			this.bpCopieLigne.Name = "bpCopieLigne";
			this.bpCopieLigne.Size = new System.Drawing.Size(132, 23);
			this.bpCopieLigne.TabIndex = 14;
			this.bpCopieLigne.Text = "Copier ligne précédente";
			this.bpCopieLigne.UseVisualStyleBackColor = true;
			this.bpCopieLigne.Click += new System.EventHandler(this.BpCopieLigne_Click);
			// 
			// groupPal
			// 
			this.groupPal.Location = new System.Drawing.Point(360, 628);
			this.groupPal.Name = "groupPal";
			this.groupPal.Size = new System.Drawing.Size(768, 57);
			this.groupPal.TabIndex = 16;
			this.groupPal.TabStop = false;
			this.groupPal.Text = "Palette initiale";
			// 
			// bpImportSplit
			// 
			this.bpImportSplit.Location = new System.Drawing.Point(752, 14);
			this.bpImportSplit.Name = "bpImportSplit";
			this.bpImportSplit.Size = new System.Drawing.Size(135, 23);
			this.bpImportSplit.TabIndex = 17;
			this.bpImportSplit.Text = "Importer image split";
			this.bpImportSplit.UseVisualStyleBackColor = true;
			this.bpImportSplit.Click += new System.EventHandler(this.BpImportSplit_Click);
			// 
			// bpGenAsm
			// 
			this.bpGenAsm.Location = new System.Drawing.Point(916, 14);
			this.bpGenAsm.Name = "bpGenAsm";
			this.bpGenAsm.Size = new System.Drawing.Size(135, 23);
			this.bpGenAsm.TabIndex = 18;
			this.bpGenAsm.Text = "Générer assembleur";
			this.bpGenAsm.UseVisualStyleBackColor = true;
			this.bpGenAsm.Click += new System.EventHandler(this.BpGenAsm_Click);
			// 
			// chkChgt
			// 
			this.chkChgt.AutoSize = true;
			this.chkChgt.Location = new System.Drawing.Point(151, 64);
			this.chkChgt.Name = "chkChgt";
			this.chkChgt.Size = new System.Drawing.Size(190, 17);
			this.chkChgt.TabIndex = 19;
			this.chkChgt.Text = "Afficher les lignes de changements";
			this.chkChgt.UseVisualStyleBackColor = true;
			this.chkChgt.CheckedChanged += new System.EventHandler(this.ChkChgt_CheckedChanged);
			// 
			// hScrollZoom
			// 
			this.hScrollZoom.Location = new System.Drawing.Point(360, 830);
			this.hScrollZoom.Maximum = 672;
			this.hScrollZoom.Name = "hScrollZoom";
			this.hScrollZoom.Size = new System.Drawing.Size(768, 18);
			this.hScrollZoom.TabIndex = 21;
			this.hScrollZoom.Scroll += new System.Windows.Forms.ScrollEventHandler(this.HScrollZoom_Scroll);
			// 
			// pictureZoom
			// 
			this.pictureZoom.Location = new System.Drawing.Point(360, 691);
			this.pictureZoom.Name = "pictureZoom";
			this.pictureZoom.Size = new System.Drawing.Size(768, 128);
			this.pictureZoom.TabIndex = 20;
			this.pictureZoom.TabStop = false;
			// 
			// pictureBox
			// 
			this.pictureBox.Location = new System.Drawing.Point(360, 62);
			this.pictureBox.Name = "pictureBox";
			this.pictureBox.Size = new System.Drawing.Size(768, 544);
			this.pictureBox.TabIndex = 0;
			this.pictureBox.TabStop = false;
			this.pictureBox.MouseClick += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseClick);
			this.pictureBox.MouseLeave += new System.EventHandler(this.PictureBox_MouseLeave);
			this.pictureBox.MouseMove += new System.Windows.Forms.MouseEventHandler(this.PictureBox_MouseMove);
			// 
			// EditSplit
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(1129, 856);
			this.Controls.Add(this.hScrollZoom);
			this.Controls.Add(this.pictureZoom);
			this.Controls.Add(this.chkChgt);
			this.Controls.Add(this.bpGenAsm);
			this.Controls.Add(this.bpImportSplit);
			this.Controls.Add(this.groupPal);
			this.Controls.Add(this.bpCopieLigne);
			this.Controls.Add(this.lblInfo);
			this.Controls.Add(this.label15);
			this.Controls.Add(this.label14);
			this.Controls.Add(this.bpSave);
			this.Controls.Add(this.bpLoad);
			this.Controls.Add(this.grpSplit);
			this.Controls.Add(this.numPen);
			this.Controls.Add(this.numLigne);
			this.Controls.Add(this.label1);
			this.Controls.Add(this.pictureBox);
			this.Controls.Add(this.retard);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedDialog;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "EditSplit";
			this.Text = "Split Editor";
			this.FormClosed += new System.Windows.Forms.FormClosedEventHandler(this.EditSplit_FormClosed);
			((System.ComponentModel.ISupportInitialize)(this.numLigne)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit0)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit1)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit2)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit3)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit4)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit5)).EndInit();
			this.grpSplit.ResumeLayout(false);
			this.grpSplit.PerformLayout();
			((System.ComponentModel.ISupportInitialize)(this.modeCpc)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.largSplit6)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.numPen)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.retard)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureZoom)).EndInit();
			((System.ComponentModel.ISupportInitialize)(this.pictureBox)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}
		private System.Windows.Forms.PictureBox pictureBox;

		#endregion
		private System.Windows.Forms.Label label1;
		private System.Windows.Forms.NumericUpDown numLigne;
		private System.Windows.Forms.CheckBox chkSplit0;
		private System.Windows.Forms.CheckBox chkSplit1;
		private System.Windows.Forms.CheckBox chkSplit2;
		private System.Windows.Forms.CheckBox chkSplit3;
		private System.Windows.Forms.CheckBox chkSplit4;
		private System.Windows.Forms.CheckBox chkSplit5;
		private System.Windows.Forms.Label label2;
		private System.Windows.Forms.Label label3;
		private System.Windows.Forms.Label label4;
		private System.Windows.Forms.Label label5;
		private System.Windows.Forms.Label label6;
		private System.Windows.Forms.Label label7;
		private System.Windows.Forms.NumericUpDown largSplit0;
		private System.Windows.Forms.NumericUpDown largSplit1;
		private System.Windows.Forms.NumericUpDown largSplit2;
		private System.Windows.Forms.NumericUpDown largSplit3;
		private System.Windows.Forms.NumericUpDown largSplit4;
		private System.Windows.Forms.NumericUpDown largSplit5;
		private System.Windows.Forms.GroupBox grpSplit;
		private System.Windows.Forms.Label lblColor0;
		private System.Windows.Forms.Label label8;
		private System.Windows.Forms.Label label9;
		private System.Windows.Forms.Label label10;
		private System.Windows.Forms.Label label11;
		private System.Windows.Forms.Label label12;
		private System.Windows.Forms.Label label13;
		private System.Windows.Forms.Label lblColor5;
		private System.Windows.Forms.Label lblColor4;
		private System.Windows.Forms.Label lblColor3;
		private System.Windows.Forms.Label lblColor2;
		private System.Windows.Forms.Label lblColor1;
		private System.Windows.Forms.NumericUpDown numPen;
		private System.Windows.Forms.Button bpLoad;
		private System.Windows.Forms.Button bpSave;
		private System.Windows.Forms.Label label14;
		private System.Windows.Forms.NumericUpDown retard;
		private System.Windows.Forms.CheckBox chkChangeMode;
		private System.Windows.Forms.Label label15;
		private System.Windows.Forms.NumericUpDown modeCpc;
		private System.Windows.Forms.Label lblInfo;
		private System.Windows.Forms.Button bpCopieLigne;
		private System.Windows.Forms.GroupBox groupPal;
		private System.Windows.Forms.Button bpImportSplit;
		private System.Windows.Forms.Button bpGenAsm;
		private System.Windows.Forms.CheckBox chkChgt;
		private System.Windows.Forms.PictureBox pictureZoom;
		private System.Windows.Forms.HScrollBar hScrollZoom;
		private System.Windows.Forms.Label label18;
		private System.Windows.Forms.Label lblColor6;
		private System.Windows.Forms.Label label16;
		private System.Windows.Forms.CheckBox chkSplit6;
		private System.Windows.Forms.NumericUpDown largSplit6;
	}
}

