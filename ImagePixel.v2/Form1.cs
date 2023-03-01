using System.Diagnostics;
using System.Drawing.Imaging;
using ImagePixel.v2;

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
            this.FormClosing+=new FormClosingEventHandler(Form1_FormClosing);
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
                menuStrip1.Enabled = true;
                trackBar1.Enabled = true;
                sw.Stop();
                Text = $"Прошедшее время: {sw.Elapsed.ToString().Remove(8)}";
            }
        }

        private Queue<int> indxTask = new Queue<int>();

        private CancellationTokenSource _src;

        private BitmapImagesKeeper _images;
        private int _currentPointIndex;
        private int _endPointIndex;

        private Task RunProcessing(Bitmap bitmap, int steps)
        {
            _currentPointIndex = 0;
            _endPointIndex = 0;
            _images = new BitmapImagesKeeper(bitmap, this.Invoke, this.pictureBox1);
           
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
            int width = bitmap.Width;
            int previewIndex = 0;
            _images.LockSource();

            Task task = new Task(() => {                



                    while (!_src.IsCancellationRequested)
                    {
                        if(indxTask.Count > 0)
                        {
                            lock (this)
                            {
                                var enumerable = indxTask.Distinct();
                                if (Tendentious)
                                    _endPointIndex = enumerable.Max();
                                else
                                    _endPointIndex = enumerable.Min();
                                indxTask.Clear();
                            }

                            if( _endPointIndex >= _currentPointIndex)
                            {
                                for (int i = _currentPointIndex + 1; i <= _endPointIndex; i++ )
                                {
                                    if(i >= 100)
                                    {
                                        _images.ChangeCurrent100();
                                        continue;
                                    }

                                    _images.LockWorking();
                                    unsafe
                                    {


                                        int bytesPerPixel =
                                            System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;
                                        byte* PtrFirstPixel_current = (byte*)_images.WorkingBitmapData.Scan0;
                                        byte* PtrFirstPixel_source = (byte*)_images.SourceBitmapData.Scan0;

                                        Parallel.For(0, pixelsInStep, (j) =>
                                        {
                                            int idx = indexes[i * pixelsInStep + j];
                                            int x1 = idx % width;
                                            int y1 = idx / width;

                                            byte* currentLIne_current = PtrFirstPixel_current +
                                                                        (y1 * _images.WorkingBitmapData.Stride);
                                            byte* currentLIne_source = PtrFirstPixel_source +
                                                                       (y1 * _images.SourceBitmapData.Stride);

                                            currentLIne_current[x1 * bytesPerPixel] =
                                                currentLIne_source[x1 * bytesPerPixel];
                                            currentLIne_current[x1 * bytesPerPixel + 1] =
                                                currentLIne_source[x1 * bytesPerPixel + 1];
                                            currentLIne_current[x1 * bytesPerPixel + 2] =
                                                currentLIne_source[x1 * bytesPerPixel + 2];
                                        });
                                    }

                                    _images.ChangeCurrent();
                                    Interlocked.Increment(ref _currentPointIndex);
                                }                                
                            }
                            else
                            {
                                for (int i = _currentPointIndex - 1; i >= _endPointIndex; i--)
                                {
                                    _images.LockWorking();
                                    
                                    int bytesPerPixel = System.Drawing.Bitmap.GetPixelFormatSize(bitmap.PixelFormat) / 8;

                                    unsafe
                                    {
                                        byte* PtrFirstPixel_current = (byte*)_images.WorkingBitmapData.Scan0;

                                        Parallel.For(0, pixelsInStep, (j) =>
                                        {
                                            int idx = indexes[i * pixelsInStep + j];
                                            int x1 = idx % width;
                                            int y1 = idx / width;

                                            byte* currentLIne_current = PtrFirstPixel_current +
                                                                        (y1 * _images.WorkingBitmapData.Stride);

                                            currentLIne_current[x1 * bytesPerPixel] = 0;
                                            currentLIne_current[x1 * bytesPerPixel + 1] = 0;
                                            currentLIne_current[x1 * bytesPerPixel + 2] = 0;
                                        });
                                    }

                                    _images.ChangeCurrent();
                                    Interlocked.Decrement(ref _currentPointIndex);
                                }                                
                            }                           
                                               
                        }                        
                    }

            }, _src.Token);
            task.Start();
            // TODO UnlockSource
            Task trashDestroyer = new Task(async () =>
            {
                while (true) 
                { 
                    await Task.Delay(1000);
                    foreach(var b in _bitmaps)   
                        b.Dispose();
                }
            }, _src.Token);


            return task;
        }

        private int previewTrackBarValue;
        private int currentTrackBarValue;
        private bool Tendentious => currentTrackBarValue > previewTrackBarValue;
        private void trackBar1_Scroll(object sender, EventArgs e)
        {
            previewTrackBarValue = currentTrackBarValue;
            currentTrackBarValue = trackBar1.Value;
            this.Invoke(new Action(() => { Text = $"{currentTrackBarValue} %"; }));
            indxTask.Enqueue(currentTrackBarValue);
        }

        private void deleteImageToolStripMenuItem_Click(object sender, EventArgs e)
        {
            if (MessageBox.Show("Delete this image?", "Program", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                _src?.Cancel();
                UpdateForm();
            }
        }

        private void Form1_FormClosing(Object sender, FormClosingEventArgs e)
        {
            _src?.Cancel();
            _images.Reset();
            e.Cancel = true;
        }
    }
}
