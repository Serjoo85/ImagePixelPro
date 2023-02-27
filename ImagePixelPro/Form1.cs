using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImagePixel
{
    public partial class Form1 : Form
    {
        private List<Bitmap> _bitmaps = new List<Bitmap>();
        private Random rnd = new Random(DateTime.Now.Millisecond);
        public Form1()
        {
            InitializeComponent();
            trackBar1.Enabled = false;
        }
        private void UpdateForm()
        {
            _bitmaps.Clear();
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
                await Task.Run(() => { RunProcessing(new Bitmap(openFileDialog1.FileName)); });
                (menuStrip1.Enabled, trackBar1.Enabled) = (true, true);
                sw.Stop();
                Text = $"Прошедшее время: {sw.Elapsed.ToString().Remove(8)}";
            }
        }
        private async void RunProcessing(Bitmap bitmap)
        {
            int pixelsInStep = (bitmap.Height * bitmap.Width) / 100;
            
            //BlockingCollection<Pixel> pixels = new BlockingCollection<Pixel>(new ConcurrentQueue<Pixel>(pixelss), pixelss.Count);
            int max = trackBar1.Maximum;
            List<Pixel> pixels = GetPixels(bitmap);
            Pixel[] currentPixelsSet = new Pixel[pixels.Count - pixelsInStep];
            int x = 0;

            Parallel.For(1, max, new ParallelOptions{MaxDegreeOfParallelism = 5}, (i) =>
            {
                int threadNumber = Interlocked.Increment(ref x);
                Bitmap currentBitmap;
                
                for (int j = 0 + pixelsInStep * (threadNumber - 1); j < pixelsInStep * threadNumber ; j++)
                {
                    currentPixelsSet[j] = pixels[j];
                }

                currentBitmap = new Bitmap(bitmap.Width, bitmap.Height);

                //foreach (Pixel pixel in currentPixelsSet)
                //    lock(this)
                //        currentBitmap.SetPixel(pixel.Point.X, pixel.Point.Y, pixel.Color);

                //lock (this)
                //    _bitmaps.Add(currentBitmap);
                
                //this.Invoke(new Action(() => { Text = $"{i} %"; }));
                //this.Invoke(new Action(() => { pictureBox1.Image = _bitmaps[trackBar1.Value++]; }));

            });

            _bitmaps.Add(bitmap);
            this.Invoke(new Action(() => { pictureBox1.Image = _bitmaps[trackBar1.Value++]; }));
        }

        private List<Pixel> GetPixels(Bitmap bitmap)
        {
            List<Pixel> pixels = new List<Pixel>(bitmap.Width * bitmap.Height);

            for (int y = 0; y < bitmap.Height; y++)
                for (int x = 0; x < bitmap.Width; x++)
                    pixels.Add(new Pixel(new Point(x, y), bitmap.GetPixel(x, y)));

            return pixels;
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

        private void pictureBox1_Click(object sender, EventArgs e)
        {

        }
    }
}
