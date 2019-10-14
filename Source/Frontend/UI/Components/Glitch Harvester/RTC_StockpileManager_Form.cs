﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Drawing;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using System.Xml.Serialization;
using System.Runtime.InteropServices;
using System.Threading.Tasks;
using RTCV.CorruptCore;
using RTCV.NetCore;
using static RTCV.UI.UI_Extensions;
using RTCV.NetCore.StaticTools;
using RTCV.UI.Modular;

namespace RTCV.UI
{
	public partial class RTC_StockpileManager_Form : ComponentForm, IAutoColorize
	{
		public new void HandleMouseDown(object s, MouseEventArgs e) => base.HandleMouseDown(s, e);
		public new void HandleFormClosing(object s, FormClosingEventArgs e) => base.HandleFormClosing(s, e);

        public bool DontLoadSelectedStockpile = false;

        private Color? originalSaveButtonColor = null;
        private bool _UnsavedEdits= false;
        public bool UnsavedEdits
        {
            get
            {
                return _UnsavedEdits;
            }
            set
            {
                _UnsavedEdits = value;
                
                if(_UnsavedEdits && btnSaveStockpile.Enabled)
                {
                    if (originalSaveButtonColor == null)
                        originalSaveButtonColor = btnSaveStockpile.BackColor;

                    btnSaveStockpile.BackColor = Color.Tomato;
                }
                else
                {
                    if (originalSaveButtonColor != null)
                        btnSaveStockpile.BackColor = originalSaveButtonColor.Value;
                }
            }
        }

        public RTC_StockpileManager_Form()
		{
			InitializeComponent();

			popoutAllowed = true;
            this.undockedSizable = true;

            dgvStockpile.RowsAdded += (o, e) => {
                RefreshNoteIcons();
            };
        }

        private void dgvStockpile_CellClick(object sender, DataGridViewCellEventArgs e)
        {
            if (e.RowIndex == -1)
                return;
            try
            {
                S.GET<RTC_StashHistory_Form>().btnAddStashToStockpile.Enabled = false;
                dgvStockpile.Enabled = false;
                btnStockpileUP.Enabled = false;
                btnStockpileDOWN.Enabled = false;

                // Stockpile Note handling
                if (e != null)
                {
                    var senderGrid = (DataGridView)sender;

                    if (senderGrid.Columns[e.ColumnIndex] is DataGridViewButtonColumn &&
                        e.RowIndex >= 0)
                    {
                        StashKey sk = (StashKey)senderGrid.Rows[e.RowIndex].Cells["Item"].Value;
                        S.SET(new RTC_NoteEditor_Form(sk, senderGrid.Rows[e.RowIndex].Cells["Note"]));
                        S.GET<RTC_NoteEditor_Form>().Show();

                        return;
                    }
                }

                S.GET<RTC_StashHistory_Form>().lbStashHistory.ClearSelected();
                S.GET<RTC_StockpilePlayer_Form>().dgvStockpile.ClearSelection();

                S.GET<RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();

                if (dgvStockpile.SelectedRows.Count == 0)
                    return;

                StockpileManager_UISide.CurrentStashkey = (dgvStockpile.SelectedRows[0].Cells[0].Value as StashKey);

                List<StashKey> keys = dgvStockpile.Rows.Cast<DataGridViewRow>().Select(x => (StashKey)x.Cells[0].Value).ToList();
                if (!StockpileManager_UISide.CheckAndFixMissingReference(StockpileManager_UISide.CurrentStashkey, false, keys))
                    return;

                if (!S.GET<RTC_GlitchHarvesterBlast_Form>().LoadOnSelect)
                    return;

                // Merge Execution
                if (dgvStockpile.SelectedRows.Count > 1)
                {
                    List<StashKey> sks = new List<StashKey>();

                    foreach (DataGridViewRow row in dgvStockpile.SelectedRows)
                        sks.Add((StashKey)row.Cells[0].Value);
                    //dgv is stupid since it selects rows backwards
                    sks.Reverse();
                    StockpileManager_UISide.MergeStashkeys(sks);

                    if (Render.RenderAtLoad && S.GET<RTC_GlitchHarvesterBlast_Form>().loadBeforeOperation)
                        Render.StartRender();

                    S.GET<RTC_StashHistory_Form>().RefreshStashHistory();
                    return;
                }

                S.GET<RTC_GlitchHarvesterBlast_Form>().OneTimeExecute();
            }
            finally
            {
                S.GET<RTC_StashHistory_Form>().btnAddStashToStockpile.Enabled = true;
                dgvStockpile.Enabled = true;
                btnStockpileUP.Enabled = true;
                btnStockpileDOWN.Enabled = true;
                
            }

            S.GET<RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();
        }

        private void dgvStockpile_MouseDown(object sender, MouseEventArgs e)
        {
            if (e.Button == MouseButtons.Right)
            {
                Point locate = new Point((sender as Control).Location.X + e.Location.X, (sender as Control).Location.Y + e.Location.Y);

                ContextMenuStrip columnsMenu = new ContextMenuStrip();
                (columnsMenu.Items.Add("Show Item Name", null,
                        (ob, ev) => { dgvStockpile.Columns["Item"].Visible ^= true; }) as ToolStripMenuItem).Checked =
                    dgvStockpile.Columns["Item"].Visible;
                (columnsMenu.Items.Add("Show Game Name", null,
                        (ob, ev) => { dgvStockpile.Columns["GameName"].Visible ^= true; }) as ToolStripMenuItem)
                    .Checked =
                    dgvStockpile.Columns["GameName"].Visible;
                (columnsMenu.Items.Add("Show System Name", null,
                        (ob, ev) => { dgvStockpile.Columns["SystemName"].Visible ^= true; }) as ToolStripMenuItem)
                    .Checked =
                    dgvStockpile.Columns["SystemName"].Visible;
                (columnsMenu.Items.Add("Show System Core", null,
                        (ob, ev) => { dgvStockpile.Columns["SystemCore"].Visible ^= true; }) as ToolStripMenuItem)
                    .Checked =
                    dgvStockpile.Columns["SystemCore"].Visible;
                (columnsMenu.Items.Add("Show Note", null, (ob, ev) => { dgvStockpile.Columns["Note"].Visible ^= true; })
                    as ToolStripMenuItem).Checked = dgvStockpile.Columns["Note"].Visible;

                columnsMenu.Items.Add(new ToolStripSeparator());
                ((ToolStripMenuItem)columnsMenu.Items.Add("Open Selected Item in Blast Editor", null, new EventHandler((ob, ev) =>
                {
                    if (S.GET<RTC_NewBlastEditor_Form>() != null)
                    {
                        var sk = (dgvStockpile.SelectedRows[0].Cells[0].Value as StashKey);
                        RTC_NewBlastEditor_Form.OpenBlastEditor(sk);
                    }
                }))).Enabled = (dgvStockpile.SelectedRows.Count == 1);

                columnsMenu.Items.Add(new ToolStripSeparator());
                ((ToolStripMenuItem)columnsMenu.Items.Add("Generate VMD from Selected Item", null, new EventHandler((ob, ev) =>
                {
                    var sk = (dgvStockpile.SelectedRows[0].Cells[0].Value as StashKey);
                    MemoryDomains.GenerateVmdFromStashkey(sk);
                    S.GET<RTC_VmdPool_Form>().RefreshVMDs();
                }))).Enabled = (dgvStockpile.SelectedRows.Count == 1);

                ((ToolStripMenuItem)columnsMenu.Items.Add("Merge Selected Stashkeys", null, new EventHandler((ob, ev) =>
                {
                    List<StashKey> sks = new List<StashKey>();
                    foreach (DataGridViewRow row in dgvStockpile.SelectedRows)
                        sks.Add((StashKey)row.Cells[0].Value);
                    StockpileManager_UISide.MergeStashkeys(sks);
                    S.GET<RTC_StashHistory_Form>().RefreshStashHistory();
                }))).Enabled = (dgvStockpile.SelectedRows.Count > 1);

                ((ToolStripMenuItem)columnsMenu.Items.Add("Replace associated ROM", null, new EventHandler((ob, ev) =>
                {
                    List<StashKey> sks = new List<StashKey>();
                    foreach (DataGridViewRow row in dgvStockpile.SelectedRows)
                        sks.Add((StashKey)row.Cells[0].Value);

                    OpenFileDialog ofd = new OpenFileDialog
                    {
                        DefaultExt = "*",
                        Title = "Select Replacement File",
                        Filter = "Any file|*.*",
                        RestoreDirectory = true
                    };
                    if (ofd.ShowDialog() == DialogResult.OK)
                    {
                        string filename = ofd.FileName.ToString();
                        string oldFilename = sks.First().RomFilename;
                        foreach (var sk in sks.Where(x => x.RomFilename == oldFilename))
                        {
                            sk.RomFilename = filename;
                            sk.RomShortFilename = Path.GetFileName(sk.RomFilename);
                        }
                    }
                }))).Enabled = (dgvStockpile.SelectedRows.Count >= 1);

                /*
				if (!RTC_NetcoreImplementation.isStandaloneUI)
				{
					((ToolStripMenuItem)columnsMenu.Items.Add("[Multiplayer] Send Selected Item as a Blast", null, new EventHandler((ob, ev) => { RTC_NetcoreImplementation.Multiplayer?.SendBlastlayer(); }))).Enabled = RTC_NetcoreImplementation.Multiplayer != null && RTC_NetcoreImplementation.Multiplayer.side != NetworkSide.DISCONNECTED;
					((ToolStripMenuItem)columnsMenu.Items.Add("[Multiplayer] Send Selected Item as a Game State", null, new EventHandler((ob, ev) => { RTC_NetcoreImplementation.Multiplayer?.SendStashkey(); }))).Enabled = RTC_NetcoreImplementation.Multiplayer != null && RTC_NetcoreImplementation.Multiplayer.side != NetworkSide.DISCONNECTED;
				}*/

                columnsMenu.Show(this, locate);
            }
        }

        public void RefreshNoteIcons()
        {
            foreach (DataGridViewRow dataRow in dgvStockpile.Rows)
            {
                StashKey sk = (StashKey)dataRow.Cells["Item"].Value;
                if (sk == null)
                    continue;
                if (String.IsNullOrWhiteSpace(sk.Note))
                {
                    dataRow.Cells["Note"].Value = "";
                }
                else
                {
                    dataRow.Cells["Note"].Value = "📝";
                }
            }
        }

        public void renameStashKey(StashKey sk)
        {
            string value = "";

            if (GetInputBox("Glitch Harvester", "Enter the new Stash name:", ref value) == DialogResult.OK && !String.IsNullOrWhiteSpace(value))
            {
                sk.Alias = value.Trim();
            }
            else
            {
                return;
            }
        }

        private void btnRenameSelected_Click(object sender, EventArgs e)
        {
            if (!btnRenameSelected.Visible)
                return;


            if (dgvStockpile.SelectedRows.Count != 0)
            {
                renameStashKey(dgvStockpile.SelectedRows[0].Cells[0].Value as StashKey);

                dgvStockpile.Refresh();
                //lbStockpile.RefreshItemsReal();
            }

            StockpileManager_UISide.StockpileChanged();

            UnsavedEdits = true;

        }

        private void btnRemoveSelectedStockpile_Click(object sender, EventArgs e) => RemoveSelected();

        public void RemoveSelected(bool force = false)
        {

            if (Control.ModifierKeys == Keys.Control || (dgvStockpile.SelectedRows.Count != 0 && (MessageBox.Show("Are you sure you want to remove the selected stockpile entries?", "Delete Stockpile Entry?", MessageBoxButtons.YesNo) == DialogResult.Yes)))
                foreach (DataGridViewRow row in dgvStockpile.SelectedRows)
                    dgvStockpile.Rows.Remove(row);

            StockpileManager_UISide.StockpileChanged();

            UnsavedEdits = true;

            S.GET<RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();

        }

        private void btnClearStockpile_Click(object sender, EventArgs e) => ClearStockpile();

        public void ClearStockpile(bool force = false)
        {

            if (force || MessageBox.Show("Are you sure you want to clear the stockpile?", "Clearing stockpile", MessageBoxButtons.YesNo) == DialogResult.Yes)
            {
                dgvStockpile.Rows.Clear();

                if (StockpileManager_UISide.CurrentStockpile != null)
                {
                    StockpileManager_UISide.CurrentStockpile.Filename = null;
                    StockpileManager_UISide.CurrentStockpile.ShortFilename = null;
                }

                btnSaveStockpile.Enabled = false;

                StockpileManager_UISide.StockpileChanged();

                UnsavedEdits = false;

                S.GET <RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();

            }
        }

        private async void LoadStockpile(string filename)
        {
            if (UnsavedEdits && MessageBox.Show("You have unsaved edits in the Glitch Harvester Stockpile. \n\n Are you sure you want to load without saving?",
                "Unsaved edits in Stockpile", MessageBoxButtons.YesNo) == DialogResult.No)
            {
                return;
            }

			var ghForm = UI_CanvasForm.GetExtraForm("Glitch Harvester");
			try
			{

				//We do this here and invoke because our unlock runs at the end of the awaited method, but there's a chance an error occurs 
				//Thus, we want this to happen within the try block
				SyncObjectSingleton.FormExecute(() =>
				{
					UICore.LockInterface(false, true);
					S.GET<UI_SaveProgress_Form>().Dock = DockStyle.Fill;
					ghForm?.OpenSubForm(S.GET<UI_SaveProgress_Form>());
				});

                await Task.Run(() =>
                {
					if (Stockpile.Load(dgvStockpile, filename))
					{
						SyncObjectSingleton.FormExecute(() =>
						{
							btnSaveStockpile.Enabled = true;
							RefreshNoteIcons();
						});
					}

					SyncObjectSingleton.FormExecute(() =>
					{
						S.GET<RTC_StockpilePlayer_Form>().dgvStockpile.Rows.Clear();

						dgvStockpile.ClearSelection();
						StockpileManager_UISide.StockpileChanged();

						UnsavedEdits = false;
					});
				});
			}
			finally
			{
				SyncObjectSingleton.FormExecute(() =>
				{
					ghForm?.CloseSubForm();
					UICore.UnlockInterface();
				});
			}
        }

        private void btnLoadStockpile_Click(object sender, MouseEventArgs e)
        {
            CorruptCore.RtcCore.CheckForProblematicProcesses();

            Point locate = new Point(((Control)sender).Location.X + e.Location.X, ((Control)sender).Location.Y + e.Location.Y);

            ContextMenuStrip loadMenuItems = new ContextMenuStrip();
            loadMenuItems.Items.Add("Load Stockpile", null, new EventHandler((ob, ev) =>
			{
				string filename = "";
				OpenFileDialog ofd = new OpenFileDialog
				{
					DefaultExt = "sks",
					Title = "Open Stockpile File",
					Filter = "SKS files|*.sks",
					RestoreDirectory = true
				};
				if (ofd.ShowDialog() == DialogResult.OK)
					filename = ofd.FileName;
				else
					return;
                LoadStockpile(filename);
            }));


            loadMenuItems.Items.Add($"Load {RtcCore.VanguardImplementationName} settings from Stockpile", null, new EventHandler((ob, ev) =>
            {
                try
                {
                    if (UnsavedEdits && MessageBox.Show($"You have unsaved edits in the Glitch Harvester Stockpile. \n\n This will restart {RtcCore.VanguardImplementationName}. Are you sure you want to load without saving?",
                        "Unsaved edits in Stockpile", MessageBoxButtons.YesNo) == DialogResult.No)
                    {
                        return;
                    }
                    AutoKillSwitch.Enabled = false;
                    Stockpile.LoadConfigFromStockpile();
                    AutoKillSwitch.Enabled = true;
                }
                finally
                {
                }
            }));

            loadMenuItems.Items.Add($"Restore {RtcCore.VanguardImplementationName} config Backup", null, new EventHandler((ob, ev) =>
            {
                try
                {
                    if (UnsavedEdits && MessageBox.Show(
                        $"You have unsaved edits in the Glitch Harvester Stockpile. \n\n This will restart {RtcCore.VanguardImplementationName}. Are you sure you want to load without saving?",
                        "Unsaved edits in Stockpile", MessageBoxButtons.YesNo) == DialogResult.No)
                        return;
                    AutoKillSwitch.Enabled = false;
                    Stockpile.RestoreEmuConfig();
                    AutoKillSwitch.Enabled = true;
                }
                finally
                {
                }
            })).Enabled = (File.Exists(Path.Combine(CorruptCore.RtcCore.EmuDir, "backup_config.ini")));

            loadMenuItems.Show(this, locate);
        }

        public async void btnSaveStockpileAs_Click(object sender, EventArgs e)
        {
            if (dgvStockpile.Rows.Count == 0)
            {
                MessageBox.Show("You cannot save the Stockpile because it is empty");
                return;
            }

			var ghForm = UI_CanvasForm.GetExtraForm("Glitch Harvester");
            try
			{
				UICore.SetHotkeyTimer(false);
				string path = "";
				SaveFileDialog saveFileDialog1 = new SaveFileDialog
				{
					DefaultExt = "sks",
					Title = "Save Stockpile File",
					Filter = "SKS files|*.sks",
					RestoreDirectory = true
				};

				if (saveFileDialog1.ShowDialog() == DialogResult.OK)
				{
					path = saveFileDialog1.FileName;
				}
				else
					return;

				Stockpile sks = new Stockpile(dgvStockpile);
                //We do this here and invoke because our unlock runs at the end of the awaited method, but there's a chance an error occurs 
                //Thus, we want this to happen within the try block
                SyncObjectSingleton.FormExecute(() =>
				{
					UICore.LockInterface(false, true);
					S.GET<UI_SaveProgress_Form>().Dock = DockStyle.Fill;
					ghForm?.OpenSubForm(S.GET<UI_SaveProgress_Form>());
				});

				await Task.Run(() =>
                {
                   saveStockpile(sks, path);
                });
			}
			finally
			{
				SyncObjectSingleton.FormExecute(() =>
				{
					ghForm?.CloseSubForm();
					UICore.UnlockInterface();
					UICore.SetHotkeyTimer(true);
                });
			}

        }
		private void saveStockpile(Stockpile sks, string path)
		{
			if (Stockpile.Save(sks, path, RTCV.NetCore.Params.IsParamSet("INCLUDE_REFERENCED_FILES"), RTCV.NetCore.Params.IsParamSet("COMPRESS_STOCKPILE")))
				SyncObjectSingleton.FormExecute(() =>
				{
					sendCurrentStockpileToSKS();
					UnsavedEdits = false;
                    btnSaveStockpile.Enabled = true;
                });
        }

        private async void btnSaveStockpile_Click(object sender, EventArgs e)
        {
			Stockpile sks = new Stockpile(dgvStockpile);
			var ghForm = UI_CanvasForm.GetExtraForm("Glitch Harvester");
            try
            {
				UICore.SetHotkeyTimer(false);
                //We do this here and invoke because our unlock runs at the end of the awaited method, but there's a chance an error occurs 
                //Thus, we want this to happen within the try block
                SyncObjectSingleton.FormExecute(() =>
				{
					UICore.LockInterface(false, true);
					S.GET<UI_SaveProgress_Form>().Dock = DockStyle.Fill;
					ghForm?.OpenSubForm(S.GET<UI_SaveProgress_Form>());
				});

				await Task.Run(() => { saveStockpile(sks, StockpileManager_UISide.CurrentStockpile.Filename); });
			}
			finally
			{
				SyncObjectSingleton.FormExecute(() =>
				{
					ghForm?.CloseSubForm();
					UICore.UnlockInterface();
					UICore.SetHotkeyTimer(true);
                });
            }
		}

        private void sendCurrentStockpileToSKS()
        {
            foreach (DataGridViewRow dataRow in dgvStockpile.Rows)
            {
                StashKey sk = (StashKey)dataRow.Cells["Item"].Value;
            }
        }

        private void btnStockpileMoveSelectedUp_Click(object sender, EventArgs e)
        {
            var selectedRows = dgvStockpile.SelectedRows.Cast<DataGridViewRow>().ToArray();
            foreach (DataGridViewRow row in selectedRows)
            {
                int pos = row.Index;
                dgvStockpile.Rows.RemoveAt(pos);

                if (pos == 0)
                {
                    dgvStockpile.Rows.Add(row);
                }
                else
                {
                    int newpos = pos - 1;
                    dgvStockpile.Rows.Insert(newpos, row);
                }
            }
            dgvStockpile.ClearSelection();
            foreach (DataGridViewRow row in selectedRows) //I don't know. Blame DGV
            {
                row.Selected = true;
            }

            UnsavedEdits = true;

            StockpileManager_UISide.StockpileChanged();
            S.GET<RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();
        }

        private void btnStockpileMoveSelectedDown_Click(object sender, EventArgs e)
        {
            var selectedRows = dgvStockpile.SelectedRows.Cast<DataGridViewRow>().ToArray();
            foreach (DataGridViewRow row in selectedRows)
            {
                int pos = row.Index;
                int count = dgvStockpile.Rows.Count;
                dgvStockpile.Rows.RemoveAt(pos);
                
                if (pos == count - 1)
                {
                    int newpos = 0;
                    dgvStockpile.Rows.Insert(newpos, row);
                }
                else
                {
                    int newpos = pos + 1;
                    dgvStockpile.Rows.Insert(newpos, row);
                }
            }
            dgvStockpile.ClearSelection();
            foreach (DataGridViewRow row in selectedRows) //I don't know. Blame DGV
            {
                row.Selected = true;
            }

            UnsavedEdits = true;

            StockpileManager_UISide.StockpileChanged();
            S.GET<RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();
        }

        private void dgvStockpile_DragDrop(object sender, DragEventArgs e)
        {
            string[] files = (string[])e.Data.GetData(DataFormats.FileDrop, false);
            if (files?.Length > 0 && files[0]
                .Contains(".sks"))
            {
                LoadStockpile(files[0]);
            }

            //Bring the UI back to normal after a drag+drop to prevent weird merge stuff 
            S.GET<RTC_GlitchHarvesterBlast_Form>().RedrawActionUI();
        }

        private void dgvStockpile_DragEnter(object sender, DragEventArgs e)
        {
            e.Effect = DragDropEffects.Link;
        }

		private async void btnImportStockpile_Click(object sender, EventArgs e)
		{
			OpenFileDialog ofd = new OpenFileDialog
			{
				DefaultExt = "*", 
				Title = "Select stockpile to import",
				Filter = "Any file|*.sks",
				RestoreDirectory = true
			};
			if (ofd.ShowDialog() == DialogResult.OK)
			{
				var ghForm = UI_CanvasForm.GetExtraForm("Glitch Harvester");
				try
				{
					UICore.SetHotkeyTimer(false);
					//We do this here and invoke because our unlock runs at the end of the awaited method, but there's a chance an error occurs 
					//Thus, we want this to happen within the try block
					SyncObjectSingleton.FormExecute(() =>
					{
						UICore.LockInterface(false, true);
						S.GET<UI_SaveProgress_Form>().Dock = DockStyle.Fill;
						ghForm?.OpenSubForm(S.GET<UI_SaveProgress_Form>());
					});

					await Task.Run(() =>
					{
						if (Stockpile.Import(ofd.FileName, dgvStockpile))
							UnsavedEdits = true;
					});
				}
				finally
				{
					SyncObjectSingleton.FormExecute(() =>
					{
						ghForm?.CloseSubForm();
						UICore.UnlockInterface();
						UICore.SetHotkeyTimer(true);
						RefreshNoteIcons();
					});
				}
			}
		}
		private void btnStockpileUP_Click(object sender, EventArgs e)
        {

            if (dgvStockpile.SelectedRows.Count == 0)
                return;

            int currentSelectedIndex = dgvStockpile.SelectedRows[0].Index;

            if (currentSelectedIndex == 0)
            {
                dgvStockpile.ClearSelection();
                dgvStockpile.Rows[dgvStockpile.Rows.Count - 1].Selected = true;
            }
            else
            {
                dgvStockpile.ClearSelection();
                dgvStockpile.Rows[currentSelectedIndex - 1].Selected = true;
            }

            dgvStockpile_CellClick(dgvStockpile, null);
        }

        private void btnStockpileDOWN_Click(object sender, EventArgs e)
        {

            if (dgvStockpile.SelectedRows.Count == 0)
                return;

            int currentSelectedIndex = dgvStockpile.SelectedRows[0].Index;

            if (currentSelectedIndex == dgvStockpile.Rows.Count - 1)
            {
                dgvStockpile.ClearSelection();
                dgvStockpile.Rows[0].Selected = true;
            }
            else
            {
                dgvStockpile.ClearSelection();
                dgvStockpile.Rows[currentSelectedIndex + 1].Selected = true;
            }

            dgvStockpile_CellClick(dgvStockpile, null);
        }

        private void RTC_StockpileManager_Form_Load(object sender, EventArgs e)
        {
            dgvStockpile.AllowDrop = true;
            dgvStockpile.DragDrop += dgvStockpile_DragDrop;
            dgvStockpile.DragEnter += dgvStockpile_DragEnter;

        }

        private void btnGlitchHarvesterSettings_MouseDown(object sender, MouseEventArgs e)
        {
            Point locate = e.GetMouseLocation(sender);
            ContextMenuStrip ghSettingsMenu = new ContextMenuStrip();

            ghSettingsMenu.Items.Add(new ToolStripLabel("Stockpile Manager settings")
            {
                Font = new Font("Segoe UI", 12)
            });
            
            ((ToolStripMenuItem)ghSettingsMenu.Items.Add("Compress Stockpiles", null, new EventHandler((ob, ev) => {

                if (RTCV.NetCore.Params.IsParamSet("COMPRESS_STOCKPILE"))
                    RTCV.NetCore.Params.RemoveParam("COMPRESS_STOCKPILE");
                else
                    RTCV.NetCore.Params.SetParam("COMPRESS_STOCKPILE");

            }))).Checked = RTCV.NetCore.Params.IsParamSet("COMPRESS_STOCKPILE");

            ((ToolStripMenuItem)ghSettingsMenu.Items.Add("Include referenced files", null, new EventHandler((ob, ev) =>
            {
                if (RTCV.NetCore.Params.IsParamSet("INCLUDE_REFERENCED_FILES"))
                    RTCV.NetCore.Params.RemoveParam("INCLUDE_REFERENCED_FILES");
                else
                    RTCV.NetCore.Params.SetParam("INCLUDE_REFERENCED_FILES");

            }))).Checked = RTCV.NetCore.Params.IsParamSet("INCLUDE_REFERENCED_FILES");



            ghSettingsMenu.Show(this, locate);
        }
    }
}
