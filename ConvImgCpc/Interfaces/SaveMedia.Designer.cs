namespace ConvImgCpc {
	partial class SaveMedia {
		/// <summary>
		/// Required designer variable.
		/// </summary>
		private System.ComponentModel.IContainer components = null;

		/// <summary>
		/// Clean up any resources being used.
		/// </summary>
		/// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
		protected override void Dispose(bool disposing) {
			if (disposing && (components != null)) {
				components.Dispose();
			}
			base.Dispose(disposing);
		}

		#region Windows Form Designer generated code

		/// <summary>
		/// Required method for Designer support - do not modify
		/// the contents of this method with the code editor.
		/// </summary>
		private void InitializeComponent() {
			this.txbLabelMedia = new System.Windows.Forms.TextBox();
			this.txbLabelPtr = new System.Windows.Forms.TextBox();
			this.chkZeroPtr = new System.Windows.Forms.CheckBox();
			this.bpOk = new System.Windows.Forms.Button();
			this.txbLabelPalette = new System.Windows.Forms.TextBox();
			this.chkLabelMedia = new System.Windows.Forms.CheckBox();
			this.chkLabelPalette = new System.Windows.Forms.CheckBox();
			this.chkLabelPtr = new System.Windows.Forms.CheckBox();
			this.numNbMedia = new System.Windows.Forms.NumericUpDown();
			this.lblNbMedia = new System.Windows.Forms.Label();
			((System.ComponentModel.ISupportInitialize)(this.numNbMedia)).BeginInit();
			this.SuspendLayout();
			// 
			// txbLabelMedia
			// 
			this.txbLabelMedia.Location = new System.Drawing.Point(163, 20);
			this.txbLabelMedia.Name = "txbLabelMedia";
			this.txbLabelMedia.Size = new System.Drawing.Size(324, 20);
			this.txbLabelMedia.TabIndex = 2;
			// 
			// txbLabelPtr
			// 
			this.txbLabelPtr.Location = new System.Drawing.Point(163, 73);
			this.txbLabelPtr.Name = "txbLabelPtr";
			this.txbLabelPtr.Size = new System.Drawing.Size(324, 20);
			this.txbLabelPtr.TabIndex = 3;
			// 
			// chkZeroPtr
			// 
			this.chkZeroPtr.AutoSize = true;
			this.chkZeroPtr.Location = new System.Drawing.Point(18, 145);
			this.chkZeroPtr.Name = "chkZeroPtr";
			this.chkZeroPtr.Size = new System.Drawing.Size(172, 17);
			this.chkZeroPtr.TabIndex = 4;
			this.chkZeroPtr.Text = "Add 0 in the end onf pointer list";
			this.chkZeroPtr.UseVisualStyleBackColor = true;
			// 
			// bpOk
			// 
			this.bpOk.Location = new System.Drawing.Point(409, 141);
			this.bpOk.Name = "bpOk";
			this.bpOk.Size = new System.Drawing.Size(75, 23);
			this.bpOk.TabIndex = 5;
			this.bpOk.Text = "Ok";
			this.bpOk.UseVisualStyleBackColor = true;
			this.bpOk.Click += new System.EventHandler(this.BpOk_Click);
			// 
			// txbLabelPalette
			// 
			this.txbLabelPalette.Location = new System.Drawing.Point(163, 46);
			this.txbLabelPalette.Name = "txbLabelPalette";
			this.txbLabelPalette.Size = new System.Drawing.Size(324, 20);
			this.txbLabelPalette.TabIndex = 2;
			// 
			// chkLabelMedia
			// 
			this.chkLabelMedia.AutoSize = true;
			this.chkLabelMedia.Checked = true;
			this.chkLabelMedia.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkLabelMedia.Location = new System.Drawing.Point(18, 23);
			this.chkLabelMedia.Name = "chkLabelMedia";
			this.chkLabelMedia.Size = new System.Drawing.Size(15, 14);
			this.chkLabelMedia.TabIndex = 6;
			this.chkLabelMedia.UseVisualStyleBackColor = true;
			this.chkLabelMedia.CheckedChanged += new System.EventHandler(this.ChkLabelMedia_CheckedChanged);
			// 
			// chkLabelPalette
			// 
			this.chkLabelPalette.AutoSize = true;
			this.chkLabelPalette.Checked = true;
			this.chkLabelPalette.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkLabelPalette.Location = new System.Drawing.Point(18, 49);
			this.chkLabelPalette.Name = "chkLabelPalette";
			this.chkLabelPalette.Size = new System.Drawing.Size(15, 14);
			this.chkLabelPalette.TabIndex = 6;
			this.chkLabelPalette.UseVisualStyleBackColor = true;
			this.chkLabelPalette.CheckedChanged += new System.EventHandler(this.ChkLabelPalette_CheckedChanged);
			// 
			// chkLabelPtr
			// 
			this.chkLabelPtr.AutoSize = true;
			this.chkLabelPtr.Checked = true;
			this.chkLabelPtr.CheckState = System.Windows.Forms.CheckState.Checked;
			this.chkLabelPtr.Location = new System.Drawing.Point(18, 75);
			this.chkLabelPtr.Name = "chkLabelPtr";
			this.chkLabelPtr.Size = new System.Drawing.Size(15, 14);
			this.chkLabelPtr.TabIndex = 6;
			this.chkLabelPtr.UseVisualStyleBackColor = true;
			this.chkLabelPtr.CheckedChanged += new System.EventHandler(this.ChkLabelPtr_CheckedChanged);
			// 
			// numNbMedia
			// 
			this.numNbMedia.Location = new System.Drawing.Point(163, 99);
			this.numNbMedia.Maximum = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.numNbMedia.Minimum = new decimal(new int[] {
            1,
            0,
            0,
            0});
			this.numNbMedia.Name = "numNbMedia";
			this.numNbMedia.Size = new System.Drawing.Size(36, 20);
			this.numNbMedia.TabIndex = 7;
			this.numNbMedia.Value = new decimal(new int[] {
            16,
            0,
            0,
            0});
			this.numNbMedia.Visible = false;
			// 
			// lblNbMedia
			// 
			this.lblNbMedia.AutoSize = true;
			this.lblNbMedia.Location = new System.Drawing.Point(15, 101);
			this.lblNbMedia.Name = "lblNbMedia";
			this.lblNbMedia.Size = new System.Drawing.Size(127, 13);
			this.lblNbMedia.TabIndex = 8;
			this.lblNbMedia.Text = "Number of sprites to save";
			this.lblNbMedia.Visible = false;
			// 
			// SaveMedia
			// 
			this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
			this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
			this.ClientSize = new System.Drawing.Size(496, 171);
			this.Controls.Add(this.lblNbMedia);
			this.Controls.Add(this.numNbMedia);
			this.Controls.Add(this.chkLabelPtr);
			this.Controls.Add(this.chkLabelPalette);
			this.Controls.Add(this.chkLabelMedia);
			this.Controls.Add(this.bpOk);
			this.Controls.Add(this.chkZeroPtr);
			this.Controls.Add(this.txbLabelPtr);
			this.Controls.Add(this.txbLabelPalette);
			this.Controls.Add(this.txbLabelMedia);
			this.FormBorderStyle = System.Windows.Forms.FormBorderStyle.FixedToolWindow;
			this.MaximizeBox = false;
			this.MinimizeBox = false;
			this.Name = "SaveMedia";
			((System.ComponentModel.ISupportInitialize)(this.numNbMedia)).EndInit();
			this.ResumeLayout(false);
			this.PerformLayout();

		}

		#endregion
		private System.Windows.Forms.TextBox txbLabelMedia;
		private System.Windows.Forms.TextBox txbLabelPtr;
		private System.Windows.Forms.CheckBox chkZeroPtr;
		private System.Windows.Forms.Button bpOk;
		private System.Windows.Forms.TextBox txbLabelPalette;
		private System.Windows.Forms.CheckBox chkLabelMedia;
		private System.Windows.Forms.CheckBox chkLabelPalette;
		private System.Windows.Forms.CheckBox chkLabelPtr;
		private System.Windows.Forms.NumericUpDown numNbMedia;
		private System.Windows.Forms.Label lblNbMedia;
	}
}