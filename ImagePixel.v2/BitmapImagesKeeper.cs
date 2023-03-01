using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImagePixel.v2
{
    public class BitmapImagesKeeper
    {
        public Bitmap WorkingBitmap { get; set; }
        public Bitmap CurrentBitmap { get; set; }
        public Bitmap SourceBitmap { get; set; }
        public Bitmap PreviewBitmap { get; set; }

        public Bitmap EmptyBitmap { get; set; }

        public BitmapData WorkingBitmapData => _workingBitmapData;
        public BitmapData SourceBitmapData => _sourceBitmapData;

        private Action<Action> _sendImageToForm;

        private PictureBox _pictureBox;
        private BitmapData _sourceBitmapData;
        private BitmapData _workingBitmapData;

        public BitmapImagesKeeper(Bitmap sourceBitmap, Action<Action> sendImageToForm, System.Windows.Forms.PictureBox pictureBox)
        {
            SourceBitmap = sourceBitmap;
            WorkingBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            EmptyBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            _sendImageToForm = sendImageToForm;
            _pictureBox = pictureBox;
        }

        public void ChangeCurrent()
        {
            PreviewBitmap = CurrentBitmap;
            CurrentBitmap = (Bitmap)WorkingBitmap.Clone();
            WorkingBitmap.UnlockBits(WorkingBitmapData);
            _sendImageToForm.Invoke(() => { _pictureBox.Image = CurrentBitmap; });
            PreviewBitmap?.Dispose();
        }

        public void ChangeCurrent100()
        {
            PreviewBitmap = CurrentBitmap;
            WorkingBitmap = (Bitmap)SourceBitmap.Clone();
            CurrentBitmap = (Bitmap)WorkingBitmap.Clone();
            _sendImageToForm.Invoke(new Action(() => { _pictureBox.Image = (Bitmap)CurrentBitmap; }));
            PreviewBitmap?.Dispose();
        }

        public void ChangeCurrent0()
        {
            PreviewBitmap = CurrentBitmap;
            WorkingBitmap = (Bitmap)EmptyBitmap.Clone();
            CurrentBitmap = (Bitmap)WorkingBitmap.Clone();
            _sendImageToForm.Invoke(new Action(() => { _pictureBox.Image = (Bitmap)CurrentBitmap; }));
            PreviewBitmap?.Dispose();
        }

        public BitmapData LockWorking()
        {
            return _workingBitmapData = WorkingBitmap.LockBits(
                new Rectangle(0, 0, SourceBitmap.Width, SourceBitmap.Height),
                ImageLockMode.ReadWrite, SourceBitmap.PixelFormat);
        }

        public BitmapData LockSource()
        {
            return _sourceBitmapData = SourceBitmap.LockBits(
                new Rectangle(0, 0, SourceBitmap.Width, SourceBitmap.Height),
                ImageLockMode.ReadWrite, SourceBitmap.PixelFormat);
        }

        public void UnLockWorking()
        {
            WorkingBitmap.UnlockBits(_workingBitmapData);
        }

        public void UnLockSource()
        {
            SourceBitmap.UnlockBits(_sourceBitmapData);
        }

        public void Reset()
        {
            WorkingBitmap.Dispose();
            CurrentBitmap.Dispose();
            SourceBitmap.Dispose();
            PreviewBitmap.Dispose();
            EmptyBitmap.Dispose();
        }
    }
}