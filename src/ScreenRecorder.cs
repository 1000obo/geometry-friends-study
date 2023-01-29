using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Runtime.InteropServices;
using System.Runtime.Serialization;
using System.Threading;
using System.Windows.Forms;

namespace GeometryFriendsAgents
{
    class ScreenRecorder
    {
        // Gdi+ constants absent from System.Drawing.
        const int PropertyTagLoopCount = 0x5101;
        const short PropertyTagTypeShort = 3;
        // Auxiliary variables to record
        private bool levelEnd;
        private readonly int fps = 30;
        private Thread t;
        // Files Path
        private string logsPath;
        private string filePath;
        // Variables to save gif
        ImageCodecInfo gifEncoder;
        FileStream stream;
        PropertyItem loopPropertyItem;
        EncoderParameters encoderParams1;
        EncoderParameters encoderParamsN;
        Bitmap firstBitmap;


        public ScreenRecorder()
        {
            //Save screen recordings here
            logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }
        }

        public void Start()
        {   
            //start capturing bitmaps
            SaveParameters();
            levelEnd = false;
            t = new Thread(new ThreadStart(StartThread));
            t.Start();         
        }

        //Taken from Stack Overflow - return scaling factor
        [DllImport("gdi32.dll")]
        static extern int GetDeviceCaps(IntPtr hdc, int nIndex);
        public enum DeviceCap
        {
            VERTRES = 10,
            DESKTOPVERTRES = 117,

            // http://pinvoke.net/default.aspx/gdi32/GetDeviceCaps.html
        }
        private float getScalingFactor()
        {
            Graphics g = Graphics.FromHwnd(IntPtr.Zero);
            IntPtr desktop = g.GetHdc();
            int LogicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.VERTRES);
            int PhysicalScreenHeight = GetDeviceCaps(desktop, (int)DeviceCap.DESKTOPVERTRES);

            float ScreenScalingFactor = (float)PhysicalScreenHeight / (float)LogicalScreenHeight;

            return ScreenScalingFactor; // 1.25 = 125%
        }

        private void StartThread()
        {
            float scalingFactor = getScalingFactor();
            Screen s = Screen.AllScreens[Screen.AllScreens.Length-1];
          
            float screenWidth = s.Bounds.Width * scalingFactor;
            float screenHeight = s.Bounds.Height * scalingFactor;

            int c = 0;

            while (!levelEnd) //while the level is not paused or it did not end capture bitmaps
            {
                Bitmap bitmap = new Bitmap((int) screenWidth, (int) screenHeight); //crop
                Graphics graphics = Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(s.WorkingArea.X, s.WorkingArea.Y, 0, 0, new System.Drawing.Size((int) screenWidth, (int) screenHeight));
                SaveBitmap(bitmap, c == 0);
                c += 1;
                Thread.Sleep(1000/fps);
            }     
        }

        public void StopThread()
        {
            levelEnd = true;
            
            if (t.IsAlive)
            {
                t.Abort();
            }
        }


        // Initial parameters to save bitmap to gif
        private void SaveParameters()
        {
            gifEncoder = GetEncoder(ImageFormat.Gif);
            // Params of the first frame.
            encoderParams1 = new EncoderParameters(1);
            encoderParams1.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            // Params of other frames.
            encoderParamsN = new EncoderParameters(1);
            encoderParamsN.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);

            // PropertyItem for the number of animation loops.
            loopPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            loopPropertyItem.Id = PropertyTagLoopCount;
            loopPropertyItem.Type = PropertyTagTypeShort;
            loopPropertyItem.Len = 1;
            // 0 means to animate forever.
            loopPropertyItem.Value = BitConverter.GetBytes((ushort)0);

            filePath = Path.Combine(logsPath, string.Format("log_{0}.gif", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));
            stream = new FileStream(filePath, FileMode.Create);

        }

        // Save bitmap to gif runtime
        private void SaveBitmap(Bitmap b, bool first)
        {       
            if (first)
            {
                firstBitmap = b;
                firstBitmap.SetPropertyItem(loopPropertyItem);
                firstBitmap.Save(stream, gifEncoder, encoderParams1);
            }
            else
            {
                firstBitmap.SaveAdd(b, encoderParamsN);
            }
        }

        private ImageCodecInfo GetEncoder(ImageFormat format)
        {
            ImageCodecInfo[] codecs = ImageCodecInfo.GetImageDecoders();
            foreach (ImageCodecInfo codec in codecs)
            {
                if (codec.FormatID == format.Guid)
                {
                    return codec;
                }
            }
            return null;
        }
       
       
    }
}
