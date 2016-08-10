namespace load_aircraft
{
    partial class Form1
    {
        /// <summary>
        /// Required designer variable.
        /// </summary>
        private System.ComponentModel.IContainer components = null;

        /// <summary>
        /// Clean up any resources being used.
        /// </summary>
        /// <param name="disposing">true if managed resources should be disposed; otherwise, false.</param>
        protected override void Dispose(bool disposing)
        {
            if (disposing && (components != null))
            {
                components.Dispose();
            }
            base.Dispose(disposing);
        }

        #region Windows Form Designer generated code

        /// <summary>
        /// Required method for Designer support - do not modify
        /// the contents of this method with the code editor.
        /// </summary>
        private void InitializeComponent()
        {
            this.buttonGenetic = new System.Windows.Forms.Button();
            this.buttonFuzzy = new System.Windows.Forms.Button();
            this.buttonGenerate = new System.Windows.Forms.Button();
            this.SuspendLayout();
            // 
            // buttonGenetic
            // 
            this.buttonGenetic.Location = new System.Drawing.Point(123, 12);
            this.buttonGenetic.Name = "buttonGenetic";
            this.buttonGenetic.Size = new System.Drawing.Size(105, 25);
            this.buttonGenetic.TabIndex = 1;
            this.buttonGenetic.Text = "Run Genetic";
            this.buttonGenetic.UseVisualStyleBackColor = true;
            this.buttonGenetic.Click += new System.EventHandler(this.buttonGenetic_Click);
            // 
            // buttonFuzzy
            // 
            this.buttonFuzzy.Location = new System.Drawing.Point(234, 12);
            this.buttonFuzzy.Name = "buttonFuzzy";
            this.buttonFuzzy.Size = new System.Drawing.Size(105, 25);
            this.buttonFuzzy.TabIndex = 1;
            this.buttonFuzzy.Text = "Run Fuzzy";
            this.buttonFuzzy.UseVisualStyleBackColor = true;
            this.buttonFuzzy.Click += new System.EventHandler(this.buttonFuzzy_Click);
            // 
            // buttonGenerate
            // 
            this.buttonGenerate.Location = new System.Drawing.Point(12, 12);
            this.buttonGenerate.Name = "buttonGenerate";
            this.buttonGenerate.Size = new System.Drawing.Size(105, 25);
            this.buttonGenerate.TabIndex = 1;
            this.buttonGenerate.Text = "Generate Data";
            this.buttonGenerate.UseVisualStyleBackColor = true;
            this.buttonGenerate.Click += new System.EventHandler(this.buttonGenerate_Click);
            // 
            // Form1
            // 
            this.AutoScaleDimensions = new System.Drawing.SizeF(6F, 13F);
            this.AutoScaleMode = System.Windows.Forms.AutoScaleMode.Font;
            this.ClientSize = new System.Drawing.Size(329, 641);
            this.Controls.Add(this.buttonGenerate);
            this.Controls.Add(this.buttonFuzzy);
            this.Controls.Add(this.buttonGenetic);
            this.Name = "Form1";
            this.Text = "Load Hold";
            this.Paint += new System.Windows.Forms.PaintEventHandler(this.Form1_Paint);
            this.ResumeLayout(false);

        }

        #endregion
        private System.Windows.Forms.Button buttonGenetic;
        private System.Windows.Forms.Button buttonFuzzy;
        private System.Windows.Forms.Button buttonGenerate;
    }
}

