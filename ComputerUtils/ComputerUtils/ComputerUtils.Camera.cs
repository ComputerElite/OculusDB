using System;
using System.Collections;
using AForge.Video;
using AForge.Video.DirectShow;
using System.Drawing;

namespace ComputerUtils.Camera
{
    public class CameraManager
    {
        private FilterInfoCollection videoDevices;
        private VideoCaptureDevice videoDevice;
        private VideoCapabilities[] snapshotCapabilities;
        private ArrayList listCamera = new ArrayList();
        public static string usbcamera;
        public Bitmap lastFrame = new Bitmap(1, 1);
        public delegate void FrameRecieved(Bitmap frame);
        public event FrameRecieved FrameRecievedEvent;
        //public string pathFolder = Application.StartupPath + @"\ImageCapture\";

        public void getListCameraUSB()
        {
            //get input devices
            videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
            int i = 0;
            if (videoDevices.Count != 0)
            {
                // add all devices to combo
                foreach (FilterInfo device in videoDevices)
                {
                    //Write all cameras in console
                    Console.WriteLine(i + ". " + device.Name);
                    i++;
                }
            }
            else
            {
                Console.WriteLine("No DirectShow devices found");
            }
        }

        public void OpenCamera(int index)
        {
            try
            {
                usbcamera = index.ToString();
                //Get all cameras
                videoDevices = new FilterInfoCollection(FilterCategory.VideoInputDevice);
                if (videoDevices.Count != 0)
                {
                    // add all devices to combo
                    foreach (FilterInfo device in videoDevices)
                    {
                        listCamera.Add(device.Name);
                    }
                }
                else
                {
                    Console.WriteLine("No Camera devices found");
                }
                //get camera
                videoDevice = new VideoCaptureDevice(videoDevices[Convert.ToInt32(usbcamera)].MonikerString);
                snapshotCapabilities = videoDevice.SnapshotCapabilities;
                if (snapshotCapabilities.Length == 0)
                {
                    //MessageBox.Show("Camera Capture Not supported");
                }
                //Start camera and set up frame event
                videoDevice.Start();
                videoDevice.NewFrame += Display;
            }
            catch (Exception err)
            {
                Console.WriteLine(err.ToString());
            }
        }



        private void Display(object sender, NewFrameEventArgs eventArgs)
        {
            //Fire fram event
            FrameRecievedEvent(eventArgs.Frame);
        }
    }
}