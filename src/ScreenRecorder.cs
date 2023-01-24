using System;
using System.Collections.Generic;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.Serialization;
using System.Threading;

namespace GeometryFriendsAgents
{
    class ScreenRecorder
    {
        // Gdi+ constants absent from System.Drawing.
        const int PropertyTagFrameDelay = 0x5100;
        const int PropertyTagLoopCount = 0x5101;
        const short PropertyTagTypeLong = 4;
        const short PropertyTagTypeShort = 3;
        const int UintBytes = 4;
        private string _filePath;
        public bool levelEnd = false;
        private List<Bitmap> frames = new List<Bitmap>();
        private string logsPath;
        private Thread t;
        private int fps = 25;

        public ScreenRecorder()
        {
            logsPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Logs");
            if (!Directory.Exists(logsPath))
            {
                Directory.CreateDirectory(logsPath);
            }
        }

        public void Start()
        {         
            frames.Clear();
            levelEnd = false;
            t = new Thread(new ThreadStart(StartThread));
            t.Start();         
        }

        private void StartThread()
        {
            var screenWidth = 1920;//Screen.PrimaryScreen.Bounds.Width;
            var screenHeight = 1080;// Screen.PrimaryScreen.Bounds.Height;
            while (!levelEnd)
            {
                var bitmap = new Bitmap(screenWidth, screenHeight); //crop
                var graphics = System.Drawing.Graphics.FromImage(bitmap);
                graphics.CopyFromScreen(0, 0, 0, 0, new System.Drawing.Size(screenWidth, screenHeight));
                frames.Add(bitmap);
                Thread.Sleep(1000/fps);
            }
        }

        public void StopThread()
        {
            if (t.IsAlive)
            {
                t.Abort();
                Save();
            }
        }

        private void Save()
        {

           var gifEncoder = GetEncoder(ImageFormat.Gif);
            // Params of the first frame.
            var encoderParams1 = new EncoderParameters(1);
            encoderParams1.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.MultiFrame);
            // Params of other frames.
            var encoderParamsN = new EncoderParameters(1);
            encoderParamsN.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.FrameDimensionTime);
            // Params for the finalizing call.
            var encoderParamsFlush = new EncoderParameters(1);
            encoderParamsFlush.Param[0] = new EncoderParameter(Encoder.SaveFlag, (long)EncoderValue.Flush);
            
            // PropertyItem for the frame delay (apparently, no other way to create a fresh instance).
            var frameDelay = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            frameDelay.Id = PropertyTagFrameDelay;
            frameDelay.Type = PropertyTagTypeLong;
            // Length of the value in bytes.
            frameDelay.Len = frames.Count * UintBytes;
            // The value is an array of 4-byte entries: one per frame.
            // Every entry is the frame delay in 1/100-s of a second, in little endian.
            frameDelay.Value = new byte[frames.Count * UintBytes];
            // E.g., here, we're setting the delay of every frame to 40 miliseconds.
            var frameDelayBytes = BitConverter.GetBytes((uint)100/fps);
            for (int j = 0; j < frames.Count; ++j)
                Array.Copy(frameDelayBytes, 0, frameDelay.Value, j * UintBytes, UintBytes);

            // PropertyItem for the number of animation loops.
            var loopPropertyItem = (PropertyItem)FormatterServices.GetUninitializedObject(typeof(PropertyItem));
            loopPropertyItem.Id = PropertyTagLoopCount;
            loopPropertyItem.Type = PropertyTagTypeShort;
            loopPropertyItem.Len = 1;
            // 0 means to animate forever.
            loopPropertyItem.Value = BitConverter.GetBytes((ushort)0);

            _filePath = Path.Combine(logsPath, string.Format("log_{0}.gif", DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss")));

            using (var stream = new FileStream(_filePath, FileMode.Create))
            {
                bool first = true;
                Bitmap firstBitmap = null;
                int numFrames = frames.Count;
                // Bitmaps is a collection of Bitmap instances that'll become gif frames.
                 for (int i = 0;  i < numFrames; i++)
                 {
                     if (first)
                     {
                         firstBitmap = frames[i];
                         firstBitmap.SetPropertyItem(frameDelay);
                         firstBitmap.SetPropertyItem(loopPropertyItem);
                         firstBitmap.Save(stream, gifEncoder, encoderParams1);
                         first = false;
                     }
                     else
                     {
                        firstBitmap.SaveAdd(frames[i], encoderParamsN);
                     }
                 }
                 firstBitmap.SaveAdd(encoderParamsFlush);
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
