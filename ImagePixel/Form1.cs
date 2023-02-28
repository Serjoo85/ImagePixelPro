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
            for (int i=0;i<cnt-1;i++)
            {
                int idx = rnd.Next(i + 1, cnt - 1);
                int t = indexes[idx];                
                indexes[idx] = indexes[i];
                indexes[i] = t;
            }

            int pixelsInStep = cnt / steps;
       
            
            Bitmap currentBitmap = new Bitmap(bitmap.Width, bitmap.Height);

            int width = bitmap.Width;
           
            for (int i = 1; i < steps; i++)
            {
                unsafe
                {
                    BitmapData bitmapData = currentBitmap.LockBits(new Rectangle(0, 0, currentBitmap.Width, currentBitmap.Height), ImageLockMode.ReadWrite, currentBitmap.PixelFormat);
                    int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat)/8;
                    int heightInPixels = bitmapData.Height;
                    int widthInBytes = bitmapData.Width * bytesPerPixel;
                    Debug.WriteLine(bytesPerPixel);

                    byte* PtrFirstPixel = (byte*)bitmapData.Scan0;

                    

                    Parallel.For(0, heightInPixels, y =>
                    {
                        //byte* currentLIne = PtrFirstPixel + (y * bitmapData.Stride);

                        byte[] b = new byte[bytesPerPixel];

                        for (int j = 0; j < pixelsInStep; j++)
                        {
                            int idx = indexes[i* pixelsInStep + j];
                            int x1 = idx % width;
                            int y1 = idx / width;
                            
                            byte* currentLIne = PtrFirstPixel + (y1 * bitmapData.Stride);
                            
                            b[0] = currentLIne[x1 * bytesPerPixel];
                            b[1] = currentLIne[x1 * bytesPerPixel + 1];
                            b[2] = currentLIne[x1 * bytesPerPixel + 2];

                            //currentBitmap.SetPixel(x, y, bitmap.GetPixel(x, y));
                        }

                        //for(int x = 0; x < widthInBytes; x = x + bytesPerPixel)
                        //{
                        //
                        //}

                        //for (int j = 0; j < pixelsInStep; j++)
                        //{
                        //    int idx = indexes[i* pixelsInStep + j];
                        //    int x = idx % bitmap.Width;
                        //    int y = idx / bitmap.Width;
                        //
                        //    currentBitmap.SetPixel(x, y, bitmap.GetPixel(x, y));                
                        //}
                        //
                        //
                        //_bitmaps.Add((Bitmap)currentBitmap.Clone());
                        //
                        //this.Invoke(new Action(() => { Text = $"{i} %"; }));
                        //this.Invoke(new Action(() => { 
                        //    pictureBox1.Image = _bitmaps[trackBar1.Value++];     
                        //}));                    
                    });

                    bitmap.UnlockBits(bitmapData);
                }

                _bitmaps.Add(bitmap);
                this.Invoke(new Action(() => { pictureBox1.Image = _bitmaps[trackBar1.Value++]; }));
            }
            
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
