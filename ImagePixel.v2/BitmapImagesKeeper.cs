using System.Drawing.Imaging;
using System.Windows.Forms;

namespace ImagePixel.v2
{
    public class BitmapImagesKeeper
    {
        public Bitmap _workingBitmap {get;set;}
        public Bitmap _currentBitmap {get;set;}
        public Bitmap _sourceBitmap {get;set;}
        public Bitmap _previewBitmap { get;set;}

        public Bitmap _emptyBitmap { get; set; }

        public BitmapData WorkingBitmapData => _workingBitmapData;
        public BitmapData SourceBitmapData => _sourceBitmapData;

        private Action<Action> _sendImageToForm;

        private PictureBox _pictureBox;
        private BitmapData _sourceBitmapData;
        private BitmapData _workingBitmapData;

        public BitmapImagesKeeper(Bitmap sourceBitmap, Action<Action> sendImageToForm, System.Windows.Forms.PictureBox pictureBox)
        {
            _sourceBitmap = sourceBitmap;
            _workingBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            _emptyBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            _sendImageToForm = sendImageToForm;
            _pictureBox = pictureBox;
        }

        public void ChangeCurrent()
        {
            _previewBitmap = _currentBitmap;
            _currentBitmap = (Bitmap)_workingBitmap.Clone();
            _workingBitmap.UnlockBits(WorkingBitmapData);
            _sendImageToForm.Invoke(() => { _pictureBox.Image = _currentBitmap; });
            _previewBitmap?.Dispose();
        }

        public void ChangeCurrent100()
        {
            _previewBitmap = _currentBitmap;
            _workingBitmap = (Bitmap)_sourceBitmap.Clone();
            _currentBitmap = (Bitmap)_workingBitmap.Clone();
            _sendImageToForm.Invoke(new Action(() => { _pictureBox.Image = (Bitmap)_currentBitmap; }));
            _previewBitmap?.Dispose();
        }

        public void ChangeCurrent0()
        {
            _previewBitmap = _currentBitmap;
            _workingBitmap = (Bitmap)_emptyBitmap.Clone();
            _currentBitmap = (Bitmap)_workingBitmap.Clone();
            _sendImageToForm.Invoke(new Action(() => { _pictureBox.Image = (Bitmap)_currentBitmap; }));
            _previewBitmap?.Dispose();
        }

        public BitmapData LockWorking()
        {
            return _workingBitmapData = _workingBitmap.LockBits(
                new Rectangle(0, 0, _sourceBitmap.Width, _sourceBitmap.Height), 
                ImageLockMode.ReadWrite,_sourceBitmap.PixelFormat);
        }

        public BitmapData LockSource()
        {
            return _sourceBitmapData = _sourceBitmap.LockBits(
                new Rectangle(0, 0, _sourceBitmap.Width, _sourceBitmap.Height), 
                ImageLockMode.ReadWrite,_sourceBitmap.PixelFormat);
        }

        public void UnLockWorking()
        {
            _workingBitmap.UnlockBits(_workingBitmapData);
        }

        public void UnLockSource()
        {
            _sourceBitmap.UnlockBits(_sourceBitmapData);
        }
    }
}