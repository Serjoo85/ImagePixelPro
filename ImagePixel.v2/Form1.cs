using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace ImagePixel
{
    public partial class Form1 : Form
    {
        private Stack<Bitmap> _bitmaps = new Stack<Bitmap>();
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
                _src?.Cancel();
                Stopwatch sw = new Stopwatch();
                sw.Start();
                menuStrip1.Enabled = false;
                UpdateForm();
                int steps = trackBar1.Maximum;
                await Task.Run(() => { RunProcessing(new Bitmap(openFileDialog1.FileName), steps); }).ConfigureAwait(true);
                //(menuStrip1.Enabled, ) = (true, true);
                menuStrip1.Enabled = true;
                trackBar1.Enabled = true;
                sw.Stop();
                Text = $"Прошедшее время: {sw.Elapsed.ToString().Remove(8)}";
            }
        }

        private Queue<int> indxTask = new Queue<int>();

        private CancellationTokenSource _src;

        private Bitmap _workingBitmap;
        private Bitmap _currentBitmap;
        private Bitmap _sourceBitmap;
        private Bitmap _previewBitmap;

        private Task RunProcessing(Bitmap bitmap, int steps)
        {
            _sourceBitmap = bitmap;            
            _src?.Cancel();
            _src?.Dispose();
            _src = new CancellationTokenSource();
            int cnt = bitmap.Height * bitmap.Width;

            int[] indexes = new int[cnt];
            for (int i = 0; i < cnt; i++)
                indexes[i] = i;

            for (int i = 0; i < cnt - 1; i++)
            {
                int idx = rnd.Next(i + 1, cnt - 1);
                (indexes[idx], indexes[i]) = (indexes[i], indexes[idx]);
            }

            int pixelsInStep = cnt / steps; 

            _workingBitmap = new Bitmap(bitmap.Width, bitmap.Height);

            Task task = new Task(() => {                
                int width = bitmap.Width;
                int previewIndex = 0;
                int currentIndex = 0;
                unsafe
                {
                    BitmapData bitmapData_source = bitmap.LockBits(
                            new Rectangle(0, 0, bitmap.Width, bitmap.Height),
                            ImageLockMode.ReadWrite, bitmap.PixelFormat);


                    while (!_src.IsCancellationRequested)
                    {                        
                        if(indxTask.Count > 0)
                        {
                            previewIndex = currentIndex;
                            currentIndex = indxTask.Dequeue();

                            if(currentIndex == 100)
                            {
                                _previewBitmap = _currentBitmap;
                                _workingBitmap = (Bitmap)_sourceBitmap.Clone();
                                _currentBitmap = (Bitmap)_workingBitmap.Clone();
                                this.Invoke(new Action(() => { pictureBox1.Image = (Bitmap)_currentBitmap; }));
                                _previewBitmap?.Dispose();
                                continue;
                            }

                            if(currentIndex >= previewIndex)
                            {
                                for (int i = previewIndex + 1; i <= currentIndex; i++ )
                                {
                                    Debug.WriteLine(i);
                                    BitmapData bitmapData_current = _workingBitmap.LockBits(
                                     new Rectangle(0, 0, bitmap.Width, bitmap.Height), 
                                    ImageLockMode.ReadWrite,bitmap.PixelFormat);

                                    int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                                    byte* PtrFirstPixel_current = (byte*)bitmapData_current.Scan0;
                                    byte* PtrFirstPixel_source = (byte*)bitmapData_source.Scan0;

                                    Parallel.For(0, pixelsInStep, (j) =>
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

                                    _workingBitmap.UnlockBits(bitmapData_current);

                                    _previewBitmap = _currentBitmap;
                                    _currentBitmap = (Bitmap)_workingBitmap.Clone();
                                    this.Invoke(new Action(() => { pictureBox1.Image = _currentBitmap; }));
                                    _previewBitmap?.Dispose();
                                }                                
                            }
                            else
                            {
                                for (int i = previewIndex - 1; i >= currentIndex; i--)
                                {
                                    Debug.WriteLine(i);
                                    BitmapData bitmapData_current = _workingBitmap.LockBits(
                                    new Rectangle(0, 0, bitmap.Width, bitmap.Height), 
                                    ImageLockMode.ReadWrite,bitmap.PixelFormat);

                                    int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                                    byte* PtrFirstPixel_current = (byte*)bitmapData_current.Scan0;

                                    Parallel.For(0, pixelsInStep, (j) =>
                                    {
                                        int idx = indexes[i * pixelsInStep + j];
                                        int x1 = idx % width;
                                        int y1 = idx / width;

                                        byte* currentLIne_current = PtrFirstPixel_current + (y1 * bitmapData_current.Stride);

                                        currentLIne_current[x1 * bytesPerPixel] = 0;
                                        currentLIne_current[x1 * bytesPerPixel + 1] = 0; 
                                        currentLIne_current[x1 * bytesPerPixel + 2] = 0;
                                    });

                                    _workingBitmap.UnlockBits(bitmapData_current);

                                    _previewBitmap = _currentBitmap;
                                    _currentBitmap = (Bitmap)_workingBitmap.Clone();
                                    this.Invoke(new Action(() => { pictureBox1.Image = _currentBitmap; }));
                                    _previewBitmap?.Dispose();
                                }                                
                            }                           
                                               
                        }                        
                    }
                    bitmap.UnlockBits(bitmapData_source);
                }
            }, _src.Token);
            task.Start();

            Task trashDestroyer = new Task(async () =>
            {
                while (true) 
                { 
                    await Task.Delay(1000);
                    foreach(var b in _bitmaps)   
                        b.Dispose();
                }
            }, _src.Token);

            //trashDestroyer.Start();
            return task;
        }

        private void trackBar1_Scroll(object sender, EventArgs e)
        {            
            this.Invoke(new Action(() => { Text = $"{trackBar1.Value} %"; }));
            indxTask.Enqueue(trackBar1.Value);
        }
                

        private void deleteImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Delete this image?", "Program", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _src?.Cancel();
                UpdateForm();
            }
        }
    }
}
