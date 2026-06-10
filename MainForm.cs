
using System;
using System.Drawing;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;
using AudioCompressor.Helpers;
using AudioCompressor.Models;
using AudioCompressor.Services;

namespace AudioCompressor
{
    public partial class MainForm : Form
    {
        private AudioMetadataService _metadataService;
        private AudioPlaybackService _playbackService;
        private AudioCompressorService _compressorService;

        private AudioFileInfo _currentAudioInfo;
        private CancellationTokenSource _decompressCts;

        private Panel dropPanel;
        private Label lblDragDropIcon, lblDragDropText, lblFileName;
        private RoundedButton btnSelectFile, btnPlay, btnStop, btnCompress, btnDecompress, btnCancel, btnReset;
        private GroupBox groupProperties, groupCompression, groupPerformance;
        private Label lblFileSizeValue, lblDurationValue, lblSampleRateValue, lblChannelsValue, lblBitRateValue, lblCodecValue;
        private Label lblSpeedValue, lblRatioValue, lblTimeRemainingValue;
        private ComboBox cmbAlgorithm;
        private NumericUpDown nudBitRate, nudSampleRate, nudBitDepth, nudChannels;
        private ProgressBar progressBar;
        private Label lblStatus, lblProgressPercent;

        public MainForm()
        {
            InitializeComponent(); 
            InitializeServices();
            InitializeCustomComponents();
            SetupDragDrop();
            DoubleBuffered = true;
        }

        private void InitializeServices()
        {
            _metadataService = new AudioMetadataService();
            _playbackService = new AudioPlaybackService();
            _playbackService.PlaybackStopped += (s, e) => UpdatePlayStopButtons(false);
            _compressorService = new AudioCompressorService();
        }

        private void InitializeCustomComponents()
        {
            this.Text = "Professional Audio Compressor";
            this.Size = new Size(1000, 720);
            this.StartPosition = FormStartPosition.CenterScreen;
            this.BackColor = Color.FromArgb(28, 28, 35);
            this.ForeColor = Color.White;
            this.FormBorderStyle = FormBorderStyle.FixedSingle;
            this.MaximizeBox = false;

            dropPanel = new Panel
            {
                Location = new Point(20, 20),
                Size = new Size(380, 200),
                BackColor = Color.FromArgb(40, 40, 48),
                Cursor = Cursors.Hand
            };
            dropPanel.Paint += (s, e) =>
            {
                var rect = new Rectangle(0, 0, dropPanel.Width - 1, dropPanel.Height - 1);
                using (var pen = new Pen(Color.FromArgb(100, 100, 130), 2) { DashStyle = System.Drawing.Drawing2D.DashStyle.Dash })
                    e.Graphics.DrawRectangle(pen, rect);
            };
            lblDragDropIcon = new Label
            {
                Text = "🎵",
                Font = new Font("Segoe UI", 36F),
                ForeColor = Color.FromArgb(180, 180, 210),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 40),
                Size = new Size(380, 60)
            };
            lblDragDropText = new Label
            {
                Text = "اسحب ملفًا صوتيًا إلى هنا\nMP3, WAV, M4A, FLAC",
                Font = new Font("Segoe UI", 10F),
                ForeColor = Color.FromArgb(160, 160, 180),
                TextAlign = ContentAlignment.MiddleCenter,
                Location = new Point(0, 110),
                Size = new Size(380, 50)
            };
            dropPanel.Controls.Add(lblDragDropIcon);
            dropPanel.Controls.Add(lblDragDropText);

            btnSelectFile = new RoundedButton
            {
                Text = "📁 اختر ملف صوتي",
                Location = new Point(20, 230),
                Size = new Size(380, 40),
                BackColor = Color.FromArgb(70, 70, 90),
                HoverColor = Color.FromArgb(90, 90, 110)
            };
            btnSelectFile.Click += BtnSelectFile_Click;

            lblFileName = new Label
            {
                Location = new Point(20, 280),
                Size = new Size(380, 25),
                Text = "لم يتم اختيار ملف",
                ForeColor = Color.LightGray,
                Font = new Font("Segoe UI", 9F, FontStyle.Italic),
                TextAlign = ContentAlignment.MiddleCenter
            };

            groupProperties = new GroupBox
            {
                Text = "خصائص الملف الصوتي",
                Location = new Point(20, 320),
                Size = new Size(380, 210),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            int y = 28;
            AddPropertyRow(groupProperties, "الحجم:", ref lblFileSizeValue, y);
            AddPropertyRow(groupProperties, "المدة:", ref lblDurationValue, y + 30);
            AddPropertyRow(groupProperties, "معدل العينات:", ref lblSampleRateValue, y + 60);
            AddPropertyRow(groupProperties, "القنوات:", ref lblChannelsValue, y + 90);
            AddPropertyRow(groupProperties, "معدل البت:", ref lblBitRateValue, y + 120);
            AddPropertyRow(groupProperties, "نوع الترميز:", ref lblCodecValue, y + 150);

            btnPlay = new RoundedButton
            {
                Text = "▶ تشغيل",
                Location = new Point(430, 20),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(0, 120, 210),
                HoverColor = Color.FromArgb(0, 140, 240)
            };
            btnPlay.Click += BtnPlay_Click;

            btnStop = new RoundedButton
            {
                Text = "⏹ إيقاف",
                Location = new Point(550, 20),
                Size = new Size(110, 40),
                BackColor = Color.FromArgb(140, 50, 50),
                HoverColor = Color.FromArgb(180, 60, 60)
            };
            btnStop.Click += BtnStop_Click;

            groupCompression = new GroupBox
            {
                Text = "إعدادات الضغط المتقدمة",
                Location = new Point(430, 75),
                Size = new Size(540, 180),
                ForeColor = Color.White
            };

            Label lblAlgorithm = new Label { Text = "خوارزمية الضغط:", Location = new Point(20, 30), Size = new Size(120, 25), ForeColor = Color.White };
            cmbAlgorithm = new ComboBox
            {
                Location = new Point(150, 28),
                Size = new Size(200, 28),
                DropDownStyle = ComboBoxStyle.DropDownList,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White
            };
            cmbAlgorithm.Items.AddRange(new[] { "MP3 (LAME)", "Mu-Law (Non-linear Quantization)", "DPCM (Differential PCM)", "ADPCM (Adaptive Delta Modulation)" });
            cmbAlgorithm.SelectedIndex = 0;
            cmbAlgorithm.SelectedIndexChanged += (s, e) => nudBitRate.Enabled = cmbAlgorithm.SelectedIndex == 0;
            groupCompression.Controls.Add(lblAlgorithm);
            groupCompression.Controls.Add(cmbAlgorithm);

            Label lblSampleRate = new Label { Text = "معدل العينات (Hz):", Location = new Point(20, 70), Size = new Size(120, 25), ForeColor = Color.White };
            nudSampleRate = new NumericUpDown
            {
                Location = new Point(150, 68),
                Size = new Size(100, 26),
                Minimum = 8000,
                Maximum = 192000,
                Value = 44100,
                Increment = 1000,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White
            };
            groupCompression.Controls.Add(lblSampleRate);
            groupCompression.Controls.Add(nudSampleRate);

            Label lblBitDepth = new Label { Text = "عمق البت (بت):", Location = new Point(270, 70), Size = new Size(100, 25), ForeColor = Color.White };
            nudBitDepth = new NumericUpDown
            {
                Location = new Point(380, 68),
                Size = new Size(70, 26),
                Minimum = 8,
                Maximum = 32,
                Value = 16,
                Increment = 8,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White
            };
            groupCompression.Controls.Add(lblBitDepth);
            groupCompression.Controls.Add(nudBitDepth);

            Label lblChannels = new Label { Text = "القنوات:", Location = new Point(20, 110), Size = new Size(80, 25), ForeColor = Color.White };
            nudChannels = new NumericUpDown
            {
                Location = new Point(150, 108),
                Size = new Size(60, 26),
                Minimum = 1,
                Maximum = 2,
                Value = 2,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White
            };
            groupCompression.Controls.Add(lblChannels);
            groupCompression.Controls.Add(nudChannels);

            Label lblBitRate = new Label { Text = "معدل البت (kbps):", Location = new Point(270, 110), Size = new Size(100, 25), ForeColor = Color.White };
            nudBitRate = new NumericUpDown
            {
                Location = new Point(380, 108),
                Size = new Size(80, 26),
                Minimum = 32,
                Maximum = 320,
                Value = 128,
                Increment = 32,
                BackColor = Color.FromArgb(50, 50, 70),
                ForeColor = Color.White
            };
            groupCompression.Controls.Add(lblBitRate);
            groupCompression.Controls.Add(nudBitRate);

            groupPerformance = new GroupBox
            {
                Text = "مراقبة الأداء (في الزمن الحقيقي)",
                Location = new Point(430, 270),
                Size = new Size(540, 150),
                ForeColor = Color.White
            };
            lblSpeedValue = new Label { Text = "0.00 MB/s", Location = new Point(200, 30), Size = new Size(150, 25), ForeColor = Color.Cyan };
            lblRatioValue = new Label { Text = "0%", Location = new Point(200, 60), Size = new Size(150, 25), ForeColor = Color.Lime };
            lblTimeRemainingValue = new Label { Text = "--", Location = new Point(200, 90), Size = new Size(150, 25), ForeColor = Color.Yellow };
            groupPerformance.Controls.Add(new Label { Text = "سرعة المعالجة:", Location = new Point(20, 30), Size = new Size(120, 25), ForeColor = Color.White });
            groupPerformance.Controls.Add(lblSpeedValue);
            groupPerformance.Controls.Add(new Label { Text = "نسبة الضغط الحالية:", Location = new Point(20, 60), Size = new Size(130, 25), ForeColor = Color.White });
            groupPerformance.Controls.Add(lblRatioValue);
            groupPerformance.Controls.Add(new Label { Text = "الوقت المتبقي:", Location = new Point(20, 90), Size = new Size(100, 25), ForeColor = Color.White });
            groupPerformance.Controls.Add(lblTimeRemainingValue);

            progressBar = new ProgressBar
            {
                Location = new Point(430, 440),
                Size = new Size(390, 25),
                Style = ProgressBarStyle.Blocks
            };
            lblProgressPercent = new Label
            {
                Text = "0%",
                Location = new Point(830, 440),
                Size = new Size(50, 25),
                ForeColor = Color.White,
                TextAlign = ContentAlignment.MiddleLeft
            };
            lblStatus = new Label
            {
                Text = "✓ جاهز",
                Location = new Point(430, 475),
                Size = new Size(540, 30),
                ForeColor = Color.LightGreen,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold),
                TextAlign = ContentAlignment.MiddleCenter
            };

            btnCompress = new RoundedButton
            {
                Text = "📦 ضغط الملف",
                Location = new Point(430, 520),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(0, 140, 70),
                HoverColor = Color.FromArgb(0, 170, 90)
            };
            btnCompress.Click += BtnCompress_Click;

            btnDecompress = new RoundedButton
            {
                Text = "🔓 فك الضغط إلى WAV",
                Location = new Point(650, 520),
                Size = new Size(200, 45),
                BackColor = Color.FromArgb(180, 120, 0),
                HoverColor = Color.FromArgb(210, 140, 0)
            };
            btnDecompress.Click += BtnDecompress_Click;

            btnCancel = new RoundedButton
            {
                Text = "❌ إلغاء",
                Location = new Point(860, 520),
                Size = new Size(110, 45),
                BackColor = Color.FromArgb(150, 50, 50),
                HoverColor = Color.FromArgb(180, 60, 60),
                Visible = false
            };
            btnCancel.Click += (s, e) =>
            {
                _compressorService.Cancel();
                _decompressCts?.Cancel();
            };

            btnReset = new RoundedButton
            {
                Text = "🔄 إعادة ضبط",
                Location = new Point(860, 520),
                Size = new Size(110, 45),
                BackColor = Color.FromArgb(70, 70, 90),
                HoverColor = Color.FromArgb(90, 90, 110),
                Visible = true
            };
            btnReset.Click += BtnReset_Click;

            this.Controls.AddRange(new Control[] {
                dropPanel, btnSelectFile, lblFileName, groupProperties,
                btnPlay, btnStop, groupCompression, groupPerformance,
                progressBar, lblProgressPercent, lblStatus,
                btnCompress, btnDecompress, btnCancel, btnReset
            });

            btnPlay.Enabled = false;
            btnStop.Enabled = false;
            btnCompress.Enabled = false;
            btnDecompress.Enabled = false;
        }

        private void AddPropertyRow(GroupBox group, string caption, ref Label valueLabel, int y)
        {
            Label cap = new Label
            {
                Text = caption,
                Location = new Point(15, y),
                Size = new Size(110, 22),
                ForeColor = Color.FromArgb(200, 200, 220)
            };
            valueLabel = new Label
            {
                Text = "—",
                Location = new Point(130, y),
                Size = new Size(230, 22),
                ForeColor = Color.White,
                Font = new Font("Segoe UI", 9F, FontStyle.Bold)
            };
            group.Controls.Add(cap);
            group.Controls.Add(valueLabel);
        }

        private void SetupDragDrop()
        {
            dropPanel.AllowDrop = true;
            this.AllowDrop = true;

            dropPanel.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            dropPanel.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0) LoadAudioFile(files[0]);
            };

            this.DragEnter += (s, e) =>
            {
                if (e.Data.GetDataPresent(DataFormats.FileDrop))
                    e.Effect = DragDropEffects.Copy;
            };
            this.DragDrop += (s, e) =>
            {
                string[] files = (string[])e.Data.GetData(DataFormats.FileDrop);
                if (files?.Length > 0) LoadAudioFile(files[0]);
            };
        }

        private void BtnSelectFile_Click(object sender, EventArgs e)
        {
            using (OpenFileDialog ofd = new OpenFileDialog())
            {
                ofd.Filter = "ملفات الصوت|*.mp3;*.wav;*.m4a;*.flac|كل الملفات|*.*";
                ofd.Title = "اختر ملفًا صوتيًا";
                if (ofd.ShowDialog() == DialogResult.OK)
                    LoadAudioFile(ofd.FileName);
            }
        }

        private async void LoadAudioFile(string filePath)
        {
            try
            {
                lblStatus.Text = "⏳ جاري تحميل الملف...";
                lblStatus.ForeColor = Color.Yellow;
                _currentAudioInfo = await Task.Run(() => _metadataService.GetMetadata(filePath));
                DisplayMetadata();
                _playbackService.Load(filePath);
                lblFileName.Text = _currentAudioInfo.FileName;
                lblStatus.Text = "✓ تم تحميل الملف بنجاح";
                lblStatus.ForeColor = Color.LightGreen;
                btnPlay.Enabled = true;
                btnCompress.Enabled = true;
                btnDecompress.Enabled = true;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في تحميل الملف:\n{ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "✗ فشل تحميل الملف";
                lblStatus.ForeColor = Color.Red;
                btnPlay.Enabled = false;
                btnCompress.Enabled = false;
                btnDecompress.Enabled = false;
            }
        }

        private void DisplayMetadata()
        {
            lblFileSizeValue.Text = _currentAudioInfo.FileSizeFormatted;
            lblDurationValue.Text = _currentAudioInfo.DurationFormatted;
            lblSampleRateValue.Text = $"{_currentAudioInfo.SampleRate:N0} Hz";
            lblChannelsValue.Text = _currentAudioInfo.Channels == 1 ? "أحادي (Mono)" : "ستيريو (Stereo)";
            lblBitRateValue.Text = $"{_currentAudioInfo.BitRate:N0} kbps";
            lblCodecValue.Text = _currentAudioInfo.Codec;
        }

        private void BtnPlay_Click(object sender, EventArgs e)
        {
            try
            {
                _playbackService.Play();
                UpdatePlayStopButtons(true);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"خطأ في التشغيل: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
        }

        private void BtnStop_Click(object sender, EventArgs e)
        {
            _playbackService.Stop();
            UpdatePlayStopButtons(false);
        }

        private void UpdatePlayStopButtons(bool isPlaying)
        {
            btnPlay.Enabled = !isPlaying;
            btnStop.Enabled = isPlaying;
        }

        private async void BtnCompress_Click(object sender, EventArgs e)
        {
            if (_currentAudioInfo == null)
            {
                MessageBox.Show("الرجاء تحميل ملف صوتي أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            var settings = new CompressionSettings
            {
                SampleRate = (int)nudSampleRate.Value,
                BitDepth = (int)nudBitDepth.Value,
                Channels = (int)nudChannels.Value,
                Algorithm = (CompressionAlgorithm)cmbAlgorithm.SelectedIndex,
                BitRate = (int)nudBitRate.Value
            };

            // جميع الخوارزميات تنتج ملفات MP3
            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "MP3 files|*.mp3";
                sfd.Title = $"حفظ الملف المضغوط - {cmbAlgorithm.Text}";
                sfd.FileName = Path.GetFileNameWithoutExtension(_currentAudioInfo.FileName) + "_compressed.mp3";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                await PerformCompression(sfd.FileName, settings);
            }
        }

        private async Task PerformCompression(string outputPath, CompressionSettings settings)
        {
            btnCompress.Enabled = false;
            btnDecompress.Enabled = false;
            btnReset.Enabled = false;
            btnCancel.Visible = true;
            btnReset.Visible = false;
            progressBar.Value = 0;
            lblProgressPercent.Text = "0%";
            lblSpeedValue.Text = "0.00 MB/s";
            lblRatioValue.Text = "0%";
            lblTimeRemainingValue.Text = "--";

            var progress = new Progress<AudioCompressor.Services.CompressionProgress>(p =>
            {
                if (!string.IsNullOrEmpty(p.ErrorMessage))
                {
                    lblStatus.Text = p.ErrorMessage;
                    lblStatus.ForeColor = Color.Yellow;
                }
                else
                {
                    progressBar.Value = (int)p.Percentage;
                    lblProgressPercent.Text = $"{p.Percentage:F1}%";
                    lblSpeedValue.Text = $"{p.SpeedMBPerSec:F2} MB/s";
                    lblRatioValue.Text = $"{p.CurrentCompressionRatio * 100:F1}%";
                    lblTimeRemainingValue.Text = p.EstimatedRemaining.ToString(@"hh\:mm\:ss");
                }
            });

            try
            {
                var result = await _compressorService.CompressAsync(_currentAudioInfo.FilePath, outputPath, settings, progress);
                if (result.Success)
                {
                    double savingPercentage = (1 - result.CompressionRatio) * 100;
                    string settingsDetails = $"الخوارزمية: {cmbAlgorithm.Text}\n" +
                                             $"معدل العينات (Sample Rate): {settings.SampleRate} Hz\n" +
                                             $"عمق البت (Bit Depth): {settings.BitDepth} bit\n" +
                                             $"القنوات: {(settings.Channels == 1 ? "Mono (أحادي)" : "Stereo (ستيريو)")}";
                    if (settings.Algorithm == CompressionAlgorithm.MP3)
                        settingsDetails += $"\nمعدل البت (Bit Rate): {settings.BitRate} kbps";

                    MessageBox.Show(
                        $"✅ اكتمل الضغط بنجاح!\n\n" +
                        $"📊 تقرير الضغط:\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"📁 حجم الملف قبل الضغط: {FormatFileSize(result.OriginalSize)}\n" +
                        $"📁 حجم الملف بعد الضغط: {FormatFileSize(result.CompressedSize)}\n" +
                        $"💾 نسبة التوفير في الحجم: {savingPercentage:F1}%\n" +
                        $"⏱️ الزمن المستغرق: {result.ProcessTime.TotalSeconds:F1} ثانية\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"⚙️ إعدادات الضغط المستخدمة:\n{settingsDetails}\n" +
                        $"━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━━\n" +
                        $"💾 تم حفظ الملف المضغوط في:\n{outputPath}",
                        "تقرير الضغط",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                    lblStatus.Text = "✓ اكتمل الضغط";
                    lblStatus.ForeColor = Color.LightGreen;
                }
                else
                {
                    MessageBox.Show($"فشل الضغط: {result.ErrorMessage}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "✗ فشل الضغط";
                    lblStatus.ForeColor = Color.Red;
                }
            }
            catch (OperationCanceledException)
            {
                lblStatus.Text = "⚠ تم إلغاء الضغط";
                lblStatus.ForeColor = Color.Yellow;
                MessageBox.Show("تم إلغاء عملية الضغط.", "ملغي", MessageBoxButtons.OK, MessageBoxIcon.Information);
            }
            catch (Exception ex)
            {
                //MessageBox.Show($"فشل الضغط: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                //lblStatus.Text = "✗ فشل الضغط";
                //lblStatus.ForeColor = Color.Red;
            }
            finally
            {
                btnCompress.Enabled = true;
                btnDecompress.Enabled = true;
                btnReset.Enabled = true;
                btnCancel.Visible = false;
                btnReset.Visible = true;
                progressBar.Value = 0;
                lblProgressPercent.Text = "0%";
            }
        }

        private async void BtnDecompress_Click(object sender, EventArgs e)
        {
            if (_currentAudioInfo == null)
            {
                MessageBox.Show("الرجاء تحميل ملف صوتي أولاً", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Warning);
                return;
            }

            using (SaveFileDialog sfd = new SaveFileDialog())
            {
                sfd.Filter = "WAV files|*.wav";
                sfd.Title = "حفظ الملف بعد فك الضغط";
                sfd.FileName = Path.GetFileNameWithoutExtension(_currentAudioInfo.FileName) + "_decompressed.wav";
                if (sfd.ShowDialog() != DialogResult.OK)
                    return;

                btnCompress.Enabled = false;
                btnDecompress.Enabled = false;
                btnReset.Enabled = false;
                btnCancel.Visible = true;
                btnReset.Visible = false;
                progressBar.Value = 0;
                lblProgressPercent.Text = "0%";
                lblSpeedValue.Text = "0.00 MB/s";
                lblRatioValue.Text = "فك ضغط";
                lblTimeRemainingValue.Text = "--";

                _decompressCts = new CancellationTokenSource();
                var token = _decompressCts.Token;

                var progress = new Progress<AudioCompressor.Services.CompressionProgress>(p =>
                {
                    progressBar.Value = (int)p.Percentage;
                    lblProgressPercent.Text = $"{p.Percentage:F1}%";
                    lblSpeedValue.Text = $"{p.SpeedMBPerSec:F2} MB/s";
                    lblRatioValue.Text = $"{p.CurrentCompressionRatio:F2}:1";
                });

                try
                {
                    var result = await _compressorService.DecompressAsync(_currentAudioInfo.FilePath, sfd.FileName, progress, token);
                    if (result.Success)
                    {
                        MessageBox.Show(
                            $"اكتمل فك الضغط بنجاح!\n" +
                            $"الملف المحفوظ: {sfd.FileName}\n" +
                            $"الحجم النهائي: {FormatFileSize(result.CompressedSize)}\n" +
                            $"الزمن المستغرق: {result.ProcessTime.TotalSeconds:F1} ثانية",
                            "نجاح",
                            MessageBoxButtons.OK,
                            MessageBoxIcon.Information);
                        lblStatus.Text = "✓ تم فك الضغط";
                        lblStatus.ForeColor = Color.LightGreen;
                    }
                    else
                    {
                        MessageBox.Show($"فشل فك الضغط: {result.ErrorMessage}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                        lblStatus.Text = "✗ فشل فك الضغط";
                        lblStatus.ForeColor = Color.Red;
                    }
                }
                catch (OperationCanceledException)
                {
                    lblStatus.Text = "⚠ تم إلغاء فك الضغط";
                    lblStatus.ForeColor = Color.Yellow;
                }
                catch (Exception ex)
                {
                    Console.WriteLine(ex.ToString());
                    MessageBox.Show($"فشل فك الضغط: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    lblStatus.Text = "✗ فشل فك الضغط";
                    lblStatus.ForeColor = Color.Red;
                }
                finally
                {
                    _decompressCts = null;
                    btnCompress.Enabled = true;
                    btnDecompress.Enabled = true;
                    btnReset.Enabled = true;
                    btnCancel.Visible = false;
                    btnReset.Visible = true;
                    progressBar.Value = 0;
                    lblProgressPercent.Text = "0%";
                }
            }
        }

        private void BtnReset_Click(object sender, EventArgs e)
        {
            if (_currentAudioInfo == null)
            {
                MessageBox.Show("لا يوجد ملف محمل لإعادة ضبط قيمه.", "تنبيه", MessageBoxButtons.OK, MessageBoxIcon.Information);
                return;
            }

            try
            {
                var freshInfo = _metadataService.GetMetadata(_currentAudioInfo.FilePath);
                _currentAudioInfo = freshInfo;
                DisplayMetadata();
                _playbackService.Load(_currentAudioInfo.FilePath);
                lblFileName.Text = _currentAudioInfo.FileName;
                lblStatus.Text = "✓ تم إعادة ضبط القيم الأصلية";
                lblStatus.ForeColor = Color.LightGreen;
                btnPlay.Enabled = true;
                btnCompress.Enabled = true;
                btnDecompress.Enabled = true;
                btnStop.Enabled = false;
                progressBar.Value = 0;
                lblProgressPercent.Text = "0%";
                lblSpeedValue.Text = "0.00 MB/s";
                lblRatioValue.Text = "0%";
                lblTimeRemainingValue.Text = "--";
                nudBitDepth.Value = 16;
                nudBitRate.Value = 128;
                nudChannels.Value = 2;
                nudSampleRate.Value = 44100;
                cmbAlgorithm.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"فشل إعادة الضبط: {ex.Message}", "خطأ", MessageBoxButtons.OK, MessageBoxIcon.Error);
                lblStatus.Text = "✗ فشل إعادة الضبط";
                lblStatus.ForeColor = Color.Red;
            }
        }

        private string FormatFileSize(long bytes)
        {
            string[] sizes = { "B", "KB", "MB", "GB" };
            double len = bytes;
            int order = 0;
            while (len >= 1024 && order < sizes.Length - 1)
            {
                order++;
                len /= 1024;
            }
            return $"{len:0.##} {sizes[order]}";
        }


    }
}









