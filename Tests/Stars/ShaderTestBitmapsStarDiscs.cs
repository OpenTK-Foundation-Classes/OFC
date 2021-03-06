﻿/*
 * Copyright 2019 Robbyxp1 @ github.com
 * Part of the EDDiscovery Project
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

using OFC;
using OFC.Controller;
using OFC.GL4;
using OpenTK;
using OpenTK.Graphics;
using OpenTK.Graphics.OpenGL;
using System;
using System.Drawing;
using System.Windows.Forms;

namespace TestOpenTk
{
    public partial class ShaderTestBitmapsStarDiscs : Form
    {
        private OFC.WinForm.GLWinFormControl glwfc;
        private Controller3D gl3dcontroller;

        private Timer systemtimer = new Timer();

        public ShaderTestBitmapsStarDiscs()
        {
            InitializeComponent();

            glwfc = new OFC.WinForm.GLWinFormControl(glControlContainer);

            systemtimer.Interval = 25;
            systemtimer.Tick += new EventHandler(SystemTick);
            systemtimer.Start();
        }

        GLRenderProgramSortedList rObjects = new GLRenderProgramSortedList();
        GLItemsList items = new GLItemsList();

        // Demonstrate buffer feedback AND geo shader add vertex/dump vertex

        protected override void OnLoad(EventArgs e)
        {
            base.OnLoad(e);

            Closed += ShaderTest_Closed;

            gl3dcontroller = new Controller3D();
            gl3dcontroller.PaintObjects = ControllerDraw;
            gl3dcontroller.ZoomDistance = 20F;
            gl3dcontroller.MatrixCalc.PerspectiveNearZDistance = 0.1f;
            glwfc.BackColor = Color.FromArgb(0, 0, 60);
            gl3dcontroller.Start(glwfc,new Vector3(0, 0, 0), new Vector3(120f, 0, 0f), 1F);

            gl3dcontroller.KeyboardTravelSpeed = (ms,eyedist) =>
            {
                return (float)ms / 20.0f;
            };

            items.Add( new GLMatrixCalcUniformBlock(), "MCUB");     // def binding of 0

            {
                items.Add(new GLColorShaderWithWorldCoord(), "COS");
                GLRenderControl rl = GLRenderControl.Lines(1);

                rObjects.Add(items.Shader("COS"),
                             GLRenderableItem.CreateVector4Color4(items, rl,
                                                        GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(-40, 0, 40), new Vector3(10, 0, 0), 9),
                                                        new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );


                rObjects.Add(items.Shader("COS"),
                             GLRenderableItem.CreateVector4Color4(items, rl,
                                   GLShapeObjectFactory.CreateLines(new Vector3(-40, 0, -40), new Vector3(40, 0, -40), new Vector3(0, 0, 10), 9),
                                                             new Color4[] { Color.Red, Color.Red, Color.Green, Color.Green })
                                   );
            }

            using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
            {
                Size bitmapsize = new Size(64, 20);
                float width = 2.5f;
                Vector3 bannersize = new Vector3(width, 0, 0);
                Font f = new Font("MS sans serif", 8f);

                tim = new GLBitmapsWithStarObjects(rObjects, bitmapsize, new Vector3(0,2,0), 2.0f, 3, false, true, 2);      // group 2
                tim.CurrentGeneration = uint.MaxValue-10;
                items.Add(tim);
                tim.Add("T1", "MFred", f, Color.White, Color.Red, new Vector3(-10, 0, genpos), bannersize, new Vector3(-90F.Radians(), 0, 0), fmt, alphascale: 10, alphaend: 5);
                tim.Add("T2", "MJim", f, Color.White, Color.Red, new Vector3(0, 0, genpos), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true);
                tim.Add("T3", "MGeorge", f, Color.White, Color.Red, new Vector3(10, 0, genpos), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true, rotateelevation: true);
            }

            OFC.GLStatics.Check();

        }

        GLBitmapsWithStarObjects tim;
        int genpos = 0;
        uint oldestrelgen = 0;

        private void ShaderTest_Closed(object sender, EventArgs e)
        {
            items.Dispose();
        }

        private void ControllerDraw(GLMatrixCalc mc, long time)
        {
            // System.Diagnostics.Debug.WriteLine("Draw eye " + gl3dcontroller.MatrixCalc.EyePosition + " to " + gl3dcontroller.Pos.Current);

            float zeroone10000s = ((float)(time % 10000000)) / 10000000.0f;
            float zeroone5000s = ((float)(time % 5000000)) / 5000000.0f;
            float zeroone1000s = ((float)(time % 1000000)) / 1000000.0f;
            float zeroone500s = ((float)(time % 500000)) / 500000.0f;
            float zeroone100s = ((float)(time % 100000)) / 100000.0f;
            float zeroone10s = ((float)(time % 10000)) / 10000.0f;
            float zeroone5s = ((float)(time % 5000)) / 5000.0f;
            float zerotwo5s = ((float)(time % 5000)) / 2500.0f;
            float timediv10s = (float)time / 10000.0f;
            float timediv100s = (float)time / 100000.0f;


            if (items.Contains("STAR-M3"))
            {
                var vid = items.Shader("STAR-M3").Get(OpenTK.Graphics.OpenGL4.ShaderType.VertexShader);
                ((GLPLVertexShaderModelCoordWithMatrixWorldTranslationCommonModelTranslation)vid).ModelTranslation = Matrix4.CreateRotationY((float)(-zeroone10s * Math.PI * 2));
                var stellarsurfaceshader = (GLPLStarSurfaceFragmentShader)items.Shader("STAR-M3").Get(OpenTK.Graphics.OpenGL4.ShaderType.FragmentShader);
                stellarsurfaceshader.TimeDeltaSpots = zeroone500s;
                stellarsurfaceshader.TimeDeltaSurface = timediv100s;
            }

            if ( tim != null )
            {
                tim.ModelTranslation = Matrix4.CreateRotationY((float)(-zeroone10s * Math.PI * 2));
                tim.TimeDeltaSpots = zeroone100s;
                tim.TimeDeltaSurface = timediv10s;
            }

            GLMatrixCalcUniformBlock mcub = (GLMatrixCalcUniformBlock)items.UB("MCUB");
            mcub.Set(gl3dcontroller.MatrixCalc);

            rObjects.Render(glwfc.RenderState,gl3dcontroller.MatrixCalc);
            GL.MemoryBarrier(MemoryBarrierFlags.AllBarrierBits);

            this.Text = //"Freq " + frequency.ToString("#.#########") + " unRadius " + unRadius + " scutoff" + scutoff + " BD " + blackdeepness + " CE " + concentrationequator
            "    Looking at " + gl3dcontroller.MatrixCalc.TargetPosition + " dir " + gl3dcontroller.PosCamera.CameraDirection + " Dist " + gl3dcontroller.MatrixCalc.EyeDistance;

        }

        private void SystemTick(object sender, EventArgs e )
        {
            gl3dcontroller.HandleKeyboardSlewsInvalidate(true, OtherKeys);
            gl3dcontroller.Redraw();
        }

        private void OtherKeys( OFC.Controller.KeyboardMonitor kb )
        {
            if ( kb.HasBeenPressed(Keys.F1))
            {
                genpos += 3;

                uint remove = tim.TagCount > 20 ? (tim.CurrentGeneration-oldestrelgen) : (tim.CurrentGeneration - 200);

                tim.CurrentGeneration++;
                System.Diagnostics.Debug.WriteLine("To make gen " + tim.CurrentGeneration + " last " + oldestrelgen + " remove " + remove + " Tag count " + tim.TagCount);
                oldestrelgen = tim.RemoveGeneration(remove);
                System.Diagnostics.Debug.WriteLine("oldest relative " + oldestrelgen);

        
                using (StringFormat fmt = new StringFormat(StringFormatFlags.NoWrap) { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Center })
                {
                    Size bitmapsize = new Size(64, 20);
                    float width = 2.5f;
                    Vector3 bannersize = new Vector3(width, 0, 0);
                    Font f = new Font("MS sans serif", 8f);
                    tim.Add("T1-" + genpos, "F" + genpos, f, Color.White, Color.Red, new Vector3(-10, 0, genpos), bannersize, new Vector3(-90F.Radians(), 0, 0), fmt, alphascale: 10, alphaend: 5);
                    tim.Add("T2" + genpos, "J" + genpos, f, Color.White, Color.Red, new Vector3(0, 0, genpos), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true);
                    tim.Add("T3" + genpos, "S" + genpos, f, Color.White, Color.Red, new Vector3(10, 0, genpos), bannersize, new Vector3(0, 0, 0), fmt, rotatetoviewer: true, rotateelevation: true);
                }
            }
        }
    }
}


