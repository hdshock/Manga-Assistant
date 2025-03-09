using System;
using System.IO;
using System.IO.Compression;
using System.Windows.Forms;
using System.Threading.Tasks;
using System.Drawing;

namespace CoverInserterApp
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();
        }

        private void InitializeComponent()
        {
            this.lblCbzFile = new System.Windows.Forms.Label();
            this.txtCbzFile = new System.Windows.Forms.TextBox();
            this.btnBrowseCbz = new System.Windows.Forms.Button();
            this.lblCoverImage = new System.Windows.Forms.Label();
            this.txtCoverImage = new System.Windows.Forms.TextBox();
            this.btnBrowseCover = new System.Windows.Forms.Button();
            this.btnInsertCover = new System.Windows.Forms.Button();
            this.txtLog = new System.Windows.Forms.TextBox();
            this.cbBackup = new System.Windows.Forms.CheckBox();
            this.SuspendLayout();
            // 
            // lblCbzFile
            // 
            this.lblCbzFile.AutoSize = true;
            this.lblCbzFile.Location = new System.Drawing.Point(12, 15);
            this.lblCbzFile.Name = "lblCbzFile";
            this.lblCbzFile.Size = new System.Drawing.Size(50, 13);
            this.lblCbzFile.TabIndex = 0;
            this.lblCbzFile.Text = "CBZ File:";
            // 
            // txtCbzFile
            // 
            this.txtCbzFile.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCbzFile.Location = new System.Drawing.Point(92, 12);
            this.txtCbzFile.Name = "txtCbzFile";
            this.txtCbzFile.Size = new System.Drawing.Size(419, 20);
            this.txtCbzFile.TabIndex = 1;
            // 
            // btnBrowseCbz
            // 
            this.btnBrowseCbz.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseCbz.Location = new System.Drawing.Point(517, 10);
            this.btnBrowseCbz.Name = "btnBrowseCbz";
            this.btnBrowseCbz.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseCbz.TabIndex = 2;
            this.btnBrowseCbz.Text = "Browse...";
            this.btnBrowseCbz.UseVisualStyleBackColor = true;
            this.btnBrowseCbz.Click += new System.EventHandler(this.btnBrowseCbz_Click);
            // 
            // lblCoverImage
            // 
            this.lblCoverImage.AutoSize = true;
            this.lblCoverImage.Location = new System.Drawing.Point(12, 41);
            this.lblCoverImage.Name = "lblCoverImage";
            this.lblCoverImage.Size = new System.Drawing.Size(74, 13);
            this.lblCoverImage.TabIndex = 3;
            this.lblCoverImage.Text = "Cover Image:";
            // 
            // txtCoverImage
            // 
            this.txtCoverImage.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtCoverImage.Location = new System.Drawing.Point(92, 38);
            this.txtCoverImage.Name = "txtCoverImage";
            this.txtCoverImage.Size = new System.Drawing.Size(419, 20);
            this.txtCoverImage.TabIndex = 4;
            // 
            // btnBrowseCover
            // 
            this.btnBrowseCover.Anchor = ((System.Windows.Forms.AnchorStyles)((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Right)));
            this.btnBrowseCover.Location = new System.Drawing.Point(517, 36);
            this.btnBrowseCover.Name = "btnBrowseCover";
            this.btnBrowseCover.Size = new System.Drawing.Size(75, 23);
            this.btnBrowseCover.TabIndex = 5;
            this.btnBrowseCover.Text = "Browse...";
            this.btnBrowseCover.UseVisualStyleBackColor = true;
            this.btnBrowseCover.Click += new System.EventHandler(this.btnBrowseCover_Click);
            // 
            // btnInsertCover
            // 
            this.btnInsertCover.Anchor = ((System.Windows.Forms.AnchorStyles)(((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.btnInsertCover.Font = new System.Drawing.Font("Microsoft Sans Serif", 8.25F, System.Drawing.FontStyle.Bold, System.Drawing.GraphicsUnit.Point, ((byte)(0)));
            this.btnInsertCover.Location = new System.Drawing.Point(92, 64);
            this.btnInsertCover.Name = "btnInsertCover";
            this.btnInsertCover.Size = new System.Drawing.Size(419, 30);
            this.btnInsertCover.TabIndex = 6;
            this.btnInsertCover.Text = "Insert Cover Image";
            this.btnInsertCover.UseVisualStyleBackColor = true;
            this.btnInsertCover.Click += new System.EventHandler(this.btnInsertCover_Click);
            // 
            // txtLog
            // 
            this.txtLog.Anchor = ((System.Windows.Forms.AnchorStyles)((((System.Windows.Forms.AnchorStyles.Top | System.Windows.Forms.AnchorStyles.Bottom) 
            | System.Windows.Forms.AnchorStyles.Left) 
            | System.Windows.Forms.AnchorStyles.Right)));
            this.txtLog.Location = new System.Drawing.Point(12, 100);
            this.txtLog.Multiline = true;
            this.txtLog.Name = "txtLog";
            this.txtLog.ReadOnly = true;
            this.txtLog.ScrollBars = System.Windows.Forms.ScrollBars.Vertical;
            this.txtLog.Size = new System.Drawing.Size(580, 249);
            this.txtLog.TabIndex = 7;
            // 
            // cbBackup
            // 
            this.cbBackup.AutoSize = true;
            this.cbBackup.Checked = true;
            this.cbBackup.CheckState = System.Windows.Forms.CheckState.Checked;
            this.cbBackup.Location = new System.Drawing.Point(15, 71);
            this.cbBackup.Name = "cbBackup";
            this.cbBackup.Size = new System.Drawing.Size(63, 17);
            this.cbBackup.TabIndex = 8;
            this.cbBackup.Text = "Backup";
            this.cbBackup.UseVisualStyleBackColor = true;
            // 
            // MainForm
            // 
            this.ClientSize = new System.Drawing.Size(604, 361);
            this.Controls.Add(this.cbBackup);
            this.Controls.Add(this.txtLog);
            this.Controls.Add(this.btnInsertCover);
            this.Controls.Add(this.btnBrowseCover);
            this.Controls.Add(this.txtCoverImage);
            this.Controls.Add(this.lblCoverImage);
            this.Controls.Add(this.btnBrowseCbz);
            this.Controls.Add(this.txtCbzFile);
            this.Controls.Add(this.lblCbzFile);
            this.MinimumSize = new System.Drawing.Size(500, 400);
            this.Name = "MainForm";
            this.StartPosition = System.Windows.Forms.FormStartPosition.CenterScreen;
            this.Text = "CBZ Cover Image Inserter";
            this.ResumeLayout(false);
            this.PerformLayout();
        }

        private System.Windows.Forms.Label lblCbzFile;
        private System.Windows.Forms.TextBox txtCbzFile;
        private System.Windows.Forms.Button btnBrowseCbz;
        private System.Windows.Forms.Label lblCoverImage;
        private System.Windows.Forms.TextBox txtCoverImage;
        private System.Windows.Forms.Button btnBrowseCover;
        private System.Windows.Forms.Button btnInsertCover;
        private System.Windows.Forms.TextBox txtLog;
        private System.Windows.Forms.CheckBox cbBackup;

        private void btnBrowseCbz_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "CBZ Files (*.cbz)|*.cbz|All Files (*.*)|*.*";
                dialog.Title = "Select CBZ File";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtCbzFile.Text = dialog.FileName;
                    LogMessage($"Selected CBZ file: {dialog.FileName}");
                }
            }
        }

        private void btnBrowseCover_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog dialog = new OpenFileDialog())
            {
                dialog.Filter = "Image Files (*.jpg;*.jpeg;*.png)|*.jpg;*.jpeg;*.png|All Files (*.*)|*.*";
                dialog.Title = "Select Cover Image";
                
                if (dialog.ShowDialog() == DialogResult.OK)
                {
                    txtCoverImage.Text = dialog.FileName;
                    LogMessage($"Selected cover image: {dialog.FileName}");
                }
            }
        }

        private async void btnInsertCover_Click(object sender, EventArgs e)
        {
            try
            {
                // Validate inputs
                if (string.IsNullOrWhiteSpace(txtCbzFile.Text))
                {
                    MessageBox.Show("Please select a CBZ file.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (string.IsNullOrWhiteSpace(txtCoverImage.Text))
                {
                    MessageBox.Show("Please select a cover image.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(txtCbzFile.Text))
                {
                    MessageBox.Show("The selected CBZ file does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                if (!File.Exists(txtCoverImage.Text))
                {
                    MessageBox.Show("The selected cover image does not exist.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }

                // Disable UI during processing
                SetControlsEnabled(false);
                
                // Insert cover image
                await Task.Run(() => InsertCoverImage(txtCbzFile.Text, txtCoverImage.Text, cbBackup.Checked));
                
                // Re-enable UI
                SetControlsEnabled(true);
                
                MessageBox.Show("Cover image inserted successfully!", "Success", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                LogMessage($"ERROR: {ex.Message}");
                LogMessage($"Stack trace: {ex.StackTrace}");
                MessageBox.Show($"Error: {ex.Message}", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                
                // Re-enable UI
                SetControlsEnabled(true);
            }
        }

        private void SetControlsEnabled(bool enabled)
        {
            btnBrowseCbz.Enabled = enabled;
            btnBrowseCover.Enabled = enabled;
            btnInsertCover.Enabled = enabled;
            cbBackup.Enabled = enabled;
        }

        private void LogMessage(string message)
        {
            if (txtLog.InvokeRequired)
            {
                txtLog.Invoke(new Action(() => LogMessage(message)));
                return;
            }

            txtLog.AppendText(message + Environment.NewLine);
            txtLog.ScrollToCaret();
        }

        private void InsertCoverImage(string cbzFilePath, string coverImagePath, bool createBackup)
        {
            string tempFilePath = null;
            
            try
            {
                LogMessage($"Starting cover insertion for: {cbzFilePath}");
                
                // Create backup if requested
                if (createBackup)
                {
                    string backupPath = cbzFilePath + ".backup";
                    LogMessage($"Creating backup at: {backupPath}");
                    File.Copy(cbzFilePath, backupPath, true);
                }
                
                // Create a temporary file for the new archive
                tempFilePath = Path.Combine(
                    Path.GetDirectoryName(cbzFilePath),
                    $"temp_{Path.GetFileName(cbzFilePath)}"
                );
                
                LogMessage($"Created temporary file at: {tempFilePath}");

                // Read the cover image into memory
                byte[] coverImageBytes = File.ReadAllBytes(coverImagePath);
                LogMessage($"Read cover image: {coverImageBytes.Length} bytes");

                // Create new zip file with cover image first
                using (FileStream tempFileStream = File.Create(tempFilePath))
                using (ZipArchive tempArchive = new ZipArchive(tempFileStream, ZipArchiveMode.Create, true))
                {
                    // Add cover image as first entry
                    LogMessage("Adding cover.jpg as first entry");
                    ZipArchiveEntry coverEntry = tempArchive.CreateEntry("cover.jpg", CompressionLevel.Optimal);
                    using (Stream entryStream = coverEntry.Open())
                    {
                        entryStream.Write(coverImageBytes, 0, coverImageBytes.Length);
                    }

                    // Copy all entries from original archive
                    LogMessage($"Opening original archive: {cbzFilePath}");
                    using (FileStream originalFileStream = File.OpenRead(cbzFilePath))
                    using (ZipArchive originalArchive = new ZipArchive(originalFileStream, ZipArchiveMode.Read))
                    {
                        LogMessage($"Original archive has {originalArchive.Entries.Count} entries");
                        
                        foreach (ZipArchiveEntry entry in originalArchive.Entries)
                        {
                            // Skip if it's already a cover image
                            if (string.Equals(entry.Name, "cover.jpg", StringComparison.OrdinalIgnoreCase))
                            {
                                LogMessage($"Skipping existing cover image: {entry.FullName}");
                                continue;
                            }

                            LogMessage($"Copying entry: {entry.FullName}");
                            
                            // Create new entry in temp archive
                            ZipArchiveEntry newEntry = tempArchive.CreateEntry(entry.FullName, CompressionLevel.Optimal);
                            
                            // Copy content
                            using (Stream originalStream = entry.Open())
                            using (Stream newStream = newEntry.Open())
                            {
                                byte[] buffer = new byte[4096];
                                int bytesRead;
                                while ((bytesRead = originalStream.Read(buffer, 0, buffer.Length)) > 0)
                                {
                                    newStream.Write(buffer, 0, bytesRead);
                                }
                            }
                        }
                    }
                }

                // Replace original with temp file
                LogMessage("Replacing original file with new file");
                File.Delete(cbzFilePath);
                File.Move(tempFilePath, cbzFilePath);

                // Verify the cover was inserted
                LogMessage("Verifying cover image was inserted");
                using (FileStream fs = File.OpenRead(cbzFilePath))
                using (ZipArchive archive = new ZipArchive(fs, ZipArchiveMode.Read))
                {
                    LogMessage("Contents of updated archive:");
                    foreach (var entry in archive.Entries)
                    {
                        LogMessage($" - {entry.FullName}");
                    }

                    var coverEntry = archive.GetEntry("cover.jpg");
                    if (coverEntry != null)
                    {
                        LogMessage("SUCCESS: cover.jpg found in archive!");
                        
                        // Check if it's the first entry
                        if (archive.Entries[0].Name.Equals("cover.jpg", StringComparison.OrdinalIgnoreCase))
                        {
                            LogMessage("SUCCESS: cover.jpg is the first entry!");
                        }
                        else
                        {
                            LogMessage("WARNING: cover.jpg is not the first entry.");
                        }
                    }
                    else
                    {
                        LogMessage("ERROR: cover.jpg not found in archive!");
                    }
                }

                LogMessage("Operation completed successfully!");
            }
            catch (Exception ex)
            {
                LogMessage($"Error during cover insertion: {ex.Message}");
                
                // Clean up the temporary file if it exists
                if (tempFilePath != null && File.Exists(tempFilePath))
                {
                    try
                    {
                        File.Delete(tempFilePath);
                        LogMessage($"Deleted temporary file after error: {tempFilePath}");
                    }
                    catch (Exception cleanupEx)
                    {
                        LogMessage($"Error cleaning up temporary file: {cleanupEx.Message}");
                    }
                }
                
                throw;
            }
        }
    }
}
