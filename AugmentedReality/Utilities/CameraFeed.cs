using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.Devices;
using System.Windows.Media.Imaging;

namespace AugmentedReality.Utilities
{
    public class CameraFeed
    {
        private PhotoCamera camera;
        private bool cameraInitialized = false;

        public CameraFeed()
        {
            try
            {
                camera = new PhotoCamera(CameraType.Primary);                
                camera.Initialized += camera_Initialized;
            }
            catch (Exception exp) { }
        }

        private void camera_Initialized(object sender, CameraOperationCompletedEventArgs e)
        {
            if (e.Succeeded)
                cameraInitialized = true;
        }        

        public WriteableBitmap GetPreview()
        {
            if (cameraInitialized)
            {
                WriteableBitmap output = new WriteableBitmap((int)camera.PreviewResolution.Width, (int)camera.PreviewResolution.Height);
                camera.GetPreviewBufferArgb32(output.Pixels);
                return output;
            }
            else
                return null;
        }


    }
}
