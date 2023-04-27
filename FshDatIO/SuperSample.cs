/*
*  This file is part of FshDatIO, a library that manipulates SC4
*  DBPF files and FSH images.
*
*  Copyright (C) 2010-2017, 2023 Nicholas Hayes
*
*  This program is free software: you can redistribute it and/or modify
*  it under the terms of the GNU General Public License as published by
*  the Free Software Foundation, either version 3 of the License, or
*  (at your option) any later version.
*
*  This program is distributed in the hope that it will be useful,
*  but WITHOUT ANY WARRANTY; without even the implied warranty of
*  MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
*  GNU General Public License for more details.
*
*  You should have received a copy of the GNU General Public License
*  along with this program.  If not, see <http://www.gnu.org/licenses/>.
*
*/

using System;
using System.Drawing;
using System.Drawing.Imaging;
using System.Runtime.InteropServices;
using System.Security.Permissions;

namespace FshDatIO
{
    /////////////////////////////////////////////////////////////////////////////////
    // Paint.NET                                                                   //
    // Copyright (C) dotPDN LLC, Rick Brewster, Tom Jackson, and contributors.     //
    // Portions Copyright (C) Microsoft Corporation. All Rights Reserved.          //
    // See src/Resources/Files/License.txt for full licensing and attribution      //
    // details.                                                                    //
    // .                                                                           //
    /////////////////////////////////////////////////////////////////////////////////

    /// <summary>
    /// The Paint.NET class that provides image resizing using super sampling
    /// </summary>
    public static class SuperSample
    {
        [StructLayout(LayoutKind.Explicit)]
        private struct Color // short replacement for ColorBgra
        {
            [FieldOffset(0)]
            public byte B;
            [FieldOffset(1)]
            public byte G;
            [FieldOffset(2)]
            public byte R;
            [FieldOffset(3)]
            public byte A;
        }

        /// <summary>
        /// Scales a Bitmap to the specified size using the SuperSampling algorithm from Paint.NET 
        /// </summary>
        /// <param name="source">The source bitmap to scale</param>
        /// <param name="width">The width to scale to</param>
        /// <param name="height">The height to scale to</param>
        /// <returns>The scaled Bitmap</returns>
        /// <exception cref="System.ArgumentNullException"><paramref name="source"/> is null.</exception>
        [SecurityPermission(SecurityAction.LinkDemand, UnmanagedCode = true)]
        public static unsafe Bitmap GetBitmapThumbnail(Bitmap source, int width, int height)
        {
            if (source == null)
            {
                throw new ArgumentNullException("source");
            }        

            Bitmap destImage = null;
            using (Bitmap image = new Bitmap(width, height))
            {
                int srcWidth = source.Width;
                int srcHeight = source.Height;

                Rectangle srcRect = new Rectangle(0, 0, srcWidth, srcHeight);
                Rectangle imageRect = new Rectangle(0, 0, image.Width, image.Height);
                BitmapData src = source.LockBits(srcRect, ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);
                BitmapData dest = image.LockBits(imageRect, ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);
               
                try
                {


                    Rectangle destrect = Rectangle.Intersect(imageRect, srcRect);
                    void* srcScan0 = src.Scan0.ToPointer();
                    void* destScan0 = dest.Scan0.ToPointer();
                    int srcStride = src.Stride;
                    int destStride = dest.Stride;

                    for (int y = destrect.Top; y < destrect.Bottom; ++y)
                    {
                        double srcTop = (double)(y * srcHeight) / (double)height;
                        double srcTopFloor = Math.Floor(srcTop);
                        double srcTopWeight = 1 - (srcTop - srcTopFloor);
                        int srcTopInt = (int)srcTopFloor;

                        double srcBottom = (double)((y + 1) * srcHeight) / (double)height;
                        double srcBottomFloor = Math.Floor(srcBottom - 0.00001);
                        double srcBottomWeight = srcBottom - srcBottomFloor;
                        int srcBottomInt = (int)srcBottomFloor;

                        Color* dstPtr = GetPoint(destScan0, destStride, destrect.Left, y);

                        for (int dstX = destrect.Left; dstX < destrect.Right; ++dstX)
                        {
                            double srcLeft = (double)(dstX * srcWidth) / (double)width;
                            double srcLeftFloor = Math.Floor(srcLeft);
                            double srcLeftWeight = 1 - (srcLeft - srcLeftFloor);
                            int srcLeftInt = (int)srcLeftFloor;

                            double srcRight = (double)((dstX + 1) * srcWidth) / (double)width;
                            double srcRightFloor = Math.Floor(srcRight - 0.00001);
                            double srcRightWeight = srcRight - srcRightFloor;
                            int srcRightInt = (int)srcRightFloor;

                            double blueSum = 0;
                            double greenSum = 0;
                            double redSum = 0;
                            double alphaSum = 0;

                            // left fractional edge
                            Color* srcLeftPtr = GetPoint(srcScan0, srcStride, srcLeftInt, srcTopInt + 1);

                            double a;
                            for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                            {
                                a = srcLeftPtr->A;
                                blueSum += srcLeftPtr->B * srcLeftWeight * a;
                                greenSum += srcLeftPtr->G * srcLeftWeight * a;
                                redSum += srcLeftPtr->R * srcLeftWeight * a;
                                alphaSum += srcLeftPtr->A * srcLeftWeight;
                                srcLeftPtr = (Color*)((byte*)srcLeftPtr + src.Stride);
                            }

                            // right fractional edge
                            Color* srcRightPtr = GetPoint(srcScan0, srcStride, srcRightInt, srcTopInt + 1);
                            for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                            {
                                a = srcRightPtr->A;
                                blueSum += srcRightPtr->B * srcRightWeight * a;
                                greenSum += srcRightPtr->G * srcRightWeight * a;
                                redSum += srcRightPtr->R * srcRightWeight * a;
                                alphaSum += srcRightPtr->A * srcRightWeight;
                                srcRightPtr = (Color*)((byte*)srcRightPtr + src.Stride);
                            }

                            // top fractional edge
                            Color* srcTopPtr = GetPoint(srcScan0, srcStride, srcLeftInt + 1, srcTopInt);
                            for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                            {
                                a = srcTopPtr->A;
                                blueSum += srcTopPtr->B * srcTopWeight * a;
                                greenSum += srcTopPtr->G * srcTopWeight * a;
                                redSum += srcTopPtr->R * srcTopWeight * a;
                                alphaSum += srcTopPtr->A * srcTopWeight;
                                ++srcTopPtr;
                            }

                            // bottom fractional edge
                            Color* srcBottomPtr = GetPoint(srcScan0, srcStride, srcLeftInt + 1, srcBottomInt);
                            for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                            {
                                a = srcBottomPtr->A;
                                blueSum += srcBottomPtr->B * srcBottomWeight * a;
                                greenSum += srcBottomPtr->G * srcBottomWeight * a;
                                redSum += srcBottomPtr->R * srcBottomWeight * a;
                                alphaSum += srcBottomPtr->A * srcBottomWeight;
                                ++srcBottomPtr;
                            }

                            // center area
                            for (int srcY = srcTopInt + 1; srcY < srcBottomInt; ++srcY)
                            {
                                Color* srcPtr = GetPoint(srcScan0, srcStride, srcLeftInt + 1, srcY);

                                for (int srcX = srcLeftInt + 1; srcX < srcRightInt; ++srcX)
                                {
                                    a = srcPtr->A;
                                    blueSum += (double)srcPtr->B * a;
                                    greenSum += (double)srcPtr->G * a;
                                    redSum += (double)srcPtr->R * a;
                                    alphaSum += (double)srcPtr->A;
                                    ++srcPtr;
                                }
                            }

                            // four corner pixels
                            Color srcTL = *GetPoint(srcScan0, srcStride, srcLeftInt, srcTopInt);
                            double srcTLA = srcTL.A;
                            blueSum += srcTL.B * (srcTopWeight * srcLeftWeight) * srcTLA;
                            greenSum += srcTL.G * (srcTopWeight * srcLeftWeight) * srcTLA;
                            redSum += srcTL.R * (srcTopWeight * srcLeftWeight) * srcTLA;
                            alphaSum += srcTL.A * (srcTopWeight * srcLeftWeight);

                            Color srcTR = *GetPoint(srcScan0, srcStride, srcRightInt, srcTopInt);
                            double srcTRA = srcTR.A;
                            blueSum += srcTR.B * (srcTopWeight * srcRightWeight) * srcTRA;
                            greenSum += srcTR.G * (srcTopWeight * srcRightWeight) * srcTRA;
                            redSum += srcTR.R * (srcTopWeight * srcRightWeight) * srcTRA;
                            alphaSum += srcTR.A * (srcTopWeight * srcRightWeight);

                            Color srcBL = *GetPoint(srcScan0, srcStride, srcLeftInt, srcBottomInt);
                            double srcBLA = srcBL.A;
                            blueSum += srcBL.B * (srcBottomWeight * srcLeftWeight) * srcBLA;
                            greenSum += srcBL.G * (srcBottomWeight * srcLeftWeight) * srcBLA;
                            redSum += srcBL.R * (srcBottomWeight * srcLeftWeight) * srcBLA;
                            alphaSum += srcBL.A * (srcBottomWeight * srcLeftWeight);

                            Color srcBR = *GetPoint(srcScan0, srcStride, srcRightInt, srcBottomInt);
                            double srcBRA = srcBR.A;
                            blueSum += srcBR.B * (srcBottomWeight * srcRightWeight) * srcBRA;
                            greenSum += srcBR.G * (srcBottomWeight * srcRightWeight) * srcBRA;
                            redSum += srcBR.R * (srcBottomWeight * srcRightWeight) * srcBRA;
                            alphaSum += srcBR.A * (srcBottomWeight * srcRightWeight);

                            double area = (srcRight - srcLeft) * (srcBottom - srcTop);

                            double alpha = alphaSum / area;
                            double blue;
                            double green;
                            double red;

                            if (alpha == 0)
                            {
                                blue = 0;
                                green = 0;
                                red = 0;
                            }
                            else
                            {
                                blue = blueSum / alphaSum;
                                green = greenSum / alphaSum;
                                red = redSum / alphaSum;
                            }

                            // add 0.5 so that rounding goes in the direction we want it to
                            blue += 0.5;
                            green += 0.5;
                            red += 0.5;
                            alpha += 0.5;

                            dstPtr->B = Clamp((uint)blue);
                            dstPtr->G = Clamp((uint)green);
                            dstPtr->R = Clamp((uint)red);
                            dstPtr->A = Clamp((uint)alpha);
                            ++dstPtr;
                        }
                    }
                }
                finally
                {
                    source.UnlockBits(src);
                    image.UnlockBits(dest);
                }

                destImage = image.Clone(imageRect, PixelFormat.Format32bppArgb);
            }
           

            return destImage;
        }
        
        static unsafe Color* GetPoint(void* scan0, int stride, int x, int y)
        { 
            return (Color*)((byte*)scan0 + (y * stride) + (x * 4));
        }
        
        static byte Clamp(uint b)
        {
            if (b < 0)
            {
                return 0;
            }
            else if (b > 255)
            {
                return 255;
            }
            
            return (byte)b;
        }
    }
}
