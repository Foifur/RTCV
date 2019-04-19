﻿using RTCV.CorruptCore;
using RTCV.NetCore;
using RTCV.NetCore.StaticTools;
using RTCV.UI.Modular;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Diagnostics;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows.Forms;
using static RTCV.UI.UI_Extensions;

namespace RTCV.UI
{
    public partial class UI_CoreForm : Form, IAutoColorize
    {
        //This form traps events and forwards them.
        //It contains the single UI_CanvasForm instance.

        public static UI_CoreForm thisForm;
        public static UI_CanvasForm cfForm;

        public CanvasGrid previousGrid = null;

        //Vallues used for padding and scaling properly in high dpi
        public static int xPadding;
        public static int corePadding; // height of the top bar
        public static int yPadding;


        public bool AutoCorrupt
        {
            get
            {
                return CorruptCore.CorruptCore.AutoCorrupt;
            }
            set
            {
                if (value)
                    btnAutoCorrupt.Text = " Stop Auto-Corrupt";
                else
                    btnAutoCorrupt.Text = " Start Auto-Corrupt";

                CorruptCore.CorruptCore.AutoCorrupt = value;
            }
        }


        public UI_CoreForm()
        {
            InitializeComponent();
            thisForm = this;
            this.FormClosing += UI_CoreForm_FormClosing;



            cfForm = new UI_CanvasForm();
            cfForm.TopLevel = false;
            cfForm.Dock = DockStyle.Fill;
            this.Controls.Add(cfForm);
            cfForm.Location = new Point(0, pnTopBar.Size.Height);
            cfForm.Show();
            cfForm.BringToFront();

            //For Horizontal tab-style menu in coreform
            //xPadding = (Width - cfForm.Width);
            //coreYPadding = pnTopBar.Height;
            //yPadding = (Height - cfForm.Height) - coreYPadding;

            //For Vertical tab-style menu in coreform
            yPadding = (Height - cfForm.Height);
            corePadding = pnTopBar.Width;
            xPadding = (Width - cfForm.Width) - corePadding;

            //UICore.SetRTCColor(UICore.GeneralColor);
        }

        private void UI_CoreForm_FormClosing(object sender, FormClosingEventArgs e)
        {
            if (S.GET<RTC_GlitchHarvester_Form>().UnsavedEdits && !UICore.isClosing && MessageBox.Show("You have unsaved edits in the Glitch Harvester Stockpile. \n\n Are you sure you want to close RTC without saving?", "Unsaved edits in Stockpile", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                e.Cancel = true;
                return;
            }

            LocalNetCoreRouter.Route(NetcoreCommands.VANGUARD, NetcoreCommands.REMOTE_EVENT_CLOSEEMULATOR);

            //Sleep to make sure the message is sent
            System.Threading.Thread.Sleep(500);

            UICore.CloseAllRtcForms();
        }

        private void UI_CoreForm_Load(object sender, EventArgs e)
        {
            btnLogo.Text = "RTCV " + CorruptCore.CorruptCore.RtcVersion;

            if (!NetCore.Params.IsParamSet("DISCLAIMER_READ"))
            {
                string disclaimer = @"Welcome to the Real-Time Corruptor
Version [ver]

Disclaimer:
This program comes with absolutely ZERO warranty.
You may use it at your own risk.

BizHawk+RTC is distributed under an MIT Licence.
Detailed information about other licences available at the
following path: BizHawk -> RTC -> LICENCES

Known facts(and warnings):
- Can generate incredible amounts of flashing and noise
- Can cause windows to BSOD (extremely rarely)
- Can write a significant amount of data to your hard drive depending on usage

This message only appears once.";

                //Use the text file if it exists
                if (File.Exists(CorruptCore.CorruptCore.RtcDir + Path.DirectorySeparatorChar + "LICENSES\\DISCLAIMER.TXT"))
                    disclaimer = File.ReadAllText(CorruptCore.CorruptCore.RtcDir + Path.DirectorySeparatorChar + "LICENSES\\DISCLAIMER.TXT");

                MessageBox.Show(disclaimer.Replace("[ver]", CorruptCore.CorruptCore.RtcVersion), "RTC", MessageBoxButtons.OK, MessageBoxIcon.Information);
                NetCore.Params.SetParam("DISCLAIMER_READ");
            }

            CorruptCore.CorruptCore.DownloadProblematicProcesses();

            //UI_DefaultGrids.engineConfig.LoadToMain();
        }

        public void SetSize(int x, int y)
        {
            //this.Size = new Size(x + xPadding, y + yPadding + coreYPadding); //For Horizontal tab-style menu in coreform
            this.Size = new Size(x + xPadding + corePadding, y + yPadding); //For Vertical tab-style menu in coreform
        }

        private void UI_CoreForm_ResizeBegin(object sender, EventArgs e)
        {
            //Sends event to SubForm 
            if(cfForm.spForm != null)
                cfForm.spForm.Parent_ResizeBegin();
            
        }


        private void UI_CoreForm_ResizeEnd(object sender, EventArgs e)
        {
            //Sends event to SubForm
            if (cfForm.spForm != null)
                cfForm.spForm?.Parent_ResizeEnd();
        }

        FormWindowState? LastWindowState = null;
        private void UI_CoreForm_Resize(object sender, EventArgs e)
        {
            // When window state changes
            if (WindowState != LastWindowState)
            {
                /*
                if (WindowState == FormWindowState.Maximized)
                {
                    // Maximized!
                }
                if (WindowState == FormWindowState.Normal)
                {
                    // Restored!
                }
                */

                if (cfForm.spForm != null)
                    cfForm.spForm?.Parent_ResizeEnd();

                LastWindowState = WindowState;
            }
        }

        private void button1_Click(object sender, EventArgs e)
        {
            
        }

        private void button4_Click(object sender, EventArgs e)
        {
            //test button, loads a dummy form in SubForm mode

            if (cfForm.spForm == null)
            {
                cfForm.ShowSubForm("UI_ComponentFormSubForm");
            }
            else
                cfForm.CloseSubForm();
        }

        public void btnEngineConfig_Click(object sender, EventArgs e)
        {
            UI_DefaultGrids.engineConfig.LoadToMain();

            //old garbage

            //Test button, creates forms using class names and coordinates.
            /*
            var GlitchHarvester = new CanvasGrid(6, 3);
            GlitchHarvester.SetTileForm("UI_SavestateManager", 0, 0);
            GlitchHarvester.SetTileForm("UI_Engine_Blast", 1, 0);
            GlitchHarvester.SetTileForm("UI_BlastManipulator", 2, 0);
            GlitchHarvester.SetTileForm("UI_BlastParameters", 1, 1);
            GlitchHarvester.SetTileForm("UI_StashHistory", 2, 1);
            GlitchHarvester.SetTileForm("UI_RenderOutput", 1, 2);
            GlitchHarvester.SetTileForm("UI_StockpileManager", 3, 0);
            */

            //EngineForm.SetTileForm("UI_Engine_MemoryDomains", 0, 2);
            /*
            var TestForm = new CanvasGrid(5, 4);
            TestForm.SetTileForm("UI_DummyTileForm3x1", 0, 0);
            TestForm.SetTileForm("UI_DummyTileForm2x1", 3, 2);
            TestForm.SetTileForm("UI_DummyTileForm2x2", 3, 0);
            TestForm.SetTileForm("UI_DummyTileForm1x1", 4, 3);
            TestForm.SetTileForm("UI_DummyTileForm3x3", 0, 1);
            */

            /*
            var multiGrid = new MultiGrid(
                EngineGrid,
                GlitchHarvester,
                TestForm
            );

            multiGrid.Load();
            */

            /*
            var TestGrid = new CanvasGrid(13, 8, "Glitch Harvester");
            TestGrid.SetTileForm("Glitch Harvester", 0, 0, 9, 8, false);
            TestGrid.LoadToNewWindow();
            */


            /*
            var multiGrid = new MultiGrid(
                EngineGrid,
                TestGrid
            );
            */

            //multiGrid.Load();

            //var tileForm = (UI_ComponentFormTile)UI_CanvasForm.getTileForm("UI_ComponentFormTile");
            //tileForm.SetCompoentForm("ComponentForm host", 4, 4);

        }

        private void pnAutoKillSwitch_MouseHover(object sender, EventArgs e)
        {
            lbAks.ForeColor = Color.FromArgb(32, 32, 32);
            pnAutoKillSwitch.BackColor = Color.Salmon;
        }

        private void pnAutoKillSwitch_MouseLeave(object sender, EventArgs e)
        {
            lbAks.ForeColor = Color.White;
            pnAutoKillSwitch.BackColor = Color.Transparent;
        }

        private void btnGlitchHarvester_Click(object sender, EventArgs e)
        {
            pnGlitchHarvesterOpen.Visible = true;

            S.GET<RTC_GlitchHarvester_Form>().Show();
            S.GET<RTC_GlitchHarvester_Form>().Focus();
        }

        public void btnAutoCorrupt_Click(object sender, EventArgs e)
        {
            if (btnAutoCorrupt.ForeColor == Color.Silver)
                return;

            AutoCorrupt = !AutoCorrupt;
            if (AutoCorrupt)
                RTCV.NetCore.AllSpec.CorruptCoreSpec.Update(RTCSPEC.STEP_RUNBEFORE.ToString(), true);
        }

        private void btnManualBlast_Click(object sender, EventArgs e)
        {
            LocalNetCoreRouter.Route(NetcoreCommands.CORRUPTCORE, NetcoreCommands.ASYNCBLAST, true);
        }

        private void btnEasyMode_MouseDown(object sender, MouseEventArgs e)
        {
            Point locate = new Point(((Button)sender).Location.X + e.Location.X, ((Button)sender).Location.Y + e.Location.Y);

            ContextMenuStrip easyButtonMenu = new ContextMenuStrip();
                                                                                                              //Refactor this shit later.
            easyButtonMenu.Items.Add("Start with Recommended Settings", null, new EventHandler(((ob, ev) => { S.GET<UI_CoreForm>().StartEasyMode(true); })));
            easyButtonMenu.Items.Add(new ToolStripSeparator());
            //EasyButtonMenu.Items.Add("Watch a tutorial video", null, new EventHandler((ob,ev) => Process.Start("https://www.youtube.com/watch?v=sIELpn4-Umw"))).Enabled = false;
            easyButtonMenu.Items.Add("Open the online wiki", null, new EventHandler((ob, ev) => Process.Start("https://corrupt.wiki/")));
            easyButtonMenu.Show(this, locate);
        }

        private void btnStockpilePlayer_Click(object sender, EventArgs e)
        {
            UI_DefaultGrids.stockpilePlayer.LoadToMain();
        }

        public void btnSettings_Click(object sender, EventArgs e)
        {
            UI_DefaultGrids.settings.LoadToMain();
        }

        private void pnAutoKillSwitch_MouseClick(object sender, MouseEventArgs e)
        {
            //needed anymore?
            S.GET<UI_CoreForm>().btnLogo_Click(sender, e);

            S.GET<UI_CoreForm>().pbAutoKillSwitchTimeout.Value = S.GET<UI_CoreForm>().pbAutoKillSwitchTimeout.Maximum;
            AutoKillSwitch.ShouldKillswitchFire = true;

            //refactor this to not use string once old coreform is dead
            AutoKillSwitch.KillEmulator("KILL + RESTART", true);
        }

        private void cbUseAutoKillSwitch_CheckedChanged(object sender, EventArgs e)
        {
            pbAutoKillSwitchTimeout.Visible = cbUseAutoKillSwitch.Checked;
            AutoKillSwitch.Enabled = cbUseAutoKillSwitch.Checked;
        }

        private void LbGameProtection_MouseClick(object sender, MouseEventArgs e)
        {
            cbUseGameProtection.Checked = !cbUseGameProtection.Checked;
        }
        private void cbUseGameProtection_CheckedChanged(object sender, EventArgs e)
        {
            if (cbUseGameProtection.Checked)
            {
                GameProtection.Start();
            }
            else
            {
                GameProtection.Stop();
                StockpileManager_UISide.BackupedState = null;
                StockpileManager_UISide.AllBackupStates.Clear();
                btnGpJumpBack.Visible = false;
                btnGpJumpNow.Visible = false;
            }
        }

        public void btnGpJumpBack_Click(object sender, EventArgs e)
        {
            try
            {
                btnGpJumpBack.Visible = false;

                if (StockpileManager_UISide.AllBackupStates.Count == 0)
                    return;

                StashKey sk = StockpileManager_UISide.AllBackupStates.Pop();

                sk?.Run();

                GameProtection.Reset();
            }
            finally
            {
                if (StockpileManager_UISide.AllBackupStates.Count != 0)
                    btnGpJumpBack.Visible = true;
            }
        }

        public void btnGpJumpNow_Click(object sender, EventArgs e)
        {
            try
            {
                btnGpJumpNow.Visible = false;

                if (StockpileManager_UISide.BackupedState != null)
                    StockpileManager_UISide.BackupedState.Run();

                GameProtection.Reset();
            }
            finally
            {
                btnGpJumpNow.Visible = true;
            }
        }

        private void btnLogo_MouseClick(object sender, MouseEventArgs e)
        {

        }

        private void btnLogo_Click(object sender, EventArgs e)
        {
            UI_DefaultGrids.connectionStatus.LoadToMain();
        }

        public void StartEasyMode(bool useTemplate)
        {
            //	if (RTC_NetcoreImplementation.isStandaloneUI && !S.GET<RTC_Core_Form>().cbUseGameProtection.Checked)
            S.GET<UI_CoreForm>().cbUseGameProtection.Checked = true;


            if (useTemplate)
            {
                //Put Console templates HERE
                string thisSystem = (string)RTCV.NetCore.AllSpec.VanguardSpec[VSPEC.SYSTEM];

                switch (thisSystem)
                {
                    case "NES":     //Nintendo Entertainment system
                        SetEngineByName("Nightmare Engine");
                        S.GET<RTC_GeneralParameters_Form>().multiTB_Intensity.Value = 2;
                        S.GET<RTC_GeneralParameters_Form>().multiTB_ErrorDelay.Value = 1;
                        break;

                    case "GB":      //Gameboy
                    case "GBC":     //Gameboy Color
                        SetEngineByName("Nightmare Engine");
                        S.GET<RTC_GeneralParameters_Form>().multiTB_Intensity.Value = 1;
                        S.GET<RTC_GeneralParameters_Form>().multiTB_ErrorDelay.Value = 4;
                        break;

                    case "SNES":    //Super Nintendo
                        SetEngineByName("Nightmare Engine");
                        S.GET<RTC_GeneralParameters_Form>().multiTB_Intensity.Value = 1;
                        S.GET<RTC_GeneralParameters_Form>().multiTB_ErrorDelay.Value = 2;
                        break;

                    case "GBA":     //Gameboy Advance
                        SetEngineByName("Nightmare Engine");
                        S.GET<RTC_GeneralParameters_Form>().multiTB_Intensity.Value = 1;
                        S.GET<RTC_GeneralParameters_Form>().multiTB_ErrorDelay.Value = 1;
                        break;

                    case "N64":     //Nintendo 64
                        SetEngineByName("Vector Engine");
                        S.GET<RTC_GeneralParameters_Form>().multiTB_Intensity.Value = 75;
                        S.GET<RTC_GeneralParameters_Form>().multiTB_ErrorDelay.Value = 1;
                        break;

                    case "SG":      //Sega SG-1000
                    case "GG":      //Sega GameGear
                    case "SMS":     //Sega Master System
                    case "GEN":     //Sega Genesis and CD
                    case "PCE":     //PC-Engine / Turbo Grafx
                    case "PSX":     //Sony Playstation 1
                    case "A26":     //Atari 2600
                    case "A78":     //Atari 7800
                    case "LYNX":    //Atari Lynx
                    case "INTV":    //Intellivision
                    case "PCECD":   //related to PC-Engine / Turbo Grafx
                    case "SGX":     //related to PC-Engine / Turbo Grafx
                    case "TI83":    //Ti-83 Calculator
                    case "WSWAN":   //Wonderswan
                    case "C64":     //Commodore 64
                    case "Coleco":  //Colecovision
                    case "SGB":     //Super Gameboy
                    case "SAT":     //Sega Saturn
                    case "DGB":
                        MessageBox.Show("WARNING: No Easy-Mode template was made for this system. Please configure it manually and use the current settings.");
                        return;

                        //TODO: Add more domains for systems like gamegear, atari, turbo graphx
                }
            }

            S.GET<UI_CoreForm>().AutoCorrupt = true;
        }

        public void SetEngineByName(string name)
        {
            //Selects an engine from a given string name

            for (int i = 0; i < S.GET<RTC_CorruptionEngine_Form>().cbSelectedEngine.Items.Count; i++)
                if (S.GET<RTC_CorruptionEngine_Form>().cbSelectedEngine.Items[i].ToString() == name)
                {
                    S.GET<RTC_CorruptionEngine_Form>().cbSelectedEngine.SelectedIndex = i;
                    break;
                }
        }
    }
}