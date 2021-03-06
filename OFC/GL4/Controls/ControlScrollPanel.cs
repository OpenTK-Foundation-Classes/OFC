﻿/*
 * Copyright 2019-2020 Robbyxp1 @ github.com
 *
 * Licensed under the Apache License, Version 2.0 (the "License"); you may not use this
 * file except in compliance with the License. You may obtain a copy of the License at
 *
 * http://www.apache.org/licenses/LICENSE-2.0
 * 
 * Unless required by applicable law or agreed to in writing, software distributed under
 * the License is distributed on an "AS IS" BASIS, WITHOUT WARRANTIES OR CONDITIONS OF
 * ANY KIND, either express or implied. See the License for the specific language
 * governing permissions and limitations under the License.
 */

using System;
using System.Drawing;

namespace OFC.GL4.Controls
{
    // Scroll panel
    // must not be a child of GLForm as it needs a bitmap to paint into

    public class GLVerticalScrollPanel : GLPanel
    {
        public GLVerticalScrollPanel(string name, Rectangle location) : base(name, location)
        {
        }

        public GLVerticalScrollPanel() : this("VSP?", DefaultWindowRectangle)
        {
        }

        public int ScrollRange { get { return (LevelBitmap != null) ? (LevelBitmap.Height - Height) : 0; } }
        public int ScrollPos { get { return scrollpos; } set { SetScrollPos(value); } }
        private int scrollpos = 0;

        // Width/Height is size of the control without scrolling
        // we layout the children within that area.
        // but if we have areas outside that, the bitmap is expanded to cover it

        public override void PerformRecursiveLayout()
        {
            base.PerformRecursiveLayout();               // layout the children

            bool needbitmap = false;

            if (ControlsZ.Count > 0)
            {
                Rectangle r = ChildArea();
                int childheight = r.Bottom;

                needbitmap = childheight > Height;

                if (needbitmap)
                {
                    if (LevelBitmap == null )
                    {
                        System.Diagnostics.Debug.WriteLine("Make SP bitmap " + Width + "," + childheight);
                        MakeLevelBitmap(Width, childheight);
                    }
                    else if ( childheight != LevelBitmap.Height || LevelBitmap.Width != Width) // if height is different, or width is different
                    {
                        MakeLevelBitmap(Width, childheight);
                        System.Diagnostics.Debug.WriteLine("Make SP bitmap " + Width + "," + childheight);
                    }
                }
            }

            if ( !needbitmap && LevelBitmap != null)
            {
                MakeLevelBitmap(0,0);
            }
        }

        public override void CheckBitmapAfterLayout()       // do nothing, we do not resize bitmap just because our client size has changed
        {
        }

        private void SetScrollPos(int value)
        {
            if (LevelBitmap != null)
            {
                int maxsp = LevelBitmap.Height - Height;
                scrollpos = Math.Max(0, Math.Min(value, maxsp));
                System.Diagnostics.Debug.WriteLine("ScrollPanel scrolled to " + scrollpos);
                Invalidate();
            }
        }

        // only will be called if we have a bitmap defined..

        protected override void PaintParent(Rectangle parentarea, Graphics parentgr)
        {
            System.Diagnostics.Debug.WriteLine("Scroll panel {0} parea {1} Bitmap {2}", Name, parentarea, LevelBitmap.Size);

            parentgr.DrawImage(LevelBitmap, parentarea.Left, parentarea.Top, new Rectangle(0, scrollpos, Width, Height), GraphicsUnit.Pixel);
        }
    }
}

