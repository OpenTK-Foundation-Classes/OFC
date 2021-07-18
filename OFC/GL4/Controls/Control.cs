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

// Rules - no winforms in Control land except for Keys

using System;
using System.Collections.Generic;
using System.Drawing;

namespace OFC.GL4.Controls
{
    [System.Diagnostics.DebuggerDisplay("Control {Name} {window}")]
    public abstract class GLBaseControl : IDisposable
    {
        #region Main UI
        public string Name { get; set; } = "?";

        // bounds of the window - include all margin/padding/borders/
        // co-ords are in offsets from 0,0 being the parent top left corner. See also Set()

        public Rectangle Bounds { get { return window; } set { SetPos(value.Left, value.Top, value.Width, value.Height); } }
        public int Left { get { return window.Left; } set { SetPos(value, window.Top, window.Width, window.Height); } }
        public int Right { get { return window.Right; } set { SetPos(window.Left, window.Top, value - window.Left, window.Height); } }
        public int Top { get { return window.Top; } set { SetPos(window.Left, value, window.Width, window.Height); } }
        public int Bottom { get { return window.Bottom; } set { SetPos(window.Left, window.Top, window.Width, value - window.Top); } }
        public int Width { get { return window.Width; } set { SetPos(window.Left, window.Top, value, window.Height); } }
        public int Height { get { return window.Height; } set { SetPos(window.Left, window.Top, window.Width, value); } }
        public Point Location { get { return new Point(window.Left, window.Top); } set { SetPos(value.X, value.Y, window.Width, window.Height); } }
        public Size Size { get { return new Size(window.Width, window.Height); } set { SetPos(window.Left, window.Top, value.Width, value.Height); } }

        // only for top level windows at the moment, we can throw them on the screen scaled..  <1 smaller, >1 bigger
        public SizeF? ScaleWindow { get { return altscale; } set { altscale = value; AltScaleChanged = true; FindDisplay()?.ReRender(); } }
        private SizeF? altscale = null;
        public bool AltScaleChanged { get; set; } = false;
        public Size ScaledSize { get { if ( altscale!=null) return new Size((int)(Width * ScaleWindow.Value.Width), (int)(Height * ScaleWindow.Value.Height)); else return Size; } }

        public List<IControlAnimation> Animators { get; set; } = new List<IControlAnimation>();

        // padding/margin and border control

        public GL4.Controls.Padding Padding { get { return padding; } set { if (padding != value) { padding = value; CalcClientRectangle(); InvalidateLayout(); } } }
        public GL4.Controls.Margin Margin { get { return margin; } set { if (margin != value) { margin = value; CalcClientRectangle(); InvalidateLayout(); } } }
        public void SetMarginBorderWidth(Margin m, int borderw, Color borderc, Padding p) { margin = m; padding = p; bordercolor = borderc; borderwidth = borderw; CalcClientRectangle(); InvalidateLayout(); }
        public Color BorderColor { get { return bordercolor; } set { if (bordercolor != value) { bordercolor = value; Invalidate(); } } }
        public int BorderWidth { get { return borderwidth; } set { if (borderwidth != value) { borderwidth = value; CalcClientRectangle(); InvalidateLayout(); } } }

        // this is the client area, inside the margin/padding/border

        public int ClientLeftMargin { get { return Margin.Left + Padding.Left + BorderWidth; } }
        public int ClientRightMargin { get { return Margin.Right + Padding.Right + BorderWidth; } }
        public int ClientWidthMargin { get { return Margin.TotalWidth + Padding.TotalWidth + BorderWidth*2; } }
        public int ClientTopMargin { get { return Margin.Top + Padding.Top + BorderWidth; } }
        public int ClientBottomMargin { get { return Margin.Bottom + Padding.Bottom + BorderWidth; } }
        public int ClientHeightMargin { get { return Margin.TotalHeight + Padding.TotalHeight + BorderWidth*2; } }
        public int ClientWidth { get { return ClientRectangle.Width ; } set { SetPos(window.Left, window.Top, value + ClientLeftMargin + ClientRightMargin, window.Height); } }
        public int ClientHeight { get { return ClientRectangle.Height; } set { SetPos(window.Left, window.Top, window.Width, value + ClientTopMargin + ClientBottomMargin); } }
        public Size ClientSize { get { return ClientRectangle.Size; } set { SetPos(window.Left, window.Top, value.Width + ClientLeftMargin + ClientRightMargin, value.Height + ClientTopMargin + ClientBottomMargin); } }
        public Point ClientLocation { get { return new Point(ClientLeftMargin, ClientTopMargin); } }
        public Rectangle ClientRectangle { get; private set; }

        // docking control

        public DockingType Dock { get { return docktype; } set { if (docktype != value) { docktype = value; InvalidateLayoutParent(); } } }
        public float DockPercent { get { return dockpercent; } set { if (value != dockpercent) { dockpercent = value; InvalidateLayoutParent(); } } }        // % in 0-1 terms used to dock on left,top,right,bottom.  0 means just use width/height

        // Autosize

        public bool AutoSize { get { return autosize; } set { if (autosize != value) { autosize = value; InvalidateLayoutParent(); } } }

        // toggle controls
        public bool Enabled { get { return enabled; } set { if (enabled != value) { SetEnabled(value); Invalidate(); } } }
        public bool Visible { get { return visible; } set { if (visible != value) { visible = value; InvalidateLayoutParent(); } } }

        // Focus
        public virtual bool Focused { get { return focused; } }
        public virtual bool Focusable { get { return focusable; } set { focusable = value; } }          // if set, it can get focus. if clear, clicking on it sets focus to null
        public virtual bool RejectFocus { get { return rejectfocus; } set { rejectfocus = value; } }    // if set, focus is never given or changed by clicking on it.
        public virtual bool GiveFocusToParent { get { return givefocustoparent; } set { givefocustoparent= value; } }    // if set, focus is passed to parent if present, and it does not reject it
        public virtual bool SetFocus() { return FindDisplay()?.SetFocus(this) ?? false; }
        
        // colour font

        private Font DefaultFont = new Font("Ms Sans Serif", 8.25f);
        public Font Font { get { return font ?? parent?.Font ?? DefaultFont; } set { SetFont(value); InvalidateLayout(); } }
        public Color BackColor { get { return backcolor; } set { if (backcolor != value) { backcolor = value; Invalidate(); } } }
        public int BackColorGradientDir { get { return backcolorgradientdir; } set { if (backcolorgradientdir != value) { backcolorgradientdir = value; Invalidate(); } } }
        public Color BackColorGradientAlt { get { return backcolorgradientalt; } set { if (backcolorgradientalt != value) { backcolorgradientalt = value; Invalidate(); } } }

        // heirarchy
        public GLBaseControl Parent { get { return parent; } }
        public GLControlDisplay FindDisplay() { return this is GLControlDisplay ? this as GLControlDisplay : parent?.FindDisplay(); }
        public GLBaseControl FindControlUnderDisplay() { return Parent is GLControlDisplay ? this : parent?.FindControlUnderDisplay(); }
        public GLForm FindForm() { return this is GLForm ? this as GLForm : parent?.FindForm(); }

        public GLBaseControl Creator { get { return creator; } set { creator = value; } } // normally the same as parent, sometimes different if the control is attached to desktop

        // tooltips
        public string ToolTipText { get; set; } = null;

        // Table layout
        public int Row { get { return row; } set { row = value; InvalidateLayoutParent(); } }       // for table layouts
        public int Column { get { return column; } set { column = value; InvalidateLayoutParent(); } } // for table layouts

        // Flow layout
        public Point FlowOffsetPosition { get; set; } = Point.Empty;        // optionally offset this control from its flow position by this value

        // Auto Invalidate
        public bool InvalidateOnEnterLeave { get; set; } = false;       // if set, invalidate on enter/leave to force a redraw
        public bool InvalidateOnMouseMove { get; set; } = false;        // if set, invalidate on mouse move in control
        public bool InvalidateOnMouseDownUp { get; set; } = false;      // if set, invalidate on mouse button down/up to force a redraw
        public bool InvalidateOnFocusChange { get; set; } = false;      // if set, invalidate on focus change

        // State for use during drawing
        public bool Hover { get; set; } = false;                        // mouse is over control
        public GLMouseEventArgs.MouseButtons MouseButtonsDown { get; set; } // set if mouse buttons down over control

        // Bitmap
        public Bitmap LevelBitmap { get { return levelbmp; } }  // return level bitmap, null if does not have a level bitmap 

        // User properties
        public Object Tag { get; set; }                         // control tag, user controlled

        // Tabs
        public int TabOrder { get; set; } = -1;                 // set, the lowest tab order wins the form focus

        // Others
        public bool TopMost { get { return topMost; } set { topMost = value; if (topMost) BringToFront(); } } // set to force top most

        // control lists

        public virtual List<GLBaseControl> ControlsIZ { get { return childreniz; } }      // read only, in inv zorder, so 0 = last layout first drawn
        public virtual List<GLBaseControl> ControlsZ { get { return childrenz; } }          // read only, in zorder, so 0 = first layout last painted
        public GLBaseControl this[string s] { get { return ControlsZ.Find((x)=>x.Name == s); } }    // null if not

        // events

        public Action<Object, GLMouseEventArgs> MouseDown { get; set; } = null;  // location in client terms, NonClientArea set if on border with negative/too big x/y for clients
        public Action<Object, GLMouseEventArgs> MouseUp { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseMove { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseClick { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseDoubleClick { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseWheel { get; set; } = null;
        public Action<Object, GLMouseEventArgs> MouseEnter { get; set; } = null;  // location in terms of whole window
        public Action<Object, GLMouseEventArgs> MouseLeave { get; set; } = null;  // location in terms of whole window
        public Action<Object, GLKeyEventArgs> KeyDown { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyUp { get; set; } = null;
        public Action<Object, GLKeyEventArgs> KeyPress { get; set; } = null;
        public enum FocusEvent { Focused,   // OnFocusChange - you get the old focus
                                Deactive,   // you get the new one focused
                                ChildFocused,   // you get the new one focused
                                ChildDeactive }; // you get the new one focused
        public Action<Object, FocusEvent, GLBaseControl> FocusChanged { get; set; } = null;     // send to control gaining/losing focus, and to its parents
        public Action<Object> FontChanged { get; set; } = null;
        public Action<Object> Resize { get; set; } = null;
        public Action<Object> Moved { get; set; } = null;
        public Action<GLBaseControl, GLBaseControl> ControlAdd { get; set; } = null;
        public Action<GLBaseControl, GLBaseControl> ControlRemove { get; set; } = null;

        public Action<GLBaseControl, GLBaseControl> GlobalFocusChanged { get; set; } = null;        // sent to all controls on a focus change. Either may be null
        public Action<GLBaseControl, GLMouseEventArgs> GlobalMouseClick { get; set; } = null;       // sent to all controls on a click
        public Action<GLBaseControl, GLMouseEventArgs> GlobalMouseDown { get; set; } = null;       // sent to all controls on a click. GLBaseControl may be null

        public Action<GLMouseEventArgs> GlobalMouseMove { get; set; }       // only hook on GLControlDisplay.  Has all the GLMouseEventArgs fields filled out including control ones

        // default color schemes and sizes

        public static Action<GLBaseControl> Themer = null;                 // set this up, will be called when the control is added for you to theme the colours/options

        static public Color DefaultFormBackColor = Color.FromArgb(255,255,255);
        static public Color DefaultFormTextColor = Color.Black;
        static public Color DefaultControlBackColor = Color.Gray;
        static public Color DefaultControlForeColor = Color.White;
        static public Color DefaultPanelBackColor = Color.FromArgb(140, 140, 140);
        static public Color DefaultLabelForeColor = Color.Black;
        static public Color DefaultBorderColor = Color.Gray;
        static public Color DefaultButtonBorderBackColor = Color.FromArgb(80, 80, 80);
        static public Color DefaultButtonBackColor = Color.Gray;
        static public Color DefaultButtonBorderColor = Color.FromArgb(100, 100, 100);
        static public Color DefaultMouseOverButtonColor = Color.FromArgb(200, 200, 200);
        static public Color DefaultMouseDownButtonColor = Color.FromArgb(230, 230, 230);
        static public Color DefaultCheckBoxBorderColor = Color.White;
        static public Color DefaultCheckBoxInnerColor = Color.Wheat;
        static public Color DefaultMenuIconStripBackColor = Color.FromArgb(160, 160, 160);
        static public Color DefaultCheckColor = Color.DarkBlue;
        static public Color DefaultErrorColor = Color.OrangeRed;
        static public Color DefaultHighlightColor = Color.Red;

        static public Color DefaultLineSeparColor = Color.Green;

        public void Invalidate()
        {
            //System.Diagnostics.Debug.WriteLine("Invalidate " + Name);
            NeedRedraw = true;

            if (BackColor == Color.Transparent)   // if we are transparent, we need the parent also to redraw to force it to redraw its background.
            {
                //System.Diagnostics.Debug.WriteLine("Invalidate " + Name + " is transparent, parent needs it too");
                Parent?.Invalidate();
            }

            FindDisplay()?.ReRender();
        }

        public void InvalidateLayout()
        {
            Invalidate();
            PerformLayout();
        }

        public void InvalidateLayoutParent()
        {
            //System.Diagnostics.Debug.WriteLine("Invalidate Layout Parent " + Name);
            if (parent != null)
            {
                FindDisplay()?.ReRender();
                //System.Diagnostics.Debug.WriteLine(".. Redraw and layout on " + Parent.Name);
                parent.NeedRedraw = true;
                parent.PerformLayout();
            }
        }

        public Rectangle ChildArea()            // area used by children controls
        {
            int left = int.MaxValue, right = int.MinValue, top = int.MaxValue, bottom = int.MinValue;

            foreach (var c in childrenz)
            {
                if (c.Left < left)
                    left = c.Left;
                if (c.Right > right)
                    right = c.Right;
                if (c.Top < top)
                    top = c.Top;
                if (c.Bottom > bottom)
                    bottom = c.Bottom;
            }

            return new Rectangle(left, top, right - left, bottom - top);
        }

        public bool IsThisOrChildOf(GLBaseControl ctrl)         // ctrl us, or one of our children?
        {
            if (ctrl == this)
                return true;
            foreach( var c in ControlsZ)
            {
                if (c.IsThisOrChildOf(ctrl))
                    return true;
            }
            return false;
        }

        public virtual bool IsThisOrChildrenFocused()
        {
            if (Focused)
                return true;
            foreach (var c in ControlsZ)
            {
                if (c.IsThisOrChildrenFocused())
                    return true;
            }
            return false;
        }

        // next tab, from tabno, either forward or back
        public GLBaseControl FindNextTabChild(int tabno, bool forward = true)       
        {
            GLBaseControl found = null;
            int mindist = int.MaxValue;

            foreach (var c in ControlsZ)
            {
                if (c.Focusable && c.Visible && c.Enabled )
                {
                    int dist = c.TabOrder - tabno;

                    if (forward ? dist > 0 : dist < 0)
                    {
                        dist = Math.Abs(dist);
                        if (dist < mindist)
                        {
                            mindist = dist;
                            found = c;
                        }
                    }
                }
            }

            return found;
        }

        public void PerformLayout()             // perform layout on all child containers inside us. Does not call Layout on ourselves
        {
            if (suspendLayoutCount>0)
            {
                needLayout = true;
                //System.Diagnostics.Debug.WriteLine("Suspended layout on " + Name);
            }
            else
            {
                PerformRecursiveSize(Parent?.ClientSize ?? ClientSize);         // we recusively size
                PerformRecursiveLayout();       // and we layout, recursively
            }
        }

        public void SuspendLayout()
        {
            suspendLayoutCount++;
            //System.Diagnostics.Debug.WriteLine("Suspend layout on " + Name);
        }

        public void ResumeLayout()
        {
            //if ( suspendLayoutSet ) System.Diagnostics.Debug.WriteLine("Resume Layout on " + Name);

            if (suspendLayoutCount == 0 || --suspendLayoutCount == 0)       // if at 0, or counts to 0
            {
                if (needLayout)
                {
                    //System.Diagnostics.Debug.WriteLine("Required layout " + Name);
                    PerformLayout();
                }
            }
        }

        public void CallPerformRecursiveLayout()        // because you can't call from an inheritor, even though your the same class, silly
        {
            PerformRecursiveLayout();
        }

        public virtual bool AddToDesktop(GLBaseControl child, bool atback = false)
        {
            var f = FindDisplay();
            if (f != null)
            {
                f.Add(child, atback);
                return true;
            }
            else
                return false;
        }

        public virtual void Add(GLBaseControl child, bool atback = false)
        {
            System.Diagnostics.Debug.Assert(!childrenz.Contains(child));        // no repeats
            child.parent = child.creator = this;

            child.ClearFlagsDown();       // in case of reuse, clear all temp flags as child is added

            if (atback)
            {
                childrenz.Add(child);
                childreniz.Insert(0, child);
            }
            else
            {
                int ipos = 0;
                if (!child.TopMost)     // add at end of top list.
                {
                    while (ipos < childrenz.Count && childrenz[ipos].TopMost)     // find first place we can insert
                        ipos++;
                }

                childrenz.Insert(ipos, child);   // in z order.  First is top of z.  insert puts it before existing
                childreniz.Insert(childreniz.Count - ipos, child);       // in inv z order. Last is top of z.  if ipos=0, at end. if ipos=1, inserted just before end
            }

            CheckZOrder();      // verify its okay 

            Themer?.Invoke(child);      // added to control, theme it

            OnControlAdd(this, child);
            child.OnControlAdd(this, child);
            InvalidateLayout();        // we are invalidated and layout
        }

        public virtual void AddItems(IEnumerable<GLBaseControl> list)
        {
            SuspendLayout();
            foreach (var i in list)
                Add(i);
            ResumeLayout();
        }

        public static void Remove(GLBaseControl child)     // remove is normal, the closes down and disposes of the child and all its children
        {                                                  
            if (child.Parent != null) // if attached
            {
                GLBaseControl parent = child.Parent;
                parent.RemoveControl(child, true, true);
                parent.InvalidateLayout();
            }
        }

        public static void Detach(GLBaseControl child)     // a detach keeps the child and its children alive and connected together, but detached from parent
        {
            if (child.Parent != null) // if attached
            {
                GLBaseControl parent = child.Parent;
                parent.RemoveControl(child, false, false);
                parent.InvalidateLayout();
            }
        }

        public virtual bool BringToFront()      // bring to the front, true if it was at the front
        {
            return Parent?.BringToFront(this) ?? true;
        }

        public virtual bool BringToFront(GLBaseControl child)   // bring child to front, true if already in front
        {
            //System.Diagnostics.Debug.WriteLine("Bring to front" + child.Name);
            int curpos = childrenz.IndexOf(child);

            if (curpos>=0)
            { 
                int ipos = 0;

                if ( !child.TopMost )
                {
                    while (ipos < childrenz.Count && childrenz[ipos].TopMost)     // find first place we can move to
                        ipos++;
                }

                if ( curpos != ipos )       // if not in first position possible
                {
                    childrenz.Remove(child);
                    childreniz.Remove(child);
                                            // list now has child removed, now insert back into position
                    childrenz.Insert(ipos, child);   // in z order.  First is top of z
                    childreniz.Insert(childreniz.Count-ipos,child);       // in inv z order. Last is top of z

                    CheckZOrder();

                    InvalidateLayout();
                    return false;
                }
            }

            return true;
        }

        // Easy way, using naming, to address controls. wildcards ? * 
        public void ApplyToControlOfName(string wildcardname,Action<GLBaseControl> act, bool recurse = false)
        {
            foreach( var c in ControlsZ)
            {
                if (recurse)
                    c.ApplyToControlOfName(wildcardname, act, recurse);
                if ( c.Name.WildCardMatch(wildcardname))
                    act(c);
            }
        }

        // p = co-coords finds including margin/padding/border area, so inside bounds
        // if control found, return offset within bounds left

        public GLBaseControl FindControlOver(Point coords, out Point offset)
        {
            Size sz = ScaledSize;       // get visual size shown to user

            if (coords.X < Left || coords.X >= Left+sz.Width || coords.Y < Top || coords.Y >= Top+sz.Height)       // if outside our bounds, not found
            {
                offset = Point.Empty;
                return null;
            }
            else
            {
                //System.Diagnostics.Debug.WriteLine($"Find {Name} {coords} {ScaleWindow} {sz}");

                coords = new Point(coords.X - Left, coords.Y - Top);            // coords translated to inside the bounds of this control
                //System.Diagnostics.Debug.WriteLine($"-> {coords} ");
                
                if ( ScaleWindow != null )      // we need to match the offset above, in screen pixels, to the internal scale. / because if the ScaleWindow <1, it means the internal scale is bigger than the visual one
                {
                    coords = new Point((int)(coords.X / ScaleWindow.Value.Width), (int)(coords.Y / ScaleWindow.Value.Height));
                    //System.Diagnostics.Debug.WriteLine($"-> {coords} ");
                }
            }

            foreach (GLBaseControl c in childrenz)       // in Z order
            {
                if (c.Visible)      // must be visible to be found..
                {
                    // convert bounds co-ords to client coords by removing client margin, and check

                    var r = c.FindControlOver(new Point(coords.X-ClientLeftMargin,coords.Y-ClientTopMargin), out offset);   
                    if (r != null)
                        return r;
                }
            }

            offset = coords;        // no children, so return bounds offset
            return this;
        }


        // given a point x in control relative to bounds, in bitmap space (so not scaled), what is its screen coords
        public Point FindScreenCoords(Point pin, bool clientpos = false)
        {
            if ( clientpos )
            {
                pin.X += ClientLeftMargin;
                pin.Y += ClientTopMargin;
            }

            PointF p = pin;

            GLBaseControl c = this;

            while (c != null)
            {
                if (c.ScaleWindow != null)
                {
                    p.X *= c.ScaleWindow.Value.Width;         // scale down the X/Y offsets by the window scale to get visual scale
                    p.Y *= c.ScaleWindow.Value.Height;
                }

                p.X += c.Left;
                p.Y += c.Top;

                c = c.Parent;

                if ( c != null )
                {
                    p.X += c.ClientLeftMargin;      // these will be scaled on the above scalar when it loops around
                    p.Y += c.ClientTopMargin;
                }

                //System.Diagnostics.Debug.WriteLine($" -> {p} ");
            }

            return new Point((int)p.X, (int)p.Y);
        }

        // what is the scale between this control and the desktop
        public SizeF FindScaler()
        {
            SizeF scale = new SizeF(1, 1);
            GLBaseControl p = this;
            while (p != null)
            {
                if (p.ScaleWindow != null)
                {
                    scale = new SizeF(scale.Width * p.ScaleWindow.Value.Width, scale.Height * p.ScaleWindow.Value.Height);
                }
                p = p.Parent;
            }
            return scale;
        }

        // Set multiple items at once.  Default is to invalidate it
        public void Set(Point? location = null,
                   Size? size = null,           // size in bounds or clientsize
                   Size? clientsize = null,
                   Margin? margin = null,
                   Padding? padding = null,
                   int? borderwidth = null,
                   bool clipsizetobounds = false,
                   bool invalidate = true)
        {
            Point oldloc = Location;
            Size oldsize = Size;

            if (clipsizetobounds)
            {
                size = new Size(Math.Min(Width, size.Value.Width), Math.Min(Height, size.Value.Height));
            }

            if (margin != null)
                this.margin = margin.Value;
            if (padding != null)
                this.padding = padding.Value;
            if (borderwidth != null)
                this.borderwidth = borderwidth.Value;
            if (location.HasValue)
                window.Location = location.Value;
            if (size.HasValue)
                window.Size = size.Value;
            else if (clientsize.HasValue)
                window.Size = new Size(clientsize.Value.Width + ClientWidthMargin, clientsize.Value.Height + ClientHeightMargin);

            CalcClientRectangle();

            if (window.Location != oldloc)
                OnMoved();

            if (oldsize != window.Size)
                OnResize();

            if (invalidate)
                Parent?.InvalidateLayout();
        }

        #endregion

        #region For Inheritors

        protected GLBaseControl(string name, Rectangle location)
        {
            this.Name = name;

            if (location.Width == 0 || location.Height == 0)
            {
                location.Width = location.Height = 10;  // nominal
                AutoSize = true;
            }

            this.window = location;
            CalcClientRectangle();
        }

        static protected readonly Rectangle DefaultWindowRectangle = new Rectangle(0, 0, 10, 10);
        static protected readonly int MinimumResizeWidth = 10;
        static protected readonly int MinimumResizeHeight = 10;

        // these change without invalidation or layout - for constructors of inheritors or for Layout/SizeControl overrides

        protected Color BorderColorNI { set { bordercolor = value; } }
        protected Color BackColorNI { set { backcolor = value; } }
        protected bool VisibleNI { set { visible = value; } }
        public void SetNI(Point? location = null, Size? size = null, Size? clientsize = null, Margin? margin = null, Padding? padding = null,
                            int? borderwidth = null, bool clipsizetobounds = false)
        {
            Set(location, size, clientsize, margin, padding, borderwidth, clipsizetobounds, false);
        }

        protected virtual void RemoveControl(GLBaseControl child, bool dispose, bool removechildren)        // recursively go thru children, bottom child first, and remove everything 
        {
            if (removechildren)
            {
                foreach (var cc in child.childrenz)     // do children of child first
                {
                    RemoveControl(cc, dispose, removechildren);
                }
            }

            child.OnControlRemove(this, child);
            OnControlRemove(this, child);
            //System.Diagnostics.Debug.WriteLine("Remove {0} {1}", child.GetType().Name, child.Name);
            FindDisplay()?.ControlRemoved(child);   // display may be pointing to it

            if (dispose)
                child.Dispose();

            child.parent = child.creator = null;

            childrenz.Remove(child);
            childreniz.Remove(child);
            CheckZOrder();
        }

        public void MakeLevelBitmap(int width , int height)     // top level controls, bitmap for
        {
            levelbmp?.Dispose();
            levelbmp = null;
            if (width > 0 && height > 0)
                levelbmp = new Bitmap(width, height);
        }

        public void Animate(ulong ts)
        {
            if (Visible)
            {
                foreach (var c in ControlsIZ)
                    c.Animate(ts);
                foreach (var a in Animators)
                    a.Animate(this, ts);
            }
        }

        #endregion

        #region Overridables

        // first,perform recursive sizing. 
        // pass in the parent size of client rectangle to each size to give them a hint what they can autosize into

        protected virtual void PerformRecursiveSize(Size parentclientrect)   
        {
            //System.Diagnostics.Debug.WriteLine("Size " + Name + " against " + parentclientrect);
            SizeControl(parentclientrect);              // size ourselves against the parent

            foreach (var c in childrenz) // in Z order
            {
                if (c.Visible)      // invisible children don't layout
                {
                    c.PerformRecursiveSize(ClientSize);
                }
            }

            SizeControlPostChild(parentclientrect);     // if you care what size your children is, do it here
        }

        // override to auto size before children. 
        // Only use the NI functions to change size. 
        protected virtual void SizeControl(Size parentclientrect)
        {
            //System.Diagnostics.Debug.WriteLine("Size " + Name + " area est is " + parentclientrect);
        }

        // override to auto size after the children sized themselves.
        protected virtual void SizeControlPostChild(Size parentclientrect)
        {
            //System.Diagnostics.Debug.WriteLine("Post Size " + Name + " area est is " + parentclientrect);
        }

        // second, layout after sizing, layout children.  We are layedout by parent, and lay out our children inside our client rectangle

        protected virtual void PerformRecursiveLayout()     // Layout all the children, and their dependents 
        {
            //System.Diagnostics.Debug.WriteLine("Laying out " + Name);
            Rectangle area = ClientRectangle;

            foreach (var c in childrenz)     // in z order, top gets first go
            {
                if (c.Visible)      // invisible children don't layout
                {
                    c.Layout(ref area);
                    c.PerformRecursiveLayout();
                }
            }

            //System.Diagnostics.Debug.WriteLine("Finished Laying out " + Name);

            //if (suspendLayoutSet)  System.Diagnostics.Debug.WriteLine("Removing suspend on " + Name);

            ClearLayoutFlags();
        }

        public void ClearLayoutFlags()
        { 
            suspendLayoutCount = 0;   // we can't be suspended
            needLayout = false;     // we have layed out
        }

        // standard layout function, layout yourself inside the area, return area left.
        public virtual void Layout(ref Rectangle parentarea)     
        {
            //System.Diagnostics.Debug.WriteLine("Control " + Name + " " + window + " " + Dock);
            int dockedwidth = DockPercent > 0 ? ((int)(parentarea.Width * DockPercent)) : (window.Width);
            int dockedheight = DockPercent > 0 ? ((int)(parentarea.Height * DockPercent)) : (window.Height);
            int wl = Width;
            int hl = Height;

            Rectangle oldwindow = window;
            Rectangle areaout = parentarea;

            if (docktype == DockingType.Fill)
            {
                window = parentarea;
                areaout = new Rectangle(0, 0, 0, 0);
            }
            else if (docktype == DockingType.Center)
            {
                int xcentre = (parentarea.Left + parentarea.Right) / 2;
                int ycentre = (parentarea.Top + parentarea.Bottom) / 2;
                Width = Math.Min(parentarea.Width, Width);
                Height = Math.Min(parentarea.Height, Height);
                window = new Rectangle(xcentre - Width / 2, ycentre - Height / 2, Width, Height);       // centre in area, bounded by area, no change in area in
            }
            else if (docktype == DockingType.None)
            {
            }
            else if (docktype >= DockingType.Bottom)
            {
                if (docktype == DockingType.Bottom)     // only if we just the whole of the bottom do we modify areaout
                {
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Bottom - dockedheight - dockingmargin.Bottom, parentarea.Width - dockingmargin.TotalWidth, dockedheight);
                    areaout = new Rectangle(parentarea.Left, parentarea.Top, parentarea.Width, parentarea.Height - dockedheight - dockingmargin.TotalWidth);
                }
                else if (docktype == DockingType.BottomCentre)
                    window = new Rectangle(parentarea.Left + parentarea.Width / 2 - wl / 2, parentarea.Bottom - dockedheight - dockingmargin.Bottom, wl, dockedheight);
                else if (docktype == DockingType.BottomLeft)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Bottom - dockedheight - dockingmargin.Bottom, wl, dockedheight);
                else // bottomright
                    window = new Rectangle(parentarea.Right - dockingmargin.Right - wl, parentarea.Bottom - dockedheight - dockingmargin.Bottom, wl, dockedheight);
            }
            else if (docktype >= DockingType.Top)
            {
                if (docktype == DockingType.Top)
                {
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, parentarea.Width - dockingmargin.TotalWidth, dockedheight);
                    areaout = new Rectangle(parentarea.Left, parentarea.Top + dockedheight + dockingmargin.TotalHeight, parentarea.Width, parentarea.Height - dockedheight - dockingmargin.TotalHeight);
                }
                else if (docktype == DockingType.TopCenter)
                    window = new Rectangle(parentarea.Left + parentarea.Width / 2 - wl / 2, parentarea.Top + dockingmargin.Top, wl, dockedheight);
                else if (docktype == DockingType.TopLeft)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, wl, dockedheight);
                else // topright
                    window = new Rectangle(parentarea.Right - dockingmargin.Right - wl, parentarea.Top + dockingmargin.Top, wl, dockedheight);
            }
            else if (docktype >= DockingType.Right)
            {
                if (docktype == DockingType.Right)
                {
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Top + dockingmargin.Top, dockedwidth, parentarea.Height - dockingmargin.TotalHeight);
                    areaout = new Rectangle(parentarea.Left, parentarea.Top, parentarea.Width - window.Width - dockingmargin.TotalWidth, parentarea.Height);
                }
                else if (docktype == DockingType.RightCenter)
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Top + parentarea.Height / 2 - hl / 2, dockedwidth, hl);
                else if (docktype == DockingType.RightTop)
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Top + dockingmargin.Top, dockedwidth, hl);
                else // rightbottom
                    window = new Rectangle(parentarea.Right - dockedwidth - dockingmargin.Right, parentarea.Bottom - dockingmargin.Bottom - hl, dockedwidth, hl);
            }
            else // must be left!
            {
                if (docktype == DockingType.Left)
                {
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, dockedwidth, parentarea.Height - dockingmargin.TotalHeight);
                    areaout = new Rectangle(parentarea.Left + dockedwidth + dockingmargin.TotalWidth, parentarea.Top, parentarea.Width - dockedwidth - dockingmargin.TotalWidth, parentarea.Height);
                }
                else if (docktype == DockingType.LeftCenter)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + parentarea.Height / 2 - hl / 2, dockedwidth, hl);
                else if (docktype == DockingType.LeftTop)
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Top + dockingmargin.Top, dockedwidth, hl);
                else  // leftbottom
                    window = new Rectangle(parentarea.Left + dockingmargin.Left, parentarea.Bottom - dockingmargin.Bottom - hl, dockedwidth, hl);
            }

            CalcClientRectangle(); // ensure client rectangle tracks window

            //System.Diagnostics.Debug.WriteLine("{0} dock {1} win {2} Area in {3} Area out {4}", Name, Dock, window, parentarea, areaout);

            CheckBitmapAfterLayout();       // check bitmap, virtual as inheritors may need to override this, make sure bitmap is the same width/height as ours
                                            // needs to be done in layout as ControlDisplay::PerformRecursiveLayout sets the textures up to match.

            parentarea = areaout;
        }

        // Override if required if you run a bitmap. Standard actions is to replace it if width/height is different.

        protected virtual void CheckBitmapAfterLayout()
        {
            if (levelbmp != null && ( levelbmp.Width != Width || levelbmp.Height != Height ))
            {
                //System.Diagnostics.Debug.WriteLine("Remake bitmap for " + Name);
                levelbmp.Dispose();
                levelbmp = new Bitmap(Width, Height);       // occurs for controls directly under form
            }
        }

        // gr = null at start, else gr used by parent
        // bounds = area that our control occupies on the bitmap, in bitmap co-ords. This may be outside of the clip area below if the child is outside of the client area of its parent control
        // cliparea = area that we can draw into, in bitmap co-ords, so we don't exceed the bounds of any parent clip areas above us. clipareas are continually narrowed
        // we must be visible to be called. Children may not be visible

        public virtual bool Redraw(Graphics parentgr, Rectangle bounds, Rectangle cliparea, bool forceredraw)
        {
            Rectangle parentarea = bounds;                      // remember the bounds passed

            Graphics gr = parentgr;                             // we normally use the parent gr

            if (levelbmp != null)                               // bitmap on this level, use it for itself and its children
            {
                cliparea = bounds = new Rectangle(0, 0, levelbmp.Width, levelbmp.Height);      // restate area in terms of bitmap, this is the bounds and the clip area

                gr = Graphics.FromImage(levelbmp);              // get graphics for it
                gr.SmoothingMode = System.Drawing.Drawing2D.SmoothingMode.None;
                gr.TextRenderingHint = System.Drawing.Text.TextRenderingHint.ClearTypeGridFit;
            }

            System.Diagnostics.Debug.Assert(gr != null);        // must normally be set, as bitmaps are created for controls under display

            bool redrawn = false;

            if (NeedRedraw || forceredraw)          // if we need a redraw, or we are forced to draw by a parent redrawing above us.
            {
                //System.Diagnostics.Debug.WriteLine("redraw {0}->{1} Bounds {2} clip {3} client {4} ({5},{6},{7},{8}) nr {9} fr {10}", Parent?.Name, Name, bounds, cliparea, ClientRectangle, ClientLeftMargin, ClientTopMargin, ClientRightMargin, ClientBottomMargin, NeedRedraw, forceredraw);

                forceredraw = true;             // all children, force redraw      
                NeedRedraw = false;             // we have been redrawn
                redrawn = true;                 // and signal up we have been redrawn

                gr.SetClip(cliparea);   // set graphics to the clip area so we can draw the background/border
                gr.TranslateTransform(bounds.X, bounds.Y);   // move to client 0,0

                //System.Diagnostics.Debug.WriteLine("..PaintBack {0} in ca {1} clip {2}", Name, bounds, cliparea);
                DrawBack(new Rectangle(0,0,Width,Height), gr, BackColor, BackColorGradientAlt, BackColorGradientDir);

                DrawBorder(gr, BorderColor, BorderWidth);
                gr.ResetTransform();
            }

            // client area, in terms of last bitmap
            Rectangle clientarea = new Rectangle(bounds.Left + ClientLeftMargin, bounds.Top + ClientTopMargin, ClientWidth, ClientHeight);

            foreach( var c in childreniz)       // in inverse Z order, last is top Z
            {
                if (c.Visible)
                {
                    Rectangle childbounds = new Rectangle(clientarea.Left + c.Left,     // not bounded by clip area, in bitmap coords
                                                          clientarea.Top + c.Top,
                                                          c.Width,
                                                          c.Height);

                    // clip area is progressively narrowed as we go down the children
                    // its the minimum of the previous clip area
                    // the child bounds
                    // and the client rectangle
 
                    int cleft = Math.Max(childbounds.Left, cliparea.Left);          // clipped to child left or cliparea left
                    cleft = Math.Max(cleft, bounds.Left + this.ClientLeftMargin);
                    int ctop = Math.Max(childbounds.Top, cliparea.Top);             // clipped to child top or cliparea top
                    ctop = Math.Max(ctop, bounds.Top + this.ClientTopMargin);
                    int cright = Math.Min(childbounds.Left + c.Width, cliparea.Right);  // clipped to child left+width or the cliparea right
                    cright = Math.Min(cright, bounds.Right - this.ClientRightMargin);     // additionally clipped to our bounds right less its client margin
                    int cbot = Math.Min(childbounds.Top + c.Height, cliparea.Bottom);   // clipped to child bottom or cliparea bottom
                    cbot = Math.Min(cbot, bounds.Bottom - this.ClientBottomMargin);       // additionally clipped to bounds bottom less its client margin

                    Rectangle childcliparea = new Rectangle(cleft, ctop, cright - cleft, cbot - ctop);  // clip area to pass down in bitmap coords

                    redrawn |= c.Redraw(gr, childbounds, childcliparea, forceredraw);   // draw, into current gr
                }
            }

            if ( forceredraw)       // will be set if NeedRedrawn or forceredrawn
            {
                //System.Diagnostics.Debug.WriteLine("..Paint {0} in ca {1} clip {2}", Name, clientarea, cliparea);
                gr.SetClip(cliparea);   // set graphics to the clip area

                gr.TranslateTransform(clientarea.X, clientarea.Y);   // move to client 0,0

                Paint(gr);

                gr.ResetTransform();

                if (parentgr != null && levelbmp != null)  // have a parent gr, and we have our own level bmp, we may be a scrollable panel
                {
                    parentgr.SetClip(parentarea);       // must set the clip area again to address the parent area      
                    PaintIntoParent(parentarea, parentgr);      // give it a chance to draw our bitmap into the parent bitmap
                }
            }

            if (levelbmp != null)        // bitmap on this level, we made a GR, dispose
                gr.Dispose();

            return redrawn;
        }

        // draw border area, override to draw something different
        protected virtual void DrawBorder(Graphics gr, Color bc, float bw)
        {
            if (bw > 0)
            {
                Rectangle rectarea = new Rectangle(Margin.Left,
                                                Margin.Top,
                                                Width - Margin.TotalWidth - 1,
                                                Height - Margin.TotalHeight - 1);

                using (var p = new Pen(bc, bw))
                {
                    gr.DrawRectangle(p, rectarea);
                }
            }
        }

        // draw back area - override to paint something different
        protected virtual void DrawBack(Rectangle area, Graphics gr, Color bc, Color bcgradientalt, int bcgradientdir)
        {
            if ( levelbmp != null)                  // if we own a bitmap, reset back to transparent, erasing anything that we drew before
                gr.Clear(Color.Transparent);       

            if (bc != Color.Transparent)            // and draw what the back colour is
            {
                if ( levelbmp == null )             // if we are a normal control, we need to start from the pixels inside us being transparent
                    gr.Clear(Color.Transparent);    // erasing anything that we drew before, because if we have half alpha in the colour, it will build up

                if (bcgradientdir != int.MinValue)
                {
                    //System.Diagnostics.Debug.WriteLine("Background " + Name +  " " + bounds + " " + bc + " -> " + bcgradientalt );
                    using (var b = new System.Drawing.Drawing2D.LinearGradientBrush(area, bc, bcgradientalt, bcgradientdir))
                        gr.FillRectangle(b, area);       // linear grad brushes do not respect smoothing mode, btw
                }
                else
                {
                    //System.Diagnostics.Debug.WriteLine("Background " + Name + " " + bounds + " " + backcolor);
                    using (Brush b = new SolidBrush(bc))     // always fill, so we get back to start
                        gr.FillRectangle(b, area);
                }
            }
        }

        protected virtual void Paint(Graphics gr)                   // normal override
        {
            //System.Diagnostics.Debug.WriteLine("Paint {0}", Name);
        }

        protected virtual void PaintIntoParent(Rectangle parentarea, Graphics parentgr) // only called if you've defined a bitmap yourself, 
        {                                                                        // gives you a chance to paint to the parent bitmap
           // System.Diagnostics.Debug.WriteLine("Paint Into parent {0} to {1}", Name, parentarea);
        }

        #endregion

        #region UI Overrides

        protected  virtual void OnMouseLeave(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("leave " + Name + " " + e.Location);
            MouseLeave?.Invoke(this, e);

            if (InvalidateOnEnterLeave)
                Invalidate();
        }

        protected virtual void OnMouseEnter(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("enter " + Name + " " + e.Location + " " + InvalidateOnEnterLeave);
            MouseEnter?.Invoke(this, e);

            if (InvalidateOnEnterLeave)
                Invalidate();
        }

        protected  virtual void OnMouseUp(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("up   " + Name + " " + e.Location + " " + e.Button);
            MouseUp?.Invoke(this, e);

            if (InvalidateOnMouseDownUp)
                Invalidate();
        }

        protected  virtual void OnMouseDown(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("down " + Name + " " + e.Location + " " + e.Button + " " + MouseButtonsDown);
            MouseDown?.Invoke(this, e);

            if (InvalidateOnMouseDownUp)
            {
                Invalidate();
            }
        }

        protected  virtual void OnMouseClick(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("click " + Name + " " + e.Button + " " + e.Clicks + " " + e.Location);
            MouseClick?.Invoke(this, e);
        }

        protected  virtual void OnMouseDoubleClick(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("doubleclick " + Name + " " + e.Button + " " + e.Clicks + " " + e.Location);
            MouseDoubleClick?.Invoke(this, e);
        }

        protected  virtual void OnMouseMove(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Over " + Name + " " + e.Location);
            MouseMove?.Invoke(this, e);

            if (InvalidateOnMouseMove)
                Invalidate();
        }

        protected  virtual void OnMouseWheel(GLMouseEventArgs e)
        {
            //System.Diagnostics.Debug.WriteLine("Over " + Name + " " + e.Location);
            MouseWheel?.Invoke(this, e);
        }

        protected  virtual void OnKeyDown(GLKeyEventArgs e)     // GLForm above control gets this as well, and can cancel call to control by handling it
        {
            KeyDown?.Invoke(this, e);
        }

        protected  virtual void OnKeyUp(GLKeyEventArgs e)       // GLForm above control gets this as well, and can cancel call to control by handling it
        {
            KeyUp?.Invoke(this, e);
        }

        protected  virtual void OnKeyPress(GLKeyEventArgs e)    // GLForm above control gets this as well, and can cancel call to control by handling it
        {
            KeyPress?.Invoke(this, e);
        }

        protected  virtual void OnFocusChanged(FocusEvent focused, GLBaseControl ctrl)  // focused elements or parents up to GLForm gets this as well
        {
            this.focused = focused == FocusEvent.Focused;
            if (InvalidateOnFocusChange)
                Invalidate();
            FocusChanged?.Invoke(this, focused, ctrl);
        }

        protected  virtual void OnGlobalFocusChanged(GLBaseControl from, GLBaseControl to) // everyone gets this
        {
            GlobalFocusChanged?.Invoke(from, to);
            List<GLBaseControl> list = new List<GLBaseControl>(ControlsZ); // copy of, in case the caller closes something
            foreach (var c in list)
                c.OnGlobalFocusChanged(from, to);
        }

        protected virtual void OnGlobalMouseClick(GLBaseControl ctrl, GLMouseEventArgs e) // everyone gets this
        {
            //System.Diagnostics.Debug.WriteLine("In " + Name + " Global click in " + ctrl.Name);
            GlobalMouseClick?.Invoke(ctrl, e);
            List<GLBaseControl> list = new List<GLBaseControl>(ControlsZ); // copy of, in case the caller closes something
            foreach (var c in list)
                c.OnGlobalMouseClick(ctrl, e);
        }

        protected virtual void OnGlobalMouseDown(GLBaseControl ctrl, GLMouseEventArgs e) // everyone gets this
        {
            //System.Diagnostics.Debug.WriteLine("In " + Name + " Global click in " + ctrl.Name);
            GlobalMouseDown?.Invoke(ctrl, e);
            List<GLBaseControl> list = new List<GLBaseControl>(ControlsZ); // copy of, in case the caller closes something
            foreach (var c in list)
                c.OnGlobalMouseDown(ctrl, e);
        }

        protected virtual void OnFontChanged()
        {
            FontChanged?.Invoke(this);
        }

        protected  virtual void OnResize()
        {
            Resize?.Invoke(this);
        }

        protected  virtual void OnMoved()
        {
            Moved?.Invoke(this);
        }

        protected  virtual void OnControlAdd(GLBaseControl parent, GLBaseControl child)     // fired to both the parent and child
        {
            ControlAdd?.Invoke(parent, child);
        }

        protected  virtual void OnControlRemove(GLBaseControl parent, GLBaseControl ctrlbeingremoved) // fired to both the parent and child
        {
            ControlRemove?.Invoke(parent, ctrlbeingremoved);
        }

        #endregion


        #region Implementation

        // Set Position, causing an invalidation layout at parent level

        private void SetPos(int left, int top, int width, int height)
        {
            Rectangle w = new Rectangle(left, top, width, height);

            if (w != window)        // if changed
            {
                bool resized = w.Size != window.Size;
                bool moved = w.Location != window.Location;

                window = w;

                if (resized)
                    CalcClientRectangle();

                if (moved)
                    OnMoved();

                if (resized)
                    OnResize();

                if (resized || (Parent?.InvalidateDueToLocationChange(this) ?? true) == true)   // if resized, or we invalidate due to location change
                {
                    NeedRedraw = true;      // we need a redraw
                                            // System.Diagnostics.Debug.WriteLine("setpos need redraw on " + Name);
                    parent?.Invalidate();   // parent is invalidated as well, and the whole form needs reendering
                    parent?.PerformLayout();     // go up one and perform layout on all its children, since we are part of it.
                }
            }
        }

        // normally a location changed (left,top) means a invalidate of parent and re-layout. But for top level windows under GLDisplayControl
        // we don't need to lay them out as they are top level GL objects and we just need to move the texture co-ords
        // this bit does that - allows the top level parent to not have to invalidate if it returns false. Default is true, must invalidate
        protected virtual bool InvalidateDueToLocationChange(GLBaseControl child)
        {
            return true;
        }

        private void CalcClientRectangle()       // client rectangle calc
        {
            ClientRectangle = new Rectangle(0, 0, Width - Margin.TotalWidth - Padding.TotalWidth - BorderWidth * 2, Height - Margin.TotalHeight - Padding.TotalHeight - BorderWidth * 2);
        }

        private void SetEnabled(bool v)
        {
            enabled = v;
            foreach (var c in childrenz)
                SetEnabled(v);
        }

        private void SetFont(Font f)
        {
            font = f;
            PropergateFontChanged(this);
        }

        private void PropergateFontChanged(GLBaseControl p)
        {
            p.OnFontChanged();
            foreach (var c in p.childrenz)
            {
                if (c.font == null)     // if child does not override font..
                    PropergateFontChanged(c);
            }
        }

        [System.Diagnostics.Conditional("DEBUG")]
        private void CheckZOrder()
        {
            int pos = childreniz.Count - 1;
            foreach (var c in childrenz)
            {
                System.Diagnostics.Debug.Assert(c == childreniz[pos--]);
            }
        }

        private void ClearFlagsDown()       // ensure this and its children have all flags cleared to default
        {
            Hover = false;
            focused = false;
            MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;
            foreach (var c in ControlsZ)
                c.ClearFlagsDown();
        }

        public virtual void Dispose()
        {
            levelbmp?.Dispose();
            levelbmp = null;
        }

        public void DumpTrees(int l, GLBaseControl prev)
        {
            string prefix = "                           ".Substring(0, l);
            System.Diagnostics.Debug.WriteLine("{0}{1} {2}", prefix, Name, Parent == prev ? "OK" : "Not linked");
            if (ControlsZ.Count > 0)
            {
                foreach (var c in ControlsZ)
                    c.DumpTrees(l + 2, this);
            }
        }

        protected bool NeedRedraw { get; set; } = true;         // we need to redraw, therefore all children also redraw

        private Bitmap levelbmp;       // set if the level has a new bitmap.  Controls under Form always does. Other ones may if they scroll
        private Font font = null;
        private Rectangle window;       // total area owned, in parent co-ords
        private bool needLayout { get; set; } = false;        // need a layout after suspend layout was called
        private int suspendLayoutCount { get; set; } = 0;        // suspend layout is on
        private bool enabled { get; set; } = true;
        private bool visible { get; set; } = true;
        private DockingType docktype { get; set; } = DockingType.None;
        private float dockpercent { get; set; } = 0;
        private Color backcolor { get; set; } = DefaultControlBackColor;
        private Color backcolorgradientalt { get; set; } = DefaultControlBackColor;
        private int backcolorgradientdir { get; set; } = int.MinValue;           // in degrees
        private Color bordercolor { get; set; } = Color.Transparent;         // Margin - border - padding is common to all controls. Area left is control area to draw in
        private int borderwidth { get; set; } = 0;
        private GL4.Controls.Padding padding { get; set; }
        private GL4.Controls.Margin margin { get; set; }
        private GL4.Controls.Margin dockingmargin { get; set; }
        private bool autosize { get; set; }
        private int column { get; set; } = 0;     // for table layouts
        private int row { get; set; } = 0;        // for table layouts
        private bool focused { get; set; } = false;
        private bool focusable { get; set; } = false;       // if true, clicking on it gets focus.  If not true, clincking on it set focus to null, unless next is set
        private bool rejectfocus { get; set; } = false;     // if true, clicking on it does nothing to focus.
        private bool givefocustoparent { get; set; } = false;     // if true, clicking on it tries to focus parent
        private bool topMost { get; set; } = false;              // if set, always force to top

        private GLBaseControl parent { get; set; } = null;       // its parent, or null if not connected or GLDisplayControl
        private GLBaseControl creator { get; set; } = null;       // its creator, normally its parent.

        private List<GLBaseControl> childrenz = new List<GLBaseControl>();
        private List<GLBaseControl> childreniz = new List<GLBaseControl>();

        #endregion

        #region Interface to GLWindowControl

        // used by GLControlDisplay only, Lower controls do not use these functions
        // here so it can call protected members of this class.  

        private GLBaseControl currentmouseover = null;              
        private GLBaseControl currentfocus = null;                  
        private GLBaseControl mousedowninitialcontrol = null;       // track where mouse down occurred

        private bool SetFocus(GLBaseControl newfocus)    // null to clear focus, true if focus taken
        {
            if (newfocus == currentfocus)       // no action if the same
                return true;

            if (newfocus != null)
            {
                if (newfocus.GiveFocusToParent && newfocus.Parent != null && newfocus.Parent.RejectFocus == false)
                    newfocus = newfocus.Parent;     // see if we want to give it to parent

                if (newfocus.RejectFocus)       // if reject focus change when clicked, abort, do not change focus
                    return false;

                if (!newfocus.Enabled || !newfocus.Focusable)       // if its not enabled or not focusable, change to no focus
                {
                    System.Diagnostics.Debug.WriteLine("Focus target not enabled/focusable " + newfocus.Name);
                    newfocus = null;
                }
            }

            GLBaseControl oldfocus = currentfocus;

            OnGlobalFocusChanged(oldfocus, newfocus);

            //            System.Diagnostics.Debug.WriteLine("Focus changed from '{0}' to '{1}' {2}", oldfocus?.Name, newfocus?.Name, Environment.StackTrace);

            if (currentfocus != null)           // if we have a focus, inform losing it, and cancel it
            {
                currentfocus.OnFocusChanged(FocusEvent.Deactive, newfocus);

                for (var c = currentfocus.Parent; c != null; c = c.Parent)      // inform change up and including the GLForm
                {
                    c.OnFocusChanged(FocusEvent.ChildDeactive, newfocus);
                    if (c is GLForm)
                        break;
                }

                currentfocus = null;
            }

            if (newfocus != null)               // if we have a new focus, set and tell it
            {
                currentfocus = newfocus;

                currentfocus.OnFocusChanged(FocusEvent.Focused, oldfocus);

                for (var c = currentfocus.Parent; c != null; c = c.Parent)      // inform change up and including the GLForm
                {
                    c.OnFocusChanged(FocusEvent.ChildFocused, currentfocus);
                    if (c is GLForm)
                        break;
                }
            }

            return true;
        }

        protected void ControlRemoved(GLBaseControl other)     // called on ControlDisplay, to inform it that a control has been removed
        {
            if (currentfocus == other)
                currentfocus = null;
            if (currentmouseover == other)
                currentmouseover = null;
        }

        protected void Gc_MouseLeave(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null)
            {
                currentmouseover.MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;
                currentmouseover.Hover = false;

                var mouseleaveev = new GLMouseEventArgs(e.WindowLocation);
                SetViewScreenCoord(ref e);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseLeave(mouseleaveev);

                currentmouseover = null;
            }
        }

        protected void Gc_MouseEnter(object sender, GLMouseEventArgs e)
        {
            Gc_MouseLeave(sender, e);       // leave current

            SetViewScreenCoord(ref e);

            currentmouseover = FindControlOver(e.ScreenCoord, out Point leftover);

            if (currentmouseover != null)
            {
                currentmouseover.Hover = true;

                SetControlLocation(ref e, currentmouseover, leftover);

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseEnter(e);
            }
        }

        protected void Gc_MouseDown(object sender, GLMouseEventArgs e)
        {
            // System.Diagnostics.Debug.WriteLine("GC Mouse down");
            if (currentmouseover != null)
            {
                currentmouseover.FindControlUnderDisplay()?.BringToFront();     // this brings to the front of the z-order the top level element holding this element and makes it visible.

                SetViewScreenCoord(ref e);
                SetControlLocation(ref e, currentmouseover);

                OnGlobalMouseDown(currentmouseover, e);

                if (currentmouseover.Enabled)
                {
                    currentmouseover.MouseButtonsDown = e.Button;
                    currentmouseover.OnMouseDown(e);
                }

                mousedowninitialcontrol = currentmouseover;
            }
            else
            {
                OnGlobalMouseDown(null, e);

                if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                    this.OnMouseDown(e);
            }
        }

        protected void Gc_MouseMove(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);
            //System.Diagnostics.Debug.WriteLine("WLoc {0} VP {1} SLoc {2}", e.WindowLocation, e.ViewportLocation, e.ScreenCoord);

            GLBaseControl c = FindControlOver(e.ScreenCoord, out Point leftover); // overcontrol ,or over display, or maybe outside display

            if (c != currentmouseover)      // if different, either going active or inactive
            {
                //System.Diagnostics.Debug.WriteLine("WLoc {0} VP {1} SLoc {2} from {3} to {4}, rel {5}", e.WindowLocation, e.ViewportLocation, e.ScreenCoord, currentmouseover?.Name, c?.Name, leftover);
                mousedowninitialcontrol = null;

                if (currentmouseover != null)   // for current, its a leave or its a drag..
                {
                    SetControlLocation(ref e, currentmouseover, leftover);

                    if (currentmouseover.MouseButtonsDown != GLMouseEventArgs.MouseButtons.None)   // click and drag, can't change control while mouse is down
                    {
                        GlobalMouseMove?.Invoke(e);     // we move, with the currentmouseover

                        if (currentmouseover.Enabled)       // and send to control if enabled
                            currentmouseover.OnMouseMove(e);

                        return;
                    }

                    currentmouseover.Hover = false;     // we are leaving this one

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseLeave(e);
                }

                currentmouseover = c;   // change to new value

                if (currentmouseover != null)       // now, are we going over a new one?
                {
                    SetControlLocation(ref e, currentmouseover, leftover);    // reset location etc

                    currentmouseover.Hover = true;

                    GlobalMouseMove?.Invoke(e);     // we move, with the new currentmouseover

                    if (currentmouseover.Enabled)       // and send to control if enabled
                        currentmouseover.OnMouseEnter(e);
                }
                else
                {
                    GlobalMouseMove?.Invoke(e);     // we move, with no mouse over

                    if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                        this.OnMouseMove(e);
                }
            }
            else
            {
                if (currentmouseover != null)
                {
                    SetControlLocation(ref e, currentmouseover, leftover);    // reset location etc

                    GlobalMouseMove?.Invoke(e);     // we move, with the new currentmouseover

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseMove(e);
                }
                else
                {
                    GlobalMouseMove?.Invoke(e);     // we move, with no mouse over

                    if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                        this.OnMouseMove(e);
                }
            }
        }


        protected void Gc_MouseUp(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (currentmouseover != null)
            {
                currentmouseover.MouseButtonsDown = GLMouseEventArgs.MouseButtons.None;

                SetControlLocation(ref e, currentmouseover);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseUp(e);
            }
            else
            {
                if (this.Enabled)               // not over any control (due to screen coord clip space), so send thru the displaycontrol
                    this.OnMouseUp(e);
            }

            mousedowninitialcontrol = null;
        }

        protected void Gc_MouseClick(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (mousedowninitialcontrol == currentmouseover && currentmouseover != null)        // clicks only occur if mouse is still over initial control
            {
                e.WasFocusedAtClick = currentmouseover == currentfocus;         // record if clicking on a focused item

                SetFocus(currentmouseover);

                if (currentmouseover != null)     // set focus could have force a loss, thru the global focus hook
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    OnGlobalMouseClick(currentmouseover, e);

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseClick(e);
               }
            }
            else if (currentmouseover == null)        // not over any control, even control display, but still click, (due to screen coord clip space), so send thru the displaycontrol
            {
                SetFocus(null);
                OnGlobalMouseClick(null, e);
                this.OnMouseClick(e);
            }
        }

        protected void Gc_MouseDoubleClick(object sender, GLMouseEventArgs e)
        {
            SetViewScreenCoord(ref e);

            if (mousedowninitialcontrol == currentmouseover && currentmouseover != null)        // clicks only occur if mouse is still over initial control
            {
                e.WasFocusedAtClick = currentmouseover == currentfocus;         // record if clicking on a focused item

                SetFocus(currentmouseover);

                if (currentmouseover != null)     // set focus could have force a loss, thru the global focus hook
                {
                    SetControlLocation(ref e, currentmouseover);    // reset location etc

                    if (currentmouseover.Enabled)
                        currentmouseover.OnMouseDoubleClick(e);
                }
            }
            else if (currentmouseover == null)        // not over any control, even control display, but still click, (due to screen coord clip space), so send thru the displaycontrol
            {
                SetFocus(null);

                if (this.Enabled)
                    this.OnMouseDoubleClick(e);
            }
        }

        protected void Gc_MouseWheel(object sender, GLMouseEventArgs e)
        {
            if (currentmouseover != null && currentmouseover.Enabled)
            {
                SetViewScreenCoord(ref e);
                SetControlLocation(ref e, currentmouseover);    // reset location etc

                if (currentmouseover.Enabled)
                    currentmouseover.OnMouseWheel(e);
            }
        }

        // overriden by GLControlDisplay. Translate WindowsLocation into ViewPortLocation and ScreenCoord
        protected virtual void SetViewScreenCoord(ref GLMouseEventArgs e)       // overridden in control class to provide co-ords
        {
        }

        // pos if passed is offset into bounds of control 
        private void SetControlLocation(ref GLMouseEventArgs e, GLBaseControl cur, Point? reltobounds = null)
        {
            Point reloffset;
            if (reltobounds == null)
            {
                var found = FindControlOver(e.ScreenCoord, out reloffset);      // if we have not computed it, compute again
            }
            else
                reloffset = reltobounds.Value;

            // record control, bounds, and client location
            e.Control = cur;
            e.BoundsLocation = reloffset;
            e.Location = new Point(reloffset.X-cur.ClientLeftMargin, reloffset.Y-cur.ClientTopMargin);      // translate to client rectangle co-ords

            // determine logical area
            if (e.Location.X < 0)
                e.Area = GLMouseEventArgs.AreaType.Left;
            else if (e.Location.X >= cur.ClientWidth)
            {
                if (e.Location.Y >= cur.ClientHeight)
                    e.Area = GLMouseEventArgs.AreaType.NWSE;
                else
                    e.Area = GLMouseEventArgs.AreaType.Right;
            }
            else if (e.Location.Y < 0)
                e.Area = GLMouseEventArgs.AreaType.Top;
            else if (e.Location.Y >= cur.ClientHeight)
                e.Area = GLMouseEventArgs.AreaType.Bottom;
            else
                e.Area = GLMouseEventArgs.AreaType.Client;

            //System.Diagnostics.Debug.WriteLine($"Pos {e.WindowLocation} VP {e.ViewportLocation} SC {e.ScreenCoord} BL {e.BoundsLocation} loc {e.Location} {e.Area} {cur.Name}");
        }

        protected void Gc_KeyUp(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyUp(e);            // reflect to form

                if (!e.Handled)                                    // send to control
                    currentfocus.OnKeyUp(e);

            }
        }

        protected void Gc_KeyDown(object sender, GLKeyEventArgs e)
        {
            System.Diagnostics.Debug.WriteLine("Control keydown " + e.KeyCode + " on " + currentfocus?.Name);
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyDown(e);          // reflect to form

                if (!e.Handled)                                    // send to control
                    currentfocus.OnKeyDown(e);

            }
        }

        protected void Gc_KeyPress(object sender, GLKeyEventArgs e)
        {
            if (currentfocus != null && currentfocus.Enabled)
            {
                if (!(currentfocus is GLForm))
                    currentfocus.FindForm()?.OnKeyPress(e);         // reflect to form

                if (!e.Handled)
                    currentfocus.OnKeyPress(e);                     // send to control
            }
        }

        #endregion

    }
}
