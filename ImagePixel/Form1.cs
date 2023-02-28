using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImagePixel
{
    public partial class Form1 : Form
    {
        private Bitmap[] _bitmaps;
        private Random rnd = new Random(DateTime.Now.Millisecond);
        public Form1()
        {
            InitializeComponent();
            trackBar1.Enabled = false;
        }
        private void UpdateForm()
        {
            pictureBox1.Image = null;
            trackBar1.Value = 0;
            trackBar1.Enabled = false;
            Text = "0 %";
        }

        private async void openToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (openFileDialog1.ShowDialog() == DialogResult.OK)
            {
                Stopwatch sw = new Stopwatch();
                sw.Start();
                menuStrip1.Enabled = false;
                UpdateForm();
                int steps = trackBar1.Maximum;
                await Task.Run(() => { RunProcessing(new Bitmap(openFileDialog1.FileName), steps); });
                //(menuStrip1.Enabled, ) = (true, true);
                menuStrip1.Enabled = true;
                trackBar1.Enabled = true;
                sw.Stop();
                Text = $"Прошедшее время: {sw.Elapsed.ToString().Remove(8)}";
            }
        }


        private void RunProcessing(Bitmap bitmap, int steps)
        {
            int cnt = bitmap.Height * bitmap.Width;

            int[] indexes = new int[cnt];
            for (int i = 0; i < cnt; i++)
                indexes[i] = i;
            for (int i = 0; i < cnt - 1; i++)
            {
                int idx = rnd.Next(i + 1, cnt - 1);
                int t = indexes[idx];
                indexes[idx] = indexes[i];
                indexes[i] = t;
            }

            int pixelsInStep = cnt / steps;

            _bitmaps = new Bitmap[pixelsInStep];
            Bitmap currentBitmap = new Bitmap(bitmap.Width, bitmap.Height);
            int width = bitmap.Width;

            unsafe
            {
                for (int i = 1; i < steps; i++)
                {
                    BitmapData bitmapData_source =
                        bitmap.LockBits(new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadWrite, bitmap.PixelFormat);

                    BitmapData bitmapData_current = currentBitmap.LockBits(
                        new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height), ImageLockMode.ReadWrite,
                        currentBitmap.PixelFormat);
                    int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                    int heightInPixels = bitmapData_current.Height;
                    int widthInBytes = bitmapData_current.Width * bytesPerPixel;

                    byte* PtrFirstPixel_current = (byte*)bitmapData_current.Scan0;
                    byte* PtrFirstPixel_source = (byte*)bitmapData_source.Scan0;


                    Parallel.For(0, pixelsInStep - 1, new ParallelOptions() { MaxDegreeOfParallelism = 5 }, (j) =>
                    {
                        int idx = indexes[i * pixelsInStep + j];
                        int x1 = idx % width;
                        int y1 = idx / width;

                        byte* currentLIne_current = PtrFirstPixel_current + (y1 * bitmapData_current.Stride);
                        byte* currentLIne_source = PtrFirstPixel_source + (y1 * bitmapData_source.Stride);

                        currentLIne_current[x1 * bytesPerPixel] = currentLIne_source[x1 * bytesPerPixel];
                        currentLIne_current[x1 * bytesPerPixel + 1] = currentLIne_source[x1 * bytesPerPixel + 1];
                        currentLIne_current[x1 * bytesPerPixel + 2] = currentLIne_source[x1 * bytesPerPixel + 2];
                    });

                    currentBitmap.UnlockBits(bitmapData_current);
                    bitmap.UnlockBits(bitmapData_source);
                    _bitmaps[i - 1] = ((Bitmap)currentBitmap.Clone());
                    this.Invoke(new Action(() => { Text = $"{i} %"; }));
                    this.Invoke(new Action(() => { pictureBox1.Image = _bitmaps[trackBar1.Value++]; }));
                }
            }
            _bitmaps[steps - 1] = bitmap;
            this.Invoke(new Action(() => { pictureBox1.Image = _bitmaps[trackBar1.Value++]; }));
        }


        private void trackBar1_Scroll(object sender, EventArgs e) =>
                pictureBox1.Image = (trackBar1.Value == 0) ? null : _bitmaps[trackBar1.Value - 1];

        private void deleteImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Delete this image?", "Program", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                UpdateForm();
            }
        }
    }
}
