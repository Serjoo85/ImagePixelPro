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

        private BitmapData _workingBitmapData;
        private BitmapData _sourceBitmapData;

        private Action<Action> _sendImageToForm;

        private PictureBox _pictureBox;

        public BitmapImagesKeeper(Bitmap sourceBitmap, Action<Action> sendImageToForm, System.Windows.Forms.PictureBox pictureBox)
        {
            _sourceBitmap = sourceBitmap;
            _workingBitmap = new Bitmap(sourceBitmap.Width, sourceBitmap.Height);
            _sendImageToForm = sendImageToForm;
            _pictureBox = pictureBox;
        }

        public void ChangeCurrent(Bitmap modifiedImage)
        {
            _workingBitmap.UnlockBits(_workingBitmapData);
            _previewBitmap = _currentBitmap;
            _currentBitmap = modifiedImage;
            _sendImageToForm.Invoke(() => { _pictureBox.Image = _currentBitmap; });
            _previewBitmap?.Dispose();
        }

        public void ChangeCurrent100(Bitmap modifiedImage)
        {
            _workingBitmap.UnlockBits(_workingBitmapData);
            _previewBitmap = _currentBitmap;
            _workingBitmap = (Bitmap)_sourceBitmap.Clone();
            _currentBitmap = (Bitmap)_workingBitmap.Clone();
            UnLockWorking();
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